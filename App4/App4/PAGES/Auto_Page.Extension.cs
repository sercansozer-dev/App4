using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI; 
using Microsoft.UI.Xaml.Media;
using App4.PAGES;
using System.Text.Json;
using System.IO;

namespace App4
{
    public sealed partial class Auto_Page
    {
        public static ObservableCollection<RfidDef> GlobalKnownRfids { get; private set; } = new();
        public ObservableCollection<RfidDef> KnownRfids => GlobalKnownRfids;

        public ObservableCollection<PlcVariable> Station1Outputs { get; set; } = new();
        public ObservableCollection<PlcVariable> Station2Outputs { get; set; } = new();
        public ObservableCollection<PlcVariable> Station3Outputs { get; set; } = new();
        public ObservableCollection<PlcVariable> Station4Outputs { get; set; } = new();

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (GlobalKnownRfids.Count == 0)
            {
                GlobalKnownRfids.Add(new RfidDef { Id = "RF123", Description = "Klima A Tipi" });
                GlobalKnownRfids.Add(new RfidDef { Id = "RF456", Description = "Klima B Tipi" });
                GlobalKnownRfids.Add(new RfidDef { Id = "RF789", Description = "Klima C Tipi" });
            }
            
            LoadPlcTagsFromPage();
            LoadPlcVariableTagsFromFile();
            ReplaceStationsWithExtended();
            InitializeOutputVariables();
            SubscribeStationEvents();
        }

        private void LoadPlcTagsFromPage()
        {
            // PLC_Page'in global koleksiyonlarýný initialize et (eðer boþsa)
            if (PLC_Page.GlobalInputVariables.Count == 0 || PLC_Page.GlobalOutputVariables.Count == 0)
            {
                // PLC_Page constructor'ýný tetiklemek için yeni instance oluþtur (veya InitializePLCVariables özel method çaðýr)
                // Bu durumda, global koleksiyonlarý doðrudan doldur
                PLC_Page.GlobalInputVariables.Clear();
                PLC_Page.GlobalOutputVariables.Clear();

                // Default INPUT variables
                PLC_Page.GlobalInputVariables.Add(new App4.PAGES.PLCVariable { Name = "D0 - Okunan Deðer", Type = "WORD", Direction = "Input", CurrentValue = 0, MinValue = 0, MaxValue = 65535 });
                PLC_Page.GlobalInputVariables.Add(new App4.PAGES.PLCVariable { Name = "M0 - Acil Durdur", Type = "BOOL", Direction = "Input", CurrentValue = false, MinValue = false, MaxValue = true });
                PLC_Page.GlobalInputVariables.Add(new App4.PAGES.PLCVariable { Name = "M1 - Sistem Ready", Type = "BOOL", Direction = "Input", CurrentValue = true, MinValue = false, MaxValue = true });

                // Default OUTPUT variables
                PLC_Page.GlobalOutputVariables.Add(new App4.PAGES.PLCVariable { Name = "D0 - Yazýlan Deðer", Type = "WORD", Direction = "Output", CurrentValue = 0, MinValue = 0, MaxValue = 65535 });
                PLC_Page.GlobalOutputVariables.Add(new App4.PAGES.PLCVariable { Name = "D1 - Ýþletim Modu", Type = "DWORD", Direction = "Output", CurrentValue = 0, MinValue = 0, MaxValue = 3 });
                PLC_Page.GlobalOutputVariables.Add(new App4.PAGES.PLCVariable { Name = "D2 - Robot Hýzý", Type = "INT", Direction = "Output", CurrentValue = 75, MinValue = 0, MaxValue = 100 });
            }

            // Koleksiyonlarý ComboBox'lar için doldur
            AvailableInputPlcTags.Clear();
            AvailableOutputPlcTags.Clear();
            AvailablePlcTags.Clear();
            
            foreach (var plcVar in PLC_Page.GlobalInputVariables)
            {
                AvailableInputPlcTags.Add(plcVar.Name);
            }
            
            foreach (var plcVar in PLC_Page.GlobalOutputVariables)
            {
                AvailableOutputPlcTags.Add(plcVar.Name);
            }
            
            // Tüm tags için fallback
            foreach (var tag in AvailableInputPlcTags)
                AvailablePlcTags.Add(tag);
            foreach (var tag in AvailableOutputPlcTags)
                if (!AvailablePlcTags.Contains(tag))
                    AvailablePlcTags.Add(tag);
        }

