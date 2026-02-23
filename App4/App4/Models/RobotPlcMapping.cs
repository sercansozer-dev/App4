using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using App4.Utilities;

namespace App4.Models
{
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
