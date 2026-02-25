using App4.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace App4
{
    public sealed partial class Auto_Page : Page
    {
        // ARTIK GLOBAL DATA'YI KULLANIYORUZ
        // Başına 'App4.Utilities.' ekliyoruz
        public ObservableCollection<App4.Utilities.RfidDef> KnownRfids => GlobalData.KnownRfids;
        public ObservableCollection<StationViewModel> Stations => GlobalData.Stations;

        public ObservableCollection<App4.Utilities.SystemCheckItem> SystemCheckList => GlobalData.SystemCheckList;
        public ObservableCollection<PlcVariable> GeneralInputVars => GlobalData.GeneralInputVars;
        public ObservableCollection<PlcVariable> GeneralOutputVars => GlobalData.GeneralOutputVars;
        public ObservableCollection<PlcVariable> Station1Vars => GlobalData.Station1Vars;
        public ObservableCollection<PlcVariable> Station2Vars => GlobalData.Station2Vars;
        public ObservableCollection<PlcVariable> Station3Vars => GlobalData.Station3Vars;
        public ObservableCollection<PlcVariable> Station1Outputs => GlobalData.Station1Outputs;
        public ObservableCollection<PlcVariable> Station2Outputs => GlobalData.Station2Outputs;
        public ObservableCollection<PlcVariable> Station3Outputs => GlobalData.Station3Outputs;

        public ObservableCollection<App4.Utilities.LogEntry> SystemLogs { get; set; } = new();
        public ObservableCollection<string> AvailableInputPlcTags { get; set; } = new();
        public ObservableCollection<string> AvailableOutputPlcTags { get; set; } = new();
        public ObservableCollection<string> AvailableModels { get; set; } = new();

        // --- ARIZA MÜHÜRLEME (LATCHING) DEĞİŞKENLERİ ---
        private bool _isLatchedFault = false;
        private string _latchedErrorMessage = "";

        public Auto_Page()
        {
            this.InitializeComponent();
            this.DataContext = this;

            // Tag listelerini ve modelleri doldur
            InitializeAvailablePlcTags();
            InitializeAvailableModels();

            // Olayları dinlemeye başla (Sayfa her açıldığında tekrar bağlanır)
            this.Loaded += Page_Loaded;
            this.Unloaded += Page_Unloaded;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            foreach (var s in Stations)
            {
                s.PropertyChanged -= Station_PropertyChanged;
            }
            
            foreach (var rfid in KnownRfids)
            {
                rfid.PropertyChanged -= Rfid_PropertyChanged;
            }
            KnownRfids.CollectionChanged -= KnownRfids_CollectionChanged;
        }

        private async void InitializeAvailableModels()
        {
            try
            {
                await App4.Utilities.RecipeManager.RefreshModelLibraryAsync();
                AvailableModels.Clear();
                
                string modelsRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utilities", "Models");

                if (App4.Utilities.GlobalSettings.AppState.ModelLibrary != null)
                {
                    foreach (var item in App4.Utilities.GlobalSettings.AppState.ModelLibrary)
                    {
                        // Use relative path so localmodels mapping works for subfolders
                        // e.g. "Sub/Model.glb" -> https://localmodels/Sub/Model.glb
                        string relativePath = Path.GetRelativePath(modelsRoot, item.FilePath).Replace("\\", "/");
                        AvailableModels.Add(relativePath);
                    }
                }
            }
            catch { }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. İstasyon Olaylarını Bağla
            foreach (var s in Stations)
            {
                s.PropertyChanged -= Station_PropertyChanged;
                s.PropertyChanged += Station_PropertyChanged;
            }

            // 2. Değişkenleri PLC Servisine Bağla
            void BindVars(ObservableCollection<PlcVariable> list)
            {
                foreach (var v in list)
                {
                    // Olayı önce çıkar sonra ekle (çift tetiklemeyi önler)
                    v.PropertyChanged -= LocalVariable_PropertyChanged;
                    v.PropertyChanged += LocalVariable_PropertyChanged;

                    // PLC ile bağlantıyı kur
                    ConnectToPlcVariable(v);
                }
            }

            // Listeleri bağla
            BindVars(GeneralInputVars); BindVars(GeneralOutputVars);
            BindVars(Station1Vars); BindVars(Station1Outputs);
            BindVars(Station2Vars); BindVars(Station2Outputs);
            BindVars(Station3Vars); BindVars(Station3Outputs);

            // --- YENİ EKLENEN KISIM: ZORLA GÜNCELLEME (FORCE SYNC) ---
            // Sayfa açıldığında "Değişiklik beklemeden" mevcut değerleri istasyonlara yaz
            void ForceUpdateStations(ObservableCollection<PlcVariable> list)
            {
                foreach (var v in list)
                {
                    // Değer boş değilse istasyon durumunu güncelle
                    if (v.Value != null)
                    {
                        UpdateStationStatus(v.Name, v.Value);
                        if (v.Name == "SLIDER_POS_ACT") UpdateSliderPosition(v.Value);
                    }
                }
            }

            // Tüm listelerdeki verileri ekrana yansıt
            ForceUpdateStations(GeneralInputVars);
            ForceUpdateStations(Station1Vars);
            ForceUpdateStations(Station2Vars);
            ForceUpdateStations(Station3Vars);
            // ---------------------------------------------------------

            // 3. Hat Durum Işıklarını Yak
            UpdateLineStatusVisuals();

            // 3.5. OTO/MANUEL Switch'leri PLC değişkeniyle senkronize et
            var switchVar = GeneralOutputVars.FirstOrDefault(v => v.Name == "LINE_AUTO_MANUAL_CMD");
            if (switchVar != null)
            {
                bool isOn = switchVar.Value?.ToUpper() == "TRUE" || switchVar.Value == "1";
                if (LineAutoManualSwitch != null) LineAutoManualSwitch.IsOn = isOn;
                if (KontrolLineAutoManualSwitch != null) KontrolLineAutoManualSwitch.IsOn = isOn;
            }

             // 4. Viewerları Başlat
            _ = InitializeStationViewers();

            // 5. RFID Model Değişikliklerini Dinle
            foreach (var rfid in KnownRfids)
            {
                rfid.PropertyChanged -= Rfid_PropertyChanged;
                rfid.PropertyChanged += Rfid_PropertyChanged;
            }
            KnownRfids.CollectionChanged -= KnownRfids_CollectionChanged;
            KnownRfids.CollectionChanged += KnownRfids_CollectionChanged;

            // 6. Robot Slider Pozisyonlarını ve Sinyal Panellerini Bağla
            InitializeRobotSliderPositions();
            InitializeRobotSignalPanels();

            // 7. Robot Durum İzleme Panelini Bağla
            InitializeRobotStatusMonitoring();
        }

        private void InitializeRobotSliderPositions()
        {
            var sliderCanvas = this.FindName("SliderCanvas") as Canvas;
            var robotPlatform = this.FindName("RobotPlatform") as Grid;
            var sliderActualPosText = this.FindName("SliderActualPosText") as TextBlock;
            var sliderStationText = this.FindName("SliderStationText") as TextBlock;
            var sliderHedefStationText = this.FindName("SliderHedefStationText") as TextBlock;

            // İstasyon pozisyon label'larını güncelle (Manuel sayfadan girilen KL100 değerleri)
            UpdateSliderStationPosLabels();

            if (sliderCanvas == null || robotPlatform == null) return;

            var robots = KukaRobotManager.Instance.Robots;

            // İstasyon sinyali (1,2,3,4) → görseli o istasyonun önüne taşı
            void RefreshSlider()
            {
                int station = GlobalData.GetSliderStationNumber();
                int colIndex = StationToColumnIndex(station);
                UpdatePlatformToStation(colIndex);

                // Başlık bilgilerini güncelle
                if (sliderStationText != null)
                    sliderStationText.Text = station == 4 ? "BAKIM" : $"İSTASYON {station}";

                // Aktüel pozisyon (mm - görseli etkilemez)
                double actualPos = GlobalData.GetSliderActualPosition();
                if (sliderActualPosText != null) sliderActualPosText.Text = $"{actualPos:F1} mm";

                // Hedef istasyon gösterimi
                if (sliderHedefStationText != null)
                {
                    var hedefVar = GlobalData.GeneralOutputVars.FirstOrDefault(v => v.Name == "KL100_HEDEF_ISTASYON");
                    string hedefVal = hedefVar?.Value ?? "0";
                    if (int.TryParse(hedefVal, out int hedef) && hedef >= 1 && hedef <= 3)
                        sliderHedefStationText.Text = $"İSTASYON {hedef}";
                    else
                        sliderHedefStationText.Text = "---";
                }
            }

            RefreshSlider();

            foreach (var robot in robots)
            {
                robot.PropertyChanged += (s, e) =>
                {
                    this.DispatcherQueue.TryEnqueue(RefreshSlider);
                };
            }

            sliderCanvas.SizeChanged += (s, e) =>
            {
                if (sliderCanvas.ActualWidth > 0) RefreshSlider();
            };
        }

        /// <summary>
        /// İstasyon numarasını 4 sütunlu Grid'deki sütun indeksine çevirir
        /// Grid: [BAKIM(0)] [İST1(1)] [İST2(2)] [İST3(3)]
        /// </summary>
        private static int StationToColumnIndex(int station)
        {
            return station switch
            {
                4 => 0,  // Bakım İstasyonu
                1 => 1,  // İstasyon 1
                2 => 2,  // İstasyon 2
                3 => 3,  // İstasyon 3
                _ => 0
            };
        }

        private void InitializeRobotSignalPanels()
        {
            var robots = KukaRobotManager.Instance.Robots;

            // ═══ ROBOT SEÇİM COMBOBOX'I ═══
            var robotCombo = this.FindName("SliderRobotCombo") as ComboBox;
            var signalCombo = this.FindName("SliderSignalCombo") as ComboBox;
            var actualPosCombo = this.FindName("SliderActualPosCombo") as ComboBox;
            var liveValueText = this.FindName("SliderLiveValue") as TextBlock;
            var actualPosLiveText = this.FindName("SliderActualPosLive") as TextBlock;

            // Robot isimlerini doldur
            if (robotCombo != null)
            {
                var robotNames = new List<string>();
                for (int i = 0; i < robots.Count; i++)
                    robotNames.Add(robots[i].Name ?? $"Robot {i + 1}");
                robotCombo.ItemsSource = robotNames;

                if (GlobalData.SliderSourceRobotIndex < robotNames.Count)
                    robotCombo.SelectedIndex = GlobalData.SliderSourceRobotIndex;

                robotCombo.SelectionChanged += (s, e) =>
                {
                    if (robotCombo.SelectedIndex >= 0)
                    {
                        GlobalData.SliderSourceRobotIndex = robotCombo.SelectedIndex;
                        UpdateSliderSignalCombo(signalCombo, robotCombo.SelectedIndex, GlobalData.SliderSourceSignalName);
                        UpdateSliderSignalCombo(actualPosCombo, robotCombo.SelectedIndex, GlobalData.SliderActualPosSignalName);
                    }
                };
            }

            // İstasyon sinyali combo
            if (signalCombo != null)
            {
                UpdateSliderSignalCombo(signalCombo, GlobalData.SliderSourceRobotIndex, GlobalData.SliderSourceSignalName);
                signalCombo.SelectionChanged += (s, e) =>
                {
                    if (signalCombo.SelectedItem is string sig)
                        GlobalData.SliderSourceSignalName = sig;
                };
            }

            // Aktüel pozisyon combo
            if (actualPosCombo != null)
            {
                UpdateSliderSignalCombo(actualPosCombo, GlobalData.SliderSourceRobotIndex, GlobalData.SliderActualPosSignalName);
                actualPosCombo.SelectionChanged += (s, e) =>
                {
                    if (actualPosCombo.SelectedItem is string sig)
                        GlobalData.SliderActualPosSignalName = sig;
                };
            }

            // ═══ ROBOT BAĞLANTI DURUMLARI ═══
            var robot1ConnStatus = this.FindName("Robot1ConnStatus") as Border;
            var robot1ConnText = this.FindName("Robot1ConnText") as TextBlock;
            var robot2ConnStatus = this.FindName("Robot2ConnStatus") as Border;
            var robot2ConnText = this.FindName("Robot2ConnText") as TextBlock;

            void UpdateConnDisplay(KukaRobotInstance robot, Border border, TextBlock text, Windows.UI.Color connectedColor)
            {
                if (border != null)
                    border.Background = new SolidColorBrush(robot.IsConnected ? connectedColor : Windows.UI.Color.FromArgb(255, 100, 30, 22));
                if (text != null)
                    text.Text = robot.IsConnected ? "BAĞLI" : "BAĞLI DEĞİL";
            }

            if (robots.Count > 0)
            {
                var r1 = robots[0];
                UpdateConnDisplay(r1, robot1ConnStatus, robot1ConnText, Windows.UI.Color.FromArgb(255, 46, 125, 50));
                r1.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(KukaRobotInstance.IsConnected))
                        this.DispatcherQueue.TryEnqueue(() => UpdateConnDisplay(r1, robot1ConnStatus, robot1ConnText, Windows.UI.Color.FromArgb(255, 46, 125, 50)));
                };
            }
            if (robots.Count > 1)
            {
                var r2 = robots[1];
                UpdateConnDisplay(r2, robot2ConnStatus, robot2ConnText, Windows.UI.Color.FromArgb(255, 0, 90, 158));
                r2.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(KukaRobotInstance.IsConnected))
                        this.DispatcherQueue.TryEnqueue(() => UpdateConnDisplay(r2, robot2ConnStatus, robot2ConnText, Windows.UI.Color.FromArgb(255, 0, 90, 158)));
                };
            }

            // ═══ CANLI DEĞER GÜNCELLEME ═══
            void UpdateLiveValues()
            {
                if (liveValueText != null)
                {
                    double stationVal = GlobalData.GetSliderPositionValue();
                    int station = (int)Math.Round(stationVal);
                    liveValueText.Text = station switch
                    {
                        4 => "BAKIM",
                        >= 1 and <= 3 => $"İSTASYON {station}",
                        _ => $"{stationVal:F1}"
                    };
                }
                if (actualPosLiveText != null)
                {
                    double actualPos = GlobalData.GetSliderActualPosition();
                    actualPosLiveText.Text = $"{actualPos:F1} mm";
                }
            }

            foreach (var robot in robots)
            {
                robot.PropertyChanged += (s, e) =>
                {
                    this.DispatcherQueue.TryEnqueue(UpdateLiveValues);
                };
            }
            UpdateLiveValues();
        }

        private void UpdateSliderSignalCombo(ComboBox signalCombo, int robotIndex, string savedValue)
        {
            if (signalCombo == null) return;
            var signals = GlobalData.GetAvailableRobotSignals(robotIndex);
            signalCombo.ItemsSource = signals;
            if (!string.IsNullOrEmpty(savedValue) && signals.Contains(savedValue))
                signalCombo.SelectedItem = savedValue;
            else if (signals.Contains("E1"))
                signalCombo.SelectedItem = "E1";
        }

        private void OnRobotSignalMappingChanged(int robotIndex)
        {
            // Eski uyumluluk - artık kullanılmıyor ama referans kaldıysa hata vermesin
            GlobalData.SaveRobotSliderMappings();
        }

        /// <summary>
        /// Slider pozisyonundan istasyon numarası hesaplar
        /// Station1: 0-750mm, Station2: 750-2250mm, Station3: 2250-3000mm
        /// </summary>
        private int GetStationNumberFromPosition(double positionMm)
        {
            if (positionMm < 750) return 1;
            if (positionMm < 2250) return 2;
            if (positionMm <= 3000) return 3;
            return 0; // Belirsiz
        }

        /// <summary>
        /// Robot platformunu slider üzerinde belirtilen sütun merkezine hizalar.
        /// 4 eşit sütunlu Grid ile aynı düzeni kullanır (Bakım, İst1, İst2, İst3).
        /// </summary>
        private void UpdatePlatformToStation(int columnIndex)
        {
            var sliderCanvas = this.FindName("SliderCanvas") as Canvas;
            var robotPlatform = this.FindName("RobotPlatform") as Grid;

            if (sliderCanvas == null || robotPlatform == null || sliderCanvas.ActualWidth <= 0) return;

            const int totalColumns = 4;
            double platformWidth = 140;
            double canvasWidth = sliderCanvas.ActualWidth;

            // Sütun merkezi: (columnIndex + 0.5) / totalColumns * canvasWidth
            double columnCenter = (columnIndex + 0.5) / totalColumns * canvasWidth;

            // Platform sol kenarı = sütun merkezi - platform genişliğinin yarısı
            double left = columnCenter - (platformWidth / 2.0);
            left = Math.Clamp(left, 0, Math.Max(0, canvasWidth - platformWidth));

            Canvas.SetLeft(robotPlatform, left);
        }

        // Eski metodu koru (geriye uyumluluk için)
        public void UpdateRobotSliderPosition(int robotIndex, double positionPercent)
        {
            // Artık istasyon bazlı pozisyonlama kullanılıyor
            int col = (int)Math.Round(positionPercent / 33.3);
            UpdatePlatformToStation(Math.Clamp(col, 0, 3));
        }

        private void KnownRfids_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null) foreach (App4.Utilities.RfidDef item in e.NewItems) item.PropertyChanged += Rfid_PropertyChanged;
            if (e.OldItems != null) foreach (App4.Utilities.RfidDef item in e.OldItems) item.PropertyChanged -= Rfid_PropertyChanged;
        }

        private void Rfid_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(App4.Utilities.RfidDef.ModelFileName))
            {
                if (sender is App4.Utilities.RfidDef rfid)
                {
                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        foreach (var s in Stations)
                        {
                            if (s is ExtendedStationViewModel ext)
                            {
                                // Specific modda TargetRfid, Mixed modda CurrentRfid kontrol et
                                bool isMatch = ext.RfidOpMode.Equals(App4.Utilities.RfidOperationMode.Specific)
                                    ? ext.TargetRfid == rfid.Id
                                    : ext.CurrentRfid == rfid.Id;

                                if (isMatch) UpdateStationModel(ext);
                            }
                        }
                    });
                }
            }
        }




        private async Task InitializeStationViewers()
        {
            try
            {
                string userDataFolder = Path.Combine(Path.GetTempPath(), "Simbiosis_WebView2_Cache");
                
                // Allow cross-origin requests (localui -> localmodels)
                var options = new CoreWebView2EnvironmentOptions { AdditionalBrowserArguments = "--disable-web-security --disable-features=IsolateOrigins,site-per-process" };
                var env = await CoreWebView2Environment.CreateWithOptionsAsync(null, userDataFolder, options);

                // 1. HTML Klasörü (Temp - Yazılabilir)
                string htmlFolder = Path.Combine(Path.GetTempPath(), "Simbiosis_HTML");
                if (!Directory.Exists(htmlFolder)) Directory.CreateDirectory(htmlFolder);

                // 2. Modeller Klasörü (App - Okunabilir)
                string modelsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utilities", "Models");
                if (!Directory.Exists(modelsFolder)) Directory.CreateDirectory(modelsFolder);

                // HTML Dosyalarını Hazırla (Assets'ten kopyala, yoksa oluştur)
                string assetsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
                
                async Task PrepareHtml(string fileName, string title)
                {
                    string destPath = Path.Combine(htmlFolder, fileName);
                    string sourcePath = Path.Combine(assetsFolder, fileName);

                    // Eğer Assets klasöründe dosya varsa onu kopyala (Geliştirme sırasında yapılan değişiklikleri alır)
                    if (File.Exists(sourcePath))
                    {
                        try 
                        {
                            File.Copy(sourcePath, destPath, true);
                            System.Diagnostics.Debug.WriteLine($">>> Copied HTML from Assets: {fileName}");
                            return; 
                        }
                        catch (Exception ex) 
                        {
                            System.Diagnostics.Debug.WriteLine($"Copy Error ({fileName}): {ex.Message}");
                        }
                    }
                    
                    // Yoksa veya hata olursa string'den oluştur
                    await CreateViewerHtml(htmlFolder, fileName, title);
                }

                await PrepareHtml("1_StationProductViewer.html", "Station 1 Viewer");
                await PrepareHtml("2_StationProductViewer.html", "Station 2 Viewer");
                await PrepareHtml("3_StationProductViewer.html", "Station 3 Viewer");

                await InitSingleViewer(Viewer_Station1, "1_StationProductViewer.html", env, htmlFolder, modelsFolder, Stations[0]);
                await InitSingleViewer(Viewer_Station2, "2_StationProductViewer.html", env, htmlFolder, modelsFolder, Stations[1]);
                await InitSingleViewer(Viewer_Station3, "3_StationProductViewer.html", env, htmlFolder, modelsFolder, Stations[2]);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Viewer Init Error: {ex.Message}");
            }
        }

        private async Task InitSingleViewer(WebView2 wv, string htmlName, CoreWebView2Environment env, string htmlPath, string modelsPath, StationViewModel station)
        {
            if (wv == null) return;

            try
            {
                await wv.EnsureCoreWebView2Async(env);

                // Clear cache by deleting content and reloading
                await wv.CoreWebView2.ExecuteScriptAsync("localStorage.clear(); sessionStorage.clear();");

                // Map virtual hosts for file access
                try
                {
                    wv.CoreWebView2.SetVirtualHostNameToFolderMapping("localui", htmlPath, CoreWebView2HostResourceAccessKind.Allow);
                    wv.CoreWebView2.SetVirtualHostNameToFolderMapping("localmodels", modelsPath, CoreWebView2HostResourceAccessKind.Allow);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Virtual host mapping error: {ex.Message}");
                }

                wv.CoreWebView2.NavigationCompleted += (s, e) =>
                {
                    if (station is ExtendedStationViewModel ext) UpdateStationModel(ext);
                };

                // Load HTML from file using file:// protocol with cache-busting query
                string htmlFilePath = Path.Combine(htmlPath, htmlName);
                if (File.Exists(htmlFilePath))
                {
                    // Use file:// for local files with timestamp to prevent caching
                    string timestamp = DateTime.Now.Ticks.ToString();
                    wv.Source = new Uri($"file:///{htmlFilePath.Replace("\\", "/")}?t={timestamp}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"HTML file not found: {htmlFilePath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InitSingleViewer Error: {ex.Message}");
            }
        }

        private async Task<bool> CreateViewerHtml(string folder, string fileName, string title)
        {
            try
            {
                // Updated HTML content with Perspective Camera, Controls, and Fixes
                string htmlContent = $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{{title}}</title>
    <style>
        body {{
            margin: 0;
            overflow: hidden;
            background-color: transparent;
            color: white;
            font-family: sans-serif;
        }}

        canvas {{
            display: block;
            width: 100vw;
            height: 100vh;
            touch-action: none; /* Prevent scroll */
        }}

        #info {{
            position: absolute;
            top: 10px;
            left: 10px;
            z-index: 100;
            pointer-events: none;
            background: rgba(0, 0, 0, 0.8);
            padding: 15px;
            border-radius: 8px;
            border-left: 4px solid #00A4EF;
            font-size: 12px;
            display: none;
        }}

        #loading {{
            position: absolute;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            font-size: 20px;
            color: #3498db;
            background: rgba(0, 0, 0, 0.8);
            padding: 30px;
            border-radius: 8px;
            display: none;
            z-index: 200;
        }}

        #error {{
            position: absolute;
            bottom: 20px;
            left: 20px;
            background: rgba(220, 53, 69, 0.9);
            color: white;
            padding: 12px 15px;
            border-radius: 5px;
            font-size: 12px;
            max-width: 400px;
            display: none;
            border-left: 4px solid #dc3545;
            z-index: 200;
            word-wrap: break-word;
        }}

        #debug {{
            position: absolute;
            top: 10px;
            right: 10px;
            background: rgba(0, 0, 0, 0.7);
            color: #888;
            padding: 8px 12px;
            border-radius: 4px;
            font-size: 10px;
            z-index: 100;
            max-width: 300px;
            display: none;
        }}

        #controls-panel {{
            position: absolute;
            top: 50px;
            right: 10px;
            z-index: 9999;
            background: rgba(0,0,0,0.85);
            padding: 10px;
            border-radius: 8px;
            text-align: right;
            border: 2px solid #00A4EF;
            min-width: 150px;
        }}
        #controls-panel button {{
            background: #444;
            color: white;
            border: 1px solid #666;
            padding: 6px 10px;
            margin: 2px;
            cursor: pointer;
            border-radius: 4px;
            font-size: 12px;
            font-weight: bold;
        }}
        #controls-panel button:hover {{ background: #00A4EF; }}
        #rot-status {{ font-size: 10px; color: yellow; margin-top: 5px; text-align: center; }}
    </style>
    <script src='https://cdnjs.cloudflare.com/ajax/libs/three.js/r128/three.min.js'></script>
    <script src='https://cdn.jsdelivr.net/npm/three@0.128.0/examples/js/controls/OrbitControls.js'></script>
    <script src='https://cdn.jsdelivr.net/npm/three@0.128.0/examples/js/loaders/GLTFLoader.js'></script>
