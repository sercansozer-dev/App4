using App4.Utilities; // PlcService ve PlcVariable buradan geliyor
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input; // KeyDown eventleri için
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using Windows.System; // VirtualKey (Enter tuşu) için
using Windows.UI;

namespace App4.PAGES
{
    public sealed partial class PLC_Page : Page
    {
        // XAML tarafında x:Bind ile bağlanmak için bu listeleri public/property olarak tanımlıyoruz.
        // PlcService içindeki listelere referans veriyoruz.
        public ObservableCollection<PlcVariable> InputVariables => PlcService.Instance.InputVariables;
        public ObservableCollection<PlcVariable> OutputVariables => PlcService.Instance.OutputVariables;

        // Log tutmak için StringBuilder
        private StringBuilder _logBuilder = new StringBuilder();

        public PLC_Page()
        {
            this.InitializeComponent();

            // İstatistik ve IO haritası (Görsel veriler - Demodur)
            InitializeIOMapping();
            InitializeStatistics();

            // Mevcut bağlantı durumunu kontrol et ve butonu güncelle
            UpdateConnectionStatus(PlcService.Instance.IsConnected);
        }

        // ════════════════════════════════════════════════════════════
        // 1. VERİ EKLEME, SİLME VE KAYDETME İŞLEMLERİ (KRİTİK BÖLÜM)
        // ════════════════════════════════════════════════════════════

        // Yeni Input Ekle
        private void AddInputVariableBtn_Click(object sender, RoutedEventArgs e)
        {
            // Listeye yeni boş bir değişken ekle
            InputVariables.Add(new PlcVariable
            {
                Name = $"Input_{InputVariables.Count + 1}",
                Type = "WORD",
                Direction = "Input",
                CurrentValue = 0
            });

            // JSON'a Kaydet
            PlcService.Instance.SaveVariables();
        }

        // Yeni Output Ekle
        private void AddOutputVariableBtn_Click(object sender, RoutedEventArgs e)
        {
            // Listeye yeni boş bir değişken ekle
            OutputVariables.Add(new PlcVariable
            {
                Name = $"Output_{OutputVariables.Count + 1}",
                Type = "WORD",
                Direction = "Output",
                CurrentValue = 0
            });

            // JSON'a Kaydet
            PlcService.Instance.SaveVariables();
        }

        // Değişken Silme (XAML'daki Çarpı Butonu)
        private void DeleteVariable_Click(object sender, RoutedEventArgs e)
        {
            // Tıklanan butonun hangi satıra ait olduğunu bul
            if (sender is Button btn && btn.DataContext is PlcVariable variable)
            {
                // Listelerden sil
                if (InputVariables.Contains(variable))
                    InputVariables.Remove(variable);
                else if (OutputVariables.Contains(variable))
                    OutputVariables.Remove(variable);

                // Değişikliği Kaydet
                PlcService.Instance.SaveVariables();
            }
        }

        // ════════════════════════════════════════════════════════════
        // 2. OTOMATİK KAYDETME VE YAZMA (ODAKLANMA OLAYLARI)
        // ════════════════════════════════════════════════════════════

        // TextBox'tan çıkınca (Başka yere tıklayınca) ÇALIŞIR
        private void Variable_Edited_LostFocus(object sender, RoutedEventArgs e)
        {
            // TwoWay binding sayesinde veri zaten güncellendi.
            // Biz sadece dosyaya yazmayı tetikliyoruz.
            PlcService.Instance.SaveVariables();
            // System.Diagnostics.Debug.WriteLine("Veri değişti ve kaydedildi.");
        }

        // TextBox'ta bir tuşa basınca ÇALIŞIR
        private void Variable_TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            // Eğer basılan tuş ENTER ise
            if (e.Key == VirtualKey.Enter)
            {
                // Odağı TextBox'tan geçici olarak kaldırıp tekrar ver.
                // Bu işlem Binding'i (Veri eşleştirmeyi) zorla tetikler.
                if (sender is TextBox textBox)
                {
                    // Focus'u kaybettirip değişikliği işlet
                    // (Basit bir hile: IsEnabled kapat aç yapınca focus düşer ve binding tetiklenir)
                    bool wasEnabled = textBox.IsEnabled;
                    textBox.IsEnabled = false;
                    textBox.IsEnabled = wasEnabled;

                    // Dosyaya kaydet
                    PlcService.Instance.SaveVariables();
                }
            }
        }

