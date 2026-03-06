using System;
using App4.Utilities.GoRobotMath;

namespace App4.Utilities
{
    /// <summary>
    /// CODESYS PLC programının C# karşılığı.
    ///
    /// Orijinal CODESYS programı:
    ///   GİRDİLER:
    ///     - Pose_Robot_Tool2 (X,Y,Z,A,B,C) → Robotun o anki Tool2 TCP pozisyonu
    ///     - Gocator_Raw_X, Gocator_Raw_Y, Gocator_Raw_Z → Gocator ham ölçüm
    ///     - Gocator_Raw_AngleZ → Gocator ham açı
    ///
    ///   ADIM 1: Gocator verisini KUKA eksenlerine çevir + optik ofset ekle
    ///   ADIM 2: Pozisyonları 4x4 matrise çevir (PoseToMatrix)
    ///   ADIM 3: Matris çarpımı ile hedefi bul (Mat_Robot × Mat_Camera)
    ///   ADIM 4: Sonucu KUKA açılarına geri çevir (MatrixToPose)
    ///
    ///   ÇIKTI: Target_Pose (X,Y,Z,A,B,C) → hedef nokta robot base frame'inde
    ///
    /// C# karşılıkları:
    ///   FB_PoseToMatrix  → KukaPose.ToMatrix()
    ///   FB_MultiplyMatrix → TransformMatrix operator*
    ///   FB_MatrixToPose  → KukaPose.FromMatrix()
    ///   FUN_ATAN2        → Math.Atan2() (built-in)
    /// </summary>
    public class CodesysMathFunction
    {
        // ═══════════════════════════════════════════════════════════════
        // OPTİK OFSET PARAMETRELERİ (değiştirilebilir)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>X ekseni optik ofset — Lens montaj kayması (mm). Varsayılan: 7.44</summary>
        public double OffsetX { get; set; } = 7.44;

        /// <summary>Y ekseni optik ofset — Lens montaj kayması (mm). Varsayılan: 16.79</summary>
        public double OffsetY { get; set; } = 16.79;

        /// <summary>Z ekseni optik ofset — Optik odak uzaklığı (mm). Varsayılan: 242.90</summary>
        public double OffsetZ { get; set; } = 242.90;

        // ═══════════════════════════════════════════════════════════════
        // GOCATOR → FONKSİYON GİRDİ EŞLEŞTİRME INDEX'LERİ
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Gocator_Raw_X olarak kullanılacak ölçüm index'i. Varsayılan: 0</summary>
        public int MapIndexX { get; set; } = 0;

        /// <summary>Gocator_Raw_Y olarak kullanılacak ölçüm index'i. Varsayılan: 1</summary>
        public int MapIndexY { get; set; } = 1;

        /// <summary>Gocator_Raw_Z olarak kullanılacak ölçüm index'i. Varsayılan: 2</summary>
        public int MapIndexZ { get; set; } = 2;

