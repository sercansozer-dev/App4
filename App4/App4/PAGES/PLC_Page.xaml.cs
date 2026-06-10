using App4.Utilities; // PlcService ve PlcVariable buradan geliyor
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input; // KeyDown eventleri için
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

            // ═══ KALICI IP/PORT YÜKLEME ═══
            // PlcService.LoadVariables() zaten app başlangıcında çağrıldı.
            // Textbox'ları kayıtlı değerlerle doldur (yoksa varsayılanlar kalır).
            if (!string.IsNullOrWhiteSpace(PlcService.Instance.PlcIpAddress))
                PLCIPAddressBox.Text = PlcService.Instance.PlcIpAddress;
            if (PlcService.Instance.PlcPort > 0)
                PLCPortBox.Text = PlcService.Instance.PlcPort.ToString();

            // IP/Port değişikliklerinde anında kaydet (odak kaybedince veya Enter'da)
            PLCIPAddressBox.LostFocus += PlcConnectionField_LostFocus;
            PLCIPAddressBox.KeyDown += PlcConnectionField_KeyDown;
            PLCPortBox.LostFocus += PlcConnectionField_LostFocus;
            PLCPortBox.KeyDown += PlcConnectionField_KeyDown;

            // Mevcut bağlantı durumunu kontrol et ve butonu güncelle
            UpdateConnectionStatus(PlcService.Instance.IsConnected);

            // Config yükleme hatası varsa göster
            if (!string.IsNullOrEmpty(PlcService.Instance.LastLoadError))
                AddLog($"[UYARI] {PlcService.Instance.LastLoadError}");

            // ═══ DEĞİŞKEN TABLOLARI GATING ═══
            // Admin (PIN 3535) giriş yapmadıysa PLC değişken + Helyum tag tablolarını gizle.
            // Event ile MainWindow'daki login değişikliklerine reaksiyon verir.
            this.Loaded += PLC_Page_AdminGating_Loaded;
            this.Unloaded += PLC_Page_AdminGating_Unloaded;
        }

        private void PLC_Page_AdminGating_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyAdminVisibility();
            GlobalData.AdminAccessChanged += OnAdminAccessChanged_Plc;
        }

        private void PLC_Page_AdminGating_Unloaded(object sender, RoutedEventArgs e)
        {
            GlobalData.AdminAccessChanged -= OnAdminAccessChanged_Plc;
        }

        private void OnAdminAccessChanged_Plc(object sender, EventArgs e)
        {
            try { DispatcherQueue.TryEnqueue(ApplyAdminVisibility); } catch { }
        }

        private void ApplyAdminVisibility()
        {
            var vis = GlobalData.IsAdminUnlocked ? Visibility.Visible : Visibility.Collapsed;
            if (MainVariablesPanel != null) MainVariablesPanel.Visibility = vis;
        }

        /// <summary>
        /// IP/Port textbox'ı değişince kalıcı olarak PLC_Config.json'a yazar.
        /// Böylece başka bilgisayara kurulunca kullanıcı bir kez girer,
        /// sonraki açılışlarda otomatik yüklenir.
        /// </summary>
        private void PersistPlcConnectionFields()
        {
            string ip = PLCIPAddressBox.Text?.Trim();
            string portStr = PLCPortBox.Text?.Trim();

            if (!string.IsNullOrWhiteSpace(ip))
                PlcService.Instance.PlcIpAddress = ip;

            if (!string.IsNullOrWhiteSpace(portStr) &&
                int.TryParse(portStr, out int port) && port > 0 && port <= 65535)
                PlcService.Instance.PlcPort = port;

            PlcService.Instance.SaveVariables();
        }

        private void PlcConnectionField_LostFocus(object sender, RoutedEventArgs e)
        {
            PersistPlcConnectionFields();
        }

        private void PlcConnectionField_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                // Binding/focus kontrolünden bağımsız doğrudan oku
                PersistPlcConnectionFields();
                this.Focus(FocusState.Programmatic);
            }
        }

        // ════════════════════════════════════════════════════════════
        // 1. VERİ EKLEME, SİLME VE KAYDETME İŞLEMLERİ (KRİTİK BÖLÜM)
        // ════════════════════════════════════════════════════════════

        // Tüm Input'ları Sil (uyarılı)
        private async void ClearAllInputs_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Tüm Input Değişkenleri Sil",
                Content = $"{InputVariables.Count} adet input değişkeni silinecek. Bu işlem geri alınamaz!",
                PrimaryButtonText = "Evet, Hepsini Sil",
                CloseButtonText = "İptal",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                InputVariables.Clear();
                PlcService.Instance.SaveVariables();
                AddLog("[INPUT] Tüm input değişkenleri silindi.");
            }
        }

        // Tüm Output'ları Sil (uyarılı)
        private async void ClearAllOutputs_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Tüm Output Değişkenleri Sil",
                Content = $"{OutputVariables.Count} adet output değişkeni silinecek. Bu işlem geri alınamaz!",
                PrimaryButtonText = "Evet, Hepsini Sil",
                CloseButtonText = "İptal",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                OutputVariables.Clear();
                PlcService.Instance.SaveVariables();
                AddLog("[OUTPUT] Tüm output değişkenleri silindi.");
            }
        }

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
                string ip = string.IsNullOrWhiteSpace(PLCIPAddressBox.Text)
                    ? PlcService.Instance.PlcIpAddress
                    : PLCIPAddressBox.Text.Trim();
                string portStr = string.IsNullOrWhiteSpace(PLCPortBox.Text)
                    ? PlcService.Instance.PlcPort.ToString()
                    : PLCPortBox.Text.Trim();

                if (!int.TryParse(portStr, out int port) || port <= 0 || port > 65535)
                {
                    AddLog($"[ERROR] Geçersiz port: '{portStr}'. 1-65535 arası olmalı.");
                    return;
                }

                // ═══ KALICI KAYIT ═══
                // Başarısız bağlantı olsa bile kullanıcının girdiği IP/Port kaydolsun,
                // tekrar uygulama açıldığında hazır olsun.
                PlcService.Instance.PlcIpAddress = ip;
                PlcService.Instance.PlcPort = port;
                PlcService.Instance.SaveVariables();

                AddLog($"[INFO] Bağlanılıyor: {ip}:{port}...");

                // UiRunner olmadan değerler tabloya yansımaz
                PlcService.Instance.Initialize(action => DispatcherQueue.TryEnqueue(() => action()));

                bool success = await PlcService.Instance.ConnectAsync(ip, port);

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

        private async void BtnImportInput_Click(object sender, RoutedEventArgs e)
            => await ImportCsvAs("Input");

        private async void BtnImportOutput_Click(object sender, RoutedEventArgs e)
            => await ImportCsvAs("Output");

        private async Task ImportCsvAs(string direction)
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
                picker.FileTypeFilter.Add(".csv");

                var window = ((App)Microsoft.UI.Xaml.Application.Current).MainWindow ?? App.m_window;
                if (window == null)
                {
                    await ShowErrorDialog("Pencere referansi bulunamadi. Uygulamayi yeniden baslatin.");
                    return;
                }
                WinRT.Interop.InitializeWithWindow.Initialize(picker,
                    WinRT.Interop.WindowNative.GetWindowHandle(window));

                var files = await picker.PickMultipleFilesAsync();
                if (files == null || files.Count == 0) return;

                TxtImportStatus.Text = $"{files.Count} CSV {direction} import ediliyor...";
                BtnImportInput.IsEnabled = false;
                BtnImportOutput.IsEnabled = false;

                var filePaths = files.Select(f => f.Path).ToList();
                int importCount = await Task.Run(() => PlcService.Instance.ImportCsvFilesToDirection(filePaths, direction));

                // Import sonrası JSON'dan tekrar yükle — UI anında güncellenir
                PlcService.Instance.LoadVariables();

                string fileNames = string.Join(", ", files.Select(f => f.Name));
                TxtImportStatus.Text = $"{importCount} {direction} degisken yuklendi ({fileNames})";
                AddLog($"[CSV_IMPORT] {fileNames} → {importCount} {direction} degisken yuklendi");
                BtnImportInput.IsEnabled = true;
                BtnImportOutput.IsEnabled = true;
            }
            catch (Exception ex)
            {
                BtnImportInput.IsEnabled = true;
                BtnImportOutput.IsEnabled = true;
                TxtImportStatus.Text = "HATA";
                AddLog($"[CSV_IMPORT] HATA: {ex.Message}");
                await ShowErrorDialog($"CSV Import Hatasi:\n{ex.Message}");
            }
        }

        private async Task ShowErrorDialog(string message)
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = "Hata",
                    Content = message,
                    CloseButtonText = "Tamam",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
            catch { }
        }

        // _logBuilder sınırsız büyüyordu -> belleği doldurabilirdi.
        // Üst sınır: 100 KB. Aşınca en eski %50'lik kısım kırpılır.
        private const int _logMaxLength = 100_000;

        private void AddLog(string message)
        {
            _logBuilder.AppendLine($"[{DateTime.Now:HH:mm:ss}] {message}");

            // Bellek sızıntısı koruması: büyüdüyse ortadan kırp
            if (_logBuilder.Length > _logMaxLength)
            {
                int trimTo = _logMaxLength / 2;
                string tail = _logBuilder.ToString(_logBuilder.Length - trimTo, trimTo);
                _logBuilder.Clear();
                _logBuilder.Append("[...eski loglar kırpıldı...]\n");
                _logBuilder.Append(tail);
            }

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

        private async void ValueTextBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            // Sadece Enter tuşuna basıldığında tetiklenir
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                var textBox = sender as TextBox;

                // TextBox'ın veri kaynağı olan PlcVariable nesnesine erişiyoruz
                var variable = textBox?.DataContext as PlcVariable;

                if (variable != null && !string.IsNullOrEmpty(textBox.Text))
                {
                    try
                    {
                        // PLC Servisindeki WriteAsync metodunu çağırıyoruz
                        await PlcService.Instance.WriteAsync(variable, textBox.Text);

                        // Opsiyonel: Yazma başarılıysa imleci kutudan çıkararak görsel geri bildirim verelim
                        // Bu sayede kullanıcının yazdığının gittiğini anlaması kolaylaşır.
                        this.Focus(FocusState.Programmatic);

                        System.Diagnostics.Debug.WriteLine($"PLC Yazıldı ({variable.Name}): {textBox.Text}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"PLC Yazma Hatası: {ex.Message}");
                    }
                }
            }
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