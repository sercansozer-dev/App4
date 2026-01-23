using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Windows.UI;
using App4.Utilities; // StationModels ve PlcVariable buradan geliyor
using Windows.System;

namespace App4
{
    public sealed partial class Auto_Page : Page
    {
        private readonly string _autoPageVariablesFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "App4", "Auto_Page_Variables.json");

        // RfidDef artık App4.Utilities altında
        // Artık veriyi GlobalData'dan çekiyor

        // --- GLOBAL RFID LİSTESİ ---
        // BU SATIRI SİL veya DEĞİŞTİR:
        // --- GLOBAL RFID LİSTESİ ---
        // XAML'ın erişebilmesi için GlobalData'ya bir köprü kuruyoruz.
        // "static" değil, normal bir property (Instance Member) olmalı.
        // Türü açıkça "App4.Utilities.RfidDef" olarak belirtiyoruz
        public ObservableCollection<App4.Utilities.RfidDef> KnownRfids => App4.Utilities.GlobalData.KnownRfids;

        // StationViewModel artık App4.Utilities altında
        public ObservableCollection<StationViewModel> Stations { get; set; } = new();

        public ObservableCollection<PlcVariable> GeneralVars { get; set; } = new();
        public ObservableCollection<PlcVariable> Station1Vars { get; set; } = new();
        public ObservableCollection<PlcVariable> Station2Vars { get; set; } = new();
        public ObservableCollection<PlcVariable> Station3Vars { get; set; } = new();
        public ObservableCollection<PlcVariable> Station4Vars { get; set; } = new();
        public ObservableCollection<PlcVariable> Station1Outputs { get; set; } = new();
        public ObservableCollection<PlcVariable> Station2Outputs { get; set; } = new();
        public ObservableCollection<PlcVariable> Station3Outputs { get; set; } = new();
        public ObservableCollection<PlcVariable> Station4Outputs { get; set; } = new();

        public ObservableCollection<string> AvailablePlcTags { get; set; } = new();
        public ObservableCollection<string> AvailableInputPlcTags { get; set; } = new();
        public ObservableCollection<string> AvailableOutputPlcTags { get; set; } = new();

        public Auto_Page()
        {
            this.InitializeComponent();

            InitializeStations();
            InitializeLocalVariables();
            InitializeAvailablePlcTags();

            this.Loaded += Page_Loaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            InitializeAvailablePlcTags();
            if (File.Exists(_autoPageVariablesFilePath)) LoadPlcVariableTagsFromFile();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ReplaceStationsWithExtended();
            InitializeOutputVariables();
            if (!File.Exists(_autoPageVariablesFilePath)) SavePlcVariableTagsToFile();
            else LoadPlcVariableTagsFromFile();
            SubscribeStationEvents();
        }

        private void InitializeStations()
        {
            Stations.Add(new StationViewModel { Name = "İSTASYON 1", Description = "Klima Dış Ünite", Mode = StationMode.Auto, StatusTag = "ST1_STATUS", AlarmTag = "ST1_ALARM", ProducingTag = "ST1_PRODUCING", ProductionCountTag = "ST1_PROD_COUNT", EfficiencyTag = "ST1_EFFICIENCY", CurrentRfidTag = "ST1_RFID_ACT", AllowedRfid = "RF123", CurrentRfid = "RF123" });
            Stations.Add(new StationViewModel { Name = "İSTASYON 2", Description = "Klima Dış Ünite", Mode = StationMode.Auto, StatusTag = "ST2_STATUS", AlarmTag = "ST2_ALARM", ProducingTag = "ST2_PRODUCING", ProductionCountTag = "ST2_PROD_COUNT", EfficiencyTag = "ST2_EFFICIENCY", CurrentRfidTag = "ST2_RFID_ACT", AllowedRfid = "RF123", CurrentRfid = "RF123" });
            Stations.Add(new StationViewModel { Name = "İSTASYON 3", Description = "Boş İstasyon", Mode = StationMode.Manual, StatusTag = "ST3_STATUS", AlarmTag = "ST3_ALARM", ProducingTag = "ST3_PRODUCING", ProductionCountTag = "ST3_PROD_COUNT", EfficiencyTag = "ST3_EFFICIENCY", CurrentRfidTag = "ST3_RFID_ACT", AllowedRfid = "RF123" });
            Stations.Add(new StationViewModel { Name = "İSTASYON 4", Description = "Klima Hatalı", Mode = StationMode.Bypass, StatusTag = "ST4_STATUS", AlarmTag = "ST4_ALARM", ProducingTag = "ST4_PRODUCING", ProductionCountTag = "ST4_PROD_COUNT", EfficiencyTag = "ST4_EFFICIENCY", CurrentRfidTag = "ST4_RFID_ACT", AllowedRfid = "RF123", CurrentRfid = "ERR01" });
        }

