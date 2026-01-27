using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Specialized;

namespace App4.Utilities
{
    public static class GlobalData
    {
        // --- DOSYA YOLLARI ---
        private static readonly string _rfidFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "App4", "Saved_RFID_List.json");
        private static readonly string _stationStateFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "App4", "Station_States.json");
        private static readonly string _autoPageVariablesFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "App4", "Auto_Page_Variables.json");

        // --- GLOBAL LİSTELER ---
        public static ObservableCollection<RfidDef> KnownRfids { get; private set; } = new();
        public static ObservableCollection<StationViewModel> Stations { get; private set; } = new();

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

        // --- UYGULAMA AÇILIRKEN ÇAĞRILACAK ANA METOT ---
        public static void Initialize()
        {
            if (_isInitialized) return;

            LoadRfids();            // 1. RFID Listesini Yükle
            InitializeStations();   // 2. İstasyonları Oluştur
            LoadStationStates();    // 3. İstasyon Ayarlarını (Auto/Manual) Yükle
            InitializeVariables();  // 4. Değişkenleri Oluştur
            LoadPlcVariableTagsFromFile(); // 5. Değişken Değerlerini Yükle

            _isInitialized = true;
        }

        // 1. RFID YÖNETİMİ
        private static void LoadRfids()
        {
            try
            {
                if (File.Exists(_rfidFilePath))
                {
                    var list = JsonSerializer.Deserialize<List<RfidDef>>(File.ReadAllText(_rfidFilePath));
                    if (list != null) foreach (var item in list) KnownRfids.Add(item);
                }
                else
                {
                    KnownRfids.Add(new RfidDef { Id = "RF123", Description = "Klima A Tipi" });
                    SaveRfids();
                }
                KnownRfids.CollectionChanged += (s, e) => SaveRfids();
            }
            catch { }
        }
        public static void SaveRfids()
        {
            try { File.WriteAllText(_rfidFilePath, JsonSerializer.Serialize(KnownRfids, new JsonSerializerOptions { WriteIndented = true })); } catch { }
        }

        // 2. İSTASYON YÖNETİMİ
        private static void InitializeStations()
        {
            // ExtendedStationViewModel olarak doğrudan oluşturuyoruz (Replace işlemine gerek kalmıyor)
            Stations.Add(new ExtendedStationViewModel { Name = "İSTASYON 1", Description = "Klima Dış Ünite", Mode = StationMode.Auto, StatusTag = "ST1_STATUS", AlarmTag = "ST1_ALARM", ProducingTag = "ST1_PRODUCING", ProductionCountTag = "ST1_PROD_COUNT", EfficiencyTag = "ST1_EFFICIENCY", CurrentRfidTag = "ST1_RFID_ACT", AllowedRfid = "RF123", CurrentRfid = "RF123", RfidOpMode = RfidOperationMode.Mixed });
            Stations.Add(new ExtendedStationViewModel { Name = "İSTASYON 2", Description = "Klima Dış Ünite", Mode = StationMode.Auto, StatusTag = "ST2_STATUS", AlarmTag = "ST2_ALARM", ProducingTag = "ST2_PRODUCING", ProductionCountTag = "ST2_PROD_COUNT", EfficiencyTag = "ST2_EFFICIENCY", CurrentRfidTag = "ST2_RFID_ACT", AllowedRfid = "RF123", CurrentRfid = "RF123", RfidOpMode = RfidOperationMode.Mixed });
            Stations.Add(new ExtendedStationViewModel { Name = "İSTASYON 3", Description = "Boş İstasyon", Mode = StationMode.Manual, StatusTag = "ST3_STATUS", AlarmTag = "ST3_ALARM", ProducingTag = "ST3_PRODUCING", ProductionCountTag = "ST3_PROD_COUNT", EfficiencyTag = "ST3_EFFICIENCY", CurrentRfidTag = "ST3_RFID_ACT", AllowedRfid = "RF123", RfidOpMode = RfidOperationMode.Mixed });
            Stations.Add(new ExtendedStationViewModel { Name = "İSTASYON 4", Description = "Klima Hatalı", Mode = StationMode.Bypass, StatusTag = "ST4_STATUS", AlarmTag = "ST4_ALARM", ProducingTag = "ST4_PRODUCING", ProductionCountTag = "ST4_PROD_COUNT", EfficiencyTag = "ST4_EFFICIENCY", CurrentRfidTag = "ST4_RFID_ACT", AllowedRfid = "RF123", CurrentRfid = "ERR01", RfidOpMode = RfidOperationMode.Mixed });
        }

        public static void SaveStationStates()
        {
            try
            {
                var states = Stations.Select(s => {
                    var ext = s as ExtendedStationViewModel;
                    return new { Name = s.Name, Mode = (int)s.Mode, RfidOpMode = ext != null ? (int)ext.RfidOpMode : 0, TargetRfid = ext != null ? ext.TargetRfid : "" };
                }).ToList();
                File.WriteAllText(_stationStateFilePath, JsonSerializer.Serialize(states, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        private static void LoadStationStates()
        {
            try
            {
                if (File.Exists(_stationStateFilePath))
                {
                    var savedStates = JsonSerializer.Deserialize<List<JsonElement>>(File.ReadAllText(_stationStateFilePath));
                    foreach (var item in savedStates)
                    {
                        if (item.TryGetProperty("Name", out var nameProp))
                        {
                            var station = Stations.FirstOrDefault(s => s.Name == nameProp.GetString());
                            if (station != null)
                            {
                                if (item.TryGetProperty("Mode", out var modeProp)) station.Mode = (StationMode)modeProp.GetInt32();
                                if (station is ExtendedStationViewModel ext)
                                {
                                    if (item.TryGetProperty("RfidOpMode", out var rfid)) ext.RfidOpMode = (RfidOperationMode)rfid.GetInt32();
                                    if (item.TryGetProperty("TargetRfid", out var target)) ext.TargetRfid = target.GetString();
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        // 3. DEĞİŞKEN YÖNETİMİ
        private static void InitializeVariables()
        {
            PlcVariable Create(string name, string type, string dir, object val) => new PlcVariable { Name = name, Type = type, Direction = dir, CurrentValue = val, IsEditable = true };

            // Helper: İstasyon Değişkenleri
            void AddVars(ObservableCollection<PlcVariable> c, int id)
            {
                c.Add(Create($"ST{id}_STATUS", "STRING", "Input", "Unknown")); c.Add(Create($"ST{id}_ALARM", "BOOL", "Input", false));
                c.Add(Create($"ST{id}_MODE", "STRING", "Input", "Manual")); c.Add(Create($"ST{id}_PRODUCING", "BOOL", "Input", false));
                c.Add(Create($"ST{id}_PROD_COUNT", "WORD", "Input", "0")); c.Add(Create($"ST{id}_EFFICIENCY", "WORD", "Input", "0"));
                c.Add(Create($"ST{id}_RFID_ACT", "STRING", "Input", ""));
            }
            // Helper: İstasyon Outputları
            void AddOutputs(ObservableCollection<PlcVariable> c, int id)
            {
                c.Add(new PlcVariable { Name = $"ST{id}_RFID_MODE", Value = "0", Description = "RFID Mod", PlcTag = $"DB10.W{(id - 1) * 20}.0" });
                c.Add(new PlcVariable { Name = $"ST{id}_RFID_TARGET", Value = "0", Description = "Hedef RFID", PlcTag = $"DB10.W{(id - 1) * 20}.4" });
                c.Add(new PlcVariable { Name = $"ST{id}_ID_MATCHED", Value = "0", Description = "ID Eşleşti", PlcTag = $"DB10.W{(id - 1) * 20}.20" });
                c.Add(new PlcVariable { Name = $"ST{id}_PROCESS_RESULT", Value = "0", Description = "Sonuç", PlcTag = $"DB10.W{(id - 1) * 20}.22" });
                c.Add(new PlcVariable { Name = $"ST{id}_CONVEYOR_PERM", Value = "0", Description = "Konveyör", PlcTag = $"DB10.W{(id - 1) * 20}.24" });
                c.Add(new PlcVariable { Name = $"ST{id}_MODE_CMD", Value = "1", Description = "Mod Cmd", PlcTag = $"DB10.W{(id - 1) * 20}.26" });
            }

            // Genel Değişkenler
            GeneralInputVars.Add(Create("SLIDER_POS_ACT", "WORD", "Input", "0"));
            GeneralInputVars.Add(Create("ROBOT_SPEED", "WORD", "Input", "100"));
            GeneralInputVars.Add(Create("GOCATOR_STATUS", "STRING", "Input", "READY"));
            GeneralInputVars.Add(Create("SAFETY_OK", "BOOL", "Input", true));
            GeneralInputVars.Add(Create("LINE_RUNNING", "BOOL", "Input", false));
            GeneralInputVars.Add(Create("LINE_AUTO_MODE", "BOOL", "Input", false));
            GeneralInputVars.Add(Create("SYS_RESET_FEEDBACK", "BOOL", "Input", false));

            GeneralOutputVars.Add(Create("CMD_LINE_START", "BOOL", "Output", false));
            GeneralOutputVars.Add(Create("CMD_LINE_STOP", "BOOL", "Output", false));
            GeneralOutputVars.Add(Create("CMD_LINE_RESET", "BOOL", "Output", false));

            AddVars(Station1Vars, 1); AddVars(Station2Vars, 2); AddVars(Station3Vars, 3); AddVars(Station4Vars, 4);
            AddOutputs(Station1Outputs, 1); AddOutputs(Station2Outputs, 2); AddOutputs(Station3Outputs, 3); AddOutputs(Station4Outputs, 4);
        }

        public static void SavePlcVariableTagsToFile()
        {
            try
            {
                object Map(ObservableCollection<PlcVariable> l) => l.Select(v => new { name = v.Name, plcTag = v.PlcTag, value = v.Value }).ToList();
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
                    Station4Vars = Map(Station4Vars),
                    Station4Outputs = Map(Station4Outputs)
                };
                File.WriteAllText(_autoPageVariablesFilePath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        private static void LoadPlcVariableTagsFromFile()
        {
            try
            {
                if (File.Exists(_autoPageVariablesFilePath))
                {
                    var json = File.ReadAllText(_autoPageVariablesFilePath);
                    var data = JsonSerializer.Deserialize<JsonElement>(json);
                    void Load(string p, ObservableCollection<PlcVariable> t)
                    {
                        if (data.TryGetProperty(p, out var a)) foreach (var i in a.EnumerateArray())
                            {
                                if (i.TryGetProperty("name", out var n))
                                {
                                    var v = t.FirstOrDefault(x => x.Name == n.GetString());
                                    if (v != null)
                                    {
                                        if (i.TryGetProperty("plcTag", out var pt)) v.PlcTag = pt.GetString();
                                        if (i.TryGetProperty("value", out var val) && val.ValueKind != JsonValueKind.Null) v.Value = val.ToString();
                                    }
                                }
                            }
                    }
                    Load("GeneralInputVars", GeneralInputVars); Load("GeneralOutputVars", GeneralOutputVars);
                    Load("Station1Vars", Station1Vars); Load("Station1Outputs", Station1Outputs);
                    Load("Station2Vars", Station2Vars); Load("Station2Outputs", Station2Outputs);
                    Load("Station3Vars", Station3Vars); Load("Station3Outputs", Station3Outputs);
                    Load("Station4Vars", Station4Vars); Load("Station4Outputs", Station4Outputs);
                }
            }
            catch { }
        }
    }
}