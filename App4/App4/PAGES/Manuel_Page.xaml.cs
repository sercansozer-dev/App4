using App4.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace App4
{
    public sealed partial class Manuel_Page : Page
    {
        private DispatcherTimer _refreshTimer;

        public Manuel_Page()
        {
            this.InitializeComponent();

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _refreshTimer.Tick += RefreshTimer_Tick;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _refreshTimer.Start();

            // R1/R2 HOME signal combos
            RefreshHomeSignalCombos();

            // KL100 station positions
            TxtKL100StBakimPos.Text = GlobalData.KL100_StationBakimPos.ToString(CultureInfo.InvariantCulture);
            TxtKL100St1Pos.Text = GlobalData.KL100_Station1Pos.ToString(CultureInfo.InvariantCulture);
            TxtKL100St2Pos.Text = GlobalData.KL100_Station2Pos.ToString(CultureInfo.InvariantCulture);
            TxtKL100St3Pos.Text = GlobalData.KL100_Station3Pos.ToString(CultureInfo.InvariantCulture);

            // Populate Klima ComboBoxes
            PopulateKlimaComboBoxes();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            _refreshTimer.Stop();
        }

        private void PopulateKlimaComboBoxes()
        {
            var rfids = GlobalData.KnownRfids;
            if (rfids == null) return;

            CmbKlima_IST1.ItemsSource = rfids;
            CmbKlima_IST2.ItemsSource = rfids;
            CmbKlima_IST3.ItemsSource = rfids;

            // Select first item by default if available
            if (rfids.Count > 0)
            {
                if (CmbKlima_IST1.SelectedIndex < 0) CmbKlima_IST1.SelectedIndex = 0;
                if (CmbKlima_IST2.SelectedIndex < 0) CmbKlima_IST2.SelectedIndex = 0;
                if (CmbKlima_IST3.SelectedIndex < 0) CmbKlima_IST3.SelectedIndex = 0;
            }

            // Update info texts
            UpdateKlimaInfoText(CmbKlima_IST1, TxtKlima_IST1_Info);
            UpdateKlimaInfoText(CmbKlima_IST2, TxtKlima_IST2_Info);
            UpdateKlimaInfoText(CmbKlima_IST3, TxtKlima_IST3_Info);

            CmbKlima_IST1.SelectionChanged += (s, e) => UpdateKlimaInfoText(CmbKlima_IST1, TxtKlima_IST1_Info);
            CmbKlima_IST2.SelectionChanged += (s, e) => UpdateKlimaInfoText(CmbKlima_IST2, TxtKlima_IST2_Info);
            CmbKlima_IST3.SelectionChanged += (s, e) => UpdateKlimaInfoText(CmbKlima_IST3, TxtKlima_IST3_Info);
        }

        private void UpdateKlimaInfoText(ComboBox cmb, TextBlock infoText)
        {
            if (cmb.SelectedItem is App4.Utilities.RfidDef rfid)
            {
                int tipIdx = GlobalData.KnownRfids.IndexOf(rfid) + 1;
                infoText.Text = $"Tip: {tipIdx} | Case: {rfid.CasingIndex}";
            }
            else
            {
                infoText.Text = "Tip: - | Case: -";
            }
        }

        private KukaRobotInstance GetRobot()
        {
            if (KukaRobotManager.Instance?.Robots != null && KukaRobotManager.Instance.Robots.Count > 0)
                return KukaRobotManager.Instance.Robots[0];
            return null;
        }

        // ================================================================
        // REFRESH TIMER
        // ================================================================
        private void RefreshTimer_Tick(object sender, object e)
        {
            // Update station LEDs
            UpdateStationLeds();

            // Update KL100 slider panel
            UpdateKL100SliderPanel();

            // Update control panel badges
            UpdateControlPanelBadges();
        }

        // ================================================================
        // CONTROL PANEL BADGES
        // ================================================================
        private void UpdateControlPanelBadges()
        {
            try
            {
                var robots = KukaRobotManager.Instance?.Robots;
                if (robots == null || robots.Count == 0) return;

                var robot = robots.FirstOrDefault(r => r.IsConnected) ?? robots[0];
                bool isConnected = robot.IsConnected;

                // System status
                if (isConnected)
                {
                    ManuelLineStatusIcon.Glyph = "\uE768";
                    ManuelLineStatusIcon.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80));
                    ManuelLineStatusText.Text = "SISTEM AKTIF";
                    ManuelLineStatusText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80));
                }
                else
                {
                    ManuelLineStatusIcon.Glyph = "\uE71A";
                    ManuelLineStatusIcon.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136));
                    ManuelLineStatusText.Text = "SISTEM KAPALI";
                    ManuelLineStatusText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136));
                }

                // Safety
                bool safetyOk = !robot.RobotError;
                if (safetyOk)
                {
                    ManuelSafetyBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 26, 42, 26));
                    ManuelSafetyIcon.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80));
                    ManuelSafetyText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80));
                }
                else
                {
                    ManuelSafetyBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 37, 40));
                    ManuelSafetyIcon.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 102, 102, 102));
                    ManuelSafetyText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 102, 102, 102));
                }

                // Alarm
                bool hasAlarm = robot.RobotError;
                if (!hasAlarm)
                {
                    ManuelAlarmBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 26, 42, 26));
                    ManuelAlarmIcon.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80));
                    ManuelAlarmText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80));
                }
                else
                {
                    ManuelAlarmBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 50, 22, 22));
                    ManuelAlarmIcon.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 244, 67, 54));
                    ManuelAlarmText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 244, 67, 54));
                }
            }
            catch { }
        }

        // ================================================================
        // STATION LED UPDATE
        // ================================================================
        private void UpdateStationLeds()
        {
            var robots = KukaRobotManager.Instance?.Robots;
            if (robots == null || robots.Count == 0) return;

            var robot = robots.FirstOrDefault(r => r.IsConnected) ?? robots[0];
            var green = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80));
            var gray = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 85, 85, 85));

            for (int st = 1; st <= 3; st++)
            {
                bool hazir = IsRobotSignalTrue(robot, $"G_IST{st}_HAZIR");
                bool isBitti = IsRobotSignalTrue(robot, $"G_IST{st}_IS_BITTI");
                bool tablaOk = IsRobotSignalTrue(robot, "G_TABLA_OLCUM_TAMAM");
                bool boruOk = IsRobotSignalTrue(robot, "G_BORU_OLCUM_TAMAM");

                // HEDEF_ISTASYON check
                var hedefVar = robot.InputVars.FirstOrDefault(v => v.Name == "G_HEDEF_ISTASYON");
                bool isHedef = false;
                if (hedefVar != null && int.TryParse(hedefVar.Value, out int hv)) isHedef = (hv == st);

                SetLed($"Led_IST{st}_Hazir", hazir, green, gray);
                SetLed($"Led_IST{st}_IsBitti", isBitti, green, gray);
                SetLed($"Led_IST{st}_HedefIst", isHedef, green, gray);
                SetLed($"Led_IST{st}_TablaOk", tablaOk, green, gray);
                SetLed($"Led_IST{st}_BoruOk", boruOk, green, gray);

                // Update value textblocks
                SetLedText($"TxtLed_IST{st}_Hazir_Val", hazir);
                SetLedText($"TxtLed_IST{st}_IsBitti_Val", isBitti);
                SetLedText($"TxtLed_IST{st}_HedefIst_Val", isHedef);
                SetLedText($"TxtLed_IST{st}_TablaOk_Val", tablaOk);
                SetLedText($"TxtLed_IST{st}_BoruOk_Val", boruOk);
            }
        }

        private void SetLed(string name, bool value, Brush greenBrush, Brush grayBrush)
        {
            if (FindName(name) is Ellipse ellipse)
            {
                ellipse.Fill = value ? greenBrush : grayBrush;
            }
        }

        private void SetLedText(string name, bool value)
        {
            if (FindName(name) is TextBlock txt)
            {
                txt.Text = value ? "TRUE" : "FALSE";
                txt.Foreground = value
                    ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80))
                    : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136));
            }
        }

        private bool IsRobotSignalTrue(KukaRobotInstance robot, string signalName)
        {
            if (robot == null || string.IsNullOrEmpty(signalName)) return false;

            var variable = robot.InputVars.FirstOrDefault(v => v.Name == signalName)
                        ?? robot.OutputVars.FirstOrDefault(v => v.Name == signalName);
            if (variable != null && variable.Value != null)
            {
                string val = variable.Value.ToUpper();
                return val == "TRUE" || val == "1" || val == "ON";
            }
            return false;
        }

        // ================================================================
        // STATION CALIS / DURDUR BUTTONS
        // ================================================================
        private async void BtnIstasyonCalis_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;
            int stNum = int.Parse(btn.Tag.ToString());

            btn.IsEnabled = false;
            try
            {
                // 1. Close other stations
                for (int i = 1; i <= 3; i++)
                {
                    if (i != stNum)
                    {
                        await WriteGlobalRobotOutVarAsync($"G_IST{i}_HAZIR", "FALSE");
                        await WriteGlobalRobotOutVarAsync($"G_IST{i}_IS_BITTI", "FALSE");
                    }
                }

                // 2. Open selected station
                await WriteGlobalRobotOutVarAsync($"G_IST{stNum}_HAZIR", "TRUE");
                await WriteGlobalRobotOutVarAsync($"G_IST{stNum}_IS_BITTI", "FALSE");

                // 3. Get klima from dropdown
                var cmb = stNum == 1 ? CmbKlima_IST1 : stNum == 2 ? CmbKlima_IST2 : CmbKlima_IST3;
                if (cmb.SelectedItem is App4.Utilities.RfidDef rfid)
                {
                    int klimaIdx = GlobalData.KnownRfids.IndexOf(rfid) + 1;
                    int caseId = rfid.CasingIndex;
                    await WriteGlobalRobotOutVarAsync("G_KLIMA_TIP", klimaIdx.ToString());
                    await WriteGlobalRobotOutVarAsync("G_CASE_ID", caseId.ToString());
                }

                // 4. Set target station (Robot 2)
                await WriteGlobalRobotOutVarAsync("G_HEDEF_ISTASYON", stNum.ToString());
                double pos = GlobalData.GetStationSliderPosition(stNum);
                await WriteGlobalRobotOutVarAsync("G_SLIDER_HEDEF_POZ", pos.ToString(CultureInfo.InvariantCulture));

                // 5. Reset TAMAM signals
                await WriteGlobalRobotOutVarAsync("G_TABLA_OLCUM_TAMAM", "FALSE");
                await WriteGlobalRobotOutVarAsync("G_BORU_OLCUM_TAMAM", "FALSE");

                // 6. Ensure OTO mode and START
                await WriteGlobalRobotOutVarAsync("G_OTO_MOD", "TRUE");
                await WriteGlobalRobotOutVarAsync("G_SISTEM_START", "TRUE");
            }
            catch { }
            finally
            {
                btn.IsEnabled = true;
            }
        }

        private async void BtnIstasyonDurdur_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;
            int stNum = int.Parse(btn.Tag.ToString());

            btn.IsEnabled = false;
            try
            {
                await WriteGlobalRobotOutVarAsync($"G_IST{stNum}_HAZIR", "FALSE");
            }
            catch { }
            finally
            {
                btn.IsEnabled = true;
            }
        }

        // ================================================================
        // CONTROL PANEL BUTTONS (BASLAT / DURDUR / RESET)
        // ================================================================
        private async void BtnManuelBaslat_Click(object sender, RoutedEventArgs e)
        {
            var robot = GetRobot();
            if (robot != null && robot.IsConnected)
            {
                await WriteGlobalRobotOutVarAsync("G_BASLAT", "TRUE");
                await Task.Delay(500);
                await WriteGlobalRobotOutVarAsync("G_BASLAT", "FALSE");
                try { await robot.StartProgramAsync(); } catch { }
            }
        }

        private async void BtnManuelDurdur_Click(object sender, RoutedEventArgs e)
        {
            await WriteGlobalRobotOutVarAsync("G_SISTEM_STOP", "TRUE");
            await Task.Delay(500);
            await WriteGlobalRobotOutVarAsync("G_SISTEM_STOP", "FALSE");

            var robot = GetRobot();
            if (robot != null && robot.IsConnected)
            {
                try { await robot.StopProgramAsync(); } catch { }
            }
        }

        private async void BtnManuelReset_Click(object sender, RoutedEventArgs e)
        {
            var robot = GetRobot();
            if (robot != null && robot.IsConnected)
            {
                await WriteGlobalRobotOutVarAsync("G_RESET", "TRUE");
                await Task.Delay(500);
                await WriteGlobalRobotOutVarAsync("G_RESET", "FALSE");
                try { await robot.ResetErrorAsync(); } catch { }
            }
        }

        // ================================================================
        // WRITE GLOBAL ROBOT OUTPUT VAR (shared across all robots + PLC)
        // ================================================================
        public async Task WriteGlobalRobotOutVarAsync(string outVarName, string valueToWrite)
        {
            var gVar = GlobalData.RobotOutputVars.FirstOrDefault(v => v.Name == outVarName);
            if (gVar != null)
            {
                gVar.Value = valueToWrite;
            }

            var plcVar = PlcService.Instance.OutputVariables.FirstOrDefault(v => v.Name == outVarName);
            if (plcVar != null)
            {
                plcVar.Value = valueToWrite;
                await PlcService.Instance.WriteAsync(plcVar, valueToWrite);
            }

            // Write to ALL robots
            var robots = KukaRobotManager.Instance?.Robots;
            if (robots != null)
            {
                foreach (var r in robots)
                {
                    if (r.IsConnected)
                    {
                        try { await r.WriteVariableAsync(outVarName, valueToWrite); } catch { }
                    }
                }
            }
        }

        // ================================================================
        // KL100 SLIDER CONTROL
        // ================================================================
        private void UpdateKL100SliderPanel()
        {
            try
            {
                var gInput = GlobalData.GeneralInputVars;
                var gOutput = GlobalData.GeneralOutputVars;

                // Is line in auto mode?
                var lineAutoVar = gInput.FirstOrDefault(v => v.Name == "LINE_AUTO_MODE");
                var lineAutoCmdVar = gOutput.FirstOrDefault(v => v.Name == "LINE_AUTO_MANUAL_CMD");
                bool isLineAuto = (lineAutoVar?.Value?.ToUpper() == "TRUE" || lineAutoVar?.Value == "1")
                               || (lineAutoCmdVar?.Value?.ToUpper() == "TRUE" || lineAutoCmdVar?.Value == "1");

                // Aktuel istasyon
                var aktuelIstVar = gInput.FirstOrDefault(v => v.Name == "AKTUEL_ISTASYON");
                string aktuelVal = aktuelIstVar?.Value ?? "0";
                if (int.TryParse(aktuelVal, out int aktuelIst) && aktuelIst >= 1 && aktuelIst <= 4)
                    KL100AktuelIstasyon.Text = aktuelIst == 4 ? "BAKIM" : $"IST {aktuelIst}";
                else
                    KL100AktuelIstasyon.Text = "---";

                // Dual robot home safety check
                bool isR1SignalConfigured = !string.IsNullOrEmpty(GlobalData.KL100_Robot1HomeSignal);
                bool isR2SignalConfigured = !string.IsNullOrEmpty(GlobalData.KL100_Robot2HomeSignal);

                bool isR1Home = isR1SignalConfigured ? IsRobotSignalTrue(0, GlobalData.KL100_Robot1HomeSignal) : true;
                bool isR2Home = isR2SignalConfigured ? IsRobotSignalTrue(1, GlobalData.KL100_Robot2HomeSignal) : true;
                bool isBothRobotsHome = isR1Home && isR2Home;

                // R1 LED
                if (isR1SignalConfigured)
                {
                    LedR1Home.Fill = new SolidColorBrush(isR1Home ? Microsoft.UI.Colors.LimeGreen : Microsoft.UI.Colors.Red);
                    TxtR1HomeStatus.Text = isR1Home ? "EVDE" : "EVDE DEGIL";
                }
                else
                {
                    LedR1Home.Fill = new SolidColorBrush(Microsoft.UI.Colors.DimGray);
                    TxtR1HomeStatus.Text = "Sinyal Yok";
                }

                // R2 LED
                if (isR2SignalConfigured)
                {
                    LedR2Home.Fill = new SolidColorBrush(isR2Home ? Microsoft.UI.Colors.LimeGreen : Microsoft.UI.Colors.Red);
                    TxtR2HomeStatus.Text = isR2Home ? "EVDE" : "EVDE DEGIL";
                }
                else
                {
                    LedR2Home.Fill = new SolidColorBrush(Microsoft.UI.Colors.DimGray);
                    TxtR2HomeStatus.Text = "Sinyal Yok";
                }

                // Hat mode display
                KL100HatModText.Text = isLineAuto ? "OTOMATIK" : "MANUEL";
                KL100HatModBorder.Background = new SolidColorBrush(
                    isLineAuto ? Windows.UI.Color.FromArgb(255, 46, 125, 50) : Windows.UI.Color.FromArgb(255, 255, 152, 0));

                // Warnings
                KL100AutoWarning.Visibility = isLineAuto ? Visibility.Visible : Visibility.Collapsed;
                KL100HomeWarning.Visibility = (!isLineAuto && !isBothRobotsHome) ? Visibility.Visible : Visibility.Collapsed;

                // Control enable: line MANUEL and BOTH robots home
                bool canControl = !isLineAuto && isBothRobotsHome;
                KL100HedefCombo.IsEnabled = canControl;
                BtnKL100HedefGit.IsEnabled = canControl;
                if (TxtKL100ManualPos != null) TxtKL100ManualPos.IsEnabled = canControl;

                // Update selected station pos display
                UpdateSelectedStationPosDisplay();

                // Update slider visual position labels
                if (ManuelSliderSt1PosLabel != null) ManuelSliderSt1PosLabel.Text = $"{GlobalData.KL100_Station1Pos:F0} mm";
                if (ManuelSliderSt2PosLabel != null) ManuelSliderSt2PosLabel.Text = $"{GlobalData.KL100_Station2Pos:F0} mm";
                if (ManuelSliderSt3PosLabel != null) ManuelSliderSt3PosLabel.Text = $"{GlobalData.KL100_Station3Pos:F0} mm";
            }
            catch { }
        }

        // Overload for the index-based robot signal check (used by KL100 panel)
        private bool IsRobotSignalTrue(int robotIndex, string signalName)
        {
            if (string.IsNullOrEmpty(signalName) || KukaRobotManager.Instance?.Robots == null || KukaRobotManager.Instance.Robots.Count <= robotIndex)
                return false;

            var robot = KukaRobotManager.Instance.Robots[robotIndex];

            // Status signals
            if (signalName.Equals("ROBOT_READY", StringComparison.OrdinalIgnoreCase)) return robot.RobotReady;
            if (signalName.Equals("ROBOT_ERROR", StringComparison.OrdinalIgnoreCase)) return robot.RobotError;
            if (signalName.Equals("ROBOT_RUNNING", StringComparison.OrdinalIgnoreCase)) return robot.RobotRunning;

            // Dynamic KRL Input / Output variable
            var variable = robot.InputVars.FirstOrDefault(v => v.Name == signalName) ?? robot.OutputVars.FirstOrDefault(v => v.Name == signalName);
            if (variable != null && variable.Value != null)
            {
                string val = variable.Value.ToUpper();
                return val == "TRUE" || val == "1" || val == "ON";
            }
            return false;
        }

        // ================================================================
        // KL100 MODE CHANGE
        // ================================================================
        private void KL100Mode_Changed(object sender, RoutedEventArgs e)
        {
            bool isStationMode = RbKL100StationMode?.IsChecked == true;
            if (KL100StationPanel != null)
                KL100StationPanel.Visibility = isStationMode ? Visibility.Visible : Visibility.Collapsed;
            if (KL100PositionPanel != null)
                KL100PositionPanel.Visibility = isStationMode ? Visibility.Collapsed : Visibility.Visible;
            if (TxtKL100GoButtonLabel != null)
                TxtKL100GoButtonLabel.Text = isStationMode ? "ISTASYONA GIT" : "POZISYONA GIT";
        }

        // ================================================================
        // STATION POSITION SAVE (TextBox LostFocus)
        // ================================================================
        private void KL100StationPos_LostFocus(object sender, RoutedEventArgs e)
        {
            var ns = NumberStyles.Any;
            var ci = CultureInfo.InvariantCulture;
            if (TxtKL100StBakimPos != null && double.TryParse(TxtKL100StBakimPos.Text, ns, ci, out double pb))
                GlobalData.KL100_StationBakimPos = pb;
            if (TxtKL100St1Pos != null && double.TryParse(TxtKL100St1Pos.Text, ns, ci, out double p1))
                GlobalData.KL100_Station1Pos = p1;
            if (TxtKL100St2Pos != null && double.TryParse(TxtKL100St2Pos.Text, ns, ci, out double p2))
                GlobalData.KL100_Station2Pos = p2;
            if (TxtKL100St3Pos != null && double.TryParse(TxtKL100St3Pos.Text, ns, ci, out double p3))
                GlobalData.KL100_Station3Pos = p3;
            GlobalData.SaveKL100StationPositions();
            GlobalData.SyncStationPosToRobots();
        }

        // ================================================================
        // SELECTED STATION POS DISPLAY
        // ================================================================
        private void UpdateSelectedStationPosDisplay()
        {
            if (KL100HedefCombo?.SelectedItem is ComboBoxItem selected && selected.Tag is string tagStr && int.TryParse(tagStr, out int st))
            {
                double pos = st switch
                {
                    1 => GlobalData.KL100_Station1Pos,
                    2 => GlobalData.KL100_Station2Pos,
                    3 => GlobalData.KL100_Station3Pos,
                    _ => 0
                };
                if (TxtKL100SelectedStationPos != null)
                    TxtKL100SelectedStationPos.Text = $"{pos:F1} mm";
            }
            else
            {
                if (TxtKL100SelectedStationPos != null)
                    TxtKL100SelectedStationPos.Text = "---";
            }
        }

        // ================================================================
        // KL100 GO BUTTON
        // ================================================================
        private async void BtnKL100HedefGit_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;

            try
            {
                double hedefPoz = 0;
                bool isStationMode = RbKL100StationMode?.IsChecked == true;

                if (isStationMode)
                {
                    if (KL100HedefCombo.SelectedItem is ComboBoxItem selected && selected.Tag is string tagStr && int.TryParse(tagStr, out int st))
                    {
                        hedefPoz = st switch
                        {
                            1 => GlobalData.KL100_Station1Pos,
                            2 => GlobalData.KL100_Station2Pos,
                            3 => GlobalData.KL100_Station3Pos,
                            _ => 0
                        };

                        var hedefIstVar = GlobalData.GeneralOutputVars.FirstOrDefault(v => v.Name == "KL100_HEDEF_ISTASYON")
                                       ?? GlobalData.GeneralInputVars.FirstOrDefault(v => v.Name == "KL100_HEDEF_ISTASYON");
                        if (hedefIstVar != null)
                        {
                            hedefIstVar.Value = st.ToString();
                            hedefIstVar.CurrentValue = st;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    if (!double.TryParse(TxtKL100ManualPos?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out hedefPoz))
                        return;
                }

                // Write target position to PLC
                var hedefPozVar = GlobalData.GeneralOutputVars.FirstOrDefault(v => v.Name == "KL100_HEDEF_POZ");
                if (hedefPozVar != null)
                {
                    hedefPozVar.Value = hedefPoz.ToString(CultureInfo.InvariantCulture);
                    hedefPozVar.CurrentValue = hedefPoz;
                }

                // Write to Robot 2: G_HEDEF_ISTASYON + G_SLIDER_HEDEF_POZ
                var robots = KukaRobotManager.Instance?.Robots;
                if (robots != null && robots.Count >= 2 && robots[1].IsConnected)
                {
                    try
                    {
                        if (isStationMode && KL100HedefCombo.SelectedItem is ComboBoxItem selItem
                            && selItem.Tag is string tStr && int.TryParse(tStr, out int stNum))
                        {
                            await robots[1].WriteVariableAsync("G_HEDEF_ISTASYON", stNum.ToString());
                            var istVar = robots[1].OutputVars.FirstOrDefault(v => v.PlcTag == "G_HEDEF_ISTASYON");
                            if (istVar != null) istVar.Value = stNum.ToString();
                        }
                        else
                        {
                            await robots[1].WriteVariableAsync("G_HEDEF_ISTASYON", "0");
                            var istVar = robots[1].OutputVars.FirstOrDefault(v => v.PlcTag == "G_HEDEF_ISTASYON");
                            if (istVar != null) istVar.Value = "0";
                        }

                        string posStr = hedefPoz.ToString(CultureInfo.InvariantCulture);
                        await robots[1].WriteVariableAsync("G_SLIDER_HEDEF_POZ", posStr);
                        var r2OutputVar = robots[1].OutputVars.FirstOrDefault(v => v.PlcTag == "G_SLIDER_HEDEF_POZ");
                        if (r2OutputVar != null) r2OutputVar.Value = posStr;
                    }
                    catch { }
                }

                // Manual slider movement command -> Robot 2: G_SLIDER_HAREKET = TRUE
                if (robots != null && robots.Count >= 2 && robots[1].IsConnected)
                {
                    try
                    {
                        await robots[1].WriteVariableAsync("G_SLIDER_HAREKET", "TRUE");
                        var harVar = robots[1].OutputVars.FirstOrDefault(v => v.PlcTag == "G_SLIDER_HAREKET");
                        if (harVar != null) harVar.Value = "TRUE";
                    }
                    catch { }
                }

                // Go command (pulse) - PLC
                var gitVar = GlobalData.GeneralOutputVars.FirstOrDefault(v => v.Name == "KL100_HEDEF_GIT");
                if (gitVar != null)
                {
                    gitVar.Value = "True";
                    gitVar.CurrentValue = true;

                    await Task.Delay(500);

                    gitVar.Value = "False";
                    gitVar.CurrentValue = false;
                }
            }
            catch { }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        /// <summary>
        /// Slider HOME button: Sends G_HEDEF_ISTASYON=0 + G_SLIDER_HAREKET=TRUE to Robot 2.
        /// Robot does dynamic HOME: A1-A6 HOME, E1 stays at current position.
        /// </summary>
        private async void BtnKL100SliderHome_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;

            try
            {
                var robots = KukaRobotManager.Instance?.Robots;
                if (robots != null && robots.Count >= 2 && robots[1].IsConnected)
                {
                    await robots[1].WriteVariableAsync("G_HEDEF_ISTASYON", "0");
                    var istVar = robots[1].OutputVars.FirstOrDefault(v => v.PlcTag == "G_HEDEF_ISTASYON");
                    if (istVar != null) istVar.Value = "0";

                    await robots[1].WriteVariableAsync("G_SLIDER_HAREKET", "TRUE");
                    var harVar = robots[1].OutputVars.FirstOrDefault(v => v.PlcTag == "G_SLIDER_HAREKET");
                    if (harVar != null) harVar.Value = "TRUE";
                }
            }
            catch { }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        // ================================================================
        // HOME SIGNAL COMBOS
        // ================================================================
        private void CmbR1HomeSignal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbR1HomeSignal.SelectedItem is string sig)
                GlobalData.KL100_Robot1HomeSignal = sig;
        }

        private void CmbR2HomeSignal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbR2HomeSignal.SelectedItem is string sig)
                GlobalData.KL100_Robot2HomeSignal = sig;
        }

        /// <summary>Populates R1/R2 Home Signal ComboBoxes with current robot signals and restores selection</summary>
        private void RefreshHomeSignalCombos()
        {
            string prevR1 = CmbR1HomeSignal.SelectedItem as string ?? GlobalData.KL100_Robot1HomeSignal;
            string prevR2 = CmbR2HomeSignal.SelectedItem as string ?? GlobalData.KL100_Robot2HomeSignal;

            var r1Signals = GlobalData.GetAvailableRobotSignals(0);
            var r2Signals = GlobalData.GetAvailableRobotSignals(1);

            CmbR1HomeSignal.ItemsSource = r1Signals;
            CmbR2HomeSignal.ItemsSource = r2Signals;

            if (!string.IsNullOrEmpty(prevR1) && r1Signals.Contains(prevR1))
                CmbR1HomeSignal.SelectedItem = prevR1;

            if (!string.IsNullOrEmpty(prevR2) && r2Signals.Contains(prevR2))
                CmbR2HomeSignal.SelectedItem = prevR2;
        }
    }
}
