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
        [JsonPropertyName("name")]
        public string Name { get; set; }

        // Desteklenen Tipler: "BOOL", "INT", "WORD", "DINT", "DWORD", "REAL"
        [JsonPropertyName("type")]
        public string Type { get; set; } = "WORD";

        [JsonPropertyName("direction")]
        public string Direction { get; set; } = "Output";

        public string Description { get; set; }
        public bool IsEditable { get; set; } = true;

        [JsonIgnore]
        public bool IsReadOnly => !IsEditable;

        private string _plcTag;
        public string PlcTag
        {
            get => _plcTag;
            set { if (_plcTag != value) { _plcTag = value; OnPropertyChanged(); } }
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
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "App4", "PLC_Config.json");



        private PlcService()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            // Klasör yoksa oluştur
            var directory = Path.GetDirectoryName(_configFilePath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            // Kayıtlı verileri yükle
            LoadVariables();
        }

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
                    // 500ms aralıkla okuma başlat
                    _refreshTimer = new System.Threading.Timer(TimerCallback, null, 0, 500);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("PLC Bağlantı Hatası: " + ex.Message);
            }
            return false;
        }

        public void Disconnect()
        {
            _refreshTimer?.Dispose();
            _melsecNet?.ConnectClose();
            IsConnected = false;
        }

        // --- GÜÇLENDİRİLMİŞ OKUMA DÖNGÜSÜ ---
        private async void TimerCallback(object state)
        {
            if (!IsConnected || _melsecNet == null) return;

            // Liste üzerinde işlem yaparken hata almamak için .ToList() kullanıyoruz
            foreach (var variable in InputVariables.ToList())
            {
                try
                {
                    // Adres formatı: "D100 - Sıcaklık" ise sadece "D100" kısmını al
                    string address = variable.Name.Split('-')[0].Trim();
                    if (string.IsNullOrEmpty(address)) continue;

                    object newValue = null;
                    bool readSuccess = false;
                    string type = variable.Type.ToUpper(); // Büyük harfe çevir (örn: "int" -> "INT")

                    // TİPE GÖRE OKUMA (SWITCH CASE)
                    switch (type)
                    {
                        case "BOOL":
                            var bRes = await _melsecNet.ReadBoolAsync(address);
                            if (bRes.IsSuccess) { newValue = bRes.Content ? 1 : 0; readSuccess = true; }
                            break;

                        case "INT": // 16-bit Signed (D, W)
                            var iRes = await _melsecNet.ReadInt16Async(address);
                            if (iRes.IsSuccess) { newValue = iRes.Content; readSuccess = true; }
                            break;

                        case "WORD": // 16-bit Unsigned (W, D) - Senin W600 için bu lazım
                            var wRes = await _melsecNet.ReadUInt16Async(address);
                            if (wRes.IsSuccess) { newValue = wRes.Content; readSuccess = true; }
                            break;

                        case "DINT": // 32-bit Signed (Çift Register)
                            var diRes = await _melsecNet.ReadInt32Async(address);
                            if (diRes.IsSuccess) { newValue = diRes.Content; readSuccess = true; }
                            break;

                        case "DWORD": // 32-bit Unsigned
                            var dwRes = await _melsecNet.ReadUInt32Async(address);
                            if (dwRes.IsSuccess) { newValue = dwRes.Content; readSuccess = true; }
                            break;

                        case "REAL": // 32-bit Float (Ondalıklı Sayı)
                        case "FLOAT":
                            var fRes = await _melsecNet.ReadFloatAsync(address);
                            if (fRes.IsSuccess) { newValue = Math.Round(fRes.Content, 2); readSuccess = true; } // 2 hane yuvarla
                            break;

                        case "STRING":
                            // 10 karakter uzunluğunda metin oku (Uzunluğa göre 10'u PLC programına göre değiştirebilirsin)
                            var sRes = await _melsecNet.ReadStringAsync(address, 10);
                            if (sRes.IsSuccess)
                            {
                                newValue = sRes.Content.Trim('\0', ' '); // Boşlukları ve null karakterleri temizle
                                readSuccess = true;
                            }
                            break;

                        default: // Tanımsızsa INT16 dene
                            var defRes = await _melsecNet.ReadInt16Async(address);
                            if (defRes.IsSuccess) { newValue = defRes.Content; readSuccess = true; }
                            break;
                    }

                    // Eğer okuma başarılıysa ve değer değiştiyse UI güncelle
                    if (readSuccess && newValue != null)
                    {
                        UiRunner?.Invoke(() =>
                        {
                            // Değerleri string karşılaştırarak gereksiz güncellemeyi önle
                            if (variable.CurrentValue?.ToString() != newValue.ToString())
                            {
                                variable.CurrentValue = newValue;
                            }
                        });
                    }
                }
                catch { }
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
                    var data = JsonSerializer.Deserialize<PlcConfigData>(json);

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
                var data = new PlcConfigData
                {
                    Inputs = InputVariables.ToList(),
                    Outputs = OutputVariables.ToList()
                };

                // Okunaklı (Indented) şekilde kaydet
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(data, options);

                File.WriteAllText(_configFilePath, json);
                System.Diagnostics.Debug.WriteLine("PLC Ayarları kaydedildi.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Kaydetme Hatası: " + ex.Message);
            }
        }

        // Varsayılanlar (Eğer dosya yoksa bunlar gelir)
        private void LoadDefaultVariables()
        {
            InputVariables.Add(new PlcVariable { Name = "D0 - Okunan Değer", Type = "WORD", Direction = "Input", CurrentValue = 0 });
            InputVariables.Add(new PlcVariable { Name = "W600 - Link Reg", Type = "WORD", Direction = "Input", CurrentValue = 0 }); // Senin W600

            OutputVariables.Add(new PlcVariable { Name = "D0 - Yazılan Değer", Type = "WORD", Direction = "Output", CurrentValue = 0 });
        }

        // JSON Serileştirme için yardımcı sınıf (PlcService sınıfının dışında veya içinde olabilir)
        public class PlcConfigData
        {
            public List<PlcVariable> Inputs { get; set; } = new();
            public List<PlcVariable> Outputs { get; set; } = new();
        }






    }
}