        // Manuel "YAZ" Butonu (Outputlar için)
        private async void ForceWrite_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is PlcVariable variable)
            {
                if (PlcService.Instance.IsConnected)
                {
                    AddLog($"[YAZMA] {variable.Name} değerine {variable.CurrentValue} yazılıyor...");
                    await PlcService.Instance.WriteAsync(variable, variable.CurrentValue);
                }
                else
                {
                    AddLog($"[UYARI] PLC bağlı değil. Değer sadece hafızada değişti.");
                }

                // Her ihtimale karşı son durumu dosyaya da kaydedelim
                PlcService.Instance.SaveVariables();
            }
        }

        // ════════════════════════════════════════════════════════════
        // 3. BAĞLANTI VE LOG İŞLEMLERİ (STANDART)
        // ════════════════════════════════════════════════════════════

        private async void ConnectPLCBtn_Click(object sender, RoutedEventArgs e)
        {
            if (PlcService.Instance.IsConnected)
            {
                PlcService.Instance.Disconnect();
                AddLog("[INFO] Bağlantı kullanıcı tarafından kesildi.");
            }
            else
            {
                string ip = string.IsNullOrWhiteSpace(PLCIPAddressBox.Text) ? "192.168.251.100" : PLCIPAddressBox.Text;
                string portStr = string.IsNullOrWhiteSpace(PLCPortBox.Text) ? "5007" : PLCPortBox.Text;

                AddLog($"[INFO] Bağlanılıyor: {ip}:{portStr}...");

                bool success = await PlcService.Instance.ConnectAsync(ip, int.Parse(portStr));

                if (success) AddLog("[SUCCESS] PLC'ye başarıyla bağlanıldı!");
                else AddLog("[ERROR] Bağlantı başarısız. IP ve Portu kontrol edin.");
            }

            UpdateConnectionStatus(PlcService.Instance.IsConnected);
        }

        private void UpdateConnectionStatus(bool isConnected)
        {
            if (isConnected)
            {
                ConnectionStatusIndicator.Fill = new SolidColorBrush(Color.FromArgb(255, 76, 175, 80)); // Yeşil
                ConnectionStatusLabel.Text = "Bağlı";
                ConnectPLCBtn.Content = "🔗 Bağlantıyı Kes";
                ConnectPLCBtn.Background = new SolidColorBrush(Color.FromArgb(255, 200, 50, 50)); // Kırmızımsı
            }
            else
            {
                ConnectionStatusIndicator.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 82, 82)); // Kırmızı
                ConnectionStatusLabel.Text = "Bağlı Değil";
                ConnectPLCBtn.Content = "🔗 Bağlan";
                ConnectPLCBtn.Background = new SolidColorBrush(Color.FromArgb(255, 76, 175, 80)); // Yeşil
            }
        }

        private void ClearLogBtn_Click(object sender, RoutedEventArgs e)
        {
            _logBuilder.Clear();
            LogTextBlock.Text = "";
        }

        private void AddLog(string message)
        {
            _logBuilder.AppendLine($"[{DateTime.Now:HH:mm:ss}] {message}");

            // UI Thread kontrolü (WinUI 3)
            if (this.DispatcherQueue.HasThreadAccess)
            {
                LogTextBlock.Text = _logBuilder.ToString();
            }
            else
            {
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    LogTextBlock.Text = _logBuilder.ToString();
                });
            }
        }

        // ════════════════════════════════════════════════════════════
        // 4. GÖRSEL DEMO VERİLERİ (İSTATİSTİK TABLOLARI İÇİN)
        // ════════════════════════════════════════════════════════════
        private void InitializeStatistics()
        {
            if (TotalTestsLabel != null) TotalTestsLabel.Text = "1,247";
            if (SuccessTestsLabel != null) SuccessTestsLabel.Text = "1,186";
            if (FailedTestsLabel != null) FailedTestsLabel.Text = "61";
            if (UptimeLabel != null) UptimeLabel.Text = "47.3h";
            if (LastTestLabel != null) LastTestLabel.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            if (AvgDurationLabel != null) AvgDurationLabel.Text = "2.36 sn";
            if (TodayTestsLabel != null) TodayTestsLabel.Text = "47 Test";
            if (SystemQualityLabel != null) SystemQualityLabel.Text = "A+";
        }

        private void InitializeIOMapping()
        {
            if (IOMapContainer == null) return;
            IOMapContainer.Children.Clear();

            var mappings = new List<(string, string, string, string, string)>
            {
                ("PLC", "D0", "Robot", "Aktif", "1"),
                ("Robot", "X1", "Kamera", "Aktif", "1"),
                ("Kamera", "Result", "PLC", "Aktif", "Pass"),
                ("Sensor", "I0.0", "PLC", "Pasif", "0"),
                ("PLC", "Q0.4", "Valf", "Aktif", "1")
            };

            foreach (var mapping in mappings)
            {
                IOMapContainer.Children.Add(CreateIORow(mapping.Item1, mapping.Item2, mapping.Item3, mapping.Item4, mapping.Item5));
            }
        }

        // IO Mapping tablosu için statik satır oluşturucu (Sadece görsel amaçlı)
        private Grid CreateIORow(string source, string channel, string destination, string status, string value)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

            grid.Background = new SolidColorBrush(Color.FromArgb(255, 26, 26, 28));
            grid.Padding = new Thickness(12, 8, 12, 8);
            grid.CornerRadius = new CornerRadius(4);
            grid.Margin = new Thickness(0, 2, 0, 2);
            grid.BorderThickness = new Thickness(0, 0, 0, 1);
            grid.BorderBrush = new SolidColorBrush(Color.FromArgb(100, 0, 164, 239));

            // Hücreleri ekle
            grid.Children.Add(CreateTextCell(source, 0, Color.FromArgb(255, 0, 164, 239)));
            grid.Children.Add(CreateTextCell(channel, 1, Color.FromArgb(255, 255, 184, 28)));
            grid.Children.Add(CreateTextCell(destination, 2, Color.FromArgb(255, 0, 255, 136)));
            grid.Children.Add(CreateTextCell(status, 3, Color.FromArgb(255, 76, 175, 80)));

            var valText = CreateTextCell(value, 4, Colors.White);
            valText.TextAlignment = TextAlignment.Right;
            grid.Children.Add(valText);

            return grid;
        }

        private TextBlock CreateTextCell(string text, int col, Color color)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 10,
                Foreground = new SolidColorBrush(color),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(tb, col);
            return tb;
        }
    }
}