using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace App4.Utilities
{
    // ═══════════════════════════════════════════════════════════════
    // PLC INPUT → ALARM TANIMI
    // Admin, Arıza Kayıt sayfasındaki tabloda tanımlar:
    //   bir PLC input + tetik yönü (ON/OFF) + mesaj + önem.
    // GlobalData.EvaluatePlcAlarms() bu tanımları periyodik değerlendirir;
    // koşul sağlanınca FaultLogService'e alarm kaydı düşer.
    // ═══════════════════════════════════════════════════════════════
    public class AlarmDefinition : INotifyPropertyChanged
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

        private string _plcInputName = "";
        public string PlcInputName
        {
            get => _plcInputName;
            set { _plcInputName = value ?? ""; OnPropertyChanged(); }
        }

        /// <summary>true: sinyal ON (1) → alarm ; false: sinyal OFF (0) → alarm.</summary>
        private bool _triggerOnTrue = true;
        public bool TriggerOnTrue
        {
            get => _triggerOnTrue;
            set { if (_triggerOnTrue != value) { _triggerOnTrue = value; OnPropertyChanged(); OnPropertyChanged(nameof(TetikIndex)); OnPropertyChanged(nameof(TetikStr)); } }
        }

        private string _mesaj = "";
        public string Mesaj
        {
            get => _mesaj;
            set { _mesaj = value ?? ""; OnPropertyChanged(); }
        }

        private string _severity = "Arıza";
        public string Severity
        {
            get => _severity;
            set { _severity = string.IsNullOrEmpty(value) ? "Arıza" : value; OnPropertyChanged(); OnPropertyChanged(nameof(SeverityIndex)); }
        }

        private bool _enabled = true;
        public bool Enabled
        {
            get => _enabled;
            set { if (_enabled != value) { _enabled = value; OnPropertyChanged(); } }
        }

        // ─── Yardımcı / gösterim (serileştirilmez) ───
        [JsonIgnore] public string Key => $"ALARM|{Id}";
        [JsonIgnore] public string TetikStr => TriggerOnTrue ? "ON (1) → Alarm" : "OFF (0) → Alarm";

        /// <summary>Tetik ComboBox bağlama: 0 = ON, 1 = OFF.</summary>
        [JsonIgnore]
        public int TetikIndex
        {
            get => TriggerOnTrue ? 0 : 1;
            set => TriggerOnTrue = (value == 0);
        }

        /// <summary>Önem ComboBox bağlama: 0 = Arıza, 1 = Uyarı, 2 = Bilgi.</summary>
        [JsonIgnore]
        public int SeverityIndex
        {
            get => Severity == "Uyarı" ? 1 : (Severity == "Bilgi" ? 2 : 0);
            set => Severity = value == 1 ? "Uyarı" : (value == 2 ? "Bilgi" : "Arıza");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
