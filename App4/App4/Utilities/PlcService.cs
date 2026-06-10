using HslCommunication;
using HslCommunication.Profinet.Melsec;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.System; // DispatcherQueue için
using System.IO;
using System.Text.Json;



namespace App4.Utilities
{
    // ==========================================
    // 1. GÜÇLENDİRİLMİŞ VERİ MODELİ
    // ==========================================
    public class PlcVariable : INotifyPropertyChanged
    {
        private string _name;
        [JsonPropertyName("name")]
        public string Name
        {
            get => _name;
            set
            {
                var trimmed = value?.Trim();
                if (_name != trimmed)
                {
                    _name = trimmed;
                    _cachedAddress = null; // Clear cache
                    OnPropertyChanged();
                }
            }
        }

        private string _cachedAddress;
        [JsonIgnore]
        public string Address
        {
            get
            {
                if (_cachedAddress == null)
                {
                    // Öncelik: PlcTag > Description (PLC adresi) > Name
                    if (!string.IsNullOrEmpty(PlcTag)) _cachedAddress = PlcTag;
                    else if (!string.IsNullOrEmpty(Description)) _cachedAddress = Description;
                    else _cachedAddress = Name?.Split('-')[0].Trim();
                }
                return _cachedAddress;
            }
        }

        private string _type = "WORD";
        // Desteklenen Tipler: "BOOL", "INT", "WORD", "DINT", "DWORD", "REAL"
        [JsonPropertyName("type")]
        public string Type
        {
            get => _type;
            set { if (_type != value) { _type = value; OnPropertyChanged(); } }
        }

        private string _direction = "Output";
        [JsonPropertyName("direction")]
        public string Direction
        {
            get => _direction;
            set { if (_direction != value) { _direction = value; OnPropertyChanged(); } }
        }

        // CSV import kaynak dosya adı (gruplandırma için)
        public string SourceFile { get; set; }

        private string _description;
        public string Description
        {
            get => _description;
            set { _description = value; _cachedAddress = null; OnPropertyChanged(); }
        }
        public bool IsEditable { get; set; } = true;

        [JsonIgnore]
        public bool IsReadOnly => !IsEditable;

        private string _plcTag;
        public string PlcTag
        {
            get => _plcTag;
            set { if (_plcTag != value) { _plcTag = value; _cachedAddress = null; OnPropertyChanged(); } }
        }

        // İkinci PLC Tag eşleştirmesi (aynı değeri iki farklı tag'e yazmak için)
        private string _plcTag2;
        public string PlcTag2
        {
            get => _plcTag2;
            set { if (_plcTag2 != value) { _plcTag2 = value; OnPropertyChanged(); } }
        }

        [JsonIgnore]
        public string Value
        {
            get => CurrentValue?.ToString();
            set => CurrentValue = value;
        }

        private object _currentValue;
        [JsonPropertyName("currentValue")]
        public object CurrentValue
        {
            get => _currentValue;
            set
            {
                // Değer değişimi kontrolü (Basit nesne karşılaştırması)
                if (_currentValue?.ToString() != value?.ToString())
                {
                    _currentValue = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        [JsonPropertyName("minValue")]
        public object MinValue { get; set; }

        [JsonPropertyName("maxValue")]
        public object MaxValue { get; set; }

        // UI etiketinde Name yanında gösterilecek ek not (örn. "(1000ms)").
        // Tag eşleştirme/lookup hep Name ile yapılır; bu sadece görseldir, JSON'a yazılmaz.
        private string _labelNote;
        [JsonIgnore]
        public string LabelNote
        {
            get => _labelNote;
            set { if (_labelNote != value) { _labelNote = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); } }
        }

        /// <summary>Tablolarda gösterilecek etiket: Name + (varsa) LabelNote.</summary>
        [JsonIgnore]
        public string DisplayName => string.IsNullOrEmpty(LabelNote) ? Name : $"{Name} {LabelNote}";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Tag listesi dolduktan sonra ComboBox x:Bind'larını zorla güncellemek için.
        /// AvailableOutputPlcTags boşken x:Bind SelectedItem eşleştiremez,
        /// bu method PropertyChanged tetikleyerek ComboBox'ı yeniden değerlendirmeye zorlar.
        /// </summary>
        public void NotifyPlcTagChanged()
        {
            OnPropertyChanged(nameof(PlcTag));
            OnPropertyChanged(nameof(PlcTag2));
        }
    }

    // ==========================================
    // 2. EVRENSEL PLC SERVİSİ
    // ==========================================
    public class PlcService
    {
        private static PlcService _instance;
        public static PlcService Instance => _instance ??= new PlcService();

        private MelsecMcNet _melsecNet;
        public bool IsConnected { get; private set; } = false;

        // ═══ Kalıcı bağlantı ayarları (PLC_Config.json içinde saklanır) ═══
        // Başka bir bilgisayara kurulduğunda varsayılan değerlerle başlar,
        // kullanıcı değiştirip Connect'e bastığında dosyaya yazılır.
        public string PlcIpAddress { get; set; } = "192.168.251.100";
        public int PlcPort { get; set; } = 5007;

        public ObservableCollection<PlcVariable> InputVariables { get; private set; } = new();
        public ObservableCollection<PlcVariable> OutputVariables { get; private set; } = new();

        private System.Threading.Timer _refreshTimer;
        private DispatcherQueue _dispatcherQueue;
        public Action<Action> UiRunner { get; private set; }

        // ═══════════════════════════════════════════════════════════════════════
        // HEARTBEAT + BAĞLANTI İZLEME SİSTEMİ
        // ═══════════════════════════════════════════════════════════════════════
        // PLC→App: PLC bir WORD register'ı her scan cycle'da artırır. App okur.
        //          Değer değişmiyorsa PLC donmuş veya yeniden başlamış demektir.
        // App→PLC: App bir WORD register'ı her okuma cycle'ında artırır. PLC okur.
        //          Değer değişmiyorsa App kapanmış demektir.
        // Ardışık okuma hatası: TCP okuma başarısız → bağlantı kopmuş.
        // ═══════════════════════════════════════════════════════════════════════

        private int _consecutiveFailCount = 0;
        private const int FAIL_THRESHOLD = 5;              // 5 × ReadInterval = ~250ms → bağlantı koptu

        private ushort _lastPlcHeartbeat = 0;
        private int _heartbeatStaleCount = 0;
        private const int HEARTBEAT_STALE_THRESHOLD = 60;  // 60 × 50ms = 3sn → PLC donmuş/yeniden başlamış
        private bool _heartbeatFirstRead = true;

        private ushort _appHeartbeatCounter = 0;

        private System.Threading.Timer _reconnectTimer;
        private bool _isReconnecting = false;
        private bool _manualDisconnect = false;

        /// <summary>PLC→App heartbeat register adresi (ör: "D9999"). PLC bu register'ı sürekli artırır.</summary>
        public string HeartbeatReadTag { get; set; }

