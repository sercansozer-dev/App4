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
using HslCommunication;
using HslCommunication.Profinet.Melsec;

namespace App4.PAGES
{
    public sealed partial class PLC_Page : Page
    {
        // PLC Deðiþkenleri Koleksiyonu
        public static ObservableCollection<PLCVariable> GlobalInputVariables { get; private set; } = new();
        public static ObservableCollection<PLCVariable> GlobalOutputVariables { get; private set; } = new();

        private ObservableCollection<PLCVariable> InputVariables { get; set; }
        private ObservableCollection<PLCVariable> OutputVariables { get; set; }

        // PLC Baðlantý Deðiþkenleri
        private MelsecMcNet _melsecNet;
        private bool _isConnected = false;
        private StringBuilder _logBuilder = new StringBuilder();
        private DispatcherTimer _timer;

        // Kalýcýlýk için dosya yolu
        private readonly string _variablesFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "App4", "PLC_Variables.json");

        public PLC_Page()
        {
            this.InitializeComponent();
            
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += Timer_Tick;

            InputVariables = GlobalInputVariables;
            OutputVariables = GlobalOutputVariables;
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
            // GlobalInputVariables'ý temizle ve doldur
            GlobalInputVariables.Clear();
            GlobalOutputVariables.Clear();

            // Default INPUT variables (User requested D0 at index 1)
            GlobalInputVariables.Add(new PLCVariable { Name = "D0 - Okunan Deðer", Type = "WORD", Direction = "Input", CurrentValue = 0, MinValue = 0, MaxValue = 65535 });
            GlobalInputVariables.Add(new PLCVariable { Name = "M0 - Acil Durdur", Type = "BOOL", Direction = "Input", CurrentValue = false, MinValue = false, MaxValue = true });
            GlobalInputVariables.Add(new PLCVariable { Name = "M1 - Sistem Ready", Type = "BOOL", Direction = "Input", CurrentValue = true, MinValue = false, MaxValue = true });

            // Default OUTPUT variables (User requested D0 at index 1 for write test)
            GlobalOutputVariables.Add(new PLCVariable { Name = "D0 - Yazýlan Deðer", Type = "WORD", Direction = "Output", CurrentValue = 0, MinValue = 0, MaxValue = 65535 });
            GlobalOutputVariables.Add(new PLCVariable { Name = "D1 - Ýþletim Modu", Type = "DWORD", Direction = "Output", CurrentValue = 0, MinValue = 0, MaxValue = 3 });
            GlobalOutputVariables.Add(new PLCVariable { Name = "D2 - Robot Hýzý", Type = "INT", Direction = "Output", CurrentValue = 75, MinValue = 0, MaxValue = 100 });

            // LocalInputVariables ve OutputVariables referansýný güncelle
            InputVariables = GlobalInputVariables;
            OutputVariables = GlobalOutputVariables;

            RefreshPLCVariablesUI();
        }

        private void RefreshPLCVariablesUI()
        {
            InputVariablesContainer.Children.Clear();
            OutputVariablesContainer.Children.Clear();

            for (int i = 0; i < InputVariables.Count; i++)
            {
                InputVariablesContainer.Children.Add(CreateInputVariableRow(i + 1, InputVariables[i]));
            }

            for (int i = 0; i < OutputVariables.Count; i++)
            {
                OutputVariablesContainer.Children.Add(CreateOutputVariableRow(i + 1, OutputVariables[i]));
            }
        }

