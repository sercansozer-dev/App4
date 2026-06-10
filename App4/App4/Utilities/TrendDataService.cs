using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace App4.Utilities
{
    // ═══════════════════════════════════════════════════════════════
    // TREND KAYIT MODELİ
    // ═══════════════════════════════════════════════════════════════
    public class TrendRecord : INotifyPropertyChanged
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12];
        public DateTime Timestamp { get; set; } = DateTime.Now;

        // İstasyon / Ürün Bilgileri
        public int StationNo { get; set; }          // 1, 2, 3
        public string StationName { get; set; }     // "İSTASYON 1"
        public string RfidTag { get; set; }         // Okunan RFID
        public string ProductName { get; set; }     // Ürün adı (RfidDef.Description)
        public string KlimaTip { get; set; }        // Klima tipi
        public int KlimaId { get; set; }            // Klima numarası

        // Gocator Ölçüm Sonuçları
        public double OffsetX { get; set; }
        public double OffsetY { get; set; }
        public double OffsetZ { get; set; }
        public double OffsetA { get; set; }
        public double OffsetB { get; set; }
        public double OffsetC { get; set; }

        // Inficon Kaçak Test Sonuçları
        public int TotalPoints { get; set; }        // Toplam test noktası
        public int OkCount { get; set; }            // Başarılı sayısı
        public int NokCount { get; set; }           // Başarısız sayısı
        public List<PointResult> PointResults { get; set; } = new();

        // Kaçak (NG) tespit edilen nokta numaraları — PLC'den Robot 1 ve Robot 2 için AYRI int array
        public List<int> NgPointsR1 { get; set; } = new();
        public List<int> NgPointsR2 { get; set; } = new();

        // Genel Sonuç
        public string OverallResult { get; set; }   // "OK" / "NOK"
        public double CycleTime { get; set; }       // Toplam süre (sn)
        public string Notes { get; set; }           // Ek not

        // Sıra No (listede gösterim için — en üstte en büyük, aşağı doğru azalır)
        private int _siraNo;
        [JsonIgnore]
        public int SiraNo { get => _siraNo; set { if (_siraNo != value) { _siraNo = value; OnPropertyChanged(); } } }

        // Hesaplanan Alanlar
        [JsonIgnore]
        public string DateStr => Timestamp.ToString("dd.MM.yyyy");
        [JsonIgnore]
        public string TimeStr => Timestamp.ToString("HH:mm:ss");
        [JsonIgnore]
        public string ResultIcon => OverallResult == "OK" ? "✓" : "✗";
        [JsonIgnore]
        public string SuccessRate => TotalPoints > 0 ? $"%{(OkCount * 100.0 / TotalPoints):F0}" : "-";

        // ─── KAÇAK (NG) NOKTA GÖSTERİMİ (popup için) — Robot 1 & Robot 2 ayrı ───
        [JsonIgnore]
        public int NgTotalCount => (NgPointsR1?.Count ?? 0) + (NgPointsR2?.Count ?? 0);
        [JsonIgnore]
        public bool HasNgPoints => NgTotalCount > 0;
        [JsonIgnore]
        public Microsoft.UI.Xaml.Visibility NgButtonVisibility =>
            (OverallResult == "NOK") ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
        [JsonIgnore]
        public Microsoft.UI.Xaml.Visibility NokTextVisibility =>
            (OverallResult == "NOK") ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;
        [JsonIgnore]
        public string NgPointsR1Text =>
            (NgPointsR1 != null && NgPointsR1.Count > 0) ? string.Join(", ", NgPointsR1) : "-";
        [JsonIgnore]
        public string NgPointsR2Text =>
            (NgPointsR2 != null && NgPointsR2.Count > 0) ? string.Join(", ", NgPointsR2) : "-";
        [JsonIgnore]
        public string NgPointsCountText => $"⚠ {NgTotalCount}";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class PointResult
    {
        public int PointNo { get; set; }
        public string Result { get; set; }      // "OK" / "NOK"
        public double Value { get; set; }       // ppm değeri
        public double Threshold { get; set; }   // Eşik değer
    }

    // ═══════════════════════════════════════════════════════════════
    // TREND İSTATİSTİK MODELİ
    // ═══════════════════════════════════════════════════════════════
    public class TrendStatistics
    {
        public int TotalRecords { get; set; }
        public int OkRecords { get; set; }
        public int NokRecords { get; set; }
        public double OkPercent => TotalRecords > 0 ? (OkRecords * 100.0 / TotalRecords) : 0;
        public double NokPercent => TotalRecords > 0 ? (NokRecords * 100.0 / TotalRecords) : 0;
        public double AvgCycleTime { get; set; }
        public double AvgOffsetX { get; set; }
        public double AvgOffsetY { get; set; }
        public double AvgOffsetZ { get; set; }
        public int TotalNokPoints { get; set; }
        public Dictionary<int, int> NokByStation { get; set; } = new();
        public Dictionary<string, int> NokByProduct { get; set; } = new();
        public Dictionary<int, int> NokByPoint { get; set; } = new();
    }

    // ═══════════════════════════════════════════════════════════════
    // TREND VERİ SERVİSİ (JSON dosya tabanlı depolama)
    // ═══════════════════════════════════════════════════════════════
    public class TrendDataService
    {
        private static TrendDataService _instance;
        public static TrendDataService Instance => _instance ??= new TrendDataService();

        private readonly string _dataFolder;
        private readonly object _fileLock = new();

        private TrendDataService()
        {
            _dataFolder = Path.Combine(GlobalData.ConfigBaseDir, "TrendData");

            if (!Directory.Exists(_dataFolder))
                Directory.CreateDirectory(_dataFolder);
        }

        // Aylık dosya adı: Trend_2025_01.json
        private string GetMonthlyFilePath(DateTime date)
            => Path.Combine(_dataFolder, $"Trend_{date:yyyy_MM}.json");

        // ─── KAYIT EKLEME ───
        public void AddRecord(TrendRecord record)
        {
            try
            {
                string filePath = GetMonthlyFilePath(record.Timestamp);
                List<TrendRecord> records;

                lock (_fileLock)
                {
                    records = LoadFromFile(filePath);
                    records.Add(record);
                    SaveToFile(filePath, records);
                }

                System.Diagnostics.Debug.WriteLine($"[TREND] Kayıt eklendi: ST{record.StationNo} {record.OverallResult} ({record.RfidTag})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TREND] Kayıt hatası: {ex.Message}");
            }
        }

        /// <summary>TÜM trend kayıtlarını siler (tüm aylık Trend_*.json dosyaları). Geri alınamaz. Silinen dosya sayısını döner.</summary>
        public int ClearAllRecords()
        {
            int deleted = 0;
            try
            {
                lock (_fileLock)
                {
                    foreach (var file in Directory.GetFiles(_dataFolder, "Trend_*.json"))
                    {
                        try { File.Delete(file); deleted++; } catch { }
                    }
                }
                System.Diagnostics.Debug.WriteLine($"[TREND] Tüm kayıtlar silindi ({deleted} dosya).");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TREND] Temizleme hatası: {ex.Message}");
            }
            return deleted;
        }

        // ─── VERİ SORGULAMA ───
        public List<TrendRecord> GetRecords(DateTime startDate, DateTime endDate,
            int? stationNo = null, string rfidTag = null, string result = null)
        {
            var allRecords = new List<TrendRecord>();

            // Tarih aralığındaki tüm aylık dosyaları yükle
            var current = new DateTime(startDate.Year, startDate.Month, 1);
            var end = new DateTime(endDate.Year, endDate.Month, 1).AddMonths(1);

            while (current < end)
            {
                string filePath = GetMonthlyFilePath(current);
                if (File.Exists(filePath))
                {
                    lock (_fileLock)
                    {
                        allRecords.AddRange(LoadFromFile(filePath));
                    }
                }
                current = current.AddMonths(1);
            }

            // Filtreleme
            var query = allRecords
                .Where(r => r.Timestamp >= startDate && r.Timestamp <= endDate);

            if (stationNo.HasValue)
                query = query.Where(r => r.StationNo == stationNo.Value);

            if (!string.IsNullOrEmpty(rfidTag))
                query = query.Where(r => r.RfidTag == rfidTag);

            if (!string.IsNullOrEmpty(result) && result != "Tümü")
                query = query.Where(r => r.OverallResult == result);

            return query.OrderByDescending(r => r.Timestamp).ToList();
        }

        // ─── İSTATİSTİK HESAPLAMA ───
        public TrendStatistics CalculateStatistics(List<TrendRecord> records)
        {
            var stats = new TrendStatistics
            {
                TotalRecords = records.Count,
                OkRecords = records.Count(r => r.OverallResult == "OK"),
                NokRecords = records.Count(r => r.OverallResult == "NOK"),
                AvgCycleTime = records.Count > 0 ? records.Average(r => r.CycleTime) : 0,
                AvgOffsetX = records.Count > 0 ? records.Average(r => r.OffsetX) : 0,
                AvgOffsetY = records.Count > 0 ? records.Average(r => r.OffsetY) : 0,
                AvgOffsetZ = records.Count > 0 ? records.Average(r => r.OffsetZ) : 0,
                TotalNokPoints = records.Sum(r => (r.NgPointsR1?.Count ?? 0) + (r.NgPointsR2?.Count ?? 0))
            };

            // İstasyona göre NOK dağılımı
            foreach (var grp in records.Where(r => r.OverallResult == "NOK").GroupBy(r => r.StationNo))
                stats.NokByStation[grp.Key] = grp.Count();

            // Ürüne göre NOK dağılımı
            foreach (var grp in records.Where(r => r.OverallResult == "NOK" && !string.IsNullOrEmpty(r.ProductName)).GroupBy(r => r.ProductName))
                stats.NokByProduct[grp.Key] = grp.Count();

            // Noktaya göre NOK dağılımı
            foreach (var grp in records.SelectMany(r => r.PointResults).Where(p => p.Result == "NOK").GroupBy(p => p.PointNo))
                stats.NokByPoint[grp.Key] = grp.Count();

            return stats;
        }

        // ─── CSV DIŞA AKTARMA ───
        public string ExportToCsv(List<TrendRecord> records, string filePath)
        {
            try
            {
                using var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);

                // Başlık satırı
                writer.WriteLine("Tarih;Saat;İstasyon;RFID;Ürün;Klima Tip;Sonuç;OK Sayısı;NOK Sayısı;Başarı%;Süre(sn);Offset X;Offset Y;Offset Z;Offset A;Offset B;Offset C;Notlar");

                foreach (var r in records)
                {
                    writer.WriteLine(string.Join(";",
                        r.DateStr, r.TimeStr, $"İSTASYON {r.StationNo}", r.RfidTag, r.ProductName,
                        r.KlimaTip, r.OverallResult, r.OkCount, r.NokCount, r.SuccessRate,
                        r.CycleTime.ToString("F1"), r.OffsetX.ToString("F3"), r.OffsetY.ToString("F3"),
                        r.OffsetZ.ToString("F3"), r.OffsetA.ToString("F3"), r.OffsetB.ToString("F3"),
                        r.OffsetC.ToString("F3"), r.Notes));
                }

                return filePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TREND] CSV export hatası: {ex.Message}");
                return null;
            }
        }

        // ─── KAYIT SİLME (Belirli ay) ───
        public int DeleteRecords(DateTime month)
        {
            string filePath = GetMonthlyFilePath(month);
            if (File.Exists(filePath))
            {
                lock (_fileLock)
                {
                    var records = LoadFromFile(filePath);
                    int count = records.Count;
                    File.Delete(filePath);
                    return count;
                }
            }
            return 0;
        }

        // ─── MEVCUT AY LİSTESİ ───
        public List<string> GetAvailableMonths()
        {
            var months = new List<string>();
            if (Directory.Exists(_dataFolder))
            {
                foreach (var file in Directory.GetFiles(_dataFolder, "Trend_*.json"))
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    // "Trend_2025_01" -> "Ocak 2025"
                    var parts = name.Replace("Trend_", "").Split('_');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int y) && int.TryParse(parts[1], out int m))
                    {
                        months.Add(new DateTime(y, m, 1).ToString("MMMM yyyy"));
                    }
                }
            }
            return months;
        }

        // ─── DOSYA İŞLEMLERİ ───
        private List<TrendRecord> LoadFromFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    return JsonConvert.DeserializeObject<List<TrendRecord>>(json) ?? new();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TREND] Dosya okuma hatası: {ex.Message}");
            }
            return new();
        }

        private void SaveToFile(string filePath, List<TrendRecord> records)
        {
            try
            {
                string json = JsonConvert.SerializeObject(records, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TREND] Dosya yazma hatası: {ex.Message}");
            }
        }
    }
}