        /// <summary>App→PLC heartbeat register adresi (ör: "D9998"). App bu register'ı sürekli artırır.</summary>
        public string HeartbeatWriteTag { get; set; }

        /// <summary>Son kopukluk nedeni (UI'da gösterilir)</summary>
        public string LastDisconnectReason { get; private set; }

        /// <summary>PLC bağlantısı koptuğunda tetiklenir. Parametre: kopukluk nedeni.</summary>
        public event Action<string> OnConnectionLost;

        /// <summary>PLC otomatik yeniden bağlandığında tetiklenir.</summary>
        public event Action OnConnectionRestored;

        private readonly string _configFilePath = Path.Combine(
    GlobalData.ConfigBaseDir, "PLC_Config.json");



        private PlcService()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            // Klasör yoksa oluştur
            var directory = Path.GetDirectoryName(_configFilePath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            // Kayıtlı verileri yükle
            LoadVariables();

            // Değişiklikleri İzle
            StartMonitoring();

            // NOT: EnsureRobotBridgeVariables() artık çağrılmıyor.
            // PLC değişkenleri CSV import ile yüklenir (PLC sayfası → "CSV'den Yükle" butonu).
            // Eski hardcoded değişken listesi fabrika entegrasyonunda kullanılmayacak.
        }

        /// <summary>
        /// Robot-PLC köprü haberleşmesi için gerekli PLC değişkenlerini ekler (yoksa).
        /// Mevcut olanları dokunmaz, sadece eksikleri ekler.
        /// </summary>
        public void EnsureRobotBridgeVariables()
        {
            void EnsureInput(string name, string type)
            {
                if (!InputVariables.Any(v => v.Name == name))
                    InputVariables.Add(new PlcVariable { Name = name, Type = type, Direction = "Input" });
            }
            void EnsureOutput(string name, string type)
            {
                if (!OutputVariables.Any(v => v.Name == name))
                    OutputVariables.Add(new PlcVariable { Name = name, Type = type, Direction = "Output" });
            }

            // ═══════════════════════════════════════════════════
            // PLC → PC (Input) — PLC'den okunan sinyaller
            // ═══════════════════════════════════════════════════
            EnsureInput("SAFETY_OK", "BOOL");
            EnsureInput("LINE_AUTO_MODE", "BOOL");
            EnsureInput("FIRST_ROBOT_GO", "BOOL");              // PLC Robot 1 başlat
            EnsureInput("SECOND_ROBOT_GO", "BOOL");             // PLC Robot 2 başlat

            // ═══════════════════════════════════════════════════
            // PC → PLC (Output) — PLC'ye yazılan sinyaller
            // ═══════════════════════════════════════════════════
            EnsureOutput("CMD_LINE_START", "BOOL");
            EnsureOutput("CMD_LINE_STOP", "BOOL");
            EnsureOutput("CMD_LINE_RESET", "BOOL");

            // --- Aktüel Klima / RFID Bilgileri (PC → PLC) ---
            EnsureOutput("AKTUEL_KLIMA_INDEX", "WORD");         // Aktüel klima tipi index (1-based)
            EnsureOutput("AKTUEL_RFID", "STRING");              // Aktüel RFID Id string değeri

            // --- Robot 1 Durum Bilgileri (PC \u2192 PLC) ---
            EnsureOutput("RB1_ROBOT_DURUM", "INT");             // 0=Bo\u015fta 1=\u00c7al\u0131\u015f\u0131yor 2=Hata
            EnsureOutput("RB1_IS_BITTI", "BOOL");               // Robot 1 i\u015f tamamland\u0131
            EnsureOutput("RB1_HATA_VAR", "BOOL");               // Robot 1 hata var
            EnsureOutput("RB1_HATA_KODU", "INT");               // Robot 1 hata kodu
            EnsureOutput("RB1_HOME_OK", "BOOL");                // Robot 1 home pozisyonunda
            EnsureOutput("RB1_AKTIF_NOKTA", "INT");             // Robot 1 aktif \u00f6l\u00e7\u00fcm noktas\u0131
            EnsureOutput("RB1_DURUM_MESAJ", "INT");             // Robot 1 durum mesaj kodu
            EnsureOutput("RB1_NOK_SAYISI", "INT");              // Robot 1 NOK say\u0131s\u0131
            EnsureOutput("RB1_TOPLAM_NOKTA", "INT");            // Robot 1 toplam nokta
            EnsureOutput("RB1_NOK_BILDIRIM", "BOOL");           // Robot 1 yeni NOK bildirimi
            EnsureOutput("RB1_NOK_NOKTA", "INT");               // Robot 1 son NOK nokta no

            // --- Robot 2 Durum Bilgileri (PC \u2192 PLC) ---
            EnsureOutput("RB2_ROBOT_DURUM", "INT");             // 0=Bo\u015fta 1=\u00c7al\u0131\u015f\u0131yor 2=Hata
            EnsureOutput("RB2_IS_BITTI", "BOOL");               // Robot 2 i\u015f tamamland\u0131
            EnsureOutput("RB2_HATA_VAR", "BOOL");               // Robot 2 hata var
            EnsureOutput("RB2_HATA_KODU", "INT");               // Robot 2 hata kodu
            EnsureOutput("RB2_HOME_OK", "BOOL");                // Robot 2 home pozisyonunda
            EnsureOutput("RB2_AKTIF_CIZGI", "INT");             // Robot 2 aktif sniffer \u00e7izgi
            EnsureOutput("RB2_DURUM_MESAJ", "INT");             // Robot 2 durum mesaj kodu
            EnsureOutput("RB2_NOK_SAYISI", "INT");              // Robot 2 NOK say\u0131s\u0131
            EnsureOutput("RB2_TOPLAM_CIZGI", "INT");            // Robot 2 toplam \u00e7izgi
            EnsureOutput("RB2_NOK_BILDIRIM", "BOOL");           // Robot 2 yeni NOK bildirimi
            EnsureOutput("RB2_NOK_CIZGI", "INT");               // Robot 2 son NOK \u00e7izgi no

            // --- Robot 2 Slider Bilgileri (PC \u2192 PLC) ---
            EnsureOutput("RB2_SLIDER_TAMAM", "BOOL");            // Robot 2 slider hedefe ula\u015ft\u0131
            EnsureOutput("RB2_SLIDER_HOME", "BOOL");             // Robot 2 slider home pozisyonunda
            EnsureOutput("RB2_SLIDER_POZ", "REAL");              // Robot 2 slider akt\u00fcel pozisyon (mm)

            // --- Slider Hedef Pozisyon (PC \u2192 Robot 2 k\u00f6pr\u00fcs\u00fc i\u00e7in) ---
            // KL100_HEDEF_POZ kaldırıldı - slider pozisyonu doğrudan Robot 2'ye yazılıyor (G_SLIDER_HEDEF_POZ) \u2192 Robot 2'ye bridge
            EnsureOutput("KL100_HEDEF_ISTASYON", "WORD");          // Hedef istasyon no (1-4) \u2192 Robot 2'ye bridge
        }