</head>
<body>
    <div id='info'>{{title}}</div>
    <div id='loading'>Yükleniyor...</div>
    <div id='error'></div>
    <div id='debug'></div>

    <div id='controls-panel' style='display:none !important;'>
        <div style='font-size:10px;color:#aaa;margin-bottom:5px;border-bottom:1px solid #555;padding-bottom:2px'>GÖRÜNÜM KONTROL</div>
        <div>
            <button onclick='setView(""iso"")'>ISO</button>
            <button onclick='setView(""front"")'>ÖN</button>
            <button onclick='setView(""top"")'>ÜST</button>
            <button onclick='setView(""side"")'>YAN</button>
            <button onclick='fitCamera()' style='background:#0078D7'>ODAKLA</button>
        </div>
        <div style='font-size:10px;color:#aaa;margin-top:5px;margin-bottom:2px'>YÖN DÜZELTME</div>
        <div>
            <button onclick='rotateModel(""x"")'>ROT X</button>
            <button onclick='rotateModel(""y"")'>ROT Y</button>
            <button onclick='rotateModel(""z"")'>ROT Z</button>
        </div>
        <div id='rot-status'>ROT: 0,0,0</div>
        <button onclick='window.location.reload()' style='width:100%;margin-top:5px;background:#800;'>SAYFA YENİLE</button>
    </div>

    <script>
        let scene, camera, renderer, controls, currentModel;
        const errorEl = document.getElementById('error');
        const loadingEl = document.getElementById('loading');
        const debugEl = document.getElementById('debug');
        let THREE_READY = false;

        function log(msg) {{
            console.log(`[{{title}}] ${{msg}}`);
            debugEl.textContent = msg;
            debugEl.style.display = 'block';
        }}

        function updateError(message) {{
            errorEl.textContent = `! ${{message}}`;
            errorEl.style.display = 'block';
            console.error(`[{{title}} ERROR] ${{message}}`);
        }}

        function clearError() {{ 
            errorEl.style.display = 'none'; 
        }}

        function showLoading(show = true) {{ 
            loadingEl.style.display = show ? 'block' : 'none'; 
        }}

        function waitForTHREE(callback, attempt = 0) {{
            if (typeof THREE !== 'undefined' && 
                typeof THREE.OrbitControls !== 'undefined' && 
                typeof THREE.GLTFLoader !== 'undefined') {{
                THREE_READY = true;
                log('THREE.js ready');
                callback();
            }} else if (attempt < 50) {{
                setTimeout(() => waitForTHREE(callback, attempt + 1), 100);
            }} else {{
                updateError('THREE.js libraries failed to load. Check CDN access.');
            }}
        }}

        function init() {{
            try {{
                scene = new THREE.Scene();
                scene.background = new THREE.Color(0x000000); // Black background
                
                // Perspective Camera Setup
                const width = window.innerWidth;
                const height = window.innerHeight;
                const distance = 200; 
                const fov = 45;
                
                camera = new THREE.PerspectiveCamera(fov, width / height, 0.1, 10000);
                // Lower Y for a better side/front view (not top-down)
                camera.position.set(distance, distance * 0.6, distance);
                camera.lookAt(0, 0, 0);

                renderer = new THREE.WebGLRenderer({{ 
                    antialias: true, 
                    alpha: true,
                    powerPreference: 'high-performance'
                }});
                renderer.setSize(window.innerWidth, window.innerHeight);
                renderer.setPixelRatio(window.devicePixelRatio);
                renderer.outputColorSpace = THREE.SRGBColorSpace;
                document.body.appendChild(renderer.domElement);

                // OrbitControls setup
                if (typeof THREE.OrbitControls !== 'undefined') {{
                    controls = new THREE.OrbitControls(camera, renderer.domElement);
                    controls.enableDamping = true;
                    controls.dampingFactor = 0.1;
                    controls.enableRotate = true;
                    controls.rotateSpeed = 1.0;
                    controls.enableZoom = true;
                    controls.enablePan = true;
                    
                    controls.mouseButtons = {{
                        LEFT: THREE.MOUSE.ROTATE,
                        MIDDLE: THREE.MOUSE.DOLLY,
                        RIGHT: THREE.MOUSE.PAN
                    }};
                    
                    controls.target.set(0, 0, 0);
                    controls.update();
                }}

                // Lighting
                const ambientLight = new THREE.AmbientLight(0xffffff, 0.7);
                scene.add(ambientLight);

                const directionalLight = new THREE.DirectionalLight(0xffffff, 0.8);
                directionalLight.position.set(distance, distance * 1.5, distance);
                directionalLight.castShadow = true;
                scene.add(directionalLight);

                const backLight = new THREE.DirectionalLight(0xffffff, 0.3);
                backLight.position.set(-distance, -distance * 0.5, -distance);
                scene.add(backLight);

                // Ground plane
                const groundGeometry = new THREE.PlaneGeometry(500, 500);
                const groundMaterial = new THREE.MeshStandardMaterial({{ 
                    color: 0x333333,
                    emissive: 0x1a1a1a
                }});
                const groundPlane = new THREE.Mesh(groundGeometry, groundMaterial);
                groundPlane.rotation.x = -Math.PI / 2; // XZ Plane
                groundPlane.position.y = -0.1;
                groundPlane.receiveShadow = true;
                scene.add(groundPlane);

                window.addEventListener('resize', onWindowResize);

                log('Scene Initialized (Perspective)');
                animate();
            }} catch (e) {{
                updateError('Init Error: ' + e.message);
            }}
        }}

        function onWindowResize() {{
            camera.aspect = window.innerWidth / window.innerHeight;
            camera.updateProjectionMatrix();
            renderer.setSize(window.innerWidth, window.innerHeight);
        }}

        function loadModel(urlPath) {{
            if (!urlPath || urlPath.trim() === '') {{
                clearError();
                log('No model specified');
                return;
            }}

            log(`Loading: ${{urlPath}}`);
            showLoading(true);
            clearError();

            if (currentModel) {{ 
                scene.remove(currentModel); 
                currentModel = null; 
            }}

            let url = urlPath;
            if (!url.startsWith('file://') && !url.startsWith('http://') && !url.startsWith('https://')) {{
                url = 'file:///' + urlPath.split('/').map(p => encodeURIComponent(p)).join('/');
            }}

            const loader = new THREE.GLTFLoader();
            
            loader.load(
                url, 
                (gltf) => {{
                    try {{
                        const model = gltf.scene;
                        currentModel = model;
                        scene.add(model);
                        
                        model.traverse((child) => {{
                            if (child.isMesh) {{
                                child.castShadow = true;
                                child.receiveShadow = true;
                            }}
                        }});
                        
                        // Default Rotation: -90 X (Stand Up)
                        model.position.set(0, 0, 0);
                        model.rotation.set(-Math.PI / 2, 0, 0); 
                        model.updateMatrixWorld(true);
                        
                        const box = new THREE.Box3().setFromObject(model);
                        const center = box.getCenter(new THREE.Vector3());
                        
                        // Sit on ground
                        model.position.y = -box.min.y;
                        model.position.x = -center.x; // Center Horizontal
                        model.position.z = -center.z; // Center Depth (relative to rotated X)
                        
                        fitCamera(); // Auto-fit camera
                        
                        showLoading(false);
                        log('Model loaded (Rot X -90)');
                        updateRotStatus();
                    }} catch (err) {{
                        updateError('Model processing error: ' + err.message);
                        showLoading(false);
                    }}
                }}, 
                (progress) => {{
                    const percentComplete = Math.round((progress.loaded / progress.total) * 100);
                    log(`Loading... ${{percentComplete}}%`);
                }}, 
                (err) => {{
                    updateError('Model load failed: ' + (err.message || JSON.stringify(err)));
                    showLoading(false);
                }}
            );
        }}

        function setView(view) {{
            const dist = 300; 
            if (!camera || !controls) return;
            
            controls.reset();
            
            switch(view) {{
                case 'front': camera.position.set(0, 0, dist); break;
                case 'back': camera.position.set(0, 0, -dist); break;
                case 'right': camera.position.set(dist, 0, 0); break;
                case 'left': camera.position.set(-dist, 0, 0); break;
                case 'top': camera.position.set(0, dist, 0); break; 
                case 'side': camera.position.set(dist, 0, dist); break; 
                case 'iso': camera.position.set(200, 200, 200); break;
            }}
            camera.lookAt(0, 0, 0);
            controls.update();
        }}

        function rotateModel(axis) {{
            if (!currentModel) return;
            currentModel.rotation[axis] += Math.PI / 2;
            currentModel.updateMatrixWorld(true);
            
            const box = new THREE.Box3().setFromObject(currentModel);
            currentModel.position.y -= box.min.y;
            currentModel.position.x = -box.getCenter(new THREE.Vector3()).x;
            currentModel.position.z = -box.getCenter(new THREE.Vector3()).z;
            
            updateRotStatus();
        }}
        
        function updateRotStatus() {{
            if(!currentModel) return;
            const r = currentModel.rotation;
            const toDeg = rad => Math.round(rad * 180 / Math.PI);
            document.getElementById('rot-status').textContent = `ROT: ${{toDeg(r.x)}}, ${{toDeg(r.y)}}, ${{toDeg(r.z)}}`;
        }}
        
        function fitCamera() {{
            if (!currentModel || !camera || !controls) return;
            
            const box = new THREE.Box3().setFromObject(currentModel);
            const size = box.getSize(new THREE.Vector3());
            const center = box.getCenter(new THREE.Vector3());

            const maxDim = Math.max(size.x, size.y, size.z);
            const fov = camera.fov * (Math.PI / 180);
            let cameraDist = Math.abs(maxDim / 2 / Math.tan(fov / 2));
            cameraDist *= 2.0;

            const direction = camera.position.clone().sub(controls.target).normalize();
            const newPos = direction.multiplyScalar(cameraDist).add(center);

            camera.position.copy(newPos);
            controls.target.copy(center);
            controls.update();
            log(`Camera fitted. Dist: ${{Math.round(cameraDist)}}`);
        }}

        function animate() {{
            requestAnimationFrame(animate);
            if (controls) {{
                controls.update();
            }}
            renderer.render(scene, camera);
        }}

        waitForTHREE(() => {{
            init();
            window.loadModel = loadModel;
            log('Ready');
        }});
    </script>
