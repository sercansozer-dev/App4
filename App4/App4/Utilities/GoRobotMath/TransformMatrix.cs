/**
 * TransformMatrix.cs
 * 4x4 homojen donusum matrisi - GoRobot::Matrix C# karsiligi.
 *
 * Referans: GoRobot/GoRobotMatrix.h (LMI Technologies)
 * Birimler: milimetre (translasyon), ortogonal rotasyon matrisi
 *
 * Matris formati:
 *   [ Ix  Jx  Kx  Tx ]
 *   [ Iy  Jy  Ky  Ty ]
 *   [ Iz  Jz  Kz  Tz ]
 *   [  0   0   0   1 ]
 */

using System;

namespace App4.Utilities.GoRobotMath
{
    /// <summary>
    /// 4x4 homojen donusum matrisi (6-DOF rigid body transform).
    /// Alt satir her zaman [0 0 0 1] kabul edilir.
    /// </summary>
    public class TransformMatrix
    {
        // --- Rotasyon kolonlari ---
        public double Ix, Iy, Iz;   // I ekseni (X yonu)
        public double Jx, Jy, Jz;   // J ekseni (Y yonu)
        public double Kx, Ky, Kz;   // K ekseni (Z yonu)

        // --- Translasyon vektoru (mm) ---
        public double Tx, Ty, Tz;

        #region Constructors

        /// <summary>
        /// Birim matris (Identity) olarak olusturur.
        /// </summary>
        public TransformMatrix()
        {
            Ix = 1; Iy = 0; Iz = 0;
            Jx = 0; Jy = 1; Jz = 0;
            Kx = 0; Ky = 0; Kz = 1;
            Tx = 0; Ty = 0; Tz = 0;
        }

        /// <summary>
        /// 12 degerden olusturur.
        /// </summary>
        public TransformMatrix(
            double ix, double iy, double iz,
            double jx, double jy, double jz,
            double kx, double ky, double kz,
            double tx, double ty, double tz)
        {
            Ix = ix; Iy = iy; Iz = iz;
            Jx = jx; Jy = jy; Jz = jz;
            Kx = kx; Ky = ky; Kz = kz;
            Tx = tx; Ty = ty; Tz = tz;
        }

        /// <summary>
        /// Kopyalama constructor.
        /// </summary>
        public TransformMatrix(TransformMatrix other)
        {
            Ix = other.Ix; Iy = other.Iy; Iz = other.Iz;
            Jx = other.Jx; Jy = other.Jy; Jz = other.Jz;
            Kx = other.Kx; Ky = other.Ky; Kz = other.Kz;
            Tx = other.Tx; Ty = other.Ty; Tz = other.Tz;
        }

        #endregion

        #region Static Factories

        /// <summary>
        /// Birim (Identity) matris dondurur.
        /// </summary>
        public static TransformMatrix Identity => new TransformMatrix();

        /// <summary>
        /// 12 elemanli flat array'den olusturur: [Ix,Iy,Iz, Jx,Jy,Jz, Kx,Ky,Kz, Tx,Ty,Tz]
        /// </summary>
        public static TransformMatrix FromArray(double[] data)
        {
            if (data == null || data.Length < 12)
                throw new ArgumentException("En az 12 eleman gerekli.", nameof(data));

            return new TransformMatrix(
                data[0], data[1], data[2],
                data[3], data[4], data[5],
                data[6], data[7], data[8],
                data[9], data[10], data[11]);
        }

        #endregion

        #region Core Operations

        /// <summary>
        /// Birim matris mi kontrol eder.
        /// </summary>
        public bool IsIdentity(double tol = 1e-9)
        {
            return Math.Abs(Ix - 1) < tol && Math.Abs(Iy) < tol && Math.Abs(Iz) < tol
                && Math.Abs(Jx) < tol && Math.Abs(Jy - 1) < tol && Math.Abs(Jz) < tol
                && Math.Abs(Kx) < tol && Math.Abs(Ky) < tol && Math.Abs(Kz - 1) < tol
                && Math.Abs(Tx) < tol && Math.Abs(Ty) < tol && Math.Abs(Tz) < tol;
        }

