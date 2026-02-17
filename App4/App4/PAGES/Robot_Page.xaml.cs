using App4.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;
using System;

namespace App4.Pages
{
    public sealed partial class Robot_Page : Page
    {
        public ObservableCollection<PlcVariable> RobotInputVars => GlobalData.RobotInputVars;
        public ObservableCollection<PlcVariable> RobotOutputVars => GlobalData.RobotOutputVars;

        private DispatcherTimer _statusTimer;

        public Robot_Page()
        {
            this.InitializeComponent();
            
            // Load Settings to UI
            RobotIPAddressBox.Text = GlobalData.Robot_IpAddress;
            RobotPortBox.Text = GlobalData.Robot_Port.ToString();

            // Log event hook
            if (KukaService.Instance != null)
            {
                KukaService.Instance.OnLog += (msg) => {
                    this.DispatcherQueue.TryEnqueue(() => {
                        RobotLogTextBlock.Text = $"[{DateTime.Now:HH:mm:ss}] {msg}\n" + RobotLogTextBlock.Text;
                    });
                };
            }
            
            // Start Service if not started (Otomasyon baþlatýldýðýnda açýlýyor zaten ama sayfa açýldýðýnda kontrol edelim)
            try { 
                if (KukaService.Instance != null) 
                {
                    KukaService.Instance.UiDispatcher = this.DispatcherQueue;
                    KukaService.Instance.Start(); 
                }
            } catch { }

            
            // Setup Timer
            _statusTimer = new DispatcherTimer();
            _statusTimer.Interval = TimeSpan.FromSeconds(1);
            _statusTimer.Tick += StatusTimer_Tick;
            _statusTimer.Start();

            // Initial Check
            UpdateConnectionStatus();
        }

        private void StatusTimer_Tick(object sender, object e)
        {
            UpdateConnectionStatus();
        }

        private void UpdateConnectionStatus()
        {
            bool isConnected = false;
            try { 
                if (KukaService.Instance != null && KukaService.Instance.IsConnected)
                    isConnected = true; 
            } catch { }

            if (ConnectionBadge != null)
            {
                ConnectionBadge.Background = new SolidColorBrush(
                    isConnected ? Microsoft.UI.Colors.LimeGreen : Microsoft.UI.Colors.Red);
            }

            if (ConnectionText != null)
            {
                ConnectionText.Text = isConnected ? "BAÐLI" : "BAÐLANTI YOK";
                ConnectionText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
            }
            if (RobotConnectionStatusIndicator != null)
            {
                RobotConnectionStatusIndicator.Fill = new SolidColorBrush(isConnected ? Microsoft.UI.Colors.LimeGreen : Microsoft.UI.Colors.Red);
            }
            if (RobotConnectionStatusLabel != null)
            {
                RobotConnectionStatusLabel.Text = isConnected ? "BAÐLI" : "Baðlý Deðil";
                RobotConnectionStatusLabel.Foreground = new SolidColorBrush(isConnected ? Microsoft.UI.Colors.LimeGreen : Microsoft.UI.Colors.Red);
            }
        }

        private void BtnAddInput_Click(object sender, RoutedEventArgs e)
        {
            GlobalData.RobotInputVars.Add(new PlcVariable 
            { 
                Name = "Input_" + (GlobalData.RobotInputVars.Count + 1), 
                Type = "STRING", 
                PlcTag = "$IN[" + (GlobalData.RobotInputVars.Count + 1) + "]",
                Value = "0",
                IsEditable = true,
                Direction = "Input"
            });
            GlobalData.SavePlcVariableTagsToFile();
        }

        private void BtnAddOutput_Click(object sender, RoutedEventArgs e)
        {
            GlobalData.RobotOutputVars.Add(new PlcVariable 
            { 
                Name = "Output_" + (GlobalData.RobotOutputVars.Count + 1), 
                Type = "STRING", 
                PlcTag = "$OUT[" + (GlobalData.RobotOutputVars.Count + 1) + "]",
                Value = "0",
                IsEditable = true,
                Direction = "Output"
            });
            GlobalData.SavePlcVariableTagsToFile();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PlcVariable v)
            {
                if (GlobalData.RobotInputVars.Contains(v)) GlobalData.RobotInputVars.Remove(v);
                else if (GlobalData.RobotOutputVars.Contains(v)) GlobalData.RobotOutputVars.Remove(v);
                
                GlobalData.SavePlcVariableTagsToFile();
            }
        }
        
        private void Variable_Edited_LostFocus(object sender, RoutedEventArgs e)
        {
            GlobalData.SavePlcVariableTagsToFile();
        }

        private async void ValueTextBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                // x:Bind kullanýldýðýnda DataContext deðil Tag üzerinden PlcVariable alýnmalý
                if (sender is TextBox tb && tb.Tag is PlcVariable v)
                {
                    string newVal = tb.Text;
                    string tagName = v.PlcTag;
                    
                    // Geçerlilik kontrolü
                    if (string.IsNullOrWhiteSpace(tagName))
                    {
                        // Log: Tag boþ
                        return;
                    }
                    
                    // Robot'a gönder
                    bool success = await KukaService.Instance.WriteVariableAsync(tagName, newVal);
                    
                    if (success)
                    {
                        // Deðeri güncelle (UI'da görünsün)
                        v.Value = newVal;
                    }
                    
                    GlobalData.SavePlcVariableTagsToFile();
                }
            }
        }

        private void ConnectRobotBtn_Click(object sender, RoutedEventArgs e)
        {
            // Kaydet
            string ip = RobotIPAddressBox.Text;
            string portStr = RobotPortBox.Text;
            
            if (!string.IsNullOrEmpty(ip) && int.TryParse(portStr, out int p))
            {
                GlobalData.Robot_IpAddress = ip;
                
                // Servisi Yeniden Baþlat (Settings deðiþtiði için KukaService.ConnectAsync ip'yi yeniden alacak)
                GlobalData.Robot_Port = p;
                
                if (KukaService.Instance != null)
                {
                    KukaService.Instance.Stop();
                    // Kýsa bir bekleme gerekebilir ama Start hemen devreye girer
                    KukaService.Instance.Start();
                }
            }
        }
    }
}
