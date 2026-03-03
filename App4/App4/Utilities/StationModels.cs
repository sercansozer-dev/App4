using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Newtonsoft.Json;
using Windows.UI;

namespace App4.Utilities
{
    // --- INDEX'Lİ JOB ITEM (GÖRÜNTÜLEME İÇİN) ---
    public class IndexedJobItem : INotifyPropertyChanged
    {
        private int _index;
        private string _jobName;

        public int Index
        {
            get => _index;
            set { _index = value; OnPropertyChanged(); }
        }

        public string JobName
        {
            get => _jobName;
            set { _jobName = value; OnPropertyChanged(); }
        }

        // Sniffer olcum suresi (milisaniye) - her job icin ayri
        private double _snifferDuration = 5000;
        public double SnifferDuration
        {
            get => _snifferDuration;
            set { _snifferDuration = value; OnPropertyChanged(); }
        }

        // Olcum sonucu durumu (runtime): null=olcum yapilmamis, "OK", "NOK"
        private string _measurementStatus;
        [JsonIgnore]
        public string MeasurementStatus
        {
            get => _measurementStatus;
            set { _measurementStatus = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusColor)); }
        }

        // Durum rengi: gri=olcum yapilmamis, yesil=OK, kirmizi=NOK
        [JsonIgnore]
        public SolidColorBrush StatusColor
        {
            get
            {
                if (MeasurementStatus == "OK") return new SolidColorBrush(Color.FromArgb(255, 46, 204, 113));
                if (MeasurementStatus == "NOK") return new SolidColorBrush(Color.FromArgb(255, 231, 76, 60));
                return new SolidColorBrush(Color.FromArgb(255, 100, 100, 100)); // Gri
            }
        }

        // ═══ ANLIK INDEX GOSTERGESİ ═══
        // Sistem su an bu index'i olcecekse true → satir vurgulu gosterilir
        private bool _isCurrent;
        [JsonIgnore]
        public bool IsCurrent
        {
            get => _isCurrent;
            set
            {
                if (_isCurrent != value)
                {
                    _isCurrent = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RowBackground));
                    OnPropertyChanged(nameof(CurrentIndicator));
                }
            }
        }

        // Satir arka plani: aktif index ise turuncu vurgulu, degilse normal
        [JsonIgnore]
        public SolidColorBrush RowBackground =>
            IsCurrent
                ? new SolidColorBrush(Color.FromArgb(255, 50, 40, 20))   // Turuncu tonlu koyu
                : new SolidColorBrush(Color.FromArgb(255, 42, 42, 44));  // Normal #2A2A2C

        // Aktif index ikonu: ▶ veya bos
        [JsonIgnore]
        public string CurrentIndicator => IsCurrent ? "\u25B6" : "";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // --- TEMEL SINIFLAR ---
    public class RfidDef : INotifyPropertyChanged
    {
        public string Id { get; set; }
        public string Description { get; set; }

        private string _modelFileName;
        public string ModelFileName 
        { 
            get => _modelFileName; 
            set 
            { 
                if (_modelFileName != value)
                {
                    _modelFileName = value; 
                    OnPropertyChanged(); 
                }
            } 
        }

        // YENİ EKLENEN ALAN: Bu model için hangi Job dosyası yüklenecek?
        private ObservableCollection<string> _jobSequence = new();
        public ObservableCollection<string> JobSequence
        {
            get => _jobSequence;
            set
            {
                if (_jobSequence != null)
                    _jobSequence.CollectionChanged -= JobSequence_CollectionChanged;

                _jobSequence = value ?? new ObservableCollection<string>();
                _jobSequence.CollectionChanged += JobSequence_CollectionChanged;
                RefreshIndexedJobs();
                OnPropertyChanged();
            }
        }

        // Her job icin sniffer olcum suresi (milisaniye) - JobSequence ile paralel
        private ObservableCollection<double> _snifferDurations = new();
        public ObservableCollection<double> SnifferDurations
        {
            get => _snifferDurations;
            set { _snifferDurations = value ?? new ObservableCollection<double>(); OnPropertyChanged(); RefreshIndexedJobs(); }
        }

        // INDEX'Lİ JOB LİSTESİ (UI GÖRÜNTÜLEME İÇİN)
        [JsonIgnore]
        public ObservableCollection<IndexedJobItem> IndexedJobSequence { get; } = new();

        public RfidDef()
        {
            _jobSequence.CollectionChanged += JobSequence_CollectionChanged;
        }

        private void JobSequence_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshIndexedJobs();
            OnPropertyChanged(nameof(JobSequence));       // Kayıt tetiklensin (GlobalData.SaveRfids)
        }

        private void RefreshIndexedJobs()
        {
            IndexedJobSequence.Clear();
            // SnifferDurations listesini JobSequence ile ayni boyutta tut
            while (_snifferDurations.Count < _jobSequence.Count) _snifferDurations.Add(5000);
            while (_snifferDurations.Count > _jobSequence.Count) _snifferDurations.RemoveAt(_snifferDurations.Count - 1);

            for (int i = 0; i < _jobSequence.Count; i++)
            {
                IndexedJobSequence.Add(new IndexedJobItem
                {
                    Index = i,
                    JobName = _jobSequence[i],
                    SnifferDuration = _snifferDurations[i]
                });
            }
            OnPropertyChanged(nameof(IndexedJobSequence));
        }

        // INDEX DISPLAY - Listedeki sıra numarası (1-based)
        private int _indexDisplay;
        [JsonIgnore]
        public int IndexDisplay
        {
            get => _indexDisplay;
            set { if (_indexDisplay != value) { _indexDisplay = value; OnPropertyChanged(); } }
        }

        // ═══ ANLIK ÖLÇÜM INDEX'İ ═══
        // Sistemin su an olcecegi job index'i (0-based, -1=hicbiri)
        private int _currentJobIndex = -1;
        [JsonIgnore]
        public int CurrentJobIndex
        {
            get => _currentJobIndex;
            set
            {
                if (_currentJobIndex != value)
                {
                    _currentJobIndex = value;
                    // IndexedJobSequence icindeki tum item'larin IsCurrent'ini guncelle
                    for (int i = 0; i < IndexedJobSequence.Count; i++)
                    {
                        IndexedJobSequence[i].IsCurrent = (i == _currentJobIndex);
                    }
                    OnPropertyChanged();
                }
            }
        }

        // ═══ AKTİF KLİMA TİPİ GÖSTERİMİ ═══
        // Aktüel istasyondaki RFID bu klima tipine eşitse premium yeşil tasarım
        private bool _isActive;
        [JsonIgnore]
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ActiveBorderBrush));
                    OnPropertyChanged(nameof(ActiveBorderThickness));
                    OnPropertyChanged(nameof(ActiveCardBackground));
                    OnPropertyChanged(nameof(ActiveHeaderBackground));
                    OnPropertyChanged(nameof(ActiveIdBadgeBrush));
                    OnPropertyChanged(nameof(ActiveSeparatorBrush));
                    OnPropertyChanged(nameof(ActiveLabelVisibility));
                }
            }
        }

        // --- Kart Cercevesi ---
        [JsonIgnore]
        public SolidColorBrush ActiveBorderBrush =>
            IsActive
                ? new SolidColorBrush(Color.FromArgb(255, 46, 204, 113))   // Parlak Yesil
                : new SolidColorBrush(Color.FromArgb(255, 68, 68, 68));    // #444

        [JsonIgnore]
        public Thickness ActiveBorderThickness =>
            IsActive ? new Thickness(2) : new Thickness(1);

        // --- Kart Arka Plani ---
        [JsonIgnore]
        public SolidColorBrush ActiveCardBackground =>
            IsActive
                ? new SolidColorBrush(Color.FromArgb(255, 22, 34, 22))     // Koyu yesil ton (#162216)
                : new SolidColorBrush(Color.FromArgb(255, 32, 32, 32));    // Normal #202020

        // --- Baslik Arka Plani (RFID badge + description satiri) ---
        [JsonIgnore]
        public SolidColorBrush ActiveHeaderBackground =>
            IsActive
                ? new SolidColorBrush(Color.FromArgb(255, 20, 42, 25))     // Yesil tonlu koyu (#142A19)
                : new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));         // Transparan (normal)

        // --- RFID Badge Rengi ---
        [JsonIgnore]
        public SolidColorBrush ActiveIdBadgeBrush =>
            IsActive
                ? new SolidColorBrush(Color.FromArgb(255, 39, 174, 96))    // Yesil (#27AE60)
                : new SolidColorBrush(Color.FromArgb(255, 0, 164, 239));   // Mavi (#00A4EF)

        // --- Ayirici Cizgi ---
        [JsonIgnore]
        public SolidColorBrush ActiveSeparatorBrush =>
            IsActive
                ? new SolidColorBrush(Color.FromArgb(255, 46, 204, 113))   // Yesil
                : new SolidColorBrush(Color.FromArgb(255, 51, 51, 51));    // #333

        // --- "AKTİF" Etiketi Gorunurlugu ---
        [JsonIgnore]
        public Visibility ActiveLabelVisibility =>
            IsActive ? Visibility.Visible : Visibility.Collapsed;

        public override string ToString() => $"{Id} ({Description})";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public enum RfidOperationMode { Mixed, Specific }
    public enum StationMode { Auto, Manual, Bypass }

    // --- LOG ENTRY SINIFI (Hata veren eksik parça buydu) ---
    public class LogEntry
    {
        public string TimeStr { get; set; }
        public string Message { get; set; }
        public string ColorCode { get; set; } // "Red", "Green", "White" vs.

        // UI tarafında renk dönüşümü için
        public SolidColorBrush ColorBrush
        {
            get
            {
                if (ColorCode == "Red") return new SolidColorBrush(Color.FromArgb(255, 231, 76, 60));
                if (ColorCode == "Green") return new SolidColorBrush(Color.FromArgb(255, 46, 204, 113));
                if (ColorCode == "Yellow") return new SolidColorBrush(Color.FromArgb(255, 241, 196, 15));
                return new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
            }
        }
    }

    public class GocatorMeasurement : INotifyPropertyChanged
    {
        public int Id { get; set; }           // Sıra No (1, 2, 3...)
        public string Name { get; set; }      // Ölçüm Adı (Örn: X Offset)
        public double Value { get; set; }     // Değer (Örn: 12.45)
        public string Unit { get; set; } = "mm";
        public string Decision { get; set; }  // Pass / Fail
        public int SourceId { get; set; }     // Sensördeki ID'si

        // UI Rengi: Pass ise Yeşil, Fail ise Kırmızı
        public SolidColorBrush StatusColor => (Decision == "Pass" || Decision == "OK")
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80))  // Yeşil
            : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 231, 76, 60)); // Kırmızı

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }


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
        public string AllowedRfid { get => _allowedRfid; set { if (_allowedRfid != value) { _allowedRfid = value; OnPropertyChanged(); UpdateVisuals(); } } }

        private string _currentRfid;
        public string CurrentRfid { get => _currentRfid; set { if (_currentRfid != value) { _currentRfid = value; OnPropertyChanged(); UpdateVisuals(); } } }

        private StationMode _mode;
        public StationMode Mode { get => _mode; set { if (_mode != value) { _mode = value; OnPropertyChanged(); UpdateVisuals(); } } }

        private bool _isProducing;
        public bool IsProducing { get => _isProducing; set { if (_isProducing != value) { _isProducing = value; OnPropertyChanged(); UpdateVisuals(); } } }

        private bool _hasAlarm;
        public bool HasAlarm { get => _hasAlarm; set { if (_hasAlarm != value) { _hasAlarm = value; OnPropertyChanged(); UpdateVisuals(); } } }

        private bool _isRobotPresent;
        public bool IsRobotPresent { get => _isRobotPresent; set { if (_isRobotPresent != value) { _isRobotPresent = value; OnPropertyChanged(); UpdateVisuals(); } } }

        private string _processStatus;
        public string ProcessStatus { get => _processStatus; set { if (_processStatus != value) { _processStatus = value; OnPropertyChanged(); UpdateVisuals(); } } }

        private string _productionCount = "0";
        public string ProductionCount { get => _productionCount; set { _productionCount = value; OnPropertyChanged(); } }

        private string _efficiency = "0";
        public string Efficiency { get => _efficiency; set { _efficiency = value; OnPropertyChanged(); } }

        // Robot HOME durumu
        private bool _isRobotHome;
        public bool IsRobotHome
        {
            get => _isRobotHome;
            set { _isRobotHome = value; UpdateVisuals(); }
        }

        // Görsel Özellikler
        public string ModeText => Mode.ToString().ToUpper();
        public SolidColorBrush ModeColor { get; private set; }
        public SolidColorBrush BorderColor { get; private set; }
        public string StateText { get; private set; }
        public string StateGlyph { get; private set; } = "\uE9F5"; // Varsayılan: dişli ikon
        public SolidColorBrush StateColor { get; private set; }
        public string AlarmText => HasAlarm ? "ALARM VAR" : "NORMAL";
        public Visibility AlarmVisibility => HasAlarm ? Visibility.Visible : Visibility.Collapsed;
        public Visibility RobotVisibility => IsRobotPresent ? Visibility.Visible : Visibility.Collapsed;
        public float RobotOpacity => IsRobotPresent ? 1.0f : 0.0f;
        
        private bool _ignoreRfidMatch = false;
        public bool IgnoreRfidMatch
        {
            get => _ignoreRfidMatch;
            set { _ignoreRfidMatch = value; UpdateVisuals(); }
        }

        public bool IsRfidMatch => IgnoreRfidMatch || (!string.IsNullOrEmpty(AllowedRfid) && !string.IsNullOrEmpty(CurrentRfid) && AllowedRfid.Trim() == CurrentRfid.Trim());
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

        // StationModels.cs dosyasındaki StationViewModel sınıfının içindeki UpdateVisuals metodunu bununla değiştirin:

        protected virtual void UpdateVisuals()
        {
            // 1. MOD RENGİ (OTOMATİK/MANUEL/BYPASS)
            switch (Mode)
            {
                case StationMode.Auto: ModeColor = new SolidColorBrush(Color.FromArgb(255, 46, 204, 113)); break;   // Yeşil
                case StationMode.Manual: ModeColor = new SolidColorBrush(Color.FromArgb(255, 255, 165, 0)); break; // Turuncu
                case StationMode.Bypass: ModeColor = new SolidColorBrush(Color.FromArgb(255, 147, 112, 219)); break; // Mor
            }

            // 2. ANA DURUM KONTROLÜ
            if (HasAlarm)
            {
                // Alarm varsa her şey kırmızı
                StateText = "HATA";
                StateGlyph = "\uE783"; // Uyarı ikonu
                StateColor = new SolidColorBrush(Color.FromArgb(255, 231, 76, 60)); // Kırmızı
                BorderColor = StateColor;
            }
            else if (IsRobotHome && !IsProducing)
            {
                // Robot bu istasyonda HOME pozisyonunda
                StateText = "ROBOT HOME";
                StateGlyph = "\uE80F"; // Ev ikonu
                StateColor = new SolidColorBrush(Color.FromArgb(255, 0, 188, 212)); // Cyan
                BorderColor = new SolidColorBrush(Color.FromArgb(255, 0, 150, 170));
            }
            else
            {
                // Alarm yoksa ProcessStatus'a bak
                StateGlyph = "\uE9F5"; // Varsayılan dişli ikon
                string status = ProcessStatus;

                // Özel Durumlar Listesi (Auto_Page.xaml.cs'deki MapStatus ile tam eşleşmeli)
                bool isProcess = (status == "3D TARAMA" || status == "GAZ KAÇAK TESTİ" || status == "TEST TAMAMLANDI");
                bool isOk = (status == "OK ÜRÜN" || status == "OK");
                bool isNok = (status == "NOK ÜRÜN" || status == "NOK");
                bool isWaiting = (status == "HAZIRLANIYOR" || status == "BEKLİYOR");

                // Duruma Göre Renk ve Metin Ata
                if (isProcess)
                {
                    StateText = status;
                    StateColor = new SolidColorBrush(Color.FromArgb(255, 52, 152, 219)); // Mavi (Process)
                    BorderColor = StateColor;
                }
                else if (isOk)
                {
                    StateText = "OK ÜRÜN";
                    StateColor = new SolidColorBrush(Color.FromArgb(255, 46, 204, 113)); // Yeşil (Başarılı)
                    BorderColor = StateColor;
                }
                else if (isNok)
                {
                    StateText = "NOK ÜRÜN";
                    StateColor = new SolidColorBrush(Color.FromArgb(255, 231, 76, 60)); // Kırmızı (Hatalı)
                    BorderColor = StateColor;
                }
                else if (isWaiting)
                {
                    StateText = status;
                    StateColor = new SolidColorBrush(Color.FromArgb(255, 241, 196, 15)); // Sarı (Bekliyor/Hazırlanıyor)
                    BorderColor = new SolidColorBrush(Color.FromArgb(255, 85, 85, 85)); // Gri Çerçeve
                }
                else
                {
                    // Tanımsız bir durum veya "ÜRETİMDE" gibi genel durumlar
                    if (IsProducing)
                    {
                        StateText = !string.IsNullOrEmpty(status) ? status : "ÜRETİMDE";
                        StateColor = new SolidColorBrush(Color.FromArgb(255, 52, 152, 219)); // Mavi
                        BorderColor = StateColor;
                    }
                    else
                    {
                        StateText = "BEKLİYOR";
                        StateColor = new SolidColorBrush(Color.FromArgb(255, 136, 136, 136)); // Gri Metin
                        BorderColor = new SolidColorBrush(Color.FromArgb(255, 85, 85, 85));   // Gri Çerçeve
                    }
                }
            }

            // Değişiklikleri Arayüze Bildir
            OnPropertyChanged(nameof(ModeText)); OnPropertyChanged(nameof(ModeColor)); OnPropertyChanged(nameof(BorderColor));
            OnPropertyChanged(nameof(StateText)); OnPropertyChanged(nameof(StateGlyph)); OnPropertyChanged(nameof(StateColor)); OnPropertyChanged(nameof(AlarmText));
            OnPropertyChanged(nameof(AlarmVisibility)); OnPropertyChanged(nameof(RobotVisibility)); OnPropertyChanged(nameof(RobotOpacity));
            OnPropertyChanged(nameof(BypassButtonText)); OnPropertyChanged(nameof(BypassButtonColor));
            OnPropertyChanged(nameof(RfidMatchIcon)); OnPropertyChanged(nameof(RfidMatchColor));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    public class SystemCheckItem
    {
        public string TagName { get; set; }
        public string ErrorMessage { get; set; }

        // Helper property for display binding
        public string DisplayText => $"{TagName} -> {ErrorMessage}";
    }
    // --- EXTENDED STATION VIEW MODEL ---
    public class ExtendedStationViewModel : StationViewModel
    {
        public string PlcTagRfidMode { get; set; }
        public string PlcTagTargetRfid { get; set; }

        public ObservableCollection<RfidDef> RefRfids => GlobalData.KnownRfids;
        public List<RfidOperationMode> RfidOpModes { get; } = new() { RfidOperationMode.Mixed, RfidOperationMode.Specific };

        private RfidOperationMode _rfidOpMode;
        public RfidOperationMode RfidOpMode
        {
            get => _rfidOpMode;
            set 
            { 
                if (_rfidOpMode != value)
                {
                    _rfidOpMode = value; 
                    IgnoreRfidMatch = (_rfidOpMode == RfidOperationMode.Mixed);
                    OnPropertyChanged(nameof(RfidOpMode)); 
                    OnPropertyChanged(nameof(IsSpecificRfidVisible)); 
                }
            }
        }

        private string _targetRfid;
        public string TargetRfid
        {
            get => _targetRfid;
            set 
            { 
                if (_targetRfid != value)
                {
                    _targetRfid = value; 
                    OnPropertyChanged(nameof(TargetRfid)); 
                    AllowedRfid = value; 
                }
            }
        }

        public Visibility IsSpecificRfidVisible => RfidOpMode == RfidOperationMode.Specific ? Visibility.Visible : Visibility.Collapsed;

        public ExtendedStationViewModel()
        {
            // React to mode changes (existing behavior) and to RFID/robot presence changes
            this.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Mode))
                {
                    OnPropertyChanged(nameof(IsAutoMode)); OnPropertyChanged(nameof(IsManualMode));
                    OnPropertyChanged(nameof(AutoBtnBg)); OnPropertyChanged(nameof(AutoBtnFg));
                    OnPropertyChanged(nameof(ManualBtnBg)); OnPropertyChanged(nameof(ManualBtnFg));
                }

                // When RFID, target or robot presence changes, try to update the global current klima index
                if (e.PropertyName == nameof(CurrentRfid) || e.PropertyName == nameof(TargetRfid) || e.PropertyName == nameof(RfidOpMode) || e.PropertyName == nameof(IsRobotPresent))
                {
                    TryUpdateAktuelKlimaIndex();
                }
            };
        }

        // Dışarıdan (örn. Page_Loaded) zorla tetiklemek için
        public void RefreshAktuelRfid() => TryUpdateAktuelKlimaIndex();

        // Update GlobalData AKTUEL_KLIMA_INDEX + AKTUEL_RFID based on station mode and RFID values
        private void TryUpdateAktuelKlimaIndex()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AKTUEL_RFID] TryUpdate çağrıldı: Station={Name}, IsRobotPresent={IsRobotPresent}, Mode={RfidOpMode}, TargetRfid={TargetRfid ?? "null"}, CurrentRfid={CurrentRfid ?? "null"}");

                // Sadece slider bu istasyonun önündeyse çalış
                if (!IsRobotPresent)
                {
                    System.Diagnostics.Debug.WriteLine($"[AKTUEL_RFID] {Name}: IsRobotPresent=false, atlanıyor");
                    return;
                }

                // Aktüel RFID değerini belirle: SPECIFIC → TargetRfid, MIX → CurrentRfid
                string aktuelRfid = null;

                if (RfidOpMode.Equals(RfidOperationMode.Specific))
                {
                    if (!string.IsNullOrEmpty(TargetRfid))
                        aktuelRfid = TargetRfid;
                    System.Diagnostics.Debug.WriteLine($"[AKTUEL_RFID] SPECIFIC mod → TargetRfid kullanılıyor: '{aktuelRfid ?? "BOŞ"}'");
                }
                else // Mixed
                {
                    if (!string.IsNullOrEmpty(CurrentRfid))
                        aktuelRfid = CurrentRfid;
                    System.Diagnostics.Debug.WriteLine($"[AKTUEL_RFID] MIX mod → CurrentRfid kullanılıyor: '{aktuelRfid ?? "BOŞ"}'");
                }

                // 1. AKTUEL_RFID → String RFID Id değeri
                var rfidVar = GlobalData.GeneralOutputVars.FirstOrDefault(v => v.Name == "AKTUEL_RFID");
                if (rfidVar != null)
                {
                    rfidVar.CurrentValue = aktuelRfid ?? "";
                    System.Diagnostics.Debug.WriteLine($"[AKTUEL_RFID] ✅ AKTUEL_RFID değeri yazıldı: '{aktuelRfid ?? ""}'");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AKTUEL_RFID] ❌ AKTUEL_RFID değişkeni GeneralOutputVars'ta bulunamadı!");
                }

                // 2. AKTUEL_KLIMA_INDEX → KnownRfids listesindeki sıra numarası (1-based)
                var indexVar = GlobalData.GeneralOutputVars.FirstOrDefault(v => v.Name == "AKTUEL_KLIMA_INDEX");
                if (indexVar != null)
                {
                    if (!string.IsNullOrEmpty(aktuelRfid))
                    {
                        var knownList = GlobalData.KnownRfids.ToList();
                        var k = knownList.FindIndex(r => r.Id == aktuelRfid);
                        indexVar.CurrentValue = (k >= 0) ? k + 1 : 0;
                        System.Diagnostics.Debug.WriteLine($"[AKTUEL_RFID] ✅ AKTUEL_KLIMA_INDEX={((k >= 0) ? k + 1 : 0)} (KnownRfids count={knownList.Count}, found at={k})");
                        if (k < 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[AKTUEL_RFID] ⚠️ RFID '{aktuelRfid}' KnownRfids'te yok! Mevcut Id'ler: [{string.Join(", ", knownList.Select(r => r.Id))}]");
                        }
                    }
                    else
                    {
                        indexVar.CurrentValue = 0;
                        System.Diagnostics.Debug.WriteLine($"[AKTUEL_RFID] AKTUEL_KLIMA_INDEX=0 (RFID boş)");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AKTUEL_RFID] ❌ AKTUEL_KLIMA_INDEX değişkeni GeneralOutputVars'ta bulunamadı!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AKTUEL_RFID] HATA: {ex.Message}");
            }
        }


        // StationModels.cs dosyasının en altına (namespace parantezinin içine) ekleyin:

       

        public bool IsAutoMode { get => Mode == StationMode.Auto; set => Mode = value ? StationMode.Auto : StationMode.Manual; }
        public bool IsManualMode { get => Mode != StationMode.Auto; set { if (value) Mode = StationMode.Manual; else Mode = StationMode.Auto; } }

        public SolidColorBrush AutoBtnBg => Mode == StationMode.Auto ? new SolidColorBrush(Color.FromArgb(255, 46, 204, 113)) : new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        public SolidColorBrush AutoBtnFg => Mode == StationMode.Auto ? new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
        public SolidColorBrush ManualBtnBg => Mode == StationMode.Manual ? new SolidColorBrush(Color.FromArgb(255, 255, 165, 0)) : new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        public SolidColorBrush ManualBtnFg => Mode == StationMode.Manual ? new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
    }

    public class CountToVisibilityConverter : Microsoft.UI.Xaml.Data.IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, string language)
        {
            int count = (value is int i) ? i : 0;
            bool isInverse = (parameter as string) == "Inverse";

            // Eğer Inverse (Ters) istenmişse: Sayı varsa GİZLE (Collapsed), yoksa GÖSTER (Visible)
            if (isInverse)
                return count > 0 ? Visibility.Collapsed : Visibility.Visible;

            // Normal: Sayı varsa GÖSTER, yoksa GİZLE
            return count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, string language)
            => throw new System.NotImplementedException();
    }

}