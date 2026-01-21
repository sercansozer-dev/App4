using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using App4.PAGES;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App4
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using Windows.UI; 

    public sealed partial class Auto_Page : Page
    {
        private readonly string _autoPageVariablesFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "App4", "Auto_Page_Variables.json");

        public ObservableCollection<StationViewModel> Stations { get; set; } = new();
        public ObservableCollection<PlcVariable> GeneralVars { get; set; } = new();
        public ObservableCollection<PlcVariable> Station1Vars { get; set; } = new();
        public ObservableCollection<PlcVariable> Station2Vars { get; set; } = new();
        public ObservableCollection<PlcVariable> Station3Vars { get; set; } = new();
        public ObservableCollection<PlcVariable> Station4Vars { get; set; } = new();

        public ObservableCollection<string> AvailablePlcTags { get; set; } = new();
        public ObservableCollection<string> AvailableInputPlcTags { get; set; } = new();
        public ObservableCollection<string> AvailableOutputPlcTags { get; set; } = new();

        static Auto_Page()
        {
            // PLC_Page'in global koleksiyonlarýný initialize et (uygulama baþladýðýnda)
            InitializeGlobalPlcVariables();
        }

        private static void InitializeGlobalPlcVariables()
        {
            // Eðer zaten dolu ise, tekrar yapma
            if (PAGES.PLC_Page.GlobalInputVariables.Count > 0 || PAGES.PLC_Page.GlobalOutputVariables.Count > 0)
                return;

            // Default INPUT variables
            PAGES.PLC_Page.GlobalInputVariables.Add(new PAGES.PLCVariable { Name = "D0 - Okunan Deðer", Type = "WORD", Direction = "Input", CurrentValue = 0, MinValue = 0, MaxValue = 65535 });
            PAGES.PLC_Page.GlobalInputVariables.Add(new PAGES.PLCVariable { Name = "M0 - Acil Durdur", Type = "BOOL", Direction = "Input", CurrentValue = false, MinValue = false, MaxValue = true });
            PAGES.PLC_Page.GlobalInputVariables.Add(new PAGES.PLCVariable { Name = "M1 - Sistem Ready", Type = "BOOL", Direction = "Input", CurrentValue = true, MinValue = false, MaxValue = true });

            // Default OUTPUT variables
            PAGES.PLC_Page.GlobalOutputVariables.Add(new PAGES.PLCVariable { Name = "D0 - Yazýlan Deðer", Type = "WORD", Direction = "Output", CurrentValue = 0, MinValue = 0, MaxValue = 65535 });
            PAGES.PLC_Page.GlobalOutputVariables.Add(new PAGES.PLCVariable { Name = "D1 - Ýþletim Modu", Type = "DWORD", Direction = "Output", CurrentValue = 0, MinValue = 0, MaxValue = 3 });
            PAGES.PLC_Page.GlobalOutputVariables.Add(new PAGES.PLCVariable { Name = "D2 - Robot Hýzý", Type = "INT", Direction = "Output", CurrentValue = 75, MinValue = 0, MaxValue = 100 });
        }

        public Auto_Page()
        {
            this.InitializeComponent();
            InitializeStations();
            InitializePlcVariables();
        }

        private void InitializePlcVariables()
        {
            var sliderVar = CreateVar("SLIDER_POS_ACT", "0", "Robot Slider Aktif Ýstasyon No (1-4)", true, "DB300.DBD10");
            GeneralVars.Add(sliderVar);

            GeneralVars.Add(CreateVar("ROBOT_SPEED", "100", "Robot Hýzý %", true, "DB300.DBD14"));
            GeneralVars.Add(CreateVar("GOCATOR_STATUS", "READY", "Kamera Durumu", false, "DB300.DBD20"));
            GeneralVars.Add(CreateVar("SAFETY_OK", "TRUE", "Güvenlik Devresi", false, "I10.0"));

            // Helper to add station vars
            void AddStationVars(ObservableCollection<PlcVariable> targetCollection, int stationId, string status, string alarm, string mode, string producing, string prodCount, string efficiency, string rfid)
            {
                 targetCollection.Add(CreateVar($"ST{stationId}_STATUS", status, $"Ýstasyon {stationId} Durum", true, $"DB10.DBD{(stationId-1)*20}"));
                 targetCollection.Add(CreateVar($"ST{stationId}_ALARM", alarm, $"Ýstasyon {stationId} Alarm", true, $"DB10.DBX{((stationId-1)*20)+4}.0"));
                 targetCollection.Add(CreateVar($"ST{stationId}_MODE", mode, $"Ýstasyon {stationId} Mod", true, $"DB10.DBG{((stationId-1)*20)+6}"));
                 targetCollection.Add(CreateVar($"ST{stationId}_PRODUCING", producing, $"Ýstasyon {stationId} Üretim", true, $"DB10.DBX{((stationId-1)*20)+8}.0"));
                 targetCollection.Add(CreateVar($"ST{stationId}_PROD_COUNT", prodCount, $"Ýstasyon {stationId} Üretim Adedi", true, $"DB10.DBD{((stationId-1)*20)+12}"));
                 targetCollection.Add(CreateVar($"ST{stationId}_EFFICIENCY", efficiency, $"Ýstasyon {stationId} Verimlilik", true, $"DB10.DBD{((stationId-1)*20)+16}"));
                 targetCollection.Add(CreateVar($"ST{stationId}_RFID_ACT", rfid, $"Ýstasyon {stationId} Okunan RFID", true, $"DB10.STR{((stationId-1)*20)+20}"));
            }

            AddStationVars(Station1Vars, 1, "3D TARAMA", "FALSE", "AUTO", "TRUE", "1,245", "92", "RF123");
            AddStationVars(Station2Vars, 2, "GAZ TESTÝ", "FALSE", "AUTO", "TRUE", "845", "95", "RF123");
            AddStationVars(Station3Vars, 3, "BEKLÝYOR", "FALSE", "MANUAL", "FALSE", "0", "0", "");
            AddStationVars(Station4Vars, 4, "STOP", "TRUE", "BYPASS", "FALSE", "0", "0", "ERR01");
        }

        private PlcVariable CreateVar(string name, string value, string description, bool isEditable, string tag)
        {
            var v = new PlcVariable { Name = name, Value = value, Description = description, IsEditable = isEditable, PlcTag = tag };
            v.PropertyChanged += PlcVariable_PropertyChanged;
            return v;
        }

        private void PlcVariable_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is PlcVariable plcVar && e.PropertyName == nameof(PlcVariable.Value))
            {
                if (plcVar.Name == "SLIDER_POS_ACT")
                {
                    UpdateSliderPosition(plcVar.Value);
                }
                else
                {
                    // Update Stations based on Tag match
                    foreach (var station in Stations)
                    {
                        if (station.StatusTag == plcVar.Name)
                        {
                            // Map integer status to text if possible
                            station.ProcessStatus = MapStatusCode(plcVar.Value);
                        }
                        else if (station.AlarmTag == plcVar.Name)
                        {
                            station.HasAlarm = ParseBool(plcVar.Value);
                        }
                        else if (station.ModeTag == plcVar.Name)
                        {
                            if (Enum.TryParse<StationMode>(plcVar.Value, true, out var mode))
                                station.Mode = mode;
                        }
                        else if (station.ProducingTag == plcVar.Name)
                        {
                             station.IsProducing = ParseBool(plcVar.Value);
                        }
                        else if (station.ProductionCountTag == plcVar.Name)
                        {
                             station.ProductionCount = plcVar.Value;
                        }
                        else if (station.EfficiencyTag == plcVar.Name)
                        {
                             // Append % if not present, but usually better to have it in VM logic or UI conversion
                             // For now direct assignment
                             station.Efficiency = plcVar.Value.Contains("%") ? plcVar.Value : "%" + plcVar.Value;
                        }
                        else if (station.CurrentRfidTag == plcVar.Name)
                        {
                             station.CurrentRfid = plcVar.Value;
                        }
                    }
                }
            }
        }

        private bool ParseBool(string value)
        {
             if (string.IsNullOrEmpty(value)) return false;
             value = value.ToUpper();
             return value == "TRUE" || value == "1" || value == "ON";
        }

        private string MapStatusCode(string value)
        {
            if (value == "1") return "3D TARAMA";
            if (value == "2") return "GAZ KAÇAK TESTÝ";
            if (value == "3") return "TEST TAMAMLANDI";
            if (value == "4") return "OK ÜRÜN";
            if (value == "5") return "NOK ÜRÜN";
            if (value == "6") return "HAZIRLANIYOR";
            return value; // Return original if no match
        }

        private void UpdateSliderPosition(string value)
        {
            // Reset all
            foreach (var station in Stations)
            {
                station.IsRobotPresent = false;
            }

            if (int.TryParse(value, out int pos) && pos >= 1 && pos <= 4)
            {
                // pos 1 -> Index 0
                Stations[pos - 1].IsRobotPresent = true;
            }
        }

        private void RemovePlcVariable_Click(object sender, RoutedEventArgs e)
        {
             // Deprecated functionality
        }

        private void InitializeStations()
        {
            Stations.Add(new StationViewModel 
            { 
                Name = "ÝSTASYON 1", 
                Description = "Klima Dýþ Ünite - Ref 2341",
                Mode = StationMode.Auto, 
                IsProducing = true, 
                ProcessStatus = "3D TARAMA",
                HasAlarm = false,
                StatusTag = "ST1_STATUS",
                AlarmTag = "ST1_ALARM",
                ModeTag = "ST1_MODE",
                ProducingTag = "ST1_PRODUCING",
                ProductionCountTag = "ST1_PROD_COUNT",
                EfficiencyTag = "ST1_EFFICIENCY",
                ProductionCount = "1,245",
                Efficiency = "%92",
                AllowedRfid = "RF123",
                CurrentRfid = "RF123"
            });

            Stations.Add(new StationViewModel 
            { 
                Name = "ÝSTASYON 2", 
                Description = "Klima Dýþ Ünite - Ref 2341",
                Mode = StationMode.Auto, 
                IsProducing = true, 
                ProcessStatus = "GAZ TESTÝ",
                IsRobotPresent = true,
                HasAlarm = false,
                StatusTag = "ST2_STATUS",
                AlarmTag = "ST2_ALARM",
                ModeTag = "ST2_MODE",
                ProducingTag = "ST2_PRODUCING",
                ProductionCountTag = "ST2_PROD_COUNT",
                EfficiencyTag = "ST2_EFFICIENCY",
                CurrentRfidTag = "ST2_RFID_ACT",
                ProductionCount = "845",
                Efficiency = "%95",
                AllowedRfid = "RF123",
                CurrentRfid = "RF123"
            });

            Stations.Add(new StationViewModel 
            { 
                Name = "ÝSTASYON 3", 
                Description = "Boþ Ýstasyon",
                Mode = StationMode.Manual, 
                IsProducing = false, 
                HasAlarm = false,
                StatusTag = "ST3_STATUS",
                AlarmTag = "ST3_ALARM",
                ModeTag = "ST3_MODE",
                ProducingTag = "ST3_PRODUCING",
                ProductionCountTag = "ST3_PROD_COUNT",
                EfficiencyTag = "ST3_EFFICIENCY",
                CurrentRfidTag = "ST3_RFID_ACT",
                ProductionCount = "0",
                Efficiency = "-",
                AllowedRfid = "RF123",
                CurrentRfid = ""
            });

            Stations.Add(new StationViewModel 
            { 
                Name = "ÝSTASYON 4", 
                Description = "Klima Dýþ Ünite - Hatalý",
                Mode = StationMode.Bypass, 
                IsProducing = false, 
                HasAlarm = true,
                StatusTag = "ST4_STATUS",
                AlarmTag = "ST4_ALARM",
                ModeTag = "ST4_MODE",
                ProducingTag = "ST4_PRODUCING",
                ProductionCountTag = "ST4_PROD_COUNT",
                EfficiencyTag = "ST4_EFFICIENCY",
                CurrentRfidTag = "ST4_RFID_ACT",
                ProductionCount = "0",
                Efficiency = "STOP",
                AllowedRfid = "RF123",
                CurrentRfid = "ERR01"
            });
        }
    }

    public enum StationMode
    {
        Auto,
        Manual,
        Bypass
    }

    public class StationViewModel : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Description { get; set; }
        
        public string StatusTag { get; set; }
        public string AlarmTag { get; set; }
        public string ModeTag { get; set; }
        public string ProducingTag { get; set; }
        public string ProductionCountTag { get; set; }
        public string EfficiencyTag { get; set; }
        public string CurrentRfidTag { get; set; }

        private string _allowedRfid;
        public string AllowedRfid
        {
            get => _allowedRfid;
            set { _allowedRfid = value; OnPropertyChanged(); UpdateVisuals(); }
        }

        private string _currentRfid;
        public string CurrentRfid
        {
            get => _currentRfid;
            set { _currentRfid = value; OnPropertyChanged(); UpdateVisuals(); }
        }

        private StationMode _mode;
        public StationMode Mode
        {
            get => _mode;
            set { _mode = value; OnPropertyChanged(); UpdateVisuals(); }
        }

        private bool _isProducing;
        public bool IsProducing
        {
            get => _isProducing;
            set { _isProducing = value; OnPropertyChanged(); UpdateVisuals(); }
        }

        private bool _hasAlarm;
        public bool HasAlarm
        {
            get => _hasAlarm;
            set { _hasAlarm = value; OnPropertyChanged(); UpdateVisuals(); }
        }

        private bool _isRobotPresent;
        public bool IsRobotPresent
        {
            get => _isRobotPresent;
            set { _isRobotPresent = value; OnPropertyChanged(); UpdateVisuals(); }
        }

        private string _processStatus;
        public string ProcessStatus
        {
            get => _processStatus;
            set { _processStatus = value; OnPropertyChanged(); UpdateVisuals(); }
        }

        private string _productionCount = "0";
        public string ProductionCount
        {
             get => _productionCount;
             set { _productionCount = value; OnPropertyChanged(); }
        }

        private string _efficiency = "0";
        public string Efficiency
        {
             get => _efficiency;
             set { _efficiency = value; OnPropertyChanged(); }
        }

        // Computed Properties for UI Binding
        public string ModeText => Mode.ToString().ToUpper();
        public SolidColorBrush ModeColor { get; private set; }
        public SolidColorBrush BorderColor { get; private set; }
        public string StateText { get; private set; }
        public SolidColorBrush StateColor { get; private set; }
        public string AlarmText => HasAlarm ? "ALARM VAR" : "NORMAL";
        public Visibility AlarmVisibility => HasAlarm ? Visibility.Visible : Visibility.Collapsed;
        public Visibility RobotVisibility => IsRobotPresent ? Visibility.Visible : Visibility.Collapsed;
        public float RobotOpacity => IsRobotPresent ? 1.0f : 0.0f;
        
        public bool IsRfidMatch => !string.IsNullOrEmpty(AllowedRfid) && !string.IsNullOrEmpty(CurrentRfid) && AllowedRfid == CurrentRfid;
        public string RfidMatchIcon => IsRfidMatch ? "\uE73E" : "\uE711"; // Checkmark vs Cancel
        public SolidColorBrush RfidMatchColor => IsRfidMatch 
             ? new SolidColorBrush(Color.FromArgb(255, 46, 204, 113)) 
             : new SolidColorBrush(Color.FromArgb(255, 231, 76, 60));

        public string BypassButtonText => Mode == StationMode.Bypass ? "ETKÝNLEÞTÝR" : "BYPASS ET";
        public SolidColorBrush BypassButtonColor => Mode == StationMode.Bypass 
            ? new SolidColorBrush(Color.FromArgb(255, 46, 204, 113)) // Green for Enable
            : new SolidColorBrush(Color.FromArgb(255, 147, 112, 219)); // Purple for Bypass

        public void ToggleBypass()
        {
            if (Mode == StationMode.Bypass)
            {
                Mode = StationMode.Auto;
                // Optionally reset alarm or status
            }
            else
            {
                Mode = StationMode.Bypass;
                IsProducing = false;
                HasAlarm = false; // Usually bypass clears alarm state visually
            }
            OnPropertyChanged(nameof(BypassButtonText));
            OnPropertyChanged(nameof(BypassButtonColor));
        }

        public StationViewModel()
        {
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            // Mode Colors
            switch (Mode)
            {
                case StationMode.Auto:
                    ModeColor = new SolidColorBrush(Color.FromArgb(255, 46, 204, 113)); // LimeGreen
                    break;
                case StationMode.Manual:
                    ModeColor = new SolidColorBrush(Color.FromArgb(255, 255, 165, 0)); // Orange
                    break;
                case StationMode.Bypass:
                    ModeColor = new SolidColorBrush(Color.FromArgb(255, 147, 112, 219)); // MediumPurple
                    break;
            }

            // State & Alarm Logic
            if (HasAlarm)
            {
                StateText = "HATA";
                StateColor = new SolidColorBrush(Color.FromArgb(255, 231, 76, 60)); // Red
                BorderColor = new SolidColorBrush(Color.FromArgb(255, 231, 76, 60)); // Red
            }
            else
            {
                BorderColor = new SolidColorBrush(Color.FromArgb(255, 85, 85, 85)); // Gray
                
                // Determine Text & Color based on explicitly mapped ProcessStatus OR IsProducing flag
                string status = ProcessStatus;
                
                bool isSpecificStatus = !string.IsNullOrEmpty(status) && 
                                      (status == "3D TARAMA" || status == "GAZ KAÇAK TESTÝ" || 
                                       status == "TEST TAMAMLANDI" || status == "OK ÜRÜN" || 
                                       status == "NOK ÜRÜN" || status == "HAZIRLANIYOR" ||
                                       status == "3" || status == "4" || status == "5"); 

                if (isSpecificStatus)
                {
                     StateText = status;
                     if (status == "OK ÜRÜN")
                        StateColor = new SolidColorBrush(Color.FromArgb(255, 46, 204, 113)); // LimeGreen
                     else if (status == "NOK ÜRÜN")
                        StateColor = new SolidColorBrush(Color.FromArgb(255, 231, 76, 60)); // Red
                     else if (status == "HAZIRLANIYOR")
                        StateColor = new SolidColorBrush(Color.FromArgb(255, 241, 196, 15)); // Yellow
                     else
                        StateColor = new SolidColorBrush(Color.FromArgb(255, 52, 152, 219)); // Blue (Process)

                     // Sync Border Color with State Color
                     BorderColor = StateColor;
                }
                else
                {
                    // Fallback to IsProducing logic if no specific mapped status is active
                    if (IsProducing)
                    {
                        StateText = !string.IsNullOrEmpty(status) ? status : "ÜRETÝMDE";
                        StateColor = new SolidColorBrush(Color.FromArgb(255, 46, 204, 113)); // LimeGreen (Default active)
                        BorderColor = StateColor;
                    }
                    else
                    {
                        StateText = "BEKLÝYOR";
                        StateColor = new SolidColorBrush(Color.FromArgb(255, 136, 136, 136)); // Gray
                        BorderColor = new SolidColorBrush(Color.FromArgb(255, 85, 85, 85)); // Gray
                    }
                }
            }
            
            OnPropertyChanged(nameof(ModeText));
            OnPropertyChanged(nameof(ModeColor));
            OnPropertyChanged(nameof(BorderColor));
            OnPropertyChanged(nameof(StateText));
            OnPropertyChanged(nameof(StateColor));
            OnPropertyChanged(nameof(AlarmText));
            OnPropertyChanged(nameof(AlarmVisibility));
            OnPropertyChanged(nameof(RobotVisibility));
            OnPropertyChanged(nameof(RobotOpacity));
            OnPropertyChanged(nameof(BypassButtonText));
            OnPropertyChanged(nameof(BypassButtonColor));
            OnPropertyChanged(nameof(RfidMatchIcon));
            OnPropertyChanged(nameof(RfidMatchColor));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class PlcVariable : INotifyPropertyChanged
    {
        public string Name { get; set; }
        
        private string _value;
        public string Value 
        { 
            get => _value;
            set { _value = value; OnPropertyChanged(); }
        }
        
        public string Description { get; set; }
        public bool IsEditable { get; set; }
        public bool IsReadOnly => !IsEditable;
        
        private string _plcTag;
        public string PlcTag
        {
             get => _plcTag;
             set 
             { 
                 if (_plcTag != value)  // Only update if value actually changed
                 {
                     _plcTag = value; 
                     OnPropertyChanged(); 
                 }
             }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