        private void InitializeLocalVariables()
        {
            PlcVariable CreateLocalVar(string name, string type, string direction, object defaultValue)
            {
                var variable = new PlcVariable { Name = name, Type = type, Direction = direction, CurrentValue = defaultValue, IsEditable = true, PlcTag = "" };
                variable.PropertyChanged += LocalVariable_PropertyChanged;
                return variable;
            }
            GeneralVars.Add(CreateLocalVar("SLIDER_POS_ACT", "WORD", "Input", "0"));
            GeneralVars.Add(CreateLocalVar("ROBOT_SPEED", "WORD", "Input", "100"));
            GeneralVars.Add(CreateLocalVar("GOCATOR_STATUS", "STRING", "Input", "READY"));
            GeneralVars.Add(CreateLocalVar("SAFETY_OK", "BOOL", "Input", true));
            void AddStationVars(ObservableCollection<PlcVariable> collection, int stationId)
            {
                collection.Add(CreateLocalVar($"ST{stationId}_STATUS", "STRING", "Input", "Unknown"));
                collection.Add(CreateLocalVar($"ST{stationId}_ALARM", "BOOL", "Input", false));
                collection.Add(CreateLocalVar($"ST{stationId}_MODE", "STRING", "Input", "Manual"));
                collection.Add(CreateLocalVar($"ST{stationId}_PRODUCING", "BOOL", "Input", false));
                collection.Add(CreateLocalVar($"ST{stationId}_PROD_COUNT", "WORD", "Input", "0"));
                collection.Add(CreateLocalVar($"ST{stationId}_EFFICIENCY", "WORD", "Input", "0"));
                collection.Add(CreateLocalVar($"ST{stationId}_RFID_ACT", "STRING", "Input", ""));
            }
            AddStationVars(Station1Vars, 1); AddStationVars(Station2Vars, 2);
            AddStationVars(Station3Vars, 3); AddStationVars(Station4Vars, 4);
        }

        private void InitializeAvailablePlcTags()
        {
            try
            {
                AvailableInputPlcTags.Clear(); AvailableOutputPlcTags.Clear(); AvailablePlcTags.Clear();
                foreach (var v in PlcService.Instance.InputVariables) { AvailableInputPlcTags.Add(v.Name); AvailablePlcTags.Add(v.Name); }
                foreach (var v in PlcService.Instance.OutputVariables) { AvailableOutputPlcTags.Add(v.Name); if (!AvailablePlcTags.Contains(v.Name)) AvailablePlcTags.Add(v.Name); }
            }
            catch { }
        }

        private void InitializeOutputVariables()
        {
            if (Station1Outputs.Count > 0) return;
            Station1Outputs.Clear(); Station2Outputs.Clear(); Station3Outputs.Clear(); Station4Outputs.Clear();
            AddStationOutputs(Station1Outputs, 1); AddStationOutputs(Station2Outputs, 2);
            AddStationOutputs(Station3Outputs, 3); AddStationOutputs(Station4Outputs, 4);
        }

