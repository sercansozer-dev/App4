using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace App4.Models
{
    /// <summary>
    /// Inficon P3000XL sniffer kaçak testi log kaydı
    /// </summary>
    public class InficonLeakLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string RobotName { get; set; }
        public string PipeId { get; set; }
        public string Result { get; set; }    // "OK" / "LEAK" / "ERROR"
        public double LeakRate { get; set; }
        public double Flow { get; set; }
        public double PE { get; set; }
    }

    /// <summary>
    /// Inficon P3000XL sniffer anlık durum modeli
    /// </summary>
    public class InficonSnifferState : INotifyPropertyChanged
    {
        public string Name { get; set; }

        private bool _ready;
        public bool Ready { get => _ready; set { if (_ready != value) { _ready = value; OnPropertyChanged(); } } }

        private bool _stable;
        public bool Stable { get => _stable; set { if (_stable != value) { _stable = value; OnPropertyChanged(); } } }

        private bool _leak;
        public bool Leak { get => _leak; set { if (_leak != value) { _leak = value; OnPropertyChanged(); } } }

        private bool _error;
        public bool Error { get => _error; set { if (_error != value) { _error = value; OnPropertyChanged(); } } }

        private bool _enable;
        public bool Enable { get => _enable; set { if (_enable != value) { _enable = value; OnPropertyChanged(); } } }

        private double _leakRate;
        public double LeakRate { get => _leakRate; set { if (Math.Abs(_leakRate - value) > 1e-15) { _leakRate = value; OnPropertyChanged(); } } }

        private double _pe;
        public double PE { get => _pe; set { if (Math.Abs(_pe - value) > 1e-15) { _pe = value; OnPropertyChanged(); } } }

        private double _flow;
        public double Flow { get => _flow; set { if (Math.Abs(_flow - value) > 1e-15) { _flow = value; OnPropertyChanged(); } } }

        private string _status = "---";
        public string Status { get => _status; set { if (_status != value) { _status = value; OnPropertyChanged(); } } }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Sniffer aktif nokta bazlı kaçak sonucu (Inficon_Page tablosunda gösterilir)
    /// </summary>
    public class SnifferPointResult : INotifyPropertyChanged
    {
        public string PointName { get; set; } = "";   // Aktif nokta adı (B1-Brazing, U3-UBend vb.)

        private string _result = "---";             // "OK", "NOK", "---"
        public string Result
        {
            get => _result;
            set { if (_result != value) { _result = value; OnPropertyChanged(); OnPropertyChanged(nameof(ResultColor)); } }
        }

        private double _leakRate;
        public double LeakRate
        {
            get => _leakRate;
            set { if (Math.Abs(_leakRate - value) > 1e-15) { _leakRate = value; OnPropertyChanged(); } }
        }

        private DateTime? _timestamp;
        public DateTime? Timestamp
        {
            get => _timestamp;
            set { _timestamp = value; OnPropertyChanged(); }
        }

        public SolidColorBrush ResultColor => Result switch
        {
            "OK" => new SolidColorBrush(Color.FromArgb(255, 46, 204, 113)),
            "NOK" => new SolidColorBrush(Color.FromArgb(255, 231, 76, 60)),
            _ => new SolidColorBrush(Color.FromArgb(255, 100, 100, 100))
        };

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
