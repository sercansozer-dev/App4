/**
 * CalibrationData.cs
 * JSON serializable kalibrasyon sonuc modeli.
 *
 * Kalibrasyon sonuclarini ve kullanilan poz ciftlerini saklar.
 * Dosya yolu: %LocalAppData%\App4\HandEyeCalibration.json
 */

using System;
using System.Collections.Generic;

namespace App4.Utilities.GoRobotMath
{
    /// <summary>
    /// Hand-Eye kalibrasyon sonuclarini iceren serializable sinif.
    /// JSON olarak kaydedilir ve tekrar yuklenebilir.
    /// </summary>
    public class CalibrationData
    {
        /// <summary>Kalibrasyon yapilan robotun adi</summary>
        public string RobotName { get; set; }

        /// <summary>Kalibrasyon tarihi</summary>
        public DateTime CalibrationDate { get; set; }

        /// <summary>Kullanilan poz cifti sayisi</summary>
        public int PoseCount { get; set; }

        /// <summary>
        /// Hand-eye donusum matrisi (12 elemanli flat array).
        /// Sira: [Ix, Iy, Iz, Jx, Jy, Jz, Kx, Ky, Kz, Tx, Ty, Tz]
        /// </summary>
        public double[] HandEyeMatrix { get; set; }

        /// <summary>Aci aralik (derece) - kalibrasyon dogrulugu</summary>
        public double AngleRangeDeg { get; set; }

        /// <summary>Aci standart sapma (derece)</summary>
        public double AngleStdDeg { get; set; }

        /// <summary>Pozisyon aralik (mm) - kalibrasyon dogrulugu</summary>
        public double PositionRangeMm { get; set; }

        /// <summary>Pozisyon standart sapma (mm)</summary>
        public double PositionStdMm { get; set; }

        /// <summary>Kalibrasyon sirasinda toplanan poz ciftleri</summary>
        public List<CalibrationPoseRecord> PoseRecords { get; set; } = new List<CalibrationPoseRecord>();

        /// <summary>Kalibrasyon notlari (opsiyonel)</summary>
        public string Notes { get; set; }

        // --- BallBar Parametreleri ---

        /// <summary>Top 1 yaricapi (mm)</summary>
        public double Sphere1Radius { get; set; } = 12.5;

        /// <summary>Top 2 yaricapi (mm)</summary>
        public double Sphere2Radius { get; set; } = 12.5;

        /// <summary>Cubuk uzunlugu - merkez-merkez (mm)</summary>
        public double BarLength { get; set; } = 50.0;
    }

    /// <summary>
    /// Tek bir kalibrasyon poz cifti kaydi.
    /// Flange (robot) ve Target (sensor) matrislerini icerir.
    /// </summary>
    public class CalibrationPoseRecord
    {
        /// <summary>Poz indeksi (0-bazli)</summary>
        public int Index { get; set; }

        /// <summary>
        /// Robot flange pozu base koordinat sisteminde (12 double flat array).
        /// </summary>
        public double[] FlangeInBase { get; set; }

        /// <summary>
        /// Hedef pozu sensor koordinat sisteminde (12 double flat array).
        /// </summary>
        public double[] TargetInSensor { get; set; }

        /// <summary>Yakalama zamani</summary>
        public DateTime CaptureTime { get; set; }
    }
}
