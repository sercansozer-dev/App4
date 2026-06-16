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
        private Microsoft.UI.Xaml.DispatcherTimer _liveTimer;

        // ═══ CANLI KAÇAK HARİTASI (görsel üzerinde NG takibi) ═══
        private bool _liveMapMode = true;   // resim üzerinde göster (toggle: Harita)
        private bool _liveNgOnly = false;   // VARSAYILAN: tüm noktalar (yeşil OK + kırmızı NG) — yeşiller hep dursun, kaçak kırmızı düşsün
        // 1sn kronometre: yapısal yeniden kurmadan yalnız süre metnini güncellemek için kayıt
        private readonly List<(TextBlock clock, DateTime start)> _liveClocks = new();
        // Çözülmüş görselleri tekrar decode etmemek için önbellek (yeniden kurmada titremeyi önler)
        private static readonly Dictionary<string, Microsoft.UI.Xaml.Media.ImageSource> _liveBmpCache = new();
        // Aktif nabız animasyonları — yeniden kurmada durdurulur (RepeatForever sızıntısını önler)
        private readonly List<Microsoft.UI.Xaml.Media.Animation.Storyboard> _livePulses = new();

        public Trend_Page()
        {
            this.InitializeComponent();
            this.Unloaded += Trend_Page_Unloaded;
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

            // ═══ CANLI ÜRETİM TAKİBİ ═══
            GlobalData.LiveProductionChanged += OnLiveProductionChanged;
            GlobalData.ProductionRecorded += OnProductionRecorded;
            if (_liveTimer == null)
            {
                _liveTimer = new Microsoft.UI.Xaml.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                // Yalnız kronometreyi güncelle — yapı (kart/çip/harita) sadece LiveProductionChanged'de yeniden kurulur
                // (her saniye görsel yeniden kurmak titreme + bitmap decode maliyeti yaratırdı).
                _liveTimer.Tick += (s2, e2) => UpdateLiveClocks();
            }
            _liveTimer.Start();
            BuildLiveProduction();
        }

        private void Trend_Page_Unloaded(object sender, RoutedEventArgs e)
        {
            GlobalData.LiveProductionChanged -= OnLiveProductionChanged;
            GlobalData.ProductionRecorded -= OnProductionRecorded;
            _liveTimer?.Stop();
            foreach (var sb in _livePulses) { try { sb.Stop(); } catch { } }
            _livePulses.Clear();
            _isPageLoaded = false;
        }

        private void OnLiveProductionChanged() => DispatcherQueue?.TryEnqueue(BuildLiveProduction);
        private void OnProductionRecorded() => DispatcherQueue?.TryEnqueue(() => { if (_isPageLoaded) RefreshData(); });

        /// <summary>Aktif çevrimdeki ürünleri + robotun o an verdiği kaçak noktalarını canlı gösterir.</summary>
        private void BuildLiveProduction()
        {
            if (LiveProductionPanel == null) return;
            _liveClocks.Clear();
            foreach (var sb in _livePulses) { try { sb.Stop(); } catch { } }
            _livePulses.Clear();
            var active = GlobalData.LiveStations.Where(l => l != null && l.Active).OrderBy(l => l.StationNo).ToList();

            if (active.Count == 0)
            {
                if (LivePanel != null) LivePanel.Visibility = Visibility.Collapsed;
                LiveProductionPanel.Children.Clear();
                return;
            }
            if (LivePanel != null) LivePanel.Visibility = Visibility.Visible;
            if (LiveCountText != null) LiveCountText.Text = $"·  {active.Count} istasyon işlemde";

            LiveProductionPanel.Children.Clear();
            var bold = Microsoft.UI.Text.FontWeights.Bold;

            // ═══ İSTASYONLAR YAN YANA ═══ — her aktif istasyon eşit genişlikte bir sütun.
            // Hat 3 istasyonlu; dikey istif yerine yan yana → çok daha az dikey alan + hepsi aynı anda görünür.
            var lineGrid = new Grid { ColumnSpacing = 8 };
            for (int c = 0; c < active.Count; c++)
                lineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            for (int si = 0; si < active.Count; si++)
            {
                var lp = active[si];
                var card = new Border
                {
                    Background = ColorFromHex("#142014"), CornerRadius = new CornerRadius(8),
                    BorderBrush = ColorFromHex("#234523"), BorderThickness = new Thickness(1), Padding = new Thickness(10, 8, 10, 8)
                };
                var cg = new StackPanel { Spacing = 6 };

                // Üst satır: istasyon · RFID · süre (dar sütun → kompakt)
                var head = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                head.Children.Add(new Border { Background = ColorFromHex("#1E6F3E"), CornerRadius = new CornerRadius(4), Padding = new Thickness(7, 2, 7, 2),
                    Child = new TextBlock { Text = $"İST {lp.StationNo}", FontSize = 11, FontWeight = bold, Foreground = BrWhite } });
                head.Children.Add(new TextBlock { Text = string.IsNullOrEmpty(lp.Rfid) ? "—" : lp.Rfid, FontSize = 12, FontWeight = bold, Foreground = BrBlue, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis });
                var ts = DateTime.Now - lp.StartTime;
                if (ts < TimeSpan.Zero) ts = TimeSpan.Zero;
                var clock = new TextBlock { Text = $"⏱ {(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}", FontSize = 12, FontWeight = bold, Foreground = ColorFromHex("#4CAF50"), VerticalAlignment = VerticalAlignment.Center };
                head.Children.Add(clock);
                _liveClocks.Add((clock, lp.StartTime)); // 1sn timer bunu günceller (yapıyı yeniden kurmadan)
                cg.Children.Add(head);

                // Ürün adı ayrı satır (dar sütunda taşmasın)
                if (!string.IsNullOrEmpty(lp.ProductName))
                    cg.Children.Add(new TextBlock { Text = lp.ProductName, FontSize = 10, Foreground = ColorFromHex("#CCCCCC"), TextTrimming = TextTrimming.CharacterEllipsis });

                // Canlı kaçak noktaları (kod çözülmüş)
                int total = (lp.NgR1?.Count ?? 0) + (lp.NgR2?.Count ?? 0);
                if (total == 0)
                {
                    cg.Children.Add(new TextBlock { Text = "✓ şimdilik kaçak yok", FontSize = 11, Foreground = ColorFromHex("#7BB97B") });
                }
                else
                {
                    var chips = new List<FrameworkElement>();
                    if (lp.NgR1 != null && lp.NgR1.Count > 0)
                    {
                        chips.Add(MakeRobotTag("R1"));
                        foreach (var v in lp.NgR1) chips.Add(LiveChip(ResolvePointCode(lp.Rfid, 1, v) ?? $"N{v}", false));
                    }
                    if (lp.NgR2 != null && lp.NgR2.Count > 0)
                    {
                        chips.Add(MakeRobotTag("R2"));
                        foreach (var v in lp.NgR2) chips.Add(LiveChip(ResolvePointCode(lp.Rfid, 2, v) ?? $"N{v}", true));
                    }
                    cg.Children.Add(new TextBlock { Text = $"⚠ {total} kaçak", FontSize = 11, FontWeight = bold, Foreground = BrRed });
                    var row = new StackPanel { Spacing = 4 };
                    StackPanel line = null;
                    for (int i = 0; i < chips.Count; i++)
                    {
                        if (i % 5 == 0) { line = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 }; row.Children.Add(line); }
                        line.Children.Add(chips[i]);
                    }
                    cg.Children.Add(row);
                }

                // ═══ CANLI GÖRSEL HARİTA (kaçak noktaları diyagram üzerinde) ═══
                // Çipler her zaman üstte kalır (koordinatı eksik diyagramlar için güvenli yedek).
                if (_liveMapMode)
                    BuildLiveLeakMap(lp, cg);

                card.Child = cg;
                Grid.SetColumn(card, si);
                lineGrid.Children.Add(card);
            }
            LiveProductionPanel.Children.Add(lineGrid);
        }

        private static Border LiveChip(string label, bool r2) => new()
        {
            Background = r2 ? BrNgBg : BrNokBg, CornerRadius = new CornerRadius(4), Padding = new Thickness(7, 2, 7, 2),
            Child = new TextBlock { Text = label, FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = r2 ? BrOrange : BrRed }
        };

        // ═══════════════════════════════════════════════════════════════════════
        //  CANLI GÖRSEL KAÇAK HARİTASI — robotun o an verdiği NG noktalarını
        //  ürünün diyagram görseli ÜZERİNDE canlı işaretler. Çiplerle BİRLİKTE çalışır.
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>1sn timer — yapıyı yeniden kurmadan yalnız kronometre metnini günceller (titreme yok).</summary>
        private void UpdateLiveClocks()
        {
            foreach (var (clock, start) in _liveClocks)
            {
                if (clock == null) continue;
                var ts = DateTime.Now - start;
                if (ts < TimeSpan.Zero) ts = TimeSpan.Zero;
                clock.Text = $"⏱ {(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}";
            }
        }

        /// <summary>Harita / Sadece-NG toggle'ları değişince paneli yeniden kur.</summary>
        private void LiveViewToggle_Changed(object sender, RoutedEventArgs e)
        {
            _liveMapMode = LiveMapToggle?.IsChecked ?? true;
            _liveNgOnly = LiveNgOnlyToggle?.IsChecked ?? true;
            BuildLiveProduction();
        }

        /// <summary>Canlı panel gövdesini gizle/aç — üst bölgede yer açmak için.</summary>
        private void LiveCollapse_Click(object sender, RoutedEventArgs e)
        {
            bool show = LiveCollapseBtn?.IsChecked ?? true;
            if (LiveProductionPanel != null) LiveProductionPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (LiveCollapseBtn != null) LiveCollapseBtn.Content = show ? "▾ Gizle" : "▸ Göster";
        }

        /// <summary>TEST: PLC olmadan canlı görsel NG haritasını görmek için sahte aktif üretim enjekte eder.
        /// Yalnızca BELLEKTEKİ GlobalData.LiveStations'a yazar — trend veritabanına HİÇBİR kayıt eklemez.
        /// Gerçek RFID'ler kullanılır (diyagramlara çözülür); NG INT'leri gerçek nokta indekslerine denk gelir.</summary>
        private void LiveDemoToggle_Click(object sender, RoutedEventArgs e)
        {
            bool on = LiveDemoToggle?.IsChecked ?? false;
            var s = GlobalData.LiveStations;
            if (on)
            {
                // İstasyon 1 — Alpha (T1): A15 (R1/i15), B6 (R1/i6), C2-2 (R2/i2)
                s[0].StationNo = 1; s[0].StationName = "İSTASYON 1"; s[0].Rfid = "RXJ20A5V1B9";
                s[0].ProductName = DemoProductName("RXJ20A5V1B9"); s[0].StartTime = DateTime.Now.AddSeconds(-23);
                s[0].Active = true; s[0].NgR1 = new List<int> { 15, 6 }; s[0].NgR2 = new List<int> { 2 };

                // İstasyon 2 — SF2-1YC (T3): A20 (R1/i20), C2-1 (R2/i1)
                s[1].StationNo = 2; s[1].StationName = "İSTASYON 2"; s[1].Rfid = "RXM20A5V1B";
                s[1].ProductName = DemoProductName("RXM20A5V1B"); s[1].StartTime = DateTime.Now.AddSeconds(-48);
                s[1].Active = true; s[1].NgR1 = new List<int> { 20 }; s[1].NgR2 = new List<int> { 1 };

                // İstasyon 3 — Alpha (T1), kaçaksız (temiz örnek: "Sadece NG" kapalıyken yeşil noktalar)
                s[2].StationNo = 3; s[2].StationName = "İSTASYON 3"; s[2].Rfid = "RXJ25A5V1B9";
                s[2].ProductName = DemoProductName("RXJ25A5V1B9"); s[2].StartTime = DateTime.Now.AddSeconds(-9);
                s[2].Active = true; s[2].NgR1 = new List<int>(); s[2].NgR2 = new List<int>();
            }
            else
            {
                foreach (var lp in s) { lp.Active = false; lp.NgR1 = new List<int>(); lp.NgR2 = new List<int>(); }
            }
            GlobalData.RaiseLiveProductionChanged();
        }

        private static string DemoProductName(string rfid)
        {
            var r = GlobalData.KnownRfids.FirstOrDefault(x => string.Equals(x.Id, rfid, StringComparison.OrdinalIgnoreCase));
            return r?.Description ?? "(demo ürün)";
        }

        /// <summary>Aktif bir istasyonun diyagram görsel(ler)ini, canlı NG noktaları işaretli olarak karta ekler.
        /// Diyagram/görsel yoksa sessizce çıkar (çipler yeterli yedek). Sadece-NG modunda yalnız kaçak olan parçalar çizilir.</summary>
        private void BuildLiveLeakMap(GlobalData.LiveProductionItem lp, Panel cardBody)
        {
            try
            {
                EnsureLeakMaps();
                if (_leakMaps?.maps == null || _leakMaps.types == null) return;
                string rfid = lp.Rfid?.Trim();
                if (string.IsNullOrEmpty(rfid)) return;

                string typeKey = ResolveTypeKey(rfid);
                if (string.IsNullOrEmpty(typeKey) || !_leakMaps.types.TryGetValue(typeKey, out var ti) || string.IsNullOrEmpty(ti?.diagram))
                    return; // diyagram yok → çipler yeterli
                var entry = BuildEffectiveEntry(ti.diagram);
                if (entry?.parts == null || entry.parts.Count == 0) return;

                var ngR1 = lp.NgR1; var ngR2 = lp.NgR2;
                bool anyNg = (ngR1?.Count ?? 0) + (ngR2?.Count ?? 0) > 0;
                if (_liveNgOnly && !anyNg) return; // kaçak yok → görsel gösterme (çip 'kaçak yok' diyor)

                Func<LeakPoint, bool> isNg = pt => IsNgPointLive(rfid, typeKey, ngR1, ngR2, pt);

                // Gösterilecek parçalar: NG-only → yalnız içinde kaçak olan parçalar; aksi halde tüm diyagram
                var partsToShow = _liveNgOnly
                    ? entry.parts.Where(p => p.points != null && p.points.Any(isNg)).ToList()
                    : entry.parts;
                if (partsToShow.Count == 0) return;

                // İstasyonlar yan yana olduğu için parçalar sütun İÇİNDE DİKEY istiflenir
                // (her görsel sütun genişliğine sığacak şekilde Uniform ölçeklenir).
                var colSp = new StackPanel { Orientation = Orientation.Vertical, Spacing = 4, Margin = new Thickness(0, 2, 0, 0) };
                foreach (var part in partsToShow)
                {
                    var col = new StackPanel { Orientation = Orientation.Vertical, Spacing = 2 };
                    col.Children.Add(new TextBlock
                    {
                        Text = part.title ?? "",
                        FontSize = 10, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        Foreground = ColorFromHex("#8AB0C8"), HorizontalAlignment = HorizontalAlignment.Center,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    });

                    var grid = new Grid { Width = part.w, Height = part.h };
                    grid.Children.Add(new Image { Source = ResolveLiveImageSource(part.image), Stretch = Stretch.Fill });
                    var canvas = new Canvas { Width = part.w, Height = part.h };
                    grid.Children.Add(canvas);
                    RenderLiveMarkers(canvas, part, isNg, rfid, typeKey);

                    // Uniform + MaxHeight: sütun genişliğine göre küçülür, çok büyümez
                    var vb = new Viewbox { Stretch = Stretch.Uniform, Child = grid, MaxHeight = 175, HorizontalAlignment = HorizontalAlignment.Center };
                    col.Children.Add(new Border
                    {
                        Background = ColorFromHex("#0A0A0A"), CornerRadius = new CornerRadius(6),
                        BorderBrush = ColorFromHex("#234523"), BorderThickness = new Thickness(1),
                        Padding = new Thickness(3), Child = vb
                    });
                    colSp.Children.Add(col);
                }

                cardBody.Children.Add(colSp);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[LIVEMAP] hata: {ex.Message}"); }
        }

        /// <summary>Canlı NG predikatı (sayfa durumundan BAĞIMSIZ — editör _lm* alanlarına dokunmaz).
        /// EffInt/EffRobot mantığının aynısı ama rfid/typeKey parametre olarak alınır (çok istasyon aynı anda).</summary>
        private bool IsNgPointLive(string rfid, string typeKey, List<int> ngR1, List<int> ngR2, LeakPoint p)
        {
            var rr = Lookup(_rfidOvr, rfid, p.name);
            var tt = Lookup(_typeOvr, typeKey, p.name);
            int effI = rr?.i ?? tt?.i ?? p.idx;
            int effR = rr?.r ?? tt?.r ?? p.robot;
            return (effR == 1 && ngR1 != null && ngR1.Contains(effI))
                || (effR == 2 && ngR2 != null && ngR2.Contains(effI));
        }

        /// <summary>Canlı marker çizimi: kaçak = kırmızı + nabız animasyonu; OK = ince yeşil (yalnız 'tümü' modunda).
        /// RenderPartMarkers ile aynı yerleşim matematiği, ama predikat parametre (editör durumuna bağımlı değil).</summary>
        private void RenderLiveMarkers(Canvas canvas, LeakPart part, Func<LeakPoint, bool> isNg, string rfid, string typeKey)
        {
            canvas.Children.Clear();
            if (part?.points == null) return;
            double W = part.w, H = part.h;
            double rad = Math.Max(13, W / 26.0);
            var green = ColorFromHex("#4CAF50");
            var red = ColorFromHex("#E74C3C");
            var greenFill = new SolidColorBrush(Windows.UI.Color.FromArgb(120, 26, 46, 26));
            var redFill = new SolidColorBrush(Windows.UI.Color.FromArgb(160, 55, 12, 12));
            foreach (var p in part.points)
            {
                bool ng = isNg(p);
                if (_liveNgOnly && !ng) continue; // sadece-NG: OK noktalarını çizme
                var ell = new Microsoft.UI.Xaml.Shapes.Ellipse
                {
                    Width = rad * 2, Height = rad * 2,
                    Stroke = ng ? red : green,
                    StrokeThickness = Math.Max(3, W / 110.0),
                    Fill = ng ? redFill : greenFill,
                    RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5)
                };
                Canvas.SetLeft(ell, p.x * W - rad);
                Canvas.SetTop(ell, p.y * H - rad);
                canvas.Children.Add(ell);

                string dsc = EffDescLive(rfid, typeKey, p);
                if (!string.IsNullOrEmpty(dsc)) ToolTipService.SetToolTip(ell, $"{p.name} — {dsc}");

                if (ng) AddPulse(ell); // canlı kaçak → nabız (göze çarpsın)

                // Etiket: NG'de her zaman; OK'te yalnız 'tümü' modunda
                if (ng || !_liveNgOnly)
                {
                    var lbl = new TextBlock
                    {
                        Text = p.name, FontSize = Math.Max(12, W / 24.0),
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = ng ? red : green
                    };
                    Canvas.SetLeft(lbl, p.x * W + rad + 2);
                    Canvas.SetTop(lbl, p.y * H - rad - 2);
                    canvas.Children.Add(lbl);
                }
            }
        }

        /// <summary>Nokta açıklaması — canlı haritada tooltip için. Kapsam: RFID override > tip override (kanonik EffDesc ile aynı).</summary>
        private string EffDescLive(string rfid, string typeKey, LeakPoint p)
        {
            var rr = Lookup(_rfidOvr, rfid, p.name);
            if (!string.IsNullOrEmpty(rr?.d)) return rr.d;
            var tt = Lookup(_typeOvr, typeKey, p.name);
            return string.IsNullOrEmpty(tt?.d) ? null : tt.d;
        }

        /// <summary>Canlı kaçak noktasına nabız (scale) animasyonu ekler ve takibe alır (yeniden kurmada durdurulur).</summary>
        private void AddPulse(Microsoft.UI.Xaml.Shapes.Ellipse ell)
        {
            try
            {
                var st = new Microsoft.UI.Xaml.Media.ScaleTransform { ScaleX = 1, ScaleY = 1 };
                ell.RenderTransform = st;
                var sb = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
                void Anim(string prop)
                {
                    var da = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
                    {
                        From = 1.0, To = 1.35,
                        Duration = new Duration(TimeSpan.FromMilliseconds(650)),
                        AutoReverse = true,
                        RepeatBehavior = Microsoft.UI.Xaml.Media.Animation.RepeatBehavior.Forever,
                        EnableDependentAnimation = true
                    };
                    Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(da, st);
                    Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(da, prop);
                    sb.Children.Add(da);
                }
                Anim("ScaleX"); Anim("ScaleY");
                _livePulses.Add(sb);
                sb.Begin();
            }
            catch { }
        }

        /// <summary>ResolveImageSource + filename önbelleği (yeniden kurmada decode maliyeti/titreme olmasın).</summary>
        private static Microsoft.UI.Xaml.Media.ImageSource ResolveLiveImageSource(string image)
        {
            if (string.IsNullOrEmpty(image)) return null;
            if (_liveBmpCache.TryGetValue(image, out var cached)) return cached;
            var src = ResolveImageSource(image);
            if (src != null) _liveBmpCache[image] = src;
            return src;
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

        // ═══════════════════════════════════════════════════════════════
        // ÖZET PANELİ — paylaşılan fırçalar ve görsel yardımcı fabrikalar
        // ═══════════════════════════════════════════════════════════════
        private static SolidColorBrush B(byte r, byte g, byte b)
            => new(Microsoft.UI.ColorHelper.FromArgb(255, r, g, b));

        private static readonly SolidColorBrush BrWhite   = new(Microsoft.UI.Colors.White);
        private static readonly SolidColorBrush BrGreen   = B(0x4C, 0xAF, 0x50);
        private static readonly SolidColorBrush BrRed     = B(0xE7, 0x4C, 0x3C);
        private static readonly SolidColorBrush BrOrange  = B(0xF3, 0x9C, 0x12);
        private static readonly SolidColorBrush BrBlue    = B(0x00, 0xA4, 0xEF);
        private static readonly SolidColorBrush BrG666    = B(0x66, 0x66, 0x66);
        private static readonly SolidColorBrush BrG888    = B(0x88, 0x88, 0x88);
        private static readonly SolidColorBrush BrZebra   = B(0x16, 0x16, 0x16);
        private static readonly SolidColorBrush BrOkBg    = B(0x0D, 0x28, 0x18);
        private static readonly SolidColorBrush BrNokBg   = B(0x2D, 0x0A, 0x0A);
        private static readonly SolidColorBrush BrNgBg    = B(0x2D, 0x1F, 0x0A);
        private static readonly SolidColorBrush BrNeutral = B(0x1E, 0x1E, 0x1E);
        private static readonly SolidColorBrush BrTrack   = B(0x22, 0x22, 0x22);
        private static readonly SolidColorBrush BrPTrack  = B(0x1A, 0x1A, 0x1A);
        private static readonly SolidColorBrush BrBadge   = B(0x3A, 0x12, 0x12);

        // Kolon pill'i: değer 0 ise nötr gri (sıfır sessiz kalır, gerçek hata renkle bağırır)
        private static Border MakePill(string text, SolidColorBrush bg, SolidColorBrush fg) => new()
        {
            Background = bg, CornerRadius = new CornerRadius(10),
            Padding = new Thickness(0, 2, 0, 2), VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock { Text = text, FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = fg,
                HorizontalTextAlignment = TextAlignment.Center }
        };

        // NG chip'i: R1 kırmızı, R2 turuncu — "C1-1 ×3" (kod yoksa "N5 ×3")
        private static Border MakeChip(string label, int adet, bool r2) => new()
        {
            Background = r2 ? BrNgBg : BrNokBg, CornerRadius = new CornerRadius(4),
            Padding = new Thickness(7, 2, 7, 2),
            Child = new TextBlock { Text = $"{label} ×{adet}", FontSize = 10,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = r2 ? BrOrange : BrRed }
        };

        // Sol tabloda seçili RFID (sağ Pareto bu ürüne filtrelenir; null = tümü)
        private string _selectedNgRfid;

        /// <summary>(rfid, robot, INT) → nokta kodu (örn. "C1-1"). Nokta Kütüphanesi eşlemesinden
        /// ters çözüm: RFID override > tip override > varsayılan. Eşleme yoksa null.</summary>
        private string ResolvePointCode(string rfid, int robot, int value)
        {
            EnsureLeakMaps();
            rfid = rfid?.Trim();
            if (string.IsNullOrEmpty(rfid) || _leakMaps?.types == null || _leakMaps.maps == null) return null;

            string typeKey = ResolveTypeKey(rfid);
            if (string.IsNullOrEmpty(typeKey) || !_leakMaps.types.TryGetValue(typeKey, out var ti)
                || ti?.diagram == null || !_leakMaps.maps.TryGetValue(ti.diagram, out var entry)
                || entry?.parts == null) return null;

            foreach (var part in entry.parts)
            {
                if (part?.points == null) continue;
                foreach (var p in part.points)
                {
                    var rr = Lookup(_rfidOvr, rfid, p.name);
                    var tt = Lookup(_typeOvr, typeKey, p.name);
                    int effI = rr?.i ?? tt?.i ?? p.idx;
                    int effR = rr?.r ?? tt?.r ?? p.robot;
                    if (effI == value && effR == robot) return p.name;
                }
            }
            return null;
        }

        // "R1"/"R2" mini etiketi
        private static Border MakeRobotTag(string t) => new()
        {
            Background = BrNeutral, CornerRadius = new CornerRadius(3),
            Padding = new Thickness(5, 1, 5, 1), VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock { Text = t, FontSize = 9,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = BrG888 }
        };

        // OK oranı hücresi: 64x6 track + star-oranlı dolgu + "%96" metni.
        // ProgressBar BİLEREK kullanılmadı (tema track rengini ezebiliyor).
        private static FrameworkElement MakeRateCell(double pct)
        {
            var fg = pct >= 90 ? BrGreen : (pct >= 70 ? BrOrange : BrRed);
            double p = Math.Clamp(pct, 0, 100);
            double vis = p == 0 ? 0 : Math.Max(p, 6);   // %1–5 dolgu görünür kalsın
            var fill = new Grid();
            fill.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(vis, GridUnitType.Star) });
            fill.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100 - vis, GridUnitType.Star) });
            if (vis > 0) fill.Children.Add(new Border { Background = fg, CornerRadius = new CornerRadius(3) });
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            sp.Children.Add(new Border { Width = 64, Height = 6, CornerRadius = new CornerRadius(3),
                Background = BrTrack, VerticalAlignment = VerticalAlignment.Center, Child = fill });
            sp.Children.Add(new TextBlock { Text = $"%{p:F0}", FontSize = 10,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = fg,
                VerticalAlignment = VerticalAlignment.Center });
            return sp;
        }

        /// <summary>RFID/model başına üretim kırılımı: zebra satır, OK-oranı barı, pill rozetler,
        /// satır altında R1/R2 kaçak nokta chip şeridi (model bazında kaçak görünümü buraya entegre).</summary>
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
                    R1 = g.SelectMany(x => x.NgPointsR1 ?? new List<int>())
                          .GroupBy(p => p).Select(p => new { Nokta = p.Key, Adet = p.Count() })
                          .OrderByDescending(x => x.Adet).ThenBy(x => x.Nokta).ToList(),
                    R2 = g.SelectMany(x => x.NgPointsR2 ?? new List<int>())
                          .GroupBy(p => p).Select(p => new { Nokta = p.Key, Adet = p.Count() })
                          .OrderByDescending(x => x.Adet).ThenBy(x => x.Nokta).ToList()
                })
                .OrderByDescending(x => x.Toplam)
                .ToList();

            // Seçili ürün artık listede yoksa filtreyi bırak
            if (_selectedNgRfid != null && !groups.Any(x => x.Urun == _selectedNgRfid))
                _selectedNgRfid = null;

            StatProductCount.Text = groups.Count.ToString();
            UpdateTopNgPoints();
            ProductBreakdownPanel.Children.Clear();

            if (groups.Count == 0)
            {
                ProductBreakdownPanel.Children.Add(new TextBlock { Text = "Seçili filtrede kayıt yok",
                    FontSize = 11, Foreground = BrG666, Margin = new Thickness(8, 10, 0, 10) });
                return;
            }

            // XAML kolon başlığıyla birebir aynı: * | 44 | 106 | 50 | 50 | 54 | 44
            double[] W = { 0, 44, 106, 50, 50, 54, 44 };
            var semi = Microsoft.UI.Text.FontWeights.SemiBold;
            var bold = Microsoft.UI.Text.FontWeights.Bold;

            for (int idx = 0; idx < groups.Count; idx++)
            {
                var g = groups[idx];
                int ngToplam = g.R1.Sum(x => x.Adet) + g.R2.Sum(x => x.Adet);
                double pct = g.Toplam > 0 ? 100.0 * g.Ok / g.Toplam : 0;

                var grid = new Grid { ColumnSpacing = 4 };
                for (int i = 0; i < 7; i++)
                    grid.ColumnDefinitions.Add(new ColumnDefinition
                    { Width = W[i] == 0 ? new GridLength(1, GridUnitType.Star) : new GridLength(W[i]) });

                // [0] durum accent şeridi + model adı
                var accent = new Microsoft.UI.Xaml.Shapes.Rectangle
                {
                    Width = 3, Height = 18, RadiusX = 1.5, RadiusY = 1.5,
                    Fill = g.Nok == 0 ? BrBlue
                         : ((double)g.Nok / g.Toplam >= 0.25 ? BrRed : BrOrange)
                };
                var name = new TextBlock { Text = g.Urun, FontSize = 12, FontWeight = semi,
                    Foreground = BrWhite, TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center };
                ToolTipService.SetToolTip(name, g.Urun);   // kırpılan ad tam gösterilir
                var nameSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8,
                    VerticalAlignment = VerticalAlignment.Center };
                nameSp.Children.Add(accent); nameSp.Children.Add(name);
                grid.Children.Add(nameSp);

                // [1] ADET
                var adet = new TextBlock { Text = g.Toplam.ToString(), FontSize = 12, FontWeight = bold,
                    Foreground = BrWhite, HorizontalTextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(adet, 1); grid.Children.Add(adet);

                // [2] OK ORANI — bar + yüzde
                var rate = MakeRateCell(pct);
                Grid.SetColumn(rate, 2); grid.Children.Add(rate);

                // [3] OK / [4] NOK / [6] NG pill'leri (0 ise nötr gri)
                var okPill  = MakePill(g.Ok.ToString(),  g.Ok  > 0 ? BrOkBg  : BrNeutral, g.Ok  > 0 ? BrGreen  : BrG666);
                var nokPill = MakePill(g.Nok.ToString(), g.Nok > 0 ? BrNokBg : BrNeutral, g.Nok > 0 ? BrRed    : BrG666);
                var ngPill  = MakePill(ngToplam.ToString(), ngToplam > 0 ? BrNgBg : BrNeutral, ngToplam > 0 ? BrOrange : BrG666);
                Grid.SetColumn(okPill, 3);  grid.Children.Add(okPill);
                Grid.SetColumn(nokPill, 4); grid.Children.Add(nokPill);

                // [5] ORT. SÜRE
                var sure = new TextBlock { Text = $"{g.OrtSure:F0} sn", FontSize = 11, Foreground = BrBlue,
                    HorizontalTextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(sure, 5); grid.Children.Add(sure);
                Grid.SetColumn(ngPill, 6); grid.Children.Add(ngPill);

                // Zebra ana satır — tıklanabilir: sağ Pareto bu ürüne filtrelenir (tekrar tıkla = tümü)
                bool isSel = _selectedNgRfid == g.Urun;
                var rowBorder = new Border
                {
                    Background = isSel ? BrZebra : (idx % 2 == 0 ? BrZebra : null),
                    BorderBrush = isSel ? BrBlue : null,
                    BorderThickness = new Thickness(isSel ? 1 : 0),
                    CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 5, 8, 5), Child = grid
                };
                string modelKey = g.Urun;
                rowBorder.Tapped += (s, e2) =>
                {
                    _selectedNgRfid = (_selectedNgRfid == modelKey) ? null : modelKey;
                    UpdateProductBreakdown();   // seçim vurgusu + sağ panel birlikte yenilenir
                };
                ToolTipService.SetToolTip(rowBorder, isSel
                    ? "Filtreyi kaldırmak için tekrar tıklayın"
                    : "Bu ürünün kaçak noktalarını sağ panelde göster");
                ProductBreakdownPanel.Children.Add(rowBorder);

                // Satır altı NG chip şeridi — yalnız kaçak varsa (model bazında kaçak noktaları)
                if (ngToplam > 0)
                {
                    var items = new List<FrameworkElement>();
                    if (g.R1.Count > 0) { items.Add(MakeRobotTag("R1"));
                        items.AddRange(g.R1.Select(p => (FrameworkElement)MakeChip(
                            ResolvePointCode(g.Urun, 1, p.Nokta) ?? $"N{p.Nokta}", p.Adet, false))); }
                    if (g.R2.Count > 0) { items.Add(MakeRobotTag("R2"));
                        items.AddRange(g.R2.Select(p => (FrameworkElement)MakeChip(
                            ResolvePointCode(g.Urun, 2, p.Nokta) ?? $"N{p.Nokta}", p.Adet, true))); }

                    var strip = new StackPanel { Spacing = 3, Margin = new Thickness(22, 1, 8, 5) };
                    StackPanel line = null;
                    for (int i = 0; i < items.Count; i++)
                    {   // ölçümsüz deterministik kırılım: 9 eleman/satır
                        if (i % 9 == 0) { line = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
                                          strip.Children.Add(line); }
                        line.Children.Add(items[i]);
                    }
                    ProductBreakdownPanel.Children.Add(strip);
                }
            }
        }

        /// <summary>En sık NG noktaları — Pareto listesi: rank rozeti, orantılı bar, adet + toplam NG payı.
        /// Sol tabloda bir RFID seçiliyse yalnız o ürünün noktaları; etiketler Nokta Kütüphanesi kodlarıyla (C1-1 vb.).</summary>
        private void UpdateTopNgPoints()
        {
            if (TopNgPointsPanel == null) return;
            TopNgPointsPanel.Children.Clear();

            bool filtered = !string.IsNullOrEmpty(_selectedNgRfid);
            var recs = filtered
                ? _currentRecords.Where(r => (string.IsNullOrWhiteSpace(r.RfidTag) ? "Tanımsız" : r.RfidTag) == _selectedNgRfid).ToList()
                : _currentRecords;

            if (TopNgTitleText != null)
                TopNgTitleText.Text = filtered ? $"EN SIK KAÇAK — {_selectedNgRfid}" : "EN SIK KAÇAK NOKTALAR";

            // Her NG girişini nokta KODUNA çözümle (RFID'sine göre); eşleme yoksa "Nokta N"
            var all = new List<string>();
            foreach (var r in recs)
            {
                if (r.NgPointsR1 != null)
                    foreach (var v in r.NgPointsR1) all.Add(ResolvePointCode(r.RfidTag, 1, v) ?? $"Nokta {v}");
                if (r.NgPointsR2 != null)
                    foreach (var v in r.NgPointsR2) all.Add(ResolvePointCode(r.RfidTag, 2, v) ?? $"Nokta {v}");
            }
            int totalNg = all.Count;
            if (TopNgTotalText != null) TopNgTotalText.Text = $"{totalNg} NG";

            if (filtered)
                TopNgPointsPanel.Children.Add(new TextBlock
                {
                    Text = "ürün filtresi aktif — tümü için satıra tekrar tıklayın",
                    FontSize = 9, Foreground = BrG666, Margin = new Thickness(2, 0, 0, 2)
                });

            if (totalNg == 0)
            {
                var empty = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8,
                    HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 24, 0, 24) };
                empty.Children.Add(new FontIcon { Glyph = "", FontSize = 16, Foreground = BrGreen });
                empty.Children.Add(new TextBlock
                {
                    Text = filtered ? "Bu üründe kaçak tespit edilmedi" : "Seçili aralıkta kaçak tespit edilmedi",
                    FontSize = 12, Foreground = BrGreen, VerticalAlignment = VerticalAlignment.Center
                });
                TopNgPointsPanel.Children.Add(empty);
                return;
            }

            var freq = all.GroupBy(p => p)
                .Select(g => new { Etiket = g.Key, Adet = g.Count() })
                .OrderByDescending(x => x.Adet).ThenBy(x => x.Etiket, StringComparer.OrdinalIgnoreCase)
                .Take(8).ToList();
            int max = freq[0].Adet;
            var bold = Microsoft.UI.Text.FontWeights.Bold;

            for (int i = 0; i < freq.Count; i++)
            {
                var f = freq[i];
                var row = new Grid { ColumnSpacing = 8 };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(78) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });

                // Rank rozeti: 1. dolu kırmızı; 2.-3. koyu zemin + kırmızı çerçeve; 4+ nötr
                var rank = new Border { Width = 22, Height = 22, CornerRadius = new CornerRadius(11),
                    VerticalAlignment = VerticalAlignment.Center };
                if (i == 0)      rank.Background = BrRed;
                else if (i <= 2) { rank.Background = BrBadge; rank.BorderBrush = BrRed; rank.BorderThickness = new Thickness(1); }
                else             rank.Background = BrNeutral;
                rank.Child = new TextBlock { Text = (i + 1).ToString(), FontSize = 11, FontWeight = bold,
                    Foreground = i == 0 ? BrWhite : (i <= 2 ? BrRed : BrG888),
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                row.Children.Add(rank);

                // Nokta kodu rozeti (C1-1 vb.; eşleme yoksa "Nokta N")
                var pt = new Border { Background = BrBadge, CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(2, 3, 2, 3), VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock { Text = f.Etiket, FontSize = 11, FontWeight = bold,
                        Foreground = BrWhite, HorizontalTextAlignment = TextAlignment.Center,
                        TextTrimming = TextTrimming.CharacterEllipsis } };
                ToolTipService.SetToolTip(pt, f.Etiket);
                Grid.SetColumn(pt, 1); row.Children.Add(pt);

                // Orantılı bar: star-genişlik (panelle ölçeklenir); taban max*0.06 → en küçük frekans da görünür
                double fillW = Math.Max(f.Adet, max * 0.06);
                var fillGrid = new Grid();
                fillGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(fillW, GridUnitType.Star) });
                fillGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(0, max - fillW), GridUnitType.Star) });
                fillGrid.Children.Add(new Border { Background = BrRed, CornerRadius = new CornerRadius(4) });
                var track = new Border { Background = BrPTrack, CornerRadius = new CornerRadius(4),
                    Height = 18, VerticalAlignment = VerticalAlignment.Center, Child = fillGrid };
                Grid.SetColumn(track, 2); row.Children.Add(track);

                // Adet + toplam NG payı
                var num = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                num.Children.Add(new TextBlock { Text = $"{f.Adet}×", FontSize = 14, FontWeight = bold,
                    Foreground = BrRed, HorizontalTextAlignment = TextAlignment.Right });
                num.Children.Add(new TextBlock { Text = $"%{100.0 * f.Adet / totalNg:F0}", FontSize = 9,
                    Foreground = BrG666, HorizontalTextAlignment = TextAlignment.Right });
                Grid.SetColumn(num, 3); row.Children.Add(num);

                TopNgPointsPanel.Children.Add(row);
            }
        }

        // NOT: Model bazında kaçak noktaları artık ayrı panel değil — UpdateProductBreakdown
        // içindeki satır altı R1/R2 chip şeritleri olarak gösteriliyor.

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
  h2 {{ font-size: 14px; color: #333; margin: 22px 0 6px 0; border-left: 4px solid #00A4EF; padding-left: 8px; }}
  .bar {{ background: #eee; border-radius: 4px; height: 12px; width: 100%; }}
  .bar-fill {{ background: #E74C3C; border-radius: 4px; height: 12px; }}
  .chip {{ display: inline-block; padding: 1px 7px; border-radius: 4px; font-weight: bold; font-size: 10px; margin: 1px; }}
  .chip-r1 {{ background: #FFEBEE; color: #C62828; }} .chip-r2 {{ background: #FFF3E0; color: #E65100; }}
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
</div>";

            // ═══ ÖZET BÖLÜMLERİ (CSV tam raporuyla aynı içerik) ═══
            var modelGroups = _currentRecords
                .GroupBy(r => string.IsNullOrWhiteSpace(r.RfidTag) ? "Tanımsız" : r.RfidTag)
                .Select(g => new
                {
                    Model = g.Key,
                    Toplam = g.Count(),
                    Ok = g.Count(x => x.OverallResult == "OK"),
                    Nok = g.Count(x => x.OverallResult == "NOK"),
                    OrtSure = g.Average(x => x.CycleTime),
                    R1 = g.SelectMany(x => x.NgPointsR1 ?? new List<int>()).GroupBy(p => p)
                          .Select(p => new { Nokta = p.Key, Adet = p.Count() })
                          .OrderByDescending(x => x.Adet).ThenBy(x => x.Nokta).ToList(),
                    R2 = g.SelectMany(x => x.NgPointsR2 ?? new List<int>()).GroupBy(p => p)
                          .Select(p => new { Nokta = p.Key, Adet = p.Count() })
                          .OrderByDescending(x => x.Adet).ThenBy(x => x.Nokta).ToList()
                })
                .OrderByDescending(x => x.Toplam)
                .ToList();

            // 1) RFID bazında üretim
            html += "<h2>📦 RFID Bazında Üretim</h2><table><thead><tr><th>RFID / Model</th><th>Adet</th><th>OK</th><th>NOK</th><th>Başarı</th><th>Ort. Süre</th><th>Kaçak</th></tr></thead><tbody>";
            foreach (var gM in modelGroups)
            {
                int ngT = gM.R1.Sum(x => x.Adet) + gM.R2.Sum(x => x.Adet);
                string basari = gM.Toplam > 0 ? $"%{(gM.Ok * 100.0 / gM.Toplam):F0}" : "-";
                html += $"<tr><td><b>{gM.Model}</b></td><td>{gM.Toplam}</td><td class='ok'><b>{gM.Ok}</b></td><td class='nok'><b>{gM.Nok}</b></td><td>{basari}</td><td>{gM.OrtSure:F0} sn</td><td class='nok'><b>{ngT}</b></td></tr>";
            }
            html += "</tbody></table>";

            // 2) En sık kaçak noktalar (Pareto)
            var allNg = _currentRecords
                .SelectMany(r => (r.NgPointsR1 ?? new List<int>()).Concat(r.NgPointsR2 ?? new List<int>()))
                .ToList();
            html += "<h2>⚠️ En Sık Kaçak Noktalar</h2>";
            if (allNg.Count == 0)
            {
                html += "<p class='ok'><b>✓ Seçili aralıkta kaçak tespit edilmedi</b></p>";
            }
            else
            {
                var freqAll = allNg.GroupBy(p => p)
                    .Select(g2 => new { Nokta = g2.Key, Adet = g2.Count() })
                    .OrderByDescending(x => x.Adet).ThenBy(x => x.Nokta).ToList();
                int maxA = freqAll[0].Adet;
                html += "<table style='max-width:560px'><thead><tr><th>#</th><th>Nokta</th><th style='width:50%'>Dağılım</th><th>Adet</th><th>Pay</th></tr></thead><tbody>";
                for (int fi = 0; fi < freqAll.Count; fi++)
                {
                    var f = freqAll[fi];
                    double w = 100.0 * f.Adet / maxA;
                    html += $"<tr><td><b>{fi + 1}.</b></td><td><b>Nokta {f.Nokta}</b></td>" +
                            $"<td><div class='bar'><div class='bar-fill' style='width:{w:F0}%'></div></div></td>" +
                            $"<td class='nok'><b>{f.Adet}×</b></td><td>%{(100.0 * f.Adet / allNg.Count):F0}</td></tr>";
                }
                html += "</tbody></table>";
            }

            // 3) Model bazında tespit edilen kaçak noktaları
            html += "<h2>🎯 Model Bazında Tespit Edilen Kaçak Noktaları</h2>";
            var ngModels = modelGroups
                .Where(m => m.R1.Count > 0 || m.R2.Count > 0)
                .OrderByDescending(m => m.R1.Sum(x => x.Adet) + m.R2.Sum(x => x.Adet))
                .ToList();
            if (ngModels.Count == 0)
            {
                html += "<p class='ok'><b>✓ Kaçak tespit edilen model yok</b></p>";
            }
            else
            {
                html += "<table><thead><tr><th>Model</th><th>Robot 1</th><th>Robot 2</th><th>Toplam</th></tr></thead><tbody>";
                foreach (var m in ngModels)
                {
                    string r1c = m.R1.Count > 0 ? string.Join(" ", m.R1.Select(p => $"<span class='chip chip-r1'>N{p.Nokta} ×{p.Adet}</span>")) : "-";
                    string r2c = m.R2.Count > 0 ? string.Join(" ", m.R2.Select(p => $"<span class='chip chip-r2'>N{p.Nokta} ×{p.Adet}</span>")) : "-";
                    int t = m.R1.Sum(x => x.Adet) + m.R2.Sum(x => x.Adet);
                    html += $"<tr><td><b>{m.Model}</b></td><td>{r1c}</td><td>{r2c}</td><td class='nok'><b>{t}×</b></td></tr>";
                }
                html += "</tbody></table>";
            }

            // ═══ DETAY KAYITLAR ═══
            html += @"<h2>📋 Detay Kayıtlar</h2>
<table><thead><tr>
  <th>Tarih</th><th>Saat</th><th>Sonuç</th><th>İstasyon</th><th>RFID</th><th>Ürün</th>
  <th>OK</th><th>NOK</th><th>Başarı</th><th>Kaçak R1</th><th>Kaçak R2</th><th>Ofs X</th><th>Ofs Y</th><th>Ofs Z</th><th>Süre</th>
</tr></thead><tbody>";

            foreach (var r in _currentRecords.Take(500))
            {
                string badge = r.OverallResult == "OK" ? "badge-ok" : "badge-nok";
                string ngR1 = (r.NgPointsR1 != null && r.NgPointsR1.Count > 0) ? string.Join(",", r.NgPointsR1) : "-";
                string ngR2 = (r.NgPointsR2 != null && r.NgPointsR2.Count > 0) ? string.Join(",", r.NgPointsR2) : "-";
                html += $@"<tr>
  <td>{r.DateStr}</td><td>{r.TimeStr}</td>
  <td><span class='badge {badge}'>{r.OverallResult}</span></td>
  <td>{r.StationName}</td><td>{r.RfidTag}</td><td>{r.ProductName}</td>
  <td>{r.OkCount}</td><td>{r.NokCount}</td><td>{r.SuccessRate}</td>
  <td>{ngR1}</td><td>{ngR2}</td>
  <td>{r.OffsetX:F3}</td><td>{r.OffsetY:F3}</td><td>{r.OffsetZ:F3}</td><td>{r.CycleTime:F1}s</td>
</tr>";
            }

            if (_currentRecords.Count > 500)
                html += $"<tr><td colspan='15' style='text-align:center;color:#888;'>... ve {_currentRecords.Count - 500} kayıt daha (CSV ile tamamını alabilirsiniz)</td></tr>";

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
        // Kullanıcı tanımlı RFID → tip eşlemesi (Excel'de olmayan yeni modeller için).
        // LeakMaps.json'daki yerleşik 'models' eşlemesini tamamlar/ezerek diyagram gösterimini açar.
        private Dictionary<string, string> _modelOvr = new(StringComparer.OrdinalIgnoreCase);
        private static readonly string _pointMapPath =
            Path.Combine(GlobalData.ConfigBaseDir, "LeakPointMapping.json");
        // Kullanıcı kaçak-harita editörü deposu
        private UserMapStore _userMap = new();
        private static readonly string _userMapPath = Path.Combine(GlobalData.ConfigBaseDir, "LeakMapsUser.json");
        private static readonly string _userImgDir = Path.Combine(GlobalData.ConfigBaseDir, "LeakMapImages");
        private string _lmDiagram;   // açık diyagram anahtarı (editör için)
        private bool _lmMarkMode;    // bölge işaretleme modu: resme tıkla = nokta ekle
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
                    _modelOvr = new(of?.models ?? new(), StringComparer.OrdinalIgnoreCase);
                }
            }
            catch { _typeOvr = new(); _rfidOvr = new(); _modelOvr = new(StringComparer.OrdinalIgnoreCase); }

            // Kullanıcı editör katmanı
            try
            {
                if (File.Exists(_userMapPath))
                    _userMap = JsonSerializer.Deserialize<UserMapStore>(File.ReadAllText(_userMapPath)) ?? new();
            }
            catch { _userMap = new(); }
            _userMap.addedParts ??= new();
            _userMap.addedPoints ??= new();
            _userMap.hidden ??= new();
        }

        private void SaveOvr()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_pointMapPath));
                File.WriteAllText(_pointMapPath, JsonSerializer.Serialize(
                    new OvrFile { types = _typeOvr, rfids = _rfidOvr, models = new Dictionary<string, string>(_modelOvr) },
                    new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[LEAKMAP] mapping kayıt hatası: {ex.Message}"); }
        }

        private static PointOvr Lookup(Dictionary<string, Dictionary<string, PointOvr>> d, string key, string name)
            => (key != null && d.TryGetValue(key, out var m) && m.TryGetValue(name, out var o)) ? o : null;

        private void SaveUserMap()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_userMapPath));
                File.WriteAllText(_userMapPath, JsonSerializer.Serialize(_userMap, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[LEAKMAP] kullanıcı katmanı kayıt hatası: {ex.Message}"); }
        }

        /// <summary>Asset diyagramı + kullanıcı katmanını birleştirip ETKİN entry üretir (derin kopya — asset bozulmaz).</summary>
        private LeakMapEntry BuildEffectiveEntry(string diagram)
        {
            var eff = new LeakMapEntry { parts = new List<LeakPart>() };
            if (string.IsNullOrEmpty(diagram)) return eff;

            // Yerleşik parçalar (gizlenenler hariç)
            if (_leakMaps?.maps != null && _leakMaps.maps.TryGetValue(diagram, out var raw) && raw?.parts != null)
                foreach (var p in raw.parts)
                {
                    if (_userMap.hidden.Contains($"part:{diagram}::{p.title}")) continue;
                    eff.parts.Add(CloneFilteredPart(diagram, p, false));
                }

            // Kullanıcı eklediği parçalar (resimler)
            if (_userMap.addedParts.TryGetValue(diagram, out var ups) && ups != null)
                foreach (var p in ups)
                    eff.parts.Add(CloneFilteredPart(diagram, p, true));

            return eff;
        }

        private LeakPart CloneFilteredPart(string diagram, LeakPart src, bool userPart)
        {
            var np = new LeakPart { image = src.image, title = src.title, w = src.w, h = src.h, isUser = userPart, points = new List<LeakPoint>() };
            string key = $"{diagram}::{src.title}";
            if (src.points != null)
                foreach (var pt in src.points)
                {
                    if (!userPart && _userMap.hidden.Contains($"point:{diagram}::{src.title}::{pt.name}")) continue;
                    np.points.Add(new LeakPoint { name = pt.name, robot = pt.robot, idx = pt.idx, x = pt.x, y = pt.y, isUser = userPart });
                }
            // Yerleşik parçaya kullanıcı eklediği noktalar
            if (_userMap.addedPoints.TryGetValue(key, out var aps) && aps != null)
                foreach (var pt in aps)
                    np.points.Add(new LeakPoint { name = pt.name, robot = pt.robot, idx = pt.idx, x = pt.x, y = pt.y, isUser = true });
            return np;
        }

        /// <summary>Açık diyagramı kullanıcı katmanıyla yeniden kurar ve görseli + lejantı tazeler.</summary>
        private void RefreshLeakEditor()
        {
            if (string.IsNullOrEmpty(_lmDiagram)) return;
            _lmEntry = BuildEffectiveEntry(_lmDiagram);
            BuildLeakParts();
            BuildLeakLegend();
        }

        /// <summary>RFID → typeKey çözümü: önce kullanıcı eşlemesi (_modelOvr), sonra yerleşik LeakMaps.models.
        /// Yeni RFID'ler (Excel'de olmayan) uygulama içinden bir tipe atanınca diyagramları açılır.</summary>
        private string ResolveTypeKey(string rfid)
        {
            if (string.IsNullOrWhiteSpace(rfid)) return null;
            rfid = rfid.Trim();
            if (_modelOvr != null && _modelOvr.TryGetValue(rfid, out var ut) && !string.IsNullOrEmpty(ut)) return ut;
            var m = _leakMaps?.models;
            if (m == null) return null;
            if (m.TryGetValue(rfid, out var t) && !string.IsNullOrEmpty(t)) return t;
            return m.FirstOrDefault(kv => string.Equals(kv.Key, rfid, StringComparison.OrdinalIgnoreCase)).Value;
        }

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

        // Etkin açıklama: RFID override > tip override > yok
        private string EffDesc(LeakPoint p)
        {
            var rr = Lookup(_rfidOvr, _lmRfid, p.name);
            if (!string.IsNullOrEmpty(rr?.d)) return rr.d;
            var tt = Lookup(_typeOvr, _lmTypeKey, p.name);
            return string.IsNullOrEmpty(tt?.d) ? null : tt.d;
        }

        /// <summary>Nokta açıklamasını düzenleme kapsamına yazar (boş = sil).</summary>
        private void SetOvrDesc(string name, string d)
        {
            var target = (_lmRfid != null) ? _rfidOvr : _typeOvr;
            string key = (_lmRfid != null) ? _lmRfid : _lmTypeKey;
            if (key == null) return;
            if (!target.TryGetValue(key, out var m)) { m = new(); target[key] = m; }
            if (!m.TryGetValue(name, out var o)) { o = new PointOvr(); m[name] = o; }
            o.d = string.IsNullOrWhiteSpace(d) ? null : d.Trim();
            SaveOvr();
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
            if (_leakMaps?.maps == null || _leakMaps.types == null) return false;
            string rfid = record.RfidTag?.Trim();
            if (string.IsNullOrEmpty(rfid)) return false;

            // RFID → tip → diyagram (kullanıcı eşlemesi öncelikli). Override TİP bazında.
            string typeKey = ResolveTypeKey(rfid);
            if (string.IsNullOrEmpty(typeKey) || _leakMaps.types == null
                || !_leakMaps.types.TryGetValue(typeKey, out var ti) || string.IsNullOrEmpty(ti?.diagram))
                return false;
            var entry = BuildEffectiveEntry(ti.diagram);
            if (entry.parts == null || entry.parts.Count == 0) return false;

            _lmRecord = record; _lmTypeKey = typeKey; _lmRfid = rfid; _lmDiagram = ti.diagram; _lmEntry = entry; _lmEdit = false; _lmMarkMode = false;

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

            // Başlık satırı (+ düzenlemede parça sil)
            var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, HorizontalAlignment = HorizontalAlignment.Center };
            titleRow.Children.Add(new TextBlock
            {
                Text = part.title ?? "",
                FontSize = vertical ? 13 : 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = ColorFromHex(part.isUser ? "#4CAF50" : "#00A4EF"),
                VerticalAlignment = VerticalAlignment.Center
            });
            if (_lmEdit)
            {
                var delPart = new Button { Content = "✕", FontSize = 10, Padding = new Thickness(5, 0, 5, 0), MinWidth = 0, MinHeight = 0, Background = ColorFromHex("#2A1212"), Foreground = ColorFromHex("#E74C3C"), VerticalAlignment = VerticalAlignment.Center };
                ToolTipService.SetToolTip(delPart, "Bu görseli/parçayı kaldır");
                var capPart = part;
                delPart.Click += (s, _) => DeletePart_Click(capPart);
                titleRow.Children.Add(delPart);
            }
            col.Children.Add(titleRow);

            var grid = new Grid { Width = part.w, Height = part.h };
            grid.Children.Add(new Image { Source = ResolveImageSource(part.image), Stretch = Stretch.Fill });
            var canvas = new Canvas { Width = part.w, Height = part.h };
            // Bölge işaretleme modunda tıkla = nokta ekle (tüm alan tıklanabilsin)
            if (_lmEdit && _lmMarkMode)
            {
                canvas.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                var capPart2 = part; var capCanvas = canvas;
                canvas.Tapped += (s, e) => OnPartTapped(capPart2, capCanvas, e);
            }
            grid.Children.Add(canvas);

            var vb = new Viewbox { Stretch = Stretch.Uniform, Child = grid };
            if (vertical) { vb.MaxHeight = 600; vb.HorizontalAlignment = HorizontalAlignment.Center; }
            else { vb.VerticalAlignment = VerticalAlignment.Top; }

            col.Children.Add(new Border
            {
                Background = ColorFromHex("#0A0A0A"),
                CornerRadius = new CornerRadius(8),
                BorderBrush = ColorFromHex(_lmMarkMode ? "#4CAF50" : "#222222"),
                BorderThickness = new Thickness(_lmMarkMode ? 2 : 1),
                Padding = new Thickness(4),
                Child = vb
            });

            _partCanvases.Add((canvas, part));
            RenderPartMarkers(canvas, part);
            return col;
        }

        /// <summary>Görsel kaynağı: önce asset (ms-appx), yoksa kullanıcı klasörü (Config\LeakMapImages).</summary>
        private static Microsoft.UI.Xaml.Media.ImageSource ResolveImageSource(string image)
        {
            if (string.IsNullOrEmpty(image)) return null;
            try
            {
                string assetPath = Path.Combine(AppContext.BaseDirectory, "Assets", "LeakMaps", image);
                if (File.Exists(assetPath))
                    return new BitmapImage(new Uri($"ms-appx:///Assets/LeakMaps/{image}"));
                string userPath = Path.Combine(_userImgDir, image);
                if (File.Exists(userPath))
                    return new BitmapImage(new Uri(userPath));
            }
            catch { }
            return null;
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

                // Açıklama tanımlıysa işaretçi + etikette tooltip olarak göster
                string mDesc = EffDesc(p);
                if (!string.IsNullOrEmpty(mDesc))
                {
                    ToolTipService.SetToolTip(ell, $"{p.name} — {mDesc}");
                    ToolTipService.SetToolTip(lbl, $"{p.name} — {mDesc}");
                }
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

            // Noktalar robot bazında gruplanır ve ETKİN INT'e göre sıralanır → tarama sırası
            // net okunur (INT 1 en üstte, 2 altında...). INT/robot değişince liste yeniden sıralanır.
            var allPts = _lmEntry.parts.Where(pt => pt?.points != null)
                .SelectMany(pt => pt.points.Select(p => (part: pt, p)))
                .ToList();

            foreach (var robotGrp in allPts.GroupBy(x => EffRobot(x.p)).OrderBy(grp => grp.Key))
            {
                LeakPointLegendPanel.Children.Add(new TextBlock
                {
                    Text = $"ROBOT {robotGrp.Key} — TARAMA SIRASI",
                    FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    Foreground = ColorFromHex(robotGrp.Key == 2 ? "#F39C12" : "#E74C3C"),
                    Margin = new Thickness(0, 8, 0, 2)
                });

                // Aynı robotta aynı INT birden fazla noktaya atanmışsa → çakışma (kırmızı vurgu)
                var dupInts = robotGrp.GroupBy(x => EffInt(x.p)).Where(grp => grp.Count() > 1)
                    .Select(grp => grp.Key).ToHashSet();

                foreach (var (part, p) in robotGrp
                    .OrderBy(x => EffInt(x.p))
                    .ThenBy(x => x.p.name, StringComparer.OrdinalIgnoreCase))
                {
                    bool ng = IsNgPoint(_lmRecord, p);
                    string desc = EffDesc(p);
                    bool dupInt = dupInts.Contains(EffInt(p));

                    if (_lmEdit)
                    {
                        // Sabit kolonlu grid: 14 | * | 70 | 48 — panele her zaman sığar
                        // (WinUI TextBox/ComboBox varsayılan MinWidth=64'tür; aşağıda MinWidth=0/70 ile eziliyor)
                        var row = new Grid { ColumnSpacing = 4, Margin = new Thickness(0, 2, 0, 0) };
                        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
                        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 0 });
                        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
                        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
                        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });

                        var dot = new Microsoft.UI.Xaml.Shapes.Ellipse { Width = 11, Height = 11, Stroke = ng ? red : green, StrokeThickness = 2.5, Fill = ng ? redFill : greenFill, VerticalAlignment = VerticalAlignment.Center };
                        row.Children.Add(dot);

                        var nameTb = new TextBlock { Text = p.name, FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = ColorFromHex("#DDDDDD"), VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
                        ToolTipService.SetToolTip(nameTb, $"{p.name} — {part.title}");
                        Grid.SetColumn(nameTb, 1); row.Children.Add(nameTb);

                        var rcombo = new ComboBox { Width = 64, MinWidth = 64, Height = 32, FontSize = 11, Padding = new Thickness(7, 4, 0, 4), Tag = p, VerticalAlignment = VerticalAlignment.Center };
                        rcombo.Items.Add("R1"); rcombo.Items.Add("R2");
                        rcombo.SelectedIndex = EffRobot(p) == 2 ? 1 : 0;
                        rcombo.SelectionChanged += Robot_Changed;
                        Grid.SetColumn(rcombo, 2); row.Children.Add(rcombo);

                        var box = new TextBox
                        {
                            Text = EffInt(p).ToString(),
                            Width = 44, MinWidth = 0, Height = 32, FontSize = 12,
                            Background = ColorFromHex("#202020"), Foreground = ColorFromHex("#FFFFFF"),
                            BorderBrush = dupInt ? red : ColorFromHex("#00A4EF"),
                            VerticalAlignment = VerticalAlignment.Center, Tag = p
                        };
                        if (dupInt) ToolTipService.SetToolTip(box, "⚠ Bu INT aynı robotta birden fazla noktaya atanmış!");
                        box.LostFocus += PointInt_Commit;
                        box.KeyDown += (s, ev) => { if (ev.Key == Windows.System.VirtualKey.Enter) PointInt_Commit(s, null); };
                        Grid.SetColumn(box, 3); row.Children.Add(box);

                        // Nokta sil/gizle (✕)
                        var delPt = new Button { Content = "✕", FontSize = 10, Padding = new Thickness(2, 0, 2, 0), MinWidth = 0, MinHeight = 0, Background = ColorFromHex("#2A1212"), Foreground = ColorFromHex("#E74C3C"), VerticalAlignment = VerticalAlignment.Center };
                        var capPart = part; var capP = p;
                        delPt.Click += (s, ev) => DeletePoint_Click(capPart, capP);
                        ToolTipService.SetToolTip(delPt, capP.isUser ? "Bu noktayı sil" : "Bu yerleşik noktayı gizle");
                        Grid.SetColumn(delPt, 4); row.Children.Add(delPt);

                        LeakPointLegendPanel.Children.Add(row);

                        // Açıklama girişi (opsiyonel) — nokta adının altında tam genişlik
                        var descBox = new TextBox
                        {
                            Text = desc ?? "",
                            PlaceholderText = "Açıklama (opsiyonel)...",
                            FontSize = 11, Height = 30,
                            Background = ColorFromHex("#1A1A1A"), Foreground = ColorFromHex("#CCCCCC"),
                            BorderBrush = ColorFromHex("#333333"),
                            Margin = new Thickness(18, 1, 0, 4), Tag = p
                        };
                        descBox.LostFocus += PointDesc_Commit;
                        descBox.KeyDown += (s, ev) => { if (ev.Key == Windows.System.VirtualKey.Enter) PointDesc_Commit(s, null); };
                        LeakPointLegendPanel.Children.Add(descBox);
                    }
                    else
                    {
                        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 1, 0, 1) };
                        row.Children.Add(new Microsoft.UI.Xaml.Shapes.Ellipse { Width = 11, Height = 11, Stroke = ng ? red : green, StrokeThickness = 2.5, Fill = ng ? redFill : greenFill, VerticalAlignment = VerticalAlignment.Center });
                        row.Children.Add(new TextBlock { Text = p.name, FontSize = 12, Width = 50, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = ColorFromHex("#DDDDDD"), VerticalAlignment = VerticalAlignment.Center });
                        row.Children.Add(new TextBlock { Text = $"R{EffRobot(p)}", FontSize = 10, Width = 24, Foreground = ColorFromHex("#777777"), VerticalAlignment = VerticalAlignment.Center });
                        row.Children.Add(new Border
                        {
                            Background = ColorFromHex("#1A1A1A"), CornerRadius = new CornerRadius(4),
                            Padding = new Thickness(6, 1, 6, 1), VerticalAlignment = VerticalAlignment.Center,
                            Child = new TextBlock { Text = "int " + EffInt(p), FontSize = 10, Foreground = dupInt ? red : ColorFromHex("#9E9E9E") }
                        });
                        row.Children.Add(new TextBlock { Text = ng ? "KAÇAK" : "OK", FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = ng ? red : green, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) });
                        LeakPointLegendPanel.Children.Add(row);

                        // Açıklama varsa nokta altında küçük gri satır
                        if (!string.IsNullOrEmpty(desc))
                            LeakPointLegendPanel.Children.Add(new TextBlock
                            {
                                Text = desc, FontSize = 10, Foreground = ColorFromHex("#8A8A8A"),
                                FontStyle = Windows.UI.Text.FontStyle.Italic,
                                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(21, 0, 0, 3)
                            });
                    }
                }
            }
        }

        private void Robot_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb && cb.Tag is LeakPoint p)
            {
                SetOvr(p.name, null, cb.SelectedIndex + 1);
                RenderAllMarkers();
                // Liste tarama sırasına göre yeniden dizilsin (event bitince — güvenli)
                DispatcherQueue.TryEnqueue(BuildLeakLegend);
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
                    // "1 dediğim başa gelsin": liste etkin INT sırasına göre yeniden dizilir
                    DispatcherQueue.TryEnqueue(BuildLeakLegend);
                }
                else
                {
                    tb.Text = EffInt(p).ToString();
                }
            }
        }

        private void PointDesc_Commit(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.Tag is LeakPoint p)
            {
                SetOvrDesc(p.name, tb.Text);
                RenderAllMarkers(); // tooltip'ler güncellensin
            }
        }

        private void ToggleLeakEdit_Click(object sender, RoutedEventArgs e)
        {
            _lmEdit = !_lmEdit;
            if (!_lmEdit) _lmMarkMode = false;
            BuildLeakParts();   // tap handler / parça-sil butonları edit durumuna göre
            BuildLeakLegend();
            UpdateLeakEditButton();
        }

        private void UpdateLeakEditButton()
        {
            if (LeakEditBtnText != null) LeakEditBtnText.Text = _lmEdit ? "Bitti" : "INT Düzenle";
            var _ev = _lmEdit ? Visibility.Visible : Visibility.Collapsed;
            if (LeakAddImageBtn != null) LeakAddImageBtn.Visibility = _ev;
            if (LeakMarkBtn != null)
            {
                LeakMarkBtn.Visibility = _ev;
                if (LeakMarkBtnText != null) LeakMarkBtnText.Text = _lmMarkMode ? "İşaretleme: AÇIK" : "Bölge İşaretle";
                LeakMarkBtn.Background = ColorFromHex(_lmMarkMode ? "#1B3D1B" : "#1E2A38");
            }
            if (LeakEditBtnIcon != null) LeakEditBtnIcon.Glyph = _lmEdit ? "" : "";
        }

        // ═══ EDİTÖR: RESİM EKLE / BÖLGE İŞARETLE / SİL ═══

        private void BtnLeakMarkMode_Click(object sender, RoutedEventArgs e)
        {
            if (!_lmEdit) return;
            _lmMarkMode = !_lmMarkMode;
            BuildLeakParts();        // canvas tap handler ekle/çıkar
            UpdateLeakEditButton();
        }

        private async void BtnAddLeakImage_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_lmDiagram)) return;
            try
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker { SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop };
                picker.FileTypeFilter.Add(".png"); picker.FileTypeFilter.Add(".jpg"); picker.FileTypeFilter.Add(".jpeg");
                var window = (Application.Current as App)?.MainWindow;
                if (window != null) WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(window));

                var file = await picker.PickSingleFileAsync();
                if (file == null) return;

                Directory.CreateDirectory(_userImgDir);
                string ext = Path.GetExtension(file.Path);
                string safe = new string((_lmDiagram ?? "d").Where(c => char.IsLetterOrDigit(c)).ToArray());
                string destName = $"user_{safe}_{DateTime.Now:yyyyMMdd_HHmmss}{ext}";
                string dest = Path.Combine(_userImgDir, destName);
                File.Copy(file.Path, dest, true);

                double w = 1000, h = 700;
                try
                {
                    using var s = await file.OpenReadAsync();
                    var dec = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(s);
                    w = dec.PixelWidth; h = dec.PixelHeight;
                }
                catch { }

                var titleBox = new TextBox { Text = Path.GetFileNameWithoutExtension(file.Path), MinWidth = 240 };
                var dlg = new ContentDialog
                {
                    Title = "Görsel Başlığı (parça adı)",
                    Content = titleBox,
                    PrimaryButtonText = "Ekle", CloseButtonText = "İptal", DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot, RequestedTheme = ElementTheme.Dark
                };
                if (await dlg.ShowAsync() != ContentDialogResult.Primary)
                {
                    try { File.Delete(dest); } catch { }
                    return;
                }
                string title = MakeUniquePartTitle(string.IsNullOrWhiteSpace(titleBox.Text) ? Path.GetFileNameWithoutExtension(file.Path) : titleBox.Text.Trim());

                if (!_userMap.addedParts.TryGetValue(_lmDiagram, out var list)) { list = new(); _userMap.addedParts[_lmDiagram] = list; }
                list.Add(new LeakPart { image = destName, title = title, w = w, h = h, points = new List<LeakPoint>() });
                SaveUserMap();
                _lmMarkMode = true;   // ekledikten sonra direkt işaretlemeye hazır
                RefreshLeakEditor();
                UpdateLeakEditButton();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[LEAKMAP] resim ekleme hatası: {ex.Message}"); }
        }

        private async void OnPartTapped(LeakPart part, Canvas canvas, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (!_lmEdit || !_lmMarkMode || part == null || string.IsNullOrEmpty(_lmDiagram)) return;
            var pos = e.GetPosition(canvas);
            double xf = part.w > 0 ? Math.Clamp(pos.X / part.w, 0, 1) : 0;
            double yf = part.h > 0 ? Math.Clamp(pos.Y / part.h, 0, 1) : 0;

            var nameBox = new TextBox { Text = SuggestPointName(part), MinWidth = 160 };
            var robotCombo = new ComboBox { MinWidth = 90 }; robotCombo.Items.Add("R1"); robotCombo.Items.Add("R2"); robotCombo.SelectedIndex = 0;
            var intBox = new TextBox { Text = "0", MinWidth = 70 };
            var sp = new StackPanel { Spacing = 8, MinWidth = 300 };
            sp.Children.Add(new TextBlock { Text = $"Konum: %{xf * 100:F0} , %{yf * 100:F0}  ·  {part.title}", FontSize = 11, Foreground = ColorFromHex("#888888") });
            var r1 = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            r1.Children.Add(new TextBlock { Text = "Kod:", Width = 50, VerticalAlignment = VerticalAlignment.Center }); r1.Children.Add(nameBox);
            var r2 = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            r2.Children.Add(new TextBlock { Text = "Robot:", Width = 50, VerticalAlignment = VerticalAlignment.Center }); r2.Children.Add(robotCombo);
            r2.Children.Add(new TextBlock { Text = "INT:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) }); r2.Children.Add(intBox);
            sp.Children.Add(r1); sp.Children.Add(r2);

            var dlg = new ContentDialog
            {
                Title = "Yeni Nokta İşaretle",
                Content = sp,
                PrimaryButtonText = "Ekle", CloseButtonText = "İptal", DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot, RequestedTheme = ElementTheme.Dark
            };
            if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

            string name = MakeUniquePointName(part, string.IsNullOrWhiteSpace(nameBox.Text) ? SuggestPointName(part) : nameBox.Text.Trim());
            int robot = robotCombo.SelectedIndex == 1 ? 2 : 1;
            int iv = int.TryParse(intBox.Text?.Trim(), out var v) ? v : 0;

            string key = $"{_lmDiagram}::{part.title}";
            if (!_userMap.addedPoints.TryGetValue(key, out var pts)) { pts = new(); _userMap.addedPoints[key] = pts; }
            pts.Add(new LeakPoint { name = name, robot = robot, idx = iv, x = xf, y = yf });
            SaveUserMap();
            RefreshLeakEditor();
        }

        private void DeletePart_Click(LeakPart part)
        {
            if (part == null || string.IsNullOrEmpty(_lmDiagram)) return;
            if (part.isUser)
            {
                if (_userMap.addedParts.TryGetValue(_lmDiagram, out var ups))
                    ups.RemoveAll(x => x.title == part.title && x.image == part.image);
                _userMap.addedPoints.Remove($"{_lmDiagram}::{part.title}");
                try { var ip = Path.Combine(_userImgDir, part.image ?? ""); if (File.Exists(ip)) File.Delete(ip); } catch { }
            }
            else
            {
                if (!_userMap.hidden.Contains($"part:{_lmDiagram}::{part.title}"))
                    _userMap.hidden.Add($"part:{_lmDiagram}::{part.title}");
            }
            SaveUserMap();
            RefreshLeakEditor();
        }

        private void DeletePoint_Click(LeakPart part, LeakPoint p)
        {
            if (part == null || p == null || string.IsNullOrEmpty(_lmDiagram)) return;
            string key = $"{_lmDiagram}::{part.title}";
            if (p.isUser)
            {
                if (_userMap.addedPoints.TryGetValue(key, out var l)) l.RemoveAll(x => x.name == p.name);
            }
            else
            {
                string h = $"point:{_lmDiagram}::{part.title}::{p.name}";
                if (!_userMap.hidden.Contains(h)) _userMap.hidden.Add(h);
            }
            SaveUserMap();
            RefreshLeakEditor();
        }

        private static string SuggestPointName(LeakPart part) => $"Y{(part?.points?.Count ?? 0) + 1}";

        private string MakeUniquePointName(LeakPart part, string name)
        {
            var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (_lmEntry?.parts != null)
                foreach (var pp in _lmEntry.parts)
                    if (pp.points != null) foreach (var pt in pp.points) existing.Add(pt.name);
            string baseName = name; int i = 2;
            while (existing.Contains(name)) name = $"{baseName}-{i++}";
            return name;
        }

        private string MakeUniquePartTitle(string title)
        {
            var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (_lmEntry?.parts != null) foreach (var pp in _lmEntry.parts) existing.Add(pp.title);
            string baseName = title; int i = 2;
            while (existing.Contains(title)) title = $"{baseName} ({i++})";
            return title;
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
            var bold = Microsoft.UI.Text.FontWeights.Bold;
            var orderedTypes = _leakMaps.types.OrderBy(k => TypeNum(k.Key)).ToList();

            // ── Yeni RFID atama formu ──
            var rfidBox = new TextBox { PlaceholderText = "RFID kodu (örn. ARXF71V1B)", MinWidth = 0, FontSize = 12, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Stretch };
            var typeCombo = new ComboBox { MinWidth = 0, FontSize = 12, PlaceholderText = "Tip / diyagram seç", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Stretch };
            foreach (var kv in orderedTypes)
                typeCombo.Items.Add(new ComboBoxItem { Content = $"{kv.Key} — {kv.Value.name}", Tag = kv.Key });
            var addBtn = new Button { Content = "Ata", Background = ColorFromHex("#1E6F3E"), Foreground = ColorFromHex("#FFFFFF"), FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
            var addStatus = new TextBlock { FontSize = 10, Foreground = ColorFromHex("#888888"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) };

            var listPanel = new StackPanel { Spacing = 6 };

            // Manuel sarmalı chip akışı (WinUI'de WrapPanel yok)
            StackPanel ChipFlow(List<FrameworkElement> chips)
            {
                var flow = new StackPanel { Spacing = 4 };
                StackPanel line = null;
                for (int i = 0; i < chips.Count; i++)
                {
                    if (i % 3 == 0) { line = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 }; flow.Children.Add(line); }
                    line.Children.Add(chips[i]);
                }
                return flow;
            }

            void Rebuild()
            {
                listPanel.Children.Clear();
                foreach (var kv in orderedTypes)
                {
                    string tk = kv.Key;
                    var builtins = (_leakMaps.models ?? new()).Where(m => m.Value == tk).Select(m => m.Key).ToList();
                    var users = _modelOvr.Where(m => m.Value == tk).Select(m => m.Key).ToList();

                    var cg = new StackPanel { Spacing = 5 };
                    // Grid: Tn (auto) | ad+diyagram (star, ellipsis çalışsın) | Düzenle (auto)
                    var head = new Grid { ColumnSpacing = 8 };
                    head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var tnBlock = new TextBlock { Text = tk, FontSize = 12, FontWeight = bold, Foreground = ColorFromHex("#00A4EF"), VerticalAlignment = VerticalAlignment.Center };
                    Grid.SetColumn(tnBlock, 0); head.Children.Add(tnBlock);

                    var midCol = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                    midCol.Children.Add(new TextBlock { Text = kv.Value.name, FontSize = 11, Foreground = ColorFromHex("#CCCCCC"), TextTrimming = TextTrimming.CharacterEllipsis });
                    midCol.Children.Add(new TextBlock { Text = "🖼 " + kv.Value.diagram, FontSize = 9, Foreground = ColorFromHex("#777777"), TextTrimming = TextTrimming.CharacterEllipsis });
                    Grid.SetColumn(midCol, 1); head.Children.Add(midCol);

                    var editBtn = new Button { Content = "Düzenle", FontSize = 11, Padding = new Thickness(10, 4, 10, 4), Background = ColorFromHex("#202020"), Foreground = ColorFromHex("#DDDDDD"), VerticalAlignment = VerticalAlignment.Center };
                    editBtn.Click += (s, _) => { dialog?.Hide(); ShowLeakLibraryForType(tk); };
                    Grid.SetColumn(editBtn, 2); head.Children.Add(editBtn);
                    cg.Children.Add(head);

                    if (builtins.Count == 0 && users.Count == 0)
                    {
                        cg.Children.Add(new TextBlock { Text = "kayıtlı RFID yok", FontSize = 10, Foreground = ColorFromHex("#666666"), FontStyle = Windows.UI.Text.FontStyle.Italic });
                    }
                    else
                    {
                        var chips = new List<FrameworkElement>();
                        foreach (var rf in builtins.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                            chips.Add(new Border { Background = ColorFromHex("#1E1E1E"), CornerRadius = new CornerRadius(4), Padding = new Thickness(7, 2, 7, 2),
                                Child = new TextBlock { Text = rf, FontSize = 10, Foreground = ColorFromHex("#AAAAAA") } });
                        foreach (var rf in users.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                        {
                            var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
                            sp.Children.Add(new TextBlock { Text = rf, FontSize = 10, FontWeight = bold, Foreground = ColorFromHex("#4CAF50"), VerticalAlignment = VerticalAlignment.Center });
                            var rmBtn = new Button { Content = "✕", FontSize = 9, Padding = new Thickness(3, 0, 3, 0), MinWidth = 0, MinHeight = 0, Background = ColorFromHex("#2A1212"), Foreground = ColorFromHex("#E74C3C") };
                            string rfCap = rf;
                            rmBtn.Click += (s, _) => { _modelOvr.Remove(rfCap); SaveOvr(); Rebuild(); };
                            sp.Children.Add(rmBtn);
                            chips.Add(new Border { Background = ColorFromHex("#13281C"), BorderBrush = ColorFromHex("#1E6F3E"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Padding = new Thickness(7, 2, 5, 2), Child = sp });
                        }
                        cg.Children.Add(ChipFlow(chips));
                    }

                    listPanel.Children.Add(new Border { Background = ColorFromHex("#161616"), CornerRadius = new CornerRadius(6), BorderBrush = ColorFromHex("#262626"), BorderThickness = new Thickness(1), Padding = new Thickness(10, 8, 10, 8), Child = cg });
                }
            }

            addBtn.Click += (s, _) =>
            {
                string rf = rfidBox.Text?.Trim();
                if (string.IsNullOrWhiteSpace(rf)) { addStatus.Text = "⚠ RFID kodu girin."; return; }
                if (typeCombo.SelectedItem is not ComboBoxItem ci || ci.Tag is not string tk) { addStatus.Text = "⚠ Tip seçin."; return; }
                _modelOvr[rf] = tk;
                SaveOvr();
                addStatus.Text = $"✓ {rf} → {tk} ({_leakMaps.types[tk].name}) atandı. Artık bu RFID'nin kaçak haritası açılır.";
                rfidBox.Text = "";
                Rebuild();
            };

            Rebuild();

            var form = new Border { Background = ColorFromHex("#141414"), CornerRadius = new CornerRadius(8), BorderBrush = ColorFromHex("#2A2A2A"), BorderThickness = new Thickness(1), Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 8) };
            var formInner = new StackPanel { Spacing = 6 };
            formInner.Children.Add(new TextBlock { Text = "YENİ RFID → TİP/DİYAGRAM ATA", FontSize = 11, FontWeight = bold, Foreground = ColorFromHex("#00A4EF") });
            formInner.Children.Add(new TextBlock { Text = "Excel'de olmayan yeni bir modeli (örn. ARXF71) mevcut bir diyagrama bağla — kaçak haritası ve nokta kütüphanesi o RFID için de açılır.", FontSize = 10, Foreground = ColorFromHex("#888888"), TextWrapping = TextWrapping.Wrap });
            // Sabit kolonlu satır: combo seçili uzun adla büyüyüp "Ata"yı itemesin
            var formRow = new Grid { ColumnSpacing = 8 };
            formRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            formRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });
            formRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(rfidBox, 0); Grid.SetColumn(typeCombo, 1); Grid.SetColumn(addBtn, 2);
            formRow.Children.Add(rfidBox); formRow.Children.Add(typeCombo); formRow.Children.Add(addBtn);
            formInner.Children.Add(formRow);
            formInner.Children.Add(addStatus);
            form.Child = formInner;

            var root = new StackPanel { Spacing = 0, Width = 500 };
            root.Children.Add(form);
            root.Children.Add(new TextBlock { Text = "DİYAGRAM / TİP BAŞINA KAYITLI MODELLER", FontSize = 11, FontWeight = bold, Foreground = ColorFromHex("#888888"), Margin = new Thickness(2, 0, 0, 6) });
            root.Children.Add(new ScrollViewer { Content = listPanel, MaxHeight = 420, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });

            dialog = new ContentDialog
            {
                Title = "Nokta Kütüphanesi — Model / RFID Yönetimi",
                Content = root,
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
            if (_leakMaps?.types == null || !_leakMaps.types.TryGetValue(typeKey, out var ti) || string.IsNullOrEmpty(ti?.diagram)) return;
            var entry = BuildEffectiveEntry(ti.diagram);  // boş olsa bile aç — resim eklenebilsin

            // Boş kayıt → tüm noktalar OK (yeşil); düzenleme açık
            var empty = new TrendRecord { NgPointsR1 = new List<int>(), NgPointsR2 = new List<int>(), OverallResult = "" };
            _lmRecord = empty; _lmTypeKey = typeKey; _lmRfid = null; _lmDiagram = ti.diagram; _lmEntry = entry; _lmEdit = true; _lmMarkMode = false;

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
            [System.Text.Json.Serialization.JsonIgnore] public bool isUser { get; set; } // kullanıcı eklediği parça mı
        }
        private class LeakPoint
        {
            public string name { get; set; }
            public int robot { get; set; }
            public int idx { get; set; }
            public double x { get; set; }
            public double y { get; set; }
            [System.Text.Json.Serialization.JsonIgnore] public bool isUser { get; set; } // kullanıcı eklediği nokta mı
        }

        // ═══ KULLANICI KAÇAK-HARİTA KATMANI (uygulama içi editör — asset üstüne yazılabilir) ═══
        private class UserMapStore
        {
            public Dictionary<string, List<LeakPart>> addedParts { get; set; } = new();   // diagram -> eklenen parçalar(resimler)
            public Dictionary<string, List<LeakPoint>> addedPoints { get; set; } = new();  // "diagram::partTitle" -> eklenen noktalar
            public List<string> hidden { get; set; } = new();                              // "point:diag::part::ad" / "part:diag::part"
        }
        private class PointOvr
        {
            public int? i { get; set; }
            public int? r { get; set; }
            public string d { get; set; }   // açıklama (opsiyonel)
        }
        private class OvrFile
        {
            public Dictionary<string, Dictionary<string, PointOvr>> types { get; set; }   // typeKey -> noktalar
            public Dictionary<string, Dictionary<string, PointOvr>> rfids { get; set; }   // RFID -> noktalar
            public Dictionary<string, string> models { get; set; }                        // RFID -> typeKey (kullanıcı eşlemesi)
        }
    }
}
