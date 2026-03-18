/**
 * CalibrationService.cs
 * Eye-on-Hand kalibrasyon orkestratoru.
 *
 * Gorevler:
 * 1. Robot flange pozu + Gocator sensor olcumu ciftlerini toplar
 * 2. Tsai-Lenz kalibrasyonu calistirir (HandEyeCalibration.CalibrateEyeOnHand)
 * 3. Sonucu JSON olarak kaydeder/yukler
 * 4. Runtime'da LocateInBase() ile sensor koordinatlarini robot base'e donusturur
 *
 * Entegrasyon:
 * - KukaRobotInstance.ReadVariableAsync() ile $POS_ACT ve $TOOL oku
 * - GocatorService.ReceiveMeasurementLogic ile BallBar olcumu al
 * - CalibrationData JSON serializasyonu ile kayit/yukleme
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using App4.Utilities;

namespace App4.Utilities.GoRobotMath
{
    /// <summary>
    /// Eye-on-Hand kalibrasyon servisi.
    /// Singleton pattern ile tum uygulama boyunca tek instance kullanilir.
    /// </summary>
    public class CalibrationService : INotifyPropertyChanged
    {
        #region Singleton

        private static CalibrationService _instance;
        public static CalibrationService Instance => _instance ??= new CalibrationService();

        #endregion

        #region Properties

        /// <summary>Kalibre edilmis hand-eye donusum matrisi (null ise kalibrasyon yapilmamis)</summary>
        public TransformMatrix HandEyeMatrix { get; private set; }

        /// <summary>Kalibrasyon yapilmis mi?</summary>
        public bool IsCalibrated => HandEyeMatrix != null;

        /// <summary>Toplanan poz cifti sayisi</summary>
        public int CollectedPoseCount => _posePairs.Count;

        /// <summary>Son kalibrasyon verileri</summary>
        public CalibrationData LastCalibrationData { get; private set; }

        /// <summary>Son kalibrasyon dogrulugu</summary>
        public CalibrationAccuracy? LastAccuracy { get; private set; }

        private string _statusMessage = "Kalibrasyon bekleniyor";
        /// <summary>Durum mesaji (UI binding icin)</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set { if (_statusMessage != value) { _statusMessage = value; OnPropertyChanged(); } }
        }

        private bool _isBusy;
        /// <summary>Islem devam ediyor mu?</summary>
        public bool IsBusy
        {
            get => _isBusy;
            private set { if (_isBusy != value) { _isBusy = value; OnPropertyChanged(); } }
        }

        // --- BallBar Parametreleri ---
        /// <summary>Top 1 yaricapi (mm)</summary>
        public double Sphere1Radius { get; set; } = 12.5;

        /// <summary>Top 2 yaricapi (mm)</summary>
        public double Sphere2Radius { get; set; } = 12.5;

        /// <summary>Cubuk uzunlugu - merkez-merkez (mm)</summary>
        public double BarLength { get; set; } = 50.0;

        #endregion

        #region Internal State

        // Toplanan poz ciftleri: (flange_in_base, target_in_sensor)
        private readonly List<(TransformMatrix flange, TransformMatrix sensor)> _posePairs = new();

        // Initialize() sadece bir kez calissin (sayfa yeniden yuklendiginde eski pozlari tekrar yuklemesin)
        private bool _isInitialized = false;

        // JSON kayit dosya yolu
        private static readonly string _calibrationFilePath = Path.Combine(
            GlobalData.ConfigBaseDir, "HandEyeCalibration.json");

        #endregion

        #region Calibration Workflow

        /// <summary>
        /// Toplanan pozlari temizler (yeni kalibrasyon baslatmak icin).
        /// </summary>
        public void ClearPoses()
        {
            _posePairs.Clear();
            StatusMessage = "Pozlar temizlendi. Yeni kalibrasyon baslatilabilir.";
            OnPropertyChanged(nameof(CollectedPoseCount));
        }

        /// <summary>
        /// Mevcut robot pozunu ve sensor olcumunu yakalar, poz listesine ekler.
        ///
        /// Islem sirasi:
        /// 1. Robot $POS_ACT (TCP) ve $TOOL[n] oku → Flange matrisi hesapla
        /// 2. Gocator'u tetikle → BallBar olcumu al (12 deger = matris)
        /// 3. (flange, sensor) ciftini kaydet
        /// </summary>
        /// <param name="robot">Bagli robot instance</param>
        /// <param name="dispatcher">UI dispatcher (Gocator cagrilari icin)</param>
        /// <returns>Basarili ise true</returns>
        public async Task<bool> CapturePoseAsync(KukaRobotInstance robot, DispatcherQueue dispatcher = null)
        {
            if (robot == null || !robot.IsConnected)
            {
                StatusMessage = "HATA: Robot bagli degil.";
                return false;
            }

            IsBusy = true;
            try
            {
                // --- 1. Robot flange pozunu oku ---
                StatusMessage = $"Poz {CollectedPoseCount + 1}: Robot pozisyonu okunuyor...";

                TransformMatrix flangeMatrix = await GetFlangeMatrixFromRobotAsync(robot);
                if (flangeMatrix == null)
                {
                    StatusMessage = "HATA: Robot flange pozu okunamadi.";
                    return false;
                }

                // --- 2. Gocator olcumu al ---
                StatusMessage = $"Poz {CollectedPoseCount + 1}: Gocator olcumu aliniyor...";

                var (status, measurements) = await ReceiveMeasurementLogic.ReceiveAndProcessMeasurements(
                    msg => StatusMessage = msg, dispatcher);

                if (status != 1 || measurements == null || measurements.Count < 12)
                {
                    StatusMessage = $"HATA: Gocator olcumu basarisiz (status={status}, count={measurements?.Count ?? 0}). " +
                                   "BallBar toolu en az 12 olcum degeri uretmeli.";
                    return false;
                }

                // --- 3. 12 olcum degerinden sensor matrisi olustur ---
                TransformMatrix sensorMatrix = BuildMatrixFromMeasurements(measurements);
                if (sensorMatrix == null)
                {
                    StatusMessage = "HATA: Olcum degerlerinden matris olusturulamadi.";
                    return false;
                }

                // --- 4. Poz ciftini kaydet ---
                _posePairs.Add((flangeMatrix, sensorMatrix));
                OnPropertyChanged(nameof(CollectedPoseCount));

                var flangePose = KukaPose.FromMatrix(flangeMatrix);
                StatusMessage = $"Poz {CollectedPoseCount} kaydedildi. " +
                               $"Flange: {flangePose.ToKukaString()}";

                return true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"HATA: {ex.Message}";
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Disaridan alinan olcumlerle poz yakalar.
        /// Job yukleme ve olcum alma UI tarafindan yapilir, bu metod sadece:
        /// 1. Robot flange pozunu okur
        /// 2. Olcum degerlerinden sensor matrisi olusturur
        /// 3. (flange, sensor) ciftini kaydeder
        /// </summary>
        /// <param name="robot">Bagli robot instance</param>
        /// <param name="measurements">Gocator'dan alinan olcum degerleri</param>
        /// <returns>Basarili ise true</returns>
        public async Task<bool> CapturePoseWithMeasurementsAsync(
            KukaRobotInstance robot, List<GocatorMeasurement> measurements)
        {
            if (robot == null || !robot.IsConnected)
            {
                StatusMessage = "HATA: Robot bagli degil.";
                return false;
            }

            if (measurements == null || measurements.Count < 12)
            {
                StatusMessage = $"HATA: En az 12 olcum degeri gerekli (gelen: {measurements?.Count ?? 0}).";
                return false;
            }

            IsBusy = true;
            try
            {
                // --- 1. Robot flange pozunu oku ---
                StatusMessage = $"Poz {CollectedPoseCount + 1}: Robot pozisyonu okunuyor...";

                TransformMatrix flangeMatrix = await GetFlangeMatrixFromRobotAsync(robot);
                if (flangeMatrix == null)
                {
                    StatusMessage = "HATA: Robot flange pozu okunamadi.";
                    return false;
                }

                // --- 2. Olcum degerlerinden sensor matrisi olustur ---
                TransformMatrix sensorMatrix = BuildMatrixFromMeasurements(measurements);
                if (sensorMatrix == null)
                {
                    StatusMessage = "HATA: Olcum degerlerinden matris olusturulamadi.";
                    return false;
                }

                // --- 3. Poz ciftini kaydet ---
                _posePairs.Add((flangeMatrix, sensorMatrix));
                OnPropertyChanged(nameof(CollectedPoseCount));

                var flangePose = KukaPose.FromMatrix(flangeMatrix);
                StatusMessage = $"Poz {CollectedPoseCount} kaydedildi. " +
                               $"Flange: {flangePose.ToKukaString()}";

                return true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"HATA: {ex.Message}";
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Manuel olarak bir poz cifti ekler (test veya harici veri icin).
        /// </summary>
        public void AddPosePair(TransformMatrix flangeInBase, TransformMatrix targetInSensor)
        {
            _posePairs.Add((flangeInBase, targetInSensor));
            OnPropertyChanged(nameof(CollectedPoseCount));
            StatusMessage = $"Manuel poz eklendi. Toplam: {CollectedPoseCount}";
        }

        /// <summary>
        /// Toplanan poz ciftleri uzerinde Tsai-Lenz kalibrasyonunu calistirir.
        /// En az 3 poz cifti gerekli (4+ onerili).
        /// </summary>
        /// <returns>Kalibrasyon dogruluk metrikleri</returns>
        public CalibrationAccuracy RunCalibration()
        {
            if (CollectedPoseCount < 3)
                throw new InvalidOperationException($"En az 3 poz cifti gerekli. Mevcut: {CollectedPoseCount}");

            StatusMessage = "Kalibrasyon hesaplaniyor...";

            var flanges = _posePairs.Select(p => p.flange).ToArray();
            var sensors = _posePairs.Select(p => p.sensor).ToArray();

            // Tsai-Lenz AX=XB cozumu
            HandEyeMatrix = HandEyeCalibration.CalibrateEyeOnHand(flanges, sensors);

            // Dogruluk olcumu — tum olcumleri base'e donustur ve tutarlilik analiz et
            var locatedInBase = HandEyeCalibration.LocateEyeOnHandBatch(flanges, sensors, HandEyeMatrix);
            var accuracy = HandEyeCalibration.MeasureAccuracy(locatedInBase);
            LastAccuracy = accuracy;

            OnPropertyChanged(nameof(IsCalibrated));
            OnPropertyChanged(nameof(HandEyeMatrix));

            var handEyePose = KukaPose.FromMatrix(HandEyeMatrix);
            StatusMessage = $"Kalibrasyon tamamlandi! " +
                           $"HandEye: {handEyePose.ToKukaString()} | " +
                           $"Pozisyon std: {accuracy.PositionStdMm:F3} mm, " +
                           $"Aci std: {accuracy.AngleStdDeg:F3} deg";

            return accuracy;
        }

        #endregion

        #region Persistence (Save/Load)

        /// <summary>
        /// Kalibrasyon sonucunu JSON olarak kaydeder.
        /// Dosya yolu: %LocalAppData%\App4\HandEyeCalibration.json
        /// </summary>
        public void SaveCalibration(string robotName = "Robot1")
        {
            if (HandEyeMatrix == null)
                throw new InvalidOperationException("Kalibrasyon henuz yapilmamis.");

            var data = new CalibrationData
            {
                RobotName = robotName,
                CalibrationDate = DateTime.Now,
                PoseCount = _posePairs.Count,
                HandEyeMatrix = HandEyeMatrix.ToArray(),
                AngleRangeDeg = LastAccuracy?.AngleRangeDeg ?? 0,
                AngleStdDeg = LastAccuracy?.AngleStdDeg ?? 0,
                PositionRangeMm = LastAccuracy?.PositionRangeMm ?? 0,
                PositionStdMm = LastAccuracy?.PositionStdMm ?? 0,
                PoseRecords = _posePairs.Select((pair, i) => new CalibrationPoseRecord
                {
                    Index = i,
                    FlangeInBase = pair.flange.ToArray(),
                    TargetInSensor = pair.sensor.ToArray(),
                    CaptureTime = DateTime.Now
                }).ToList(),
                Sphere1Radius = this.Sphere1Radius,
                Sphere2Radius = this.Sphere2Radius,
                BarLength = this.BarLength
            };

            try
            {
                var dir = Path.GetDirectoryName(_calibrationFilePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_calibrationFilePath, json);

                LastCalibrationData = data;
                StatusMessage = $"Kalibrasyon kaydedildi: {_calibrationFilePath}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"HATA: Kalibrasyon kaydedilemedi - {ex.Message}";
            }
        }

        /// <summary>
        /// JSON'dan kalibrasyon yukler.
        /// </summary>
        /// <returns>Basarili ise true</returns>
        public bool LoadCalibration()
        {
            try
            {
                if (!File.Exists(_calibrationFilePath))
                {
                    StatusMessage = "Kalibrasyon dosyasi bulunamadi.";
                    return false;
                }

                var json = File.ReadAllText(_calibrationFilePath);
                var data = JsonSerializer.Deserialize<CalibrationData>(json);

                if (data?.HandEyeMatrix == null || data.HandEyeMatrix.Length != 12)
                {
                    StatusMessage = "HATA: Kalibrasyon verisi gecersiz.";
                    return false;
                }

                HandEyeMatrix = TransformMatrix.FromArray(data.HandEyeMatrix);
                LastCalibrationData = data;

                // BallBar parametrelerini yukle
                if (data.Sphere1Radius > 0) Sphere1Radius = data.Sphere1Radius;
                if (data.Sphere2Radius > 0) Sphere2Radius = data.Sphere2Radius;
                if (data.BarLength > 0) BarLength = data.BarLength;

                // Poz ciftlerini de yukle (tekrar kalibrasyon icin)
                _posePairs.Clear();
                if (data.PoseRecords != null)
                {
                    foreach (var record in data.PoseRecords)
                    {
                        if (record.FlangeInBase?.Length == 12 && record.TargetInSensor?.Length == 12)
                        {
                            _posePairs.Add((
                                TransformMatrix.FromArray(record.FlangeInBase),
                                TransformMatrix.FromArray(record.TargetInSensor)));
                        }
                    }
                }

                OnPropertyChanged(nameof(IsCalibrated));
                OnPropertyChanged(nameof(HandEyeMatrix));
                OnPropertyChanged(nameof(CollectedPoseCount));

                var handEyePose = KukaPose.FromMatrix(HandEyeMatrix);
                StatusMessage = $"Kalibrasyon yuklendi ({data.CalibrationDate:yyyy-MM-dd HH:mm}). " +
                               $"Robot: {data.RobotName}, Pozlar: {data.PoseCount}. " +
                               $"HandEye: {handEyePose.ToKukaString()}";
                return true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"HATA: Kalibrasyon yuklenemedi - {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Uygulama baslatildiginda otomatik yukle.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            LoadCalibration();
            // Global erişim için kalibrasyon bilgilerini güncelle
            GlobalData.UpdateActiveCalibrationInfo();
        }

        #endregion

        #region Runtime - Locate (Sensor → Base Donusumu)

        /// <summary>
        /// Sensor koordinatlarini robot base koordinatlarina donusturur.
        /// T_base_target = T_base_flange * handEye * T_sensor_target
        /// </summary>
        /// <param name="flangeInBase">Robot flange pozu base'de</param>
        /// <param name="targetInSensor">Hedef pozu sensor'de</param>
        /// <returns>Hedefin base koordinat sistemindeki pozu</returns>
        public TransformMatrix LocateInBase(TransformMatrix flangeInBase, TransformMatrix targetInSensor)
        {
            if (!IsCalibrated)
                throw new InvalidOperationException("Kalibrasyon yapilmamis. Once CalibrateEyeOnHand calistirilmali.");

            return HandEyeCalibration.LocateEyeOnHand(flangeInBase, targetInSensor, HandEyeMatrix);
        }

        /// <summary>
        /// Convenience wrapper: KukaPose olarak dondurur.
        /// </summary>
        public KukaPose LocateKukaPoseInBase(KukaPose flangePose, TransformMatrix targetInSensor)
        {
            var flangeMatrix = flangePose.ToMatrix();
            var targetInBase = LocateInBase(flangeMatrix, targetInSensor);
            return KukaPose.FromMatrix(targetInBase);
        }

        /// <summary>
        /// Tam akis: Robot'tan flange oku → sensor olcumunu donustur → KukaPose dondur.
        /// </summary>
        public async Task<KukaPose> LocateFromRobotAsync(
            KukaRobotInstance robot, TransformMatrix targetInSensor, int userBaseNo = -1)
        {
            if (!IsCalibrated)
                throw new InvalidOperationException("Kalibrasyon yapilmamis.");

            var flangeMatrix = await GetFlangeMatrixFromRobotAsync(robot);
            if (flangeMatrix == null) return null;

            // Debug: Flange matrisini logla
            var flangePose = KukaPose.FromMatrix(flangeMatrix);
            StatusMessage = $"[DEBUG] Flange: X={flangePose.X:F2} Y={flangePose.Y:F2} Z={flangePose.Z:F2} A={flangePose.A:F2} B={flangePose.B:F2} C={flangePose.C:F2}";

            // Hand-Eye donusum: sonuc $POS_ACT'in baz aldigi koordinat sisteminde
            var targetInActiveBase = LocateInBase(flangeMatrix, targetInSensor);

            // Kullanici farkli bir base'de calisiyorsa donustur
            if (userBaseNo >= 0)
            {
                targetInActiveBase = await ConvertToUserBaseAsync(robot, targetInActiveBase, userBaseNo);
            }

            return KukaPose.FromMatrix(targetInActiveBase);
        }

        /// <summary>
        /// Sonucu $BASE_ACT koordinat sisteminden kullanicinin istegi base'e donusturur.
        /// $POS_ACT, $BASE_ACT'in tanimladigi base'de veri verir.
        /// Kullanici farkli bir base (ornegin Base 1) ile calisiyorsa:
        ///   result_userBase = UserBase^(-1) * ActiveBase * result_activeBase
        /// </summary>
        private async Task<TransformMatrix> ConvertToUserBaseAsync(
            KukaRobotInstance robot, TransformMatrix resultInActiveBase, int userBaseNo)
        {
            try
            {
                int activeBase = (int)ParseRobotDouble(await robot.ReadVariableAsync("$ACT_BASE"));

                StatusMessage = $"[DEBUG] $ACT_BASE={activeBase}, Hedef Base={userBaseNo}";

                if (activeBase == userBaseNo)
                    return resultInActiveBase; // Zaten dogru base'de

                // Aktif base matrisi (0=World=Identity)
                TransformMatrix activeBM = TransformMatrix.Identity;
                if (activeBase > 0)
                    activeBM = await ReadBaseMatrixAsync(robot, activeBase);

                // Kullanici base matrisi
                TransformMatrix userBM = TransformMatrix.Identity;
                if (userBaseNo > 0)
                    userBM = await ReadBaseMatrixAsync(robot, userBaseNo);

                // World'e cevir, sonra kullanici base'ine cevir
                var resultInWorld = activeBM * resultInActiveBase;
                var resultInUserBase = userBM.Inverse() * resultInWorld;

                var dbgPose = KukaPose.FromMatrix(resultInUserBase);
                StatusMessage = $"[DEBUG] Base{userBaseNo}'e donusturuldu: X={dbgPose.X:F2} Y={dbgPose.Y:F2} Z={dbgPose.Z:F2}";

                return resultInUserBase;
            }
            catch (Exception ex)
            {
                StatusMessage = $"[DEBUG] Base donusum hatasi: {ex.Message}, donusturmeden devam";
                return resultInActiveBase;
            }
        }

        /// <summary>
        /// $BASE[n] degerlerini okuyarak TransformMatrix olusturur.
        /// </summary>
        private async Task<TransformMatrix> ReadBaseMatrixAsync(KukaRobotInstance robot, int baseNo)
        {
            double bx = ParseRobotDouble(await robot.ReadVariableAsync($"$BASE[{baseNo}].X"));
            double by = ParseRobotDouble(await robot.ReadVariableAsync($"$BASE[{baseNo}].Y"));
            double bz = ParseRobotDouble(await robot.ReadVariableAsync($"$BASE[{baseNo}].Z"));
            double ba = ParseRobotDouble(await robot.ReadVariableAsync($"$BASE[{baseNo}].A"));
            double bb = ParseRobotDouble(await robot.ReadVariableAsync($"$BASE[{baseNo}].B"));
            double bc = ParseRobotDouble(await robot.ReadVariableAsync($"$BASE[{baseNo}].C"));
            return new KukaPose(bx, by, bz, ba, bb, bc).ToMatrix();
        }

        #endregion

        #region Robot Flange Pose Helpers

        /// <summary>
        /// Robot'tan flange matrisini okur.
        /// TCP = KukaPose(PosX..PosC).ToMatrix()
        /// Tool = KukaPose($TOOL[n].X..$TOOL[n].C).ToMatrix()
        /// Flange = TCP * Tool.Inverse()
        /// </summary>
        private async Task<TransformMatrix> GetFlangeMatrixFromRobotAsync(KukaRobotInstance robot)
        {
            try
            {
                // TCP pozu (surekli okunan degerler)
                var tcpPose = new KukaPose(robot.PosX, robot.PosY, robot.PosZ,
                                           robot.PosA, robot.PosB, robot.PosC);
                var tcpMatrix = tcpPose.ToMatrix();

                int toolNo = robot.ToolNo;
                if (toolNo <= 0) return tcpMatrix; // Tool 0 = flange = TCP

                // $TOOL[n] degerlerini oku
                double tx = ParseRobotDouble(await robot.ReadVariableAsync($"$TOOL[{toolNo}].X"));
                double ty = ParseRobotDouble(await robot.ReadVariableAsync($"$TOOL[{toolNo}].Y"));
                double tz = ParseRobotDouble(await robot.ReadVariableAsync($"$TOOL[{toolNo}].Z"));
                double ta = ParseRobotDouble(await robot.ReadVariableAsync($"$TOOL[{toolNo}].A"));
                double tb = ParseRobotDouble(await robot.ReadVariableAsync($"$TOOL[{toolNo}].B"));
                double tc = ParseRobotDouble(await robot.ReadVariableAsync($"$TOOL[{toolNo}].C"));

                var toolMatrix = new KukaPose(tx, ty, tz, ta, tb, tc).ToMatrix();

                // Flange = TCP * Tool^(-1)
                return tcpMatrix * toolMatrix.Inverse();
            }
            catch (Exception ex)
            {
                StatusMessage = $"HATA: Flange pozu okunamadi - {ex.Message}";
                return null;
            }
        }

        /// <summary>
        /// Robot'tan okunan string degeri double'a cevirir.
        /// KukaVarProxy format: virgul veya nokta ayirici olabilir.
        /// </summary>
        private static double ParseRobotDouble(string val)
        {
            if (string.IsNullOrEmpty(val)) return 0;
            val = val.Replace(",", ".").Trim();
            return double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out double result)
                ? result : 0;
        }

        #endregion

        #region Sensor Measurement Helpers

        /// <summary>
        /// Gocator BallBar olcumlerinden (12 deger) TransformMatrix olusturur.
        ///
        /// Gocator SurfaceBallBar toolu 12 degeri ROW-MAJOR sirada cikarir:
        ///   [R00, R01, R02,  R10, R11, R12,  R20, R21, R22,  Tx, Ty, Tz]
        ///
        /// TransformMatrix COLUMN-MAJOR saklar:
        ///   Ix=R00, Iy=R10, Iz=R20   (I kolonu = matrisin 1. sutunu)
        ///   Jx=R01, Jy=R11, Jz=R21   (J kolonu = matrisin 2. sutunu)
        ///   Kx=R02, Ky=R12, Kz=R22   (K kolonu = matrisin 3. sutunu)
        ///
        /// GoRobot::GetBallBarPoseMeasurement C# karsiligi.
        /// </summary>
        private TransformMatrix BuildMatrixFromMeasurements(List<GocatorMeasurement> measurements)
        {
            try
            {
                // SourceId'ye gore sirala (ardisik 12 deger)
                var sorted = measurements.OrderBy(m => m.SourceId).Take(12).ToList();

                if (sorted.Count < 12) return null;

                // Gocator row-major cikis:
                // [0]=R00  [1]=R01  [2]=R02
                // [3]=R10  [4]=R11  [5]=R12
                // [6]=R20  [7]=R21  [8]=R22
                // [9]=Tx  [10]=Ty  [11]=Tz
                //
                // TransformMatrix(Ix,Iy,Iz, Jx,Jy,Jz, Kx,Ky,Kz, Tx,Ty,Tz) = column-major
                // Ix=R00, Iy=R10, Iz=R20 → sorted[0], sorted[3], sorted[6]
                // Jx=R01, Jy=R11, Jz=R21 → sorted[1], sorted[4], sorted[7]
                // Kx=R02, Ky=R12, Kz=R22 → sorted[2], sorted[5], sorted[8]

                return new TransformMatrix(
                    sorted[0].Value, sorted[3].Value, sorted[6].Value,     // Ix=R00, Iy=R10, Iz=R20
                    sorted[1].Value, sorted[4].Value, sorted[7].Value,     // Jx=R01, Jy=R11, Jz=R21
                    sorted[2].Value, sorted[5].Value, sorted[8].Value,     // Kx=R02, Ky=R12, Kz=R22
                    sorted[9].Value, sorted[10].Value, sorted[11].Value);  // Tx, Ty, Tz
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