        private void StartMonitoring()
        {
            void OnCollectionChanged() => _readListDirty = true;
            void OnItemChanged(object s, PropertyChangedEventArgs e) 
            { 
                if (e.PropertyName != "CurrentValue" && e.PropertyName != "Value") SaveVariables(); 
            }

            InputVariables.CollectionChanged += (s, e) =>
            {
                OnCollectionChanged();
                if (e.NewItems != null) foreach (PlcVariable i in e.NewItems) i.PropertyChanged += OnItemChanged;
                if (e.OldItems != null) foreach (PlcVariable i in e.OldItems) i.PropertyChanged -= OnItemChanged;
                SaveVariables();
            };

            OutputVariables.CollectionChanged += (s, e) =>
            {
                OnCollectionChanged();
                if (e.NewItems != null) foreach (PlcVariable i in e.NewItems) i.PropertyChanged += OnItemChanged;
                if (e.OldItems != null) foreach (PlcVariable i in e.OldItems) i.PropertyChanged -= OnItemChanged;
                SaveVariables();
            };

            foreach (var item in InputVariables) item.PropertyChanged += OnItemChanged;
            foreach (var item in OutputVariables) item.PropertyChanged += OnItemChanged;
            
            // Initial build
            _readListDirty = true;
        }

        // DEPRECATED: Old handler removed in favor of lambda
        // private void OnVariableChanged(object sender, PropertyChangedEventArgs e) { ... }

        public void Initialize(Action<Action> uiRunner) => UiRunner = uiRunner;

