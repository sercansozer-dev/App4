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

        // --- GLOBAL LİSTELER ---
        public static ObservableCollection<RfidDef> KnownRfids { get; private set; } = new();
        public static ObservableCollection<StationViewModel> Stations { get; private set; } = new();
        public static ObservableCollection<SystemCheckItem> SystemCheckList { get; private set; } = new();
        public static ObservableCollection<GocatorMeasurement> LastMeasurements { get; private set; } = new();
        public static ObservableCollection<PlcTransferItem> PlcTransferRows { get; private set; } = new();

        // PLC Değişken Listeleri
        public static ObservableCollection<PlcVariable> GeneralInputVars { get; private set; } = new();
        public static ObservableCollection<PlcVariable> GeneralOutputVars { get; private set; } = new();
        public static ObservableCollection<PlcVariable> Station1Vars { get; private set; } = new();
        public static ObservableCollection<PlcVariable> Station2Vars { get; private set; } = new();
        public static ObservableCollection<PlcVariable> Station3Vars { get; private set; } = new();
        public static ObservableCollection<PlcVariable> Station4Vars { get; private set; } = new();
        public static ObservableCollection<PlcVariable> Station1Outputs { get; private set; } = new();
        public static ObservableCollection<PlcVariable> Station2Outputs { get; private set; } = new();
        public static ObservableCollection<PlcVariable> Station3Outputs { get; private set; } = new();
        public static ObservableCollection<PlcVariable> Station4Outputs { get; private set; } = new();

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

        // EVENTLER VE DURUM
        public static event Action<string> OnAutomationLog;
        public static event Action OnAutomationStatusChanged;

        private static string _processStatus = "HAZIR";
        public static string ProcessStatus { get => _processStatus; set { if (_processStatus != value) { _processStatus = value; OnAutomationStatusChanged?.Invoke(); } } }

        private static bool _isProcessRunning = false;
        public static bool IsProcessRunning { get => _isProcessRunning; set { if (_isProcessRunning != value) { _isProcessRunning = value; OnAutomationStatusChanged?.Invoke(); } } }

        // --- BAŞLATMA ---
        public static void Initialize()
        {
            if (_isInitialized) return;
            LoadRfids();
            InitializeStations();
            LoadStationStates();
            InitializeVariables();
            LoadPlcVariableTagsFromFile();
            LoadSystemChecks();
            LoadMeasurements();
            LoadTransferRows(); // <-- Burası eklendi
            LoadAutomationSettings();
            StartAutomationListener();
            _isInitialized = true;
            
        }

        // --- METOTLAR ---
        public static void SaveSystemChecks() { try { string json = System.Text.Json.JsonSerializer.Serialize(SystemCheckList, new JsonSerializerOptions { WriteIndented = true }); File.WriteAllText(_systemChecksFilePath, json); } catch { } }
        private static void LoadSystemChecks() { try { if (File.Exists(_systemChecksFilePath)) { var list = System.Text.Json.JsonSerializer.Deserialize<List<SystemCheckItem>>(File.ReadAllText(_systemChecksFilePath)); if (list != null) foreach (var item in list) SystemCheckList.Add(item); } } catch { } }

        public static void SaveMeasurements() { try { string json = System.Text.Json.JsonSerializer.Serialize(LastMeasurements, new JsonSerializerOptions { WriteIndented = true }); File.WriteAllText(_measurementsFilePath, json); } catch { } }
        private static void LoadMeasurements() { try { if (File.Exists(_measurementsFilePath)) { var list = System.Text.Json.JsonSerializer.Deserialize<List<GocatorMeasurement>>(File.ReadAllText(_measurementsFilePath)); if (list != null) { LastMeasurements.Clear(); foreach (var item in list) LastMeasurements.Add(item); } } } catch { } }

        private static void LoadRfids() { try { if (File.Exists(_rfidFilePath)) { var list = System.Text.Json.JsonSerializer.Deserialize<List<RfidDef>>(File.ReadAllText(_rfidFilePath)); if (list != null) foreach (var item in list) KnownRfids.Add(item); } else { KnownRfids.Add(new RfidDef { Id = "RF123", Description = "Klima A Tipi" }); SaveRfids(); } KnownRfids.CollectionChanged += (s, e) => SaveRfids(); } catch { } }
        public static void SaveRfids() { try { File.WriteAllText(_rfidFilePath, System.Text.Json.JsonSerializer.Serialize(KnownRfids, new JsonSerializerOptions { WriteIndented = true })); } catch { } }

        private static void InitializeStations()
        {
            Stations.Add(new ExtendedStationViewModel { Name = "İSTASYON 1", Description = "Klima Dış Ünite", Mode = StationMode.Auto, StatusTag = "ST1_STATUS", AlarmTag = "ST1_ALARM", ProducingTag = "ST1_PRODUCING", ProductionCountTag = "ST1_PROD_COUNT", EfficiencyTag = "ST1_EFFICIENCY", CurrentRfidTag = "ST1_RFID_ACT", AllowedRfid = "RF123", CurrentRfid = "RF123", RfidOpMode = RfidOperationMode.Mixed });
            Stations.Add(new ExtendedStationViewModel { Name = "İSTASYON 2", Description = "Klima Dış Ünite", Mode = StationMode.Auto, StatusTag = "ST2_STATUS", AlarmTag = "ST2_ALARM", ProducingTag = "ST2_PRODUCING", ProductionCountTag = "ST2_PROD_COUNT", EfficiencyTag = "ST2_EFFICIENCY", CurrentRfidTag = "ST2_RFID_ACT", AllowedRfid = "RF123", CurrentRfid = "RF123", RfidOpMode = RfidOperationMode.Mixed });
            Stations.Add(new ExtendedStationViewModel { Name = "İSTASYON 3", Description = "Boş İstasyon", Mode = StationMode.Manual, StatusTag = "ST3_STATUS", AlarmTag = "ST3_ALARM", ProducingTag = "ST3_PRODUCING", ProductionCountTag = "ST3_PROD_COUNT", EfficiencyTag = "ST3_EFFICIENCY", CurrentRfidTag = "ST3_RFID_ACT", AllowedRfid = "RF123", RfidOpMode = RfidOperationMode.Mixed });
            Stations.Add(new ExtendedStationViewModel { Name = "İSTASYON 4", Description = "Klima Hatalı", Mode = StationMode.Bypass, StatusTag = "ST4_STATUS", AlarmTag = "ST4_ALARM", ProducingTag = "ST4_PRODUCING", ProductionCountTag = "ST4_PROD_COUNT", EfficiencyTag = "ST4_EFFICIENCY", CurrentRfidTag = "ST4_RFID_ACT", AllowedRfid = "RF123", CurrentRfid = "ERR01", RfidOpMode = RfidOperationMode.Mixed });
        }
        public static void SaveStationStates() { try { var states = Stations.Select(s => { var ext = s as ExtendedStationViewModel; return new { Name = s.Name, Mode = (int)s.Mode, RfidOpMode = ext != null ? (int)ext.RfidOpMode : 0, TargetRfid = ext != null ? ext.TargetRfid : "" }; }).ToList(); File.WriteAllText(_stationStateFilePath, System.Text.Json.JsonSerializer.Serialize(states, new JsonSerializerOptions { WriteIndented = true })); } catch { } }
        private static void LoadStationStates() { try { if (File.Exists(_stationStateFilePath)) { var savedStates = System.Text.Json.JsonSerializer.Deserialize<List<JsonElement>>(File.ReadAllText(_stationStateFilePath)); foreach (var item in savedStates) { if (item.TryGetProperty("Name", out var nameProp)) { var station = Stations.FirstOrDefault(s => s.Name == nameProp.GetString()); if (station != null) { if (item.TryGetProperty("Mode", out var modeProp)) station.Mode = (StationMode)modeProp.GetInt32(); if (station is ExtendedStationViewModel ext) { if (item.TryGetProperty("RfidOpMode", out var rfid)) ext.RfidOpMode = (RfidOperationMode)rfid.GetInt32(); if (item.TryGetProperty("TargetRfid", out var target)) ext.TargetRfid = target.GetString(); } } } } } } catch { } }

        private static void InitializeVariables()
        {
            PlcVariable Create(string name, string type, string dir, object val) => new PlcVariable { Name = name, Type = type, Direction = dir, CurrentValue = val, IsEditable = true };
            void AddVars(ObservableCollection<PlcVariable> c, int id) { c.Add(Create($"ST{id}_STATUS", "STRING", "Input", "Unknown")); c.Add(Create($"ST{id}_ALARM", "BOOL", "Input", false)); c.Add(Create($"ST{id}_MODE", "STRING", "Input", "Manual")); c.Add(Create($"ST{id}_PRODUCING", "BOOL", "Input", false)); c.Add(Create($"ST{id}_PROD_COUNT", "WORD", "Input", "0")); c.Add(Create($"ST{id}_EFFICIENCY", "WORD", "Input", "0")); c.Add(Create($"ST{id}_RFID_ACT", "STRING", "Input", "")); }
            void AddOutputs(ObservableCollection<PlcVariable> c, int id) { c.Add(new PlcVariable { Name = $"ST{id}_RFID_MODE", Value = "0", Description = "RFID Mod", PlcTag = $"DB10.W{(id - 1) * 20}.0" }); c.Add(new PlcVariable { Name = $"ST{id}_RFID_TARGET", Value = "0", Description = "Hedef RFID", PlcTag = $"DB10.W{(id - 1) * 20}.4" }); c.Add(new PlcVariable { Name = $"ST{id}_ID_MATCHED", Value = "0", Description = "ID Eşleşti", PlcTag = $"DB10.W{(id - 1) * 20}.20" }); c.Add(new PlcVariable { Name = $"ST{id}_PROCESS_RESULT", Value = "0", Description = "Sonuç", PlcTag = $"DB10.W{(id - 1) * 20}.22" }); c.Add(new PlcVariable { Name = $"ST{id}_CONVEYOR_PERM", Value = "0", Description = "Konveyör", PlcTag = $"DB10.W{(id - 1) * 20}.24" }); c.Add(new PlcVariable { Name = $"ST{id}_MODE_CMD", Value = "1", Description = "Mod Cmd", PlcTag = $"DB10.W{(id - 1) * 20}.26" }); }
            GeneralInputVars.Add(Create("SLIDER_POS_ACT", "WORD", "Input", "0")); 
            GeneralInputVars.Add(Create("ROBOT_SPEED", "WORD", "Input", "100")); 
            GeneralInputVars.Add(Create("GOCATOR_STATUS", "STRING", "Input", "READY")); 
            GeneralInputVars.Add(Create("SAFETY_OK", "BOOL", "Input", true)); 
            GeneralInputVars.Add(Create("LINE_RUNNING", "BOOL", "Input", false)); 
            GeneralInputVars.Add(Create("LINE_AUTO_MODE", "BOOL", "Input", false)); 
            GeneralInputVars.Add(Create("SYS_RESET_FEEDBACK", "BOOL", "Input", false));
            // ▼▼▼ KAMERA ÖLÇÜM SİNYALİ - Yeni ölçüm geldiğinde 1, sıfırlandığında 0 ▼▼▼
            GeneralInputVars.Add(Create("MEASUREMENT_NEW_DATA", "BOOL", "Input", false));
            GeneralOutputVars.Add(Create("CMD_LINE_START", "BOOL", "Output", false)); 
            GeneralOutputVars.Add(Create("CMD_LINE_STOP", "BOOL", "Output", false)); 
            GeneralOutputVars.Add(Create("CMD_LINE_RESET", "BOOL", "Output", false));
            // ▼▼▼ KAMERA ÖLÇÜM OUTPUT - Manuel başla butonuyla sıfırlanır ▼▼▼
            GeneralOutputVars.Add(Create("MEASUREMENT_TRIGGER_OUT", "BOOL", "Output", false));
            AddVars(Station1Vars, 1); AddVars(Station2Vars, 2); AddVars(Station3Vars, 3); AddVars(Station4Vars, 4); AddOutputs(Station1Outputs, 1); AddOutputs(Station2Outputs, 2); AddOutputs(Station3Outputs, 3); AddOutputs(Station4Outputs, 4);
        }

        public static void SavePlcVariableTagsToFile() { try { object Map(ObservableCollection<PlcVariable> l) => l.Select(v => new { name = v.Name, plcTag = v.PlcTag, value = v.Value }).ToList(); var data = new { GeneralInputVars = Map(GeneralInputVars), GeneralOutputVars = Map(GeneralOutputVars), Station1Vars = Map(Station1Vars), Station1Outputs = Map(Station1Outputs), Station2Vars = Map(Station2Vars), Station2Outputs = Map(Station2Outputs), Station3Vars = Map(Station3Vars), Station3Outputs = Map(Station3Outputs), Station4Vars = Map(Station4Vars), Station4Outputs = Map(Station4Outputs) }; File.WriteAllText(_autoPageVariablesFilePath, System.Text.Json.JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true })); } catch { } }
        private static void LoadPlcVariableTagsFromFile() { try { if (File.Exists(_autoPageVariablesFilePath)) { var json = File.ReadAllText(_autoPageVariablesFilePath); var data = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(json); void Load(string p, ObservableCollection<PlcVariable> t) { if (data.TryGetProperty(p, out var a)) foreach (var i in a.EnumerateArray()) { if (i.TryGetProperty("name", out var n)) { var v = t.FirstOrDefault(x => x.Name == n.GetString()); if (v != null) { if (i.TryGetProperty("plcTag", out var pt)) v.PlcTag = pt.GetString(); if (i.TryGetProperty("value", out var val) && val.ValueKind != JsonValueKind.Null) v.Value = val.ToString(); } } } } Load("GeneralInputVars", GeneralInputVars); Load("GeneralOutputVars", GeneralOutputVars); Load("Station1Vars", Station1Vars); Load("Station1Outputs", Station1Outputs); Load("Station2Vars", Station2Vars); Load("Station2Outputs", Station2Outputs); Load("Station3Vars", Station3Vars); Load("Station3Outputs", Station3Outputs); Load("Station4Vars", Station4Vars); Load("Station4Outputs", Station4Outputs); } } catch { } }

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

            if (settings.ContainsKey("MeasurementOutputTag"))
            {
                var val = settings["MeasurementOutputTag"] as string;
                if (!string.IsNullOrEmpty(val)) _measurementOutputTag = val;
            }
            
            // Debug log
            System.Diagnostics.Debug.WriteLine($"[GlobalData] Otomasyon ayarları yüklendi: RFID={_autoRfidTag}, Index={_autoIndexTag}, Trigger={_autoTriggerTag}, MeasurementOutput={_measurementOutputTag}");
        }
        public static void SaveAutomationSettings() 
        { 
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings.Values; 
            settings["Auto_RfidTag"] = Auto_RfidTag ?? ""; 
            settings["Auto_IndexTag"] = Auto_IndexTag ?? ""; 
            settings["Auto_TriggerTag"] = Auto_TriggerTag ?? "";
            settings["MeasurementOutputTag"] = MeasurementOutputTag ?? "";
            StartAutomationListener(); 
        }

        // --- PLC DİNLEYİCİSİ ---
        private static PlcVariable _currentTriggerVar; // Referansı tutmak için

        private static void StartAutomationListener()
        {
            if (string.IsNullOrEmpty(Auto_TriggerTag)) return;

            // Önceki dinleyiciyi temizle
            if (_currentTriggerVar != null)
            {
                _currentTriggerVar.PropertyChanged -= TriggerVar_PropertyChanged;
                _currentTriggerVar = null;
            }
            
            // 1. Önce Global Input listesine bak
            var triggerVar = GeneralInputVars.FirstOrDefault(v => v.Name == Auto_TriggerTag);

            // 2. Bulamazsak PlcService Input/Output listelerine bak
            if (triggerVar == null && PlcService.Instance != null)
            {
                triggerVar = PlcService.Instance.InputVariables.FirstOrDefault(v => v.Name == Auto_TriggerTag);
                if (triggerVar == null)
                {
                    triggerVar = PlcService.Instance.OutputVariables.FirstOrDefault(v => v.Name == Auto_TriggerTag);
                }
            }

            if (triggerVar != null)
            {
                _currentTriggerVar = triggerVar;
                _currentTriggerVar.PropertyChanged += TriggerVar_PropertyChanged;
                OnAutomationLog?.Invoke($"Otomasyon devrede: {Auto_TriggerTag} izleniyor.");
            }
            else
            {
                OnAutomationLog?.Invoke($"⚠ UYARI: Trigger Tag '{Auto_TriggerTag}' sistemde bulunamadı.");
            }
        }

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

        // --- İŞLEM AKIŞI ---
        public static async Task RunAutomationSequence()
        {
            if (IsProcessRunning) return;
            IsProcessRunning = true;
            ProcessStatus = "İŞLENİYOR...";

            // ▼▼▼ SİNYAL SIFIRLA (Ölçüm başlıyor) ▼▼▼
            ResetMeasurementSignal();

            try
            {
                // RFID Variable Bul
                var rfidVar = GeneralInputVars.FirstOrDefault(v => v.Name == Auto_RfidTag);
                if (rfidVar == null && PlcService.Instance != null) 
                    rfidVar = PlcService.Instance.InputVariables.FirstOrDefault(v => v.Name == Auto_RfidTag);

                // Index Variable Bul
                var indexVar = GeneralInputVars.FirstOrDefault(v => v.Name == Auto_IndexTag);
                if (indexVar == null && PlcService.Instance != null)
                    indexVar = PlcService.Instance.InputVariables.FirstOrDefault(v => v.Name == Auto_IndexTag);

                string currentRfid = rfidVar?.Value ?? "---";
                string currentIndex = indexVar?.Value ?? "0";

                OnAutomationLog?.Invoke($"Tetik: {currentRfid} (Index: {currentIndex})");

                var recipe = KnownRfids.FirstOrDefault(r => r.Id == currentRfid);
                if (recipe == null) throw new Exception("Tanımsız RFID");

                int.TryParse(currentIndex, out int idx);
                if (idx < 0 || idx >= recipe.JobSequence.Count) throw new Exception("Geçersiz Index");

                string jobName = recipe.JobSequence[idx];
                ProcessStatus = $"JOB: {jobName}";

                bool loadOk = await App4.Utilities.GocatorJobLogic.LoadJob(jobName, (s) => OnAutomationLog?.Invoke(s));
                if (!loadOk) throw new Exception("Job yüklenemedi");

                ProcessStatus = "ÖLÇÜM...";
                var (status, measurements) = await App4.Utilities.ReceiveMeasurementLogic.ReceiveAndProcessMeasurements((s) => OnAutomationLog?.Invoke(s), null);

                if (status == 1 && measurements != null)
                {
                    // ▼▼▼ SİNYAL GÖNDER (Ölçüm tamamlandı) ▼▼▼
                    SetMeasurementSignal();

                    OnAutomationLog?.Invoke("PLC Yazma işlemi başlıyor...");
                    for (int i = 0; i < PlcTransferRows.Count; i++)
                    {
                        var row = PlcTransferRows[i];
                        if (i < measurements.Count)
                        {
                            var meas = measurements[i];
                            row.Value = meas.Value.ToString();
                            row.Status = "SENT";

                            if (!string.IsNullOrEmpty(row.SelectedTag))
                            {
                                var plcTag = GeneralOutputVars.FirstOrDefault(v => v.Name == row.SelectedTag) ?? PlcService.Instance.OutputVariables.FirstOrDefault(v => v.Name == row.SelectedTag);
                                if (plcTag != null)
                                {
                                    await PlcService.Instance.WriteAsync(plcTag, meas.Value);
                                    OnAutomationLog?.Invoke($"PLC WR: {plcTag.Name} = {meas.Value}");
                                }
                            }
                        }
                    }
                    ProcessStatus = "TAMAMLANDI";
                }
                else
                {
                    ProcessStatus = "VERİ YOK";
                    OnAutomationLog?.Invoke("⚠ Ölçüm alınamadı: Çıktı yok veya zaman aşımı.");
                }
            }
            catch (Exception ex)
            {
                ProcessStatus = "HATA";
                OnAutomationLog?.Invoke($"Hata: {ex.Message}");
            }
            finally
            {
                IsProcessRunning = false;
                await Task.Delay(2000);
                if (ProcessStatus == "TAMAMLANDI" || ProcessStatus == "VERİ YOK" || ProcessStatus == "HATA") ProcessStatus = "HAZIR";
            }
        }
    } // GlobalData Class Sonu

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
}