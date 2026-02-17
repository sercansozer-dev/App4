using App4.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace App4.Pages
{
    public sealed partial class MultiRobot_Page : Page
    {
        private const int MAX_LOG_LINES = 100;
        private static bool _logHandlerAttached = false;
        private Dictionary<KukaRobotInstance, WebView2> _robotWebViews = new();

        public MultiRobot_Page()
        {
            this.InitializeComponent();

            // Manager'ı initialize et (robotları otomatik başlatır)
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
                _robotWebViews.Clear();
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
            var card = new Border
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 21, 21, 21)),
                CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(2),
                Padding = new Thickness(0),
                Margin = new Thickness(0, 0, 0, 10)
            };

            robot.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(robot.IsConnected))
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
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });  // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });     // Kontrol Paneli
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });     // İçerik

            // Header
            var header = CreateRobotHeader(robot);
            Grid.SetRow(header, 0);
            mainGrid.Children.Add(header);

            // Kontrol Paneli (YENİ)
            var controlPanel = CreateRobotControlPanel(robot);
            Grid.SetRow(controlPanel, 1);
            mainGrid.Children.Add(controlPanel);

            // İçerik (Pozisyon + Eksen + Robot Kolu Çizimi)
            var content = CreateRobotContent(robot);
            Grid.SetRow(content, 2);
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
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Sol - Durum LED ve İsim
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

            leftPanel.Children.Add(new TextBlock { Text = robot.Name, FontSize = 18, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Microsoft.UI.Colors.White) });
            Grid.SetColumn(leftPanel, 0);
            headerGrid.Children.Add(leftPanel);

            // IP düzenleme alanı
            var ipPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(15, 0, 0, 0) };

            var ipBox = new TextBox { Text = robot.IpAddress, Width = 130, Height = 28, FontSize = 11, Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 45, 45, 48)), Foreground = new SolidColorBrush(Microsoft.UI.Colors.White), BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 164, 239)) };
            var portBox = new TextBox { Text = robot.Port.ToString(), Width = 60, Height = 28, FontSize = 11, Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 45, 45, 48)), Foreground = new SolidColorBrush(Microsoft.UI.Colors.White), BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 164, 239)) };

            ipBox.LostFocus += (s, e) =>
            {
                if (ipBox.Text != robot.IpAddress)
                {
                    int.TryParse(portBox.Text, out int p);
                    if (p == 0) p = 7000;
                    KukaRobotManager.Instance.UpdateRobotIp(robot, ipBox.Text, p);
                }
            };
            portBox.LostFocus += (s, e) =>
            {
                int.TryParse(portBox.Text, out int p);
                if (p > 0 && p != robot.Port)
                {
                    KukaRobotManager.Instance.UpdateRobotIp(robot, ipBox.Text, p);
                }
            };

            ipPanel.Children.Add(new TextBlock { Text = "IP:", Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136)), VerticalAlignment = VerticalAlignment.Center });
            ipPanel.Children.Add(ipBox);
            ipPanel.Children.Add(new TextBlock { Text = "Port:", Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136)), VerticalAlignment = VerticalAlignment.Center });
            ipPanel.Children.Add(portBox);

            var statusBadge = new Border { CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 2, 8, 2), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) };
            var statusBadgeText = new TextBlock { FontSize = 10, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 10, 10, 10)) };
            robot.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(robot.IsConnected))
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
            ipPanel.Children.Add(statusBadge);

            Grid.SetColumn(ipPanel, 1);
            headerGrid.Children.Add(ipPanel);

            // Sağ - Override değerleri
            var rightPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 20, VerticalAlignment = VerticalAlignment.Center };

            var proStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
            proStack.Children.Add(new TextBlock { Text = "PRO:", FontSize = 11, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136)) });
            var proValue = new TextBlock { FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 170, 255)) };
            robot.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(robot.OverridePro)) this.DispatcherQueue.TryEnqueue(() => proValue.Text = robot.OverridePro.ToString()); };
            proValue.Text = robot.OverridePro.ToString();
            proStack.Children.Add(proValue);
            proStack.Children.Add(new TextBlock { Text = "%", FontSize = 11, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136)) });
            rightPanel.Children.Add(proStack);

            var jogStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
            jogStack.Children.Add(new TextBlock { Text = "JOG:", FontSize = 11, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136)) });
            var jogValue = new TextBlock { FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 170, 0)) };
            robot.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(robot.OverrideJog)) this.DispatcherQueue.TryEnqueue(() => jogValue.Text = robot.OverrideJog.ToString()); };
            jogValue.Text = robot.OverrideJog.ToString();
            jogStack.Children.Add(jogValue);
            jogStack.Children.Add(new TextBlock { Text = "%", FontSize = 11, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136)) });
            rightPanel.Children.Add(jogStack);

            Grid.SetColumn(rightPanel, 3);
            headerGrid.Children.Add(rightPanel);

            header.Child = headerGrid;
            return header;
        }

        #region Robot Kontrol Paneli

        private Border CreateRobotControlPanel(KukaRobotInstance robot)
        {
            var controlBorder = new Border
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 18, 18, 20)),
                Padding = new Thickness(15, 12, 15, 12),
                BorderThickness = new Thickness(0, 1, 0, 1),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 35, 35, 38))
            };

            var mainStack = new StackPanel { Spacing = 12 };

            // ========== ROW 1: Ana Kontroller + Servo + Reçete ==========
            var row1 = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 20 };

            // Kontrol Butonları
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            btnPanel.Children.Add(CreateControlButton("▶", "Programı Başlat (START)", "#4CAF50", 42, async () => await robot.StartProgramAsync()));
            btnPanel.Children.Add(CreateControlButton("⏹", "Programı Durdur (STOP)", "#F44336", 42, async () => await robot.StopProgramAsync()));
            btnPanel.Children.Add(CreateControlButton("⟳", "Hata Resetle (RESET)", "#FF9800", 42, async () => await robot.ResetErrorAsync()));
            btnPanel.Children.Add(CreateControlButton("⌂", "HOME Pozisyonuna Git", "#2196F3", 42, async () => await robot.GoHomeAsync()));
            row1.Children.Add(btnPanel);

            // Ayırıcı
            row1.Children.Add(new Border { Width = 1, Height = 28, Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 50, 50, 55)), Margin = new Thickness(5, 0, 5, 0) });

            // Servo Kontrolü
            var servoPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
            servoPanel.Children.Add(new TextBlock { Text = "⚡SERVO:", FontSize = 10, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 188, 212)), VerticalAlignment = VerticalAlignment.Center });
            servoPanel.Children.Add(CreateControlButton("ON", "Servo Aç", "#00BCD4", 38, async () => await robot.ServoOnAsync()));
            servoPanel.Children.Add(CreateControlButton("OFF", "Servo Kapat", "#607D8B", 38, async () => await robot.ServoOffAsync()));
            row1.Children.Add(servoPanel);

            // Ayırıcı
            row1.Children.Add(new Border { Width = 1, Height = 28, Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 50, 50, 55)), Margin = new Thickness(5, 0, 5, 0) });

            // Reçete Seçimi
            var recipePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
            recipePanel.Children.Add(new TextBlock { Text = "📋 Reçete:", FontSize = 11, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136)), VerticalAlignment = VerticalAlignment.Center });
            var recipeCombo = new ComboBox { Width = 70, Height = 32, FontSize = 12, Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 45, 45, 48)), Foreground = new SolidColorBrush(Microsoft.UI.Colors.White) };
            for (int i = 1; i <= 20; i++) recipeCombo.Items.Add(i);
            recipeCombo.SelectedIndex = 0;
            recipeCombo.SelectionChanged += async (s, e) => { if (recipeCombo.SelectedItem is int no) await robot.SelectRecipeAsync(no); };
            recipePanel.Children.Add(recipeCombo);
            row1.Children.Add(recipePanel);

            mainStack.Children.Add(row1);

            // ========== ROW 2: Override Sliders ==========
            var row2 = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 30 };

            // PRO Override
            var proPanel = CreateOverrideControl("⚡ PRO Override", "#00AAFF", robot.OverridePro, 
                async (val) => await robot.SetOverrideProAsync(val),
                (tb) => robot.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(robot.OverridePro)) this.DispatcherQueue.TryEnqueue(() => tb.Text = $"{robot.OverridePro}%"); });
            row2.Children.Add(proPanel);

            // JOG Override
            var jogPanel = CreateOverrideControl("🎮 JOG Override", "#FFAA00", robot.OverrideJog,
                async (val) => await robot.SetOverrideJogAsync(val),
                (tb) => robot.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(robot.OverrideJog)) this.DispatcherQueue.TryEnqueue(() => tb.Text = $"{robot.OverrideJog}%"); });
            row2.Children.Add(jogPanel);

            mainStack.Children.Add(row2);

            // ========== ROW 3: JOG Kontrolleri ==========
            var row3 = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 15 };
            row3.Children.Add(new TextBlock { Text = "🎮 JOG:", FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 170, 0)), VerticalAlignment = VerticalAlignment.Center });

            // Eksen JOG butonları
            for (int axis = 1; axis <= 6; axis++)
            {
                int ax = axis;
                var axisPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
                axisPanel.Children.Add(CreateJogButton("-", $"A{axis} Negatif Yönde Hareket", async () => await robot.JogAxisAsync(ax, -1)));
                axisPanel.Children.Add(new TextBlock { Text = $"A{axis}", FontSize = 10, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 200, 200, 200)), VerticalAlignment = VerticalAlignment.Center, Width = 24, TextAlignment = TextAlignment.Center });
                axisPanel.Children.Add(CreateJogButton("+", $"A{axis} Pozitif Yönde Hareket", async () => await robot.JogAxisAsync(ax, 1)));
                row3.Children.Add(axisPanel);
            }

            // JOG STOP
            var jogStopBtn = new Button
            {
                Content = "⏹ STOP",
                Height = 28,
                Padding = new Thickness(12, 0, 12, 0),
                FontSize = 10,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 244, 67, 54)),
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(10, 0, 0, 0)
            };
            jogStopBtn.Click += async (s, e) => await robot.StopJogAsync();
            ToolTipService.SetToolTip(jogStopBtn, "Tüm JOG Hareketlerini Durdur");
            row3.Children.Add(jogStopBtn);

            mainStack.Children.Add(row3);

            // ========== ROW 4: Durum Göstergeleri ==========
            var row4 = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
            row4.Children.Add(new TextBlock { Text = "📊 DURUM:", FontSize = 10, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136)), VerticalAlignment = VerticalAlignment.Center });

            // READY
            var readyIndicator = CreateStatusIndicator("READY", "#4CAF50", robot.RobotReady);
            robot.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(robot.RobotReady)) this.DispatcherQueue.TryEnqueue(() => UpdateStatusIndicator(readyIndicator, robot.RobotReady, "#4CAF50")); };
            row4.Children.Add(readyIndicator);

            // RUNNING
            var runningIndicator = CreateStatusIndicator("RUNNING", "#2196F3", robot.RobotRunning);
            robot.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(robot.RobotRunning)) this.DispatcherQueue.TryEnqueue(() => UpdateStatusIndicator(runningIndicator, robot.RobotRunning, "#2196F3")); };
            row4.Children.Add(runningIndicator);

            // ERROR
            var errorIndicator = CreateStatusIndicator("ERROR", "#F44336", robot.RobotError);
            robot.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(robot.RobotError)) this.DispatcherQueue.TryEnqueue(() => UpdateStatusIndicator(errorIndicator, robot.RobotError, "#F44336")); };
            row4.Children.Add(errorIndicator);

            mainStack.Children.Add(row4);

            controlBorder.Child = mainStack;
            return controlBorder;
        }

        private Button CreateControlButton(string icon, string tooltip, string color, int width, Func<System.Threading.Tasks.Task> action)
        {
            var c = ParseColor(color);
            var btn = new Button
            {
                Content = icon,
                Width = width,
                Height = 32,
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Background = new SolidColorBrush(c),
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(0)
            };
            ToolTipService.SetToolTip(btn, tooltip);
            btn.Click += async (s, e) => await action();
            return btn;
        }

        private Button CreateJogButton(string text, string tooltip, Func<System.Threading.Tasks.Task> action)
        {
            var btn = new Button
            {
                Content = text,
                Width = 26,
                Height = 26,
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 55, 55, 60)),
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(0)
            };
            ToolTipService.SetToolTip(btn, tooltip);
            btn.Click += async (s, e) => await action();
            return btn;
        }

        private StackPanel CreateOverrideControl(string label, string color, int initialValue, Func<int, System.Threading.Tasks.Task> onChange, Action<TextBlock> bindValue)
        {
            var c = ParseColor(color);
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };

            panel.Children.Add(new TextBlock { Text = label + ":", FontSize = 10, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = new SolidColorBrush(c), VerticalAlignment = VerticalAlignment.Center, Width = 100 });

            var slider = new Slider { Width = 120, Minimum = 1, Maximum = 100, Value = initialValue, Height = 28 };
            var valueText = new TextBlock { Text = $"{initialValue}%", Width = 40, FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(c), VerticalAlignment = VerticalAlignment.Center };

            slider.ValueChanged += async (s, e) =>
            {
                int val = (int)slider.Value;
                valueText.Text = $"{val}%";
                await onChange(val);
            };

            bindValue(valueText);

            panel.Children.Add(slider);
            panel.Children.Add(valueText);
            return panel;
        }

        private Border CreateStatusIndicator(string label, string color, bool isActive)
        {
            var c = ParseColor(color);
            var border = new Border
            {
                Background = new SolidColorBrush(isActive ? c : Windows.UI.Color.FromArgb(255, 45, 45, 48)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 4, 10, 4)
            };
            border.Child = new TextBlock
            {
                Text = label,
                FontSize = 10,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new SolidColorBrush(isActive ? Microsoft.UI.Colors.White : Windows.UI.Color.FromArgb(255, 80, 80, 80))
            };
            return border;
        }

        private void UpdateStatusIndicator(Border indicator, bool isActive, string color)
        {
            var c = ParseColor(color);
            indicator.Background = new SolidColorBrush(isActive ? c : Windows.UI.Color.FromArgb(255, 45, 45, 48));
            if (indicator.Child is TextBlock tb)
            {
                tb.Foreground = new SolidColorBrush(isActive ? Microsoft.UI.Colors.White : Windows.UI.Color.FromArgb(255, 80, 80, 80));
            }
        }

        private Windows.UI.Color ParseColor(string hex)
        {
            hex = hex.Replace("#", "");
            return Windows.UI.Color.FromArgb(255,
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16));
        }

        #endregion

        private Grid CreateRobotContent(KukaRobotInstance robot)
        {
            var content = new Grid { Padding = new Thickness(15, 10, 15, 15) };
            content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Ana içerik
            content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Gelişmiş veriler

            // ═══ ROW 0: Ana İçerik ═══
            var row0 = new Grid();
            row0.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row0.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row0.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) });
            Grid.SetRow(row0, 0);
            content.Children.Add(row0);

            // TCP Pozisyonu
            var tcpPanel = CreatePositionPanel(robot, "📍 TCP POZİSYONU", "#00FF88",
                new (string Label, Func<double> Value)[] { ("X:", () => robot.PosX), ("Y:", () => robot.PosY), ("Z:", () => robot.PosZ) },
                new (string Label, Func<double> Value)[] { ("A:", () => robot.PosA), ("B:", () => robot.PosB), ("C:", () => robot.PosC) });
            Grid.SetColumn(tcpPanel, 0);
            tcpPanel.Margin = new Thickness(0, 0, 8, 0);
            row0.Children.Add(tcpPanel);

            // Eksen Açıları
            var axisPanel = CreatePositionPanel(robot, "🔧 EKSEN AÇILARI", "#FFAA00",
                new (string Label, Func<double> Value)[] { ("A1:", () => robot.A1), ("A2:", () => robot.A2), ("A3:", () => robot.A3) },
                new (string Label, Func<double> Value)[] { ("A4:", () => robot.A4), ("A5:", () => robot.A5), ("A6:", () => robot.A6) }, true);
            Grid.SetColumn(axisPanel, 1);
            axisPanel.Margin = new Thickness(8, 0, 8, 0);
            row0.Children.Add(axisPanel);

            // Robot Kolu Çizimi
            var robotArmPanel = CreateRobotArmVisualization(robot);
            Grid.SetColumn(robotArmPanel, 2);
            robotArmPanel.Margin = new Thickness(8, 0, 0, 0);
            row0.Children.Add(robotArmPanel);

            // ═══ ROW 1: Gelişmiş Veriler ═══
            var row1 = CreateAdvancedDataPanel(robot);
            Grid.SetRow(row1, 1);
            row1.Margin = new Thickness(0, 10, 0, 0);
            content.Children.Add(row1);

            return content;
        }

        private Grid CreateAdvancedDataPanel(KukaRobotInstance robot)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Safety
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Tork
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Program
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Hız

            // Safety Panel
            var safetyPanel = CreateInfoPanel(robot, "🛡️ SAFETY", "#E91E63", new (string Label, Func<object> Value, string Color)[]
            {
                ("Sürücüler:", () => robot.DrivesOn ? "✅ ON" : "❌ OFF", robot.DrivesOn ? "#4CAF50" : "#F44336"),
                ("E-STOP:", () => robot.EmergencyStop ? "🔴 AKTIF" : "🟢 OK", robot.EmergencyStop ? "#F44336" : "#4CAF50"),
                ("Peripheral:", () => robot.PeripheralReady ? "✅ Hazır" : "⏳ Bekle", robot.PeripheralReady ? "#4CAF50" : "#FF9800"),
                ("User Safety:", () => robot.UserSafety ? "✅ OK" : "⚠️ Uyarı", robot.UserSafety ? "#4CAF50" : "#FF9800"),
                ("Mod:", () => robot.OperationModeText, "#00BCD4"),
            });
            Grid.SetColumn(safetyPanel, 0);
            safetyPanel.Margin = new Thickness(0, 0, 4, 0);
            grid.Children.Add(safetyPanel);

            // Tork Panel
            var torquePanel = CreateInfoPanel(robot, "💪 TORK (%)", "#9C27B0", new (string Label, Func<object> Value, string Color)[]
            {
                ("A1:", () => $"{robot.Torque1:F1}%", GetTorqueColor(robot.Torque1)),
                ("A2:", () => $"{robot.Torque2:F1}%", GetTorqueColor(robot.Torque2)),
                ("A3:", () => $"{robot.Torque3:F1}%", GetTorqueColor(robot.Torque3)),
                ("A4:", () => $"{robot.Torque4:F1}%", GetTorqueColor(robot.Torque4)),
                ("A5:", () => $"{robot.Torque5:F1}%", GetTorqueColor(robot.Torque5)),
                ("A6:", () => $"{robot.Torque6:F1}%", GetTorqueColor(robot.Torque6)),
            });
            Grid.SetColumn(torquePanel, 1);
            torquePanel.Margin = new Thickness(4, 0, 4, 0);
            grid.Children.Add(torquePanel);

            // Program Panel
            var programPanel = CreateInfoPanel(robot, "📋 PROGRAM", "#3F51B5", new (string Label, Func<object> Value, string Color)[]
            {
                ("Durum:", () => robot.ProgramStateText, robot.ProgramState == 1 ? "#4CAF50" : "#FF9800"),
                ("Program:", () => string.IsNullOrEmpty(robot.ProgramName) ? "-" : robot.ProgramName, "#FFFFFF"),
                ("Adım:", () => string.IsNullOrEmpty(robot.CurrentStep) ? "-" : robot.CurrentStep, "#FFFFFF"),
                ("Tool:", () => $"T{robot.ToolNo}", "#00BCD4"),
                ("Base:", () => $"B{robot.BaseNo}", "#00BCD4"),
            });
            Grid.SetColumn(programPanel, 2);
            programPanel.Margin = new Thickness(4, 0, 4, 0);
            grid.Children.Add(programPanel);

            // Hız Panel
            var speedPanel = CreateInfoPanel(robot, "⚡ HIZ (°/s)", "#FF5722", new (string Label, Func<object> Value, string Color)[]
            {
                ("A1:", () => $"{robot.Vel1:F1}", "#FFFFFF"),
                ("A2:", () => $"{robot.Vel2:F1}", "#FFFFFF"),
                ("A3:", () => $"{robot.Vel3:F1}", "#FFFFFF"),
                ("TCP:", () => $"{robot.TcpSpeed:F1} mm/s", "#00FF88"),
                ("Çevrim:", () => $"{robot.CycleTime:F2}s", "#FFAA00"),
            });
            Grid.SetColumn(speedPanel, 3);
            speedPanel.Margin = new Thickness(4, 0, 0, 0);
            grid.Children.Add(speedPanel);

            return grid;
        }

        private Border CreateInfoPanel(KukaRobotInstance robot, string title, string titleColor, (string Label, Func<object> Value, string Color)[] items)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 26, 26, 26)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8, 10, 8)
            };

            var stack = new StackPanel { Spacing = 4 };

            // Başlık
            var titleText = new TextBlock
            {
                Text = title,
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new SolidColorBrush(ParseColor(titleColor)),
                Margin = new Thickness(0, 0, 0, 5)
            };
            stack.Children.Add(titleText);

            // Değerler
            foreach (var item in items)
            {
                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var label = new TextBlock
                {
                    Text = item.Label,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136))
                };
                Grid.SetColumn(label, 0);
                row.Children.Add(label);

                var valueText = new TextBlock
                {
                    FontSize = 10,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Text = item.Value()?.ToString() ?? "-"
                };

                // Dinamik güncelleme
                robot.PropertyChanged += (s, e) =>
                {
                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        try
                        {
                            valueText.Text = item.Value()?.ToString() ?? "-";
                            // Renk güncelleme (tork gibi dinamik renkler için)
                            if (item.Color.StartsWith("#"))
                                valueText.Foreground = new SolidColorBrush(ParseColor(item.Color));
                        }
                        catch { }
                    });
                };

                valueText.Foreground = new SolidColorBrush(ParseColor(item.Color));
                Grid.SetColumn(valueText, 1);
                row.Children.Add(valueText);

                stack.Children.Add(row);
            }

            border.Child = stack;
            return border;
        }

        private string GetTorqueColor(double torque)
        {
            if (torque > 80) return "#F44336"; // Kırmızı - Tehlikeli
            if (torque > 60) return "#FF9800"; // Turuncu - Uyarı
            if (torque > 40) return "#FFEB3B"; // Sarı - Dikkat
            return "#4CAF50"; // Yeşil - Normal
        }

        private Border CreateRobotArmVisualization(KukaRobotInstance robot)
        {
            var panel = new Border
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 26, 26, 26)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(0)
            };

            var stack = new StackPanel();

            var titleText = new TextBlock
            {
                Text = "🤖 3D ROBOT KOLU",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 170, 255)),
                Margin = new Thickness(10, 8, 10, 5)
            };
            stack.Children.Add(titleText);

            // WebView2 for 3D robot arm visualization
            var webView = new WebView2
            {
                Width = 230,
                Height = 180
            };

            _robotWebViews[robot] = webView;

            webView.Loaded += async (s, e) =>
            {
                try
                {
                    await webView.EnsureCoreWebView2Async();

                    // HTML dosyasını yükle
                    string htmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "RobotArmViewer.html");
                    if (System.IO.File.Exists(htmlPath))
                    {
                        webView.Source = new Uri(htmlPath);
                    }
                    else
                    {
                        // Fallback: inline HTML
                        webView.NavigateToString(GetInlineRobotArmHtml());
                    }

                    // İlk açıları ayarla
                    webView.NavigationCompleted += async (sender, args) =>
                    {
                        await UpdateWebView3DAngles(webView, robot);
                    };
                }
                catch { }
            };

            // Robot açıları değiştiğinde güncelle
            robot.PropertyChanged += async (s, e) =>
            {
                if (e.PropertyName?.StartsWith("A") == true && e.PropertyName.Length == 2 && char.IsDigit(e.PropertyName[1]))
                {
                    if (_robotWebViews.TryGetValue(robot, out var wv) && wv.CoreWebView2 != null)
                    {
                        await UpdateWebView3DAngles(wv, robot);
                    }
                }
            };

            stack.Children.Add(webView);

            // Açı göstergesi
            var angleIndicators = new Grid { Margin = new Thickness(5, 5, 5, 8) };
            angleIndicators.ColumnDefinitions.Add(new ColumnDefinition());
            angleIndicators.ColumnDefinitions.Add(new ColumnDefinition());
            angleIndicators.ColumnDefinitions.Add(new ColumnDefinition());
            angleIndicators.ColumnDefinitions.Add(new ColumnDefinition());
            angleIndicators.ColumnDefinitions.Add(new ColumnDefinition());
            angleIndicators.ColumnDefinitions.Add(new ColumnDefinition());

            var segmentColors = new[] { "#FF5722", "#FF9800", "#FFEB3B", "#4CAF50", "#2196F3", "#9C27B0" };
            for (int i = 0; i < 6; i++)
            {
                var axisIndex = i;
                var indicator = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                indicator.Children.Add(new TextBlock { Text = $"A{i + 1}", FontSize = 8, Foreground = new SolidColorBrush(ParseColor(segmentColors[i])), HorizontalAlignment = HorizontalAlignment.Center });

                var valueText = new TextBlock { FontSize = 8, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Microsoft.UI.Colors.White), HorizontalAlignment = HorizontalAlignment.Center };
                valueText.Text = GetAxisValue(robot, axisIndex).ToString("F0") + "°";
                robot.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == $"A{axisIndex + 1}")
                    {
                        this.DispatcherQueue.TryEnqueue(() => valueText.Text = GetAxisValue(robot, axisIndex).ToString("F0") + "°");
                    }
                };
                indicator.Children.Add(valueText);

                Grid.SetColumn(indicator, i);
                angleIndicators.Children.Add(indicator);
            }
            stack.Children.Add(angleIndicators);

            panel.Child = stack;
            return panel;
        }

        private async System.Threading.Tasks.Task UpdateWebView3DAngles(WebView2 webView, KukaRobotInstance robot)
        {
            try
            {
                if (webView?.CoreWebView2 == null) return;
                string script = $"window.setAngles({robot.A1:F2}, {robot.A2:F2}, {robot.A3:F2}, {robot.A4:F2}, {robot.A5:F2}, {robot.A6:F2});";
                await webView.ExecuteScriptAsync(script);
            }
            catch { }
        }

        private string GetInlineRobotArmHtml()
        {
            return @"<!DOCTYPE html>
<html><head><meta charset='UTF-8'><style>
*{margin:0;padding:0}body{background:#0D0D0D;overflow:hidden}
#c{width:100%;height:100%}
</style></head><body>
<canvas id='c'></canvas>
<script src='https://cdnjs.cloudflare.com/ajax/libs/three.js/r128/three.min.js'></script>
<script>
const c=document.getElementById('c'),W=c.width=230,H=c.height=180;
const scene=new THREE.Scene();scene.background=new THREE.Color(0x0D0D0D);
const camera=new THREE.PerspectiveCamera(45,W/H,0.1,1000);camera.position.set(200,150,200);camera.lookAt(0,80,0);
const renderer=new THREE.WebGLRenderer({canvas:c,antialias:true});renderer.setSize(W,H);
scene.add(new THREE.AmbientLight(0xffffff,0.5));
const dl=new THREE.DirectionalLight(0xffffff,0.8);dl.position.set(100,150,100);scene.add(dl);
scene.add(new THREE.GridHelper(200,10,0x333,0x222));
const colors=[0xFF5722,0xFF9800,0xFFEB3B,0x4CAF50,0x2196F3,0x9C27B0];
const lens=[40,60,50,35,25,20];
const arm=new THREE.Group();scene.add(arm);
const base=new THREE.Mesh(new THREE.CylinderGeometry(20,25,15,16),new THREE.MeshPhongMaterial({color:0x333}));
base.position.y=7.5;arm.add(base);
const joints=[],segs=[];let cur=arm;
for(let i=0;i<6;i++){
const jg=new THREE.Group();jg.position.y=i===0?15:lens[i-1];cur.add(jg);
const jm=new THREE.Mesh(new THREE.SphereGeometry(8,12,12),new THREE.MeshPhongMaterial({color:0x00FF88}));jg.add(jm);
const sg=new THREE.CylinderGeometry(5,5,lens[i],12);sg.translate(0,lens[i]/2,0);
const sm=new THREE.Mesh(sg,new THREE.MeshPhongMaterial({color:colors[i]}));jg.add(sm);
joints.push(jg);segs.push(sm);cur=jg;
}
const tcp=new THREE.Mesh(new THREE.ConeGeometry(7,18,6),new THREE.MeshPhongMaterial({color:0xFF0000}));
tcp.position.y=lens[5]+9;joints[5].add(tcp);
let angles=[0,0,0,0,0,0],drag=false,px=0,py=0,theta=Math.PI/4,phi=Math.PI/4,dist=300;
function update(){
joints[0].rotation.y=angles[0]*Math.PI/180;
joints[1].rotation.z=-angles[1]*Math.PI/180;
joints[2].rotation.z=-angles[2]*Math.PI/180;
joints[3].rotation.y=angles[3]*Math.PI/180;
joints[4].rotation.z=-angles[4]*Math.PI/180;
joints[5].rotation.y=angles[5]*Math.PI/180;
}
function camPos(){
camera.position.x=dist*Math.sin(phi)*Math.cos(theta);
camera.position.y=dist*Math.cos(phi);
camera.position.z=dist*Math.sin(phi)*Math.sin(theta);
camera.lookAt(0,80,0);
}
c.onmousedown=e=>{drag=true;px=e.clientX;py=e.clientY};
c.onmouseup=c.onmouseleave=()=>drag=false;
c.onmousemove=e=>{if(!drag)return;theta-=(e.clientX-px)*0.01;phi=Math.max(0.1,Math.min(Math.PI-0.1,phi-(e.clientY-py)*0.01));camPos();px=e.clientX;py=e.clientY};
c.onwheel=e=>{dist=Math.max(100,Math.min(500,dist+e.deltaY*0.3));camPos()};
window.setAngles=(a1,a2,a3,a4,a5,a6)=>{angles=[a1,a2,a3,a4,a5,a6];update()};
function anim(){requestAnimationFrame(anim);renderer.render(scene,camera)}
update();camPos();anim();
</script></body></html>";
        }

        private double GetAxisValue(KukaRobotInstance robot, int index)
        {
            return index switch
            {
                0 => robot.A1,
                1 => robot.A2,
                2 => robot.A3,
                3 => robot.A4,
                4 => robot.A5,
                5 => robot.A6,
                _ => 0
            };
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