        private void AddStationOutputs(ObservableCollection<PlcVariable> outputs, int stationId)
        {
            outputs.Add(CreateVarExt($"ST{stationId}_RFID_MODE", "Mixed", "RFID Mod", true, $"DB10.DBX{(stationId - 1) * 20}.0"));
            outputs.Add(CreateVarExt($"ST{stationId}_RFID_TARGET", "", "Hedef RFID", true, $"DB10.STR{(stationId - 1) * 20}.4"));
            outputs.Add(CreateVarExt($"ST{stationId}_ID_MATCHED", "FALSE", "ID Eşleşti", true, $"DB10.DBX{(stationId - 1) * 20}.20"));
            outputs.Add(CreateVarExt($"ST{stationId}_PROCESS_RESULT", "0", "Sonuç", true, $"DB10.DBX{(stationId - 1) * 20}.22"));
            outputs.Add(CreateVarExt($"ST{stationId}_CONVEYOR_PERM", "FALSE", "Konveyör", true, $"DB10.DBX{(stationId - 1) * 20}.24"));
            outputs.Add(CreateVarExt($"ST{stationId}_MODE_CMD", "AUTO", "Mod Cmd", true, $"DB10.DBX{(stationId - 1) * 20}.26"));
        }

        private PlcVariable CreateVarExt(string name, string value, string description, bool isEditable, string tag)
        {
            return new PlcVariable { Name = name, Value = value, Description = description, IsEditable = isEditable, PlcTag = tag };
        }

        private void ConnectToPlcVariable(PlcVariable localVar)
        {
            if (string.IsNullOrEmpty(localVar.PlcTag)) return;

            // 1. PLC Havuzundaki (merkezi) değişkeni bul
            var sourceRealVar = PlcService.Instance.InputVariables.FirstOrDefault(v => v.Name == localVar.PlcTag)
                             ?? PlcService.Instance.OutputVariables.FirstOrDefault(v => v.Name == localVar.PlcTag);

            if (sourceRealVar != null)
            {
                // --- OKUMA TARAFI (PLC -> UI) ---
                this.DispatcherQueue.TryEnqueue(() => {
                    localVar.Value = sourceRealVar.CurrentValue?.ToString();
                });

                sourceRealVar.PropertyChanged += (s, e) => {
                    if (e.PropertyName == "CurrentValue")
                    {
                        this.DispatcherQueue.TryEnqueue(() => {
                            if (localVar.PlcTag == sourceRealVar.Name)
                                localVar.Value = sourceRealVar.CurrentValue?.ToString();
                        });
                    }
                };

                // --- YAZMA TARAFI (UI -> PLC) --- [cite: 6, 7]
                            // Eğer değişken bir Output ise, arayüzdeki değişimi dinle ve PLC'ye yaz
                localVar.PropertyChanged += async (s, e) => {
                    if (e.PropertyName == "CurrentValue" || e.PropertyName == "Value")
                    {
                        // Kullanıcının girdiği yeni değeri merkezi havuza aktar
                        if (sourceRealVar.CurrentValue?.ToString() != localVar.CurrentValue?.ToString())
                        {
                            sourceRealVar.CurrentValue = localVar.CurrentValue;

                            // Gerçek PLC yazma işlemini tetikle
                            await PlcService.Instance.WriteAsync(sourceRealVar, localVar.CurrentValue);
                        }
                    }
                };
            }
        }

