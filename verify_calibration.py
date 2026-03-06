import numpy as np
import json

# Actual calibration data from JSON
with open(r"C:\Users\Simbiosis\AppData\Local\Packages\50fa64ab-bbad-4118-9d39-022f9b669499_3de5qd2wwgft2\LocalCache\Local\App4\HandEyeCalibration.json", "r") as f:
    data = json.load(f)

flanges = []
sensors = []
for rec in data["PoseRecords"]:
    f = rec["FlangeInBase"]
    s = rec["TargetInSensor"]
    # TransformMatrix layout: [Ix,Iy,Iz, Jx,Jy,Jz, Kx,Ky,Kz, Tx,Ty,Tz]
    fmat = np.array([
        [f[0], f[3], f[6], f[9]],
        [f[1], f[4], f[7], f[10]],
        [f[2], f[5], f[8], f[11]],
        [0, 0, 0, 1]
    ])
    smat = np.array([
        [s[0], s[3], s[6], s[9]],
        [s[1], s[4], s[7], s[10]],
        [s[2], s[5], s[8], s[11]],
        [0, 0, 0, 1]
    ])
    flanges.append(fmat)
    sensors.append(smat)

n = len(flanges)

print("=" * 70)
print("1. ROTATION MATRIX VALIDITY CHECK")
print("=" * 70)
for i in range(n):
    Rf = flanges[i][:3,:3]
    Rs = sensors[i][:3,:3]
    f_orth = np.max(np.abs(Rf.T @ Rf - np.eye(3)))
    s_orth = np.max(np.abs(Rs.T @ Rs - np.eye(3)))
    f_det = np.linalg.det(Rf)
    s_det = np.linalg.det(Rs)
    sf = "OK" if f_orth < 0.01 and abs(f_det-1) < 0.01 else "FAIL"
    ss = "OK" if s_orth < 0.01 and abs(s_det-1) < 0.01 else "FAIL"
    print(f"Pose {i}: Flange[orth={f_orth:.6f} det={f_det:.4f}] {sf}  |  Sensor[orth={s_orth:.6f} det={s_det:.4f}] {ss}")

print()
print("=" * 70)
print("2. RELATIVE MOTIONS A_i and B_i")
print("=" * 70)
relA = []
relB = []
for i in range(n-1):
    A_i = np.linalg.inv(flanges[i]) @ flanges[i+1]
    B_i = sensors[i] @ np.linalg.inv(sensors[i+1])
    relA.append(A_i)
    relB.append(B_i)
    Ra = A_i[:3,:3]
    Rb = B_i[:3,:3]
    trA = np.clip((np.trace(Ra)-1)/2, -1, 1)
    trB = np.clip((np.trace(Rb)-1)/2, -1, 1)
    angA = np.degrees(np.arccos(trA))
    angB = np.degrees(np.arccos(trB))
    ratio = angA/angB if angB > 0.01 else float("inf")
    print(f"Pair {i}-{i+1}: |angle_A|={angA:.2f} deg  |angle_B|={angB:.2f} deg  ratio={ratio:.3f}")

print()
print("=" * 70)
print("3. TSAI-LENZ CALIBRATION (Python reference)")
print("=" * 70)

def axis_angle(R):
    tr = np.clip((np.trace(R)-1)/2, -1, 1)
    theta = np.arccos(tr)
    if abs(theta) < 1e-10:
        return np.array([0,0,1]), 0.0
    s = 2*np.sin(theta)
    axis = np.array([R[2,1]-R[1,2], R[0,2]-R[2,0], R[1,0]-R[0,1]]) / s
    nrm = np.linalg.norm(axis)
    if nrm > 1e-12:
        axis = axis / nrm
    return axis, theta

def mod_rodrigues(theta, axis):
    return 2*np.sin(theta/2) * axis

def skew(v):
    return np.array([[0, -v[2], v[1]], [v[2], 0, -v[0]], [-v[1], v[0], 0]])