        private void SubscribeStationEvents()
        {
            foreach (var s in Stations)
            {
                s.PropertyChanged -= Station_PropertyChanged;
                s.PropertyChanged += Station_PropertyChanged;
            }
        }

        private void Station_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is ExtendedStationViewModel station)
            {
                int index = Stations.IndexOf(station);
                if (index < 0) return;
                
                ObservableCollection<PlcVariable> outputs = null;
                switch (index)
                {
                    case 0: outputs = Station1Outputs; break;
                    case 1: outputs = Station2Outputs; break;
                    case 2: outputs = Station3Outputs; break;
                    case 3: outputs = Station4Outputs; break;
                }

                if (outputs != null)
                {
                    if (e.PropertyName == nameof(StationViewModel.CurrentRfid) || 
                        e.PropertyName == nameof(StationViewModel.AllowedRfid) ||
                        e.PropertyName == nameof(ExtendedStationViewModel.TargetRfid))
                    {
                        var isMatch = station.IsRfidMatch;
                        UpdatePlcVar(outputs, $"ST{index + 1}_ID_MATCHED", isMatch ? "TRUE" : "FALSE");
                        UpdatePlcVar(outputs, $"ST{index + 1}_CONVEYOR_PERM", isMatch ? "TRUE" : "FALSE");
                        
                        // Update target RFID output text
                        if (e.PropertyName == nameof(ExtendedStationViewModel.TargetRfid) || e.PropertyName == nameof(StationViewModel.AllowedRfid))
                        {
                             UpdatePlcVar(outputs, $"ST{index + 1}_RFID_TARGET", station.TargetRfid);
                        }
                    }
                    else if (e.PropertyName == nameof(ExtendedStationViewModel.RfidOpMode))
                    {
                        UpdatePlcVar(outputs, $"ST{index + 1}_RFID_MODE", station.RfidOpMode.ToString());
                    }
                    else if (e.PropertyName == nameof(StationViewModel.Mode))
                    {
                        UpdatePlcVar(outputs, $"ST{index + 1}_MODE_CMD", station.Mode == StationMode.Auto ? "AUTO" : "MANUAL");
                    }
                }
            }
        }

        private void UpdatePlcVar(ObservableCollection<PlcVariable> collection, string partialName, string newValue)
        {
            var v = System.Linq.Enumerable.FirstOrDefault(collection, x => x.Name == partialName);
            if (v != null && v.Value != newValue)
            {
                v.Value = newValue;
            }
        }

        private void InitializeOutputVariables()
        {
            if (Station1Outputs.Count > 0) return;
            AddStationOutputs(Station1Outputs, 1);
            AddStationOutputs(Station2Outputs, 2);
            AddStationOutputs(Station3Outputs, 3);
            AddStationOutputs(Station4Outputs, 4);
        }

        private void AddStationOutputs(ObservableCollection<PlcVariable> outputs, int stationId)
        {
            outputs.Add(CreateVarExt($"ST{stationId}_RFID_MODE", "Mixed", $"RFID Çalýþma Modu", true, $"DB10.DBX{(stationId-1)*20}.0")); 
            outputs.Add(CreateVarExt($"ST{stationId}_RFID_TARGET", "", $"Hedef RFID", true, $"DB10.STR{(stationId-1)*20}.4"));
            outputs.Add(CreateVarExt($"ST{stationId}_ID_MATCHED", "FALSE", $"ID Eþleþti (1=OK)", true, $"DB10.DBX{(stationId-1)*20}.20"));
            outputs.Add(CreateVarExt($"ST{stationId}_PROCESS_RESULT", "0", $"Ýþlem Sonucu", true, $"DB10.DBX{(stationId-1)*20}.22"));
            outputs.Add(CreateVarExt($"ST{stationId}_CONVEYOR_PERM", "FALSE", $"Konveyör Ýzni", true, $"DB10.DBX{(stationId-1)*20}.24"));
            outputs.Add(CreateVarExt($"ST{stationId}_MODE_CMD", "AUTO", $"Mod (AUTO/MANUAL)", true, $"DB10.DBX{(stationId-1)*20}.26"));
        }

        private PlcVariable CreateVarExt(string name, string value, string description, bool isEditable, string tag)
        {
            return new PlcVariable { Name = name, Value = value, Description = description, IsEditable = isEditable, PlcTag = tag };
        }

        private void ReplaceStationsWithExtended()
        {
            for(int i = 0; i < Stations.Count; i++)
            {
                if (Stations[i] is not ExtendedStationViewModel)
                {
                    var item = Stations[i];
                    var ext = new ExtendedStationViewModel();
                    // Copy base props
                    ext.Name = item.Name;
                    ext.Description = item.Description;
                    ext.StatusTag = item.StatusTag;
                    ext.AlarmTag = item.AlarmTag;
                    ext.ModeTag = item.ModeTag;
                    ext.ProducingTag = item.ProducingTag;
                    ext.ProductionCountTag = item.ProductionCountTag;
                    ext.EfficiencyTag = item.EfficiencyTag;
                    ext.CurrentRfidTag = item.CurrentRfidTag;

                    // Fix for missing tag in Station 1 initialization
                    if (string.IsNullOrEmpty(ext.CurrentRfidTag) && i == 0)
                    {
                        ext.CurrentRfidTag = "ST1_RFID_ACT";
                    }

                    ext.AllowedRfid = item.AllowedRfid;
                    ext.CurrentRfid = item.CurrentRfid;
                    ext.Mode = item.Mode;
                    ext.IsProducing = item.IsProducing;
                    ext.HasAlarm = item.HasAlarm;
                    ext.IsRobotPresent = item.IsRobotPresent;
                    ext.ProcessStatus = item.ProcessStatus;
                    ext.ProductionCount = item.ProductionCount;
                    ext.Efficiency = item.Efficiency;

                    // Init extended props
                    ext.RfidOpMode = RfidOperationMode.Mixed;
                    if (!string.IsNullOrEmpty(ext.AllowedRfid))
                    {
                        ext.TargetRfid = ext.AllowedRfid;
                        if(ext.AllowedRfid != "") ext.RfidOpMode = RfidOperationMode.Specific;
                    }
                    
                    // Tags
                    if(!string.IsNullOrEmpty(ext.StatusTag))
                    {
                         ext.PlcTagRfidMode = ext.StatusTag.Replace("_STATUS", "_RFID_MODE");
                         ext.PlcTagTargetRfid = ext.StatusTag.Replace("_STATUS", "_RFID_TARGET");
                    }

                    // Replace in-place to avoid clearing collection
                    Stations[i] = ext;
                }
            }
        }

        private void BtnAddRfid_Click(object sender, RoutedEventArgs e)
        {
            if (TxtNewRfidId != null && !string.IsNullOrWhiteSpace(TxtNewRfidId.Text))
            {
                GlobalKnownRfids.Add(new RfidDef 
                { 
                    Id = TxtNewRfidId.Text, 
                    Description = TxtNewRfidDesc != null ? TxtNewRfidDesc.Text : "" 
                });
                
                TxtNewRfidId.Text = "";
                if(TxtNewRfidDesc != null) TxtNewRfidDesc.Text = "";
            }
        }

        private void PlcTagComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.DataContext is PlcVariable plcVar)
            {
                plcVar.PlcTag = comboBox.SelectedItem as string;
                SavePlcVariableTagsToFile();
            }
        }

        private void LoadPlcVariableTagsFromFile()
        {
            try
            {
                if (File.Exists(_autoPageVariablesFilePath))
                {
                    var json = File.ReadAllText(_autoPageVariablesFilePath);
                    var data = JsonSerializer.Deserialize<JsonElement>(json);

                    if (data.TryGetProperty("GeneralVars", out var generalVarsArray))
                    {
                        foreach (var item in generalVarsArray.EnumerateArray())
                        {
                            if (item.TryGetProperty("name", out var nameElem) && item.TryGetProperty("plcTag", out var tagElem))
                            {
                                var name = nameElem.GetString();
                                var tag = tagElem.GetString();
                                var variable = GeneralVars.FirstOrDefault(v => v.Name == name);
                                if (variable != null)
                                    variable.PlcTag = tag;
                            }
                        }
                    }

                    LoadStationVariableTags(data, "Station1Vars", Station1Vars);
                    LoadStationVariableTags(data, "Station2Vars", Station2Vars);
                    LoadStationVariableTags(data, "Station3Vars", Station3Vars);
                    LoadStationVariableTags(data, "Station4Vars", Station4Vars);
                    LoadStationVariableTags(data, "Station1Outputs", Station1Outputs);
                    LoadStationVariableTags(data, "Station2Outputs", Station2Outputs);
                    LoadStationVariableTags(data, "Station3Outputs", Station3Outputs);
                    LoadStationVariableTags(data, "Station4Outputs", Station4Outputs);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Tag verileri yüklenirken hata: {ex.Message}");
            }
        }

        private void LoadStationVariableTags(JsonElement data, string propertyName, ObservableCollection<PlcVariable> targetCollection)
        {
            if (data.TryGetProperty(propertyName, out var varsArray))
            {
                foreach (var item in varsArray.EnumerateArray())
                {
                    if (item.TryGetProperty("name", out var nameElem) && item.TryGetProperty("plcTag", out var tagElem))
                    {
                        var name = nameElem.GetString();
                        var tag = tagElem.GetString();
                        var variable = targetCollection.FirstOrDefault(v => v.Name == name);
                        if (variable != null)
                            variable.PlcTag = tag;
                    }
                }
            }
        }

        private void SavePlcVariableTagsToFile()
        {
            try
            {
                var directory = Path.GetDirectoryName(_autoPageVariablesFilePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

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

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_autoPageVariablesFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Tag verileri kaydedilirken hata: {ex.Message}");
            }
        }
    }

    public class ExtendedStationViewModel : StationViewModel
    {
        public string PlcTagRfidMode { get; set; }
        public string PlcTagTargetRfid { get; set; }

        public ObservableCollection<RfidDef> RefRfids => Auto_Page.GlobalKnownRfids;
        
        public System.Collections.Generic.List<RfidOperationMode> RfidOpModes { get; } = new System.Collections.Generic.List<RfidOperationMode>
        {
             RfidOperationMode.Mixed,
             RfidOperationMode.Specific
        };

        private RfidOperationMode _rfidOpMode;
        public RfidOperationMode RfidOpMode
        {
             get => _rfidOpMode;
             set 
             { 
                 _rfidOpMode = value; 
                 OnPropertyChanged(nameof(RfidOpMode)); 
                 OnPropertyChanged(nameof(IsSpecificRfidVisible));
             }
        }

        private string _targetRfid;
        public string TargetRfid
        {
             get => _targetRfid;
             set 
             { 
                 _targetRfid = value; 
                 OnPropertyChanged(nameof(TargetRfid)); 
                 AllowedRfid = value;
             }
        }

        public Visibility IsSpecificRfidVisible => RfidOpMode == RfidOperationMode.Specific ? Visibility.Visible : Visibility.Collapsed;

        public ExtendedStationViewModel()
        {
            this.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Mode)) 
                {
                    OnPropertyChanged(nameof(IsAutoMode));
                    OnPropertyChanged(nameof(IsManualMode));
                    OnPropertyChanged(nameof(AutoBtnBg));
                    OnPropertyChanged(nameof(AutoBtnFg));
                    OnPropertyChanged(nameof(ManualBtnBg));
                    OnPropertyChanged(nameof(ManualBtnFg));
                }
            };
        }

        public bool IsAutoMode
        {
            get => Mode == StationMode.Auto;
            set 
            {
                 Mode = value ? StationMode.Auto : StationMode.Manual;
                 // Notifications handled by PropertyChanged event above
            }
        }

        // Button Visuals
        public SolidColorBrush AutoBtnBg => Mode == StationMode.Auto ? new SolidColorBrush(Color.FromArgb(255, 46, 204, 113)) : new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        public SolidColorBrush AutoBtnFg => Mode == StationMode.Auto ? new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
        
        public SolidColorBrush ManualBtnBg => Mode == StationMode.Manual ? new SolidColorBrush(Color.FromArgb(255, 255, 165, 0)) : new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        public SolidColorBrush ManualBtnFg => Mode == StationMode.Manual ? new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));

        public bool IsManualMode
        {
             get => Mode != StationMode.Auto; // Simple toggle logic
             set
             {
                 if(value) Mode = StationMode.Manual;
                 else Mode = StationMode.Auto;
                 OnPropertyChanged(nameof(IsAutoMode));
                 OnPropertyChanged(nameof(IsManualMode));
             }
        }
    }
}
