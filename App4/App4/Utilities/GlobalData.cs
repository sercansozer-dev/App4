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
        private static readonly string _rfidFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "App4", "Saved_RFID_List.json");
        private static readonly string _stationStateFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "App4", "Station_States.json");
        private static readonly string _autoPageVariablesFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "App4", "Auto_Page_Variables.json");
        private static readonly string _measurementsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "App4", "Saved_Measurements.json");
        private static readonly string _transferRowsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "App4", "Camera_PlcTransfer.json");
        private static readonly string _systemChecksFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "App4", "System_Checks.json");
        private static readonly string _transformedMeasurementsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "App4", "Transformed_Measurements.json");
        private static readonly string _robotSliderMappingFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "App4", "Robot_Slider_Mapping.json");

        // --- GLOBAL LİSTELER ---
        public static ObservableCollection<RfidDef> KnownRfids { get; private set; } = new();

        // YENİ: Model Klasöründeki Dosyaların Listesi
        public static ObservableCollection<string> AvailableModels { get; private set; } = new();

        public static ObservableCollection<StationViewModel> Stations { get; private set; } = new();
        public static ObservableCollection<SystemCheckItem> SystemCheckList { get; private set; } = new();
        public static ObservableCollection<GocatorMeasurement> LastMeasurements { get; private set; } = new();
        public static ObservableCollection<PlcTransferItem> PlcTransferRows { get; private set; } = new();

        // DÖNÜŞTÜRÜLMÜŞ ÖLÇÜM (Hand-Eye kalibrasyon sonrası base koordinatları)
        public static ObservableCollection<GocatorMeasurement> TransformedMeasurements { get; private set; } = new();

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
        private static double _kl100Station1Pos = 0;
        public static double KL100_Station1Pos
        {
            get => _kl100Station1Pos;
            set { if (_kl100Station1Pos != value) { _kl100Station1Pos = value; SaveKL100StationPositions(); } }
        }

        private static double _kl100Station2Pos = 0;
        public static double KL100_Station2Pos
        {
            get => _kl100Station2Pos;
            set { if (_kl100Station2Pos != value) { _kl100Station2Pos = value; SaveKL100StationPositions(); } }
        }

        private static double _kl100Station3Pos = 0;
        public static double KL100_Station3Pos
        {
            get => _kl100Station3Pos;
            set { if (_kl100Station3Pos != value) { _kl100Station3Pos = value; SaveKL100StationPositions(); } }
        }

        private static readonly string _kl100PosFilePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "App4", "KL100_StationPositions.json");

        public static void SaveKL100StationPositions()
        {
            try
            {
                var data = new { Station1 = _kl100Station1Pos, Station2 = _kl100Station2Pos, Station3 = _kl100Station3Pos };
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
                    if (doc.TryGetProperty("Station1", out var s1)) _kl100Station1Pos = s1.GetDouble();
                    if (doc.TryGetProperty("Station2", out var s2)) _kl100Station2Pos = s2.GetDouble();
                    if (doc.TryGetProperty("Station3", out var s3)) _kl100Station3Pos = s3.GetDouble();
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

        // --- TABLA ÖLÇÜM ÇIKTI TAG ---
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
            InitializeStations();
            LoadStationStates();
            InitializeVariables();
            // NOT: LoadPlcVariableTagsFromFile() artık InitializeVariables() içinde çağrılıyor.
            // İkinci kez çağrılırsa dosyadan RB1/RB2 gibi kaldırılmış değişkenleri geri yükler.
            LoadSystemChecks();
            LoadMeasurements();
            LoadTransformedMeasurements();
            LoadTransferRows();
            LoadTablaMeasurements();
            LoadTablaTransferRows();
            LoadRobotSliderMappings(); // Robot sinyal eşleştirmelerini yükle
            LoadKL100StationPositions(); // KL100 istasyon pozisyon verilerini yükle

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

        public static void SaveMeasurements() { try { string json = System.Text.Json.JsonSerializer.Serialize(LastMeasurements, new JsonSerializerOptions { WriteIndented = true }); File.WriteAllText(_measurementsFilePath, json); } catch { } }
        private static void LoadMeasurements() { try { if (File.Exists(_measurementsFilePath)) { var list = System.Text.Json.JsonSerializer.Deserialize<List<GocatorMeasurement>>(File.ReadAllText(_measurementsFilePath)); if (list != null) { LastMeasurements.Clear(); foreach (var item in list) LastMeasurements.Add(item); } } } catch { } }

        // --- TABLA KAÇIKLIK ÖLÇÜM KAYIT ---
        private static readonly string _tablaMeasurementsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "App4", "Saved_Tabla_Measurements.json");
        private static readonly string _tablaTransferFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "App4", "Tabla_Transfer_Rows.json");

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
                e.PropertyName == nameof(RfidDef.Description))
                SaveRfids();
        }

        public static void SaveRfids() { try { File.WriteAllText(_rfidFilePath, JsonConvert.SerializeObject(KnownRfids, Formatting.Indented)); } catch { } }

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
            GeneralOutputVars.Add(Create("AKTUEL_RFID", "STRING", "Output", ""));            // Aktüel RFID Id string değeri
            GeneralOutputVars.Add(Create("SNIFFER_OLCUM_SURE", "REAL", "Output", "0"));       // Seçili index'in sniffer ölçüm süresi (ms)
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

            // ▼▼▼ Temizlik: Eski dosyada kalmış RB1/RB2 robot değişkenlerini GeneralOutputVars'tan kaldır ▼▼▼
            // (Bu değişkenler artık sadece PlcService.EnsureRobotBridgeVariables'da yönetiliyor)
            var robotVarsToRemove = GeneralOutputVars.Where(v => v.Name.StartsWith("RB1_") || v.Name.StartsWith("RB2_")).ToList();
            foreach (var rv in robotVarsToRemove) GeneralOutputVars.Remove(rv);
            
            // ═══════════════════════════════════════════════════════════════
            // ROBOT → PLC DEĞİŞKENLERİ (Input - Robottan gelen sinyaller)
            // $CONFIG.dat USER GLOBALS ile birebir eşleşir
            // ═══════════════════════════════════════════════════════════════
            if (RobotInputVars.Count == 0)
            {
                // --- ROBOT GENEL DURUM ---
                RobotInputVars.Add(Create("G_ROBOT_DURUM", "INT", "Input", 0));          // R1: 0=Bosta 1=Calisiyor 2=Hata 10-12=Gocator 50-51=Tabla | R2: 5=Timeout 10-11=TablaOffset 20-22=Sniffer 30-31=Slider
                RobotInputVars.Add(Create("G_IS_BITTI", "BOOL", "Input", false));         // Is tamamlandi bayragi
                RobotInputVars.Add(Create("G_HATA_VAR", "BOOL", "Input", false));         // Hata var bayragi
                RobotInputVars.Add(Create("G_HATA_KODU", "INT", "Input", 0));             // Hata kodu
                // --- KLIMA SECIMI ---
                RobotInputVars.Add(Create("G_KLIMA_TIP", "INT", "Input", 0));             // 0=Secilmedi 1..N=Klima tipi
                RobotInputVars.Add(Create("G_KLIMA_ADET", "INT", "Input", 0));            // Toplam klima tipi sayisi
                RobotInputVars.Add(Create("G_AKTIF_NOKTA", "INT", "Input", 0));           // Suanki olcum noktasi (1..7)
                RobotInputVars.Add(Create("G_TOPLAM_NOKTA", "INT", "Input", 0));          // Toplam olcum noktasi
                RobotInputVars.Add(Create("G_NOK_SAYISI", "INT", "Input", 0));            // Basarisiz nokta sayisi
                RobotInputVars.Add(Create("G_NOK_NOKTA", "INT", "Input", 0));             // Son NOK olan nokta numarasi
                RobotInputVars.Add(Create("G_NOK_BILDIRIM", "BOOL", "Input", false));     // NOK bildirimi bayragi
                // --- GOCATOR BORU KAYNAK OFFSET ---
                RobotInputVars.Add(Create("G_OFFSET_X", "REAL", "Input", 0.0));
                RobotInputVars.Add(Create("G_OFFSET_Y", "REAL", "Input", 0.0));
                RobotInputVars.Add(Create("G_OFFSET_Z", "REAL", "Input", 0.0));
                RobotInputVars.Add(Create("G_OFFSET_A", "REAL", "Input", 0.0));
                RobotInputVars.Add(Create("G_OFFSET_B", "REAL", "Input", 0.0));
                RobotInputVars.Add(Create("G_OFFSET_C", "REAL", "Input", 0.0));
                // --- GOCATOR OLCUM SISTEMI (TEKLI JOB INDEX) ---
                RobotInputVars.Add(Create("G_JOB_INDEX", "INT", "Input", 0));             // Gocator job (0=tabla, 1..N=boru noktasi)
                RobotInputVars.Add(Create("G_OLCUM_TETIK", "BOOL", "Input", false));      // Robot -> PC : Olcum baslat
                RobotInputVars.Add(Create("G_BORU_OLCUM_TAMAM", "BOOL", "Input", false));  // PC -> Robot : Boru olcum tamamlandi
                RobotInputVars.Add(Create("G_TABLA_OLCUM_TAMAM", "BOOL", "Input", false)); // PC -> Robot : Tabla olcum tamamlandi
                RobotInputVars.Add(Create("G_OLCUM_OK", "BOOL", "Input", false));         // PC -> Robot : Sonuc OK/NOK
                // --- GOCATOR TABLA OFFSET ---
                RobotInputVars.Add(Create("G_TABLA_OFFSET_X", "REAL", "Input", 0.0));
                RobotInputVars.Add(Create("G_TABLA_OFFSET_Y", "REAL", "Input", 0.0));
                RobotInputVars.Add(Create("G_TABLA_OFFSET_Z", "REAL", "Input", 0.0));
                RobotInputVars.Add(Create("G_TABLA_OFFSET_A", "REAL", "Input", 0.0));
                RobotInputVars.Add(Create("G_TABLA_OFFSET_B", "REAL", "Input", 0.0));
                RobotInputVars.Add(Create("G_TABLA_OFFSET_C", "REAL", "Input", 0.0));
                // --- DURUM MESAJI ---
                RobotInputVars.Add(Create("G_DURUM_MESAJ", "INT", "Input", 0));           // Durum mesaj kodu (INT)
                // --- RESET ---
                RobotInputVars.Add(Create("G_RESET", "BOOL", "Input", false));            // Reset komutu bayragi
                // --- SISTEM KONTROL (Her iki robot) ---
                RobotInputVars.Add(Create("G_R1_HOME", "BOOL", "Input", false));           // Robot 1 home pozisyonunda
                RobotInputVars.Add(Create("G_R2_HOME", "BOOL", "Input", false));           // Robot 2 home pozisyonunda
                // --- INFICON SNIFFER OLCUM (Her iki robot) ---
                RobotInputVars.Add(Create("G_SNIFFER_OLCUM_TETIK", "BOOL", "Input", false)); // Robot -> PC : Sniffer olcum baslat/durdur (R1 + R2)
                RobotInputVars.Add(Create("G_SNIFFER_OLCUM_BITTI", "BOOL", "Input", false)); // Robot -> PC : Sniffer olcum tamamlandi (R1 + R2)
                RobotInputVars.Add(Create("G_SNIFFER_DEGER", "REAL", "Input", 0.0));      // Sniffer olcum degeri (geri okuma)
                RobotInputVars.Add(Create("G_SNIFFER_READY", "BOOL", "Input", false));    // PC -> Robot : INFICON cihazi hazir (geri okuma)
                RobotInputVars.Add(Create("G_AKTIF_CIZGI", "INT", "Input", 0));           // Robot 2 aktif sniffer cizgi no
                RobotInputVars.Add(Create("G_TOPLAM_CIZGI", "INT", "Input", 0));          // Robot 2 toplam cizgi sayisi
                RobotInputVars.Add(Create("G_NOK_CIZGI", "INT", "Input", 0));             // Robot 2 son NOK cizgi no
                // --- ROBOT 2 SLIDER (KL100) DURUM ---
                RobotInputVars.Add(Create("G_SLIDER_HAREKET", "BOOL", "Input", false));    // Slider hareket ediyor
                RobotInputVars.Add(Create("G_SLIDER_TAMAM", "BOOL", "Input", false));      // Slider hedefe ulasti
                RobotInputVars.Add(Create("G_SLIDER_HOME", "BOOL", "Input", false));       // Slider home pozisyonunda
                RobotInputVars.Add(Create("G_SLIDER_AKTUEL_POZ", "REAL", "Input", 0.0));   // Slider aktuel pozisyon (mm)
                // --- ROBOT 2 TABLA OFFSET DURUMU ---
                RobotInputVars.Add(Create("G_TABLA_OFFSET_HAZIR", "BOOL", "Input", false)); // Robot 1'den tabla offset alindi mi
            }

            // ═══════════════════════════════════════════════════════════════
            // PLC → ROBOT DEĞİŞKENLERİ (Output - Robota gönderilecek sinyaller)
            // PC uygulamasi bu degiskenlere yazar, robot okur
            // ═══════════════════════════════════════════════════════════════
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
                RobotOutputVars.Add(Create("G_BORU_OLCUM_TAMAM", "BOOL", "Output", false));  // Boru olcum tamamlandi (PC -> Robot)
                RobotOutputVars.Add(Create("G_TABLA_OLCUM_TAMAM", "BOOL", "Output", false)); // Tabla olcum tamamlandi (PC -> Robot)
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
                RobotOutputVars.Add(Create("G_SNIFFER_DEGER", "REAL", "Output", 0.0));    // Sniffer olcum degeri
                // --- ROBOT 2 SLIDER KONTROL (PC -> Robot 2) ---
                RobotOutputVars.Add(Create("G_SLIDER_HEDEF_POZ", "REAL", "Output", 0.0)); // Slider hedef pozisyon (mm)
                RobotOutputVars.Add(Create("G_SLIDER_HAREKET", "BOOL", "Output", false)); // Slider hareket komutu (PC -> Robot 2)
                RobotOutputVars.Add(Create("G_HEDEF_ISTASYON", "INT", "Output", 0));      // Hedef istasyon no (1=Ist1, 2=Ist2, 3=Ist3, 4=Bakim)
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

            settings["Robot_IpAddress"] = Robot_IpAddress;
            settings["Robot_Port"] = Robot_Port;
            settings["Robot_ReadSpeed"] = Robot_ReadSpeed;

            settings["Plc_IpAddress"] = Plc_IpAddress;
            settings["Plc_Port"] = Plc_Port;

            settings["Gocator_IpAddress"] = Gocator_IpAddress;
            settings["Gocator_Port"] = Gocator_Port;

            // KL100 Slider R1/R2 Home sinyal se\u00e7imleri
            settings["KL100_R1Home"] = KL100_Robot1HomeSignal ?? "";
            settings["KL100_R2Home"] = KL100_Robot2HomeSignal ?? "";

            StartAutomationListener(); 
        }

        // --- PLC DİNLEYİCİSİ (Çift Trigger) ---
        private static PlcVariable _currentTriggerVar;  // Boru ölçüm tetik
        private static PlcVariable _currentTriggerVar2; // Tabla kaçıklık tetik

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
                _currentTriggerVar2.PropertyChanged -= TriggerVar_PropertyChanged;
                _currentTriggerVar2 = null;
            }

            if (!string.IsNullOrEmpty(Auto_TriggerTag2))
            {
                var triggerVar2 = FindPlcVarByName(Auto_TriggerTag2);
                if (triggerVar2 != null)
                {
                    _currentTriggerVar2 = triggerVar2;
                    _currentTriggerVar2.PropertyChanged += TriggerVar_PropertyChanged;
                    OnAutomationLog?.Invoke($"Tabla tetik devrede: {Auto_TriggerTag2} izleniyor.");
                }
                else
                {
                    OnAutomationLog?.Invoke($"⚠ UYARI: Tabla Trigger Tag '{Auto_TriggerTag2}' bulunamadı.");
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
        private static async Task WriteToAllRobotsAsync(string varName, string value)
        {
            // 1. GlobalData output var güncelle
            var gVar = RobotOutputVars.FirstOrDefault(v => v.Name == varName);
            if (gVar != null) gVar.Value = value;

            // 2. PLC'ye yaz
            var plcVar = PlcService.Instance?.OutputVariables?.FirstOrDefault(v => v.Name == varName);
            if (plcVar != null)
            {
                plcVar.Value = value;
                try { await PlcService.Instance.WriteAsync(plcVar, value); } catch { }
            }

            // 3. Tüm robotlara yaz
            var robots = Utilities.KukaRobotManager.Instance?.Robots;
            if (robots != null)
            {
                foreach (var r in robots)
                {
                    if (r.IsConnected)
                    {
                        try { await r.WriteVariableAsync(varName, value); } catch { }
                    }
                }
            }
        }

        // ─── ROBOT SİNYAL İZLEME (GLOBAL - SAYFA BAĞIMSIZ) ───
        // G_OLCUM_TETIK ve G_SNIFFER_OLCUM_TETIK sinyallerini GlobalData'dan dinler.
        // Hangi sayfada olursa olsun tetik alınır ve işlenir.
        // ════════════════════════════════════════════════════════════

        private static bool _robotSignalMonitoringActive = false;
        private static bool _olcumTetikProcessing = false;
        private static bool _snifferOlcumProcessing = false;

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
            }

            _robotSignalMonitoringActive = true;
            OnAutomationLog?.Invoke("Robot sinyal izleme başlatıldı (Global - sayfa bağımsız).");
        }

        private static void CheckRobotTriggerSignalGlobal(PlcVariable changedVar, KukaRobotInstance robot, int robotNo)
        {
            bool isTrue = changedVar.Value?.ToUpper() == "TRUE" || changedVar.Value == "1";
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
                // 1. JOB_INDEX'i robottan oku
                var jobIndexVar = robot.InputVars.FirstOrDefault(v => v.Name == "G_JOB_INDEX");
                if (jobIndexVar != null) int.TryParse(jobIndexVar.Value, out jobIndex);

                OnAutomationLog?.Invoke($"[Robot {robotNo}] Ölçüm tetik alındı (JOB_INDEX={jobIndex})");

                // 2. Aktif RFID'den job adını bul
                string currentRfid = AktuelRfid;
                var recipe = KnownRfids.FirstOrDefault(r => r.Id == currentRfid);
                string jobName = null;

                if (recipe?.JobSequence != null && jobIndex >= 0 && jobIndex < recipe.JobSequence.Count)
                    jobName = recipe.JobSequence[jobIndex];

                if (string.IsNullOrEmpty(jobName))
                {
                    OnAutomationLog?.Invoke($"[Robot {robotNo}] Job bulunamadı (RFID={currentRfid}, Index={jobIndex})");
                    await WriteToAllRobotsAsync("G_OLCUM_OK", "FALSE");
                    return;
                }

                // 4. Gocator job yükle
                OnAutomationLog?.Invoke($"[Robot {robotNo}] Job yükleniyor: {jobName}");
                bool loadOk = await GocatorJobLogic.LoadJob(jobName, s => OnAutomationLog?.Invoke($"[Gocator] {s}"));
                if (!loadOk)
                {
                    OnAutomationLog?.Invoke($"[Robot {robotNo}] Job yüklenemedi: {jobName}");
                    await WriteToAllRobotsAsync("G_OLCUM_OK", "FALSE");
                    return;
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
                        try
                        {
                            await PlcService.Instance.RunOnUiAsync(() =>
                            {
                                TablaLastMeasurements.Clear();
                                foreach (var m in results) TablaLastMeasurements.Add(m);
                                SaveTablaMeasurements();
                                LastMeasurements.Clear();
                                SaveMeasurements();
                            });
                        }
                        catch { }

                        string[] tablaOffsets = { "G_TABLA_OFFSET_X", "G_TABLA_OFFSET_Y", "G_TABLA_OFFSET_Z",
                                                  "G_TABLA_OFFSET_A", "G_TABLA_OFFSET_B", "G_TABLA_OFFSET_C" };
                        for (int i = 0; i < Math.Min(results.Count, tablaOffsets.Length); i++)
                            await WriteToAllRobotsAsync(tablaOffsets[i], results[i].Value.ToString("F3"));

                        await WriteToAllRobotsAsync("G_TABLA_OFFSET_HAZIR", "TRUE");
                        OnAutomationLog?.Invoke($"[Robot {robotNo}] Tabla ölçüm OK - {results.Count} offset yazıldı");
                    }
                    else
                    {
                        // --- BORU ÖLÇÜM (JOB 1..N) ---
                        string[] boruOffsets = { "G_OFFSET_X", "G_OFFSET_Y", "G_OFFSET_Z",
                                                 "G_OFFSET_A", "G_OFFSET_B", "G_OFFSET_C" };

                        if (CalibrationService.Instance.IsCalibrated && results.Count >= 6)
                        {
                            // Hand-Eye kalibrasyon ile sensor → base donusumu
                            double gocX = results[0].Value, gocY = results[1].Value, gocZ = results[2].Value;
                            double gocA = results[3].Value, gocB = results[4].Value, gocC = results[5].Value;

                            // Sensor koordinatlarinda hedef matris olustur
                            var sensorTarget = new KukaPose(gocX, gocY, gocZ, gocA, gocB, gocC).ToMatrix();

                            // Robot flange → base donusumu uygula
                            var basePose = await CalibrationService.Instance.LocateFromRobotAsync(robot, sensorTarget);

                            if (basePose != null)
                            {
                                // UI'da dönüştürülmüş verileri göster
                                try { await PlcService.Instance.RunOnUiAsync(() => PopulateTransformedMeasurements(basePose)); } catch { }

                                double[] baseVals = { basePose.X, basePose.Y, basePose.Z,
                                                      basePose.A, basePose.B, basePose.C };
                                for (int i = 0; i < 6; i++)
                                    await WriteToAllRobotsAsync(boruOffsets[i], baseVals[i].ToString("F3"));

                                OnAutomationLog?.Invoke($"[Robot {robotNo}] Boru ölçüm OK (Job {jobIndex}) - HandEye dönüşüm uygulandı: " +
                                    $"X={basePose.X:F2} Y={basePose.Y:F2} Z={basePose.Z:F2} A={basePose.A:F2} B={basePose.B:F2} C={basePose.C:F2}");
                            }
                            else
                            {
                                OnAutomationLog?.Invoke($"[Robot {robotNo}] UYARI: HandEye dönüşüm başarısız, ham değerler yazılıyor");
                                for (int i = 0; i < Math.Min(results.Count, boruOffsets.Length); i++)
                                    await WriteToAllRobotsAsync(boruOffsets[i], results[i].Value.ToString("F3"));
                            }
                        }
                        else
                        {
                            // Kalibrasyon yoksa ham değerleri yaz
                            for (int i = 0; i < Math.Min(results.Count, boruOffsets.Length); i++)
                                await WriteToAllRobotsAsync(boruOffsets[i], results[i].Value.ToString("F3"));

                            OnAutomationLog?.Invoke($"[Robot {robotNo}] Boru ölçüm OK (Job {jobIndex}) - {results.Count} ham offset yazıldı (kalibrasyon yok)");
                        }
                    }

                    // Başarı sinyali
                    await WriteToAllRobotsAsync("G_OLCUM_OK", "TRUE");

                    // PLC çıktı sinyali
                    if (jobIndex == 0)
                        SetTablaMeasurementSignal();
                    else
                        SetMeasurementSignal();
                }
                else
                {
                    OnAutomationLog?.Invoke($"[Robot {robotNo}] Ölçüm BAŞARISIZ (Job {jobIndex}: {jobName})");
                    await WriteToAllRobotsAsync("G_OLCUM_OK", "FALSE");
                }
            }
            catch (Exception ex)
            {
                OnAutomationLog?.Invoke($"[Robot {robotNo}] Ölçüm tetik hatası: {ex.Message}");
                try
                {
                    await WriteToAllRobotsAsync("G_OLCUM_OK", "FALSE");
                }
                catch { }
            }
            finally
            {
                // TAMAM pulse - robot KRL bu sinyali REPEAT/UNTIL dongusunde bekliyor
                try
                {
                    string tamamSignal = jobIndex == 0 ? "G_TABLA_OLCUM_TAMAM" : "G_BORU_OLCUM_TAMAM";
                    await WriteToAllRobotsAsync(tamamSignal, "TRUE");
                    await Task.Delay(500);
                    await WriteToAllRobotsAsync(tamamSignal, "FALSE");
                    OnAutomationLog?.Invoke($"[Robot {robotNo}] {tamamSignal} pulse gönderildi");
                }
                catch { }

                _olcumTetikProcessing = false;
                OlcumInProgress = false;
            }
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
                string aktifInfo = "";
                if (robotNo == 1)
                {
                    var noktaVar = robot.InputVars.FirstOrDefault(v => v.Name == "G_AKTIF_NOKTA");
                    aktifInfo = noktaVar != null ? $"Nokta={noktaVar.Value}" : "";
                }
                else
                {
                    var cizgiVar = robot.InputVars.FirstOrDefault(v => v.Name == "G_AKTIF_CIZGI");
                    aktifInfo = cizgiVar != null ? $"Çizgi={cizgiVar.Value}" : "";
                }

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

                // INFICON ölçüm
                bool olcumOK = true;
                double olcumDeger = 0.0;
                // *** INFICON SDK ENTEGRASYON NOKTASI ***
                // var inficonResult = await InficonService.Instance.MeasureAsync();
                // olcumOK = inficonResult.IsPass;
                // olcumDeger = inficonResult.LeakRate;

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

        private static void TriggerVar_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Value" || e.PropertyName == "CurrentValue")
            {
                var plcVar = sender as PlcVariable;
                if (plcVar != null && (plcVar.Value == "1" || plcVar.Value?.ToLower() == "true")) _ = RunAutomationSequence();
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

                if (found)
                {
                    // OnAutomationLog?.Invoke($"✓ Ölçüm sinyali sıfırlandı: {targetTag} = 0");
                    OnAutomationStatusChanged?.Invoke();
                }
                else
                {
                    OnAutomationLog?.Invoke($"⚠ Reset için Tag bulunamadı: {targetTag}");
                }
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

                if (found)
                {
                    OnAutomationLog?.Invoke($"✓ Ölçüm sinyali gönderildi: {targetTag} = 1");
                    OnAutomationStatusChanged?.Invoke();
                }
                else
                {
                    OnAutomationLog?.Invoke($"⚠ Sinyal için Tag bulunamadı: {targetTag}");
                }
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

                OnAutomationLog?.Invoke($"✓ Tabla sinyal sıfırlandı: {targetTag} = 0");
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

                bool found = false;

                var outputVar = GeneralOutputVars.FirstOrDefault(v => v.Name == targetTag);
                if (outputVar != null) { outputVar.CurrentValue = 1; found = true; }

                var inputVar = GeneralInputVars.FirstOrDefault(v => v.Name == targetTag);
                if (inputVar != null) { inputVar.CurrentValue = 1; found = true; }

                if (PlcService.Instance != null)
                {
                    var plcVar = PlcService.Instance.OutputVariables.FirstOrDefault(v => v.Name == targetTag);
                    if (plcVar != null) { await PlcService.Instance.WriteAsync(plcVar, 1); plcVar.CurrentValue = 1; found = true; }
                    else
                    {
                        var plcIn = PlcService.Instance.InputVariables.FirstOrDefault(v => v.Name == targetTag);
                        if (plcIn != null) { await PlcService.Instance.WriteAsync(plcIn, 1); plcIn.CurrentValue = 1; found = true; }
                    }
                }

                if (found)
                {
                    OnAutomationLog?.Invoke($"✓ Tabla sinyal gönderildi: {targetTag} = 1");
                    OnAutomationStatusChanged?.Invoke();
                }
                else
                {
                    OnAutomationLog?.Invoke($"⚠ Tabla sinyal Tag bulunamadı: {targetTag}");
                }
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

            var recipe = KnownRfids.FirstOrDefault(r =>
                string.Equals(r.Id, currentRfid, StringComparison.OrdinalIgnoreCase));
            if (recipe == null) return;

            recipe.CurrentJobIndex = jobIndex;

            // ═══ SEÇİLİ INDEX'İN SNİFFER SÜRESİNİ OUTPUT TAG'E YAZ (ms) ═══
            UpdateSnifferDurationOutput(recipe, jobIndex);
        }

        /// <summary>
        /// Seçili job index'inin sniffer ölçüm süresini SNIFFER_OLCUM_SURE output değişkenine yazar.
        /// Auto_Page'deki ConnectToPlcVariable / ConnectToPlcVariable2 PropertyChanged event'leri
        /// ile PlcTag ve PlcTag2'ye otomatik yazılır.
        /// </summary>
        private static void UpdateSnifferDurationOutput(RfidDef recipe, int jobIndex)
        {
            double snifferMs = 0;
            if (jobIndex >= 0 && jobIndex < recipe.SnifferDurations.Count)
            {
                snifferMs = recipe.SnifferDurations[jobIndex];
            }

            // GeneralOutputVars tablosundaki değişkeni güncelle
            // PropertyChanged tetiklenir → ConnectToPlcVariable PlcTag'e yazar
            //                            → ConnectToPlcVariable2 PlcTag2'ye yazar
            var snifferVar = GeneralOutputVars.FirstOrDefault(v => v.Name == "SNIFFER_OLCUM_SURE");
            if (snifferVar != null)
            {
                snifferVar.CurrentValue = snifferMs;
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
        public static async Task RunAutomationSequence()
        {
            if (IsProcessRunning) return;
            if (OlcumInProgress) return; // HandleOlcumTetikAsync zaten calisiyor
            IsProcessRunning = true;
            OlcumInProgress = true;
            ProcessStatus = "İŞLENİYOR...";

            // ▼▼▼ SİNYAL SIFIRLA (Ölçüm başlıyor) ▼▼▼
            ResetMeasurementSignal();
            ResetTablaMeasurementSignal();
            ResetAllJobStatuses();

            try
            {
                // RFID Variable Bul (PLC + Robot tüm kaynaklarda ara)
                var rfidVar = FindPlcVarByName(Auto_RfidTag);

                // Index Variable Bul (G_JOB_INDEX gibi robot değişkenleri de dahil)
                var indexVar = FindPlcVarByName(Auto_IndexTag);

                string currentRfid = rfidVar?.Value ?? "---";
                string currentIndex = indexVar?.Value ?? "0";

                OnAutomationLog?.Invoke($"Tetik: {currentRfid} (Index: {currentIndex})");

                var recipe = KnownRfids.FirstOrDefault(r => r.Id == currentRfid);
                if (recipe == null) throw new Exception("Tanımsız RFID");

                int.TryParse(currentIndex, out int idx);
                if (idx < 0 || idx >= recipe.JobSequence.Count) throw new Exception("Geçersiz Index");

                string jobName = recipe.JobSequence[idx];
                ProcessStatus = $"JOB: {jobName}";

                // ▶ Anlik index gostergesini guncelle (kamera sayfasinda vurgulu satir)
                UpdateCurrentJobIndex(idx);

                bool loadOk = await App4.Utilities.GocatorJobLogic.LoadJob(jobName, (s) => OnAutomationLog?.Invoke(s));
                if (!loadOk) throw new Exception("Job yüklenemedi");

                ProcessStatus = "ÖLÇÜM...";
                var (status, measurements) = await App4.Utilities.ReceiveMeasurementLogic.ReceiveAndProcessMeasurements((s) => OnAutomationLog?.Invoke(s), null);

                if (status == 1 && measurements != null)
                {
                    // Job durumunu OK olarak guncelle (idx 0-based, UpdateJobStatus 1-based)
                    UpdateJobStatus(idx + 1, "OK");

                    // ▼▼▼ SİNYAL GÖNDER (Ölçüm tamamlandı) ▼▼▼
                    if (idx == 0)
                        SetTablaMeasurementSignal();
                    else
                        SetMeasurementSignal();

                    OnAutomationLog?.Invoke("PLC Yazma işlemi başlıyor...");

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

                        // Tabloyu Genişlet (Eğer eksik varsa)
                        while (targetRows.Count < measurements.Count)
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

                        // Değerleri Eşle
                        for (int i = 0; i < measurements.Count; i++)
                        {
                            var row = targetRows[i];
                            var meas = measurements[i];

                            row.Value = meas.Value.ToString();
                            row.Status = "WAIT"; // Önce WAIT (PLC yazılıyor)
                            row.StatusColor = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 165, 0));

                            tagsToWrite.Add(row.SelectedTag);
                            valuesToWrite.Add(meas.Value);
                        }
                        saveAction();
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
                        for (int i = 0; i < Math.Min(measurements.Count, offsets.Length); i++)
                            await WriteToAllRobotsAsync(offsets[i], measurements[i].Value.ToString("F3"));
                        await WriteToAllRobotsAsync("G_TABLA_OFFSET_HAZIR", "TRUE");
                    }
                    else
                    {
                        offsets = new[] { "G_OFFSET_X", "G_OFFSET_Y", "G_OFFSET_Z",
                                          "G_OFFSET_A", "G_OFFSET_B", "G_OFFSET_C" };

                        if (CalibrationService.Instance.IsCalibrated && measurements.Count >= 6)
                        {
                            // Hand-Eye kalibrasyon ile sensor → base donusumu
                            double gocX = measurements[0].Value, gocY = measurements[1].Value, gocZ = measurements[2].Value;
                            double gocA = measurements[3].Value, gocB = measurements[4].Value, gocC = measurements[5].Value;

                            var sensorTarget = new KukaPose(gocX, gocY, gocZ, gocA, gocB, gocC).ToMatrix();

                            // Robot 1'i (Gocator monte) bul
                            var robot = KukaRobotManager.Instance?.Robots?.FirstOrDefault();
                            KukaPose basePose = null;
                            if (robot != null)
                                basePose = await CalibrationService.Instance.LocateFromRobotAsync(robot, sensorTarget);

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

                    await WriteToAllRobotsAsync("G_OLCUM_OK", "TRUE");
                    OnAutomationLog?.Invoke("Robot write-back: Offset + G_OLCUM_OK yazildi");

                    // TAMAM pulse (robot KRL REPEAT/UNTIL dongusunde bekliyor)
                    string tamamSignal = idx == 0 ? "G_TABLA_OLCUM_TAMAM" : "G_BORU_OLCUM_TAMAM";
                    await WriteToAllRobotsAsync(tamamSignal, "TRUE");
                    await Task.Delay(500);
                    await WriteToAllRobotsAsync(tamamSignal, "FALSE");

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
                    OnAutomationLog?.Invoke("Olcum alinamadi: Cikti yok veya zaman asimi.");

                    // Robot'a basarisizlik bildirimi
                    await WriteToAllRobotsAsync("G_OLCUM_OK", "FALSE");
                    string tamamSignalErr = idx == 0 ? "G_TABLA_OLCUM_TAMAM" : "G_BORU_OLCUM_TAMAM";
                    await WriteToAllRobotsAsync(tamamSignalErr, "TRUE");
                    await Task.Delay(500);
                    await WriteToAllRobotsAsync(tamamSignalErr, "FALSE");
                }
            }
            catch (Exception ex)
            {
                ProcessStatus = "HATA";
                UpdateCurrentJobIndex(-1);
                OnAutomationLog?.Invoke($"Hata: {ex.Message}");

                // Robot'a hata bildirimi
                try
                {
                    await WriteToAllRobotsAsync("G_OLCUM_OK", "FALSE");
                    // Exception durumunda her iki TAMAM sinyalini de pulse et
                    await WriteToAllRobotsAsync("G_BORU_OLCUM_TAMAM", "TRUE");
                    await WriteToAllRobotsAsync("G_TABLA_OLCUM_TAMAM", "TRUE");
                    await Task.Delay(500);
                    await WriteToAllRobotsAsync("G_BORU_OLCUM_TAMAM", "FALSE");
                    await WriteToAllRobotsAsync("G_TABLA_OLCUM_TAMAM", "FALSE");
                }
                catch { }
            }
            finally
            {
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
        private static readonly string _appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "App4");

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

                    // 3. PLC Service ayarlarını kopyala
                    string plcVarsPath = Path.Combine(_appDataFolder, "PlcService_Variables.json");
                    if (File.Exists(plcVarsPath))
                        File.Copy(plcVarsPath, Path.Combine(tempDir, "PlcService_Variables.json"), true);

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