# Build linear system for rotation
M_rows = []
rhs_rows = []
for i in range(len(relA)):
    Ra = relA[i][:3,:3]
    Rb = relB[i][:3,:3]
    axA, angA = axis_angle(Ra)
    axB, angB = axis_angle(Rb)
    pa = mod_rodrigues(angA, axA)
    pb = mod_rodrigues(angB, axB)
    S = skew(pa + pb)
    M_rows.append(S)
    rhs_rows.append(pb - pa)

M = np.vstack(M_rows)
rhs_vec = np.concatenate(rhs_rows)
pcxPrime, _, _, _ = np.linalg.lstsq(M, rhs_vec, rcond=None)
pcxNorm = np.linalg.norm(pcxPrime)

# CORRECT: theta_x = 2 * atan(||pcxPrime||)
thetaX_correct = 2 * np.arctan(pcxNorm)
axisX = pcxPrime / pcxNorm if pcxNorm > 1e-12 else np.array([0,0,1])

# OLD BUG: theta_x = 2 * atan(||pcx||/2) where pcx = 2*pcxPrime/sqrt(1+||pcxPrime||^2)
scale_old = 2.0 / np.sqrt(1 + pcxNorm**2)
pcx_old = pcxPrime * scale_old
pcxLen_old = np.linalg.norm(pcx_old)
thetaX_bug = 2 * np.arctan(pcxLen_old / 2)

print(f"||pcxPrime|| = {pcxNorm:.6f} (= tan(theta/2))")
print(f"theta_X CORRECT = {np.degrees(thetaX_correct):.4f} deg")
print(f"theta_X BUGGY   = {np.degrees(thetaX_bug):.4f} deg")
print(f"ROTATION ERROR  = {np.degrees(abs(thetaX_correct - thetaX_bug)):.4f} deg")

def rodrigues_to_R(axis, angle):
    c = np.cos(angle)
    s = np.sin(angle)
    t = 1-c
    x,y,z = axis
    return np.array([
        [t*x*x+c, t*x*y-s*z, t*x*z+s*y],
        [t*x*y+s*z, t*y*y+c, t*y*z-s*x],
        [t*x*z-s*y, t*y*z+s*x, t*z*z+c]
    ])

Rx_correct = rodrigues_to_R(axisX, thetaX_correct)
Rx_bug = rodrigues_to_R(axisX, thetaX_bug)

# Translation solution - CORRECT
Mt_rows = []
rhsT_rows = []
for i in range(len(relA)):
    Ra = relA[i][:3,:3]
    ta = relA[i][:3,3]
    tb = relB[i][:3,3]
    Rxtb = Rx_correct @ tb
    Mt_rows.append(Ra - np.eye(3))
    rhsT_rows.append(Rxtb - ta)
Mt = np.vstack(Mt_rows)
rhsT_vec = np.concatenate(rhsT_rows)
tx_correct, _, _, _ = np.linalg.lstsq(Mt, rhsT_vec, rcond=None)

# Translation solution - BUG
Mt2 = []
rhsT2 = []
for i in range(len(relA)):
    Ra = relA[i][:3,:3]
    ta = relA[i][:3,3]
    tb = relB[i][:3,3]
    Rxtb_bug = Rx_bug @ tb
    Mt2.append(Ra - np.eye(3))
    rhsT2.append(Rxtb_bug - ta)
Mt_b = np.vstack(Mt2)
rhsT_b = np.concatenate(rhsT2)
tx_bug, _, _, _ = np.linalg.lstsq(Mt_b, rhsT_b, rcond=None)

print()
print(f"Translation CORRECT: [{tx_correct[0]:.3f}, {tx_correct[1]:.3f}, {tx_correct[2]:.3f}]")
print(f"Translation BUGGY:   [{tx_bug[0]:.3f}, {tx_bug[1]:.3f}, {tx_bug[2]:.3f}]")
print(f"Translation DIFF:    {np.linalg.norm(tx_correct - tx_bug):.3f} mm")

# Build hand-eye matrices
HE_correct = np.eye(4)
HE_correct[:3,:3] = Rx_correct
HE_correct[:3,3] = tx_correct

