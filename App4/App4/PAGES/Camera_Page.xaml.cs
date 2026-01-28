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
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Streams;
using static GoPxLSdkSamplesCommon.Utilities;
using System.Collections.ObjectModel;
using Windows.UI;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Dispatching;
using System.IO.Compression; // Zip dosyası yapmak için şart
using Windows.Storage.Pickers; // Kayıt penceresi için

namespace App4.PAGES
{
    public class PlcTransferItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public int Index { get; set; }

        // --- TAG SEÇİMİ (Zaten Vardı) ---
        private string _selectedTag;
        public string SelectedTag
        {
            get => _selectedTag;
            set { if (_selectedTag != value) { _selectedTag = value; OnPropertyChanged(); } }
        }

        // --- DEĞER (BUNU GÜNCELLEMEMİZ GEREKİYORDU) ---
        private string _value;
        public string Value
        {
            get => _value;
            set { if (_value != value) { _value = value; OnPropertyChanged(); } } // <-- ARTIK CANLI
        }

        // --- DURUM METNİ (SENT, WAIT) ---
        private string _status;
        public string Status
        {
            get => _status;
            set { if (_status != value) { _status = value; OnPropertyChanged(); } }
        }

        // --- DURUM RENGİ ---
        private SolidColorBrush _statusColor;
        [JsonIgnore]
        public SolidColorBrush StatusColor
        {
            get => _statusColor;
            set { if (_statusColor != value) { _statusColor = value; OnPropertyChanged(); } }
        }

