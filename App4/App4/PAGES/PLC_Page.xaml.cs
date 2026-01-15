using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.UI;

namespace App4.PAGES
{
    public sealed partial class PLC_Page : Page
    {
        // PLC Deðiþkenleri Koleksiyonu
        private ObservableCollection<PLCVariable> InputVariables { get; set; }
        private ObservableCollection<PLCVariable> OutputVariables { get; set; }

        // PLC Baðlantý Deðiþkenleri
        private TcpClient _plcClient;
        private NetworkStream _plcStream;
        private bool _isConnected = false;
        private StringBuilder _logBuilder = new StringBuilder();

        // Kalýcýlýk için dosya yolu
        private readonly string _variablesFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "App4", "PLC_Variables.json");

        public PLC_Page()
        {
            this.InitializeComponent();
            InputVariables = new ObservableCollection<PLCVariable>();
            OutputVariables = new ObservableCollection<PLCVariable>();
            InitializeIOMapping();
            InitializeSystemMonitor();
            InitializeStatistics();
            InitializePLCVariables();
            LoadVariablesFromFile();
        }

        private void InitializeStatistics()
        {
            TotalTestsLabel.Text = "1,247";
            SuccessTestsLabel.Text = "1,186";
            FailedTestsLabel.Text = "61";
            UptimeLabel.Text = "47.3h";
            LastTestLabel.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            AvgDurationLabel.Text = "2.36 sn";
            TodayTestsLabel.Text = "47 Test";
            SystemQualityLabel.Text = "A+";
        }

        private void InitializeIOMapping()
        {
            var mappings = new List<(string, string, string, string, string)>
            {
                ("PLC", "D0", "Robot", "Aktif", "1"),
                ("Robot", "X1", "Kamera", "Aktif", "1"),
                ("Kamera", "Result", "PLC", "Aktif", "Pass"),
            };

            foreach (var mapping in mappings)
            {
                IOMapContainer.Children.Add(CreateIORow(mapping.Item1, mapping.Item2, mapping.Item3, mapping.Item4, mapping.Item5));
            }
        }

        private void InitializeSystemMonitor()
        {
            // Statik veriler zaten XAML'de tanýmlý
        }

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