        /// <summary>
        /// Matris carpimi: this * other.
        /// 4x4 matris carpimi, sadece ust 3 satir hesaplanir (alt satir [0 0 0 1]).
        /// </summary>
        public TransformMatrix Multiply(TransformMatrix b)
        {
            var r = new TransformMatrix();

            // Satir 0
            r.Ix = Ix * b.Ix + Jx * b.Iy + Kx * b.Iz;
            r.Jx = Ix * b.Jx + Jx * b.Jy + Kx * b.Jz;
            r.Kx = Ix * b.Kx + Jx * b.Ky + Kx * b.Kz;
            r.Tx = Ix * b.Tx + Jx * b.Ty + Kx * b.Tz + Tx;

            // Satir 1
            r.Iy = Iy * b.Ix + Jy * b.Iy + Ky * b.Iz;
            r.Jy = Iy * b.Jx + Jy * b.Jy + Ky * b.Jz;
            r.Ky = Iy * b.Kx + Jy * b.Ky + Ky * b.Kz;
            r.Ty = Iy * b.Tx + Jy * b.Ty + Ky * b.Tz + Ty;

            // Satir 2
            r.Iz = Iz * b.Ix + Jz * b.Iy + Kz * b.Iz;
            r.Jz = Iz * b.Jx + Jz * b.Jy + Kz * b.Jz;
            r.Kz = Iz * b.Kx + Jz * b.Ky + Kz * b.Kz;
            r.Tz = Iz * b.Tx + Jz * b.Ty + Kz * b.Tz + Tz;

            return r;
        }

        /// <summary>
        /// Matris carpim operatoru.
        /// </summary>
        public static TransformMatrix operator *(TransformMatrix a, TransformMatrix b)
        {
            return a.Multiply(b);
        }

        /// <summary>
        /// Ters matris (rigid body transform icin).
        /// Ortogonal rotasyon: R_inv = R^T, t_inv = -R^T * t
        /// </summary>
        public TransformMatrix Inverse()
        {
            var inv = new TransformMatrix();

            // R^T: satir-sutun degistir
            inv.Ix = Ix; inv.Iy = Jx; inv.Iz = Kx;
            inv.Jx = Iy; inv.Jy = Jy; inv.Jz = Ky;
            inv.Kx = Iz; inv.Ky = Jz; inv.Kz = Kz;

            // t_inv = -R^T * t
            inv.Tx = -(inv.Ix * Tx + inv.Jx * Ty + inv.Kx * Tz);
            inv.Ty = -(inv.Iy * Tx + inv.Jy * Ty + inv.Ky * Tz);
            inv.Tz = -(inv.Iz * Tx + inv.Jz * Ty + inv.Kz * Tz);

            return inv;
        }

        /// <summary>
        /// Keyfi eksen etrafinda donme uygular (pre-multiply).
        /// </summary>
        /// <param name="degrees">Donme acisi (derece)</param>
        /// <param name="axX">Eksen X bileseni</param>
        /// <param name="axY">Eksen Y bileseni</param>
        /// <param name="axZ">Eksen Z bileseni</param>
        /// <returns>Yeni donmus matris</returns>
        public TransformMatrix Rotate(double degrees, double axX, double axY, double axZ)
        {
            var rot = CreateRotation(degrees, axX, axY, axZ);
            return rot * this;
        }

        /// <summary>
        /// Translasyon uygular (pre-multiply).
        /// </summary>
        public TransformMatrix Translate(double tx, double ty, double tz)
        {
            var t = Identity;
            t.Tx = tx;
            t.Ty = ty;
            t.Tz = tz;
            return t * this;
        }

        /// <summary>
        /// Keyfi eksen etrafinda donme matrisi olusturur (Rodrigues formulu).
        /// </summary>
        public static TransformMatrix CreateRotation(double degrees, double axX, double axY, double axZ)
        {
            double rad = degrees * Math.PI / 180.0;
            double c = Math.Cos(rad);
            double s = Math.Sin(rad);
            double t = 1.0 - c;

            // Ekseni normalize et
            double len = Math.Sqrt(axX * axX + axY * axY + axZ * axZ);
            if (len < 1e-12)
                return Identity;

            double x = axX / len;
            double y = axY / len;
            double z = axZ / len;

            var m = new TransformMatrix();
            m.Ix = t * x * x + c;
            m.Iy = t * x * y + s * z;
            m.Iz = t * x * z - s * y;

            m.Jx = t * x * y - s * z;
            m.Jy = t * y * y + c;
            m.Jz = t * y * z + s * x;

            m.Kx = t * x * z + s * y;
            m.Ky = t * y * z - s * x;
            m.Kz = t * z * z + c;

            m.Tx = 0; m.Ty = 0; m.Tz = 0;
            return m;
        }

