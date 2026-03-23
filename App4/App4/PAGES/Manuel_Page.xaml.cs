using App4.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace App4
{
    public sealed partial class Manuel_Page : Page
    {
        private DispatcherTimer _refreshTimer;
        private DispatcherTimer _proDebounceTimer;
        private DispatcherTimer _jogDebounceTimer;
        private bool _suppressSliderEvents = false;

        public ObservableCollection<SignalItemViewModel> SignalList { get; set; } = new ObservableCollection<SignalItemViewModel>();

        public Manuel_Page()
        {
            this.InitializeComponent();

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _refreshTimer.Tick += RefreshTimer_Tick;

            _proDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _proDebounceTimer.Tick += ProDebounceTimer_Tick;

            _jogDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _jogDebounceTimer.Tick += JogDebounceTimer_Tick;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _refreshTimer.Start();
            BuildValueDisplayMap();
            SignalListView.ItemsSource = SignalList;

            // ▼▼▼ R1/R2 HOME SİNYALİ VE HEDEF İSTASYON ▼▼▼
            RefreshHomeSignalCombos();

            // KL100 İstasyon pozisyon değerlerini yükle
            TxtKL100StBakimPos.Text = GlobalData.KL100_StationBakimPos.ToString(System.Globalization.CultureInfo.InvariantCulture);
            TxtKL100St1Pos.Text = GlobalData.KL100_Station1Pos.ToString(System.Globalization.CultureInfo.InvariantCulture);
            TxtKL100St2Pos.Text = GlobalData.KL100_Station2Pos.ToString(System.Globalization.CultureInfo.InvariantCulture);
            TxtKL100St3Pos.Text = GlobalData.KL100_Station3Pos.ToString(System.Globalization.CultureInfo.InvariantCulture);

            // SNIFFER + SAPMA değerlerini aktif kart+job'a göre senkronize et
            GlobalData.SyncCurrentJobOutputs();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            _refreshTimer.Stop();
        }

        private KukaRobotInstance GetRobot()
        {
            if (KukaRobotManager.Instance?.Robots != null && KukaRobotManager.Instance.Robots.Count > 0)
                return KukaRobotManager.Instance.Robots[0];
            return null;
        }

        private void RefreshTimer_Tick(object sender, object e)
        {
            BuildValueDisplayMap();

            var robot = GetRobot();
            if (robot != null && robot.IsConnected)
            {
                if (!_proDebounceTimer.IsEnabled && Math.Abs(SliderOverridePro.Value - robot.OverridePro) > 1)
                {
                    _suppressSliderEvents = true;
                    SliderOverridePro.Value = robot.OverridePro;
                    OverrideProValueText.Text = $"{robot.OverridePro}%";
                    _suppressSliderEvents = false;
                }

                if (!_jogDebounceTimer.IsEnabled && Math.Abs(SliderOverrideJog.Value - robot.OverrideJog) > 1)
                {
                    _suppressSliderEvents = true;
                    SliderOverrideJog.Value = robot.OverrideJog;
                    OverrideJogValueText.Text = $"{robot.OverrideJog}%";
                    _suppressSliderEvents = false;
                }
            }

            var gInOut = GlobalData.RobotInputVars;

            string GetVar(string varName) => gInOut.FirstOrDefault(v => v.Name == varName)?.Value ?? "0.0";
            bool IsTrue(string varName) => gInOut.FirstOrDefault(v => v.Name == varName)?.Value?.ToUpper() == "TRUE" || GetVar(varName) == "1";

            TxtBoruX.Text = GetVar("G_GOCATOR_X");
            TxtBoruY.Text = GetVar("G_GOCATOR_Y");
            TxtBoruZ.Text = GetVar("G_GOCATOR_Z");
            TxtBoruA.Text = GetVar("G_GOCATOR_A");
            TxtBoruB.Text = GetVar("G_GOCATOR_B");
            TxtBoruC.Text = GetVar("G_GOCATOR_C");

            StatusBoruTara.Text = "TARA: " + (IsTrue("G_TARA") ? "Açık" : "Kapalı");
            StatusBoruTamam.Text = "TAMAM: " + (IsTrue("G_BORU_OLCUM_TAMAM") ? "Açık" : "Kapalı");
            StatusBoruHazir.Text = "OK: " + (IsTrue("G_OLCUM_OK") ? "Başarılı" : "Bekleniyor");

            TxtInficonDeger.Text = GetVar("L_INFICON_DEGER");
            StatusInficonTamam.Text = "TAMAM: " + (IsTrue("G_SNIFFER_TAMAM") ? "Açık" : "Kapalı");
            StatusInficonOk.Text = "OK (Sonuç): " + (IsTrue("G_OLCUM_OK") ? "Başarılı" : "Bekleniyor");

            TxtTablaX.Text = GetVar("T_OFFSET_X");
            TxtTablaY.Text = GetVar("T_OFFSET_Y");
            TxtTablaZ.Text = GetVar("T_OFFSET_Z");
            TxtTablaA.Text = GetVar("T_OFFSET_A");
            TxtTablaB.Text = GetVar("T_OFFSET_B");
            TxtTablaC.Text = GetVar("T_OFFSET_C");

            StatusTablaTara.Text = "TARA: " + (IsTrue("T_TABLA_TARA") ? "Açık" : "Kapalı");
            StatusTablaTamam.Text = "TAMAM: " + (IsTrue("G_TABLA_OLCUM_TAMAM") ? "Açık" : "Kapalı");

            // Otomatik modda RFID'ye göre klima tipi index'ini robotlara gönder
            _ = UpdateAutoKlimaTipFromRfid();

            // Ölçüm sonuçlarını güncelle
            if (GlobalData.LastMeasurements.Count > 0 && OlcumSonuclariList.ItemsSource != GlobalData.LastMeasurements)
            {
                OlcumSonuclariList.ItemsSource = GlobalData.LastMeasurements;
            }

            // ═══ KL100 SLIDER KONTROL GÜNCELLEMESİ ═══
            UpdateKL100SliderPanel();
        }

        // --- KAMERA ÖLÇÜM (MANUEL BAŞLAT) ---
        private async void BtnManuelOlcumBaslat_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;

            string rfidValue = TxtManuelRfid.Text?.Trim() ?? "";
            string indexValue = TxtManuelIndex.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(rfidValue) || string.IsNullOrEmpty(indexValue))
            {
                TxtOlcumDurum.Text = "RFID ve Index zorunludur!";
                TxtOlcumDurum.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Orange);
                if (btn != null) btn.IsEnabled = true;
                return;
            }

            TxtOlcumDurum.Text = "Ölçüm alınıyor...";
            TxtOlcumDurum.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Yellow);

            try
            {
                // 1. RFID'ye göre kart tanımını bul
                var rfidDef = GlobalData.KnownRfids.FirstOrDefault(r => r.Id == rfidValue);
                string selectedJob = null;
                int jobIndex = -1;

                if (rfidDef != null && rfidDef.JobSequence != null && rfidDef.JobSequence.Count > 0)
                {
                    if (int.TryParse(indexValue, out jobIndex))
                    {
                        if (jobIndex >= 0 && jobIndex < rfidDef.JobSequence.Count)
                        {
                            selectedJob = rfidDef.JobSequence[jobIndex];
                        }
                        else
                        {
                            TxtOlcumDurum.Text = $"Index {jobIndex} geçersiz (Job sayısı: {rfidDef.JobSequence.Count})";
                            TxtOlcumDurum.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
                            if (btn != null) btn.IsEnabled = true;
                            return;
                        }
                    }
                    else
                    {
                        TxtOlcumDurum.Text = "Index değeri sayı olmalıdır!";
                        TxtOlcumDurum.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
                        if (btn != null) btn.IsEnabled = true;
                        return;
                    }
                }

                if (string.IsNullOrEmpty(selectedJob))
                {
                    TxtOlcumDurum.Text = $"Job bulunamadı (RFID: {rfidValue}, Index: {indexValue})";
                    TxtOlcumDurum.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
                    if (btn != null) btn.IsEnabled = true;
                    return;
                }

                // 2. Job'u yükle
                TxtOlcumDurum.Text = $"Job yükleniyor: {selectedJob}...";
                bool jobLoaded = await GocatorJobLogic.LoadJob(selectedJob, (msg) => { });
                if (!jobLoaded)
                {
                    TxtOlcumDurum.Text = $"Job yüklenemedi: {selectedJob}";
                    TxtOlcumDurum.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
                    if (btn != null) btn.IsEnabled = true;
                    return;
                }

                // 3. Ölçüm sinyalini sıfırla
                GlobalData.ResetMeasurementSignal();

                // 4. Sensörden ölçüm al
                TxtOlcumDurum.Text = "Sensörden veri alınıyor...";
                var result = await ReceiveMeasurementLogic.ReceiveAndProcessMeasurements(
                    (msg) => { }, this.DispatcherQueue);

                if (result.Item1 == 1) // Başarılı
                {
                    if (jobIndex == 0)
                    {
                        // --- TABLA ÖLÇÜM (JOB 0) ---
                        GlobalData.TablaLastMeasurements.Clear();
                        if (result.Item2 != null)
                            foreach (var m in result.Item2)
                                GlobalData.TablaLastMeasurements.Add(m);
                        GlobalData.SaveTablaMeasurements();

                        // Boru tablosunda tabla sonuçları kalmasın
                        GlobalData.LastMeasurements.Clear();
                        GlobalData.SaveMeasurements();

                        GlobalData.SetTablaMeasurementSignal();
                        OlcumSonuclariList.ItemsSource = GlobalData.TablaLastMeasurements;
                    }
                    else
                    {
                        // --- BORU ÖLÇÜM (JOB 1..N) ---
                        GlobalData.SetMeasurementSignal();
                        OlcumSonuclariList.ItemsSource = GlobalData.LastMeasurements;
                    }
                    TxtOlcumDurum.Text = $"BAŞARILI! {result.Item2.Count} ölçüm alındı (RFID: {rfidValue}, Job: {selectedJob})";
                    TxtOlcumDurum.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LimeGreen);
                }
                else
                {
                    TxtOlcumDurum.Text = "Sensör verisi alınamadı (Output yok veya zaman aşımı)";
                    TxtOlcumDurum.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
                }
            }
            catch (Exception ex)
            {
                TxtOlcumDurum.Text = $"Hata: {ex.Message}";
                TxtOlcumDurum.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        private async void BtnBaslat_Click(object sender, RoutedEventArgs e)
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

        private async void BtnDur_Click(object sender, RoutedEventArgs e)
        {
            await WriteGlobalRobotOutVarAsync("G_DUR", "TRUE");
            await Task.Delay(500);
            await WriteGlobalRobotOutVarAsync("G_DUR", "FALSE");
            
            var robot = GetRobot();
            if (robot != null && robot.IsConnected)
            {
                try { await robot.StopProgramAsync(); } catch { }
            }
        }

        private async void BtnReset_Click(object sender, RoutedEventArgs e)
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

        private async void BtnHome_Click(object sender, RoutedEventArgs e)
        {
            var robot = GetRobot();
            if (robot != null && robot.IsConnected)
            {
                try { await robot.GoHomeAsync(); } catch { }
            }
        }

        private async void ComboKlimaTipi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboKlimaTipi.SelectedIndex >= 0)
            {
                int tipi = ComboKlimaTipi.SelectedIndex + 1;
                await WriteGlobalRobotOutVarAsync("G_KLIMA_TIP", tipi.ToString());
            }
        }

        private void SliderOverridePro_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_suppressSliderEvents || _proDebounceTimer == null) return;
            int val = (int)e.NewValue;
            OverrideProValueText.Text = $"{val}%";
            _proDebounceTimer.Stop();
            _proDebounceTimer.Start();
        }

        private void SliderOverrideJog_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_suppressSliderEvents || _jogDebounceTimer == null) return;
            int val = (int)e.NewValue;
            OverrideJogValueText.Text = $"{val}%";
            _jogDebounceTimer.Stop();
            _jogDebounceTimer.Start();
        }

        private async void ProDebounceTimer_Tick(object sender, object e)
        {
            _proDebounceTimer.Stop();
            int val = (int)SliderOverridePro.Value;

            var robots = KukaRobotManager.Instance?.Robots;
            if (robots != null)
            {
                foreach (var r in robots)
                {
                    if (r.IsConnected)
                        try { await r.SetOverrideProAsync(val); } catch { }
                }
            }
        }

        private async void JogDebounceTimer_Tick(object sender, object e)
        {
            _jogDebounceTimer.Stop();
            int val = (int)SliderOverrideJog.Value;

            var robots = KukaRobotManager.Instance?.Robots;
            if (robots != null)
            {
                foreach (var r in robots)
                {
                    if (r.IsConnected)
                        try { await r.SetOverrideJogAsync(val); } catch { }
                }
            }
        }

        private async void BtnOffsetHazir_Click(object sender, RoutedEventArgs e)
        {
            await WriteGlobalRobotOutVarAsync("G_OLCUM_OK", "TRUE");
            await Task.Delay(500);
            await WriteGlobalRobotOutVarAsync("G_OLCUM_OK", "FALSE");
        }

        private async void BtnGocatorTamam_Click(object sender, RoutedEventArgs e)
        {
            await WriteGlobalRobotOutVarAsync("G_BORU_OLCUM_TAMAM", "TRUE");
            await Task.Delay(500);
            await WriteGlobalRobotOutVarAsync("G_BORU_OLCUM_TAMAM", "FALSE");
        }

        private async void BtnInficonOlcum_Click(object sender, RoutedEventArgs e)
        {
            await WriteGlobalRobotOutVarAsync("G_OLCUM_OK", "TRUE");
            await Task.Delay(500);
            await WriteGlobalRobotOutVarAsync("G_OLCUM_OK", "FALSE");
        }

        private async void BtnInficonReset_Click(object sender, RoutedEventArgs e)
        {
            await WriteGlobalRobotOutVarAsync("G_OLCUM_OK", "FALSE");
            await Task.Delay(200);
            await WriteGlobalRobotOutVarAsync("G_SNIFFER_TAMAM", "FALSE");
        }

        private async void BtnInficonTamam_Click(object sender, RoutedEventArgs e)
        {
            await WriteGlobalRobotOutVarAsync("G_SNIFFER_TAMAM", "TRUE");
            await Task.Delay(500);
            await WriteGlobalRobotOutVarAsync("G_SNIFFER_TAMAM", "FALSE");
        }

        private async void BtnTablaTamam_Click(object sender, RoutedEventArgs e)
        {
            await WriteGlobalRobotOutVarAsync("G_TABLA_OLCUM_TAMAM", "TRUE");
            await Task.Delay(500);
            await WriteGlobalRobotOutVarAsync("G_TABLA_OLCUM_TAMAM", "FALSE");
        }

        // DÜZELTİLDİ: Tüm robotlara yazar (eskiden sadece 1. robota yazıyordu)
        public async Task WriteGlobalRobotOutVarAsync(string outVarName, string valueToWrite)
        {
            var gVar = GlobalData.RobotOutputVars.FirstOrDefault(v => v.Name == outVarName);
            if (gVar != null)
            {
                gVar.Value = valueToWrite;
            }

            var plcVar = PlcService.Instance.OutputVariables.FirstOrDefault(v => v.Name == outVarName);
            if (plcVar != null)
            {
                plcVar.Value = valueToWrite;
                await PlcService.Instance.WriteAsync(plcVar, valueToWrite);
            }

            // TÜM robotlara yaz
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

        /// <summary>
        /// Otomatik modda: Robotun bulunduğu istasyonun RFID/Mod bilgisine göre klima tipi index'i belirler.
        /// - Mixed modda: İstasyondaki okunan (CurrentRfid) RFID'nin KnownRfids listesindeki sırası
        /// - Specific modda: İstasyonun beklenen (TargetRfid) RFID'sinin KnownRfids listesindeki sırası
        /// Sonuç hem robotlara G_KLIMA_TIP olarak, hem de PLC'ye AKTUEL_KLIMA_INDEX olarak yazılır.
        /// </summary>
        private async Task UpdateAutoKlimaTipFromRfid()
        {
            var stations = GlobalData.Stations;
            var knownRfids = GlobalData.KnownRfids;
            var robots = KukaRobotManager.Instance?.Robots;
            if (stations == null || knownRfids == null || robots == null || knownRfids.Count == 0) return;

            // Robotun aktüel istasyonunu bul
            var aktuelIstVar = GlobalData.GeneralInputVars.FirstOrDefault(v => v.Name == "AKTUEL_ISTASYON");
            int aktuelIstasyon = 0;
            if (aktuelIstVar != null) int.TryParse(aktuelIstVar.Value, out aktuelIstasyon);

            int klimaIndex = -1;

            // Aktüel istasyona göre klima index'ini belirle
            if (aktuelIstasyon >= 1 && aktuelIstasyon <= stations.Count)
            {
                var station = stations[aktuelIstasyon - 1];
                string rfidToLookup = null;

                if (station is ExtendedStationViewModel ext)
                {
                    if (ext.RfidOpMode.Equals(RfidOperationMode.Specific))
                    {
                        // Specific mod: Beklenen (TargetRfid) RFID'nin index'i
                        rfidToLookup = ext.TargetRfid;
                    }
                    else
                    {
                        // Mixed mod: Okunan (CurrentRfid) RFID'nin index'i
                        rfidToLookup = station.CurrentRfid;
                    }
                }
                else
                {
                    rfidToLookup = station.CurrentRfid;
                }

                if (!string.IsNullOrEmpty(rfidToLookup))
                {
                    for (int i = 0; i < knownRfids.Count; i++)
                    {
                        if (knownRfids[i].Id == rfidToLookup.Trim())
                        {
                            klimaIndex = i + 1; // 1-based index
                            break;
                        }
                    }
                }
            }
            else
            {
                // Aktüel istasyon bilinmiyorsa, tüm istasyonları tara (eski davranış fallback)
                foreach (var station in stations)
                {
                    string rfidToLookup = null;
                    if (station is ExtendedStationViewModel ext)
                    {
                        rfidToLookup = ext.RfidOpMode.Equals(RfidOperationMode.Specific)
                            ? ext.TargetRfid
                            : station.CurrentRfid;
                    }
                    else
                    {
                        rfidToLookup = station.CurrentRfid;
                    }

                    if (string.IsNullOrEmpty(rfidToLookup)) continue;

                    for (int i = 0; i < knownRfids.Count; i++)
                    {
                        if (knownRfids[i].Id == rfidToLookup.Trim())
                        {
                            klimaIndex = i + 1;
                            break;
                        }
                    }
                    if (klimaIndex > 0) break;
                }
            }

            if (klimaIndex < 0) return;

            // PLC'ye AKTUEL_KLIMA_INDEX yaz
            var aktuelKlimaVar = GlobalData.GeneralOutputVars.FirstOrDefault(v => v.Name == "AKTUEL_KLIMA_INDEX");
            if (aktuelKlimaVar != null && aktuelKlimaVar.Value != klimaIndex.ToString())
            {
                aktuelKlimaVar.Value = klimaIndex.ToString();
                aktuelKlimaVar.CurrentValue = klimaIndex;
            }

            // Tüm robotlara G_KLIMA_TIP olarak gönder
            foreach (var robot in robots)
            {
                if (robot.IsConnected)
                {
                    try { await robot.WriteVariableAsync("G_KLIMA_TIP", klimaIndex.ToString()); } catch { }
                }
            }
        }

        // ═══ KL100 SLIDER KONTROL ═══
        private void UpdateKL100SliderPanel()
        {
            try
            {
                var gInput = GlobalData.GeneralInputVars;
                var gOutput = GlobalData.GeneralOutputVars;

                // Hat otomatik modda mı?
                var lineAutoVar = gInput.FirstOrDefault(v => v.Name == "LINE_AUTO_MODE");
                var lineAutoCmdVar = gOutput.FirstOrDefault(v => v.Name == "LINE_AUTO_MANUAL_CMD");
                bool isLineAuto = (lineAutoVar?.Value?.ToUpper() == "TRUE" || lineAutoVar?.Value == "1")
                               || (lineAutoCmdVar?.Value?.ToUpper() == "TRUE" || lineAutoCmdVar?.Value == "1");

                // Aktüel istasyon
                var aktuelIstVar = gInput.FirstOrDefault(v => v.Name == "AKTUEL_ISTASYON");
                string aktuelVal = aktuelIstVar?.Value ?? "0";
                if (int.TryParse(aktuelVal, out int aktuelIst) && aktuelIst >= 1 && aktuelIst <= 4)
                    KL100AktuelIstasyon.Text = aktuelIst == 4 ? "BAKIM" : $"İST {aktuelIst}";
                else
                    KL100AktuelIstasyon.Text = "---";

                // ▼▼▼ ÇİFT ROBOT HOME GÜVENLİK KONTROLÜ ▼▼▼
                // Sinyal seçilmemişse → güvenli kabul et (robot yok veya kontrol yapılandırılmamış)
                bool isR1SignalConfigured = !string.IsNullOrEmpty(GlobalData.KL100_Robot1HomeSignal);
                bool isR2SignalConfigured = !string.IsNullOrEmpty(GlobalData.KL100_Robot2HomeSignal);

                bool isR1Home = isR1SignalConfigured ? IsRobotSignalTrue(0, GlobalData.KL100_Robot1HomeSignal) : true;
                bool isR2Home = isR2SignalConfigured ? IsRobotSignalTrue(1, GlobalData.KL100_Robot2HomeSignal) : true;
                bool isBothRobotsHome = isR1Home && isR2Home; // İkisi de güvende mi?

                // R1 LED
                if (isR1SignalConfigured)
                {
                    LedR1Home.Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(isR1Home ? Microsoft.UI.Colors.LimeGreen : Microsoft.UI.Colors.Red);
                    TxtR1HomeStatus.Text = isR1Home ? "EVDE" : "EVDE DEĞİL";
                }
                else
                {
                    LedR1Home.Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DimGray);
                    TxtR1HomeStatus.Text = "Sinyal Yok";
                }

                // R2 LED
                if (isR2SignalConfigured)
                {
                    LedR2Home.Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(isR2Home ? Microsoft.UI.Colors.LimeGreen : Microsoft.UI.Colors.Red);
                    TxtR2HomeStatus.Text = isR2Home ? "EVDE" : "EVDE DEĞİL";
                }
                else
                {
                    LedR2Home.Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DimGray);
                    TxtR2HomeStatus.Text = "Sinyal Yok";
                }

                // Hat modu gösterimi
                KL100HatModText.Text = isLineAuto ? "OTOMATİK" : "MANUEL";
                KL100HatModBorder.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    isLineAuto ? Windows.UI.Color.FromArgb(255, 46, 125, 50) : Windows.UI.Color.FromArgb(255, 255, 152, 0));

                // Uyarı mesajları
                KL100AutoWarning.Visibility = isLineAuto ? Visibility.Visible : Visibility.Collapsed;
                KL100HomeWarning.Visibility = (!isLineAuto && !isBothRobotsHome) ? Visibility.Visible : Visibility.Collapsed;

                // KONTROL AKTİFLİĞİ: Hat MANUEL ise ve İKİ ROBOT DA HOME ise çalışsın
                bool canControl = !isLineAuto && isBothRobotsHome;
                KL100HedefCombo.IsEnabled = canControl;
                BtnKL100HedefGit.IsEnabled = canControl;
                if (TxtKL100ManualPos != null) TxtKL100ManualPos.IsEnabled = canControl;

                // Seçili istasyonun kayıtlı pozisyonunu güncelle
                UpdateSelectedStationPosDisplay();
                // ▲▲▲ ▲▲▲ ▲▲▲
            }
            catch { }
        }

        // ═══ KL100 MOD DEĞİŞİMİ (İstasyona Git / Pozisyona Git) ═══
        private void KL100Mode_Changed(object sender, RoutedEventArgs e)
        {
            bool isStationMode = RbKL100StationMode?.IsChecked == true;
            if (KL100StationPanel != null)
                KL100StationPanel.Visibility = isStationMode ? Visibility.Visible : Visibility.Collapsed;
            if (KL100PositionPanel != null)
                KL100PositionPanel.Visibility = isStationMode ? Visibility.Collapsed : Visibility.Visible;
            if (TxtKL100GoButtonLabel != null)
                TxtKL100GoButtonLabel.Text = isStationMode ? "İSTASYONA GİT" : "POZİSYONA GİT";
        }

        // ═══ İSTASYON POZİSYON KAYIT (TextBox LostFocus) ═══
        private void KL100StationPos_LostFocus(object sender, RoutedEventArgs e)
        {
            var ns = System.Globalization.NumberStyles.Any;
            var ci = System.Globalization.CultureInfo.InvariantCulture;
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

        // ═══ HEDEF İSTASYON SEÇİLDİĞİNDE KAYITLI POZİSYONU GÖSTER ═══
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

        // ═══ ANA GİT BUTONU ═══
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
                    // ═══ İSTASYONA GİT MODU ═══
                    if (KL100HedefCombo.SelectedItem is ComboBoxItem selected && selected.Tag is string tagStr && int.TryParse(tagStr, out int st))
                    {
                        // Kayıtlı istasyon pozisyonunu al
                        hedefPoz = st switch
                        {
                            1 => GlobalData.KL100_Station1Pos,
                            2 => GlobalData.KL100_Station2Pos,
                            3 => GlobalData.KL100_Station3Pos,
                            _ => 0
                        };

                        // İstasyon numarasını da PLC'ye yaz
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
                        return; // İstasyon seçilmemiş
                    }
                }
                else
                {
                    // ═══ POZİSYONA GİT MODU ═══
                    if (!double.TryParse(TxtKL100ManualPos?.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out hedefPoz))
                        return; // Geçerli pozisyon girilmemiş
                }

                // Hedef pozisyonu PLC'ye yaz
                var hedefPozVar = GlobalData.GeneralOutputVars.FirstOrDefault(v => v.Name == "KL100_HEDEF_POZ");
                if (hedefPozVar != null)
                {
                    hedefPozVar.Value = hedefPoz.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    hedefPozVar.CurrentValue = hedefPoz;
                }

                // ═══ Robot 2'ye G_HEDEF_ISTASYON + G_SLIDER_HEDEF_POZ doğrudan yaz ═══
                // Robot 2 KRL programı bu değerleri okuyarak istasyon bazlı HOME ve slider hareketi yapar.
                var robots = KukaRobotManager.Instance?.Robots;
                if (robots != null && robots.Count >= 2 && robots[1].IsConnected)
                {
                    try
                    {
                        // 1. G_HEDEF_ISTASYON (INT) → istasyon numarası
                        // İstasyon modu: 1-4 (istasyon bazlı HOME kullanılır)
                        // Pozisyon modu: 0 (robot dinamik HOME yapar, E1 aktüel pozisyonda kalır)
                        if (isStationMode && KL100HedefCombo.SelectedItem is ComboBoxItem selItem
                            && selItem.Tag is string tStr && int.TryParse(tStr, out int stNum))
                        {
                            await robots[1].WriteVariableAsync("G_HEDEF_ISTASYON", stNum.ToString());
                            var istVar = robots[1].OutputVars.FirstOrDefault(v => v.PlcTag == "G_HEDEF_ISTASYON");
                            if (istVar != null) istVar.Value = stNum.ToString();
                        }
                        else
                        {
                            // Pozisyon modu: G_HEDEF_ISTASYON = 0 → dinamik HOME
                            await robots[1].WriteVariableAsync("G_HEDEF_ISTASYON", "0");
                            var istVar = robots[1].OutputVars.FirstOrDefault(v => v.PlcTag == "G_HEDEF_ISTASYON");
                            if (istVar != null) istVar.Value = "0";
                        }

                        // 2. G_SLIDER_HEDEF_POZ (REAL) → mm pozisyonu
                        string posStr = hedefPoz.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        await robots[1].WriteVariableAsync("G_SLIDER_HEDEF_POZ", posStr);
                        var r2OutputVar = robots[1].OutputVars.FirstOrDefault(v => v.PlcTag == "G_SLIDER_HEDEF_POZ");
                        if (r2OutputVar != null) r2OutputVar.Value = posStr;
                    }
                    catch { }
                }

                // Manuel slider hareket komutu → Robot 2'ye G_SLIDER_HAREKET = TRUE yaz
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

                // Git komutunu gönder (pulse) - PLC'ye
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
        /// Slider HOME butonu: Robot 2'ye G_HEDEF_ISTASYON=0 + G_SLIDER_HAREKET=TRUE gönderir.
        /// Robot dinamik HOME yapar: A1-A6 HOME, E1 aktüel pozisyonda kalır.
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
                    // G_HEDEF_ISTASYON = 0 → dinamik HOME (E1 aktüel pozisyonda)
                    await robots[1].WriteVariableAsync("G_HEDEF_ISTASYON", "0");
                    var istVar = robots[1].OutputVars.FirstOrDefault(v => v.PlcTag == "G_HEDEF_ISTASYON");
                    if (istVar != null) istVar.Value = "0";

                    // G_SLIDER_HAREKET = TRUE → HOME komutunu tetikle
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

        /// <summary>R1/R2 Home Signal ComboBox'larını güncel robot sinyalleriyle doldurur ve seçimi geri yükler</summary>
        private void RefreshHomeSignalCombos()
        {
            // Mevcut seçimleri koru
            string prevR1 = CmbR1HomeSignal.SelectedItem as string ?? GlobalData.KL100_Robot1HomeSignal;
            string prevR2 = CmbR2HomeSignal.SelectedItem as string ?? GlobalData.KL100_Robot2HomeSignal;

            // Listeleri yeniden doldur
            var r1Signals = GlobalData.GetAvailableRobotSignals(0);
            var r2Signals = GlobalData.GetAvailableRobotSignals(1);

            CmbR1HomeSignal.ItemsSource = r1Signals;
            CmbR2HomeSignal.ItemsSource = r2Signals;

            // Seçimi geri yükle
            if (!string.IsNullOrEmpty(prevR1) && r1Signals.Contains(prevR1))
                CmbR1HomeSignal.SelectedItem = prevR1;

            if (!string.IsNullOrEmpty(prevR2) && r2Signals.Contains(prevR2))
                CmbR2HomeSignal.SelectedItem = prevR2;
        }

        // Seçilen KUKA Değişkeninin anlık "True/1" olup olmadığını kontrol eden yardımcı metod
        private bool IsRobotSignalTrue(int robotIndex, string signalName)
        {
            if (string.IsNullOrEmpty(signalName) || KukaRobotManager.Instance?.Robots == null || KukaRobotManager.Instance.Robots.Count <= robotIndex)
                return false;

            var robot = KukaRobotManager.Instance.Robots[robotIndex];

            // Sabit Durum Sinyalleri mi?
            if (signalName.Equals("ROBOT_READY", StringComparison.OrdinalIgnoreCase)) return robot.RobotReady;
            if (signalName.Equals("ROBOT_ERROR", StringComparison.OrdinalIgnoreCase)) return robot.RobotError;
            if (signalName.Equals("ROBOT_RUNNING", StringComparison.OrdinalIgnoreCase)) return robot.RobotRunning;

            // Dinamik KRL Input / Output değişkeni mi?
            var variable = robot.InputVars.FirstOrDefault(v => v.Name == signalName) ?? robot.OutputVars.FirstOrDefault(v => v.Name == signalName);
            if (variable != null && variable.Value != null)
            {
                string val = variable.Value.ToUpper();
                return val == "TRUE" || val == "1" || val == "ON";
            }
            return false;
        }


        private void BuildValueDisplayMap()
        {
            try
            {
                var plcInputVars = PlcService.Instance.InputVariables;
                var plcOutputVars = PlcService.Instance.OutputVariables;

                // 1. Mevcut listeyi koruyup sadece değerleri değiştireceğiz (UI titremesini önlemek için)
                if (SignalList.Count == 0)
                {
                    // İlk yükleme - Listeyi doldur
                    var mergedVars = new List<SignalItemViewModel>();
                    foreach(var v in GlobalData.RobotInputVars)
                    {
                        var pVar = plcInputVars.FirstOrDefault(x => x.Name == v.Name);
                        mergedVars.Add(new SignalItemViewModel
                        {
                            VarName = v.Name,
                            RobotName = "ROBOT 1",
                            DirectionTarget = "ROBOT/PLC ➔ PC (Okuma)",
                            PlcTagName = pVar?.PlcTag ?? "-",
                            PlcType = pVar?.Type ?? "-",
                            ValueStr = v.Value,
                            IsOutput = false
                        });
                    }
                    foreach(var v in GlobalData.RobotOutputVars)
                    {
                        var pVar = plcOutputVars.FirstOrDefault(x => x.Name == v.Name);
                        mergedVars.Add(new SignalItemViewModel
                        {
                            VarName = v.Name,
                            RobotName = "ROBOT 1",
                            DirectionTarget = "PC ➔ ROBOT/PLC (Yazma)",
                            PlcTagName = pVar?.PlcTag ?? "-",
                            PlcType = pVar?.Type ?? "-",
                            ValueStr = v.Value,
                            IsOutput = true
                        });
                    }

                    foreach (var item in mergedVars.OrderBy(v => v.VarName))
                    {
                        SignalList.Add(item);
                    }
                }
                else
                {
                    // Zaten dolu - Sadece güncel değerleri yerleştir
                    foreach(var item in SignalList)
                    {
                        if (item.IsOutput)
                        {
                            var gVar = GlobalData.RobotOutputVars.FirstOrDefault(v => v.Name == item.VarName);
                            if (gVar != null && item.ValueStr != gVar.Value) 
                            {
                                item.ValueStr = gVar.Value;
                                // INotifyPropertyChanged yok, bu yüzden ufak bir trick yapabiliriz veya
                                // viewmodel'e ekleyebiliriz ama şimdilik performansı etkilememesi için
                                // item'in objesini yenilemek en garantilisi. Fakat ObservableCollection
                                // olduğu için objeyi değiştirince yine titrer.
                                // En iyisi SignalItemViewModel'e INotifyPropertyChanged eklemek.
                            }
                        }
                        else
                        {
                            var gVar = GlobalData.RobotInputVars.FirstOrDefault(v => v.Name == item.VarName);
                            if (gVar != null && item.ValueStr != gVar.Value) 
                            {
                                item.ValueStr = gVar.Value;
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }

    public class SignalItemViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        public string RobotName { get; set; }
        public string DirectionTarget { get; set; }
        public string VarName { get; set; }
        public string PlcTagName { get; set; }
        public string PlcType { get; set; }
        
        private string _valueStr;
        public string ValueStr 
        { 
            get => _valueStr; 
            set 
            { 
                if (_valueStr != value) 
                { 
                    _valueStr = value; 
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(ValueStr))); 
                } 
            } 
        }
        
        public bool IsOutput { get; set; }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
    }
}
