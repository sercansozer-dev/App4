using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Newtonsoft.Json;

namespace App4.Utilities
{
    // ═══════════════════════════════════════════════════════════════
    // ARIZA / ALARM KAYIT MODELİ
    // TrendRecord deseninin (TrendDataService.cs) arıza karşılığı.
    // Bir arıza GELDİĞİNDE Aktif=true ile açılır; GEÇTİĞİNDE BitisZamani
    // + Aktif=false ile kapatılır → "Süre" hesaplanır. (Bkz. FaultLogService)
    // ═══════════════════════════════════════════════════════════════
    public class FaultRecord : INotifyPropertyChanged
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12];

        /// <summary>Arızanın başladığı an (kova seçimi + sıralama bunu kullanır).</summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string Kaynak { get; set; }       // "R1" / "R2" / "PLC" / "Sistem"
        public int RobotNo { get; set; }          // 1 / 2 ; 0 = sistem
        public int Kod { get; set; }              // G_HATA_KODU
        public string Mesaj { get; set; }         // çözümlenmiş hata mesajı
        public string Severity { get; set; } = "Arıza";  // Arıza / Uyarı / Bilgi
        public int AktifNokta { get; set; }       // G_AKTIF_NOKTA_NO (hata anındaki nokta)

        private DateTime? _bitisZamani;
        public DateTime? BitisZamani
        {
            get => _bitisZamani;
            set { _bitisZamani = value; OnPropertyChanged(); OnPropertyChanged(nameof(SureStr)); }
        }

        private bool _aktif = true;
        public bool Aktif
        {
            get => _aktif;
            set
            {
                if (_aktif == value) return;
                _aktif = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DurumStr));
                OnPropertyChanged(nameof(DurumIcon));
                OnPropertyChanged(nameof(SureStr));
            }
        }

        // İleride kullanım için (UI yok) — operatör onayı
        public bool KuitlendiMi { get; set; }
        public DateTime? KuitleyenZaman { get; set; }

        // Liste sıra no (Trend deseni)
        private int _siraNo;
        [JsonIgnore]
        public int SiraNo { get => _siraNo; set { if (_siraNo != value) { _siraNo = value; OnPropertyChanged(); } } }

        // ─── Hesaplanan gösterim alanları (serileştirilmez) ───
        [JsonIgnore] public string DateStr => Timestamp.ToString("dd.MM.yyyy");
        [JsonIgnore] public string TimeStr => Timestamp.ToString("HH:mm:ss");
        [JsonIgnore] public string KodStr => Kod > 0 ? Kod.ToString() : "-";
        [JsonIgnore] public string DurumStr => Aktif ? "AKTİF" : "Geçti";
        [JsonIgnore] public string DurumIcon => Aktif ? "🔴" : "✓";

        [JsonIgnore]
        public string SureStr
        {
            get
            {
                if (!BitisZamani.HasValue) return Aktif ? "Aktif" : "-";
                var ts = BitisZamani.Value - Timestamp;
                if (ts < TimeSpan.Zero) ts = TimeSpan.Zero;
                if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours} sa {ts.Minutes} dk";
                if (ts.TotalMinutes >= 1) return $"{(int)ts.TotalMinutes} dk {ts.Seconds} sn";
                return $"{ts.Seconds} sn";
            }
        }

        [JsonIgnore]
        public SolidColorBrush SeverityBrush => new SolidColorBrush(
            Severity == "Arıza" ? ColorHelper.FromArgb(255, 0xE7, 0x4C, 0x3C) :
            Severity == "Uyarı" ? ColorHelper.FromArgb(255, 0xF3, 0x9C, 0x12) :
                                  ColorHelper.FromArgb(255, 0x95, 0xA5, 0xA6));

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
