using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.IO;
using Windows.Storage.Pickers;
using App4.Utilities;

namespace App4
{
    public sealed partial class Settings_Page : Page
    {
        private bool _isPageLoaded = false;

        public Settings_Page()
        {
            InitializeComponent();
            this.Loaded += Settings_Page_Loaded;
        }

        private void Settings_Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Kayıtlı değerleri TextBox'lara yükle
            TxtPlcIp.Text = GlobalData.Plc_IpAddress;
            TxtPlcPort.Text = GlobalData.Plc_Port.ToString();
            TxtGocatorIp.Text = GlobalData.Gocator_IpAddress;
            TxtGocatorPort.Text = GlobalData.Gocator_Port.ToString();
            TxtRobotPort.Text = GlobalData.Robot_Port.ToString();

            // Robot haberleşme hızı
            SliderRobotReadSpeed.Value = GlobalData.Robot_ReadSpeed;
            TxtRobotReadSpeedValue.Text = $"{GlobalData.Robot_ReadSpeed} ms";

            // Haberleşme zamanlama ayarları
            SliderPlcReadInterval.Value = GlobalData.Plc_ReadInterval;
            TxtPlcReadInterval.Text = $"{GlobalData.Plc_ReadInterval} ms";

            SliderTriggerMonitor.Value = GlobalData.TriggerMonitor_Interval;
            TxtTriggerMonitor.Text = $"{GlobalData.TriggerMonitor_Interval} ms";

            SliderRobotTcpTimeout.Value = GlobalData.Robot_TcpTimeout;
            TxtRobotTcpTimeout.Text = $"{GlobalData.Robot_TcpTimeout} ms";

            SliderGocatorTimeout.Value = GlobalData.Gocator_RestTimeout;
            TxtGocatorTimeout.Text = $"{GlobalData.Gocator_RestTimeout} ms";

            SliderInficonRefresh.Value = GlobalData.Inficon_RefreshInterval;
            TxtInficonRefresh.Text = $"{GlobalData.Inficon_RefreshInterval} ms";

            SliderInficonTrend.Value = GlobalData.Inficon_TrendInterval;
            TxtInficonTrend.Text = $"{GlobalData.Inficon_TrendInterval} ms";

            SliderSafetyCheck.Value = GlobalData.Safety_CheckInterval;
            TxtSafetyCheck.Text = $"{GlobalData.Safety_CheckInterval} ms";

            // Artık slider değişiklikleri kaydedilebilir
            _isPageLoaded = true;
        }

