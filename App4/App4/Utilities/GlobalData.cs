using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Specialized; // CollectionChanged için gerekli

namespace App4.Utilities
{
    public static class GlobalData
    {
        // 1. Dosyanın kaydedileceği yol (AppData klasörü)
        private static readonly string _rfidFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "App4", "Saved_RFID_List.json");

        private static ObservableCollection<RfidDef> _knownRfids;

        public static ObservableCollection<RfidDef> KnownRfids
        {
            get
            {
                if (_knownRfids == null)
                {
                    // 2. Listeyi yüklemeyi dene
                    LoadRfids();
                }
                return _knownRfids;
            }
        }

        private static void LoadRfids()
        {
            _knownRfids = new ObservableCollection<RfidDef>();

            try
            {
                // Dosya varsa oradan oku
                if (File.Exists(_rfidFilePath))
                {
                    string json = File.ReadAllText(_rfidFilePath);
                    var list = JsonSerializer.Deserialize<List<RfidDef>>(json);

                    if (list != null)
                    {
                        foreach (var item in list)
                        {
                            _knownRfids.Add(item);
                        }
                    }
                }
                else
                {
                    // Dosya yoksa VARSAYILANLARI yükle (İlk açılış)
                    _knownRfids.Add(new RfidDef { Id = "RF123", Description = "Klima A Tipi" });
                    _knownRfids.Add(new RfidDef { Id = "RF456", Description = "Klima B Tipi" });
                    _knownRfids.Add(new RfidDef { Id = "RF789", Description = "Klima C Tipi" });

                    // Varsayılanları hemen dosyaya yaz ki dosya oluşsun
                    SaveRfids();
                }
            }
            catch
            {
                // Hata durumunda boş liste kalmasın diye varsayılan ekle
                _knownRfids.Add(new RfidDef { Id = "ERR01", Description = "Yükleme Hatası" });
            }

            // 3. KRİTİK NOKTA: Listede değişiklik (Ekleme/Silme) olduğunda otomatik kaydet
            _knownRfids.CollectionChanged += KnownRfids_CollectionChanged;
        }

        // Liste değişince tetiklenen olay
        private static void KnownRfids_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SaveRfids();
        }

        // Diske yazma işlemi
        public static void SaveRfids()
        {
            try
            {
                var dir = Path.GetDirectoryName(_rfidFilePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                // Türkçe karakter ve okunaklı format için seçenekler
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                string json = JsonSerializer.Serialize(_knownRfids, options);
                File.WriteAllText(_rfidFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RFID Kayıt Hatası: {ex.Message}");
            }
        }
    }
}