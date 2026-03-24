using App4.Models;
using App4.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace App4.Pages
{
    public sealed partial class Robot_Page : Page
    {
        private DispatcherTimer _statusTimer;
        private Dictionary<KukaRobotInstance, RobotPanelControls> _robotControls = new();

        // Robot panel renkler (döngüsel kullanılacak)
        private readonly string[] _robotColors = { "#00FF88", "#FF9800", "#00A4EF", "#E91E63", "#9C27B0", "#00BCD4" };

        // Robot-PLC Köprü eşleşmeleri
        // Mapping koleksiyonları artık KukaRobotManager'da global olarak yaşıyor
        private ObservableCollection<RobotPlcMapping> _mappings => KukaRobotManager.Instance.RobotPlcMappings;
        private ObservableCollection<RobotRobotMapping> _robotRobotMappings => KukaRobotManager.Instance.RobotRobotMappings;

        // Programatik combo değişikliklerinde save tetiklenmesini engeller
        private bool _suppressSave = false;

        public Robot_Page()
        {
            this.InitializeComponent();

            // KukaRobotManager'ı initialize et
            KukaRobotManager.Instance.Initialize(this.DispatcherQueue);

            // Robot panellerini oluştur
            RefreshRobotPanels();

            // Robot listesi değiştiğinde panelleri yenile
            KukaRobotManager.Instance.Robots.CollectionChanged += (s, e) => RefreshRobotPanels();

            // Köprü eşleşmelerini yükle
            LoadMappings();
            RefreshBridgeTables();

            // Robot-Robot eşleşmelerini yükle
            LoadRobotRobotMappings();
            RefreshRobotRobotRows();

            // Handshake değişkenleri tablosunu yenile
            RefreshHandshakeRows();

            // Setup Timer
            _statusTimer = new DispatcherTimer();
            _statusTimer.Interval = TimeSpan.FromSeconds(1);
            _statusTimer.Tick += StatusTimer_Tick;
            _statusTimer.Start();
        }

        private void RefreshRobotPanels()
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                RobotPanelsContainer.Children.Clear();
                _robotControls.Clear();
                
                int robotCount = KukaRobotManager.Instance.Robots.Count;
                RobotCountText.Text = $"{robotCount} Robot";

                int index = 0;
                foreach (var robot in KukaRobotManager.Instance.Robots)
                {
                    robot.UiDispatcher = this.DispatcherQueue;
                    string color = _robotColors[index % _robotColors.Length];
                    var panel = CreateRobotPanel(robot, index + 1, color);
                    RobotPanelsContainer.Children.Add(panel);
                    
                    // Log handler
                    robot.OnLog += (msg) => LogMessage($"[R{index + 1}] {msg}");
                    index++;
                }
            });
        }

        private Border CreateRobotPanel(KukaRobotInstance robot, int robotNumber, string color)
        {
            var borderColor = ParseColor(color);
            
            var panel = new Border
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 26, 26, 28)),
                CornerRadius = new CornerRadius(10),
                BorderThickness = new Thickness(2),
                BorderBrush = new SolidColorBrush(borderColor),
                Margin = new Thickness(0, 0, 0, 0)
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) }); // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });    // Tablolar

            // Header
            var header = CreateRobotHeader(robot, robotNumber, color);
            Grid.SetRow(header, 0);
            mainGrid.Children.Add(header);

            // Tablolar
            var tables = CreateRobotTables(robot, robotNumber, color);
            Grid.SetRow(tables, 1);
            mainGrid.Children.Add(tables);

            panel.Child = mainGrid;

            // Status update için kontrolleri sakla
            robot.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(robot.IsConnected) && _robotControls.TryGetValue(robot, out var ctrl))
                {
                    this.DispatcherQueue.TryEnqueue(() => UpdateRobotStatus(robot, ctrl, color));
                }
            };

            return panel;
        }

        private Border CreateRobotHeader(KukaRobotInstance robot, int robotNumber, string color)
        {
            var borderColor = ParseColor(color);
            
            var header = new Border
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 37, 38)),
                CornerRadius = new CornerRadius(8, 8, 0, 0),
                Padding = new Thickness(12, 0, 12, 0),
                BorderThickness = new Thickness(0, 0, 0, 2),
                BorderBrush = new SolidColorBrush(borderColor)
            };

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Robot adı
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // IP/Port
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Kaydet
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Status
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // PRO/JOG
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Sil

            // Robot adı + LED
            var namePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
            var statusLed = new Ellipse { Width = 14, Height = 14, Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 82, 82)) };
            namePanel.Children.Add(statusLed);
            namePanel.Children.Add(new TextBlock { Text = $"🤖 ROBOT {robotNumber}", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Microsoft.UI.Colors.White) });
            Grid.SetColumn(namePanel, 0);
            headerGrid.Children.Add(namePanel);

            // IP/Port
            var ipPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(20, 0, 0, 0) };
            ipPanel.Children.Add(new TextBlock { Text = "IP:", FontSize = 10, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136)), VerticalAlignment = VerticalAlignment.Center });
            var ipBox = new TextBox { Text = robot.IpAddress, Width = 120, Height = 26, FontSize = 10, Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 45, 45, 48)), Foreground = new SolidColorBrush(Microsoft.UI.Colors.White), BorderBrush = new SolidColorBrush(borderColor) };
            ipPanel.Children.Add(ipBox);
            ipPanel.Children.Add(new TextBlock { Text = "Port:", FontSize = 10, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) });
            var portBox = new TextBox { Text = robot.Port.ToString(), Width = 60, Height = 26, FontSize = 10, Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 45, 45, 48)), Foreground = new SolidColorBrush(Microsoft.UI.Colors.White), BorderBrush = new SolidColorBrush(borderColor) };
            ipPanel.Children.Add(portBox);
            Grid.SetColumn(ipPanel, 1);
            headerGrid.Children.Add(ipPanel);

            // Kaydet butonu
            var saveBtn = new Button { Content = "💾 Kaydet", Background = new SolidColorBrush(borderColor), Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0)), CornerRadius = new CornerRadius(4), Height = 26, FontWeight = Microsoft.UI.Text.FontWeights.Bold, FontSize = 9, Padding = new Thickness(10, 0, 10, 0), Margin = new Thickness(10, 0, 0, 0) };
            saveBtn.Click += (s, e) =>
            {
                string ip = ipBox.Text;
                int.TryParse(portBox.Text, out int port);
                if (port == 0) port = 7000;
                KukaRobotManager.Instance.UpdateRobotIp(robot, ip, port);
                LogMessage($"[R{robotNumber}] IP güncellendi: {ip}:{port}");
            };
            Grid.SetColumn(saveBtn, 2);
            headerGrid.Children.Add(saveBtn);

            // Status badge
            var statusBadge = new Border { Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 82, 82)), CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 2, 8, 2), Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            var statusText = new TextBlock { Text = "BAĞLI DEĞİL", FontSize = 9, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Microsoft.UI.Colors.White) };
            statusBadge.Child = statusText;
            Grid.SetColumn(statusBadge, 3);
            headerGrid.Children.Add(statusBadge);

            // PRO/JOG
            var overridePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 15, VerticalAlignment = VerticalAlignment.Center };
            var proStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 3 };
            proStack.Children.Add(new TextBlock { Text = "PRO:", FontSize = 10, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136)) });
            var proText = new TextBlock { Text = "100", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 170, 255)) };
            proStack.Children.Add(proText);
            proStack.Children.Add(new TextBlock { Text = "%", FontSize = 10, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136)) });
            overridePanel.Children.Add(proStack);

            var jogStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 3 };
            jogStack.Children.Add(new TextBlock { Text = "JOG:", FontSize = 10, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136)) });
            var jogText = new TextBlock { Text = "100", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 170, 0)) };
            jogStack.Children.Add(jogText);
            jogStack.Children.Add(new TextBlock { Text = "%", FontSize = 10, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136)) });
            overridePanel.Children.Add(jogStack);
            Grid.SetColumn(overridePanel, 5);
            headerGrid.Children.Add(overridePanel);

            // Sil butonu
            var deleteBtn = new Button { Content = "🗑️", Background = new SolidColorBrush(Colors.Transparent), Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 82, 82)), FontSize = 14, Padding = new Thickness(5), Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            deleteBtn.Click += async (s, e) =>
            {
                var dialog = new ContentDialog
                {
                    Title = "Robot Sil",
                    Content = $"Robot {robotNumber} silinecek. Emin misiniz?",
                    PrimaryButtonText = "Sil",
                    CloseButtonText = "İptal",
                    XamlRoot = this.XamlRoot,
                    DefaultButton = ContentDialogButton.Close
                };
                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    KukaRobotManager.Instance.RemoveRobot(robot);
                    LogMessage($"Robot {robotNumber} silindi");
                }
            };
            Grid.SetColumn(deleteBtn, 6);
            headerGrid.Children.Add(deleteBtn);

            header.Child = headerGrid;

            // Kontrolleri sakla
            _robotControls[robot] = new RobotPanelControls
            {
                StatusLed = statusLed,
                StatusBadge = statusBadge,
                StatusText = statusText,
                ProText = proText,
                JogText = jogText
            };

            return header;
        }

        private Grid CreateRobotTables(KukaRobotInstance robot, int robotNumber, string color)
        {
            var tablesGrid = new Grid { Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 13, 13, 13)), Padding = new Thickness(8) };
            tablesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tablesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // INPUT Table
            var inputTable = CreateVariableTable(robot, robotNumber, "📥 INPUT (OKUNAN VERİLER)", "#4CAF50", "#1A4D2E", true);
            Grid.SetColumn(inputTable, 0);
            inputTable.Margin = new Thickness(0, 0, 4, 0);
            tablesGrid.Children.Add(inputTable);

            // OUTPUT Table
            var outputTable = CreateVariableTable(robot, robotNumber, "📤 OUTPUT (YAZILAN VERİLER)", "#FF9800", "#4D3A1A", false);
            Grid.SetColumn(outputTable, 1);
            outputTable.Margin = new Thickness(4, 0, 0, 0);
            tablesGrid.Children.Add(outputTable);

            return tablesGrid;
        }

        private Border CreateVariableTable(KukaRobotInstance robot, int robotNumber, string title, string accentColor, string headerBg, bool isInput)
        {
            var accent = ParseColor(accentColor);
            var headerBackground = ParseColor(headerBg);

            var tableBorder = new Border
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 13, 13, 13)),
                CornerRadius = new CornerRadius(6)
            };

            var tableGrid = new Grid();
            tableGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) }); // Header
            tableGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(24) }); // Column headers
            tableGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(200) }); // List

            // Header
            var headerBorder = new Border { Background = new SolidColorBrush(headerBackground), CornerRadius = new CornerRadius(6, 6, 0, 0), Padding = new Thickness(8), BorderThickness = new Thickness(1), BorderBrush = new SolidColorBrush(accent) };
            var headerGrid = new Grid();
            headerGrid.Children.Add(new TextBlock { Text = title, FontSize = 10, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(accent), VerticalAlignment = VerticalAlignment.Center });
            
            var addBtn = new Button { Content = "➕", Background = new SolidColorBrush(accent), Foreground = new SolidColorBrush(Microsoft.UI.Colors.White), CornerRadius = new CornerRadius(3), Height = 18, Width = 22, Padding = new Thickness(0), FontSize = 9, HorizontalAlignment = HorizontalAlignment.Right };
            addBtn.Click += (s, e) =>
            {
                var collection = isInput ? robot.InputVars : robot.OutputVars;
                collection.Add(new PlcVariable
                {
                    Name = (isInput ? "Input_" : "Output_") + (collection.Count + 1),
                    Type = "STRING",
                    PlcTag = isInput ? "$IN[" + (collection.Count + 1) + "]" : "$OUT[" + (collection.Count + 1) + "]",
                    Value = "0",
                    IsEditable = true,
                    Direction = isInput ? "Input" : "Output"
                });
                try { KukaRobotManager.Instance.SaveRobotVariables(); } catch { }
            };
            headerGrid.Children.Add(addBtn);
            headerBorder.Child = headerGrid;
            Grid.SetRow(headerBorder, 0);
            tableGrid.Children.Add(headerBorder);

            // Column headers
            var colHeaderBorder = new Border { Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 26, 26, 28)), Padding = new Thickness(4, 2, 4, 2) };
            var colHeaderGrid = new Grid();
            colHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(25) });
            colHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            colHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            colHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            colHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            colHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(45) });
            colHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(25) });

            var headers = new[] { "#", "İsim", "Tip", "KRL Tag", "Değer", "Sıra", "Sil" };
            for (int i = 0; i < headers.Length; i++)
            {
                var tb = new TextBlock { Text = headers[i], FontSize = 7, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 85, 85, 85)), FontWeight = Microsoft.UI.Text.FontWeights.Bold, TextAlignment = i == 1 ? TextAlignment.Left : TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(tb, i);
                colHeaderGrid.Children.Add(tb);
            }
            colHeaderBorder.Child = colHeaderGrid;
            Grid.SetRow(colHeaderBorder, 1);
            tableGrid.Children.Add(colHeaderBorder);

            // ListView
            var listView = new ListView
            {
                ItemsSource = isInput ? robot.InputVars : robot.OutputVars,
                ItemTemplate = (DataTemplate)this.Resources[isInput ? "RobotInputVariableTemplate" : "RobotOutputVariableTemplate"],
                SelectionMode = ListViewSelectionMode.None,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 13, 13, 13))
            };
            Grid.SetRow(listView, 2);
            tableGrid.Children.Add(listView);

            tableBorder.Child = tableGrid;
            return tableBorder;
        }

        private void UpdateRobotStatus(KukaRobotInstance robot, RobotPanelControls ctrl, string color)
        {
            bool connected = robot.IsConnected;
            var connectedColor = Windows.UI.Color.FromArgb(255, 0, 255, 136);
            var disconnectedColor = Windows.UI.Color.FromArgb(255, 255, 82, 82);

            ctrl.StatusLed.Fill = new SolidColorBrush(connected ? connectedColor : disconnectedColor);
            ctrl.StatusBadge.Background = new SolidColorBrush(connected ? connectedColor : disconnectedColor);
            ctrl.StatusText.Text = connected ? "BAĞLI" : "BAĞLI DEĞİL";
            ctrl.StatusText.Foreground = new SolidColorBrush(connected ? Windows.UI.Color.FromArgb(255, 0, 0, 0) : Microsoft.UI.Colors.White);
            ctrl.ProText.Text = robot.OverridePro.ToString();
            ctrl.JogText.Text = robot.OverrideJog.ToString();
        }

        private void StatusTimer_Tick(object sender, object e)
        {
            foreach (var kvp in _robotControls)
            {
                var robot = kvp.Key;
                var ctrl = kvp.Value;
                int index = KukaRobotManager.Instance.Robots.IndexOf(robot);
                string color = index >= 0 ? _robotColors[index % _robotColors.Length] : "#00FF88";
                UpdateRobotStatus(robot, ctrl, color);
            }

            // Köprü eşleşmeleri artık KukaRobotManager'da global olarak işleniyor
            // Burada sadece UI'deki LastValue gösterimlerini güncellemek yeterli

            // Referans tablosundaki canlı sinyalleri güncelle
            UpdateRefLiveSignals();

            // Safety sinyalleri tablosunu güncelle
            UpdateSafetySignalsTable();
        }

        // ═══ REFERANS TABLOSU CANLI SİNYAL GÜNCELLEMESİ ═══
        private void UpdateRefLiveSignals()
        {
            try
            {
                var robots = KukaRobotManager.Instance.Robots;

                // Robot 1
                if (robots.Count > 0)
                {
                    var r1 = robots[0];
                    string GetVar(string name) => r1.InputVars.FirstOrDefault(v => v.Name == name)?.Value ?? "---";

                    string r1Durum = GetVar("G_ROBOT_DURUM");
                    string r1Mesaj = GetVar("G_DURUM_MESAJ");
                    string r1HataVar = GetVar("G_HATA_VAR");
                    string r1HataKodu = GetVar("G_HATA_KODU");

                    int.TryParse(r1Durum, out int rd1);
                    bool hata1 = r1HataVar?.ToUpper() == "TRUE" || r1HataVar == "1";

                    // Durum rengi
                    string durumColor1 = rd1 switch { 0 => "#4CAF50", 1 => "#FF9800", 2 => "#F44336", _ => "#888" };
                    string dotColor1 = !r1.IsConnected ? "#555" : (hata1 || rd1 == 2) ? "#F44336" : (rd1 >= 1) ? "#FF9800" : "#4CAF50";
                    string hataColor1 = (hata1 || r1HataKodu != "0" && r1HataKodu != "---") ? "#F44336" : "#4CAF50";

                    SetRefText(RefR1RobotDurum, $"{r1Durum} ({DecodeDurum(rd1)})", durumColor1);
                    SetRefText(RefR1DurumMesaj, r1Mesaj, "#888");
                    SetRefText(RefR1HataVar, r1HataVar, hataColor1);
                    SetRefText(RefR1HataKodu, r1HataKodu, hataColor1);
                    SetRefDot(RefR1StatusDot, dotColor1);
                }

                // Robot 2
                if (robots.Count > 1)
                {
                    var r2 = robots[1];
                    string GetVar(string name) => r2.InputVars.FirstOrDefault(v => v.Name == name)?.Value ?? "---";

                    string r2Durum = GetVar("G_ROBOT_DURUM");
                    string r2Mesaj = GetVar("G_DURUM_MESAJ");
                    string r2HataVar = GetVar("G_HATA_VAR");
                    string r2HataKodu = GetVar("G_HATA_KODU");

                    int.TryParse(r2Durum, out int rd2);
                    bool hata2 = r2HataVar?.ToUpper() == "TRUE" || r2HataVar == "1";

                    string durumColor2 = rd2 switch { 0 => "#4CAF50", 1 => "#FF9800", 2 => "#F44336", _ => "#888" };
                    string dotColor2 = !r2.IsConnected ? "#555" : (hata2 || rd2 == 2) ? "#F44336" : (rd2 >= 1) ? "#FF9800" : "#4CAF50";
                    string hataColor2 = (hata2 || r2HataKodu != "0" && r2HataKodu != "---") ? "#F44336" : "#4CAF50";

                    SetRefText(RefR2RobotDurum, $"{r2Durum} ({DecodeDurum(rd2)})", durumColor2);
                    SetRefText(RefR2DurumMesaj, r2Mesaj, "#888");
                    SetRefText(RefR2HataVar, r2HataVar, hataColor2);
                    SetRefText(RefR2HataKodu, r2HataKodu, hataColor2);
                    SetRefDot(RefR2StatusDot, dotColor2);
                }
            }
            catch { }
        }

        // ═══ SAFETY SİNYALLERİ TABLO GÜNCELLEMESİ ═══
        private void UpdateSafetySignalsTable()
        {
            try
            {
                var robots = KukaRobotManager.Instance.Robots;

                if (robots.Count > 0)
                {
                    var r1 = robots[0];
                    SetSafetyCell(SafeR1DrivesOn, r1.DrivesOn, "ON", "OFF");
                    SetSafetyCell(SafeR1EmergencyStop, r1.EmergencyStop, "AKTİF", "PASİF", true);
                    SetSafetyCell(SafeR1PeripheralReady, r1.PeripheralReady, "Hazır", "Bekle");
                    SetSafetyCell(SafeR1UserSafety, r1.UserSafety, "OK", "Uyarı");
                    SetSafetyCell(SafeR1AlarmStop, r1.AlarmStop, "AKTİF", "OK", true);
                    SetSafetyCell(SafeR1RobotReady, r1.RobotReady, "Hazır", "Değil");
                    SetSafetyCellText(SafeR1Mode, r1.OperationModeText, "#2196F3");
                }

                if (robots.Count > 1)
                {
                    var r2 = robots[1];
                    SetSafetyCell(SafeR2DrivesOn, r2.DrivesOn, "ON", "OFF");
                    SetSafetyCell(SafeR2EmergencyStop, r2.EmergencyStop, "AKTİF", "PASİF", true);
                    SetSafetyCell(SafeR2PeripheralReady, r2.PeripheralReady, "Hazır", "Bekle");
                    SetSafetyCell(SafeR2UserSafety, r2.UserSafety, "OK", "Uyarı");
                    SetSafetyCell(SafeR2AlarmStop, r2.AlarmStop, "AKTİF", "OK", true);
                    SetSafetyCell(SafeR2RobotReady, r2.RobotReady, "Hazır", "Değil");
                    SetSafetyCellText(SafeR2Mode, r2.OperationModeText, "#2196F3");
                }
            }
            catch { }
        }

        private void SetSafetyCell(Border cell, bool value, string trueText, string falseText, bool invertColor = false)
        {
            if (cell == null) return;
            bool isGood = invertColor ? !value : value;
            string bgColor = isGood ? "#1B3A1B" : "#3A1B1B";
            string fgColor = isGood ? "#4CAF50" : "#F44336";
            string text = value ? trueText : falseText;

            try
            {
                cell.Background = new SolidColorBrush(ParseColor(bgColor));
                if (cell.Child is TextBlock tb)
                {
                    tb.Text = text;
                    tb.Foreground = new SolidColorBrush(ParseColor(fgColor));
                }
            }
            catch { }
        }

        private void SetSafetyCellText(Border cell, string text, string color)
        {
            if (cell == null) return;
            try
            {
                cell.Background = new SolidColorBrush(ParseColor("#1B2A3A"));
                if (cell.Child is TextBlock tb)
                {
                    tb.Text = text ?? "---";
                    tb.Foreground = new SolidColorBrush(ParseColor(color));
                }
            }
            catch { }
        }

        private static string DecodeDurum(int durum) => durum switch
        {
            0 => "Bosta",
            1 => "Calisiyor",
            2 => "HATA",
            10 => "Gocator Bekleniyor",
            11 => "Gocator OK",
            12 => "Gocator NOK",
            20 => "Sniffer Bekleniyor",
            21 => "Sniffer OK",
            22 => "Sniffer NOK",
            30 => "Slider Hareket",
            31 => "Slider Tamam",
            50 => "Tabla Bekleniyor",
            51 => "Tabla OK",
            60 => "Diger Robot Bekleniyor",
            61 => "Diger Robot Hatasi",
            62 => "Robot 1 Home Bekleniyor",
            63 => "Ge\u00e7ersiz \u0130stasyon No",
            _ => $"Kod:{durum}"
        };

        private void SetRefText(TextBlock tb, string text, string color)
        {
            if (tb == null) return;
            tb.Text = text ?? "---";
            try { tb.Foreground = new SolidColorBrush(ParseColor(color)); } catch { }
        }

        private void SetRefDot(Border dot, string color)
        {
            if (dot == null) return;
            try { dot.Background = new SolidColorBrush(ParseColor(color)); } catch { }
        }

        private void LogMessage(string msg)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                RobotLogTextBlock.Text = $"[{DateTime.Now:HH:mm:ss}] {msg}\n" + RobotLogTextBlock.Text;
                if (RobotLogTextBlock.Text.Length > 5000)
                    RobotLogTextBlock.Text = RobotLogTextBlock.Text.Substring(0, 5000);
            });
        }

        private Windows.UI.Color ParseColor(string hex)
        {
            hex = hex.Replace("#", "");
            return Windows.UI.Color.FromArgb(255,
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16));
        }

        // Event Handlers
        private async void BtnAddRobot_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Yeni Robot Ekle",
                PrimaryButtonText = "Ekle",
                CloseButtonText = "İptal",
                XamlRoot = this.XamlRoot,
                DefaultButton = ContentDialogButton.Primary
            };

            var panel = new StackPanel { Spacing = 10 };
            var nameBox = new TextBox { PlaceholderText = "Robot Adı", Text = $"ROBOT {KukaRobotManager.Instance.Robots.Count + 1}" };
            var ipBox = new TextBox { PlaceholderText = "IP Adresi", Text = "192.168.251.71" };
            var portBox = new TextBox { PlaceholderText = "Port", Text = "7000" };

            panel.Children.Add(new TextBlock { Text = "Robot Adı:" });
            panel.Children.Add(nameBox);
            panel.Children.Add(new TextBlock { Text = "IP Adresi:" });
            panel.Children.Add(ipBox);
            panel.Children.Add(new TextBlock { Text = "Port:" });
            panel.Children.Add(portBox);

            dialog.Content = panel;

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                string name = nameBox.Text.Trim();
                string ip = ipBox.Text.Trim();
                int.TryParse(portBox.Text, out int port);
                if (port == 0) port = 7000;

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(ip))
                {
                    KukaRobotManager.Instance.AddRobot(name, ip, port);
                    LogMessage($"Yeni robot eklendi: {name} ({ip}:{port})");
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        // EXPORT / IMPORT - Robot Değişkenlerini Dışa/İçe Aktar
        // ═══════════════════════════════════════════════════════

        private async void BtnExportVars_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FileSavePicker();
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
                picker.FileTypeChoices.Add("JSON Dosyası", new List<string> { ".json" });
                picker.SuggestedFileName = $"Robot_Variables_Export_{DateTime.Now:yyyyMMdd_HHmmss}";

                // WinUI 3 window handle
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.m_window);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var file = await picker.PickSaveFileAsync();
                if (file == null) return;

                string json = KukaRobotManager.Instance.ExportRobotVariablesToJson();
                await Windows.Storage.FileIO.WriteTextAsync(file, json);

                int totalInputs = 0, totalOutputs = 0;
                foreach (var r in KukaRobotManager.Instance.Robots)
                {
                    totalInputs += r.InputVars.Count;
                    totalOutputs += r.OutputVars.Count;
                }

                LogMessage($"✅ Dışa aktarıldı: {totalInputs} Input, {totalOutputs} Output → {file.Path}");

                var dlg = new ContentDialog
                {
                    Title = "Dışa Aktarma Başarılı",
                    Content = $"Toplam {totalInputs} Input ve {totalOutputs} Output değişken dışa aktarıldı.\n\nDosya: {file.Path}",
                    CloseButtonText = "Tamam",
                    XamlRoot = this.XamlRoot
                };
                await dlg.ShowAsync();
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Dışa aktarma hatası: {ex.Message}");
            }
        }

        private async void BtnImportVars_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
                picker.FileTypeFilter.Add(".json");

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.m_window);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var file = await picker.PickSingleFileAsync();
                if (file == null) return;

                string json = await Windows.Storage.FileIO.ReadTextAsync(file);

                // Onay dialogu göster
                var confirmDlg = new ContentDialog
                {
                    Title = "İçe Aktarma Onayı",
                    Content = $"'{file.Name}' dosyasından robot değişkenleri yüklenecek.\n\nMevcut Input/Output tabloları bu dosyadaki verilerle DEĞİŞTİRİLECEK.\n\nDevam etmek istiyor musunuz?",
                    PrimaryButtonText = "Evet, İçe Aktar",
                    CloseButtonText = "İptal",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };

                if (await confirmDlg.ShowAsync() != ContentDialogResult.Primary) return;

                var (inputCount, outputCount, robotName) = KukaRobotManager.Instance.ImportRobotVariablesFromJson(json);

                LogMessage($"✅ İçe aktarıldı: {inputCount} Input, {outputCount} Output ← {file.Name}");

                var successDlg = new ContentDialog
                {
                    Title = "İçe Aktarma Başarılı",
                    Content = $"Toplam {inputCount} Input ve {outputCount} Output değişken yüklendi.\n\nDeğişiklikler kaydedildi.",
                    CloseButtonText = "Tamam",
                    XamlRoot = this.XamlRoot
                };
                await successDlg.ShowAsync();
            }
            catch (Exception ex)
            {
                LogMessage($"❌ İçe aktarma hatası: {ex.Message}");

                try
                {
                    var errDlg = new ContentDialog
                    {
                        Title = "İçe Aktarma Hatası",
                        Content = $"Dosya yüklenirken hata oluştu:\n\n{ex.Message}\n\nJSON formatının doğru olduğundan emin olun.",
                        CloseButtonText = "Tamam",
                        XamlRoot = this.XamlRoot
                    };
                    await errDlg.ShowAsync();
                }
                catch { }
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PlcVariable v)
            {
                bool removed = false;
                foreach (var robot in KukaRobotManager.Instance.Robots)
                {
                    if (robot.InputVars.Contains(v)) { robot.InputVars.Remove(v); removed = true; break; }
                    if (robot.OutputVars.Contains(v)) { robot.OutputVars.Remove(v); removed = true; break; }
                }
                if (removed)
                {
                    try { KukaRobotManager.Instance.SaveRobotVariables(); } catch { }
                }
            }
        }

        private void BtnMoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PlcVariable v)
            {
                foreach (var robot in KukaRobotManager.Instance.Robots)
                {
                    int idx = robot.InputVars.IndexOf(v);
                    if (idx > 0) { robot.InputVars.Move(idx, idx - 1); SaveAndReturn(); return; }
                    idx = robot.OutputVars.IndexOf(v);
                    if (idx > 0) { robot.OutputVars.Move(idx, idx - 1); SaveAndReturn(); return; }
                }
            }
            void SaveAndReturn() { try { KukaRobotManager.Instance.SaveRobotVariables(); } catch { } }
        }

        private void BtnMoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PlcVariable v)
            {
                foreach (var robot in KukaRobotManager.Instance.Robots)
                {
                    int idx = robot.InputVars.IndexOf(v);
                    if (idx >= 0 && idx < robot.InputVars.Count - 1) { robot.InputVars.Move(idx, idx + 1); SaveAndReturn(); return; }
                    idx = robot.OutputVars.IndexOf(v);
                    if (idx >= 0 && idx < robot.OutputVars.Count - 1) { robot.OutputVars.Move(idx, idx + 1); SaveAndReturn(); return; }
                }
            }
            void SaveAndReturn() { try { KukaRobotManager.Instance.SaveRobotVariables(); } catch { } }
        }

        private void Variable_Edited_LostFocus(object sender, RoutedEventArgs e)
        {
            // x:Bind TwoWay binding zaten PlcVariable instance'ını güncelledi.
            // Her LostFocus'ta robot değişkenlerini diske kaydet.
            try { KukaRobotManager.Instance.SaveRobotVariables(); } catch { }
        }

        private async void ValueTextBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (sender is TextBox tb && tb.Tag is PlcVariable v)
                {
                    string newVal = tb.Text;
                    string tagName = v.PlcTag;
                    if (string.IsNullOrWhiteSpace(tagName)) return;

                    // Hangi robota ait?
                    foreach (var robot in KukaRobotManager.Instance.Robots)
                    {
                        if (robot.InputVars.Contains(v) || robot.OutputVars.Contains(v))
                        {
                            bool success = await robot.WriteVariableAsync(tagName, newVal);
                            if (success)
                            {
                                v.Value = newVal;
                                // ★ CommunicationLoop cache'ini güncelle — eski değeri geri yazmasın
                                robot.UpdateLastWrittenOutput(tagName, newVal);
                                // ★ GlobalData'daki karşılığını da güncelle (diğer sayfalar görsün)
                                var gVar = GlobalData.RobotOutputVars.FirstOrDefault(g => g.Name == v.Name);
                                if (gVar != null) gVar.Value = newVal;
                                LogMessage($"✅ {tagName} = {newVal} yazıldı");
                            }
                            else
                            {
                                LogMessage($"❌ {tagName} yazılamadı");
                            }
                            return;
                        }
                    }
                }
            }
        }

        #region Robot-PLC Köprü Tablosu

        private void RefreshBridgeTables()
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                _suppressSave = true;
                BridgeTablesContainer.Children.Clear();
                MappingCountText.Text = $"{_mappings.Count} Eşleşme";

                foreach (var robot in KukaRobotManager.Instance.Robots)
                {
                    int robotIndex = KukaRobotManager.Instance.Robots.IndexOf(robot);
                    string color = _robotColors[robotIndex % _robotColors.Length];
                    var accent = ParseColor(color);

                    var inputMappings = _mappings.Where(m => m.RobotName == robot.Name && m.TableType == "Input").ToList();
                    var outputMappings = _mappings.Where(m => m.RobotName == robot.Name && m.TableType == "Output").ToList();

                    // Per-robot bridge panel
                    var panelBorder = new Border
                    {
                        Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 30, 32)),
                        CornerRadius = new CornerRadius(8),
                        BorderThickness = new Thickness(1),
                        BorderBrush = new SolidColorBrush(accent)
                    };

                    var mainGrid = new Grid();
                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36) });
                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    // Header
                    var headerBorder = new Border
                    {
                        Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 37, 40)),
                        CornerRadius = new CornerRadius(8, 8, 0, 0),
                        Padding = new Thickness(12, 0, 12, 0)
                    };
                    var headerStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
                    headerStack.Children.Add(new FontIcon { Glyph = "\uE8F4", Foreground = new SolidColorBrush(accent), FontSize = 13 });
                    headerStack.Children.Add(new TextBlock
                    {
                        Text = $"{robot.Name} \u2194 PLC KÖPRÜSÜ",
                        FontSize = 11,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Microsoft.UI.Colors.White)
                    });
                    var countBadge = new Border
                    {
                        Background = new SolidColorBrush(accent),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(6, 1, 6, 1),
                        Margin = new Thickness(8, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    countBadge.Child = new TextBlock
                    {
                        Text = $"{inputMappings.Count + outputMappings.Count} Eşleşme",
                        FontSize = 9,
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                        Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0))
                    };
                    headerStack.Children.Add(countBadge);
                    headerBorder.Child = headerStack;
                    Grid.SetRow(headerBorder, 0);
                    mainGrid.Children.Add(headerBorder);

                    // Two-column layout for Input/Output tables
                    var tablesGrid = new Grid { Padding = new Thickness(6) };
                    tablesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    tablesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    var inputPanel = CreateBridgeTablePanel(robot, "Input", "\uE9D2 PLC'DEN ROBOTA G\u00d6NDER\u0130M", "#4CAF50", inputMappings);
                    Grid.SetColumn(inputPanel, 0);
                    inputPanel.Margin = new Thickness(0, 0, 3, 0);
                    tablesGrid.Children.Add(inputPanel);

                    var outputPanel = CreateBridgeTablePanel(robot, "Output", "\uE7E8 ROBOTTAN PLC'YE OKUMA", "#FF9800", outputMappings);
                    Grid.SetColumn(outputPanel, 1);
                    outputPanel.Margin = new Thickness(3, 0, 0, 0);
                    tablesGrid.Children.Add(outputPanel);

                    Grid.SetRow(tablesGrid, 1);
                    mainGrid.Children.Add(tablesGrid);

                    panelBorder.Child = mainGrid;
                    BridgeTablesContainer.Children.Add(panelBorder);
                }
                _suppressSave = false;
            });
        }

        private Border CreateBridgeTablePanel(KukaRobotInstance robot, string tableType, string title, string accentHex, List<RobotPlcMapping> tableMappings)
        {
            var accent = ParseColor(accentHex);
            bool isInput = tableType == "Input";

            var border = new Border
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 13, 13, 13)),
                CornerRadius = new CornerRadius(6)
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(22) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Sub-header with add button
            var subHeader = new Border
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 26, 26, 28)),
                CornerRadius = new CornerRadius(6, 6, 0, 0),
                Padding = new Thickness(8, 0, 8, 0)
            };
            var subHeaderGrid = new Grid();
            subHeaderGrid.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 9,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new SolidColorBrush(accent),
                VerticalAlignment = VerticalAlignment.Center
            });
            var addBtn = new Button
            {
                Content = "+",
                Background = new SolidColorBrush(accent),
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                CornerRadius = new CornerRadius(3),
                Height = 20, Width = 20,
                Padding = new Thickness(0),
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            addBtn.Click += (s, e) =>
            {
                _mappings.Add(new RobotPlcMapping
                {
                    RobotName = robot.Name,
                    TableType = tableType,
                    Direction = isInput ? "PLC\u2192Robot" : "Robot\u2192PLC"
                });
                SaveMappings();
                RefreshBridgeTables();
            };
            subHeaderGrid.Children.Add(addBtn);
            subHeader.Child = subHeaderGrid;
            Grid.SetRow(subHeader, 0);
            grid.Children.Add(subHeader);

            // Column headers
            var colBorder = new Border
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 20, 20, 22)),
                Padding = new Thickness(4, 2, 4, 2)
            };
            var colGrid = new Grid();
            colGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
            colGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            colGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
            colGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            colGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            colGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            colGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            colGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });

            string[] colHeaders = isInput
                ? new[] { "#", "PLC Tag", "\u2192", "Robot Tag", "Değer", "Aktif", "Sıra", "Sil" }
                : new[] { "#", "Robot Tag", "\u2192", "PLC Tag", "Değer", "Aktif", "Sıra", "Sil" };

            for (int i = 0; i < colHeaders.Length; i++)
            {
                var tb = new TextBlock
                {
                    Text = colHeaders[i],
                    FontSize = 7,
                    Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 85, 85, 85)),
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(tb, i);
                colGrid.Children.Add(tb);
            }
            colBorder.Child = colGrid;
            Grid.SetRow(colBorder, 1);
            grid.Children.Add(colBorder);

            // Rows
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                MaxHeight = 180,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 13, 13, 13))
            };
            var rowsContainer = new StackPanel { Spacing = 2, Padding = new Thickness(2) };
            for (int i = 0; i < tableMappings.Count; i++)
            {
                var row = CreateBridgeMappingRow(robot, tableMappings[i], i + 1, isInput, accentHex);
                rowsContainer.Children.Add(row);
            }
            scrollViewer.Content = rowsContainer;
            Grid.SetRow(scrollViewer, 2);
            grid.Children.Add(scrollViewer);

            border.Child = grid;
            return border;
        }

        private Border CreateBridgeMappingRow(KukaRobotInstance robot, RobotPlcMapping mapping, int rowNumber, bool isInput, string accentHex)
        {
            var accent = ParseColor(accentHex);

            var rowBorder = new Border
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 26, 26, 28)),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 3, 4, 3)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });

            // #
            grid.Children.Add(CreateTextInColumn(rowNumber.ToString(), 0, 9, "#646464"));

            // Source Tag ComboBox
            var sourceCombo = new ComboBox
            {
                FontSize = 8, Height = 26,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 37, 40)),
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                Margin = new Thickness(2, 0, 2, 0)
            };

            // Target Tag ComboBox
            var targetCombo = new ComboBox
            {
                FontSize = 8, Height = 26,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 37, 40)),
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                Margin = new Thickness(2, 0, 2, 0)
            };

            if (isInput)
            {
                // Input: PLC → Robot | Source = PLC tags, Target = Robot tags
                foreach (var plcVar in PlcService.Instance.OutputVariables.Concat(PlcService.Instance.InputVariables))
                    sourceCombo.Items.Add($"{plcVar.Name} ({plcVar.Address})");
                foreach (var v in robot.InputVars.Concat(robot.OutputVars))
                    targetCombo.Items.Add($"{v.Name} ({v.PlcTag})");

                if (!string.IsNullOrEmpty(mapping.PlcTag))
                    foreach (var item in sourceCombo.Items)
                        if (item.ToString() == mapping.PlcTag) { sourceCombo.SelectedItem = item; break; }
                if (!string.IsNullOrEmpty(mapping.RobotTag))
                    foreach (var item in targetCombo.Items)
                        if (item.ToString() == mapping.RobotTag) { targetCombo.SelectedItem = item; break; }

                sourceCombo.SelectionChanged += (s, e) => { if (_suppressSave) return; mapping.PlcTag = sourceCombo.SelectedItem?.ToString(); SaveMappings(); };
                targetCombo.SelectionChanged += (s, e) => { if (_suppressSave) return; mapping.RobotTag = targetCombo.SelectedItem?.ToString(); SaveMappings(); };
            }
            else
            {
                // Output: Robot → PLC | Source = Robot tags, Target = PLC tags
                foreach (var v in robot.InputVars.Concat(robot.OutputVars))
                    sourceCombo.Items.Add($"{v.Name} ({v.PlcTag})");
                foreach (var plcVar in PlcService.Instance.OutputVariables.Concat(PlcService.Instance.InputVariables))
                    targetCombo.Items.Add($"{plcVar.Name} ({plcVar.Address})");

                if (!string.IsNullOrEmpty(mapping.RobotTag))
                    foreach (var item in sourceCombo.Items)
                        if (item.ToString() == mapping.RobotTag) { sourceCombo.SelectedItem = item; break; }
                if (!string.IsNullOrEmpty(mapping.PlcTag))
                    foreach (var item in targetCombo.Items)
                        if (item.ToString() == mapping.PlcTag) { targetCombo.SelectedItem = item; break; }

                sourceCombo.SelectionChanged += (s, e) => { if (_suppressSave) return; mapping.RobotTag = sourceCombo.SelectedItem?.ToString(); SaveMappings(); };
                targetCombo.SelectionChanged += (s, e) => { if (_suppressSave) return; mapping.PlcTag = targetCombo.SelectedItem?.ToString(); SaveMappings(); };
            }

            Grid.SetColumn(sourceCombo, 1);
            grid.Children.Add(sourceCombo);

            // Arrow
            var arrow = new TextBlock
            {
                Text = "\u2192", FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new SolidColorBrush(accent),
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(arrow, 2);
            grid.Children.Add(arrow);

            Grid.SetColumn(targetCombo, 3);
            grid.Children.Add(targetCombo);

            // Value
            var valueText = new TextBlock
            {
                Text = mapping.LastValue ?? "-", FontSize = 9, FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80)),
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            mapping.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(RobotPlcMapping.LastValue))
                    this.DispatcherQueue.TryEnqueue(() => valueText.Text = mapping.LastValue ?? "-");
            };
            Grid.SetColumn(valueText, 4);
            grid.Children.Add(valueText);

            // Active toggle
            var activeToggle = new ToggleSwitch
            {
                IsOn = mapping.IsActive, MinWidth = 0, MinHeight = 0,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            activeToggle.Toggled += (s, e) => { mapping.IsActive = activeToggle.IsOn; SaveMappings(); };
            Grid.SetColumn(activeToggle, 5);
            grid.Children.Add(activeToggle);

            // Reorder buttons (▲▼)
            var reorderPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Spacing = 1 };
            var upBtn = new Button { Content = "\uE70E", FontFamily = new FontFamily("Segoe MDL2 Assets"), Background = new SolidColorBrush(Colors.Transparent), Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136)), BorderThickness = new Thickness(0), Padding = new Thickness(1), FontSize = 9, MinWidth = 18, MinHeight = 18, Height = 18, Width = 18 };
            var downBtn = new Button { Content = "\uE70D", FontFamily = new FontFamily("Segoe MDL2 Assets"), Background = new SolidColorBrush(Colors.Transparent), Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136)), BorderThickness = new Thickness(0), Padding = new Thickness(1), FontSize = 9, MinWidth = 18, MinHeight = 18, Height = 18, Width = 18 };
            upBtn.Click += (s, e) => { int idx = _mappings.IndexOf(mapping); if (idx > 0) { _mappings.Move(idx, idx - 1); SaveMappings(); RefreshBridgeTables(); } };
            downBtn.Click += (s, e) => { int idx = _mappings.IndexOf(mapping); if (idx >= 0 && idx < _mappings.Count - 1) { _mappings.Move(idx, idx + 1); SaveMappings(); RefreshBridgeTables(); } };
            reorderPanel.Children.Add(upBtn);
            reorderPanel.Children.Add(downBtn);
            Grid.SetColumn(reorderPanel, 6);
            grid.Children.Add(reorderPanel);

            // Delete
            var deleteBtn = new Button
            {
                Content = "\uE74D", FontFamily = new FontFamily("Segoe MDL2 Assets"),
                Background = new SolidColorBrush(Colors.Transparent),
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 82, 82)),
                BorderThickness = new Thickness(0), Padding = new Thickness(0), FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            deleteBtn.Click += (s, e) =>
            {
                _mappings.Remove(mapping);
                SaveMappings();
                RefreshBridgeTables();
            };
            Grid.SetColumn(deleteBtn, 7);
            grid.Children.Add(deleteBtn);

            rowBorder.Child = grid;
            return rowBorder;
        }

        private TextBlock CreateTextInColumn(string text, int column, double fontSize, string colorHex)
        {
            var tb = new TextBlock
            {
                Text = text, FontSize = fontSize,
                Foreground = new SolidColorBrush(ParseColor(colorHex)),
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(tb, column);
            return tb;
        }

        // ProcessMappingsAsync artik KukaRobotManager.ProcessRobotPlcBridgeAsync() tarafindan global olarak calistiriliyor


        private void LoadMappings()
        {
            // Mappingler artik KukaRobotManager.Initialize() da global olarak yukleniyor
        }

        /// <summary>
        /// Robot-PLC k\u00f6pr\u00fc tablolar\u0131na eksik varsay\u0131lan e\u015fle\u015fmeleri ekler.
        /// Mevcut e\u015fle\u015fmeleri korur, sadece eksikleri ekler (merge).
        /// </summary>
        private void MergeDefaultBridgeMappings()
        {
            // EnsureRobotBridgeVariables() kaldırıldı — PLC değişkenleri CSV import ile yönetiliyor

            // Mevcut e\u015fle\u015fmelerin anahtar seti (RobotName | Robot de\u011fi\u015fken ad\u0131)
            var existingKeys = new HashSet<string>(
                _mappings.Where(m => !string.IsNullOrEmpty(m.RobotTag))
                    .Select(m => $"{m.RobotName}|{m.RobotTag.Split(' ')[0]}"),
                StringComparer.OrdinalIgnoreCase);

            string FindPlcTag(string name)
            {
                var v = PlcService.Instance.OutputVariables.Concat(PlcService.Instance.InputVariables)
                    .FirstOrDefault(x => x.Name == name);
                return v != null ? $"{v.Name} ({v.Address})" : null;
            }

            string FindRobotTag(KukaRobotInstance robot, string name)
            {
                var v = robot.InputVars.Concat(robot.OutputVars)
                    .FirstOrDefault(x => x.Name == name);
                return v != null ? $"{v.Name} ({v.PlcTag})" : null;
            }

            bool added = false;
            void AddIfMissing(KukaRobotInstance robot, string plcVarName, string robotVarName,
                string tableType, string direction)
            {
                var key = $"{robot.Name}|{robotVarName}";
                if (existingKeys.Contains(key)) return;

                var plcTag = FindPlcTag(plcVarName);
                var robotTag = FindRobotTag(robot, robotVarName);
                if (plcTag == null || robotTag == null) return;

                _mappings.Add(new RobotPlcMapping
                {
                    RobotName = robot.Name,
                    PlcTag = plcTag,
                    RobotTag = robotTag,
                    Direction = direction,
                    TableType = tableType,
                    IsActive = true
                });
                existingKeys.Add(key);
                added = true;
            }

            foreach (var robot in KukaRobotManager.Instance.Robots)
            {
                if (robot.Name == "ROBOT 1")
                {
                    // \u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550
                    // ROBOT 1 INPUT (PLC \u2192 Robot 1)
                    // PLC'den okunan de\u011ferler \u2192 Robot 1'e yaz\u0131l\u0131r
                    // \u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550
                    AddIfMissing(robot, "SAFETY_OK", "G_SAFETY_OK", "Input", "PLC\u2192Robot");
                    AddIfMissing(robot, "FIRST_ROBOT_GO", "G_SISTEM_START", "Input", "PLC\u2192Robot");
                    AddIfMissing(robot, "CMD_LINE_STOP", "G_SISTEM_STOP", "Input", "PLC\u2192Robot");
                    AddIfMissing(robot, "CMD_LINE_RESET", "G_RESET", "Input", "PLC\u2192Robot");
                    AddIfMissing(robot, "LINE_AUTO_MODE", "G_OTO_MOD", "Input", "PLC\u2192Robot");
                    AddIfMissing(robot, "AKTUEL_KLIMA_INDEX", "G_KLIMA_TIP", "Input", "PLC\u2192Robot");

                    // \u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550
                    // ROBOT 1 OUTPUT (Robot 1 \u2192 PLC)
                    // Robot 1'den okunan de\u011ferler \u2192 PLC'ye yaz\u0131l\u0131r
                    // \u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550
                    AddIfMissing(robot, "RB1_ROBOT_DURUM", "G_ROBOT_DURUM", "Output", "Robot\u2192PLC");
                    AddIfMissing(robot, "RB1_IS_BITTI", "G_IS_BITTI", "Output", "Robot\u2192PLC");
                    AddIfMissing(robot, "RB1_HATA_VAR", "G_HATA_VAR", "Output", "Robot\u2192PLC");
                    AddIfMissing(robot, "RB1_HATA_KODU", "G_HATA_KODU", "Output", "Robot\u2192PLC");
                    AddIfMissing(robot, "RB1_HOME_OK", "G_R1_HOME", "Output", "Robot\u2192PLC");
                    AddIfMissing(robot, "RB1_AKTIF_NOKTA", "G_AKTIF_NOKTA", "Output", "Robot\u2192PLC");
                    AddIfMissing(robot, "RB1_DURUM_MESAJ", "G_DURUM_MESAJ", "Output", "Robot\u2192PLC");
                    AddIfMissing(robot, "RB1_NOK_SAYISI", "G_NOK_SAYISI", "Output", "Robot\u2192PLC");
                    AddIfMissing(robot, "RB1_TOPLAM_NOKTA", "G_TOPLAM_NOKTA", "Output", "Robot\u2192PLC");
                    AddIfMissing(robot, "RB1_NOK_BILDIRIM", "G_NOK_BILDIRIM", "Output", "Robot\u2192PLC");
                    AddIfMissing(robot, "RB1_NOK_NOKTA", "G_NOK_NOKTA", "Output", "Robot\u2192PLC");
                }
                else if (robot.Name == "ROBOT 2")
                {
                    // \u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550
                    // ROBOT 2 INPUT (PLC \u2192 Robot 2)
                    // PLC'den okunan de\u011ferler \u2192 Robot 2'ye yaz\u0131l\u0131r
                    // \u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550
                    AddIfMissing(robot, "SAFETY_OK", "G_SAFETY_OK", "Input", "PLC\u2192Robot");
                    AddIfMissing(robot, "SECOND_ROBOT_GO", "G_SISTEM_START", "Input", "PLC\u2192Robot");
                    AddIfMissing(robot, "CMD_LINE_STOP", "G_SISTEM_STOP", "Input", "PLC\u2192Robot");
                    AddIfMissing(robot, "CMD_LINE_RESET", "G_RESET", "Input", "PLC\u2192Robot");
                    AddIfMissing(robot, "LINE_AUTO_MODE", "G_OTO_MOD", "Input", "PLC\u2192Robot");
                    AddIfMissing(robot, "AKTUEL_KLIMA_INDEX", "G_KLIMA_TIP", "Input", "PLC\u2192Robot");
                    // Slider hedef pozisyonu: Manuel sayfadaki istasyon pozisyonunu Robot 2'ye yaz
                    AddIfMissing(robot, "KL100_HEDEF_POZ", "G_SLIDER_HEDEF_POZ", "Input", "PLC\u2192Robot");
                    // Hedef istasyon numaras\u0131: Hangi istasyona gidecek (1=Ist1, 2=Ist2, 3=Ist3, 4=Bak\u0131m)
                    AddIfMissing(robot, "KL100_HEDEF_ISTASYON", "G_HEDEF_ISTASYON", "Input", "PLC\u2192Robot");

                    // \u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550
                    // ROBOT 2 OUTPUT (Robot 2 \u2192 PLC)
                    // Robot 2'den okunan de\u011ferler \u2192 PLC'ye yaz\u0131l\u0131r
                    // \u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550
                    AddIfMissing(robot, "RB2_ROBOT_DURUM", "G_ROBOT_DURUM", "Output", "Robot\u2192PLC");
                    AddIfMissing(robot, "RB2_IS_BITTI", "G_IS_BITTI", "Output", "Robot\u2192PLC");
                    AddIfMissing(robot, "RB2_HATA_VAR", "G_HATA_VAR", "Output", "Robot\u2192PLC");
                    AddIfMissing(robot, "RB2_HATA_KODU", "G_HATA_KODU", "Output", "Robot\u2192PLC");
                    AddIfMissing(robot, "RB2_HOME_OK", "G_R2_HOME", "Output", "Robot\u2192PLC");
                    AddIfMissing(robot, "RB2_AKTIF_CIZGI", "G_AKTIF_CIZGI", "Output", "Robot\u2192PLC");
                    AddIfMissing(robot, "RB2_DURUM_MESAJ", "G_DURUM_MESAJ", "Output", "Robot\u2192PLC");
                    AddIfMissing(robot, "RB2_NOK_SAYISI", "G_NOK_SAYISI", "Output", "Robot\u2192PLC");
                    AddIfMissing(robot, "RB2_TOPLAM_CIZGI", "G_TOPLAM_CIZGI", "Output", "Robot\u2192PLC");
                    AddIfMissing(robot, "RB2_NOK_BILDIRIM", "G_NOK_BILDIRIM", "Output", "Robot\u2192PLC");
                    AddIfMissing(robot, "RB2_NOK_CIZGI", "G_NOK_CIZGI", "Output", "Robot\u2192PLC");
                    // --- Robot 2 Slider (Robot 2 \u2192 PLC) ---
                    AddIfMissing(robot, "RB2_SLIDER_TAMAM", "G_SLIDER_TAMAM", "Output", "Robot\u2192PLC");
                    AddIfMissing(robot, "RB2_SLIDER_HOME", "G_SLIDER_HOME", "Output", "Robot\u2192PLC");
                    AddIfMissing(robot, "RB2_SLIDER_POZ", "G_SLIDER_AKTUEL_POZ", "Output", "Robot\u2192PLC");
                }
            }

            if (added)
                SaveMappings();
        }

        private void SaveMappings()
        {
            KukaRobotManager.Instance.SaveRobotPlcMappings();
        }

        #endregion

        #region Robot-Robot Haberleşme Tablosu

        private void BtnAddRobotRobotMapping_Click(object sender, RoutedEventArgs e)
        {
            _robotRobotMappings.Add(new RobotRobotMapping());
            SaveRobotRobotMappings();
            RefreshRobotRobotRows();
        }

        private void RefreshRobotRobotRows()
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                _suppressSave = true;
                RobotRobotRowsContainer.Children.Clear();
                RobotRobotCountText.Text = $"{_robotRobotMappings.Count} Eşleşme";

                for (int i = 0; i < _robotRobotMappings.Count; i++)
                {
                    var row = CreateRobotRobotMappingRow(_robotRobotMappings[i], i + 1);
                    RobotRobotRowsContainer.Children.Add(row);
                }
                _suppressSave = false;
            });
        }

        private Border CreateRobotRobotMappingRow(RobotRobotMapping mapping, int rowNumber)
        {
            var rowBorder = new Border
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 26, 26, 28)),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 3, 4, 3)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });

            // #
            grid.Children.Add(CreateTextInColumn(rowNumber.ToString(), 0, 9, "#646464"));

            // Source Robot ComboBox
            var srcRobotCombo = new ComboBox
            {
                FontSize = 9, Height = 26,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 37, 40)),
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                Margin = new Thickness(2, 0, 2, 0)
            };
            foreach (var r in KukaRobotManager.Instance.Robots)
                srcRobotCombo.Items.Add(r.Name);
            if (!string.IsNullOrEmpty(mapping.SourceRobotName))
                srcRobotCombo.SelectedItem = mapping.SourceRobotName;
            Grid.SetColumn(srcRobotCombo, 1);
            grid.Children.Add(srcRobotCombo);

            // Source Tag ComboBox
            var srcTagCombo = new ComboBox
            {
                FontSize = 9, Height = 26,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 37, 40)),
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                Margin = new Thickness(2, 0, 2, 0)
            };
            Grid.SetColumn(srcTagCombo, 2);
            grid.Children.Add(srcTagCombo);

            // Arrow
            var arrow = new TextBlock
            {
                Text = "\u2192", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 87, 34)),
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(arrow, 3);
            grid.Children.Add(arrow);

            // Target Robot ComboBox
            var tgtRobotCombo = new ComboBox
            {
                FontSize = 9, Height = 26,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 37, 40)),
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                Margin = new Thickness(2, 0, 2, 0)
            };
            foreach (var r in KukaRobotManager.Instance.Robots)
                tgtRobotCombo.Items.Add(r.Name);
            if (!string.IsNullOrEmpty(mapping.TargetRobotName))
                tgtRobotCombo.SelectedItem = mapping.TargetRobotName;
            Grid.SetColumn(tgtRobotCombo, 4);
            grid.Children.Add(tgtRobotCombo);

            // Target Tag ComboBox
            var tgtTagCombo = new ComboBox
            {
                FontSize = 9, Height = 26,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 37, 40)),
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                Margin = new Thickness(2, 0, 2, 0)
            };
            Grid.SetColumn(tgtTagCombo, 5);
            grid.Children.Add(tgtTagCombo);

            // Value
            var valueText = new TextBlock
            {
                Text = mapping.LastValue ?? "-", FontSize = 10, FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80)),
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            mapping.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(RobotRobotMapping.LastValue))
                    this.DispatcherQueue.TryEnqueue(() => valueText.Text = mapping.LastValue ?? "-");
            };
            Grid.SetColumn(valueText, 6);
            grid.Children.Add(valueText);

            // Active toggle
            var activeToggle = new ToggleSwitch
            {
                IsOn = mapping.IsActive, MinWidth = 0, MinHeight = 0,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            activeToggle.Toggled += (s, e) => { mapping.IsActive = activeToggle.IsOn; SaveRobotRobotMappings(); };
            Grid.SetColumn(activeToggle, 7);
            grid.Children.Add(activeToggle);

            // Reorder buttons (▲▼)
            var rrReorderPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Spacing = 1 };
            var rrUpBtn = new Button { Content = "\uE70E", FontFamily = new FontFamily("Segoe MDL2 Assets"), Background = new SolidColorBrush(Colors.Transparent), Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136)), BorderThickness = new Thickness(0), Padding = new Thickness(1), FontSize = 9, MinWidth = 18, MinHeight = 18, Height = 18, Width = 18 };
            var rrDownBtn = new Button { Content = "\uE70D", FontFamily = new FontFamily("Segoe MDL2 Assets"), Background = new SolidColorBrush(Colors.Transparent), Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136)), BorderThickness = new Thickness(0), Padding = new Thickness(1), FontSize = 9, MinWidth = 18, MinHeight = 18, Height = 18, Width = 18 };
            rrUpBtn.Click += (s, e) => { int idx = _robotRobotMappings.IndexOf(mapping); if (idx > 0) { _robotRobotMappings.Move(idx, idx - 1); SaveRobotRobotMappings(); RefreshRobotRobotRows(); } };
            rrDownBtn.Click += (s, e) => { int idx = _robotRobotMappings.IndexOf(mapping); if (idx >= 0 && idx < _robotRobotMappings.Count - 1) { _robotRobotMappings.Move(idx, idx + 1); SaveRobotRobotMappings(); RefreshRobotRobotRows(); } };
            rrReorderPanel.Children.Add(rrUpBtn);
            rrReorderPanel.Children.Add(rrDownBtn);
            Grid.SetColumn(rrReorderPanel, 8);
            grid.Children.Add(rrReorderPanel);

            // Delete
            var deleteBtn = new Button
            {
                Content = "\uE74D", FontFamily = new FontFamily("Segoe MDL2 Assets"),
                Background = new SolidColorBrush(Colors.Transparent),
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 82, 82)),
                BorderThickness = new Thickness(0), Padding = new Thickness(0), FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            deleteBtn.Click += (s, e) =>
            {
                _robotRobotMappings.Remove(mapping);
                SaveRobotRobotMappings();
                RefreshRobotRobotRows();
            };
            Grid.SetColumn(deleteBtn, 9);
            grid.Children.Add(deleteBtn);

            // Source Robot tag list update — Kaynak robottan okunabilecek TÜM değişkenler
            void UpdateSourceTags()
            {
                srcTagCombo.Items.Clear();
                var selectedRobot = KukaRobotManager.Instance.Robots.FirstOrDefault(r => r.Name == srcRobotCombo.SelectedItem?.ToString());
                if (selectedRobot == null) return;
                // Hem InputVars hem OutputVars göster — robot-robot köprüde her değişken okunabilir/yazılabilir
                var allVars = selectedRobot.InputVars.Concat(selectedRobot.OutputVars);
                var seen = new HashSet<string>();
                foreach (var v in allVars)
                {
                    string display = $"{v.Name} ({v.PlcTag})";
                    if (seen.Add(display)) srcTagCombo.Items.Add(display);
                }
                if (!string.IsNullOrEmpty(mapping.SourceTag))
                    foreach (var item in srcTagCombo.Items)
                        if (item.ToString() == mapping.SourceTag) { srcTagCombo.SelectedItem = item; break; }
            }

            // Target Robot tag list update — Hedef robota yazılabilecek TÜM değişkenler
            void UpdateTargetTags()
            {
                tgtTagCombo.Items.Clear();
                var selectedRobot = KukaRobotManager.Instance.Robots.FirstOrDefault(r => r.Name == tgtRobotCombo.SelectedItem?.ToString());
                if (selectedRobot == null) return;
                // Hem OutputVars hem InputVars göster — WriteVariableAsync her değişkene yazabilir
                var allVars = selectedRobot.OutputVars.Concat(selectedRobot.InputVars);
                var seen = new HashSet<string>();
                foreach (var v in allVars)
                {
                    string display = $"{v.Name} ({v.PlcTag})";
                    if (seen.Add(display)) tgtTagCombo.Items.Add(display);
                }
                if (!string.IsNullOrEmpty(mapping.TargetTag))
                    foreach (var item in tgtTagCombo.Items)
                        if (item.ToString() == mapping.TargetTag) { tgtTagCombo.SelectedItem = item; break; }
            }

            // Initial tag list fill — ÖNCE tagleri doldur, SONRA event handler bağla
            // Aksi halde Items.Clear() → SelectionChanged → tag=null → save → mapping kayboluyor!
            _suppressSave = true;
            if (!string.IsNullOrEmpty(mapping.SourceRobotName)) UpdateSourceTags();
            if (!string.IsNullOrEmpty(mapping.TargetRobotName)) UpdateTargetTags();
            _suppressSave = false;

            srcRobotCombo.SelectionChanged += (s, e) => { if (_suppressSave) return; mapping.SourceRobotName = srcRobotCombo.SelectedItem?.ToString(); _suppressSave = true; UpdateSourceTags(); _suppressSave = false; SaveRobotRobotMappings(); };
            srcTagCombo.SelectionChanged += (s, e) => { if (_suppressSave) return; mapping.SourceTag = srcTagCombo.SelectedItem?.ToString(); SaveRobotRobotMappings(); };
            tgtRobotCombo.SelectionChanged += (s, e) => { if (_suppressSave) return; mapping.TargetRobotName = tgtRobotCombo.SelectedItem?.ToString(); _suppressSave = true; UpdateTargetTags(); _suppressSave = false; SaveRobotRobotMappings(); };
            tgtTagCombo.SelectionChanged += (s, e) => { if (_suppressSave) return; mapping.TargetTag = tgtTagCombo.SelectedItem?.ToString(); SaveRobotRobotMappings(); };

            rowBorder.Child = grid;
            return rowBorder;
        }

        // ProcessRobotRobotMappingsAsync artik KukaRobotManager.ProcessRobotRobotBridgeAsync() tarafindan global olarak calistiriliyor


        private void LoadRobotRobotMappings()
        {
            // Robot-Robot mappingler artik KukaRobotManager.Initialize() da global olarak yukleniyor
        }

        /// <summary>
        /// Robot\u2194Robot haberle\u015fme tablosuna eksik varsay\u0131lan e\u015fle\u015fmeleri ekler.
        /// Mevcut e\u015fle\u015fmeleri korur, sadece eksikleri ekler (merge).
        /// </summary>
        private void MergeDefaultRobotRobotMappings()
        {
            var robots = KukaRobotManager.Instance.Robots;
            var robot1 = robots.FirstOrDefault(r => r.Name == "ROBOT 1");
            var robot2 = robots.FirstOrDefault(r => r.Name == "ROBOT 2");
            if (robot1 == null || robot2 == null) return;

            // Mevcut e\u015fle\u015fmelerin anahtar seti (Source|SourceVar|Target|TargetVar)
            var existingKeys = new HashSet<string>(
                _robotRobotMappings
                    .Where(m => !string.IsNullOrEmpty(m.SourceTag) && !string.IsNullOrEmpty(m.TargetTag))
                    .Select(m => $"{m.SourceRobotName}|{m.SourceTag.Split(' ')[0]}|{m.TargetRobotName}|{m.TargetTag.Split(' ')[0]}"),
                StringComparer.OrdinalIgnoreCase);

            string FindTag(KukaRobotInstance robot, string name)
            {
                var v = robot.InputVars.Concat(robot.OutputVars)
                    .FirstOrDefault(x => x.Name == name);
                return v != null ? $"{v.Name} ({v.PlcTag})" : null;
            }

            bool added = false;
            void AddRRIfMissing(string srcRobotName, KukaRobotInstance srcRobot,
                       string srcVarName, string tgtRobotName,
                       KukaRobotInstance tgtRobot, string tgtVarName)
            {
                var key = $"{srcRobotName}|{srcVarName}|{tgtRobotName}|{tgtVarName}";
                if (existingKeys.Contains(key)) return;

                var sourceTag = FindTag(srcRobot, srcVarName);
                var targetTag = FindTag(tgtRobot, tgtVarName);
                if (sourceTag == null || targetTag == null) return;

                _robotRobotMappings.Add(new RobotRobotMapping
                {
                    SourceRobotName = srcRobotName,
                    SourceTag = sourceTag,
                    TargetRobotName = tgtRobotName,
                    TargetTag = targetTag,
                    IsActive = true
                });
                existingKeys.Add(key);
                added = true;
            }

            // \u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550
            // ROBOT 1 \u2192 ROBOT 2 : Tabla Offset Aktar\u0131m\u0131
            // \u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550
            AddRRIfMissing("ROBOT 1", robot1, "G_TABLA_OFFSET_X", "ROBOT 2", robot2, "G_TABLA_OFFSET_X");
            AddRRIfMissing("ROBOT 1", robot1, "G_TABLA_OFFSET_Y", "ROBOT 2", robot2, "G_TABLA_OFFSET_Y");
            AddRRIfMissing("ROBOT 1", robot1, "G_TABLA_OFFSET_Z", "ROBOT 2", robot2, "G_TABLA_OFFSET_Z");
            AddRRIfMissing("ROBOT 1", robot1, "G_TABLA_OFFSET_A", "ROBOT 2", robot2, "G_TABLA_OFFSET_A");
            AddRRIfMissing("ROBOT 1", robot1, "G_TABLA_OFFSET_B", "ROBOT 2", robot2, "G_TABLA_OFFSET_B");
            AddRRIfMissing("ROBOT 1", robot1, "G_TABLA_OFFSET_C", "ROBOT 2", robot2, "G_TABLA_OFFSET_C");
            AddRRIfMissing("ROBOT 1", robot1, "G_TABLA_OFFSET_HAZIR", "ROBOT 2", robot2, "G_TABLA_OFFSET_HAZIR");

            // \u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550
            // ROBOT 1 \u2192 ROBOT 2 : Home + Durum Bilgileri
            // \u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550
            AddRRIfMissing("ROBOT 1", robot1, "G_R1_HOME", "ROBOT 2", robot2, "G_R1_HOME");
            AddRRIfMissing("ROBOT 1", robot1, "G_IS_BITTI", "ROBOT 2", robot2, "G_R1_IS_BITTI");
            AddRRIfMissing("ROBOT 1", robot1, "G_ROBOT_DURUM", "ROBOT 2", robot2, "G_R1_ROBOT_DURUM");
            AddRRIfMissing("ROBOT 1", robot1, "G_HATA_VAR", "ROBOT 2", robot2, "G_R1_HATA_VAR");
            AddRRIfMissing("ROBOT 1", robot1, "G_HATA_KODU", "ROBOT 2", robot2, "G_R1_HATA_KODU");

            // \u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550
            // ROBOT 1 \u2192 ROBOT 2 : Harici Eksen Pozisyonu
            // \u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550
            AddRRIfMissing("ROBOT 1", robot1, "AXIS_E1", "ROBOT 2", robot2, "G_R1_EKSEN_E1");

            // \u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550
            // ROBOT 2 \u2192 ROBOT 1 : Home + Durum Bilgileri
            // \u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550
            AddRRIfMissing("ROBOT 2", robot2, "G_R2_HOME", "ROBOT 1", robot1, "G_R2_HOME");
            AddRRIfMissing("ROBOT 2", robot2, "G_IS_BITTI", "ROBOT 1", robot1, "G_R2_IS_BITTI");
            AddRRIfMissing("ROBOT 2", robot2, "G_ROBOT_DURUM", "ROBOT 1", robot1, "G_R2_ROBOT_DURUM");
            AddRRIfMissing("ROBOT 2", robot2, "G_HATA_VAR", "ROBOT 1", robot1, "G_R2_HATA_VAR");
            AddRRIfMissing("ROBOT 2", robot2, "G_HATA_KODU", "ROBOT 1", robot1, "G_R2_HATA_KODU");

            // \u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550
            // ROBOT 2 \u2192 ROBOT 1 : Harici Eksen (KL100 Slider) Pozisyonu
            // \u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550
            AddRRIfMissing("ROBOT 2", robot2, "AXIS_E1", "ROBOT 1", robot1, "G_R2_EKSEN_E1");

            // ══════════════════════════════════
            // ROBOT 2 → ROBOT 1 : Slider Durum Sinyalleri
            // ══════════════════════════════════
            AddRRIfMissing("ROBOT 2", robot2, "G_SLIDER_TAMAM", "ROBOT 1", robot1, "G_R2_SLIDER_TAMAM");
            AddRRIfMissing("ROBOT 2", robot2, "G_SLIDER_HOME", "ROBOT 1", robot1, "G_R2_SLIDER_HOME");
            AddRRIfMissing("ROBOT 2", robot2, "G_SLIDER_AKTUEL_POZ", "ROBOT 1", robot1, "G_R2_SLIDER_POZ");

            if (added)
                SaveRobotRobotMappings();
        }

        private void SaveRobotRobotMappings()
        {
            KukaRobotManager.Instance.SaveRobotRobotMappings();
        }

        #endregion

        #region Handshake Değişkenleri Tablosu

        private void BtnAddHandshakeEntry_Click(object sender, RoutedEventArgs e)
        {
            KukaRobotManager.Instance.HandshakeEntries.Add(new HandshakeEntry());
            KukaRobotManager.Instance.SaveHandshakeConfig();
            RefreshHandshakeRows();
        }

        private void RefreshHandshakeRows()
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                HandshakeRowsContainer.Children.Clear();
                HandshakeCountText.Text = $"{KukaRobotManager.Instance.HandshakeEntries.Count} Değişken";

                for (int i = 0; i < KukaRobotManager.Instance.HandshakeEntries.Count; i++)
                {
                    var row = CreateHandshakeRow(KukaRobotManager.Instance.HandshakeEntries[i], i + 1);
                    HandshakeRowsContainer.Children.Add(row);
                }
            });
        }

        private Border CreateHandshakeRow(HandshakeEntry entry, int rowNumber)
        {
            var rowBorder = new Border
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 26, 26, 28)),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 3, 4, 3)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });

            // Col 0: Satır numarası
            grid.Children.Add(CreateTextInColumn(rowNumber.ToString(), 0, 9, "#646464"));

            // Col 1: Robot ComboBox
            var robotCombo = new ComboBox
            {
                FontSize = 9, Height = 26,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 37, 40)),
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                Margin = new Thickness(2, 0, 2, 0)
            };
            foreach (var r in KukaRobotManager.Instance.Robots)
                robotCombo.Items.Add(r.Name);
            if (!string.IsNullOrEmpty(entry.RobotName))
                robotCombo.SelectedItem = entry.RobotName;
            Grid.SetColumn(robotCombo, 1);
            grid.Children.Add(robotCombo);

            // Col 2: Değişken ComboBox (OutputVars listesi + elle giriş)
            var varCombo = new ComboBox
            {
                FontSize = 9, Height = 26,
                IsEditable = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 37, 40)),
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                Margin = new Thickness(2, 0, 2, 0)
            };
            Grid.SetColumn(varCombo, 2);
            grid.Children.Add(varCombo);

            // Col 3: Tip TextBlock (otomatik doldurulur)
            var typeText = new TextBlock
            {
                Text = entry.Type ?? "?",
                FontSize = 9,
                Foreground = new SolidColorBrush(ParseColor("#00A4EF")),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(typeText, 3);
            grid.Children.Add(typeText);

            // Col 4: Varsayılan Değer TextBox
            var defaultValueBox = new TextBox
            {
                Text = entry.DefaultValue ?? "",
                FontSize = 9, Height = 26,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 37, 38)),
                Foreground = new SolidColorBrush(ParseColor("#4CAF50")),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 51, 51)),
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Margin = new Thickness(2, 0, 2, 0)
            };
            defaultValueBox.LostFocus += (s, e) =>
            {
                entry.DefaultValue = defaultValueBox.Text;
                KukaRobotManager.Instance.SaveHandshakeConfig();
            };
            Grid.SetColumn(defaultValueBox, 4);
            grid.Children.Add(defaultValueBox);

            // Col 5: Son Yazılan Değer (runtime, salt okunur)
            var lastWrittenText = new TextBlock
            {
                Text = entry.LastWrittenValue ?? "-",
                FontSize = 10,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new SolidColorBrush(ParseColor("#4CAF50")),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            entry.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(HandshakeEntry.LastWrittenValue))
                    this.DispatcherQueue.TryEnqueue(() => lastWrittenText.Text = entry.LastWrittenValue ?? "-");
            };
            Grid.SetColumn(lastWrittenText, 5);
            grid.Children.Add(lastWrittenText);

            // Col 6: Aktif toggle
            var activeToggle = new ToggleSwitch
            {
                IsOn = entry.IsActive,
                MinWidth = 0, MinHeight = 0,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            activeToggle.Toggled += (s, e) =>
            {
                entry.IsActive = activeToggle.IsOn;
                KukaRobotManager.Instance.SaveHandshakeConfig();
            };
            Grid.SetColumn(activeToggle, 6);
            grid.Children.Add(activeToggle);

            // Col 7: Sıra butonları (▲▼)
            var hsReorderPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Spacing = 1 };
            var hsUpBtn = new Button { Content = "\uE70E", FontFamily = new FontFamily("Segoe MDL2 Assets"), Background = new SolidColorBrush(Colors.Transparent), Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136)), BorderThickness = new Thickness(0), Padding = new Thickness(1), FontSize = 9, MinWidth = 18, MinHeight = 18, Height = 18, Width = 18 };
            var hsDownBtn = new Button { Content = "\uE70D", FontFamily = new FontFamily("Segoe MDL2 Assets"), Background = new SolidColorBrush(Colors.Transparent), Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136)), BorderThickness = new Thickness(0), Padding = new Thickness(1), FontSize = 9, MinWidth = 18, MinHeight = 18, Height = 18, Width = 18 };
            var hsEntries = KukaRobotManager.Instance.HandshakeEntries;
            hsUpBtn.Click += (s, e) => { int idx = hsEntries.IndexOf(entry); if (idx > 0) { hsEntries.Move(idx, idx - 1); KukaRobotManager.Instance.SaveHandshakeConfig(); RefreshHandshakeRows(); } };
            hsDownBtn.Click += (s, e) => { int idx = hsEntries.IndexOf(entry); if (idx >= 0 && idx < hsEntries.Count - 1) { hsEntries.Move(idx, idx + 1); KukaRobotManager.Instance.SaveHandshakeConfig(); RefreshHandshakeRows(); } };
            hsReorderPanel.Children.Add(hsUpBtn);
            hsReorderPanel.Children.Add(hsDownBtn);
            Grid.SetColumn(hsReorderPanel, 7);
            grid.Children.Add(hsReorderPanel);

            // Col 8: Sil butonu
            var deleteBtn = new Button
            {
                Content = "\uE74D",
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"),
                Background = new SolidColorBrush(Colors.Transparent),
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 82, 82)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            deleteBtn.Click += (s, e) =>
            {
                KukaRobotManager.Instance.HandshakeEntries.Remove(entry);
                KukaRobotManager.Instance.SaveHandshakeConfig();
                RefreshHandshakeRows();
            };
            Grid.SetColumn(deleteBtn, 8);
            grid.Children.Add(deleteBtn);

            // ---- Değişken dropdown doldurma mantığı ----
            void UpdateVariableList()
            {
                varCombo.Items.Clear();
                var selectedRobot = KukaRobotManager.Instance.Robots
                    .FirstOrDefault(r => r.Name == robotCombo.SelectedItem?.ToString());
                if (selectedRobot == null) return;

                foreach (var v in selectedRobot.OutputVars)
                {
                    if (!string.IsNullOrEmpty(v.PlcTag))
                        varCombo.Items.Add(v.PlcTag);
                }

                if (!string.IsNullOrEmpty(entry.PlcTag))
                {
                    // Listede varsa seç
                    foreach (var item in varCombo.Items)
                    {
                        if (item.ToString() == entry.PlcTag)
                        {
                            varCombo.SelectedItem = item;
                            break;
                        }
                    }
                    // Listede yoksa text olarak göster (elle girilmiş olabilir)
                    if (varCombo.SelectedItem == null)
                        varCombo.Text = entry.PlcTag;
                }
            }

            void UpdateTypeFromVariable()
            {
                var selectedRobot = KukaRobotManager.Instance.Robots
                    .FirstOrDefault(r => r.Name == robotCombo.SelectedItem?.ToString());
                if (selectedRobot == null) return;
                string varName = varCombo.SelectedItem?.ToString() ?? varCombo.Text;
                var matchedVar = selectedRobot.OutputVars
                    .FirstOrDefault(v => v.PlcTag == varName);
                if (matchedVar != null)
                {
                    entry.Type = matchedVar.Type;
                    typeText.Text = matchedVar.Type;
                }
            }

            robotCombo.SelectionChanged += (s, e) =>
            {
                entry.RobotName = robotCombo.SelectedItem?.ToString();
                UpdateVariableList();
                KukaRobotManager.Instance.SaveHandshakeConfig();
            };

            varCombo.SelectionChanged += (s, e) =>
            {
                if (varCombo.SelectedItem != null)
                {
                    entry.PlcTag = varCombo.SelectedItem.ToString();
                    UpdateTypeFromVariable();
                    KukaRobotManager.Instance.SaveHandshakeConfig();
                }
            };

            // Elle giriş yapıldığında da kaydet
            varCombo.LostFocus += (s, e) =>
            {
                string text = varCombo.Text?.Trim();
                if (!string.IsNullOrEmpty(text) && text != entry.PlcTag)
                {
                    entry.PlcTag = text;
                    UpdateTypeFromVariable();
                    KukaRobotManager.Instance.SaveHandshakeConfig();
                }
            };

            // İlk yüklemede dropdown'ı doldur
            if (!string.IsNullOrEmpty(entry.RobotName))
                UpdateVariableList();

            rowBorder.Child = grid;
            return rowBorder;
        }

        #endregion

        // Helper class
        private class RobotPanelControls
        {
            public Ellipse StatusLed { get; set; }
            public Border StatusBadge { get; set; }
            public TextBlock StatusText { get; set; }
            public TextBlock ProText { get; set; }
            public TextBlock JogText { get; set; }
        }

        // Unused colors reference
        private static class Colors
        {
            public static Windows.UI.Color Transparent => Windows.UI.Color.FromArgb(0, 0, 0, 0);
        }
    }
}
