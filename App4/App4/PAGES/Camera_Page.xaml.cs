using GoPxLSdk;
using GoPxLSdk.GoGdpMsg;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using static GoPxLSdkSamplesCommon.Utilities;
//deneme test
//asdasd
namespace App4.PAGES
{
    public sealed partial class Camera_Page : Page
    {
        private List<string> logHistory = new();
        private WebView2Manager webViewManager;

        public Camera_Page()
        {
            this.InitializeComponent();
            webViewManager = new WebView2Manager(PointCloudWebView, AddLog);
        }

        #region ═══ Logging System ═══

        private void AddLog(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string logEntry = $"[{timestamp}] {message}";
            logHistory.Add(logEntry);

            if (logHistory.Count > 100)
                logHistory.RemoveAt(0);

            this.DispatcherQueue.TryEnqueue(() =>
            {
                LogOutput.Text = string.Join("\n", logHistory);
            });

            Debug.WriteLine(logEntry);
        }

        #endregion

        #region ═══ WebView2 Initialization ═══

        private async void PointCloudWebView_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
        {
            try
            {
                AddLog("► WebView2 CoreWebView2Initialized event tetiklendi");

                try
                {
                    // ms-appx-web:// protokolü ile doğrudan yükle
                    string url = "ms-appx-web:///Assets/PointCloud3DViewer.html";
                    AddLog($"► URL yükleniyor: {url}");
                    sender.CoreWebView2.Navigate(url);
                    AddLog("✓ URL navigate başarılı");
                }
                catch (Exception navEx)
                {
                    AddLog($"✗ Navigate hatası: {navEx.Message}");
                    return;
                }

                await Task.Delay(2000); // HTML tam yüklenmesi için bekle
                AddLog("✓ WebView2 tamamen başlatıldı");
            }
            catch (Exception ex)
            {
                AddLog($"✗ WebView2 başlatma hatası: {ex.Message}");
                AddLog($"  Stack: {ex.StackTrace}");
            }
        }

        #endregion

        #region ═══ Photo Capture ═══

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusLabel.Text = "Haberleşme Katmanı çalıştırılıyor...";
                StatusLabel.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Orange);
                PhotoButton.IsEnabled = false;
                LoadingRing.IsActive = true;

                AddLog("► ÇEK işlemi başlatılıyor...");
                int result = await ReceiveImageSample.ReceiveImageNet(AddLog);

