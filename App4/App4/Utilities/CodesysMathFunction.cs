using System;
using App4.Utilities.GoRobotMath;

namespace App4.Utilities
{
    /// <summary>
    /// CODESYS PLC programının C# karşılığı (6 eksenli).
    ///
    /// GİRDİLER:
    ///   - Pose_Robot_Tool2 (X,Y,Z,A,B,C) → Robotun o anki Tool2 TCP pozisyonu
    ///   - Gocator_Raw_X/Y/Z/Yaw/Roll/Pitch → 6 eksen ham ölçüm
    ///
    /// ADIM 1: Gocator verisini KUKA eksenlerine çevir + optik ofset ekle
    ///   PartInCam.X = Raw_Y + OffsetX
    ///   PartInCam.Y = -Raw_X + OffsetY
    ///   PartInCam.Z = OffsetZ - Raw_Z
    ///   PartInCam.A = -Raw_Yaw
    ///   PartInCam.B = -Raw_Roll
    ///   PartInCam.C = Raw_Pitch
    /// ADIM 2: Pozisyonları 4x4 matrise çevir
    /// ADIM 3: Matris çarpımı: Target = RobotTool2 × PartInCam
    /// ADIM 4: Sonucu KUKA açılarına geri çevir
    ///
    /// ÇIKTI: Target_Pose (X,Y,Z,A,B,C) → hedef nokta robot base frame'inde
    /// </summary>
    public class CodesysMathFunction
    {
        // ═══════════════════════════════════════════════════════════════
        // OPTİK OFSET PARAMETRELERİ (değiştirilebilir)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>X ekseni optik ofset (mm). Varsayılan: 0</summary>
        public double OffsetX { get; set; } = 0;

        /// <summary>Y ekseni optik ofset (mm). Varsayılan: 0</summary>
        public double OffsetY { get; set; } = 0;

        /// <summary>Z ekseni optik ofset — Optik odak uzaklığı (mm). Varsayılan: 242.90</summary>
        public double OffsetZ { get; set; } = 242.90;

        // ═══════════════════════════════════════════════════════════════
        // GOCATOR → FONKSİYON GİRDİ EŞLEŞTİRME INDEX'LERİ (6 eksen)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Gocator_Raw_X index'i. Varsayılan: 0</summary>
        public int MapIndexX { get; set; } = 0;

        /// <summary>Gocator_Raw_Y index'i. Varsayılan: 1</summary>
        public int MapIndexY { get; set; } = 1;

        /// <summary>Gocator_Raw_Z index'i. Varsayılan: 2</summary>
        public int MapIndexZ { get; set; } = 2;

        /// <summary>Gocator_Raw_Yaw (Z dönüşü) index'i. Varsayılan: 3 (eski AngleZ)</summary>
        public int MapIndexYaw { get; set; } = 3;

        /// <summary>Gocator_Raw_Roll (X dönüşü) index'i. Varsayılan: 4</summary>
        public int MapIndexRoll { get; set; } = 4;

        /// <summary>Gocator_Raw_Pitch (Y dönüşü) index'i. Varsayılan: 5</summary>
        public int MapIndexPitch { get; set; } = 5;

        // ═══════════════════════════════════════════════════════════════
        // SON HESAPLAMA SONUÇLARI
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Son hesaplama sonucu: Hedef nokta (KUKA XYZABC)</summary>
        public KukaPose LastTargetPose { get; private set; }

        /// <summary>Son hesaplamada kullanılan ara değer: Kamera frame'indeki parça pozisyonu</summary>
        public KukaPose LastPartInCam { get; private set; }

        /// <summary>Hesaplama başarılı mı?</summary>
        public bool LastCalculationSuccess { get; private set; }

        /// <summary>Son hata mesajı</summary>
        public string LastError { get; private set; } = "";