        // Event handler'ı temizlemek için boş bir referans metodu (Lambda kullandığımız için opsiyonel ama iyi pratik)
        private void LocalVar_SourceChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) { }

        private void LoadPlcVariableTagsFromFile()
        {
            try
            {
                if (File.Exists(_autoPageVariablesFilePath))
                {
                    var json = File.ReadAllText(_autoPageVariablesFilePath);
                    var data = JsonSerializer.Deserialize<JsonElement>(json);
                    void LoadTags(string propName, ObservableCollection<PlcVariable> target)
                    {
                        if (data.TryGetProperty(propName, out var arr))
                            foreach (var item in arr.EnumerateArray())
                                if (item.TryGetProperty("name", out var n) && item.TryGetProperty("plcTag", out var t))
                                { var v = target.FirstOrDefault(x => x.Name == n.GetString()); if (v != null) { v.PlcTag = t.GetString(); ConnectToPlcVariable(v); } }
                    }
                    LoadTags("GeneralVars", GeneralVars); LoadTags("Station1Vars", Station1Vars); LoadTags("Station2Vars", Station2Vars);
                    LoadTags("Station3Vars", Station3Vars); LoadTags("Station4Vars", Station4Vars); LoadTags("Station1Outputs", Station1Outputs);
                    LoadTags("Station2Outputs", Station2Outputs); LoadTags("Station3Outputs", Station3Outputs); LoadTags("Station4Outputs", Station4Outputs);
                }
            }
            catch { }
        }

        private void SavePlcVariableTagsToFile()
        {
            try
            {
                var dir = Path.GetDirectoryName(_autoPageVariablesFilePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var data = new
                {
                    GeneralVars = GeneralVars.Select(v => new { name = v.Name, plcTag = v.PlcTag }).ToList(),
                    Station1Vars = Station1Vars.Select(v => new { name = v.Name, plcTag = v.PlcTag }).ToList(),
                    Station2Vars = Station2Vars.Select(v => new { name = v.Name, plcTag = v.PlcTag }).ToList(),
                    Station3Vars = Station3Vars.Select(v => new { name = v.Name, plcTag = v.PlcTag }).ToList(),
                    Station4Vars = Station4Vars.Select(v => new { name = v.Name, plcTag = v.PlcTag }).ToList(),
                    Station1Outputs = Station1Outputs.Select(v => new { name = v.Name, plcTag = v.PlcTag }).ToList(),
                    Station2Outputs = Station2Outputs.Select(v => new { name = v.Name, plcTag = v.PlcTag }).ToList(),
                    Station3Outputs = Station3Outputs.Select(v => new { name = v.Name, plcTag = v.PlcTag }).ToList(),
                    Station4Outputs = Station4Outputs.Select(v => new { name = v.Name, plcTag = v.PlcTag }).ToList()
                };
                File.WriteAllText(_autoPageVariablesFilePath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        private void PlcTagComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.DataContext is PlcVariable plcVar)
            {
                string selected = comboBox.SelectedItem as string;
                if (plcVar.PlcTag != selected) { plcVar.PlcTag = selected; SavePlcVariableTagsToFile(); ConnectToPlcVariable(plcVar); }
            }
        }

        private void LocalVariable_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is PlcVariable localVar && e.PropertyName == nameof(PlcVariable.CurrentValue))
            {
                string strValue = localVar.CurrentValue?.ToString() ?? "";
                if (localVar.Name == "SLIDER_POS_ACT") UpdateSliderPosition(strValue);
                else UpdateStationStatus(localVar.Name, strValue);
            }
        }

        private void SubscribeStationEvents() { foreach (var s in Stations) { s.PropertyChanged -= Station_PropertyChanged; s.PropertyChanged += Station_PropertyChanged; } }

        private void Station_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is ExtendedStationViewModel station)
            {
                int index = Stations.IndexOf(station); if (index < 0) return;
                ObservableCollection<PlcVariable> outputs = null;
                switch (index) { case 0: outputs = Station1Outputs; break; case 1: outputs = Station2Outputs; break; case 2: outputs = Station3Outputs; break; case 3: outputs = Station4Outputs; break; }
                if (outputs != null)
                {
                    if (e.PropertyName == nameof(StationViewModel.CurrentRfid) || e.PropertyName == nameof(StationViewModel.AllowedRfid) || e.PropertyName == nameof(ExtendedStationViewModel.TargetRfid))
                    {
                        UpdatePlcVar(outputs, $"ST{index + 1}_ID_MATCHED", station.IsRfidMatch ? "TRUE" : "FALSE");
                        UpdatePlcVar(outputs, $"ST{index + 1}_CONVEYOR_PERM", station.IsRfidMatch ? "TRUE" : "FALSE");
                        if (e.PropertyName == nameof(ExtendedStationViewModel.TargetRfid) || e.PropertyName == nameof(StationViewModel.AllowedRfid)) UpdatePlcVar(outputs, $"ST{index + 1}_RFID_TARGET", station.TargetRfid);
                    }
                    else if (e.PropertyName == nameof(ExtendedStationViewModel.RfidOpMode)) UpdatePlcVar(outputs, $"ST{index + 1}_RFID_MODE", station.RfidOpMode.ToString());
                    else if (e.PropertyName == nameof(StationViewModel.Mode)) UpdatePlcVar(outputs, $"ST{index + 1}_MODE_CMD", station.Mode == StationMode.Auto ? "AUTO" : "MANUAL");
                }
            }
        }

        private void ReplaceStationsWithExtended()
        {
            for (int i = 0; i < Stations.Count; i++)
            {
                if (Stations[i] is not ExtendedStationViewModel)
                {
                    var item = Stations[i];
                    var ext = new ExtendedStationViewModel
                    {
                        Name = item.Name,
                        Description = item.Description,
                        StatusTag = item.StatusTag,
                        AlarmTag = item.AlarmTag,
                        ModeTag = item.ModeTag,
                        ProducingTag = item.ProducingTag,
                        ProductionCountTag = item.ProductionCountTag,
                        EfficiencyTag = item.EfficiencyTag,
                        CurrentRfidTag = item.CurrentRfidTag,
                        AllowedRfid = item.AllowedRfid,
                        CurrentRfid = item.CurrentRfid,
                        Mode = item.Mode,
                        IsProducing = item.IsProducing,
                        HasAlarm = item.HasAlarm,
                        IsRobotPresent = item.IsRobotPresent,
                        ProcessStatus = item.ProcessStatus,
                        ProductionCount = item.ProductionCount,
                        Efficiency = item.Efficiency
                    };
                    if (string.IsNullOrEmpty(ext.CurrentRfidTag) && i == 0) ext.CurrentRfidTag = "ST1_RFID_ACT";
                    ext.RfidOpMode = App4.Utilities.RfidOperationMode.Mixed;
                    if (!string.IsNullOrEmpty(ext.AllowedRfid)) { ext.TargetRfid = ext.AllowedRfid; if (ext.AllowedRfid != "") ext.RfidOpMode = App4.Utilities.RfidOperationMode.Specific; }
                    Stations[i] = ext;
                }
            }
        }

        private void UpdatePlcVar(ObservableCollection<PlcVariable> collection, string partialName, string newValue) { var v = collection.FirstOrDefault(x => x.Name == partialName); if (v != null && v.Value != newValue) v.Value = newValue; }
        private void BtnAddRfid_Click(object sender, RoutedEventArgs e) { }
        private void UpdateSliderPosition(string value) { foreach (var station in Stations) station.IsRobotPresent = false; if (int.TryParse(value, out int pos) && pos >= 1 && pos <= 4) Stations[pos - 1].IsRobotPresent = true; }
        private void UpdateStationStatus(string varName, string value) { foreach (var station in Stations) { if (station.StatusTag == varName) station.ProcessStatus = MapStatusCode(value); else if (station.AlarmTag == varName) station.HasAlarm = ParseBool(value); else if (station.ProducingTag == varName) station.IsProducing = ParseBool(value); else if (station.ProductionCountTag == varName) station.ProductionCount = value; else if (station.EfficiencyTag == varName) station.Efficiency = value.Contains("%") ? value : "%" + value; else if (station.CurrentRfidTag == varName) station.CurrentRfid = value; } }
        private bool ParseBool(string value) { if (string.IsNullOrEmpty(value)) return false; value = value.ToUpper(); return value == "TRUE" || value == "1" || value == "ON"; }
        private string MapStatusCode(string value) { return value switch { "1" => "3D TARAMA", "2" => "GAZ KAÇAK TESTİ", "3" => "TEST TAMAMLANDI", "4" => "OK ÜRÜN", "5" => "NOK ÜRÜN", "6" => "HAZIRLANIYOR", _ => value }; }
    }
}