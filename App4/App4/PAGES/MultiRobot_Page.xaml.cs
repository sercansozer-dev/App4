using App4.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Linq;

namespace App4.Pages
{
    public sealed partial class MultiRobot_Page : Page
    {
        private const int MAX_LOG_LINES = 100;
        private static bool _logHandlerAttached = false;

        public MultiRobot_Page()
        {
            this.InitializeComponent();

            // Manager'ı initialize et
            KukaRobotManager.Instance.Initialize(this.DispatcherQueue);
            
            // Log handler (sadece bir kez ekle)
            if (!_logHandlerAttached)
            {
                _logHandlerAttached = true;
                KukaRobotManager.Instance.OnLog += OnLogReceived;
            }
            
            // Robot kartlarını oluştur
            RefreshRobotCards();
            
            // Robot listesi değiştiğinde kartları yenile
            KukaRobotManager.Instance.Robots.CollectionChanged += (s, e) => RefreshRobotCards();
        }

        private void RefreshRobotCards()
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                RobotCardsPanel.Children.Clear();
                RobotCountText.Text = $"{KukaRobotManager.Instance.Robots.Count} Robot";

                foreach (var robot in KukaRobotManager.Instance.Robots)
                {
                    robot.UiDispatcher = this.DispatcherQueue;
                    var card = CreateRobotCard(robot);
                    RobotCardsPanel.Children.Add(card);
                }
            });
        }

        private Border CreateRobotCard(KukaRobotInstance robot)
        {
            // Ana kart
            var card = new Border
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 21, 21, 21)),
                CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(2),
                Padding = new Thickness(0)
            };

            // BorderBrush binding
            robot.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(robot.IsConnected) || e.PropertyName == nameof(robot.StatusColor))
                {
                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        card.BorderBrush = new SolidColorBrush(robot.IsConnected 
                            ? Windows.UI.Color.FromArgb(255, 0, 255, 136)
                            : Windows.UI.Color.FromArgb(255, 255, 68, 68));
                    });
                }
            };
            card.BorderBrush = new SolidColorBrush(robot.IsConnected 
                ? Windows.UI.Color.FromArgb(255, 0, 255, 136)
                : Windows.UI.Color.FromArgb(255, 255, 68, 68));

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Header
            var header = CreateRobotHeader(robot);
            Grid.SetRow(header, 0);
            mainGrid.Children.Add(header);

            // Content
            var content = CreateRobotContent(robot);
            Grid.SetRow(content, 1);
            mainGrid.Children.Add(content);

            card.Child = mainGrid;
            return card;
        }

        private Border CreateRobotHeader(KukaRobotInstance robot)
        {
            var header = new Border
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 26, 26, 26)),
                CornerRadius = new CornerRadius(10, 10, 0, 0),
                Padding = new Thickness(15, 0, 15, 0)
            };

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Sol taraf - İsim ve durum
            var leftPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, VerticalAlignment = VerticalAlignment.Center };
            
            var statusLed = new Ellipse { Width = 14, Height = 14 };
            robot.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(robot.IsConnected))
                {
                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        statusLed.Fill = new SolidColorBrush(robot.IsConnected 
                            ? Windows.UI.Color.FromArgb(255, 0, 255, 136)
                            : Windows.UI.Color.FromArgb(255, 255, 68, 68));
                    });
                }
            };
            statusLed.Fill = new SolidColorBrush(robot.IsConnected 
                ? Windows.UI.Color.FromArgb(255, 0, 255, 136)
                : Windows.UI.Color.FromArgb(255, 255, 68, 68));
            leftPanel.Children.Add(statusLed);

            var nameText = new TextBlock { Text = robot.Name, FontSize = 18, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Microsoft.UI.Colors.White) };
            leftPanel.Children.Add(nameText);

            var ipText = new TextBlock { Text = $"{robot.IpAddress}:{robot.Port}", FontSize = 12, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136)), VerticalAlignment = VerticalAlignment.Center };
            leftPanel.Children.Add(ipText);

            var statusBadge = new Border { CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 2, 8, 2), VerticalAlignment = VerticalAlignment.Center };
            var statusBadgeText = new TextBlock { FontSize = 10, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 10, 10, 10)) };
            robot.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(robot.IsConnected) || e.PropertyName == nameof(robot.StatusText))
                {
                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        statusBadge.Background = new SolidColorBrush(robot.IsConnected 
                            ? Windows.UI.Color.FromArgb(255, 0, 255, 136)
                            : Windows.UI.Color.FromArgb(255, 255, 68, 68));
                        statusBadgeText.Text = robot.StatusText;
                    });
                }
            };
            statusBadge.Background = new SolidColorBrush(robot.IsConnected 
                ? Windows.UI.Color.FromArgb(255, 0, 255, 136)
                : Windows.UI.Color.FromArgb(255, 255, 68, 68));
            statusBadgeText.Text = robot.StatusText;
            statusBadge.Child = statusBadgeText;
            leftPanel.Children.Add(statusBadge);

            Grid.SetColumn(leftPanel, 0);
            headerGrid.Children.Add(leftPanel);

            // Orta - Override değerleri
            var centerPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 20, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            
            var proStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
            proStack.Children.Add(new TextBlock { Text = "PRO:", FontSize = 11, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136)) });
            var proValue = new TextBlock { FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 170, 255)) };
            robot.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(robot.OverridePro)) this.DispatcherQueue.TryEnqueue(() => proValue.Text = robot.OverridePro.ToString()); };
            proValue.Text = robot.OverridePro.ToString();
            proStack.Children.Add(proValue);
            proStack.Children.Add(new TextBlock { Text = "%", FontSize = 11, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136)) });
            centerPanel.Children.Add(proStack);

            var jogStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
            jogStack.Children.Add(new TextBlock { Text = "JOG:", FontSize = 11, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136)) });
            var jogValue = new TextBlock { FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 170, 0)) };
            robot.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(robot.OverrideJog)) this.DispatcherQueue.TryEnqueue(() => jogValue.Text = robot.OverrideJog.ToString()); };
            jogValue.Text = robot.OverrideJog.ToString();
            jogStack.Children.Add(jogValue);
            jogStack.Children.Add(new TextBlock { Text = "%", FontSize = 11, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136)) });
            centerPanel.Children.Add(jogStack);

            Grid.SetColumn(centerPanel, 1);
            headerGrid.Children.Add(centerPanel);

            header.Child = headerGrid;
            return header;
        }

        private Grid CreateRobotContent(KukaRobotInstance robot)
        {
            var content = new Grid { Padding = new Thickness(15) };
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });

            // TCP Pozisyonu
            var tcpPanel = CreatePositionPanel(robot, "📍 TCP POZİSYONU", "#00FF88",
                new (string Label, Func<double> Value)[] { ("X:", () => robot.PosX), ("Y:", () => robot.PosY), ("Z:", () => robot.PosZ) },
                new (string Label, Func<double> Value)[] { ("A:", () => robot.PosA), ("B:", () => robot.PosB), ("C:", () => robot.PosC) });
            Grid.SetColumn(tcpPanel, 0);
            tcpPanel.Margin = new Thickness(0, 0, 8, 0);
            content.Children.Add(tcpPanel);

            // Eksen Açıları
            var axisPanel = CreatePositionPanel(robot, "🔧 EKSEN AÇILARI", "#FFAA00",
                new (string Label, Func<double> Value)[] { ("A1:", () => robot.A1), ("A2:", () => robot.A2), ("A3:", () => robot.A3) },
                new (string Label, Func<double> Value)[] { ("A4:", () => robot.A4), ("A5:", () => robot.A5), ("A6:", () => robot.A6) }, true);
            Grid.SetColumn(axisPanel, 1);
            axisPanel.Margin = new Thickness(8, 0, 8, 0);
            content.Children.Add(axisPanel);

            // Kontroller
            var controlPanel = CreateControlPanel(robot);
            Grid.SetColumn(controlPanel, 2);
            controlPanel.Margin = new Thickness(8, 0, 0, 0);
            content.Children.Add(controlPanel);

            return content;
        }

        private Border CreatePositionPanel(KukaRobotInstance robot, string title, string color, 
            (string Label, Func<double> Value)[] leftValues, (string Label, Func<double> Value)[] rightValues, bool showDegree = false)
        {
            var panel = new Border
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 26, 26, 26)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15)
            };

            var stack = new StackPanel();
            
            var titleText = new TextBlock 
            { 
                Text = title, 
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 12)
            };
            titleText.Foreground = new SolidColorBrush(ParseColor(color));
            stack.Children.Add(titleText);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            for (int i = 0; i < 3; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });

                // Sol taraf
                var leftLabel = new TextBlock { Text = leftValues[i].Label, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136)), FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetRow(leftLabel, i);
                Grid.SetColumn(leftLabel, 0);
                grid.Children.Add(leftLabel);

                var leftValue = new TextBlock { FontWeight = Microsoft.UI.Text.FontWeights.Bold, FontSize = 14, Foreground = new SolidColorBrush(Microsoft.UI.Colors.White), VerticalAlignment = VerticalAlignment.Center };
                var leftFunc = leftValues[i].Value;
                leftValue.Text = leftFunc().ToString("F2") + (showDegree ? "°" : "");
                robot.PropertyChanged += (s, e) => this.DispatcherQueue.TryEnqueue(() => leftValue.Text = leftFunc().ToString("F2") + (showDegree ? "°" : ""));
                Grid.SetRow(leftValue, i);
                Grid.SetColumn(leftValue, 1);
                grid.Children.Add(leftValue);

                // Sağ taraf
                var rightLabel = new TextBlock { Text = rightValues[i].Label, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136)), FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetRow(rightLabel, i);
                Grid.SetColumn(rightLabel, 2);
                grid.Children.Add(rightLabel);

                var rightValue = new TextBlock { FontWeight = Microsoft.UI.Text.FontWeights.Bold, FontSize = 14, Foreground = new SolidColorBrush(Microsoft.UI.Colors.White), VerticalAlignment = VerticalAlignment.Center };
                var rightFunc = rightValues[i].Value;
                rightValue.Text = rightFunc().ToString("F2") + (showDegree ? "°" : "");
                robot.PropertyChanged += (s, e) => this.DispatcherQueue.TryEnqueue(() => rightValue.Text = rightFunc().ToString("F2") + (showDegree ? "°" : ""));
                Grid.SetRow(rightValue, i);
                Grid.SetColumn(rightValue, 3);
                grid.Children.Add(rightValue);
            }

            stack.Children.Add(grid);
            panel.Child = stack;
            return panel;
        }

        private Border CreateControlPanel(KukaRobotInstance robot)
        {
            var panel = new Border
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 26, 26, 26)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15)
            };

            var stack = new StackPanel { Spacing = 10 };
            
            var titleText = new TextBlock 
            { 
                Text = "⚡ KONTROL", 
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 170, 255)),
                Margin = new Thickness(0, 0, 0, 5)
            };
            stack.Children.Add(titleText);

            var startBtn = new Button { Content = "▶ START", Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 27, 94, 32)), Foreground = new SolidColorBrush(Microsoft.UI.Colors.White), HorizontalAlignment = HorizontalAlignment.Stretch, Height = 32, CornerRadius = new CornerRadius(4) };
            startBtn.Click += (s, e) => { robot.UiDispatcher = this.DispatcherQueue; robot.Start(); };
            stack.Children.Add(startBtn);

            var stopBtn = new Button { Content = "⏹ STOP", Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 183, 28, 28)), Foreground = new SolidColorBrush(Microsoft.UI.Colors.White), HorizontalAlignment = HorizontalAlignment.Stretch, Height = 32, CornerRadius = new CornerRadius(4) };
            stopBtn.Click += (s, e) => robot.Stop();
            stack.Children.Add(stopBtn);

            var removeBtn = new Button { Content = "🗑 Kaldır", Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 55, 71, 79)), Foreground = new SolidColorBrush(Microsoft.UI.Colors.White), HorizontalAlignment = HorizontalAlignment.Stretch, Height = 32, CornerRadius = new CornerRadius(4) };
            removeBtn.Click += async (s, e) =>
            {
                var dialog = new ContentDialog
                {
                    Title = "Robot Kaldır",
                    Content = $"'{robot.Name}' robotunu kaldırmak istediğinize emin misiniz?",
                    PrimaryButtonText = "Kaldır",
                    CloseButtonText = "İptal",
                    XamlRoot = this.XamlRoot,
                    DefaultButton = ContentDialogButton.Close
                };
                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    KukaRobotManager.Instance.RemoveRobot(robot);
                }
            };
            stack.Children.Add(removeBtn);

            panel.Child = stack;
            return panel;
        }

        private Windows.UI.Color ParseColor(string hex)
        {
            hex = hex.Replace("#", "");
            return Windows.UI.Color.FromArgb(255,
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16));
        }

        private void OnLogReceived(string msg)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                string currentText = LogTextBlock.Text ?? "";
                string[] lines = currentText.Split(new[] { '\n' }, StringSplitOptions.None);
                if (lines.Length > MAX_LOG_LINES)
                {
                    LogTextBlock.Text = $"[{DateTime.Now:HH:mm:ss}] {msg}\n" +
                        string.Join("\n", lines.Take(MAX_LOG_LINES - 1).ToArray());
                }
                else
                {
                    LogTextBlock.Text = $"[{DateTime.Now:HH:mm:ss}] {msg}\n" + currentText;
                }
            });
        }

        private void BtnStartAll_Click(object sender, RoutedEventArgs e)
        {
            KukaRobotManager.Instance.StartAll();
        }

        private void BtnStopAll_Click(object sender, RoutedEventArgs e)
        {
            KukaRobotManager.Instance.StopAll();
        }

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
                    var robot = KukaRobotManager.Instance.AddRobot(name, ip, port);
                    robot.OnLog += OnLogReceived;
                }
            }
        }
    }
}
