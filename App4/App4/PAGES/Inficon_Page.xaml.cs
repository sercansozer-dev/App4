using App4.Models;
using App4.Utilities;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace App4.PAGES
{
    public sealed partial class Inficon_Page : Page
    {
        private DispatcherTimer _refreshTimer;
        private DispatcherTimer _trendTimer;
        private bool _webViewReady;

        // Onceki leak durumu (edge detection icin)
        private bool _prev1Leak;
        private bool _prev2Leak;

        public Inficon_Page()
        {
            this.InitializeComponent();
            RefreshLogTable();
        }

        // ═══ PAGE LIFECYCLE ═══
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // WebView2 baslat
            try
            {
                var env = await CoreWebView2Environment.CreateWithOptionsAsync(null,
                    Path.Combine(Path.GetTempPath(), "App4_InficonTrend"), null);
                await TrendWebView.EnsureCoreWebView2Async(env);
                string htmlPath = Path.Combine(AppContext.BaseDirectory, "Assets", "InficonTrendChart.html");
                if (File.Exists(htmlPath))
                    TrendWebView.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
                _webViewReady = true;
            }
            catch { _webViewReady = false; }

            // PLC okuma timer (GlobalData'dan, default 200ms)
            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(GlobalData.Inficon_RefreshInterval) };
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();

            // Trend timer (GlobalData'dan, default 1000ms)
            _trendTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(GlobalData.Inficon_TrendInterval) };
            _trendTimer.Tick += TrendTimer_Tick;
            _trendTimer.Start();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_refreshTimer != null) { _refreshTimer.Stop(); _refreshTimer.Tick -= RefreshTimer_Tick; _refreshTimer = null; }
            if (_trendTimer != null) { _trendTimer.Stop(); _trendTimer.Tick -= TrendTimer_Tick; _trendTimer = null; }
        }

        // ═══ TIMER TICK — PLC OKUMA (200ms) ═══
        private void RefreshTimer_Tick(object sender, object e)
        {
            try
            {
                UpdateSnifferPanel(1,
                    Led1Ready, Led1Stable, Led1Leak, Led1Error, Led1Enable,
                    Txt1LeakRate, Txt1PE, Txt1Flow,
                    TxtSnf1Status, Snf1StatusBorder, Btn1Enable);

                UpdateSnifferPanel(2,
                    Led2Ready, Led2Stable, Led2Leak, Led2Error, Led2Enable,
                    Txt2LeakRate, Txt2PE, Txt2Flow,
                    TxtSnf2Status, Snf2StatusBorder, Btn2Enable);

                // PLC durum gostergesi
                bool plcOk = PlcService.Instance?.IsConnected == true;
                TxtPlcStatus.Text = plcOk ? "PLC: BAĞLI" : "PLC: BAĞLI DEĞİL";
                PnlPlcStatus.Background = new SolidColorBrush(plcOk ? ParseColor("#1B3A1B") : ParseColor("#3A1B1B"));
                TxtPlcStatus.Foreground = new SolidColorBrush(plcOk ? ParseColor("#4CAF50") : ParseColor("#F44336"));

                // Leak edge detection — yeni log kaydı
                CheckLeakEdge(1, ref _prev1Leak);
                CheckLeakEdge(2, ref _prev2Leak);
            }
            catch { }
        }

        private void UpdateSnifferPanel(int id,
            Border ledReady, Border ledStable, Border ledLeak, Border ledError, Border ledEnable,
            TextBlock txtLeakRate, TextBlock txtPE, TextBlock txtFlow,
            TextBlock txtStatus, Border statusBorder, ToggleButton btnEnable)
        {
            string prefix = $"INFICON{id}_";

            bool ready = GetBool(prefix + "READY");
            bool stable = GetBool(prefix + "STABLE");
            bool leak = GetBool(prefix + "LEAK");
            bool error = GetBool(prefix + "ERROR");
            bool enable = GetBool(prefix + "ENABLE");
            double leakRate = GetDouble(prefix + "LEAKRATE");
            double pe = GetDouble(prefix + "PE");
            double flow = GetDouble(prefix + "FLOW");

            // LED'ler
            SetLed(ledReady, "READY", ready, false);
            SetLed(ledStable, "STABLE", stable, false);
            SetLed(ledLeak, "LEAK", leak, true);
            SetLed(ledError, "ERROR", error, true);
            SetLed(ledEnable, "ENABLE", enable, false);

            // Degerler
            txtLeakRate.Text = leakRate > 0 ? leakRate.ToString("E2") + " mbar·l/s" : "---";
            txtLeakRate.Foreground = new SolidColorBrush(leak ? ParseColor("#F44336") : ParseColor("#4CAF50"));
            txtPE.Text = pe > 0 ? pe.ToString("F1") + " mbar" : "---";
            txtFlow.Text = flow > 0 ? flow.ToString("F0") + " sccm" : "---";

            // Status hesapla
            string status = "---";
            if (error) status = "ERROR";
            else if (!enable) status = "DISABLED";
            else if (ready && stable) status = leak ? "LEAK!" : "MEAS";
            else if (ready) status = "READY";
            else status = "INIT";

            txtStatus.Text = status;
            string statusColor = status switch
            {
                "MEAS" => "#4CAF50",
                "LEAK!" => "#F44336",
                "ERROR" => "#F44336",
                "READY" => "#2196F3",
                "DISABLED" => "#666",
                _ => "#888"
            };
            txtStatus.Foreground = new SolidColorBrush(ParseColor(statusColor));
            statusBorder.Background = new SolidColorBrush(ParseColor(status == "LEAK!" ? "#3A1B1B" : "#333"));

            // Enable toggle state
            btnEnable.IsChecked = enable;
        }

        // ═══ TREND TIMER (1s) ═══
        private void TrendTimer_Tick(object sender, object e)
        {
            if (!_webViewReady || TrendWebView?.CoreWebView2 == null) return;
            try
            {
                double lr1 = GetDouble("INFICON1_LEAKRATE");
                double lr2 = GetDouble("INFICON2_LEAKRATE");
                string time = DateTime.Now.ToString("HH:mm:ss");
                string json = $"{{\"time\":\"{time}\",\"lr1\":{lr1.ToString("E6", System.Globalization.CultureInfo.InvariantCulture)},\"lr2\":{lr2.ToString("E6", System.Globalization.CultureInfo.InvariantCulture)}}}";
                TrendWebView.CoreWebView2.PostWebMessageAsString(json);
            }
            catch { }
        }

        // ═══ LEAK EDGE DETECTION ═══
        private void CheckLeakEdge(int id, ref bool prevLeak)
        {
            bool currentLeak = GetBool($"INFICON{id}_LEAK");
            bool stable = GetBool($"INFICON{id}_STABLE");

            // Yukselen kenar: leak FALSE -> TRUE (kacak baslangiqi)
            // VEYA stable kenar: olcum tamamlandiginda log kaydi
            if (currentLeak && !prevLeak)
            {
                AddLeakLog(id, "LEAK");
            }
            else if (!currentLeak && prevLeak && stable)
            {
                // Leak temizlendi ve stable — bu OK sonucu
                AddLeakLog(id, "OK");
            }
            prevLeak = currentLeak;
        }

        private void AddLeakLog(int id, string result)
        {
            try
            {
                string prefix = $"INFICON{id}_";
                var rfidVar = GlobalData.GeneralOutputVars.FirstOrDefault(v => v.Name == "AKTUEL_RFID");
                string pipeId = rfidVar?.CurrentValue?.ToString() ?? "---";

                var entry = new InficonLeakLogEntry
                {
                    Timestamp = DateTime.Now,
                    RobotName = $"Robot {id}",
                    PipeId = pipeId,
                    Result = result,
                    LeakRate = GetDouble(prefix + "LEAKRATE"),
                    Flow = GetDouble(prefix + "FLOW"),
                    PE = GetDouble(prefix + "PE")
                };

                GlobalData.InficonLeakLogs.Insert(0, entry);

                // Max 500 kayit tut
                while (GlobalData.InficonLeakLogs.Count > 500)
                    GlobalData.InficonLeakLogs.RemoveAt(GlobalData.InficonLeakLogs.Count - 1);

                GlobalData.SaveInficonLogs();
                RefreshLogTable();
            }
            catch { }
        }

        // ═══ LOG TABLOSU GUNCELLEME ═══
        private void RefreshLogTable()
        {
            try
            {
                LogTablePanel.Children.Clear();
                TxtLogCount.Text = $"({GlobalData.InficonLeakLogs.Count} kayıt)";

                foreach (var log in GlobalData.InficonLeakLogs.Take(100))
                {
                    var row = new Grid { Padding = new Thickness(0, 2, 0, 2) };
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

                    AddCell(row, 0, log.Timestamp.ToString("dd.MM HH:mm:ss"), "#AAA");
                    AddCell(row, 1, log.RobotName, log.RobotName.Contains("1") ? "#2196F3" : "#FF9800");
                    AddCell(row, 2, log.PipeId, "#CCC");

                    string resultColor = log.Result == "LEAK" ? "#F44336" : log.Result == "OK" ? "#4CAF50" : "#888";
                    AddCell(row, 3, log.Result, resultColor);
                    AddCell(row, 4, log.LeakRate > 0 ? log.LeakRate.ToString("E2") : "---", "#CCC");
                    AddCell(row, 5, log.Flow > 0 ? log.Flow.ToString("F0") : "---", "#CCC");
                    AddCell(row, 6, log.PE > 0 ? log.PE.ToString("F1") : "---", "#CCC");

                    LogTablePanel.Children.Add(row);
                }
            }
            catch { }
        }

        private static void AddCell(Grid row, int col, string text, string color)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 10,
                FontFamily = new FontFamily("Consolas"),
                Foreground = new SolidColorBrush(ParseColor(color)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(tb, col);
            row.Children.Add(tb);
        }

        // ═══ KOMUT BUTONLARI — SNİFFER 1 ═══
        private async void Btn1Start_Click(object s, RoutedEventArgs e) => await PulsePlcBit("INFICON1_START");
        private async void Btn1Zero_Click(object s, RoutedEventArgs e) => await PulsePlcBit("INFICON1_ZERO");
        private async void Btn1Cal_Click(object s, RoutedEventArgs e) => await PulsePlcBit("INFICON1_CAL");
        private async void Btn1Reset_Click(object s, RoutedEventArgs e) => await PulsePlcBit("INFICON1_RESET");
        private async void Btn1Standby_Click(object s, RoutedEventArgs e) => await PulsePlcBit("INFICON1_STANDBY");
        private async void Btn1ErrClr_Click(object s, RoutedEventArgs e) => await PulsePlcBit("INFICON1_ERRCLEAR");
        private async void Btn1CalAbort_Click(object s, RoutedEventArgs e) => await PulsePlcBit("INFICON1_CAL_ABORT");
        private async void Btn1Enable_Click(object s, RoutedEventArgs e) => await TogglePlcBit("INFICON1_ENABLE", Btn1Enable.IsChecked == true);

        // ═══ KOMUT BUTONLARI — SNİFFER 2 ═══
        private async void Btn2Start_Click(object s, RoutedEventArgs e) => await PulsePlcBit("INFICON2_START");
        private async void Btn2Zero_Click(object s, RoutedEventArgs e) => await PulsePlcBit("INFICON2_ZERO");
        private async void Btn2Cal_Click(object s, RoutedEventArgs e) => await PulsePlcBit("INFICON2_CAL");
        private async void Btn2Reset_Click(object s, RoutedEventArgs e) => await PulsePlcBit("INFICON2_RESET");
        private async void Btn2Standby_Click(object s, RoutedEventArgs e) => await PulsePlcBit("INFICON2_STANDBY");
        private async void Btn2ErrClr_Click(object s, RoutedEventArgs e) => await PulsePlcBit("INFICON2_ERRCLEAR");
        private async void Btn2CalAbort_Click(object s, RoutedEventArgs e) => await PulsePlcBit("INFICON2_CAL_ABORT");
        private async void Btn2Enable_Click(object s, RoutedEventArgs e) => await TogglePlcBit("INFICON2_ENABLE", Btn2Enable.IsChecked == true);

        // ═══ LOG TEMİZLE ═══
        private void BtnClearLogs_Click(object s, RoutedEventArgs e)
        {
            GlobalData.InficonLeakLogs.Clear();
            GlobalData.SaveInficonLogs();
            RefreshLogTable();
        }

        // ═══ PLC YAZMA YARDIMCILARI ═══
        private async Task PulsePlcBit(string varName)
        {
            try
            {
                var plcVar = FindOutputVar(varName);
                if (plcVar == null) return;

                await PlcService.Instance.WriteAsync(plcVar, true);
                await Task.Delay(500);
                await PlcService.Instance.WriteAsync(plcVar, false);
            }
            catch { }
        }

        private async Task TogglePlcBit(string varName, bool value)
        {
            try
            {
                var plcVar = FindOutputVar(varName);
                if (plcVar == null) return;
                await PlcService.Instance.WriteAsync(plcVar, value);
            }
            catch { }
        }

        // ═══ YARDIMCI FONKSIYONLAR ═══
        private static bool GetBool(string name)
        {
            var v = GlobalData.GeneralInputVars.FirstOrDefault(x => x.Name == name)
                 ?? GlobalData.GeneralOutputVars.FirstOrDefault(x => x.Name == name);
            if (v?.CurrentValue == null) return false;
            string s = v.CurrentValue.ToString().Trim().ToUpperInvariant();
            return s == "TRUE" || s == "1";
        }

        private static double GetDouble(string name)
        {
            var v = GlobalData.GeneralInputVars.FirstOrDefault(x => x.Name == name);
            if (v?.CurrentValue == null) return 0;
            if (double.TryParse(v.CurrentValue.ToString().Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double result))
                return result;
            return 0;
        }

        private static PlcVariable FindOutputVar(string name)
            => GlobalData.GeneralOutputVars.FirstOrDefault(x => x.Name == name);

        private static void SetLed(Border led, string label, bool value, bool invertColor)
        {
            if (led == null) return;
            bool isGood = invertColor ? !value : value;
            string bgColor = isGood ? "#1B3A1B" : (value && invertColor ? "#3A1B1B" : "#2A2A2A");
            string fgColor = isGood ? "#4CAF50" : (value && invertColor ? "#F44336" : "#888");
            string text = $"{label}: {(value ? "ON" : "OFF")}";

            led.Background = new SolidColorBrush(ParseColor(bgColor));
            if (led.Child is TextBlock tb)
            {
                tb.Text = text;
                tb.Foreground = new SolidColorBrush(ParseColor(fgColor));
            }
        }

        private static Windows.UI.Color ParseColor(string hex)
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
                return Windows.UI.Color.FromArgb(255,
                    byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
                    byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
                    byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber));
            return Windows.UI.Color.FromArgb(255, 128, 128, 128);
        }
    }
}
