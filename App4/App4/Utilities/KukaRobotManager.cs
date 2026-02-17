using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

namespace App4.Utilities
{
    // ═══════════════════════════════════════════════════════════════════════════
    // KUKA ROBOT INSTANCE - TEK BİR ROBOT BAĞLANTISI
    // ═══════════════════════════════════════════════════════════════════════════
    public class KukaRobotInstance : INotifyPropertyChanged
    {
        #region Temel Özellikler

        private string _name = "Robot";
        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(); } }
        }

        private string _ipAddress = "192.168.251.71";
        public string IpAddress
        {
            get => _ipAddress;
            set { if (_ipAddress != value) { _ipAddress = value; OnPropertyChanged(); } }
        }

        private int _port = 7000;
        public int Port
        {
            get => _port;
            set { if (_port != value) { _port = value; OnPropertyChanged(); } }
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(StatusColor));
                }
            }
        }

        public string StatusText => IsConnected ? "BAĞLI" : "BAĞLI DEĞİL";
        public string StatusColor => IsConnected ? "#00FF88" : "#FF4444";

        #endregion

        #region Robot Durumu (Canlı Veriler)

        // TCP Pozisyonu
        private double _posX, _posY, _posZ, _posA, _posB, _posC;
        public double PosX { get => _posX; set { if (Math.Abs(_posX - value) > 0.001) { _posX = value; OnPropertyChanged(); } } }
        public double PosY { get => _posY; set { if (Math.Abs(_posY - value) > 0.001) { _posY = value; OnPropertyChanged(); } } }
        public double PosZ { get => _posZ; set { if (Math.Abs(_posZ - value) > 0.001) { _posZ = value; OnPropertyChanged(); } } }
        public double PosA { get => _posA; set { if (Math.Abs(_posA - value) > 0.001) { _posA = value; OnPropertyChanged(); } } }
        public double PosB { get => _posB; set { if (Math.Abs(_posB - value) > 0.001) { _posB = value; OnPropertyChanged(); } } }
        public double PosC { get => _posC; set { if (Math.Abs(_posC - value) > 0.001) { _posC = value; OnPropertyChanged(); } } }

        // Eksen Açıları
        private double _a1, _a2, _a3, _a4, _a5, _a6;
        public double A1 { get => _a1; set { if (Math.Abs(_a1 - value) > 0.001) { _a1 = value; OnPropertyChanged(); } } }
        public double A2 { get => _a2; set { if (Math.Abs(_a2 - value) > 0.001) { _a2 = value; OnPropertyChanged(); } } }
        public double A3 { get => _a3; set { if (Math.Abs(_a3 - value) > 0.001) { _a3 = value; OnPropertyChanged(); } } }
        public double A4 { get => _a4; set { if (Math.Abs(_a4 - value) > 0.001) { _a4 = value; OnPropertyChanged(); } } }
        public double A5 { get => _a5; set { if (Math.Abs(_a5 - value) > 0.001) { _a5 = value; OnPropertyChanged(); } } }
        public double A6 { get => _a6; set { if (Math.Abs(_a6 - value) > 0.001) { _a6 = value; OnPropertyChanged(); } } }

        // Override Değerleri
        private int _overridePro = 100, _overrideJog = 100;
        public int OverridePro { get => _overridePro; set { if (_overridePro != value) { _overridePro = value; OnPropertyChanged(); } } }
        public int OverrideJog { get => _overrideJog; set { if (_overrideJog != value) { _overrideJog = value; OnPropertyChanged(); } } }

        // Durum Flagleri
        private bool _robotReady, _robotError, _robotRunning;
        public bool RobotReady { get => _robotReady; set { if (_robotReady != value) { _robotReady = value; OnPropertyChanged(); } } }
        public bool RobotError { get => _robotError; set { if (_robotError != value) { _robotError = value; OnPropertyChanged(); } } }
        public bool RobotRunning { get => _robotRunning; set { if (_robotRunning != value) { _robotRunning = value; OnPropertyChanged(); } } }

        #endregion

        #region Değişken Listeleri

        public ObservableCollection<PlcVariable> InputVars { get; } = new();
        public ObservableCollection<PlcVariable> OutputVars { get; } = new();

        // Standart KUKA değişkenleri (pozisyon, eksen, override için)
        private readonly List<(string Tag, Action<string> Setter)> _standardReads;

        #endregion

        #region Private Alanlar

        private TcpClient _client;
        private NetworkStream _stream;
        private bool _isRunning;
        private ushort _msgId;
        private readonly System.Threading.SemaphoreSlim _lock = new(1, 1);

        public event Action<string> OnLog;
        public DispatcherQueue UiDispatcher { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public KukaRobotInstance()
        {
            // Standart okuma listesi
            _standardReads = new List<(string, Action<string>)>
            {
                ("$POS_ACT.X", v => PosX = ParseDouble(v)),
                ("$POS_ACT.Y", v => PosY = ParseDouble(v)),
                ("$POS_ACT.Z", v => PosZ = ParseDouble(v)),
                ("$POS_ACT.A", v => PosA = ParseDouble(v)),
                ("$POS_ACT.B", v => PosB = ParseDouble(v)),
                ("$POS_ACT.C", v => PosC = ParseDouble(v)),
                ("$AXIS_ACT.A1", v => A1 = ParseDouble(v)),
                ("$AXIS_ACT.A2", v => A2 = ParseDouble(v)),
                ("$AXIS_ACT.A3", v => A3 = ParseDouble(v)),
                ("$AXIS_ACT.A4", v => A4 = ParseDouble(v)),
                ("$AXIS_ACT.A5", v => A5 = ParseDouble(v)),
                ("$AXIS_ACT.A6", v => A6 = ParseDouble(v)),
                ("$OV_PRO", v => OverridePro = ParseInt(v)),
                ("$OV_JOG", v => OverrideJog = ParseInt(v)),
                ("Robot_Ready", v => RobotReady = ParseBool(v)),
                ("Robot_Error", v => RobotError = ParseBool(v)),
                ("Robot_Running", v => RobotRunning = ParseBool(v)),
            };
        }

        public KukaRobotInstance(string name, string ip, int port = 7000) : this()
        {
            Name = name;
            IpAddress = ip;
            Port = port;
            InitializeDefaultVariables();
        }

        private void InitializeDefaultVariables()
        {
            // TCP Pozisyon değişkenleri (Input - Okunacak)
            InputVars.Add(new PlcVariable { Name = "TCP_X", Type = "REAL", PlcTag = "$POS_ACT.X", Direction = "Input" });
            InputVars.Add(new PlcVariable { Name = "TCP_Y", Type = "REAL", PlcTag = "$POS_ACT.Y", Direction = "Input" });
            InputVars.Add(new PlcVariable { Name = "TCP_Z", Type = "REAL", PlcTag = "$POS_ACT.Z", Direction = "Input" });
            InputVars.Add(new PlcVariable { Name = "TCP_A", Type = "REAL", PlcTag = "$POS_ACT.A", Direction = "Input" });
            InputVars.Add(new PlcVariable { Name = "TCP_B", Type = "REAL", PlcTag = "$POS_ACT.B", Direction = "Input" });
            InputVars.Add(new PlcVariable { Name = "TCP_C", Type = "REAL", PlcTag = "$POS_ACT.C", Direction = "Input" });

            // Eksen açıları (Input - Okunacak)
            InputVars.Add(new PlcVariable { Name = "AXIS_A1", Type = "REAL", PlcTag = "$AXIS_ACT.A1", Direction = "Input" });
            InputVars.Add(new PlcVariable { Name = "AXIS_A2", Type = "REAL", PlcTag = "$AXIS_ACT.A2", Direction = "Input" });
            InputVars.Add(new PlcVariable { Name = "AXIS_A3", Type = "REAL", PlcTag = "$AXIS_ACT.A3", Direction = "Input" });
            InputVars.Add(new PlcVariable { Name = "AXIS_A4", Type = "REAL", PlcTag = "$AXIS_ACT.A4", Direction = "Input" });
            InputVars.Add(new PlcVariable { Name = "AXIS_A5", Type = "REAL", PlcTag = "$AXIS_ACT.A5", Direction = "Input" });
            InputVars.Add(new PlcVariable { Name = "AXIS_A6", Type = "REAL", PlcTag = "$AXIS_ACT.A6", Direction = "Input" });

            // Override değerleri (Input - Okunacak)
            InputVars.Add(new PlcVariable { Name = "OV_PRO", Type = "INT", PlcTag = "$OV_PRO", Direction = "Input" });
            InputVars.Add(new PlcVariable { Name = "OV_JOG", Type = "INT", PlcTag = "$OV_JOG", Direction = "Input" });

            // Durum değişkenleri (Input - Okunacak)
            InputVars.Add(new PlcVariable { Name = "ROBOT_READY", Type = "BOOL", PlcTag = "Robot_Ready", Direction = "Input" });
            InputVars.Add(new PlcVariable { Name = "ROBOT_ERROR", Type = "BOOL", PlcTag = "Robot_Error", Direction = "Input" });
            InputVars.Add(new PlcVariable { Name = "ROBOT_RUNNING", Type = "BOOL", PlcTag = "Robot_Running", Direction = "Input" });

            // Output değişkenleri (Yazılacak)
            OutputVars.Add(new PlcVariable { Name = "PC_START", Type = "BOOL", PlcTag = "PC_Start", Direction = "Output" });
            OutputVars.Add(new PlcVariable { Name = "PC_STOP", Type = "BOOL", PlcTag = "PC_Stop", Direction = "Output" });
            OutputVars.Add(new PlcVariable { Name = "PC_RESET", Type = "BOOL", PlcTag = "PC_Reset", Direction = "Output" });
            OutputVars.Add(new PlcVariable { Name = "PC_RECIPE", Type = "INT", PlcTag = "PC_RecipeNo", Direction = "Output" });
            OutputVars.Add(new PlcVariable { Name = "SET_JOG_OV", Type = "INT", PlcTag = "$OV_JOG", Direction = "Output" });
        }

        #region Bağlantı Yönetimi

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;
            Task.Run(CommunicationLoop);
            OnLog?.Invoke($"[{Name}] Servis başlatıldı.");
        }

        public void Stop()
        {
            _isRunning = false;
            Disconnect();
            OnLog?.Invoke($"[{Name}] Servis durduruldu.");
        }

        private void Disconnect()
        {
            try
            {
                _stream?.Close();
                _client?.Close();
                _client = null;
                IsConnected = false;
            }
            catch { }
        }

        private async Task ConnectAsync()
        {
            try
            {
                OnLog?.Invoke($"[{Name}] Bağlanılıyor {IpAddress}:{Port}...");
                _client = new TcpClient { SendTimeout = 5000, ReceiveTimeout = 5000 };
                await _client.ConnectAsync(IpAddress, Port);
                _stream = _client.GetStream();
                IsConnected = true;
                OnLog?.Invoke($"[{Name}] ✅ Bağlandı!");
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"[{Name}] ❌ Bağlantı hatası: {ex.Message}");
                _client = null;
                IsConnected = false;
            }
        }

        #endregion

        #region Haberleşme Döngüsü

        private async Task CommunicationLoop()
        {
            while (_isRunning)
            {
                try
                {
                    if (!IsConnected || _client == null || !_client.Connected)
                    {
                        IsConnected = false;
                        await ConnectAsync();
                    }

                    if (IsConnected)
                    {
                        // 1. Standart değişkenleri oku (pozisyon, eksen, override)
                        foreach (var (tag, setter) in _standardReads)
                        {
                            if (!_isRunning || !IsConnected) break;
                            try
                            {
                                string val = await ReadVariableAsync(tag);
                                if (!string.IsNullOrEmpty(val))
                                {
                                    DispatchToUi(() => setter(val));
                                }
                            }
                            catch { }
                            await Task.Delay(30); // Değişkenler arası kısa bekleme
                        }

                        // 2. Kullanıcı tanımlı Input değişkenlerini oku
                        var userInputs = InputVars.Where(v => !string.IsNullOrEmpty(v.PlcTag)).ToList();
                        foreach (var variable in userInputs)
                        {
                            if (!_isRunning || !IsConnected) break;
                            try
                            {
                                string val = await ReadVariableAsync(variable.PlcTag);
                                if (!string.IsNullOrEmpty(val) && variable.Value != val)
                                {
                                    DispatchToUi(() => variable.Value = val);
                                }
                            }
                            catch { }
                            await Task.Delay(30);
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnLog?.Invoke($"[{Name}] Hata: {ex.Message}");
                    Disconnect();
                    await Task.Delay(3000);
                }

                await Task.Delay(500); // Döngü hızı
            }
        }

        #endregion

        #region Okuma/Yazma İşlemleri

        public async Task<string> ReadVariableAsync(string varName)
        {
            return await SendRequestAsync(varName, null, 0);
        }

        public async Task<bool> WriteVariableAsync(string varName, string value)
        {
            var result = await SendRequestAsync(varName, value, 1);
            return result == "OK";
        }

        private async Task<string> SendRequestAsync(string varName, string value, int type)
        {
            if (!IsConnected || _stream == null) return null;

            await _lock.WaitAsync();
            try
            {
                _msgId++;
                ushort id = _msgId;

                string cleanValue = value?.Replace(",", ".").Trim() ?? "";
                byte[] nameBytes = Encoding.ASCII.GetBytes(varName);
                byte[] valueBytes = type == 1 ? Encoding.ASCII.GetBytes(cleanValue) : Array.Empty<byte>();

                int nameLen = nameBytes.Length;
                int valueLen = valueBytes.Length;
                int contentLen = 1 + 2 + nameLen + (type == 1 ? 2 + valueLen : 0);

                var packet = new List<byte>
                {
                    (byte)((id >> 8) & 0xFF), (byte)(id & 0xFF),
                    (byte)((contentLen >> 8) & 0xFF), (byte)(contentLen & 0xFF),
                    (byte)type,
                    (byte)((nameLen >> 8) & 0xFF), (byte)(nameLen & 0xFF)
                };
                packet.AddRange(nameBytes);

                if (type == 1)
                {
                    packet.Add((byte)((valueLen >> 8) & 0xFF));
                    packet.Add((byte)(valueLen & 0xFF));
                    packet.AddRange(valueBytes);
                }

                await _stream.WriteAsync(packet.ToArray(), 0, packet.Count);

                // Response
                byte[] header = new byte[4];
                int read = await ReadExactAsync(header, 4);
                if (read != 4) throw new Exception("Header okunamadı");

                ushort respContentLen = (ushort)((header[2] << 8) | header[3]);
                if (respContentLen == 0) return null;

                byte[] content = new byte[respContentLen];
                read = await ReadExactAsync(content, respContentLen);
                if (read != respContentLen || respContentLen < 6) return null;

                ushort respValueLen = (ushort)((content[1] << 8) | content[2]);
                if (respContentLen < 3 + respValueLen + 3) return null;

                string resultValue = respValueLen > 0
                    ? Encoding.ASCII.GetString(content, 3, respValueLen).Trim('\0').Trim()
                    : "";

                int tailStart = 3 + respValueLen;
                bool isSuccess = content[tailStart] == 0x00 && content[tailStart + 1] == 0x01 && content[tailStart + 2] == 0x01;

                return isSuccess ? (type == 0 ? resultValue : "OK") : null;
            }
            catch
            {
                Disconnect();
                return null;
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<int> ReadExactAsync(byte[] buffer, int length)
        {
            int offset = 0;
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(3));
            while (offset < length)
            {
                int read = await _stream.ReadAsync(buffer.AsMemory(offset, length - offset), cts.Token);
                if (read == 0) break;
                offset += read;
            }
            return offset;
        }

        #endregion

        #region Yardımcı Metodlar

        private void DispatchToUi(Action action)
        {
            if (UiDispatcher != null)
                UiDispatcher.TryEnqueue(() => action());
            else
                action();
        }

        private static double ParseDouble(string val)
        {
            if (string.IsNullOrEmpty(val)) return 0;
            val = val.Replace(",", ".").Trim();
            return double.TryParse(val, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double result) ? Math.Round(result, 2) : 0;
        }

        private static int ParseInt(string val)
        {
            if (string.IsNullOrEmpty(val)) return 0;
            return int.TryParse(val.Trim(), out int result) ? result : 0;
        }

        private static bool ParseBool(string val)
        {
            if (string.IsNullOrEmpty(val)) return false;
            val = val.Trim().ToUpper();
            return val == "TRUE" || val == "1";
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // KUKA ROBOT MANAGER - ÇOKLU ROBOT YÖNETİCİSİ
    // ═══════════════════════════════════════════════════════════════════════════
    public class KukaRobotManager : INotifyPropertyChanged
    {
        private static KukaRobotManager _instance;
        public static KukaRobotManager Instance => _instance ??= new KukaRobotManager();

        public ObservableCollection<KukaRobotInstance> Robots { get; } = new();

        public event Action<string> OnLog;
        public DispatcherQueue UiDispatcher { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly string _configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "App4", "KukaRobots.json");

        private KukaRobotManager()
        {
            LoadRobots();
        }

        public void Initialize(DispatcherQueue dispatcher)
        {
            UiDispatcher = dispatcher;
            foreach (var robot in Robots)
            {
                robot.UiDispatcher = dispatcher;
                robot.OnLog += msg => OnLog?.Invoke(msg);
            }

            // Robotları otomatik başlat
            StartAll();
        }

        public KukaRobotInstance AddRobot(string name, string ip, int port = 7000)
        {
            var robot = new KukaRobotInstance(name, ip, port)
            {
                UiDispatcher = UiDispatcher
            };
            robot.OnLog += msg => OnLog?.Invoke(msg);
            Robots.Add(robot);
            SaveRobots();
            // Yeni eklenen robotu otomatik başlat
            robot.Start();
            return robot;
        }

        public void UpdateRobotIp(KukaRobotInstance robot, string newIp, int newPort)
        {
            robot.Stop();
            robot.IpAddress = newIp;
            robot.Port = newPort;
            SaveRobots();
            robot.Start();
        }

        public void RemoveRobot(KukaRobotInstance robot)
        {
            robot.Stop();
            Robots.Remove(robot);
            SaveRobots();
        }

        public void StartAll()
        {
            foreach (var robot in Robots)
                robot.Start();
        }

        public void StopAll()
        {
            foreach (var robot in Robots)
                robot.Stop();
        }

        private void LoadRobots()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    var configs = JsonSerializer.Deserialize<List<RobotConfig>>(json);
                    if (configs != null && configs.Count > 0)
                    {
                        foreach (var cfg in configs)
                        {
                            Robots.Add(new KukaRobotInstance(cfg.Name, cfg.IpAddress, cfg.Port));
                        }
                        return;
                    }
                }
            }
            catch { }

            // Varsayılan robotlar
            Robots.Add(new KukaRobotInstance("ROBOT 1", "192.168.251.71", 7000));
            Robots.Add(new KukaRobotInstance("ROBOT 2", "192.168.251.72", 7000));
            SaveRobots();
        }

        public void SaveRobots()
        {
            try
            {
                var configs = Robots.Select(r => new RobotConfig
                {
                    Name = r.Name,
                    IpAddress = r.IpAddress,
                    Port = r.Port
                }).ToList();

                var json = JsonSerializer.Serialize(configs, new JsonSerializerOptions { WriteIndented = true });

                var dir = Path.GetDirectoryName(_configPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(_configPath, json);
            }
            catch { }
        }

        private class RobotConfig
        {
            public string Name { get; set; }
            public string IpAddress { get; set; }
            public int Port { get; set; }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
