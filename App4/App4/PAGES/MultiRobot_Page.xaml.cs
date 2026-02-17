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
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });     // İçerik

            // Header
            var header = CreateRobotHeader(robot);
            Grid.SetRow(header, 0);
            mainGrid.Children.Add(header);

            // İçerik (Pozisyon + Eksen + Robot Kolu Çizimi)
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

        private Grid CreateRobotContent(KukaRobotInstance robot)
        {
            var content = new Grid { Padding = new Thickness(15, 10, 15, 15) };
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) }); // Robot kolu çizimi için

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

            // Robot Kolu Çizimi
            var robotArmPanel = CreateRobotArmVisualization(robot);
            Grid.SetColumn(robotArmPanel, 2);
            robotArmPanel.Margin = new Thickness(8, 0, 0, 0);
            content.Children.Add(robotArmPanel);

            return content;
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
