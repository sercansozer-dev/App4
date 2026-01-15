using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI;

namespace App4.PAGES
{
    public sealed partial class PLC_Page : Page
    {
        // PLC Deðiþkenleri Koleksiyonu
        private ObservableCollection<PLCVariable> PLCVariables { get; set; }

        public PLC_Page()
        {
            this.InitializeComponent();
            PLCVariables = new ObservableCollection<PLCVariable>();
            InitializeIOMapping();
            InitializeSystemMonitor();
            InitializeStatistics();
            InitializePLCVariables();
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

        /// <summary>
        /// Yeni Deðiþken Ekle butonu týklandý
        /// </summary>
        private void AddVariableBtn_Click(object sender, RoutedEventArgs e)
        {
            AddNewPLCVariable();
        }

        /// <summary>
        /// PLC Deðiþkenlerini baþlat ve kontrol tablosunu oluþtur
        /// </summary>
        private void InitializePLCVariables()
        {
            var variables = new List<(string Name, string Type, object CurrentValue, object MinValue, object MaxValue)>
            {
                ("D0 - Baþlat Sinyali", "BOOL", false, false, true),
                ("D1 - Ýþletim Modu", "DWORD", 0, 0, 3),
                ("D2 - Robot Hýzý", "INT", 75, 0, 100),
                ("D3 - Sýcaklýk Setpoint", "REAL", 42.5, 20.0, 80.0),
                ("M0 - Acil Durdur", "BOOL", false, false, true),
                ("M1 - Sistem Ready", "BOOL", true, false, true),
                ("Y0 - Çýkýþ 1", "BOOL", false, false, true),
                ("Y1 - Çýkýþ 2", "BOOL", true, false, true),
            };

            foreach (var variable in variables)
            {
                PLCVariables.Add(new PLCVariable
                {
                    Name = variable.Name,
                    Type = variable.Type,
                    CurrentValue = variable.CurrentValue,
                    MinValue = variable.MinValue,
                    MaxValue = variable.MaxValue
                });
            }

            RefreshPLCVariablesUI();
        }

        /// <summary>
        /// PLC Deðiþkenleri UI'ý yenile
        /// </summary>
        private void RefreshPLCVariablesUI()
        {
            PLCVariablesContainer.Children.Clear();

            for (int i = 0; i < PLCVariables.Count; i++)
            {
                var variable = PLCVariables[i];
                var row = CreatePLCVariableRow(i, variable);
                PLCVariablesContainer.Children.Add(row);
            }
        }

        /// <summary>
        /// PLC Deðiþkeni kontrol satýrý oluþtur
        /// </summary>
        private Border CreatePLCVariableRow(int index, PLCVariable variable)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 26, 26, 28)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(100, 0, 164, 239))
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            grid.ColumnSpacing = 8;

