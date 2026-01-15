using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using Windows.UI;

namespace App4.PAGES
{
    public sealed partial class PLC_Page : Page
    {
        public PLC_Page()
        {
            this.InitializeComponent();
            InitializeIOMapping();
            InitializeSystemMonitor();
            InitializeStatistics();
        }

        /// <summary>
        /// Ýstatistikleri baþlat
        /// </summary>
        private void InitializeStatistics()
        {
            // Örnek veriler - gerçek veriler veritabanýndan çekilebilir
            TotalTestsLabel.Text = "1,247";
            SuccessTestsLabel.Text = "1,186";
            FailedTestsLabel.Text = "61";
            LastTestLabel.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            UptimeLabel.Text = "47.3h";
            AvgDurationLabel.Text = "2.36 sn";
            TodayTestsLabel.Text = "47 Test";
            SystemQualityLabel.Text = "A+";
        }

        /// <summary>
        /// I/O Mapping verilerini dinamik olarak yükle
        /// </summary>
        private void InitializeIOMapping()
        {
            var ioMappings = new List<(string Source, string Channel, string Destination, string Status, string Value)>
            {
                ("PLC", "DI01", "Robot.Input.StartSignal", "Aktif", "HIGH"),
                ("Robot", "DO01", "Camera.Trigger", "Aktif", "PULSE"),
                ("Camera", "AI01", "PLC.Data.PointCloud", "Aktif", "2048 Points"),
                ("PLC", "DI02", "Robot.Input.SafetyStop", "Pasif", "LOW"),
                ("Robot", "DO02", "Camera.Record", "Aktif", "ON"),
                ("Camera", "AI02", "PLC.Data.Coordinates", "Aktif", "X:125mm"),
                ("PLC", "DO03", "Robot.LED.Status", "Aktif", "GREEN"),
                ("Robot", "AI03", "PLC.Position.X", "Aktif", "125.5mm"),
            };

            foreach (var mapping in ioMappings)
            {
                var row = CreateIORow(mapping.Source, mapping.Channel, mapping.Destination, mapping.Status, mapping.Value);
                IOMapContainer.Children.Add(row);
            }
        }

        /// <summary>
        /// I/O Mapping satýrý oluþtur
        /// </summary>
        private Grid CreateIORow(string source, string channel, string destination, string status, string value)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

            grid.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 26, 26, 28));
            grid.Padding = new Thickness(12, 8, 12, 8);
            grid.CornerRadius = new CornerRadius(4);
            grid.Margin = new Thickness(0, 2, 0, 2);
            grid.BorderThickness = new Thickness(0, 0, 0, 1);
            grid.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(100, 0, 164, 239));

            // Kaynak
            var sourceBlock = new TextBlock
            {
                Text = source,
                Foreground = GetSourceColor(source),
                FontSize = 10,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(sourceBlock, 0);
            grid.Children.Add(sourceBlock);

            // Kanal
            var channelBlock = new TextBlock
            {
                Text = channel,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 255, 136)),
                FontSize = 10,
                FontFamily = new FontFamily("Cascadia Mono"),
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            };
            Grid.SetColumn(channelBlock, 1);
            grid.Children.Add(channelBlock);

            // Oklar (Veri Akýþý)
            var arrowBlock = new TextBlock
            {
                Text = "?",
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 200, 255)),
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            };
            Grid.SetColumn(arrowBlock, 2);
            grid.Children.Add(arrowBlock);

            // Hedef
            var destinationBlock = new TextBlock
            {
                Text = destination,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(destinationBlock, 3);
            grid.Children.Add(destinationBlock);

            // Durum Badge
            var statusColor = status == "Aktif" 
                ? Windows.UI.Color.FromArgb(255, 76, 175, 80)
                : Windows.UI.Color.FromArgb(255, 170, 170, 170);

            var statusBlock = new TextBlock
            {
                Text = value,
                Foreground = new SolidColorBrush(statusColor),
                FontSize = 9,
                FontFamily = new FontFamily("Cascadia Mono"),
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Right
            };
            Grid.SetColumn(statusBlock, 4);
            grid.Children.Add(statusBlock);

            return grid;
        }

        /// <summary>
        /// Kaynak rengini belirle (PLC, Robot, Camera)
        /// </summary>
        private SolidColorBrush GetSourceColor(string source)
        {
            return source switch
            {
                "PLC" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 152, 0)), // Orange
                "Robot" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 33, 150, 243)), // Blue
                "Camera" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80)), // Green
                _ => new SolidColorBrush(Microsoft.UI.Colors.White)
            };
        }

        /// <summary>
        /// Sistem monitörünü baþlat
        /// </summary>
        private void InitializeSystemMonitor()
        {
            // Burada timer baþlatýp real-time veri güncellenebilir
            // Þimdilik örnek veriler sabit
        }
    }
}