        public async Task<bool> ConnectAsync(string ip, int port)
        {
            try
            {
                if (IsConnected) return true;

                // Otomatik yeniden bağlanma timer'ını durdur (varsa)
                _reconnectTimer?.Dispose();
                _reconnectTimer = null;
                _isReconnecting = false;

                // ═══ Bağlantı bilgilerini HEMEN sakla (auto-reconnect için) ═══
                // İlk bağlantı başarısız olsa bile ip/port saklı kalmalı ki
                // StartAutoReconnect() doğru adrese yeniden bağlanmayı deneyebilsin.
                PlcIpAddress = ip;
                PlcPort = port;

                // Mitsubishi MC Protokolü (Binary)
                _melsecNet = new MelsecMcNet(ip, port);
                var result = await _melsecNet.ConnectServerAsync();

                if (result.IsSuccess)
                {
                    IsConnected = true;
                    GlobalData.PlcConnected = true;
                    _manualDisconnect = false;

                    // ═══ Heartbeat sayaçlarını sıfırla ═══
                    _consecutiveFailCount = 0;
                    _heartbeatStaleCount = 0;
                    _heartbeatFirstRead = true;
                    _appHeartbeatCounter = 0;
                    LastDisconnectReason = null;

                    // ═══ GÜVENLİ BAŞLANGIÇ: Output değişkenlerini PLC'ye yaz ═══
                    // PLC belleğinde eski TRUE değerleri kalabiliyor (STOP, RESET vb.)
                    // Timer başlamadan ÖNCE tüm output BOOL'ları FALSE'a yazılır.
                    // Bu sayede CMD_LINE_STOP, CMD_LINE_RESET gibi sinyaller
                    // eski değerleriyle robota aktarılmaz.
                    await InitializeOutputsAsync();

                    // Okuma hızı GlobalData'dan alınır (default 50ms, Limiter ile güvenli hız)
                    _refreshTimer = new System.Threading.Timer(TimerCallback, null, 0, GlobalData.Plc_ReadInterval);

                    System.Diagnostics.Debug.WriteLine($"[PLC_HB] Bağlantı kuruldu: {ip}:{port} | HB_Read={HeartbeatReadTag ?? "(yok)"} HB_Write={HeartbeatWriteTag ?? "(yok)"}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("PLC Bağlantı Hatası: " + ex.Message);
            }
            return false;
        }

        /// <summary>
        /// PLC bağlantısı kurulduktan sonra tüm Output değişkenlerinin
        /// güvenli başlangıç değerlerini PLC'ye yazar.
        /// BOOL → false, INT/WORD → 0, REAL → 0.0
        /// Bu sayede PLC belleğinde kalan eski değerler (STOP=TRUE vb.) temizlenir.
        /// </summary>
        private async Task InitializeOutputsAsync()
        {
            if (_melsecNet == null) return;

            foreach (var outVar in OutputVariables)
            {
                try
                {
                    string address = outVar.Address;
                    if (string.IsNullOrEmpty(address)) continue;

                    string type = (outVar.Type ?? "BOOL").ToUpper();
                    switch (type)
                    {
                        case "BOOL":
                            await _melsecNet.WriteAsync(address, false);
                            outVar.CurrentValue = 0;
                            break;
                        case "INT":
                        case "WORD":
                            await _melsecNet.WriteAsync(address, (short)0);
                            outVar.CurrentValue = 0;
                            break;
                        case "DINT":
                        case "DWORD":
                            await _melsecNet.WriteAsync(address, 0);
                            outVar.CurrentValue = 0;
                            break;
                        case "REAL":
                        case "FLOAT":
                            await _melsecNet.WriteAsync(address, 0.0f);
                            outVar.CurrentValue = 0.0f;
                            break;
                    }
                    System.Diagnostics.Debug.WriteLine($"[PLC_INIT] {outVar.Name} ({address}) = 0/false");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PLC_INIT] {outVar.Name} yazma hatası: {ex.Message}");
                }
            }
        }

        public void Disconnect()
        {
            _manualDisconnect = true;  // Otomatik yeniden bağlanmayı engelle
            _refreshTimer?.Dispose();
            _refreshTimer = null;
            _reconnectTimer?.Dispose();
            _reconnectTimer = null;
            _melsecNet?.ConnectClose();
            IsConnected = false;
            GlobalData.PlcConnected = false;
            _consecutiveFailCount = 0;
            _heartbeatStaleCount = 0;
            _heartbeatFirstRead = true;
            System.Diagnostics.Debug.WriteLine("[PLC_HB] Manuel bağlantı kesme.");
        }

        // ═══════════════════════════════════════════════════════════
        // BLOK OKUMA SİSTEMİ
        // Ardışık adresleri gruplar → tek TCP isteğiyle okur → değişkenlere dağıtır
        // 417 ayrı istek → ~25 blok okuma (15-20x hız artışı)
        // ═══════════════════════════════════════════════════════════

        private bool _isScanning = false;
        private List<PlcVariable> _cachedReadList;
        private bool _readListDirty = true;
        private List<ReadGroup> _cachedReadGroups;

        // Bir blok okuma grubunu temsil eder
        private class ReadGroup
        {
            public string BaseAddress;     // Blok başlangıç adresi (örn: "W900", "D10100")
            public ushort WordCount;       // Okunacak WORD sayısı
            public string GroupType;       // "BOOL_WORD", "WORD", "REAL"
            public List<(PlcVariable Var, int BitIndex)> BoolMembers;  // BOOL: bit pozisyonları
            public List<(PlcVariable Var, int WordOffset, string Type)> WordMembers; // WORD/INT/REAL: offset
            public PlcVariable SingleVar;  // Tek başına okunan değişken (gruplanamayan)
        }

        private void UpdateReadList()
        {
            _cachedReadList = InputVariables.Concat(OutputVariables).ToList();
            _cachedReadGroups = BuildReadGroups(_cachedReadList);
            _readListDirty = false;
            System.Diagnostics.Debug.WriteLine($"[PLC_BLOCK] {_cachedReadList.Count} değişken → {_cachedReadGroups.Count} blok okuma grubu");
        }

        /// <summary>
        /// Değişkenleri adres analizi yaparak blok okuma gruplarına ayırır.
        /// Aynı base word'deki BOOL'lar tek WORD olarak okunur.
        /// Ardışık WORD/REAL'ler blok halinde okunur.
        /// </summary>
        private List<ReadGroup> BuildReadGroups(List<PlcVariable> allVars)
        {
            var groups = new List<ReadGroup>();
            var boolsByBase = new Dictionary<string, List<(PlcVariable var, int bitIdx)>>();
            var wordVars = new List<(PlcVariable var, string prefix, int num)>();
            var realVars = new List<(PlcVariable var, string prefix, int num)>();
            var singleVars = new List<PlcVariable>(); // Gruplanamayan

            foreach (var v in allVars)
            {
                string addr = v.Address;
                if (string.IsNullOrEmpty(addr)) continue;
                string type = v.Type?.ToUpper() ?? "";

                if (type == "BOOL" && addr.Contains('.'))
                {
                    // BOOL bit adresi: W900.0, D10000.5 vb.
                    int dotIdx = addr.LastIndexOf('.');
                    string baseAddr = addr.Substring(0, dotIdx);
                    string bitStr = addr.Substring(dotIdx + 1);
                    int bitIndex = ParseHexBit(bitStr);
                    if (bitIndex >= 0)
                    {
                        if (!boolsByBase.ContainsKey(baseAddr))
                            boolsByBase[baseAddr] = new List<(PlcVariable, int)>();
                        boolsByBase[baseAddr].Add((v, bitIndex));
                        continue;
                    }
                }

                if ((type == "REAL" || type == "FLOAT") && !addr.Contains('.'))
                {
                    // REAL: 2 WORD, adres numarası
                    var (prefix, num) = ParseAddress(addr);
                    if (prefix != null) { realVars.Add((v, prefix, num)); continue; }
                }

                if ((type == "WORD" || type == "INT" || type == "DINT" || type == "DWORD") && !addr.Contains('.'))
                {
                    var (prefix, num) = ParseAddress(addr);
                    if (prefix != null) { wordVars.Add((v, prefix, num)); continue; }
                }

                // Gruplanamayan (STRING, adressiz, vb.)
                singleVars.Add(v);
            }

            // 1. BOOL grupları: her base word → 1 WORD okuma → bit dağıtımı
            foreach (var kvp in boolsByBase)
            {
                groups.Add(new ReadGroup
                {
                    BaseAddress = kvp.Key,
                    WordCount = 1,
                    GroupType = "BOOL_WORD",
                    BoolMembers = kvp.Value,
                    WordMembers = new()
                });
            }

            // 2. WORD/INT blokları: aynı prefix, ardışık adresler
            var wordByPrefix = wordVars.GroupBy(w => w.prefix);
            foreach (var pg in wordByPrefix)
            {
                var sorted = pg.OrderBy(x => x.num).ToList();
                BuildWordBlocks(sorted, groups, 1); // WORD = 1 word
            }

            // 3. REAL blokları: aynı prefix, ardışık adresler (her REAL = 2 word, adres farkı 2)
            var realByPrefix = realVars.GroupBy(r => r.prefix);
            foreach (var pg in realByPrefix)
            {
                var sorted = pg.OrderBy(x => x.num).ToList();
                BuildRealBlocks(sorted, groups);
            }

            // 4. Tekil değişkenler (gruplanamayan)
            foreach (var v in singleVars)
            {
                if (!string.IsNullOrEmpty(v.Address))
                {
                    groups.Add(new ReadGroup
                    {
                        GroupType = "SINGLE",
                        SingleVar = v
                    });
                }
            }

            return groups;
        }

        private void BuildWordBlocks(List<(PlcVariable var, string prefix, int num)> sorted, List<ReadGroup> groups, int stride)
        {
            int i = 0;
            while (i < sorted.Count)
            {
                var block = new List<(PlcVariable var, string prefix, int num)> { sorted[i] };
                // Ardışık olanları topla (max boşluk = 4 word — küçük boşluklar blokla kapansın)
                while (i + 1 < sorted.Count && sorted[i + 1].num - sorted[i].num <= 4)
                {
                    i++;
                    block.Add(sorted[i]);
                }

                if (block.Count == 1)
                {
                    // Tekil WORD/INT
                    var v = block[0];
                    groups.Add(new ReadGroup
                    {
                        GroupType = "SINGLE",
                        SingleVar = v.var
                    });
                }
                else
                {
                    // Blok okuma
                    int startNum = block[0].num;
                    int endNum = block[block.Count - 1].num;
                    ushort wordCount = (ushort)(endNum - startNum + stride);
                    string baseAddr = $"{block[0].prefix}{startNum}";

                    var members = new List<(PlcVariable Var, int WordOffset, string Type)>();
                    foreach (var b in block)
                    {
                        int offset = b.num - startNum;
                        members.Add((b.var, offset, b.var.Type.ToUpper()));
                    }

                    groups.Add(new ReadGroup
                    {
                        BaseAddress = baseAddr,
                        WordCount = wordCount,
                        GroupType = "WORD_BLOCK",
                        BoolMembers = new(),
                        WordMembers = members
                    });
                }
                i++;
            }
        }

        private void BuildRealBlocks(List<(PlcVariable var, string prefix, int num)> sorted, List<ReadGroup> groups)
        {
            int i = 0;
            while (i < sorted.Count)
            {
                var block = new List<(PlcVariable var, string prefix, int num)> { sorted[i] };
                // REAL ardışık = adres farkı 2 (her REAL 2 word kaplar), max boşluk 4
                while (i + 1 < sorted.Count && sorted[i + 1].num - sorted[i].num <= 4)
                {
                    i++;
                    block.Add(sorted[i]);
                }

                if (block.Count == 1)
                {
                    groups.Add(new ReadGroup
                    {
                        GroupType = "SINGLE",
                        SingleVar = block[0].var
                    });
                }
                else
                {
                    int startNum = block[0].num;
                    int endNum = block[block.Count - 1].num;
                    ushort wordCount = (ushort)(endNum - startNum + 2); // +2 son REAL'in 2 word'ü

                    var members = new List<(PlcVariable Var, int WordOffset, string Type)>();
                    foreach (var b in block)
                        members.Add((b.var, b.num - startNum, "REAL"));

                    groups.Add(new ReadGroup
                    {
                        BaseAddress = $"{block[0].prefix}{startNum}",
                        WordCount = wordCount,
                        GroupType = "REAL_BLOCK",
                        BoolMembers = new(),
                        WordMembers = members
                    });
                }
                i++;
            }
        }

        private static (string prefix, int num) ParseAddress(string addr)
        {
            // "W900" → ("W", 900), "D10100" → ("D", 10100)
            if (string.IsNullOrEmpty(addr)) return (null, 0);
            int numStart = -1;
            for (int i = 0; i < addr.Length; i++)
            {
                if (char.IsDigit(addr[i])) { numStart = i; break; }
            }
            if (numStart <= 0) return (null, 0);
            string prefix = addr.Substring(0, numStart);
            if (int.TryParse(addr.Substring(numStart), out int num))
                return (prefix, num);
            return (null, 0);
        }

        private static int ParseHexBit(string bitStr)
        {
            // "0"-"9" → 0-9, "A"-"F" → 10-15
            if (string.IsNullOrEmpty(bitStr)) return -1;
            if (int.TryParse(bitStr, out int dec)) return dec;
            if (bitStr.Length == 1)
            {
                char c = char.ToUpper(bitStr[0]);
                if (c >= 'A' && c <= 'F') return 10 + (c - 'A');
            }
            return -1;
        }

        // ═══ ANA OKUMA DÖNGÜSÜ (BLOK TABANLI + HEARTBEAT) ═══
        private async void TimerCallback(object state)
        {
            if (!IsConnected || _melsecNet == null || _isScanning) return;

            _isScanning = true;
            try
            {
                // ══════════════════════════════════════════════════════
                // 1. BAĞLANTI PROBE — Heartbeat register veya ilk input
                //    Bu okuma başarısızsa TCP bağlantısı kopmuş demektir.
                // ══════════════════════════════════════════════════════
                bool probeOk = await ProbeConnection();

                if (!probeOk)
                {
                    _consecutiveFailCount++;
                    if (_consecutiveFailCount >= FAIL_THRESHOLD)
                    {
                        HandleConnectionLost("PLC iletişim hatası — ardışık okuma başarısız");
                    }
                    return;  // Bu cycle'ı atla — okuma anlamsız
                }

                // Probe başarılı → fail sayacını sıfırla
                _consecutiveFailCount = 0;

                // ══════════════════════════════════════════════════════
                // 2. NORMAL OKUMA DÖNGÜSÜ (mevcut blok okuma sistemi)
                // ══════════════════════════════════════════════════════
                if (_readListDirty || _cachedReadGroups == null) UpdateReadList();
                var readGroups = _cachedReadGroups;
                if (readGroups == null || readGroups.Count == 0) return;

                foreach (var group in readGroups)
                {
                    if (!IsConnected) break;

                    try
                    {
                        switch (group.GroupType)
                        {
                            case "BOOL_WORD":
                                await ReadBoolWordGroup(group);
                                break;
                            case "WORD_BLOCK":
                                await ReadWordBlock(group);
                                break;
                            case "REAL_BLOCK":
                                await ReadRealBlock(group);
                                break;
                            case "SINGLE":
                                await ReadSingleVar(group.SingleVar);
                                break;
                        }
                    }
                    catch { }
                }

                // ══════════════════════════════════════════════════════
                // 3. APP → PLC HEARTBEAT WRITE
                //    App'in canlı olduğunu PLC'ye bildirir.
                // ══════════════════════════════════════════════════════
                await WriteAppHeartbeat();
            }
            finally
            {
                _isScanning = false;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // HEARTBEAT YARDIMCI METOTLARI
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Bağlantı probe'u: Heartbeat register'ını okumayı dener.
        /// HeartbeatReadTag tanımlıysa → o register'ı okur + değer değişimi kontrol eder.
        /// Tanımlı değilse → ilk input değişkeninin adresini okur (salt bağlantı testi).
        /// Başarılıysa true, TCP hatası varsa false döner.
        /// </summary>
        private async Task<bool> ProbeConnection()
        {
            try
            {
                // ── 1. Heartbeat tag tanımlıysa → PLC canlılık kontrolü ──
                if (!string.IsNullOrEmpty(HeartbeatReadTag))
                {
                    var result = await _melsecNet.ReadUInt16Async(HeartbeatReadTag);
                    if (!result.IsSuccess) return false;

                    ushort newVal = result.Content;

                    if (_heartbeatFirstRead)
                    {
                        // İlk okuma — referans değerini kaydet
                        _lastPlcHeartbeat = newVal;
                        _heartbeatFirstRead = false;
                        _heartbeatStaleCount = 0;
                        return true;
                    }

                    if (newVal != _lastPlcHeartbeat)
                    {
                        // PLC canlı — counter değişiyor
                        _lastPlcHeartbeat = newVal;
                        _heartbeatStaleCount = 0;
                    }
                    else
                    {
                        // Değer aynı — PLC donmuş olabilir
                        _heartbeatStaleCount++;
                        if (_heartbeatStaleCount >= HEARTBEAT_STALE_THRESHOLD)
                        {
                            HandleConnectionLost("PLC heartbeat donmuş — PLC programı çalışmıyor olabilir");
                            return false;
                        }
                    }

                    return true;
                }

                // ── 2. Heartbeat yok → basit TCP bağlantı testi ──
                // İlk input değişkenini okumayı dene (sadece bağlantı kontrolü)
                var firstInput = InputVariables.FirstOrDefault();
                if (firstInput != null && !string.IsNullOrEmpty(firstInput.Address))
                {
                    var res = await _melsecNet.ReadInt16Async(firstInput.Address);
                    return res.IsSuccess;
                }

                // Hiç değişken yok → test yapılamıyor, OK varsay
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// App→PLC heartbeat: Her cycle'da artan bir counter'ı PLC'ye yazar.
        /// PLC bu değeri izleyerek App'in canlı olup olmadığını bilir.
        /// </summary>
        private async Task WriteAppHeartbeat()
        {
            if (string.IsNullOrEmpty(HeartbeatWriteTag) || _melsecNet == null) return;

            try
            {
                unchecked { _appHeartbeatCounter++; }
                await _melsecNet.WriteAsync(HeartbeatWriteTag, _appHeartbeatCounter);
            }
            catch
            {
                // Yazma hatası → probe zaten yakalayacak, burada sessizce devam
            }
        }

        /// <summary>
        /// Bağlantı kaybı algılandığında çağrılır:
        /// 1. Okuma timer'ını durdurur
        /// 2. Tüm input değerlerini sıfırlar (bayat veri temizliği)
        /// 3. Otomatik yeniden bağlanmayı başlatır
        /// </summary>
        private void HandleConnectionLost(string reason)
        {
            if (!IsConnected) return;  // Zaten kopuk

            System.Diagnostics.Debug.WriteLine($"[PLC_HB] *** BAĞLANTI KOPTU: {reason} ***");

            // Timer'ı durdur
            _refreshTimer?.Dispose();
            _refreshTimer = null;

            // Durumu güncelle
            IsConnected = false;
            GlobalData.PlcConnected = false;
            LastDisconnectReason = reason;

            // ═══ BAYAT VERİ TEMİZLİĞİ ═══
            // Tüm Input değişkenlerinin CurrentValue'sunu sıfırla.
            // Eski/donmuş RFID verileri dahil her şey temizlenir.
            // Timer tekrar başladığında taze veriler PLC'den okunur.
            InvalidateInputValues();

            // TCP bağlantısını kapat (temiz kapanış)
            try { _melsecNet?.ConnectClose(); } catch { }

            // Event tetikle (GlobalData ve UI dinleyicileri için)
            try { OnConnectionLost?.Invoke(reason); } catch { }

            // Otomatik yeniden bağlanmayı başlat (manuel disconnect değilse)
            if (!_manualDisconnect)
            {
                StartAutoReconnect();
            }
        }

        /// <summary>
        /// Tüm PLC Input değişkenlerinin CurrentValue'sunu sıfırlar.
        /// PLC bağlantısı koptuğunda bayat verilerin ekranda kalmasını engeller.
        /// RFID, status, sensor verileri dahil tüm input'lar etkilenir.
        /// </summary>
        private void InvalidateInputValues()
        {
            try
            {
                UiRunner?.Invoke(() =>
                {
                    foreach (var v in InputVariables)
                    {
                        if (v.CurrentValue != null)
                        {
                            v.CurrentValue = null;
                        }
                    }
                    System.Diagnostics.Debug.WriteLine($"[PLC_HB] {InputVariables.Count} input değişkeni sıfırlandı (bayat veri temizliği)");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PLC_HB] Input temizleme hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Her 3 saniyede bir PLC'ye yeniden bağlanmayı dener.
        /// Başarılı olursa timer'ı durdurup normal okuma döngüsünü yeniden başlatır.
        /// </summary>
        public void StartAutoReconnect()
        {
            if (_reconnectTimer != null || _manualDisconnect || IsConnected) return;
            _manualDisconnect = false; // Dışarıdan çağrıldığında reconnect'e izin ver

            System.Diagnostics.Debug.WriteLine($"[PLC_HB] Otomatik yeniden bağlanma başlatıldı ({PlcIpAddress}:{PlcPort} — her 3sn)");

            _reconnectTimer = new System.Threading.Timer(async (state) =>
            {
                if (_isReconnecting || IsConnected || _manualDisconnect) return;
                _isReconnecting = true;

                try
                {
                    System.Diagnostics.Debug.WriteLine("[PLC_HB] Yeniden bağlanma deneniyor...");

                    _melsecNet = new MelsecMcNet(PlcIpAddress, PlcPort);
                    var result = await _melsecNet.ConnectServerAsync();

                    if (result.IsSuccess)
                    {
                        // ═══ BAĞLANTI YENİDEN KURULDU ═══
                        _reconnectTimer?.Dispose();
                        _reconnectTimer = null;

                        IsConnected = true;
                        GlobalData.PlcConnected = true;
                        LastDisconnectReason = null;

                        // Sayaçları sıfırla
                        _consecutiveFailCount = 0;
                        _heartbeatStaleCount = 0;
                        _heartbeatFirstRead = true;
                        _appHeartbeatCounter = 0;

                        // Output'ları güvenli başlangıç değerleriyle yaz
                        await InitializeOutputsAsync();

                        // Okuma timer'ını yeniden başlat
                        _refreshTimer = new System.Threading.Timer(TimerCallback, null, 0, GlobalData.Plc_ReadInterval);

                        System.Diagnostics.Debug.WriteLine("[PLC_HB] ✓ PLC yeniden bağlandı!");

                        // Event tetikle
                        try { OnConnectionRestored?.Invoke(); } catch { }
                    }
                    else
                    {
                        try { _melsecNet?.ConnectClose(); } catch { }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PLC_HB] Yeniden bağlanma hatası: {ex.Message}");
                }
                finally
                {
                    _isReconnecting = false;
                }
            }, null, 1000, 3000);  // İlk deneme 1sn sonra, sonra her 3sn
        }

        /// <summary>1 WORD oku → 16 bit'e ayır → BOOL değişkenlere dağıt</summary>
        private async Task ReadBoolWordGroup(ReadGroup group)
        {
            var res = await _melsecNet.ReadUInt16Async(group.BaseAddress);
            if (!res.IsSuccess) return;

            ushort word = res.Content;
            foreach (var (variable, bitIndex) in group.BoolMembers)
            {
                int newVal = (word >> bitIndex) & 1;
                if (variable.CurrentValue?.ToString() != newVal.ToString())
                    UiRunner?.Invoke(() => variable.CurrentValue = newVal);
            }
        }

        /// <summary>Ardışık WORD/INT'leri blok halinde oku → offset'e göre dağıt</summary>
        private async Task ReadWordBlock(ReadGroup group)
        {
            var res = await _melsecNet.ReadAsync(group.BaseAddress, group.WordCount);
            if (!res.IsSuccess || res.Content == null) return;

            byte[] data = res.Content;
            foreach (var (variable, wordOffset, type) in group.WordMembers)
            {
                int byteOffset = wordOffset * 2;
                if (byteOffset + 1 >= data.Length) continue;

                object newValue = null;
                switch (type)
                {
                    case "WORD":
                        newValue = BitConverter.ToUInt16(data, byteOffset);
                        break;
                    case "INT":
                        newValue = BitConverter.ToInt16(data, byteOffset);
                        break;
                    case "DINT":
                        if (byteOffset + 3 < data.Length)
                            newValue = BitConverter.ToInt32(data, byteOffset);
                        break;
                    case "DWORD":
                        if (byteOffset + 3 < data.Length)
                            newValue = BitConverter.ToUInt32(data, byteOffset);
                        break;
                }

                if (newValue != null && variable.CurrentValue?.ToString() != newValue.ToString())
                    UiRunner?.Invoke(() => variable.CurrentValue = newValue);
            }
        }

        /// <summary>Ardışık REAL'leri blok halinde oku → 2 WORD'den float çıkar → dağıt</summary>
        private async Task ReadRealBlock(ReadGroup group)
        {
            var res = await _melsecNet.ReadAsync(group.BaseAddress, group.WordCount);
            if (!res.IsSuccess || res.Content == null) return;

            byte[] data = res.Content;
            foreach (var (variable, wordOffset, _) in group.WordMembers)
            {
                int byteOffset = wordOffset * 2;
                if (byteOffset + 3 >= data.Length) continue;

                float raw = BitConverter.ToSingle(data, byteOffset);
                object newValue = Math.Round(raw, 2);

                if (variable.CurrentValue?.ToString() != newValue.ToString())
                    UiRunner?.Invoke(() => variable.CurrentValue = newValue);
            }
        }

        /// <summary>Gruplanamayan tekil değişkeni oku (eski yöntem)</summary>
        private async Task ReadSingleVar(PlcVariable variable)
        {
            if (variable == null) return;
            string address = variable.Address;
            if (string.IsNullOrEmpty(address)) return;

            object newValue = null;
            string type = variable.Type?.ToUpper() ?? "";

            switch (type)
            {
                case "BOOL":
                    var bRes = await _melsecNet.ReadBoolAsync(address);
                    if (bRes.IsSuccess) newValue = bRes.Content ? 1 : 0;
                    break;
                case "INT":
                    var iRes = await _melsecNet.ReadInt16Async(address);
                    if (iRes.IsSuccess) newValue = iRes.Content;
                    break;
                case "WORD":
                    var wRes = await _melsecNet.ReadUInt16Async(address);
                    if (wRes.IsSuccess) newValue = wRes.Content;
                    break;
                case "DINT":
                    var diRes = await _melsecNet.ReadInt32Async(address);
                    if (diRes.IsSuccess) newValue = diRes.Content;
                    break;
                case "DWORD":
                    var dwRes = await _melsecNet.ReadUInt32Async(address);
                    if (dwRes.IsSuccess) newValue = dwRes.Content;
                    break;
                case "REAL":
                case "FLOAT":
                    var fRes = await _melsecNet.ReadFloatAsync(address);
                    if (fRes.IsSuccess) newValue = Math.Round(fRes.Content, 2);
                    break;
                case "STRING":
                    var sRes = await _melsecNet.ReadStringAsync(address, 10);
                    if (sRes.IsSuccess) newValue = sRes.Content?.Trim('\0', ' ');
                    break;
                default:
                    var defRes = await _melsecNet.ReadInt16Async(address);
                    if (defRes.IsSuccess) newValue = defRes.Content;
                    break;
            }

            if (newValue != null && variable.CurrentValue?.ToString() != newValue.ToString())
                UiRunner?.Invoke(() => variable.CurrentValue = newValue);
        }

        // --- GÜÇLENDİRİLMİŞ YAZMA METODU ---
        public async Task WriteAsync(PlcVariable variable, object value)
        {
            if (!IsConnected) return;
            // Adres çözümü: PlcTag > Description (PLC adresi) > Name. Önceden yalnızca Name kullanılıyordu;
            // bu yüzden adı "HEARTBEAT" olup adresi (M5178) Description'da olan degiskenler PLC'ye yazilamiyordu.
            string address = variable.Address;
            if (string.IsNullOrWhiteSpace(address)) { System.Diagnostics.Debug.WriteLine($"[PLC_WRITE] {variable.Name}: adres bos, yazma atlandi"); return; }
            string type = variable.Type.ToUpper();

            try
            {
                switch (type)
                {
                    case "BOOL":
                        await _melsecNet.WriteAsync(address, Convert.ToBoolean(value));
                        break;
                    case "INT":
                        await _melsecNet.WriteAsync(address, Convert.ToInt16(value));
                        break;
                    case "WORD":
                        await _melsecNet.WriteAsync(address, Convert.ToUInt16(value));
                        break;
                    case "DINT":
                        await _melsecNet.WriteAsync(address, Convert.ToInt32(value));
                        break;
                    case "DWORD":
                        await _melsecNet.WriteAsync(address, Convert.ToUInt32(value));
                        break;
                    case "REAL":
                    case "FLOAT":
                        await _melsecNet.WriteAsync(address, Convert.ToSingle(value));
                        break;
                    case "STRING":
                        await _melsecNet.WriteAsync(address, Convert.ToString(value));
                        break;
                    default:
                        await _melsecNet.WriteAsync(address, Convert.ToInt16(value));
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Yazma Hatası ({address}): {ex.Message}");
            }
        }

        public string LastLoadError { get; private set; }

        public void LoadVariables()
        {
            LastLoadError = null;
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    LastLoadError = $"Config dosyasi bulunamadi: {_configFilePath}";
                    System.Diagnostics.Debug.WriteLine($"[PLC_LOAD] {LastLoadError}");
                    return;
                }

                string json = File.ReadAllText(_configFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    LastLoadError = "Config dosyasi bos.";
                    System.Diagnostics.Debug.WriteLine($"[PLC_LOAD] {LastLoadError}");
                    return;
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<PlcConfigData>(json, options);

                if (data == null)
                {
                    LastLoadError = "Config dosyasi parse edilemedi.";
                    System.Diagnostics.Debug.WriteLine($"[PLC_LOAD] {LastLoadError}");
                    return;
                }

                // Bağlantı ayarları — boş ya da null değilse uygula
                if (!string.IsNullOrWhiteSpace(data.PlcIpAddress))
                    PlcIpAddress = data.PlcIpAddress.Trim();
                if (data.PlcPort.HasValue && data.PlcPort.Value > 0 && data.PlcPort.Value <= 65535)
                    PlcPort = data.PlcPort.Value;

                // Heartbeat tag adresleri
                HeartbeatReadTag = data.HeartbeatReadTag;
                HeartbeatWriteTag = data.HeartbeatWriteTag;

                InputVariables.Clear();
                if (data.Inputs != null)
                    foreach (var item in data.Inputs) InputVariables.Add(item);

                OutputVariables.Clear();
                if (data.Outputs != null)
                    foreach (var item in data.Outputs) OutputVariables.Add(item);

                _readListDirty = true;
                System.Diagnostics.Debug.WriteLine($"[PLC_LOAD] Basarili: IP={PlcIpAddress}:{PlcPort} — {InputVariables.Count} input, {OutputVariables.Count} output yuklendi ({_configFilePath})");
            }
            catch (Exception ex)
            {
                LastLoadError = $"Yukleme hatasi: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[PLC_LOAD] {LastLoadError}");
            }
        }

        public void SaveVariables()
        {
            try
            {
                // Klasör yoksa oluştur
                var dir = Path.GetDirectoryName(_configFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var data = new PlcConfigData
                {
                    PlcIpAddress = PlcIpAddress,
                    PlcPort = PlcPort,
                    HeartbeatReadTag = HeartbeatReadTag,
                    HeartbeatWriteTag = HeartbeatWriteTag,
                    Inputs = InputVariables.ToList(),
                    Outputs = OutputVariables.ToList()
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(data, options);

                File.WriteAllText(_configFilePath, json);
                System.Diagnostics.Debug.WriteLine($"[PLC_SAVE] {_configFilePath} → IP={data.PlcIpAddress}:{data.PlcPort}, {data.Inputs.Count} input, {data.Outputs.Count} output kaydedildi");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PLC_SAVE] HATA: {ex.Message}");
            }
        }

        // ═══ CSV'DEN PLC DEĞİŞKEN IMPORT ═══
        private static readonly HashSet<string> _standardTypes = new(StringComparer.OrdinalIgnoreCase)
            { "BOOL", "BIT", "INT", "WORD", "REAL", "LREAL", "DINT", "DWORD", "FLOAT" };

        private static readonly HashSet<string> _outputKeywords = new(StringComparer.OrdinalIgnoreCase)
            { "start", "cal", "cal_abort", "zero", "errClear", "stanby", "reset", "enable",
              "manStart", "manStop", "autoStart", "autoStop", "alarmReset", "lightOn",
              "manSetSpeed", "TASK_SAVE", "MODE_CMD", "RFIDMOD", "CONVEYOR_PERM",
              "JOB_TRIGGER", "SLIDER_SERVO_GO", "SLIDER_MOTOR_ON", "FIRST_ROBOT_GO", "SECOND_ROBOT_GO",
              "ROBOT_POS_GO", "MEASUREMENT_OK" };

        /// <summary>
        /// Seçilen CSV dosyalarını parse edip belirtilen yöne (Input/Output) ekler.
        /// Mevcut yöndeki değişkenleri siler, diğer yöndekilere dokunmaz.
        /// </summary>
        public int ImportCsvFilesToDirection(List<string> csvFilePaths, string direction)
        {
            return ImportCsvToDirectionInternal(csvFilePaths, direction);
        }

        /// <summary>
        /// CSV klasöründeki tüm CSV dosyalarını parse edip belirtilen yöne (Input/Output) ekler.
        /// </summary>
        public int ImportCsvToDirection(string folderPath, string direction)
        {
            var csvFiles = Directory.GetFiles(folderPath, "*.csv").ToList();
            return ImportCsvToDirectionInternal(csvFiles, direction);
        }

        private int ImportCsvToDirectionInternal(List<string> csvFiles, string direction)
        {
            var newVars = new List<PlcVariable>();

            foreach (var csvFile in csvFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(csvFile).ToLowerInvariant();
                if (fileName.Contains("m+global") || fileName.Contains("sdcard")) continue;

                try
                {
                    var bytes = File.ReadAllBytes(csvFile);
                    string content;
                    if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                        content = System.Text.Encoding.Unicode.GetString(bytes);
                    else
                        content = System.Text.Encoding.UTF8.GetString(bytes);

                    var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    for (int i = 2; i < lines.Length; i++)
                    {
                        var line = lines[i].Trim();
                        if (string.IsNullOrEmpty(line)) continue;

                        var cols = line.Split('\t');
                        if (cols.Length < 3) continue;

                        string labelName = cols[1].Trim('"', ' ');
                        string dataType = cols[2].Trim('"', ' ');
                        string assignDevice = cols.Length > 5 ? cols[5].Trim('"', ' ') : "";
                        // "Detailed Setting" gibi açıklayıcı metinleri filtrele
                        if (assignDevice.Contains("Setting", StringComparison.OrdinalIgnoreCase) ||
                            assignDevice.Contains("Detail", StringComparison.OrdinalIgnoreCase))
                            assignDevice = "";
                        string address = assignDevice;

                        if (string.IsNullOrEmpty(labelName)) continue;

                        string normalizedType = dataType;
                        if (dataType.StartsWith("STRING", StringComparison.OrdinalIgnoreCase))
                            normalizedType = "STRING";

                        if (!_standardTypes.Contains(normalizedType) && normalizedType != "STRING")
                            continue;

                        // Tip normalizasyonu
                        if (normalizedType.Equals("LREAL", StringComparison.OrdinalIgnoreCase))
                            normalizedType = "REAL";
                        if (normalizedType.Equals("BIT", StringComparison.OrdinalIgnoreCase))
                            normalizedType = "BOOL";

                        if (newVars.Any(v => v.Name == labelName)) continue;

                        newVars.Add(new PlcVariable
                        {
                            Name = labelName,
                            Type = normalizedType.ToUpperInvariant(),
                            Direction = direction,
                            Description = address ?? "",
                            SourceFile = Path.GetFileName(csvFile),
                            CurrentValue = null
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CSV_IMPORT] {csvFile} hata: {ex.Message}");
                }
            }

            // CSV dosya sırasını koru (önce dosya sırası, sonra CSV satır sırası)
            var sorted = newVars;

            // Mevcut config'i oku, sadece ilgili yönü güncelle
            PlcConfigData existingData = new PlcConfigData { Inputs = new(), Outputs = new() };
            try
            {
                if (File.Exists(_configFilePath))
                {
                    string existing = File.ReadAllText(_configFilePath);
                    existingData = JsonSerializer.Deserialize<PlcConfigData>(existing,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? existingData;
                }
            }
            catch { }

            // Mevcut listeye ekle (aynı isimde olanları atla)
            var existingList = direction == "Input" ? existingData.Inputs : existingData.Outputs;
            if (existingList == null) existingList = new List<PlcVariable>();
            var existingNames = new HashSet<string>(existingList.Select(v => v.Name));
            foreach (var v in sorted)
            {
                if (!existingNames.Contains(v.Name))
                    existingList.Add(v);
            }

            if (direction == "Input")
                existingData.Inputs = existingList;
            else
                existingData.Outputs = existingList;

            // Dosyaya kaydet
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(_configFilePath, JsonSerializer.Serialize(existingData, options));

            // UI koleksiyonunu da güncelle
            var finalList = direction == "Input" ? existingData.Inputs : existingData.Outputs;
            _dispatcherQueue?.TryEnqueue(() =>
            {
                var target = direction == "Input" ? InputVariables : OutputVariables;
                target.Clear();
                foreach (var v in finalList) target.Add(v);
                _readListDirty = true;
            });

            System.Diagnostics.Debug.WriteLine($"[CSV_IMPORT] {sorted.Count} degisken {direction} olarak yuklendi");
            return sorted.Count;
        }

        // Varsayılanlar (Eğer dosya yoksa bunlar gelir)
        private void LoadDefaultVariables()
        {
            // Config dosyası yoksa boş başla — PLC sayfasından "CSV'den Yükle" ile doldurulacak
            System.Diagnostics.Debug.WriteLine("[PLC] PLC_Config.json bulunamadi. CSV import ile degisken yukleyin.");
        }

        public async Task RunOnUiAsync(Action action)
        {
            if (UiRunner == null)
            {
                action(); // Fallback to current thread
                return;
            }

            var tcs = new TaskCompletionSource();
            UiRunner.Invoke(() =>
            {
                try
                {
                    action();
                    tcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });
            await tcs.Task;
        }

        // JSON Serileştirme için yardımcı sınıf (PlcService sınıfının dışında veya içinde olabilir)
        public class PlcConfigData
        {
            // Bağlantı ayarları — başka bilgisayara kurulduğunda kalıcı kalsın
            [JsonPropertyName("plcIpAddress")]
            public string PlcIpAddress { get; set; }

            [JsonPropertyName("plcPort")]
            public int? PlcPort { get; set; }

            // ═══ Heartbeat tag adresleri ═══
            // PLC→App: PLC'nin sürekli artırdığı WORD register (ör: "D9999")
            [JsonPropertyName("heartbeatReadTag")]
            public string HeartbeatReadTag { get; set; }

            // App→PLC: App'in sürekli artırdığı WORD register (ör: "D9998")
            [JsonPropertyName("heartbeatWriteTag")]
            public string HeartbeatWriteTag { get; set; }

            public List<PlcVariable> Inputs { get; set; } = new();
            public List<PlcVariable> Outputs { get; set; } = new();
        }






    }
}