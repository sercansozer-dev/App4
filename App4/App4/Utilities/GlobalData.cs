using App4.PAGES;
using GoPxLSdk;
using GoPxLSdk.GoGdpMsg;
using Microsoft.UI.Dispatching;
using Newtonsoft.Json; // Newtonsoft.Json eklendi
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json; // System.Text.Json (JsonElement için)
using System.Text.Json.Serialization; // JsonIgnore (System)
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using static App4.PAGES.Camera_Page;
using static App4.Utilities.ExtendedStationViewModel;
using App4.Utilities.GoRobotMath;

namespace App4.Utilities
{
    public static class GlobalData
    {
        // --- DOSYA YOLLARI ---
        public static readonly string ConfigBaseDir = @"C:\Simbiosis\SimbiosisLeakTestApp\Config";
        private static readonly string _rfidFilePath = Path.Combine(ConfigBaseDir, "Saved_RFID_List.json");
        private static readonly string _stationStateFilePath = Path.Combine(ConfigBaseDir, "Station_States.json");
        private static readonly string _autoPageVariablesFilePath = Path.Combine(ConfigBaseDir, "Auto_Page_Variables.json");
        private static readonly string _measurementsFilePath = Path.Combine(ConfigBaseDir, "Saved_Measurements.json");
        private static readonly string _transferRowsFilePath = Path.Combine(ConfigBaseDir, "Camera_PlcTransfer.json");
        private static readonly string _systemChecksFilePath = Path.Combine(ConfigBaseDir, "System_Checks.json");
        private static readonly string _transformedMeasurementsFilePath = Path.Combine(ConfigBaseDir, "Transformed_Measurements.json");
        private static readonly string _robotSliderMappingFilePath = Path.Combine(ConfigBaseDir, "Robot_Slider_Mapping.json");
        private static readonly string _safetyAlarmsFilePath = Path.Combine(ConfigBaseDir, "Safety_Alarms.json");
        private static readonly string _safetyWarningsFilePath = Path.Combine(ConfigBaseDir, "Safety_Warnings.json");
        private static readonly string _inficonLogsFilePath = Path.Combine(ConfigBaseDir, "Inficon_Leak_Logs.json");
        private static readonly string _casingTypesFilePath = Path.Combine(ConfigBaseDir, "Casing_Types.json");

        // --- GLOBAL LİSTELER ---
        public static ObservableCollection<RfidDef> KnownRfids { get; private set; } = new();

        // YENİ: Model Klasöründeki Dosyaların Listesi
        public static ObservableCollection<string> AvailableModels { get; private set; } = new();

        public static ObservableCollection<StationViewModel> Stations { get; private set; } = new();
        public static ObservableCollection<SystemCheckItem> SystemCheckList { get; private set; } = new();
        public static ObservableCollection<SafetyCheckItem> SafetyAlarmList { get; private set; } = new();
        public static ObservableCollection<SafetyCheckItem> SafetyWarningList { get; private set; } = new();
        public static ObservableCollection<App4.Models.InficonLeakLogEntry> InficonLeakLogs { get; private set; } = new();
        // Sniffer aktif nokta bazlı kaçak sonuçları (Inficon_Page tabloları)
        public static ObservableCollection<App4.Models.SnifferPointResult> Robot1SnifferPoints { get; set; } = new();
        public static ObservableCollection<App4.Models.SnifferPointResult> Robot2SnifferPoints { get; set; } = new();
        public static ObservableCollection<GocatorMeasurement> LastMeasurements { get; private set; } = new();
        public static ObservableCollection<CasingType> CasingTypes { get; private set; } = new();
        public static ObservableCollection<PlcTransferItem> PlcTransferRows { get; private set; } = new();

        // DÖNÜŞTÜRÜLMÜŞ ÖLÇÜM (Hand-Eye kalibrasyon sonrası base koordinatları)
        public static ObservableCollection<GocatorMeasurement> TransformedMeasurements { get; private set; } = new();

        // CODESYS HESAPLAMA SONUÇLARI (otomasyon sırasında hesaplanan hedef nokta)
        public static ObservableCollection<GocatorMeasurement> CodesysTargetResults { get; private set; } = new();

        // TABLA CODESYS SONUÇLARI (YENİ ÖLÇÜM NOKTASI paneli — tabla ölçüm CODESYS çıktısı)
        public static ObservableCollection<GocatorMeasurement> TablaCodesysTargetResults { get; private set; } = new();

        // ═══ AKTİF KALİBRASYON BİLGİLERİ (tüm sayfalardan erişilebilir) ═══
        public static string CalibHandEyeX { get; set; } = "---";
        public static string CalibHandEyeY { get; set; } = "---";
        public static string CalibHandEyeZ { get; set; } = "---";
        public static string CalibHandEyeA { get; set; } = "---";
        public static string CalibHandEyeB { get; set; } = "---";
        public static string CalibHandEyeC { get; set; } = "---";
        public static string CalibAccuracyMm { get; set; } = "--- mm";
        public static string CalibDate { get; set; } = "---";
        public static string CalibRobotName { get; set; } = "---";
        public static bool CalibIsActive { get; set; } = false;

        /// <summary>
        /// Aktif kalibrasyon bilgilerini gunceller (tum sayfalardan gorulebilir).
        /// CalibrationService.LoadCalibration veya RunCalibration sonrasi cagirilir.
        /// </summary>
        public static void UpdateActiveCalibrationInfo()
        {
            var svc = CalibrationService.Instance;
            if (svc.IsCalibrated && svc.HandEyeMatrix != null)
            {
                var pose = KukaPose.FromMatrix(svc.HandEyeMatrix);
                CalibHandEyeX = pose.X.ToString("F2");
                CalibHandEyeY = pose.Y.ToString("F2");
                CalibHandEyeZ = pose.Z.ToString("F2");
                CalibHandEyeA = pose.A.ToString("F2");
                CalibHandEyeB = pose.B.ToString("F2");
                CalibHandEyeC = pose.C.ToString("F2");
                CalibIsActive = true;

                if (svc.LastCalibrationData != null)
                {
                    CalibAccuracyMm = $"{svc.LastCalibrationData.PositionStdMm:F3} mm";
                    CalibDate = svc.LastCalibrationData.CalibrationDate.ToString("yyyy-MM-dd HH:mm");
                    CalibRobotName = svc.LastCalibrationData.RobotName ?? "---";
                }
            }
            else
            {
                CalibHandEyeX = "---"; CalibHandEyeY = "---"; CalibHandEyeZ = "---";
                CalibHandEyeA = "---"; CalibHandEyeB = "---"; CalibHandEyeC = "---";
                CalibAccuracyMm = "--- mm"; CalibDate = "---"; CalibRobotName = "---";
                CalibIsActive = false;
            }
        }

        // TABLA KAÇIKLIK İÇİN AYRI LİSTELER
        public static ObservableCollection<GocatorMeasurement> TablaLastMeasurements { get; private set; } = new();
        public static ObservableCollection<PlcTransferItem> TablaTransferRows { get; private set; } = new();

        // ═══ AKTUEL RFID / KLİMA INDEX ═══
        // Her yerden erişilebilir statik property'ler
        private static string _aktuelRfid = "";
        public static string AktuelRfid
        {
            get => _aktuelRfid;
            set
            {
                if (_aktuelRfid != value)
                {
                    _aktuelRfid = value ?? "";
                    // GeneralOutputVars'taki değişkeni de güncelle (UI tablosunda görünsün)
                    var rfidVar = GeneralOutputVars.FirstOrDefault(v => v.Name == "AKTUEL_RFID");
                    if (rfidVar != null && rfidVar.Value != _aktuelRfid)
                        rfidVar.Value = _aktuelRfid;
                    // PlcService bridge değişkenini de güncelle (PLC'ye yazılsın)
                    var plcVar = PlcService.Instance?.OutputVariables?.FirstOrDefault(v => v.Name == "AKTUEL_RFID");
                    if (plcVar != null && plcVar.CurrentValue?.ToString() != _aktuelRfid)
                        plcVar.CurrentValue = _aktuelRfid;
                    // Kamera sayfasindaki klima kartlarinin cercevesini guncelle
                    UpdateActiveRfidHighlight(_aktuelRfid);
                    System.Diagnostics.Debug.WriteLine($"[GlobalData] AktuelRfid = '{_aktuelRfid}'");

                    // ═══ KRİTİK: Yeni klima tipi seçildiğinde Sniffer + Sapma limiti hemen robota yaz ═══
                    SyncCurrentJobOutputs();
                }
            }
        }

        /// <summary>
        /// KnownRfids icinde aktuel RFID'ye esit olanin IsActive=true, digerlerini false yapar.
        /// Kamera sayfasindaki kart cercevesi yesil gosterimi icin.
        /// </summary>
        public static void UpdateActiveRfidHighlight(string rfidId)
        {
            foreach (var rfid in KnownRfids)
            {
                rfid.IsActive = !string.IsNullOrEmpty(rfidId) &&
                                string.Equals(rfid.Id, rfidId, StringComparison.OrdinalIgnoreCase);
            }
        }

        // ═══ Slider Hedef İstasyon (otomasyon tarafından set edilir) ═══
        private static int _targetSliderStation = 0;
        /// <summary>
        /// Slider'ın gitmesi gereken hedef istasyon numarası (1, 2, 3).
        /// UpdateAktuelRfidFromStation tarafından set edilir.
        /// </summary>
        public static int TargetSliderStation
        {
            get => _targetSliderStation;
            set => _targetSliderStation = value;
        }

        /// <summary>
        /// Hedef istasyonun mm cinsinden slider pozisyonunu döndürür.
        /// Manuel sayfada tanımlanan KL100_Station1/2/3Pos değerlerinden alır.
        /// </summary>
        public static double GetStationSliderPosition(int stationNumber) => stationNumber switch
        {
            1 => KL100_Station1Pos,
            2 => KL100_Station2Pos,
            3 => KL100_Station3Pos,
            _ => 0
        };

        private static int _aktuelKlimaIndex = 0;
        public static int AktuelKlimaIndex
        {
            get => _aktuelKlimaIndex;
            set
            {
                if (_aktuelKlimaIndex != value)
                {
                    _aktuelKlimaIndex = value;
                    // GeneralOutputVars'taki değişkeni de güncelle (UI tablosunda görünsün)
                    var indexVar = GeneralOutputVars.FirstOrDefault(v => v.Name == "AKTUEL_KLIMA_INDEX");
                    if (indexVar != null)
                    {
                        string idxStr = _aktuelKlimaIndex.ToString();
                        if (indexVar.Value != idxStr)
                            indexVar.Value = idxStr;
                    }
                    // PlcService bridge değişkenini de güncelle (PLC'ye yazılsın)
                    var plcVar = PlcService.Instance?.OutputVariables?.FirstOrDefault(v => v.Name == "AKTUEL_KLIMA_INDEX");
                    if (plcVar != null)
                    {
                        object cv = plcVar.CurrentValue;
                        if (cv?.ToString() != _aktuelKlimaIndex.ToString())
                            plcVar.CurrentValue = _aktuelKlimaIndex;
                    }

                    // Tüm bağlı robotlara G_KLIMA_TIP yaz (KukaVarProxy)
                    // CommunicationLoop sadece okuma yapar, otomatik yazma yok.
                    // Bu yüzden değer değiştiğinde robotlara açıkça yazıyoruz.
                    string klimaStr = _aktuelKlimaIndex.ToString();
                    var robots = KukaRobotManager.Instance?.Robots;
                    if (robots != null)
                    {
                        foreach (var robot in robots)
                        {
                            if (robot.IsConnected)
                            {
                                _ = robot.WriteVariableAsync("G_KLIMA_TIP", klimaStr);
                            }
                        }
                    }
                    System.Diagnostics.Debug.WriteLine($"[GlobalData] AktuelKlimaIndex = {_aktuelKlimaIndex} (robotlara yazıldı)");

                    // AKTUEL_CASE_ID: RFID'nin CasingIndex degerini oku ve yaz
                    int caseId = 0;
                    if (_aktuelKlimaIndex > 0 && _aktuelKlimaIndex <= KnownRfids.Count)
                    {
                        var rfid = KnownRfids[_aktuelKlimaIndex - 1];
                        caseId = rfid.CasingIndex;
                    }

                    // GeneralOutputVars guncelle
                    var caseVar = GeneralOutputVars.FirstOrDefault(v => v.Name == "AKTUEL_CASE_ID");
                    if (caseVar != null)
                    {
                        string caseStr = caseId.ToString();
                        if (caseVar.Value != caseStr) caseVar.Value = caseStr;
                    }

                    // PLC'ye yaz
                    var plcCaseVar = PlcService.Instance?.OutputVariables?.FirstOrDefault(v => v.Name == "AKTUEL_CASE_ID");
                    if (plcCaseVar != null)
                    {
                        if (plcCaseVar.CurrentValue?.ToString() != caseId.ToString())
                            plcCaseVar.CurrentValue = caseId;
                    }

                    // Robotlara G_CASE_ID yaz
                    string caseIdStr = caseId.ToString();
                    if (robots != null)
                    {
                        foreach (var robot in robots)
                        {
                            if (robot.IsConnected)
                            {
                                _ = robot.WriteVariableAsync("G_CASE_ID", caseIdStr);
                            }
                        }
                    }
                    System.Diagnostics.Debug.WriteLine($"[GlobalData] AktuelCaseId = {caseId} (robotlara yazıldı)");

                    // ═══ RFID değişince mevcut G_JOB_INDEX için job ön-yükle ═══
                    // (0→0 değişim tetiklenmez, bu yüzden RFID değişiminde zorla tetikle)
                    try
                    {
                        var jobIdxVar = RobotOutputVars.FirstOrDefault(v => v.Name == "G_JOB_INDEX");
                        string currentJobIdx = jobIdxVar?.Value ?? "0";
                        _ = HandleJobIndexChangeAsync(currentJobIdx, 0);
                    }
                    catch { }
                }
            }
        }

        // PLC Değişken Listeleri
        public static ObservableCollection<PlcVariable> GeneralInputVars { get; private set; } = new();
        public static ObservableCollection<PlcVariable> GeneralOutputVars { get; private set; } = new();
        public static ObservableCollection<PlcVariable> Station1Vars { get; private set; } = new();
        public static ObservableCollection<PlcVariable> Station2Vars { get; private set; } = new();
        public static ObservableCollection<PlcVariable> Station3Vars { get; private set; } = new();
        public static ObservableCollection<PlcVariable> Station1Outputs { get; private set; } = new();
        public static ObservableCollection<PlcVariable> Station2Outputs { get; private set; } = new();
        public static ObservableCollection<PlcVariable> Station3Outputs { get; private set; } = new();
        public static ObservableCollection<PlcVariable> RobotInputVars { get; private set; } = new();
        public static ObservableCollection<PlcVariable> RobotOutputVars { get; private set; } = new();

        // ═══════════════════════════════════════════════════════════════════════════
        // ROBOT SLİDER SİNYAL EŞLEŞTİRME - Robottan gelen verilerle görsel eşleştirme
        // ═══════════════════════════════════════════════════════════════════════════
        public static RobotSliderSignalMapping Robot1SliderMapping { get; private set; } = new("Robot 1");
        public static RobotSliderSignalMapping Robot2SliderMapping { get; private set; } = new("Robot 2");

        // ═══════════════════════════════════════════════════════════════════════════
        // SLİDER POZİSYON EŞLEŞTİRME (Basitleştirilmiş)
        // Hangi robotun hangi sinyali → Slider görseli pozisyonunu belirler
        // ═══════════════════════════════════════════════════════════════════════════
        private static int _sliderSourceRobotIndex = 0; // 0 = Robot 1, 1 = Robot 2
        public static int SliderSourceRobotIndex
        {
            get => _sliderSourceRobotIndex;
            set { if (_sliderSourceRobotIndex != value) { _sliderSourceRobotIndex = value; SaveRobotSliderMappings(); } }
        }

        // İstasyon sinyali: Robottan 1, 2, 3 değeri gelir → görsel o istasyona gider
        private static string _sliderSourceSignalName = "E1";
        public static string SliderSourceSignalName
        {
            get => _sliderSourceSignalName;
            set { if (_sliderSourceSignalName != value) { _sliderSourceSignalName = value; SaveRobotSliderMappings(); } }
        }

        // KL100 R1 ve R2 Home Sinyal Seçimleri
        private static string _kl100Robot1HomeSignal = "";
        public static string KL100_Robot1HomeSignal
        {
            get => _kl100Robot1HomeSignal;
            set { if (_kl100Robot1HomeSignal != value) { _kl100Robot1HomeSignal = value; SaveAutomationSettings(); } }
        }

        private static string _kl100Robot2HomeSignal = "";
        public static string KL100_Robot2HomeSignal
        {
            get => _kl100Robot2HomeSignal;
            set { if (_kl100Robot2HomeSignal != value) { _kl100Robot2HomeSignal = value; SaveAutomationSettings(); } }
        }

        // ═══ KL100 İSTASYON POZİSYONLARI (mm cinsinden kaydedilen konum verileri) ═══
        private static double _kl100StationBakimPos = 0;
        public static double KL100_StationBakimPos
        {
            get => _kl100StationBakimPos;
            set { if (_kl100StationBakimPos != value) { _kl100StationBakimPos = value; SaveKL100StationPositions(); SyncStationPosToRobots(); } }
        }

        private static double _kl100Station1Pos = 1041;
        public static double KL100_Station1Pos
        {
            get => _kl100Station1Pos;
            set { if (_kl100Station1Pos != value) { _kl100Station1Pos = value; SaveKL100StationPositions(); SyncStationPosToRobots(); } }
        }

        private static double _kl100Station2Pos = 2377;
        public static double KL100_Station2Pos
        {
            get => _kl100Station2Pos;
            set { if (_kl100Station2Pos != value) { _kl100Station2Pos = value; SaveKL100StationPositions(); SyncStationPosToRobots(); } }
        }

        private static double _kl100Station3Pos = 3716;
        public static double KL100_Station3Pos
        {
            get => _kl100Station3Pos;
            set { if (_kl100Station3Pos != value) { _kl100Station3Pos = value; SaveKL100StationPositions(); SyncStationPosToRobots(); } }
        }

        private static readonly string _kl100PosFilePath = System.IO.Path.Combine(ConfigBaseDir, "KL100_StationPositions.json");

        public static void SaveKL100StationPositions()
        {
            try
            {
                var data = new { StationBakim = _kl100StationBakimPos, Station1 = _kl100Station1Pos, Station2 = _kl100Station2Pos, Station3 = _kl100Station3Pos };
                var json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                var dir = System.IO.Path.GetDirectoryName(_kl100PosFilePath);
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                System.IO.File.WriteAllText(_kl100PosFilePath, json);
            }
            catch { }
        }

        public static void LoadKL100StationPositions()
        {
            try
            {
                if (System.IO.File.Exists(_kl100PosFilePath))
                {
                    var json = System.IO.File.ReadAllText(_kl100PosFilePath);
                    var doc = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
                    if (doc.TryGetProperty("StationBakim", out var sb)) _kl100StationBakimPos = sb.GetDouble();
                    if (doc.TryGetProperty("Station1", out var s1)) _kl100Station1Pos = s1.GetDouble();
                    if (doc.TryGetProperty("Station2", out var s2)) _kl100Station2Pos = s2.GetDouble();
                    if (doc.TryGetProperty("Station3", out var s3)) _kl100Station3Pos = s3.GetDouble();
                }
            }
            catch { }
        }

