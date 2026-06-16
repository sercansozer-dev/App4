using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace App4.Utilities
{
    /// <summary>
    /// ═══ GLOBAL ÜRETİM İZLEYİCİ (sayfadan bağımsız) ═══
    /// Çevrim mantığı (IS_BASLADI / ISLEM_BITTI / NG_NOKTALAR) artık Auto_Page'de DEĞİL,
    /// burada — uygulama açıldığı anda başlayan tek bir global serviste — çalışır.
    /// Sayfalar yalnızca GÖSTERİM yapar; üretim kaydı, NG mandalı, KPI ve canlı üretim
    /// takibi bu servisin tek sorumluluğudur (tek-yazıcı → çift kayıt olmaz).
    ///
    /// Tasarım:
    ///  • Olay yolu: GlobalData.GeneralInputVars[IS_BASLADI/ISLEM_BITTI/NG_*] PropertyChanged.
    ///    (Bu yerel değişkenler PLC köprüsüyle beslenir; köprü Auto açılışta kurulur ve
    ///     sayfa cache'li olduğu için ömür boyu canlı kalır.)
    ///  • Güvenlik ağı: global System.Threading.Timer ile periyodik PollCycleSignals —
    ///    kaçan kenarları yakalar; Auto kapalıyken de çalışır (DispatcherTimer DEĞİL).
    ///  • UI/VM yazımları PlcService.Instance.UiRunner (MainWindow DispatcherQueue) ile
    ///    UI thread'e marshal edilir; sayfanın DispatcherQueue'una bağımlı değildir.
    /// </summary>
    public static class ProductionMonitor
    {
        private static bool _started;
        private static readonly object _gate = new();

        // Ortak IS_BASLADI ile başlayan aktif çevrim istasyonu (ISLEM_BITTI'de aynısını kullan)
        private static StationViewModel _cycleStation;

        // ═══ KAÇAK (NG) NOKTA MANDALI ═══
        // NG_NOKTALAR ortak sinyal; çevrim ortasında yazılıp iş bitmeden silinebiliyor.
        // NG değiştikçe (olay-tabanlı) aktif istasyona mandalla, IS_BASLADI'da sıfırla, kayıtta kullan.
        private static readonly Dictionary<int, List<int>> _ngLatchR1 = new();
        private static readonly Dictionary<int, List<int>> _ngLatchR2 = new();
        private static PlcVariable _isBasladiVar, _islemBittiVar, _ngVarR1, _ngVarR2;

        // ── TAM TUR (3 İSTASYON) ÇEVRİM SÜRESİ — durum (gösterim Auto'da) ──
        private static bool _lineRunning;
        private static DateTime _lineStart;
        private static DateTime _lineLastBitti = DateTime.MinValue;
        private static int _lineTourCount;       // mevcut turda biten üretim sayısı
        private const double LINE_GRACE_SEC = 12;

        /// <summary>Hat turu sürüyor mu (Auto gösterimi okur).</summary>
        public static bool LineRunning => _lineRunning;
        /// <summary>Mevcut turun başlangıcı (Auto canlı kronometresi okur).</summary>
        public static DateTime LineStart => _lineStart;
        /// <summary>Son tamamlanan tam-tur süresi (Auto donmuş gösterimi okur).</summary>
        public static TimeSpan LineLastDuration { get; private set; } = TimeSpan.Zero;
        /// <summary>Son tamamlanan turdaki üretim adedi (vardiya tahmini için).</summary>
        public static int LineLastTourCount { get; private set; }
        /// <summary>Hat turu durumu değişince (başladı/donduruldu) tetiklenir — Auto TextBlock'ları tazeler.</summary>
        public static event Action LineCycleChanged;

        private static System.Threading.Timer _safetyNetTimer;

        /// <summary>Uygulama açılışında (GlobalData.Initialize) bir kez çağrılır. Idempotent.</summary>
        public static void Start()
        {
            lock (_gate)
            {
                if (_started) return;
                _started = true;
            }
            try
            {
                HookCycleSignals();

                // Güvenlik ağı: ortak IS_BASLADI/ISLEM_BITTI'yi periyodik aktif istasyona uygula.
                int interval = Math.Max(100, GlobalData.Safety_CheckInterval);
                _safetyNetTimer = new System.Threading.Timer(_ => SafetyNetTick(), null, interval, interval);

                System.Diagnostics.Debug.WriteLine("[PRODMON] Global üretim izleyici başlatıldı.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PRODMON] Start hatası: {ex.Message}");
            }
        }

        /// <summary>Çevrim sinyallerine (olay yolu) abone ol. Idempotent (önce -= sonra +=).</summary>
        public static void HookCycleSignals()
        {
            try
            {
                var gen = GlobalData.GeneralInputVars;
                if (gen == null) return;

                if (_isBasladiVar != null) _isBasladiVar.PropertyChanged -= CycleSignal_Changed;
                if (_islemBittiVar != null) _islemBittiVar.PropertyChanged -= CycleSignal_Changed;
                if (_ngVarR1 != null) _ngVarR1.PropertyChanged -= NgLatch_Changed;
                if (_ngVarR2 != null) _ngVarR2.PropertyChanged -= NgLatch_Changed;

                _isBasladiVar = gen.FirstOrDefault(v => v.Name == "IS_BASLADI");
                _islemBittiVar = gen.FirstOrDefault(v => v.Name == "ISLEM_BITTI");
                _ngVarR1 = gen.FirstOrDefault(v => v.Name == "NG_NOKTALAR_R1");
                _ngVarR2 = gen.FirstOrDefault(v => v.Name == "NG_NOKTALAR_R2");

                if (_isBasladiVar != null) _isBasladiVar.PropertyChanged += CycleSignal_Changed;
                if (_islemBittiVar != null) _islemBittiVar.PropertyChanged += CycleSignal_Changed;
                if (_ngVarR1 != null) _ngVarR1.PropertyChanged += NgLatch_Changed;
                if (_ngVarR2 != null) _ngVarR2.PropertyChanged += NgLatch_Changed;
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[PRODMON] HookCycleSignals hatası: {ex.Message}"); }
        }

        private static void SafetyNetTick()
        {
            // UiRunner hazır değilse (çok erken init) atla — VM yazımı UI thread ister.
            if (PlcService.Instance?.UiRunner == null) return;
            Ui(() =>
            {
                try { PollCycleSignals(); UpdateLineCycleState(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[PRODMON] SafetyNetTick hatası: {ex.Message}"); }
            });
        }

        /// <summary>UI/VM yazımını UI thread'e marshal eder (sayfa DispatcherQueue'una bağımlı değil).</summary>
        private static void Ui(Action a)
        {
            if (a == null) return;
            var runner = PlcService.Instance?.UiRunner;
            if (runner != null) runner.Invoke(a);
            else { try { a(); } catch { } } // UiRunner yoksa (nadir, init anı) en iyi çaba
        }

        // ─────────────────────────────────────────────────────────────────────
        //  OLAY YOLU: ortak çevrim sinyali değiştiğinde aktif istasyona yönlendir
        // ─────────────────────────────────────────────────────────────────────
        private static void CycleSignal_Changed(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(PlcVariable.CurrentValue) && e.PropertyName != nameof(PlcVariable.Value)) return;
            if (sender is not PlcVariable v) return;
            // Olay UI thread'inde gelir (köprü/PlcService UiRunner ile yazar) → doğrudan işle.
            try
            {
                if (v.Name == "IS_BASLADI")
                {
                    var st = ResolveActiveStation();
                    if (st != null) { _cycleStation = st; HandleCycleIsBasladi(st, IsTrue(v.Value)); }
                }
                else if (v.Name == "ISLEM_BITTI")
                {
                    var st = _cycleStation ?? ResolveActiveStation();
                    if (st != null) HandleCycleIslemBitti(st, IsTrue(v.Value));
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[PRODMON] CycleSignal hatası: {ex.Message}"); }
        }

        private static void NgLatch_Changed(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(PlcVariable.CurrentValue) && e.PropertyName != nameof(PlcVariable.Value)) return;
            try
            {
                var cs = _cycleStation;
                if (cs == null || !cs.CycleRunning) return; // yalnız aktif çevrimde mandalla
                int sn = GlobalData.Stations.IndexOf(cs) + 1;
                if (sn < 1) return;
                var gen = GlobalData.GeneralInputVars;
                var r1 = ParseNgPoints(gen.FirstOrDefault(v => v.Name == "NG_NOKTALAR_R1")?.Value);
                var r2 = ParseNgPoints(gen.FirstOrDefault(v => v.Name == "NG_NOKTALAR_R2")?.Value);
                if (r1.Count > 0) _ngLatchR1[sn] = UnionInts(_ngLatchR1.GetValueOrDefault(sn), r1);
                if (r2.Count > 0) _ngLatchR2[sn] = UnionInts(_ngLatchR2.GetValueOrDefault(sn), r2);

                // Canlı üretim takibine yansıt (Trend canlı paneli için)
                if (sn >= 1 && sn <= 3)
                {
                    var lp = GlobalData.LiveStations[sn - 1];
                    lp.NgR1 = new List<int>(_ngLatchR1.GetValueOrDefault(sn) ?? new List<int>());
                    lp.NgR2 = new List<int>(_ngLatchR2.GetValueOrDefault(sn) ?? new List<int>());
                    GlobalData.RaiseLiveProductionChanged();
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[PRODMON] NgLatch hatası: {ex.Message}"); }
        }

        /// <summary>Ortak çevrim sinyalleri için aktif istasyonu çözer.
        /// Öncelik: TargetSliderStation → KL100_HEDEF_ISTASYON → AKTUEL_ISTASYON → üretimdeki ilk istasyon.</summary>
        private static StationViewModel ResolveActiveStation()
        {
            var Stations = GlobalData.Stations;
            int tss = GlobalData.TargetSliderStation;
            if (tss >= 1 && tss <= Stations.Count) return Stations[tss - 1];

            var hedefVar = GlobalData.GeneralOutputVars.FirstOrDefault(v => v.Name == "KL100_HEDEF_ISTASYON")
                        ?? GlobalData.GeneralInputVars.FirstOrDefault(v => v.Name == "KL100_HEDEF_ISTASYON");
            if (int.TryParse(hedefVar?.Value?.Trim(), out int hno) && hno >= 1 && hno <= Stations.Count) return Stations[hno - 1];

            var av = GlobalData.GeneralInputVars.FirstOrDefault(x => x.Name == "AKTUEL_ISTASYON");
            if (int.TryParse(av?.Value?.Trim(), out int no) && no >= 1 && no <= Stations.Count) return Stations[no - 1];

            return Stations.FirstOrDefault(s => s.IsProducing);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // CYCLE — IS_BASLADI başlatır, ISLEM_BITTI durdurur + üretim kaydı
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>IS_BASLADI — yükselen kenarda (false→true) çevrimi başlatır.</summary>
        private static void HandleCycleIsBasladi(StationViewModel s, bool val)
        {
            if (val && !s.LastIsBasladi)
            {
                s.CycleStartTime = DateTime.Now;
                s.CycleRunning = true;
                s.LastIslemBitti = false;
                s.CycleTimeText = "00:00";

                int sn = GlobalData.Stations.IndexOf(s) + 1;
                if (sn >= 1) { _ngLatchR1.Remove(sn); _ngLatchR2.Remove(sn); }

                // Canlı üretim takibi: bu istasyonda yeni ürün işleniyor
                if (sn >= 1 && sn <= 3)
                {
                    var live = GlobalData.LiveStations[sn - 1];
                    live.Active = true;
                    live.StationName = s.Name;
                    live.Rfid = s.CurrentRfid ?? "";
                    live.ProductName = GlobalData.KnownRfids.FirstOrDefault(r => string.Equals(r.Id, s.CurrentRfid, StringComparison.OrdinalIgnoreCase))?.Description ?? "";
                    live.StartTime = DateTime.Now;
                    live.NgR1 = new List<int>();
                    live.NgR2 = new List<int>();
                    GlobalData.RaiseLiveProductionChanged();
                }

                // Tam tur: hat boştayken ilk başlama → tur başlar
                if (!_lineRunning) { _lineRunning = true; _lineStart = DateTime.Now; _lineLastBitti = DateTime.MinValue; _lineTourCount = 0; RaiseLineCycleChanged(); }
            }
            s.LastIsBasladi = val;
        }

        /// <summary>ISLEM_BITTI — yükselen kenarda çevrimi durdurur, üretimi kaydeder.</summary>
        private static void HandleCycleIslemBitti(StationViewModel s, bool val)
        {
            if (val && !s.LastIslemBitti && s.CycleRunning && s.CycleStartTime.HasValue)
            {
                var elapsed = DateTime.Now - s.CycleStartTime.Value;
                s.CycleTimeText = FormatCycle(elapsed);
                s.CycleRunning = false;

                // Sonuç verisi (RESULT_NG/NG_NOKTALAR) ISLEM_BITTI'den hemen sonra gelebildiği için oturtup kaydet.
                _ = RecordProductionDelayed(s, elapsed);

                _lineLastBitti = DateTime.Now;
                if (_lineRunning) _lineTourCount++;
            }
            s.LastIslemBitti = val;
        }

        /// <summary>ISLEM_BITTI sonrası sonuç verisini (RESULT_OK/RESULT_NG/NG_NOKTALAR) oturtup kaydeder (maks ~1.6sn).</summary>
        private static async System.Threading.Tasks.Task RecordProductionDelayed(StationViewModel s, TimeSpan elapsed)
        {
            int stationNo = GlobalData.Stations.IndexOf(s) + 1;
            var capNgR1 = new List<int>();
            var capNgR2 = new List<int>();
            bool capRNg = false, capROk = false;
            if (stationNo >= 1)
            {
                var vars = stationNo switch
                {
                    1 => GlobalData.Station1Vars,
                    2 => GlobalData.Station2Vars,
                    3 => GlobalData.Station3Vars,
                    _ => null
                };
                for (int i = 0; i < 8; i++) // 8 × 200ms = maks 1.6 sn
                {
                    if (!capROk) capROk = IsTrue(vars?.FirstOrDefault(v => v.Name == $"ST{stationNo}_RESULT_OK")?.Value);
                    if (!capRNg) capRNg = IsTrue(vars?.FirstOrDefault(v => v.Name == $"ST{stationNo}_RESULT_NG")?.Value);
                    var gen0 = GlobalData.GeneralInputVars;
                    var r1 = ParseNgPoints(gen0.FirstOrDefault(v => v.Name == "NG_NOKTALAR_R1")?.Value);
                    var r2 = ParseNgPoints(gen0.FirstOrDefault(v => v.Name == "NG_NOKTALAR_R2")?.Value);
                    if (r1.Count > 0) capNgR1 = r1;
                    if (r2.Count > 0) capNgR2 = r2;
                    if (capROk || capRNg || capNgR1.Count > 0 || capNgR2.Count > 0) break;
                    await System.Threading.Tasks.Task.Delay(200);
                }
            }
            RecordProduction(s, elapsed, capNgR1, capNgR2, capRNg);
        }

        /// <summary>Çevrim bitince bir üretim kaydı oluşturup TrendDataService'e ekler.</summary>
        private static void RecordProduction(StationViewModel s, TimeSpan elapsed,
            List<int> capNgR1 = null, List<int> capNgR2 = null, bool capRNg = false)
        {
            try
            {
                int stationNo = GlobalData.Stations.IndexOf(s) + 1;
                if (stationNo < 1) return;

                var vars = stationNo switch
                {
                    1 => GlobalData.Station1Vars,
                    2 => GlobalData.Station2Vars,
                    3 => GlobalData.Station3Vars,
                    _ => null
                };
                var gen = GlobalData.GeneralInputVars;
                var capturedR1 = (capNgR1 != null) ? capNgR1 : ParseNgPoints(gen.FirstOrDefault(v => v.Name == "NG_NOKTALAR_R1")?.Value);
                var capturedR2 = (capNgR2 != null) ? capNgR2 : ParseNgPoints(gen.FirstOrDefault(v => v.Name == "NG_NOKTALAR_R2")?.Value);
                var ngR1 = UnionInts(_ngLatchR1.GetValueOrDefault(stationNo), capturedR1);
                var ngR2 = UnionInts(_ngLatchR2.GetValueOrDefault(stationNo), capturedR2);
                _ngLatchR1.Remove(stationNo); _ngLatchR2.Remove(stationNo);

                bool ngResult = capRNg || IsTrue(vars?.FirstOrDefault(v => v.Name == $"ST{stationNo}_RESULT_NG")?.Value);
                bool ng = ngResult || ngR1.Count > 0 || ngR2.Count > 0;
                string result = ng ? "NOK" : "OK";

                string rfid = s.CurrentRfid ?? "";
                var recipe = GlobalData.KnownRfids.FirstOrDefault(
                    r => string.Equals(r.Id, rfid, StringComparison.OrdinalIgnoreCase));

                var record = new TrendRecord
                {
                    Timestamp = DateTime.Now,
                    StationNo = stationNo,
                    StationName = s.Name,
                    RfidTag = rfid,
                    ProductName = recipe?.Description ?? "",
                    KlimaTip = recipe?.Id ?? "",
                    KlimaId = recipe?.IndexDisplay ?? 0,
                    CycleTime = Math.Round(elapsed.TotalSeconds, 1),
                    OverallResult = result,
                    NgPointsR1 = ngR1,
                    NgPointsR2 = ngR2,
                    NokCount = ngR1.Count + ngR2.Count,
                    OffsetX = ParseOffset(s.TablaOffsetX),
                    OffsetY = ParseOffset(s.TablaOffsetY),
                    OffsetZ = ParseOffset(s.TablaOffsetZ),
                    OffsetA = ParseOffset(s.TablaOffsetA),
                    OffsetB = ParseOffset(s.TablaOffsetB),
                    OffsetC = ParseOffset(s.TablaOffsetC)
                };

                TrendDataService.Instance.AddRecord(record);
                System.Diagnostics.Debug.WriteLine($"[PRODMON] Üretim kaydı: ST{stationNo} {result} {rfid} {record.CycleTime}sn | NG R1={ngR1.Count} R2={ngR2.Count}");

                if (stationNo >= 1 && stationNo <= 3)
                {
                    var live = GlobalData.LiveStations[stationNo - 1];
                    live.Active = false;
                    live.NgR1 = new List<int>(ngR1);
                    live.NgR2 = new List<int>(ngR2);
                }
                GlobalData.RaiseLiveProductionChanged();
                GlobalData.RaiseProductionRecorded();

                RecalcStationKpis();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PRODMON] Üretim kaydı hatası: {ex.Message}");
            }
        }

        /// <summary>İstasyon kartlarındaki Üretim Adedi + Verimlilik'i BUGÜNKÜ trend kayıtlarından hesaplar.</summary>
        public static void RecalcStationKpis()
        {
            try
            {
                var start = DateTime.Today;
                var end = DateTime.Now;
                var Stations = GlobalData.Stations;
                for (int i = 0; i < Stations.Count; i++)
                {
                    int stNo = i + 1;
                    var recs = TrendDataService.Instance.GetRecords(start, end, stNo, null, null);
                    int total = recs?.Count ?? 0;
                    int ok = recs?.Count(r => r.OverallResult == "OK") ?? 0;
                    int eff = total > 0 ? (int)Math.Round(ok * 100.0 / total) : 0;
                    var s = Stations[i];
                    Ui(() =>
                    {
                        s.ProductionCount = total.ToString();
                        s.Efficiency = $"%{eff}";
                    });
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[PRODMON] KPI hesaplama hatası: {ex.Message}"); }
        }

        /// <summary>Güvenlik ağı: ortak IS_BASLADI/ISLEM_BITTI'yi periyodik okuyup aktif istasyona uygular (kaçan kenarlar).</summary>
        private static void PollCycleSignals()
        {
            var st = ResolveActiveStation();
            if (st == null) return;
            var isb = GlobalData.GeneralInputVars.FirstOrDefault(v => v.Name == "IS_BASLADI");
            var ibt = GlobalData.GeneralInputVars.FirstOrDefault(v => v.Name == "ISLEM_BITTI");
            bool isBasladi = IsTrue(isb?.Value);
            bool islemBitti = IsTrue(ibt?.Value);
            if (isBasladi && !st.LastIsBasladi) _cycleStation = st;
            HandleCycleIsBasladi(st, isBasladi);
            HandleCycleIslemBitti(_cycleStation ?? st, islemBitti);

            // Çalışan istasyonların canlı kronometresini de güncelle (Auto kapalıyken bile VM güncel kalsın)
            foreach (var s in GlobalData.Stations)
            {
                if (s.CycleRunning && s.CycleStartTime.HasValue)
                    s.CycleTimeText = FormatCycle(DateTime.Now - s.CycleStartTime.Value);
            }
        }

        /// <summary>Tam-tur durum geçişini hesaplar (hepsi boş + grace → turu dondur). Gösterim Auto'da.</summary>
        private static void UpdateLineCycleState()
        {
            if (!_lineRunning) return;
            bool allIdle = GlobalData.Stations.All(st => !st.CycleRunning);
            if (allIdle && _lineLastBitti != DateTime.MinValue && (DateTime.Now - _lineLastBitti).TotalSeconds >= LINE_GRACE_SEC)
            {
                LineLastDuration = _lineLastBitti - _lineStart;   // ilk başladı → son bitti
                LineLastTourCount = _lineTourCount;
                _lineRunning = false;
                RaiseLineCycleChanged();
            }
        }

        private static void RaiseLineCycleChanged()
        {
            try { Ui(() => LineCycleChanged?.Invoke()); } catch { }
        }

        // ───────────────────────── Yardımcılar ─────────────────────────
        private static List<int> UnionInts(List<int> a, List<int> b)
        {
            var set = new SortedSet<int>();
            if (a != null) foreach (var x in a) set.Add(x);
            if (b != null) foreach (var x in b) set.Add(x);
            return new List<int>(set);
        }

        private static List<int> ParseNgPoints(string v)
        {
            var list = new List<int>();
            if (string.IsNullOrWhiteSpace(v)) return list;
            var parts = v.Split(new[] { ',', ';', ' ', '\t', '|', '[', ']', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                if (int.TryParse(p.Trim(), out int n) && n > 0 && !list.Contains(n))
                    list.Add(n);
            }
            return list;
        }

        private static double ParseOffset(string v)
        {
            if (string.IsNullOrWhiteSpace(v)) return 0;
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var styles = System.Globalization.NumberStyles.Any;
            if (double.TryParse(v, styles, inv, out double d)) return d;
            if (double.TryParse(v?.Replace(',', '.'), styles, inv, out d)) return d;
            return 0;
        }

        private static string FormatCycle(TimeSpan t)
        {
            if (t.TotalSeconds < 0) t = TimeSpan.Zero;
            if (t.TotalHours >= 1)
                return $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
            return $"{t.Minutes:D2}:{t.Seconds:D2}";
        }

        private static bool IsTrue(string v) => !string.IsNullOrEmpty(v) && (v.ToUpper() == "TRUE" || v == "1" || v == "ON");
    }
}
