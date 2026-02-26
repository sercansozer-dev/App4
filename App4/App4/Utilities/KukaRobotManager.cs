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
            // ═══════════════════════════════════════════════════════════════
            // STANDART KUKA SİSTEM DEĞİŞKENLERİ (Input - Okunacak)
            // ═══════════════════════════════════════════════════════════════

            // TCP Pozisyon değişkenleri
            InputVars.Add(new PlcVariable { Name = "TCP_X", Type = "REAL", PlcTag = "$POS_ACT.X", Direction = "Input" });
            InputVars.Add(new PlcVariable { Name = "TCP_Y", Type = "REAL", PlcTag = "$POS_ACT.Y", Direction = "Input" });
            InputVars.Add(new PlcVariable { Name = "TCP_Z", Type = "REAL", PlcTag = "$POS_ACT.Z", Direction = "Input" });
            InputVars.Add(new PlcVariable { Name = "TCP_A", Type = "REAL", PlcTag = "$POS_ACT.A", Direction = "Input" });
            InputVars.Add(new PlcVariable { Name = "TCP_B", Type = "REAL", PlcTag = "$POS_ACT.B", Direction = "Input" });
            InputVars.Add(new PlcVariable { Name = "TCP_C", Type = "REAL", PlcTag = "$POS_ACT.C", Direction = "Input" });

            // Eksen açıları
            InputVars.Add(new PlcVariable { Name = "AXIS_A1", Type = "REAL", PlcTag = "$AXIS_ACT.A1", Direction = "Input" });
            InputVars.Add(new PlcVariable { Name = "AXIS_A2", Type = "REAL", PlcTag = "$AXIS_ACT.A2", Direction = "Input" });
            InputVars.Add(new PlcVariable { Name = "AXIS_A3", Type = "REAL", PlcTag = "$AXIS_ACT.A3", Direction = "Input" });
            InputVars.Add(new PlcVariable { Name = "AXIS_A4", Type = "REAL", PlcTag = "$AXIS_ACT.A4", Direction = "Input" });
            InputVars.Add(new PlcVariable { Name = "AXIS_A5", Type = "REAL", PlcTag = "$AXIS_ACT.A5", Direction = "Input" });
            InputVars.Add(new PlcVariable { Name = "AXIS_A6", Type = "REAL", PlcTag = "$AXIS_ACT.A6", Direction = "Input" });

            // Override değerleri
            InputVars.Add(new PlcVariable { Name = "OV_PRO", Type = "INT", PlcTag = "$OV_PRO", Direction = "Input" });
            InputVars.Add(new PlcVariable { Name = "OV_JOG", Type = "INT", PlcTag = "$OV_JOG", Direction = "Input" });

            // Durum değişkenleri
            InputVars.Add(new PlcVariable { Name = "ROBOT_READY", Type = "BOOL", PlcTag = "Robot_Ready", Direction = "Input" });
            InputVars.Add(new PlcVariable { Name = "ROBOT_ERROR", Type = "BOOL", PlcTag = "Robot_Error", Direction = "Input" });
            InputVars.Add(new PlcVariable { Name = "ROBOT_RUNNING", Type = "BOOL", PlcTag = "Robot_Running", Direction = "Input" });

            // ═══════════════════════════════════════════════════════════════
            // KRL GLOBAL DEĞİŞKENLER - Robot → Masaüstü (Input - Okunacak)
            // Robot yazar, uygulama okur (KukaVarProxy ile)
            // ═══════════════════════════════════════════════════════════════

            // Robot Durum Sinyalleri
            InputVars.Add(new PlcVariable { Name = "G_ROBOT_HAZIR", Type = "BOOL", PlcTag = "G_ROBOT_HAZIR", Direction = "Input", Description = "Robot hazır mı?" });
            InputVars.Add(new PlcVariable { Name = "G_IS_BITTI", Type = "BOOL", PlcTag = "G_IS_BITTI", Direction = "Input", Description = "Tüm işlem tamamlandı" });
            InputVars.Add(new PlcVariable { Name = "G_HATA_VAR", Type = "BOOL", PlcTag = "G_HATA_VAR", Direction = "Input", Description = "Hata var mı?" });
            InputVars.Add(new PlcVariable { Name = "G_HATA_KODU", Type = "INT", PlcTag = "G_HATA_KODU", Direction = "Input", Description = "Hata kodu" });
            InputVars.Add(new PlcVariable { Name = "G_ROBOT_DURUM", Type = "INT", PlcTag = "G_ROBOT_DURUM", Direction = "Input", Description = "0=Idle, 1=Çalışıyor, 2=Hata, 3=Bekliyor" });

            // İşlem İlerleme
            InputVars.Add(new PlcVariable { Name = "G_AKTIF_NOKTA", Type = "INT", PlcTag = "G_AKTIF_NOKTA", Direction = "Input", Description = "Şu an hangi noktada (1,2,3...)" });
            InputVars.Add(new PlcVariable { Name = "G_TOPLAM_NOKTA", Type = "INT", PlcTag = "G_TOPLAM_NOKTA", Direction = "Input", Description = "Toplam nokta sayısı" });

            // NOK Bildirim
            InputVars.Add(new PlcVariable { Name = "G_NOK_NOKTA", Type = "INT", PlcTag = "G_NOK_NOKTA", Direction = "Input", Description = "Hangi noktada NOK çıktı" });
            InputVars.Add(new PlcVariable { Name = "G_NOK_SAYISI", Type = "INT", PlcTag = "G_NOK_SAYISI", Direction = "Input", Description = "Toplam NOK sayısı" });
            InputVars.Add(new PlcVariable { Name = "G_NOK_BILDIRIM", Type = "BOOL", PlcTag = "G_NOK_BILDIRIM", Direction = "Input", Description = "Yeni NOK var" });

            // Ölçüm İstekleri (Robot ister, uygulama karşılar)
            InputVars.Add(new PlcVariable { Name = "G_OLCUM_TETIK", Type = "BOOL", PlcTag = "G_OLCUM_TETIK", Direction = "Input", Description = "Robot ölçüm tetik (JOB_INDEX'e göre)" });
            InputVars.Add(new PlcVariable { Name = "G_JOB_INDEX", Type = "INT", PlcTag = "G_JOB_INDEX", Direction = "Input", Description = "Gocator job index (0=tabla, 1..N=boru)" });
            InputVars.Add(new PlcVariable { Name = "G_KLIMA_ADET", Type = "INT", PlcTag = "G_KLIMA_ADET", Direction = "Input", Description = "Toplam klima tipi sayısı geri okuma" });

            // Durum Mesajı (INT kod - PC bu kodu okuyarak ekranda mesaj gösterir)
            InputVars.Add(new PlcVariable { Name = "G_DURUM_MESAJ", Type = "INT", PlcTag = "G_DURUM_MESAJ", Direction = "Input", Description = "Durum mesaj kodu (0=Bosta, 1=Baslatiliyor, 2=Tamam, 3=Hata...)" });

            // ═══ GOCATOR BORU KAYNAK OFFSET (Robot'tan geri okuma) ═══
            InputVars.Add(new PlcVariable { Name = "G_OFFSET_X_RD", Type = "REAL", PlcTag = "G_OFFSET_X", Direction = "Input", Description = "Boru offset X geri okuma" });
            InputVars.Add(new PlcVariable { Name = "G_OFFSET_Y_RD", Type = "REAL", PlcTag = "G_OFFSET_Y", Direction = "Input", Description = "Boru offset Y geri okuma" });
            InputVars.Add(new PlcVariable { Name = "G_OFFSET_Z_RD", Type = "REAL", PlcTag = "G_OFFSET_Z", Direction = "Input", Description = "Boru offset Z geri okuma" });
            InputVars.Add(new PlcVariable { Name = "G_OFFSET_A_RD", Type = "REAL", PlcTag = "G_OFFSET_A", Direction = "Input", Description = "Boru offset A geri okuma" });
            InputVars.Add(new PlcVariable { Name = "G_OFFSET_B_RD", Type = "REAL", PlcTag = "G_OFFSET_B", Direction = "Input", Description = "Boru offset B geri okuma" });
            InputVars.Add(new PlcVariable { Name = "G_OFFSET_C_RD", Type = "REAL", PlcTag = "G_OFFSET_C", Direction = "Input", Description = "Boru offset C geri okuma" });
            InputVars.Add(new PlcVariable { Name = "G_OLCUM_TAMAM_RD", Type = "BOOL", PlcTag = "G_OLCUM_TAMAM", Direction = "Input", Description = "Ölçüm tamamlandı geri okuma" });
            InputVars.Add(new PlcVariable { Name = "G_OLCUM_OK_RD", Type = "BOOL", PlcTag = "G_OLCUM_OK", Direction = "Input", Description = "Ölçüm OK/NOK geri okuma" });

            // ═══ GOCATOR TABLA OFFSET (Robot → PC) ═══
            InputVars.Add(new PlcVariable { Name = "G_TABLA_OFFSET_X", Type = "REAL", PlcTag = "G_TABLA_OFFSET_X", Direction = "Input", Description = "Tabla offset X (mm)" });
            InputVars.Add(new PlcVariable { Name = "G_TABLA_OFFSET_Y", Type = "REAL", PlcTag = "G_TABLA_OFFSET_Y", Direction = "Input", Description = "Tabla offset Y (mm)" });
            InputVars.Add(new PlcVariable { Name = "G_TABLA_OFFSET_Z", Type = "REAL", PlcTag = "G_TABLA_OFFSET_Z", Direction = "Input", Description = "Tabla offset Z (mm)" });
            InputVars.Add(new PlcVariable { Name = "G_TABLA_OFFSET_A", Type = "REAL", PlcTag = "G_TABLA_OFFSET_A", Direction = "Input", Description = "Tabla offset A (derece)" });
            InputVars.Add(new PlcVariable { Name = "G_TABLA_OFFSET_B", Type = "REAL", PlcTag = "G_TABLA_OFFSET_B", Direction = "Input", Description = "Tabla offset B (derece)" });
            InputVars.Add(new PlcVariable { Name = "G_TABLA_OFFSET_C", Type = "REAL", PlcTag = "G_TABLA_OFFSET_C", Direction = "Input", Description = "Tabla offset C (derece)" });
            InputVars.Add(new PlcVariable { Name = "G_TABLA_OFFSET_HAZIR_RD", Type = "BOOL", PlcTag = "G_TABLA_OFFSET_HAZIR", Direction = "Input", Description = "Tabla offset hazır geri okuma" });

            // ═══ KLIMA SEÇİMİ (Geri okuma) ═══
            InputVars.Add(new PlcVariable { Name = "G_KLIMA_TIP_RD", Type = "INT", PlcTag = "G_KLIMA_TIP", Direction = "Input", Description = "Klima tipi geri okuma (0=Secilmedi, 1..11)" });
            InputVars.Add(new PlcVariable { Name = "G_NOK_SAYISI_RD", Type = "INT", PlcTag = "G_NOK_SAYISI", Direction = "Input", Description = "Basarisiz nokta sayisi" });

            // ═══ SİSTEM KONTROL (Robot'tan Okunacak) ═══
            InputVars.Add(new PlcVariable { Name = "G_R1_HOME", Type = "BOOL", PlcTag = "G_R1_HOME", Direction = "Input", Description = "Robot 1 home pozisyonunda" });
            InputVars.Add(new PlcVariable { Name = "G_R2_HOME", Type = "BOOL", PlcTag = "G_R2_HOME", Direction = "Input", Description = "Robot 2 home pozisyonunda" });

            // ═══ ROBOT 2 - SNİFFER ÖLÇÜM (Robot → PC) ═══
            InputVars.Add(new PlcVariable { Name = "G_SNIFFER_OLCUM_YAP", Type = "BOOL", PlcTag = "G_SNIFFER_OLCUM_YAP", Direction = "Input", Description = "Robot 2 sniffer olcum istedi" });
            InputVars.Add(new PlcVariable { Name = "G_AKTIF_CIZGI", Type = "INT", PlcTag = "G_AKTIF_CIZGI", Direction = "Input", Description = "Robot 2 aktif sniffer cizgi no" });
            InputVars.Add(new PlcVariable { Name = "G_TOPLAM_CIZGI", Type = "INT", PlcTag = "G_TOPLAM_CIZGI", Direction = "Input", Description = "Robot 2 toplam cizgi sayisi" });
            InputVars.Add(new PlcVariable { Name = "G_NOK_CIZGI", Type = "INT", PlcTag = "G_NOK_CIZGI", Direction = "Input", Description = "Robot 2 son NOK cizgi no" });

            // ═══ ROBOT 2 - SLİDER (KL100) DURUM (Robot → PC) ═══
            InputVars.Add(new PlcVariable { Name = "G_SLIDER_HAREKET", Type = "BOOL", PlcTag = "G_SLIDER_HAREKET", Direction = "Input", Description = "Slider hareket ediyor" });
            InputVars.Add(new PlcVariable { Name = "G_SLIDER_TAMAM", Type = "BOOL", PlcTag = "G_SLIDER_TAMAM", Direction = "Input", Description = "Slider hedefe ulasti" });
            InputVars.Add(new PlcVariable { Name = "G_SLIDER_HOME", Type = "BOOL", PlcTag = "G_SLIDER_HOME", Direction = "Input", Description = "Slider home pozisyonunda" });

            // ═══ ROBOT 2 - SLİDER AKTÜEL POZİSYON ═══
            InputVars.Add(new PlcVariable { Name = "G_SLIDER_AKTUEL_POZ", Type = "REAL", PlcTag = "G_SLIDER_AKTUEL_POZ", Direction = "Input", Description = "Slider aktüel pozisyon (mm)" });

            // ═══════════════════════════════════════════════════════════════
            // STANDART KONTROL (Output - Yazılacak)
            // ═══════════════════════════════════════════════════════════════

            OutputVars.Add(new PlcVariable { Name = "PC_START", Type = "BOOL", PlcTag = "PC_Start", Direction = "Output" });
            OutputVars.Add(new PlcVariable { Name = "PC_STOP", Type = "BOOL", PlcTag = "PC_Stop", Direction = "Output" });
            OutputVars.Add(new PlcVariable { Name = "PC_RESET", Type = "BOOL", PlcTag = "PC_Reset", Direction = "Output" });
            OutputVars.Add(new PlcVariable { Name = "PC_RECIPE", Type = "INT", PlcTag = "PC_RecipeNo", Direction = "Output" });
            OutputVars.Add(new PlcVariable { Name = "SET_JOG_OV", Type = "INT", PlcTag = "$OV_JOG", Direction = "Output" });

            // ═══════════════════════════════════════════════════════════════
            // KRL GLOBAL DEĞİŞKENLER - Masaüstü → Robot (Output - Yazılacak)
            // Uygulama yazar, robot okur (KukaVarProxy ile)
            // ═══════════════════════════════════════════════════════════════

            // Kontrol Sinyalleri
            OutputVars.Add(new PlcVariable { Name = "G_BASLAT", Type = "BOOL", PlcTag = "G_BASLAT", Direction = "Output", Description = "Başla tetikle" });
            OutputVars.Add(new PlcVariable { Name = "G_RESET", Type = "BOOL", PlcTag = "G_RESET", Direction = "Output", Description = "Hata reset" });
            OutputVars.Add(new PlcVariable { Name = "G_DUR", Type = "BOOL", PlcTag = "G_DUR", Direction = "Output", Description = "Acil dur" });

            // Klima Bilgisi
            OutputVars.Add(new PlcVariable { Name = "G_KLIMA_TIP", Type = "INT", PlcTag = "G_KLIMA_TIP", Direction = "Output", Description = "1=Tip1, 2=Tip2, 3=Tip3" });
            OutputVars.Add(new PlcVariable { Name = "G_KLIMA_ID", Type = "INT", PlcTag = "G_KLIMA_ID", Direction = "Output", Description = "Kaçıncı klima" });

            // Gocator Offset (Uygulama Gocator'dan alıp buraya yazar)
            OutputVars.Add(new PlcVariable { Name = "G_OFFSET_X", Type = "REAL", PlcTag = "G_OFFSET_X", Direction = "Output", Description = "Boru X kayması (mm)" });
            OutputVars.Add(new PlcVariable { Name = "G_OFFSET_Y", Type = "REAL", PlcTag = "G_OFFSET_Y", Direction = "Output", Description = "Boru Y kayması (mm)" });
            OutputVars.Add(new PlcVariable { Name = "G_OFFSET_Z", Type = "REAL", PlcTag = "G_OFFSET_Z", Direction = "Output", Description = "Boru Z kayması (mm)" });
            OutputVars.Add(new PlcVariable { Name = "G_OFFSET_A", Type = "REAL", PlcTag = "G_OFFSET_A", Direction = "Output", Description = "Boru A dönüşü (derece)" });
            OutputVars.Add(new PlcVariable { Name = "G_OFFSET_B", Type = "REAL", PlcTag = "G_OFFSET_B", Direction = "Output", Description = "Boru B dönüşü (derece)" });
            OutputVars.Add(new PlcVariable { Name = "G_OFFSET_C", Type = "REAL", PlcTag = "G_OFFSET_C", Direction = "Output", Description = "Boru C dönüşü (derece)" });
            // Ölçüm Sonuç Sinyalleri (JOB INDEX tabanlı birleşik)
            OutputVars.Add(new PlcVariable { Name = "G_OLCUM_TAMAM", Type = "BOOL", PlcTag = "G_OLCUM_TAMAM", Direction = "Output", Description = "Ölçüm tamamlandı (pulse)" });
            OutputVars.Add(new PlcVariable { Name = "G_OLCUM_OK", Type = "BOOL", PlcTag = "G_OLCUM_OK", Direction = "Output", Description = "Ölçüm sonucu OK/NOK" });
            OutputVars.Add(new PlcVariable { Name = "G_KLIMA_ADET", Type = "INT", PlcTag = "G_KLIMA_ADET", Direction = "Output", Description = "Toplam klima tipi sayısı" });

            // ═══════════════════════════════════════════════════════════════
            // GOCATOR TABLA OFFSET - Masaüstü → Robot (Output - Yazılacak)
            // Uygulama tabla taramadan alıp buraya yazar
            // ═══════════════════════════════════════════════════════════════
            OutputVars.Add(new PlcVariable { Name = "G_TABLA_OFFSET_X", Type = "REAL", PlcTag = "G_TABLA_OFFSET_X", Direction = "Output", Description = "Tabla X kayması (mm)" });
            OutputVars.Add(new PlcVariable { Name = "G_TABLA_OFFSET_Y", Type = "REAL", PlcTag = "G_TABLA_OFFSET_Y", Direction = "Output", Description = "Tabla Y kayması (mm)" });
            OutputVars.Add(new PlcVariable { Name = "G_TABLA_OFFSET_Z", Type = "REAL", PlcTag = "G_TABLA_OFFSET_Z", Direction = "Output", Description = "Tabla Z kayması (mm)" });
            OutputVars.Add(new PlcVariable { Name = "G_TABLA_OFFSET_A", Type = "REAL", PlcTag = "G_TABLA_OFFSET_A", Direction = "Output", Description = "Tabla A dönüşü (derece)" });
            OutputVars.Add(new PlcVariable { Name = "G_TABLA_OFFSET_B", Type = "REAL", PlcTag = "G_TABLA_OFFSET_B", Direction = "Output", Description = "Tabla B dönüşü (derece)" });
            OutputVars.Add(new PlcVariable { Name = "G_TABLA_OFFSET_C", Type = "REAL", PlcTag = "G_TABLA_OFFSET_C", Direction = "Output", Description = "Tabla C dönüşü (derece)" });
            OutputVars.Add(new PlcVariable { Name = "G_TABLA_OFFSET_HAZIR", Type = "BOOL", PlcTag = "G_TABLA_OFFSET_HAZIR", Direction = "Output", Description = "Tabla offset hazır (Robot 2'ye)" });

            // ═══════════════════════════════════════════════════════════════
            // SİSTEM KONTROL - Masaüstü → Robot (Output - Yazılacak)
            // ═══════════════════════════════════════════════════════════════
            OutputVars.Add(new PlcVariable { Name = "G_SAFETY_OK", Type = "BOOL", PlcTag = "G_SAFETY_OK", Direction = "Output", Description = "Safety sinyali uygun (PLC'den robot'a)" });
            OutputVars.Add(new PlcVariable { Name = "G_SISTEM_START", Type = "BOOL", PlcTag = "G_SISTEM_START", Direction = "Output", Description = "Sistem baslatma komutu" });
            OutputVars.Add(new PlcVariable { Name = "G_SISTEM_STOP", Type = "BOOL", PlcTag = "G_SISTEM_STOP", Direction = "Output", Description = "Sistem durdurma komutu" });
            OutputVars.Add(new PlcVariable { Name = "G_OTO_MOD", Type = "BOOL", PlcTag = "G_OTO_MOD", Direction = "Output", Description = "Otomatik/Manuel mod" });

            // ═══════════════════════════════════════════════════════════════
            // ROBOT-ROBOT HABERLEŞMEDEKİ ÇAPRAZ DURUM DEĞİŞKENLERİ
            // Diğer robotun durumunu bu robota yazar (PC köprüsü ile)
            // ═══════════════════════════════════════════════════════════════
            OutputVars.Add(new PlcVariable { Name = "G_R1_IS_BITTI", Type = "BOOL", PlcTag = "G_R1_IS_BITTI", Direction = "Output", Description = "Robot 1 iş bitti (Robot-Robot)" });
            OutputVars.Add(new PlcVariable { Name = "G_R1_ROBOT_DURUM", Type = "INT", PlcTag = "G_R1_ROBOT_DURUM", Direction = "Output", Description = "Robot 1 durum kodu (Robot-Robot)" });
            OutputVars.Add(new PlcVariable { Name = "G_R1_HATA_VAR", Type = "BOOL", PlcTag = "G_R1_HATA_VAR", Direction = "Output", Description = "Robot 1 hata var (Robot-Robot)" });
            OutputVars.Add(new PlcVariable { Name = "G_R1_HATA_KODU", Type = "INT", PlcTag = "G_R1_HATA_KODU", Direction = "Output", Description = "Robot 1 hata kodu (Robot-Robot)" });
            OutputVars.Add(new PlcVariable { Name = "G_R1_EKSEN_E1", Type = "REAL", PlcTag = "G_R1_EKSEN_E1", Direction = "Output", Description = "Robot 1 harici eksen E1 (Robot-Robot)" });
            OutputVars.Add(new PlcVariable { Name = "G_R2_IS_BITTI", Type = "BOOL", PlcTag = "G_R2_IS_BITTI", Direction = "Output", Description = "Robot 2 iş bitti (Robot-Robot)" });
            OutputVars.Add(new PlcVariable { Name = "G_R2_ROBOT_DURUM", Type = "INT", PlcTag = "G_R2_ROBOT_DURUM", Direction = "Output", Description = "Robot 2 durum kodu (Robot-Robot)" });
            OutputVars.Add(new PlcVariable { Name = "G_R2_HATA_VAR", Type = "BOOL", PlcTag = "G_R2_HATA_VAR", Direction = "Output", Description = "Robot 2 hata var (Robot-Robot)" });
            OutputVars.Add(new PlcVariable { Name = "G_R2_HATA_KODU", Type = "INT", PlcTag = "G_R2_HATA_KODU", Direction = "Output", Description = "Robot 2 hata kodu (Robot-Robot)" });
            OutputVars.Add(new PlcVariable { Name = "G_R2_EKSEN_E1", Type = "REAL", PlcTag = "G_R2_EKSEN_E1", Direction = "Output", Description = "Robot 2 KL100 slider E1 (Robot-Robot)" });

            // ═══════════════════════════════════════════════════════════════
            // ROBOT 2 - SNİFFER SONUÇLARI - Masaüstü → Robot (Output)
            // ═══════════════════════════════════════════════════════════════
            OutputVars.Add(new PlcVariable { Name = "G_SNIFFER_TAMAM", Type = "BOOL", PlcTag = "G_SNIFFER_TAMAM", Direction = "Output", Description = "Sniffer olcum tamamlandi" });
            OutputVars.Add(new PlcVariable { Name = "G_SNIFFER_OK", Type = "BOOL", PlcTag = "G_SNIFFER_OK", Direction = "Output", Description = "Sniffer sonuc OK/NOK" });
            OutputVars.Add(new PlcVariable { Name = "G_SNIFFER_DEGER", Type = "REAL", PlcTag = "G_SNIFFER_DEGER", Direction = "Output", Description = "Sniffer olcum degeri" });

            // ═══════════════════════════════════════════════════════════════
            // ROBOT 2 - SLİDER KONTROL - Masaüstü → Robot (Output)
            // ═══════════════════════════════════════════════════════════════
            OutputVars.Add(new PlcVariable { Name = "G_SLIDER_HEDEF_POZ", Type = "REAL", PlcTag = "G_SLIDER_HEDEF_POZ", Direction = "Output", Description = "Slider hedef pozisyon (mm)" });
            OutputVars.Add(new PlcVariable { Name = "G_SLIDER_HAREKET", Type = "BOOL", PlcTag = "G_SLIDER_HAREKET", Direction = "Output", Description = "Slider hareket komutu" });

            // ═══════════════════════════════════════════════════════════════
            // ROBOT 2 - SLİDER KÖPRÜ (R2 → R1 bridge)
            // ═══════════════════════════════════════════════════════════════
            OutputVars.Add(new PlcVariable { Name = "G_R2_SLIDER_TAMAM", Type = "BOOL", PlcTag = "G_R2_SLIDER_TAMAM", Direction = "Output", Description = "R2 slider hedefe ulaştı (R1'e yazılır)" });
            OutputVars.Add(new PlcVariable { Name = "G_R2_SLIDER_HOME", Type = "BOOL", PlcTag = "G_R2_SLIDER_HOME", Direction = "Output", Description = "R2 slider home (R1'e yazılır)" });
            OutputVars.Add(new PlcVariable { Name = "G_R2_SLIDER_POZ", Type = "REAL", PlcTag = "G_R2_SLIDER_POZ", Direction = "Output", Description = "R2 slider aktüel poz (R1'e yazılır)" });
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

        /// <summary>Robot hatalarını resetler ve G_KLIMA_TIP değerini yeniden gönderir</summary>
        public async Task<bool> ResetErrorAsync()
        {
            if (!IsConnected) return false;
            OnLog?.Invoke($"[{Name}] 🔄 RESET komutu gönderiliyor...");

            // Reset öncesi: G_KLIMA_TIP değerini yeniden gönder
            // KRL programı reset sırasında G_KLIMA_TIP'i dahili olarak sıfırlayabilir.
            int klimaIndex = GlobalData.AktuelKlimaIndex;
            if (klimaIndex > 0)
            {
                await WriteVariableAsync("G_KLIMA_TIP", klimaIndex.ToString());
            }

            bool result = await WriteVariableAsync("PC_Reset", "TRUE");
            if (result) OnLog?.Invoke($"[{Name}] ✅ Reset tamamlandı");
            await Task.Delay(200);
            await WriteVariableAsync("PC_Reset", "FALSE");

            // Reset sonrası: G_KLIMA_TIP değerini bir kez daha gönder (KRL reset gecikmesi için)
            if (klimaIndex > 0)
            {
                await Task.Delay(300);
                await WriteVariableAsync("G_KLIMA_TIP", klimaIndex.ToString());
                OnLog?.Invoke($"[{Name}] G_KLIMA_TIP={klimaIndex} yeniden gönderildi");
            }

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
                        // load persisted robot variable definitions if present
                        LoadRobotVariables();
                        return;
                    }
                }
            }
            catch { }

            // Varsayılan robotlar
            Robots.Add(new KukaRobotInstance("ROBOT 1", "192.168.251.71", 7000));
            Robots.Add(new KukaRobotInstance("ROBOT 2", "192.168.251.72", 7000));
            SaveRobots();
            // After creating default robots, try to load any saved variable definitions
            LoadRobotVariables();
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

        // Persist robot-specific Input/Output variable definitions to disk
        private readonly string _robotVarsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "App4", "Robot_Variables.json");

        public void SaveRobotVariables()
        {
            try
            {
                var list = new List<object>();
                foreach (var r in Robots)
                {
                    object MapVars(ObservableCollection<PlcVariable> vars) => vars.Select(v => new { name = v.Name, type = v.Type, plcTag = v.PlcTag, value = v.Value, direction = v.Direction }).ToList();
                    list.Add(new { robot = r.Name, inputs = MapVars(r.InputVars), outputs = MapVars(r.OutputVars) });
                }

                var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                var dir = System.IO.Path.GetDirectoryName(_robotVarsPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                // If a previous file exists, back it up (preserve current table values before overwriting)
                try
                {
                    if (File.Exists(_robotVarsPath))
                    {
                        var backupsDir = System.IO.Path.Combine(dir, "Backups");
                        if (!Directory.Exists(backupsDir)) Directory.CreateDirectory(backupsDir);
                        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        var backupFile = System.IO.Path.Combine(backupsDir, $"Robot_Variables_{timestamp}.json");
                        File.Copy(_robotVarsPath, backupFile, true);
                    }
                }
                catch { /* ignore backup errors */ }

                File.WriteAllText(_robotVarsPath, json);
            }
            catch { }
        }

        private void LoadRobotVariables()
        {
            try
            {
                if (!File.Exists(_robotVarsPath)) return;
                var json = File.ReadAllText(_robotVarsPath);
                var doc = JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
                if (doc.ValueKind != System.Text.Json.JsonValueKind.Array) return;

                foreach (var entry in doc.EnumerateArray())
                {
                    string robotName = entry.GetProperty("robot").GetString();
                    var robot = Robots.FirstOrDefault(r => r.Name == robotName);
                    if (robot == null) continue;

                    void LoadVars(string propName, ObservableCollection<PlcVariable> target)
                    {
                        if (!entry.TryGetProperty(propName, out var arr)) return;

                        var newList = new List<PlcVariable>();

                        foreach (var v in arr.EnumerateArray())
                        {
                            string name = v.GetProperty("name").GetString();
                            string type = v.TryGetProperty("type", out var t) ? t.GetString() : "STRING";
                            string plcTag = v.TryGetProperty("plcTag", out var p) ? p.GetString() : null;
                            string value = v.TryGetProperty("value", out var val) && val.ValueKind != System.Text.Json.JsonValueKind.Null ? val.ToString() : null;

                            var nv = new PlcVariable { Name = name, Type = type, PlcTag = plcTag, Value = value, Direction = target == robot.InputVars ? "Input" : "Output" };

                            // preserve runtime CurrentValue when possible (match by PlcTag first, then Name)
                            var existing = target.FirstOrDefault(x => !string.IsNullOrEmpty(x.PlcTag) && !string.IsNullOrEmpty(nv.PlcTag) && string.Equals(x.PlcTag?.Trim(), nv.PlcTag?.Trim(), StringComparison.OrdinalIgnoreCase))
                                           ?? target.FirstOrDefault(x => string.Equals(x.Name?.Trim(), nv.Name?.Trim(), StringComparison.OrdinalIgnoreCase));
                            if (existing != null)
                            {
                                nv.CurrentValue = existing.CurrentValue;
                            }

                            newList.Add(nv);
                        }

                        // Merge: Kaydedilmiş dosyada olmayan varsayılan değişkenleri de koru
                        // (Yeni eklenen değişkenler kaybolmasın)
                        var savedNames = new HashSet<string>(
                            newList.Select(v => v.Name),
                            StringComparer.OrdinalIgnoreCase);
                        foreach (var defaultVar in target.ToList())
                        {
                            if (!savedNames.Contains(defaultVar.Name))
                                newList.Add(defaultVar);
                        }

                        target.Clear();
                        foreach (var nv in newList) target.Add(nv);
                    }

                    LoadVars("inputs", robot.InputVars);
                    LoadVars("outputs", robot.OutputVars);
                }
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