            AddGridChild(grid, 0, new TextBlock { Text = source, FontSize = 10, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 164, 239)) });
            AddGridChild(grid, 1, new TextBlock { Text = channel, FontSize = 10, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 184, 28)) });
            AddGridChild(grid, 2, new TextBlock { Text = destination, FontSize = 10, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 255, 136)) });
            AddGridChild(grid, 3, new TextBlock { Text = status, FontSize = 10, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80)) });
            AddGridChild(grid, 4, new TextBlock { Text = value, FontSize = 10, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)), TextAlignment = TextAlignment.Right });

            return grid;
        }

        private void AddGridChild(Grid grid, int column, UIElement element)
        {
            Grid.SetColumn(element as FrameworkElement, column);
            grid.Children.Add(element);
        }

        private void InitializePLCVariables()
        {
            // Default INPUT variables
            InputVariables.Add(new PLCVariable { Name = "M0 - Acil Durdur", Type = "BOOL", Direction = "Input", CurrentValue = false, MinValue = false, MaxValue = true });
            InputVariables.Add(new PLCVariable { Name = "M1 - Sistem Ready", Type = "BOOL", Direction = "Input", CurrentValue = true, MinValue = false, MaxValue = true });
            InputVariables.Add(new PLCVariable { Name = "D0 - Durum Kodu", Type = "INT", Direction = "Input", CurrentValue = 0, MinValue = 0, MaxValue = 100 });

            // Default OUTPUT variables
            OutputVariables.Add(new PLCVariable { Name = "D0 - Baþlat Sinyali", Type = "BOOL", Direction = "Output", CurrentValue = false, MinValue = false, MaxValue = true });
            OutputVariables.Add(new PLCVariable { Name = "D1 - Ýþletim Modu", Type = "DWORD", Direction = "Output", CurrentValue = 0, MinValue = 0, MaxValue = 3 });
            OutputVariables.Add(new PLCVariable { Name = "D2 - Robot Hýzý", Type = "INT", Direction = "Output", CurrentValue = 75, MinValue = 0, MaxValue = 100 });
            OutputVariables.Add(new PLCVariable { Name = "D3 - Sýcaklýk Setpoint", Type = "REAL", Direction = "Output", CurrentValue = 42.5, MinValue = 20.0, MaxValue = 80.0 });

            RefreshPLCVariablesUI();
        }

        private void RefreshPLCVariablesUI()
        {
            InputVariablesContainer.Children.Clear();
            OutputVariablesContainer.Children.Clear();

            for (int i = 0; i < InputVariables.Count; i++)
            {
                InputVariablesContainer.Children.Add(CreateCompactVariableRow(i + 1, InputVariables[i]));
            }

            for (int i = 0; i < OutputVariables.Count; i++)
            {
                OutputVariablesContainer.Children.Add(CreateCompactVariableRow(i + 1, OutputVariables[i]));
            }
        }

        private Border CreateCompactVariableRow(int index, PLCVariable variable)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 35, 35, 40)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6),
                Margin = new Thickness(0, 2, 0, 2),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(80, 0, 164, 239))
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(25) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(65) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(45) });

            // Index
            var indexTb = new TextBlock { Text = index.ToString(), FontSize = 9, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 184, 28)), TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(indexTb, 0);
            grid.Children.Add(indexTb);

            // Name
            var nameTb = new TextBlock { Text = variable.Name, FontSize = 9, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 200, 200, 200)), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(nameTb, 1);
            grid.Children.Add(nameTb);

            // Type
            var typeTb = new TextBlock { Text = variable.Type, FontSize = 9, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 184, 28)), TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontWeight = Microsoft.UI.Text.FontWeights.Bold };
            Grid.SetColumn(typeTb, 2);
            grid.Children.Add(typeTb);

            // Value
            var valueTb = new TextBlock { Text = variable.CurrentValue?.ToString() ?? "0", FontSize = 9, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80)), TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontWeight = Microsoft.UI.Text.FontWeights.Bold };
            Grid.SetColumn(valueTb, 3);
            grid.Children.Add(valueTb);

            // Min/Max
            var minMaxTb = new TextBlock { Text = $"{variable.MinValue}..{variable.MaxValue}", FontSize = 8, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 120, 120, 120)), TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(minMaxTb, 4);
            grid.Children.Add(minMaxTb);

            // Force Button
            var forceBtn = new Button { Content = "Force", Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80)), Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)), CornerRadius = new CornerRadius(3), Height = 24, Padding = new Thickness(4, 0, 4, 0), FontSize = 8, FontWeight = Microsoft.UI.Text.FontWeights.Bold };
            Grid.SetColumn(forceBtn, 5);
            grid.Children.Add(forceBtn);

            // Delete Button
            var deleteBtn = new Button { Content = "Sil", Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 82, 82)), Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)), CornerRadius = new CornerRadius(3), Height = 24, Padding = new Thickness(4, 0, 4, 0), FontSize = 8, FontWeight = Microsoft.UI.Text.FontWeights.Bold };
            Grid.SetColumn(deleteBtn, 6);
            grid.Children.Add(deleteBtn);

            border.Child = grid;
            return border;
        }

        private void AddVariableBtn_Click(object sender, RoutedEventArgs e)
        {
            OutputVariables.Add(new PLCVariable { Name = $"D{OutputVariables.Count} - Yeni", Type = "BOOL", Direction = "Output", CurrentValue = false, MinValue = false, MaxValue = true });
            RefreshPLCVariablesUI();
        }

        private void AddInputVariableBtn_Click(object sender, RoutedEventArgs e)
        {
            InputVariables.Add(new PLCVariable { Name = $"M{InputVariables.Count} - Yeni Input", Type = "BOOL", Direction = "Input", CurrentValue = false, MinValue = false, MaxValue = true });
            RefreshPLCVariablesUI();
            SaveVariablesToFile();
        }

        private void AddOutputVariableBtn_Click(object sender, RoutedEventArgs e)
        {
            OutputVariables.Add(new PLCVariable { Name = $"D{OutputVariables.Count} - Yeni Output", Type = "BOOL", Direction = "Output", CurrentValue = false, MinValue = false, MaxValue = true });
            RefreshPLCVariablesUI();
            SaveVariablesToFile();
        }

        private void ExportVariablesBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "PLC_Exports");
                Directory.CreateDirectory(path);
                var file = Path.Combine(path, $"PLC_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.json");
                var json = JsonSerializer.Serialize(InputVariables.Concat(OutputVariables).ToList(), new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(file, json);
                AddLog("[SUCCESS] Tüm deðiþkenler dýþa aktarýldý!");
            }
            catch (Exception ex)
            {
                AddLog($"[ERROR] {ex.Message}");
            }
        }

        private async void ConnectPLCBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isConnected) DisconnectPLC();
            else await ConnectToPLC();
        }

        private async Task ConnectToPLC()
        {
            try
            {
                _plcClient = new TcpClient();
                await _plcClient.ConnectAsync(PLCIPAddressBox.Text, int.Parse(PLCPortBox.Text));
                _isConnected = true;
                UpdateConnectionStatus(true);
                AddLog("[SUCCESS] PLC'ye baðlanýldý!");
                ConnectPLCBtn.Content = "?? Baðlantýyý Kes";
            }
            catch (Exception ex)
            {
                AddLog($"[ERROR] {ex.Message}");
            }
        }

        private void DisconnectPLC()
        {
            _plcStream?.Close();
            _plcClient?.Close();
            _isConnected = false;
            UpdateConnectionStatus(false);
            ConnectPLCBtn.Content = "?? Baðlan";
            AddLog("[INFO] Baðlantý kesildi");
        }

        private void ClearLogBtn_Click(object sender, RoutedEventArgs e)
        {
            _logBuilder.Clear();
            LogTextBlock.Text = "";
        }

        private void SaveVariablesToFile()
        {
            try
            {
                var directory = Path.GetDirectoryName(_variablesFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var data = new
                {
                    Input = InputVariables.ToList(),
                    Output = OutputVariables.ToList()
                };

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_variablesFilePath, json);
            }
            catch (Exception ex)
            {
                AddLog($"[ERROR] Deðiþkenler kaydedilirken hata: {ex.Message}");
            }
        }

        private void LoadVariablesFromFile()
        {
            try
            {
                if (File.Exists(_variablesFilePath))
                {
                    var json = File.ReadAllText(_variablesFilePath);
                    var data = JsonSerializer.Deserialize<JsonElement>(json);

                    if (data.TryGetProperty("Input", out var inputArray))
                    {
                        InputVariables.Clear();
                        foreach (var item in inputArray.EnumerateArray())
                        {
                            var variable = JsonSerializer.Deserialize<PLCVariable>(item.GetRawText());
                            if (variable != null)
                                InputVariables.Add(variable);
                        }
                    }

                    if (data.TryGetProperty("Output", out var outputArray))
                    {
                        OutputVariables.Clear();
                        foreach (var item in outputArray.EnumerateArray())
                        {
                            var variable = JsonSerializer.Deserialize<PLCVariable>(item.GetRawText());
                            if (variable != null)
                                OutputVariables.Add(variable);
                        }
                    }

                    RefreshPLCVariablesUI();
                    AddLog($"[INFO] Deðiþkenler dosyadan yüklendi: {InputVariables.Count} INPUT + {OutputVariables.Count} OUTPUT");
                }
            }
            catch (Exception ex)
            {
                AddLog($"[ERROR] Deðiþkenler yüklenirken hata: {ex.Message}");
            }
        }

        private void AddLog(string message)
        {
            _logBuilder.AppendLine($"[{DateTime.Now:HH:mm:ss}] {message}");
            DispatcherQueue.TryEnqueue(() => LogTextBlock.Text = _logBuilder.ToString());
        }

        private void UpdateConnectionStatus(bool isConnected)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (isConnected)
                {
                    ConnectionStatusIndicator.Fill = new SolidColorBrush(Color.FromArgb(255, 76, 175, 80));
                    ConnectionStatusLabel.Text = "Baðlý";
                }
                else
                {
                    ConnectionStatusIndicator.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 82, 82));
                    ConnectionStatusLabel.Text = "Baðlý Deðil";
                }
            });
        }
    }

    public class PLCVariable
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("direction")]
        public string Direction { get; set; } = "Output";

        [JsonPropertyName("currentValue")]
        public object CurrentValue { get; set; }

        [JsonPropertyName("minValue")]
        public object MinValue { get; set; }

        [JsonPropertyName("maxValue")]
        public object MaxValue { get; set; }
    }
}
