using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace App4.Utilities
{
    /// <summary>
    /// Robot durum kodu → mesaj referans tablosunun tek satırı. Mesaj (ve kod) uygulama içinden
    /// düzenlenebilir; GlobalData.SaveRobotStatusCodes ile RobotStatusCodes.json'a kalıcı yazılır.
    /// </summary>
    public class RobotStatusCodeEntry : INotifyPropertyChanged
    {
        private int _code;
        public int Code
        {
            get => _code;
            set { if (_code != value) { _code = value; OnPropertyChanged(); OnPropertyChanged(nameof(CodeText)); } }
        }

        /// <summary>TextBox'ta düzenlenebilmesi için kodun string hali (int↔string köprüsü).</summary>
        public string CodeText
        {
            get => _code.ToString();
            set { if (int.TryParse(value?.Trim(), out int c)) Code = c; }
        }

        private string _message;
        public string Message
        {
            get => _message;
            set { if (_message != value) { _message = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
