using GoPxLSdk;
using GoPxLSdk.GoGdpMsg;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching; // DispatcherQueue için gerekli

namespace App4.Utilities
{
    // ==========================================
    // 1. JOB YÖNETİMİ (DOSYA YÜKLEME / DEĞİŞTİRME)
    // ==========================================
    public class GocatorJobLogic
    {
        private const string JOB_FILES_PATH = "/jobs/files";
        private const string JOB_LOAD_PATH = "/jobs/commands/load";
        private static string SENSOR_IP => GlobalData.Gocator_IpAddress;
        private static int CONTROL_PORT => GlobalData.Gocator_Port;

        // 1. SENSÖRDEKİ JOB DOSYALARINI LİSTELE
        public static async Task<List<string>> GetJobList(Action<string> log)
        {
            return await Task.Run(() =>
            {
                var list = new List<string>();
                try
                {
                    IPAddress ipAddress = IPAddress.Parse(SENSOR_IP);
                    using (GoSystem system = new GoSystem(ipAddress, (ushort)CONTROL_PORT))
                    {
                        system.Connect();
                        // expandLevel=1 ile dosya adlarını alıyoruz
                        JObject args = new JObject { ["expandLevel"] = 1 };
                        JObject response = system.Client().Read(JOB_FILES_PATH, args: args).GetResponse().Payload;

                        var items = response.SelectToken("_embedded.item");
                        if (items != null)
                        {
                            foreach (var item in items)
                            {
                                string name = item.SelectToken("fileName")?.ToString() ??
                                              item.SelectToken("jobName")?.ToString();
                                if (!string.IsNullOrEmpty(name)) list.Add(name);
                            }
                        }
                        system.Disconnect();
                    }
                }
                catch (Exception ex) { log($"Job Liste Hatası: {ex.Message}"); }
                return list;
            });
        }

        // 2. SEÇİLEN JOB'I AKTİF ET (LOAD)
        // Persistent bağlantı varsa onu kullanır, yoksa yeni bağlantı kurar.
        public static async Task<bool> LoadJob(string jobName, Action<string> log)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Persistent bağlantı varsa, sensörü durdur → job yükle → tekrar başlat
                    // Bu şekilde her seferinde connect/disconnect overhead'i olmaz
                    var system = ReceiveMeasurementLogic.GetPersistentSystem();
                    if (system != null)
                    {
                        log?.Invoke($"Job yükleniyor (persistent): {jobName}...");
                        try
                        {
                            if (system.RunningState() == GoSystem.State.Running)
                                system.Stop();
                        }
                        catch { }

                        JObject payload = new JObject { ["name"] = jobName };
                        system.Client().Call(JOB_LOAD_PATH, payload).CheckResponse(5000);

                        // Job yüklendi — sensörü Start ETME (ölçüm sırasında Start edilecek)
                        log?.Invoke($"Job yüklendi: {jobName}");
                        return true;
                    }

