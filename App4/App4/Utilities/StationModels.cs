using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace App4.Utilities  // <--- DÜZELTME: .Utilities EKLENDİ
{
    // --- TEMEL SINIFLAR VE ENUMLAR ---
    public class RfidDef
    {
        public string Id { get; set; }
        public string Description { get; set; }

        public override string ToString() => $"{Id} ({Description})";
    }

    public enum RfidOperationMode { Mixed, Specific }
    public enum StationMode { Auto, Manual, Bypass }

    // --- STATION VIEW MODEL ---
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
        public string AllowedRfid { get => _allowedRfid; set { _allowedRfid = value; OnPropertyChanged(); UpdateVisuals(); } }

        private string _currentRfid;
        public string CurrentRfid { get => _currentRfid; set { _currentRfid = value; OnPropertyChanged(); UpdateVisuals(); } }

        private StationMode _mode;
        public StationMode Mode { get => _mode; set { _mode = value; OnPropertyChanged(); UpdateVisuals(); } }

        private bool _isProducing;
        public bool IsProducing { get => _isProducing; set { _isProducing = value; OnPropertyChanged(); UpdateVisuals(); } }

        private bool _hasAlarm;
        public bool HasAlarm { get => _hasAlarm; set { _hasAlarm = value; OnPropertyChanged(); UpdateVisuals(); } }

        private bool _isRobotPresent;
        public bool IsRobotPresent { get => _isRobotPresent; set { _isRobotPresent = value; OnPropertyChanged(); UpdateVisuals(); } }

        private string _processStatus;
        public string ProcessStatus { get => _processStatus; set { _processStatus = value; OnPropertyChanged(); UpdateVisuals(); } }

        private string _productionCount = "0";
        public string ProductionCount { get => _productionCount; set { _productionCount = value; OnPropertyChanged(); } }

        private string _efficiency = "0";
        public string Efficiency { get => _efficiency; set { _efficiency = value; OnPropertyChanged(); } }

        // Görsel Özellikler
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

        public string RfidMatchIcon => IsRfidMatch ? "\uE73E" : "\uE711";
        public SolidColorBrush RfidMatchColor => IsRfidMatch ? new SolidColorBrush(Color.FromArgb(255, 46, 204, 113)) : new SolidColorBrush(Color.FromArgb(255, 231, 76, 60));

        public string BypassButtonText => Mode == StationMode.Bypass ? "ETKİNLEŞTİR" : "BYPASS ET";
        public SolidColorBrush BypassButtonColor => Mode == StationMode.Bypass ? new SolidColorBrush(Color.FromArgb(255, 46, 204, 113)) : new SolidColorBrush(Color.FromArgb(255, 147, 112, 219));

        public void ToggleBypass()
        {
            if (Mode == StationMode.Bypass) { Mode = StationMode.Auto; }
            else { Mode = StationMode.Bypass; IsProducing = false; HasAlarm = false; }
            OnPropertyChanged(nameof(BypassButtonText));
            OnPropertyChanged(nameof(BypassButtonColor));
        }

        public StationViewModel() { UpdateVisuals(); }

        private void UpdateVisuals()
        {
            switch (Mode)
            {
                case StationMode.Auto: ModeColor = new SolidColorBrush(Color.FromArgb(255, 46, 204, 113)); break;
                case StationMode.Manual: ModeColor = new SolidColorBrush(Color.FromArgb(255, 255, 165, 0)); break;
                case StationMode.Bypass: ModeColor = new SolidColorBrush(Color.FromArgb(255, 147, 112, 219)); break;
            }

            if (HasAlarm)
            {
                StateText = "HATA";
                StateColor = new SolidColorBrush(Color.FromArgb(255, 231, 76, 60));
                BorderColor = new SolidColorBrush(Color.FromArgb(255, 231, 76, 60));
            }
            else
            {
                BorderColor = new SolidColorBrush(Color.FromArgb(255, 85, 85, 85));
                string status = ProcessStatus;
                bool isSpecificStatus = !string.IsNullOrEmpty(status) && (status == "3D TARAMA" || status == "GAZ KAÇAK TESTİ" || status == "TEST TAMAMLANDI" || status == "OK ÜRÜN" || status == "NOK ÜRÜN" || status == "HAZIRLANIYOR");

                if (isSpecificStatus)
                {
                    StateText = status;
                    if (status == "OK ÜRÜN") StateColor = new SolidColorBrush(Color.FromArgb(255, 46, 204, 113));
                    else if (status == "NOK ÜRÜN") StateColor = new SolidColorBrush(Color.FromArgb(255, 231, 76, 60));
                    else if (status == "HAZIRLANIYOR") StateColor = new SolidColorBrush(Color.FromArgb(255, 241, 196, 15));
                    else StateColor = new SolidColorBrush(Color.FromArgb(255, 52, 152, 219));
                    BorderColor = StateColor;
                }
                else
                {
                    if (IsProducing)
                    {
                        StateText = !string.IsNullOrEmpty(status) ? status : "ÜRETİMDE";
                        StateColor = new SolidColorBrush(Color.FromArgb(255, 46, 204, 113));
                        BorderColor = StateColor;
                    }
                    else
                    {
                        StateText = "BEKLİYOR";
                        StateColor = new SolidColorBrush(Color.FromArgb(255, 136, 136, 136));
                        BorderColor = new SolidColorBrush(Color.FromArgb(255, 85, 85, 85));
                    }
                }
            }

            OnPropertyChanged(nameof(ModeText)); OnPropertyChanged(nameof(ModeColor)); OnPropertyChanged(nameof(BorderColor));
            OnPropertyChanged(nameof(StateText)); OnPropertyChanged(nameof(StateColor)); OnPropertyChanged(nameof(AlarmText));
            OnPropertyChanged(nameof(AlarmVisibility)); OnPropertyChanged(nameof(RobotVisibility)); OnPropertyChanged(nameof(RobotOpacity));
            OnPropertyChanged(nameof(BypassButtonText)); OnPropertyChanged(nameof(BypassButtonColor));
            OnPropertyChanged(nameof(RfidMatchIcon)); OnPropertyChanged(nameof(RfidMatchColor));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // --- EXTENDED STATION VIEW MODEL ---
    public class ExtendedStationViewModel : StationViewModel
    {
        public string PlcTagRfidMode { get; set; }
        public string PlcTagTargetRfid { get; set; }

        // Artık veriyi GlobalData'dan çekiyor
        public ObservableCollection<RfidDef> RefRfids => App4.Utilities.GlobalData.KnownRfids;

        public List<RfidOperationMode> RfidOpModes { get; } = new() { RfidOperationMode.Mixed, RfidOperationMode.Specific };

        private RfidOperationMode _rfidOpMode;
        public RfidOperationMode RfidOpMode
        {
            get => _rfidOpMode;
            set { _rfidOpMode = value; OnPropertyChanged(nameof(RfidOpMode)); OnPropertyChanged(nameof(IsSpecificRfidVisible)); }
        }

        private string _targetRfid;
        public string TargetRfid
        {
            get => _targetRfid;
            set { _targetRfid = value; OnPropertyChanged(nameof(TargetRfid)); AllowedRfid = value; }
        }

        public Visibility IsSpecificRfidVisible => RfidOpMode == RfidOperationMode.Specific ? Visibility.Visible : Visibility.Collapsed;

        public ExtendedStationViewModel()
        {
            this.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Mode))
                {
                    OnPropertyChanged(nameof(IsAutoMode)); OnPropertyChanged(nameof(IsManualMode));
                    OnPropertyChanged(nameof(AutoBtnBg)); OnPropertyChanged(nameof(AutoBtnFg));
                    OnPropertyChanged(nameof(ManualBtnBg)); OnPropertyChanged(nameof(ManualBtnFg));
                }
            };
        }

        public bool IsAutoMode { get => Mode == StationMode.Auto; set => Mode = value ? StationMode.Auto : StationMode.Manual; }
        public bool IsManualMode { get => Mode != StationMode.Auto; set { if (value) Mode = StationMode.Manual; else Mode = StationMode.Auto; } }

        public SolidColorBrush AutoBtnBg => Mode == StationMode.Auto ? new SolidColorBrush(Color.FromArgb(255, 46, 204, 113)) : new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        public SolidColorBrush AutoBtnFg => Mode == StationMode.Auto ? new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
        public SolidColorBrush ManualBtnBg => Mode == StationMode.Manual ? new SolidColorBrush(Color.FromArgb(255, 255, 165, 0)) : new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        public SolidColorBrush ManualBtnFg => Mode == StationMode.Manual ? new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
    }
}