        private void SliderRobotReadSpeed_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (TxtRobotReadSpeedValue != null)
                TxtRobotReadSpeedValue.Text = $"{(int)e.NewValue} ms";
            if (_isPageLoaded)
                GlobalData.Robot_ReadSpeed = (int)e.NewValue;
        }

        private void SliderPlcReadInterval_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (TxtPlcReadInterval != null)
                TxtPlcReadInterval.Text = $"{(int)e.NewValue} ms";
            if (_isPageLoaded)
                GlobalData.Plc_ReadInterval = (int)e.NewValue;
        }

        private void SliderTriggerMonitor_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (TxtTriggerMonitor != null)
                TxtTriggerMonitor.Text = $"{(int)e.NewValue} ms";
            if (_isPageLoaded)
                GlobalData.TriggerMonitor_Interval = (int)e.NewValue;
        }

        private void SliderRobotTcpTimeout_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (TxtRobotTcpTimeout != null)
                TxtRobotTcpTimeout.Text = $"{(int)e.NewValue} ms";
            if (_isPageLoaded)
                GlobalData.Robot_TcpTimeout = (int)e.NewValue;
        }

        private void SliderGocatorTimeout_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (TxtGocatorTimeout != null)
                TxtGocatorTimeout.Text = $"{(int)e.NewValue} ms";
            if (_isPageLoaded)
                GlobalData.Gocator_RestTimeout = (int)e.NewValue;
        }

        private void SliderInficonRefresh_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (TxtInficonRefresh != null)
                TxtInficonRefresh.Text = $"{(int)e.NewValue} ms";
            if (_isPageLoaded)
                GlobalData.Inficon_RefreshInterval = (int)e.NewValue;
        }

        private void SliderInficonTrend_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (TxtInficonTrend != null)
                TxtInficonTrend.Text = $"{(int)e.NewValue} ms";
            if (_isPageLoaded)
                GlobalData.Inficon_TrendInterval = (int)e.NewValue;
        }

        private void SliderSafetyCheck_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (TxtSafetyCheck != null)
                TxtSafetyCheck.Text = $"{(int)e.NewValue} ms";
            if (_isPageLoaded)
                GlobalData.Safety_CheckInterval = (int)e.NewValue;
        }

        private void BtnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // PLC
                if (!string.IsNullOrWhiteSpace(TxtPlcIp.Text))
                    GlobalData.Plc_IpAddress = TxtPlcIp.Text.Trim();
                if (int.TryParse(TxtPlcPort.Text.Trim(), out int plcPort))
                    GlobalData.Plc_Port = plcPort;

                // Gocator
                if (!string.IsNullOrWhiteSpace(TxtGocatorIp.Text))
                    GlobalData.Gocator_IpAddress = TxtGocatorIp.Text.Trim();
                if (int.TryParse(TxtGocatorPort.Text.Trim(), out int gocPort))
                    GlobalData.Gocator_Port = gocPort;

                // Robot
                if (int.TryParse(TxtRobotPort.Text.Trim(), out int robotPort))
                    GlobalData.Robot_Port = robotPort;

                // Robot haberleşme hızı
                GlobalData.Robot_ReadSpeed = (int)SliderRobotReadSpeed.Value;

                // Haberleşme zamanlama ayarları
                GlobalData.Plc_ReadInterval = (int)SliderPlcReadInterval.Value;
                GlobalData.TriggerMonitor_Interval = (int)SliderTriggerMonitor.Value;
                GlobalData.Robot_TcpTimeout = (int)SliderRobotTcpTimeout.Value;
                GlobalData.Gocator_RestTimeout = (int)SliderGocatorTimeout.Value;
                GlobalData.Inficon_RefreshInterval = (int)SliderInficonRefresh.Value;
                GlobalData.Inficon_TrendInterval = (int)SliderInficonTrend.Value;
                GlobalData.Safety_CheckInterval = (int)SliderSafetyCheck.Value;

                GlobalData.SaveAutomationSettings();

                TxtSaveStatus.Text = "✅ Ayarlar kaydedildi. Değişiklikler bir sonraki bağlantıda geçerli olacak.";
                TxtSaveStatus.Foreground = new SolidColorBrush(Microsoft.UI.Colors.LimeGreen);
            }
            catch (Exception ex)
            {
                TxtSaveStatus.Text = $"❌ Kaydetme hatası: {ex.Message}";
                TxtSaveStatus.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
            }
        }

        private void BtnResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            TxtPlcIp.Text = "192.168.251.100";
            TxtPlcPort.Text = "5007";
            TxtGocatorIp.Text = "192.168.251.30";
            TxtGocatorPort.Text = "3600";
            TxtRobotPort.Text = "7000";
            SliderRobotReadSpeed.Value = 100;

            // Zamanlama ayarları default
            SliderPlcReadInterval.Value = 50;
            SliderTriggerMonitor.Value = 500;
            SliderRobotTcpTimeout.Value = 5000;
            SliderGocatorTimeout.Value = 30000;
            SliderInficonRefresh.Value = 200;
            SliderInficonTrend.Value = 1000;
            SliderSafetyCheck.Value = 1000;

            TxtSaveStatus.Text = "Varsayılan değerler yüklendi. Kaydetmek için 'Ayarları Kaydet' butonuna basın.";
            TxtSaveStatus.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Orange);
        }

        private async void BtnExportConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnExportConfig.IsEnabled = false;
                TxtConfigStatus.Text = "⏳ Yedekleme hazırlanıyor...";
                TxtConfigStatus.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Orange);

                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string fileName = $"App4_Config_{DateTime.Now:yyyyMMdd_HHmm}.zip";
                string fullPath = Path.Combine(desktopPath, fileName);

                string result = await ConfigBackupManager.ExportConfigAsync(fullPath);

                if (!string.IsNullOrEmpty(result))
                {
                    TxtConfigStatus.Text = $"✅ Yedek alındı: Masaüstü\\{fileName}";
                    TxtConfigStatus.Foreground = new SolidColorBrush(Microsoft.UI.Colors.LimeGreen);
                }
                else
                {
                    TxtConfigStatus.Text = "❌ Yedekleme başarısız.";
                    TxtConfigStatus.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
                }
            }
            catch (Exception ex)
            {
                TxtConfigStatus.Text = $"❌ Hata: {ex.Message}";
                TxtConfigStatus.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
            }
            finally
            {
                BtnExportConfig.IsEnabled = true;
            }
        }

        private async void BtnImportConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileOpenPicker();
                picker.FileTypeFilter.Add(".zip");

                var window = (Application.Current as App)?.MainWindow;
                if (window != null)
                {
                    var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);
                }

                var file = await picker.PickSingleFileAsync();
                if (file == null) return;

                BtnImportConfig.IsEnabled = false;
                TxtConfigStatus.Text = "⏳ Ayarlar içe aktarılıyor...";
                TxtConfigStatus.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Orange);

                bool success = await ConfigBackupManager.ImportConfigAsync(file.Path);

                if (success)
                {
                    TxtConfigStatus.Text = "✅ Ayarlar başarıyla içe aktarıldı. Değişikliklerin geçerli olması için uygulamayı yeniden başlatın.";
                    TxtConfigStatus.Foreground = new SolidColorBrush(Microsoft.UI.Colors.LimeGreen);
                }
                else
                {
                    TxtConfigStatus.Text = "❌ İçe aktarma başarısız.";
                    TxtConfigStatus.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
                }
            }
            catch (Exception ex)
            {
                TxtConfigStatus.Text = $"❌ Hata: {ex.Message}";
                TxtConfigStatus.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
            }
            finally
            {
                BtnImportConfig.IsEnabled = true;
            }
        }
    }
}