                if (result == OK_STATUS)
                {
                    StatusLabel.Text = "BAŞARILI: Çekim yapıldı ve kaydedildi.";
                    StatusLabel.Foreground = new SolidColorBrush(Microsoft.UI.Colors.LightGreen);
                    PhotoStatus.Text = "OK Başarıyla çekildi!";
                    PhotoStatus.Foreground = new SolidColorBrush(Microsoft.UI.Colors.LimeGreen);
                    AddLog("✓ Çekim başarıyla tamamlandı");
                    await ShowCapturedImage();
                }
                else
                {
                    StatusLabel.Text = "HATA: İşlem başarısız (Kod: " + result + ")";
                    StatusLabel.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
                    PhotoStatus.Text = "HATA Çekim başarısız";
                    PhotoStatus.Foreground = new SolidColorBrush(Microsoft.UI.Colors.IndianRed);
                    AddLog("✗ Çekim başarısız!");
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Text = "Uygulama Hatası: " + ex.Message;
                PhotoStatus.Text = "HATA " + ex.Message;
                AddLog($"✗ HATA: {ex.Message}");
            }
            finally
            {
                LoadingRing.IsActive = false;
                PhotoButton.IsEnabled = true;
            }
        }

        private async Task ShowCapturedImage()
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                DirectoryInfo di = new(desktopPath);

                FileInfo? rawFile = di.GetFiles("Gocator_*.raw")
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();

                if (rawFile != null)
                {
                    AddLog($"► Görüntü dosyası bulundu: {rawFile.Name}");
                    await ConvertRawToBmpAndDisplay(rawFile, SensorDisplay);
                }
                else
                {
                    AddLog("✗ Görüntü dosyası (RAW) bulunamadı");
                }
            }
            catch (Exception ex)
            {
                AddLog($"✗ Görüntüleme Hatası: {ex.Message}");
            }
        }

        private async Task ConvertRawToBmpAndDisplay(FileInfo rawFile, Image targetImage)
        {
            try
            {
                string namePart = rawFile.Name.Replace("Gocator_", "").Replace(".raw", "");
                string[] parts = namePart.Split('_');

                if (parts.Length >= 2 && int.TryParse(parts[0], out int width) && int.TryParse(parts[1], out int height))
                {
                    AddLog($"► Görüntü boyutu: {width}x{height}");
                    byte[] rawBytes = await Task.Run(() => File.ReadAllBytes(rawFile.FullName));

                    if (rawBytes.Length != width * height)
                    {
                        AddLog($"✗ Dosya boyutu eşleşmiyor. Beklenen: {width * height}, Alınan: {rawBytes.Length}");
                        return;
                    }

                    byte[] bgraBytes = new byte[width * height * 4];
                    for (int i = 0; i < rawBytes.Length; i++)
                    {
                        int pos = i * 4;
                        byte val = rawBytes[i];
                        bgraBytes[pos] = val;
                        bgraBytes[pos + 1] = val;
                        bgraBytes[pos + 2] = val;
                        bgraBytes[pos + 3] = 255;
                    }

                    string bmpPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        $"Gocator_{width}_{height}.bmp");

                    await SaveBitmapFile(bmpPath, bgraBytes, width, height);
                    AddLog($"✓ BMP dosyası kaydedildi: {Path.GetFileName(bmpPath)}");
                    await DisplayBitmapFile(new FileInfo(bmpPath), targetImage);
                    AddLog("✓ Görüntü başarıyla gösterildi");
                }
            }
            catch (Exception ex)
            {
                AddLog($"✗ Dönüştürme Hatası: {ex.Message}");
            }
        }

        private async Task SaveBitmapFile(string filePath, byte[] bgraData, int width, int height)
        {
            try
            {
                using (FileStream fs = new(filePath, FileMode.Create, FileAccess.Write))
                {
                    byte[] fileHeader = new byte[14];
                    fileHeader[0] = (byte)'B';
                    fileHeader[1] = (byte)'M';

                    int fileSize = 54 + (width * height * 3);
                    BitConverter.GetBytes(fileSize).CopyTo(fileHeader, 2);
                    BitConverter.GetBytes(54).CopyTo(fileHeader, 10);

                    byte[] dibHeader = new byte[40];
                    BitConverter.GetBytes(40).CopyTo(dibHeader, 0);
                    BitConverter.GetBytes(width).CopyTo(dibHeader, 4);
                    BitConverter.GetBytes(height).CopyTo(dibHeader, 8);
                    dibHeader[12] = 1;
                    dibHeader[14] = 24;

                    await fs.WriteAsync(fileHeader, 0, fileHeader.Length);
                    await fs.WriteAsync(dibHeader, 0, dibHeader.Length);

                    byte[] pixelData = new byte[width * height * 3];
                    for (int i = 0; i < width * height; i++)
                    {
                        int srcPos = i * 4;
                        int dstPos = i * 3;
                        pixelData[dstPos] = bgraData[srcPos];
                        pixelData[dstPos + 1] = bgraData[srcPos + 1];
                        pixelData[dstPos + 2] = bgraData[srcPos + 2];
                    }

                    await fs.WriteAsync(pixelData, 0, pixelData.Length);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BMP Kayıt Hatası: {ex.Message}");
                throw;
            }
        }

        private async Task DisplayBitmapFile(FileInfo bmpFile, Image targetImage)
        {
            try
            {
                using (IRandomAccessStream stream = await (await StorageFile.GetFileFromPathAsync(bmpFile.FullName))
                    .OpenAsync(FileAccessMode.Read))
                {
                    var bitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                    await bitmap.SetSourceAsync(stream);
                    targetImage.Source = bitmap;
                }
            }
            catch (Exception ex)
            {
                AddLog($"✗ DisplayBitmapFile Hatası: {ex.Message}");
            }
        }

        #endregion

        #region ═══ Surface / Point Cloud ═══

        private async void SurfaceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cloud3DStatus.Text = "⏳ İşleniyor...";
                Cloud3DStatus.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Orange);
                SurfaceButton.IsEnabled = false;

                AddLog("► SURFACE işlemi başlatılıyor...");

                if (!await webViewManager.EnsureInitializedAsync())
                {
                    Cloud3DStatus.Text = "✗ HATA: WebView2 başlatılamadı";
                    Cloud3DStatus.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
                    AddLog("✗ WebView2 hazırlanamadı!");
                    return;
                }

                var (result, pointCloudJson) = await ReceiveSurfaceSample.ReceiveSurfacePointCloudNet(AddLog);

                if (result == OK_STATUS && !string.IsNullOrEmpty(pointCloudJson))
                {
                    Cloud3DStatus.Text = "✓ Point Cloud hazır!";
                    Cloud3DStatus.Foreground = new SolidColorBrush(Microsoft.UI.Colors.LimeGreen);
                    AddLog("✓ Point Cloud başarıyla işlendi");
                    await webViewManager.SendPointCloudAsync(pointCloudJson);
                }
                else
                {
                    Cloud3DStatus.Text = "✗ HATA Başarısız";
                    Cloud3DStatus.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
                    AddLog("✗ Surface işlemi başarısız!");
                }
            }
            catch (Exception ex)
            {
                Cloud3DStatus.Text = "HATA " + ex.Message;
                Cloud3DStatus.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
                AddLog($"✗ HATA: {ex.Message}");
            }
            finally
            {
                SurfaceButton.IsEnabled = true;
            }
        }

        #endregion
    }

    #region ═══ WebView2 Manager ═══

    public class WebView2Manager
    {
        private readonly WebView2 webView;
        private readonly Action<string> log;

        public WebView2Manager(WebView2 webView, Action<string> log)
        {
            this.webView = webView;
            this.log = log;
        }

        public async Task<bool> EnsureInitializedAsync()
        {
            try
            {
                if (webView?.CoreWebView2 != null)
                {
                    log("✓ WebView2 hazır");
                    return true;
                }

                log("► WebView2 initialization bekleniyor...");
                int maxAttempts = 100;
                int attempt = 0;

                while (webView?.CoreWebView2 == null && attempt < maxAttempts)
                {
                    await Task.Delay(100);
                    attempt++;

                    if (attempt % 10 == 0)
                        log($"  ⏳ Bekleniyor... ({attempt * 100 / 1000}s)");
                }

                if (webView?.CoreWebView2 != null)
                {
                    log("✓ WebView2 hazır");
                    return true;
                }

                log("✗ WebView2 zaman aşımı");
                return false;
            }
            catch (Exception ex)
            {
                log($"✗ WebView2 hatası: {ex.Message}");
                return false;
            }
        }

        public async Task SendPointCloudAsync(string pointCloudJson)
        {
            try
            {
                log("► Point Cloud gönderiliyor...");

                if (webView?.CoreWebView2 == null)
                {
                    log("✗ CoreWebView2 null");
                    return;
                }

                // Viewer initialize et
                await webView.CoreWebView2.ExecuteScriptAsync("window.initPointCloudViewer?.();");
                await Task.Delay(300);

                // JSON'u güvenli şekilde gönder
                string escapedJson = JsonConvert.ToString(pointCloudJson);
                string script = $"window.loadPointCloud?.({escapedJson});";

                log("► JavaScript çalıştırılıyor...");
                string result = await webView.CoreWebView2.ExecuteScriptAsync(script);
                log($"✓ Point Cloud gönderildi");
            }
            catch (Exception ex)
            {
                log($"✗ Gönderme hatası: {ex.Message}");
            }
        }
    }

    #endregion

    #region ═══ Image Capture ═══

    public class ReceiveImageSample
    {
        private const string SCAN_MODE_PATH = "$.parameters.scanModeSettings.scanMode";
        private const int IMAGE_MODE = 0;
        private const string GOCATOR_CONTROL_PATH = "/controls/gocator";
        private const string GOCATOR_OUTPUT_PATH = GOCATOR_CONTROL_PATH + "/outputs";
        private const string GOCATOR_ADD_OUTPUT_PATH = GOCATOR_OUTPUT_PATH + "/commands/add";
        private const string REPLAY_PATH = "/replay/playback";
        private const int RECEIVE_DATA_TIMEOUT_MSEC = 60000;
        private const string SENSOR_IP = "192.168.251.40";
        private const int CONTROL_PORT = 3600;

        public static async Task<int> ReceiveImageNet(Action<string>? log = null)
        {
            IPAddress ipAddress = IPAddress.Parse(SENSOR_IP);

            return await Task.Run(async () =>
            {
                using (GoSystem system = new GoSystem(ipAddress, CONTROL_PORT))
                {
                    try
                    {
                        log?.Invoke("► Sensora bağlanıyor...");
                        system.Connect();
                        log?.Invoke("✓ Sensora bağlandı");

                        if (VerifyConnection(system) == ERROR_STATUS)
                        {
                            log?.Invoke("✗ Connection doğrulama başarısız");
                            return ERROR_STATUS;
                        }

                        log?.Invoke("✓ Connection doğrulandı");

                        log?.Invoke("► Replay durumu kontrol ediliyor...");
                        JObject response = system.Client().Read(REPLAY_PATH).GetResponse().Payload;
                        bool replayDataEnabled = (bool)response.GetValue("enabled")!;
                        log?.Invoke($"   Replay enabled: {replayDataEnabled}");

                        if (!replayDataEnabled)
                        {
                            log?.Invoke("► Scanner yapılandırması okunuyor...");
                            response = system.Client().Read(SCANNER_PATH).GetResponse().Payload;

                            if ((int)response.SelectToken(SCAN_MODE_PATH)! != IMAGE_MODE)
                            {
                                log?.Invoke("► Scan mode IMAGE_MODE olarak ayarlanıyor...");
                                JObject payload = new JObject
                                {
                                    ["parameters"] = new JObject
                                    {
                                        ["scanModeSettings"] = new JObject { ["scanMode"] = IMAGE_MODE }
                                    }
                                };
                                system.Client().Update(SCANNER_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
                                await Task.Delay(500);
                                log?.Invoke("✓ Scan mode ayarlandı");
                            }
                        }

                        log?.Invoke("► Gocator Protocol etkinleştiriliyor...");
                        system.Client().Update(GOCATOR_CONTROL_PATH, new JObject { ["enabled"] = true })
                            .CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
                        await Task.Delay(500);
                        log?.Invoke("✓ Gocator Protocol etkinleştirildi");

                        log?.Invoke("► Sistem başlatılıyor...");
                        if (system.RunningState() == GoSystem.State.Ready)
                        {
                            system.Start();
                            await Task.Delay(1000);
                        }
                        log?.Invoke("✓ Sistem başlatıldı");

                        string dataSourceKey = "Image";
                        string imageDataSourceId = $"scan:{ENGINE_ID}:{SCANNER_ID}:{SCAN_ENGINE_COMPONENT}{dataSourceKey}0";

                        bool imageOutputAdded = false;
                        try
                        {
                            response = system.Client().Read(GOCATOR_OUTPUT_PATH).GetResponse().Payload;
                            JArray map = (JArray)response.GetValue("map")!;

                            for (int i = 0; i < map.Count; i++)
                            {
                                if (map[i]?.ToString().Contains(dataSourceKey) == true)
                                {
                                    imageOutputAdded = true;
                                    log?.Invoke("► Image output zaten eklenmiş");
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            log?.Invoke($"⚠ Output kontrol hatası: {ex.Message}");
                        }

                        if (!imageOutputAdded)
                        {
                            log?.Invoke("► Image output ekleniyor...");
                            try
                            {
                                JObject payload = new JObject
                                {
                                    ["source"] = imageDataSourceId,
                                    ["outputId"] = 0,
                                    ["autoShift"] = true
                                };
                                system.Client().Call(GOCATOR_ADD_OUTPUT_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
                                log?.Invoke("✓ Image output eklendi");
                            }
                            catch (Exception ex)
                            {
                                log?.Invoke($"⚠ Output ekleme hatası: {ex.Message}");
                            }
                        }

                        log?.Invoke("► GDP Client bağlanıyor...");
                        using (GoGdpClient gdpClient = new GoGdpClient())
                        {
                            gdpClient.Connect(system.Address, system.GdpPort());
                            log?.Invoke("✓ GDP Client bağlandı");

                            log?.Invoke("► Veri bekleniyor (timeout: 60 saniye)...");
                            gdpClient.ReceiveDataSync(RECEIVE_DATA_TIMEOUT_MSEC);
                            log?.Invoke($"✓ Veri alındı. DataSet sayısı: {gdpClient.DataSet?.Count ?? 0}");

                            if (gdpClient.DataSet != null && gdpClient.DataSet.Count > 0)
                            {
                                for (int msgIndex = 0; msgIndex < gdpClient.DataSet.Count; msgIndex++)
                                {
                                    GoGdpMsg msg = gdpClient.DataSet.GdpMsgAt(msgIndex);

                                    if (msg.Type == MessageType.Image)
                                    {
                                        GoGdpImage? imageMsg = msg as GoGdpImage;
                                        if (imageMsg != null)
                                        {
                                            log?.Invoke($"✓ Image mesajı: {imageMsg.Width}x{imageMsg.Height}");

                                            byte[,] pixelArray = imageMsg.Pixels;
                                            int rows = pixelArray.GetLength(0);
                                            int cols = pixelArray.GetLength(1);

                                            byte[] flatData = new byte[rows * cols];
                                            System.Buffer.BlockCopy(pixelArray, 0, flatData, 0, flatData.Length);

                                            string path = Path.Combine(
                                                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                                                $"Gocator_{cols}_{rows}.raw");

                                            File.WriteAllBytes(path, flatData);
                                            log?.Invoke($"✓ RAW kaydedildi: {Path.GetFileName(path)}");
                                        }
                                    }
                                }
                            }

                            gdpClient.Close();
                        }

                        log?.Invoke("► Sistem durduruluyor...");
                        system.Stop();
                        log?.Invoke("✓ Sistem durduruldu");
                        log?.Invoke("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                        log?.Invoke("✓✓✓ BAŞARILI ✓✓✓");
                        log?.Invoke("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                        return OK_STATUS;
                    }
                    catch (Exception ex)
                    {
                        log?.Invoke($"✗ HATA: {ex.Message}");
                        log?.Invoke($"  Stack: {ex.StackTrace}");
                        return ERROR_STATUS;
                    }
                }
            });
        }
    }

    #endregion

    #region ═══ Surface Point Cloud ═══

    public class ReceiveSurfaceSample
    {
        private const string SCAN_MODE_PATH = "$.parameters.scanModeSettings.scanMode";
        private const int SURFACE_MODE = 3;
        private const string GOCATOR_CONTROL_PATH = "/controls/gocator";
        private const string GOCATOR_OUTPUT_PATH = GOCATOR_CONTROL_PATH + "/outputs";
        private const string GOCATOR_ADD_OUTPUT_PATH = GOCATOR_OUTPUT_PATH + "/commands/add";
        private const string REPLAY_PATH = "/replay/playback";
        private const int RECEIVE_DATA_TIMEOUT_MSEC = 60000;
        private const string SENSOR_IP = "192.168.251.40";
        private const int CONTROL_PORT = 3600;

        public static async Task<(int status, string pointCloudJson)> ReceiveSurfacePointCloudNet(Action<string>? log = null)
        {
            IPAddress ipAddress = IPAddress.Parse(SENSOR_IP);

            return await Task.Run(async () =>
            {
                using (GoSystem system = new GoSystem(ipAddress, CONTROL_PORT))
                {
                    try
                    {
                        log?.Invoke("► Sensora bağlanıyor...");
                        system.Connect();
                        log?.Invoke("✓ Sensora bağlandı");

                        if (VerifyConnection(system) == ERROR_STATUS)
                        {
                            log?.Invoke("✗ Connection doğrulama başarısız");
                            return (ERROR_STATUS, string.Empty);
                        }

                        log?.Invoke("✓ Connection doğrulandı");

                        log?.Invoke("► Replay durumu kontrol ediliyor...");
                        JObject response = system.Client().Read(REPLAY_PATH).GetResponse().Payload;
                        bool replayDataEnabled = (bool)response.GetValue("enabled")!;
                        log?.Invoke($"   Replay enabled: {replayDataEnabled}");

                        if (!replayDataEnabled)
                        {
                            log?.Invoke("► Scanner yapılandırması okunuyor...");
                            response = system.Client().Read(SCANNER_PATH).GetResponse().Payload;

                            if ((int)response.SelectToken(SCAN_MODE_PATH)! != SURFACE_MODE)
                            {
                                log?.Invoke("► Scan mode SURFACE olarak ayarlanıyor...");
                                JObject payload = new JObject
                                {
                                    ["parameters"] = new JObject
                                    {
                                        ["scanModeSettings"] = new JObject { ["scanMode"] = SURFACE_MODE }
                                    }
                                };
                                system.Client().Update(SCANNER_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
                                await Task.Delay(1000);
                                log?.Invoke("✓ Scan mode SURFACE olarak ayarlandı");
                            }
                        }

                        log?.Invoke("► Intensity etkinleştiriliyor...");
                        JObject intensityPayload = new JObject
                        {
                            ["parameters"] = new JObject
                            {
                                ["scanModeSettings"] = new JObject { ["intensityEnabled"] = true }
                            }
                        };
                        system.Client().Update(SCANNER_PATH, intensityPayload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
                        await Task.Delay(1000);
                        log?.Invoke("✓ Intensity etkinleştirildi");

                        log?.Invoke("► Gocator Protocol etkinleştiriliyor...");
                        system.Client().Update(GOCATOR_CONTROL_PATH, new JObject { ["enabled"] = true })
                            .CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
                        await Task.Delay(1000);
                        log?.Invoke("✓ Gocator Protocol etkinleştirildi");

                        log?.Invoke("► Sistem başlatılıyor...");
                        if (system.RunningState() == GoSystem.State.Ready)
                        {
                            system.Start();
                            await Task.Delay(2000);
                        }
                        log?.Invoke("✓ Sistem başlatıldı");

                        const string dataSourceKey = "UniformSurface";
                        const string dataSourceComponent = "top";
                        string uniformSurfaceDataSourceId = $"scan:{ENGINE_ID}:{SCANNER_ID}:{dataSourceComponent}{dataSourceKey}0";

                        bool outputAdded = false;
                        try
                        {
                            response = system.Client().Read(GOCATOR_OUTPUT_PATH).GetResponse().Payload;
                            JArray map = (JArray)response.GetValue("map")!;

                            for (int i = 0; i < map.Count; i++)
                            {
                                if (map[i]?.ToString().Contains(dataSourceKey) == true)
                                {
                                    outputAdded = true;
                                    log?.Invoke("► Uniform Surface output zaten eklenmiş");
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            log?.Invoke($"⚠ Output kontrol hatası: {ex.Message}");
                        }

                        if (!outputAdded)
                        {
                            log?.Invoke("► Uniform Surface output ekleniyor...");
                            try
                            {
                                JObject payload = new JObject
                                {
                                    ["source"] = uniformSurfaceDataSourceId,
                                    ["outputId"] = 0,
                                    ["autoShift"] = true
                                };
                                system.Client().Call(GOCATOR_ADD_OUTPUT_PATH, payload)
                                    .CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
                                await Task.Delay(1000);
                                log?.Invoke("✓ Uniform Surface output eklendi");
                            }
                            catch (Exception ex)
                            {
                                log?.Invoke($"⚠ Output ekleme hatası: {ex.Message}");
                            }
                        }

                        log?.Invoke("► GDP Client bağlanıyor...");
                        string pointCloudJson = string.Empty;

                        using (GoGdpClient gdpClient = new GoGdpClient())
                        {
                            gdpClient.Connect(system.Address, system.GdpPort());
                            log?.Invoke("✓ GDP Client bağlandı");

                            log?.Invoke("► Surface verisi bekleniyor (timeout: 60 saniye)...");
                            gdpClient.ReceiveDataSync(RECEIVE_DATA_TIMEOUT_MSEC);

                            int datasetCount = (int)(gdpClient.DataSet?.Count ?? 0);
                            log?.Invoke($"✓ Veri alındı. DataSet sayısı: {datasetCount}");

                            if (datasetCount > 0 && gdpClient.DataSet != null)
                            {
                                log?.Invoke("📋 DataSet içeriği:");
                                for (int msgIndex = 0; msgIndex < datasetCount; msgIndex++)
                                {
                                    GoGdpMsg msg = gdpClient.DataSet.GdpMsgAt(msgIndex);
                                    log?.Invoke($"  [{msgIndex}] Mesaj Tipi: {msg.Type}");
                                    log?.Invoke($"      GDP ID: {msg.GdpId}");
                                    log?.Invoke($"      Data Source ID: {msg.DataSourceId}");

                                    if (msg.Type == MessageType.UniformSurface)
                                    {
                                        log?.Invoke($"      ✓✓✓ UniformSurface bulundu!");
                                        if (msg is GoGdpSurfaceUniform uniformMsg)
                                        {
                                            log?.Invoke($"          Width: {uniformMsg.Width}, Length: {uniformMsg.Length}");
                                            log?.Invoke($"          Offset: ({uniformMsg.Offset.X}, {uniformMsg.Offset.Y}, {uniformMsg.Offset.Z})");
                                        }
                                    }
                                    else if (msg.Type == MessageType.SurfacePointCloud)
                                    {
                                        log?.Invoke($"      ✓✓✓ SurfacePointCloud bulundu!");
                                        if (msg is GoGdpSurfacePointCloud surfaceMsg)
                                        {
                                            log?.Invoke($"          Width: {surfaceMsg.Width}, Length: {surfaceMsg.Length}");
                                            log?.Invoke($"          Offset: ({surfaceMsg.Offset.X}, {surfaceMsg.Offset.Y}, {surfaceMsg.Offset.Z})");
                                        }
                                    }
                                }
                            }

                            // UniformSurface işle
                            for (int msgIndex = 0; msgIndex < datasetCount; msgIndex++)
                            {
                                if (gdpClient.DataSet?.GdpMsgAt(msgIndex) is GoGdpSurfaceUniform uniformMsg)
                                {
                                    log?.Invoke($"✓ UniformSurface mesajı işleniyor...");
                                    pointCloudJson = ProcessUniformSurface(uniformMsg, log);
                                    if (!string.IsNullOrEmpty(pointCloudJson))
                                    {
                                        log?.Invoke($"✓ JSON başarıyla oluşturuldu");
                                        break;
                                    }
                                }
                            }

                            // SurfacePointCloud işle
                            if (string.IsNullOrEmpty(pointCloudJson))
                            {
                                for (int msgIndex = 0; msgIndex < datasetCount; msgIndex++)
                                {
                                    if (gdpClient.DataSet?.GdpMsgAt(msgIndex) is GoGdpSurfacePointCloud pointMsg)
                                    {
                                        log?.Invoke($"✓ SurfacePointCloud mesajı işleniyor...");
                                        pointCloudJson = ProcessSurfacePointCloud(pointMsg, log);
                                        if (!string.IsNullOrEmpty(pointCloudJson))
                                        {
                                            log?.Invoke($"✓ JSON başarıyla oluşturuldu");
                                            break;
                                        }
                                    }
                                }
                            }

                            gdpClient.Close();
                        }

                        log?.Invoke("► Sistem durduruluyor...");
                        system.Stop();
                        log?.Invoke("✓ Sistem durduruldu");
                        log?.Invoke("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                        if (!string.IsNullOrEmpty(pointCloudJson))
                        {
                            log?.Invoke("✓✓✓ BAŞARILI ✓✓✓");
                            return (OK_STATUS, pointCloudJson);
                        }
                        else
                        {
                            log?.Invoke("✗ Surface mesajı bulunamadı!");
                            return (ERROR_STATUS, string.Empty);
                        }
                    }
                    catch (Exception ex)
                    {
                        log?.Invoke($"✗ HATA: {ex.Message}");
                        log?.Invoke($"  Stack: {ex.StackTrace}");
                        return (ERROR_STATUS, string.Empty);
                    }
                }
            });
        }

        private static string ProcessUniformSurface(GoGdpSurfaceUniform uniformSurfaceMsg, Action<string>? log)
        {
            try
            {
                uint validPointCount = 0;
                int length = (int)uniformSurfaceMsg.Length;
                int width = (int)uniformSurfaceMsg.Width;
                int intensityLength = (int)uniformSurfaceMsg.IntensityLength;
                int intensityWidth = (int)uniformSurfaceMsg.IntensityWidth;

                var pointCloudData = new PointCloudData
                {
                    metadata = new PointCloudMetadata
                    {
                        timestamp = DateTime.Now,
                        pointCount = length * width,
                        validPoints = 0,
                        offsetX = uniformSurfaceMsg.Offset.X,
                        offsetY = uniformSurfaceMsg.Offset.Y,
                        offsetZ = uniformSurfaceMsg.Offset.Z,
                        resolutionX = uniformSurfaceMsg.Resolution.X,
                        resolutionY = uniformSurfaceMsg.Resolution.Y,
                        resolutionZ = uniformSurfaceMsg.Resolution.Z,
                        width = (uint)width,
                        length = (uint)length
                    },
                    points = new List<Point3D>()
                };

                for (int j = 0; j < length; j++)
                {
                    for (int k = 0; k < width; k++)
                    {
                        short data = uniformSurfaceMsg.Ranges[j, k];

                        if (data != short.MinValue)
                        {
                            double x = uniformSurfaceMsg.Offset.X + uniformSurfaceMsg.Resolution.X * k;
                            double y = uniformSurfaceMsg.Offset.Y + uniformSurfaceMsg.Resolution.Y * j;
                            double z = uniformSurfaceMsg.Offset.Z + uniformSurfaceMsg.Resolution.Z * data;

                            byte intensity = 0;
                            if (uniformSurfaceMsg.Intensities != null && j < intensityLength && k < intensityWidth)
                            {
                                intensity = uniformSurfaceMsg.Intensities[j, k];
                            }

                            pointCloudData.points.Add(new Point3D { x = x, y = y, z = z, intensity = intensity });
                            validPointCount++;
                        }
                    }
                }

                pointCloudData.metadata.validPoints = (int)validPointCount;
                log?.Invoke($"✓ UniformSurface işlendi: {validPointCount} geçerli nokta");
                log?.Invoke($"Surface data length: {length}");
                log?.Invoke($"Surface data width: {width}");
                log?.Invoke($"✓ Geçerli noktalar: {validPointCount}/{length * width}");

                log?.Invoke($"📊 JSON oluşturuluyor... ({pointCloudData.points.Count} nokta)");
                string jsonResult = JsonConvert.SerializeObject(pointCloudData, Formatting.Indented);
                log?.Invoke($"✓ JSON oluşturuldu: {jsonResult.Length} karakter");

                return jsonResult;
            }
            catch (Exception ex)
            {
                log?.Invoke($"✗ UniformSurface işleme hatası: {ex.Message}");
                return string.Empty;
            }
        }

        private static string ProcessSurfacePointCloud(GoGdpSurfacePointCloud pointCloudMsg, Action<string>? log)
        {
            try
            {
                uint validPointCount = 0;
                int length = (int)pointCloudMsg.Length;
                int width = (int)pointCloudMsg.Width;
                byte[,]? intensityArray = pointCloudMsg.Intensities;
                int intensityLength = (int)pointCloudMsg.IntensityLength;
                int intensityWidth = (int)pointCloudMsg.IntensityWidth;

                SurfacePointLocal[,] surfaceBuffer = new SurfacePointLocal[length, width];

                for (int j = 0; j < length; j++)
                {
                    for (int k = 0; k < width; k++)
                    {
                        GoPoint3d16s point = pointCloudMsg.Ranges[j, k];

                        surfaceBuffer[j, k].X = pointCloudMsg.Offset.X + pointCloudMsg.Resolution.X * point.X;
                        surfaceBuffer[j, k].Y = pointCloudMsg.Offset.Y + pointCloudMsg.Resolution.Y * point.Y;

                        if (point.Z != short.MinValue)
                        {
                            surfaceBuffer[j, k].Z = pointCloudMsg.Offset.Z + pointCloudMsg.Resolution.Z * point.Z;
                            validPointCount++;
                        }
                        else
                        {
                            surfaceBuffer[j, k].Z = double.MinValue;
                        }
                    }
                }

                if (intensityArray != null)
                {
                    int maxJ = Math.Min(length, intensityLength);
                    int maxK = Math.Min(width, intensityWidth);

                    for (int j = 0; j < maxJ; j++)
                    {
                        for (int k = 0; k < maxK; k++)
                        {
                            surfaceBuffer[j, k].Intensity = intensityArray[j, k];
                        }
                    }
                }

                log?.Invoke($"Surface data length: {length}");
                log?.Invoke($"Surface data width: {width}");
                log?.Invoke($"✓ Geçerli noktalar: {validPointCount}/{length * width}");

                var pointCloudData = new PointCloudData
                {
                    metadata = new PointCloudMetadata
                    {
                        timestamp = DateTime.Now,
                        pointCount = length * width,
                        validPoints = (int)validPointCount,
                        offsetX = pointCloudMsg.Offset.X,
                        offsetY = pointCloudMsg.Offset.Y,
                        offsetZ = pointCloudMsg.Offset.Z,
                        resolutionX = pointCloudMsg.Resolution.X,
                        resolutionY = pointCloudMsg.Resolution.Y,
                        resolutionZ = pointCloudMsg.Resolution.Z,
                        width = (uint)width,
                        length = (uint)length
                    },
                    points = new List<Point3D>()
                };

                for (int j = 0; j < length; j++)
                {
                    for (int k = 0; k < width; k++)
                    {
                        if (surfaceBuffer[j, k].Z != double.MinValue)
                        {
                            pointCloudData.points.Add(new Point3D
                            {
                                x = surfaceBuffer[j, k].X,
                                y = surfaceBuffer[j, k].Y,
                                z = surfaceBuffer[j, k].Z,
                                intensity = surfaceBuffer[j, k].Intensity
                            });
                        }
                    }
                }

                log?.Invoke($"📊 JSON oluşturuluyor... ({pointCloudData.points.Count} nokta)");
                string jsonResult = JsonConvert.SerializeObject(pointCloudData, Formatting.Indented);
                log?.Invoke($"✓ JSON oluşturuldu: {jsonResult.Length} karakter");

                return jsonResult;
            }
            catch (Exception ex)
            {
                log?.Invoke($"✗ Point Cloud işleme hatası: {ex.Message}");
                return string.Empty;
            }
        }

        public class PointCloudData
        {
            [JsonProperty("metadata")]
            public PointCloudMetadata metadata { get; set; } = new();

            [JsonProperty("points")]
            public List<Point3D> points { get; set; } = new();
        }

        public class PointCloudMetadata
        {
            [JsonProperty("timestamp")] public DateTime timestamp { get; set; }
            [JsonProperty("pointCount")] public int pointCount { get; set; }
            [JsonProperty("validPoints")] public int validPoints { get; set; }
            [JsonProperty("offsetX")] public double offsetX { get; set; }
            [JsonProperty("offsetY")] public double offsetY { get; set; }
            [JsonProperty("offsetZ")] public double offsetZ { get; set; }
            [JsonProperty("resolutionX")] public double resolutionX { get; set; }
            [JsonProperty("resolutionY")] public double resolutionY { get; set; }
            [JsonProperty("resolutionZ")] public double resolutionZ { get; set; }
            [JsonProperty("width")] public uint width { get; set; }
            [JsonProperty("length")] public uint length { get; set; }
        }

        public class Point3D
        {
            [JsonProperty("x")] public double x { get; set; }
            [JsonProperty("y")] public double y { get; set; }
            [JsonProperty("z")] public double z { get; set; }
            [JsonProperty("intensity")] public byte intensity { get; set; }
        }

        private struct SurfacePointLocal
        {
            public double X;
            public double Y;
            public double Z;
            public byte Intensity;
        }
    }

    #endregion
}