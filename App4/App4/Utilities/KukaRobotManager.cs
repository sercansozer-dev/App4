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
                    // Global ekipman durumunu güncelle
                    GlobalData.RefreshEquipmentStatus();
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

        // Harici Eksenler (KL100 Slider için E1)
        private double _e1, _e2, _e3, _e4, _e5, _e6;
        public double E1 { get => _e1; set { if (Math.Abs(_e1 - value) > 0.1) { _e1 = value; OnPropertyChanged(); OnPropertyChanged(nameof(SliderPositionPercent)); } } }
        public double E2 { get => _e2; set { if (Math.Abs(_e2 - value) > 0.1) { _e2 = value; OnPropertyChanged(); } } }
        public double E3 { get => _e3; set { if (Math.Abs(_e3 - value) > 0.1) { _e3 = value; OnPropertyChanged(); } } }
        public double E4 { get => _e4; set { if (Math.Abs(_e4 - value) > 0.1) { _e4 = value; OnPropertyChanged(); } } }
        public double E5 { get => _e5; set { if (Math.Abs(_e5 - value) > 0.1) { _e5 = value; OnPropertyChanged(); } } }
        public double E6 { get => _e6; set { if (Math.Abs(_e6 - value) > 0.1) { _e6 = value; OnPropertyChanged(); } } }

        // KL100 Slider Sınırları (mm cinsinden - varsayılan değerler, ayardan okunabilir)
        public double SliderMinPos { get; set; } = 0;      // Slider minimum pozisyon (mm)
        public double SliderMaxPos { get; set; } = 3000;   // Slider maksimum pozisyon (mm)

        /// <summary>
        /// Slider pozisyonunu yüzde olarak döndürür (0-100)
        /// E1 harici eksen değerine göre hesaplanır
        /// </summary>
        public double SliderPositionPercent
        {
            get
            {
                if (SliderMaxPos <= SliderMinPos) return 0;
                double percent = ((E1 - SliderMinPos) / (SliderMaxPos - SliderMinPos)) * 100;
                return Math.Clamp(percent, 0, 100);
            }
        }

        // Override Değerleri
        private int _overridePro = 100, _overrideJog = 100;
        public int OverridePro { get => _overridePro; set { if (_overridePro != value) { _overridePro = value; OnPropertyChanged(); } } }
        public int OverrideJog { get => _overrideJog; set { if (_overrideJog != value) { _overrideJog = value; OnPropertyChanged(); } } }

        // Temel Durum Flagleri
        private bool _robotReady, _robotError, _robotRunning;
        public bool RobotReady { get => _robotReady; set { if (_robotReady != value) { _robotReady = value; OnPropertyChanged(); } } }
        public bool RobotError { get => _robotError; set { if (_robotError != value) { _robotError = value; OnPropertyChanged(); } } }
        public bool RobotRunning { get => _robotRunning; set { if (_robotRunning != value) { _robotRunning = value; OnPropertyChanged(); } } }

        #endregion

        #region Safety & Gelişmiş Durum Verileri

        // Safety Durumları
        private bool _drivesOn, _emergencyStop, _safetyGate, _peripheralReady, _userSafety, _alarmStop;
        public bool DrivesOn { get => _drivesOn; set { if (_drivesOn != value) { _drivesOn = value; OnPropertyChanged(); } } }
        public bool EmergencyStop { get => _emergencyStop; set { if (_emergencyStop != value) { _emergencyStop = value; OnPropertyChanged(); } } }
        public bool SafetyGate { get => _safetyGate; set { if (_safetyGate != value) { _safetyGate = value; OnPropertyChanged(); } } }
        public bool PeripheralReady { get => _peripheralReady; set { if (_peripheralReady != value) { _peripheralReady = value; OnPropertyChanged(); } } }
        public bool UserSafety { get => _userSafety; set { if (_userSafety != value) { _userSafety = value; OnPropertyChanged(); } } }
        public bool AlarmStop { get => _alarmStop; set { if (_alarmStop != value) { _alarmStop = value; OnPropertyChanged(); } } }

        // Operasyon Modu (1=T1, 2=T2, 3=AUT, 4=EXT)
        private int _operationMode;
        public int OperationMode { get => _operationMode; set { if (_operationMode != value) { _operationMode = value; OnPropertyChanged(); OnPropertyChanged(nameof(OperationModeText)); } } }
        public string OperationModeText => OperationMode switch { 1 => "T1", 2 => "T2", 3 => "AUT", 4 => "EXT", _ => "?" };

        // Program Durumu
        private string _programName = "", _currentStep = "";
        private int _programState; // 0=Stopped, 1=Running, 2=Paused
        public string ProgramName { get => _programName; set { if (_programName != value) { _programName = value; OnPropertyChanged(); } } }
        public string CurrentStep { get => _currentStep; set { if (_currentStep != value) { _currentStep = value; OnPropertyChanged(); } } }
        public int ProgramState { get => _programState; set { if (_programState != value) { _programState = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProgramStateText)); } } }
        public string ProgramStateText => ProgramState switch { 0 => "Stopped", 1 => "Running", 2 => "Paused", _ => "Unknown" };

        // Tool & Base
        private int _toolNo, _baseNo;
        public int ToolNo { get => _toolNo; set { if (_toolNo != value) { _toolNo = value; OnPropertyChanged(); } } }
        public int BaseNo { get => _baseNo; set { if (_baseNo != value) { _baseNo = value; OnPropertyChanged(); } } }

        // Hata Bilgisi
        private int _errorNo;
        private string _errorMessage = "";
        public int ErrorNo { get => _errorNo; set { if (_errorNo != value) { _errorNo = value; OnPropertyChanged(); } } }
        public string ErrorMessage { get => _errorMessage; set { if (_errorMessage != value) { _errorMessage = value; OnPropertyChanged(); } } }

        #endregion

        #region Motor & Performans Verileri

        // Eksen Torkları (%)
        private double _torque1, _torque2, _torque3, _torque4, _torque5, _torque6;
        public double Torque1 { get => _torque1; set { if (Math.Abs(_torque1 - value) > 0.1) { _torque1 = value; OnPropertyChanged(); } } }
        public double Torque2 { get => _torque2; set { if (Math.Abs(_torque2 - value) > 0.1) { _torque2 = value; OnPropertyChanged(); } } }
        public double Torque3 { get => _torque3; set { if (Math.Abs(_torque3 - value) > 0.1) { _torque3 = value; OnPropertyChanged(); } } }
        public double Torque4 { get => _torque4; set { if (Math.Abs(_torque4 - value) > 0.1) { _torque4 = value; OnPropertyChanged(); } } }
        public double Torque5 { get => _torque5; set { if (Math.Abs(_torque5 - value) > 0.1) { _torque5 = value; OnPropertyChanged(); } } }
        public double Torque6 { get => _torque6; set { if (Math.Abs(_torque6 - value) > 0.1) { _torque6 = value; OnPropertyChanged(); } } }

        // Motor Sıcaklıkları (°C)
        private double _temp1, _temp2, _temp3, _temp4, _temp5, _temp6;
        public double Temp1 { get => _temp1; set { if (Math.Abs(_temp1 - value) > 0.1) { _temp1 = value; OnPropertyChanged(); } } }
        public double Temp2 { get => _temp2; set { if (Math.Abs(_temp2 - value) > 0.1) { _temp2 = value; OnPropertyChanged(); } } }
        public double Temp3 { get => _temp3; set { if (Math.Abs(_temp3 - value) > 0.1) { _temp3 = value; OnPropertyChanged(); } } }
        public double Temp4 { get => _temp4; set { if (Math.Abs(_temp4 - value) > 0.1) { _temp4 = value; OnPropertyChanged(); } } }
        public double Temp5 { get => _temp5; set { if (Math.Abs(_temp5 - value) > 0.1) { _temp5 = value; OnPropertyChanged(); } } }
        public double Temp6 { get => _temp6; set { if (Math.Abs(_temp6 - value) > 0.1) { _temp6 = value; OnPropertyChanged(); } } }

        // Eksen Hızları (°/s veya mm/s)
        private double _vel1, _vel2, _vel3, _vel4, _vel5, _vel6;
        public double Vel1 { get => _vel1; set { if (Math.Abs(_vel1 - value) > 0.1) { _vel1 = value; OnPropertyChanged(); } } }
        public double Vel2 { get => _vel2; set { if (Math.Abs(_vel2 - value) > 0.1) { _vel2 = value; OnPropertyChanged(); } } }
        public double Vel3 { get => _vel3; set { if (Math.Abs(_vel3 - value) > 0.1) { _vel3 = value; OnPropertyChanged(); } } }
        public double Vel4 { get => _vel4; set { if (Math.Abs(_vel4 - value) > 0.1) { _vel4 = value; OnPropertyChanged(); } } }
        public double Vel5 { get => _vel5; set { if (Math.Abs(_vel5 - value) > 0.1) { _vel5 = value; OnPropertyChanged(); } } }
        public double Vel6 { get => _vel6; set { if (Math.Abs(_vel6 - value) > 0.1) { _vel6 = value; OnPropertyChanged(); } } }

        // Çalışma Süreleri
        private double _operatingHours, _cycleTime;
        public double OperatingHours { get => _operatingHours; set { if (Math.Abs(_operatingHours - value) > 0.01) { _operatingHours = value; OnPropertyChanged(); } } }
        public double CycleTime { get => _cycleTime; set { if (Math.Abs(_cycleTime - value) > 0.001) { _cycleTime = value; OnPropertyChanged(); } } }

        // TCP Hızı
        private double _tcpSpeed;
        public double TcpSpeed { get => _tcpSpeed; set { if (Math.Abs(_tcpSpeed - value) > 0.1) { _tcpSpeed = value; OnPropertyChanged(); } } }

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
            // Standart okuma listesi - TÜM KUKA DEĞİŞKENLERİ
            _standardReads = new List<(string, Action<string>)>
            {
                // ═══ TCP POZİSYONU ═══
                ("$POS_ACT.X", v => PosX = ParseDouble(v)),
                ("$POS_ACT.Y", v => PosY = ParseDouble(v)),
                ("$POS_ACT.Z", v => PosZ = ParseDouble(v)),
                ("$POS_ACT.A", v => PosA = ParseDouble(v)),
                ("$POS_ACT.B", v => PosB = ParseDouble(v)),
                ("$POS_ACT.C", v => PosC = ParseDouble(v)),

                // ═══ EKSEN AÇILARI ═══
                ("$AXIS_ACT.A1", v => A1 = ParseDouble(v)),
                ("$AXIS_ACT.A2", v => A2 = ParseDouble(v)),
                ("$AXIS_ACT.A3", v => A3 = ParseDouble(v)),
                ("$AXIS_ACT.A4", v => A4 = ParseDouble(v)),
                ("$AXIS_ACT.A5", v => A5 = ParseDouble(v)),
                ("$AXIS_ACT.A6", v => A6 = ParseDouble(v)),

                // ═══ HARİCİ EKSENLER (KL100 Slider için E1-E6) ═══
                ("$AXIS_ACT.E1", v => E1 = ParseDouble(v)),
                ("$AXIS_ACT.E2", v => E2 = ParseDouble(v)),
                ("$AXIS_ACT.E3", v => E3 = ParseDouble(v)),
                ("$AXIS_ACT.E4", v => E4 = ParseDouble(v)),
                ("$AXIS_ACT.E5", v => E5 = ParseDouble(v)),
                ("$AXIS_ACT.E6", v => E6 = ParseDouble(v)),

                // ═══ OVERRİDE ═══
                ("$OV_PRO", v => OverridePro = ParseInt(v)),
                ("$OV_JOG", v => OverrideJog = ParseInt(v)),

                // ═══ SAFETY DURUMLARI ═══
                ("$DRIVES_ON", v => DrivesOn = ParseBool(v)),
                ("$STOPMESS", v => EmergencyStop = ParseBool(v)),
                ("$PERI_RDY", v => PeripheralReady = ParseBool(v)),
                ("$ROB_RDY", v => RobotReady = ParseBool(v)),
                ("$USER_SAF", v => UserSafety = ParseBool(v)),
                ("$ALARM_STOP", v => AlarmStop = ParseBool(v)),

                // ═══ OPERASYON MODU ═══
                ("$MODE_OP", v => OperationMode = ParseInt(v)),

                // ═══ PROGRAM BİLGİLERİ ═══
                ("$PRO_STATE", v => ProgramState = ParseInt(v)),
                ("$PRO_NAME[]", v => ProgramName = v?.Trim() ?? ""),
                ("$STEP_NAME[]", v => CurrentStep = v?.Trim() ?? ""),

                // ═══ TOOL & BASE ═══
                ("$TOOL_ACT", v => ToolNo = ParseInt(v)),
                ("$BASE_ACT", v => BaseNo = ParseInt(v)),

                // ═══ HATA BİLGİLERİ ═══
                ("$ERR.NO", v => ErrorNo = ParseInt(v)),
                ("$ERR.MSG[]", v => ErrorMessage = v?.Trim() ?? ""),

                // ═══ TORK VERİLERİ (%) ═══
                ("$TORQUE_AXIS_ACT[1]", v => Torque1 = ParseDouble(v)),
                ("$TORQUE_AXIS_ACT[2]", v => Torque2 = ParseDouble(v)),
                ("$TORQUE_AXIS_ACT[3]", v => Torque3 = ParseDouble(v)),
                ("$TORQUE_AXIS_ACT[4]", v => Torque4 = ParseDouble(v)),
                ("$TORQUE_AXIS_ACT[5]", v => Torque5 = ParseDouble(v)),
                ("$TORQUE_AXIS_ACT[6]", v => Torque6 = ParseDouble(v)),

                // ═══ HIZ VERİLERİ ═══
                ("$VEL_AXIS_ACT[1]", v => Vel1 = ParseDouble(v)),
                ("$VEL_AXIS_ACT[2]", v => Vel2 = ParseDouble(v)),
                ("$VEL_AXIS_ACT[3]", v => Vel3 = ParseDouble(v)),
                ("$VEL_AXIS_ACT[4]", v => Vel4 = ParseDouble(v)),
                ("$VEL_AXIS_ACT[5]", v => Vel5 = ParseDouble(v)),
                ("$VEL_AXIS_ACT[6]", v => Vel6 = ParseDouble(v)),
                ("$VEL_ACT", v => TcpSpeed = ParseDouble(v)),

                // ═══ ÇALIŞMA SÜRELERİ ═══
                ("$TIMER[1]", v => CycleTime = ParseDouble(v)),

                // ═══ KULLANICI DEĞİŞKENLERİ ═══
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

        #region Robot Kontrol Komutları

        /// <summary>Robot programını başlatır</summary>
        public async Task<bool> StartProgramAsync()
        {
            if (!IsConnected) return false;
            OnLog?.Invoke($"[{Name}] ▶️ START komutu gönderiliyor...");
            bool result = await WriteVariableAsync("PC_Start", "TRUE");
            if (result) OnLog?.Invoke($"[{Name}] ✅ Program başlatıldı");
            return result;
        }

        /// <summary>Robot programını durdurur</summary>
        public async Task<bool> StopProgramAsync()
        {
            if (!IsConnected) return false;
            OnLog?.Invoke($"[{Name}] ⏹️ STOP komutu gönderiliyor...");
            bool result = await WriteVariableAsync("PC_Stop", "TRUE");
            if (result) OnLog?.Invoke($"[{Name}] ✅ Program durduruldu");
            return result;
        }

        /// <summary>Robot hatalarını resetler</summary>
        public async Task<bool> ResetErrorAsync()
        {
            if (!IsConnected) return false;
            OnLog?.Invoke($"[{Name}] 🔄 RESET komutu gönderiliyor...");
            bool result = await WriteVariableAsync("PC_Reset", "TRUE");
            if (result) OnLog?.Invoke($"[{Name}] ✅ Reset tamamlandı");
            await Task.Delay(200);
            await WriteVariableAsync("PC_Reset", "FALSE");
            return result;
        }

        /// <summary>Robotu HOME pozisyonuna gönderir</summary>
        public async Task<bool> GoHomeAsync()
        {
            if (!IsConnected) return false;
            OnLog?.Invoke($"[{Name}] 🏠 HOME komutu gönderiliyor...");
            bool result = await WriteVariableAsync("PC_GoHome", "TRUE");
            if (result) OnLog?.Invoke($"[{Name}] ✅ Home pozisyonuna gidiyor");
            return result;
        }

        /// <summary>Program/Reçete numarası seçer</summary>
        public async Task<bool> SelectRecipeAsync(int recipeNo)
        {
            if (!IsConnected) return false;
            OnLog?.Invoke($"[{Name}] 📋 Reçete {recipeNo} seçiliyor...");
            bool result = await WriteVariableAsync("PC_RecipeNo", recipeNo.ToString());
            if (result) OnLog?.Invoke($"[{Name}] ✅ Reçete {recipeNo} seçildi");
            return result;
        }

        /// <summary>Program Override hızını ayarlar (%1-100)</summary>
        public async Task<bool> SetOverrideProAsync(int percent)
        {
            if (!IsConnected) return false;
            percent = Math.Clamp(percent, 1, 100);
            bool result = await WriteVariableAsync("$OV_PRO", percent.ToString());
            if (result) OverridePro = percent;
            return result;
        }

        /// <summary>JOG Override hızını ayarlar (%1-100)</summary>
        public async Task<bool> SetOverrideJogAsync(int percent)
        {
            if (!IsConnected) return false;
            percent = Math.Clamp(percent, 1, 100);
            bool result = await WriteVariableAsync("$OV_JOG", percent.ToString());
            if (result) OverrideJog = percent;
            return result;
        }

        /// <summary>JOG hareketi - Belirtilen ekseni hareket ettirir</summary>
        public async Task<bool> JogAxisAsync(int axis, int direction)
        {
            if (!IsConnected || axis < 1 || axis > 6) 
            {
                OnLog?.Invoke($"[{Name}] ❌ JOG A{axis} başarısız: Bağlı değil veya geçersiz eksen");
                return false;
            }
            string varName = $"PC_JogAxis{axis}";
            OnLog?.Invoke($"[{Name}] 🎮 JOG A{axis}: {(direction > 0 ? "+" : direction < 0 ? "-" : "STOP")} -> Değişken: {varName} = {direction}");
            bool result = await WriteVariableAsync(varName, direction.ToString());
            if (!result)
            {
                OnLog?.Invoke($"[{Name}] ❌ JOG yazma başarısız! Değişken '{varName}' robotta tanımlı olmayabilir.");
            }
            return result;
        }

        /// <summary>Tüm JOG hareketlerini durdurur</summary>
        public async Task<bool> StopJogAsync()
        {
            if (!IsConnected) return false;
            OnLog?.Invoke($"[{Name}] ⏹️ Tüm JOG hareketleri durduruluyor");
            for (int i = 1; i <= 6; i++)
                await WriteVariableAsync($"PC_JogAxis{i}", "0");
            return true;
        }

        /// <summary>Robotu Servo ON yapar</summary>
        public async Task<bool> ServoOnAsync()
        {
            if (!IsConnected) return false;
            OnLog?.Invoke($"[{Name}] ⚡ Servo ON");
            return await WriteVariableAsync("PC_ServoOn", "TRUE");
        }

        /// <summary>Robotu Servo OFF yapar</summary>
        public async Task<bool> ServoOffAsync()
        {
            if (!IsConnected) return false;
            OnLog?.Invoke($"[{Name}] ⚡ Servo OFF");
            return await WriteVariableAsync("PC_ServoOn", "FALSE");
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

        private bool _isInitialized = false;

        private KukaRobotManager()
        {
            LoadRobots();
        }

        public void Initialize(DispatcherQueue dispatcher)
        {
            UiDispatcher = dispatcher;

            // Dispatcher'ı her zaman güncelle (sayfa değişikliklerinde)
            foreach (var robot in Robots)
                robot.UiDispatcher = dispatcher;

            // İlk seferde robotları başlat, sonraki çağrılarda sadece dispatcher güncelle
            if (!_isInitialized)
            {
                _isInitialized = true;
                foreach (var robot in Robots)
                    robot.OnLog += msg => OnLog?.Invoke(msg);
                StartAll();
            }
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
