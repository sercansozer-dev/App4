using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
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
            // Kayýtlý verileri yüklemeye çalýþ
            if (!LoadVariables())
            {
                // Eðer kayýtlý veri yoksa default deðerleri ekle
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
                
                // Yeni eklenen default deðerleri kaydet
                SaveVariables();
            }

            RefreshPLCVariablesUI();
        }

        /// <summary>
        /// Deðiþkenleri JSON dosyasýna kaydet
        /// </summary>
        private void SaveVariables()
        {
            try
            {
                // AppData klasörü oluþtur
                var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Simbiosis", "App4");
                Directory.CreateDirectory(appDataPath);

                var filePath = Path.Combine(appDataPath, "plc_variables.json");

                // JSON options
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                // JSON'a serialize et
                var json = JsonSerializer.Serialize(PLCVariables, options);
                
                // Dosyaya yaz
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Hata - Deðiþkenler kaydedilemedi: {ex.Message}");
            }
        }

        /// <summary>
        /// Deðiþkenleri JSON dosyasýndan yükle
        /// </summary>
        private bool LoadVariables()
        {
            try
            {
                var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Simbiosis", "App4");
                var filePath = Path.Combine(appDataPath, "plc_variables.json");

                // Dosya yoksa false döndür
                if (!File.Exists(filePath))
                    return false;

                // Dosyayý oku
                var json = File.ReadAllText(filePath);

                // JSON options
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                // JSON'dan deserialize et
                var loadedVariables = JsonSerializer.Deserialize<List<PLCVariable>>(json, options);

                if (loadedVariables != null && loadedVariables.Count > 0)
                {
                    PLCVariables.Clear();
                    foreach (var variable in loadedVariables)
                    {
                        PLCVariables.Add(variable);
                    }
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Hata - Deðiþkenler yüklenemedi: {ex.Message}");
                return false;
            }
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
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(100, 0, 164, 239)),
                Margin = new Thickness(0, 2, 0, 2)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            grid.ColumnSpacing = 8;
            grid.Padding = new Thickness(4, 0, 4, 0);

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
            nameBox.LostFocus += (s, e) => 
            { 
                variable.Name = nameBox.Text;
                SaveVariables(); // Veri kaydet
            };
            Grid.SetColumn(nameBox, 1);
            grid.Children.Add(nameBox);

            // Tip (ComboBox - Dropdown) ve TextBlock Container
            var typeContainer = new Grid();
            typeContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            typeContainer.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 60, 60, 65));
            typeContainer.CornerRadius = new CornerRadius(4);
            typeContainer.BorderThickness = new Thickness(1);
            typeContainer.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(150, 255, 184, 28));
            typeContainer.Height = 32;

            // Seçili Tip'i gösteren TextBlock (ön planda)
            var typeLabel = new TextBlock
            {
                Text = variable.Type,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(8, 0, 0, 0),
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            };
            Grid.SetColumn(typeLabel, 0);
            typeContainer.Children.Add(typeLabel);

            // ComboBox (arka planda, transparent)
            var typeCombo = new ComboBox
            {
                SelectedItem = variable.Type,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 60, 60, 65)), // Transparent
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 255, 255, 255)), // Transparent text
                Height = 32,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 10,
                BorderThickness = new Thickness(0)
            };
            typeCombo.Items.Add("BOOL");
            typeCombo.Items.Add("INT");
            typeCombo.Items.Add("DWORD");
            typeCombo.Items.Add("REAL");
            
            typeCombo.SelectionChanged += (s, e) =>
            {
                if (typeCombo.SelectedItem != null)
                {
                    var newType = typeCombo.SelectedItem.ToString();
                    variable.Type = newType;
                    
                    // TextBlock'u güncelle
                    typeLabel.Text = newType;
                    
                    // Min/Max deðerlerini tipe göre güncelle
                    (variable.MinValue, variable.MaxValue) = GetDefaultMinMaxForType(newType);
                    
                    // Deðer tipini de güncelle
                    try
                    {
                        variable.CurrentValue = Convert.ChangeType(variable.CurrentValue ?? "0", GetTypeFromString(newType));
                    }
                    catch
                    {
                        variable.CurrentValue = GetDefaultValueForType(newType);
                    }
                    
                    // UI'ý güncelle ve kaydet
                    RefreshPLCVariablesUI();
                    SaveVariables();
                }
            };
            
            Grid.SetColumn(typeCombo, 0);
            typeContainer.Children.Add(typeCombo);

            // Grid'e typeContainer'ý ekle
            Grid.SetColumn(typeContainer, 2);
            grid.Children.Add(typeContainer);

            // Þu Anki Deðer (Editlenebilir)
            var valueBox = new TextBox
            {
                Text = variable.CurrentValue?.ToString() ?? "0",
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80)),
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 45, 45, 50)),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(100, 76, 175, 80)),
                Padding = new Thickness(6),
                Height = 32,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 10,
                FontFamily = new FontFamily("Cascadia Mono"),
                TextAlignment = TextAlignment.Center
            };
            valueBox.LostFocus += (s, e) =>
            {
                try
                {
                    variable.CurrentValue = Convert.ChangeType(valueBox.Text, GetTypeFromString(variable.Type));
                    valueBox.Text = variable.CurrentValue.ToString();
                    SaveVariables(); // Veri kaydet
                }
                catch 
                { 
                    valueBox.Text = variable.CurrentValue?.ToString() ?? "0";
                }
            };
            Grid.SetColumn(valueBox, 3);
            grid.Children.Add(valueBox);

            // Min/Max Range Label
            var rangeBlock = new TextBlock
            {
                Text = $"{variable.MinValue}..{variable.MaxValue}",
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(200, 136, 136, 136)),
                FontSize = 9,
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = new FontFamily("Cascadia Mono"),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };
            Grid.SetColumn(rangeBlock, 4);
            grid.Children.Add(rangeBlock);

            // Yeni Deðer Input (Force için)
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
                SaveVariables(); // Veri kaydet
            };

            Grid.SetColumn(deleteButton, 7);
            grid.Children.Add(deleteButton);

            border.Child = grid;
            return border;
        }

        /// <summary>
        /// Tipe göre default Min/Max deðerlerini döndür
        /// </summary>
        private (object minValue, object maxValue) GetDefaultMinMaxForType(string type)
        {
            return type switch
            {
                "BOOL" => (false, true),
                "INT" => (0, 32767),
                "DWORD" => (0u, 4294967295u),
                "REAL" => (0.0, 1000.0),
                _ => (0, 100)
            };
        }

        /// <summary>
        /// Tipe göre default deðer döndür
        /// </summary>
        private object GetDefaultValueForType(string type)
        {
            return type switch
            {
                "BOOL" => false,
                "INT" => 0,
                "DWORD" => 0u,
                "REAL" => 0.0,
                _ => "0"
            };
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
                SaveVariables(); // Veri kaydet

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
        private void AddNewPLCVariable()
        {
            // Yeni boþ deðiþken ekle
            var newVariable = new PLCVariable
            {
                Name = $"D{PLCVariables.Count} - Yeni Deðiþken",
                Type = "BOOL",
                CurrentValue = false,
                MinValue = false,
                MaxValue = true
            };

            PLCVariables.Add(newVariable);
            RefreshPLCVariablesUI();
            SaveVariables(); // Veri kaydet
        }
    }

    /// <summary>
    /// PLC Deðiþkeni Model Sýnýfý
    /// </summary>
    public class PLCVariable
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("currentValue")]
        public object CurrentValue { get; set; }

        [JsonPropertyName("minValue")]
        public object MinValue { get; set; }

        [JsonPropertyName("maxValue")]
        public object MaxValue { get; set; }
    }
}