HE_bug = np.eye(4)
HE_bug[:3,:3] = Rx_bug
HE_bug[:3,3] = tx_bug

print()
print("=" * 70)
print("4. ACCURACY COMPARISON (CORRECT vs BUGGY)")
print("=" * 70)

def measure_accuracy(flanges, sensors, HE):
    targets = []
    for fl, se in zip(flanges, sensors):
        t = fl @ HE @ se
        targets.append(t)
    positions = np.array([t[:3,3] for t in targets])
    avg_pos = positions.mean(axis=0)
    dists = np.linalg.norm(positions - avg_pos, axis=1)
    pos_std = np.std(dists, ddof=1) if len(dists) > 1 else 0
    pos_range = dists.max() - dists.min()
    angles = []
    for t in targets:
        R = t[:3,:3]
        tr = np.clip((np.trace(R)-1)/2, -1, 1)
        angles.append(np.degrees(np.arccos(tr)))
    angles = np.array(angles)
    angle_std = np.std(angles, ddof=1) if len(angles) > 1 else 0
    angle_range = angles.max() - angles.min()
    return pos_std, pos_range, angle_std, angle_range, positions

ps_c, pr_c, as_c, ar_c, pos_c = measure_accuracy(flanges, sensors, HE_correct)
ps_b, pr_b, as_b, ar_b, pos_b = measure_accuracy(flanges, sensors, HE_bug)

print(f"                   CORRECT (fixed)     BUGGY (old)")
print(f"Position Std:     {ps_c:10.3f} mm      {ps_b:10.3f} mm")
print(f"Position Range:   {pr_c:10.3f} mm      {pr_b:10.3f} mm")
print(f"Angle Std:        {as_c:10.3f} deg     {as_b:10.3f} deg")
print(f"Angle Range:      {ar_c:10.3f} deg     {ar_b:10.3f} deg")

print()
print("Target positions in base (CORRECT):")
for i, p in enumerate(pos_c):
    print(f"  Pose {i}: X={p[0]:.3f}  Y={p[1]:.3f}  Z={p[2]:.3f}")

print()
print("=" * 70)
print("5. SENSOR HANDEDNESS CHECK (I x J = K)")
print("=" * 70)
for i in range(n):
    Rs = sensors[i][:3,:3]
    cross = np.cross(Rs[:,0], Rs[:,1])
    diff = np.linalg.norm(cross - Rs[:,2])
    hand = "RIGHT" if diff < 0.1 else "LEFT/ERR"
    print(f"Pose {i}: I x J vs K diff={diff:.6f} -> {hand}-handed")

print()
print("=" * 70)
print("6. FLANGE POSE DIVERSITY")
print("=" * 70)
for i in range(n):
    t = flanges[i][:3,3]
    R = flanges[i][:3,:3]
    tr = np.clip((np.trace(R)-1)/2, -1, 1)
    ang = np.degrees(np.arccos(tr))
    print(f"Pose {i}: t=[{t[0]:8.1f},{t[1]:8.1f},{t[2]:8.1f}] rot_angle={ang:.1f} deg")

print("\nAngular differences between consecutive poses:")
for i in range(n-1):
    R_rel = flanges[i][:3,:3].T @ flanges[i+1][:3,:3]
    tr = np.clip((np.trace(R_rel)-1)/2, -1, 1)
    ang = np.degrees(np.arccos(tr))
    print(f"  {i}->{i+1}: {ang:.2f} deg")

print()
print("=" * 70)
print("7. AX=XB RESIDUAL CHECK (how well does X satisfy AX=XB)")
print("=" * 70)
for i in range(len(relA)):
    AX = relA[i] @ HE_correct
    XB = HE_correct @ relB[i]
    err = np.max(np.abs(AX - XB))
    err_t = np.linalg.norm(AX[:3,3] - XB[:3,3])
    R_err = np.max(np.abs(AX[:3,:3] - XB[:3,:3]))
    print(f"  Pair {i}: rot_err={R_err:.6f}  trans_err={err_t:.3f} mm")