        private Border CreateInputVariableRow(int index, PLCVariable variable)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 30, 35)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 4, 0, 4),
                BorderThickness = new Thickness(1.5),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(100, 76, 175, 80))
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(35) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(45) });

            // Index
            var indexTb = new TextBlock { Text = index.ToString(), FontSize = 9, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 184, 28)), TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
            Grid.SetColumn(indexTb, 0);
            grid.Children.Add(indexTb);

            // Name (Editable TextBox)
            var nameTb = new TextBox 
            { 
                Text = variable.Name, 
                FontSize = 9, 
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 45, 45, 50)),
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 210, 210, 210)),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(80, 76, 175, 80)),
                Padding = new Thickness(6, 4, 6, 4),
                Height = 28,
                VerticalAlignment = VerticalAlignment.Center
            };
            nameTb.TextChanged += (s, e) => { variable.Name = nameTb.Text; SaveVariablesToFile(); };
            Grid.SetColumn(nameTb, 1);
            grid.Children.Add(nameTb);

            // Type (Dropdown ComboBox)
            var typeCombo = new ComboBox 
            { 
                SelectedItem = variable.Type,
                FontSize = 9,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 45, 45, 50)),
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 184, 28)),
                Height = 28,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            var dataTypes = new[] { "BOOL", "INT", "DWORD", "REAL", "STRING", "WORD", "BYTE", "SINT" };
            foreach (var dt in dataTypes)
                typeCombo.Items.Add(dt);
            
            typeCombo.SelectedItem = variable.Type;
            typeCombo.SelectionChanged += (s, e) => 
            { 
                variable.Type = typeCombo.SelectedItem?.ToString() ?? "BOOL";
                SaveVariablesToFile();
            };
            Grid.SetColumn(typeCombo, 2);
            grid.Children.Add(typeCombo);

            // Value (Editable TextBox)
            var valueTb = new TextBox 
            { 
                Text = variable.CurrentValue?.ToString() ?? "0", 
                FontSize = 9, 
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 45, 45, 50)),
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 200, 100)),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(100, 76, 175, 80)),
                TextAlignment = TextAlignment.Center,
                Padding = new Thickness(6, 4, 6, 4),
                Height = 28,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            };
            valueTb.TextChanged += (s, e) => 
            { 
                if (double.TryParse(valueTb.Text, out var val))
                    variable.CurrentValue = val;
                SaveVariablesToFile();
            };
            Grid.SetColumn(valueTb, 3);
            grid.Children.Add(valueTb);

            // Delete Button
            var deleteBtn = new Button 
            { 
                Content = "×", 
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 70, 70)), 
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)), 
                CornerRadius = new CornerRadius(4), 
                Height = 28, 
                Width = 32,
                Padding = new Thickness(0), 
                FontSize = 16,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold 
            };
            deleteBtn.Click += (s, e) => DeleteVariable(variable);
            Grid.SetColumn(deleteBtn, 4);
            grid.Children.Add(deleteBtn);

            border.Child = grid;
            return border;
        }

        private Border CreateOutputVariableRow(int index, PLCVariable variable)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 30, 35)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 4, 0, 4),
                BorderThickness = new Thickness(1.5),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(100, 255, 152, 0))
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(35) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(65) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(45) });

            // Index
            var indexTb = new TextBlock { Text = index.ToString(), FontSize = 9, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 184, 28)), TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
            Grid.SetColumn(indexTb, 0);
            grid.Children.Add(indexTb);

            // Name (Editable TextBox)
            var nameTb = new TextBox 
            { 
                Text = variable.Name, 
                FontSize = 9, 
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 45, 45, 50)),
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 210, 210, 210)),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(80, 255, 152, 0)),
                Padding = new Thickness(6, 4, 6, 4),
                Height = 28,
                VerticalAlignment = VerticalAlignment.Center
            };
            nameTb.TextChanged += (s, e) => { variable.Name = nameTb.Text; SaveVariablesToFile(); };
            Grid.SetColumn(nameTb, 1);
            grid.Children.Add(nameTb);

            // Type (Dropdown ComboBox)
            var typeCombo = new ComboBox 
            { 
                SelectedItem = variable.Type,
                FontSize = 9,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 45, 45, 50)),
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 184, 28)),
                Height = 28,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            var dataTypes = new[] { "BOOL", "INT", "DWORD", "REAL", "STRING", "WORD", "BYTE", "SINT" };
            foreach (var dt in dataTypes)
                typeCombo.Items.Add(dt);
            
            typeCombo.SelectedItem = variable.Type;
            typeCombo.SelectionChanged += (s, e) => 
            { 
                variable.Type = typeCombo.SelectedItem?.ToString() ?? "BOOL";
                SaveVariablesToFile();
            };
            Grid.SetColumn(typeCombo, 2);
            grid.Children.Add(typeCombo);

            // Value (Editable TextBox)
            var valueTb = new TextBox 
            { 
                Text = variable.CurrentValue?.ToString() ?? "0", 
                FontSize = 9, 
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 45, 45, 50)),
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 200, 100)),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(100, 255, 152, 0)),
                TextAlignment = TextAlignment.Center,
                Padding = new Thickness(6, 4, 6, 4),
                Height = 28,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            };
            valueTb.TextChanged += (s, e) => 
            { 
                if (double.TryParse(valueTb.Text, out var val))
                    variable.CurrentValue = val;
                SaveVariablesToFile();
            };
            Grid.SetColumn(valueTb, 3);
            grid.Children.Add(valueTb);

            // Force Button (Only for OUTPUT)
            var forceBtn = new Button { Content = "Force", Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 200, 100)), Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)), CornerRadius = new CornerRadius(4), Height = 28, Width = 60, Padding = new Thickness(0), FontSize = 9, FontWeight = Microsoft.UI.Text.FontWeights.Bold };
            forceBtn.Click += async (s, e) => 
            { 
               await WriteVariableToPLC(variable);
            };
            Grid.SetColumn(forceBtn, 4);
            grid.Children.Add(forceBtn);

            // Delete Button
            var deleteBtn = new Button 
            { 
                Content = "×", 
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 70, 70)), 
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)), 
                CornerRadius = new CornerRadius(4), 
                Height = 28, 
                Width = 32,
                Padding = new Thickness(0), 
                FontSize = 16,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold 
            };
            deleteBtn.Click += (s, e) => DeleteVariable(variable);
            Grid.SetColumn(deleteBtn, 5);
            grid.Children.Add(deleteBtn);

            border.Child = grid;
            return border;
        }

        private void DeleteVariable(PLCVariable variable)
        {
            if (GlobalInputVariables.Contains(variable))
            {
                GlobalInputVariables.Remove(variable);
                InputVariables = GlobalInputVariables;
                AddLog($"[INFO] INPUT deðiþkeni silindi: {variable.Name}");
            }
            else if (GlobalOutputVariables.Contains(variable))
            {
                GlobalOutputVariables.Remove(variable);
                OutputVariables = GlobalOutputVariables;
                AddLog($"[INFO] OUTPUT deðiþkeni silindi: {variable.Name}");
            }
            RefreshPLCVariablesUI();
            SaveVariablesToFile();
        }

        private void AddVariableBtn_Click(object sender, RoutedEventArgs e)
        {
            GlobalOutputVariables.Add(new PLCVariable { Name = $"D{GlobalOutputVariables.Count} - Yeni", Type = "BOOL", Direction = "Output", CurrentValue = false, MinValue = false, MaxValue = true });
            OutputVariables = GlobalOutputVariables;
            RefreshPLCVariablesUI();
        }

        private void AddInputVariableBtn_Click(object sender, RoutedEventArgs e)
        {
            GlobalInputVariables.Add(new PLCVariable { Name = $"M{GlobalInputVariables.Count} - Yeni Input", Type = "BOOL", Direction = "Input", CurrentValue = false, MinValue = false, MaxValue = true });
            InputVariables = GlobalInputVariables;
            RefreshPLCVariablesUI();
            SaveVariablesToFile();
        }

        private void AddOutputVariableBtn_Click(object sender, RoutedEventArgs e)
        {
            GlobalOutputVariables.Add(new PLCVariable { Name = $"D{GlobalOutputVariables.Count} - Yeni Output", Type = "BOOL", Direction = "Output", CurrentValue = false, MinValue = false, MaxValue = true });
            OutputVariables = GlobalOutputVariables;
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
                AddLog($"[INFO] Baðlanýlýyor: {PLCIPAddressBox.Text}:{PLCPortBox.Text}...");
                
                // Mitsubishi PLC (R Serisi) için MC Protocol Baðlantýsý
                _melsecNet = new MelsecMcNet(PLCIPAddressBox.Text, int.Parse(PLCPortBox.Text));
                
                // Baðlantý iþlemini asenkron olarak gerçekleþtir
                var connect = await _melsecNet.ConnectServerAsync();

                if (connect.IsSuccess)
                {
                    _isConnected = true;
                    UpdateConnectionStatus(true);
                    AddLog("[SUCCESS] PLC'ye baþarýyla baðlanýldý!");
                    ConnectPLCBtn.Content = "?? Baðlantýyý Kes";
                    _timer.Start();
                }
                else
                {
                    AddLog($"[ERROR] Baðlantý Baþarýsýz: {connect.Message}");
                }
            }
            catch (Exception ex)
            {
                AddLog($"[ERROR] {ex.Message}");
            }
        }

        private void DisconnectPLC()
        {
            if (_melsecNet != null)
            {
                _melsecNet.ConnectClose();
                _melsecNet = null;
            }

            _isConnected = false;
            UpdateConnectionStatus(false);
            ConnectPLCBtn.Content = "?? Baðlan";
            _timer.Stop();
            AddLog("[INFO] Baðlantý kullanýcý tarafýndan kesildi.");
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

                // Global koleksiyonlarý kullan
                var data = new
                {
                    Input = GlobalInputVariables.ToList(),
                    Output = GlobalOutputVariables.ToList()
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
                        GlobalInputVariables.Clear();
                        foreach (var item in inputArray.EnumerateArray())
                        {
                            var variable = JsonSerializer.Deserialize<PLCVariable>(item.GetRawText());
                            if (variable != null)
                                GlobalInputVariables.Add(variable);
                        }
                    }

                    if (data.TryGetProperty("Output", out var outputArray))
                    {
                        GlobalOutputVariables.Clear();
                        foreach (var item in outputArray.EnumerateArray())
                        {
                            var variable = JsonSerializer.Deserialize<PLCVariable>(item.GetRawText());
                            if (variable != null)
                                GlobalOutputVariables.Add(variable);
                        }
                    }

                    InputVariables = GlobalInputVariables;
                    OutputVariables = GlobalOutputVariables;
                    RefreshPLCVariablesUI();
                    AddLog($"[INFO] Deðiþkenler dosyadan yüklendi: {GlobalInputVariables.Count} INPUT + {GlobalOutputVariables.Count} OUTPUT");
                }
            }
            catch (Exception ex)
            {
                AddLog($"[ERROR] Deðiþkenler yüklenirken hata: {ex.Message}");
            }
        }

        private async void Timer_Tick(object sender, object e)
        {
            if (!_isConnected || _melsecNet == null) return;
            await SyncVariablesWithPLC();
        }

        private async Task SyncVariablesWithPLC()
        {
            // Read Inputs (Poll real values from PLC)
            for (int i = 0; i < InputVariables.Count; i++)
            {
                await ReadPLCValue(InputVariables[i], i, InputVariablesContainer);
            }

            // DO NOT Read Outputs automatically. 
            // This prevents overwriting the value the user is typing before they click "Force".
            // Users can verify the write by looking at the Input list if they map the same address.
            
            /*
            for (int i = 0; i < OutputVariables.Count; i++)
            {
                await ReadPLCValue(OutputVariables[i], i, OutputVariablesContainer);
            }
            */
        }

        private async Task ReadPLCValue(PLCVariable variable, int index, StackPanel container)
        {
            try
            {
                string address = variable.Name.Split('-')[0].Trim();
                if (string.IsNullOrEmpty(address)) return;

                OperateResult readResult = new OperateResult();
                string newValStr = "";

                switch (variable.Type)
                {
                    case "BOOL":
                        var b = await _melsecNet.ReadBoolAsync(address);
                        readResult = b;
                        newValStr = b.Content.ToString();
                        variable.CurrentValue = b.Content;
                        break;
                    case "INT":
                    case "WORD":
                        var i = await _melsecNet.ReadInt16Async(address);
                        readResult = i;
                        newValStr = i.Content.ToString();
                        variable.CurrentValue = i.Content;
                        break;
                    case "DWORD":
                        var d = await _melsecNet.ReadInt32Async(address);
                        readResult = d;
                        newValStr = d.Content.ToString();
                        variable.CurrentValue = d.Content;
                        break;
                    case "REAL":
                        var f = await _melsecNet.ReadFloatAsync(address);
                        readResult = f;
                        newValStr = f.Content.ToString("F2");
                        variable.CurrentValue = f.Content;
                        break;
                }

                if (readResult.IsSuccess)
                {
                    // Update UI if exists
                   if (container.Children.Count > index && container.Children[index] is Border b && b.Child is Grid g)
                   {
                        // Column 3 is Value TextBox
                        foreach(var child in g.Children)
                        {
                            if (Grid.GetColumn(child as FrameworkElement) == 3 && child is TextBox tb)
                            {
                                if (tb.FocusState == FocusState.Unfocused) // Only update if user isn't typing
                                    tb.Text = newValStr;
                            }
                        }
                   }
                }
            }
            catch { }
        }

        private async Task WriteVariableToPLC(PLCVariable variable)
        {
            if (!_isConnected || _melsecNet == null) 
            {
                AddLog("[WARN] PLC Baðlý deðil!");
                return;
            }

            try
            {
                string address = variable.Name.Split('-')[0].Trim();
                AddLog($"[INFO] Yazýlýyor: {address} = {variable.CurrentValue} ({variable.Type})");

                OperateResult writeResult = new OperateResult();

                switch (variable.Type)
                {
                    case "BOOL":
                        bool bVal = variable.CurrentValue.ToString().ToLower() == "true";
                        writeResult = await _melsecNet.WriteAsync(address, bVal);
                        break;
                    case "INT":
                    case "WORD":
                        short sVal = Convert.ToInt16(variable.CurrentValue);
                        writeResult = await _melsecNet.WriteAsync(address, sVal);
                        break;
                    case "DWORD":
                        int iVal = Convert.ToInt32(variable.CurrentValue);
                        writeResult = await _melsecNet.WriteAsync(address, iVal);
                        break;
                    case "REAL":
                        float fVal = Convert.ToSingle(variable.CurrentValue);
                        writeResult = await _melsecNet.WriteAsync(address, fVal);
                        break;
                    default:
                        AddLog($"[ERROR] Desteklenmeyen tip: {variable.Type}");
                        return;
                }

                if (writeResult.IsSuccess)
                    AddLog($"[SUCCESS] Yazma baþarýlý: {address}");
                else
                    AddLog($"[ERROR] Yazma hatasý: {writeResult.Message}");
            }
            catch (Exception ex)
            {
                AddLog($"[ERROR] Yazma exception: {ex.Message}");
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