</body>
</html>";
                string path = Path.Combine(folder, fileName);
                await File.WriteAllTextAsync(path, htmlContent);
                System.Diagnostics.Debug.WriteLine($">>> Auto HTML created: {path}");
                return true;
            }
            catch (Exception ex) 
            {
                System.Diagnostics.Debug.WriteLine($"HTML Create Error ({fileName}): {ex.Message}");
                return false;
            }
        }

        // Duplicate InitSingleViewer removed

        private void UpdateStationModel(ExtendedStationViewModel station)
        {
            // UI thread'de çalıştığından emin ol (ExecuteScriptAsync UI thread gerektirir)
            if (!this.DispatcherQueue.HasThreadAccess)
            {
                this.DispatcherQueue.TryEnqueue(() => UpdateStationModel(station));
                return;
            }

            _ = UpdateStationModelAsync(station);
        }

        private async Task UpdateStationModelAsync(ExtendedStationViewModel station)
        {
            try
            {
                int index = Stations.IndexOf(station);
                WebView2 targetWebView = index switch
                {
                    0 => Viewer_Station1,
                    1 => Viewer_Station2,
                    2 => Viewer_Station3,
                    _ => null
                };

                if (targetWebView?.CoreWebView2 == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Station {index + 1}] WebView not ready, skipping model update");
                    return;
                }

                // Determine which RFID to use based on operation mode
                string rfidToLookup;
                if (station.RfidOpMode.Equals(App4.Utilities.RfidOperationMode.Mixed))
                {
                    // Mixed: Okunan (CurrentRfid) RFID'nin modeli
                    rfidToLookup = station.CurrentRfid;
                }
                else
                {
                    // Specific: Beklenen (TargetRfid) RFID'nin modeli
                    rfidToLookup = station.TargetRfid;
                }

                System.Diagnostics.Debug.WriteLine($"[Station {index + 1}] Model güncelleniyor. Mod: {station.RfidOpMode}, RFID: {rfidToLookup ?? "(boş)"}");

                // RFID boşsa modeli temizle
                if (string.IsNullOrEmpty(rfidToLookup))
                {
                    System.Diagnostics.Debug.WriteLine($"[Station {index + 1}] RFID boş - model temizleniyor");
                    await targetWebView.ExecuteScriptAsync("if(currentModel) { scene.remove(currentModel); currentModel = null; } document.getElementById('debug').textContent = 'RFID bekleniyor...';");
                    return;
                }

                var rfidDef = GlobalData.KnownRfids.FirstOrDefault(r => r.Id == rfidToLookup);

                if (rfidDef == null || string.IsNullOrEmpty(rfidDef.ModelFileName))
                {
                    System.Diagnostics.Debug.WriteLine($"[Station {index + 1}] RFID '{rfidToLookup}' için model tanımlı değil");
                    await targetWebView.ExecuteScriptAsync($"if(currentModel) {{ scene.remove(currentModel); currentModel = null; }} document.getElementById('debug').textContent = 'RFID: {rfidToLookup} - Model tanımsız';");
                    return;
                }

                string modelPath = rfidDef.ModelFileName.Replace("\\", "/");
                string modelsRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utilities", "Models");
                string fullPath = Path.Combine(modelsRoot, modelPath);

                // Try exact path first, then search
                if (!File.Exists(fullPath))
                {
                    try
                    {
                        var foundFile = Directory.GetFiles(modelsRoot, Path.GetFileName(modelPath), SearchOption.AllDirectories).FirstOrDefault();
                        if (foundFile != null)
                        {
                            fullPath = foundFile;
                        }
                    }
                    catch { /* Models klasörü yoksa hata vermesin */ }
                }

                if (File.Exists(fullPath))
                {
                    string fileUri = new Uri(fullPath).AbsoluteUri;
                    string jsCode = $"if(window.loadModel) {{ window.loadModel('{fileUri.Replace("'", "\\'")}'); }} else {{ console.error('loadModel not ready'); }}";

                    System.Diagnostics.Debug.WriteLine($"[Station {index + 1}] Yükleniyor: {Path.GetFileName(fullPath)}");
                    await targetWebView.ExecuteScriptAsync(jsCode);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[Station {index + 1}] Model dosyası bulunamadı: {fullPath}");
                    await targetWebView.ExecuteScriptAsync($"document.getElementById('debug').textContent = 'Dosya bulunamadı: {Path.GetFileName(modelPath)}';");
                }
            }
            catch (Exception ex) 
            {
                System.Diagnostics.Debug.WriteLine($"UpdateStationModel Error: {ex.Message}");
            }
        }


        // --- İSTASYON DURUMU DEĞİŞİRSE KAYDET ---
        private void Station_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is ExtendedStationViewModel station)
            {
                // Visual / State updates that need saving
                if (e.PropertyName == nameof(StationViewModel.Mode) ||
                    e.PropertyName == nameof(ExtendedStationViewModel.RfidOpMode) ||
                    e.PropertyName == nameof(ExtendedStationViewModel.TargetRfid))
                {
                    GlobalData.SaveStationStates(); // <-- GLOBAL KAYDET
                }

                // Model Update Triggering:
                // TargetRfid değişirse (Specific mod), CurrentRfid değişirse (Mixed mod), veya OpMode değişirse
                if (e.PropertyName == nameof(ExtendedStationViewModel.TargetRfid) || 
                    e.PropertyName == nameof(StationViewModel.CurrentRfid) ||
                    e.PropertyName == nameof(ExtendedStationViewModel.RfidOpMode))
                {
                    this.DispatcherQueue.TryEnqueue(() => UpdateStationModel(station));
                }

                // PLC'ye Yazma İşlemleri (Eski kodunun aynısı)
                int index = Stations.IndexOf(station);
                if (index < 0) return;
                ObservableCollection<PlcVariable> outputs = index switch { 0 => Station1Outputs, 1 => Station2Outputs, 2 => Station3Outputs, _ => null };

                if (outputs != null)
                {
                    if (e.PropertyName == nameof(StationViewModel.CurrentRfid) || e.PropertyName == nameof(ExtendedStationViewModel.TargetRfid))
                    {
                        string matchVal = station.IsRfidMatch ? "1" : "0";
                        UpdatePlcVar(outputs, $"ST{index + 1}_ID_MATCHED", matchVal);
                        UpdatePlcVar(outputs, $"ST{index + 1}_CONVEYOR_PERM", matchVal);
                        
                        if (e.PropertyName == nameof(ExtendedStationViewModel.TargetRfid)) 
                        {
                            // Target RFID is STRING type, send full value
                            string rfidVal = station.TargetRfid;
                            System.Diagnostics.Debug.WriteLine($"[PLC] Sending RFID Target to ST{index + 1}: {rfidVal}");
                            UpdatePlcVar(outputs, $"ST{index + 1}_RFID_TARGET", rfidVal);
                        }
                    }
                    else if (e.PropertyName == nameof(ExtendedStationViewModel.RfidOpMode)) UpdatePlcVar(outputs, $"ST{index + 1}_RFID_MODE", ((int)station.RfidOpMode).ToString());
                    else if (e.PropertyName == nameof(StationViewModel.Mode)) UpdatePlcVar(outputs, $"ST{index + 1}_MODE_CMD", (station.Mode == StationMode.Auto) ? "1" : "0");
                }
            }
        }

        // --- DEĞİŞKEN DEĞERİ DEĞİŞİRSE KAYDET ---
        private void LocalVariable_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is PlcVariable localVar && e.PropertyName == nameof(PlcVariable.CurrentValue))
            {
                GlobalData.SavePlcVariableTagsToFile(); // <-- GLOBAL KAYDET

                UpdateLineStatusVisuals();

                // Slider vb. görsel güncellemeler
                if (localVar.Name == "SLIDER_POS_ACT") UpdateSliderPosition(localVar.CurrentValue?.ToString());
                else UpdateStationStatus(localVar.Name, localVar.CurrentValue?.ToString());

                // OTO/MANUEL switch senkronizasyonu (PLC'den gelen değeri UI'a yansıt)
                if (localVar.Name == "LINE_AUTO_MANUAL_CMD")
                {
                    bool isOn = localVar.Value?.ToUpper() == "TRUE" || localVar.Value == "1";
                    if (LineAutoManualSwitch != null && LineAutoManualSwitch.IsOn != isOn) LineAutoManualSwitch.IsOn = isOn;
                    if (KontrolLineAutoManualSwitch != null && KontrolLineAutoManualSwitch.IsOn != isOn) KontrolLineAutoManualSwitch.IsOn = isOn;
                }
            }
        }

        // --- DİĞER FONKSİYONLAR (AYNEN KALDI) ---
        private void InitializeAvailablePlcTags()
        {
            try
            {
                AvailableInputPlcTags.Clear(); AvailableOutputPlcTags.Clear();
                foreach (var v in PlcService.Instance.InputVariables) AvailableInputPlcTags.Add(v.Name);
                foreach (var v in PlcService.Instance.OutputVariables) AvailableOutputPlcTags.Add(v.Name);
            }
            catch { }
        }

        private void ConnectToPlcVariable(PlcVariable localVar)
        {
            if (string.IsNullOrEmpty(localVar.PlcTag)) return;
            var sourceRealVar = PlcService.Instance.InputVariables.FirstOrDefault(v => v.Name == localVar.PlcTag) ?? PlcService.Instance.OutputVariables.FirstOrDefault(v => v.Name == localVar.PlcTag);
            
            if (sourceRealVar != null)
            {
                // Okuma
                localVar.Value = sourceRealVar.CurrentValue?.ToString();
                sourceRealVar.PropertyChanged += (s, e) => { if (e.PropertyName == "CurrentValue") this.DispatcherQueue.TryEnqueue(() => localVar.Value = sourceRealVar.CurrentValue?.ToString()); };
                // Yazma
                localVar.PropertyChanged += async (s, e) => {
                    if ((e.PropertyName == "CurrentValue" || e.PropertyName == "Value") && sourceRealVar.CurrentValue?.ToString() != localVar.CurrentValue?.ToString())
                    { sourceRealVar.CurrentValue = localVar.CurrentValue; await PlcService.Instance.WriteAsync(sourceRealVar, localVar.CurrentValue); }
                };
            }
            else
            {
                // Fallback: Doğrudan Adres Yazma (Direct Write)
                // Eğer PlcTags serviste kayıtlı bir isim değilse (örn: DB10.W20.0 gibi ham adres ise)
                localVar.PropertyChanged += async (s, e) => {
                    if (e.PropertyName == "CurrentValue" || e.PropertyName == "Value")
                    {
                        // Geçici değişken ile yazma (Name=Address kuralına göre)
                        var tempVar = new PlcVariable { Name = localVar.PlcTag, Type = localVar.Type ?? "WORD" };
                        await PlcService.Instance.WriteAsync(tempVar, localVar.CurrentValue);
                    }
                };
            }
        }

        private void PlcTagComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb && cb.DataContext is PlcVariable v) { if (v.PlcTag != cb.SelectedItem as string) { v.PlcTag = cb.SelectedItem as string; GlobalData.SavePlcVariableTagsToFile(); ConnectToPlcVariable(v); } }
        }

        // ═══ HAT OTO/MANUEL SWITCH ═══
        private void LineAutoManualSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch ts)
            {
                var switchVar = GeneralOutputVars.FirstOrDefault(v => v.Name == "LINE_AUTO_MANUAL_CMD");
                if (switchVar != null)
                {
                    switchVar.Value = ts.IsOn ? "True" : "False";
                    switchVar.CurrentValue = ts.IsOn;
                }

                // İki switch'i senkronize et
                if (ts == LineAutoManualSwitch && KontrolLineAutoManualSwitch != null && KontrolLineAutoManualSwitch.IsOn != ts.IsOn)
                    KontrolLineAutoManualSwitch.IsOn = ts.IsOn;
                else if (ts == KontrolLineAutoManualSwitch && LineAutoManualSwitch != null && LineAutoManualSwitch.IsOn != ts.IsOn)
                    LineAutoManualSwitch.IsOn = ts.IsOn;
            }
        }

        private void RfidModel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb && cb.DataContext is App4.Utilities.RfidDef rfid) 
            { 
                if (cb.SelectedItem is string selectedModel && rfid.ModelFileName != selectedModel) 
                { 
                    rfid.ModelFileName = selectedModel; 
                } 
            }
        }

        // --- GÜVENLİ DEĞER KONTROLÜ (BOOL, INT, STRING HEPSİNİ KABUL EDER) ---
        // GENİŞLETİLDİ: GeneralInputVars + PlcService değişkenlerinde arar
        private bool IsConditionMet(string varName, bool expectedTrue)
        {
            // 1. Önce GeneralInputVars'ta ara
            PlcVariable variable = GeneralInputVars.FirstOrDefault(v => v.Name == varName);

            // 2. Bulamazsa PlcService Input/Output değişkenlerinde ara
            if (variable == null && PlcService.Instance != null)
            {
                variable = PlcService.Instance.InputVariables.FirstOrDefault(v => v.Name == varName);
                if (variable == null)
                    variable = PlcService.Instance.OutputVariables.FirstOrDefault(v => v.Name == varName);
            }

            // 3. İstasyon değişkenlerinde de ara
            if (variable == null)
            {
                variable = Station1Vars.FirstOrDefault(v => v.Name == varName)
                        ?? Station2Vars.FirstOrDefault(v => v.Name == varName)
                        ?? Station3Vars.FirstOrDefault(v => v.Name == varName);
            }

            if (variable == null || variable.CurrentValue == null) return false;

            string val = variable.CurrentValue.ToString().ToUpper().Trim();

            // PLC'den gelebilecek tüm "DOĞRU" ihtimalleri
            bool isTrue = (val == "TRUE" || val == "1" || val == "ON" || val == "OK" || val == "READY");

            return expectedTrue ? isTrue : !isTrue;
        }

        // --- CANLI DURUM VE ÖN KOŞUL KONTROLÜ ---
        private void UpdateLineStatusVisuals()
        {
            bool isRunning = IsConditionMet("LINE_RUNNING", true);
            string physicalErrorMessage = null;

            // 1. FİZİKSEL DURUM KONTROLÜ (SystemCheckList + İstasyonlar)
            foreach (var check in GlobalData.SystemCheckList)
            {
                if (!IsConditionMet(check.TagName, true))
                {
                    physicalErrorMessage = check.ErrorMessage;
                    break; // İlk hatayı bul
                }
            }

            if (string.IsNullOrEmpty(physicalErrorMessage))
            {
                if (Stations.Any(s => s.HasAlarm))
                {
                    physicalErrorMessage = "İSTASYON ARIZASI (İSTASYONLARI KONTROL EDİN)";
                }
            }

            // 2. ARIZA MÜHÜRLEME (LATCHING) VE OTOMATİK DURDURMA MANTIĞI
            if (physicalErrorMessage != null)
            {
                // ▼▼▼ YENİ EKLENEN GÜVENLİK BLOĞU ▼▼▼
                // Eğer hata İLK DEFA oluşuyorsa (daha önce mühürlenmemişse), SİSTEMİ DURDUR!
                if (!_isLatchedFault)
                {
                    AddLog($"ARIZA TESPİT EDİLDİ: {physicalErrorMessage}. Sistem otomatik durduruldu.", "Red");

                    // PLC'ye durma sinyali gönder ve start'ı iptal et
                    SetBtn("CMD_LINE_STOP", true);
                    SetBtn("CMD_LINE_START", false);

                    // Simülasyon/İç durumu sıfırla ki Reset atıldığında direk başlamasın
                    SetIn("LINE_RUNNING", false);
                    isRunning = false;
                }
                // ▲▲▲ YENİ EKLENEN GÜVENLİK BLOĞU ▲▲▲

                _isLatchedFault = true;
                _latchedErrorMessage = physicalErrorMessage;
            }

            // --- GÖRSEL KARTLARIN GÜNCELLENMESİ ---
            if (physicalErrorMessage == null)
            {
                CheckAlarmBorder.Background = new SolidColorBrush(Microsoft.UI.Colors.DarkGreen);
                CheckAlarmBorder.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.LimeGreen);
                CheckAlarmIcon.Glyph = "\uE73E";
                CheckAlarmText.Text = "SİSTEM TEMİZ";
            }
            else
            {
                CheckAlarmBorder.Background = new SolidColorBrush(Microsoft.UI.Colors.DarkRed);
                CheckAlarmBorder.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);
                CheckAlarmIcon.Glyph = "\uE7BA";
                CheckAlarmText.Text = physicalErrorMessage;
            }
            CheckAlarmText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
            CheckAlarmIcon.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);

            // --- BUTONLARIN KİLİT MANTIĞI ---

            // BAŞLAT: Arıza mühürü yoksa VE sistem zaten çalışmıyorsa aktif
            BtnStart.IsEnabled = !_isLatchedFault && !isRunning;
            BtnStart.Opacity = BtnStart.IsEnabled ? 1.0 : 0.3;

            // DURDUR: Sadece sistem ÇALIŞIYORSA aktif
            BtnStop.IsEnabled = isRunning;
            BtnStop.Opacity = BtnStop.IsEnabled ? 1.0 : 0.3;

            // RESET: Sadece arıza mühürü varsa aktif
            BtnReset.IsEnabled = _isLatchedFault;
            BtnReset.Opacity = BtnReset.IsEnabled ? 1.0 : 0.3;

            // --- ANA DURUM (KONTROL PANELİ ÜST METNİ) ---
            if (_isLatchedFault)
            {
                if (physicalErrorMessage != null)
                {
                    // Hata hala devam ediyor
                    SetStatus($"ARIZA DEVAM EDİYOR!", Microsoft.UI.Colors.Red, "\uE7BA");
                }
                else
                {
                    // Hata fiziksel olarak düzelmiş ama RESET'e basılmamış
                    SetStatus($"RESET BEKLENİYOR...", Microsoft.UI.Colors.Orange, "\uE72C");
                }
            }
            else if (isRunning)
            {
                SetStatus("HAT ÇALIŞIYOR", Microsoft.UI.Colors.LimeGreen, "\uE768");
            }
            else
            {
                SetStatus("BAŞLATMAYA HAZIR", Microsoft.UI.Colors.LightBlue, "\uE73E");
            }
        }

        // Yardımcı: Status Kartını Boya
        private void SetStatus(string text, Windows.UI.Color color, string icon)
        {
            LineStatusCard.BorderBrush = new SolidColorBrush(color);
            LineStatusText.Text = text;
            LineStatusText.Foreground = new SolidColorBrush(color);
            LineStatusIcon.Glyph = icon;
            LineStatusIcon.Foreground = new SolidColorBrush(color);
        }

        // --- START BUTONU (ARTIK KONTROLLÜ) ---
        // DÜZELTİLDİ: Hardcoded SAFETY_OK kontrolü kaldırıldı.
        // Tüm ön koşullar SystemCheckList + İstasyon alarmları üzerinden kontrol edilir.
        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            // Son güvenlik kontrolü
            if (_isLatchedFault)
            {
                AddLog("BAŞLATILAMADI: Önce arızayı giderip RESET atmanız gerekmektedir.", "Red");
                return;
            }

            SetBtn("CMD_LINE_START", true);
            SetBtn("CMD_LINE_STOP", false);

            // Simülasyon geri bildirimi (Gerçekte bu PLC'den gelecektir)
            SetIn("LINE_RUNNING", true);

            AddLog("Hat Başlatıldı.", "Green");
            UpdateLineStatusVisuals();
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            SetBtn("CMD_LINE_STOP", true);
            SetBtn("CMD_LINE_START", false);

            // Simülasyon geri bildirimi
            SetIn("LINE_RUNNING", false);

            AddLog("Hat Durduruldu.", "Yellow");
            UpdateLineStatusVisuals();
        }

        private async void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            if (!_isLatchedFault) return; // Mühür yoksa reset atılamaz

            BtnReset.IsEnabled = false; // Çift tıklamayı önle

            // PLC'ye Reset Sinyali Gönder
            var resetVar = GeneralOutputVars.FirstOrDefault(x => x.Name == "CMD_LINE_RESET");
            if (resetVar != null)
            {
                resetVar.CurrentValue = true;
                AddLog("Reset sinyali PLC'ye gönderiliyor...", "Orange");
                await Task.Delay(1000);
                resetVar.CurrentValue = false;
            }

            // Simülasyon: İstasyon alarmlarını temizlemeyi dene
            foreach (var s in Stations) s.HasAlarm = false;

            // --- RESET SONRASI TEKRAR KONTROL ET ---
            string remainingError = null;
            foreach (var check in GlobalData.SystemCheckList)
            {
                if (!IsConditionMet(check.TagName, true))
                {
                    remainingError = check.ErrorMessage;
                    break;
                }
            }

            if (string.IsNullOrEmpty(remainingError) && Stations.Any(s => s.HasAlarm))
            {
                remainingError = "İSTASYON ARIZASI";
            }

            // Karar: Arızalar tamamen gitti mi?
            if (remainingError == null)
            {
                _isLatchedFault = false; // Mühürü Kır!
                _latchedErrorMessage = "";
                AddLog("Arızalar giderildi, sistem temiz.", "Green");
            }
            else
            {
                // Arıza hala devam ediyor, mühür kırılmıyor
                _latchedErrorMessage = remainingError;
                AddLog($"Reset atıldı ancak arıza devam ediyor: {remainingError}", "Red");
            }

            // Ekranı yeni duruma göre güncelle
            UpdateLineStatusVisuals();
        }

        private void SetBtn(string n, bool v) { var var = GeneralOutputVars.FirstOrDefault(x => x.Name == n); if (var != null) var.CurrentValue = v; }
        private void SetIn(string n, bool v) { var var = GeneralInputVars.FirstOrDefault(x => x.Name == n); if (var != null) var.CurrentValue = v; }

        private void BtnAddRfid_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TxtNewRfidId.Text))
            {
                // DÜZELTME: 'new' eklendi ve tam adres yazıldı
                KnownRfids.Add(new App4.Utilities.RfidDef
                {
                    Id = TxtNewRfidId.Text,
                    Description = TxtNewRfidDesc.Text
                });

                TxtNewRfidId.Text = "";
                TxtNewRfidDesc.Text = "";
            }
        }
        private void BtnDeleteRfid_Click(object sender, RoutedEventArgs e)
        {
            // DÜZELTME: 'is RfidDef' yerine 'is App4.Utilities.RfidDef' yazıldı
            if (sender is Button b && b.DataContext is App4.Utilities.RfidDef item)
            {
                KnownRfids.Remove(item);
            }
        }
        private void BtnClearLogs_Click(object sender, RoutedEventArgs e) => SystemLogs.Clear();

        private void AddLog(string msg, string clr) => SystemLogs.Insert(0, new App4.Utilities.LogEntry { TimeStr = DateTime.Now.ToString("HH:mm:ss"), Message = msg, ColorCode = clr });
        private void UpdatePlcVar(ObservableCollection<PlcVariable> c, string n, string v) { var i = c.FirstOrDefault(x => x.Name == n); if (i != null && i.Value != v) i.Value = v; }
        private void UpdateSliderPosition(string v) { foreach (var s in Stations) s.IsRobotPresent = false; if (int.TryParse(v, out int p) && p >= 1 && p <= 3) Stations[p - 1].IsRobotPresent = true; }
        private void UpdateStationStatus(string n, string v) { foreach (var s in Stations) { if (s.StatusTag == n) s.ProcessStatus = MapStatus(v);// Başına ünlem (!) koyarak tersini alıyoruz.
                                                                                                                                                  // IsTrue(v) 1 dönerse (True), ! işareti onu False yapar (Alarm Yok).
                                                                                                                                                  // IsTrue(v) 0 dönerse (False), ! işareti onu True yapar (Alarm Var).
                else if (s.AlarmTag == n) s.HasAlarm = !IsTrue(v); else if (s.ProducingTag == n) s.IsProducing = IsTrue(v); else if (s.ProductionCountTag == n) s.ProductionCount = v; else if (s.EfficiencyTag == n) s.Efficiency = v.Contains("%") ? v : "%" + v; else if (s.CurrentRfidTag == n) s.CurrentRfid = v; } }
        private bool IsTrue(string v) => !string.IsNullOrEmpty(v) && (v.ToUpper() == "TRUE" || v == "1" || v == "ON");
        private string MapStatus(string v) => v switch
        {
            "1" => "3D TARAMA",
            "2" => "GAZ KAÇAK TESTİ", // <-- "TESTİ" kelimesinin olduğundan emin olun
            "3" => "TEST TAMAMLANDI",
            "4" => "OK ÜRÜN",
            "5" => "NOK ÜRÜN",
            "6" => "HAZIRLANIYOR",
            _ => v
        };

        private void BtnAddCheck_Click(object sender, RoutedEventArgs e)
        {
            // 1. Tag Seçili mi?
            if (ComboCheckTag.SelectedItem is not string selectedTag)
            {
                AddLog("HATA: Lütfen listeden bir PLC Input seçiniz.", "Orange");
                return;
            }

            // 2. Mesaj Yazılmış mı?
            if (string.IsNullOrEmpty(TxtCheckMessage.Text))
            {
                AddLog("HATA: Lütfen bir hata mesajı yazınız.", "Orange");
                return;
            }

            // 3. Ekleme İşlemi
            try
            {
                GlobalData.SystemCheckList.Add(new App4.Utilities.SystemCheckItem
                {
                    TagName = selectedTag,
                    ErrorMessage = TxtCheckMessage.Text.ToUpper()
                });

                GlobalData.SaveSystemChecks(); // Kaydet

                TxtCheckMessage.Text = ""; // Kutuyu temizle
                                           // ComboCheckTag.SelectedIndex = -1; // İsterseniz seçimi de sıfırlayabilirsiniz

                // Ekler eklemez kontrol etsin
                UpdateLineStatusVisuals();

                AddLog($"Kural Eklendi: {selectedTag}", "Green");
            }
            catch (Exception ex)
            {
                AddLog($"Ekleme Hatası: {ex.Message}", "Red");
            }
        }

        private void BtnDeleteCheck_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.DataContext is App4.Utilities.SystemCheckItem item)
            {
                GlobalData.SystemCheckList.Remove(item);
                GlobalData.SaveSystemChecks(); // Kaydet
                UpdateLineStatusVisuals();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // ROBOT DURUM İZLEME PANELİ
        // ═══════════════════════════════════════════════════════════════

        // --- ROBOT SİNYAL İZLEME BAYRAKLARI ---
        private bool _gocatorTaraProcessing = false;
        private bool _tablaTaraProcessing = false;
        private bool _inficonOlcumProcessing = false;
        private bool _snifferOlcumProcessing = false;

        private void InitializeRobotStatusMonitoring()
        {
            var robots = KukaRobotManager.Instance.Robots;

            // İlk güncellemeyi yap
            if (robots.Count > 0) UpdateRobotStatusPanel(robots[0], 1);
            if (robots.Count > 1) UpdateRobotStatusPanel(robots[1], 2);

            // PropertyChanged ile canlı güncelle + sinyal dinleme
            if (robots.Count > 0)
            {
                robots[0].PropertyChanged += (s, e) =>
                {
                    this.DispatcherQueue.TryEnqueue(() => UpdateRobotStatusPanel(robots[0], 1));
                };

                // InputVars değişikliklerini dinle
                foreach (var v in robots[0].InputVars)
                {
                    v.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(PlcVariable.Value))
                        {
                            this.DispatcherQueue.TryEnqueue(() => UpdateRobotStatusPanel(robots[0], 1));
                            // Robot 1 sinyal tetiklemeleri
                            CheckRobotTriggerSignal(v, robots[0], 1);
                        }
                    };
                }
            }
            if (robots.Count > 1)
            {
                robots[1].PropertyChanged += (s, e) =>
                {
                    this.DispatcherQueue.TryEnqueue(() => UpdateRobotStatusPanel(robots[1], 2));
                };

                foreach (var v in robots[1].InputVars)
                {
                    v.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(PlcVariable.Value))
                        {
                            this.DispatcherQueue.TryEnqueue(() => UpdateRobotStatusPanel(robots[1], 2));
                            // Robot 2 sinyal tetiklemeleri
                            CheckRobotTriggerSignal(v, robots[1], 2);
                        }
                    };
                }
            }
        }

        // =====================================================
        // ROBOT SİNYAL TETİKLEME SİSTEMİ
        // Robot InputVar değiştiğinde otomatik cevap verir
        // =====================================================
        private void CheckRobotTriggerSignal(PlcVariable changedVar, KukaRobotInstance robot, int robotNo)
        {
            bool isTrue = changedVar.Value?.ToUpper() == "TRUE" || changedVar.Value == "1";
            if (!isTrue) return;

            switch (changedVar.Name)
            {
                case "G_GOCATOR_TARA":
                    if (!_gocatorTaraProcessing)
                        _ = HandleGocatorTaraAsync(robot, robotNo);
                    break;

                case "G_TABLA_TARA":
                    if (!_tablaTaraProcessing)
                        _ = HandleTablaTaraAsync(robot, robotNo);
                    break;

                case "G_INFICON_OLCUM_YAP":
                    if (!_inficonOlcumProcessing)
                        _ = HandleInficonOlcumAsync(robot, robotNo);
                    break;

                case "G_SNIFFER_OLCUM_YAP":
                    if (!_snifferOlcumProcessing)
                        _ = HandleSnifferOlcumAsync(robot, robotNo);
                    break;
            }
        }

        // --- YARDIMCI: Robota output değişken yaz ---
        private async Task WriteRobotOutVarAsync(string outVarName, string valueToWrite)
        {
            // GlobalData güncelle
            var gVar = GlobalData.RobotOutputVars.FirstOrDefault(v => v.Name == outVarName);
            if (gVar != null) gVar.Value = valueToWrite;

            // PLC'ye yaz
            var plcVar = PlcService.Instance?.OutputVariables?.FirstOrDefault(v => v.Name == outVarName);
            if (plcVar != null)
            {
                plcVar.Value = valueToWrite;
                await PlcService.Instance.WriteAsync(plcVar, valueToWrite);
            }

            // Tüm robotlara yaz
            var robots = KukaRobotManager.Instance?.Robots;
            if (robots != null)
            {
                foreach (var r in robots)
                {
                    if (r.IsConnected)
                    {
                        try { await r.WriteVariableAsync(outVarName, valueToWrite); } catch { }
                    }
                }
            }
        }

        // =====================================================
        // 1. GOCATOR BORU TARAMA
        // Robot G_GOCATOR_TARA=TRUE → PC Gocator'dan ölçüm al → offset yaz → G_GOCATOR_TAMAM=TRUE
        // =====================================================
        private async Task HandleGocatorTaraAsync(KukaRobotInstance robot, int robotNo)
        {
            _gocatorTaraProcessing = true;
            try
            {
                AddAutoLog($"[Robot {robotNo}] Gocator boru tarama isteği alındı");

                // 1. Gocator'dan ölçüm al
                var (status, results) = await ReceiveMeasurementLogic.ReceiveAndProcessMeasurements(
                    msg => AddAutoLog($"[Gocator] {msg}"),
                    this.DispatcherQueue);

                if (status == 1 && results != null && results.Count > 0)
                {
                    // 2. Ölçüm sonuçlarından offset değerlerini yaz
                    // Gocator ölçümleri sırasıyla X,Y,Z,A,B,C offset olarak yorumlanır
                    string[] offsetNames = { "G_OFFSET_X", "G_OFFSET_Y", "G_OFFSET_Z", "G_OFFSET_A", "G_OFFSET_B", "G_OFFSET_C" };
                    for (int i = 0; i < Math.Min(results.Count, offsetNames.Length); i++)
                    {
                        await WriteRobotOutVarAsync(offsetNames[i], results[i].Value.ToString("F3"));
                    }

                    // 3. Offset hazır sinyali
                    await WriteRobotOutVarAsync("G_OFFSET_HAZIR", "TRUE");

                    AddAutoLog($"[Robot {robotNo}] Gocator tarama OK - {results.Count} ölçüm alındı");
                }
                else
                {
                    AddAutoLog($"[Robot {robotNo}] Gocator tarama BAŞARISIZ!");
                }

                // 4. Tamamlandı sinyali gönder
                await WriteRobotOutVarAsync("G_GOCATOR_TAMAM", "TRUE");
                await Task.Delay(500);
                await WriteRobotOutVarAsync("G_GOCATOR_TAMAM", "FALSE");
            }
            catch (Exception ex)
            {
                AddAutoLog($"[Robot {robotNo}] Gocator tarama hatası: {ex.Message}");
                await WriteRobotOutVarAsync("G_GOCATOR_TAMAM", "TRUE");
                await Task.Delay(500);
                await WriteRobotOutVarAsync("G_GOCATOR_TAMAM", "FALSE");
            }
            finally
            {
                _gocatorTaraProcessing = false;
            }
        }

        // =====================================================
        // 2. GOCATOR TABLA TARAMA
        // Robot G_TABLA_TARA=TRUE → PC Gocator'dan tabla ölçümü → tabla offset yaz → G_TABLA_TAMAM=TRUE
        // =====================================================
        private async Task HandleTablaTaraAsync(KukaRobotInstance robot, int robotNo)
        {
            _tablaTaraProcessing = true;
            try
            {
                AddAutoLog($"[Robot {robotNo}] Tabla tarama isteği alındı");

                var (status, results) = await ReceiveMeasurementLogic.ReceiveAndProcessMeasurements(
                    msg => AddAutoLog($"[Gocator-Tabla] {msg}"),
                    this.DispatcherQueue);

                if (status == 1 && results != null && results.Count > 0)
                {
                    string[] offsetNames = { "G_TABLA_OFFSET_X", "G_TABLA_OFFSET_Y", "G_TABLA_OFFSET_Z", "G_TABLA_OFFSET_A", "G_TABLA_OFFSET_B", "G_TABLA_OFFSET_C" };
                    for (int i = 0; i < Math.Min(results.Count, offsetNames.Length); i++)
                    {
                        await WriteRobotOutVarAsync(offsetNames[i], results[i].Value.ToString("F3"));
                    }

                    await WriteRobotOutVarAsync("G_TABLA_OFFSET_HAZIR", "TRUE");

                    AddAutoLog($"[Robot {robotNo}] Tabla tarama OK - {results.Count} ölçüm alındı");
                }
                else
                {
                    AddAutoLog($"[Robot {robotNo}] Tabla tarama BAŞARISIZ!");
                }

                await WriteRobotOutVarAsync("G_TABLA_TAMAM", "TRUE");
                await Task.Delay(500);
                await WriteRobotOutVarAsync("G_TABLA_TAMAM", "FALSE");
            }
            catch (Exception ex)
            {
                AddAutoLog($"[Robot {robotNo}] Tabla tarama hatası: {ex.Message}");
                await WriteRobotOutVarAsync("G_TABLA_TAMAM", "TRUE");
                await Task.Delay(500);
                await WriteRobotOutVarAsync("G_TABLA_TAMAM", "FALSE");
            }
            finally
            {
                _tablaTaraProcessing = false;
            }
        }

        // =====================================================
        // 3. INFICON ÖLÇÜM
        // Robot G_INFICON_OLCUM_YAP=TRUE → PC ölçüm değerini okur → G_INFICON_OK + G_INFICON_TAMAM
        // NOT: Inficon cihaz entegrasyonu henüz yok, sinyal handshake'i hazır
        // =====================================================
        private async Task HandleInficonOlcumAsync(KukaRobotInstance robot, int robotNo)
        {
            _inficonOlcumProcessing = true;
            try
            {
                AddAutoLog($"[Robot {robotNo}] Inficon ölçüm isteği alındı");

                // TODO: Inficon cihazından gerçek ölçüm alma kodu buraya eklenecek
                // Şu an sinyal handshake'i hazır, cihaz entegrasyonu yapılacak
                // Örnek: var result = await InficonService.MeasureAsync();
                //        double deger = result.Value;
                //        bool ok = deger < threshold;

                // Geçici: Ölçüm değeri ve sonucu robottan okunan/sabit değerle doldurulacak
                // Gerçek entegrasyonda burası InficonService ile değiştirilecek
                AddAutoLog($"[Robot {robotNo}] Inficon ölçüm - cihaz entegrasyonu bekleniyor (handshake hazır)");

                // Tamamlandı sinyali gönder (robot timeout'a düşmesin)
                await WriteRobotOutVarAsync("G_INFICON_TAMAM", "TRUE");
                await Task.Delay(500);
                await WriteRobotOutVarAsync("G_INFICON_TAMAM", "FALSE");
            }
            catch (Exception ex)
            {
                AddAutoLog($"[Robot {robotNo}] Inficon ölçüm hatası: {ex.Message}");
                await WriteRobotOutVarAsync("G_INFICON_TAMAM", "TRUE");
                await Task.Delay(500);
                await WriteRobotOutVarAsync("G_INFICON_TAMAM", "FALSE");
            }
            finally
            {
                _inficonOlcumProcessing = false;
            }
        }

        // =====================================================
        // 4. SNIFFER ÖLÇÜM (Robot 2)
        // Robot G_SNIFFER_OLCUM_YAP=TRUE → PC ölçüm → G_SNIFFER_OK + G_SNIFFER_TAMAM
        // NOT: Sniffer cihaz entegrasyonu henüz yok, sinyal handshake'i hazır
        // =====================================================
        private async Task HandleSnifferOlcumAsync(KukaRobotInstance robot, int robotNo)
        {
            _snifferOlcumProcessing = true;
            try
            {
                AddAutoLog($"[Robot {robotNo}] Sniffer ölçüm isteği alındı");

                // TODO: Sniffer cihazından gerçek ölçüm alma kodu buraya eklenecek
                // Örnek: var result = await SnifferService.MeasureAsync();
                AddAutoLog($"[Robot {robotNo}] Sniffer ölçüm - cihaz entegrasyonu bekleniyor (handshake hazır)");

                await WriteRobotOutVarAsync("G_SNIFFER_TAMAM", "TRUE");
                await Task.Delay(500);
                await WriteRobotOutVarAsync("G_SNIFFER_TAMAM", "FALSE");
            }
            catch (Exception ex)
            {
                AddAutoLog($"[Robot {robotNo}] Sniffer ölçüm hatası: {ex.Message}");
                await WriteRobotOutVarAsync("G_SNIFFER_TAMAM", "TRUE");
                await Task.Delay(500);
                await WriteRobotOutVarAsync("G_SNIFFER_TAMAM", "FALSE");
            }
            finally
            {
                _snifferOlcumProcessing = false;
            }
        }

        // --- LOG YARDIMCISI ---
        private void AddAutoLog(string message)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                SystemLogs.Insert(0, new App4.Utilities.LogEntry
                {
                    TimeStr = DateTime.Now.ToString("HH:mm:ss"),
                    Message = message,
                    ColorCode = "White"
                });
                if (SystemLogs.Count > 200) SystemLogs.RemoveAt(SystemLogs.Count - 1);
            });
        }

        private void UpdateRobotStatusPanel(KukaRobotInstance robot, int robotNo)
        {
            try
            {
                // Değişkenleri oku
                string GetVar(string name) => robot.InputVars.FirstOrDefault(v => v.Name == name)?.Value ?? "";

                int robotDurum = int.TryParse(GetVar("G_ROBOT_DURUM"), out var rd) ? rd : -1;
                int durumMesaj = int.TryParse(GetVar("G_DURUM_MESAJ"), out var dm) ? dm : -1;
                bool hataVar = GetVar("G_HATA_VAR")?.ToUpper() == "TRUE" || GetVar("G_HATA_VAR") == "1";
                int hataKodu = int.TryParse(GetVar("G_HATA_KODU"), out var hk) ? hk : 0;
                int aktifNokta = int.TryParse(GetVar("G_AKTIF_NOKTA"), out var an) ? an : 0;
                int toplamNokta = int.TryParse(GetVar("G_TOPLAM_NOKTA"), out var tn) ? tn : 0;
                int aktifCizgi = int.TryParse(GetVar("G_AKTIF_CIZGI"), out var ac) ? ac : 0;
                int toplamCizgi = int.TryParse(GetVar("G_TOPLAM_CIZGI"), out var tc) ? tc : 0;
                int nokSayisi = int.TryParse(GetVar("G_NOK_SAYISI"), out var ns) ? ns : 0;

                // Durum metni
                string durumText = robotDurum switch
                {
                    0 => "Bosta",
                    1 => "Calisiyor",
                    2 => "HATA!",
                    10 => "Gocator Tarama",
                    11 => "Gocator OK",
                    20 => "Olcum Yapiliyor",
                    21 => "Olcum OK",
                    50 => "Tabla Tarama",
                    51 => "Tabla OK",
                    _ => $"Bilinmiyor ({robotDurum})"
                };

                // Durum rengi
                string durumColor = robotDurum switch
                {
                    0 => "#4CAF50",
                    1 => "#FF9800",
                    2 => "#F44336",
                    10 or 11 => "#00BCD4",
                    20 or 21 => "#2196F3",
                    50 or 51 => "#FF5722",
                    _ => "#888"
                };

                // Mesaj metni
                string mesajText = DecodeDurumMesaj(durumMesaj, robotNo);

                // Hata metni
                string hataText = "Yok";
                string hataColor = "#4CAF50";
                if (hataVar || hataKodu > 0)
                {
                    hataText = hataKodu switch
                    {
                        5 => "Tabla Timeout!",
                        10 => "Gocator Timeout!",
                        20 => robotNo == 1 ? "Inficon Timeout!" : "Sniffer Timeout!",
                        _ => hataKodu > 0 ? $"Kod: {hataKodu}" : "Hata Var!"
                    };
                    hataColor = "#F44336";
                }

                // Status dot rengi
                string dotColor = "#555";
                if (!robot.IsConnected) dotColor = "#555";
                else if (hataVar || robotDurum == 2) dotColor = "#F44336";
                else if (robotDurum == 1 || robotDurum >= 10) dotColor = "#FF9800";
                else dotColor = "#4CAF50";

                // Warning
                string warningText = "";
                if (hataVar || hataKodu > 0)
                {
                    warningText = $"HATA: {hataText}";
                }
                else if (nokSayisi > 0)
                {
                    warningText = $"NOK: {nokSayisi} basarisiz olcum";
                }

                // UI Guncelle
                if (robotNo == 1)
                {
                    SetTextSafe(Robot1DurumText, durumText, durumColor);
                    SetTextSafe(Robot1MesajText, mesajText, "#888");
                    SetTextSafe(Robot1HataText, hataText, hataColor);
                    SetTextSafe(Robot1NoktaText, aktifNokta > 0 ? $"{aktifNokta} / {toplamNokta}" : "---", "#888");
                    SetDotColor(Robot1StatusDot, dotColor);
                    SetWarning(Robot1WarningBorder, Robot1WarningText, warningText);
                }
                else
                {
                    SetTextSafe(Robot2DurumText, durumText, durumColor);
                    SetTextSafe(Robot2MesajText, mesajText, "#888");
                    SetTextSafe(Robot2HataText, hataText, hataColor);
                    SetTextSafe(Robot2CizgiText, aktifCizgi > 0 ? $"{aktifCizgi} / {toplamCizgi}" : "---", "#888");
                    SetDotColor(Robot2StatusDot, dotColor);
                    SetWarning(Robot2WarningBorder, Robot2WarningText, warningText);
                }
            }
            catch { /* Güvenli hata yutma */ }
        }

        private string DecodeDurumMesaj(int mesaj, int robotNo)
        {
            // Genel kodlar (her iki robot icin ortak)
            switch (mesaj)
            {
                case 0: return "Bosta";
                case 2: return "Is bitti";
                case 3: return "Hata ile bitti";
                case 5: return "Klima tipi bekleniyor";
                case 10: return "Home'a gidiyor";
                case 11: return "Home tamam";
            }

            // Tabla kontrol (Robot 1)
            if (robotNo == 1)
            {
                switch (mesaj)
                {
                    case 50: return "Tabla kontrol basladi";
                    case 51: return "Tabla taranıyor";
                    case 52: return "Tabla offset tamam";
                }
            }

            // Klima tip kodlari: N*100 serisi
            if (mesaj >= 100 && mesaj <= 1199)
            {
                int tipNo = mesaj / 100;
                int alt = mesaj % 100;

                if (robotNo == 1)
                {
                    // Robot 1: N*100=basladi, +1=gocator ok, +2=olcum, +3=bitis
                    return alt switch
                    {
                        0 => $"Tip {tipNo}: Taramaya basladi",
                        1 => $"Tip {tipNo}: Gocator OK, olcume geciyor",
                        2 => $"Tip {tipNo}: Inficon olcum yapiliyor",
                        3 => $"Tip {tipNo}: Bitti",
                        _ => $"Tip {tipNo}: Kod {alt}"
                    };
                }
                else
                {
                    // Robot 2: N*100=gecis, +1..+7=cizgi no
                    if (alt == 0) return $"Tip {tipNo}: Gecis pozisyonu";
                    if (alt >= 1 && alt <= 7) return $"Tip {tipNo}: Cizgi {alt} olcum";
                    return $"Tip {tipNo}: Kod {alt}";
                }
            }

            return mesaj >= 0 ? $"Kod: {mesaj}" : "---";
        }

        private void SetTextSafe(TextBlock tb, string text, string color)
        {
            if (tb == null) return;
            tb.Text = text;
            try { tb.Foreground = new SolidColorBrush(ParseHexColor(color)); } catch { }
        }

        private void SetDotColor(Border dot, string color)
        {
            if (dot == null) return;
            try { dot.Background = new SolidColorBrush(ParseHexColor(color)); } catch { }
        }

        private void SetWarning(Border border, TextBlock text, string message)
        {
            if (border == null || text == null) return;
            if (string.IsNullOrEmpty(message))
            {
                border.Visibility = Visibility.Collapsed;
            }
            else
            {
                border.Visibility = Visibility.Visible;
                text.Text = message;
            }
        }

        // --- İSTASYON POZİSYON LABEL GÜNCELLEMESİ ---
        private void UpdateSliderStationPosLabels()
        {
            if (SliderSt1PosLabel != null)
                SliderSt1PosLabel.Text = $"{GlobalData.KL100_Station1Pos:F0} mm";
            if (SliderSt2PosLabel != null)
                SliderSt2PosLabel.Text = $"{GlobalData.KL100_Station2Pos:F0} mm";
            if (SliderSt3PosLabel != null)
                SliderSt3PosLabel.Text = $"{GlobalData.KL100_Station3Pos:F0} mm";
        }

        private static Windows.UI.Color ParseHexColor(string hex)
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
                return Windows.UI.Color.FromArgb(255,
                    Convert.ToByte(hex.Substring(0, 2), 16),
                    Convert.ToByte(hex.Substring(2, 2), 16),
                    Convert.ToByte(hex.Substring(4, 2), 16));
            return Windows.UI.Color.FromArgb(255, 136, 136, 136);
        }


    }
}