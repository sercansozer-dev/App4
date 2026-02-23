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
            RefreshMappingRows();

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
                foreach (var robot in KukaRobotManager.Instance.Robots)
                {
                    if (robot.InputVars.Contains(v)) { robot.InputVars.Remove(v); return; }
                    if (robot.OutputVars.Contains(v)) { robot.OutputVars.Remove(v); return; }
                }
            }
        }

        private void Variable_Edited_LostFocus(object sender, RoutedEventArgs e) { }

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

        private void BtnAddMapping_Click(object sender, RoutedEventArgs e)
        {
            _mappings.Add(new RobotPlcMapping());
            RefreshMappingRows();
            SaveMappings();
            MappingCountText.Text = $"{_mappings.Count} Eşleşme";
        }

        private void RefreshMappingRows()
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                MappingRowsContainer.Children.Clear();
                MappingCountText.Text = $"{_mappings.Count} Eşleşme";

                for (int i = 0; i < _mappings.Count; i++)
                {
                    var mapping = _mappings[i];
                    var row = CreateMappingRow(mapping, i + 1);
                    MappingRowsContainer.Children.Add(row);
                }
            });
        }

        private Border CreateMappingRow(RobotPlcMapping mapping, int rowNumber)
        {
            var rowBorder = new Border
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 26, 26, 28)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4, 8, 4)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });   // #
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });  // Yön (Robot→PLC / PLC→Robot)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Robot
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Robot Tag
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // PLC Tag
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });   // Değer
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });   // Aktif
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });   // Sil

            // # Sıra numarası
            var numText = new TextBlock
            {
                Text = rowNumber.ToString(),
                FontSize = 10,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(numText, 0);
            grid.Children.Add(numText);

            // Yön seçimi ComboBox (Robot→PLC / PLC→Robot)
            var directionCombo = new ComboBox
            {
                FontSize = 9,
                Height = 28,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 45, 45, 48)),
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                Margin = new Thickness(2, 0, 2, 0)
            };
            directionCombo.Items.Add("Robot→PLC");
            directionCombo.Items.Add("PLC→Robot");
            directionCombo.SelectedItem = mapping.Direction ?? "Robot→PLC";
            directionCombo.SelectionChanged += (s, e) =>
            {
                mapping.Direction = directionCombo.SelectedItem?.ToString() ?? "Robot→PLC";
                SaveMappings();
            };
            Grid.SetColumn(directionCombo, 1);
            grid.Children.Add(directionCombo);

            // Robot seçimi ComboBox
            var robotCombo = new ComboBox
            {
                FontSize = 10,
                Height = 28,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 45, 45, 48)),
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                Margin = new Thickness(2, 0, 2, 0)
            };
            foreach (var robot in KukaRobotManager.Instance.Robots)
            {
                robotCombo.Items.Add(robot.Name);
            }
            if (!string.IsNullOrEmpty(mapping.RobotName))
            {
                robotCombo.SelectedItem = mapping.RobotName;
            }
            Grid.SetColumn(robotCombo, 2);
            grid.Children.Add(robotCombo);

            // Robot Tag seçimi ComboBox
            var robotTagCombo = new ComboBox
            {
                FontSize = 10,
                Height = 28,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 45, 45, 48)),
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                Margin = new Thickness(2, 0, 2, 0)
            };
            Grid.SetColumn(robotTagCombo, 3);
            grid.Children.Add(robotTagCombo);

            // PLC Tag seçimi ComboBox
            var plcTagCombo = new ComboBox
            {
                FontSize = 10,
                Height = 28,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 45, 45, 48)),
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                Margin = new Thickness(2, 0, 2, 0)
            };
            foreach (var plcVar in PlcService.Instance.OutputVariables.Concat(PlcService.Instance.InputVariables))
            {
                string display = $"{plcVar.Name} ({plcVar.Address})";
                plcTagCombo.Items.Add(display);
            }
            if (!string.IsNullOrEmpty(mapping.PlcTag))
            {
                foreach (var item in plcTagCombo.Items)
                {
                    if (item.ToString() == mapping.PlcTag)
                    {
                        plcTagCombo.SelectedItem = item;
                        break;
                    }
                }
            }
            Grid.SetColumn(plcTagCombo, 4);
            grid.Children.Add(plcTagCombo);

            // Değer gösterimi
            var valueText = new TextBlock
            {
                Text = mapping.LastValue ?? "-",
                FontSize = 10,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            mapping.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(RobotPlcMapping.LastValue))
                {
                    this.DispatcherQueue.TryEnqueue(() => valueText.Text = mapping.LastValue ?? "-");
                }
            };
            Grid.SetColumn(valueText, 5);
            grid.Children.Add(valueText);

            // Aktif/Pasif toggle
            var activeToggle = new ToggleSwitch
            {
                IsOn = mapping.IsActive,
                MinWidth = 0,
                MinHeight = 0,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            activeToggle.Toggled += (s, e) =>
            {
                mapping.IsActive = activeToggle.IsOn;
                SaveMappings();
            };
            Grid.SetColumn(activeToggle, 6);
            grid.Children.Add(activeToggle);

            // Sil butonu
            var deleteBtn = new Button
            {
                Content = "\uE74D",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                Background = new SolidColorBrush(Colors.Transparent),
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 82, 82)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            deleteBtn.Click += (s, e) =>
            {
                _mappings.Remove(mapping);
                RefreshMappingRows();
                SaveMappings();
                LogMessage($"🗑️ Köprü eşleşmesi silindi");
            };
            Grid.SetColumn(deleteBtn, 7);
            grid.Children.Add(deleteBtn);

            // Robot tag listesini güncelleme
            void UpdateRobotTags()
            {
                robotTagCombo.Items.Clear();
                string selectedRobotName = robotCombo.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(selectedRobotName)) return;

                var selectedRobot = KukaRobotManager.Instance.Robots.FirstOrDefault(r => r.Name == selectedRobotName);
                if (selectedRobot == null) return;

                foreach (var v in selectedRobot.InputVars)
                {
                    robotTagCombo.Items.Add($"{v.Name} ({v.PlcTag})");
                }
                foreach (var v in selectedRobot.OutputVars)
                {
                    robotTagCombo.Items.Add($"{v.Name} ({v.PlcTag})");
                }

                if (!string.IsNullOrEmpty(mapping.RobotTag))
                {
                    foreach (var item in robotTagCombo.Items)
                    {
                        if (item.ToString() == mapping.RobotTag)
                        {
                            robotTagCombo.SelectedItem = item;
                            break;
                        }
                    }
                }
            }

            robotCombo.SelectionChanged += (s, e) =>
            {
                mapping.RobotName = robotCombo.SelectedItem?.ToString();
                UpdateRobotTags();
                SaveMappings();
            };

            robotTagCombo.SelectionChanged += (s, e) =>
            {
                mapping.RobotTag = robotTagCombo.SelectedItem?.ToString();
                SaveMappings();
            };

            plcTagCombo.SelectionChanged += (s, e) =>
            {
                mapping.PlcTag = plcTagCombo.SelectedItem?.ToString();
                SaveMappings();
            };

            // İlk yüklemede robot tag listesini doldur
            if (!string.IsNullOrEmpty(mapping.RobotName))
            {
                UpdateRobotTags();
            }

            rowBorder.Child = grid;
            return rowBorder;
        }

        private async Task ProcessMappingsAsync()
        {
            foreach (var mapping in _mappings)
            {
                if (!mapping.IsActive || string.IsNullOrEmpty(mapping.RobotName) ||
                    string.IsNullOrEmpty(mapping.RobotTag) || string.IsNullOrEmpty(mapping.PlcTag))
                    continue;

                try
                {
                    // Robotu bul
                    var robot = KukaRobotManager.Instance.Robots.FirstOrDefault(r => r.Name == mapping.RobotName);
                    if (robot == null || !robot.IsConnected) continue;

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

                    if (robotVar == null) continue;

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

                    if (plcVar == null) continue;

                    // ÇİFT YÖNLÜ AKTARIM
                    string direction = mapping.Direction ?? "Robot→PLC";

                    if (direction == "Robot→PLC")
                    {
                        // Robot'tan oku → PLC'ye yaz
                        if (string.IsNullOrEmpty(robotVar.Value)) continue;
                        string currentValue = robotVar.Value;
                        mapping.LastValue = currentValue;

                        if (PlcService.Instance.IsConnected && plcVar.Value != currentValue)
                        {
                            await PlcService.Instance.WriteAsync(plcVar, currentValue);
                        }
                    }
                    else // PLC→Robot
                    {
                        // PLC'den oku → Robot'a yaz
                        string plcValue = plcVar.CurrentValue?.ToString() ?? plcVar.Value;
                        if (string.IsNullOrEmpty(plcValue)) continue;
                        mapping.LastValue = plcValue;

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