                    // Persistent bağlantı yoksa eski yöntemle bağlan
                    IPAddress ipAddress = IPAddress.Parse(SENSOR_IP);
                    using (GoSystem newSystem = new GoSystem(ipAddress, (ushort)CONTROL_PORT))
                    {
                        log?.Invoke($"Sensöre bağlanılıyor ({jobName} yükle)...");
                        newSystem.Connect();
                        GlobalData.GocatorOnline = true;

                        if (newSystem.RunningState() == GoSystem.State.Running)
                            newSystem.Stop();

                        JObject payload = new JObject { ["name"] = jobName };
                        newSystem.Client().Call(JOB_LOAD_PATH, payload).CheckResponse(5000);

                        newSystem.Disconnect();
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    log?.Invoke($"Job Yükleme Hatası: {ex.Message}");
                    return false;
                }
            });
        }

        // 3. JOB İNDİR (BACKUP)
        public static async Task<string> DownloadJob(string jobName, Action<string> log)
        {
            return await Task.Run(() =>
            {
                try
                {
                    IPAddress ipAddress = IPAddress.Parse(SENSOR_IP);
                    using (GoSystem system = new GoSystem(ipAddress, (ushort)CONTROL_PORT))
                    {
                        system.Connect();
                        string readPath = $"{JOB_FILES_PATH}/{jobName}/data";
                        log?.Invoke($"Job indiriliyor: {jobName}...");

                        JObject response = system.Client().Read(readPath).GetResponse(10000).Payload;
                        byte[] data = response["content"].ToObject<byte[]>();

                        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        string path = Path.Combine(desktop, $"{jobName}.gpjob");
                        File.WriteAllBytes(path, data);

                        system.Disconnect();
                        return path;
                    }
                }
                catch (Exception ex)
                {
                    log?.Invoke($"Job İndirme Hatası: {ex.Message}");
                    return null;
                }
            });
        }

        // 4. BİLGİSAYARDAN JOB YÜKLE (UPLOAD)
        public static async Task<bool> UploadJob(string filePath, Action<string> log)
        {
            return await Task.Run(() =>
            {
                try
                {
                    byte[] content = File.ReadAllBytes(filePath);
                    string fileName = Path.GetFileNameWithoutExtension(filePath);

                    IPAddress ipAddress = IPAddress.Parse(SENSOR_IP);
                    using (GoSystem system = new GoSystem(ipAddress, (ushort)CONTROL_PORT))
                    {
                        log?.Invoke($"Job yükleniyor: {fileName}...");
                        system.Connect();

                        JObject payload = new JObject
                        {
                            ["fromLive"] = false,
                            ["name"] = fileName,
                            ["content"] = content
                        };

                        system.Client().Create(JOB_FILES_PATH, payload).CheckResponse(10000);
                        system.Disconnect();
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    log?.Invoke($"Upload Hatası: {ex.Message}");
                    return false;
                }
            });
        }
    }

    // ==========================================
    // 2. ÖLÇÜM ALMA (MEASUREMENT) — Persistent Connection
    // ==========================================
    public class ReceiveMeasurementLogic
    {
        private const string GOCATOR_CONTROL_PATH = "/controls/gocator";
        private const int RECEIVE_DATA_TIMEOUT_MSEC = 5000;
        private static string SENSOR_IP => GlobalData.Gocator_IpAddress;
        private static int CONTROL_PORT => GlobalData.Gocator_Port;

        // Software trigger endpoint — SDK: SCANNER_PATH + "/actions/trigger"
        // ENGINE_ID = LMIFringeSnapshot (G3 snapshot sensor), SCANNER_ID = scanner-0
        //
        // NOT: Trigger source (Software/Time/DigitalInput) job içinde ayarlı;
        //      GoPxL üzerinde manuel olarak "Software" yapılıyor. Dolayısıyla
        //      her tetik öncesi Update ile zorlamaya gerek yok → kod net.
        private const string SCANNER_PATH               = "/scan/engines/LMIFringeSnapshot/scanners/scanner-0";
        private const string SOFTWARE_TRIGGER_PATH      = SCANNER_PATH + "/actions/trigger";

        // ▼▼▼ PERSISTENT CONNECTION STATE ▼▼▼
        private static GoSystem _persistentSystem;
        private static GoGdpClient _persistentGdpClient;
        private static readonly object _connectionLock = new object();
        private static string _connectedIp;
        private static int _connectedPort;

        /// <summary>
        /// Sensör bağlantısını kur (Start YAPMAZ — her ölçüm kendi Start/Stop döngüsünü yapar).
        /// Bağlantı persistent kalır → Connect overhead'i sadece 1 kez.
        /// </summary>
        private static void EnsureConnected(Action<string> log)
        {
            // IP/Port değiştiyse eski bağlantıyı kapat
            if (_persistentSystem != null && (_connectedIp != SENSOR_IP || _connectedPort != CONTROL_PORT))
            {
                log?.Invoke("Sensör adresi değişti, yeniden bağlanılıyor...");
                DisconnectPersistent();
            }

            if (_persistentSystem != null)
            {
                // Bağlantı hala canlı mı kontrol et
                try
                {
                    _persistentSystem.RunningState(); // Bağlantı test
                    // GDP client kontrol
                    if (_persistentGdpClient == null)
                    {
                        _persistentGdpClient = new GoGdpClient();
                        _persistentGdpClient.Connect(_persistentSystem.Address, _persistentSystem.GdpPort());
                    }
                    return; // Bağlantı canlı
                }
                catch
                {
                    log?.Invoke("Sensör bağlantısı kopmuş, yeniden bağlanılıyor...");
                    DisconnectPersistent();
                }
            }

            // Yeni bağlantı kur
            IPAddress ipAddress = IPAddress.Parse(SENSOR_IP);
            _persistentSystem = new GoSystem(ipAddress, (ushort)CONTROL_PORT);
            _persistentSystem.Connect();
            GlobalData.GocatorOnline = true;
            _persistentSystem.Client().Update(GOCATOR_CONTROL_PATH, new JObject { ["enabled"] = true }).CheckResponse(5000);

            _persistentGdpClient = new GoGdpClient();
            _persistentGdpClient.Connect(_persistentSystem.Address, _persistentSystem.GdpPort());

            _connectedIp = SENSOR_IP;
            _connectedPort = CONTROL_PORT;
            log?.Invoke("Sensör bağlantısı kuruldu (persistent — Start yapılmadı).");
        }

        /// <summary>
        /// Persistent GoSystem referansı (LoadJob vb. için)
        /// </summary>
        public static GoSystem GetPersistentSystem() => _persistentSystem;

        /// <summary>
        /// Persistent bağlantıyı temizle (uygulama kapanışı, hata recovery, vb.)
        /// </summary>
        public static void DisconnectPersistent()
        {
            try { _persistentGdpClient?.Dispose(); } catch { }
            try
            {
                if (_persistentSystem != null)
                {
                    try { _persistentSystem.Stop(); } catch { }
                    try { _persistentSystem.Disconnect(); } catch { }
                    _persistentSystem.Dispose();
                }
            }
            catch { }
            _persistentSystem = null;
            _persistentGdpClient = null;
            _connectedIp = null;
            GlobalData.GocatorOnline = false;
        }

        // DÖNÜŞ TİPİNİ DEĞİŞTİRDİK: (int status, List<GocatorMeasurement> results)
        public static async Task<(int, List<GocatorMeasurement>)> ReceiveAndProcessMeasurements(Action<string> log, DispatcherQueue dispatcher)
        {
            return await Task.Run(() =>
            {
                var results = new List<GocatorMeasurement>();
                try
                {
                    lock (_connectionLock)
                    {
                        EnsureConnected(log);

                        // ▼▼▼ STOP → START → SNAPSHOT TRIGGER → RECEIVE → STOP ▼▼▼
                        // Önce sensörü durdur (eski GDP buffer temizlenir)
                        try
                        {
                            if (_persistentSystem.RunningState() != GoSystem.State.Ready)
                            {
                                _persistentSystem.Stop();
                                log?.Invoke("Sensör durduruldu (eski buffer temizlendi)");
                            }
                        }
                        catch { }

                        // GDP buffer'ı temizle (varsa eski ölçüm verisi)
                        try { _persistentGdpClient.ClearData(); } catch { }

                        // Start
                        _persistentSystem.Start();
                        log?.Invoke("Sensör başlatıldı");

                        // Software trigger (snapshot)
                        log?.Invoke("Software trigger (snapshot) gönderiliyor...");
                        try
                        {
                            _persistentSystem.Client().Call(SOFTWARE_TRIGGER_PATH).CheckResponse(5000);
                        }
                        catch (Exception exTrig)
                        {
                            log?.Invoke($"⚠ Trigger hatası: {exTrig.Message}");
                        }

                        log?.Invoke("Ölçüm verisi bekleniyor...");
                        _persistentGdpClient.ReceiveDataSync(RECEIVE_DATA_TIMEOUT_MSEC);

                        // Ölçüm alındı — sensörü durdur
                        try { _persistentSystem.Stop(); } catch { }

                        if (_persistentGdpClient.DataSet != null && _persistentGdpClient.DataSet.Count > 0)
                        {
                            int counter = 1;
                            for (int i = 0; i < _persistentGdpClient.DataSet.Count; i++)
                            {
                                var msg = _persistentGdpClient.DataSet.GdpMsgAt(i);
                                if (msg.Type == MessageType.Measurement && msg is GoGdpMeasurement mMsg)
                                {
                                    int gdpIndex = mMsg.GdpId;
                                    var newItem = new GocatorMeasurement
                                    {
                                        Id = counter++,
                                        SourceId = gdpIndex,
                                        Name = $"Measurement {gdpIndex}",
                                        Value = Math.Round(mMsg.Value, 3),
                                        Decision = mMsg.Decision.ToString(),
                                    };
                                    results.Add(newItem);
                                }
                            }

                            // SourceId'ye göre sırala (GDP ID: X=0,Y=1,Z=2,Roll=3,Pitch=4,Yaw=5)
                            results = results.OrderBy(m => m.SourceId).ToList();

                            // Çoklu nokta: her 6 değer = 1 nokta (PointIndex + anlamlı isim)
                            string[] axNames = { "X", "Y", "Z", "Roll", "Pitch", "Yaw" };
                            for (int si = 0; si < results.Count; si++)
                            {
                                results[si].Id = si + 1;
                                results[si].PointIndex = si / 6;
                                results[si].IsFirstInPoint = (si % 6 == 0);
                                results[si].Name = $"Nokta {si / 6 + 1} - {axNames[si % 6]}";
                            }

                            // Sıralanmış verileri UI listesine ekle
                            if (dispatcher != null)
                            {
                                var sortedCopy = results.ToList();
                                dispatcher.TryEnqueue(() =>
                                {
                                    GlobalData.LastMeasurements.Clear();
                                    foreach (var item in sortedCopy)
                                        GlobalData.LastMeasurements.Add(item);
                                    GlobalData.SaveMeasurements();
                                });
                            }

                            log?.Invoke($"✓ {results.Count} adet ölçüm alındı (GdpId sıralı).");
                        }
                        else
                        {
                            log?.Invoke("⚠ Ölçüm verisi bulunamadı (DataSet boş).");
                        }
                    }

                    // Sonuç kontrolü: ölçüm verisi yoksa status=0 döndür (başarısız)
                    if (results.Count == 0)
                    {
                        log?.Invoke("⚠ Hiç ölçüm noktası alınamadı — ölçüm başarısız.");
                        return (0, results);
                    }
                    return (1, results);
                }
                catch (Exception ex)
                {
                    // Hata durumunda bağlantıyı sıfırla — sonraki çağrıda yeniden bağlanır
                    DisconnectPersistent();
                    log?.Invoke($"✗ Ölçüm Hatası: {ex.Message}");
                    return (-1, null);
                }
            });
        }
    }
}