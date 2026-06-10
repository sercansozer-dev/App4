using App4.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
            if (e.ClickedItem is TrendRecord record)
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
    }
}
