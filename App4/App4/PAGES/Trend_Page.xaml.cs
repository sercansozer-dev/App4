using App4.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace App4.PAGES
{
    // ═══════════════════════════════════════════════════════════════
    // VALUE CONVERTERS
    // ═══════════════════════════════════════════════════════════════
    public class ResultBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string result = value as string;
            if (result == "OK") return new SolidColorBrush(Windows.UI.Color.FromArgb(40, 76, 175, 80));
            if (result == "NOK") return new SolidColorBrush(Windows.UI.Color.FromArgb(40, 231, 76, 60));
            return new SolidColorBrush(Windows.UI.Color.FromArgb(20, 128, 128, 128));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    public class ResultForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string result = value as string;
            if (result == "OK") return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80));
            if (result == "NOK") return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 231, 76, 60));
            return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 180, 180, 180));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    // ═══════════════════════════════════════════════════════════════
    // TREND PAGE
    // ═══════════════════════════════════════════════════════════════
    public sealed partial class Trend_Page : Page
    {
        private readonly TrendDataService _trendService = TrendDataService.Instance;
        private List<TrendRecord> _currentRecords = new();
        private bool _isLoading = false;
        private bool _isPageLoaded = false;

        public Trend_Page()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Varsayılan: Bu ay
            var now = DateTime.Now;
            _isLoading = true; // DateChanged eventlerini bastır
            DpStart.Date = new DateTimeOffset(new DateTime(now.Year, now.Month, 1));
            DpEnd.Date = new DateTimeOffset(now.Date);
            _isLoading = false;

            _isPageLoaded = true;
            RefreshData();
        }

        // ─── VERİ YENİLEME ───
        private void RefreshData()
        {
            if (_isLoading) return;
            // Kontroller henüz hazır değilse çık
            if (CmbStation == null || CmbResult == null || TxtRfidFilter == null || 
                DpStart == null || DpEnd == null || TrendListView == null) return;
            _isLoading = true;

            try
            {
                // Tarih aralığını al
                DateTime startDate = DpStart.Date?.DateTime ?? DateTime.Now.AddMonths(-1);
                DateTime endDate = (DpEnd.Date?.DateTime ?? DateTime.Now).AddDays(1).AddSeconds(-1);

                // İstasyon filtresi
                int? stationFilter = null;
                if (CmbStation.SelectedItem is ComboBoxItem stItem && stItem.Tag is string tagStr && int.TryParse(tagStr, out int stNo) && stNo > 0)
                    stationFilter = stNo;

                // Sonuç filtresi — SADECE "OK" veya "NOK" seçilince filtrele.
                // "Tumu" (hepsi) seçiliyse null bırak (eskiden "Tumu" stringi filtreye gidip
                //  GetRecords'taki "Tümü" kontrolüyle uyuşmadığı için TÜM kayıtları eliyordu).
                string resultFilter = null;
                if (CmbResult.SelectedItem is ComboBoxItem resItem)
                {
                    string rc = resItem.Content?.ToString();
                    if (rc == "OK" || rc == "NOK") resultFilter = rc;
                }

                // RFID filtresi
                string rfidFilter = string.IsNullOrWhiteSpace(TxtRfidFilter.Text) ? null : TxtRfidFilter.Text.Trim();

                // Verileri çek (zaten tarihe göre AZALAN sıralı — en yeni en üstte)
                _currentRecords = _trendService.GetRecords(startDate, endDate, stationFilter, rfidFilter, resultFilter);

                // SIRA NO: en üstteki (en son işlenen ürün) en büyük numara, aşağı doğru azalır
                int _total = _currentRecords.Count;
                for (int _i = 0; _i < _total; _i++)
                    _currentRecords[_i].SiraNo = _total - _i;

                // ListView güncelle
                TrendListView.ItemsSource = _currentRecords;

                // Boş durum kontrolü
                EmptyState.Visibility = _currentRecords.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

                // İstatistikleri güncelle
                UpdateStatistics();

                // Durum çubuğu
                TxtRecordCount.Text = $"{_currentRecords.Count} kayıt";
                TxtDateRange.Text = $"{startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}";
                TxtStatusInfo.Text = $"Son güncelleme: {DateTime.Now:HH:mm:ss}";

                // Depolama bilgisi
                var months = _trendService.GetAvailableMonths();
                TxtStorageInfo.Text = $"{months.Count} aylık veri mevcut";
            }
            catch (Exception ex)
            {
                if (TxtStatusInfo != null)
                    TxtStatusInfo.Text = $"Hata: {ex.Message}";
            }
            finally
            {
                _isLoading = false;
            }
        }

        // ─── İSTATİSTİK GÜNCELLEME ───
        private void UpdateStatistics()
        {
            var stats = _trendService.CalculateStatistics(_currentRecords);

            StatTotal.Text = stats.TotalRecords.ToString();
            StatOk.Text = stats.OkRecords.ToString();
            StatOkPct.Text = $"(%{stats.OkPercent:F0})";
            StatNok.Text = stats.NokRecords.ToString();
            StatNokPct.Text = $"(%{stats.NokPercent:F0})";
            StatAvgTime.Text = stats.AvgCycleTime.ToString("F1");
            StatNokPoints.Text = stats.TotalNokPoints.ToString();
            StatOffsetX.Text = $"X:{stats.AvgOffsetX:F2}";
            StatOffsetY.Text = $"Y:{stats.AvgOffsetY:F2}";
            StatOffsetZ.Text = $"Z:{stats.AvgOffsetZ:F2}";

            // ─── ÜRÜN ÇEŞİTLİLİĞİ (filtrelenen gruptaki ürün tipleri) ───
            UpdateProductBreakdown();
        }

        /// <summary>Filtrelenen kayıtlarda ürün tipi başına Adet / OK / NOK / Ort.Süre / NG detayı.</summary>
        private void UpdateProductBreakdown()
        {
            if (ProductBreakdownPanel == null) return;

            var groups = _currentRecords
                .GroupBy(r => string.IsNullOrWhiteSpace(r.RfidTag) ? "Tanımsız" : r.RfidTag)
                .Select(g => new
                {
                    Urun = g.Key,
                    Toplam = g.Count(),
                    Ok = g.Count(x => x.OverallResult == "OK"),
                    Nok = g.Count(x => x.OverallResult == "NOK"),
                    OrtSure = g.Average(x => x.CycleTime),
                    NgNokta = g.Sum(x => (x.NgPointsR1?.Count ?? 0) + (x.NgPointsR2?.Count ?? 0))
                })
                .OrderByDescending(x => x.Toplam)
                .ToList();

            StatProductCount.Text = groups.Count.ToString();
            UpdateTopNgPoints();
            ProductBreakdownPanel.Children.Clear();

            if (groups.Count == 0)
            {
                ProductBreakdownPanel.Children.Add(new TextBlock { Text = "Kayıt yok", FontSize = 11, Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray) });
                return;
            }

            var gray = new SolidColorBrush(Microsoft.UI.Colors.Gray);
            var white = new SolidColorBrush(Microsoft.UI.Colors.White);
            var green = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 0x4C, 0xAF, 0x50));
            var red = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 0xE7, 0x4C, 0x3C));
            var blue = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 0x00, 0xA4, 0xEF));
            var orange = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 0xF3, 0x9C, 0x12));
            var bold = Microsoft.UI.Text.FontWeights.Bold;
            var norm = Microsoft.UI.Text.FontWeights.Normal;
            double[] W = { 0, 42, 34, 34, 54, 38 }; // 0 = esnek (ürün adı)

            Grid MakeRow(string[] vals, Microsoft.UI.Xaml.Media.Brush[] brushes, bool header)
            {
                var g = new Grid { Margin = new Thickness(0, header ? 0 : 1, 0, header ? 3 : 1) };
                for (int i = 0; i < 6; i++)
                    g.ColumnDefinitions.Add(new ColumnDefinition { Width = W[i] == 0 ? new GridLength(1, GridUnitType.Star) : new GridLength(W[i]) });
                for (int i = 0; i < 6; i++)
                {
                    var tb = new TextBlock
                    {
                        Text = vals[i],
                        FontSize = header ? 9 : 11,
                        Foreground = header ? gray : brushes[i],
                        FontWeight = (header || i == 1) ? bold : norm,
                        HorizontalTextAlignment = i == 0 ? TextAlignment.Left : TextAlignment.Center,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                    Grid.SetColumn(tb, i);
                    g.Children.Add(tb);
                }
                return g;
            }

            var hb = new[] { gray, gray, gray, gray, gray, gray };
            ProductBreakdownPanel.Children.Add(MakeRow(new[] { "RFID", "ADET", "OK", "NOK", "SÜRE", "NG" }, hb, true));
            var rb = new[] { white, white, green, red, blue, orange };
            foreach (var g in groups)
                ProductBreakdownPanel.Children.Add(MakeRow(
                    new[] { g.Urun, g.Toplam.ToString(), g.Ok.ToString(), g.Nok.ToString(), g.OrtSure.ToString("F0") + "sn", g.NgNokta.ToString() },
                    rb, false));
        }

        /// <summary>Filtrelenen kayıtlarda en sık NG tespit edilen nokta numaraları (R1+R2 birleşik, frekansa göre).</summary>
        private void UpdateTopNgPoints()
        {
            if (TopNgPointsPanel == null) return;
            TopNgPointsPanel.Children.Clear();

            var freq = _currentRecords
                .SelectMany(r => (r.NgPointsR1 ?? new List<int>()).Concat(r.NgPointsR2 ?? new List<int>()))
                .GroupBy(p => p)
                .Select(g => new { Nokta = g.Key, Adet = g.Count() })
                .OrderByDescending(x => x.Adet).ThenBy(x => x.Nokta)
                .Take(8)
                .ToList();

            if (freq.Count == 0)
            {
                TopNgPointsPanel.Children.Add(new TextBlock { Text = "Kaçak nokta yok", FontSize = 11, Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray) });
                return;
            }

            int max = freq.Max(x => x.Adet);
            var red = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 0xE7, 0x4C, 0x3C));
            var redBg = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 0x3A, 0x12, 0x12));
            var white = new SolidColorBrush(Microsoft.UI.Colors.White);

            foreach (var f in freq)
            {
                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(66) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });

                var lbl = new TextBlock { Text = $"Nokta {f.Nokta}", FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = white, VerticalAlignment = VerticalAlignment.Center };

                var barHost = new Grid { Margin = new Thickness(4, 0, 4, 0) };
                var barBg = new Border { Background = redBg, CornerRadius = new CornerRadius(3), Height = 14, HorizontalAlignment = HorizontalAlignment.Stretch };
                var bar = new Border { Background = red, CornerRadius = new CornerRadius(3), Height = 14, HorizontalAlignment = HorizontalAlignment.Left, Width = Math.Max(8, 240.0 * f.Adet / max) };
                barHost.Children.Add(barBg); barHost.Children.Add(bar);

                var cnt = new TextBlock { Text = $"{f.Adet}x", FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = red, HorizontalTextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center };

                Grid.SetColumn(barHost, 1); Grid.SetColumn(cnt, 2);
                row.Children.Add(lbl); row.Children.Add(barHost); row.Children.Add(cnt);
                TopNgPointsPanel.Children.Add(row);
            }
        }

        // ─── FİLTRE DEĞİŞİKLİKLERİ ───
        private void Filter_Changed(object sender, object e)
        {
            if (_isLoading || !_isPageLoaded) return;
            RefreshData();
        }

        private void TxtRfidFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoading || !_isPageLoaded) return;
            RefreshData();
        }

        // ─── HIZLI FİLTRELER ───
        private void QuickFilter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                var now = DateTime.Now;
                switch (tag)
                {
                    case "today":
                        DpStart.Date = new DateTimeOffset(now.Date);
                        DpEnd.Date = new DateTimeOffset(now.Date);
                        break;
                    case "week":
                        int dayOfWeek = ((int)now.DayOfWeek + 6) % 7; // Pazartesi = 0
                        DpStart.Date = new DateTimeOffset(now.Date.AddDays(-dayOfWeek));
                        DpEnd.Date = new DateTimeOffset(now.Date);
                        break;
                    case "month":
                        DpStart.Date = new DateTimeOffset(new DateTime(now.Year, now.Month, 1));
                        DpEnd.Date = new DateTimeOffset(now.Date);
                        break;
                    case "year":
                        DpStart.Date = new DateTimeOffset(now.Date.AddMonths(-12));
                        DpEnd.Date = new DateTimeOffset(now.Date);
                        break;
                }
                // CalendarDatePicker DateChanged event otomatik tetikler → RefreshData çalışır
            }
        }

        // ─── YENİLE ───
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshData();
        }

        // ─── TÜM KAYITLARI TEMİZLE ───
        private async void BtnClearTrend_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Tüm Kayıtları Sil",
                Content = "Tüm trend üretim kayıtları kalıcı olarak silinecek. Bu işlem geri alınamaz!\n\nİstasyon Üretim Adedi ve Verimlilik de sıfırlanır. Emin misiniz?",
                PrimaryButtonText = "Evet, Hepsini Sil",
                CloseButtonText = "İptal",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot,
                RequestedTheme = ElementTheme.Dark
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                int n = _trendService.ClearAllRecords();
                RefreshData();
                if (TxtStatusInfo != null) TxtStatusInfo.Text = $"✅ Tüm kayıtlar silindi ({n} dosya).";
            }
        }

        // ─── TEK KAYIT SİL (yanlış/manuel veriyi temizle) ───
        private async void DeleteTrendRecord_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.Tag is not TrendRecord record) return;
            var dialog = new ContentDialog
            {
                Title = "Kaydı Sil",
                Content = $"Bu kayıt silinecek:\n\n{record.DateStr} {record.TimeStr}  ·  {record.StationName}\nRFID: {record.RfidTag}  ·  {record.OverallResult}  ·  {record.CycleTime:F1} sn\n\nEmin misiniz?",
                PrimaryButtonText = "Sil",
                CloseButtonText = "İptal",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot,
                RequestedTheme = ElementTheme.Dark
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                bool ok = _trendService.DeleteRecord(record);
                RefreshData();
                if (TxtStatusInfo != null) TxtStatusInfo.Text = ok ? "✅ Kayıt silindi." : "⚠ Kayıt silinemedi.";
            }
        }

        // ─── CSV DIŞA AKTAR ───
        private async void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (_currentRecords.Count == 0)
            {
                TxtStatusInfo.Text = "⚠ Dışa aktarılacak veri yok.";
                return;
            }

            try
            {
                var picker = new FileSavePicker();
                picker.SuggestedStartLocation = PickerLocationId.Desktop;
                picker.FileTypeChoices.Add("CSV Dosyası", new List<string> { ".csv" });
                picker.SuggestedFileName = $"Trend_Rapor_{DateTime.Now:yyyyMMdd_HHmm}";

                var window = (Application.Current as App)?.MainWindow;
                if (window != null)
                {
                    var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);
                }

                var file = await picker.PickSaveFileAsync();
                if (file != null)
                {
                    string result = _trendService.ExportToCsv(_currentRecords, file.Path);
                    if (result != null)
                        TxtStatusInfo.Text = $"✅ {_currentRecords.Count} kayıt dışa aktarıldı: {Path.GetFileName(file.Path)}";
                    else
                        TxtStatusInfo.Text = "❌ CSV dışa aktarma hatası.";
                }
            }
            catch (Exception ex)
            {
                TxtStatusInfo.Text = $"❌ Hata: {ex.Message}";
            }
        }

        // ─── YAZDIR ───
        private async void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (_currentRecords.Count == 0)
            {
                TxtStatusInfo.Text = "⚠ Yazdırılacak veri yok.";
                return;
            }

            try
            {
                // HTML rapor oluştur ve varsayılan tarayıcıda aç
                string htmlPath = GeneratePrintableReport();
                if (!string.IsNullOrEmpty(htmlPath))
                {
                    var processInfo = new System.Diagnostics.ProcessStartInfo(htmlPath) { UseShellExecute = true };
                    System.Diagnostics.Process.Start(processInfo);
                    TxtStatusInfo.Text = "✅ Yazdırma raporu oluşturuldu ve açıldı.";
                }
            }
            catch (Exception ex)
            {
                TxtStatusInfo.Text = $"❌ Yazdırma hatası: {ex.Message}";
            }
        }

        private string GeneratePrintableReport()
        {
            var stats = _trendService.CalculateStatistics(_currentRecords);
            string filePath = Path.Combine(Path.GetTempPath(), $"Trend_Rapor_{DateTime.Now:yyyyMMdd_HHmm}.html");

            string html = $@"<!DOCTYPE html>
<html><head><meta charset='utf-8'/>
<title>Trend Raporu - {DateTime.Now:dd.MM.yyyy HH:mm}</title>
<style>
  body {{ font-family: 'Segoe UI', sans-serif; background: #fff; color: #222; padding: 20px; font-size: 12px; }}
  h1 {{ color: #00A4EF; border-bottom: 2px solid #00A4EF; padding-bottom: 8px; font-size: 18px; }}
  .stats {{ display: flex; gap: 12px; margin: 16px 0; }}
  .stat-card {{ background: #f5f5f5; border-radius: 8px; padding: 12px 18px; min-width: 100px; }}
  .stat-card .label {{ font-size: 9px; color: #888; text-transform: uppercase; font-weight: bold; }}
  .stat-card .value {{ font-size: 22px; font-weight: bold; }}
  .ok {{ color: #4CAF50; }} .nok {{ color: #E74C3C; }}
  table {{ width: 100%; border-collapse: collapse; margin-top: 16px; font-size: 11px; }}
  th {{ background: #222; color: #fff; padding: 8px 6px; text-align: left; }}
  td {{ padding: 6px; border-bottom: 1px solid #eee; }}
  tr:nth-child(even) {{ background: #fafafa; }}
  .badge {{ display: inline-block; padding: 2px 8px; border-radius: 4px; font-weight: bold; font-size: 10px; }}
  .badge-ok {{ background: #E8F5E9; color: #4CAF50; }} .badge-nok {{ background: #FFEBEE; color: #E74C3C; }}
  @media print {{ body {{ padding: 0; }} .no-print {{ display: none; }} }}
</style></head><body>
<h1>📊 Trend Raporu</h1>
<p>Tarih Aralığı: <b>{DpStart.Date?.DateTime:dd.MM.yyyy}</b> – <b>{DpEnd.Date?.DateTime:dd.MM.yyyy}</b> | Oluşturulma: {DateTime.Now:dd.MM.yyyy HH:mm}</p>
<div class='stats'>
  <div class='stat-card'><div class='label'>TOPLAM</div><div class='value'>{stats.TotalRecords}</div></div>
  <div class='stat-card'><div class='label'>OK</div><div class='value ok'>{stats.OkRecords} (%{stats.OkPercent:F0})</div></div>
  <div class='stat-card'><div class='label'>NOK</div><div class='value nok'>{stats.NokRecords} (%{stats.NokPercent:F0})</div></div>
  <div class='stat-card'><div class='label'>ORT. SÜRE</div><div class='value'>{stats.AvgCycleTime:F1}s</div></div>
  <div class='stat-card'><div class='label'>NOK NOKTA</div><div class='value nok'>{stats.TotalNokPoints}</div></div>
</div>
<table><thead><tr>
  <th>Tarih</th><th>Saat</th><th>Sonuç</th><th>İstasyon</th><th>RFID</th><th>Ürün</th>
  <th>OK</th><th>NOK</th><th>Başarı</th><th>Ofs X</th><th>Ofs Y</th><th>Ofs Z</th><th>Süre</th>
</tr></thead><tbody>";

            foreach (var r in _currentRecords.Take(500))
            {
                string badge = r.OverallResult == "OK" ? "badge-ok" : "badge-nok";
                html += $@"<tr>
  <td>{r.DateStr}</td><td>{r.TimeStr}</td>
  <td><span class='badge {badge}'>{r.OverallResult}</span></td>
  <td>{r.StationName}</td><td>{r.RfidTag}</td><td>{r.ProductName}</td>
  <td>{r.OkCount}</td><td>{r.NokCount}</td><td>{r.SuccessRate}</td>
  <td>{r.OffsetX:F3}</td><td>{r.OffsetY:F3}</td><td>{r.OffsetZ:F3}</td><td>{r.CycleTime:F1}s</td>
</tr>";
            }

            if (_currentRecords.Count > 500)
                html += $"<tr><td colspan='13' style='text-align:center;color:#888;'>... ve {_currentRecords.Count - 500} kayıt daha (CSV ile tamamını alabilirsiniz)</td></tr>";

            html += "</tbody></table></body></html>";

            File.WriteAllText(filePath, html, System.Text.Encoding.UTF8);
            return filePath;
        }

        // ─── DETAY GÖRÜNTÜLEME ───
        private async void TrendListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is not TrendRecord record) return;
            if (TryShowLeakMap(record)) return;   // Kaçak haritası diyagramı varsa görsel overlay göster
            {
                string details = $"Tarih: {record.DateStr} {record.TimeStr}\n" +
                                 $"İstasyon: {record.StationName}\n" +
                                 $"RFID: {record.RfidTag}\n" +
                                 $"Ürün: {record.ProductName}\n" +
                                 $"Klima: {record.KlimaTip} (#{record.KlimaId})\n" +
                                 $"Sonuç: {record.OverallResult}\n" +
                                 $"─────────────────────\n" +
                                 $"OK: {record.OkCount}  |  NOK: {record.NokCount}  |  Başarı: {record.SuccessRate}\n" +
                                 $"Çevrim Süresi: {record.CycleTime:F1} sn\n" +
                                 $"─────────────────────\n" +
                                 $"Offset X: {record.OffsetX:F3} mm\n" +
                                 $"Offset Y: {record.OffsetY:F3} mm\n" +
                                 $"Offset Z: {record.OffsetZ:F3} mm\n" +
                                 $"Offset A: {record.OffsetA:F3}°\n" +
                                 $"Offset B: {record.OffsetB:F3}°\n" +
                                 $"Offset C: {record.OffsetC:F3}°\n";

                if (record.PointResults.Count > 0)
                {
                    details += "─────────────────────\nNokta Detayları:\n";
                    foreach (var p in record.PointResults)
                    {
                        string icon = p.Result == "OK" ? "✓" : "✗";
                        details += $"  {icon} Nokta {p.PointNo}: {p.Result} ({p.Value:F2} ppm)\n";
                    }
                }

                if (!string.IsNullOrEmpty(record.Notes))
                    details += $"\nNot: {record.Notes}";

                var dialog = new ContentDialog
                {
                    Title = $"Kayıt Detayı - {record.ResultIcon} {record.OverallResult}",
                    Content = new ScrollViewer
                    {
                        Content = new TextBlock
                        {
                            Text = details,
                            FontFamily = new FontFamily("Consolas"),
                            FontSize = 12,
                            IsTextSelectionEnabled = true,
                            TextWrapping = TextWrapping.Wrap
                        },
                        MaxHeight = 400
                    },
                    CloseButtonText = "Kapat",
                    XamlRoot = this.XamlRoot,
                    RequestedTheme = ElementTheme.Dark
                };

                await dialog.ShowAsync();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // KAÇAK HARİTASI — seçili ürünün kaçak noktalarını diyagram üzerinde gösterir
        // ═══════════════════════════════════════════════════════════════
        private LeakMapData _leakMaps;
        private bool _leakMapsLoaded;
        // İki katmanlı override: tip geneli (kütüphane) + RFID'ye özel. RFID override tipi EZER.
        private Dictionary<string, Dictionary<string, PointOvr>> _typeOvr = new();
        private Dictionary<string, Dictionary<string, PointOvr>> _rfidOvr = new();
        private static readonly string _pointMapPath =
            Path.Combine(GlobalData.ConfigBaseDir, "LeakPointMapping.json");
        // Açık overlay durumu
        private TrendRecord _lmRecord;
        private string _lmTypeKey;
        private string _lmRfid;          // null => kütüphane modu (tip kapsamı)
        private LeakMapEntry _lmEntry;
        private bool _lmEdit;
        private readonly List<(Canvas canvas, LeakPart part)> _partCanvases = new();

        private void EnsureLeakMaps()
        {
            if (_leakMapsLoaded) return;
            _leakMapsLoaded = true;
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, "Assets", "LeakMaps", "LeakMaps.json");
                if (File.Exists(path))
                {
                    var opt = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    _leakMaps = JsonSerializer.Deserialize<LeakMapData>(File.ReadAllText(path), opt);
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[LEAKMAP] yükleme hatası: {ex.Message}"); }
            try
            {
                if (File.Exists(_pointMapPath))
                {
                    var of = JsonSerializer.Deserialize<OvrFile>(File.ReadAllText(_pointMapPath));
                    _typeOvr = of?.types ?? new();
                    _rfidOvr = of?.rfids ?? new();
                }
            }
            catch { _typeOvr = new(); _rfidOvr = new(); }
        }

        private void SaveOvr()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_pointMapPath));
                File.WriteAllText(_pointMapPath, JsonSerializer.Serialize(new OvrFile { types = _typeOvr, rfids = _rfidOvr }, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[LEAKMAP] mapping kayıt hatası: {ex.Message}"); }
        }

        private static PointOvr Lookup(Dictionary<string, Dictionary<string, PointOvr>> d, string key, string name)
            => (key != null && d.TryGetValue(key, out var m) && m.TryGetValue(name, out var o)) ? o : null;

        /// <summary>Düzenleme kapsamına yazar: RFID modunda RFID'ye özel, kütüphane modunda tip geneli.</summary>
        private void SetOvr(string name, int? i, int? r)
        {
            var target = (_lmRfid != null) ? _rfidOvr : _typeOvr;
            string key = (_lmRfid != null) ? _lmRfid : _lmTypeKey;
            if (key == null) return;
            if (!target.TryGetValue(key, out var m)) { m = new(); target[key] = m; }
            if (!m.TryGetValue(name, out var o)) { o = new PointOvr(); m[name] = o; }
            if (i.HasValue) o.i = i;
            if (r.HasValue) o.r = r;
            SaveOvr();
        }

        // Etkin değer: RFID override > tip override > varsayılan (isimdeki numara)
        private int EffInt(LeakPoint p)
        {
            var rr = Lookup(_rfidOvr, _lmRfid, p.name);
            if (rr?.i != null) return rr.i.Value;
            var tt = Lookup(_typeOvr, _lmTypeKey, p.name);
            return tt?.i ?? p.idx;
        }
        private int EffRobot(LeakPoint p)
        {
            var rr = Lookup(_rfidOvr, _lmRfid, p.name);
            if (rr?.r != null) return rr.r.Value;
            var tt = Lookup(_typeOvr, _lmTypeKey, p.name);
            return tt?.r ?? p.robot;
        }

        private bool IsNgPoint(TrendRecord r, LeakPoint p)
        {
            int v = EffInt(p);
            int rob = EffRobot(p);
            return (rob == 1 && r.NgPointsR1 != null && r.NgPointsR1.Contains(v))
                || (rob == 2 && r.NgPointsR2 != null && r.NgPointsR2.Contains(v));
        }

        /// <summary>RFID'ye karşılık gelen diyagram varsa görsel kaçak haritasını açar (true), yoksa false.</summary>
        private bool TryShowLeakMap(TrendRecord record)
        {
            EnsureLeakMaps();
            if (_leakMaps?.maps == null || _leakMaps.models == null) return false;
            string rfid = record.RfidTag?.Trim();
            if (string.IsNullOrEmpty(rfid)) return false;

            // RFID → tip → diyagram. Override TİP bazında (aynı tipteki tüm RFID'ler ortak ayar).
            if (!_leakMaps.models.TryGetValue(rfid, out string typeKey))
                typeKey = _leakMaps.models.FirstOrDefault(kv => string.Equals(kv.Key, rfid, StringComparison.OrdinalIgnoreCase)).Value;
            if (string.IsNullOrEmpty(typeKey) || _leakMaps.types == null
                || !_leakMaps.types.TryGetValue(typeKey, out var ti)
                || !_leakMaps.maps.TryGetValue(ti.diagram, out var entry) || entry.parts == null || entry.parts.Count == 0)
                return false;

            _lmRecord = record; _lmTypeKey = typeKey; _lmRfid = rfid; _lmEntry = entry; _lmEdit = false;

            // Başlık / rozet
            bool isNok = record.OverallResult == "NOK";
            LeakMapResult.Text = record.OverallResult ?? "";
            LeakMapResult.Foreground = ColorFromHex(isNok ? "#E74C3C" : "#4CAF50");
            LeakMapResultBadge.Background = ColorFromHex(isNok ? "#2D0A0A" : "#0D2818");
            LeakMapSubTitle.Text = $"{rfid}  •  {ti.name}";

            // Bilgi paneli
            LeakMapInfoPanel.Children.Clear();
            void Info(string k, string v, string color = "#DDDDDD")
            {
                var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                sp.Children.Add(new TextBlock { Text = k, FontSize = 11, Width = 92, Foreground = ColorFromHex("#828282") });
                sp.Children.Add(new TextBlock { Text = v, FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = ColorFromHex(color), TextWrapping = TextWrapping.Wrap });
                LeakMapInfoPanel.Children.Add(sp);
            }
            Info("Tarih", $"{record.DateStr} {record.TimeStr}");
            Info("İstasyon", record.StationName ?? "");
            Info("RFID", rfid, "#00A4EF");
            Info("Ürün", record.ProductName ?? "");
            Info("Sonuç", record.OverallResult ?? "", isNok ? "#E74C3C" : "#4CAF50");
            Info("OK / NOK", $"{record.OkCount} / {record.NokCount}");
            Info("Çevrim", $"{record.CycleTime:F1} sn");

            BuildLeakParts();
            BuildLeakLegend();
            UpdateLeakEditButton();

            LeakMapOverlay.Visibility = Visibility.Visible;
            return true;
        }

        /// <summary>Parça görsellerini oluşturur. Kütüphane modu (RFID yok) → alt alta (büyük, kaymasız);
        /// test kaydı modu → yan yana eşit kolonlar (ekrana sığar).</summary>
        private void BuildLeakParts()
        {
            _partCanvases.Clear();
            LeakPartsGrid.Children.Clear();
            LeakPartsGrid.ColumnDefinitions.Clear();
            if (_lmEntry?.parts == null) return;

            bool vertical = (_lmRfid == null); // kütüphane → dikey istif

            if (vertical)
            {
                var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 16 };
                foreach (var part in _lmEntry.parts)
                    stack.Children.Add(MakePartColumn(part, true));
                LeakPartsGrid.Children.Add(new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    Content = stack
                });
            }
            else
            {
                for (int idx = 0; idx < _lmEntry.parts.Count; idx++)
                {
                    LeakPartsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    var colEl = MakePartColumn(_lmEntry.parts[idx], false);
                    Grid.SetColumn(colEl, idx);
                    LeakPartsGrid.Children.Add(colEl);
                }
            }
        }

        /// <summary>Tek bir parça sütunu (başlık + görsel + marker canvas) oluşturur.</summary>
        private FrameworkElement MakePartColumn(LeakPart part, bool vertical)
        {
            var col = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6, Margin = new Thickness(4, 0, 4, 0) };
            col.Children.Add(new TextBlock
            {
                Text = part.title ?? "",
                FontSize = vertical ? 13 : 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = ColorFromHex("#00A4EF"),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            var grid = new Grid { Width = part.w, Height = part.h };
            grid.Children.Add(new Image
            {
                Source = new BitmapImage(new Uri($"ms-appx:///Assets/LeakMaps/{part.image}")),
                Stretch = Stretch.Fill
            });
            var canvas = new Canvas();
            grid.Children.Add(canvas);

            var vb = new Viewbox { Stretch = Stretch.Uniform, Child = grid };
            if (vertical) { vb.MaxHeight = 600; vb.HorizontalAlignment = HorizontalAlignment.Center; }
            else { vb.VerticalAlignment = VerticalAlignment.Top; }

            col.Children.Add(new Border
            {
                Background = ColorFromHex("#0A0A0A"),
                CornerRadius = new CornerRadius(8),
                BorderBrush = ColorFromHex("#222222"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4),
                Child = vb
            });

            _partCanvases.Add((canvas, part));
            RenderPartMarkers(canvas, part);
            return col;
        }

        /// <summary>Bir parçanın marker'larını (yeşil=OK, kırmızı=KAÇAK) çizer.</summary>
        private void RenderPartMarkers(Canvas canvas, LeakPart part)
        {
            canvas.Children.Clear();
            double W = part.w, H = part.h;
            double rad = Math.Max(13, W / 26.0);
            var green = ColorFromHex("#4CAF50");
            var red = ColorFromHex("#E74C3C");
            var greenFill = new SolidColorBrush(Windows.UI.Color.FromArgb(120, 26, 46, 26));
            var redFill = new SolidColorBrush(Windows.UI.Color.FromArgb(150, 45, 10, 10));
            foreach (var p in part.points)
            {
                bool ng = IsNgPoint(_lmRecord, p);
                var ell = new Microsoft.UI.Xaml.Shapes.Ellipse
                {
                    Width = rad * 2,
                    Height = rad * 2,
                    Stroke = ng ? red : green,
                    StrokeThickness = Math.Max(3, W / 110.0),
                    Fill = ng ? redFill : greenFill
                };
                Canvas.SetLeft(ell, p.x * W - rad);
                Canvas.SetTop(ell, p.y * H - rad);
                canvas.Children.Add(ell);

                var lbl = new TextBlock
                {
                    Text = p.name,
                    FontSize = Math.Max(12, W / 24.0),
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    Foreground = ng ? red : green
                };
                Canvas.SetLeft(lbl, p.x * W + rad + 2);
                Canvas.SetTop(lbl, p.y * H - rad - 2);
                canvas.Children.Add(lbl);
            }
        }

        private void RenderAllMarkers()
        {
            foreach (var (canvas, part) in _partCanvases) RenderPartMarkers(canvas, part);
        }

        /// <summary>Sağ paneldeki nokta listesi (parça bazlı). Düzenleme modunda her noktaya robot + INT girişi.</summary>
        private void BuildLeakLegend()
        {
            if (_lmEntry?.parts == null) return;
            LeakPointLegendPanel.Children.Clear();
            var green = ColorFromHex("#4CAF50");
            var red = ColorFromHex("#E74C3C");
            var greenFill = new SolidColorBrush(Windows.UI.Color.FromArgb(120, 26, 46, 26));
            var redFill = new SolidColorBrush(Windows.UI.Color.FromArgb(150, 45, 10, 10));

            if (_lmEdit)
                LeakPointLegendPanel.Children.Add(new TextBlock
                {
                    Text = _lmRfid != null
                        ? $"Düzenleme kapsamı: SADECE bu RFID ({_lmRfid}). Burada yaptığın değişiklik tipin geneline değil, yalnızca bu ürüne uygulanır (tip varsayılanını ezer)."
                        : "Düzenleme kapsamı: TÜM TİP (grup). Bu ayar tipteki tüm RFID'lere varsayılan olur.",
                    FontSize = 11, Foreground = ColorFromHex(_lmRfid != null ? "#F39C12" : "#00A4EF"),
                    TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8)
                });

            foreach (var part in _lmEntry.parts)
            {
                LeakPointLegendPanel.Children.Add(new TextBlock
                {
                    Text = part.title ?? "",
                    FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    Foreground = ColorFromHex("#888888"), Margin = new Thickness(0, 6, 0, 2)
                });

                foreach (var p in part.points)
                {
                    bool ng = IsNgPoint(_lmRecord, p);
                    var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 1, 0, 1) };
                    row.Children.Add(new Microsoft.UI.Xaml.Shapes.Ellipse { Width = 11, Height = 11, Stroke = ng ? red : green, StrokeThickness = 2.5, Fill = ng ? redFill : greenFill, VerticalAlignment = VerticalAlignment.Center });
                    row.Children.Add(new TextBlock { Text = p.name, FontSize = 12, Width = 50, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = ColorFromHex("#DDDDDD"), VerticalAlignment = VerticalAlignment.Center });

                    if (_lmEdit)
                    {
                        var rcombo = new ComboBox { Width = 58, Height = 32, FontSize = 11, Tag = p, VerticalAlignment = VerticalAlignment.Center };
                        rcombo.Items.Add("R1"); rcombo.Items.Add("R2");
                        rcombo.SelectedIndex = EffRobot(p) == 2 ? 1 : 0;
                        rcombo.SelectionChanged += Robot_Changed;
                        row.Children.Add(rcombo);

                        var box = new TextBox
                        {
                            Text = EffInt(p).ToString(),
                            Width = 56, Height = 32, FontSize = 12,
                            Background = ColorFromHex("#202020"), Foreground = ColorFromHex("#FFFFFF"),
                            BorderBrush = ColorFromHex("#00A4EF"), VerticalAlignment = VerticalAlignment.Center, Tag = p
                        };
                        box.LostFocus += PointInt_Commit;
                        box.KeyDown += (s, ev) => { if (ev.Key == Windows.System.VirtualKey.Enter) PointInt_Commit(s, null); };
                        row.Children.Add(box);
                    }
                    else
                    {
                        row.Children.Add(new TextBlock { Text = $"R{EffRobot(p)}", FontSize = 10, Width = 24, Foreground = ColorFromHex("#777777"), VerticalAlignment = VerticalAlignment.Center });
                        row.Children.Add(new Border
                        {
                            Background = ColorFromHex("#1A1A1A"), CornerRadius = new CornerRadius(4),
                            Padding = new Thickness(6, 1, 6, 1), VerticalAlignment = VerticalAlignment.Center,
                            Child = new TextBlock { Text = "int " + EffInt(p), FontSize = 10, Foreground = ColorFromHex("#9E9E9E") }
                        });
                        row.Children.Add(new TextBlock { Text = ng ? "KAÇAK" : "OK", FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = ng ? red : green, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) });
                    }
                    LeakPointLegendPanel.Children.Add(row);
                }
            }
        }

        private void Robot_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb && cb.Tag is LeakPoint p)
            {
                SetOvr(p.name, null, cb.SelectedIndex + 1);
                RenderAllMarkers();
            }
        }

        private void PointInt_Commit(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.Tag is LeakPoint p)
            {
                if (int.TryParse(tb.Text?.Trim(), out int v))
                {
                    SetOvr(p.name, v, null);
                    RenderAllMarkers();
                }
                else
                {
                    tb.Text = EffInt(p).ToString();
                }
            }
        }

        private void ToggleLeakEdit_Click(object sender, RoutedEventArgs e)
        {
            _lmEdit = !_lmEdit;
            BuildLeakLegend();
            UpdateLeakEditButton();
        }

        private void UpdateLeakEditButton()
        {
            if (LeakEditBtnText != null) LeakEditBtnText.Text = _lmEdit ? "Bitti" : "INT Düzenle";
            if (LeakEditBtnIcon != null) LeakEditBtnIcon.Glyph = _lmEdit ? "" : "";
        }

        private static SolidColorBrush ColorFromHex(string hex)
        {
            hex = hex.Replace("#", "");
            byte r = System.Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = System.Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = System.Convert.ToByte(hex.Substring(4, 2), 16);
            return new SolidColorBrush(Windows.UI.Color.FromArgb(255, r, g, b));
        }

        private void CloseLeakMap_Click(object sender, RoutedEventArgs e) => LeakMapOverlay.Visibility = Visibility.Collapsed;
        private void LeakMapOverlay_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e) => LeakMapOverlay.Visibility = Visibility.Collapsed;
        private void LeakMapCard_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e) => e.Handled = true;

        private static int TypeNum(string tk) => int.TryParse(tk.TrimStart('T', 't'), out int n) ? n : 999;

        /// <summary>Nokta Kütüphanesi: 16 tip için nokta→INT eşlemesini test kaydı olmadan önceden ayarlamayı sağlar.</summary>
        private async void BtnLeakLibrary_Click(object sender, RoutedEventArgs e)
        {
            EnsureLeakMaps();
            if (_leakMaps?.types == null || _leakMaps.types.Count == 0)
            {
                await new ContentDialog { Title = "Nokta Kütüphanesi", Content = "Tip tanımı bulunamadı.", CloseButtonText = "Kapat", XamlRoot = this.XamlRoot, RequestedTheme = ElementTheme.Dark }.ShowAsync();
                return;
            }
            ContentDialog dialog = null;
            var panel = new StackPanel { Spacing = 4 };
            foreach (var kv in _leakMaps.types.OrderBy(k => TypeNum(k.Key)))
            {
                string tk = kv.Key;
                var btn = new Button
                {
                    Content = $"{tk}  —  {kv.Value.name}",
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(0, 2, 0, 2),
                    Background = ColorFromHex("#202020"),
                    Foreground = ColorFromHex("#DDDDDD"),
                    FontSize = 12
                };
                btn.Click += (s, _) => { dialog?.Hide(); ShowLeakLibraryForType(tk); };
                panel.Children.Add(btn);
            }
            dialog = new ContentDialog
            {
                Title = "Nokta Kütüphanesi — Tip seç",
                Content = new ScrollViewer { Content = panel, MaxHeight = 480, MinWidth = 380 },
                CloseButtonText = "Kapat",
                XamlRoot = this.XamlRoot,
                RequestedTheme = ElementTheme.Dark
            };
            await dialog.ShowAsync();
        }

        /// <summary>Kütüphane modu: test kaydı olmadan bir tipin nokta→INT/robot eşlemesini düzenler (tüm gruba uygulanır).</summary>
        private void ShowLeakLibraryForType(string typeKey)
        {
            EnsureLeakMaps();
            if (_leakMaps?.types == null || !_leakMaps.types.TryGetValue(typeKey, out var ti)) return;
            if (!_leakMaps.maps.TryGetValue(ti.diagram, out var entry) || entry.parts == null || entry.parts.Count == 0) return;

            // Boş kayıt → tüm noktalar OK (yeşil); düzenleme açık
            var empty = new TrendRecord { NgPointsR1 = new List<int>(), NgPointsR2 = new List<int>(), OverallResult = "" };
            _lmRecord = empty; _lmTypeKey = typeKey; _lmRfid = null; _lmEntry = entry; _lmEdit = true;

            LeakMapResult.Text = "KÜTÜPHANE";
            LeakMapResult.Foreground = ColorFromHex("#00A4EF");
            LeakMapResultBadge.Background = ColorFromHex("#13283C");
            LeakMapSubTitle.Text = ti.name;

            LeakMapInfoPanel.Children.Clear();
            LeakMapInfoPanel.Children.Add(new TextBlock
            {
                Text = "Bu tipteki TÜM RFID'ler için ortak nokta→INT / robot eşlemesi. Buradaki ayar tüm gruba uygulanır — tek tek RFID düzenlemene gerek yok.",
                FontSize = 11, Foreground = ColorFromHex("#9E9E9E"), TextWrapping = TextWrapping.Wrap
            });

            BuildLeakParts();
            BuildLeakLegend();
            UpdateLeakEditButton();
            LeakMapOverlay.Visibility = Visibility.Visible;
        }

        private class LeakMapData
        {
            public Dictionary<string, LeakMapEntry> maps { get; set; }   // diagram -> geometri
            public Dictionary<string, string> models { get; set; }        // RFID -> typeKey
            public Dictionary<string, TypeInfo> types { get; set; }       // typeKey -> {diagram, name}
        }
        private class TypeInfo
        {
            public string diagram { get; set; }
            public string name { get; set; }
        }
        private class LeakMapEntry
        {
            public List<LeakPart> parts { get; set; }
        }
        private class LeakPart
        {
            public string image { get; set; }
            public string title { get; set; }
            public double w { get; set; }
            public double h { get; set; }
            public List<LeakPoint> points { get; set; }
        }
        private class LeakPoint
        {
            public string name { get; set; }
            public int robot { get; set; }
            public int idx { get; set; }
            public double x { get; set; }
            public double y { get; set; }
        }
        private class PointOvr
        {
            public int? i { get; set; }
            public int? r { get; set; }
        }
        private class OvrFile
        {
            public Dictionary<string, Dictionary<string, PointOvr>> types { get; set; }   // typeKey -> noktalar
            public Dictionary<string, Dictionary<string, PointOvr>> rfids { get; set; }   // RFID -> noktalar
        }
    }
}
