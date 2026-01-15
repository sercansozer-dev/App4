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
            InitializeTags();
        }

        private void InitializeTags()
        {
            var tags = new List<(string Name, string Type, string Value, string Status)>
            {
                ("Temperature_Sensor_01", "Float", "45.8 °C", "Aktif"),
                ("Pressure_Gauge_01", "Float", "2.5 Bar", "Aktif"),
                ("Motor_Speed", "Int32", "1500 RPM", "Aktif"),
                ("System_Alarm", "Bool", "False", "Deaktif"),
                ("Flow_Rate", "Float", "125.3 L/min", "Aktif"),
                ("Valve_Position", "Int32", "75%", "Aktif"),
            };

            foreach (var tag in tags)
            {
                var tagRow = CreateTagRow(tag.Name, tag.Type, tag.Value, tag.Status);
                TagsContainer.Children.Add(tagRow);
            }
        }

        private Grid CreateTagRow(string tagName, string dataType, string value, string status)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 26, 26, 28));
            grid.Padding = new Thickness(15, 12, 15, 12);
            grid.CornerRadius = new CornerRadius(4);
            grid.Margin = new Thickness(0, 4, 0, 4);

            // Tag Adý
            var nameBlock = new TextBlock
            {
                Text = tagName,
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(nameBlock, 0);
            grid.Children.Add(nameBlock);

            // Veri Tipi
            var typeBlock = new TextBlock
            {
                Text = dataType,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 170, 170, 170)),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(typeBlock, 1);
            grid.Children.Add(typeBlock);

            // Deðer
            var valueBlock = new TextBlock
            {
                Text = value,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 255, 136)),
                FontSize = 12,
                FontFamily = new FontFamily("Cascadia Mono"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(valueBlock, 2);
            grid.Children.Add(valueBlock);

            // Durum
            var statusColor = status == "Aktif" 
                ? Windows.UI.Color.FromArgb(255, 76, 175, 80)  // Yeþil
                : Windows.UI.Color.FromArgb(255, 255, 107, 107); // Kýrmýzý

            var statusBlock = new TextBlock
            {
                Text = status,
                Foreground = new SolidColorBrush(statusColor),
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };
            Grid.SetColumn(statusBlock, 3);
            grid.Children.Add(statusBlock);

            return grid;
        }
    }
}
