using App4.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO; // File check için gerekebilir ama load/save Global'de
using System.Linq;

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
        public ObservableCollection<PlcVariable> Station4Vars => GlobalData.Station4Vars;
        public ObservableCollection<PlcVariable> Station1Outputs => GlobalData.Station1Outputs;
        public ObservableCollection<PlcVariable> Station2Outputs => GlobalData.Station2Outputs;
        public ObservableCollection<PlcVariable> Station3Outputs => GlobalData.Station3Outputs;
        public ObservableCollection<PlcVariable> Station4Outputs => GlobalData.Station4Outputs;

        public ObservableCollection<App4.Utilities.LogEntry> SystemLogs { get; set; } = new();
        public ObservableCollection<string> AvailableInputPlcTags { get; set; } = new();
        public ObservableCollection<string> AvailableOutputPlcTags { get; set; } = new();

        public Auto_Page()
        {
            this.InitializeComponent();

            // Tag listelerini doldur
            InitializeAvailablePlcTags();

            // Olayları dinlemeye başla (Sayfa her açıldığında tekrar bağlanır)
            this.Loaded += Page_Loaded;
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
            BindVars(Station4Vars); BindVars(Station4Outputs);

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
            ForceUpdateStations(Station4Vars);
            // ---------------------------------------------------------

            // 3. Hat Durum Işıklarını Yak
            UpdateLineStatusVisuals();
        }




        // --- İSTASYON DURUMU DEĞİŞİRSE KAYDET ---
        private void Station_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is ExtendedStationViewModel station)
            {
                if (e.PropertyName == nameof(StationViewModel.Mode) ||
                    e.PropertyName == nameof(ExtendedStationViewModel.RfidOpMode) ||
                    e.PropertyName == nameof(ExtendedStationViewModel.TargetRfid))
                {
                    GlobalData.SaveStationStates(); // <-- GLOBAL KAYDET
                }

                // PLC'ye Yazma İşlemleri (Eski kodunun aynısı)
                int index = Stations.IndexOf(station);
                if (index < 0) return;
                ObservableCollection<PlcVariable> outputs = index switch { 0 => Station1Outputs, 1 => Station2Outputs, 2 => Station3Outputs, 3 => Station4Outputs, _ => null };

                if (outputs != null)
                {
                    if (e.PropertyName == nameof(StationViewModel.CurrentRfid) || e.PropertyName == nameof(ExtendedStationViewModel.TargetRfid))
                    {
                        string matchVal = station.IsRfidMatch ? "1" : "0";
                        UpdatePlcVar(outputs, $"ST{index + 1}_ID_MATCHED", matchVal);
                        UpdatePlcVar(outputs, $"ST{index + 1}_CONVEYOR_PERM", matchVal);
                        if (e.PropertyName == nameof(ExtendedStationViewModel.TargetRfid)) UpdatePlcVar(outputs, $"ST{index + 1}_RFID_TARGET", station.TargetRfid);
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
        }

        private void PlcTagComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb && cb.DataContext is PlcVariable v) { if (v.PlcTag != cb.SelectedItem as string) { v.PlcTag = cb.SelectedItem as string; GlobalData.SavePlcVariableTagsToFile(); ConnectToPlcVariable(v); } }
        }

        // --- GÜVENLİ DEĞER KONTROLÜ (BOOL, INT, STRING HEPSİNİ KABUL EDER) ---
        private bool IsConditionMet(string varName, bool expectedTrue)
        {
            var variable = GeneralInputVars.FirstOrDefault(v => v.Name == varName);
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

            string activeErrorMessage = null;

            // 1. KULLANICININ EKLEDİĞİ LİSTEYİ TARA
            // Sizin eklediğiniz her maddeyi tek tek kontrol eder
            foreach (var check in GlobalData.SystemCheckList)
            {
                // Tag'i bul
                var variable = GeneralInputVars.FirstOrDefault(v => v.Name == check.TagName);

                // Tag varsa ve değeri "1" veya "True" DEĞİLSE hata var demektir.
                // (IsConditionMet false dönerse hata var)
                if (!IsConditionMet(check.TagName, true))
                {
                    activeErrorMessage = check.ErrorMessage;
                    break; // İlk hatayı bulup çık, ekrana onu yazalım
                }
            }

            // 2. İstasyon Alarmlarına Bak (Eğer listede hata yoksa)
            if (string.IsNullOrEmpty(activeErrorMessage))
            {
                if (Stations.Any(s => s.HasAlarm))
                {
                    activeErrorMessage = "İSTASYON ARIZA";
                }
            }

            // --- GÖRSEL GÜNCELLEME (AYNEN KALIYOR) ---

            // Buradan sonrası önceki kodla aynıdır, sadece activeErrorMessage'ı ekrana basar.

            // ALARM IŞIĞI GÜNCELLEME
            if (activeErrorMessage == null)
            {
                CheckAlarmBorder.Background = new SolidColorBrush(Microsoft.UI.Colors.DarkGreen);
                CheckAlarmBorder.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.LimeGreen);
                CheckAlarmIcon.Glyph = "\uE73E";
                CheckAlarmIcon.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
                CheckAlarmText.Text = "SİSTEM TEMİZ";
                CheckAlarmText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
            }
            else
            {
                // HATA VAR!
                CheckAlarmBorder.Background = new SolidColorBrush(Microsoft.UI.Colors.DarkRed);
                CheckAlarmBorder.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);
                CheckAlarmIcon.Glyph = "\uE7BA";
                CheckAlarmIcon.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);

                // Hatanın adını yazıyoruz (Örn: HAVA BASINCI DÜŞÜK)
                CheckAlarmText.Text = activeErrorMessage;
                CheckAlarmText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
            }

            // START BUTONU KİLİDİ
            bool canStart = (activeErrorMessage == null) && !isRunning;
            BtnStart.IsEnabled = canStart;
            BtnStart.Opacity = canStart ? 1.0 : 0.4;

            // ANA DURUM METNİ
            if (activeErrorMessage != null)
            {
                SetStatus(activeErrorMessage, Microsoft.UI.Colors.Red, "\uE7BA");
            }
            else if (isRunning)
            {
                SetStatus("HAT ÇALIŞIYOR", Microsoft.UI.Colors.LimeGreen, "\uE768");
                BtnReset.IsEnabled = false;
                BtnReset.Opacity = 0.5;
            }
            else
            {
                SetStatus("BAŞLATMAYA HAZIR", Microsoft.UI.Colors.LightBlue, "\uE73E");
                BtnReset.IsEnabled = true;
                BtnReset.Opacity = 1;
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
        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            // Son bir kez kontrol et (UI güncellemesi gecikmiş olabilir)
            if (!IsConditionMet("SAFETY_OK", true))
            {
                AddLog("BAŞLATILAMADI: Safety (Acil Stop) sinyali yok!", "Red");
                return;
            }

            if (Stations.Any(s => s.HasAlarm))
            {
                AddLog("BAŞLATILAMADI: İstasyonlarda arıza var.", "Red");
                return;
            }

            // Başlat
            SetBtn("CMD_LINE_START", true);
            SetBtn("CMD_LINE_STOP", false);

            // Simülasyon geri bildirimi
            SetIn("LINE_RUNNING", true);

            AddLog("Hat Başlatıldı.", "Green");
            UpdateLineStatusVisuals();
        }

        // --- DİĞER BUTONLAR ---
        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            SetBtn("CMD_LINE_STOP", true);
            SetBtn("CMD_LINE_START", false);
            SetIn("LINE_RUNNING", false);
            AddLog("Hat Durduruldu.", "Red");
            UpdateLineStatusVisuals();
        }

        private async void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            var resetVar = GeneralOutputVars.FirstOrDefault(x => x.Name == "CMD_LINE_RESET");
            if (resetVar != null)
            {
                resetVar.CurrentValue = true;
                AddLog("Resetleniyor...", "Yellow");
                await System.Threading.Tasks.Task.Delay(1000);
                resetVar.CurrentValue = false;

                // Simülasyon: Alarmları temizle
                foreach (var s in Stations) s.HasAlarm = false;

                AddLog("Reset Tamamlandı.", "White");
                UpdateLineStatusVisuals();
            }
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
        private void UpdateSliderPosition(string v) { foreach (var s in Stations) s.IsRobotPresent = false; if (int.TryParse(v, out int p) && p >= 1 && p <= 4) Stations[p - 1].IsRobotPresent = true; }
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



    }
}