            // Index Numarasý
            var indexBlock = new TextBlock
            {
                Text = (index + 1).ToString(),
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 184, 28)),
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                FontFamily = new FontFamily("Cascadia Mono")
            };
            Grid.SetColumn(indexBlock, 0);
            grid.Children.Add(indexBlock);

            // Deðiþken Adý (Editlenebilir)
            var nameBox = new TextBox
            {
                Text = variable.Name,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 164, 239)),
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 45, 45, 50)),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(100, 0, 164, 239)),
                Padding = new Thickness(6),
                Height = 32,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 10,
                FontFamily = new FontFamily("Cascadia Mono")
            };
            nameBox.LostFocus += (s, e) => { variable.Name = nameBox.Text; };
            Grid.SetColumn(nameBox, 1);
            grid.Children.Add(nameBox);

            // Tip
            var typeBlock = new TextBlock
            {
                Text = variable.Type,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 184, 28)),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(typeBlock, 2);
            grid.Children.Add(typeBlock);

            // Þu Anki Deðer (Editlenebilir)
            var valueBox = new TextBox
            {
                Text = variable.CurrentValue.ToString(),
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80)),
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 45, 45, 50)),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(100, 76, 175, 80)),
                Padding = new Thickness(6),
                Height = 32,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 10,
                FontFamily = new FontFamily("Cascadia Mono")
            };
            valueBox.LostFocus += (s, e) =>
            {
                try
                {
                    variable.CurrentValue = Convert.ChangeType(valueBox.Text, GetTypeFromString(variable.Type));
                }
                catch { }
            };
            Grid.SetColumn(valueBox, 3);
            grid.Children.Add(valueBox);

            // Min/Max
            var rangeBlock = new TextBlock
            {
                Text = $"{variable.MinValue}..{variable.MaxValue}",
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(200, 136, 136, 136)),
                FontSize = 9,
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = new FontFamily("Cascadia Mono")
            };
            Grid.SetColumn(rangeBlock, 4);
            grid.Children.Add(rangeBlock);

            // Yeni Deðer Input
            var inputBox = new TextBox
            {
                PlaceholderText = "Yeni deðer",
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 45, 45, 50)),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(100, 255, 255, 255)),
                Padding = new Thickness(6),
                Height = 32,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 9
            };
            Grid.SetColumn(inputBox, 5);
            grid.Children.Add(inputBox);

            // Force Et Butonu
            var forceButton = new Button
            {
                Content = "Force",
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80)),
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                CornerRadius = new CornerRadius(4),
                Height = 32,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                FontSize = 9,
                Padding = new Thickness(4)
            };

            forceButton.Click += (s, e) =>
            {
                ForceVariable(variable.Name, variable.Type, inputBox.Text, variable, valueBox);
                inputBox.Text = "";
            };

            Grid.SetColumn(forceButton, 6);
            grid.Children.Add(forceButton);

            // Sil Butonu
            var deleteButton = new Button
            {
                Content = "Sil",
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 82, 82)),
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                CornerRadius = new CornerRadius(4),
                Height = 32,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                FontSize = 9,
                Padding = new Thickness(4)
            };

            deleteButton.Click += (s, e) =>
            {
                PLCVariables.Remove(variable);
                RefreshPLCVariablesUI();
            };

            Grid.SetColumn(deleteButton, 7);
            grid.Children.Add(deleteButton);

            border.Child = grid;
            return border;
        }

        /// <summary>
        /// String tipini Type'a çevir
        /// </summary>
        private Type GetTypeFromString(string typeString)
        {
            return typeString switch
            {
                "BOOL" => typeof(bool),
                "INT" => typeof(int),
                "DWORD" => typeof(uint),
                "REAL" => typeof(double),
                _ => typeof(string)
            };
        }

        /// <summary>
        /// PLC deðiþkenine force iþlemi yap
        /// </summary>
        private void ForceVariable(string variableName, string type, string newValue, PLCVariable variable, TextBox valueBox)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newValue))
                    return;

                object convertedValue = type switch
                {
                    "BOOL" => bool.Parse(newValue),
                    "INT" => int.Parse(newValue),
                    "DWORD" => uint.Parse(newValue),
                    "REAL" => double.Parse(newValue),
                    _ => newValue
                };

                // Þu anki deðeri güncelle
                variable.CurrentValue = convertedValue;
                valueBox.Text = convertedValue.ToString();

                // TODO: Gerçek PLC baðlantýsý burada yapýlýr
                // Örn: _plcConnection.WriteVariable(variableName, convertedValue);

                // Baþarý mesajý göster
                var dialog = new ContentDialog
                {
                    Title = "Force Baþarýlý",
                    Content = $"{variableName} deðiþkeni {convertedValue} olarak PLC'ye gönderildi.",
                    CloseButtonText = "Tamam",
                    XamlRoot = this.Content.XamlRoot,
                    RequestedTheme = ElementTheme.Dark
                };
                _ = dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Hata",
                    Content = $"Deðer dönüþtürülemedi: {ex.Message}",
                    CloseButtonText = "Tamam",
                    XamlRoot = this.Content.XamlRoot,
                    RequestedTheme = ElementTheme.Dark
                };
                _ = errorDialog.ShowAsync();
            }
        }

        /// <summary>
        /// Yeni PLC deðiþkeni ekle
        /// </summary>
        private async void AddNewPLCVariable()
        {
            var dialog = new ContentDialog
            {
                Title = "Yeni PLC Deðiþkeni Ekle",
                XamlRoot = this.Content.XamlRoot,
                RequestedTheme = ElementTheme.Dark,
                PrimaryButtonText = "Ekle",
                CloseButtonText = "Ýptal"
            };

            var stackPanel = new StackPanel { Spacing = 10 };

            var nameLabel = new TextBlock { Text = "Deðiþken Adý:", FontWeight = Microsoft.UI.Text.FontWeights.Bold };
            var nameBox = new TextBox { PlaceholderText = "Örn: D4 - Test Deðiþkeni" };

            var typeLabel = new TextBlock { Text = "Tip:", FontWeight = Microsoft.UI.Text.FontWeights.Bold, Margin = new Thickness(0, 10, 0, 0) };
            var typeCombo = new ComboBox { SelectedIndex = 0 };
            typeCombo.Items.Add("BOOL");
            typeCombo.Items.Add("INT");
            typeCombo.Items.Add("DWORD");
            typeCombo.Items.Add("REAL");

            var valueLabel = new TextBlock { Text = "Baþlangýç Deðeri:", FontWeight = Microsoft.UI.Text.FontWeights.Bold, Margin = new Thickness(0, 10, 0, 0) };
            var valueBox = new TextBox { PlaceholderText = "0" };

            var minLabel = new TextBlock { Text = "Min Deðer:", FontWeight = Microsoft.UI.Text.FontWeights.Bold, Margin = new Thickness(0, 10, 0, 0) };
            var minBox = new TextBox { PlaceholderText = "0" };

            var maxLabel = new TextBlock { Text = "Max Deðer:", FontWeight = Microsoft.UI.Text.FontWeights.Bold, Margin = new Thickness(0, 10, 0, 0) };
            var maxBox = new TextBox { PlaceholderText = "100" };

            stackPanel.Children.Add(nameLabel);
            stackPanel.Children.Add(nameBox);
            stackPanel.Children.Add(typeLabel);
            stackPanel.Children.Add(typeCombo);
            stackPanel.Children.Add(valueLabel);
            stackPanel.Children.Add(valueBox);
            stackPanel.Children.Add(minLabel);
            stackPanel.Children.Add(minBox);
            stackPanel.Children.Add(maxLabel);
            stackPanel.Children.Add(maxBox);

            dialog.Content = stackPanel;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(nameBox.Text))
            {
                try
                {
                    var typeStr = typeCombo.SelectedItem.ToString();
                    var currentValue = Convert.ChangeType(string.IsNullOrWhiteSpace(valueBox.Text) ? "0" : valueBox.Text, GetTypeFromString(typeStr));
                    var minValue = Convert.ChangeType(string.IsNullOrWhiteSpace(minBox.Text) ? "0" : minBox.Text, GetTypeFromString(typeStr));
                    var maxValue = Convert.ChangeType(string.IsNullOrWhiteSpace(maxBox.Text) ? "100" : maxBox.Text, GetTypeFromString(typeStr));

                    PLCVariables.Add(new PLCVariable
                    {
                        Name = nameBox.Text,
                        Type = typeStr,
                        CurrentValue = currentValue,
                        MinValue = minValue,
                        MaxValue = maxValue
                    });

                    RefreshPLCVariablesUI();
                }
                catch (Exception ex)
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "Hata",
                        Content = $"Deðiþken eklenemedi: {ex.Message}",
                        CloseButtonText = "Tamam",
                        XamlRoot = this.Content.XamlRoot,
                        RequestedTheme = ElementTheme.Dark
                    };
                     _ = errorDialog.ShowAsync();
                }
            }
        }
    }

    /// <summary>
    /// PLC Deðiþkeni Model Sýnýfý
    /// </summary>
    public class PLCVariable
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public object CurrentValue { get; set; }
        public object MinValue { get; set; }
        public object MaxValue { get; set; }
    }
}