        // ═══════════════════════════════════════════════════════════════
        // ANA HESAPLAMA FONKSİYONU
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// CODESYS ana programın C# karşılığı (6 eksenli).
        /// Gocator 6 eksen ham verisini KUKA eksenlerine çevirip optik ofset ekler,
        /// ardından Robot Tool2 TCP matrisi ile çarparak hedef noktayı bulur.
        /// Target = RobotTool2 × PartInCam
        /// </summary>
        public KukaPose Calculate(
            double gocRawX,
            double gocRawY,
            double gocRawZ,
            double gocRawYaw,
            double gocRawRoll,
            double gocRawPitch,
            KukaPose robotTool2Pose)
        {
            try
            {
                // ─────────────────────────────────────────────────────────────
                // ADIM 1: GOCATOR VERİSİNİ KUKA EKSENLERİNE ÇEVİR + OPTİK OFSET
                // ─────────────────────────────────────────────────────────────
                // Pozisyon eşleştirme:
                //   PartInCam.X = Raw_Y + OffsetX
                //   PartInCam.Y = -Raw_X + OffsetY
                //   PartInCam.Z = OffsetZ - Raw_Z
                // Açı eşleştirme (Gocator Z ters yönlü):
                //   PartInCam.A = -Raw_Yaw   (KUKA A = Z dönüşü)
                //   PartInCam.B = -Raw_Roll   (KUKA B = Y dönüşü ← Goc -X)
                //   PartInCam.C = Raw_Pitch   (KUKA C = X dönüşü ← Goc +Y)
                var partInCam = new KukaPose
                {
                    X = gocRawY + OffsetX,
                    Y = -gocRawX + OffsetY,
                    Z = OffsetZ - gocRawZ,
                    A = -gocRawYaw,
                    B = -gocRawRoll,
                    C = gocRawPitch
                };

                LastPartInCam = partInCam;

                // ─────────────────────────────────────────────────────────────
                // ADIM 2: POZİSYONLARI 4×4 MATRİSE ÇEVİR
                // ─────────────────────────────────────────────────────────────
                // CODESYS: fbToMatrix_Robot(PoseIn := Pose_Robot_Tool2, MatOut => Mat_RobotBase_Tool2)
                // C#:      KukaPose.ToMatrix() — aynı ZYX Euler konvansiyonu
                var matRobot = robotTool2Pose.ToMatrix();

                // CODESYS: fbToMatrix_Camera(PoseIn := Pose_PartInCam, MatOut => Mat_Camera_Part)
                var matCamera = partInCam.ToMatrix();

                // ─────────────────────────────────────────────────────────────
                // ADIM 3: MATRİS ÇARPIMI İLE HEDEFİ BUL
                // ─────────────────────────────────────────────────────────────
                // CODESYS: fbMultiply(Mat1 := Mat_RobotBase_Tool2, Mat2 := Mat_Camera_Part, MatResult => Mat_Base_Part)
                // C#:      TransformMatrix operator* — standart 4×4 matris çarpımı
                var matTarget = matRobot * matCamera;

                // ─────────────────────────────────────────────────────────────
                // ADIM 4: NİHAİ HEDEFİ KUKA AÇILARINA GERİ ÇEVİR
                // ─────────────────────────────────────────────────────────────
                // CODESYS: fbMatrixToPose(MatIn := Mat_Base_Part, PoseOut => Target_Pose)
                // C#:      KukaPose.FromMatrix() — aynı atan2 tabanlı Euler çıkarma
                var targetPose = KukaPose.FromMatrix(matTarget);

                LastTargetPose = targetPose;
                LastCalculationSuccess = true;
                LastError = "";

                return targetPose;
            }
            catch (Exception ex)
            {
                LastCalculationSuccess = false;
                LastError = ex.Message;
                LastTargetPose = new KukaPose();
                return new KukaPose();
            }
        }

        /// <summary>
        /// Gocator ölçüm dizisinden eşleştirme index'lerine göre 6 eksen ham değerleri çekerek hesapla.
        /// </summary>
        public KukaPose CalculateFromArray(double[] gocatorValues, KukaPose robotTool2Pose)
        {
            double rawX = MapIndexX < gocatorValues.Length ? gocatorValues[MapIndexX] : 0;
            double rawY = MapIndexY < gocatorValues.Length ? gocatorValues[MapIndexY] : 0;
            double rawZ = MapIndexZ < gocatorValues.Length ? gocatorValues[MapIndexZ] : 0;
            double rawYaw = MapIndexYaw < gocatorValues.Length ? gocatorValues[MapIndexYaw] : 0;
            double rawRoll = MapIndexRoll < gocatorValues.Length ? gocatorValues[MapIndexRoll] : 0;
            double rawPitch = MapIndexPitch < gocatorValues.Length ? gocatorValues[MapIndexPitch] : 0;

            return Calculate(rawX, rawY, rawZ, rawYaw, rawRoll, rawPitch, robotTool2Pose);
        }

        /// <summary>
        /// Ayarları sıfırla — varsayılan ofset ve eşleştirme değerlerine dön.
        /// </summary>
        public void ResetToDefaults()
        {
            OffsetX = 0;
            OffsetY = 0;
            OffsetZ = 242.90;
            MapIndexX = 0;
            MapIndexY = 1;
            MapIndexZ = 2;
            MapIndexYaw = 3;
            MapIndexRoll = 4;
            MapIndexPitch = 5;
        }

        /// <summary>
        /// Hesaplama sonucunun özet metnini döner.
        /// </summary>
        public string GetResultSummary()
        {
            if (!LastCalculationSuccess)
                return $"HATA: {LastError}";

            var t = LastTargetPose;
            return $"Hedef: X={t.X:F3} Y={t.Y:F3} Z={t.Z:F3} A={t.A:F3} B={t.B:F3} C={t.C:F3}";
        }
    }
}
