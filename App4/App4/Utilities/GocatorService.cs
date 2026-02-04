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
        private const string SENSOR_IP = "192.168.251.30";
        private const int CONTROL_PORT = 3600;

        // 1. SENSÖRDEKİ JOB DOSYALARINI LİSTELE
        public static async Task<List<string>> GetJobList(Action<string> log)
        {
            return await Task.Run(() =>
            {
                var list = new List<string>();
                try
                {
                    IPAddress ipAddress = IPAddress.Parse(SENSOR_IP);
                    using (GoSystem system = new GoSystem(ipAddress, CONTROL_PORT))
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
        public static async Task<bool> LoadJob(string jobName, Action<string> log)
        {
            return await Task.Run(() =>
            {
                try
                {
                    IPAddress ipAddress = IPAddress.Parse(SENSOR_IP);
                    using (GoSystem system = new GoSystem(ipAddress, CONTROL_PORT))
                    {
                        // log?.Invoke ile null kontrolü yapıyoruz (Arka planda log olmayabilir)
                        log?.Invoke($"Sensöre bağlanılıyor ({jobName} yükle)...");
                        system.Connect();

                        if (system.RunningState() == GoSystem.State.Running)
                            system.Stop();

                        JObject payload = new JObject { ["name"] = jobName };
                        system.Client().Call(JOB_LOAD_PATH, payload).CheckResponse(5000);

                        system.Disconnect();
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
                    using (GoSystem system = new GoSystem(ipAddress, CONTROL_PORT))
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
                    using (GoSystem system = new GoSystem(ipAddress, CONTROL_PORT))
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
    // 2. ÖLÇÜM ALMA (MEASUREMENT)
    // ==========================================
    public class ReceiveMeasurementLogic
    {
        private const string GOCATOR_CONTROL_PATH = "/controls/gocator";
        private const int RECEIVE_DATA_TIMEOUT_MSEC = 5000;
        private const string SENSOR_IP = "192.168.251.30";
        private const int CONTROL_PORT = 3600;

        // DÖNÜŞ TİPİNİ DEĞİŞTİRDİK: (int status, List<GocatorMeasurement> results)
        public static async Task<(int, List<GocatorMeasurement>)> ReceiveAndProcessMeasurements(Action<string> log, DispatcherQueue dispatcher)
        {
            return await Task.Run(() =>
            {
                var results = new List<GocatorMeasurement>();
                try
                {
                    IPAddress ipAddress = IPAddress.Parse(SENSOR_IP);
                    using (GoSystem system = new GoSystem(ipAddress, CONTROL_PORT))
                    {
                        log?.Invoke("Sensöre bağlanılıyor (Ölçüm)...");
                        system.Connect();
                        system.Client().Update(GOCATOR_CONTROL_PATH, new JObject { ["enabled"] = true }).CheckResponse(5000);

                        using (GoGdpClient gdpClient = new GoGdpClient())
                        {
                            gdpClient.Connect(system.Address, system.GdpPort());
                            
                            try
                            {
                                if (system.RunningState() == GoSystem.State.Ready) system.Start();

                                log?.Invoke("Ölçüm verisi bekleniyor...");
                                gdpClient.ReceiveDataSync(RECEIVE_DATA_TIMEOUT_MSEC);

                                if (gdpClient.DataSet != null && gdpClient.DataSet.Count > 0)
                                {
                                    // UI Temizliği (Sadece Dispatcher varsa)
                                    if (dispatcher != null) dispatcher.TryEnqueue(() => GlobalData.LastMeasurements.Clear());

                                    int counter = 1;
                                    for (int i = 0; i < gdpClient.DataSet.Count; i++)
                                    {
                                        var msg = gdpClient.DataSet.GdpMsgAt(i);
                                        if (msg.Type == MessageType.Measurement && msg is GoGdpMeasurement mMsg)
                                        {
                                            int.TryParse(mMsg.DataSourceId, out int parsedSourceId);
                                            var newItem = new GocatorMeasurement
                                            {
                                                Id = counter++,
                                                SourceId = parsedSourceId,
                                                Name = $"Measurement {mMsg.DataSourceId}",
                                                Value = Math.Round(mMsg.Value, 3),
                                                Decision = mMsg.Decision.ToString(),
                                            };

                                            // 1. Sonuç listesine ekle (GlobalData için)
                                            results.Add(newItem);

                                            // 2. UI Listesine ekle (Eğer sayfa açıksa)
                                            if (dispatcher != null)
                                            {
                                                dispatcher.TryEnqueue(() =>
                                                {
                                                    GlobalData.LastMeasurements.Add(newItem);
                                                    GlobalData.SaveMeasurements();
                                                });
                                            }
                                        }
                                    }
                                    log?.Invoke($"✓ {results.Count} adet ölçüm alındı.");
                                }
                                else
                                {
                                    log?.Invoke("⚠ Ölçüm verisi bulunamadı.");
                                }
                            }
                            finally
                            {
                                // Timeout veya hata durumunda sensörü durdurmayı garantiye al
                                try 
                                { 
                                    system.Stop(); 
                                } 
                                catch { }
                            }
                        }
                    }
                    return (1, results);
                }
                catch (Exception ex)
                {
                    log?.Invoke($"✗ Ölçüm Hatası: {ex.Message}");
                    return (-1, null);
                }
            });
        }
    }
}