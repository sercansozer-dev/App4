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
        public ObservableCollection<App4.Utilities.SafetyCheckItem> SafetyAlarmList => GlobalData.SafetyAlarmList;
        public ObservableCollection<App4.Utilities.SafetyCheckItem> SafetyWarningList => GlobalData.SafetyWarningList;
        public ObservableCollection<PlcVariable> GeneralInputVars => GlobalData.GeneralInputVars;
        public ObservableCollection<PlcVariable> GeneralOutputVars => GlobalData.GeneralOutputVars;
        // INFICON değişkenleri Inficon_Page'de gösterilecek — Auto_Page'den filtrele
        public ObservableCollection<PlcVariable> FilteredGeneralInputVars { get; set; } = new();
        public ObservableCollection<PlcVariable> FilteredGeneralOutputVars { get; set; } = new();
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

        // R2'nin son hedef istasyonu (R1 de aynı istasyonda, hedefIst=0 olunca son istasyonda kalsın)
        private int _r2LastHedefIst = 0;

        // --- SAFETY ALARM ÇIKIŞ TAG SEÇİMİ ---
        public string SafetyAlarmR1OutputTag
        {
            get => GlobalData.SafetyAlarmR1OutputTag;
            set { if (GlobalData.SafetyAlarmR1OutputTag != value) { GlobalData.SafetyAlarmR1OutputTag = value; } }
        }

        public string SafetyAlarmR2OutputTag
        {
            get => GlobalData.SafetyAlarmR2OutputTag;
            set { if (GlobalData.SafetyAlarmR2OutputTag != value) { GlobalData.SafetyAlarmR2OutputTag = value; } }
        }

        private void ComboSafetyAlarmOutput_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cmb && cmb.SelectedItem is string selectedTag)
            {
                if (cmb.Name == "ComboSafetyAlarmR1Output")
                    GlobalData.SafetyAlarmR1OutputTag = selectedTag;
                else if (cmb.Name == "ComboSafetyAlarmR2Output")
                    GlobalData.SafetyAlarmR2OutputTag = selectedTag;
            }
        }

        // Alarm tetiklendiğinde seçili tag'lere TRUE, temizlendiğinde FALSE yazar
        private bool? _lastSafetyAlarmState = null; // null = henuz yazilmadi, ilk cagri mutlaka calisir
        private async void WriteSafetyAlarmOutputs(bool alarmActive)
        {
            if (_lastSafetyAlarmState.HasValue && _lastSafetyAlarmState.Value == alarmActive) return;
            _lastSafetyAlarmState = alarmActive;

            string value = alarmActive ? "TRUE" : "FALSE";
            string r1Tag = GlobalData.SafetyAlarmR1OutputTag;
            string r2Tag = GlobalData.SafetyAlarmR2OutputTag;

            if (!string.IsNullOrEmpty(r1Tag))
            {
                try { await WriteToRobotTagAsync(r1Tag, value); } catch { }
            }
            if (!string.IsNullOrEmpty(r2Tag))
            {
                try { await WriteToRobotTagAsync(r2Tag, value); } catch { }
            }

            // SAFETY_OK output degiskeni — kullanici tablodan robot tag eslestirmesi yapar
            // CMD_LINE_START gibi diger output degiskenleriyle ayni mantikta calisir
            string safetyOkValue = alarmActive ? "FALSE" : "TRUE";
            var safetyOkVar = GlobalData.GeneralOutputVars.FirstOrDefault(v => v.Name == "SAFETY_OK");
            if (safetyOkVar != null)
            {
                safetyOkVar.CurrentValue = safetyOkValue;
                safetyOkVar.Value = safetyOkValue;
            }

            if (alarmActive)
                AddLog($"⚠ SAFETY ALARM: SAFETY_OK = FALSE", "Red");
            else
                AddLog($"✓ SAFETY OK: SAFETY_OK = TRUE", "Green");
        }

        /// <summary>
        /// R1:/R2: prefix'li veya prefix'siz tag'e değer yazar.
        /// "R1:G_XXX" → Robot 1'e G_XXX yazar, "R2:G_YYY" → Robot 2'ye G_YYY yazar.
        /// Prefix yoksa WriteToAllRobotsAsync ile tüm robotlara yazar.
        /// </summary>
        private async Task WriteToRobotTagAsync(string tagName, string value)
        {
            if (string.IsNullOrEmpty(tagName)) return;

            // R1:/R2: prefix parse
            if (tagName.Length > 3 && tagName[0] == 'R' && tagName[2] == ':')
            {
                if (int.TryParse(tagName.Substring(1, 1), out int robotNo) && robotNo >= 1)
                {
                    string varName = tagName.Substring(3); // "R1:G_XXX" → "G_XXX"
                    var robots = KukaRobotManager.Instance?.Robots;
                    if (robots != null && robotNo <= robots.Count)
                    {
                        var robot = robots[robotNo - 1];
                        if (robot.IsConnected)
                        {
                            // Robot output var güncelle
                            var outVar = robot.OutputVars.FirstOrDefault(v => v.Name == varName);
                            if (outVar != null) outVar.CurrentValue = value;

                            // KRL tag bul ve yaz
                            string krlTag = varName;
                            var matchVar = robot.OutputVars.FirstOrDefault(v => v.Name == varName)
                                        ?? robot.InputVars.FirstOrDefault(v => v.Name == varName);
                            if (matchVar != null && !string.IsNullOrEmpty(matchVar.PlcTag))
                                krlTag = matchVar.PlcTag;

                            await robot.WriteVariableAsync(krlTag, value);
                        }
                    }
                    return;
                }
            }

            // Prefix yoksa tüm robotlara yaz
            await GlobalData.WriteToAllRobotsAsync(tagName, value);
        }

        // --- SAFETY SİNYAL GÜNCELLEME TIMER'I ---
        private DispatcherTimer _safetySignalTimer;

        // --- ARIZA MÜHÜRLEME (LATCHING) DEĞİŞKENLERİ ---
        private bool _isLatchedFault = false;
        private string _latchedErrorMessage = "";

        public Auto_Page()
        {
            this.InitializeComponent();
            this.DataContext = this;

            // INFICON değişkenlerini filtrele (Inficon_Page'de gösterilecek)
            foreach (var v in GlobalData.GeneralInputVars.Where(v => !v.Name.StartsWith("INFICON")))
                FilteredGeneralInputVars.Add(v);
            foreach (var v in GlobalData.GeneralOutputVars.Where(v => !v.Name.StartsWith("INFICON")))
                FilteredGeneralOutputVars.Add(v);

            // Tag listelerini ve modelleri doldur
            InitializeAvailablePlcTags();
            InitializeAvailableModels();

            // Olayları dinlemeye başla (Sayfa her açıldığında tekrar bağlanır)
            this.Loaded += Page_Loaded;
            this.Unloaded += Page_Unloaded;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            // Safety timer durdur
            _safetySignalTimer?.Stop();

            foreach (var s in Stations)
            {
                s.PropertyChanged -= Station_PropertyChanged;
            }

            foreach (var rfid in KnownRfids)
            {
                rfid.PropertyChanged -= Rfid_PropertyChanged;
            }
            KnownRfids.CollectionChanged -= KnownRfids_CollectionChanged;

            // ═══ SAYFA KAPANIRKEN PLC TAG EŞLEŞTİRMELERİNİ KAYDET ═══
            try
            {
                GlobalData.SavePlcVariableTagsToFile();
                System.Diagnostics.Debug.WriteLine("[PAGE_UNLOAD] PlcVariable tag eşleştirmeleri kaydedildi.");
            }
            catch { }
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

                    // PLC ile bağlantıyı kur (Tag 1 + Tag 2)
                    ConnectToPlcVariable(v);
                    ConnectToPlcVariable2(v);
                }
            }

            // 2.5. Robot köprü değişkenleri artık CSV import ile yönetiliyor
            // EnsureRobotBridgeVariables() kaldırıldı — PLC sayfasından CSV ile yükle

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

            // 2.9. AKTUEL_RFID ve AKTUEL_KLIMA_INDEX güncelle
            // Tüm binding'ler tamamlandıktan sonra aktif istasyondan RFID bilgisini yaz
            UpdateAktuelRfidFromStation();

            // 2.92. SNIFFER + SAPMA değerlerini aktif kart+job'a göre senkronize et
            // AktuelRfid değişmemiş olsa bile doğru değerleri output tag'lere yazmayı garanti eder.
            GlobalData.SyncCurrentJobOutputs();

            // 2.95. ComboBox PlcTag binding'lerini zorla güncelle
            // SORUN: InitializeComponent sırasında AvailableOutputPlcTags boş.
            //        x:Bind OneWay, PlcTag'ı boş listeye eşleştiremez → SelectedItem null kalır.
            //        Sonra liste dolsa da PlcTag değişmediği için x:Bind tekrar push yapmaz.
            // ÇÖZÜM: DispatcherQueue ile UI render tamamlandıktan sonra PropertyChanged tetikle.
            //        Bu ComboBox'ları PlcTag değerine göre doğru item'ı seçmeye zorlar.
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                void RefreshTags(ObservableCollection<PlcVariable> vars)
                {
                    foreach (var v in vars) v.NotifyPlcTagChanged();
                }
                RefreshTags(GeneralInputVars);
                RefreshTags(GeneralOutputVars);
                RefreshTags(Station1Vars);
                RefreshTags(Station1Outputs);
                RefreshTags(Station2Vars);
                RefreshTags(Station2Outputs);
                RefreshTags(Station3Vars);
                RefreshTags(Station3Outputs);
                System.Diagnostics.Debug.WriteLine("[PLC_TAG_REFRESH] ComboBox PlcTag binding'leri güncellendi");
            });

            // 3. Hat Durum Işıklarını Yak
            UpdateLineStatusVisuals();

            // 3.5. OTO/MANUEL Switch'leri PLC değişkeniyle senkronize et
            var switchVar = GeneralOutputVars.FirstOrDefault(v => v.Name == "LINE_AUTO_MANUAL_CMD");
            if (switchVar != null)
            {
                bool isOn = switchVar.Value?.ToUpper() == "TRUE" || switchVar.Value == "1";
                if (KontrolLineAutoManualSwitch != null) KontrolLineAutoManualSwitch.IsOn = isOn;
            }

             // 4. Viewerları Başlat
            _ = InitializeStationViewers();

            // 4.5 Kasa Tipleri Panelini Doldur
            RefreshCasingTypesPanel();

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

            // 8. Safety Çalışma Koşulları Panelini Bağla
            InitializeSafetyConditionsPanel();

            // 9. Safety Sinyal Timer — Robot VarProxy sinyallerini periyodik güncelle
            if (_safetySignalTimer == null)
            {
                _safetySignalTimer = new DispatcherTimer();
                _safetySignalTimer.Interval = TimeSpan.FromMilliseconds(GlobalData.Safety_CheckInterval);
                _safetySignalTimer.Tick += (s, args) =>
                {
                    UpdateSafetySignalLeds();
                    UpdateLineStatusVisuals();
                    UpdateActivePoints();
                };
            }
            _safetySignalTimer.Start();
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

                // ▼▼▼ MOD BAZLI RFID ÇÖZÜMLEME ▼▼▼
                string rfidToLookup;
                string modeStr;

                if (station.RfidOpMode.Equals(App4.Utilities.RfidOperationMode.Mixed))
                {
                    // MIX MOD → Okunan (CurrentRfid) RFID'ye eşleşen model
                    rfidToLookup = station.CurrentRfid;
                    modeStr = "MIX";
                }
                else
                {
                    // SPECIFIC MOD → Beklenen (TargetRfid) RFID'ye eşleşen model
                    rfidToLookup = station.TargetRfid;
                    modeStr = "SPECIFIC";
                }

                System.Diagnostics.Debug.WriteLine($"[Station {index + 1}] Mode={modeStr}, TargetRfid={station.TargetRfid ?? "null"}, CurrentRfid={station.CurrentRfid ?? "null"}, Seçilen={rfidToLookup ?? "null"}");

                // ▼▼▼ RFID BOŞSA → ÖNCEKİ MODELİ TEMİZLE ▼▼▼
                if (string.IsNullOrEmpty(rfidToLookup))
                {
                    System.Diagnostics.Debug.WriteLine($"[Station {index + 1}] RFID boş - model temizleniyor");
                    await targetWebView.ExecuteScriptAsync(
                        "if(currentModel) { scene.remove(currentModel); currentModel = null; } " +
                        $"document.getElementById('debug').textContent = '{modeStr}: RFID bekleniyor...';");
                    return;
                }

                // ▼▼▼ KnownRfids'TE ARA ▼▼▼
                var rfidDef = GlobalData.KnownRfids.FirstOrDefault(r => r.Id == rfidToLookup);

                if (rfidDef == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Station {index + 1}] RFID '{rfidToLookup}' KnownRfids'te bulunamadı! Mevcut Id'ler: [{string.Join(", ", GlobalData.KnownRfids.Select(r => r.Id))}]");
                    await targetWebView.ExecuteScriptAsync(
                        "if(currentModel) { scene.remove(currentModel); currentModel = null; } " +
                        $"document.getElementById('debug').textContent = '{modeStr}: \\'{rfidToLookup}\\' tanımsız';");
                    return;
                }

                if (string.IsNullOrEmpty(rfidDef.ModelFileName))
                {
                    System.Diagnostics.Debug.WriteLine($"[Station {index + 1}] RFID '{rfidToLookup}' model dosyası atanmamış");
                    await targetWebView.ExecuteScriptAsync(
                        "if(currentModel) { scene.remove(currentModel); currentModel = null; } " +
                        $"document.getElementById('debug').textContent = '{modeStr}: {rfidToLookup} - Model atanmadı';");
                    return;
                }

                // ▼▼▼ MODEL DOSYASINI YÜKLEme ▼▼▼
                string modelPath = rfidDef.ModelFileName.Replace("\\", "/");
                string modelsRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utilities", "Models");
                string fullPath = Path.Combine(modelsRoot, modelPath);

                if (!File.Exists(fullPath))
                {
                    try
                    {
                        var foundFile = Directory.GetFiles(modelsRoot, Path.GetFileName(modelPath), SearchOption.AllDirectories).FirstOrDefault();
                        if (foundFile != null) fullPath = foundFile;
                    }
                    catch { }
                }

                if (File.Exists(fullPath))
                {
                    string fileUri = new Uri(fullPath).AbsoluteUri;
                    string jsCode = $"if(window.loadModel) {{ window.loadModel('{fileUri.Replace("'", "\\'")}'); }} else {{ console.error('loadModel not ready'); }}";

                    System.Diagnostics.Debug.WriteLine($"[Station {index + 1}] {modeStr} | RFID={rfidToLookup} | Model={Path.GetFileName(fullPath)}");
                    await targetWebView.ExecuteScriptAsync(jsCode);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[Station {index + 1}] Dosya bulunamadı: {fullPath}");
                    await targetWebView.ExecuteScriptAsync(
                        "if(currentModel) { scene.remove(currentModel); currentModel = null; } " +
                        $"document.getElementById('debug').textContent = '{modeStr}: Dosya bulunamadı: {Path.GetFileName(modelPath)}';");
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
                // RfidOpMode, TargetRfid VEYA CurrentRfid değişirse → modeli güncelle
                // (UpdateStationModelAsync zaten mod bazlı doğru RFID'yi seçiyor)
                bool shouldUpdateModel =
                    e.PropertyName == nameof(ExtendedStationViewModel.RfidOpMode) ||
                    e.PropertyName == nameof(ExtendedStationViewModel.TargetRfid) ||
                    e.PropertyName == nameof(StationViewModel.CurrentRfid);

                if (shouldUpdateModel)
                {
                    System.Diagnostics.Debug.WriteLine($"[MODEL] {station.Name}: Property={e.PropertyName} → Model güncelleniyor (Mode={station.RfidOpMode})");
                    this.DispatcherQueue.TryEnqueue(() => UpdateStationModel(station));

                    // Her RFID/Mod değişikliğinde AKTUEL_RFID güncelle
                    // (hangi istasyonun aktif olduğunu fonksiyon kendi belirler)
                    UpdateAktuelRfidFromStation();
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

        // --- DEĞİŞKEN DEĞERİ DEĞİŞİRSE UI GÜNCELLE ---
        // NOT: SavePlcVariableTagsToFile() artık burada çağrılmıyor.
        // PlcTag eşleştirmeleri sadece ComboBox değiştiğinde kaydedilir.
        // Her 50ms'lik CurrentValue değişiminde disk yazma aşırı I/O yükü + dosya bozulma riski oluşturuyordu.
        private void LocalVariable_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is PlcVariable localVar && e.PropertyName == nameof(PlcVariable.CurrentValue))
            {
                UpdateLineStatusVisuals();

                // Slider vb. görsel güncellemeler
                if (localVar.Name == "SLIDER_POS_ACT") UpdateSliderPosition(localVar.CurrentValue?.ToString());
                else UpdateStationStatus(localVar.Name, localVar.CurrentValue?.ToString());

                // Robot 2 HOME sinyali → ilgili istasyonda ev ikonu göster
                if (localVar.Name == "RB2_HOME_OK")
                {
                    bool isHome = IsTrue(localVar.CurrentValue?.ToString());
                    UpdateRobotHomeOnStation(isHome);
                }

                // OTO/MANUEL switch senkronizasyonu (PLC'den gelen değeri UI'a yansıt)
                if (localVar.Name == "LINE_AUTO_MANUAL_CMD")
                {
                    bool isOn = localVar.Value?.ToUpper() == "TRUE" || localVar.Value == "1";
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
                // v9.1 FIX: Trim() ile sondaki boşlukları temizle
                // PLC_Config.json'da sondaki boşluk olursa ComboBox SelectedItem eşleşmesi bozuluyordu
                foreach (var v in PlcService.Instance.InputVariables) AvailableInputPlcTags.Add(v.Name?.Trim());
                foreach (var v in PlcService.Instance.OutputVariables) AvailableOutputPlcTags.Add(v.Name?.Trim());

                // Robot InputVars'ları R1:/R2: prefix'iyle ekle (dinamik — Robot_Page'den eklenen/çıkarılan sinyaller burada görünür)
                var robots = KukaRobotManager.Instance?.Robots;
                if (robots != null)
                {
                    for (int i = 0; i < robots.Count; i++)
                    {
                        foreach (var v in robots[i].InputVars)
                            if (!string.IsNullOrEmpty(v.Name))
                                AvailableInputPlcTags.Add($"R{i + 1}:{v.Name}");

                        // Robot OutputVars'ları da R1:/R2: prefix'iyle output tag listesine ekle
                        foreach (var v in robots[i].OutputVars)
                            if (!string.IsNullOrEmpty(v.Name))
                                AvailableOutputPlcTags.Add($"R{i + 1}:{v.Name}");
                    }

                    // KUKA VarProxy Safety Tag'leri — doğrudan robot property'lerinden okunur
                    if (robots.Count > 0)
                    {
                        AvailableInputPlcTags.Add("$STOPMESS");
                        AvailableInputPlcTags.Add("$DRIVES_ON");
                        AvailableInputPlcTags.Add("$USER_SAF");
                        AvailableInputPlcTags.Add("$PERI_RDY");
                        AvailableInputPlcTags.Add("$ALARM_STOP");
                        AvailableInputPlcTags.Add("$ROB_RDY");
                    }
                }
            }
            catch { }

            // Safety Alarm çıkış tag seçimlerini geri yükle
            try
            {
                string r1Tag = GlobalData.SafetyAlarmR1OutputTag;
                if (!string.IsNullOrEmpty(r1Tag) && AvailableOutputPlcTags.Contains(r1Tag))
                    ComboSafetyAlarmR1Output.SelectedItem = r1Tag;

                string r2Tag = GlobalData.SafetyAlarmR2OutputTag;
                if (!string.IsNullOrEmpty(r2Tag) && AvailableOutputPlcTags.Contains(r2Tag))
                    ComboSafetyAlarmR2Output.SelectedItem = r2Tag;
            }
            catch { }
        }

        // PC tarafından yönetilen bridge değişkenler (PLC'den geri okuma yapılmaz, sadece yazılır)
        private static readonly HashSet<string> _pcManagedVars = new() { "AKTUEL_RFID", "AKTUEL_KLIMA_INDEX", "SNIFFER_OLCUM_SURE", "NOKTA_SAPMA_LIMIT" };

        /// <summary>
        /// Tag adına göre PlcVariable'ı bulur.
        /// PlcService Input/Output + Robot Input/Output (R1:/R2: prefix) desteği.
        /// </summary>
        private PlcVariable FindTargetVariable(string tagName)
        {
            if (string.IsNullOrEmpty(tagName)) return null;

            // 1. R1:/R2: prefix kontrolü → Robot değişkeni
            if (tagName.Length > 3 && tagName[0] == 'R' && tagName[2] == ':')
            {
                if (int.TryParse(tagName.Substring(1, 1), out int robotNo) && robotNo >= 1)
                {
                    var robots = KukaRobotManager.Instance?.Robots;
                    if (robots != null && robotNo <= robots.Count)
                    {
                        var robot = robots[robotNo - 1];
                        string varName = tagName.Substring(3); // "R1:G_XXX" → "G_XXX"
                        return robot.InputVars.FirstOrDefault(v => v.Name == varName)
                            ?? robot.OutputVars.FirstOrDefault(v => v.Name == varName);
                    }
                }
            }

            // 2. Normal PlcService değişkeni
            return PlcService.Instance.InputVariables.FirstOrDefault(v => v.Name == tagName)
                ?? PlcService.Instance.OutputVariables.FirstOrDefault(v => v.Name == tagName);
        }

        private void ConnectToPlcVariable(PlcVariable localVar)
        {
            if (string.IsNullOrEmpty(localVar.PlcTag)) return;
            var sourceRealVar = FindTargetVariable(localVar.PlcTag);

            if (sourceRealVar != null)
            {
                bool isPcManaged = _pcManagedVars.Contains(localVar.Name);

                if (!isPcManaged)
                {
                    // Normal değişken: PLC'den oku (initial sync + polling)
                    localVar.Value = sourceRealVar.CurrentValue?.ToString();
                    sourceRealVar.PropertyChanged += (s, e) => { if (e.PropertyName == "CurrentValue") this.DispatcherQueue.TryEnqueue(() => localVar.Value = sourceRealVar.CurrentValue?.ToString()); };
                }
                // else: PC-managed değişken → PLC'den okuma yapma, değer GlobalData'dan gelir

                // Yazma (her iki tip için de): local değiştiğinde hedef değişkene yaz
                // Robot değişkenleri (R1:/R2:) için fiziksel robota yazma işlemi
                // CommunicationLoop tarafından otomatik yapılır (OutputVars dirty check)
                localVar.PropertyChanged += async (s, e) => {
                    if ((e.PropertyName == "CurrentValue" || e.PropertyName == "Value") && sourceRealVar.CurrentValue?.ToString() != localVar.CurrentValue?.ToString())
                    {
                        try
                        {
                            sourceRealVar.CurrentValue = localVar.CurrentValue;
                            System.Diagnostics.Debug.WriteLine($"[PLC_WRITE] {localVar.Name}→{localVar.PlcTag} = {localVar.CurrentValue}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[PLC_WRITE] {localVar.Name} yazma hatası: {ex.Message}");
                        }
                    }
                };
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[PLC_CONNECT] ⚠ Tag bulunamadı: {localVar.PlcTag} (localVar={localVar.Name})");
            }
        }

        /// <summary>
        /// İkinci PLC Tag bağlantısı — localVar değiştiğinde PlcTag2'ye de yazar.
        /// </summary>
        private void ConnectToPlcVariable2(PlcVariable localVar)
        {
            if (string.IsNullOrEmpty(localVar.PlcTag2)) return;
            var target2 = FindTargetVariable(localVar.PlcTag2);

            if (target2 != null)
            {
                // Robot değişkenleri (R1:/R2:) için fiziksel robota yazma işlemi
                // CommunicationLoop tarafından otomatik yapılır (OutputVars dirty check)
                localVar.PropertyChanged += async (s, e) => {
                    if ((e.PropertyName == "CurrentValue" || e.PropertyName == "Value") && target2.CurrentValue?.ToString() != localVar.CurrentValue?.ToString())
                    {
                        try
                        {
                            target2.CurrentValue = localVar.CurrentValue;
                            System.Diagnostics.Debug.WriteLine($"[PLC_WRITE2] {localVar.Name}→{localVar.PlcTag2} = {localVar.CurrentValue}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[PLC_WRITE2] {localVar.Name}→{localVar.PlcTag2} yazma hatası: {ex.Message}");
                        }
                    }
                };
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[PLC_CONNECT2] ⚠ Tag2 bulunamadı: {localVar.PlcTag2} (localVar={localVar.Name})");
            }
        }

        private void PlcTagComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb && cb.DataContext is PlcVariable v)
            {
                string newTag = cb.SelectedItem as string;

                // ═══ GUARD: ComboBox başlatma sırasında null overwrite'ı engelle ═══
                // ComboBox oluşurken ItemsSource henüz dolmamışsa SelectedItem=null olur
                // ve kaydedilmiş PlcTag'i silerek kalıcılığı bozar.
                if (string.IsNullOrEmpty(newTag) && !string.IsNullOrEmpty(v.PlcTag))
                {
                    System.Diagnostics.Debug.WriteLine($"[PLC_TAG_GUARD] {v.Name}: PlcTag={v.PlcTag} korundu (ComboBox null gönderdi)");
                    return;
                }

                if (v.PlcTag != newTag)
                {
                    System.Diagnostics.Debug.WriteLine($"[PLC_TAG] {v.Name}: PlcTag değişti '{v.PlcTag}' → '{newTag}'");
                    v.PlcTag = newTag;
                    GlobalData.SavePlcVariableTagsToFile();
                    ConnectToPlcVariable(v);
                }
            }
        }

        private void PlcTag2ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb && cb.DataContext is PlcVariable v)
            {
                string newTag2 = cb.SelectedItem as string;

                // ═══ GUARD: ComboBox başlatma sırasında null overwrite'ı engelle ═══
                if (string.IsNullOrEmpty(newTag2) && !string.IsNullOrEmpty(v.PlcTag2))
                {
                    System.Diagnostics.Debug.WriteLine($"[PLC_TAG2_GUARD] {v.Name}: PlcTag2={v.PlcTag2} korundu (ComboBox null gönderdi)");
                    return;
                }

                if (v.PlcTag2 != newTag2)
                {
                    System.Diagnostics.Debug.WriteLine($"[PLC_TAG2] {v.Name}: PlcTag2 değişti '{v.PlcTag2}' → '{newTag2}'");
                    v.PlcTag2 = newTag2;
                    GlobalData.SavePlcVariableTagsToFile();
                    ConnectToPlcVariable2(v);
                }
            }
        }

        // ═══ HAT OTO/MANUEL SWITCH (KONTROL PANELİNDEN) ═══
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
        // GENİŞLETİLDİ: GeneralInputVars + PlcService + Robot InputVars değişkenlerinde arar
        private bool IsConditionMet(string varName, bool expectedTrue)
        {
            // ══════ ROBOT VARPROXY SAFETY TAG KONTROLÜ ══════
            // $STOPMESS, $DRIVES_ON vb. tag'ler doğrudan robot property'lerinden okunur
            // Tüm bağlı robotlar için kontrol eder (herhangi birinde koşul sağlanmıyorsa false döner)
            var robotSafetyValue = GetRobotSafetyValue(varName);
            if (robotSafetyValue.HasValue)
            {
                return expectedTrue ? robotSafetyValue.Value : !robotSafetyValue.Value;
            }

            // ══════ ROBOT INPUT VARS (R1:xxx, R2:xxx prefix) ══════
            PlcVariable variable = null;

            if (varName.StartsWith("R") && varName.Contains(":"))
            {
                var parts = varName.Split(':', 2);
                if (int.TryParse(parts[0].Substring(1), out int ri) && ri >= 1)
                {
                    var robots = KukaRobotManager.Instance?.Robots;
                    if (robots != null && ri <= robots.Count)
                        variable = robots[ri - 1].InputVars.FirstOrDefault(v => v.Name == parts[1]);
                }
            }

            // 1. Önce GeneralInputVars'ta ara
            if (variable == null)
                variable = GeneralInputVars.FirstOrDefault(v => v.Name == varName);

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

        /// <summary>
        /// KUKA VarProxy safety tag'lerini doğrudan robot property'lerinden okur.
        /// Tüm bağlı robotlar için kontrol eder.
        /// $STOPMESS → EmergencyStop (ters mantık: TRUE=kötü)
        /// $DRIVES_ON → DrivesOn
        /// $USER_SAF → UserSafety
        /// $PERI_RDY → PeripheralReady
        /// $ALARM_STOP → AlarmStop (TRUE=iyi, FALSE=alarm)
        /// $ROB_RDY → RobotReady
        /// </summary>
        private bool? GetRobotSafetyValue(string tagName)
        {
            var robots = KukaRobotManager.Instance?.Robots;
            if (robots == null || robots.Count == 0) return null;

            // Tag adı eşleştirme (büyük/küçük harf duyarsız)
            string tag = tagName.Trim().ToUpper();

            Func<KukaRobotInstance, bool> getter = tag switch
            {
                "$STOPMESS" => r => !r.EmergencyStop,      // TRUE=stop aktif → koşul SAĞLANMAMIŞ
                "$DRIVES_ON" => r => r.DrivesOn,
                "$USER_SAF" => r => r.UserSafety,
                "$PERI_RDY" => r => r.PeripheralReady,
                "$ALARM_STOP" => r => r.AlarmStop,          // TRUE=OK, FALSE=alarm
                "$ROB_RDY" => r => r.RobotReady,
                _ => null
            };

            if (getter == null) return null;

            // Tüm bağlı robotlarda kontrol — herhangi birinde koşul sağlanmıyorsa false
            foreach (var robot in robots)
            {
                if (!robot.IsConnected) continue;
                if (!getter(robot)) return false;
            }

            return true;
        }

        // --- SAFETY KOŞUL KONTROLÜ (Sadece ALARM grubu → sistemi durdurur) ---
        private string CheckSafetyConditions()
        {
            var robots = KukaRobotManager.Instance?.Robots;
            if (robots == null) return null;

            // Sabit ALARM sinyalleri: Sadece $ALARM_STOP
            for (int i = 0; i < robots.Count; i++)
            {
                var r = robots[i];
                if (!r.IsConnected) continue;
                string prefix = $"R{i + 1}";

                if (!r.AlarmStop) return $"{prefix} ACİL STOP PASİF";
            }

            // Kullanıcı tanımlı ALARM koşulları
            foreach (var check in GlobalData.SafetyAlarmList)
            {
                if (!IsConditionMet(check.TagName, true))
                {
                    return check.ErrorMessage;
                }
            }

            return null; // Tüm ALARM koşulları OK
        }

        // --- CANLI DURUM VE ÖN KOŞUL KONTROLÜ ---
        private void UpdateLineStatusVisuals()
        {
            bool isRunning = IsConditionMet("LINE_RUNNING", true);
            string physicalErrorMessage = null;
            string safetyErrorMessage = null;

            // 0. SAFETY KOŞUL KONTROLÜ (Sabit robot sinyalleri + kullanıcı tanımlı)
            safetyErrorMessage = CheckSafetyConditions();

            // 1. FİZİKSEL DURUM KONTROLÜ (SystemCheckList + İstasyonlar)
            foreach (var check in GlobalData.SystemCheckList)
            {
                if (!IsConditionMet(check.TagName, true))
                {
                    physicalErrorMessage = check.ErrorMessage;
                    break; // İlk hatayı bul
                }
            }

            // 1b. ROBOT HATA KONTROLÜ
            if (string.IsNullOrEmpty(physicalErrorMessage))
            {
                physicalErrorMessage = CheckRobotAlarms();
            }

            // 1c. İSTASYON ALARM KONTROLÜ
            if (string.IsNullOrEmpty(physicalErrorMessage))
            {
                if (Stations.Any(s => s.HasAlarm))
                {
                    physicalErrorMessage = "İSTASYON ARIZASI (İSTASYONLARI KONTROL EDİN)";
                }
            }

            // 1d. SAFETY KOŞUL HATASI → fiziksel hataya yansıt
            if (string.IsNullOrEmpty(physicalErrorMessage) && safetyErrorMessage != null)
            {
                physicalErrorMessage = safetyErrorMessage;
            }

            // --- SAFETY SİNYAL LED GÜNCELLEMESİ ---
            UpdateSafetySignalLeds();

            // --- SAFETY OK GÖSTERGE GÜNCELLEMESİ ---
            if (safetyErrorMessage == null)
            {
                CheckSafetyBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 37, 40));
                CheckSafetyIcon.Glyph = "\uE7BA";
                CheckSafetyIcon.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 102, 102, 102));
                CheckSafetyText.Text = "OK";
                CheckSafetyText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 102, 102, 102));
            }
            else
            {
                CheckSafetyBorder.Background = new SolidColorBrush(Microsoft.UI.Colors.DarkRed);
                CheckSafetyIcon.Glyph = "\uE7BA";
                CheckSafetyIcon.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
                CheckSafetyText.Text = "HATA";
                CheckSafetyText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
            }

            // ═══ SAFETY ALARM ÇIKIŞ SİNYALİ ═══
            // Herhangi bir alarm koşulu tetiklendiğinde seçili tag'lere TRUE yaz
            WriteSafetyAlarmOutputs(safetyErrorMessage != null);

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
            // OTOMATİK MOD kontrolü - Manuel modda START verilemez
            if (!KontrolLineAutoManualSwitch.IsOn)
            {
                AddLog("BAŞLATILAMADI: Önce OTOMATİK mod seçilmelidir.", "Yellow");
                return;
            }

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

            // Reset öncesi: G_KLIMA_TIP değerini robotlara yeniden gönder
            // Robot KRL programı reset sırasında G_KLIMA_TIP'i dahili olarak sıfırlayabilir.
            // Bridge cache'deki değer aynı kaldığı için otomatik yeniden yazma tetiklenmez.
            // Bu yüzden reset öncesinde değeri zorla yeniden yazıyoruz.
            await ResendKlimaTipToRobots();

            // ═══ ROBOTLARA G_RESET SİNYALİ GÖNDER ═══
            // KRL programı WAIT FOR G_RESET==TRUE ile hata durumunda bekler.
            // Bu sinyal gönderilmezse robot sonsuza dek takılı kalır!
            var robots = KukaRobotManager.Instance?.Robots;
            if (robots != null)
            {
                for (int i = 0; i < robots.Count; i++)
                {
                    var robot = robots[i];
                    if (!robot.IsConnected) continue;

                    AddLog($"Robot {i + 1} G_RESET sinyali gönderiliyor...", "Orange");
                    var resetOutVar = robot.OutputVars.FirstOrDefault(v => v.Name == "G_RESET");
                    if (resetOutVar != null)
                    {
                        resetOutVar.CurrentValue = true;
                    }
                    // KukaVarProxy ile de doğrudan yaz (güvenlik için)
                    try { await robot.WriteVariableAsync("G_RESET", "TRUE"); } catch { }
                }

                await Task.Delay(500); // Robot KRL'nin WAIT FOR G_RESET==TRUE'yu yakalaması için

                for (int i = 0; i < robots.Count; i++)
                {
                    var robot = robots[i];
                    if (!robot.IsConnected) continue;

                    var resetOutVar = robot.OutputVars.FirstOrDefault(v => v.Name == "G_RESET");
                    if (resetOutVar != null)
                    {
                        resetOutVar.CurrentValue = false;
                    }
                    try { await robot.WriteVariableAsync("G_RESET", "FALSE"); } catch { }
                    AddLog($"Robot {i + 1} G_RESET tamamlandı", "Green");
                }
            }

            // PLC'ye Reset Sinyali Gönder
            var resetVar = GeneralOutputVars.FirstOrDefault(x => x.Name == "CMD_LINE_RESET");
            if (resetVar != null)
            {
                resetVar.CurrentValue = true;
                AddLog("Reset sinyali PLC'ye gönderiliyor...", "Orange");
                await Task.Delay(1000);
                resetVar.CurrentValue = false;
            }

            // Reset sonrası: G_KLIMA_TIP değerini bir kez daha gönder (KRL reset gecikmesi için)
            await Task.Delay(500);
            await ResendKlimaTipToRobots();

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

            // Robot hata kontrolü
            if (string.IsNullOrEmpty(remainingError))
            {
                remainingError = CheckRobotAlarms();
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

        // ═══ KASA TİPLERİ YÖNETİMİ ═══
        private void RefreshCasingTypesPanel()
        {
            CasingTypesPanel.Children.Clear();
            foreach (var ct in GlobalData.CasingTypes)
            {
                var badge = new Border
                {
                    Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 30, 60, 30)),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(8, 4, 8, 4)
                };
                var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                var idxText = new TextBlock { Text = ct.Index.ToString(), FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Microsoft.UI.Colors.Orange), FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
                var nameText = new TextBlock { Text = ct.Name, Foreground = new SolidColorBrush(Microsoft.UI.Colors.LimeGreen), FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
                var delBtn = new Button { Content = "X", FontSize = 9, Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent), Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red), Padding = new Thickness(4, 0, 4, 0), MinWidth = 20, MinHeight = 20, Tag = ct };
                delBtn.Click += BtnDeleteCasing_Click;
                sp.Children.Add(idxText);
                sp.Children.Add(nameText);
                sp.Children.Add(delBtn);
                badge.Child = sp;
                CasingTypesPanel.Children.Add(badge);
            }
        }

        private void BtnAddCasing_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNewCasingName.Text)) return;
            int nextIdx = GlobalData.CasingTypes.Count > 0 ? GlobalData.CasingTypes.Max(c => c.Index) + 1 : 1;
            GlobalData.CasingTypes.Add(new CasingType { Index = nextIdx, Name = TxtNewCasingName.Text.Trim() });
            GlobalData.SaveCasingTypes();
            TxtNewCasingName.Text = "";
            RefreshCasingTypesPanel();
        }

        private void BtnDeleteCasing_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is CasingType ct)
            {
                GlobalData.CasingTypes.Remove(ct);
                GlobalData.SaveCasingTypes();
                RefreshCasingTypesPanel();
            }
        }

        private void CasingLabel_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock tb && tb.DataContext is App4.Utilities.RfidDef rfid)
            {
                var ct = GlobalData.CasingTypes.FirstOrDefault(c => c.Index == rfid.CasingIndex);
                tb.Text = ct != null ? $"{ct.Index}-{ct.Name}" : "-";
                // Tiklayinca casing sec
                var parent = tb.Parent as Border;
                if (parent != null)
                {
                    parent.Tapped += (s, args) =>
                    {
                        // Sirali casing degistir
                        var types = GlobalData.CasingTypes.ToList();
                        int currentIdx = types.FindIndex(c => c.Index == rfid.CasingIndex);
                        int nextIdx = (currentIdx + 1) % (types.Count + 1); // +1 = atanmamis (0)
                        if (nextIdx < types.Count)
                        {
                            rfid.CasingIndex = types[nextIdx].Index;
                            tb.Text = $"{types[nextIdx].Index}-{types[nextIdx].Name}";
                        }
                        else
                        {
                            rfid.CasingIndex = 0;
                            tb.Text = "-";
                        }
                        GlobalData.SaveRfids();
                    };
                }
            }
        }

        private void BtnAddRfid_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TxtNewRfidId.Text))
            {
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
            if (sender is Button b && b.DataContext is App4.Utilities.RfidDef item)
            {
                KnownRfids.Remove(item);
            }
        }
        /// <summary>
        /// Reset sırasında G_KLIMA_TIP değerini tüm bağlı robotlara zorla yeniden gönderir.
        /// Robot KRL programı reset aldığında G_KLIMA_TIP'i dahili olarak 0'a temizler.
        /// Bridge cache'deki değer hala aynı olduğu için otomatik yazma tetiklenmez,
        /// bu yüzden değeri açıkça yeniden yazmamız gerekir.
        /// </summary>
        private async Task ResendKlimaTipToRobots()
        {
            try
            {
                int klimaIndex = GlobalData.AktuelKlimaIndex;
                if (klimaIndex <= 0) return;

                // ═══ ÖNCE: Slider hedef pozisyonu Robot 2'ye gönder ═══
                // G_SLIDER_HEDEF_POZ, G_KLIMA_TIP'ten ÖNCE yazılmalı.
                // Robot 2 KRL'de: G_KLIMA_TIP alınca CALISTIR'a girer, sonra G_SLIDER_HEDEF_POZ okur.
                await SendSliderPositionToRobot2Async();

                // ═══ SONRA: Klima tipini tüm robotlara gönder ═══
                var robots = KukaRobotManager.Instance?.Robots;
                if (robots == null) return;

                foreach (var robot in robots)
                {
                    if (robot.IsConnected)
                    {
                        try
                        {
                            await robot.WriteVariableAsync("G_KLIMA_TIP", klimaIndex.ToString());
                            // Robot Output var cache'ini de güncelle ki bridge "aynı değer" diye atlamamasın
                            var outputVar = robot.OutputVars.FirstOrDefault(v => v.PlcTag == "G_KLIMA_TIP");
                            if (outputVar != null) outputVar.Value = klimaIndex.ToString();
                            AddLog($"[{robot.Name}] G_KLIMA_TIP={klimaIndex} yeniden gönderildi", "Cyan");
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Hedef istasyonun pozisyonunu (mm) Robot 2'ye G_SLIDER_HEDEF_POZ olarak yazar.
        /// Pozisyon değeri Manuel sayfadaki KL100_Station1/2/3Pos'tan alınır.
        /// </summary>
        /// <summary>
        /// Hedef istasyon numarasını ve pozisyonunu Robot 2'ye gönderir.
        /// G_HEDEF_ISTASYON (INT) = İstasyon numarası (1, 2, 3, 4=Bakım)
        /// G_SLIDER_HEDEF_POZ (REAL) = İstasyon mm pozisyonu (uyumluluk için)
        /// Robot 2 KRL: G_HEDEF_ISTASYON ile XHOME1/2/3/BAKIM seçer
        /// </summary>
        private async Task SendSliderPositionToRobot2Async()
        {
            try
            {
                int targetStation = GlobalData.TargetSliderStation;
                if (targetStation < 1 || targetStation > 4) return;

                var robots = KukaRobotManager.Instance?.Robots;
                if (robots == null || robots.Count < 2) return;

                // Robot 2 = index 1 (slider'ı kontrol eden robot)
                var robot2 = robots[1];
                if (!robot2.IsConnected) return;

                // ═══ 1. G_HEDEF_ISTASYON (INT) → Robot 2'ye istasyon numarası yaz ═══
                // Robot KRL bu değere göre XHOME1/2/3/BAKIM seçer
                await robot2.WriteVariableAsync("G_HEDEF_ISTASYON", targetStation.ToString());
                var istVar = robot2.OutputVars.FirstOrDefault(v => v.PlcTag == "G_HEDEF_ISTASYON");
                if (istVar != null) istVar.Value = targetStation.ToString();

                // ═══ 2. G_SLIDER_HEDEF_POZ (REAL) → mm pozisyonu da yaz (uyumluluk) ═══
                double position = GlobalData.GetStationSliderPosition(targetStation);
                string posStr = position.ToString(System.Globalization.CultureInfo.InvariantCulture);
                await robot2.WriteVariableAsync("G_SLIDER_HEDEF_POZ", posStr);
                var posVar = robot2.OutputVars.FirstOrDefault(v => v.PlcTag == "G_SLIDER_HEDEF_POZ");
                if (posVar != null) posVar.Value = posStr;

                // ═══ 3. PLC değişkenlerini güncelle (görsel senkronizasyon) ═══
                var hedefPozVar = GlobalData.GeneralOutputVars.FirstOrDefault(v => v.Name == "KL100_HEDEF_POZ");
                if (hedefPozVar != null)
                {
                    hedefPozVar.Value = posStr;
                    hedefPozVar.CurrentValue = position;
                }

                var hedefIstVar = GlobalData.GeneralOutputVars.FirstOrDefault(v => v.Name == "KL100_HEDEF_ISTASYON")
                               ?? GlobalData.GeneralInputVars.FirstOrDefault(v => v.Name == "KL100_HEDEF_ISTASYON");
                if (hedefIstVar != null)
                {
                    hedefIstVar.Value = targetStation.ToString();
                    hedefIstVar.CurrentValue = targetStation;
                }

                string stationName = targetStation == 4 ? "BAKIM" : $"İstasyon {targetStation}";
                AddLog($"[Robot 2] G_HEDEF_ISTASYON={targetStation} ({stationName}), G_SLIDER_HEDEF_POZ={position:F1} mm", "Cyan");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SendSliderPositionToRobot2 HATA: {ex.Message}");
            }
        }

        private void BtnClearLogs_Click(object sender, RoutedEventArgs e) => SystemLogs.Clear();

        private void AddLog(string msg, string clr) => SystemLogs.Insert(0, new App4.Utilities.LogEntry { TimeStr = DateTime.Now.ToString("HH:mm:ss"), Message = msg, ColorCode = clr });
        private void UpdatePlcVar(ObservableCollection<PlcVariable> c, string n, string v) { var i = c.FirstOrDefault(x => x.Name == n); if (i != null && i.Value != v) i.Value = v; }
        private void UpdateSliderPosition(string v)
        {
            foreach (var s in Stations) s.IsRobotPresent = false;
            if (int.TryParse(v, out int p) && p >= 1 && p <= 3) Stations[p - 1].IsRobotPresent = true;
            // Slider pozisyonu değişti → AKTUEL_RFID ve INDEX güncelle
            UpdateAktuelRfidFromStation();
        }

        /// <summary>
        /// Slider hangi istasyondaysa, o istasyonun moduna göre AKTUEL_RFID ve AKTUEL_KLIMA_INDEX günceller.
        /// Specific mod → Beklenen ID (TargetRfid), Mixed mod → Okunan ID (CurrentRfid)
        /// </summary>
        private void UpdateAktuelRfidFromStation()
        {
            try
            {
                // 1. Slider hangi istasyonda?
                // Ana kaynak: GlobalData.GetSliderStationNumber() (robot sinyalinden hesaplanır)
                Utilities.ExtendedStationViewModel activeStation = null;

                int sliderStation = GlobalData.GetSliderStationNumber();
                if (sliderStation >= 1 && sliderStation <= 3)
                {
                    activeStation = Stations[sliderStation - 1] as Utilities.ExtendedStationViewModel;
                }

                // Fallback: IsRobotPresent'a bak
                if (activeStation == null)
                {
                    var baseStation = Stations.FirstOrDefault(s => s.IsRobotPresent);
                    if (baseStation != null)
                        activeStation = baseStation as Utilities.ExtendedStationViewModel;
                }

                // Fallback 2: SLIDER_POS_ACT değişkeninden doğrudan oku
                if (activeStation == null)
                {
                    var sliderVar = GeneralInputVars.FirstOrDefault(v => v.Name == "SLIDER_POS_ACT");
                    string sliderVal = sliderVar?.Value ?? sliderVar?.CurrentValue?.ToString();
                    if (int.TryParse(sliderVal, out int pos) && pos >= 1 && pos <= 3)
                    {
                        activeStation = Stations[pos - 1] as Utilities.ExtendedStationViewModel;
                    }
                }

                if (activeStation == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AKTUEL_RFID] ❌ Aktif istasyon bulunamadı! SliderStation={sliderStation}, IsRobotPresent={Stations.Any(s => s.IsRobotPresent)}");
                    return;
                }

                // 1.5. Aktif istasyonu hedef slider istasyonu olarak kaydet
                int stationIndex = Stations.IndexOf(activeStation);
                if (stationIndex >= 0)
                {
                    GlobalData.TargetSliderStation = stationIndex + 1;
                    System.Diagnostics.Debug.WriteLine($"[SLIDER] TargetSliderStation = {stationIndex + 1} ({activeStation.Name})");
                }

                // 2. İstasyonun moduna göre RFID belirle
                string aktuelRfid = "";
                if (activeStation.RfidOpMode == Utilities.RfidOperationMode.Specific)
                {
                    // Specific mod → Beklenen ID listbox'ta ne seçildiyse o
                    aktuelRfid = activeStation.TargetRfid ?? "";
                }
                else
                {
                    // Mixed mod → Okunan RFID
                    aktuelRfid = activeStation.CurrentRfid ?? "";
                }

                System.Diagnostics.Debug.WriteLine($"[AKTUEL_RFID] Station={activeStation.Name}, Mode={activeStation.RfidOpMode}, RFID='{aktuelRfid}'");

                // 3. GlobalData üzerinden yaz (hem UI tablosu hem PlcService hem static erişim)
                GlobalData.AktuelRfid = aktuelRfid;

                // 4. AKTUEL_KLIMA_INDEX → KnownRfids listesinde kaçıncı sırada (1-based)
                int idx = 0;
                if (!string.IsNullOrEmpty(aktuelRfid))
                {
                    var knownList = GlobalData.KnownRfids.ToList();
                    int k = knownList.FindIndex(r => r.Id == aktuelRfid);
                    idx = (k >= 0) ? k + 1 : 0;
                }
                GlobalData.AktuelKlimaIndex = idx;

                // 5. Slider hedef pozisyonunu Robot 2'ye gönder (istasyon değiştiğinde)
                if (idx > 0 && GlobalData.TargetSliderStation >= 1)
                {
                    _ = SendSliderPositionToRobot2Async();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AKTUEL_RFID] UpdateAktuelRfidFromStation HATA: {ex.Message}");
            }
        }
        private void UpdateStationStatus(string n, string v) { foreach (var s in Stations) { if (s.StatusTag == n) s.ProcessStatus = MapStatus(v);// Başına ünlem (!) koyarak tersini alıyoruz.
                                                                                                                                                  // IsTrue(v) 1 dönerse (True), ! işareti onu False yapar (Alarm Yok).
                                                                                                                                                  // IsTrue(v) 0 dönerse (False), ! işareti onu True yapar (Alarm Var).
                else if (s.AlarmTag == n) s.HasAlarm = !IsTrue(v); else if (s.ProducingTag == n) s.IsProducing = IsTrue(v); else if (s.ProductionCountTag == n) s.ProductionCount = v; else if (s.EfficiencyTag == n) s.Efficiency = v.Contains("%") ? v : "%" + v; else if (s.CurrentRfidTag == n) s.CurrentRfid = v; } }
        private bool IsTrue(string v) => !string.IsNullOrEmpty(v) && (v.ToUpper() == "TRUE" || v == "1" || v == "ON");

        /// <summary>
        /// Robot 2 HOME sinyaline göre hedef istasyonun ev ikonunu günceller.
        /// TargetSliderStation (1-3) ile hangi istasyonda HOME olduğu belirlenir.
        /// </summary>
        private void UpdateRobotHomeOnStation(bool isHome)
        {
            int targetSt = GlobalData.TargetSliderStation; // 1-based
            for (int i = 0; i < Stations.Count; i++)
            {
                // Sadece hedef istasyon HOME, diğerleri değil
                Stations[i].IsRobotHome = isHome && (i + 1 == targetSt);
            }
        }

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

        // ═══ SAFETY ALARM EKLEME / SİLME ═══
        private void BtnAddSafetyAlarm_Click(object sender, RoutedEventArgs e)
        {
            if (ComboSafetyAlarmTag.SelectedItem is not string selectedTag)
            {
                AddLog("HATA: Lütfen listeden bir PLC Input seçiniz.", "Orange");
                return;
            }
            if (string.IsNullOrEmpty(TxtSafetyAlarmMsg.Text))
            {
                AddLog("HATA: Lütfen bir alarm mesajı yazınız.", "Orange");
                return;
            }
            try
            {
                GlobalData.SafetyAlarmList.Add(new App4.Utilities.SafetyCheckItem
                {
                    TagName = selectedTag,
                    ErrorMessage = TxtSafetyAlarmMsg.Text.ToUpper()
                });
                GlobalData.SaveSafetyAlarms();
                TxtSafetyAlarmMsg.Text = "";
                UpdateLineStatusVisuals();
                AddLog($"Safety Alarm Eklendi: {selectedTag}", "Green");
            }
            catch (Exception ex)
            {
                AddLog($"Ekleme Hatası: {ex.Message}", "Red");
            }
        }

        private void BtnDeleteSafetyAlarm_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.DataContext is App4.Utilities.SafetyCheckItem item)
            {
                GlobalData.SafetyAlarmList.Remove(item);
                GlobalData.SaveSafetyAlarms();
                UpdateLineStatusVisuals();
            }
        }

        // ═══ SAFETY UYARI EKLEME / SİLME ═══
        private void BtnAddSafetyWarning_Click(object sender, RoutedEventArgs e)
        {
            if (ComboSafetyWarningTag.SelectedItem is not string selectedTag)
            {
                AddLog("HATA: Lütfen listeden bir PLC Input seçiniz.", "Orange");
                return;
            }
            if (string.IsNullOrEmpty(TxtSafetyWarningMsg.Text))
            {
                AddLog("HATA: Lütfen bir uyarı mesajı yazınız.", "Orange");
                return;
            }
            try
            {
                GlobalData.SafetyWarningList.Add(new App4.Utilities.SafetyCheckItem
                {
                    TagName = selectedTag,
                    ErrorMessage = TxtSafetyWarningMsg.Text.ToUpper()
                });
                GlobalData.SaveSafetyWarnings();
                TxtSafetyWarningMsg.Text = "";
                AddLog($"Safety Uyarı Eklendi: {selectedTag}", "Green");
            }
            catch (Exception ex)
            {
                AddLog($"Ekleme Hatası: {ex.Message}", "Red");
            }
        }

        private void BtnDeleteSafetyWarning_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.DataContext is App4.Utilities.SafetyCheckItem item)
            {
                GlobalData.SafetyWarningList.Remove(item);
                GlobalData.SaveSafetyWarnings();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // ROBOT DURUM İZLEME PANELİ
        // ═══════════════════════════════════════════════════════════════

        // NOT: Robot sinyal tetiklemeleri (G_OLCUM_TETIK, G_SNIFFER_OLCUM_TETIK)
        // artık GlobalData.StartRobotSignalMonitoring() üzerinden sayfa bağımsız çalışır.

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

                // InputVars değişikliklerini dinle (Sadece UI güncelleme)
                // NOT: G_OLCUM_TETIK/G_SNIFFER_OLCUM_TETIK artık GlobalData'dan dinleniyor
                foreach (var v in robots[0].InputVars)
                {
                    v.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(PlcVariable.Value))
                        {
                            this.DispatcherQueue.TryEnqueue(() => UpdateRobotStatusPanel(robots[0], 1));
                            // Alarm sinyalleri değiştiğinde alarm sistemini güncelle
                            if (v.Name == "G_HATA_VAR" || v.Name == "G_ROBOT_DURUM" || v.Name == "G_HATA_KODU")
                                this.DispatcherQueue.TryEnqueue(() => UpdateLineStatusVisuals());
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
                            // Alarm sinyalleri değiştiğinde alarm sistemini güncelle
                            if (v.Name == "G_HATA_VAR" || v.Name == "G_ROBOT_DURUM" || v.Name == "G_HATA_KODU")
                                this.DispatcherQueue.TryEnqueue(() => UpdateLineStatusVisuals());
                        }
                    };
                }
            }

            // --- GeneralInputVars sniffer tetik sinyalleri değiştiğinde ellipse güncelle ---
            foreach (var gv in GlobalData.GeneralInputVars)
            {
                if (gv.Name == "R1_SNIFFER_TETIK" || gv.Name == "R2_SNIFFER_TETIK")
                {
                    gv.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(PlcVariable.Value))
                        {
                            this.DispatcherQueue.TryEnqueue(() =>
                            {
                                if (gv.Name == "R1_SNIFFER_TETIK")
                                    UpdateSnifferEllipseFromGeneralInput("R1_SNIFFER_TETIK", R1RobotEllipse, isR1: true);
                                else
                                    UpdateSnifferEllipseFromGeneralInput("R2_SNIFFER_TETIK", R2RobotEllipse, isR1: false);
                            });
                        }
                    };
                }
            }

            // --- INFICON SNIFFER READY sinyalini tüm robotlara yaz ---
            // Robot bağlantısı kurulduğunda INFICON hazır sinyali gönderilir
            // Robotlar bu sinyali kontrol ederek sniffer tetik verebilir
            _ = SetSnifferReadyOnAllRobotsAsync(robots);
        }

        private async Task SetSnifferReadyOnAllRobotsAsync(System.Collections.ObjectModel.ObservableCollection<KukaRobotInstance> robots)
        {
            // Kısa gecikme - robot bağlantısı stabil olsun
            await Task.Delay(2000);
            foreach (var r in robots)
            {
                if (r.IsConnected)
                {
                    try
                    {
                        await r.WriteVariableAsync("G_SNIFFER_READY", "TRUE");
                        AddAutoLog($"[Robot] INFICON READY sinyali gönderildi ({r.Name ?? "?"})");
                    }
                    catch { }
                }
            }
            // GlobalData'yı da güncelle
            var gReady = GlobalData.RobotOutputVars.FirstOrDefault(v => v.Name == "G_SNIFFER_READY");
            if (gReady != null) gReady.Value = "TRUE";
        }

        // =====================================================
        // ROBOT SİNYAL TETİKLEME → GlobalData'ya taşındı
        // HandleOlcumTetikAsync → GlobalData.HandleOlcumTetikAsync
        // HandleSnifferOlcumAsync → GlobalData.HandleSnifferOlcumAsync
        // Sayfa bağımsız çalışır, hangi sayfada olursa olsun tetik alır
        // =====================================================

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

        // =====================================================
        // ROBOT ALARM KONTROLÜ
        // Robot G_HATA_VAR veya G_ROBOT_DURUM=2 ise alarm döndürür
        // =====================================================
        private string CheckRobotAlarms()
        {
            var robots = KukaRobotManager.Instance?.Robots;
            if (robots == null) return null;

            for (int i = 0; i < robots.Count; i++)
            {
                var robot = robots[i];
                if (!robot.IsConnected) continue;

                string GetVar(string name) => robot.InputVars.FirstOrDefault(v => v.Name == name)?.Value ?? "";

                bool hataVar = GetVar("G_HATA_VAR")?.ToUpper() == "TRUE" || GetVar("G_HATA_VAR") == "1";
                int robotDurum = int.TryParse(GetVar("G_ROBOT_DURUM"), out var rd) ? rd : -1;
                int hataKodu = int.TryParse(GetVar("G_HATA_KODU"), out var hk) ? hk : 0;

                if (hataVar || robotDurum == 2)
                {
                    string hataDesc = hataKodu switch
                    {
                        5 => "Tabla Timeout",
                        6 => "Olcum Timeout",
                        8 => "Diger Robot Hatasi",
                        50 => "Boru Olcum NOK",
                        51 => "Boru Olcum Timeout",
                        52 => "Hedef Nokta Limit Asildi",
                        99 => "Genel Hata",
                        _ => hataKodu > 0 ? $"Kod: {hataKodu}" : "Hata Var"
                    };
                    return $"ROBOT {i + 1} HATASI ({hataDesc})";
                }
            }
            return null;
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

                // İstasyon + Aktif Nokta bilgisi ekle
                int hedefIst = int.TryParse(GetVar("G_HEDEF_ISTASYON"), out var hi) ? hi : 0;
                string istNoktaInfo = "";
                if (hedefIst > 0)
                    istNoktaInfo += $"İST-{hedefIst}";
                if (aktifNokta > 0)
                    istNoktaInfo += (istNoktaInfo.Length > 0 ? " | " : "") + $"Nokta: {aktifNokta}/{toplamNokta}";
                if (!string.IsNullOrEmpty(istNoktaInfo))
                    mesajText = $"[{istNoktaInfo}] {mesajText}";

                // Hata metni
                string hataText = "Yok";
                string hataColor = "#4CAF50";
                if (hataVar || hataKodu > 0)
                {
                    hataText = hataKodu switch
                    {
                        5 => "Tabla Timeout!",
                        6 => "Olcum Timeout!",
                        8 => "Diger Robot Hatasi!",
                        10 => "Gocator Timeout!",
                        20 => robotNo == 1 ? "Inficon Timeout!" : "Sniffer Timeout!",
                        99 => "Gecersiz Klima Tipi!",
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

                // Hata badge arkaplan rengi
                string hataBadgeBg = (hataVar || hataKodu > 0) ? "#2A0A0A" : "#1A2A1A";
                // Kart border rengi (hata varsa kırmızı, çalışıyorsa turuncu, normal gri)
                string cardBorderColor = (hataVar || robotDurum == 2) ? "#F44336" :
                    (robotDurum == 1 || robotDurum >= 10) ? "#FF9800" :
                    robot.IsConnected ? "#333" : "#222";

                // İstasyon kartına robot bilgisi aktar
                string stationDurum = durumText;
                string stationMesaj = mesajText;

                var durumBrush = StationViewModel.HexToBrush(durumColor);
                var dotBrush = StationViewModel.HexToBrush(dotColor);
                var defaultDurumBrush = StationViewModel.HexToBrush("#888888");
                var defaultDotBrush = StationViewModel.HexToBrush("#555555");

                // R2 hedef istasyonu belirliyor, R1 de aynı istasyonda çalışıyor
                // R1 için hedefIst yoksa R2'nin son bilinen hedefini kullan
                int aktifIst;
                if (robotNo == 2)
                {
                    aktifIst = (hedefIst >= 1 && hedefIst <= 3) ? hedefIst : _r2LastHedefIst;
                    if (aktifIst != _r2LastHedefIst && _r2LastHedefIst >= 1 && _r2LastHedefIst <= 3)
                    {
                        // Eski istasyondaki her iki robot bilgisini temizle
                        var oldSt = Stations[_r2LastHedefIst - 1];
                        oldSt.R1DurumText = ""; oldSt.R1MesajText = "";
                        oldSt.R1DurumColor = defaultDurumBrush; oldSt.R1StatusDotColor = defaultDotBrush;
                        oldSt.R2DurumText = ""; oldSt.R2MesajText = "";
                        oldSt.R2DurumColor = defaultDurumBrush; oldSt.R2StatusDotColor = defaultDotBrush;
                    }
                    if (aktifIst >= 1 && aktifIst <= 3)
                    {
                        var st = Stations[aktifIst - 1];
                        st.R2DurumText = stationDurum;
                        st.R2MesajText = stationMesaj;
                        st.R2DurumColor = durumBrush;
                        st.R2StatusDotColor = dotBrush;
                        _r2LastHedefIst = aktifIst;
                    }
                }
                else
                {
                    // R1 — R2'nin hedefini kullan
                    aktifIst = _r2LastHedefIst;
                    if (aktifIst >= 1 && aktifIst <= 3)
                    {
                        var st = Stations[aktifIst - 1];
                        st.R1DurumText = stationDurum;
                        st.R1MesajText = stationMesaj;
                        st.R1DurumColor = durumBrush;
                        st.R1StatusDotColor = dotBrush;
                    }
                }

                // UI Guncelle
                if (robotNo == 1)
                {
                    SetTextSafe(Robot1DurumText, durumText, durumColor);
                    SetTextSafe(Robot1MesajText, mesajText, "#666");
                    SetTextSafe(Robot1HataText, $"Hata: {hataText}", hataColor);
                    SetDotColor(Robot1StatusDot, dotColor);
                    SetBorderBgSafe(Robot1HataBadge, hataBadgeBg);
                    SetBorderColorSafe(Robot1StatusCard, cardBorderColor);
                    // Sniffer tetik → R1 robot sembolü renklendirme (GeneralInputVars'dan okur)
                    UpdateSnifferEllipseFromGeneralInput("R1_SNIFFER_TETIK", R1RobotEllipse, isR1: true);
                }
                else
                {
                    SetTextSafe(Robot2DurumText, durumText, durumColor);
                    SetTextSafe(Robot2MesajText, mesajText, "#666");
                    SetTextSafe(Robot2HataText, $"Hata: {hataText}", hataColor);
                    SetDotColor(Robot2StatusDot, dotColor);
                    SetBorderBgSafe(Robot2HataBadge, hataBadgeBg);
                    SetBorderColorSafe(Robot2StatusCard, cardBorderColor);
                    // Sniffer tetik → R2 robot sembolü renklendirme (GeneralInputVars'dan okur)
                    UpdateSnifferEllipseFromGeneralInput("R2_SNIFFER_TETIK", R2RobotEllipse, isR1: false);
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
                case 1: return "Klima tipi bekleniyor";
                case 2: return "İş tamamlandı";
                case 3: return "HATA - Reset bekleniyor";
                case 4: return "Oto mod - Start bekleniyor";
                case 8: return "Manuel mod";
                case 70: return "Hazır istasyon bekleniyor";
                case 71: return "İstasyon 1 seçildi - çalışılıyor";
                case 72: return "İstasyon 2 seçildi - çalışılıyor";
                case 73: return "İstasyon 3 seçildi - çalışılıyor";
                case 74: return "Sonraki → İstasyon 1'e geçiliyor";
                case 75: return "Sonraki → İstasyon 2'ye geçiliyor";
                case 76: return "Sonraki → İstasyon 3'e geçiliyor";
                case 99: return "HATA - Bilinmeyen klima tipi";
            }

            // Robot 1 özel kodlar
            if (robotNo == 1)
            {
                switch (mesaj)
                {
                    case 5: return "HATA - Tabla ölçüm başarısız";
                    case 20: return "Slider pozisyon bekleniyor";
                    case 21: return "Slider pozisyona ulaştı";
                    case 50: return "Tabla ölçüm pozisyonuna gidiliyor";
                    case 51: return "Tabla ölçüm bekleniyor";
                    case 52: return "Tabla ölçüm OK";
                    case 60: return "Robot 2 hatada, bekleniyor";
                    case 61: return "Robot 2 hatası nedeniyle durdu";
                    case 65: return "Robot 2 iş bitişi bekleniyor";
                }
            }

            // Robot 2 özel kodlar
            if (robotNo == 2)
            {
                switch (mesaj)
                {
                    case 5: return "HATA - Tabla offset alınamadı";
                    case 10: return "Robot 1'den tabla offset bekleniyor";
                    case 11: return "Tabla offset alındı";
                    case 20: return "Slider istasyona gidiyor";
                    case 21: return "Slider pozisyona ulaştı";
                    case 60: return "Robot 1 hatada, bekleniyor";
                    case 61: return "Robot 1 hatası nedeniyle durdu";
                    case 62: return "Robot 1 Home bekleniyor";
                    case 63: return "HATA - Geçersiz istasyon numarası";
                }
            }

            // Klima nokta kodlari: (N+1)*100 serisi — Nokta 1=200, Nokta 2=300, ... Nokta 15=1600
            if (mesaj >= 100 && mesaj <= 1699)
            {
                int tipNo = mesaj / 100;
                int alt = mesaj % 100;
                int nokta = tipNo - 1;

                if (robotNo == 1)
                {
                    // Robot 1: +0=geçiş, +10=gocator tetik, +1=hedefe gidiyor, +2=sniffer koklama, +3=tamamlandı
                    return alt switch
                    {
                        0 => $"Nokta {nokta}: Geçiş pozisyonuna gidiyor",
                        10 => $"Nokta {nokta}: Gocator ölçüm tetiklendi",
                        1 => $"Nokta {nokta}: Hedefe gidiyor + Sniffer",
                        2 => $"Nokta {nokta}: Sniffer koklama yapılıyor",
                        3 => $"Nokta {nokta}: Tamamlandı",
                        4 => $"Nokta {nokta}: NOK sonuç",
                        _ => $"Nokta {nokta}: Kod {alt}"
                    };
                }
                else
                {
                    // Robot 2: N*100=geçiş, +1..+7=çizgi no
                    if (alt == 0) return $"Nokta {nokta}: Geçiş pozisyonu";
                    if (alt >= 1 && alt <= 7) return $"Nokta {nokta}: Çizgi {alt} sniffer tarama";
                    return $"Nokta {nokta}: Kod {alt}";
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

        private void SetBorderBgSafe(Border border, string color)
        {
            if (border == null) return;
            try { border.Background = new SolidColorBrush(ParseHexColor(color)); } catch { }
        }

        private void SetBorderColorSafe(Border border, string color)
        {
            if (border == null) return;
            try { border.BorderBrush = new SolidColorBrush(ParseHexColor(color)); } catch { }
        }

        /// <summary>
        /// GeneralInputVars'daki sniffer tetik sinyaline göre robot ellipse + ikon rengini günceller.
        /// Tetik aktifken: Yeşil arka plan + Beyaz robot ikonu
        /// Tetik pasifken: Koyu arka plan + Orijinal renk (R1=Yeşil, R2=Mavi)
        /// </summary>
        private void UpdateSnifferEllipseFromGeneralInput(string varName, Microsoft.UI.Xaml.Shapes.Ellipse ellipse, bool isR1)
        {
            if (ellipse == null) return;
            try
            {
                // GeneralInputVars tablosundan sniffer tetik değerini oku
                var snifferVar = GlobalData.GeneralInputVars.FirstOrDefault(v => v.Name == varName);
                bool snifferActive = snifferVar?.Value?.ToUpper() == "TRUE" || snifferVar?.Value == "1";

                // İlgili robot ikonunu bul
                var robotIcon = isR1 ? R1RobotIcon : R2RobotIcon;

                if (snifferActive)
                {
                    // Sniffer aktif → Yeşil parlak arka plan
                    var activeBrush = new LinearGradientBrush();
                    activeBrush.StartPoint = new Windows.Foundation.Point(0, 0);
                    activeBrush.EndPoint = new Windows.Foundation.Point(1, 1);
                    activeBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 76, 175, 80), Offset = 0 });   // #4CAF50
                    activeBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 46, 125, 50), Offset = 1 });   // #2E7D32
                    ellipse.Fill = activeBrush;

                    // Robot ikonu beyaz
                    if (robotIcon != null)
                        robotIcon.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
                }
                else
                {
                    // Sniffer pasif → Orijinal koyu gradyan
                    var normalBrush = new LinearGradientBrush();
                    normalBrush.StartPoint = new Windows.Foundation.Point(0, 0);
                    normalBrush.EndPoint = new Windows.Foundation.Point(1, 1);
                    normalBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 42, 42, 42), Offset = 0 });    // #2A2A2A
                    normalBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 26, 26, 26), Offset = 1 });    // #1A1A1A
                    ellipse.Fill = normalBrush;

                    // Robot ikonu orijinal renk (R1=Yeşil, R2=Mavi)
                    if (robotIcon != null)
                    {
                        var originalColor = isR1
                            ? Windows.UI.Color.FromArgb(255, 76, 175, 80)    // #4CAF50 (R1 Yeşil)
                            : Windows.UI.Color.FromArgb(255, 0, 120, 212);   // #0078D4 (R2 Mavi)
                        robotIcon.Foreground = new SolidColorBrush(originalColor);
                    }
                }
            }
            catch { }
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


        // ═══ SAFETY ÇALIŞMA KOŞULLARI PANELİ ═══
        private void InitializeSafetyConditionsPanel()
        {
            var robots = KukaRobotManager.Instance.Robots;

            foreach (var robot in robots)
            {
                robot.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName is nameof(KukaRobotInstance.DrivesOn)
                        or nameof(KukaRobotInstance.AlarmStop)
                        or nameof(KukaRobotInstance.EmergencyStop)
                        or nameof(KukaRobotInstance.UserSafety)
                        or nameof(KukaRobotInstance.PeripheralReady))
                    {
                        this.DispatcherQueue.TryEnqueue(() => UpdateSafetySignalLeds());
                    }
                };
            }

            UpdateSafetySignalLeds();
        }

        /// <summary>
        /// Safety sinyal LED'lerini robotların anlık durumuna göre günceller.
        /// Hem PropertyChanged hem UpdateLineStatusVisuals'dan çağrılır.
        /// </summary>
        // ═══ AKTİF NOKTA BİLGİSİ — Robot G_AKTIF_NOKTA_ADI → İstasyon kartları ═══
        private void UpdateActivePoints()
        {
            try
            {
                var robots = KukaRobotManager.Instance?.Robots;
                if (robots == null || robots.Count == 0) return;

                // Robot 1 aktif nokta
                string r1Point = "";
                if (robots.Count >= 1)
                {
                    var v = robots[0].InputVars.FirstOrDefault(x => x.Name == "G_AKTIF_NOKTA_ADI");
                    r1Point = v?.Value ?? "";
                    if (r1Point == "-") r1Point = "";
                }

                // Robot 2 aktif nokta
                string r2Point = "";
                if (robots.Count >= 2)
                {
                    var v = robots[1].InputVars.FirstOrDefault(x => x.Name == "G_AKTIF_NOKTA_ADI");
                    r2Point = v?.Value ?? "";
                    if (r2Point == "-") r2Point = "";
                }

                // Tüm istasyonlara yaz
                foreach (var station in Stations)
                {
                    station.R1ActivePoint = r1Point;
                    station.R2ActivePoint = r2Point;
                }
            }
            catch { }
        }

        private void UpdateSafetySignalLeds()
        {
            try
            {
                var robots = KukaRobotManager.Instance?.Robots;
                if (robots == null) return;

                if (robots.Count > 0)
                {
                    var r1 = robots[0];
                    UpdateSafetyRow(SafeAutoR1DrivesLed, SafeAutoR1DrivesText, r1.DrivesOn, "ON", "OFF", false);
                    UpdateSafetyRow(SafeAutoR1EstopLed, SafeAutoR1EstopText, r1.AlarmStop, "AKTİF", "PASİF", false);
                    UpdateSafetyRow(SafeAutoR1UserSafLed, SafeAutoR1UserSafText, r1.UserSafety, "OK", "Uyarı", false);
                    UpdateSafetyRow(SafeAutoR1PeriLed, SafeAutoR1PeriText, r1.PeripheralReady, "Hazır", "Bekle", false);
                }
                if (robots.Count > 1)
                {
                    var r2 = robots[1];
                    UpdateSafetyRow(SafeAutoR2DrivesLed, SafeAutoR2DrivesText, r2.DrivesOn, "ON", "OFF", false);
                    UpdateSafetyRow(SafeAutoR2EstopLed, SafeAutoR2EstopText, r2.AlarmStop, "AKTİF", "PASİF", false);
                    UpdateSafetyRow(SafeAutoR2UserSafLed, SafeAutoR2UserSafText, r2.UserSafety, "OK", "Uyarı", false);
                    UpdateSafetyRow(SafeAutoR2PeriLed, SafeAutoR2PeriText, r2.PeripheralReady, "Hazır", "Bekle", false);
                }
            }
            catch { }
        }

        private void UpdateSafetyRow(Microsoft.UI.Xaml.Shapes.Ellipse led, TextBlock text, bool value, string trueText, string falseText, bool invertColor)
        {
            if (led == null || text == null) return;
            bool isGood = invertColor ? !value : value;
            var color = isGood
                ? Windows.UI.Color.FromArgb(255, 76, 175, 80)   // yeşil
                : Windows.UI.Color.FromArgb(255, 244, 67, 54);  // kırmızı

            led.Fill = new SolidColorBrush(color);
            text.Text = value ? trueText : falseText;
            text.Foreground = new SolidColorBrush(color);
        }

    }
}