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

        public Auto_Page()
        {
            this.InitializeComponent();
            InitializeStations();
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
                HasAlarm = false 
            });

            Stations.Add(new StationViewModel 
            { 
                Name = "ÝSTASYON 2", 
                Description = "Klima Dýþ Ünite - Ref 2341",
                Mode = StationMode.Auto, 
                IsProducing = true, 
                ProcessStatus = "GAZ TESTÝ",
                HasAlarm = false 
            });

            Stations.Add(new StationViewModel 
            { 
                Name = "ÝSTASYON 3", 
                Description = "Boþ Ýstasyon",
                Mode = StationMode.Manual, 
                IsProducing = false, 
                HasAlarm = false 
            });

            Stations.Add(new StationViewModel 
            { 
                Name = "ÝSTASYON 4", 
                Description = "Klima Dýþ Ünite - Hatalý",
                Mode = StationMode.Bypass, 
                IsProducing = false, 
                HasAlarm = true 
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
            OnPropertyChanged(nameof(BypassButtonText));
            OnPropertyChanged(nameof(BypassButtonColor));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
