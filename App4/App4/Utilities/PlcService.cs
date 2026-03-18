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
                    if (!string.IsNullOrEmpty(PlcTag)) _cachedAddress = PlcTag;
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

        public string Description { get; set; }
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

        public ObservableCollection<PlcVariable> InputVariables { get; private set; } = new();
        public ObservableCollection<PlcVariable> OutputVariables { get; private set; } = new();

        private System.Threading.Timer _refreshTimer;
        private DispatcherQueue _dispatcherQueue;
        public Action<Action> UiRunner { get; private set; }

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

                // Mitsubishi MC Protokolü (Binary)
                _melsecNet = new MelsecMcNet(ip, port);
                var result = await _melsecNet.ConnectServerAsync();

                if (result.IsSuccess)
                {
                    IsConnected = true;
                    GlobalData.PlcConnected = true;

                    // ═══ GÜVENLİ BAŞLANGIÇ: Output değişkenlerini PLC'ye yaz ═══
                    // PLC belleğinde eski TRUE değerleri kalabiliyor (STOP, RESET vb.)
                    // Timer başlamadan ÖNCE tüm output BOOL'ları FALSE'a yazılır.
                    // Bu sayede CMD_LINE_STOP, CMD_LINE_RESET gibi sinyaller
                    // eski değerleriyle robota aktarılmaz.
                    await InitializeOutputsAsync();

                    // Okuma hızı GlobalData'dan alınır (default 50ms, Limiter ile güvenli hız)
                    _refreshTimer = new System.Threading.Timer(TimerCallback, null, 0, GlobalData.Plc_ReadInterval);
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
            _refreshTimer?.Dispose();
            _melsecNet?.ConnectClose();
            IsConnected = false;
            GlobalData.PlcConnected = false;
        }

        // --- GÜÇLENDİRİLMİŞ OKUMA DÖNGÜSÜ ---
        private bool _isScanning = false;
        private List<PlcVariable> _cachedReadList;
        private bool _readListDirty = true;
        
        // Semaphore to limit parallel PLC requests to avoid overloading the socket/network
        private System.Threading.SemaphoreSlim _parallelLimiter = new System.Threading.SemaphoreSlim(4);

        private void UpdateReadList()
        {
            _cachedReadList = InputVariables.Concat(OutputVariables).ToList();
            _readListDirty = false;
        }

        private async void TimerCallback(object state)
        {
            if (!IsConnected || _melsecNet == null || _isScanning) return;
            
            _isScanning = true;
            try
            {
                if (_readListDirty) UpdateReadList();
                var scanList = _cachedReadList;

                // PARALEL OKUMA (Limiter ile): 
                // Serial çok yavaş kalıyor. Parallel full çok riskli. 
                // SemaphoreSlim ile aynı anda maks 4 istek gönderiyoruz.
                var tasks = scanList.Select(async variable =>
                {
                    await _parallelLimiter.WaitAsync();
                    try
                    {
                        string address = variable.Address;
                        if (string.IsNullOrEmpty(address)) return;

                        object newValue = null;
                        bool readSuccess = false;
                        string type = variable.Type.ToUpper(); 

                        switch (type)
                        {
                            case "BOOL":
                                var bRes = await _melsecNet.ReadBoolAsync(address);
                                if (bRes.IsSuccess) { newValue = bRes.Content ? 1 : 0; readSuccess = true; }
                                break;

                            case "INT": 
                                var iRes = await _melsecNet.ReadInt16Async(address);
                                if (iRes.IsSuccess) { newValue = iRes.Content; readSuccess = true; }
                                break;

                            case "WORD": 
                                var wRes = await _melsecNet.ReadUInt16Async(address);
                                if (wRes.IsSuccess) { newValue = wRes.Content; readSuccess = true; }
                                break;

                            case "DINT": 
                                var diRes = await _melsecNet.ReadInt32Async(address);
                                if (diRes.IsSuccess) { newValue = diRes.Content; readSuccess = true; }
                                break;

                            case "DWORD": 
                                var dwRes = await _melsecNet.ReadUInt32Async(address);
                                if (dwRes.IsSuccess) { newValue = dwRes.Content; readSuccess = true; }
                                break;

                            case "REAL": 
                            case "FLOAT":
                                var fRes = await _melsecNet.ReadFloatAsync(address);
                                if (fRes.IsSuccess) { newValue = Math.Round(fRes.Content, 2); readSuccess = true; } 
                                break;

                            case "STRING":
                                var sRes = await _melsecNet.ReadStringAsync(address, 10);
                                if (sRes.IsSuccess)
                                {
                                    newValue = sRes.Content.Trim('\0', ' '); 
                                    readSuccess = true;
                                }
                                break;

                            default: 
                                var defRes = await _melsecNet.ReadInt16Async(address);
                                if (defRes.IsSuccess) { newValue = defRes.Content; readSuccess = true; }
                                break;
                        }

                        if (readSuccess && newValue != null)
                        {
                            if (variable.CurrentValue?.ToString() != newValue.ToString())
                            {
                                UiRunner?.Invoke(() => variable.CurrentValue = newValue);
                            }
                        }
                    }
                    catch { }
                    finally
                    {
                        _parallelLimiter.Release();
                    }
                });

                await Task.WhenAll(tasks);
            }
            finally
            {
                _isScanning = false;
            }
        }

        // --- GÜÇLENDİRİLMİŞ YAZMA METODU ---
        public async Task WriteAsync(PlcVariable variable, object value)
        {
            if (!IsConnected) return;
            string address = variable.Name.Split('-')[0].Trim();
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

        // --- ÖRNEK DEĞİŞKENLER (SENİN İÇİN ÇEŞİTLENDİRDİM) ---
        private void LoadVariables()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    string json = File.ReadAllText(_configFilePath);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var data = JsonSerializer.Deserialize<PlcConfigData>(json, options);

                    if (data != null)
                    {
                        InputVariables.Clear();
                        foreach (var item in data.Inputs) InputVariables.Add(item);

                        OutputVariables.Clear();
                        foreach (var item in data.Outputs) OutputVariables.Add(item);

                        System.Diagnostics.Debug.WriteLine("PLC Ayarları yüklendi.");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Yükleme Hatası: " + ex.Message);
            }

            // Eğer dosya yoksa veya hata varsa varsayılanları yükle
            LoadDefaultVariables();
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
                    Inputs = InputVariables.ToList(),
                    Outputs = OutputVariables.ToList()
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(data, options);

                File.WriteAllText(_configFilePath, json);
                System.Diagnostics.Debug.WriteLine($"[PLC_SAVE] {_configFilePath} → {data.Inputs.Count} input, {data.Outputs.Count} output kaydedildi");
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
        /// CSV klasöründeki tüm CSV dosyalarını parse edip belirtilen yöne (Input/Output) ekler.
        /// Mevcut yöndeki değişkenleri siler, diğer yöndekilere dokunmaz.
        /// </summary>
        public int ImportCsvToDirection(string folderPath, string direction)
        {
            var newVars = new List<PlcVariable>();

            var csvFiles = Directory.GetFiles(folderPath, "*.csv");
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
                        string address = cols.Length > 5 ? cols[5].Trim('"', ' ') : "";

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
                            Description = string.IsNullOrEmpty(address) ? "" : $"PLC: {address}",
                            CurrentValue = null
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CSV_IMPORT] {csvFile} hata: {ex.Message}");
                }
            }

            var sorted = newVars.OrderBy(v => v.Name).ToList();

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

            if (direction == "Input")
                existingData.Inputs = sorted;
            else
                existingData.Outputs = sorted;

            // Dosyaya kaydet
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(_configFilePath, JsonSerializer.Serialize(existingData, options));

            // UI thread'de koleksiyonu güncelle
            _dispatcherQueue?.TryEnqueue(() =>
            {
                var target = direction == "Input" ? InputVariables : OutputVariables;
                target.Clear();
                foreach (var v in sorted) target.Add(v);
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
            public List<PlcVariable> Inputs { get; set; } = new();
            public List<PlcVariable> Outputs { get; set; } = new();
        }






    }
}