        // --- ARKA PLAN RENGİ ---
        [JsonIgnore]
        public SolidColorBrush BackgroundColor { get; set; }
    }

    public sealed partial class Camera_Page : Page
    {
        public ObservableCollection<string> PlcOutputTags { get; set; } = new();
        public ObservableCollection<PlcTransferItem> PlcTransferRows { get; set; } = new();

        private List<string> _logHistory = new();
        private bool _isWebViewInitialized = false;
        private readonly string _transferRowsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "App4", "Camera_PlcTransfer.json");

        // Cached brushes for better performance
        private static readonly SolidColorBrush BrushOrange = new(Microsoft.UI.Colors.Orange);
        private static readonly SolidColorBrush BrushGreen = new(Microsoft.UI.Colors.LimeGreen);
        private static readonly SolidColorBrush BrushRed = new(Microsoft.UI.Colors.Red);
        private static readonly SolidColorBrush BrushIndianRed = new(Microsoft.UI.Colors.IndianRed);

        public Camera_Page()
        {
            this.InitializeComponent();
            this.DataContext = this;
            this.Loaded += Camera_Page_Loaded;
        }

        private async void BtnGetMeasurement_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;

            AddLog("► Ölçüm alma isteği gönderildi...");

            // 1. Gocator'dan veriyi çek ve GlobalData'ya kaydet
            int result = await ReceiveMeasurementLogic.ReceiveAndProcessMeasurements(AddLog, this.DispatcherQueue);

            // 2. Eğer işlem başarılıysa, verileri sağdaki tabloya taşı
            if (result == 1)
            {
                TransferMeasurementsToPlcRows(); // <-- YENİ EKLENEN SATIR
            }

            if (btn != null) btn.IsEnabled = true;
        }


        private void LoadTransferRows()
        {
            try
            {
                if (File.Exists(_transferRowsFilePath))
                {
                    var json = File.ReadAllText(_transferRowsFilePath);
                    var items = JsonConvert.DeserializeObject<List<PlcTransferItem>>(json);
                    
                    if (items != null && items.Count > 0)
                    {
                        PlcTransferRows.Clear();
                        foreach (var item in items)
                        {
                            // Restore brushes based on status/index
                            if (item.Status == "SENT") item.StatusColor = BrushGreen;
                            else if (item.Status == "WAIT") item.StatusColor = BrushOrange;
                            else item.StatusColor = BrushRed;

                            item.BackgroundColor = (item.Index % 2 == 1) 
                                ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 15, 15)) 
                                : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 20, 20, 20));

                            PlcTransferRows.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog("Tablo yüklenemedi: " + ex.Message);
            }
        }

        private void SaveTransferRows()
        {
            try
            {
                var directory = Path.GetDirectoryName(_transferRowsFilePath);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                var json = JsonConvert.SerializeObject(PlcTransferRows, Formatting.Indented);
                File.WriteAllText(_transferRowsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Save Error: " + ex.Message);
            }
        }

        private void PlcTransferItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlcTransferItem.SelectedTag))
            {
                SaveTransferRows();
            }
        }

        private void PlcTransferRows_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (PlcTransferItem item in e.NewItems)
                    item.PropertyChanged += PlcTransferItem_PropertyChanged;
            }

            if (e.OldItems != null)
            {
                foreach (PlcTransferItem item in e.OldItems)
                    item.PropertyChanged -= PlcTransferItem_PropertyChanged;
            }
            
            SaveTransferRows();
        }

        private void LoadPlcTags()
        {
            try
            {
                PlcOutputTags.Clear();

                // ---------------------------------------------------------
                // 1. KAYNAK: PLC SAYFASINDA OLUŞTURULAN TAGLER (PlcService)
                // ---------------------------------------------------------
                // Kullanıcının PLC_Page üzerinden elle eklediği outputlar burada tutulur.
                foreach (var v in App4.Utilities.PlcService.Instance.OutputVariables)
                {
                    if (!string.IsNullOrEmpty(v.Name) && !PlcOutputTags.Contains(v.Name))
                    {
                        PlcOutputTags.Add(v.Name);
                    }
                }

                // ---------------------------------------------------------
                // 2. KAYNAK: GLOBAL DATA (OTOMATİK SAYFASI & GENEL TANIMLAR)
                // ---------------------------------------------------------
                // Eğer GlobalData'daki (Otomatik sayfası) tagleri de görmek istiyorsanız bu kısım kalmalı.
                // İstemiyorsanız 2. Kaynak kısmını silebilirsiniz.

                // A. Genel Outputlar
                foreach (var v in App4.Utilities.GlobalData.GeneralOutputVars)
                {
                    if (!string.IsNullOrEmpty(v.Name) && !PlcOutputTags.Contains(v.Name))
                        PlcOutputTags.Add(v.Name);
                }

                // B. İstasyon Outputları
                var allStationOutputs = new[]
                {
            App4.Utilities.GlobalData.Station1Outputs,
            App4.Utilities.GlobalData.Station2Outputs,
            App4.Utilities.GlobalData.Station3Outputs,
            App4.Utilities.GlobalData.Station4Outputs
        };

                foreach (var stationList in allStationOutputs)
                {
                    foreach (var v in stationList)
                    {
                        if (!string.IsNullOrEmpty(v.Name) && !PlcOutputTags.Contains(v.Name))
                            PlcOutputTags.Add(v.Name);
                    }
                }

                AddLog($"✓ PLC Tag listesi güncellendi. Toplam: {PlcOutputTags.Count} tag.");
            }
            catch (Exception ex)
            {
                AddLog("PLC Tagleri yüklenemedi: " + ex.Message);
            }
        }


        private void TransferMeasurementsToPlcRows()
        {
            try
            {
                var measurements = App4.Utilities.GlobalData.LastMeasurements;

                if (measurements.Count == 0) return;

                // Ölçüm sayısı kadar veya Tablo satır sayısı kadar (hangisi küçükse) döngü kur
                int count = Math.Min(measurements.Count, PlcTransferRows.Count);

                for (int i = 0; i < count; i++)
                {
                    // Soldaki ölçüm değerini al
                    var measuredVal = measurements[i].Value.ToString();

                    // Sağdaki satıra yaz
                    PlcTransferRows[i].Value = measuredVal;

                    // Görsel durumu güncelle (Yeni veri geldiği için rengi turuncu/bekliyor yapalım)
                    PlcTransferRows[i].Status = "WAIT";
                    PlcTransferRows[i].StatusColor = BrushOrange;

                    // UI tetiklemesi için (ItemsControl bazen property değişimini kaçırabilir)
                    // Bu yöntem PropertyChanged olayını manuel tetikler
                    var temp = PlcTransferRows[i].SelectedTag;
                    PlcTransferRows[i].SelectedTag = null;
                    PlcTransferRows[i].SelectedTag = temp;
                }

                AddLog($"► {count} adet veri PLC tablosuna aktarıldı.");
            }
            catch (Exception ex)
            {
                AddLog($"Veri aktarım hatası: {ex.Message}");
            }
        }

        private void InitializeDefaultPlcRows()
        {
            if (PlcTransferRows.Count > 0) return;

            PlcTransferRows.Add(new PlcTransferItem 
            { 
                Index = 1, 
                SelectedTag = PlcOutputTags.FirstOrDefault(), 
                Value = "12845", 
                Status = "SENT", 
                StatusColor = BrushGreen, 
                BackgroundColor = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 15, 15)) 
            });
            PlcTransferRows.Add(new PlcTransferItem 
            { 
                Index = 2, 
                SelectedTag = PlcOutputTags.Skip(1).FirstOrDefault(), 
                Value = "4520", 
                Status = "SENT", 
                StatusColor = BrushGreen, 
                BackgroundColor = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 20, 20, 20)) 
            });
             PlcTransferRows.Add(new PlcTransferItem 
            { 
                Index = 3, 
                SelectedTag = PlcOutputTags.Skip(2).FirstOrDefault(), 
                Value = "1205", 
                Status = "SENT", 
                StatusColor = BrushGreen, 
                BackgroundColor = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 15, 15)) 
            });
             PlcTransferRows.Add(new PlcTransferItem 
            { 
                Index = 4, 
                SelectedTag = PlcOutputTags.Skip(3).FirstOrDefault(), 
                Value = "0", 
                Status = "WAIT", 
                StatusColor = BrushOrange, 
                BackgroundColor = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 20, 20, 20)) 
            });
        }

        // --- YENİ SATIR EKLEME ---
        private void BtnAddPlcRow_Click(object sender, RoutedEventArgs e)
        {
            var index = PlcTransferRows.Count + 1;

            // Arka plan rengini sırayla koyu/açık yap
            var color = (index % 2 == 1)
                ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 15, 15))
                : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 20, 20, 20));

            PlcTransferRows.Add(new PlcTransferItem
            {
                Index = index,
                SelectedTag = null, // Başlangıçta boş olsun, kullanıcı seçsin
                Value = "0",
                Status = "WAIT",
                StatusColor = BrushOrange,
                BackgroundColor = color
            });

            // Listeyi kaydet
            SaveTransferRows();
        }

        // --- SİSTEM YEDEKLEME FONKSİYONU ---
        // --- SİSTEM YEDEKLEME FONKSİYONU (DİREKT MASAÜSTÜNE) ---
        private async void BtnBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Kaynak ve Hedef Yolları Belirle
                string sourcePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "App4");
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string zipFileName = $"App4_Yedek_{DateTime.Now:yyyyMMdd_HHmm}.zip";
                string destinationZip = Path.Combine(desktopPath, zipFileName);

                // 2. Kaynak Kontrolü
                if (!Directory.Exists(sourcePath))
                {
                    AddLog("⚠ Yedeklenecek veri klasörü bulunamadı (Henüz veri oluşmamış olabilir).");
                    return;
                }

                // Butonu kilitle (Çift tıklamayı önle)
                if (sender is Button btn) btn.IsEnabled = false;
                AddLog("⏳ Masaüstüne yedek alınıyor, lütfen bekleyin...");

                // 3. Arka Planda Sıkıştırma İşlemi
                await Task.Run(() =>
                {
                    try
                    {
                        // Eğer aynı isimde dosya varsa sil
                        if (File.Exists(destinationZip)) File.Delete(destinationZip);

                        // Klasörü direkt ziple
                        System.IO.Compression.ZipFile.CreateFromDirectory(sourcePath, destinationZip);
                    }
                    catch (Exception zipEx)
                    {
                        // Hata olursa (örn: dosya kullanımdayken) fırlat
                        throw new Exception("Sıkıştırma hatası: " + zipEx.Message);
                    }
                });

                AddLog($"✅ YEDEKLEME BAŞARILI!");
                AddLog($"📁 Konum: Masaüstü\\{zipFileName}");

                // Butonu aç
                if (sender is Button btnRestore) btnRestore.IsEnabled = true;
            }
            catch (Exception ex)
            {
                AddLog($"❌ Yedekleme Hatası: {ex.Message}");
                if (sender is Button btn) btn.IsEnabled = true;
            }
        }

        // GOCATOR YEDEKLEME MANTIĞI
        public class GocatorBackupLogic
        {
            private const string SYSTEM_BACKUP_PATH = "/system/commands/archive";
            private const string SENSOR_IP = "192.168.251.30";
            private const int CONTROL_PORT = 3600;
            private const int RECEIVE_DATA_TIMEOUT_MSEC = 60000; // Büyük dosyalar için süre

            public static async Task<string> PerformBackup(Action<string> log)
            {
                return await Task.Run(() =>
                {
                    try
                    {
                        IPAddress ipAddress = IPAddress.Parse(SENSOR_IP);
                        using (GoSystem system = new GoSystem(ipAddress, CONTROL_PORT))
                        {
                            log("Sensöre bağlanılıyor (Yedekleme)...");
                            system.Connect();

                            // Yedek içeriğini belirle (Jobs, Workspaces, Global vb.)
                            JObject payload = new JObject
                            {
                                ["contents"] = new JArray {
                            "global",
                            "allWorkspaces",
                            "allJobs",
                            "replay",
                            "liveJob"
                        }
                            };

                            log("Yedek verisi sensörden çekiliyor (Bu işlem sürebilir)...");

                            // API Çağrısı
                            JObject response = system.Client().Call(SYSTEM_BACKUP_PATH, payload).GetResponse(RECEIVE_DATA_TIMEOUT_MSEC).Payload;

                            // Byte verisini al
                            byte[] data = response["data"].ToObject<byte[]>();

                            // Dosyayı Masaüstüne Kaydet
                            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                            string fileName = $"Gocator_Backup_{DateTime.Now:yyyyMMdd_HHmm}.gpbak";
                            string fullPath = Path.Combine(desktopPath, fileName);

                            File.WriteAllBytes(fullPath, data);

                            system.Disconnect();
                            return fullPath; // Başarılı ise dosya yolunu döndür
                        }
                    }
                    catch (Exception ex)
                    {
                        log($"Gocator Yedek Hatası: {ex.Message}");
                        return null;
                    }
                });
            }
        }


        // --- GOCATOR YEDEKLEME BUTONU ---
        private async void BtnBackupGocator_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;

            AddLog("► Gocator yedekleme işlemi başlatıldı...");

            // Mantık sınıfını çağır
            string resultPath = await GocatorBackupLogic.PerformBackup(AddLog);

            if (!string.IsNullOrEmpty(resultPath))
            {
                AddLog("✅ GOCATOR YEDEĞİ ALINDI!");
                AddLog($"📁 Dosya: {Path.GetFileName(resultPath)}");
                AddLog($"📍 Konum: Masaüstü");
            }
            else
            {
                AddLog("❌ Gocator yedeği alınamadı.");
            }

            if (btn != null) btn.IsEnabled = true;
        }



        // --- SATIR SİLME (YENİ EKLENECEK) ---
        private void BtnDeletePlcRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is PlcTransferItem item)
            {
                PlcTransferRows.Remove(item);

                // Sıra numaralarını (Index) yeniden düzenle
                for (int i = 0; i < PlcTransferRows.Count; i++)
                {
                    PlcTransferRows[i].Index = i + 1;
                    // Renkleri de düzelt
                    PlcTransferRows[i].BackgroundColor = ((i + 1) % 2 == 1)
                        ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 15, 15))
                        : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 20, 20, 20));
                }

                SaveTransferRows();
            }
        }

        #region ═══ WebView2 Initialization (Modern Approach) ═══

        private async void Camera_Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPlcTags();
            LoadTransferRows(); // Load saved rows first
            InitializeDefaultPlcRows(); // Only if empty

            // Hook change events
            PlcTransferRows.CollectionChanged -= PlcTransferRows_CollectionChanged; // Prevent double hook
            PlcTransferRows.CollectionChanged += PlcTransferRows_CollectionChanged;
            
            foreach (var item in PlcTransferRows)
            {
                item.PropertyChanged -= PlcTransferItem_PropertyChanged;
                item.PropertyChanged += PlcTransferItem_PropertyChanged;
            }

            if (_isWebViewInitialized) return;

            try
            {
                AddLog("► WebView2 Ortamı Hazırlanıyor...");

                // 1. Temp klasöründe UserData oluştur (Önbellek/Cache için)
                string userDataFolder = Path.Combine(Path.GetTempPath(), "App4_WebView2_Cache");

                // Environment oluştur
                var env = await CoreWebView2Environment.CreateWithOptionsAsync(null, userDataFolder, null);

                // 2. CoreWebView2'yi bu environment ile başlat
                await PointCloudWebView.EnsureCoreWebView2Async(env);

                // 3. Assets Klasörünü Belirle ve Eşle
                string assetsPath;
                try
                {
                    // Paketli uygulama (MSIX) için yol
                    assetsPath = Path.Combine(Package.Current.InstalledLocation.Path, "Assets");
                }
                catch
                {
                    // Paketsiz çalışıyorsa (Debug/Release folder)
                    assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
                }

                if (!Directory.Exists(assetsPath))
                {
                    AddLog($"⚠ Assets klasörü bulunamadı: {assetsPath}");
                }
                else
                {
                    AddLog($"► Mapping Yolu: {assetsPath}");
                }

                // Sanal Host: https://appassets/ -> Yerel Assets klasörü
                PointCloudWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "appassets",
                    assetsPath,
                    CoreWebView2HostResourceAccessKind.Allow);

                // Ayarlar
                PointCloudWebView.CoreWebView2.Settings.IsScriptEnabled = true;
                PointCloudWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;

                // Eventleri bağla
                PointCloudWebView.NavigationCompleted += PointCloudWebView_NavigationCompleted;
                PointCloudWebView.WebMessageReceived += PointCloudWebView_WebMessageReceived;

                // 4. HTML'i Yükle
                AddLog("► HTML Yükleniyor...");
                PointCloudWebView.Source = new Uri("https://appassets/PointCloud3DViewer.html");

                _isWebViewInitialized = true;
            }
            catch (Exception ex)
            {
                AddLog($"✗ WebView2 Başlatma Hatası: {ex.Message}");
                // UI'daki yükleniyor ekranını kaldırıp hata göster
                ShowCloudStatus("HATA: WebView2 Başlatılamadı", Microsoft.UI.Colors.Red, false);
            }
        }

        private void PointCloudWebView_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            if (args.IsSuccess)
            {
                AddLog("✓ HTML Sayfası Başarıyla Yüklendi");
                // Yüklenme dönen simgeyi durdur
                ShowCloudStatus("Hazır", Microsoft.UI.Colors.LimeGreen, false);
            }
            else
            {
                AddLog($"✗ HTML Navigasyon Hatası: {args.WebErrorStatus}");
                ShowCloudStatus("Sayfa Yüklenemedi", Microsoft.UI.Colors.Red, false);
            }
        }

        private void PointCloudWebView_WebMessageReceived(WebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            try
            {
                string msg = args.TryGetWebMessageAsString();
                if (!string.IsNullOrEmpty(msg))
                {
                    Debug.WriteLine($"[WEBVIEW LOG] {msg}");
                    // HTML'den gelen "Error:" mesajlarını da loglayabiliriz
                    if (msg.StartsWith("Error")) AddLog($"⚠ WebView JS Hatası: {msg}");
                }
            }
            catch { }
        }

        #endregion

        #region ═══ Surface / Point Cloud Process ═══

        private async void SurfaceButton_Click(object sender, RoutedEventArgs e)
        {
            // WebView hazır mı kontrolü
            if (!_isWebViewInitialized || PointCloudWebView.CoreWebView2 == null)
            {
                AddLog("✗ WebView henüz hazır değil, işlem yapılamaz.");
                ShowCloudStatus("WebView Hazır Değil", Microsoft.UI.Colors.Red, false);
                return;
            }

            try
            {
                // UI Durum Güncelleme
                ShowCloudStatus("Veriler Alınıyor...", Microsoft.UI.Colors.Orange, true);
                SurfaceButton.IsEnabled = false;

                AddLog("════════════════════════════════════════════");
                AddLog("► POINT CLOUD İŞLEMİ BAŞLATILIYOR");

                // Asenkron olarak veriyi çek
                var (result, pointCloudJson) = await ReceiveSurfaceSample.ReceiveSurfacePointCloudNet(AddLog);

                if (result != OK_STATUS)
                {
                    ShowCloudStatus("Sensör Hatası", Microsoft.UI.Colors.Red, false);
                    AddLog($"✗ Gocator Hatası (Kod: {result})");
                    return;
                }

                if (string.IsNullOrEmpty(pointCloudJson))
                {
                    ShowCloudStatus("Veri Boş", Microsoft.UI.Colors.Orange, false);
                    AddLog("✗ JSON Verisi oluşturulamadı veya boş.");
                    return;
                }

                AddLog($"✓ {pointCloudJson.Length} byte JSON alındı. WebView'a aktarılıyor...");
                AddLog("💡 Bakış Açısı: Z ekseni → Kırmızı (yakın) ön plana, Mavi (uzak) arka planda");
                ShowCloudStatus("Çizim Yapılıyor...", Microsoft.UI.Colors.LightBlue, true);

                // JSON verisini doğrudan JavaScript'e gönder
                string jsCode = $@"
                    (function() {{
                        try {{
                            if (typeof window.loadPointCloud === 'function') {{
                                const data = {pointCloudJson};
                                console.log('Loading ' + (data.points ? data.points.length : 0) + ' points...');
                                window.loadPointCloud(data);
                            }} else {{
                                console.error('loadPointCloud function not found');
                                window.chrome.webview.postMessage('Error: loadPointCloud fonksiyonu bulunamadı');
                            }}
                        }} catch(err) {{
                            console.error('Error loading point cloud:', err);
                            window.chrome.webview.postMessage('Error: ' + err.message);
                        }}
                    }})();
                ";

                AddLog("► JavaScript execute ediliyor...");
                var jsResult = await PointCloudWebView.ExecuteScriptAsync(jsCode);
                AddLog($"✓ JavaScript sonucu: {jsResult}");

                // İşlem tamamlandı
                ShowCloudStatus("Point Cloud Hazır!", Microsoft.UI.Colors.LimeGreen, false);
                AddLog("✓✓✓ İŞLEM TAMAMLANDI ✓✓✓");
                AddLog("════════════════════════════════════════════");
            }
            catch (Exception ex)
            {
                ShowCloudStatus("Uygulama Hatası", Microsoft.UI.Colors.Red, false);
                AddLog($"✗ Kritik Hata: {ex.Message}");
            }
            finally
            {
                SurfaceButton.IsEnabled = true;
                // Status panelini kapat
              
            }
        }

        #endregion

        #region ═══ Photo Capture Process ═══

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusLabel.Text = "Çekim Yapılıyor...";
                StatusLabel.Foreground = BrushOrange;
                PhotoButton.IsEnabled = false;
                LoadingRing.IsActive = true;

                AddLog("► Kamera Çekimi Başlatıldı...");
                int result = await ReceiveImageSample.ReceiveImageNet(AddLog);

                if (result == OK_STATUS)
                {
                    StatusLabel.Text = "BAŞARILI";
                    StatusLabel.Foreground = BrushGreen;
                    PhotoStatus.Text = "OK";
                    PhotoStatus.Foreground = BrushGreen;
                    AddLog("✓ Fotoğraf başarıyla çekildi");
                    await ShowCapturedImage();
                }
                else
                {
                    StatusLabel.Text = $"HATA: {result}";
                    StatusLabel.Foreground = BrushRed;
                    PhotoStatus.Text = "HATA";
                    PhotoStatus.Foreground = BrushIndianRed;
                    AddLog("✗ Çekim başarısız oldu");
                }
            }
            catch (Exception ex)
            {
                AddLog($"✗ Hata: {ex.Message}");
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
                    AddLog($"► Dosya bulundu: {rawFile.Name}");
                    await ConvertRawToBmpAndDisplay(rawFile, SensorDisplay);
                }
                else
                {
                    AddLog("⚠ Görüntülenecek RAW dosyası bulunamadı.");
                }
            }
            catch (Exception ex) { AddLog($"✗ Görüntüleme Hatası: {ex.Message}"); }
        }

        private async Task ConvertRawToBmpAndDisplay(FileInfo rawFile, Image targetImage)
        {
            try
            {
                string namePart = rawFile.Name.Replace("Gocator_", "").Replace(".raw", "");
                string[] parts = namePart.Split('_');

                if (parts.Length >= 2 && int.TryParse(parts[0], out int width) && int.TryParse(parts[1], out int height))
                {
                    byte[] rawBytes = await Task.Run(() => File.ReadAllBytes(rawFile.FullName));

                    if (rawBytes.Length != width * height)
                    {
                        AddLog("⚠ Dosya boyutu beklenen çözünürlükle eşleşmiyor.");
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
                        bgraBytes[pos + 3] = 255; // Alpha
                    }

                    string bmpPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        $"Gocator_{width}_{height}.bmp");

                    await SaveBitmapFile(bmpPath, bgraBytes, width, height);
                    await DisplayBitmapFile(new FileInfo(bmpPath), targetImage);
                }
            }
            catch (Exception ex) { AddLog($"✗ Dönüştürme Hatası: {ex.Message}"); }
        }

        private async Task SaveBitmapFile(string filePath, byte[] bgraData, int width, int height)
        {
            using (FileStream fs = new(filePath, FileMode.Create, FileAccess.Write))
            {
                byte[] fileHeader = new byte[14];
                fileHeader[0] = 66; // B
                fileHeader[1] = 77; // M

                int fileSize = 54 + (width * height * 3);
                BitConverter.GetBytes(fileSize).CopyTo(fileHeader, 2);
                BitConverter.GetBytes(54).CopyTo(fileHeader, 10);

                byte[] dibHeader = new byte[40];
                BitConverter.GetBytes(40).CopyTo(dibHeader, 0);
                BitConverter.GetBytes(width).CopyTo(dibHeader, 4);
                BitConverter.GetBytes(height).CopyTo(dibHeader, 8);
                dibHeader[12] = 1; // Planes
                dibHeader[14] = 24; // Bits per pixel

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

        private async Task DisplayBitmapFile(FileInfo bmpFile, Image targetImage)
        {
            using (IRandomAccessStream stream = await (await StorageFile.GetFileFromPathAsync(bmpFile.FullName))
                .OpenAsync(FileAccessMode.Read))
            {
                var bitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                await bitmap.SetSourceAsync(stream);
                targetImage.Source = bitmap;
                
                // Görüntü yüklendikten sonra "Görüntü bekleniyor" yazısını gizle
                NoDataText.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region ═══ Helper Methods (Logs, Status) ═══

        private void AddLog(string message)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                string logEntry = $"[{timestamp}] {message}";
                _logHistory.Add(logEntry);

                if (_logHistory.Count > 200) _logHistory.RemoveAt(0);

                this.DispatcherQueue.TryEnqueue(() =>
                {
                    if (LogOutput != null)
                        LogOutput.Text = string.Join("\n", _logHistory);
                });

                Debug.WriteLine(logEntry);
            }
            catch { }
        }

        private void ShowCloudStatus(string message, Windows.UI.Color color, bool isLoading)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (Cloud3DStatus != null)
                {
                    Cloud3DStatus.Text = message;
                    Cloud3DStatus.Foreground = new SolidColorBrush(color);
                }

               
            });
        }

        #endregion
    }

    #region ═══ GOCATOR CLASSES ═══

    public class ReceiveImageSample
    {
        private const string SCAN_MODE_PATH = "$.parameters.scanModeSettings.scanMode";
        private const int IMAGE_MODE = 0;
        private const string GOCATOR_CONTROL_PATH = "/controls/gocator";
        private const string GOCATOR_OUTPUT_PATH = GOCATOR_CONTROL_PATH + "/outputs";
        private const string GOCATOR_ADD_OUTPUT_PATH = GOCATOR_OUTPUT_PATH + "/commands/add";
        private const string REPLAY_PATH = "/replay/playback";
        private const int RECEIVE_DATA_TIMEOUT_MSEC = 60000;
        private const string SENSOR_IP = "192.168.251.30";
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
                        log?.Invoke("Sensöre bağlanılıyor...");
                        system.Connect();
                        if (VerifyConnection(system) == ERROR_STATUS) return ERROR_STATUS;

                        JObject response = system.Client().Read(REPLAY_PATH).GetResponse().Payload;
                        bool replayDataEnabled = (bool)response.GetValue("enabled")!;

                        if (!replayDataEnabled)
                        {
                            response = system.Client().Read(SCANNER_PATH).GetResponse().Payload;
                            if ((int)response.SelectToken(SCAN_MODE_PATH)! != IMAGE_MODE)
                            {
                                log?.Invoke("Mod değiştiriliyor: IMAGE");
                                JObject payload = new JObject { ["parameters"] = new JObject { ["scanModeSettings"] = new JObject { ["scanMode"] = IMAGE_MODE } } };
                                system.Client().Update(SCANNER_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
                                await Task.Delay(500);
                            }
                        }

                        system.Client().Update(GOCATOR_CONTROL_PATH, new JObject { ["enabled"] = true }).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
                        await Task.Delay(500);

                        if (system.RunningState() == GoSystem.State.Ready) system.Start();

                        // Output Add Check
                        string dataSourceKey = "Image";
                        string imageDataSourceId = $"scan:{ENGINE_ID}:{SCANNER_ID}:{SCAN_ENGINE_COMPONENT}{dataSourceKey}0";
                        bool outputExists = false;
                        try
                        {
                            var map = (JArray)system.Client().Read(GOCATOR_OUTPUT_PATH).GetResponse().Payload.GetValue("map")!;
                            outputExists = map.Any(m => m.ToString().Contains(dataSourceKey));
                        }
                        catch { }

                        if (!outputExists)
                        {
                            system.Client().Call(GOCATOR_ADD_OUTPUT_PATH, new JObject { ["source"] = imageDataSourceId, ["outputId"] = 0, ["autoShift"] = true }).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
                        }

                        using (GoGdpClient gdpClient = new GoGdpClient())
                        {
                            gdpClient.Connect(system.Address, system.GdpPort());
                            gdpClient.ReceiveDataSync(RECEIVE_DATA_TIMEOUT_MSEC);

                            if (gdpClient.DataSet != null)
                            {
                                for (int i = 0; i < gdpClient.DataSet.Count; i++)
                                {
                                    if (gdpClient.DataSet.GdpMsgAt(i) is GoGdpImage imageMsg)
                                    {
                                        int width = (int)imageMsg.Width;
                                        int height = (int)imageMsg.Height;

                                        byte[] flatData = new byte[width * height];
                                        System.Buffer.BlockCopy(imageMsg.Pixels, 0, flatData, 0, flatData.Length);

                                        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Gocator_{width}_{height}.raw");
                                        File.WriteAllBytes(path, flatData);
                                        log?.Invoke($"RAW Kaydedildi: {Path.GetFileName(path)}");
                                    }
                                }
                            }
                            gdpClient.Close();
                        }
                        system.Stop();
                        return OK_STATUS;
                    }
                    catch (Exception ex)
                    {
                        log?.Invoke($"Resim Alma Hatası: {ex.Message}");
                        return ERROR_STATUS;
                    }
                }
            });
        }
    }

    public class ReceiveSurfaceSample
    {
        private const string SCAN_MODE_PATH = "$.parameters.scanModeSettings.scanMode";
        private const int SURFACE_MODE = 3;
        private const string GOCATOR_CONTROL_PATH = "/controls/gocator";
        private const string GOCATOR_OUTPUT_PATH = GOCATOR_CONTROL_PATH + "/outputs";
        private const string GOCATOR_ADD_OUTPUT_PATH = GOCATOR_OUTPUT_PATH + "/commands/add";
        private const string REPLAY_PATH = "/replay/playback";
        private const int RECEIVE_DATA_TIMEOUT_MSEC = 60000;
        private const string SENSOR_IP = "192.168.251.30";
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
                        log?.Invoke("Sensöre bağlanılıyor...");
                        system.Connect();
                        if (VerifyConnection(system) == ERROR_STATUS)
                            return (ERROR_STATUS, "");

                        JObject response = system.Client().Read(REPLAY_PATH).GetResponse().Payload;
                        bool replayDataEnabled = (bool)response.GetValue("enabled")!;

                        if (!replayDataEnabled)
                        {
                            response = system.Client().Read(SCANNER_PATH).GetResponse().Payload;
                            if ((int)response.SelectToken(SCAN_MODE_PATH)! != SURFACE_MODE)
                            {
                                log?.Invoke("Mod değiştiriliyor: SURFACE");
                                JObject payload = new JObject { ["parameters"] = new JObject { ["scanModeSettings"] = new JObject { ["scanMode"] = SURFACE_MODE } } };
                                system.Client().Update(SCANNER_PATH, payload).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
                                
                            }
                        }

                        system.Client().Update(SCANNER_PATH, new JObject { ["parameters"] = new JObject { ["scanModeSettings"] = new JObject { ["intensityEnabled"] = true } } }).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);

                        system.Client().Update(GOCATOR_CONTROL_PATH, new JObject { ["enabled"] = true }).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
                        

                        if (system.RunningState() == GoSystem.State.Ready)
                        {
                            system.Start();
                           
                        }

                        string dataSourceKey = "UniformSurface";
                        string uniformSurfaceDataSourceId = $"scan:{ENGINE_ID}:{SCANNER_ID}:top{dataSourceKey}0";
                        bool outputExists = false;
                        try
                        {
                            var map = (JArray)system.Client().Read(GOCATOR_OUTPUT_PATH).GetResponse().Payload.GetValue("map")!;
                            outputExists = map.Any(m => m.ToString().Contains(dataSourceKey));
                        }
                        catch { }

                        if (!outputExists)
                        {
                            log?.Invoke("Uniform Output ekleniyor...");
                            system.Client().Call(GOCATOR_ADD_OUTPUT_PATH, new JObject { ["source"] = uniformSurfaceDataSourceId, ["outputId"] = 0, ["autoShift"] = true }).CheckResponse(REST_COMMAND_TIMEOUT_MSEC);
                        }

                        string resultJson = "";
                        using (GoGdpClient gdpClient = new GoGdpClient())
                        {
                            gdpClient.Connect(system.Address, system.GdpPort());
                            log?.Invoke("Veri bekleniyor...");
                            gdpClient.ReceiveDataSync(RECEIVE_DATA_TIMEOUT_MSEC);

                            int count = (int)(gdpClient.DataSet?.Count ?? 0);
                            log?.Invoke($"Veri paketi alındı. Mesaj sayısı: {count}");

                            if (count > 0 && gdpClient.DataSet != null)
                            {
                                for (int i = 0; i < count; i++)
                                {
                                    if (gdpClient.DataSet.GdpMsgAt(i) is GoGdpSurfaceUniform uMsg)
                                    {
                                        log?.Invoke("Uniform Surface işleniyor...");
                                        resultJson = ProcessUniformSurface(uMsg);
                                        if (!string.IsNullOrEmpty(resultJson)) break;
                                    }
                                    else if (gdpClient.DataSet.GdpMsgAt(i) is GoGdpSurfacePointCloud pMsg)
                                    {
                                        log?.Invoke("Point Cloud işleniyor...");
                                        resultJson = ProcessSurfacePointCloud(pMsg);
                                        if (!string.IsNullOrEmpty(resultJson)) break;
                                    }
                                }
                            }
                            gdpClient.Close();
                        }

                        system.Stop();
                        return string.IsNullOrEmpty(resultJson) ? (ERROR_STATUS, "") : (OK_STATUS, resultJson);
                    }
                    catch (Exception ex)
                    {
                        log?.Invoke($"Surface Alma Hatası: {ex.Message}");
                        return (ERROR_STATUS, "");
                    }
                }
            });
        }

        private static string ProcessUniformSurface(GoGdpSurfaceUniform msg)
        {
            var data = new PointCloudData
            {
                metadata = new PointCloudMetadata
                {
                    timestamp = DateTime.Now,
                    pointCount = (int)(msg.Width * msg.Length),
                    offsetX = msg.Offset.X,
                    offsetY = msg.Offset.Y,
                    offsetZ = msg.Offset.Z,
                    resolutionX = msg.Resolution.X,
                    resolutionY = msg.Resolution.Y,
                    resolutionZ = msg.Resolution.Z,
                    width = (uint)msg.Width,
                    length = (uint)msg.Length
                }
            };

            // Kapasiteyi önceden ayarla
            data.points = new List<Point3D>((int)(msg.Width * msg.Length));

            for (int r = 0; r < msg.Length; r++)
            {
                for (int c = 0; c < msg.Width; c++)
                {
                    short val = msg.Ranges[r, c];
                    if (val != short.MinValue)
                    {
                        data.points.Add(new Point3D
                        {
                            x = -(msg.Offset.X + msg.Resolution.X * c), // X eksenini ters çevir (aynalama düzeltme)
                            y = msg.Offset.Y + msg.Resolution.Y * r,
                            z = msg.Offset.Z + msg.Resolution.Z * val,
                            intensity = (msg.Intensities != null && r < msg.IntensityLength && c < msg.IntensityWidth) 
                                ? msg.Intensities[r, c] 
                                : (byte)0
                        });
                    }
                }
            }

            return JsonConvert.SerializeObject(data, new JsonSerializerSettings 
            { 
                NullValueHandling = NullValueHandling.Ignore 
            });
        }

        private static string ProcessSurfacePointCloud(GoGdpSurfacePointCloud msg)
        {
            var data = new PointCloudData
            {
                metadata = new PointCloudMetadata
                {
                    timestamp = DateTime.Now,
                    pointCount = (int)(msg.Width * msg.Length),
                    offsetX = msg.Offset.X,
                    offsetY = msg.Offset.Y,
                    offsetZ = msg.Offset.Z,
                    resolutionX = msg.Resolution.X,
                    resolutionY = msg.Resolution.Y,
                    resolutionZ = msg.Resolution.Z,
                    width = (uint)msg.Width,
                    length = (uint)msg.Length
                }
            };

            // Kapasiteyi önceden ayarla
            data.points = new List<Point3D>((int)(msg.Width * msg.Length));

            for (int r = 0; r < msg.Length; r++)
            {
                for (int c = 0; c < msg.Width; c++)
                {
                    var p = msg.Ranges[r, c];
                    if (p.Z != short.MinValue)
                    {
                        data.points.Add(new Point3D
                        {
                            x = -(msg.Offset.X + msg.Resolution.X * p.X), // X eksenini ters çevir (aynalama düzeltme)
                            y = msg.Offset.Y + msg.Resolution.Y * p.Y,
                            z = msg.Offset.Z + msg.Resolution.Z * p.Z,
                            intensity = (msg.Intensities != null && r < msg.IntensityLength && c < msg.IntensityWidth) 
                                ? msg.Intensities[r, c] 
                                : (byte)0
                        });
                    }
                }
            }

            return JsonConvert.SerializeObject(data, new JsonSerializerSettings 
            { 
                NullValueHandling = NullValueHandling.Ignore 
            });
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
            [JsonProperty("timestamp")]
            public DateTime timestamp { get; set; }
            
            [JsonProperty("pointCount")]
            public int pointCount { get; set; }
            
            [JsonProperty("offsetX")]
            public double offsetX { get; set; }
            
            [JsonProperty("offsetY")]
            public double offsetY { get; set; }
            
            [JsonProperty("offsetZ")]
            public double offsetZ { get; set; }
            
            [JsonProperty("resolutionX")]
            public double resolutionX { get; set; }
            
            [JsonProperty("resolutionY")]
            public double resolutionY { get; set; }
            
            [JsonProperty("resolutionZ")]
            public double resolutionZ { get; set; }
            
            [JsonProperty("width")]
            public uint width { get; set; }
            
            [JsonProperty("length")]
            public uint length { get; set; }
        }

        public class Point3D
        {
            [JsonProperty("x")]
            public double x { get; set; }
            
            [JsonProperty("y")]
            public double y { get; set; }
            
            [JsonProperty("z")]
            public double z { get; set; }
            
            [JsonProperty("intensity")]
            public byte intensity { get; set; }
        }
    }
    #endregion
    // BU SINIF Camera_Page.xaml.cs EN ALTINA Gelecek (Namespace içine)
    public class ReceiveMeasurementLogic
    {
        private const string GOCATOR_CONTROL_PATH = "/controls/gocator";
        private const int RECEIVE_DATA_TIMEOUT_MSEC = 5000;
        private const string SENSOR_IP = "192.168.251.30";
        private const int CONTROL_PORT = 3600;

        // YENİ PARAMETRE EKLENDİ: DispatcherQueue dispatcher
        public static async Task<int> ReceiveAndProcessMeasurements(Action<string> log, DispatcherQueue dispatcher)
        {
            return await Task.Run(() =>
            {
                try
                {
                    IPAddress ipAddress = IPAddress.Parse(SENSOR_IP);
                    using (GoSystem system = new GoSystem(ipAddress, CONTROL_PORT))
                    {
                        log("Sensöre bağlanılıyor (Ölçüm)...");
                        system.Connect();

                        // Gocator Protokolünü Aktif Et
                        system.Client().Update(GOCATOR_CONTROL_PATH, new JObject { ["enabled"] = true }).CheckResponse(5000);

                        using (GoGdpClient gdpClient = new GoGdpClient())
                        {
                            gdpClient.Connect(system.Address, system.GdpPort());

                            if (system.RunningState() == GoSystem.State.Ready)
                                system.Start();

                            log("Ölçüm verisi bekleniyor...");

                            gdpClient.ReceiveDataSync(RECEIVE_DATA_TIMEOUT_MSEC);

                            if (gdpClient.DataSet != null && gdpClient.DataSet.Count > 0)
                            {
                                // DÜZELTME: WinUI 3 Uyumlu Dispatcher Kullanımı
                                dispatcher.TryEnqueue(() =>
                                {
                                    App4.Utilities.GlobalData.LastMeasurements.Clear();
                                });

                                int counter = 1;

                                for (int i = 0; i < gdpClient.DataSet.Count; i++)
                                {
                                    var msg = gdpClient.DataSet.GdpMsgAt(i);

                                    if (msg.Type == MessageType.Measurement && msg is GoGdpMeasurement mMsg)
                                    {
                                        // String -> Int Dönüşümü (Güvenli)
                                        int.TryParse(mMsg.DataSourceId, out int parsedSourceId);

                                        var newItem = new App4.Utilities.GocatorMeasurement
                                        {
                                            Id = counter++,
                                            SourceId = parsedSourceId,
                                            Name = $"Measurement {mMsg.DataSourceId}",
                                            Value = Math.Round(mMsg.Value, 3),
                                            Decision = mMsg.Decision.ToString(),
                                        };

                                        // DÜZELTME: WinUI 3 Uyumlu Dispatcher Kullanımı
                                        dispatcher.TryEnqueue(() =>
                                        {
                                            App4.Utilities.GlobalData.LastMeasurements.Add(newItem);
                                            App4.Utilities.GlobalData.SaveMeasurements();
                                        });
                                    }
                                }
                                log($"✓ {counter - 1} adet ölçüm alındı.");
                            }
                            else
                            {
                                log("⚠ Ölçüm verisi bulunamadı.");
                            }
                            system.Stop();
                        }
                    }
                    return 1;
                }
                catch (Exception ex)
                {
                    log($"✗ Ölçüm Hatası: {ex.Message}");
                    return -1;
                }
            });
        }
    }




}