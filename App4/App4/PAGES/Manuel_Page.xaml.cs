using App4.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace App4
{
    public sealed partial class Manuel_Page : Page
    {
        private DispatcherTimer _refreshTimer;
        // Manuel sayfadan yazılan değerlerin local kopyası (InputVars'a dokunmadan LED güncellemesi için)
        // Local state kaldırıldı — tüm sinyal yönetimi GlobalData.RobotOutputVars üzerinden yapılır



        public Manuel_Page()
        {
            this.InitializeComponent();

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _refreshTimer.Tick += RefreshTimer_Tick;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _refreshTimer.Start();

            // R1/R2 HOME signal combos
            RefreshHomeSignalCombos();

            // KL100 station positions
            TxtKL100StBakimPos.Text = GlobalData.KL100_StationBakimPos.ToString(CultureInfo.InvariantCulture);
            TxtKL100St1Pos.Text = GlobalData.KL100_Station1Pos.ToString(CultureInfo.InvariantCulture);
            TxtKL100St2Pos.Text = GlobalData.KL100_Station2Pos.ToString(CultureInfo.InvariantCulture);
            TxtKL100St3Pos.Text = GlobalData.KL100_Station3Pos.ToString(CultureInfo.InvariantCulture);

            // İlk yüklemede klima bilgilerini otomatik sayfadan göster
            UpdateKlimaDisplayFromAuto();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            _refreshTimer.Stop();
        }

        /// <summary>
        /// Otomatik sayfadaki istasyon ürün tipi bilgisini manuel sayfada gösterir.
        /// ComboBox yok — sadece okuma. Seçim otomatik sayfadan yapılır.
        /// </summary>
        private void UpdateKlimaDisplayFromAuto()
        {
            try
            {
                var stations = GlobalData.Stations;
                if (stations == null || stations.Count < 3) return;

                UpdateSingleStationKlima(stations[0], TxtKlima_IST1_Name, TxtKlima_IST1_Info);
                UpdateSingleStationKlima(stations[1], TxtKlima_IST2_Name, TxtKlima_IST2_Info);
                UpdateSingleStationKlima(stations[2], TxtKlima_IST3_Name, TxtKlima_IST3_Info);
            }
            catch { }
        }

        private void UpdateSingleStationKlima(StationViewModel station, TextBlock nameText, TextBlock infoText)
        {
            var ext = station as ExtendedStationViewModel;
            if (ext == null)
            {
                nameText.Text = "---";
                infoText.Text = "Tip: - | Case: -";
                return;
            }

            // Mod bilgisi
            bool isSpecific = ((int)ext.RfidOpMode == (int)RfidOperationMode.Specific);
            string modStr = isSpecific ? "SPESİFİK" : "MİX";

            // Specific mod → TargetRfid (beklenen), Mixed mod → CurrentRfid (okunan)
            string rfidId = isSpecific ? ext.TargetRfid : ext.CurrentRfid;

            if (string.IsNullOrEmpty(rfidId))
            {
                nameText.Text = isSpecific ? "Beklenen seçilmedi" : "RFID okunmadı";
                nameText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 200, 80, 80));
                infoText.Text = $"[{modStr}] Tip: - | Case: -";
                return;
            }

            var rfidDef = GlobalData.KnownRfids.FirstOrDefault(r => r.Id == rfidId);
            if (rfidDef != null)
            {
                int tipIdx = GlobalData.KnownRfids.IndexOf(rfidDef) + 1;
                nameText.Text = rfidDef.Id;
                nameText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80));
                infoText.Text = $"[{modStr}] Tip: {tipIdx} | Case: {rfidDef.CasingIndex}";
            }
            else
            {
                nameText.Text = rfidId;
                nameText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 193, 7));
                infoText.Text = $"[{modStr}] Tip: ? | Tanımsız RFID";
            }
        }

        private KukaRobotInstance GetRobot()
        {
            if (KukaRobotManager.Instance?.Robots != null && KukaRobotManager.Instance.Robots.Count > 0)
                return KukaRobotManager.Instance.Robots[0];
            return null;
        }

        // ================================================================
        // REFRESH TIMER
        // ================================================================
        private void RefreshTimer_Tick(object sender, object e)
        {
            // Update station LEDs
            UpdateStationLeds();

            // Klima tipi bilgilerini otomatik sayfadan güncelle
            UpdateKlimaDisplayFromAuto();

            // Update KL100 slider panel
            UpdateKL100SliderPanel();

            // Update control panel badges
            UpdateControlPanelBadges();

        }

        // ================================================================
        // CONTROL PANEL BADGES
        // ================================================================
        private void UpdateControlPanelBadges()
        {
            // KONTROL paneli Manuel_Page.xaml içinde yorum altına alındı.
            // Bu metod derleme hatası vermemesi için boş bırakıldı; panel geri açıldığında aşağıdaki blok da geri açılmalıdır.
            return;
            /*
            try
            {
                var robots = KukaRobotManager.Instance?.Robots;
                if (robots == null || robots.Count == 0) return;

                var robot = robots.FirstOrDefault(r => r.IsConnected) ?? robots[0];
                bool isConnected = robot.IsConnected;

                // System status
                if (isConnected)
                {
                    ManuelLineStatusIcon.Glyph = "\uE768";
                    ManuelLineStatusIcon.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80));
                    ManuelLineStatusText.Text = "SISTEM AKTIF";
                    ManuelLineStatusText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80));
                }
                else
                {
                    ManuelLineStatusIcon.Glyph = "\uE71A";
                    ManuelLineStatusIcon.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136));
                    ManuelLineStatusText.Text = "SISTEM KAPALI";
                    ManuelLineStatusText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136));
                }

                // Safety
                bool safetyOk = !robot.RobotError;
                if (safetyOk)
                {
                    ManuelSafetyBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 26, 42, 26));
                    ManuelSafetyIcon.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80));
                    ManuelSafetyText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80));
                }
                else
                {
                    ManuelSafetyBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 37, 37, 40));
                    ManuelSafetyIcon.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 102, 102, 102));
                    ManuelSafetyText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 102, 102, 102));
                }

                // Alarm
                bool hasAlarm = robot.RobotError;
                if (!hasAlarm)
                {
                    ManuelAlarmBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 26, 42, 26));
                    ManuelAlarmIcon.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80));
                    ManuelAlarmText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80));
                }
                else
                {
                    ManuelAlarmBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 50, 22, 22));
                    ManuelAlarmIcon.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 244, 67, 54));
                    ManuelAlarmText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 244, 67, 54));
                }
            }
            catch { }
            */
        }

        // ================================================================
        // STATION LED UPDATE
        // ================================================================
        private void UpdateStationLeds()
        {
            var green = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80));
            var blue = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 66, 165, 245));
            var orange = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 152, 0));
            var gray = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 85, 85, 85));

            // Robot instanceları al
            var robots = KukaRobotManager.Instance?.Robots;
            var r1 = (robots != null && robots.Count >= 1) ? robots[0] : null;
            var r2 = (robots != null && robots.Count >= 2) ? robots[1] : null;

            // R2 HEDEF_ISTASYON değerini oku (R2 OutputVars — PC yazdı veya bridge güncelledi)
            int r2Hedef = 0;
            if (r2 != null)
            {
                var hVar = r2.OutputVars.FirstOrDefault(v => v.Name == "G_HEDEF_ISTASYON")
                        ?? r2.InputVars.FirstOrDefault(v => v.Name == "G_HEDEF_ISTASYON");
                if (hVar != null) int.TryParse(hVar.Value, out r2Hedef);
            }

            for (int st = 1; st <= 3; st++)
            {
                // --- ORTAK SİNYALLER ---
                // HAZIR: Robottan oku (robot FALSE'a çekerse LED söner)
                // Önce robot InputVars'tan oku, bağlı değilse PC cache'ten
                bool hazir = IsRobotSignalTrue(r1, $"G_IST{st}_HAZIR")
                          || IsGlobalVarTrue($"G_IST{st}_HAZIR");
                bool tablaOk = IsGlobalVarTrue("G_TABLA_OLCUM_TAMAM");
                bool boruOk = IsGlobalVarTrue("G_BORU_OLCUM_TAMAM");

                SetLed($"Led_IST{st}_Hazir", hazir, green, gray);
                SetLed($"Led_IST{st}_TablaOk", tablaOk, green, gray);
                SetLed($"Led_IST{st}_BoruOk", boruOk, green, gray);
                SetLedText($"TxtLed_IST{st}_Hazir_Val", hazir);
                SetLedText($"TxtLed_IST{st}_TablaOk_Val", tablaOk);
                SetLedText($"TxtLed_IST{st}_BoruOk_Val", boruOk);

                // --- R1 SİNYALLERİ (Robot 1 InputVars) ---
                bool r1IsBitti = IsRobotSignalTrue(r1, "G_IS_BITTI");
                bool r1IsBasladi = IsRobotSignalTrue(r1, "G_IS_BASLADI");
                bool r1Home = IsRobotSignalTrue(r1, "G_R1_HOME");

                SetLed($"Led_IST{st}_R1_IsBitti", r1IsBitti, blue, gray);
                SetLed($"Led_IST{st}_R1_IsBasladi", r1IsBasladi, blue, gray);
                SetLed($"Led_IST{st}_R1_Home", r1Home, blue, gray);
                SetLedText($"TxtLed_IST{st}_R1_IsBitti_Val", r1IsBitti);
                SetLedText($"TxtLed_IST{st}_R1_IsBasladi_Val", r1IsBasladi);
                SetLedText($"TxtLed_IST{st}_R1_Home_Val", r1Home);

                // --- R2 SİNYALLERİ (Robot 2 InputVars) ---
                bool r2IsBitti = IsRobotSignalTrue(r2, "G_IS_BITTI");
                bool r2IsBasladi = IsRobotSignalTrue(r2, "G_IS_BASLADI");
                bool r2Slider = IsRobotSignalTrue(r2, "G_SLIDER_TAMAM");
                bool r2Home = IsRobotSignalTrue(r2, "G_R2_HOME");
                bool r2IsHedef = (r2Hedef == st);

                SetLed($"Led_IST{st}_R2_IsBitti", r2IsBitti, orange, gray);
                SetLed($"Led_IST{st}_R2_IsBasladi", r2IsBasladi, orange, gray);
                SetLed($"Led_IST{st}_R2_Slider", r2Slider, orange, gray);
                SetLed($"Led_IST{st}_R2_Home", r2Home, orange, gray);
                SetLed($"Led_IST{st}_R2_Hedef", r2IsHedef, orange, gray);
                SetLedText($"TxtLed_IST{st}_R2_IsBitti_Val", r2IsBitti);
                SetLedText($"TxtLed_IST{st}_R2_IsBasladi_Val", r2IsBasladi);
                SetLedText($"TxtLed_IST{st}_R2_Slider_Val", r2Slider);
                SetLedText($"TxtLed_IST{st}_R2_Home_Val", r2Home);
                // HEDEF: sayı göster (TRUE/FALSE yerine)
                if (FindName($"TxtLed_IST{st}_R2_Hedef_Val") is TextBlock hedefTxt)
                {
                    hedefTxt.Text = r2Hedef > 0 ? r2Hedef.ToString() : "--";
                    hedefTxt.Foreground = r2IsHedef ? orange : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136));
                }

                // Border: HAZIR ise yeşil çerçeve
                if (FindName($"Border_IST{st}") is Border border)
                {
                    border.BorderBrush = hazir
                        ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80))
                        : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 51, 51));
                }
            }
        }

        private bool IsGlobalVarTrue(string varName)
        {
            // GlobalData.RobotOutputVars — tek kaynak, tüm sayfalardan erişilebilir
            var gVar = GlobalData.RobotOutputVars.FirstOrDefault(v => v.Name == varName);
            if (gVar != null && !string.IsNullOrEmpty(gVar.Value))
            {
                string val = gVar.Value.ToUpper();
                return val == "TRUE" || val == "1" || val == "ON";
            }
            return false;
        }

        private void SetLed(string name, bool value, Brush greenBrush, Brush grayBrush)
        {
            if (FindName(name) is Ellipse ellipse)
            {
                ellipse.Fill = value ? greenBrush : grayBrush;
            }
        }

        private void SetLedText(string name, bool value)
        {
            if (FindName(name) is TextBlock txt)
            {
                txt.Text = value ? "TRUE" : "FALSE";
                txt.Foreground = value
                    ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80))
                    : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 136, 136, 136));
            }
        }

        private bool IsRobotSignalTrue(KukaRobotInstance robot, string signalName)
        {
            if (robot == null || string.IsNullOrEmpty(signalName)) return false;

            var variable = robot.InputVars.FirstOrDefault(v => v.Name == signalName)
                        ?? robot.OutputVars.FirstOrDefault(v => v.Name == signalName);
            if (variable != null && variable.Value != null)
            {
                string val = variable.Value.ToUpper();
                return val == "TRUE" || val == "1" || val == "ON";
            }
            return false;
        }

        // ================================================================
        // STATION CALIS / DURDUR BUTTONS
        // ================================================================

        /// <summary>
        /// Seçilen istasyonu aktif yap:
        /// - Seçilen: HAZIR=TRUE, IS_BITTI=FALSE
        /// - Diğerleri: HAZIR=FALSE, IS_BITTI=TRUE
        /// - G_HEDEF_ISTASYON = seçilen istasyon no
        /// </summary>
        private async void BtnIstasyonCalis_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;
            int stNum = int.Parse(btn.Tag.ToString());

            btn.IsEnabled = false;
            try
            {
                // Aktif istasyonu hazırla
                await SetStationActive(stNum);

                // Tabla + Boru ölçüm sinyallerini sıfırla (yeni çevrim temiz başlasın)
                await WriteToRobotsAndLocal("G_TABLA_OLCUM_TAMAM", "FALSE");
                await WriteToRobotsAndLocal("G_TABLA_OLCUM_ALINDI", "FALSE");
                await WriteToRobotsAndLocal("G_TABLA_OFFSET_HAZIR", "FALSE");
                await WriteToRobotsAndLocal("G_BORU_OLCUM_TAMAM", "FALSE");

                // Dahili PC ölçüm state sıfırla
                GlobalData.ResetTablaMeasurementSignal();
                GlobalData.ResetMeasurementSignal();

                // Klima tip bilgisini otomatik sayfadaki istasyon ayarından al
                var station = (stNum >= 1 && stNum <= GlobalData.Stations.Count)
                    ? GlobalData.Stations[stNum - 1] as ExtendedStationViewModel
                    : null;

                if (station != null)
                {
                    string rfidId = ((int)station.RfidOpMode == (int)RfidOperationMode.Specific)
                        ? station.TargetRfid
                        : station.CurrentRfid;

                    var rfidDef = !string.IsNullOrEmpty(rfidId)
                        ? GlobalData.KnownRfids.FirstOrDefault(r => r.Id == rfidId)
                        : null;

                    if (rfidDef != null)
                    {
                        int klimaIdx = GlobalData.KnownRfids.IndexOf(rfidDef) + 1;
                        int caseId = rfidDef.CasingIndex;
                        // RFID string + index + case — otomatik sayfayla aynı
                        GlobalData.AktuelRfid = rfidDef.Id;
                        GlobalData.AktuelKlimaIndex = klimaIdx;
                        await WriteToRobotsAndLocal("G_KLIMA_TIP", klimaIdx.ToString());
                        // G_CASE_ID sadece Robot 1'e — Robot 2 case kullanmıyor, sadece tabla offset alıyor
                        await WriteToRobot1AndLocal("G_CASE_ID", caseId.ToString());
                    }
                }
            }
            catch { }
            finally
            {
                btn.IsEnabled = true;
            }
        }

        /// <summary>
        /// Seçilen istasyonu durdur:
        /// - HAZIR=FALSE, IS_BITTI=TRUE
        /// </summary>
        private async void BtnIstasyonDurdur_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;
            int stNum = int.Parse(btn.Tag.ToString());

            btn.IsEnabled = false;
            try
            {
                // 1. İstasyon sinyalleri: HAZIR kapat, İŞ BİTTİ aç
                await WriteToRobotsAndLocal($"G_IST{stNum}_HAZIR", "FALSE");
                await WriteToRobotsAndLocal($"G_IST{stNum}_IS_BITTI", "TRUE");

                // 2. Robot 1 ana iş bitti sinyali — her iki robota gider
                //    Robot 2 bu sinyali G_R1_IS_BITTI olarak bridge ile alır
                await WriteToRobotsAndLocal("G_IS_BITTI", "TRUE");
            }
            catch { }
            finally
            {
                btn.IsEnabled = true;
            }
        }


        /// <summary>
        /// İstasyonu aktifleştir — diğerlerini kapat.
        /// Hem robotlara yazar hem local state günceller (LED anında yanıt verir).
        /// </summary>
        private async Task SetStationActive(int stNum)
        {
            // Robotların ana iş bitti sinyalini sıfırla (yeni çevrim başlıyor)
            await WriteToRobotsAndLocal("G_IS_BITTI", "FALSE");

            for (int i = 1; i <= 3; i++)
            {
                if (i == stNum)
                {
                    // SEÇİLEN: İş bitmedi, hazır
                    await WriteToRobotsAndLocal($"G_IST{i}_IS_BITTI", "FALSE");
                    await WriteToRobotsAndLocal($"G_IST{i}_HAZIR", "TRUE");
                }
                else
                {
                    // DİĞERLERİ: İş bitti, hazır değil
                    await WriteToRobotsAndLocal($"G_IST{i}_HAZIR", "FALSE");
                    await WriteToRobotsAndLocal($"G_IST{i}_IS_BITTI", "TRUE");
                }
            }

            // Hedef istasyon — her iki robota
            await WriteToRobotsAndLocal("G_HEDEF_ISTASYON", stNum.ToString());

            // ★ TargetSliderStation senkronize et — Auto_Page monitoring'i eski değerle ezmesin
            GlobalData.TargetSliderStation = stNum;

            // Tabla + Boru ölçüm sıfırla
            await WriteToRobotsAndLocal("G_TABLA_OLCUM_TAMAM", "FALSE");
            await WriteToRobotsAndLocal("G_BORU_OLCUM_TAMAM", "FALSE");

            // Robot 2'ye slider pozisyonu
            var robots = KukaRobotManager.Instance?.Robots;
            if (robots != null && robots.Count >= 2 && robots[1].IsConnected)
            {
                double pos = GlobalData.GetStationSliderPosition(stNum);
                await robots[1].WriteVariableAsync("G_SLIDER_HEDEF_POZ", pos.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// GlobalData.RobotOutputVars + robotlara yazar.
        /// Tüm sayfalardan erişilebilir, LED'ler anında güncellenir.
        /// </summary>
        private async Task WriteToRobotsAndLocal(string varName, string value)
        {
            // GlobalData OutputVars güncelle (LED ve diğer sayfalar için)
            var gVar = GlobalData.RobotOutputVars.FirstOrDefault(v => v.Name == varName);
            if (gVar != null)
            {
                gVar.Value = value;
                System.Diagnostics.Debug.WriteLine($"[MANUEL] GlobalData.{varName} = {value} (Value set OK)");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[MANUEL] ⚠ GlobalData.{varName} BULUNAMADI!");
            }

            // Robotlara yaz (bağlıysa)
            try { await GlobalData.WriteToAllRobotsAsync(varName, value); } catch { }

            // Doğrulama: Yazıldıktan sonra hala doğru mu?
            var verify = GlobalData.RobotOutputVars.FirstOrDefault(v => v.Name == varName);
            System.Diagnostics.Debug.WriteLine($"[MANUEL] DOĞRULAMA: {varName} = '{verify?.Value}' (beklenen: {value})");
        }

        /// <summary>
        /// Sadece Robot 1'e yazar — G_CASE_ID gibi yalnız tek robotun kullandığı değişkenler için.
        /// Robot 2 case bilgisini kullanmıyor, sadece tabla offset alıyor.
        /// </summary>
        private async Task WriteToRobot1AndLocal(string varName, string value)
        {
            var gVar = GlobalData.RobotOutputVars.FirstOrDefault(v => v.Name == varName);
            if (gVar != null) gVar.Value = value;

            // PLC bridge varsa oraya da yaz (UI tabloları senkron kalsın)
            var plcVar = App4.Utilities.PlcService.Instance?.OutputVariables?.FirstOrDefault(v => v.Name == varName);
            if (plcVar != null)
            {
                plcVar.Value = value;
                try { await App4.Utilities.PlcService.Instance.WriteAsync(plcVar, value); } catch { }
            }

            // Sadece Robot 1
            var robots = App4.Utilities.KukaRobotManager.Instance?.Robots;
            if (robots != null && robots.Count >= 1 && robots[0].IsConnected)
            {
                try { await robots[0].WriteVariableAsync(varName, value); } catch { }
            }
            System.Diagnostics.Debug.WriteLine($"[MANUEL] Robot 1'e yazıldı: {varName} = {value}");
        }

        // ================================================================
        // 3 ISTASYON CALIS (Tümünü aynı anda hazırla)
        // ================================================================
        private async void BtnTumIstasyonCalis_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;

            try
            {
                // Robotların ana iş bitti sinyalini sıfırla (yeni çevrim başlıyor)
                await WriteToRobotsAndLocal("G_IS_BITTI", "FALSE");

                // 3 istasyonun hepsi: IS_BITTI=FALSE, HAZIR=TRUE
                for (int st = 1; st <= 3; st++)
                {
                    await WriteToRobotsAndLocal($"G_IST{st}_IS_BITTI", "FALSE");
                    await WriteToRobotsAndLocal($"G_IST{st}_HAZIR", "TRUE");
                }

                // Tabla + Boru ölçüm sinyallerini sıfırla
                await WriteToRobotsAndLocal("G_TABLA_OLCUM_TAMAM", "FALSE");
                await WriteToRobotsAndLocal("G_TABLA_OLCUM_ALINDI", "FALSE");
                await WriteToRobotsAndLocal("G_TABLA_OFFSET_HAZIR", "FALSE");
                await WriteToRobotsAndLocal("G_BORU_OLCUM_TAMAM", "FALSE");
                GlobalData.ResetTablaMeasurementSignal();
                GlobalData.ResetMeasurementSignal();

                // Klima tip: İlk HAZIR istasyonun otomatik sayfadaki ürün tipini gönder
                // (Round-robin R2 sırayla işleyecek, ilk istasyonun tipini başlangıç olarak yaz)
                for (int st = 0; st < Math.Min(3, GlobalData.Stations.Count); st++)
                {
                    var ext = GlobalData.Stations[st] as ExtendedStationViewModel;
                    if (ext == null) continue;

                    string rfidId = ((int)ext.RfidOpMode == (int)RfidOperationMode.Specific)
                        ? ext.TargetRfid
                        : ext.CurrentRfid;

                    var rfidDef = !string.IsNullOrEmpty(rfidId)
                        ? GlobalData.KnownRfids.FirstOrDefault(r => r.Id == rfidId)
                        : null;

                    if (rfidDef != null)
                    {
                        int klimaIdx = GlobalData.KnownRfids.IndexOf(rfidDef) + 1;
                        int caseId = rfidDef.CasingIndex;
                        GlobalData.AktuelRfid = rfidDef.Id;
                        GlobalData.AktuelKlimaIndex = klimaIdx;
                        await WriteToRobotsAndLocal("G_KLIMA_TIP", klimaIdx.ToString());
                        // G_CASE_ID sadece Robot 1'e — Robot 2 case kullanmıyor
                        await WriteToRobot1AndLocal("G_CASE_ID", caseId.ToString());
                        break; // İlk geçerli istasyonun tipini yaz
                    }
                }
            }
            catch { }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        // ================================================================
        // CONTROL PANEL BUTTONS (BASLAT / DURDUR / RESET)
        // ================================================================
        private async void BtnManuelBaslat_Click(object sender, RoutedEventArgs e)
        {
            var robot = GetRobot();
            if (robot != null && robot.IsConnected)
            {
                await WriteGlobalRobotOutVarAsync("G_BASLAT", "TRUE");
                await Task.Delay(500);
                await WriteGlobalRobotOutVarAsync("G_BASLAT", "FALSE");
                try { await robot.StartProgramAsync(); } catch { }
            }
        }

        private async void BtnManuelDurdur_Click(object sender, RoutedEventArgs e)
        {
            await WriteGlobalRobotOutVarAsync("G_SISTEM_STOP", "TRUE");
            await Task.Delay(500);
            await WriteGlobalRobotOutVarAsync("G_SISTEM_STOP", "FALSE");

            var robot = GetRobot();
            if (robot != null && robot.IsConnected)
            {
                try { await robot.StopProgramAsync(); } catch { }
            }
        }

        private async void BtnManuelReset_Click(object sender, RoutedEventArgs e)
        {
            var robot = GetRobot();
            if (robot != null && robot.IsConnected)
            {
                await WriteGlobalRobotOutVarAsync("G_RESET", "TRUE");
                await Task.Delay(500);
                await WriteGlobalRobotOutVarAsync("G_RESET", "FALSE");
                try { await robot.ResetErrorAsync(); } catch { }
            }
        }

        // ================================================================
        // WRITE GLOBAL ROBOT OUTPUT VAR (shared across all robots + PLC)
        // ================================================================
        public async Task WriteGlobalRobotOutVarAsync(string outVarName, string valueToWrite)
        {
            // WriteToRobotsAndLocal üzerinden yaz — robot.OutputVars + _lastWrittenOutputs cache güncellemesi dahil
            await WriteToRobotsAndLocal(outVarName, valueToWrite);

            // PLC'ye de yaz (WriteToRobotsAndLocal sadece robotlara yazıyor)
            try
            {
                var plcVar = PlcService.Instance?.OutputVariables?.FirstOrDefault(v => v.Name == outVarName);
                if (plcVar != null)
                {
                    plcVar.Value = valueToWrite;
                    await PlcService.Instance.WriteAsync(plcVar, valueToWrite);
                }
            }
            catch { }
        }

        // ================================================================
        // KL100 SLIDER CONTROL
        // ================================================================
        private void UpdateKL100SliderPanel()
        {
            try
            {
                var gInput = GlobalData.GeneralInputVars;
                var gOutput = GlobalData.GeneralOutputVars;

                // Is line in auto mode?
                var lineAutoVar = gInput.FirstOrDefault(v => v.Name == "LINE_AUTO_MODE");
                var lineAutoCmdVar = gOutput.FirstOrDefault(v => v.Name == "LINE_AUTO_MANUAL_CMD");
                bool isLineAuto = (lineAutoVar?.Value?.ToUpper() == "TRUE" || lineAutoVar?.Value == "1")
                               || (lineAutoCmdVar?.Value?.ToUpper() == "TRUE" || lineAutoCmdVar?.Value == "1");

                // Aktuel istasyon
                var aktuelIstVar = gInput.FirstOrDefault(v => v.Name == "AKTUEL_ISTASYON");
                string aktuelVal = aktuelIstVar?.Value ?? "0";
                if (int.TryParse(aktuelVal, out int aktuelIst) && aktuelIst >= 1 && aktuelIst <= 4)
                    KL100AktuelIstasyon.Text = aktuelIst == 4 ? "BAKIM" : $"IST {aktuelIst}";
                else
                    KL100AktuelIstasyon.Text = "---";

                // Dual robot home safety check
                bool isR1SignalConfigured = !string.IsNullOrEmpty(GlobalData.KL100_Robot1HomeSignal);
                bool isR2SignalConfigured = !string.IsNullOrEmpty(GlobalData.KL100_Robot2HomeSignal);

                bool isR1Home = isR1SignalConfigured ? IsRobotSignalTrue(0, GlobalData.KL100_Robot1HomeSignal) : true;
                bool isR2Home = isR2SignalConfigured ? IsRobotSignalTrue(1, GlobalData.KL100_Robot2HomeSignal) : true;
                bool isBothRobotsHome = isR1Home && isR2Home;

                // R1 LED
                if (isR1SignalConfigured)
                {
                    LedR1Home.Fill = new SolidColorBrush(isR1Home ? Microsoft.UI.Colors.LimeGreen : Microsoft.UI.Colors.Red);
                    TxtR1HomeStatus.Text = isR1Home ? "EVDE" : "EVDE DEGIL";
                }
                else
                {
                    LedR1Home.Fill = new SolidColorBrush(Microsoft.UI.Colors.DimGray);
                    TxtR1HomeStatus.Text = "Sinyal Yok";
                }

                // R2 LED
                if (isR2SignalConfigured)
                {
                    LedR2Home.Fill = new SolidColorBrush(isR2Home ? Microsoft.UI.Colors.LimeGreen : Microsoft.UI.Colors.Red);
                    TxtR2HomeStatus.Text = isR2Home ? "EVDE" : "EVDE DEGIL";
                }
                else
                {
                    LedR2Home.Fill = new SolidColorBrush(Microsoft.UI.Colors.DimGray);
                    TxtR2HomeStatus.Text = "Sinyal Yok";
                }

                // Hat mode display
                KL100HatModText.Text = isLineAuto ? "OTOMATIK" : "MANUEL";
                KL100HatModBorder.Background = new SolidColorBrush(
                    isLineAuto ? Windows.UI.Color.FromArgb(255, 46, 125, 50) : Windows.UI.Color.FromArgb(255, 255, 152, 0));

                // Warnings
                KL100AutoWarning.Visibility = isLineAuto ? Visibility.Visible : Visibility.Collapsed;
                KL100HomeWarning.Visibility = (!isLineAuto && !isBothRobotsHome) ? Visibility.Visible : Visibility.Collapsed;

                // Control enable: line MANUEL and BOTH robots home
                bool canControl = !isLineAuto && isBothRobotsHome;
                KL100HedefCombo.IsEnabled = canControl;
                BtnKL100HedefGit.IsEnabled = canControl;
                if (TxtKL100ManualPos != null) TxtKL100ManualPos.IsEnabled = canControl;

                // Update selected station pos display
                UpdateSelectedStationPosDisplay();
            }
            catch { }
        }

        // Overload for the index-based robot signal check (used by KL100 panel)
        private bool IsRobotSignalTrue(int robotIndex, string signalName)
        {
            if (string.IsNullOrEmpty(signalName) || KukaRobotManager.Instance?.Robots == null || KukaRobotManager.Instance.Robots.Count <= robotIndex)
                return false;

            var robot = KukaRobotManager.Instance.Robots[robotIndex];

            // Status signals
            if (signalName.Equals("ROBOT_READY", StringComparison.OrdinalIgnoreCase)) return robot.RobotReady;
            if (signalName.Equals("ROBOT_ERROR", StringComparison.OrdinalIgnoreCase)) return robot.RobotError;
            if (signalName.Equals("ROBOT_RUNNING", StringComparison.OrdinalIgnoreCase)) return robot.RobotRunning;

            // Dynamic KRL Input / Output variable
            var variable = robot.InputVars.FirstOrDefault(v => v.Name == signalName) ?? robot.OutputVars.FirstOrDefault(v => v.Name == signalName);
            if (variable != null && variable.Value != null)
            {
                string val = variable.Value.ToUpper();
                return val == "TRUE" || val == "1" || val == "ON";
            }
            return false;
        }

        // ================================================================
        // KL100 MODE CHANGE
        // ================================================================
        private void KL100Mode_Changed(object sender, RoutedEventArgs e)
        {
            bool isStationMode = RbKL100StationMode?.IsChecked == true;
            if (KL100StationPanel != null)
                KL100StationPanel.Visibility = isStationMode ? Visibility.Visible : Visibility.Collapsed;
            if (KL100PositionPanel != null)
                KL100PositionPanel.Visibility = isStationMode ? Visibility.Collapsed : Visibility.Visible;
            if (TxtKL100GoButtonLabel != null)
                TxtKL100GoButtonLabel.Text = isStationMode ? "ISTASYONA GIT" : "POZISYONA GIT";
        }

        // ================================================================
        // STATION POSITION SAVE (TextBox LostFocus)
        // ================================================================
        private void KL100StationPos_LostFocus(object sender, RoutedEventArgs e)
        {
            var ns = NumberStyles.Any;
            var ci = CultureInfo.InvariantCulture;
            if (TxtKL100StBakimPos != null && double.TryParse(TxtKL100StBakimPos.Text, ns, ci, out double pb))
                GlobalData.KL100_StationBakimPos = pb;
            if (TxtKL100St1Pos != null && double.TryParse(TxtKL100St1Pos.Text, ns, ci, out double p1))
                GlobalData.KL100_Station1Pos = p1;
            if (TxtKL100St2Pos != null && double.TryParse(TxtKL100St2Pos.Text, ns, ci, out double p2))
                GlobalData.KL100_Station2Pos = p2;
            if (TxtKL100St3Pos != null && double.TryParse(TxtKL100St3Pos.Text, ns, ci, out double p3))
                GlobalData.KL100_Station3Pos = p3;
            GlobalData.SaveKL100StationPositions();
            GlobalData.SyncStationPosToRobots();
        }

        // ================================================================
        // SELECTED STATION POS DISPLAY
        // ================================================================
        private void UpdateSelectedStationPosDisplay()
        {
            if (KL100HedefCombo?.SelectedItem is ComboBoxItem selected && selected.Tag is string tagStr && int.TryParse(tagStr, out int st))
            {
                double pos = st switch
                {
                    1 => GlobalData.KL100_Station1Pos,
                    2 => GlobalData.KL100_Station2Pos,
                    3 => GlobalData.KL100_Station3Pos,
                    _ => 0
                };
                if (TxtKL100SelectedStationPos != null)
                    TxtKL100SelectedStationPos.Text = $"{pos:F1} mm";
            }
            else
            {
                if (TxtKL100SelectedStationPos != null)
                    TxtKL100SelectedStationPos.Text = "---";
            }
        }

        // ================================================================
        // KL100 GO BUTTON
        // ================================================================
        private async void BtnKL100HedefGit_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;

            try
            {
                double hedefPoz = 0;
                bool isStationMode = RbKL100StationMode?.IsChecked == true;

                if (isStationMode)
                {
                    if (KL100HedefCombo.SelectedItem is ComboBoxItem selected && selected.Tag is string tagStr && int.TryParse(tagStr, out int st))
                    {
                        hedefPoz = st switch
                        {
                            1 => GlobalData.KL100_Station1Pos,
                            2 => GlobalData.KL100_Station2Pos,
                            3 => GlobalData.KL100_Station3Pos,
                            _ => 0
                        };

                        var hedefIstVar = GlobalData.GeneralOutputVars.FirstOrDefault(v => v.Name == "KL100_HEDEF_ISTASYON")
                                       ?? GlobalData.GeneralInputVars.FirstOrDefault(v => v.Name == "KL100_HEDEF_ISTASYON");
                        if (hedefIstVar != null)
                        {
                            hedefIstVar.Value = st.ToString();
                            hedefIstVar.CurrentValue = st;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    if (!double.TryParse(TxtKL100ManualPos?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out hedefPoz))
                        return;
                }

                // Write target position to PLC
                var hedefPozVar = GlobalData.GeneralOutputVars.FirstOrDefault(v => v.Name == "KL100_HEDEF_POZ");
                if (hedefPozVar != null)
                {
                    hedefPozVar.Value = hedefPoz.ToString(CultureInfo.InvariantCulture);
                    hedefPozVar.CurrentValue = hedefPoz;
                }

                // Write to Robot 2: G_HEDEF_ISTASYON + G_SLIDER_HEDEF_POZ
                var robots = KukaRobotManager.Instance?.Robots;
                if (robots != null && robots.Count >= 2 && robots[1].IsConnected)
                {
                    try
                    {
                        if (isStationMode && KL100HedefCombo.SelectedItem is ComboBoxItem selItem
                            && selItem.Tag is string tStr && int.TryParse(tStr, out int stNum))
                        {
                            await robots[1].WriteVariableAsync("G_HEDEF_ISTASYON", stNum.ToString());
                            var istVar = robots[1].OutputVars.FirstOrDefault(v => v.PlcTag == "G_HEDEF_ISTASYON");
                            if (istVar != null) istVar.Value = stNum.ToString();
                        }
                        else
                        {
                            await robots[1].WriteVariableAsync("G_HEDEF_ISTASYON", "0");
                            var istVar = robots[1].OutputVars.FirstOrDefault(v => v.PlcTag == "G_HEDEF_ISTASYON");
                            if (istVar != null) istVar.Value = "0";
                        }

                        string posStr = hedefPoz.ToString(CultureInfo.InvariantCulture);
                        await robots[1].WriteVariableAsync("G_SLIDER_HEDEF_POZ", posStr);
                        var r2OutputVar = robots[1].OutputVars.FirstOrDefault(v => v.PlcTag == "G_SLIDER_HEDEF_POZ");
                        if (r2OutputVar != null) r2OutputVar.Value = posStr;
                    }
                    catch { }
                }

                // Manual slider movement command -> Robot 2: G_SLIDER_HAREKET = TRUE
                if (robots != null && robots.Count >= 2 && robots[1].IsConnected)
                {
                    try
                    {
                        await robots[1].WriteVariableAsync("G_SLIDER_HAREKET", "TRUE");
                        var harVar = robots[1].OutputVars.FirstOrDefault(v => v.PlcTag == "G_SLIDER_HAREKET");
                        if (harVar != null) harVar.Value = "TRUE";
                    }
                    catch { }
                }

                // Go command (pulse) - PLC
                var gitVar = GlobalData.GeneralOutputVars.FirstOrDefault(v => v.Name == "KL100_HEDEF_GIT");
                if (gitVar != null)
                {
                    gitVar.Value = "True";
                    gitVar.CurrentValue = true;

                    await Task.Delay(500);

                    gitVar.Value = "False";
                    gitVar.CurrentValue = false;
                }
            }
            catch { }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        /// <summary>
        /// Slider HOME button: Sends G_HEDEF_ISTASYON=0 + G_SLIDER_HAREKET=TRUE to Robot 2.
        /// Robot does dynamic HOME: A1-A6 HOME, E1 stays at current position.
        /// </summary>
        private async void BtnKL100SliderHome_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;

            try
            {
                var robots = KukaRobotManager.Instance?.Robots;
                if (robots != null && robots.Count >= 2 && robots[1].IsConnected)
                {
                    await robots[1].WriteVariableAsync("G_HEDEF_ISTASYON", "0");
                    var istVar = robots[1].OutputVars.FirstOrDefault(v => v.PlcTag == "G_HEDEF_ISTASYON");
                    if (istVar != null) istVar.Value = "0";

                    await robots[1].WriteVariableAsync("G_SLIDER_HAREKET", "TRUE");
                    var harVar = robots[1].OutputVars.FirstOrDefault(v => v.PlcTag == "G_SLIDER_HAREKET");
                    if (harVar != null) harVar.Value = "TRUE";
                }
            }
            catch { }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        // ================================================================
        // HOME SIGNAL COMBOS
        // ================================================================
        private void CmbR1HomeSignal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbR1HomeSignal.SelectedItem is string sig)
                GlobalData.KL100_Robot1HomeSignal = sig;
        }

        private void CmbR2HomeSignal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbR2HomeSignal.SelectedItem is string sig)
                GlobalData.KL100_Robot2HomeSignal = sig;
        }

        /// <summary>Populates R1/R2 Home Signal ComboBoxes with current robot signals and restores selection</summary>
        private void RefreshHomeSignalCombos()
        {
            string prevR1 = CmbR1HomeSignal.SelectedItem as string ?? GlobalData.KL100_Robot1HomeSignal;
            string prevR2 = CmbR2HomeSignal.SelectedItem as string ?? GlobalData.KL100_Robot2HomeSignal;

            var r1Signals = GlobalData.GetAvailableRobotSignals(0);
            var r2Signals = GlobalData.GetAvailableRobotSignals(1);

            CmbR1HomeSignal.ItemsSource = r1Signals;
            CmbR2HomeSignal.ItemsSource = r2Signals;

            if (!string.IsNullOrEmpty(prevR1) && r1Signals.Contains(prevR1))
                CmbR1HomeSignal.SelectedItem = prevR1;

            if (!string.IsNullOrEmpty(prevR2) && r2Signals.Contains(prevR2))
                CmbR2HomeSignal.SelectedItem = prevR2;
        }
    }
}