        #endregion

        #region Rotation Helpers

        /// <summary>
        /// 3x3 rotasyon matrisini double[3,3] olarak dondurur.
        /// </summary>
        public double[,] GetRotation3x3()
        {
            return new double[,]
            {
                { Ix, Jx, Kx },
                { Iy, Jy, Ky },
                { Iz, Jz, Kz }
            };
        }

        /// <summary>
        /// 3x3 rotasyonu ayarlar (translasyon degismez).
        /// </summary>
        public void SetRotation3x3(double[,] r)
        {
            Ix = r[0, 0]; Jx = r[0, 1]; Kx = r[0, 2];
            Iy = r[1, 0]; Jy = r[1, 1]; Ky = r[1, 2];
            Iz = r[2, 0]; Jz = r[2, 1]; Kz = r[2, 2];
        }

        /// <summary>
        /// Rotasyon matrisinin izini (trace) dondurur.
        /// trace(R) = Ix + Jy + Kz
        /// </summary>
        public double RotationTrace()
        {
            return Ix + Jy + Kz;
        }

        /// <summary>
        /// Translasyon vektorunu double[3] olarak dondurur.
        /// </summary>
        public double[] GetTranslation()
        {
            return new double[] { Tx, Ty, Tz };
        }

        /// <summary>
        /// Translasyonu ayarlar (rotasyon degismez).
        /// </summary>
        public void SetTranslation(double tx, double ty, double tz)
        {
            Tx = tx; Ty = ty; Tz = tz;
        }

        #endregion

        #region Serialization

        /// <summary>
        /// 12 elemanli flat array: [Ix,Iy,Iz, Jx,Jy,Jz, Kx,Ky,Kz, Tx,Ty,Tz]
        /// </summary>
        public double[] ToArray()
        {
            return new double[] { Ix, Iy, Iz, Jx, Jy, Jz, Kx, Ky, Kz, Tx, Ty, Tz };
        }

        /// <summary>
        /// Insan okuyabilir format.
        /// </summary>
        public override string ToString()
        {
            return $"[{Ix,10:F4} {Jx,10:F4} {Kx,10:F4} {Tx,10:F4}]\n" +
                   $"[{Iy,10:F4} {Jy,10:F4} {Ky,10:F4} {Ty,10:F4}]\n" +
                   $"[{Iz,10:F4} {Jz,10:F4} {Kz,10:F4} {Tz,10:F4}]\n" +
                   $"[{0,10:F4} {0,10:F4} {0,10:F4} {1,10:F4}]";
        }

        /// <summary>
        /// Tek satirda ozet format.
        /// </summary>
        public string ToCompactString()
        {
            return $"I({Ix:F3},{Iy:F3},{Iz:F3}) J({Jx:F3},{Jy:F3},{Jz:F3}) " +
                   $"K({Kx:F3},{Ky:F3},{Kz:F3}) T({Tx:F3},{Ty:F3},{Tz:F3})";
        }

        #endregion

        #region Equality

        /// <summary>
        /// Iki matrisin yaklasik esitligini kontrol eder.
        /// </summary>
        public bool ApproxEquals(TransformMatrix other, double tol = 1e-6)
        {
            if (other == null) return false;
            return Math.Abs(Ix - other.Ix) < tol && Math.Abs(Iy - other.Iy) < tol && Math.Abs(Iz - other.Iz) < tol
                && Math.Abs(Jx - other.Jx) < tol && Math.Abs(Jy - other.Jy) < tol && Math.Abs(Jz - other.Jz) < tol
                && Math.Abs(Kx - other.Kx) < tol && Math.Abs(Ky - other.Ky) < tol && Math.Abs(Kz - other.Kz) < tol
                && Math.Abs(Tx - other.Tx) < tol && Math.Abs(Ty - other.Ty) < tol && Math.Abs(Tz - other.Tz) < tol;
        }

        #endregion
    }
}
