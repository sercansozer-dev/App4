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
        public ObservableCollection<StationViewModel> Stations { get; set; } = new();
        public ObservableCollection<PlcVariable> GeneralVars { get; set; } = new();
        public ObservableCollection<PlcVariable> Station1Vars { get; set; } = new();
        public ObservableCollection<PlcVariable> Station2Vars { get; set; } = new();
        public ObservableCollection<PlcVariable> Station3Vars { get; set; } = new();
        public ObservableCollection<PlcVariable> Station4Vars { get; set; } = new();

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
            void AddStationVars(ObservableCollection<PlcVariable> targetCollection, int stationId, string status, string alarm, string mode, string producing)
            {
                 targetCollection.Add(CreateVar($"ST{stationId}_STATUS", status, $"Ýstasyon {stationId} Durum", true, $"DB10.DBD{(stationId-1)*20}"));
                 targetCollection.Add(CreateVar($"ST{stationId}_ALARM", alarm, $"Ýstasyon {stationId} Alarm", true, $"DB10.DBX{((stationId-1)*20)+4}.0"));
                 targetCollection.Add(CreateVar($"ST{stationId}_MODE", mode, $"Ýstasyon {stationId} Mod", true, $"DB10.DBG{((stationId-1)*20)+6}"));
                 targetCollection.Add(CreateVar($"ST{stationId}_PRODUCING", producing, $"Ýstasyon {stationId} Üretim", true, $"DB10.DBX{((stationId-1)*20)+8}.0"));
            }

            AddStationVars(Station1Vars, 1, "3D TARAMA", "FALSE", "AUTO", "TRUE");
            AddStationVars(Station2Vars, 2, "GAZ TESTÝ", "FALSE", "AUTO", "TRUE");
            AddStationVars(Station3Vars, 3, "BEKLÝYOR", "FALSE", "MANUAL", "FALSE");
            AddStationVars(Station4Vars, 4, "STOP", "TRUE", "BYPASS", "FALSE");
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
                            station.ProcessStatus = plcVar.Value;
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
                ProducingTag = "ST1_PRODUCING"
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
                ProducingTag = "ST2_PRODUCING" 
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
                ProducingTag = "ST3_PRODUCING"
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
                ProducingTag = "ST4_PRODUCING"
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
                
                if (IsProducing)
                {
                    StateText = !string.IsNullOrEmpty(ProcessStatus) ? ProcessStatus : "ÜRETÝMDE";
                    StateColor = new SolidColorBrush(Color.FromArgb(255, 46, 204, 113)); // LimeGreen
                }
                else
                {
                    StateText = "BEKLÝYOR";
                    StateColor = new SolidColorBrush(Color.FromArgb(255, 136, 136, 136)); // Gray
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
             set { _plcTag = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
