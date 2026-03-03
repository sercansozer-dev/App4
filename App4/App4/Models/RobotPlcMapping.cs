using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using App4.Utilities;

namespace App4.Models
{
    /// <summary>Robot ↔ PLC tek satır eşleşmesi (Input veya Output)</summary>
    public class RobotPlcMapping : INotifyPropertyChanged
    {
        private string _robotName;
        public string RobotName
        {
            get => _robotName;
            set { if (_robotName != value) { _robotName = value; OnPropertyChanged(); } }
        }

        private string _robotTag;
        public string RobotTag
        {
            get => _robotTag;
            set { if (_robotTag != value) { _robotTag = value; OnPropertyChanged(); } }
        }

        private string _plcTag;
        public string PlcTag
        {
            get => _plcTag;
            set { if (_plcTag != value) { _plcTag = value; OnPropertyChanged(); } }
        }

        private string _lastValue = "-";
        [JsonIgnore]
        public string LastValue
        {
            get => _lastValue;
            set { if (_lastValue != value) { _lastValue = value; OnPropertyChanged(); } }
        }

        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive;
            set { if (_isActive != value) { _isActive = value; OnPropertyChanged(); } }
        }

        // Çift yönlü aktarım desteği: "Robot→PLC" veya "PLC→Robot"
        private string _direction = "Robot→PLC";
        public string Direction
        {
            get => _direction;
            set { if (_direction != value) { _direction = value; OnPropertyChanged(); } }
        }

        // Hangi tabloya ait: "Input" veya "Output"
        private string _tableType = "Input";
        public string TableType
        {
            get => _tableType;
            set { if (_tableType != value) { _tableType = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>Robot ↔ Robot tek satır eşleşmesi (robotlar arası sinyal aktarımı)</summary>
    public class RobotRobotMapping : INotifyPropertyChanged
    {
        private string _sourceRobotName;
        public string SourceRobotName
        {
            get => _sourceRobotName;
            set { if (_sourceRobotName != value) { _sourceRobotName = value; OnPropertyChanged(); } }
        }

        private string _sourceTag;
        public string SourceTag
        {
            get => _sourceTag;
            set { if (_sourceTag != value) { _sourceTag = value; OnPropertyChanged(); } }
        }

        private string _targetRobotName;
        public string TargetRobotName
        {
            get => _targetRobotName;
            set { if (_targetRobotName != value) { _targetRobotName = value; OnPropertyChanged(); } }
        }

        private string _targetTag;
        public string TargetTag
        {
            get => _targetTag;
            set { if (_targetTag != value) { _targetTag = value; OnPropertyChanged(); } }
        }

        private string _lastValue = "-";
        [JsonIgnore]
        public string LastValue
        {
            get => _lastValue;
            set { if (_lastValue != value) { _lastValue = value; OnPropertyChanged(); } }
        }

        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive;
            set { if (_isActive != value) { _isActive = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// İlk bağlantı handshake sırasında robota yazılacak bir değişken tanımı.
    /// Global liste: Her robotun kendi handshake seti RobotName alanıyla filtrelenir.
    /// JSON ile kalıcı saklanır (%LocalAppData%\App4\HandshakeConfig.json).
    /// </summary>
    public class HandshakeEntry : INotifyPropertyChanged
    {
        private string _robotName;
        public string RobotName
        {
            get => _robotName;
            set { if (_robotName != value) { _robotName = value; OnPropertyChanged(); } }
        }

        private string _plcTag;
        public string PlcTag
        {
            get => _plcTag;
            set { if (_plcTag != value) { _plcTag = value; OnPropertyChanged(); } }
        }

        private string _type = "INT";
        public string Type
        {
            get => _type;
            set { if (_type != value) { _type = value; OnPropertyChanged(); } }
        }

        private string _defaultValue = "0";
        public string DefaultValue
        {
            get => _defaultValue;
            set { if (_defaultValue != value) { _defaultValue = value; OnPropertyChanged(); } }
        }

        private string _lastWrittenValue = "-";
        [JsonIgnore]
        public string LastWrittenValue
        {
            get => _lastWrittenValue;
            set { if (_lastWrittenValue != value) { _lastWrittenValue = value; OnPropertyChanged(); } }
        }

        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive;
            set { if (_isActive != value) { _isActive = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
