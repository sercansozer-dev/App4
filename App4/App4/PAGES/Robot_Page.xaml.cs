using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace App4.Pages
{
    public sealed partial class Robot_Page : Page
    {
        public Robot_Page()
        {
            InitializeComponent();
            InitializeRobotStatus();
        }

        private void InitializeRobotStatus()
        {
            RobotStatusLabel.Text = "ÇALIÞIYOR";
            CurrentStepLabel.Text = "(Adým 3 / 8)";
            
            // GOCATOR Ofset Verileri
            XOffsetLabel.Text = "+2.35mm";
            YOffsetLabel.Text = "-1.84mm";
            ZOffsetLabel.Text = "+0.12mm";
            
            OffsetStatusLabel.Text = "? Tolerans Ýçinde - Çalýþmaya Hazýr";
            ErrorWarningLabel.Text = "Hata Yok - Sistem Saðlýklý";
            SyncStatusLabel.Text = "%100 SENKRON";
        }

        private void OffsetCompensation_Click(object sender, RoutedEventArgs e)
        {
            // GOCATOR ofset verilerine göre robot konumunu ayarla
            // X, Y, Z ofsetlerini oku ve robot motorlarýna aktarým yap
        }

        private void ManualPosition_Click(object sender, RoutedEventArgs e)
        {
            // Kullanýcýdan X, Y, Z koordinatlarý iste
            // Ýlgili konuma robot hareket ettir
        }

        private void Calibration_Click(object sender, RoutedEventArgs e)
        {
            // Robot kalibrasyonu baþlat
            // Gocator sensörü kullanarak referans noktalarýný ayarla
        }

        private void PauseResume_Click(object sender, RoutedEventArgs e)
        {
            // Robot hareketini duraklat veya devam ettir
        }

        private void StartTest_Click(object sender, RoutedEventArgs e)
        {
            // Robotun baþlangýç konumuna hareket ettir
            // Adým adým iþlemleri yürüt
            // Her adýmda GOCATOR verilerini oku ve KOMPENSASYONu uygula
        }

        private void AutoRepeat_Click(object sender, RoutedEventArgs e)
        {
            // Otomatik tekrarlama baþlat
            // Belirli aralýklarla testi tekrarla
        }
    }
}