        /// <summary>Gocator_Raw_AngleZ olarak kullanılacak ölçüm index'i. Varsayılan: 3</summary>
        public int MapIndexAngleZ { get; set; } = 3;

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
        /// CODESYS ana programın C# karşılığı.
        /// Gocator ham verisini KUKA eksenlerine çevirip optik ofset ekler,
        /// ardından Robot Tool2 TCP matrisi ile çarparak hedef noktayı bulur.
        ///
        /// CODESYS karşılığı:
        /// <code>
        /// // ADIM 1: GOCATOR VERİSİNİ KUKA EKSENLERİNE ÇEVİR VE OPTİK OFFSETİ EKLE
        /// Pose_PartInCam.X := Gocator_Raw_Y + 7.44;
        /// Pose_PartInCam.Y := -Gocator_Raw_X + 16.79;
        /// Pose_PartInCam.Z := 242.90 - Gocator_Raw_Z;
        /// Pose_PartInCam.A := Gocator_Raw_AngleZ;
        /// Pose_PartInCam.B := 0.0;
        /// Pose_PartInCam.C := 0.0;
        ///
        /// // ADIM 2: POZİSYONLARI MATRİSE ÇEVİR
        /// fbToMatrix_Robot(PoseIn := Pose_Robot_Tool2, MatOut => Mat_RobotBase_Tool2);
        /// fbToMatrix_Camera(PoseIn := Pose_PartInCam, MatOut => Mat_Camera_Part);
        ///
        /// // ADIM 3: MATRİS ÇARPIMI İLE HEDEFİ BUL
        /// fbMultiply(Mat1 := Mat_RobotBase_Tool2, Mat2 := Mat_Camera_Part, MatResult => Mat_Base_Part);
        ///
        /// // ADIM 4: NİHAİ HEDEFİ KUKA AÇILARINA GERİ ÇEVİR
        /// fbMatrixToPose(MatIn := Mat_Base_Part, PoseOut => Target_Pose);
        /// </code>
        /// </summary>
        /// <param name="gocRawX">Gocator ham X değeri (mm)</param>
        /// <param name="gocRawY">Gocator ham Y değeri (mm)</param>
        /// <param name="gocRawZ">Gocator ham Z değeri (mm)</param>
        /// <param name="gocRawAngleZ">Gocator ham açı Z değeri (derece)</param>
        /// <param name="robotTool2Pose">Robotun o anki Tool2 TCP pozisyonu (KUKA XYZABC)</param>
        /// <returns>Hesaplanan hedef nokta (KUKA XYZABC, robot base frame'inde)</returns>
        public KukaPose Calculate(
            double gocRawX,
            double gocRawY,
            double gocRawZ,
            double gocRawAngleZ,
            KukaPose robotTool2Pose)
        {
            try
            {
                // ─────────────────────────────────────────────────────────────
                // ADIM 1: GOCATOR VERİSİNİ KUKA EKSENLERİNE ÇEVİR + OPTİK OFSET
                // ─────────────────────────────────────────────────────────────
                // CODESYS: Pose_PartInCam.X := Gocator_Raw_Y + 7.44;
                // CODESYS: Pose_PartInCam.Y := -Gocator_Raw_X + 16.79;
                // CODESYS: Pose_PartInCam.Z := 242.90 - Gocator_Raw_Z;
                var partInCam = new KukaPose
                {
                    X = gocRawY + OffsetX,        // Gocator Y → KUKA X + lens kayması
                    Y = -gocRawX + OffsetY,       // -Gocator X → KUKA Y + lens kayması
                    Z = OffsetZ - gocRawZ,        // Optik odak uzaklığı - Gocator Z
                    A = gocRawAngleZ,             // Açı Z doğrudan aktarılır
                    B = 0.0,                      // Sabit: 0
                    C = 0.0                       // Sabit: 0
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
        /// Gocator ölçüm dizisinden eşleştirme index'lerine göre ham değerleri çekerek hesapla.
        /// </summary>
        /// <param name="gocatorValues">Gocator ham ölçüm dizisi (mapped)</param>
        /// <param name="robotTool2Pose">Robotun o anki Tool2 TCP pozisyonu</param>
        /// <returns>Hesaplanan hedef nokta</returns>
        public KukaPose CalculateFromArray(double[] gocatorValues, KukaPose robotTool2Pose)
        {
            double rawX = MapIndexX < gocatorValues.Length ? gocatorValues[MapIndexX] : 0;
            double rawY = MapIndexY < gocatorValues.Length ? gocatorValues[MapIndexY] : 0;
            double rawZ = MapIndexZ < gocatorValues.Length ? gocatorValues[MapIndexZ] : 0;
            double rawAngleZ = MapIndexAngleZ < gocatorValues.Length ? gocatorValues[MapIndexAngleZ] : 0;

            return Calculate(rawX, rawY, rawZ, rawAngleZ, robotTool2Pose);
        }

        /// <summary>
        /// Ayarları sıfırla — varsayılan ofset ve eşleştirme değerlerine dön.
        /// </summary>
        public void ResetToDefaults()
        {
            OffsetX = 7.44;
            OffsetY = 16.79;
            OffsetZ = 242.90;
            MapIndexX = 0;
            MapIndexY = 1;
            MapIndexZ = 2;
            MapIndexAngleZ = 3;
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
