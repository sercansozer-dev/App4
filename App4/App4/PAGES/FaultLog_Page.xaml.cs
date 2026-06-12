using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using App4.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace App4.PAGES
{
    public sealed partial class FaultLog_Page : Page
    {
        private readonly FaultLogService _service = FaultLogService.Instance;
        private List<FaultRecord> _current = new();
        private bool _isLoaded;

        public FaultLog_Page()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Varsayılan aralık: son 1 ay
            DpStart.Date = new DateTimeOffset(DateTime.Now.Date.AddMonths(-1));
            DpEnd.Date = new DateTimeOffset(DateTime.Now.Date);

            // Yeni arıza düşünce / kapanınca sayfa açıksa canlı yenile
            _service.OnChanged += OnServiceChanged;
            _isLoaded = true;
            RefreshData();

            // Admin: PLC input → alarm tanım tablosu
            AlarmDefList.ItemsSource = GlobalData.AlarmDefinitions;
            PopulateInputDropdown();
            ApplyAdminVisibility();
            GlobalData.AdminAccessChanged += OnAdminChanged;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _service.OnChanged -= OnServiceChanged;
            GlobalData.AdminAccessChanged -= OnAdminChanged;
            _isLoaded = false;
        }

        // ═══ ADMİN: PLC INPUT → ALARM EŞLEŞTİRME TABLOSU ═══
        private void OnAdminChanged(object sender, EventArgs e)
            => this.DispatcherQueue?.TryEnqueue(ApplyAdminVisibility);

        private void ApplyAdminVisibility()
            => AlarmDefPanel.Visibility = GlobalData.IsAdminUnlocked ? Visibility.Visible : Visibility.Collapsed;

        private void PopulateInputDropdown()
        {
            var names = PlcService.Instance?.InputVariables?
                .Select(v => v.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .OrderBy(n => n)
                .ToList() ?? new List<string>();
            CmbNewInput.ItemsSource = names;
        }

        private void BtnRefreshInputs_Click(object sender, RoutedEventArgs e) => PopulateInputDropdown();

        private void BtnAddAlarmDef_Click(object sender, RoutedEventArgs e)
        {
            var input = CmbNewInput.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(input))
            {
                TxtStatusInfo.Text = "⚠ Önce bir PLC input seçin.";
                return;
            }
            var def = new AlarmDefinition
            {
                PlcInputName = input,
                TriggerOnTrue = CmbNewTrigger.SelectedIndex == 0, // 0=ON, 1=OFF
                Mesaj = TxtNewMsg.Text?.Trim() ?? "",
                Severity = (CmbNewSeverity.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Arıza",
                Enabled = true
            };
            GlobalData.AlarmDefinitions.Add(def);
            GlobalData.SaveAlarmDefinitions();
            TxtNewMsg.Text = "";
            TxtStatusInfo.Text = $"✅ Alarm tanımı eklendi: {input} ({def.TetikStr})";
        }

        // Satır içi düzenleme (tetik / mesaj / önem / aktif) değişince kaydet
        private void AlarmDef_Changed(object sender, object e) => GlobalData.SaveAlarmDefinitions();

        private async void DeleteAlarmDef_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.Tag is not AlarmDefinition def) return;
            var dialog = new ContentDialog
            {
                Title = "Alarm Tanımını Sil",
                Content = $"{def.PlcInputName} · {def.TetikStr}\n\nBu alarm tanımı silinsin mi?",
                PrimaryButtonText = "Sil",
                CloseButtonText = "İptal",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot,
                RequestedTheme = ElementTheme.Dark
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                FaultLogService.Instance.ClearFault(def.Key); // açık alarmı da kapat
                GlobalData.AlarmDefinitions.Remove(def);
                GlobalData.SaveAlarmDefinitions();
            }
        }

        // Servis arka plan thread'inden tetikler → UI thread'ine taşı
        private void OnServiceChanged()
        {
            this.DispatcherQueue?.TryEnqueue(() => { if (_isLoaded) RefreshData(); });
        }

        private void Filter_Changed(object sender, object e) => RefreshData();

        private void QuickFilter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe) return;
            var now = DateTime.Now.Date;
            DateTime start = fe.Tag?.ToString() switch
            {
                "today" => now,
                "week" => now.AddDays(-((now.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)now.DayOfWeek) - 1)),
                "month" => new DateTime(now.Year, now.Month, 1),
                "year" => now.AddMonths(-12),
                _ => now.AddMonths(-1)
            };
            DpStart.Date = new DateTimeOffset(start);
            DpEnd.Date = new DateTimeOffset(now);
            RefreshData();
        }

        private static string Sel(ComboBox cmb) => (cmb.SelectedItem as ComboBoxItem)?.Content?.ToString();

        private void RefreshData()
        {
            if (!_isLoaded) return;
            try
            {
                DateTime start = (DpStart.Date?.DateTime ?? DateTime.Now.AddMonths(-1)).Date;
                DateTime end = (DpEnd.Date?.DateTime ?? DateTime.Now).Date.AddDays(1).AddTicks(-1); // gün sonu

                string kaynak = Sel(CmbKaynak);
                string severity = Sel(CmbSeverity);
                string durumSel = Sel(CmbDurum);
                bool? sadeceAktif = durumSel == "Aktif" ? true : (durumSel == "Geçmiş" ? false : (bool?)null);

                _current = _service.GetRecords(start, end, kaynak, severity, sadeceAktif);
                for (int i = 0; i < _current.Count; i++) _current[i].SiraNo = _current.Count - i;

                FaultListView.ItemsSource = null;
                FaultListView.ItemsSource = _current;

                TxtRecordCount.Text = $"{_current.Count} kayıt";
                EmptyState.Visibility = _current.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

                int aktif = _current.Count(r => r.Aktif);
                TxtStatusInfo.Text = aktif > 0
                    ? $"⚠ {aktif} aktif arıza · toplam {_current.Count} kayıt"
                    : $"{_current.Count} kayıt";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FAULT_UI] RefreshData hata: {ex.Message}");
            }
        }

        private async void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileSavePicker
                {
                    SuggestedStartLocation = PickerLocationId.Desktop,
                    SuggestedFileName = $"Ariza_Kayit_{DateTime.Now:yyyyMMdd_HHmm}"
                };
                picker.FileTypeChoices.Add("CSV Dosyası", new List<string> { ".csv" });

                var window = (Application.Current as App)?.MainWindow;
                if (window != null)
                {
                    var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);
                }

                var file = await picker.PickSaveFileAsync();
                if (file != null)
                {
                    var res = _service.ExportToCsv(_current, file.Path);
                    TxtStatusInfo.Text = res != null
                        ? $"✅ {_current.Count} kayıt dışa aktarıldı: {Path.GetFileName(file.Path)}"
                        : "⚠ Dışa aktarma başarısız.";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FAULT_UI] Export hata: {ex.Message}");
            }
        }

        private async void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Tüm Arıza Kayıtlarını Sil",
                Content = "TÜM arıza kayıtları kalıcı olarak silinecek. Bu işlem geri alınamaz. Devam edilsin mi?",
                PrimaryButtonText = "Evet, Sil",
                CloseButtonText = "İptal",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot,
                RequestedTheme = ElementTheme.Dark
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                int n = _service.ClearAllRecords();
                RefreshData();
                TxtStatusInfo.Text = $"🗑 {n} dosya silindi.";
            }
        }

        private async void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.Tag is not FaultRecord record) return;
            var dialog = new ContentDialog
            {
                Title = "Kaydı Sil",
                Content = $"{record.DateStr} {record.TimeStr} · {record.Kaynak} · {record.Mesaj}\n\nBu arıza kaydı silinsin mi?",
                PrimaryButtonText = "Sil",
                CloseButtonText = "İptal",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot,
                RequestedTheme = ElementTheme.Dark
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                _service.DeleteRecord(record);
                RefreshData();
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => RefreshData();
    }
}