        /// <summary>
        /// İstasyon E1 pozisyonlarını robot output değişkenlerine yazar.
        /// Config değiştiğinde veya uygulama başlangıcında çağrılır.
        /// </summary>
        public static void SyncStationPosToRobots()
        {
            try
            {
                var robots = KukaRobotManager.Instance?.Robots;
                if (robots == null) return;
                foreach (var robot in robots)
                {
                    var bakimVar = robot.OutputVars.FirstOrDefault(v => v.Name == "G_IST_BAKIM_E1_POZ");
                    if (bakimVar != null) bakimVar.Value = _kl100StationBakimPos.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    var ist1Var = robot.OutputVars.FirstOrDefault(v => v.Name == "G_IST1_E1_POZ");
                    if (ist1Var != null) ist1Var.Value = _kl100Station1Pos.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    var ist2Var = robot.OutputVars.FirstOrDefault(v => v.Name == "G_IST2_E1_POZ");
                    if (ist2Var != null) ist2Var.Value = _kl100Station2Pos.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    var ist3Var = robot.OutputVars.FirstOrDefault(v => v.Name == "G_IST3_E1_POZ");
                    if (ist3Var != null) ist3Var.Value = _kl100Station3Pos.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            catch { }
        }



        // Aktüel pozisyon sinyali: Gerçek mm değerini gösterir (görseli etkilemez)
        private static string _sliderActualPosSignalName = "E1";
        public static string SliderActualPosSignalName
        {
            get => _sliderActualPosSignalName;
            set { if (_sliderActualPosSignalName != value) { _sliderActualPosSignalName = value; SaveRobotSliderMappings(); } }
        }

        /// <summary>
        /// İstasyon numarasını okur (1, 2, 3 veya 4=Bakım). Görsel pozisyonlama için.
        /// </summary>
        public static int GetSliderStationNumber()
        {
            double val = GetSliderPositionValue();
            int station = (int)Math.Round(val);
            if (station == 4) return 4; // Bakım İstasyonu
            return Math.Clamp(station, 1, 3);
        }

        /// <summary>
        /// İstasyon sinyalinin ham değerini okur (seçilen robotun seçilen sinyalinden)
        /// </summary>
        public static double GetSliderPositionValue()
        {
            var robots = KukaRobotManager.Instance?.Robots;
            if (robots == null || robots.Count <= SliderSourceRobotIndex) return 0;
            var robot = robots[SliderSourceRobotIndex];
            return GetRobotSignalValue(robot, SliderSourceSignalName);
        }

        /// <summary>
        /// Aktüel slider pozisyonunu mm olarak okur (görseli etkilemez)
        /// </summary>
        public static double GetSliderActualPosition()
        {
            var robots = KukaRobotManager.Instance?.Robots;
            if (robots == null || robots.Count <= SliderSourceRobotIndex) return 0;
            var robot = robots[SliderSourceRobotIndex];
            return GetRobotSignalValue(robot, SliderActualPosSignalName);
        }

        private static double GetRobotSignalValue(KukaRobotInstance robot, string signalName)
        {
            if (robot == null || string.IsNullOrEmpty(signalName)) return 0;

            // 1. Sabit property'ler
            var val = signalName switch
            {
                "E1" => robot.E1, "E2" => robot.E2, "E3" => robot.E3,
                "E4" => robot.E4, "E5" => robot.E5, "E6" => robot.E6,
                "PosX" => robot.PosX, "PosY" => robot.PosY, "PosZ" => robot.PosZ,
                "A1" => robot.A1, "A2" => robot.A2, "A3" => robot.A3,
                "A4" => robot.A4, "A5" => robot.A5, "A6" => robot.A6,
                "OverridePro" => robot.OverridePro,
                "OverrideJog" => robot.OverrideJog,
                "OperationMode" => robot.OperationMode,
                "ProgramState" => robot.ProgramState,
                "ToolNo" => robot.ToolNo,
                "BaseNo" => robot.BaseNo,
                _ => double.NaN
            };
            if (!double.IsNaN(val)) return val;

            // 2. Robot InputVars/OutputVars'dan dinamik sinyal oku
            var inputVar = robot.InputVars?.FirstOrDefault(v => v.Name == signalName);
            if (inputVar?.CurrentValue != null && double.TryParse(inputVar.CurrentValue.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double iv))
                return iv;

            var outputVar = robot.OutputVars?.FirstOrDefault(v => v.Name == signalName);
            if (outputVar?.CurrentValue != null && double.TryParse(outputVar.CurrentValue.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double ov))
                return ov;

            return 0;
        }

        private static bool _isInitialized = false;

        // OTOMASYON AYARLARI
        private static string _autoRfidTag;
        public static string Auto_RfidTag 
        { 
            get => _autoRfidTag; 
            set 
            { 
                if (_autoRfidTag == value) return; 
                _autoRfidTag = value; 
                SaveAutomationSettings(); 
            } 
        }

        private static string _autoIndexTag;
        public static string Auto_IndexTag
        {
            get => _autoIndexTag;
            set
            {
                if (_autoIndexTag == value) return;
                _autoIndexTag = value;
                SaveAutomationSettings();
                StartAutomationListener(); // Index watcher'ı yeniden kur
            }
        }

        private static string _autoTriggerTag;
        public static string Auto_TriggerTag
        {
            get => _autoTriggerTag;
            set
            {
                if (_autoTriggerTag == value) return;
                _autoTriggerTag = value;
                SaveAutomationSettings();
                StartAutomationListener();
            }
        }

        // TABLA KAÇIKLIK TETİK SİNYALİ (ikinci trigger)
        private static string _autoTriggerTag2;
        public static string Auto_TriggerTag2
        {
            get => _autoTriggerTag2;
            set
            {
                if (_autoTriggerTag2 == value) return;
                _autoTriggerTag2 = value;
                SaveAutomationSettings();
                StartAutomationListener();
            }
        }

        // ÖLÇÜMEvent SINYALI AYARLARI (Yeni ölçüm geldiğinde output sinyali)
        private static string _measurementOutputTag;
        public static string MeasurementOutputTag
        {
            get => _measurementOutputTag;
            set
            {
                if (_measurementOutputTag == value) return;
                _measurementOutputTag = value;
                SaveAutomationSettings();
            }
        }

        // --- TABLA ÖLÇÜM ÇIKTI TAG (Robot 1) ---
        private static string _tablaOutputTag;
        public static string TablaOutputTag
        {
            get => _tablaOutputTag;
            set
            {
                if (_tablaOutputTag == value) return;
                _tablaOutputTag = value;
                SaveAutomationSettings();
            }
        }

        // --- TABLA ÖLÇÜM ÇIKTI TAG (Robot 2) ---
        private static string _tablaOutputTag2;
        public static string TablaOutputTag2
        {
            get => _tablaOutputTag2;
            set
            {
                if (_tablaOutputTag2 == value) return;
                _tablaOutputTag2 = value;
                SaveAutomationSettings();
            }
        }

        // ═══ SİNYAL DURUM BAYRAKLARI (UI için güvenilir kaynak) ═══
        // Tag değişken koleksiyonlarında bulunamasa bile UI durumu doğru gösterir
        public static bool BoruSignalActive { get; set; } = false;
        public static bool TablaSignalActive { get; set; } = false;

        // ═══ TABLA LİMİT ALARM SİSTEMİ ═══
        // Tabla ölçüm sonuçlarında herhangi bir eksen TablaAlarmLimit'i aşarsa alarm aktif olur.
        // TABLA_LIMIT_ALARM output değişkeni GeneralOutputVars'ta oluşturulur → otomasyon sayfasında görünür.
        // Kullanıcı bu değişkeni PLC/sistem alarm tag'ine eşleştirir. Robot'a G_TABLA_LIMIT_ALARM yazılır.

        private static double _tablaAlarmLimit = 5.0; // mm — varsayılan eşik
        public static double TablaAlarmLimit
        {
            get => _tablaAlarmLimit;
            set
            {
                if (Math.Abs(_tablaAlarmLimit - value) < 0.0001) return;
                _tablaAlarmLimit = value;
                SaveAutomationSettings();
            }
        }

        /// <summary>Tabla kaçıklık alarmı aktif mi (herhangi bir eksen limiti aştı)</summary>
        public static bool TablaAlarmActive { get; private set; } = false;

        /// <summary>Alarm detay mesajı (hangi eksen, ne kadar aştı)</summary>
        public static string TablaAlarmMessage { get; private set; } = "";

        /// <summary>
        /// Tabla ölçüm sonuçlarını limit kontrolünden geçirir.
        /// Herhangi bir eksen |değer| > TablaAlarmLimit ise alarm aktif eder.
        /// TABLA_LIMIT_ALARM output değişkenini günceller (GeneralOutputVars).
        /// </summary>
        public static void CheckTablaAlarmLimits(List<GocatorMeasurement> measurements)
        {
            if (measurements == null || measurements.Count == 0)
            {
                ClearTablaLimitAlarm();
                return;
            }

            // Eksen isimleri (6 değer döngüsü: X, Y, Z, Roll, Pitch, Yaw)
            string[] axNames = { "X", "Y", "Z", "Roll", "Pitch", "Yaw" };
            double limit = TablaAlarmLimit;
            var exceeded = new List<string>();

            for (int i = 0; i < measurements.Count; i++)
            {
                double absVal = Math.Abs(measurements[i].Value);
                if (absVal > limit)
                {
                    string axName = axNames[i % 6];
                    exceeded.Add($"{axName}={measurements[i].Value:F3} (limit:{limit:F1})");
                }
            }

            if (exceeded.Count > 0)
            {
                TablaAlarmActive = true;
                TablaAlarmMessage = $"TABLA KAÇIKLIK ALARM: {string.Join(", ", exceeded)}";
                OnAutomationLog?.Invoke($"🚨 {TablaAlarmMessage}");

                // GeneralOutputVars'taki TABLA_LIMIT_ALARM değişkenini güncelle
                var alarmVar = GeneralOutputVars.FirstOrDefault(v => v.Name == "TABLA_LIMIT_ALARM");
                if (alarmVar != null) alarmVar.CurrentValue = true;

                // Robot'a da bildir
                _ = WriteToAllRobotsAsync("G_TABLA_LIMIT_ALARM", "TRUE");
            }
            else
            {
                ClearTablaLimitAlarm();
            }

            OnAutomationStatusChanged?.Invoke();
        }

        /// <summary>
        /// Tabla aktarım tablosundaki değerleri limit kontrolünden geçirir.
        /// Herhangi bir değer |value| > TablaAlarmLimit ise TABLA_LIMIT_ALARM aktif olur.
        /// </summary>
        public static void CheckTablaTransferLimits(ObservableCollection<PlcTransferItem> transferRows, List<object> values)
        {
            if (values == null || values.Count == 0)
            {
                ClearTablaLimitAlarm();
                return;
            }

            double limit = TablaAlarmLimit;
            var exceeded = new List<string>();

            for (int i = 0; i < values.Count; i++)
            {
                if (!double.TryParse(values[i]?.ToString(), out double val)) continue;
                double absVal = Math.Abs(val);
                if (absVal > limit)
                {
                    string tag = (i < transferRows.Count && !string.IsNullOrEmpty(transferRows[i].SelectedTag))
                        ? transferRows[i].SelectedTag
                        : $"Satır {i + 1}";
                    exceeded.Add($"{tag}={val:F3} (limit:{limit:F1})");
                }
            }

            if (exceeded.Count > 0)
            {
                TablaAlarmActive = true;
                TablaAlarmMessage = $"TABLA DEĞER LİMİT AŞIMI: {string.Join(", ", exceeded)}";
                OnAutomationLog?.Invoke($"🚨 {TablaAlarmMessage}");

                // TABLA_LIMIT_ALARM güncelle
                var alarmVar = GeneralOutputVars.FirstOrDefault(v => v.Name == "TABLA_LIMIT_ALARM");
                if (alarmVar != null) alarmVar.CurrentValue = true;

                _ = WriteToAllRobotsAsync("G_TABLA_LIMIT_ALARM", "TRUE");
            }
            else
            {
                ClearTablaLimitAlarm();
            }

            OnAutomationStatusChanged?.Invoke();
        }

        /// <summary>Tabla limit alarmını temizle</summary>
        public static void ClearTablaLimitAlarm()
        {
            TablaAlarmActive = false;
            TablaAlarmMessage = "";

            var alarmVar = GeneralOutputVars.FirstOrDefault(v => v.Name == "TABLA_LIMIT_ALARM");
            if (alarmVar != null) alarmVar.CurrentValue = false;

            _ = WriteToAllRobotsAsync("G_TABLA_LIMIT_ALARM", "FALSE");
        }

        // --- ROBOT BAĞLANTI AYARLARI ---
        private static string _robotIpAddress = "127.0.0.1";
        public static string Robot_IpAddress
        {
            get => _robotIpAddress;
            set
            {
                if (_robotIpAddress == value) return;
                _robotIpAddress = value;
                SaveAutomationSettings();
            }
        }

        private static int _robotPort = 7000;
        public static int Robot_Port
        {
            get => _robotPort;
            set
            {
                if (_robotPort == value) return;
                _robotPort = value;
                SaveAutomationSettings();
            }
        }

        // --- ROBOT HABERLEŞME HIZI (ms) ---
        private static int _robotReadSpeed = 100;
        public static int Robot_ReadSpeed
        {
            get => _robotReadSpeed;
            set
            {
                int clamped = Math.Max(50, Math.Min(500, value));
                if (_robotReadSpeed == clamped) return;
                _robotReadSpeed = clamped;
                SaveAutomationSettings();
            }
        }

        // --- PLC BAĞLANTI AYARLARI ---
        private static string _plcIpAddress = "192.168.251.100";
        public static string Plc_IpAddress
        {
            get => _plcIpAddress;
            set
            {
                if (_plcIpAddress == value) return;
                _plcIpAddress = value;
                SaveAutomationSettings();
            }
        }

        private static int _plcPort = 5007;
        public static int Plc_Port
        {
            get => _plcPort;
            set
            {
                if (_plcPort == value) return;
                _plcPort = value;
                SaveAutomationSettings();
            }
        }

        // --- GOCATOR BAĞLANTI AYARLARI ---
        private static string _gocatorIpAddress = "192.168.251.30";
        public static string Gocator_IpAddress
        {
            get => _gocatorIpAddress;
            set
            {
                if (_gocatorIpAddress == value) return;
                _gocatorIpAddress = value;
                SaveAutomationSettings();
            }
        }

        private static int _gocatorPort = 3600;
        public static int Gocator_Port
        {
            get => _gocatorPort;
            set
            {
                if (_gocatorPort == value) return;
                _gocatorPort = value;
                SaveAutomationSettings();
            }
        }

        // ═══════════════════════════════════════════════════════
        // HABERLEŞME ZAMANLAMA AYARLARI (ms)
        // ═══════════════════════════════════════════════════════

        // --- PLC Okuma Döngüsü (ms) ---
        private static int _plcReadInterval = 50;
        public static int Plc_ReadInterval
        {
            get => _plcReadInterval;
            set
            {
                int clamped = Math.Max(20, Math.Min(500, value));
                if (_plcReadInterval == clamped) return;
                _plcReadInterval = clamped;
                SaveAutomationSettings();
            }
        }

        // --- PLC Tetik İzleme Hızı (ms) ---
        private static int _triggerMonitorInterval = 500;
        public static int TriggerMonitor_Interval
        {
            get => _triggerMonitorInterval;
            set
            {
                int clamped = Math.Max(200, Math.Min(2000, value));
                if (_triggerMonitorInterval == clamped) return;
                _triggerMonitorInterval = clamped;
                SaveAutomationSettings();
            }
        }

        // --- Robot TCP Timeout (ms) ---
        private static int _robotTcpTimeout = 5000;
        public static int Robot_TcpTimeout
        {
            get => _robotTcpTimeout;
            set
            {
                int clamped = Math.Max(1000, Math.Min(30000, value));
                if (_robotTcpTimeout == clamped) return;
                _robotTcpTimeout = clamped;
                SaveAutomationSettings();
            }
        }

        // --- Gocator REST API Timeout (ms) ---
        private static int _gocatorRestTimeout = 30000;
        public static int Gocator_RestTimeout
        {
            get => _gocatorRestTimeout;
            set
            {
                int clamped = Math.Max(5000, Math.Min(120000, value));
                if (_gocatorRestTimeout == clamped) return;
                _gocatorRestTimeout = clamped;
                SaveAutomationSettings();
            }
        }

        // --- Inficon Panel Güncelleme Hızı (ms) ---
        private static int _inficonRefreshInterval = 200;
        public static int Inficon_RefreshInterval
        {
            get => _inficonRefreshInterval;
            set
            {
                int clamped = Math.Max(100, Math.Min(1000, value));
                if (_inficonRefreshInterval == clamped) return;
                _inficonRefreshInterval = clamped;
                SaveAutomationSettings();
            }
        }

        // --- Inficon Trend Grafik Güncelleme (ms) ---
        private static int _inficonTrendInterval = 1000;
        public static int Inficon_TrendInterval
        {
            get => _inficonTrendInterval;
            set
            {
                int clamped = Math.Max(500, Math.Min(5000, value));
                if (_inficonTrendInterval == clamped) return;
                _inficonTrendInterval = clamped;
                SaveAutomationSettings();
            }
        }

        // --- Güvenlik Sinyal Kontrol Hızı (ms) ---
        private static int _safetyCheckInterval = 1000;
        public static int Safety_CheckInterval
        {
            get => _safetyCheckInterval;
            set
            {
                int clamped = Math.Max(500, Math.Min(5000, value));
                if (_safetyCheckInterval == clamped) return;
                _safetyCheckInterval = clamped;
                SaveAutomationSettings();
            }
        }

        // EVENTLER VE DURUM
        public static event Action<string> OnAutomationLog;
        public static event Action OnAutomationStatusChanged;
        public static event Action OnEquipmentStatusChanged;

        private static string _processStatus = "HAZIR";
        public static string ProcessStatus { get => _processStatus; set { if (_processStatus != value) { _processStatus = value; OnAutomationStatusChanged?.Invoke(); } } }

        private static bool _isProcessRunning = false;
        public static bool IsProcessRunning { get => _isProcessRunning; set { if (_isProcessRunning != value) { _isProcessRunning = value; OnAutomationStatusChanged?.Invoke(); } } }

        // Cift tetik korumasi - RunAutomationSequence ve HandleOlcumTetikAsync cakismasini onler
        public static bool OlcumInProgress { get; set; } = false;

        // ═══ ÖLÇÜM DURUM BİLGİLERİ (Kamera sayfası status bar) ═══
        // Tabla: BASE 2 + TOOL 2 ile çekildi mi?
        // Boru: BASE 1 + TOOL 2 ile çekildi mi?
        public static string TablaOlcumDurum { get; set; } = "";
        public static bool TablaOlcumBasarili { get; set; } = false;
        public static string BoruOlcumDurum { get; set; } = "";
        public static bool BoruOlcumBasarili { get; set; } = false;

        // ═══════════════════════════════════════════════════════════════════════════
        // GLOBAL EKİPMAN DURUMLARI - Tüm uygulamadan erişilebilir
        // ═══════════════════════════════════════════════════════════════════════════
        private static bool _plcConnected;
        public static bool PlcConnected
        {
            get => _plcConnected;
            set { if (_plcConnected != value) { _plcConnected = value; OnEquipmentStatusChanged?.Invoke(); } }
        }

        private static bool _gocatorOnline;
        public static bool GocatorOnline
        {
            get => _gocatorOnline;
            set { if (_gocatorOnline != value) { _gocatorOnline = value; OnEquipmentStatusChanged?.Invoke(); } }
        }

        // ═══ VERİ KAYNAĞI SEÇİMİ (kalıcı) ═══
        // "SENSOR" = Ham Gocator, "CODESYS" = Matematik fonksiyon, "HAND_EYE" = Kalibrasyon dönüşüm
        private static string _dataSourceMode = "CODESYS";
        public static string DataSourceMode
        {
            get => _dataSourceMode;
            set { _dataSourceMode = value ?? "CODESYS"; SaveAutomationSettings(); }
        }

        // ═══ CODESYS Matematik Fonksiyonu Ayarları (kalıcı) ═══
        private static readonly string _codesysOffsetsFilePath = Path.Combine("C:\\Simbiosis\\Config", "Codesys_Offsets.json");
        private static double _codesysOffsetX = 0;
        public static double CodesysOffsetX
        {
            get => _codesysOffsetX;
            set { _codesysOffsetX = value; SaveAutomationSettings(); SaveCodesysOffsets(); }
        }

        private static double _codesysOffsetY = 0;
        public static double CodesysOffsetY
        {
            get => _codesysOffsetY;
            set { _codesysOffsetY = value; SaveAutomationSettings(); SaveCodesysOffsets(); }
        }

        private static double _codesysOffsetZ = 242.90;
        public static double CodesysOffsetZ
        {
            get => _codesysOffsetZ;
            set { _codesysOffsetZ = value; SaveAutomationSettings(); SaveCodesysOffsets(); }
        }

        // BORU/TABLA A/B/C dahil/hariç ayarları
        private static bool _boruAbcDahil = false;
        public static bool BoruAbcDahil
        {
            get => _boruAbcDahil;
            set { _boruAbcDahil = value; SaveCodesysOffsets(); }
        }

        private static bool _tablaAbcDahil = false;
        public static bool TablaAbcDahil
        {
            get => _tablaAbcDahil;
            set { _tablaAbcDahil = value; SaveCodesysOffsets(); }
        }

        /// <summary>
        /// CODESYS ofsetleri ve eşleştirmeleri JSON dosyasına kaydeder.
        /// Başka bilgisayara taşınabilir (C:\Simbiosis\Config\Codesys_Offsets.json)
        /// </summary>
        public static void SaveCodesysOffsets()
        {
            try
            {
                var data = new Dictionary<string, object>
                {
                    ["OffsetX"] = _codesysOffsetX,
                    ["OffsetY"] = _codesysOffsetY,
                    ["OffsetZ"] = _codesysOffsetZ,
                    ["GocMappings"] = _codesysGocMappings,
                    ["BoruAbcDahil"] = _boruAbcDahil,
                    ["TablaAbcDahil"] = _tablaAbcDahil
                };
                Directory.CreateDirectory(Path.GetDirectoryName(_codesysOffsetsFilePath));
                File.WriteAllText(_codesysOffsetsFilePath,
                    System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        /// <summary>
        /// CODESYS ofsetleri JSON dosyasından yükler (başka bilgisayardan kopyalanabilir)
        /// </summary>
        public static void LoadCodesysOffsets()
        {
            try
            {
                if (!File.Exists(_codesysOffsetsFilePath)) return;
                var json = File.ReadAllText(_codesysOffsetsFilePath);
                var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("OffsetX", out var ox)) _codesysOffsetX = ox.GetDouble();
                if (root.TryGetProperty("OffsetY", out var oy)) _codesysOffsetY = oy.GetDouble();
                if (root.TryGetProperty("OffsetZ", out var oz)) _codesysOffsetZ = oz.GetDouble();
                if (root.TryGetProperty("GocMappings", out var gm)) _codesysGocMappings = gm.GetString() ?? "0,1,2,3";
                if (root.TryGetProperty("BoruAbcDahil", out var ba)) _boruAbcDahil = ba.GetBoolean();
                if (root.TryGetProperty("TablaAbcDahil", out var ta)) _tablaAbcDahil = ta.GetBoolean();
            }
            catch { }
        }

        private static string _codesysGocMappings = "0,1,2,3";
        public static string CodesysGocMappings
        {
            get => _codesysGocMappings;
            set { _codesysGocMappings = value ?? "0,1,2,3"; SaveAutomationSettings(); SaveCodesysOffsets(); }
        }

        // Tool göreceli konum verileri (JSON formatında kalıcı kayıt)
        private static string _savedToolRelativeOffsets = "";
        public static string SavedToolRelativeOffsets
        {
            get => _savedToolRelativeOffsets;
            set
            {
                _savedToolRelativeOffsets = value ?? "";
                SaveAutomationSettings();
            }
        }

        // ═══ SAFETY ALARM ÇIKIŞ TAG'LERİ ═══
        // Alarm tablosundaki herhangi bir sinyal tetiklendiğinde bu tag'lere TRUE yazılır.
        private static string _safetyAlarmR1OutputTag = "";
        public static string SafetyAlarmR1OutputTag
        {
            get => _safetyAlarmR1OutputTag;
            set { if (_safetyAlarmR1OutputTag != value) { _safetyAlarmR1OutputTag = value ?? ""; SaveAutomationSettings(); } }
        }

        private static string _safetyAlarmR2OutputTag = "";
        public static string SafetyAlarmR2OutputTag
        {
            get => _safetyAlarmR2OutputTag;
            set { if (_safetyAlarmR2OutputTag != value) { _safetyAlarmR2OutputTag = value ?? ""; SaveAutomationSettings(); } }
        }

        private static int _robotConnectedCount;
        public static int RobotConnectedCount
        {
            get => _robotConnectedCount;
            set { if (_robotConnectedCount != value) { _robotConnectedCount = value; OnEquipmentStatusChanged?.Invoke(); } }
        }

        private static int _robotTotalCount;
        public static int RobotTotalCount
        {
            get => _robotTotalCount;
            set { if (_robotTotalCount != value) { _robotTotalCount = value; OnEquipmentStatusChanged?.Invoke(); } }
        }

        /// <summary>
        /// Tüm ekipman durumlarını günceller. Timer'dan veya bağlantı değişikliklerinde çağrılır.
        /// </summary>
        public static void RefreshEquipmentStatus()
        {
            // PLC
            PlcConnected = PlcService.Instance?.IsConnected ?? false;

            // Robot (KukaRobotManager)
            var robots = KukaRobotManager.Instance?.Robots;
            if (robots != null)
            {
                RobotTotalCount = robots.Count;
                RobotConnectedCount = robots.Count(r => r.IsConnected);
            }

            // Gocator - Son başarılı işleme göre kontrol
            // (Gocator kalıcı bağlantı tutmuyor, her işlemde bağlanıp kapanıyor)
        }

        // --- BAŞLATMA ---
        public static void Initialize()
        {
            if (_isInitialized) return;
            LoadRfids();
            LoadCasingTypes();
            LoadCodesysOffsets();
            InitializeStations();
            LoadStationStates();
            InitializeVariables();
            // NOT: LoadPlcVariableTagsFromFile() artık InitializeVariables() içinde çağrılıyor.
            // İkinci kez çağrılırsa dosyadan RB1/RB2 gibi kaldırılmış değişkenleri geri yükler.
            LoadSystemChecks();
            LoadSafetyAlarms();
            LoadSafetyWarnings();
            LoadMeasurements();
            LoadTransformedMeasurements();
            LoadTransferRows();
            LoadTablaMeasurements();
            LoadTablaTransferRows();
            LoadTablaReferences();
            LoadInficonLogs();
            LoadRobotSliderMappings(); // Robot sinyal eşleştirmelerini yükle
            LoadKL100StationPositions(); // KL100 istasyon pozisyon verilerini yükle
            SyncStationPosToRobots();   // İstasyon E1 pozisyonlarını robotlara yaz

            // Modelleri Yükle
            RefreshAvailableModels();

            LoadAutomationSettings();
            StartAutomationListener();
            _isInitialized = true;

        }

        // ═══════════════════════════════════════════════════════════════════════════
        // ROBOT SLİDER EŞLEŞTİRME KAYIT/YÜKLEME
        // ═══════════════════════════════════════════════════════════════════════════
        public static void SaveRobotSliderMappings()
        {
            try
            {
                var data = new
                {
                    Robot1 = Robot1SliderMapping.ToSaveData(),
                    Robot2 = Robot2SliderMapping.ToSaveData(),
                    SliderSourceRobotIndex = SliderSourceRobotIndex,
                    SliderSourceSignalName = SliderSourceSignalName ?? "E1",
                    SliderActualPosSignalName = SliderActualPosSignalName ?? "E1"
                };
                string dir = Path.GetDirectoryName(_robotSliderMappingFilePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(_robotSliderMappingFilePath, JsonConvert.SerializeObject(data, Formatting.Indented));
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"SaveRobotSliderMappings Error: {ex.Message}"); }
        }

        public static void LoadRobotSliderMappings()
        {
            try
            {
                if (!File.Exists(_robotSliderMappingFilePath)) return;
                var json = File.ReadAllText(_robotSliderMappingFilePath);
                var jObj = Newtonsoft.Json.Linq.JObject.Parse(json);

                if (jObj["Robot1"] != null)
                    Robot1SliderMapping.LoadFromData(jObj["Robot1"].ToObject<RobotSliderMappingData>());
                if (jObj["Robot2"] != null)
                    Robot2SliderMapping.LoadFromData(jObj["Robot2"].ToObject<RobotSliderMappingData>());

                if (jObj["SliderSourceRobotIndex"] != null)
                    _sliderSourceRobotIndex = jObj["SliderSourceRobotIndex"].Value<int>();
                if (jObj["SliderSourceSignalName"] != null)
                    _sliderSourceSignalName = jObj["SliderSourceSignalName"].Value<string>() ?? "E1";
                if (jObj["SliderActualPosSignalName"] != null)
                    _sliderActualPosSignalName = jObj["SliderActualPosSignalName"].Value<string>() ?? "E1";
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"LoadRobotSliderMappings Error: {ex.Message}"); }
        }

        /// <summary>
        /// Robotun Input/Output sinyallerinden seçilebilir liste döndürür
        /// </summary>
        public static List<string> GetAvailableRobotSignals(int robotIndex = 0)
        {
            var signals = new List<string> { "" }; // Boş seçenek

            // Robot property'leri (sabit sinyaller)
            signals.AddRange(new[] {
                "E1", "E2", "E3", "E4", "E5", "E6",
                "PosX", "PosY", "PosZ", "PosA", "PosB", "PosC",
                "A1", "A2", "A3", "A4", "A5", "A6",
                "OverridePro", "OverrideJog",
                "OperationMode", "ProgramState",
                "ToolNo", "BaseNo"
            });

            // Robot'un InputVars ve OutputVars'dan dinamik sinyaller
            var robots = KukaRobotManager.Instance?.Robots;
            if (robots != null && robots.Count > robotIndex)
            {
                var robot = robots[robotIndex];
                foreach (var v in robot.InputVars)
                {
                    if (!string.IsNullOrEmpty(v.Name) && !signals.Contains(v.Name))
                        signals.Add(v.Name);
                }
                foreach (var v in robot.OutputVars)
                {
                    if (!string.IsNullOrEmpty(v.Name) && !signals.Contains(v.Name))
                        signals.Add(v.Name);
                }
            }

            return signals;
        }

        public static void RefreshAvailableModels()
        {
            try
            {
                AvailableModels.Clear();
                string modelsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utilities", "Models");
                if (Directory.Exists(modelsFolder))
                {
                    var files = Directory.GetFiles(modelsFolder, "*.glb");
                    foreach (var file in files)
                    {
                        AvailableModels.Add(Path.GetFileName(file));
                    }
                }
            }
            catch { }
        }

        // --- METOTLAR ---
        public static void SaveSystemChecks() { try { string json = System.Text.Json.JsonSerializer.Serialize(SystemCheckList, new JsonSerializerOptions { WriteIndented = true }); File.WriteAllText(_systemChecksFilePath, json); } catch { } }
        private static void LoadSystemChecks() { try { if (File.Exists(_systemChecksFilePath)) { var list = System.Text.Json.JsonSerializer.Deserialize<List<SystemCheckItem>>(File.ReadAllText(_systemChecksFilePath)); if (list != null) foreach (var item in list) SystemCheckList.Add(item); } } catch { } }

        public static void SaveSafetyAlarms() { try { string json = System.Text.Json.JsonSerializer.Serialize(SafetyAlarmList, new JsonSerializerOptions { WriteIndented = true }); File.WriteAllText(_safetyAlarmsFilePath, json); } catch { } }
        private static void LoadSafetyAlarms() { try { if (File.Exists(_safetyAlarmsFilePath)) { var list = System.Text.Json.JsonSerializer.Deserialize<List<SafetyCheckItem>>(File.ReadAllText(_safetyAlarmsFilePath)); if (list != null) foreach (var item in list) SafetyAlarmList.Add(item); } } catch { } }

        public static void SaveSafetyWarnings() { try { string json = System.Text.Json.JsonSerializer.Serialize(SafetyWarningList, new JsonSerializerOptions { WriteIndented = true }); File.WriteAllText(_safetyWarningsFilePath, json); } catch { } }
        private static void LoadSafetyWarnings() { try { if (File.Exists(_safetyWarningsFilePath)) { var list = System.Text.Json.JsonSerializer.Deserialize<List<SafetyCheckItem>>(File.ReadAllText(_safetyWarningsFilePath)); if (list != null) foreach (var item in list) SafetyWarningList.Add(item); } } catch { } }

        // --- INFICON KAÇAK LOG KAYIT ---
        public static void SaveInficonLogs() { try { string json = System.Text.Json.JsonSerializer.Serialize(InficonLeakLogs, new JsonSerializerOptions { WriteIndented = true }); File.WriteAllText(_inficonLogsFilePath, json); } catch { } }
        public static void LoadInficonLogs() { try { if (File.Exists(_inficonLogsFilePath)) { var list = System.Text.Json.JsonSerializer.Deserialize<List<App4.Models.InficonLeakLogEntry>>(File.ReadAllText(_inficonLogsFilePath)); if (list != null) { InficonLeakLogs.Clear(); foreach (var item in list) InficonLeakLogs.Add(item); } } } catch { } }

        public static void SaveMeasurements() { try { string json = System.Text.Json.JsonSerializer.Serialize(LastMeasurements, new JsonSerializerOptions { WriteIndented = true }); File.WriteAllText(_measurementsFilePath, json); } catch { } }
        private static void LoadMeasurements() { try { if (File.Exists(_measurementsFilePath)) { var list = System.Text.Json.JsonSerializer.Deserialize<List<GocatorMeasurement>>(File.ReadAllText(_measurementsFilePath)); if (list != null) { LastMeasurements.Clear(); foreach (var item in list) LastMeasurements.Add(item); } } } catch { } }

        // --- TABLA KAÇIKLIK ÖLÇÜM KAYIT ---
        private static readonly string _tablaMeasurementsFilePath = Path.Combine(ConfigBaseDir, "Saved_Tabla_Measurements.json");
        private static readonly string _tablaTransferFilePath = Path.Combine(ConfigBaseDir, "Tabla_Transfer_Rows.json");

        // --- TABLA REFERANS NOKTALARI (Case bazlı) ---
        private static readonly string _tablaReferencesFilePath = Path.Combine(ConfigBaseDir, "Tabla_References.json");
        public static Dictionary<int, TablaReference> TablaReferencePoints { get; private set; } = new();

        public static void SaveTablaReferences()
        {
            try
            {
                string json = System.Text.Json.JsonSerializer.Serialize(TablaReferencePoints.Values.ToList(), new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_tablaReferencesFilePath, json);
            }
            catch { }
        }

        public static void LoadTablaReferences()
        {
            try
            {
                if (File.Exists(_tablaReferencesFilePath))
                {
                    var list = System.Text.Json.JsonSerializer.Deserialize<List<TablaReference>>(File.ReadAllText(_tablaReferencesFilePath));
                    if (list != null)
                    {
                        TablaReferencePoints.Clear();
                        foreach (var r in list)
                            TablaReferencePoints[r.CasingIndex] = r;
                    }
                }
            }
            catch { }
        }

        public static TablaReference GetTablaReference(int casingIndex)
        {
            return TablaReferencePoints.TryGetValue(casingIndex, out var r) ? r : null;
        }

        public static void SetTablaReference(int casingIndex, double x, double y, double z, double a, double b, double c)
        {
            TablaReferencePoints[casingIndex] = new TablaReference
            {
                CasingIndex = casingIndex,
                X = x, Y = y, Z = z, A = a, B = b, C = c,
                HasReference = true,
                DateTaken = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            };
            SaveTablaReferences();
        }

        public static void SaveTablaMeasurements() { try { string json = System.Text.Json.JsonSerializer.Serialize(TablaLastMeasurements, new JsonSerializerOptions { WriteIndented = true }); File.WriteAllText(_tablaMeasurementsFilePath, json); } catch { } }
        private static void LoadTablaMeasurements() { try { if (File.Exists(_tablaMeasurementsFilePath)) { var list = System.Text.Json.JsonSerializer.Deserialize<List<GocatorMeasurement>>(File.ReadAllText(_tablaMeasurementsFilePath)); if (list != null) { TablaLastMeasurements.Clear(); foreach (var item in list) TablaLastMeasurements.Add(item); } } } catch { } }

        public static void SaveTransformedMeasurements() { try { string json = System.Text.Json.JsonSerializer.Serialize(TransformedMeasurements, new JsonSerializerOptions { WriteIndented = true }); File.WriteAllText(_transformedMeasurementsFilePath, json); } catch { } }
        private static void LoadTransformedMeasurements() { try { if (File.Exists(_transformedMeasurementsFilePath)) { var list = System.Text.Json.JsonSerializer.Deserialize<List<GocatorMeasurement>>(File.ReadAllText(_transformedMeasurementsFilePath)); if (list != null) { TransformedMeasurements.Clear(); foreach (var item in list) TransformedMeasurements.Add(item); } } } catch { } }

        /// <summary>
        /// Hand-Eye donusum sonuclarini TransformedMeasurements koleksiyonuna yazar.
        /// </summary>
        public static void PopulateTransformedMeasurements(KukaPose basePose)
        {
            TransformedMeasurements.Clear();
            string[] names = { "Base X", "Base Y", "Base Z", "Base A", "Base B", "Base C" };
            string[] units = { "mm", "mm", "mm", "deg", "deg", "deg" };
            double[] vals = { basePose.X, basePose.Y, basePose.Z, basePose.A, basePose.B, basePose.C };
            for (int i = 0; i < 6; i++)
            {
                TransformedMeasurements.Add(new GocatorMeasurement
                {
                    Id = i + 1,
                    PointIndex = 0,
                    SourceId = i,
                    IsFirstInPoint = (i == 0),
                    Name = names[i],
                    Value = vals[i],
                    Unit = units[i],
                    Decision = "OK"
                });
            }
            SaveTransformedMeasurements();
        }

        public static void SaveTablaTransferRows()
        {
            try
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(TablaTransferRows, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(_tablaTransferFilePath, json);
            }
            catch { }
        }
        private static void LoadTablaTransferRows()
        {
            try
            {
                if (!File.Exists(_tablaTransferFilePath)) return;
                string json = File.ReadAllText(_tablaTransferFilePath);
                var settings = new Newtonsoft.Json.JsonSerializerSettings { MissingMemberHandling = Newtonsoft.Json.MissingMemberHandling.Ignore };
                var items = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PlcTransferItem>>(json, settings);
                if (items != null)
                {
                    TablaTransferRows.Clear();
                    foreach (var item in items)
                    {
                        item.PropertyChanged += (s, e) => { if (e.PropertyName == "SelectedTag") SaveTablaTransferRows(); };
                        TablaTransferRows.Add(item);
                    }
                }
                TablaTransferRows.CollectionChanged += (s, e) => SaveTablaTransferRows();
            }
            catch { }
        }

        private static void LoadRfids() 
        { 
            try 
            { 
                if (File.Exists(_rfidFilePath)) 
                { 
                    var list = JsonConvert.DeserializeObject<List<RfidDef>>(File.ReadAllText(_rfidFilePath));
                    if (list != null) foreach (var item in list) KnownRfids.Add(item); 
                } 
                else 
                { 
                    KnownRfids.Add(new RfidDef { Id = "RF123", Description = "Klima A Tipi" }); 
                    SaveRfids(); 
                }

                // 1. YENİLİK: Uygulama ilk açıldığında yüklenen mevcut kayıtların Index değerlerini sırayla ata
                for (int i = 0; i < KnownRfids.Count; i++)
                {
                    KnownRfids[i].IndexDisplay = i + 1;
                }

                // Mevcut öğeleri dinle
                foreach (var item in KnownRfids) item.PropertyChanged += RfidDef_PropertyChanged;

                KnownRfids.CollectionChanged += (s, e) =>
                {
                    if (e.NewItems != null) foreach (RfidDef item in e.NewItems) item.PropertyChanged += RfidDef_PropertyChanged;
                    if (e.OldItems != null) foreach (RfidDef item in e.OldItems) item.PropertyChanged -= RfidDef_PropertyChanged;
                    // Index numaralarını yeniden ata (ekleme/silme sonrası)
                    RefreshRfidIndexes();
                    SaveRfids();
                }; 
            } 
            catch { } 
        }

        /// <summary>RFID listesindeki index numaralarını yeniden atar (1-based)</summary>
        public static void RefreshRfidIndexes()
        {
            for (int i = 0; i < KnownRfids.Count; i++)
            {
                KnownRfids[i].IndexDisplay = i + 1;
            }
        }

        private static void RfidDef_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Kalıcı alanlardan herhangi biri değiştiğinde kaydet
            if (e.PropertyName == nameof(RfidDef.ModelFileName) ||
                e.PropertyName == nameof(RfidDef.JobSequence) ||
                e.PropertyName == nameof(RfidDef.SnifferDurations) ||
                e.PropertyName == nameof(RfidDef.DeviationLimits) ||
                e.PropertyName == nameof(RfidDef.Description))
                SaveRfids();
        }

        public static void SaveRfids() { try { File.WriteAllText(_rfidFilePath, JsonConvert.SerializeObject(KnownRfids, Formatting.Indented)); } catch { } }

        // --- CASING TİPLERİ ---
        public static void LoadCasingTypes()
        {
            try
            {
                if (File.Exists(_casingTypesFilePath))
                {
                    var list = JsonConvert.DeserializeObject<List<CasingType>>(File.ReadAllText(_casingTypesFilePath));
                    if (list != null) { CasingTypes.Clear(); foreach (var c in list) CasingTypes.Add(c); }
                }
                else
                {
                    // Varsayilan 4 kasa tipi
                    CasingTypes.Clear();
                    CasingTypes.Add(new CasingType { Index = 1, Name = "ALPHA" });
                    CasingTypes.Add(new CasingType { Index = 2, Name = "SF2" });
                    CasingTypes.Add(new CasingType { Index = 3, Name = "BML-H New PCB" });
                    CasingTypes.Add(new CasingType { Index = 4, Name = "BMS" });
                    SaveCasingTypes();
                }
            }
            catch { }
        }

        public static void SaveCasingTypes()
        {
            try { File.WriteAllText(_casingTypesFilePath, JsonConvert.SerializeObject(CasingTypes.ToList(), Formatting.Indented)); } catch { }
        }

        private static void InitializeStations()
        {
            Stations.Add(new ExtendedStationViewModel { Name = "İSTASYON 1", Description = "Klima Dış Ünite", Mode = StationMode.Manual, StatusTag = "ST1_STATUS", AlarmTag = "ST1_ALARM", ProducingTag = "ST1_PRODUCING", ProductionCountTag = "ST1_PROD_COUNT", EfficiencyTag = "ST1_EFFICIENCY", CurrentRfidTag = "ST1_RFID_ACT", AllowedRfid = "RF123", CurrentRfid = "RF123", RfidOpMode = RfidOperationMode.Mixed });
            Stations.Add(new ExtendedStationViewModel { Name = "İSTASYON 2", Description = "Klima Dış Ünite", Mode = StationMode.Manual, StatusTag = "ST2_STATUS", AlarmTag = "ST2_ALARM", ProducingTag = "ST2_PRODUCING", ProductionCountTag = "ST2_PROD_COUNT", EfficiencyTag = "ST2_EFFICIENCY", CurrentRfidTag = "ST2_RFID_ACT", AllowedRfid = "RF123", CurrentRfid = "RF123", RfidOpMode = RfidOperationMode.Mixed });
            Stations.Add(new ExtendedStationViewModel { Name = "İSTASYON 3", Description = "Boş İstasyon", Mode = StationMode.Manual, StatusTag = "ST3_STATUS", AlarmTag = "ST3_ALARM", ProducingTag = "ST3_PRODUCING", ProductionCountTag = "ST3_PROD_COUNT", EfficiencyTag = "ST3_EFFICIENCY", CurrentRfidTag = "ST3_RFID_ACT", AllowedRfid = "RF123", RfidOpMode = RfidOperationMode.Mixed });
        }
        public static void SaveStationStates() { try { var states = Stations.Select(s => { var ext = s as ExtendedStationViewModel; return new { Name = s.Name, Mode = (int)s.Mode, RfidOpMode = ext != null ? (int)ext.RfidOpMode : 0, TargetRfid = ext != null ? ext.TargetRfid : "" }; }).ToList(); File.WriteAllText(_stationStateFilePath, System.Text.Json.JsonSerializer.Serialize(states, new JsonSerializerOptions { WriteIndented = true })); } catch { } }
        private static void LoadStationStates() { try { if (File.Exists(_stationStateFilePath)) { var savedStates = System.Text.Json.JsonSerializer.Deserialize<List<JsonElement>>(File.ReadAllText(_stationStateFilePath)); foreach (var item in savedStates) { if (item.TryGetProperty("Name", out var nameProp)) { var station = Stations.FirstOrDefault(s => s.Name == nameProp.GetString()); if (station != null) { if (item.TryGetProperty("Mode", out var modeProp)) station.Mode = (StationMode)modeProp.GetInt32(); if (station is ExtendedStationViewModel ext) { if (item.TryGetProperty("RfidOpMode", out var rfid)) ext.RfidOpMode = (RfidOperationMode)rfid.GetInt32(); if (item.TryGetProperty("TargetRfid", out var target)) ext.TargetRfid = target.GetString(); } } } } } } catch { } }

        private static void InitializeVariables()
        {
            PlcVariable Create(string name, string type, string dir, object val) => new PlcVariable { Name = name, Type = type, Direction = dir, CurrentValue = val, IsEditable = true };
            // DÜZELTİLDİ: ST{id}_ALARM başlangıç değeri true (fail-safe: 1=normal, 0=alarm)
            // Böylece PLC bağlanmadan önce istasyonlar "alarm yok" durumunda başlar
            void AddVars(ObservableCollection<PlcVariable> c, int id) { c.Add(Create($"ST{id}_STATUS", "STRING", "Input", "Unknown")); c.Add(Create($"ST{id}_ALARM", "BOOL", "Input", true)); c.Add(Create($"ST{id}_MODE", "STRING", "Input", "Manual")); c.Add(Create($"ST{id}_PRODUCING", "BOOL", "Input", false)); c.Add(Create($"ST{id}_PROD_COUNT", "WORD", "Input", "0")); c.Add(Create($"ST{id}_EFFICIENCY", "WORD", "Input", "0")); c.Add(Create($"ST{id}_RFID_ACT", "STRING", "Input", "")); c.Add(Create($"ST{id}_YENI_URUN", "BOOL", "Input", false)); c.Add(Create($"ST{id}_ISLEM_BITTI", "BOOL", "Input", false)); }
            void AddOutputs(ObservableCollection<PlcVariable> c, int id) { c.Add(new PlcVariable { Name = $"ST{id}_RFID_MODE", Value = "0", Description = "RFID Mod", PlcTag = $"DB10.W{(id - 1) * 20}.0" }); c.Add(new PlcVariable { Name = $"ST{id}_RFID_TARGET", Value = "", Type="STRING", Description = "Hedef RFID", PlcTag = $"DB10.S{(id - 1) * 20}.4" }); c.Add(new PlcVariable { Name = $"ST{id}_ID_MATCHED", Value = "0", Description = "ID Eşleşti", PlcTag = $"DB10.W{(id - 1) * 20}.20" }); c.Add(new PlcVariable { Name = $"ST{id}_PROCESS_RESULT", Value = "0", Description = "Sonuç", PlcTag = $"DB10.W{(id - 1) * 20}.22" }); c.Add(new PlcVariable { Name = $"ST{id}_CONVEYOR_PERM", Value = "0", Description = "Konveyör", PlcTag = $"DB10.W{(id - 1) * 20}.24" }); c.Add(new PlcVariable { Name = $"ST{id}_MODE_CMD", Value = "1", Description = "Mod Cmd", PlcTag = $"DB10.W{(id - 1) * 20}.26" }); }
            // ═══════════════════════════════════════════════════════════════════════════
            // GeneralInputVars / GeneralOutputVars: PLC ↔ PC (Robot DEĞİL)
            // Bu koleksiyonlar SADECE PLC/genel sistem sinyalleri içerir.
            // Robot sinyalleri (G_ prefix) burada OLMAMALIDIR.
            // Otomasyon sayfasındaki "Genel Değişkenler" tablosunda görünür.
            // ═══════════════════════════════════════════════════════════════════════════
            GeneralInputVars.Add(Create("SLIDER_POS_ACT", "WORD", "Input", "0"));
            GeneralInputVars.Add(Create("ROBOT_SPEED", "WORD", "Input", "100"));
            GeneralInputVars.Add(Create("GOCATOR_STATUS", "STRING", "Input", "READY"));
            GeneralInputVars.Add(Create("SAFETY_OK", "BOOL", "Input", true));
            GeneralInputVars.Add(Create("LINE_RUNNING", "BOOL", "Input", false));
            GeneralInputVars.Add(Create("LINE_AUTO_MODE", "BOOL", "Input", false));
            GeneralInputVars.Add(Create("SYS_RESET_FEEDBACK", "BOOL", "Input", false));
            // ▼▼▼ VARSAYILAN DEĞİŞKENLERİ HER ZAMAN EKLE (yeni eklenenler kayıp olmasın) ▼▼▼
            // ▼▼▼ KAMERA ÖLÇÜM SİNYALİ - Yeni ölçüm geldiğinde 1, sıfırlandığında 0 ▼▼▼
            GeneralInputVars.Add(Create("MEASUREMENT_NEW_DATA", "BOOL", "Input", false));
            // ▼▼▼ AKTÜEL İSTASYON VE KLİMA INDEX ▼▼▼
            GeneralInputVars.Add(Create("AKTUEL_ISTASYON", "WORD", "Input", "0"));           // Robotun aktüel olduğu istasyon numarası
            GeneralInputVars.Add(Create("ROBOT_HOME", "BOOL", "Input", false));              // Robot HOME pozisyonunda mı
            GeneralOutputVars.Add(Create("CMD_LINE_START", "BOOL", "Output", false));
            GeneralOutputVars.Add(Create("CMD_LINE_STOP", "BOOL", "Output", false));
            GeneralOutputVars.Add(Create("CMD_LINE_RESET", "BOOL", "Output", false));
            // ▼▼▼ KAMERA ÖLÇÜM OUTPUT - Manuel başla butonuyla sıfırlanır ▼▼▼
            GeneralOutputVars.Add(Create("MEASUREMENT_TRIGGER_OUT", "BOOL", "Output", false));
            // ▼▼▼ MAKİNA OTO/MANUEL SWITCH ▼▼▼
            GeneralOutputVars.Add(Create("LINE_AUTO_MANUAL_CMD", "BOOL", "Output", false));  // true=Oto, false=Manuel - Tüm hat oto/manuel switch
            // ▼▼▼ AKTÜEL KLİMA INDEX VE RFID ▼▼▼
            GeneralOutputVars.Add(Create("AKTUEL_KLIMA_INDEX", "WORD", "Output", "0"));      // Aktüel klima tipi indexi (KnownRfids sıra no)
            GeneralOutputVars.Add(Create("AKTUEL_CASE_ID", "INT", "Output", "0"));           // Aktüel kasa tipi indexi (1=ALPHA, 2=SF2, 3=BML-H, 4=BMS)
            GeneralOutputVars.Add(Create("AKTUEL_RFID", "STRING", "Output", ""));            // Aktüel RFID Id string değeri
            GeneralOutputVars.Add(Create("SNIFFER_OLCUM_SURE", "REAL", "Output", "0"));       // Seçili index'in sniffer ölçüm süresi (ms)
            GeneralOutputVars.Add(Create("NOKTA_SAPMA_LIMIT", "REAL", "Output", "0"));       // Seçili index'in nokta sapma limiti (mm)
            // ▼▼▼ TABLA LİMİT ALARM ▼▼▼
            GeneralOutputVars.Add(Create("TABLA_LIMIT_ALARM", "BOOL", "Output", false));   // Tabla kaçıklık/değer limiti aşıldığında TRUE → G_TABLA_LIMIT_ALARM
            GeneralOutputVars.Add(Create("SAFETY_OK", "BOOL", "Output", false));               // Safety tablosu sonucu: tum alarmlar temizse TRUE
            // KL100_HEDEF_ISTASYON should be an Input (PLC -> PC): robot/PLC writes target station
            GeneralInputVars.Add(Create("KL100_HEDEF_ISTASYON", "WORD", "Input", "0"));    // KL100 slider hedef istasyon numarası
            // KL100_HEDEF_POZ kaldırıldı - slider pozisyonu doğrudan Robot 2'ye yazılıyor (G_SLIDER_HEDEF_POZ)
            GeneralOutputVars.Add(Create("KL100_HEDEF_GIT", "BOOL", "Output", false));       // KL100 hedef istasyona git komutu
            // ▼▼▼ SNİFFER TETİK SİNYALLERİ (Robot → PC, görselleştirme için) ▼▼▼
            GeneralInputVars.Add(Create("R1_SNIFFER_TETIK", "BOOL", "Input", false));          // Robot 1 sniffer tetik sinyali (slider görselleştirme)
            GeneralInputVars.Add(Create("R2_SNIFFER_TETIK", "BOOL", "Input", false));          // Robot 2 sniffer tetik sinyali (slider görselleştirme)
            // ▼▼▼ ROBOT GİT KOMUTLARI (PLC → PC) ▼▼▼
            GeneralInputVars.Add(Create("FIRST_ROBOT_GO", "BOOL", "Input", false));           // PLC Robot 1 başlat komutu
            GeneralInputVars.Add(Create("SECOND_ROBOT_GO", "BOOL", "Input", false));          // PLC Robot 2 başlat komutu
            // NOT: RB1/RB2 robot durum değişkenleri burada değil, PlcService.EnsureRobotBridgeVariables() içinde
            // ve Robot sayfasının kendi değişken koleksiyonlarında yönetilir.
            AddVars(Station1Vars, 1); AddVars(Station2Vars, 2); AddVars(Station3Vars, 3); AddOutputs(Station1Outputs, 1); AddOutputs(Station2Outputs, 2); AddOutputs(Station3Outputs, 3);

            // ▼▼▼ Kaydedilmiş ayarlar varsa, PlcTag ve Value bilgilerini üzerine yaz (merge) ▼▼▼
            if (File.Exists(_autoPageVariablesFilePath))
            {
                try
                {
                    LoadPlcVariableTagsFromFile();
                }
                catch { }
            }

            // ▼▼▼ INFICON P3000XL SNIFFER DEĞİŞKENLERİ (PLC ↔ Sniffer) ▼▼▼
            // Inficon 1 (Robot 1) — INPUT (PLC'den okunan)
            GeneralInputVars.Add(Create("INFICON1_READY", "BOOL", "Input", false));
            GeneralInputVars.Add(Create("INFICON1_STABLE", "BOOL", "Input", false));
            GeneralInputVars.Add(Create("INFICON1_LEAK", "BOOL", "Input", false));
            GeneralInputVars.Add(Create("INFICON1_ERROR", "BOOL", "Input", false));
            GeneralInputVars.Add(Create("INFICON1_LEAKRATE", "REAL", "Input", "0.0"));
            GeneralInputVars.Add(Create("INFICON1_PE", "REAL", "Input", "0.0"));
            GeneralInputVars.Add(Create("INFICON1_FLOW", "REAL", "Input", "0.0"));
            // Inficon 1 (Robot 1) — OUTPUT (PLC'ye yazılan)
            GeneralOutputVars.Add(Create("INFICON1_START", "BOOL", "Output", false));
            GeneralOutputVars.Add(Create("INFICON1_CAL", "BOOL", "Output", false));
            GeneralOutputVars.Add(Create("INFICON1_CAL_ABORT", "BOOL", "Output", false));
            GeneralOutputVars.Add(Create("INFICON1_ZERO", "BOOL", "Output", false));
            GeneralOutputVars.Add(Create("INFICON1_ERRCLEAR", "BOOL", "Output", false));
            GeneralOutputVars.Add(Create("INFICON1_STANDBY", "BOOL", "Output", false));
            GeneralOutputVars.Add(Create("INFICON1_RESET", "BOOL", "Output", false));
            GeneralOutputVars.Add(Create("INFICON1_ENABLE", "BOOL", "Output", false));
            // Inficon 2 (Robot 2) — INPUT (PLC'den okunan)
            GeneralInputVars.Add(Create("INFICON2_READY", "BOOL", "Input", false));
            GeneralInputVars.Add(Create("INFICON2_STABLE", "BOOL", "Input", false));
            GeneralInputVars.Add(Create("INFICON2_LEAK", "BOOL", "Input", false));
            GeneralInputVars.Add(Create("INFICON2_ERROR", "BOOL", "Input", false));
            GeneralInputVars.Add(Create("INFICON2_LEAKRATE", "REAL", "Input", "0.0"));
            GeneralInputVars.Add(Create("INFICON2_PE", "REAL", "Input", "0.0"));
            GeneralInputVars.Add(Create("INFICON2_FLOW", "REAL", "Input", "0.0"));
            // Inficon 2 (Robot 2) — OUTPUT (PLC'ye yazılan)
            GeneralOutputVars.Add(Create("INFICON2_START", "BOOL", "Output", false));
            GeneralOutputVars.Add(Create("INFICON2_CAL", "BOOL", "Output", false));
            GeneralOutputVars.Add(Create("INFICON2_CAL_ABORT", "BOOL", "Output", false));
            GeneralOutputVars.Add(Create("INFICON2_ZERO", "BOOL", "Output", false));
            GeneralOutputVars.Add(Create("INFICON2_ERRCLEAR", "BOOL", "Output", false));
            GeneralOutputVars.Add(Create("INFICON2_STANDBY", "BOOL", "Output", false));
            GeneralOutputVars.Add(Create("INFICON2_RESET", "BOOL", "Output", false));
            GeneralOutputVars.Add(Create("INFICON2_ENABLE", "BOOL", "Output", false));

            // ▼▼▼ Temizlik: Eski dosyada kalmış RB1/RB2 robot değişkenlerini GeneralOutputVars'tan kaldır ▼▼▼
            // (Bu değişkenler artık sadece PlcService.EnsureRobotBridgeVariables'da yönetiliyor)
            var robotVarsToRemove = GeneralOutputVars.Where(v => v.Name.StartsWith("RB1_") || v.Name.StartsWith("RB2_")).ToList();
            foreach (var rv in robotVarsToRemove) GeneralOutputVars.Remove(rv);

            // ▼▼▼ Temizlik: Kaldırılan R1/R2_ROBOT_SAFETY değişkenlerini GeneralInputVars'tan da temizle ▼▼▼
            var safetyVarsToRemove = GeneralInputVars.Where(v => v.Name == "R1_ROBOT_SAFETY" || v.Name == "R2_ROBOT_SAFETY").ToList();
            foreach (var sv in safetyVarsToRemove) GeneralInputVars.Remove(sv);

            // ▼▼▼ Temizlik: Eski TABLA_KACIKLIK_ALARM ve TABLA_DEGER_LIMIT_ASIMI kaldırıldı → tek TABLA_LIMIT_ALARM ▼▼▼
            var oldTablaVars = GeneralOutputVars.Where(v => v.Name == "TABLA_KACIKLIK_ALARM" || v.Name == "TABLA_DEGER_LIMIT_ASIMI").ToList();
            foreach (var tv in oldTablaVars) GeneralOutputVars.Remove(tv);
            
            // ═══════════════════════════════════════════════════════════════════════════
            // RobotInputVars: Robot → PC (Robot'tan OKUNAN değerler)
            // $CONFIG.dat USER GLOBALS ile birebir eşleşir
            // ─────────────────────────────────────────────────────────────────────────
            // SİNYAL YÖN HARİTASI:
            //   • Salt Input (Robot→PC): G_ROBOT_DURUM, G_IS_BITTI, G_HATA_*, G_AKTIF_NOKTA,
            //     G_TOPLAM_NOKTA, G_NOK_*, G_JOB_INDEX, G_OLCUM_TETIK, G_DURUM_MESAJ,
            //     G_R1/R2_HOME, G_SNIFFER_OLCUM_*, G_AKTIF_CIZGI, G_TOPLAM_CIZGI,
            //     G_SLIDER_TAMAM, G_SLIDER_HOME, G_SLIDER_AKTUEL_POZ
            //
            //   • Handshake (hem Input hem Output'ta — PC yazar, robot sıfırlar, PC okur):
            //     G_BORU_OLCUM_TAMAM, G_TABLA_OLCUM_TAMAM, G_OLCUM_OK,
            //     G_RESET, G_SLIDER_HAREKET, G_TABLA_OFFSET_HAZIR
            //
            //   • Geri-okuma kopyaları (PC'nin yazdığı değerin Input'ta izlenmesi):
            //     G_OFFSET_X/Y/Z/A/B/C, G_TABLA_OFFSET_X/Y/Z/A/B/C,
            //     G_KLIMA_TIP, G_KLIMA_ADET, G_SNIFFER_READY, G_SNIFFER_DEGER
            //     NOT: Bu kopyalar kaldırılabilir ama mevcut tag eşleşmeleri bozulabilir.
            // ═══════════════════════════════════════════════════════════════════════════
            if (RobotInputVars.Count == 0)
            {
                // --- ROBOT GENEL DURUM (Salt Input: Robot→PC) ---
                RobotInputVars.Add(Create("G_ROBOT_DURUM", "INT", "Input", 0));          // R1: 0=Bosta 1=Calisiyor 2=Hata 10-12=Gocator 50-51=Tabla | R2: 5=Timeout 10-11=TablaOffset 20-22=Sniffer 30-31=Slider
                RobotInputVars.Add(Create("G_IS_BITTI", "BOOL", "Input", false));         // Is tamamlandi bayragi
                RobotInputVars.Add(Create("G_HATA_VAR", "BOOL", "Input", false));         // Hata var bayragi
                RobotInputVars.Add(Create("G_HATA_KODU", "INT", "Input", 0));             // Hata kodu
                // --- KLIMA SECIMI ---
                RobotInputVars.Add(Create("G_KLIMA_TIP", "INT", "Input", 0));             // 0=Secilmedi 1..N=Klima tipi
                RobotInputVars.Add(Create("G_KLIMA_ADET", "INT", "Input", 0));            // Toplam klima tipi sayisi
                RobotInputVars.Add(Create("G_AKTIF_NOKTA_NO", "INT", "Input", 0));    // Aktif nokta no (0=bekleme, 1+=aktif)
                RobotInputVars.Add(Create("G_TOPLAM_NOKTA", "INT", "Input", 0));          // Toplam olcum noktasi
                RobotInputVars.Add(Create("G_NOK_SAYISI", "INT", "Input", 0));            // Basarisiz nokta sayisi
                RobotInputVars.Add(Create("G_NOK_NOKTA", "INT", "Input", 0));             // Son NOK olan nokta numarasi
                RobotInputVars.Add(Create("G_NOK_BILDIRIM", "BOOL", "Input", false));     // NOK bildirimi bayragi
                // --- GOCATOR BORU KAYNAK OFFSET (Geri-okuma: PC yazar Output'tan, burada izlenir) ---
                RobotInputVars.Add(Create("G_OFFSET_X", "REAL", "Input", 0.0));
                RobotInputVars.Add(Create("G_OFFSET_Y", "REAL", "Input", 0.0));
                RobotInputVars.Add(Create("G_OFFSET_Z", "REAL", "Input", 0.0));
                RobotInputVars.Add(Create("G_OFFSET_A", "REAL", "Input", 0.0));
                RobotInputVars.Add(Create("G_OFFSET_B", "REAL", "Input", 0.0));
                RobotInputVars.Add(Create("G_OFFSET_C", "REAL", "Input", 0.0));
                // --- GOCATOR OLCUM SISTEMI (TEKLI JOB INDEX) ---
                RobotInputVars.Add(Create("G_JOB_INDEX", "INT", "Input", 0));             // Salt Input: Gocator job (0=tabla, 1..N=boru noktasi)
                RobotInputVars.Add(Create("G_OLCUM_TETIK", "BOOL", "Input", false));      // Salt Input: Robot -> PC : Olcum baslat
                RobotInputVars.Add(Create("G_BORU_OLCUM_TAMAM", "BOOL", "Input", false));  // ⇄ Handshake: PC TRUE yazar, robot FALSE'a çeker
                RobotInputVars.Add(Create("G_TABLA_OLCUM_TAMAM", "BOOL", "Input", false)); // ⇄ Handshake: PC TRUE yazar, robot FALSE'a çeker
                RobotInputVars.Add(Create("G_OLCUM_OK", "BOOL", "Input", false));         // ⇄ Handshake: PC sonuc yazar, robot okur
                // --- GOCATOR TABLA OFFSET (Geri-okuma: PC yazar Output'tan, burada izlenir) ---
                RobotInputVars.Add(Create("G_TABLA_OFFSET_X", "REAL", "Input", 0.0));
                RobotInputVars.Add(Create("G_TABLA_OFFSET_Y", "REAL", "Input", 0.0));
                RobotInputVars.Add(Create("G_TABLA_OFFSET_Z", "REAL", "Input", 0.0));
                RobotInputVars.Add(Create("G_TABLA_OFFSET_A", "REAL", "Input", 0.0));
                RobotInputVars.Add(Create("G_TABLA_OFFSET_B", "REAL", "Input", 0.0));
                RobotInputVars.Add(Create("G_TABLA_OFFSET_C", "REAL", "Input", 0.0));
                // --- DURUM MESAJI ---
                RobotInputVars.Add(Create("G_DURUM_MESAJ", "INT", "Input", 0));           // Durum mesaj kodu (INT)
                // --- RESET (⇄ Bidirectional: Her iki taraf tetikleyebilir) ---
                RobotInputVars.Add(Create("G_RESET", "BOOL", "Input", false));            // ⇄ Bidirectional: Reset komutu bayragi
                // --- SISTEM KONTROL (Her iki robot) ---
                RobotInputVars.Add(Create("G_R1_HOME", "BOOL", "Input", false));           // Robot 1 home pozisyonunda
                RobotInputVars.Add(Create("G_R2_HOME", "BOOL", "Input", false));           // Robot 2 home pozisyonunda
                // --- INFICON SNIFFER OLCUM (Her iki robot) ---
                RobotInputVars.Add(Create("G_SNIFFER_OLCUM_TETIK", "BOOL", "Input", false)); // Robot -> PC : Sniffer olcum baslat/durdur (R1 + R2)
                RobotInputVars.Add(Create("G_SNIFFER_OLCUM_BITTI", "BOOL", "Input", false)); // Robot -> PC : Sniffer olcum tamamlandi (R1 + R2)
                RobotInputVars.Add(Create("G_SNIFFER_DEGER", "REAL", "Input", 0.0));      // Salt Input: Sniffer olcum degeri
                RobotInputVars.Add(Create("G_SNIFFER_READY", "BOOL", "Input", false));    // Geri-okuma: PC Output'tan yazar, burada izlenir
                // G_AKTIF_CIZGI kaldırıldı — G_AKTIF_NOKTA_ADI string ile değiştirildi
                RobotInputVars.Add(Create("G_TOPLAM_CIZGI", "INT", "Input", 0));          // Robot 2 toplam cizgi sayisi
                RobotInputVars.Add(Create("G_NOK_CIZGI", "INT", "Input", 0));             // Robot 2 son NOK cizgi no
                // --- ROBOT 2 SLIDER (KL100) DURUM ---
                RobotInputVars.Add(Create("G_SLIDER_HAREKET", "BOOL", "Input", false));    // ⇄ Bidirectional: Slider hareket durumu
                RobotInputVars.Add(Create("G_SLIDER_TAMAM", "BOOL", "Input", false));      // Slider hedefe ulasti
                RobotInputVars.Add(Create("G_SLIDER_HOME", "BOOL", "Input", false));       // Slider home pozisyonunda
                RobotInputVars.Add(Create("G_SLIDER_AKTUEL_POZ", "REAL", "Input", 0.0));   // Slider aktuel pozisyon (mm)
                // --- ROBOT 2 TABLA OFFSET DURUMU ---
                RobotInputVars.Add(Create("G_TABLA_OFFSET_HAZIR", "BOOL", "Input", false)); // ⇄ Handshake: PC TRUE yazar, robot okur ve sıfırlar
            }

            // ═══════════════════════════════════════════════════════════════════════════
            // RobotOutputVars: PC → Robot (Robot'a YAZILAN değerler)
            // PC uygulamasi bu degiskenlere yazar, robot okur
            // WriteToAllRobotsAsync() ile 3 katmana yazılır:
            //   1) GlobalData.RobotOutputVars (in-memory şablon)
            //   2) PlcService.OutputVariables (PLC köprüsü)
            //   3) KukaVarProxy TCP (doğrudan robot kontrolcüsü)
            // ─────────────────────────────────────────────────────────────────────────
            // Handshake sinyalleri (TAMAM, OK, RESET, SLIDER_HAREKET, TABLA_OFFSET_HAZIR)
            // hem burada hem RobotInputVars'ta bulunur — Robot'un sıfırlamasını izlemek için.
            // ═══════════════════════════════════════════════════════════════════════════
            if (RobotOutputVars.Count == 0)
            {
                // --- KONTROL ---
                RobotOutputVars.Add(Create("G_BASLAT", "BOOL", "Output", false));         // Baslat komutu (PC'den yazilir)
                RobotOutputVars.Add(Create("G_DUR", "BOOL", "Output", false));            // Dur komutu (PC'den yazilir)
                RobotOutputVars.Add(Create("G_RESET", "BOOL", "Output", false));          // Reset komutu (PC'den yazilir)
                RobotOutputVars.Add(Create("G_KLIMA_TIP", "INT", "Output", 0));           // Klima tipi secimi (PC'den yazilir)
                RobotOutputVars.Add(Create("G_KLIMA_ADET", "INT", "Output", 0));          // Toplam klima tipi sayisi (PC'den yazilir)
                // --- GOCATOR BORU OFFSET (PC yazar) ---
                RobotOutputVars.Add(Create("G_OFFSET_X", "REAL", "Output", 0.0));
                RobotOutputVars.Add(Create("G_OFFSET_Y", "REAL", "Output", 0.0));
                RobotOutputVars.Add(Create("G_OFFSET_Z", "REAL", "Output", 0.0));
                RobotOutputVars.Add(Create("G_OFFSET_A", "REAL", "Output", 0.0));
                RobotOutputVars.Add(Create("G_OFFSET_B", "REAL", "Output", 0.0));
                RobotOutputVars.Add(Create("G_OFFSET_C", "REAL", "Output", 0.0));
                // --- OLCUM SONUC (PC -> Robot) ---
                // G_BORU_OLCUM_TAMAM: Output listesinde TUTULMAZ — handshake sinyali.
                // WriteToAllRobotsAsync ile anlık yazılır, robot FALSE'a çeker.
                // G_TABLA_OLCUM_TAMAM: Output listesinde TUTULMAZ — handshake sinyali.
                // WriteToAllRobotsAsync ile anlık yazılır, robot FALSE'a çeker.
                // Output'ta tutulursa comm loop eski TRUE'yu geri yazar.
                RobotOutputVars.Add(Create("G_OLCUM_OK", "BOOL", "Output", false));       // Sonuc OK/NOK (PC -> Robot)
                // --- GOCATOR TABLA OFFSET (PC yazar) ---
                RobotOutputVars.Add(Create("G_TABLA_OFFSET_X", "REAL", "Output", 0.0));
                RobotOutputVars.Add(Create("G_TABLA_OFFSET_Y", "REAL", "Output", 0.0));
                RobotOutputVars.Add(Create("G_TABLA_OFFSET_Z", "REAL", "Output", 0.0));
                RobotOutputVars.Add(Create("G_TABLA_OFFSET_A", "REAL", "Output", 0.0));
                RobotOutputVars.Add(Create("G_TABLA_OFFSET_B", "REAL", "Output", 0.0));
                RobotOutputVars.Add(Create("G_TABLA_OFFSET_C", "REAL", "Output", 0.0));
                RobotOutputVars.Add(Create("G_TABLA_OFFSET_HAZIR", "BOOL", "Output", false)); // Tabla offset hazir (R1 tabla olcum sonrasi)
                // --- SISTEM KONTROL (PC -> Her iki Robot) ---
                RobotOutputVars.Add(Create("G_SAFETY_OK", "BOOL", "Output", false));      // Safety sinyali uygun
                RobotOutputVars.Add(Create("G_SISTEM_START", "BOOL", "Output", false));   // Sistem baslatma komutu
                RobotOutputVars.Add(Create("G_SISTEM_STOP", "BOOL", "Output", false));    // Sistem durdurma komutu
                RobotOutputVars.Add(Create("G_OTO_MOD", "BOOL", "Output", false));        // Otomatik/Manuel mod
                // --- INFICON SNIFFER SONUCLARI (PC -> Her iki Robot) ---
                RobotOutputVars.Add(Create("G_SNIFFER_READY", "BOOL", "Output", false));  // INFICON cihazi hazir (PC -> Robot)
                RobotOutputVars.Add(Create("G_SNIFFER_TAMAM", "BOOL", "Output", false));  // Sniffer olcum tamamlandi
                RobotOutputVars.Add(Create("G_SNIFFER_OK", "BOOL", "Output", false));     // Sniffer sonuc OK/NOK
                RobotOutputVars.Add(Create("G_SNIFFER_DEGER", "REAL", "Output", 0.0));    // Geri-yazma: Input'tan okunan değer robotlara dağıtılır
                // --- ROBOT 2 SLIDER KONTROL (PC -> Robot 2) ---
                RobotOutputVars.Add(Create("G_SLIDER_HEDEF_POZ", "REAL", "Output", 0.0)); // Slider hedef pozisyon (mm)
                RobotOutputVars.Add(Create("G_SLIDER_HAREKET", "BOOL", "Output", false)); // Slider hareket komutu (PC -> Robot 2)
                RobotOutputVars.Add(Create("G_HEDEF_ISTASYON", "INT", "Output", 0));      // Hedef istasyon no (1=Ist1, 2=Ist2, 3=Ist3, 4=Bakim)
                // --- İSTASYON HAZIR + İŞ BİTTİ (Robot programlarında mevcut) ---
                RobotOutputVars.Add(Create("G_IST1_HAZIR", "BOOL", "Output", false));      // İstasyon 1 hazır
                RobotOutputVars.Add(Create("G_IST2_HAZIR", "BOOL", "Output", false));      // İstasyon 2 hazır
                RobotOutputVars.Add(Create("G_IST3_HAZIR", "BOOL", "Output", false));      // İstasyon 3 hazır
                RobotOutputVars.Add(Create("G_IST1_IS_BITTI", "BOOL", "Output", false));   // İstasyon 1 iş bitti
                RobotOutputVars.Add(Create("G_IST2_IS_BITTI", "BOOL", "Output", false));   // İstasyon 2 iş bitti
                RobotOutputVars.Add(Create("G_IST3_IS_BITTI", "BOOL", "Output", false));   // İstasyon 3 iş bitti
                // --- İSTASYON E1 SLİDER POZİSYONLARI (PC -> Robot 2, config'den okunur) ---
                RobotOutputVars.Add(Create("G_IST_BAKIM_E1_POZ", "REAL", "Output", 0.0));   // Bakim istasyonu E1 (0mm)
                RobotOutputVars.Add(Create("G_IST1_E1_POZ", "REAL", "Output", 1041.0));  // Istasyon 1 E1 pozisyonu (mm)
                RobotOutputVars.Add(Create("G_IST2_E1_POZ", "REAL", "Output", 2377.0));  // Istasyon 2 E1 pozisyonu (mm)
                RobotOutputVars.Add(Create("G_IST3_E1_POZ", "REAL", "Output", 3716.0));  // Istasyon 3 E1 pozisyonu (mm)
                // --- ROBOT 2 SLIDER KOPRU (R2 -> R1 bridge) ---
                RobotOutputVars.Add(Create("G_R2_SLIDER_TAMAM", "BOOL", "Output", false));  // R2 slider hedefe ulasti (Robot 1'e yazilir)
                RobotOutputVars.Add(Create("G_R2_SLIDER_HOME", "BOOL", "Output", false));   // R2 slider home'da (Robot 1'e yazilir)
                RobotOutputVars.Add(Create("G_R2_SLIDER_POZ", "REAL", "Output", 0.0));      // R2 slider aktuel poz (Robot 1'e yazilir)
                // --- ÇAPRAZ ROBOT DURUM (Robot-Robot haberleşme) ---
                RobotOutputVars.Add(Create("G_R1_IS_BITTI", "BOOL", "Output", false));       // Robot 1 iş bitti (Robot 2'ye yazılır)
                RobotOutputVars.Add(Create("G_R1_ROBOT_DURUM", "INT", "Output", 0));         // Robot 1 durum (Robot 2'ye yazılır)
                RobotOutputVars.Add(Create("G_R1_HATA_VAR", "BOOL", "Output", false));       // Robot 1 hata var (Robot 2'ye yazılır)
                RobotOutputVars.Add(Create("G_R1_HATA_KODU", "INT", "Output", 0));           // Robot 1 hata kodu (Robot 2'ye yazılır)
                RobotOutputVars.Add(Create("G_R1_EKSEN_E1", "REAL", "Output", 0.0));         // Robot 1 harici eksen E1 (Robot 2'ye yazılır)
                RobotOutputVars.Add(Create("G_R2_IS_BITTI", "BOOL", "Output", false));       // Robot 2 iş bitti (Robot 1'e yazılır)
                RobotOutputVars.Add(Create("G_R2_ROBOT_DURUM", "INT", "Output", 0));         // Robot 2 durum (Robot 1'e yazılır)
                RobotOutputVars.Add(Create("G_R2_HATA_VAR", "BOOL", "Output", false));       // Robot 2 hata var (Robot 1'e yazılır)
                RobotOutputVars.Add(Create("G_R2_HATA_KODU", "INT", "Output", 0));           // Robot 2 hata kodu (Robot 1'e yazılır)
                RobotOutputVars.Add(Create("G_R2_EKSEN_E1", "REAL", "Output", 0.0));         // Robot 2 KL100 slider E1 (Robot 1'e yazılır)
            }
        }

        public static void SavePlcVariableTagsToFile()
        {
            try
            {
                object Map(ObservableCollection<PlcVariable> l) => l.Select(v => new
                {
                    name = v.Name,
                    plcTag = v.PlcTag,
                    plcTag2 = v.PlcTag2,
                    value = v.Value,
                    type = v.Type,          // ← YENİ: Tip bilgisi de kaydediliyor
                    direction = v.Direction  // ← YENİ: Yön bilgisi de kaydediliyor
                }).ToList();

                var data = new
                {
                    GeneralInputVars = Map(GeneralInputVars),
                    GeneralOutputVars = Map(GeneralOutputVars),
                    Station1Vars = Map(Station1Vars),
                    Station1Outputs = Map(Station1Outputs),
                    Station2Vars = Map(Station2Vars),
                    Station2Outputs = Map(Station2Outputs),
                    Station3Vars = Map(Station3Vars),
                    Station3Outputs = Map(Station3Outputs),
                    RobotInputVars = Map(RobotInputVars),
                    RobotOutputVars = Map(RobotOutputVars)
                };

                File.WriteAllText(_autoPageVariablesFilePath,
                    System.Text.Json.JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SAVE_ERROR] PlcVariableTags kayıt hatası: {ex.Message}");
            }
        }
        private static void LoadPlcVariableTagsFromFile()
        {
            try
            {
                if (!File.Exists(_autoPageVariablesFilePath)) return;
                var json = File.ReadAllText(_autoPageVariablesFilePath);
                var data = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(json);

                void Load(string p, ObservableCollection<PlcVariable> t)
                {
                    if (!data.TryGetProperty(p, out var a)) return;

                    // Build new list from file but preserve runtime CurrentValue from existing items
                    var newList = new List<PlcVariable>();

                    foreach (var i in a.EnumerateArray())
                    {
                        if (!i.TryGetProperty("name", out var n)) continue;
                        string name = n.GetString()?.Trim();
                        string plcTag = i.TryGetProperty("plcTag", out var pt) ? pt.GetString()?.Trim() : null;
                        string plcTag2 = i.TryGetProperty("plcTag2", out var pt2) ? pt2.GetString()?.Trim() : null;
                        string type = i.TryGetProperty("type", out var tt) ? tt.GetString() : null;
                        string direction = i.TryGetProperty("direction", out var dir) ? dir.GetString() : null;
                        string value = null;
                        if (i.TryGetProperty("value", out var val) && val.ValueKind != JsonValueKind.Null) value = val.ToString();

                        // Önce varsayılan (default) değişkeni bul — Type/Direction fallback için
                        var existing = t.FirstOrDefault(x => string.Equals(x.Name?.Trim(), name?.Trim(), StringComparison.OrdinalIgnoreCase));

                        var v = new PlcVariable
                        {
                            Name = name ?? "",
                            Type = type ?? existing?.Type ?? "STRING",           // ← Dosyadan oku, yoksa default'tan al
                            Direction = direction ?? existing?.Direction ?? "Input", // ← Dosyadan oku, yoksa default'tan al
                            IsEditable = true
                        };
                        if (!string.IsNullOrEmpty(plcTag)) v.PlcTag = plcTag;
                        if (!string.IsNullOrEmpty(plcTag2)) v.PlcTag2 = plcTag2;
                        if (!string.IsNullOrEmpty(value)) v.Value = value;

                        // Preserve runtime CurrentValue
                        if (existing != null)
                        {
                            v.CurrentValue = existing.CurrentValue;
                        }

                        // PlcTag yükleme durumunu logla
                        if (!string.IsNullOrEmpty(plcTag))
                            System.Diagnostics.Debug.WriteLine($"[PLC_LOAD] {name} → PlcTag={plcTag}, PlcTag2={plcTag2 ?? "(yok)"}");

                        newList.Add(v);
                    }

                    // Merge: Kaydedilmiş dosyada olmayan varsayılan değişkenleri de koru
                    var savedNames = new HashSet<string>(
                        newList.Select(nv2 => nv2.Name),
                        StringComparer.OrdinalIgnoreCase);
                    foreach (var defaultVar in t.ToList())
                    {
                        if (!savedNames.Contains(defaultVar.Name))
                            newList.Add(defaultVar);
                    }

                    t.Clear();
                    foreach (var nv in newList)
                        t.Add(nv);
                }

                Load("GeneralInputVars", GeneralInputVars);
                Load("GeneralOutputVars", GeneralOutputVars);
                Load("Station1Vars", Station1Vars);
                Load("Station1Outputs", Station1Outputs);
                Load("Station2Vars", Station2Vars);
                Load("Station2Outputs", Station2Outputs);
                Load("Station3Vars", Station3Vars);
                Load("Station3Outputs", Station3Outputs);
                Load("RobotInputVars", RobotInputVars);
                Load("RobotOutputVars", RobotOutputVars);

                System.Diagnostics.Debug.WriteLine($"[PLC_LOAD] Auto_Page_Variables.json başarıyla yüklendi");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PLC_LOAD_ERROR] PlcVariableTags yükleme hatası: {ex.Message}");
            }
        }

        private static void LoadAutomationSettings() 
        { 
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings.Values; 
            
            // ▼▼▼ KRİTİK: SETTER DEĞİL BACKING FIELD KULLAN ▼▼▼
            // Setter kullanırsak her biri SaveAutomationSettings çağırır ve 
            // diğer değerler henüz yüklenmeden "" olarak kaydedilir!
            
            if (settings.ContainsKey("Auto_RfidTag"))
            {
                var val = settings["Auto_RfidTag"] as string;
                if (!string.IsNullOrEmpty(val)) _autoRfidTag = val;
            }
            
            if (settings.ContainsKey("Auto_IndexTag"))
            {
                var val = settings["Auto_IndexTag"] as string;
                if (!string.IsNullOrEmpty(val)) _autoIndexTag = val;
            }
            
            if (settings.ContainsKey("Auto_TriggerTag"))
            {
                var val = settings["Auto_TriggerTag"] as string;
                if (!string.IsNullOrEmpty(val)) _autoTriggerTag = val;
            }

            if (settings.ContainsKey("Auto_TriggerTag2"))
            {
                var val = settings["Auto_TriggerTag2"] as string;
                if (!string.IsNullOrEmpty(val)) _autoTriggerTag2 = val;
            }

            if (settings.ContainsKey("MeasurementOutputTag"))
            {
                var val = settings["MeasurementOutputTag"] as string;
                if (!string.IsNullOrEmpty(val)) _measurementOutputTag = val;
            }

            if (settings.ContainsKey("TablaOutputTag"))
            {
                var val = settings["TablaOutputTag"] as string;
                if (!string.IsNullOrEmpty(val)) _tablaOutputTag = val;
            }
            if (settings.ContainsKey("TablaOutputTag2"))
            {
                var val = settings["TablaOutputTag2"] as string;
                if (!string.IsNullOrEmpty(val)) _tablaOutputTag2 = val;
            }

            // Robot Ayarları
            if (settings.ContainsKey("Robot_IpAddress"))
            {
                var val = settings["Robot_IpAddress"] as string;
                if (!string.IsNullOrEmpty(val)) _robotIpAddress = val;
            }
            if (settings.ContainsKey("Robot_Port"))
            {
                try { _robotPort = Convert.ToInt32(settings["Robot_Port"]); }
                catch { }
            }
            if (settings.ContainsKey("Robot_ReadSpeed"))
            {
                try
                {
                    int rs = Convert.ToInt32(settings["Robot_ReadSpeed"]);
                    _robotReadSpeed = Math.Max(50, Math.Min(500, rs));
                    System.Diagnostics.Debug.WriteLine($"[GlobalData] Robot_ReadSpeed yüklendi: {_robotReadSpeed}ms");
                }
                catch { System.Diagnostics.Debug.WriteLine($"[GlobalData] Robot_ReadSpeed yükleme HATASI, tip: {settings["Robot_ReadSpeed"]?.GetType()?.Name}"); }
            }

            if (settings.ContainsKey("KL100_R1Home")) _kl100Robot1HomeSignal = settings["KL100_R1Home"] as string;
            if (settings.ContainsKey("KL100_R2Home")) _kl100Robot2HomeSignal = settings["KL100_R2Home"] as string;

            // Safety Alarm çıkış tag'leri
            if (settings.ContainsKey("SafetyAlarmR1OutputTag")) _safetyAlarmR1OutputTag = settings["SafetyAlarmR1OutputTag"] as string ?? "";
            if (settings.ContainsKey("SafetyAlarmR2OutputTag")) _safetyAlarmR2OutputTag = settings["SafetyAlarmR2OutputTag"] as string ?? "";

            // PLC Ayarları
            if (settings.ContainsKey("Plc_IpAddress"))
            {
                var val = settings["Plc_IpAddress"] as string;
                if (!string.IsNullOrEmpty(val)) _plcIpAddress = val;
            }
            if (settings.ContainsKey("Plc_Port"))
            {
                try { _plcPort = Convert.ToInt32(settings["Plc_Port"]); } catch { }
            }

            // Gocator Ayarları
            if (settings.ContainsKey("Gocator_IpAddress"))
            {
                var val = settings["Gocator_IpAddress"] as string;
                if (!string.IsNullOrEmpty(val)) _gocatorIpAddress = val;
            }
            if (settings.ContainsKey("Gocator_Port"))
            {
                try { _gocatorPort = Convert.ToInt32(settings["Gocator_Port"]); } catch { }
            }
            if (settings.ContainsKey("ToolRelativeOffsets"))
            {
                try { _savedToolRelativeOffsets = settings["ToolRelativeOffsets"] as string ?? ""; } catch { }
            }

            // Tabla Kaçıklık Alarm Limiti
            if (settings.ContainsKey("TablaAlarmLimit"))
            {
                try { _tablaAlarmLimit = Convert.ToDouble(settings["TablaAlarmLimit"]); } catch { }
            }

            // Veri kaynağı seçimi
            if (settings.ContainsKey("DataSourceMode"))
            {
                var mode = settings["DataSourceMode"] as string;
                if (!string.IsNullOrEmpty(mode)) _dataSourceMode = mode;
            }

            // CODESYS ofsetleri artık Codesys_Offsets.json'dan yükleniyor (LoadCodesysOffsets)
            // LocalSettings'teki eski değerleri temizle (migration)
            settings.Remove("CodesysOffsetX");
            settings.Remove("CodesysOffsetY");
            settings.Remove("CodesysOffsetZ");
            settings.Remove("CodesysGocMappings");

            // Haberleşme zamanlama ayarları (clamp ile güvenli yükleme)
            if (settings.ContainsKey("Plc_ReadInterval"))
            {
                try { _plcReadInterval = Math.Max(20, Math.Min(500, Convert.ToInt32(settings["Plc_ReadInterval"]))); } catch { }
            }
            if (settings.ContainsKey("TriggerMonitor_Interval"))
            {
                try { _triggerMonitorInterval = Math.Max(200, Math.Min(2000, Convert.ToInt32(settings["TriggerMonitor_Interval"]))); } catch { }
            }
            if (settings.ContainsKey("Robot_TcpTimeout"))
            {
                try { _robotTcpTimeout = Math.Max(1000, Math.Min(30000, Convert.ToInt32(settings["Robot_TcpTimeout"]))); } catch { }
            }
            if (settings.ContainsKey("Gocator_RestTimeout"))
            {
                try { _gocatorRestTimeout = Math.Max(5000, Math.Min(120000, Convert.ToInt32(settings["Gocator_RestTimeout"]))); } catch { }
            }
            if (settings.ContainsKey("Inficon_RefreshInterval"))
            {
                try { _inficonRefreshInterval = Math.Max(100, Math.Min(1000, Convert.ToInt32(settings["Inficon_RefreshInterval"]))); } catch { }
            }
            if (settings.ContainsKey("Inficon_TrendInterval"))
            {
                try { _inficonTrendInterval = Math.Max(500, Math.Min(5000, Convert.ToInt32(settings["Inficon_TrendInterval"]))); } catch { }
            }
            if (settings.ContainsKey("Safety_CheckInterval"))
            {
                try { _safetyCheckInterval = Math.Max(500, Math.Min(5000, Convert.ToInt32(settings["Safety_CheckInterval"]))); } catch { }
            }

            // Debug log
            System.Diagnostics.Debug.WriteLine($"[GlobalData] Ayarlar yüklendi: RFID={_autoRfidTag}, Trigger={_autoTriggerTag}, RobotIP={_robotIpAddress}, PlcIP={_plcIpAddress}:{_plcPort}, GocatorIP={_gocatorIpAddress}:{_gocatorPort}, ReadSpeed={_robotReadSpeed}ms");
        }
        public static void SaveAutomationSettings() 
        { 
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings.Values; 
            settings["Auto_RfidTag"] = Auto_RfidTag ?? ""; 
            settings["Auto_IndexTag"] = Auto_IndexTag ?? ""; 
            settings["Auto_TriggerTag"] = Auto_TriggerTag ?? "";
            settings["Auto_TriggerTag2"] = Auto_TriggerTag2 ?? "";
            settings["MeasurementOutputTag"] = MeasurementOutputTag ?? "";
            settings["TablaOutputTag"] = TablaOutputTag ?? "";
            settings["TablaOutputTag2"] = TablaOutputTag2 ?? "";
            settings["TablaAlarmLimit"] = TablaAlarmLimit;

            settings["Robot_IpAddress"] = Robot_IpAddress;
            settings["Robot_Port"] = Robot_Port;
            settings["Robot_ReadSpeed"] = Robot_ReadSpeed;

            settings["Plc_IpAddress"] = Plc_IpAddress;
            settings["Plc_Port"] = Plc_Port;

            settings["Gocator_IpAddress"] = Gocator_IpAddress;
            settings["Gocator_Port"] = Gocator_Port;
            settings["ToolRelativeOffsets"] = SavedToolRelativeOffsets ?? "";

            // Haberleşme zamanlama ayarları
            settings["Plc_ReadInterval"] = Plc_ReadInterval;
            settings["TriggerMonitor_Interval"] = TriggerMonitor_Interval;
            settings["Robot_TcpTimeout"] = Robot_TcpTimeout;
            settings["Gocator_RestTimeout"] = Gocator_RestTimeout;
            settings["Inficon_RefreshInterval"] = Inficon_RefreshInterval;
            settings["Inficon_TrendInterval"] = Inficon_TrendInterval;
            settings["Safety_CheckInterval"] = Safety_CheckInterval;

            // Veri kaynağı seçimi
            settings["DataSourceMode"] = DataSourceMode;

            // CODESYS ofsetleri artık SaveCodesysOffsets() ile JSON'a yazılıyor

            // KL100 Slider R1/R2 Home sinyal seçimleri
            settings["KL100_R1Home"] = KL100_Robot1HomeSignal ?? "";
            settings["KL100_R2Home"] = KL100_Robot2HomeSignal ?? "";

            // Safety Alarm çıkış tag'leri
            settings["SafetyAlarmR1OutputTag"] = SafetyAlarmR1OutputTag ?? "";
            settings["SafetyAlarmR2OutputTag"] = SafetyAlarmR2OutputTag ?? "";

            StartAutomationListener(); 
        }

        // --- PLC DİNLEYİCİSİ (Çift Trigger) ---
        private static PlcVariable _currentTriggerVar;  // Boru ölçüm tetik
        private static PlcVariable _currentTriggerVar2; // Tabla kaçıklık tetik
        private static PlcVariable? _currentIndexVar;   // Job index watcher (Camera sayfası kapalıyken de senkronize eder)

        /// <summary>
        /// KUKA REAL formatını ("2.0", "2.000") güvenli şekilde int'e çevirir.
        /// int.TryParse "2.0" parse edemez — bu yüzden double üzerinden geçeriz.
        /// </summary>
        private static bool TryParseIndex(string value, out int index)
        {
            index = 0;
            if (string.IsNullOrEmpty(value)) return false;

            // Önce direkt int dene (en hızlı yol)
            if (int.TryParse(value, out index)) return true;

            // KUKA REAL format: "2.0", "2.000" vb. — double üzerinden çevir
            if (double.TryParse(value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double d))
            {
                index = (int)Math.Round(d);
                return true;
            }
            return false;
        }

        private static PlcVariable FindPlcVarByName(string tagName)
        {
            if (string.IsNullOrEmpty(tagName)) return null;

            // 1. PLC genel değişkenler
            var v = GeneralInputVars.FirstOrDefault(x => x.Name == tagName);
            if (v != null) return v;
            v = GeneralOutputVars.FirstOrDefault(x => x.Name == tagName);
            if (v != null) return v;

            // 2. PLC servis değişkenleri
            if (PlcService.Instance != null)
            {
                v = PlcService.Instance.InputVariables.FirstOrDefault(x => x.Name == tagName);
                if (v != null) return v;
                v = PlcService.Instance.OutputVariables.FirstOrDefault(x => x.Name == tagName);
                if (v != null) return v;
            }

            // 3. Robot CANLI instance değişkenleri (CommunicationLoop tarafından sürekli güncellenir)
            //    ÖNCELİKLİ: Tetik dinleyicisi canlı PropertyChanged alabilsin diye
            //    statik GlobalData kopyası DEĞİL, robot instance'ın kendisi aranır.
            var robots = Utilities.KukaRobotManager.Instance?.Robots;
            if (robots != null)
            {
                foreach (var robot in robots)
                {
                    v = robot.InputVars.FirstOrDefault(x => x.Name == tagName);
                    if (v != null) return v;
                    v = robot.OutputVars.FirstOrDefault(x => x.Name == tagName);
                    if (v != null) return v;
                }
            }

            // 4. GlobalData statik robot değişkenleri (fallback — robot henüz bağlanmadıysa)
            v = RobotInputVars.FirstOrDefault(x => x.Name == tagName);
            if (v != null) return v;
            v = RobotOutputVars.FirstOrDefault(x => x.Name == tagName);
            if (v != null) return v;

            return null;
        }

        /// <summary>
        /// Tetik dinleyicilerini (yeniden) bağlar.
        /// Robot bağlantısı kurulduktan sonra çağrılmalı ki canlı instance değişkenleri kullanılsın.
        /// </summary>
        public static void StartAutomationListener()
        {
            // --- Trigger 1: Boru Ölçüm ---
            if (_currentTriggerVar != null)
            {
                _currentTriggerVar.PropertyChanged -= TriggerVar_PropertyChanged;
                _currentTriggerVar = null;
            }

            if (!string.IsNullOrEmpty(Auto_TriggerTag))
            {
                var triggerVar = FindPlcVarByName(Auto_TriggerTag);
                if (triggerVar != null)
                {
                    _currentTriggerVar = triggerVar;
                    _currentTriggerVar.PropertyChanged += TriggerVar_PropertyChanged;
                    OnAutomationLog?.Invoke($"Boru tetik devrede: {Auto_TriggerTag} izleniyor.");
                }
                else
                {
                    OnAutomationLog?.Invoke($"⚠ UYARI: Boru Trigger Tag '{Auto_TriggerTag}' bulunamadı.");
                }
            }

            // --- Trigger 2: Tabla Kaçıklık ---
            if (_currentTriggerVar2 != null)
            {
                _currentTriggerVar2.PropertyChanged -= TriggerVar2_PropertyChanged;
                _currentTriggerVar2 = null;
            }

            if (!string.IsNullOrEmpty(Auto_TriggerTag2))
            {
                var triggerVar2 = FindPlcVarByName(Auto_TriggerTag2);
                if (triggerVar2 != null)
                {
                    _currentTriggerVar2 = triggerVar2;
                    _currentTriggerVar2.PropertyChanged += TriggerVar2_PropertyChanged;
                    OnAutomationLog?.Invoke($"Tabla tetik devrede: {Auto_TriggerTag2} izleniyor (TABLA ZORUNLU idx=0).");
                }
                else
                {
                    OnAutomationLog?.Invoke($"⚠ UYARI: Tabla Trigger Tag '{Auto_TriggerTag2}' bulunamadı.");
                }
            }

            // --- Index Watcher: G_JOB_INDEX değiştiğinde CurrentJobIndex + Sapma/Sniffer senkronize et ---
            if (_currentIndexVar != null)
            {
                _currentIndexVar.PropertyChanged -= IndexVar_PropertyChanged;
                _currentIndexVar = null;
            }

            if (!string.IsNullOrEmpty(Auto_IndexTag))
            {
                var indexVar = FindPlcVarByName(Auto_IndexTag);
                if (indexVar != null)
                {
                    _currentIndexVar = indexVar;
                    _currentIndexVar.PropertyChanged += IndexVar_PropertyChanged;

                    // Mevcut değeri hemen senkronize et (KUKA REAL "2.0" formatını da destekler)
                    if (TryParseIndex(indexVar.Value, out int currentIdx))
                    {
                        UpdateCurrentJobIndex(currentIdx);
                    }
                }
            }

            // Robot InputVars sinyal izlemeyi de başlat (G_OLCUM_TETIK, G_SNIFFER_OLCUM_TETIK)
            StartRobotSignalMonitoring();
        }

        // ─── ROBOT GERİ YAZMA (3 KATMANLI: GlobalData + PLC + Robots) ───
        /// <summary>
        /// Değişkeni 3 katmana yazar: GlobalData, PLC, tüm bağlı robotlar.
        /// Hem RunAutomationSequence hem HandleOlcumTetikAsync tarafından kullanılır.
        /// </summary>
        public static async Task WriteToAllRobotsAsync(string varName, string value)
        {
            // 1. GlobalData output var güncelle (in-memory)
            var gVar = RobotOutputVars.FirstOrDefault(v => v.Name == varName);
            if (gVar != null) gVar.Value = value;

            // 2. PLC'ye yaz
            var plcVar = PlcService.Instance?.OutputVariables?.FirstOrDefault(v => v.Name == varName);
            if (plcVar != null)
            {
                plcVar.Value = value;
                try { await PlcService.Instance.WriteAsync(plcVar, value); } catch { }
            }

            // 3. ★ Tüm robotlara PARALEL yaz (sıralı → paralel: 2 robot = 2x hız kazanımı)
            var robots = Utilities.KukaRobotManager.Instance?.Robots;
            if (robots != null)
            {
                var writeTasks = new List<Task>();
                for (int ri = 0; ri < robots.Count; ri++)
                {
                    var r = robots[ri];
                    int robotIdx = ri + 1;
                    if (r.IsConnected)
                    {
                        // ★ Robot'ta tanımlı PlcTag varsa onu kullan (CommunicationLoop PlcTag ile okur)
                        // OutputVars ÖNCE aranmalı — yazma işlemi yapıyoruz, CommunicationLoop OutputVars'tan okur
                        string krlTag = varName;
                        var matchVar = r.OutputVars.FirstOrDefault(v => v.Name == varName)
                                    ?? r.InputVars.FirstOrDefault(v => v.Name == varName);
                        if (matchVar != null)
                        {
                            if (!string.IsNullOrEmpty(matchVar.PlcTag))
                                krlTag = matchVar.PlcTag;
                            // ★ Robot'un kendi OutputVars/InputVars değerini de güncelle
                            // CommunicationLoop bu değeri okuyup robota yazıyor
                            matchVar.Value = value;
                            // ★ CommunicationLoop'un _lastWrittenOutputs cache'ini de güncelle
                            // Yoksa eski cache değeri farklıysa CommunicationLoop tekrar eski değeri yazabilir
                            r.UpdateLastWrittenOutput(matchVar.PlcTag ?? varName, value);
                        }

                        string capturedTag = krlTag; // closure için kopyala
                        writeTasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                bool ok = await r.WriteVariableAsync(capturedTag, value);
                                if (!ok)
                                    OnAutomationLog?.Invoke($"⚠ Robot {robotIdx} yazma başarısız: {capturedTag}={value} (VarProxy hata)");
                            }
                            catch (Exception exW)
                            {
                                OnAutomationLog?.Invoke($"✗ Robot {robotIdx} yazma hatası: {capturedTag}={value} → {exW.Message}");
                            }
                        }));
                    }
                    else
                    {
                        OnAutomationLog?.Invoke($"⚠ Robot {robotIdx} bağlı değil, yazılamadı: {varName}={value}");
                    }
                }
                if (writeTasks.Count > 0)
                    await Task.WhenAll(writeTasks);
            }
            else
            {
                OnAutomationLog?.Invoke($"⚠ Robot listesi boş, yazılamadı: {varName}={value}");
            }
        }

        // ─── İSTASYON ÇALIŞ (MANUEL SAYFA → GLOBAL ETKİ) ───
        // Manuel sayfadan istasyon çalış butonuna basıldığında her iki robota:
        //   1) G_ISTx_IS_BITTI = FALSE  (iş bitmedi)
        //   2) G_ISTx_HAZIR = TRUE      (istasyon hazır)
        //   3) G_HEDEF_ISTASYON = stationNo
        //   4) Tabla ölçüm sıfırla (bekleniyor)
        //   5) Boru ölçüm sıfırla (bekleniyor)
        // ════════════════════════════════════════════════════════════
        public static async Task PrepareStationWorkAsync(int stationNo)
        {
            if (stationNo < 1 || stationNo > 3) return;

            OnAutomationLog?.Invoke($"[İstasyon Çalış] İstasyon {stationNo} hazırlanıyor...");

            // 1. Seçilen istasyon: IS_BITTI=FALSE, HAZIR=TRUE
            //    Diğer istasyonlar: IS_BITTI=TRUE, HAZIR=FALSE
            for (int ist = 1; ist <= 3; ist++)
            {
                if (ist == stationNo)
                {
                    await WriteToAllRobotsAsync($"G_IST{ist}_IS_BITTI", "FALSE");
                    await WriteToAllRobotsAsync($"G_IST{ist}_HAZIR", "TRUE");
                }
                else
                {
                    await WriteToAllRobotsAsync($"G_IST{ist}_IS_BITTI", "TRUE");
                    await WriteToAllRobotsAsync($"G_IST{ist}_HAZIR", "FALSE");
                }
            }

            // 2. Hedef istasyon bilgisini her iki robota yaz
            await WriteToAllRobotsAsync("G_HEDEF_ISTASYON", stationNo.ToString());

            // 3. Tabla ölçüm → 0'a çek
            await WriteToAllRobotsAsync("G_TABLA_OLCUM_TAMAM", "FALSE");
            _tablaOlcumTamamFlags.Clear();
            ResetTablaMeasurementSignal();

            // 4. Boru ölçüm → 0'a çek
            await WriteToAllRobotsAsync("G_BORU_OLCUM_TAMAM", "FALSE");
            _boruOlcumTamamFlags.Clear();
            ResetMeasurementSignal();

            // 5. Klima tip bilgisini tazele (tekrar test için)
            var klimaTipVar = RobotOutputVars.FirstOrDefault(v => v.Name == "G_KLIMA_TIP");
            if (klimaTipVar != null && !string.IsNullOrEmpty(klimaTipVar.Value))
            {
                await WriteToAllRobotsAsync("G_KLIMA_TIP", klimaTipVar.Value);
            }

            OnAutomationLog?.Invoke($"[İstasyon Çalış] İstasyon {stationNo} aktif. Diğerleri kapatıldı. Klima tip tazelendi. Tabla+Boru sıfırlandı.");

            // Tabla kaçıklık UI sıfırla
            if (stationNo >= 1 && stationNo <= 3 && stationNo - 1 < Stations.Count)
            {
                var st = Stations[stationNo - 1];
                st.TablaOffsetX = "0.0"; st.TablaOffsetY = "0.0"; st.TablaOffsetZ = "0.0";
                st.TablaOffsetA = "0.0"; st.TablaOffsetB = "0.0"; st.TablaOffsetC = "0.0";
                st.TablaOlcumTamam = false;
            }
        }

        /// <summary>
        /// Aktif istasyonun tabla kaçıklık değerlerini UI'da günceller.
        /// </summary>
        private static void UpdateStationTablaOffsets(List<GocatorMeasurement> results)
        {
            // Hedef istasyonu bul
            var hedefVar = RobotOutputVars.FirstOrDefault(v => v.Name == "G_HEDEF_ISTASYON");
            int hedefIdx = 0;
            if (hedefVar != null) int.TryParse(hedefVar.Value, out hedefIdx);
            if (hedefIdx < 1 || hedefIdx > 3 || hedefIdx - 1 >= Stations.Count) return;

            var station = Stations[hedefIdx - 1];
            string[] axes = { "X", "Y", "Z", "A", "B", "C" };
            for (int i = 0; i < Math.Min(results.Count, 6); i++)
            {
                string val = results[i].Value.ToString("F3");
                switch (i)
                {
                    case 0: station.TablaOffsetX = val; break;
                    case 1: station.TablaOffsetY = val; break;
                    case 2: station.TablaOffsetZ = val; break;
                    case 3: station.TablaOffsetA = val; break;
                    case 4: station.TablaOffsetB = val; break;
                    case 5: station.TablaOffsetC = val; break;
                }
            }
            station.TablaOlcumTamam = true;
        }

        // ─── ROBOT SİNYAL İZLEME (GLOBAL - SAYFA BAĞIMSIZ) ───
        // G_OLCUM_TETIK ve G_SNIFFER_OLCUM_TETIK sinyallerini GlobalData'dan dinler.
        // Hangi sayfada olursa olsun tetik alınır ve işlenir.
        // ════════════════════════════════════════════════════════════

        private static bool _robotSignalMonitoringActive = false;
        private static bool _olcumTetikProcessing = false;
        private static bool _snifferOlcumProcessing = false;

        // ═══ GOCATOR JOB ÖN-YÜKLEME (PRE-LOAD) ═══
        // Robot G_JOB_INDEX değiştirdiği anda job yüklenir → robot hareket ederken paralel yükleme
        // Ölçüm tetik geldiğinde LoadJob atlanır → süre kazanımı (~300-700ms per nokta)
        private static string _preLoadedGocatorJob = null;
        private static Task _jobPreLoadTask = Task.CompletedTask;

        /// <summary>
        /// Tüm robotların InputVars değişimlerini dinlemeye başlar.
        /// Bir kez çağrılır, sayfa değişse bile aktif kalır.
        /// </summary>
        public static void StartRobotSignalMonitoring()
        {
            if (_robotSignalMonitoringActive) return;

            var robots = Utilities.KukaRobotManager.Instance?.Robots;
            if (robots == null || robots.Count == 0) return;

            for (int i = 0; i < robots.Count; i++)
            {
                int robotNo = i + 1;
                var robot = robots[i];

                foreach (var v in robot.InputVars)
                {
                    v.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(PlcVariable.Value))
                        {
                            CheckRobotTriggerSignalGlobal(v, robot, robotNo);
                        }
                    };
                }

                // ═══ OUTPUT SİNYAL YAŞAM DÖNGÜSÜ İZLEME ═══
                // TAMAM/HAZIR sinyallerinin TRUE↔FALSE geçişlerini logla
                // Robot bu sinyalleri aldıktan sonra FALSE'a çektiğinde burada görünür
                foreach (var v in robot.OutputVars)
                {
                    string vName = v.Name;
                    if (string.IsNullOrEmpty(vName)) continue;
                    // Sadece TAMAM/HAZIR sinyallerini izle (gereksiz log kirliliği önlenir)
                    if (!vName.Contains("TAMAM") && !vName.Contains("HAZIR")) continue;

                    string prevValue = v.Value ?? "";
                    v.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName != nameof(PlcVariable.Value)) return;
                        string newVal = v.Value ?? "";
                        if (newVal == prevValue) return;

                        bool wasTrue = prevValue == "TRUE" || prevValue == "1";
                        bool isTrue = newVal == "TRUE" || newVal == "1" || newVal.Equals("true", StringComparison.OrdinalIgnoreCase);
                        bool isFalse = newVal == "FALSE" || newVal == "0" || newVal.Equals("false", StringComparison.OrdinalIgnoreCase);

                        if (wasTrue && isFalse)
                            OnAutomationLog?.Invoke($"[Robot {robotNo}] {vName}: TRUE → FALSE (robot sinyali aldı, sonraki ölçüme hazır)");
                        else if (!wasTrue && isTrue)
                            OnAutomationLog?.Invoke($"[Robot {robotNo}] {vName}: → TRUE (ölçüm tamamlandı, sinyal gönderildi)");

                        prevValue = newVal;
                    };
                }
            }

            _robotSignalMonitoringActive = true;

            // ═══ KLİMA TİP PERİYODİK SYNC (2sn) ═══
            // Ölçüm yapılmıyorken klima tipini robotlara yazar
            // Robot programı G_KLIMA_TIP=0 yapsa bile PC düzeltir
            StartKlimaTipSyncTimer();

            OnAutomationLog?.Invoke("Robot sinyal izleme başlatıldı (Global - sayfa bağımsız, Output lifecycle aktif).");
        }

        private static System.Threading.Timer _klimaTipSyncTimer;

        private static void StartKlimaTipSyncTimer()
        {
            _klimaTipSyncTimer?.Dispose();
            _klimaTipSyncTimer = new System.Threading.Timer(async _ =>
            {
                try
                {
                    // Ölçüm veya sniffer işlemi varsa YAZMA — haberleşme hattını meşgul etme
                    if (_olcumTetikProcessing || _snifferOlcumProcessing) return;

                    int currentIdx = _aktuelKlimaIndex;
                    if (currentIdx <= 0 || currentIdx > KnownRfids.Count) return;

                    string klimaStr = currentIdx.ToString();
                    int caseId = KnownRfids[currentIdx - 1].CasingIndex;

                    var robots = Utilities.KukaRobotManager.Instance?.Robots;
                    if (robots == null) return;

                    foreach (var r in robots)
                    {
                        if (!r.IsConnected) continue;

                        // Sadece robotun değeri farklıysa yaz (gereksiz trafik önlenir)
                        var tipVar = r.InputVars.FirstOrDefault(v => v.Name == "G_KLIMA_TIP");
                        if (tipVar != null && tipVar.Value == klimaStr) continue;

                        try
                        {
                            await r.WriteVariableAsync("G_KLIMA_TIP", klimaStr);
                            await r.WriteVariableAsync("G_CASE_ID", caseId.ToString());
                        }
                        catch { }
                    }
                }
                catch { }
            }, null, 2000, 2000);
        }

        private static void CheckRobotTriggerSignalGlobal(PlcVariable changedVar, KukaRobotInstance robot, int robotNo)
        {
            // ═══ TABLA OLCUM TAMAM → Her iki robot TRUE aldıysa 2sn sonra FALSE'a çek ═══
            if (changedVar.Name == "G_TABLA_OLCUM_TAMAM")
            {
                bool isTamamTrue = changedVar.Value?.ToUpper() == "TRUE" || changedVar.Value == "1";
                if (isTamamTrue)
                {
                    _tablaOlcumTamamFlags[robotNo] = true;

                    // Her iki robot da TRUE aldı mı kontrol et
                    bool r1Ok = _tablaOlcumTamamFlags.ContainsKey(1) && _tablaOlcumTamamFlags[1];
                    bool r2Ok = _tablaOlcumTamamFlags.ContainsKey(2) && _tablaOlcumTamamFlags[2];

                    if (r1Ok && r2Ok)
                    {
                        OnAutomationLog?.Invoke($"[Tabla] Her iki robot TAMAM aldı → 2sn sonra FALSE'a çekilecek");
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(2000);
                            await WriteToAllRobotsAsync("G_TABLA_OLCUM_TAMAM", "FALSE");
                            _tablaOlcumTamamFlags[1] = false;
                            _tablaOlcumTamamFlags[2] = false;
                            // Kamera sayfası tabla çıktı sinyalini de BEKLİYOR'a al
                            ResetTablaMeasurementSignal();
                            OnAutomationLog?.Invoke($"[Tabla] G_TABLA_OLCUM_TAMAM = FALSE + Tabla sinyal sıfırlandı (yeni ölçüm bekleniyor)");
                        });
                    }
                }
                else
                {
                    _tablaOlcumTamamFlags[robotNo] = false;
                }
                return;
            }

            // ═══ G_RESET → TAMAM sinyallerini sıfırla ═══
            if (changedVar.Name == "G_RESET")
            {
                bool isResetTrue = changedVar.Value?.ToUpper() == "TRUE" || changedVar.Value == "1";
                if (isResetTrue)
                {
                    _ = Task.Run(async () =>
                    {
                        await WriteToAllRobotsAsync("G_TABLA_OLCUM_TAMAM", "FALSE");
                        await WriteToAllRobotsAsync("G_BORU_OLCUM_TAMAM", "FALSE");
                        ResetTablaMeasurementSignal();
                        ResetMeasurementSignal();
                        _tablaOlcumTamamFlags.Clear();
                        _boruOlcumTamamFlags.Clear();
                        OnAutomationLog?.Invoke($"[Reset] G_TABLA_OLCUM_TAMAM + G_BORU_OLCUM_TAMAM = FALSE (sistem reset)");
                    });
                }
                return;
            }

            // ═══ G_KLIMA_TIP SIFIRA DÜŞTÜ → App tekrar yazsın ═══
            if (changedVar.Name == "G_KLIMA_TIP")
            {
                if (changedVar.Value == "0" || changedVar.Value == "0.0")
                {
                    int currentIdx = _aktuelKlimaIndex;
                    if (currentIdx > 0 && currentIdx <= KnownRfids.Count)
                    {
                        // App'te hâlâ geçerli klima var — robota tekrar yaz
                        string klimaStr = currentIdx.ToString();
                        int caseId = KnownRfids[currentIdx - 1].CasingIndex;
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(1000); // Robot restart tamamlansın
                            await WriteToAllRobotsAsync("G_KLIMA_TIP", klimaStr);
                            await WriteToAllRobotsAsync("G_CASE_ID", caseId.ToString());
                            OnAutomationLog?.Invoke($"[Robot {robotNo}] G_KLIMA_TIP=0 algılandı → tekrar yazıldı: TIP={klimaStr}, CASE={caseId}");
                        });
                    }
                }
                return;
            }

            // ═══ BORU OLCUM TAMAM → Robot 1 TRUE aldıysa 2sn sonra FALSE'a çek ═══
            // Boru ölçüm sadece Robot 1'i ilgilendiriyor (Gocator Robot 1'de)
            if (changedVar.Name == "G_BORU_OLCUM_TAMAM")
            {
                bool isTamamTrue = changedVar.Value?.ToUpper() == "TRUE" || changedVar.Value == "1";
                if (isTamamTrue && robotNo == 1)
                {
                    OnAutomationLog?.Invoke($"[Boru] Robot 1 TAMAM aldı → 2sn sonra FALSE'a çekilecek");
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(2000);
                        await WriteToAllRobotsAsync("G_BORU_OLCUM_TAMAM", "FALSE");
                        ResetMeasurementSignal();
                        OnAutomationLog?.Invoke($"[Boru] G_BORU_OLCUM_TAMAM = FALSE + sinyal sıfırlandı");
                    });
                }
                return;
            }

            // ═══ G_JOB_INDEX DEĞİŞİMİ → JOB ÖN-YÜKLEME ═══
            // Robot index'i değiştirdiği an job yüklenir (robot hareket ederken paralel)
            if (changedVar.Name == "G_JOB_INDEX")
            {
                _jobPreLoadTask = HandleJobIndexChangeAsync(changedVar.Value, robotNo);
                return;
            }

            // ═══ YÜKSELEN KENAR (FALSE→TRUE) TESTİ ═══
            bool isTrue = changedVar.Value?.ToUpper() == "TRUE" || changedVar.Value == "1";

            // Boru/Tabla tetik yükselen kenar ARM mekanizması (global seviyede)
            if (changedVar.Name == "G_BORU_OLCUM_TETIK")
            {
                if (!isTrue) { _boruTriggerArmed = true; return; }
                if (!_boruTriggerArmed) return;
                _boruTriggerArmed = false;
                OnAutomationLog?.Invoke($"[Robot {robotNo}] G_BORU_OLCUM_TETIK (global) → RunAutomationSequence");
                _ = RunAutomationSequence(forceTablaIndex: false);
                return;
            }

            if (changedVar.Name == "G_TABLA_OLCUM_TETIK")
            {
                if (!isTrue) { _tablaTriggerArmed = true; return; }
                if (!_tablaTriggerArmed) return;
                _tablaTriggerArmed = false;
                OnAutomationLog?.Invoke($"[Robot {robotNo}] G_TABLA_OLCUM_TETIK (global) → RunAutomationSequence(tabla)");
                _ = RunAutomationSequence(forceTablaIndex: true);
                return;
            }

            if (!isTrue) return;

            switch (changedVar.Name)
            {
                case "G_OLCUM_TETIK":
                    if (!_olcumTetikProcessing)
                        _ = HandleOlcumTetikAsync(robot, robotNo);
                    break;

                case "G_SNIFFER_OLCUM_TETIK":
                    if (!_snifferOlcumProcessing)
                        _ = HandleSnifferOlcumAsync(robot, robotNo);
                    break;
            }
        }

        /// <summary>
        /// G_JOB_INDEX değiştiğinde çağrılır. Gocator'a ilgili job'u hemen yükler.
        /// Robot ölçüm noktasına giderken paralel yükleme yapılır → süre kazanımı.
        /// </summary>
        private static async Task HandleJobIndexChangeAsync(string rawIndex, int robotNo)
        {
            try
            {
                TryParseIndex(rawIndex, out int idx);
                if (idx < 0) return;

                // Ölçüm devam ediyorsa ön-yükleme yapma (sensör meşgul)
                if (OlcumInProgress) return;

                string currentRfid = AktuelRfid;
                var recipe = KnownRfids.FirstOrDefault(r => r.Id == currentRfid);
                if (recipe?.JobSequence == null || idx >= recipe.JobSequence.Count) return;

                string jobName = recipe.JobSequence[idx];
                if (string.IsNullOrEmpty(jobName)) return;

                // ═══ SNIFFER SÜRESİ + SAPMA LİMİTİ ROBOTA GÖNDER (sayfa bağımsız) ═══
                UpdateSnifferDurationOutput(recipe, idx);
                UpdateDeviationLimitOutput(recipe, idx);
                OnAutomationLog?.Invoke($"[Robot {robotNo}] G_JOB_INDEX={idx} → Sniffer + Sapma limiti gönderildi");

                // ═══ GOCATOR JOB ÖN-YÜKLEME ═══
                // NOT: Cache kontrolü kaldırıldı — aynı job bile olsa yükle (0→0 durumu için)

                OnAutomationLog?.Invoke($"[Robot {robotNo}] G_JOB_INDEX={idx} → Job ön-yükleniyor: {jobName}...");
                bool loadOk = await GocatorJobLogic.LoadJob(jobName, s => OnAutomationLog?.Invoke($"[Gocator] {s}"));

                if (loadOk)
                {
                    _preLoadedGocatorJob = jobName;
                    OnAutomationLog?.Invoke($"[Robot {robotNo}] ✓ Job ön-yükleme tamamlandı: {jobName} (robot hareket ederken hazır)");
                }
                else
                {
                    _preLoadedGocatorJob = null;
                    OnAutomationLog?.Invoke($"[Robot {robotNo}] ⚠ Job ön-yükleme başarısız: {jobName}");
                }
            }
            catch (Exception ex)
            {
                _preLoadedGocatorJob = null;
                OnAutomationLog?.Invoke($"[Robot {robotNo}] ⚠ Job ön-yükleme hatası: {ex.Message}");
            }
        }

        // =====================================================
        // BİRLEŞİK ÖLÇÜM TETİK (GLOBAL)
        // Robot G_OLCUM_TETIK=TRUE → PC, G_JOB_INDEX'e göre ölçüm yapar
        //   JOB_INDEX=0 → Tabla ölçüm → G_TABLA_OFFSET_X..C yaz
        //   JOB_INDEX>0 → Boru ölçüm  → G_OFFSET_X..C yaz
        // Sinyal: G_OLCUM_DURUM (0=Bosta 1=Cekim 2=TamamOK 3=Hata)
        // =====================================================
        public static async Task HandleOlcumTetikAsync(KukaRobotInstance robot, int robotNo)
        {
            if (_olcumTetikProcessing) return;
            if (OlcumInProgress) return; // RunAutomationSequence zaten calisiyor

            _olcumTetikProcessing = true;
            OlcumInProgress = true;
            int jobIndex = 0;
            try
            {
                // 1. JOB_INDEX'i doğrudan robottan CANLI oku (cache'e güvenme — race condition önlenir)
                string rawJobIndex = null;
                try
                {
                    rawJobIndex = await robot.ReadVariableAsync("G_JOB_INDEX");
                }
                catch { }

                // Canlı okuma başarılıysa onu kullan, değilse cache'e düş
                if (!string.IsNullOrEmpty(rawJobIndex))
                {
                    TryParseIndex(rawJobIndex, out jobIndex);
                }
                else
                {
                    var jobIndexVar = robot.InputVars.FirstOrDefault(v => v.Name == "G_JOB_INDEX");
                    if (jobIndexVar != null) TryParseIndex(jobIndexVar.Value, out jobIndex);
                    rawJobIndex = jobIndexVar?.Value ?? "null";
                }

                OnAutomationLog?.Invoke($"[Robot {robotNo}] Ölçüm tetik alındı (JOB_INDEX={jobIndex}, raw=\"{rawJobIndex}\")");

                // ═══ TETİK ANINDA POZİSYON SNAPSHOT ═══
                // Robot tetik gönderdiğinde durağan — pozisyonu ŞİMDİ yakala.
                // Gocator ölçümü ~200-500ms sürer; bu süre boyunca cache değişmez
                // ama güvenlik için snapshot'ı en erken noktada alıyoruz.
                // Canlı okuma dene, başarısızsa cache kullan.
                KukaPose snapshotPose;
                try
                {
                    var tasks = new[]
                    {
                        robot.ReadVariableAsync("$POS_ACT.X"),
                        robot.ReadVariableAsync("$POS_ACT.Y"),
                        robot.ReadVariableAsync("$POS_ACT.Z"),
                        robot.ReadVariableAsync("$POS_ACT.A"),
                        robot.ReadVariableAsync("$POS_ACT.B"),
                        robot.ReadVariableAsync("$POS_ACT.C")
                    };
                    await Task.WhenAll(tasks);
                    double.TryParse(tasks[0].Result, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double sx);
                    double.TryParse(tasks[1].Result, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double sy);
                    double.TryParse(tasks[2].Result, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double sz);
                    double.TryParse(tasks[3].Result, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double sa);
                    double.TryParse(tasks[4].Result, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double sb);
                    double.TryParse(tasks[5].Result, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double sc);
                    snapshotPose = new KukaPose(sx, sy, sz, sa, sb, sc);
                    OnAutomationLog?.Invoke($"[Robot {robotNo}] Pozisyon snapshot (canlı): X={sx:F3} Y={sy:F3} Z={sz:F3} A={sa:F3} B={sb:F3} C={sc:F3}");
                }
                catch
                {
                    // Canlı okuma başarısız → cache'den al
                    snapshotPose = new KukaPose(robot.PosX, robot.PosY, robot.PosZ, robot.PosA, robot.PosB, robot.PosC);
                    OnAutomationLog?.Invoke($"[Robot {robotNo}] Pozisyon snapshot (cache): X={robot.PosX:F3} Y={robot.PosY:F3} Z={robot.PosZ:F3}");
                }

                // ═══ SEÇİLİ INDEX'İN SNIFFER SÜRESİ + NOKTA SAPMA LİMİTİ GÜNCELLE ═══
                UpdateCurrentJobIndex(jobIndex);

                // 2. Aktif RFID'den job adını bul
                string currentRfid = AktuelRfid;
                var recipe = KnownRfids.FirstOrDefault(r => r.Id == currentRfid);
                string jobName = null;

                if (recipe?.JobSequence != null && jobIndex >= 0 && jobIndex < recipe.JobSequence.Count)
                    jobName = recipe.JobSequence[jobIndex];

                if (string.IsNullOrEmpty(jobName))
                {
                    OnAutomationLog?.Invoke($"[Robot {robotNo}] Job bulunamadı (RFID={currentRfid}, Index={jobIndex})");
                    return;
                }

                // 4. Gocator job yükle (ÖN-YÜKLEME KONTROLÜ)
                // Devam eden ön-yükleme varsa bekle (sensör çakışmasını önle)
                try { await _jobPreLoadTask; } catch { }

                if (_preLoadedGocatorJob != null && _preLoadedGocatorJob == jobName)
                {
                    OnAutomationLog?.Invoke($"[Robot {robotNo}] Job zaten ön-yüklü: {jobName} → LoadJob atlandı (süre kazanımı)");
                    _preLoadedGocatorJob = null; // Kullanıldı, sıfırla
                }
                else
                {
                    _preLoadedGocatorJob = null; // Farklı job veya ilk çağrı
                    OnAutomationLog?.Invoke($"[Robot {robotNo}] Job yükleniyor: {jobName}");
                    bool loadOk = await GocatorJobLogic.LoadJob(jobName, s => OnAutomationLog?.Invoke($"[Gocator] {s}"));
                    if (!loadOk)
                    {
                        OnAutomationLog?.Invoke($"[Robot {robotNo}] Job yüklenemedi: {jobName}");
                        return;
                    }
                }

                // 5. Gocator'dan ölçüm al
                OnAutomationLog?.Invoke($"[Robot {robotNo}] Ölçüm alınıyor (Job: {jobName})...");
                var (status, results) = await ReceiveMeasurementLogic.ReceiveAndProcessMeasurements(
                    s => OnAutomationLog?.Invoke($"[Gocator] {s}"), null);

                bool olcumBasarili = (status == 1 && results != null && results.Count > 0);

                if (olcumBasarili)
                {
                    if (jobIndex == 0)
                    {
                        // --- TABLA ÖLÇÜM (JOB 0) ---

                        // 1. Tabla ölçüm tablosunu güncelle + Tabla aktarım tablosunu doldur
                        try
                        {
                            double[] tablaVals = new double[results.Count];
                            for (int i = 0; i < results.Count; i++) tablaVals[i] = results[i].Value;

                            await PlcService.Instance.RunOnUiAsync(() =>
                            {
                                TablaLastMeasurements.Clear();
                                foreach (var m in results) TablaLastMeasurements.Add(m);
                                SaveTablaMeasurements();
                                LastMeasurements.Clear();
                                SaveMeasurements();

                                // Tabla aktarım tablosunu doldur (TablaTransferRows — save otomatik)
                                UpdatePlcTransferRowsFromValues(tablaVals, TablaTransferRows);
                            });
                        }
                        catch { }

                        // 2. Tabla aktarım tablosundaki tag seçimlerine göre robota yaz
                        for (int i = 0; i < Math.Min(results.Count, TablaTransferRows.Count); i++)
                        {
                            string tag = TablaTransferRows[i].SelectedTag;
                            if (!string.IsNullOrEmpty(tag))
                            {
                                await WriteToAllRobotsAsync(tag, results[i].Value.ToString("F3"));
                            }
                        }

                        await WriteToAllRobotsAsync("G_TABLA_OFFSET_HAZIR", "TRUE");
                        OnAutomationLog?.Invoke($"[Robot {robotNo}] Tabla ölçüm OK - {results.Count} offset yazıldı (tag tabanlı)");

                        // Aktif istasyonun tabla kaçıklık UI'ını güncelle
                        UpdateStationTablaOffsets(results);
                    }
                    else
                    {
                        // --- BORU ÖLÇÜM (JOB 1..N) ---

                        // HAM VERİ TABLOSUNU GÜNCELLE (her zaman, veri izleme için)
                        try
                        {
                            await PlcService.Instance.RunOnUiAsync(() =>
                            {
                                LastMeasurements.Clear();
                                foreach (var m in results) LastMeasurements.Add(m);
                                SaveMeasurements();
                            });
                        }
                        catch { }

                        string[] boruOffsets = { "G_OFFSET_X", "G_OFFSET_Y", "G_OFFSET_Z",
                                                 "G_OFFSET_A", "G_OFFSET_B", "G_OFFSET_C" };

                        // Per-job ölçüm yöntemi (reçetede tanımlı, yoksa global fallback)
                        string jobDataMode = (recipe.DataSourceModes != null && jobIndex < recipe.DataSourceModes.Count)
                            ? recipe.DataSourceModes[jobIndex] : DataSourceMode;

                        // ═══ ÇOKLU NOKTA DESTEĞİ: Her 6 GDP değeri = 1 ölçüm noktası ═══
                        int pointCount = (results.Count + 5) / 6;

                        if (jobDataMode == "CODESYS")
                        {
                            // CODESYS matematik fonksiyonu ile hedef hesapla (çoklu nokta döngüsü)
                            try
                            {
                                var codesysCalc = new CodesysMathFunction
                                {
                                    OffsetX = CodesysOffsetX,
                                    OffsetY = CodesysOffsetY,
                                    OffsetZ = CodesysOffsetZ,
                                    IncludeABC = BoruAbcDahil
                                };
                                var mappingParts = (CodesysGocMappings ?? "0,1,2,3,4,5").Split(',');
                                if (mappingParts.Length >= 1 && int.TryParse(mappingParts[0], out int mx)) codesysCalc.MapIndexX = mx;
                                if (mappingParts.Length >= 2 && int.TryParse(mappingParts[1], out int my)) codesysCalc.MapIndexY = my;
                                if (mappingParts.Length >= 3 && int.TryParse(mappingParts[2], out int mz)) codesysCalc.MapIndexZ = mz;
                                if (mappingParts.Length >= 4 && int.TryParse(mappingParts[3], out int ma)) codesysCalc.MapIndexYaw = ma;
                                if (mappingParts.Length >= 5 && int.TryParse(mappingParts[4], out int mr)) codesysCalc.MapIndexRoll = mr;
                                if (mappingParts.Length >= 6 && int.TryParse(mappingParts[5], out int mp)) codesysCalc.MapIndexPitch = mp;

                                // Tetik anında alınan snapshot pozisyonunu kullan (cache değil)
                                var robotPose = snapshotPose;
                                var allTargetValues = new List<double>();
                                var allTargetPoses = new List<KukaPose>();
                                bool allSuccess = true;

                                // Her nokta için CODESYS hesapla
                                for (int pt = 0; pt < pointCount; pt++)
                                {
                                    int startIdx = pt * 6;
                                    int count = Math.Min(6, results.Count - startIdx);
                                    var pointValues = new double[count];
                                    for (int gi = 0; gi < count; gi++)
                                        pointValues[gi] = results[startIdx + gi].Value;

                                    var target = codesysCalc.CalculateFromArray(pointValues, robotPose);

                                    if (codesysCalc.LastCalculationSuccess)
                                    {
                                        allTargetPoses.Add(target);
                                        allTargetValues.AddRange(new[] { target.X, target.Y, target.Z,
                                                                         target.A, target.B, target.C });
                                    }
                                    else
                                    {
                                        allSuccess = false;
                                        OnAutomationLog?.Invoke($"[Robot {robotNo}] CODESYS hesaplama başarısız (Nokta {pt + 1})");
                                        break;
                                    }
                                }

                                if (allSuccess && allTargetValues.Count > 0)
                                {
                                    double[] targetVals = allTargetValues.ToArray();

                                    // İlk noktanın 6 değerini mevcut G_OFFSET_X..C'ye yaz (geriye uyumlu)
                                    for (int i = 0; i < Math.Min(6, targetVals.Length); i++)
                                        await WriteToAllRobotsAsync(boruOffsets[i], targetVals[i].ToString("F3"));

                                    // ═══ UI TABLOLARI GÜNCELLE (HEDEF NOKTA + BORU AKTARIM) ═══
                                    try
                                    {
                                        await PlcService.Instance.RunOnUiAsync(() =>
                                        {
                                            UpdateCodesysTargetResultsMultiPoint(allTargetPoses);
                                            UpdatePlcTransferRowsFromValues(targetVals, PlcTransferRows);
                                        });
                                    }
                                    catch { }

                                    OnAutomationLog?.Invoke($"[Robot {robotNo}] Boru ölçüm OK (Job {jobIndex}) - CODESYS {pointCount} nokta hesaplandı");
                                }
                                else
                                {
                                    // Fallback: tüm ham değerleri yaz
                                    OnAutomationLog?.Invoke($"[Robot {robotNo}] CODESYS başarısız, tüm ham değerler yazılıyor");
                                    double[] rawVals = new double[results.Count];
                                    for (int i = 0; i < rawVals.Length; i++)
                                        rawVals[i] = results[i].Value;
                                    for (int i = 0; i < Math.Min(rawVals.Length, boruOffsets.Length); i++)
                                        await WriteToAllRobotsAsync(boruOffsets[i], rawVals[i].ToString("F3"));
                                    try { await PlcService.Instance.RunOnUiAsync(() => UpdatePlcTransferRowsFromValues(rawVals, PlcTransferRows)); } catch { }
                                }
                            }
                            catch (Exception exCod)
                            {
                                OnAutomationLog?.Invoke($"[Robot {robotNo}] CODESYS hatası: {exCod.Message}, ham değerler yazılıyor");
                                double[] rawVals = new double[results.Count];
                                for (int i = 0; i < rawVals.Length; i++)
                                    rawVals[i] = results[i].Value;
                                for (int i = 0; i < Math.Min(rawVals.Length, boruOffsets.Length); i++)
                                    _ = WriteToAllRobotsAsync(boruOffsets[i], rawVals[i].ToString("F3"));
                                try { await PlcService.Instance.RunOnUiAsync(() => UpdatePlcTransferRowsFromValues(rawVals, PlcTransferRows)); } catch { }
                            }
                        }
                        else if (jobDataMode == "HAND_EYE" && CalibrationService.Instance.IsCalibrated && results.Count >= 3)
                        {
                            // Hand-Eye kalibrasyon ile sensor → base dönüşümü (çoklu nokta döngüsü)
                            var allBaseValues = new List<double>();
                            var allBasePoses = new List<KukaPose>();
                            bool allHandEyeSuccess = true;

                            for (int pt = 0; pt < pointCount; pt++)
                            {
                                int startIdx = pt * 6;
                                int count = Math.Min(6, results.Count - startIdx);
                                if (count < 3) { allHandEyeSuccess = false; break; }

                                double gocX = results[startIdx].Value;
                                double gocY = results[startIdx + 1].Value;
                                double gocZ = results[startIdx + 2].Value;
                                double gocA = count > 3 ? results[startIdx + 3].Value : 0;
                                double gocB = count > 4 ? results[startIdx + 4].Value : 0;
                                double gocC = count > 5 ? results[startIdx + 5].Value : 0;

                                var sensorTarget = new KukaPose(gocX, gocY, gocZ, gocA, gocB, gocC).ToMatrix();
                                var basePose = await CalibrationService.Instance.LocateFromRobotAsync(robot, sensorTarget, userBaseNo: 1);

                                if (basePose != null)
                                {
                                    allBasePoses.Add(basePose);
                                    allBaseValues.AddRange(new[] { basePose.X, basePose.Y, basePose.Z,
                                                                    basePose.A, basePose.B, basePose.C });
                                }
                                else
                                {
                                    allHandEyeSuccess = false;
                                    OnAutomationLog?.Invoke($"[Robot {robotNo}] HandEye dönüşüm başarısız (Nokta {pt + 1})");
                                    break;
                                }
                            }

                            if (allHandEyeSuccess && allBaseValues.Count > 0)
                            {
                                double[] baseVals = allBaseValues.ToArray();

                                // UI'da dönüştürülmüş verileri göster (çoklu nokta)
                                try
                                {
                                    await PlcService.Instance.RunOnUiAsync(() =>
                                    {
                                        TransformedMeasurements.Clear();
                                        string[] axNames = { "X", "Y", "Z", "A", "B", "C" };
                                        string[] units = { "mm", "mm", "mm", "°", "°", "°" };
                                        int id = 1;
                                        for (int pt = 0; pt < allBasePoses.Count; pt++)
                                        {
                                            var bp = allBasePoses[pt];
                                            double[] vals = { bp.X, bp.Y, bp.Z, bp.A, bp.B, bp.C };
                                            for (int ax = 0; ax < 6; ax++)
                                            {
                                                TransformedMeasurements.Add(new GocatorMeasurement
                                                {
                                                    Id = id++, PointIndex = pt, SourceId = pt * 6 + ax,
                                                    IsFirstInPoint = (ax == 0),
                                                    Name = $"Nokta {pt + 1} Base {axNames[ax]}",
                                                    Value = Math.Round(vals[ax], 3), Unit = units[ax], Decision = "OK"
                                                });
                                            }
                                        }
                                    });
                                }
                                catch { }

                                // İlk noktanın 6 değerini G_OFFSET_X..C'ye yaz
                                for (int i = 0; i < Math.Min(6, baseVals.Length); i++)
                                    await WriteToAllRobotsAsync(boruOffsets[i], baseVals[i].ToString("F3"));

                                // ═══ BORU AKTARIM TABLOSU GÜNCELLE ═══
                                try { await PlcService.Instance.RunOnUiAsync(() => UpdatePlcTransferRowsFromValues(baseVals, PlcTransferRows)); } catch { }

                                OnAutomationLog?.Invoke($"[Robot {robotNo}] Boru ölçüm OK (Job {jobIndex}) - HandEye {pointCount} nokta dönüştürüldü");
                            }
                            else
                            {
                                OnAutomationLog?.Invoke($"[Robot {robotNo}] UYARI: HandEye dönüşüm başarısız, ham değerler yazılıyor");
                                double[] rawVals = new double[results.Count];
                                for (int i = 0; i < rawVals.Length; i++)
                                    rawVals[i] = results[i].Value;
                                for (int i = 0; i < Math.Min(rawVals.Length, boruOffsets.Length); i++)
                                    await WriteToAllRobotsAsync(boruOffsets[i], rawVals[i].ToString("F3"));
                                try { await PlcService.Instance.RunOnUiAsync(() => UpdatePlcTransferRowsFromValues(rawVals, PlcTransferRows)); } catch { }
                            }
                        }
                        else
                        {
                            // SENSOR (ham veri) modu: tüm değerleri yaz
                            double[] rawVals = new double[results.Count];
                            for (int i = 0; i < rawVals.Length; i++)
                                rawVals[i] = results[i].Value;
                            // İlk 6 değeri G_OFFSET_X..C'ye yaz (geriye uyumlu)
                            for (int i = 0; i < Math.Min(rawVals.Length, boruOffsets.Length); i++)
                                await WriteToAllRobotsAsync(boruOffsets[i], rawVals[i].ToString("F3"));
                            try { await PlcService.Instance.RunOnUiAsync(() => UpdatePlcTransferRowsFromValues(rawVals, PlcTransferRows)); } catch { }

                            OnAutomationLog?.Invoke($"[Robot {robotNo}] Boru ölçüm OK (Job {jobIndex}) - {results.Count} ham offset yazıldı ({pointCount} nokta)");
                        }
                    }

                    // ═══ TAMAM SİNYALİ: CODESYS hesaplandı + offset robota yazıldı → TAMAM gönder ═══
                    string tamamSignal = jobIndex == 0 ? "G_TABLA_OLCUM_TAMAM" : "G_BORU_OLCUM_TAMAM";
                    await WriteToAllRobotsAsync(tamamSignal, "TRUE");
                    OnAutomationLog?.Invoke($"[Robot {robotNo}] {tamamSignal} = TRUE (offset yazıldı, hesap tamam)");

                    // Durum bilgisi güncelle (Kamera sayfası status bar)
                    if (jobIndex == 0)
                    {
                        TablaOlcumDurum = $"Tabla ölçüm: BASE 2 + TOOL 2 ile yapıldı (Poz: X={snapshotPose.X:F1} Y={snapshotPose.Y:F1} Z={snapshotPose.Z:F1})";
                        TablaOlcumBasarili = true;
                        SetTablaMeasurementSignal();
                        OnAutomationLog?.Invoke($"[Robot {robotNo}] Tabla aktarım tamamlandı → TABLA_OFFSET_TAMAM = 1");
                    }
                    else
                    {
                        BoruOlcumDurum = $"Boru ölçüm: BASE 1 + TOOL 2 ile yapıldı (Poz: X={snapshotPose.X:F1} Y={snapshotPose.Y:F1} Z={snapshotPose.Z:F1})";
                        BoruOlcumBasarili = true;
                        SetMeasurementSignal();
                    }
                }
                else
                {
                    OnAutomationLog?.Invoke($"[Robot {robotNo}] Ölçüm BAŞARISIZ (Job {jobIndex}: {jobName}) — TAMAM GÖNDERİLMEDİ");
                    _preLoadedGocatorJob = null;
                }
            }
            catch (Exception ex)
            {
                OnAutomationLog?.Invoke($"[Robot {robotNo}] Ölçüm tetik hatası: {ex.Message} — TAMAM GÖNDERİLMEDİ");
                _preLoadedGocatorJob = null;
            }
            finally
            {
                // Sadece temizlik — TAMAM sinyali artık burada DEĞİL, başarı bloğunda
                _olcumTetikProcessing = false;
                OlcumInProgress = false;
            }
        }

        // =====================================================
        // SNIFFER LEAK MONİTÖR — Sürekli PLC izleme (otomasyon aktifken)
        // INFICON_LEAK edge detection → aktif noktaya NOK/OK yaz
        // =====================================================
        private static bool _prevInficon1Leak = false;
        private static bool _prevInficon2Leak = false;
        private static System.Threading.Timer _snifferMonitorTimer;

        public static void StartSnifferMonitor()
        {
            _prevInficon1Leak = false;
            _prevInficon2Leak = false;
            _snifferMonitorTimer?.Dispose();
            _snifferMonitorTimer = new System.Threading.Timer(_ => CheckSnifferLeaks(), null, 0, 200);
            OnAutomationLog?.Invoke("[Sniffer] Kaçak monitörü başlatıldı (200ms döngü)");
        }

        public static void StopSnifferMonitor()
        {
            _snifferMonitorTimer?.Dispose();
            _snifferMonitorTimer = null;
            OnAutomationLog?.Invoke("[Sniffer] Kaçak monitörü durduruldu");
        }

        private static void CheckSnifferLeaks()
        {
            if (!IsProcessRunning) return;
            try
            {
                CheckSnifferLeakForRobot(1, ref _prevInficon1Leak);
                CheckSnifferLeakForRobot(2, ref _prevInficon2Leak);
            }
            catch { }
        }

        private static void CheckSnifferLeakForRobot(int robotNo, ref bool prevLeak)
        {
            var leakVar = GeneralInputVars.FirstOrDefault(v => v.Name == $"INFICON{robotNo}_LEAK");
            if (leakVar == null) return;

            string val = leakVar.Value ?? leakVar.CurrentValue?.ToString() ?? "0";
            bool currentLeak = (val == "1" || val.Equals("true", StringComparison.OrdinalIgnoreCase));

            if (currentLeak && !prevLeak)
            {
                // Yükselen kenar: LEAK başladı → NOK
                string noktaAdi = GetSnifferAktifNoktaAdi(robotNo);
                double leakRate = GetSnifferLeakRate(robotNo);
                if (!string.IsNullOrEmpty(noktaAdi))
                {
                    var collection = robotNo == 1 ? Robot1SnifferPoints : Robot2SnifferPoints;
                    UpdateSnifferPoint(collection, robotNo, noktaAdi, "NOK", leakRate);
                }
            }
            else if (!currentLeak && prevLeak)
            {
                // Düşen kenar: LEAK temizlendi → OK
                string noktaAdi = GetSnifferAktifNoktaAdi(robotNo);
                double leakRate = GetSnifferLeakRate(robotNo);
                if (!string.IsNullOrEmpty(noktaAdi))
                {
                    var collection = robotNo == 1 ? Robot1SnifferPoints : Robot2SnifferPoints;
                    UpdateSnifferPoint(collection, robotNo, noktaAdi, "OK", leakRate);
                }
            }
            prevLeak = currentLeak;
        }

        private static string GetSnifferAktifNoktaAdi(int robotNo)
        {
            var robots = KukaRobotManager.Instance?.Robots;
            if (robots == null || robots.Count < robotNo) return "";
            var robot = robots[robotNo - 1];
            var v = robot.InputVars.FirstOrDefault(x => x.Name == "G_AKTIF_NOKTA_NO");
            if (v != null && int.TryParse(v.Value, out int no) && no > 0) return $"Nokta {no}";
            return "";
        }

        private static double GetSnifferLeakRate(int robotNo)
        {
            var v = GeneralInputVars.FirstOrDefault(x => x.Name == $"INFICON{robotNo}_LEAKRATE");
            if (v != null && double.TryParse(v.Value ?? v.CurrentValue?.ToString(),
                System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double rate)) return rate;
            return 0.0;
        }

        private static void RunOnUiThread(Microsoft.UI.Dispatching.DispatcherQueueHandler action)
        {
            var window = ((App)Microsoft.UI.Xaml.Application.Current)?.MainWindow ?? App.m_window;
            var dispatcher = window?.DispatcherQueue;
            if (dispatcher != null)
                dispatcher.TryEnqueue(action);
            else
                action(); // fallback: dogrudan calistir
        }

        public static void UpdateSnifferPoint(ObservableCollection<App4.Models.SnifferPointResult> collection,
            int robotNo, string pointName, string result, double leakRate)
        {
            RunOnUiThread(() =>
            {
                var existing = collection.FirstOrDefault(p => p.PointName == pointName);
                if (existing != null)
                {
                    existing.Result = result;
                    existing.LeakRate = leakRate;
                    existing.Timestamp = DateTime.Now;
                }
                else
                {
                    collection.Add(new App4.Models.SnifferPointResult
                    {
                        PointName = pointName,
                        Result = result,
                        LeakRate = leakRate,
                        Timestamp = DateTime.Now
                    });
                }
            });
            OnAutomationLog?.Invoke($"[Sniffer] Robot{robotNo} {pointName}: {result} (LeakRate={leakRate:E2})");
        }

        public static void ClearSnifferPoints()
        {
            RunOnUiThread(() =>
            {
                Robot1SnifferPoints.Clear();
                Robot2SnifferPoints.Clear();
            });
        }

        // =====================================================
        // INFICON SNIFFER ÖLÇÜM (GLOBAL)
        // Robot G_SNIFFER_OLCUM_TETIK=TRUE → INFICON ölçüm → G_SNIFFER_OK + G_SNIFFER_TAMAM
        // =====================================================
        public static async Task HandleSnifferOlcumAsync(KukaRobotInstance robot, int robotNo)
        {
            _snifferOlcumProcessing = true;
            try
            {
                var noktaAdiVar = robot.InputVars.FirstOrDefault(v => v.Name == "G_AKTIF_NOKTA_NO");
                string aktifInfo = noktaAdiVar?.Value?.Trim('"', ' ') ?? "";

                OnAutomationLog?.Invoke($"[Robot {robotNo}] INFICON sniffer ölçüm isteği alındı ({aktifInfo})");

                // Sniffer stabilizasyon süresi
                double snifferSure = 0;
                var sureVar = robot.InputVars.FirstOrDefault(v => v.Name == "G_SNIFFER_SURE");
                if (sureVar != null) double.TryParse(sureVar.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out snifferSure);
                if (snifferSure > 0)
                {
                    OnAutomationLog?.Invoke($"[Robot {robotNo}] INFICON stabilizasyon bekleniyor ({snifferSure:F0}ms)");
                    await Task.Delay((int)Math.Min(snifferSure, 30000));
                }

                // INFICON ölçüm — PLC'den LEAK sinyali oku
                var leakVar = GeneralInputVars.FirstOrDefault(v => v.Name == $"INFICON{robotNo}_LEAK");
                bool isLeak = false;
                if (leakVar != null)
                {
                    string val = leakVar.Value ?? leakVar.CurrentValue?.ToString() ?? "0";
                    isLeak = (val == "1" || val.Equals("true", StringComparison.OrdinalIgnoreCase));
                }
                bool olcumOK = !isLeak;
                double olcumDeger = 0.0;
                var leakRateVar = GeneralInputVars.FirstOrDefault(v => v.Name == $"INFICON{robotNo}_LEAKRATE");
                if (leakRateVar != null)
                    double.TryParse(leakRateVar.Value ?? leakRateVar.CurrentValue?.ToString(),
                        System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out olcumDeger);

                OnAutomationLog?.Invoke($"[Robot {robotNo}] INFICON sonuç: {(olcumOK ? "OK" : "NOK")} (Değer={olcumDeger:F4})");

                await WriteToAllRobotsAsync("G_SNIFFER_OK", olcumOK ? "TRUE" : "FALSE");
                await WriteToAllRobotsAsync("G_SNIFFER_DEGER", olcumDeger.ToString("F4", System.Globalization.CultureInfo.InvariantCulture));
                await WriteToAllRobotsAsync("G_SNIFFER_TAMAM", "TRUE");
            }
            catch (Exception ex)
            {
                OnAutomationLog?.Invoke($"[Robot {robotNo}] INFICON sniffer hatası: {ex.Message}");
                try
                {
                    await WriteToAllRobotsAsync("G_SNIFFER_OK", "FALSE");
                    await WriteToAllRobotsAsync("G_SNIFFER_TAMAM", "TRUE");
                }
                catch { }
            }
            finally
            {
                _snifferOlcumProcessing = false;
            }
        }

        // ════════════════════════════════════════════════════════════
        // YÜKSELEN KENAR TETİK — FALSE→TRUE geçişinde çalışır.
        // Robot tetik=TRUE yazar → ölçüm başlar.
        // Robot tetik=FALSE yazana kadar yeni tetik kabul edilmez.
        // ════════════════════════════════════════════════════════════

        private static bool _boruTriggerArmed = true;   // FALSE geldi mi? (ilk tetik için true)
        private static bool _tablaTriggerArmed = true;
        private static Dictionary<int, int> _noktaNoPrev = new(); // Robot bazlı önceki G_AKTIF_NOKTA_NO
        private static Dictionary<int, bool> _tablaOlcumTamamFlags = new(); // Robot bazlı TABLA_OLCUM_TAMAM durumu
        private static Dictionary<int, bool> _boruOlcumTamamFlags = new(); // Robot bazlı BORU_OLCUM_TAMAM durumu

        private static void TriggerVar_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Value" || e.PropertyName == "CurrentValue")
            {
                var plcVar = sender as PlcVariable;
                if (plcVar == null) return;

                bool isHigh = plcVar.Value == "1" || plcVar.Value?.ToLower() == "true";

                if (!isHigh)
                {
                    // FALSE geldi → sonraki TRUE için hazır
                    _boruTriggerArmed = true;
                    return;
                }

                // TRUE geldi — ama armed değilse (henüz FALSE gelmemiş) → atla
                if (!_boruTriggerArmed) return;
                _boruTriggerArmed = false; // Tetik kabul edildi, FALSE gelene kadar kilitle

                _ = RunAutomationSequence();
            }
        }

        /// <summary>
        /// Tabla kaçıklık tetik handler'ı — yükselen kenar.
        /// G_JOB_INDEX okumadan direkt idx=0 (tabla) akışını başlatır.
        /// </summary>
        private static void TriggerVar2_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Value" || e.PropertyName == "CurrentValue")
            {
                var plcVar = sender as PlcVariable;
                if (plcVar == null) return;

                bool isHigh = plcVar.Value == "1" || plcVar.Value?.ToLower() == "true";

                if (!isHigh)
                {
                    _tablaTriggerArmed = true;
                    return;
                }

                if (!_tablaTriggerArmed) return;
                _tablaTriggerArmed = false;

                _ = RunAutomationSequence(forceTablaIndex: true);
            }
        }

        /// <summary>
        /// G_JOB_INDEX değiştiğinde CurrentJobIndex + NOKTA_SAPMA_LIMIT + SNIFFER_OLCUM_SURE senkronize eder.
        /// Camera sayfası kapalı olsa bile çalışır (GlobalData seviyesinde dinleme).
        /// </summary>
        private static void IndexVar_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Value" || e.PropertyName == "CurrentValue")
            {
                var plcVar = sender as PlcVariable;
                if (plcVar != null && TryParseIndex(plcVar.Value, out int newIdx))
                {
                    OnAutomationLog?.Invoke($"[IndexWatcher] G_JOB_INDEX değişti: {newIdx} (raw=\"{plcVar.Value}\")");
                    UpdateCurrentJobIndex(newIdx);
                }
            }
        }

        // ▼▼▼ KAMERA ÖLÇÜM SİNYALLERİ ▼▼▼

        /// <summary>
        /// Ölçüm başlatıldığında çağrılır - output sinyalini 0'a düşürür
        /// </summary>
        public static async void ResetMeasurementSignal()
        {
            try
            {
                string targetTag = MeasurementOutputTag;
                // Default tag (İlk output)
                if (string.IsNullOrEmpty(targetTag))
                {
                    var defaultOutput = GeneralOutputVars.FirstOrDefault(v => 
                        v.Name.Contains("MEASUREMENT") || v.Name.Contains("TRIGGER"));
                    if (defaultOutput != null) targetTag = defaultOutput.Name;
                    else return;
                }

                bool found = false;

                // 1. Try GeneralOutputVars
                var outputVar = GeneralOutputVars.FirstOrDefault(v => v.Name == targetTag);
                if (outputVar != null)
                {
                    outputVar.CurrentValue = 0;
                    found = true;
                }
                
                // 2. Try GeneralInputVars (Simulation Input)
                var inputVar = GeneralInputVars.FirstOrDefault(v => v.Name == targetTag);
                if (inputVar != null)
                {
                    inputVar.CurrentValue = 0;
                    found = true;
                }

                // 3. Try PlcService (Real PLC)
                if (PlcService.Instance != null)
                {
                    var plcVar = PlcService.Instance.OutputVariables.FirstOrDefault(v => v.Name == targetTag);
                    if (plcVar != null)
                    {
                        await PlcService.Instance.WriteAsync(plcVar, 0);
                        plcVar.CurrentValue = 0;
                        found = true;
                    }
                    else
                    {
                        // Check Input list too (writable)
                        var plcIn = PlcService.Instance.InputVariables.FirstOrDefault(v => v.Name == targetTag);
                        if (plcIn != null)
                        {
                            await PlcService.Instance.WriteAsync(plcIn, 0);
                            plcIn.CurrentValue = 0;
                            found = true;
                        }
                    }
                }

                // 4. Robot değişkenleri — GlobalData şablonu (in-memory)
                // ★ KUKA VarProxy BOOL → "FALSE" string kullan (int 0 değil)
                var robotOutVar = RobotOutputVars.FirstOrDefault(v => v.Name == targetTag);
                if (robotOutVar != null) robotOutVar.Value = "FALSE";
                var robotInVar = RobotInputVars.FirstOrDefault(v => v.Name == targetTag);
                if (robotInVar != null) robotInVar.Value = "FALSE";

                // 4b. Robot Instance Variables (CANLI - UI watcher bu nesneye abone)
                var robots = KukaRobotManager.Instance?.Robots;
                if (robots != null)
                {
                    foreach (var robot in robots)
                    {
                        var liveOut = robot.OutputVars.FirstOrDefault(v => v.Name == targetTag);
                        if (liveOut != null) liveOut.CurrentValue = "FALSE";
                        var liveIn = robot.InputVars.FirstOrDefault(v => v.Name == targetTag);
                        if (liveIn != null) liveIn.CurrentValue = "FALSE";
                    }
                }

                // ★★★ 5. MUTLAKA TÜM ROBOTLARA TCP İLE YAZ ★★★
                try { await WriteToAllRobotsAsync(targetTag, "FALSE"); } catch { }

                BoruSignalActive = false;
                OnAutomationStatusChanged?.Invoke();
            }
            catch (Exception ex)
            {
                OnAutomationLog?.Invoke($"✗ Sinyal sıfırlama hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Yeni ölçüm verisi geldiğinde çağrılır - output sinyalini 1'e koyar
        /// </summary>
        public static async void SetMeasurementSignal()
        {
            try
            {
                string targetTag = MeasurementOutputTag;
                // Default tag (İlk output)
                if (string.IsNullOrEmpty(targetTag))
                {
                    var defaultOutput = GeneralOutputVars.FirstOrDefault(v => 
                        v.Name.Contains("MEASUREMENT") || v.Name.Contains("TRIGGER"));
                    if (defaultOutput != null) targetTag = defaultOutput.Name;
                    else 
                    {
                        OnAutomationLog?.Invoke("⚠ SetSignal: Tag seçili değil.");
                        return;
                    }
                }

                bool found = false;

                // 1. Try GeneralOutputVars
                var outputVar = GeneralOutputVars.FirstOrDefault(v => v.Name == targetTag);
                if (outputVar != null)
                {
                    outputVar.CurrentValue = 1;
                    found = true;
                }
                
                // 2. Try GeneralInputVars (Simulation Input)
                var inputVar = GeneralInputVars.FirstOrDefault(v => v.Name == targetTag);
                if (inputVar != null)
                {
                    inputVar.CurrentValue = 1;
                    found = true;
                }

                // 3. Try PlcService (Real PLC)
                if (PlcService.Instance != null)
                {
                    var plcVar = PlcService.Instance.OutputVariables.FirstOrDefault(v => v.Name == targetTag);
                    if (plcVar != null)
                    {
                        await PlcService.Instance.WriteAsync(plcVar, 1);
                        plcVar.CurrentValue = 1;
                        found = true;
                    }
                    else
                    {
                        // Check Input list too (writable)
                        var plcIn = PlcService.Instance.InputVariables.FirstOrDefault(v => v.Name == targetTag);
                        if(plcIn != null)
                        {
                            await PlcService.Instance.WriteAsync(plcIn, 1);
                            plcIn.CurrentValue = 1;
                            found = true;
                        }
                    }
                }

                // 4. Robot değişkenleri — GlobalData şablonu (in-memory)
                // ★ KUKA VarProxy BOOL → "TRUE"/"FALSE" string kullan (int 1/0 DEĞİL!)
                var robotOutVar = RobotOutputVars.FirstOrDefault(v => v.Name == targetTag);
                if (robotOutVar != null) robotOutVar.Value = "TRUE";
                var robotInVar = RobotInputVars.FirstOrDefault(v => v.Name == targetTag);
                if (robotInVar != null) robotInVar.Value = "TRUE";

                // 4b. Robot Instance Variables (CANLI - UI watcher bu nesneye abone)
                var robots = KukaRobotManager.Instance?.Robots;
                if (robots != null)
                {
                    foreach (var robot in robots)
                    {
                        var liveOut = robot.OutputVars.FirstOrDefault(v => v.Name == targetTag);
                        if (liveOut != null) liveOut.CurrentValue = "TRUE";
                        var liveIn = robot.InputVars.FirstOrDefault(v => v.Name == targetTag);
                        if (liveIn != null) liveIn.CurrentValue = "TRUE";
                    }
                }

                // ★★★ 5. MUTLAKA TÜM ROBOTLARA TCP İLE YAZ ★★★
                // Koleksiyonda bulunmasa bile doğrudan robot TCP'sine yazılır
                try { await WriteToAllRobotsAsync(targetTag, "TRUE"); } catch { }

                BoruSignalActive = true;
                OnAutomationLog?.Invoke($"✓ Ölçüm sinyali gönderildi: {targetTag} = TRUE (BoruSignalActive=true)");
                OnAutomationStatusChanged?.Invoke();
            }
            catch (Exception ex)
            {
                OnAutomationLog?.Invoke($"✗ Sinyal ayarlama hatası: {ex.Message}");
            }
        }

        // ▼▼▼ TABLA ÖLÇÜM SİNYALLERİ ▼▼▼

        public static async void ResetTablaMeasurementSignal()
        {
            try
            {
                string targetTag = TablaOutputTag;
                if (string.IsNullOrEmpty(targetTag)) return;

                var outputVar = GeneralOutputVars.FirstOrDefault(v => v.Name == targetTag);
                if (outputVar != null) outputVar.CurrentValue = 0;
                var inputVar = GeneralInputVars.FirstOrDefault(v => v.Name == targetTag);
                if (inputVar != null) inputVar.CurrentValue = 0;

                if (PlcService.Instance != null)
                {
                    var plcVar = PlcService.Instance.OutputVariables.FirstOrDefault(v => v.Name == targetTag);
                    if (plcVar != null) { await PlcService.Instance.WriteAsync(plcVar, 0); plcVar.CurrentValue = 0; }
                    else
                    {
                        var plcIn = PlcService.Instance.InputVariables.FirstOrDefault(v => v.Name == targetTag);
                        if (plcIn != null) { await PlcService.Instance.WriteAsync(plcIn, 0); plcIn.CurrentValue = 0; }
                    }
                }

                // ★ KUKA VarProxy BOOL → "FALSE" string kullan (int 0 değil)
                var robotOutVar = RobotOutputVars.FirstOrDefault(v => v.Name == targetTag);
                if (robotOutVar != null) robotOutVar.Value = "FALSE";
                var robotInVar = RobotInputVars.FirstOrDefault(v => v.Name == targetTag);
                if (robotInVar != null) robotInVar.Value = "FALSE";

                // 4b. Robot Instance Variables (CANLI - UI watcher bu nesneye abone)
                var robots = KukaRobotManager.Instance?.Robots;
                if (robots != null)
                {
                    foreach (var robot in robots)
                    {
                        var liveOut = robot.OutputVars.FirstOrDefault(v => v.Name == targetTag);
                        if (liveOut != null) liveOut.CurrentValue = "FALSE";
                        var liveIn = robot.InputVars.FirstOrDefault(v => v.Name == targetTag);
                        if (liveIn != null) liveIn.CurrentValue = "FALSE";
                    }
                }

                // ★★★ MUTLAKA TÜM ROBOTLARA TCP İLE YAZ ★★★
                try { await WriteToAllRobotsAsync(targetTag, "FALSE"); } catch { }

                // ★★★ ROBOT 2 İÇİN AYRI TAG SIFIRLAMA ★★★
                string targetTag2 = TablaOutputTag2;
                if (!string.IsNullOrEmpty(targetTag2) && targetTag2 != targetTag)
                {
                    try { await WriteToAllRobotsAsync(targetTag2, "FALSE"); } catch { }
                }

                TablaSignalActive = false;
                OnAutomationStatusChanged?.Invoke();
            }
            catch (Exception ex)
            {
                OnAutomationLog?.Invoke($"✗ Tabla sinyal sıfırlama hatası: {ex.Message}");
            }
        }

        public static async void SetTablaMeasurementSignal()
        {
            try
            {
                string targetTag = TablaOutputTag;
                if (string.IsNullOrEmpty(targetTag))
                {
                    OnAutomationLog?.Invoke("⚠ SetTablaSignal: Tag seçili değil.");
                    return;
                }

                // 1. GeneralOutputVars / InputVars (in-memory)
                var outputVar = GeneralOutputVars.FirstOrDefault(v => v.Name == targetTag);
                if (outputVar != null) outputVar.CurrentValue = 1;
                var inputVar = GeneralInputVars.FirstOrDefault(v => v.Name == targetTag);
                if (inputVar != null) inputVar.CurrentValue = 1;

                // 2. PlcService (gerçek PLC)
                if (PlcService.Instance != null)
                {
                    var plcVar = PlcService.Instance.OutputVariables.FirstOrDefault(v => v.Name == targetTag);
                    if (plcVar != null) { await PlcService.Instance.WriteAsync(plcVar, 1); plcVar.CurrentValue = 1; }
                    else
                    {
                        var plcIn = PlcService.Instance.InputVariables.FirstOrDefault(v => v.Name == targetTag);
                        if (plcIn != null) { await PlcService.Instance.WriteAsync(plcIn, 1); plcIn.CurrentValue = 1; }
                    }
                }

                // 3. Robot değişkenleri — GlobalData şablonu (in-memory)
                // ★ KUKA VarProxy BOOL değişkenler "TRUE"/"FALSE" string bekler, int 1/0 DEĞİL!
                // Comm loop variable.Value (=CurrentValue.ToString()) okur ve robota yazar.
                // int 1 → "1" gönderir → KUKA bunu tanımaz. "TRUE" string → "TRUE" gönderir ✓
                var robotOutVar = RobotOutputVars.FirstOrDefault(v => v.Name == targetTag);
                if (robotOutVar != null) robotOutVar.Value = "TRUE";
                var robotInVar = RobotInputVars.FirstOrDefault(v => v.Name == targetTag);
                if (robotInVar != null) robotInVar.Value = "TRUE";

                // 3b. Robot Instance Variables (CANLI - UI watcher bu nesneye abone)
                var robots = KukaRobotManager.Instance?.Robots;
                if (robots != null)
                {
                    foreach (var robot in robots)
                    {
                        var liveOut = robot.OutputVars.FirstOrDefault(v => v.Name == targetTag);
                        if (liveOut != null) liveOut.CurrentValue = "TRUE";
                        var liveIn = robot.InputVars.FirstOrDefault(v => v.Name == targetTag);
                        if (liveIn != null) liveIn.CurrentValue = "TRUE";
                    }
                }

                // ★★★ 4. MUTLAKA TÜM ROBOTLARA TCP İLE YAZ ★★★
                // Koleksiyonda bulunmasa bile doğrudan robot TCP'sine yazılır
                // Robot VarProxy herhangi bir KRL değişken adını kabul eder
                try { await WriteToAllRobotsAsync(targetTag, "TRUE"); } catch { }

                // ★★★ 4b. ROBOT 2 İÇİN AYRI TAG YAZMA ★★★
                string targetTag2 = TablaOutputTag2;
                if (!string.IsNullOrEmpty(targetTag2) && targetTag2 != targetTag)
                {
                    try { await WriteToAllRobotsAsync(targetTag2, "TRUE"); } catch { }
                    OnAutomationLog?.Invoke($"✓ Tabla sinyal (R2): {targetTag2} = TRUE");
                }

                TablaSignalActive = true;
                OnAutomationLog?.Invoke($"✓ Tabla sinyal gönderildi: {targetTag} = TRUE (TablaSignalActive=true)");
                OnAutomationStatusChanged?.Invoke();
            }
            catch (Exception ex)
            {
                OnAutomationLog?.Invoke($"✗ Tabla sinyal ayarlama hatası: {ex.Message}");
            }
        }

        // --- TRANSFER LISTESI YÖNETİMİ ---
        public static void SaveTransferRows()
        {
            try
            {
                // Renkleri yok sayarak JSON formatında kaydet (Newtonsoft.Json kullanılıyor)
                string json = JsonConvert.SerializeObject(PlcTransferRows, Formatting.Indented);
                File.WriteAllText(_transferRowsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Transfer Save Error: " + ex.Message);
            }
        }

        private static void LoadTransferRows()
        {
            try
            {
                if (File.Exists(_transferRowsFilePath))
                {
                    var json = File.ReadAllText(_transferRowsFilePath);
                    // Hataları yutarak deserialize et (Renkler gelmezse sorun değil)
                    var settings = new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        Error = (sender, args) => { args.ErrorContext.Handled = true; }
                    };
                    var items = JsonConvert.DeserializeObject<List<PlcTransferItem>>(json, settings);

                    if (items != null && items.Count > 0)
                    {
                        PlcTransferRows.Clear();
                        foreach (var item in items)
                        {
                            // Renkleri manuel olarak ve GÜVENLİ şekilde yeniden oluştur
                            if (item.Status == "SENT") item.StatusColor = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 50, 205, 50)); // LimeGreen
                            else if (item.Status == "WAIT") item.StatusColor = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 165, 0)); // Orange
                            else item.StatusColor = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 0, 0)); // Red

                            item.BackgroundColor = (item.Index % 2 == 1)
                                ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 15, 15))
                                : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 20, 20, 20));

                            // Değişiklik olduğunda GlobalData'daki Save metodunu çağır (Kritik nokta)
                            item.PropertyChanged += (s, e) => { if (e.PropertyName == "SelectedTag") SaveTransferRows(); };

                            PlcTransferRows.Add(item);
                        }
                    }
                }

                // Liste değişirse (Ekle/Sil) kaydet
                PlcTransferRows.CollectionChanged += (s, e) => SaveTransferRows();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadTransferRows Hatası: {ex.Message}");
            }
        }

        // --- JOB DURUM TAKİBİ (PLC -> UI Renklendirme) ---

        /// <summary>
        /// Tum job'larin MeasurementStatus'unu sifirlar (gri).
        /// Yeni urun / yeni olcum baslangicinda cagrilir.
        /// </summary>
        public static void ResetAllJobStatuses()
        {
            foreach (var rfid in KnownRfids)
            {
                rfid.CurrentJobIndex = -1;  // Anlik index gostergesini temizle
                foreach (var job in rfid.IndexedJobSequence)
                {
                    job.MeasurementStatus = null;
                }
            }
        }

        /// <summary>
        /// Aktuel RFID kartinda su an olculecek job index'ini isaretler.
        /// Kamera sayfasinda ilgili satir vurgulu gosterilir.
        /// <summary>
        /// Aktif kartın aktif job'unun SNIFFER + SAPMA değerlerini output tag'lere yazar.
        /// Sayfa geçişlerinde, AktuelRfid değişmese bile doğru değerlerin yazılmasını sağlar.
        /// Auto_Page.Page_Loaded'dan çağrılmalıdır.
        /// </summary>
        public static void SyncCurrentJobOutputs()
        {
            try
            {
                if (string.IsNullOrEmpty(_aktuelRfid)) return;

                var recipe = KnownRfids.FirstOrDefault(r =>
                    string.Equals(r.Id, _aktuelRfid, StringComparison.OrdinalIgnoreCase));
                if (recipe == null) return;

                // Mevcut job index'i bul
                int currentIdx = recipe.CurrentJobIndex;

                // Eğer -1 (ölçüm bitmiş/temizlenmiş) ise G_JOB_INDEX'ten oku
                if (currentIdx < 0 && !string.IsNullOrEmpty(Auto_IndexTag))
                {
                    var indexVar = FindPlcVarByName(Auto_IndexTag);
                    if (indexVar != null && TryParseIndex(indexVar.Value, out int idx))
                        currentIdx = idx;
                }

                if (currentIdx >= 0)
                {
                    UpdateSnifferDurationOutput(recipe, currentIdx);
                    UpdateDeviationLimitOutput(recipe, currentIdx);
                    System.Diagnostics.Debug.WriteLine(
                        $"[GlobalData] SyncCurrentJobOutputs → RFID={_aktuelRfid}, JobIndex={currentIdx}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GlobalData] SyncCurrentJobOutputs hatası: {ex.Message}");
            }
        }

        /// jobIndex: 0-based (RunAutomationSequence'daki idx degeri)
        /// -1 = hicbiri (olcum bitti/iptal)
        /// </summary>
        public static void UpdateCurrentJobIndex(int jobIndex)
        {
            string currentRfid = AktuelRfid;
            if (string.IsNullOrEmpty(currentRfid))
            {
                var rfidVar = GeneralOutputVars.FirstOrDefault(v => v.Name == "AKTUEL_RFID");
                if (rfidVar != null) currentRfid = rfidVar.Value;
            }

            if (string.IsNullOrEmpty(currentRfid)) return;

            RfidDef recipe = null;
            // Aktif kartı bul + diğer kartların ► göstergesini temizle
            foreach (var rfid in KnownRfids)
            {
                if (string.Equals(rfid.Id, currentRfid, StringComparison.OrdinalIgnoreCase))
                {
                    recipe = rfid;
                }
                else
                {
                    // Aktif olmayan kartların göstergesini temizle
                    if (rfid.CurrentJobIndex != -1)
                        rfid.CurrentJobIndex = -1;
                }
            }
            if (recipe == null) return;

            recipe.CurrentJobIndex = jobIndex;

            // ═══ SEÇİLİ INDEX'İN SNİFFER SÜRESİ + NOKTA SAPMA LİMİTİ OUTPUT TAG'E YAZ ═══
            UpdateSnifferDurationOutput(recipe, jobIndex);
            UpdateDeviationLimitOutput(recipe, jobIndex);
        }

        /// <summary>
        /// Seçili job index'inin sniffer ölçüm süresini SNIFFER_OLCUM_SURE output değişkenine yazar.
        /// GeneralOutputVars + PlcTag/PlcTag2 üzerinden doğrudan robota/PLC'ye yazılır.
        /// </summary>
        private static void UpdateSnifferDurationOutput(RfidDef recipe, int jobIndex)
        {
            double snifferMs = 0;
            if (jobIndex >= 0 && jobIndex < recipe.SnifferDurations.Count)
            {
                snifferMs = recipe.SnifferDurations[jobIndex];
            }

            var snifferVar = GeneralOutputVars.FirstOrDefault(v => v.Name == "SNIFFER_OLCUM_SURE");
            if (snifferVar != null)
            {
                snifferVar.CurrentValue = snifferMs;

                // Bridge (Auto_Page ConnectToPlcVariable) sayfa ziyaret edilmemişse çalışmaz.
                // Doğrudan PlcTag/PlcTag2 üzerinden robota yaz.
                WriteOutputVarToTarget(snifferVar.PlcTag, snifferMs);
                WriteOutputVarToTarget(snifferVar.PlcTag2, snifferMs);
            }
        }

        /// <summary>
        /// CODESYS hesaplama sonucunu CodesysTargetResults koleksiyonuna yazar.
        /// Camera sayfasındaki HEDEF NOKTA paneli bu koleksiyonu gösterir.
        /// UI thread'de çağrılmalıdır.
        /// </summary>
        private static void UpdateCodesysTargetResults(KukaPose target)
        {
            // Tek nokta versiyonunu çoklu nokta metoduna yönlendir (geriye uyumluluk)
            UpdateCodesysTargetResultsMultiPoint(new List<KukaPose> { target });
        }

        /// <summary>
        /// Çoklu nokta CODESYS hedef sonuçlarını günceller.
        /// Her nokta için 6 değer (X,Y,Z,A,B,C) ayrı kart olarak gösterilir.
        /// </summary>
        private static void UpdateCodesysTargetResultsMultiPoint(List<KukaPose> targets)
        {
            CodesysTargetResults.Clear();
            string[] axNames = { "X", "Y", "Z", "A", "B", "C" };
            string[] units = { "mm", "mm", "mm", "°", "°", "°" };
            int id = 1;
            for (int pt = 0; pt < targets.Count; pt++)
            {
                var t = targets[pt];
                double[] vals = { t.X, t.Y, t.Z, t.A, t.B, t.C };
                for (int ax = 0; ax < 6; ax++)
                {
                    CodesysTargetResults.Add(new GocatorMeasurement
                    {
                        Id = id++,
                        PointIndex = pt,
                        SourceId = pt * 6 + ax,
                        IsFirstInPoint = (ax == 0),
                        Name = $"Nokta {pt + 1} Target {axNames[ax]}",
                        Value = Math.Round(vals[ax], 3),
                        Unit = units[ax],
                        Decision = "Pass"
                    });
                }
            }
        }

        /// <summary>
        /// Hesaplanan değerleri PlcTransferRows (veya TablaTransferRows) tablosuna yazar.
        /// HandleOlcumTetikAsync'ten çağrılır — RunAutomationSequence kendi mekanizmasını kullanır.
        /// UI thread'de çağrılmalıdır.
        /// </summary>
        private static void UpdatePlcTransferRowsFromValues(double[] values, ObservableCollection<PlcTransferItem> targetRows)
        {
            // Doğru save fonksiyonunu belirle (Tabla vs Boru)
            bool isTabla = (targetRows == TablaTransferRows);
            Action saveAction = isTabla ? SaveTablaTransferRows : SaveTransferRows;

            // Tabloyu Genişlet (eksik satırlar varsa)
            while (targetRows.Count < values.Length)
            {
                int index = targetRows.Count + 1;
                var color = (index % 2 == 1)
                    ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 15, 15))
                    : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 20, 20, 20));

                var newItem = new PlcTransferItem
                {
                    Index = index,
                    Value = "0",
                    Status = "WAIT",
                    StatusColor = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 165, 0)),
                    BackgroundColor = color
                };
                newItem.PropertyChanged += (s, e) => { if (e.PropertyName == "SelectedTag") saveAction(); };
                targetRows.Add(newItem);
            }

            // Değerleri Eşle + PointIndex ata
            for (int i = 0; i < values.Length && i < targetRows.Count; i++)
            {
                var row = targetRows[i];
                row.Value = values[i].ToString("F3");
                row.PointIndex = i / 6; // Çoklu nokta: her 6 değer = 1 nokta
                row.Status = "OK";
                row.StatusColor = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80)); // Yeşil
            }
            saveAction();
        }

        /// <summary>
        /// Seçili job index'inin nokta sapma limitini NOKTA_SAPMA_LIMIT output değişkenine yazar.
        /// Küp güvenlik kontrolü için robot tarafına aktarılır (G_HEDEF_NOKTA_LIMIT).
        /// GeneralOutputVars + PlcTag/PlcTag2 üzerinden doğrudan robota/PLC'ye yazılır.
        /// </summary>
        private static void UpdateDeviationLimitOutput(RfidDef recipe, int jobIndex)
        {
            double limitMm = 50.0;
            if (jobIndex >= 0 && jobIndex < recipe.DeviationLimits.Count)
            {
                limitMm = recipe.DeviationLimits[jobIndex];
            }

            var limitVar = GeneralOutputVars.FirstOrDefault(v => v.Name == "NOKTA_SAPMA_LIMIT");
            if (limitVar != null)
            {
                limitVar.CurrentValue = limitMm;

                // Bridge (Auto_Page ConnectToPlcVariable) sayfa ziyaret edilmemişse çalışmaz.
                // Doğrudan PlcTag/PlcTag2 üzerinden robota yaz.
                WriteOutputVarToTarget(limitVar.PlcTag, limitMm);
                WriteOutputVarToTarget(limitVar.PlcTag2, limitMm);
            }
        }

        /// <summary>
        /// PlcTag formatındaki hedef değişkene değer yazar.
        /// "R1:G_XXX" → Robot 1'in G_XXX output değişkenine yazar.
        /// "TAG_NAME" → PLC output değişkenine yazar.
        /// Auto_Page bridge mekanizmasına bağımlı olmadan doğrudan yazma sağlar.
        /// </summary>
        /// <summary>
        /// PlcTag formatındaki hedef değişkene değer yazar.
        /// "R1:G_XXX" → Robot 1'in G_XXX değişkenine doğrudan WriteVariableAsync ile yazar.
        /// "TAG_NAME" → PLC output değişkenine yazar.
        /// Auto_Page bridge mekanizmasına BAĞIMLI DEĞİLDİR — her sayfadan çalışır.
        /// </summary>
        private static void WriteOutputVarToTarget(string plcTag, double value)
        {
            if (string.IsNullOrEmpty(plcTag)) return;

            try
            {
                // "R1:G_XXX" formatı → Robot değişkeni
                if (plcTag.Length > 3 && plcTag[0] == 'R' && plcTag[2] == ':')
                {
                    if (int.TryParse(plcTag.Substring(1, 1), out int robotNo) && robotNo >= 1)
                    {
                        var robots = KukaRobotManager.Instance?.Robots;
                        if (robots != null && robotNo <= robots.Count)
                        {
                            var robot = robots[robotNo - 1];
                            if (robot.IsConnected)
                            {
                                string varName = plcTag.Substring(3);

                                // Bellekteki değeri de güncelle (RSI output buffer)
                                var targetVar = robot.OutputVars.FirstOrDefault(v => v.Name == varName);
                                if (targetVar != null)
                                    targetVar.CurrentValue = value;

                                // Robota doğrudan yaz (Auto_Page bridge'e bağımlı değil)
                                _ = robot.WriteVariableAsync(varName, value.ToString("F3"));
                            }
                        }
                    }
                }
                else
                {
                    // Normal PLC değişkeni
                    var plcVar = PlcService.Instance?.OutputVariables?.FirstOrDefault(v => v.Name == plcTag)
                              ?? PlcService.Instance?.InputVariables?.FirstOrDefault(v => v.Name == plcTag);
                    if (plcVar != null)
                    {
                        plcVar.CurrentValue = value;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WriteOutputVarToTarget] {plcTag} yazma hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Aktuel RFID'nin belirtilen job index'inin durumunu gunceller.
        /// PLC degisken degisikliklerinden cagrilir.
        /// </summary>
        public static void UpdateJobStatus(int jobIndex, string status)
        {
            // Aktuel RFID'yi bul
            string currentRfid = null;
            var rfidVar = GeneralOutputVars.FirstOrDefault(v => v.Name == "AKTUEL_RFID");
            if (rfidVar != null) currentRfid = rfidVar.Value;

            if (string.IsNullOrEmpty(currentRfid)) return;

            var recipe = KnownRfids.FirstOrDefault(r => r.Id == currentRfid);
            if (recipe == null) return;

            // jobIndex 1-based (robot tarafindan), IndexedJobSequence 0-based
            int idx = jobIndex - 1;
            if (idx >= 0 && idx < recipe.IndexedJobSequence.Count)
            {
                recipe.IndexedJobSequence[idx].MeasurementStatus = status;
            }
        }

        /// <summary>
        /// Aktuel job'in sniffer suresini dondurur (milisaniye).
        /// Robot/PC tarafindan sniffer bekleme suresi icin kullanilir.
        /// </summary>
        public static double GetCurrentSnifferDuration(int jobIndex)
        {
            string currentRfid = null;
            var rfidVar = GeneralOutputVars.FirstOrDefault(v => v.Name == "AKTUEL_RFID");
            if (rfidVar != null) currentRfid = rfidVar.Value;

            if (string.IsNullOrEmpty(currentRfid)) return 5000;

            var recipe = KnownRfids.FirstOrDefault(r => r.Id == currentRfid);
            if (recipe == null) return 5000;

            int idx = jobIndex - 1;
            if (idx >= 0 && idx < recipe.SnifferDurations.Count)
                return recipe.SnifferDurations[idx];

            return 5000;
        }

        // --- İŞLEM AKIŞI ---
        public static async Task RunAutomationSequence(bool forceTablaIndex = false)
        {
            if (IsProcessRunning) return;
            if (OlcumInProgress) return; // HandleOlcumTetikAsync zaten calisiyor
            IsProcessRunning = true;
            OlcumInProgress = true;
            ProcessStatus = "İŞLENİYOR...";

            // Sniffer kaçak monitörünü başlat + önceki sonuçları temizle
            ClearSnifferPoints();
            StartSnifferMonitor();

            // ▼▼▼ SİNYAL SIFIRLA — SADECE İLGİLİ KANAL ▼▼▼
            // Tabla tetik → sadece tabla sinyalini sıfırla (boru TAMAM'ına dokunma)
            // Boru tetik → sadece boru sinyalini sıfırla (tabla TAMAM'ına dokunma)
            if (forceTablaIndex)
            {
                ResetTablaMeasurementSignal();
                // Yeni tetik geldi → önceki TAMAM sinyalini FALSE'a çek
                _ = WriteToAllRobotsAsync("G_TABLA_OLCUM_TAMAM", "FALSE");
            }
            else
            {
                ResetMeasurementSignal();
                // Yeni tetik geldi → önceki TAMAM sinyalini FALSE'a çek
                _ = WriteToAllRobotsAsync("G_BORU_OLCUM_TAMAM", "FALSE");
            }
            ResetAllJobStatuses();


            try
            {
                // RFID Variable Bul (PLC + Robot tüm kaynaklarda ara)
                var rfidVar = FindPlcVarByName(Auto_RfidTag);

                string currentRfid = rfidVar?.Value ?? "---";
                string currentIndex = "0";

                if (forceTablaIndex)
                {
                    // Tabla tetik — G_JOB_INDEX okumaya gerek yok, direkt idx=0
                    currentIndex = "0";
                    OnAutomationLog?.Invoke($"Tabla Tetik: {currentRfid} (Index: 0 [TABLA ZORUNLU])");
                }
                else
                {
                    // Boru tetik — Index değerini doğrudan robottan CANLI oku (cache race condition önlenir)
                    try
                    {
                        var robots = KukaRobotManager.Instance?.Robots;
                        var connectedRobot = robots?.FirstOrDefault(r => r.IsConnected);
                        if (connectedRobot != null && !string.IsNullOrEmpty(Auto_IndexTag))
                        {
                            string freshVal = await connectedRobot.ReadVariableAsync(Auto_IndexTag);
                            if (!string.IsNullOrEmpty(freshVal))
                                currentIndex = freshVal;
                        }
                    }
                    catch { }

                    // Canlı okuma başarısızsa cache'e düş
                    if (currentIndex == "0")
                    {
                        var indexVar = _currentIndexVar ?? FindPlcVarByName(Auto_IndexTag);
                        string cachedVal = indexVar?.Value;
                        if (!string.IsNullOrEmpty(cachedVal) && cachedVal != "0")
                            currentIndex = cachedVal;
                    }

                    OnAutomationLog?.Invoke($"Tetik: {currentRfid} (Index: {currentIndex} [CANLI])");
                }

                var recipe = KnownRfids.FirstOrDefault(r => r.Id == currentRfid);
                if (recipe == null) throw new Exception("Tanımsız RFID");

                TryParseIndex(currentIndex, out int idx);
                OnAutomationLog?.Invoke($"[RunAuto] Parsed index: {idx} (raw=\"{currentIndex}\")");
                if (idx < 0 || idx >= recipe.JobSequence.Count) throw new Exception("Geçersiz Index");

                string jobName = recipe.JobSequence[idx];
                ProcessStatus = $"JOB: {jobName}";

                // ▶ Anlik index gostergesini guncelle (kamera sayfasinda vurgulu satir)
                UpdateCurrentJobIndex(idx);

                // ═══ TETİK ANINDA POZİSYON SNAPSHOT (RunAutomationSequence) ═══
                KukaPose seqSnapshotPose = null;
                try
                {
                    var snapRobot = KukaRobotManager.Instance?.Robots?.FirstOrDefault(r => r.IsConnected);
                    if (snapRobot != null)
                    {
                        var posTasks = new[]
                        {
                            snapRobot.ReadVariableAsync("$POS_ACT.X"),
                            snapRobot.ReadVariableAsync("$POS_ACT.Y"),
                            snapRobot.ReadVariableAsync("$POS_ACT.Z"),
                            snapRobot.ReadVariableAsync("$POS_ACT.A"),
                            snapRobot.ReadVariableAsync("$POS_ACT.B"),
                            snapRobot.ReadVariableAsync("$POS_ACT.C")
                        };
                        await Task.WhenAll(posTasks);
                        double.TryParse(posTasks[0].Result, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double sx);
                        double.TryParse(posTasks[1].Result, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double sy);
                        double.TryParse(posTasks[2].Result, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double sz);
                        double.TryParse(posTasks[3].Result, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double sa);
                        double.TryParse(posTasks[4].Result, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double sb);
                        double.TryParse(posTasks[5].Result, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double sc);
                        seqSnapshotPose = new KukaPose(sx, sy, sz, sa, sb, sc);
                        OnAutomationLog?.Invoke($"[RunAuto] Pozisyon snapshot (canlı): X={sx:F3} Y={sy:F3} Z={sz:F3} A={sa:F3} B={sb:F3} C={sc:F3}");
                    }
                }
                catch
                {
                    OnAutomationLog?.Invoke("[RunAuto] Pozisyon snapshot canlı okuma başarısız → cache kullanılacak");
                }

                // ▼▼▼ JOB YÜKLEME (ÖN-YÜKLEME KONTROLÜ) ▼▼▼
                // Devam eden ön-yükleme varsa bekle (sensör çakışmasını önle)
                try { await _jobPreLoadTask; } catch { }

                if (_preLoadedGocatorJob != null && _preLoadedGocatorJob == jobName)
                {
                    OnAutomationLog?.Invoke($"Job zaten ön-yüklü: {jobName} → LoadJob atlandı (süre kazanımı)");
                    _preLoadedGocatorJob = null; // Kullanıldı, sıfırla
                }
                else
                {
                    _preLoadedGocatorJob = null; // Farklı job veya ilk çağrı, sıfırla
                    bool loadOk = await App4.Utilities.GocatorJobLogic.LoadJob(jobName, (s) => OnAutomationLog?.Invoke(s));
                    if (!loadOk) throw new Exception("Job yüklenemedi");
                }
                // ▲▲▲ JOB YÜKLEME SONU ▲▲▲

                ProcessStatus = "ÖLÇÜM...";
                var (status, measurements) = await App4.Utilities.ReceiveMeasurementLogic.ReceiveAndProcessMeasurements((s) => OnAutomationLog?.Invoke(s), null);

                if (status == 1 && measurements != null && measurements.Count > 0)
                {
                    // Job durumunu OK olarak guncelle (idx 0-based, UpdateJobStatus 1-based)
                    UpdateJobStatus(idx + 1, "OK");

                    // SİNYAL: Boru ve Tabla TAMAM → CODESYS hesaplama + offset yazma SONRASINDA gönderilecek
                    OnAutomationLog?.Invoke("PLC Yazma işlemi başlıyor...");

                    // ▼▼▼ CODESYS HESAPLAMA (per-job DataSourceMode kontrolü + çoklu nokta) ▼▼▼
                    // Per-job ölçüm yöntemi (reçetede tanımlı, yoksa global fallback)
                    string jobDataMode = (recipe.DataSourceModes != null && idx < recipe.DataSourceModes.Count)
                        ? recipe.DataSourceModes[idx] : DataSourceMode;

                    int pointCount = (measurements.Count + 5) / 6;
                    double[] codesysTargetValues = null;

                    if (jobDataMode == "CODESYS")
                    {
                        try
                        {
                            var robot = KukaRobotManager.Instance?.Robots?.FirstOrDefault();
                            if (robot != null && robot.IsConnected)
                            {
                                // idx==0: tabla ölçüm, idx>0: boru ölçüm → ABC dahil/hariç ayarı
                                var codesysCalc = new CodesysMathFunction
                                {
                                    OffsetX = CodesysOffsetX,
                                    OffsetY = CodesysOffsetY,
                                    OffsetZ = CodesysOffsetZ,
                                    IncludeABC = (idx == 0) ? TablaAbcDahil : BoruAbcDahil
                                };
                                var mappingParts = (CodesysGocMappings ?? "0,1,2,3,4,5").Split(',');
                                if (mappingParts.Length >= 1 && int.TryParse(mappingParts[0], out int mx)) codesysCalc.MapIndexX = mx;
                                if (mappingParts.Length >= 2 && int.TryParse(mappingParts[1], out int my)) codesysCalc.MapIndexY = my;
                                if (mappingParts.Length >= 3 && int.TryParse(mappingParts[2], out int mz)) codesysCalc.MapIndexZ = mz;
                                if (mappingParts.Length >= 4 && int.TryParse(mappingParts[3], out int ma)) codesysCalc.MapIndexYaw = ma;
                                if (mappingParts.Length >= 5 && int.TryParse(mappingParts[4], out int mr)) codesysCalc.MapIndexRoll = mr;
                                if (mappingParts.Length >= 6 && int.TryParse(mappingParts[5], out int mp)) codesysCalc.MapIndexPitch = mp;

                                // Tetik anında alınan snapshot varsa onu kullan, yoksa cache
                                var robotPose = seqSnapshotPose ?? new KukaPose(robot.PosX, robot.PosY, robot.PosZ, robot.PosA, robot.PosB, robot.PosC);
                                var allTargetValues = new List<double>();
                                var allTargetPoses = new List<KukaPose>();
                                bool allSuccess = true;

                                // Çoklu nokta: her 6 değer için ayrı CODESYS hesapla
                                for (int pt = 0; pt < pointCount; pt++)
                                {
                                    int startIdx = pt * 6;
                                    int count = Math.Min(6, measurements.Count - startIdx);
                                    var pointValues = new double[count];
                                    for (int gi = 0; gi < count; gi++)
                                        pointValues[gi] = measurements[startIdx + gi].Value;

                                    var target = codesysCalc.CalculateFromArray(pointValues, robotPose);

                                    if (codesysCalc.LastCalculationSuccess)
                                    {
                                        allTargetPoses.Add(target);
                                        allTargetValues.AddRange(new[] { target.X, target.Y, target.Z,
                                                                         target.A, target.B, target.C });
                                    }
                                    else
                                    {
                                        allSuccess = false;
                                        OnAutomationLog?.Invoke($"⚠ CODESYS hesaplama başarısız (Nokta {pt + 1}): {codesysCalc.LastError}");
                                        break;
                                    }
                                }

                                if (allSuccess && allTargetValues.Count > 0)
                                {
                                    codesysTargetValues = allTargetValues.ToArray();

                                    // ═══ HEDEF NOKTA TABLOSUNU GÜNCELLE (CodesysTargetResults — çoklu nokta) ═══
                                    try { await PlcService.Instance.RunOnUiAsync(() => UpdateCodesysTargetResultsMultiPoint(allTargetPoses)); } catch { }

                                    OnAutomationLog?.Invoke($"CODESYS hedef: {pointCount} nokta hesaplandı ({codesysTargetValues.Length} değer)");
                                }
                                else
                                {
                                    OnAutomationLog?.Invoke($"⚠ CODESYS hesaplama başarısız, ham veri kullanılıyor");
                                }
                            }
                            else
                            {
                                OnAutomationLog?.Invoke("⚠ CODESYS: Robot bağlı değil, ham veri kullanılıyor");
                            }
                        }
                        catch (Exception exCod)
                        {
                            OnAutomationLog?.Invoke($"⚠ CODESYS hesaplama hatası: {exCod.Message}");
                        }
                    }
                    // ▲▲▲ CODESYS HESAPLAMA SONU ▲▲▲

                    // ═══ TABLA YENİ ÖLÇÜM NOKTASI TABLOSUNU GÜNCELLE (idx==0, CODESYS çıktısı) ═══
                    if (idx == 0 && codesysTargetValues != null && codesysTargetValues.Length >= 6)
                    {
                        try
                        {
                            await PlcService.Instance.RunOnUiAsync(() =>
                            {
                                TablaCodesysTargetResults.Clear();
                                string[] cNames = { "X", "Y", "Z", "A", "B", "C" };
                                string[] cUnits = { "mm", "mm", "mm", "°", "°", "°" };
                                for (int ci = 0; ci < 6; ci++)
                                    TablaCodesysTargetResults.Add(new GocatorMeasurement
                                    { Id = ci + 1, Name = cNames[ci], Value = Math.Round(codesysTargetValues[ci], 3), Unit = cUnits[ci], Decision = "Pass" });
                            });
                            OnAutomationLog?.Invoke($"Tabla YENİ ÖLÇÜM: X={codesysTargetValues[0]:F3} Y={codesysTargetValues[1]:F3} Z={codesysTargetValues[2]:F3}");
                        }
                        catch { }
                    }

                    // Yazılacak verileri topla
                    List<string> tagsToWrite = new List<string>();
                    List<object> valuesToWrite = new List<object>();

                    // 1. UI Thread updates (Table & Init)
                    await PlcService.Instance.RunOnUiAsync(() =>
                    {
                        if (idx == 0)
                        {
                            // TABLA ÖLÇÜM - TablaLastMeasurements'a kopyala, boru temizle
                            TablaLastMeasurements.Clear();
                            foreach (var m in measurements) TablaLastMeasurements.Add(m);
                            SaveTablaMeasurements();
                            LastMeasurements.Clear();
                            SaveMeasurements();

                            // ═══ TABLA KAÇIKLIK ALARM KONTROLÜ ═══
                            CheckTablaAlarmLimits(measurements);
                        }
                        else
                        {
                            // BORU ÖLÇÜM - LastMeasurements'a yaz
                            LastMeasurements.Clear();
                            foreach (var m in measurements) LastMeasurements.Add(m);
                            SaveMeasurements();
                        }

                        // idx'e göre doğru transfer tablosunu seç
                        var targetRows = (idx == 0) ? TablaTransferRows : PlcTransferRows;
                        Action saveAction = (idx == 0) ? SaveTablaTransferRows : SaveTransferRows;

                        // Aktif değer sayısını belirle (CODESYS çoklu nokta ise N*6, yoksa ölçüm sayısı)
                        int valueCount = codesysTargetValues != null ? codesysTargetValues.Length : measurements.Count;

                        // Tabloyu Genişlet (Eğer eksik varsa)
                        while (targetRows.Count < valueCount)
                        {
                            int index = targetRows.Count + 1;
                            var color = (index % 2 == 1)
                                ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 15, 15))
                                : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 20, 20, 20));

                            var newItem = new PlcTransferItem
                            {
                                Index = index,
                                Value = "0",
                                Status = "WAIT",
                                StatusColor = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 165, 0)), // Orange
                                BackgroundColor = color
                            };
                            newItem.PropertyChanged += (s, e) => { if (e.PropertyName == "SelectedTag") saveAction(); };
                            targetRows.Add(newItem);
                        }

                        // Değerleri Eşle (CODESYS hesaplanmışsa hedef değerleri, yoksa ham ölçüm) + PointIndex ata
                        if (codesysTargetValues != null)
                        {
                            double[] writeValues = codesysTargetValues;

                            // ═══ TABLA KAÇIKLIK: CODESYS sonucundan REFERANS farkı hesapla ═══
                            if (idx == 0 && codesysTargetValues.Length >= 6)
                            {
                                // Aktif case ID'ye göre referans bul
                                int caseId = 0;
                                int klimaIdx = AktuelKlimaIndex;
                                if (klimaIdx > 0 && klimaIdx <= KnownRfids.Count)
                                    caseId = KnownRfids[klimaIdx - 1].CasingIndex;

                                var refPoint = GetTablaReference(caseId);
                                if (refPoint != null && refPoint.HasReference)
                                {
                                    // FARK = YENİ ÖLÇÜM - REFERANS
                                    double[] refVals = { refPoint.X, refPoint.Y, refPoint.Z, refPoint.A, refPoint.B, refPoint.C };
                                    writeValues = new double[6];
                                    for (int di = 0; di < 6; di++)
                                        writeValues[di] = Math.Round(codesysTargetValues[di] - refVals[di], 3);

                                    OnAutomationLog?.Invoke($"Tabla FARK: X={writeValues[0]:F3} Y={writeValues[1]:F3} Z={writeValues[2]:F3} A={writeValues[3]:F3} B={writeValues[4]:F3} C={writeValues[5]:F3}");
                                }
                                else
                                {
                                    OnAutomationLog?.Invoke($"⚠ Tabla REFERANS yok (Case {caseId}), CODESYS çıktısı direkt aktarılıyor");
                                }
                            }

                            // A/B/C HARİÇ ise → A/B/C değerlerini 0'a çek (her 6'lı blokta index 3,4,5)
                            bool abcDahil = (idx == 0) ? TablaAbcDahil : BoruAbcDahil;
                            if (!abcDahil && writeValues.Length >= 6)
                            {
                                for (int blk = 0; blk < writeValues.Length / 6; blk++)
                                {
                                    writeValues[blk * 6 + 3] = 0; // A
                                    writeValues[blk * 6 + 4] = 0; // B
                                    writeValues[blk * 6 + 5] = 0; // C
                                }
                                OnAutomationLog?.Invoke($"A/B/C HARİÇ → A=0, B=0, C=0 (idx={idx})");
                            }

                            // CODESYS hesaplanmış değerleri (tabla: fark, boru: hedef)
                            int cCount = Math.Min(writeValues.Length, targetRows.Count);
                            for (int i = 0; i < cCount; i++)
                            {
                                var row = targetRows[i];
                                row.Value = writeValues[i].ToString("F3");
                                row.PointIndex = i / 6;
                                row.Status = "WAIT";
                                row.StatusColor = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 152, 0));
                                tagsToWrite.Add(row.SelectedTag);
                                valuesToWrite.Add(writeValues[i]);
                            }
                        }
                        else
                        {
                            // Ham ölçüm değerleri (SENSOR / HAND_EYE modu)
                            for (int i = 0; i < measurements.Count; i++)
                            {
                                var row = targetRows[i];
                                row.Value = measurements[i].Value.ToString();
                                row.PointIndex = i / 6; // Çoklu nokta
                                row.Status = "WAIT";
                                row.StatusColor = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 165, 0));
                                tagsToWrite.Add(row.SelectedTag);
                                valuesToWrite.Add(measurements[i].Value);
                            }
                        }
                        saveAction();

                        // ═══ TABLA AKTARIM DEĞER LİMİT KONTROLÜ ═══
                        // Aktarım tablosundaki değerlerin herhangi biri limiti aşarsa alarm ver
                        if (idx == 0)
                        {
                            CheckTablaTransferLimits(targetRows, valuesToWrite);
                        }
                    });

                    // 2. PLC'ye Yaz (Background Thread)
                    for (int i = 0; i < valuesToWrite.Count; i++)
                    {
                        string tag = tagsToWrite[i];
                        object val = valuesToWrite[i];

                        if (!string.IsNullOrEmpty(tag))
                        {
                            // ÖNCELİK DEĞİŞTİRİLDİ: Artık kullanıcı tanımlı (PlcService) değişkenlere öncelik veriliyor.
                            var plcTag = PlcService.Instance?.OutputVariables.FirstOrDefault(v => v.Name == tag) 
                                      ?? GeneralOutputVars.FirstOrDefault(v => v.Name == tag);

                            if (plcTag != null)
                            {
                                // Gocator'dan gelen veri float/double ise ve Tag tipi uygun değilse REAL yap
                                if (val is double || val is float)
                                {
                                    bool isReal = plcTag.Type.Equals("REAL", StringComparison.OrdinalIgnoreCase) || 
                                                  plcTag.Type.Equals("FLOAT", StringComparison.OrdinalIgnoreCase);
                                    
                                    if (!isReal)
                                    {
                                        plcTag.Type = "REAL";
                                        PlcService.Instance?.SaveVariables(); // Değişikliği kaydet
                                        OnAutomationLog?.Invoke($"⚠ Tag '{plcTag.Name}' tipi REAL olarak güncellendi (Gocator verisi düzeltmesi).");
                                    }
                                }

                                await PlcService.Instance.WriteAsync(plcTag, val);
                                OnAutomationLog?.Invoke($"PLC WR: {plcTag.Name} = {val}");
                            }
                        }
                    }

                    // 3. Durumu Güncelle (UI Thread)
                    var statusRows = (idx == 0) ? TablaTransferRows : PlcTransferRows;
                    await PlcService.Instance.RunOnUiAsync(() =>
                    {
                        for (int i = 0; i < valuesToWrite.Count; i++)
                        {
                            var row = statusRows[i];
                            row.Status = "SENT";
                            row.StatusColor = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 50, 205, 50)); // LimeGreen
                        }
                    });

                    // ▼▼▼ ROBOT GERİ YAZMA ▼▼▼
                    string[] offsets;
                    if (idx == 0)
                    {
                        offsets = new[] { "G_TABLA_OFFSET_X", "G_TABLA_OFFSET_Y", "G_TABLA_OFFSET_Z",
                                          "G_TABLA_OFFSET_A", "G_TABLA_OFFSET_B", "G_TABLA_OFFSET_C" };
                        // AKTARIM tablosundaki değerleri yaz (CODESYS FARK hesabı dahil)
                        for (int i = 0; i < Math.Min(valuesToWrite.Count, offsets.Length); i++)
                        {
                            double v = valuesToWrite[i] is double dv ? dv : Convert.ToDouble(valuesToWrite[i]);
                            await WriteToAllRobotsAsync(offsets[i], v.ToString("F3"));
                        }
                        await WriteToAllRobotsAsync("G_TABLA_OFFSET_HAZIR", "TRUE");

                        // Aktif istasyonun tabla kaçıklık UI'ını güncelle
                        var tablaValues = new List<GocatorMeasurement>();
                        for (int i = 0; i < Math.Min(valuesToWrite.Count, 6); i++)
                        {
                            double v = valuesToWrite[i] is double dv2 ? dv2 : Convert.ToDouble(valuesToWrite[i]);
                            tablaValues.Add(new GocatorMeasurement { Value = v });
                        }
                        UpdateStationTablaOffsets(tablaValues);
                    }
                    else
                    {
                        offsets = new[] { "G_OFFSET_X", "G_OFFSET_Y", "G_OFFSET_Z",
                                          "G_OFFSET_A", "G_OFFSET_B", "G_OFFSET_C" };

                        if (codesysTargetValues != null)
                        {
                            // CODESYS hesaplanmış hedef değerleri robota yaz
                            for (int i = 0; i < Math.Min(codesysTargetValues.Length, offsets.Length); i++)
                                await WriteToAllRobotsAsync(offsets[i], codesysTargetValues[i].ToString("F3"));

                            OnAutomationLog?.Invoke($"Boru ölçüm OK (Job {idx}) - CODESYS hedef yazıldı: " +
                                $"X={codesysTargetValues[0]:F2} Y={codesysTargetValues[1]:F2} Z={codesysTargetValues[2]:F2}");
                        }
                        else if (jobDataMode == "HAND_EYE" && CalibrationService.Instance.IsCalibrated && measurements.Count >= 3)
                        {
                            // Hand-Eye kalibrasyon ile sensor → base donusumu
                            double gocX = measurements[0].Value, gocY = measurements[1].Value, gocZ = measurements[2].Value;
                            double gocA = measurements.Count > 3 ? measurements[3].Value : 0;
                            double gocB = measurements.Count > 4 ? measurements[4].Value : 0;
                            double gocC = measurements.Count > 5 ? measurements[5].Value : 0;

                            var sensorTarget = new KukaPose(gocX, gocY, gocZ, gocA, gocB, gocC).ToMatrix();

                            // Robot 1'i (Gocator monte) bul
                            var robot = KukaRobotManager.Instance?.Robots?.FirstOrDefault();
                            KukaPose basePose = null;
                            if (robot != null)
                                basePose = await CalibrationService.Instance.LocateFromRobotAsync(robot, sensorTarget, userBaseNo: 1);

                            if (basePose != null)
                            {
                                // UI'da dönüştürülmüş verileri göster
                                try { await PlcService.Instance.RunOnUiAsync(() => PopulateTransformedMeasurements(basePose)); } catch { }

                                double[] baseVals = { basePose.X, basePose.Y, basePose.Z,
                                                      basePose.A, basePose.B, basePose.C };
                                for (int i = 0; i < 6; i++)
                                    await WriteToAllRobotsAsync(offsets[i], baseVals[i].ToString("F3"));

                                OnAutomationLog?.Invoke($"HandEye dönüşüm: X={basePose.X:F2} Y={basePose.Y:F2} Z={basePose.Z:F2} " +
                                    $"A={basePose.A:F2} B={basePose.B:F2} C={basePose.C:F2}");
                            }
                            else
                            {
                                OnAutomationLog?.Invoke("UYARI: HandEye dönüşüm başarısız, ham değerler yazılıyor");
                                for (int i = 0; i < Math.Min(measurements.Count, offsets.Length); i++)
                                    await WriteToAllRobotsAsync(offsets[i], measurements[i].Value.ToString("F3"));
                            }
                        }
                        else
                        {
                            for (int i = 0; i < Math.Min(measurements.Count, offsets.Length); i++)
                                await WriteToAllRobotsAsync(offsets[i], measurements[i].Value.ToString("F3"));
                        }
                    }

                    // ═══ TAMAM SİNYALİ: CODESYS hesaplanıp offset yazıldıktan SONRA ═══
                    string tamamSignal = idx == 0 ? "G_TABLA_OLCUM_TAMAM" : "G_BORU_OLCUM_TAMAM";
                    await WriteToAllRobotsAsync(tamamSignal, "TRUE");
                    OnAutomationLog?.Invoke($"{tamamSignal} = TRUE (offset yazıldı, hesap tamam)");

                    // Durum bilgisi güncelle (Kamera sayfası status bar)
                    string posInfo = seqSnapshotPose != null ? $"Poz: X={seqSnapshotPose.X:F1} Y={seqSnapshotPose.Y:F1} Z={seqSnapshotPose.Z:F1}" : "";
                    if (idx == 0)
                    {
                        TablaOlcumDurum = $"Tabla ölçüm: BASE 2 + TOOL 2 ile yapıldı ({posInfo})";
                        TablaOlcumBasarili = true;
                        SetTablaMeasurementSignal();
                        OnAutomationLog?.Invoke("Tabla aktarım tamamlandı → TABLA_OFFSET_TAMAM = 1");
                    }
                    else
                    {
                        BoruOlcumDurum = $"Boru ölçüm: BASE 1 + TOOL 2 ile yapıldı ({posInfo})";
                        BoruOlcumBasarili = true;
                        SetMeasurementSignal();
                    }

                    ProcessStatus = "TAMAMLANDI";
                    // Olcum bitti — index gostergesini temizle
                    UpdateCurrentJobIndex(-1);
                }
                else
                {
                    // Job durumunu NOK olarak guncelle
                    UpdateJobStatus(idx + 1, "NOK");
                    UpdateCurrentJobIndex(-1);
                    ProcessStatus = "VERİ YOK";
                    OnAutomationLog?.Invoke("Ölçüm alınamadı: Çıktı yok veya zaman aşımı — TAMAM GÖNDERİLMEDİ");
                    _preLoadedGocatorJob = null;
                }
            }
            catch (Exception ex)
            {
                ProcessStatus = "HATA";
                UpdateCurrentJobIndex(-1);
                _preLoadedGocatorJob = null;
                OnAutomationLog?.Invoke($"Hata: {ex.Message} — TAMAM GÖNDERİLMEDİ");
            }
            finally
            {
                StopSnifferMonitor();
                IsProcessRunning = false;
                OlcumInProgress = false;
                await Task.Delay(2000);
                if (ProcessStatus == "TAMAMLANDI" || ProcessStatus == "VERİ YOK" || ProcessStatus == "HATA") ProcessStatus = "HAZIR";
            }
        }
    } // GlobalData Class Sonu

    // ═══════════════════════════════════════════════════════════════════════════
    // AYAR YEDEKLEME/İÇE AKTARMA SİSTEMİ
    // ═══════════════════════════════════════════════════════════════════════════
    public static class ConfigBackupManager
    {
        private static readonly string _appDataFolder = GlobalData.ConfigBaseDir;

        /// <summary>
        /// Tüm ayar dosyalarını tek bir ZIP dosyasına yedekler.
        /// </summary>
        public static async Task<string> ExportConfigAsync(string destinationPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Geçici klasör oluştur
                    string tempDir = Path.Combine(Path.GetTempPath(), $"App4_ConfigExport_{Guid.NewGuid():N}");
                    Directory.CreateDirectory(tempDir);

                    // 1. AppData JSON dosyalarını kopyala
                    if (Directory.Exists(_appDataFolder))
                    {
                        foreach (var file in Directory.GetFiles(_appDataFolder, "*.json"))
                            File.Copy(file, Path.Combine(tempDir, Path.GetFileName(file)), true);
                    }

                    // 2. LocalSettings (Otomasyon, Robot ayarları) — JSON olarak dışa aktar
                    var settings = Windows.Storage.ApplicationData.Current.LocalSettings.Values;
                    var settingsDict = new Dictionary<string, object>();
                    foreach (var key in settings.Keys)
                    {
                        var val = settings[key];
                        if (val != null)
                            settingsDict[key] = val;
                    }
                    string settingsJson = System.Text.Json.JsonSerializer.Serialize(settingsDict, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(Path.Combine(tempDir, "_LocalSettings.json"), settingsJson);

                    // 3. PLC Config dosyasını kopyala (zaten _appDataFolder içinde JSON olarak kopyalandı)
                    // PLC_Config.json yukarıdaki GetFiles("*.json") ile zaten dahil ediliyor

                    // ZIP oluştur
                    if (File.Exists(destinationPath)) File.Delete(destinationPath);
                    System.IO.Compression.ZipFile.CreateFromDirectory(tempDir, destinationPath);

                    // Geçici klasörü temizle
                    Directory.Delete(tempDir, true);

                    return destinationPath;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Config Export Error: {ex.Message}");
                    return null;
                }
            });
        }

        /// <summary>
        /// ZIP dosyasından ayarları içe aktarır.
        /// </summary>
        public static async Task<bool> ImportConfigAsync(string zipFilePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string tempDir = Path.Combine(Path.GetTempPath(), $"App4_ConfigImport_{Guid.NewGuid():N}");
                    System.IO.Compression.ZipFile.ExtractToDirectory(zipFilePath, tempDir, true);

                    // 1. JSON dosyalarını AppData'ya kopyala
                    if (!Directory.Exists(_appDataFolder))
                        Directory.CreateDirectory(_appDataFolder);

                    foreach (var file in Directory.GetFiles(tempDir, "*.json"))
                    {
                        string fileName = Path.GetFileName(file);
                        if (fileName == "_LocalSettings.json") continue; // Ayrı işlenecek
                        File.Copy(file, Path.Combine(_appDataFolder, fileName), true);
                    }

                    // 2. LocalSettings'i geri yükle
                    string localSettingsPath = Path.Combine(tempDir, "_LocalSettings.json");
                    if (File.Exists(localSettingsPath))
                    {
                        var json = File.ReadAllText(localSettingsPath);
                        var settingsDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(json);
                        if (settingsDict != null)
                        {
                            var settings = Windows.Storage.ApplicationData.Current.LocalSettings.Values;
                            foreach (var kvp in settingsDict)
                            {
                                switch (kvp.Value.ValueKind)
                                {
                                    case System.Text.Json.JsonValueKind.String:
                                        settings[kvp.Key] = kvp.Value.GetString();
                                        break;
                                    case System.Text.Json.JsonValueKind.Number:
                                        if (kvp.Value.TryGetInt32(out int intVal))
                                            settings[kvp.Key] = intVal;
                                        else
                                            settings[kvp.Key] = kvp.Value.GetDouble();
                                        break;
                                    case System.Text.Json.JsonValueKind.True:
                                        settings[kvp.Key] = true;
                                        break;
                                    case System.Text.Json.JsonValueKind.False:
                                        settings[kvp.Key] = false;
                                        break;
                                }
                            }
                        }
                    }

                    // Geçici klasörü temizle
                    Directory.Delete(tempDir, true);
                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Config Import Error: {ex.Message}");
                    return false;
                }
            });
        }
    }

    // PLC Transfer Item (GlobalData namespace'i içine ama sınıfın dışına)
    public class PlcTransferItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public int Index { get; set; }

        private string _selectedTag;
        public string SelectedTag
        {
            get => _selectedTag;
            set { if (_selectedTag != value) { _selectedTag = value; OnPropertyChanged(); } }
        }

        private string _value;
        public string Value
        {
            get => _value;
            set { if (_value != value) { _value = value; OnPropertyChanged(); } }
        }

        private string _status;
        public string Status
        {
            get => _status;
            set { if (_status != value) { _status = value; OnPropertyChanged(); } }
        }

        private SolidColorBrush _statusColor;
        [Newtonsoft.Json.JsonIgnore]
        public SolidColorBrush StatusColor
        {
            get => _statusColor;
            set { if (_statusColor != value) { _statusColor = value; OnPropertyChanged(); } }
        }

        [Newtonsoft.Json.JsonIgnore]
        public SolidColorBrush BackgroundColor { get; set; }

        // Çoklu Nokta Desteği
        public int PointIndex { get; set; } = 0; // 0-based nokta indexi

        [Newtonsoft.Json.JsonIgnore]
        public bool IsFirstInPoint => (Index > 0 && ((Index - 1) % 6 == 0));

        [Newtonsoft.Json.JsonIgnore]
        public string PointLabel => $"NOKTA {PointIndex + 1}";
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ROBOT SLİDER SİNYAL EŞLEŞTİRME SINIFI
    // Robottan gelen sinyalleri amaçlarına göre eşleştirme
    // ═══════════════════════════════════════════════════════════════════════════
    public class RobotSliderSignalMapping : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public string RobotName { get; set; }

        public RobotSliderSignalMapping(string robotName)
        {
            RobotName = robotName;
        }

        // ═══ İSTASYON DURUMU ═══
        // Robotun şu an hangi istasyonda olduğunu belirten int değer (1, 2, 3)
        private string _stationNumberSignal = ""; // Robot sinyali adı (örn: "Station_No" veya kullanıcı tanımlı)
        public string StationNumberSignal
        {
            get => _stationNumberSignal;
            set { if (_stationNumberSignal != value) { _stationNumberSignal = value; OnPropertyChanged(); } }
        }

        private int _currentStationNumber = 0; // Güncel değer (1, 2, 3)
        public int CurrentStationNumber
        {
            get => _currentStationNumber;
            set { if (_currentStationNumber != value) { _currentStationNumber = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentStationText)); } }
        }

        public string CurrentStationText => CurrentStationNumber > 0 ? $"İSTASYON {CurrentStationNumber}" : "BELİRSİZ";

        // ═══ SLİDER POZİSYONU ═══
        // Slider pozisyonunu mm cinsinden veren sinyal
        private string _sliderPositionSignal = "E1"; // Varsayılan: E1 harici ekseni
        public string SliderPositionSignal
        {
            get => _sliderPositionSignal;
            set { if (_sliderPositionSignal != value) { _sliderPositionSignal = value; OnPropertyChanged(); } }
        }

        private double _currentSliderPosition = 0; // mm cinsinden
        public double CurrentSliderPosition
        {
            get => _currentSliderPosition;
            set { if (Math.Abs(_currentSliderPosition - value) > 0.1) { _currentSliderPosition = value; OnPropertyChanged(); OnPropertyChanged(nameof(SliderPositionPercent)); } }
        }

        // İstasyon pozisyonları (mm) - Slider üzerindeki sabit konumlar
        public double Station1Position { get; set; } = 0;
        public double Station2Position { get; set; } = 1500;
        public double Station3Position { get; set; } = 3000;

        public double SliderPositionPercent
        {
            get
            {
                if (Station3Position <= Station1Position) return 0;
                double percent = ((CurrentSliderPosition - Station1Position) / (Station3Position - Station1Position)) * 100;
                return Math.Clamp(percent, 0, 100);
            }
        }

        // ═══ ROBOT DURUMU ═══
        private string _robotStatusSignal = ""; // Robot durum sinyali (Ready, Running, Error vb.)
        public string RobotStatusSignal
        {
            get => _robotStatusSignal;
            set { if (_robotStatusSignal != value) { _robotStatusSignal = value; OnPropertyChanged(); } }
        }

        private int _currentRobotStatus = 0;
        public int CurrentRobotStatus
        {
            get => _currentRobotStatus;
            set { if (_currentRobotStatus != value) { _currentRobotStatus = value; OnPropertyChanged(); OnPropertyChanged(nameof(RobotStatusText)); } }
        }

        public string RobotStatusText => CurrentRobotStatus switch
        {
            0 => "KAPALI",
            1 => "HAZIR",
            2 => "ÇALIŞIYOR",
            3 => "HATA",
            4 => "DURAKLATILDI",
            _ => $"DURUM {CurrentRobotStatus}"
        };

        // ═══ OVERRİDE ═══
        private string _overrideSignal = "OverridePro";
        public string OverrideSignal
        {
            get => _overrideSignal;
            set { if (_overrideSignal != value) { _overrideSignal = value; OnPropertyChanged(); } }
        }

        private int _currentOverride = 100;
        public int CurrentOverride
        {
            get => _currentOverride;
            set { if (_currentOverride != value) { _currentOverride = value; OnPropertyChanged(); } }
        }

        // ═══ OPERASYON MODU ═══
        private string _operationModeSignal = "OperationMode";
        public string OperationModeSignal
        {
            get => _operationModeSignal;
            set { if (_operationModeSignal != value) { _operationModeSignal = value; OnPropertyChanged(); } }
        }

        private int _currentOperationMode = 0;
        public int CurrentOperationMode
        {
            get => _currentOperationMode;
            set { if (_currentOperationMode != value) { _currentOperationMode = value; OnPropertyChanged(); OnPropertyChanged(nameof(OperationModeText)); } }
        }

        public string OperationModeText => CurrentOperationMode switch { 1 => "T1", 2 => "T2", 3 => "AUT", 4 => "EXT", _ => "?" };

        // ═══ BAĞLANTI DURUMU ═══
        private bool _isConnected = false;
        public bool IsConnected
        {
            get => _isConnected;
            set { if (_isConnected != value) { _isConnected = value; OnPropertyChanged(); OnPropertyChanged(nameof(ConnectionStatusText)); } }
        }

        public string ConnectionStatusText => IsConnected ? "BAĞLI" : "BAĞLI DEĞİL";

        // ═══ KAYIT/YÜKLEME İÇİN SERİALİZE EDİLEBİLİR VERİLER ═══
        public RobotSliderMappingData ToSaveData() => new()
        {
            StationNumberSignal = StationNumberSignal,
            SliderPositionSignal = SliderPositionSignal,
            OverrideSignal = OverrideSignal,
            OperationModeSignal = OperationModeSignal,
            RobotStatusSignal = RobotStatusSignal,
            Station1Position = Station1Position,
            Station2Position = Station2Position,
            Station3Position = Station3Position
        };

        public void LoadFromData(RobotSliderMappingData data)
        {
            if (data == null) return;
            StationNumberSignal = data.StationNumberSignal ?? "";
            SliderPositionSignal = data.SliderPositionSignal ?? "E1";
            OverrideSignal = data.OverrideSignal ?? "OverridePro";
            OperationModeSignal = data.OperationModeSignal ?? "OperationMode";
            RobotStatusSignal = data.RobotStatusSignal ?? "";
            Station1Position = data.Station1Position;
            Station2Position = data.Station2Position;
            Station3Position = data.Station3Position;
        }
    }

    // Kayıt için veri sınıfı
    public class RobotSliderMappingData
    {
        public string StationNumberSignal { get; set; }
        public string SliderPositionSignal { get; set; }
        public string OverrideSignal { get; set; }
        public string OperationModeSignal { get; set; }
        public string RobotStatusSignal { get; set; }
        public double Station1Position { get; set; }
        public double Station2Position { get; set; }
        public double Station3Position { get; set; }
    }
}