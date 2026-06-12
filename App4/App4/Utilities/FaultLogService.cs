using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace App4.Utilities
{
    // ═══════════════════════════════════════════════════════════════
    // ARIZA KAYIT SERVİSİ (JSON dosya tabanlı — TrendDataService deseni)
    //   • Aylık kovalar: FaultData\Fault_2026_06.json
    //   • Açık/kapalı kayıt mantığı (RaiseFault / ClearFault) → spam önler
    //   • 1 yıllık retention (PurgeOldFiles — 12 aydan eski ay dosyaları silinir)
    // ═══════════════════════════════════════════════════════════════
    public class FaultLogService
    {
        private static FaultLogService _instance;
        public static FaultLogService Instance => _instance ??= new FaultLogService();

        private readonly string _dataFolder;
        private readonly object _fileLock = new();
        private readonly object _stateLock = new();

        // Açık (aktif) kayıtlar — anahtar: kaynak (örn. "R1"). Aynı kaynak için tek açık kayıt.
        private readonly Dictionary<string, FaultRecord> _aktifKayitlar = new();

        /// <summary>Kayıt eklenince/güncellenince/silinince tetiklenir (açık sayfanın canlı yenilenmesi için).</summary>
        public event Action OnChanged;

        private FaultLogService()
        {
            _dataFolder = Path.Combine(GlobalData.ConfigBaseDir, "FaultData");
            if (!Directory.Exists(_dataFolder))
                Directory.CreateDirectory(_dataFolder);
        }

        private string GetMonthlyFilePath(DateTime date)
            => Path.Combine(_dataFolder, $"Fault_{date:yyyy_MM}.json");

        // ═══ EDGE / DE-DUP API (çağıran taraf seviye gönderse bile servis kenarı yönetir) ═══

        /// <summary>Arıza GELDİ. Aynı 'key' zaten açıksa hiçbir şey yapmaz (tekrar kaydı engeller).</summary>
        public void RaiseFault(string key, string kaynak, int kod, string mesaj, string severity, int robotNo, int aktifNokta)
        {
            if (string.IsNullOrEmpty(key)) return;
            FaultRecord rec;
            lock (_stateLock)
            {
                if (_aktifKayitlar.ContainsKey(key)) return; // zaten açık → spam yok
                rec = new FaultRecord
                {
                    Timestamp = DateTime.Now,
                    Kaynak = kaynak,
                    RobotNo = robotNo,
                    Kod = kod,
                    Mesaj = mesaj,
                    Severity = string.IsNullOrEmpty(severity) ? "Arıza" : severity,
                    AktifNokta = aktifNokta,
                    Aktif = true
                };
                _aktifKayitlar[key] = rec;
            }
            AddRecord(rec);
            System.Diagnostics.Debug.WriteLine($"[FAULT] Açıldı: {kaynak} kod={kod} \"{mesaj}\"");
            OnChanged?.Invoke();
        }

        /// <summary>Arıza GEÇTİ. Açık kayıt varsa BitisZamani + Aktif=false yazar (süre dolar).</summary>
        public void ClearFault(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            FaultRecord rec;
            lock (_stateLock)
            {
                if (!_aktifKayitlar.TryGetValue(key, out rec) || rec == null) return;
                _aktifKayitlar.Remove(key);
            }
            rec.BitisZamani = DateTime.Now;
            rec.Aktif = false;
            UpdateRecord(rec);
            System.Diagnostics.Debug.WriteLine($"[FAULT] Kapandı: {key} ({rec.SureStr})");
            OnChanged?.Invoke();
        }

        // ═══ DOSYA İŞLEMLERİ ═══

        public void AddRecord(FaultRecord record)
        {
            try
            {
                string filePath = GetMonthlyFilePath(record.Timestamp);
                lock (_fileLock)
                {
                    var records = LoadFromFile(filePath);
                    records.Add(record);
                    SaveToFile(filePath, records);
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[FAULT] Ekleme hatası: {ex.Message}"); }
        }

        /// <summary>Açık kaydı kapatırken (BitisZamani yaz) aynı aydaki dosyada Id ile değiştirir.</summary>
        public void UpdateRecord(FaultRecord record)
        {
            if (record == null) return;
            try
            {
                string filePath = GetMonthlyFilePath(record.Timestamp);
                lock (_fileLock)
                {
                    var records = LoadFromFile(filePath);
                    int idx = records.FindIndex(r => r.Id == record.Id);
                    if (idx >= 0) records[idx] = record;
                    else records.Add(record);
                    SaveToFile(filePath, records);
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[FAULT] Güncelleme hatası: {ex.Message}"); }
        }

        /// <summary>Tek kaydı siler (Id ile; bulunamazsa zaman+kaynak+kod ile). Başarılıysa true.</summary>
        public bool DeleteRecord(FaultRecord record)
        {
            if (record == null) return false;
            bool ok = false;
            try
            {
                string filePath = GetMonthlyFilePath(record.Timestamp);
                lock (_fileLock)
                {
                    var records = LoadFromFile(filePath);
                    int removed = !string.IsNullOrEmpty(record.Id)
                        ? records.RemoveAll(r => r.Id == record.Id)
                        : records.RemoveAll(r => r.Timestamp == record.Timestamp && r.Kaynak == record.Kaynak && r.Kod == record.Kod);
                    if (removed > 0) { SaveToFile(filePath, records); ok = true; }
                }
                // Açık listede de varsa düş
                lock (_stateLock)
                {
                    var k = _aktifKayitlar.FirstOrDefault(p => p.Value?.Id == record.Id).Key;
                    if (k != null) _aktifKayitlar.Remove(k);
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[FAULT] Silme hatası: {ex.Message}"); }
            if (ok) OnChanged?.Invoke();
            return ok;
        }

        /// <summary>TÜM arıza kayıtlarını siler (tüm aylık Fault_*.json). Silinen dosya sayısını döner.</summary>
        public int ClearAllRecords()
        {
            int deleted = 0;
            try
            {
                lock (_fileLock)
                {
                    foreach (var file in Directory.GetFiles(_dataFolder, "Fault_*.json"))
                    { try { File.Delete(file); deleted++; } catch { } }
                }
                lock (_stateLock) { _aktifKayitlar.Clear(); }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[FAULT] Temizleme hatası: {ex.Message}"); }
            OnChanged?.Invoke();
            return deleted;
        }

        // ═══ SORGULAMA ═══
        public List<FaultRecord> GetRecords(DateTime startDate, DateTime endDate,
            string kaynak = null, string severity = null, bool? sadeceAktif = null)
        {
            var all = new List<FaultRecord>();
            var current = new DateTime(startDate.Year, startDate.Month, 1);
            var end = new DateTime(endDate.Year, endDate.Month, 1).AddMonths(1);

            while (current < end)
            {
                string fp = GetMonthlyFilePath(current);
                if (File.Exists(fp))
                {
                    lock (_fileLock) { all.AddRange(LoadFromFile(fp)); }
                }
                current = current.AddMonths(1);
            }

            var q = all.Where(r => r.Timestamp >= startDate && r.Timestamp <= endDate);
            if (!string.IsNullOrEmpty(kaynak) && kaynak != "Tümü") q = q.Where(r => r.Kaynak == kaynak);
            if (!string.IsNullOrEmpty(severity) && severity != "Tümü") q = q.Where(r => r.Severity == severity);
            if (sadeceAktif.HasValue) q = q.Where(r => r.Aktif == sadeceAktif.Value);

            return q.OrderByDescending(r => r.Timestamp).ToList();
        }

        // ═══ CSV DIŞA AKTARMA ═══
        public string ExportToCsv(List<FaultRecord> records, string filePath)
        {
            try
            {
                using var w = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);
                w.WriteLine("Tarih;Saat;Kaynak;Kod;Mesaj;Severity;Durum;Süre;Aktif Nokta");
                foreach (var r in records)
                {
                    w.WriteLine(string.Join(";",
                        r.DateStr, r.TimeStr, r.Kaynak, r.Kod,
                        (r.Mesaj ?? "").Replace(";", ","), r.Severity, r.DurumStr, r.SureStr, r.AktifNokta));
                }
                return filePath;
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[FAULT] CSV export hatası: {ex.Message}"); return null; }
        }

        // ═══ RETENTION — 1 AY (1 aydan eski ay dosyaları silinir; PC şişmesin) ═══
        // Aylık kovalar olduğundan: içinde bulunulan ay + bir önceki ay tutulur,
        // daha eskileri silinir. Böylece her an en fazla ~son 1-2 ay veri kalır.
        public int PurgeOldFiles()
        {
            int deleted = 0;
            try
            {
                var cutoff = DateTime.Now.AddMonths(-1);
                var cutoffMonth = new DateTime(cutoff.Year, cutoff.Month, 1);
                lock (_fileLock)
                {
                    foreach (var file in Directory.GetFiles(_dataFolder, "Fault_*.json"))
                    {
                        var name = Path.GetFileNameWithoutExtension(file);
                        var parts = name.Replace("Fault_", "").Split('_');
                        if (parts.Length == 2 && int.TryParse(parts[0], out int y) && int.TryParse(parts[1], out int m))
                        {
                            if (new DateTime(y, m, 1) < cutoffMonth)
                            { try { File.Delete(file); deleted++; } catch { } }
                        }
                    }
                }
                if (deleted > 0) System.Diagnostics.Debug.WriteLine($"[FAULT] {deleted} eski ay dosyası silindi (>1 ay).");
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[FAULT] Purge hatası: {ex.Message}"); }
            return deleted;
        }

        // ═══ DOSYA OKU/YAZ ═══
        private List<FaultRecord> LoadFromFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    return JsonConvert.DeserializeObject<List<FaultRecord>>(json) ?? new();
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[FAULT] Dosya okuma hatası: {ex.Message}"); }
            return new();
        }

        private void SaveToFile(string filePath, List<FaultRecord> records)
        {
            try { File.WriteAllText(filePath, JsonConvert.SerializeObject(records, Formatting.Indented)); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[FAULT] Dosya yazma hatası: {ex.Message}"); }
        }
    }
}
