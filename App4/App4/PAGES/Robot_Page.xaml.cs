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
        private ObservableCollection<RobotPlcMapping> _mappings = new();
        private readonly string _mappingsConfigPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "App4", "RobotPlcMappings.json");

        // Robot-Robot Haberleşme eşleşmeleri
        private ObservableCollection<RobotRobotMapping> _robotRobotMappings = new();
        private readonly string _robotRobotConfigPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "App4", "RobotRobotMappings.json");

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
            colHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(25) });

            var headers = new[] { "#", "İsim", "Tip", "KRL Tag", "Değer", "Sil" };
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

            // Köprü eşleşmelerini işle
            _ = ProcessMappingsAsync();
            _ = ProcessRobotRobotMappingsAsync();
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

        private void Variable_Edited_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is TextBox tb && tb.DataContext is PlcVariable v)
                {
                    // Binding updates should already have modified the PlcVariable instance.
                    // Persist robot variable definitions to disk so they survive restart.
                    try { KukaRobotManager.Instance.SaveRobotVariables(); } catch { }
                }
            }
            catch { }
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

                    var inputPanel = CreateBridgeTablePanel(robot, "Input", "\uE9D2 INPUT (PLC \u2192 Robot)", "#4CAF50", inputMappings);
                    Grid.SetColumn(inputPanel, 0);
                    inputPanel.Margin = new Thickness(0, 0, 3, 0);
                    tablesGrid.Children.Add(inputPanel);

                    var outputPanel = CreateBridgeTablePanel(robot, "Output", "\uE7E8 OUTPUT (Robot \u2192 PLC)", "#FF9800", outputMappings);
                    Grid.SetColumn(outputPanel, 1);
                    outputPanel.Margin = new Thickness(3, 0, 0, 0);
                    tablesGrid.Children.Add(outputPanel);

                    Grid.SetRow(tablesGrid, 1);
                    mainGrid.Children.Add(tablesGrid);

                    panelBorder.Child = mainGrid;
                    BridgeTablesContainer.Children.Add(panelBorder);
                }
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
            colGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });

            string[] colHeaders = isInput
                ? new[] { "#", "PLC Tag", "\u2192", "Robot Tag", "Değer", "Aktif", "Sil" }
                : new[] { "#", "Robot Tag", "\u2192", "PLC Tag", "Değer", "Aktif", "Sil" };

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

                sourceCombo.SelectionChanged += (s, e) => { mapping.PlcTag = sourceCombo.SelectedItem?.ToString(); SaveMappings(); };
                targetCombo.SelectionChanged += (s, e) => { mapping.RobotTag = targetCombo.SelectedItem?.ToString(); SaveMappings(); };
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

                sourceCombo.SelectionChanged += (s, e) => { mapping.RobotTag = sourceCombo.SelectedItem?.ToString(); SaveMappings(); };
                targetCombo.SelectionChanged += (s, e) => { mapping.PlcTag = targetCombo.SelectedItem?.ToString(); SaveMappings(); };
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
            Grid.SetColumn(deleteBtn, 6);
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

        private async Task ProcessMappingsAsync()
        {
            foreach (var mapping in _mappings)
            {
                if (string.IsNullOrEmpty(mapping.RobotName) ||
                    string.IsNullOrEmpty(mapping.RobotTag) || string.IsNullOrEmpty(mapping.PlcTag))
                    continue;

                try
                {
                    // Robotu bul
                    var robot = KukaRobotManager.Instance.Robots.FirstOrDefault(r => r.Name == mapping.RobotName);
                    if (robot == null) continue;

                    // Robot tag'inden değişkeni bul
                    string robotTagDisplay = mapping.RobotTag;
                    PlcVariable robotVar = null;

                    foreach (var v in robot.InputVars.Concat(robot.OutputVars))
                    {
                        if ($"{v.Name} ({v.PlcTag})" == robotTagDisplay)
                        {
                            robotVar = v;
                            break;
                        }
                    }

                    // PLC tag'ini bul
                    string plcTagDisplay = mapping.PlcTag;
                    PlcVariable plcVar = null;

                    foreach (var v in PlcService.Instance.OutputVariables.Concat(PlcService.Instance.InputVariables))
                    {
                        if ($"{v.Name} ({v.Address})" == plcTagDisplay)
                        {
                            plcVar = v;
                            break;
                        }
                    }

                    // ANLIK DEĞER GÖSTERİMİ: Aktif olmasa bile mevcut değeri oku ve göster
                    string direction = mapping.Direction ?? "Robot→PLC";
                    if (direction == "Robot→PLC" && robotVar != null && !string.IsNullOrEmpty(robotVar.Value))
                    {
                        mapping.LastValue = robotVar.Value;
                    }
                    else if (direction == "PLC→Robot" && plcVar != null)
                    {
                        string plcValue = plcVar.CurrentValue?.ToString() ?? plcVar.Value;
                        if (!string.IsNullOrEmpty(plcValue))
                            mapping.LastValue = plcValue;
                    }

                    // Aktif değilse sadece okuma yap, yazma yapma
                    if (!mapping.IsActive) continue;
                    if (robotVar == null || plcVar == null) continue;

                    // ÇİFT YÖNLÜ AKTARIM (sadece aktif mapping'ler için)
                    if (direction == "Robot→PLC")
                    {
                        // Robot'tan oku → PLC'ye yaz
                        if (string.IsNullOrEmpty(robotVar.Value)) continue;
                        string currentValue = robotVar.Value;

                        if (robot.IsConnected && PlcService.Instance.IsConnected && plcVar.Value != currentValue)
                        {
                            await PlcService.Instance.WriteAsync(plcVar, currentValue);
                        }
                    }
                    else // PLC→Robot
                    {
                        // PLC'den oku → Robot'a yaz
                        string plcValue = plcVar.CurrentValue?.ToString() ?? plcVar.Value;
                        if (string.IsNullOrEmpty(plcValue)) continue;

                        if (robot.IsConnected && robotVar.Value != plcValue)
                        {
                            await robot.WriteVariableAsync(robotVar.PlcTag, plcValue);
                        }
                    }
                }
                catch { }
            }
        }

        private void LoadMappings()
        {
            try
            {
                if (File.Exists(_mappingsConfigPath))
                {
                    string json = File.ReadAllText(_mappingsConfigPath);
                    var list = JsonSerializer.Deserialize<List<RobotPlcMapping>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (list != null)
                    {
                        _mappings.Clear();
                        foreach (var m in list)
                            _mappings.Add(m);
                    }
                }
            }
            catch { }

            // Bo\u015f girdileri temizle
            var emptyEntries = _mappings.Where(m => string.IsNullOrEmpty(m.RobotTag) && string.IsNullOrEmpty(m.PlcTag)).ToList();
            foreach (var e in emptyEntries)
                _mappings.Remove(e);

            // Her zaman eksik varsay\u0131lan e\u015fle\u015fmeleri kontrol et ve ekle (merge)
            MergeDefaultBridgeMappings();
        }

        /// <summary>
        /// Robot-PLC k\u00f6pr\u00fc tablolar\u0131na eksik varsay\u0131lan e\u015fle\u015fmeleri ekler.
        /// Mevcut e\u015fle\u015fmeleri korur, sadece eksikleri ekler (merge).
        /// </summary>
        private void MergeDefaultBridgeMappings()
        {
            PlcService.Instance.EnsureRobotBridgeVariables();

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
            try
            {
                var json = JsonSerializer.Serialize(_mappings.ToList(), new JsonSerializerOptions { WriteIndented = true });
                var dir = System.IO.Path.GetDirectoryName(_mappingsConfigPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(_mappingsConfigPath, json);
            }
            catch { }
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
                RobotRobotRowsContainer.Children.Clear();
                RobotRobotCountText.Text = $"{_robotRobotMappings.Count} Eşleşme";

                for (int i = 0; i < _robotRobotMappings.Count; i++)
                {
                    var row = CreateRobotRobotMappingRow(_robotRobotMappings[i], i + 1);
                    RobotRobotRowsContainer.Children.Add(row);
                }
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
            Grid.SetColumn(deleteBtn, 8);
            grid.Children.Add(deleteBtn);

            // Source Robot tag list update
            void UpdateSourceTags()
            {
                srcTagCombo.Items.Clear();
                var selectedRobot = KukaRobotManager.Instance.Robots.FirstOrDefault(r => r.Name == srcRobotCombo.SelectedItem?.ToString());
                if (selectedRobot == null) return;
                foreach (var v in selectedRobot.InputVars.Concat(selectedRobot.OutputVars))
                    srcTagCombo.Items.Add($"{v.Name} ({v.PlcTag})");
                if (!string.IsNullOrEmpty(mapping.SourceTag))
                    foreach (var item in srcTagCombo.Items)
                        if (item.ToString() == mapping.SourceTag) { srcTagCombo.SelectedItem = item; break; }
            }

            void UpdateTargetTags()
            {
                tgtTagCombo.Items.Clear();
                var selectedRobot = KukaRobotManager.Instance.Robots.FirstOrDefault(r => r.Name == tgtRobotCombo.SelectedItem?.ToString());
                if (selectedRobot == null) return;
                foreach (var v in selectedRobot.InputVars.Concat(selectedRobot.OutputVars))
                    tgtTagCombo.Items.Add($"{v.Name} ({v.PlcTag})");
                if (!string.IsNullOrEmpty(mapping.TargetTag))
                    foreach (var item in tgtTagCombo.Items)
                        if (item.ToString() == mapping.TargetTag) { tgtTagCombo.SelectedItem = item; break; }
            }

            srcRobotCombo.SelectionChanged += (s, e) => { mapping.SourceRobotName = srcRobotCombo.SelectedItem?.ToString(); UpdateSourceTags(); SaveRobotRobotMappings(); };
            srcTagCombo.SelectionChanged += (s, e) => { mapping.SourceTag = srcTagCombo.SelectedItem?.ToString(); SaveRobotRobotMappings(); };
            tgtRobotCombo.SelectionChanged += (s, e) => { mapping.TargetRobotName = tgtRobotCombo.SelectedItem?.ToString(); UpdateTargetTags(); SaveRobotRobotMappings(); };
            tgtTagCombo.SelectionChanged += (s, e) => { mapping.TargetTag = tgtTagCombo.SelectedItem?.ToString(); SaveRobotRobotMappings(); };

            // Initial tag list fill
            if (!string.IsNullOrEmpty(mapping.SourceRobotName)) UpdateSourceTags();
            if (!string.IsNullOrEmpty(mapping.TargetRobotName)) UpdateTargetTags();

            rowBorder.Child = grid;
            return rowBorder;
        }

        private async Task ProcessRobotRobotMappingsAsync()
        {
            foreach (var mapping in _robotRobotMappings)
            {
                if (string.IsNullOrEmpty(mapping.SourceRobotName) || string.IsNullOrEmpty(mapping.SourceTag) ||
                    string.IsNullOrEmpty(mapping.TargetRobotName) || string.IsNullOrEmpty(mapping.TargetTag))
                    continue;

                try
                {
                    var sourceRobot = KukaRobotManager.Instance.Robots.FirstOrDefault(r => r.Name == mapping.SourceRobotName);
                    var targetRobot = KukaRobotManager.Instance.Robots.FirstOrDefault(r => r.Name == mapping.TargetRobotName);
                    if (sourceRobot == null || targetRobot == null) continue;

                    // Find source variable
                    PlcVariable sourceVar = null;
                    foreach (var v in sourceRobot.InputVars.Concat(sourceRobot.OutputVars))
                    {
                        if ($"{v.Name} ({v.PlcTag})" == mapping.SourceTag) { sourceVar = v; break; }
                    }
                    if (sourceVar == null) continue;

                    // Update last value display
                    if (!string.IsNullOrEmpty(sourceVar.Value))
                        mapping.LastValue = sourceVar.Value;

                    if (!mapping.IsActive) continue;

                    // Find target variable
                    PlcVariable targetVar = null;
                    foreach (var v in targetRobot.InputVars.Concat(targetRobot.OutputVars))
                    {
                        if ($"{v.Name} ({v.PlcTag})" == mapping.TargetTag) { targetVar = v; break; }
                    }
                    if (targetVar == null) continue;

                    // Transfer: read from source robot, write to target robot
                    string currentValue = sourceVar.Value;
                    if (string.IsNullOrEmpty(currentValue)) continue;

                    if (sourceRobot.IsConnected && targetRobot.IsConnected && targetVar.Value != currentValue)
                    {
                        await targetRobot.WriteVariableAsync(targetVar.PlcTag, currentValue);
                    }
                }
                catch { }
            }
        }

        private void LoadRobotRobotMappings()
        {
            try
            {
                if (File.Exists(_robotRobotConfigPath))
                {
                    string json = File.ReadAllText(_robotRobotConfigPath);
                    var list = JsonSerializer.Deserialize<List<RobotRobotMapping>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (list != null)
                    {
                        _robotRobotMappings.Clear();
                        foreach (var m in list)
                            _robotRobotMappings.Add(m);
                    }
                }
            }
            catch { }

            // Bo\u015f girdileri temizle
            var emptyEntries = _robotRobotMappings.Where(m => string.IsNullOrEmpty(m.SourceTag) && string.IsNullOrEmpty(m.TargetTag)).ToList();
            foreach (var e in emptyEntries)
                _robotRobotMappings.Remove(e);

            // Her zaman eksik varsay\u0131lan e\u015fle\u015fmeleri kontrol et ve ekle (merge)
            MergeDefaultRobotRobotMappings();
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

            if (added)
                SaveRobotRobotMappings();
        }

        private void SaveRobotRobotMappings()
        {
            try
            {
                var json = JsonSerializer.Serialize(_robotRobotMappings.ToList(), new JsonSerializerOptions { WriteIndented = true });
                var dir = System.IO.Path.GetDirectoryName(_robotRobotConfigPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(_robotRobotConfigPath, json);
            }
            catch { }
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
