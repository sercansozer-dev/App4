using App4.Pages;
using App4.PAGES;
using App4.Utilities; // PlcService için namespace
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ApplicationSettings;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App4
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private AppWindow m_AppWindow;
        private string currentUserRole = "Operatör";
        // Mevcut değişkenlerin altına ekleyin
        private readonly string _stationStateFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "App4", "Station_States.json");
        // Bu değişken simülasyonun sadece 1 kere çalışmasını garanti eder
        private bool _hasRunStartup = false;

        public MainWindow()
        {
            this.InitializeComponent();
            InitializeTriggerMonitor(); // Eklendi: Trigger izleme

            // Pencere Ayarları (WindowID ile AppWindow alma)
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            m_AppWindow = AppWindow.GetFromWindowId(windowId);

            ConfigureWindow();

            // AÇILIŞ SİMÜLASYONU TETİKLEYİCİSİ
            this.Activated += MainWindow_Activated;

            PlcService.Instance.Initialize((action) =>
            {
                this.DispatcherQueue.TryEnqueue(() => action());
            });



        }

        private async void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            // Eğer daha önce çalışmadıysa çalıştır
            if (!_hasRunStartup)
            {
                _hasRunStartup = true; // Kilidi kapat
                NavigateToTag("auto"); // Arka planda ana sayfayı aç
                await SimulateStartup(); // Simülasyonu başlat
            }
        }

        // --- GÜNCELLENEN BAŞLANGIÇ SENARYOSU ---
        private async Task SimulateStartup()
        {
            // Animasyonu başlat
            PulseLogoAnim.Begin();

            try
            {
                // 0. BAŞLANGIÇ: Her şey görünür
                AppSplashScreen.Visibility = Visibility.Visible;
                AppSplashScreen.Opacity = 1;
                SplashContent.Opacity = 1;
                SplashContent.Visibility = Visibility.Visible;
                
                // Varsayılan metin rengi (Gri yerine Beyaz - Okunabilirlik için)
                var normalColor = new SolidColorBrush(Microsoft.UI.Colors.WhiteSmoke);
                SplashStatusText.Foreground = normalColor;

                // 1. YÜKLEME AŞAMALARI
                SplashStatusText.Text = "Sistem yapılandırması okunuyor...";
                await Task.Delay(1000);

                // --- 2. PLC BAĞLANTISINI GERÇEKLEŞTİR (Burada yapıyoruz) ---
                SplashStatusText.Text = "PLC Bağlantısı kuruluyor (192.168.251.100)...";
                SplashStatusText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Orange); // Dikkat çekmesi için renk değişimi

                // GLOBAL SERVİS ÜZERİNDEN BAĞLAN (Bekleme süresi bağlantı hızına bağlı)
                await Task.Delay(500); // Kullanıcı mesajı görebilsin
                bool connected = await PlcService.Instance.ConnectAsync("192.168.251.100", 5007);

                if (connected)
                {
                    SplashStatusText.Text = "✓ PLC Bağlantısı Başarılı!";
                    SplashStatusText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.LimeGreen);
                }
                else
                {
                    SplashStatusText.Text = "⚠ PLC Bağlantısı Başarısız! (Offline Mod)";
                    SplashStatusText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
                }

                await Task.Delay(1500); // Kullanıcı sonucu okuyabilsin diye bekleme

                // Rengi normale döndür
                SplashStatusText.Foreground = normalColor;

                SplashStatusText.Text = "Gocator 3D Sensör bağlantısı kuruluyor...";
                await Task.Delay(1000);

                SplashStatusText.Text = "KUKA Robot arayüzü başlatılıyor...";
                App4.Utilities.KukaService.Instance.UiDispatcher = this.DispatcherQueue;
                App4.Utilities.KukaService.Instance.Start();
                await Task.Delay(1000);

                SplashStatusText.Text = "Arayüz hazırlanıyor...";
                await Task.Delay(800);


                // 3. LOGOYU VE YAZILARI SİL (Fade Out)
                for (double i = 1.0; i >= 0; i -= 0.1)
                {
                    SplashContent.Opacity = i;
                    await Task.Delay(20);
                }
                SplashContent.Visibility = Visibility.Collapsed;

                // --- HAYALET GEÇİŞ KORUMASI (Pencere Boyutlandırma) ---
                if (m_AppWindow != null)
                {
                    // Titremeyi azaltmak için Hide/Show kaldırıldı, direkt geçiş
                    m_AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                    await Task.Delay(100);
                }

                // 4. SİYAH PERDEYİ KALDIR (Fade Out)
                for (double i = 1.0; i >= 0; i -= 0.1)
                {
                    AppSplashScreen.Opacity = i;
                    await Task.Delay(25);
                }
                AppSplashScreen.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                // Hata durumunda ekranı temizle
                AppSplashScreen.Visibility = Visibility.Collapsed;
                System.Diagnostics.Debug.WriteLine("Startup Hatası: " + ex.Message);
            }
        }

       

       



        private void ConfigureWindow()
        {
            if (m_AppWindow != null)
            {
                m_AppWindow.Title = "Simbiosis Mekatronik";

                // Splash Ekranı Boyutu (Küçük Başlasın)
                int splashWidth = 900;
                int splashHeight = 600;

                m_AppWindow.SetPresenter(AppWindowPresenterKind.Default);
                m_AppWindow.Resize(new Windows.Graphics.SizeInt32(splashWidth, splashHeight));

                // Ekranı Ortala
                var displayArea = DisplayArea.GetFromWindowId(m_AppWindow.Id, DisplayAreaFallback.Primary);
                if (displayArea != null)
                {
                    var screenW = displayArea.WorkArea.Width;
                    var screenH = displayArea.WorkArea.Height;
                    int centeredX = (screenW - splashWidth) / 2;
                    int centeredY = (screenH - splashHeight) / 2;

                    if (centeredX < 0) centeredX = 0;
                    if (centeredY < 0) centeredY = 0;

                    m_AppWindow.Move(new Windows.Graphics.PointInt32(centeredX, centeredY));
                }
            }
            ExtendsContentIntoTitleBar = true;
        }

        private async void NavigateToTag(string tag)
        {
            if (tag == "exit")
            {
                ContentDialog exitDialog = new ContentDialog
                {
                    Title = "Sistemi Kapat",
                    Content = "Uygulamadan çıkış yapmak üzeresiniz. Onaylıyor musunuz?",
                    PrimaryButtonText = "Evet, Kapat",
                    CloseButtonText = "İptal",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.Content.XamlRoot,
                    RequestedTheme = ElementTheme.Dark
                };

                var result = await exitDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    Application.Current.Exit();
                }
                return;
            }

            var transition = new Microsoft.UI.Xaml.Media.Animation.SlideNavigationTransitionInfo()
            {
                Effect = Microsoft.UI.Xaml.Media.Animation.SlideNavigationTransitionEffect.FromRight
            };

            switch (tag)
            {
                case "auto": ContentFrame.Navigate(typeof(Auto_Page), null, transition); break;
                case "manuel": ContentFrame.Navigate(typeof(Manuel_Page), null, transition); break;
                case "camera": ContentFrame.Navigate(typeof(Camera_Page), null, transition); break;
                case "plc": ContentFrame.Navigate(typeof(PLC_Page), null, transition); break;
                case "settings": ContentFrame.Navigate(typeof(Settings_Page), null, transition); break;
                case "robot": ContentFrame.Navigate(typeof(Robot_Page), null, transition); break;
                case "multirobot": ContentFrame.Navigate(typeof(MultiRobot_Page), null, transition); break;
                case "recipes": ContentFrame.Navigate(typeof(Recipes_Page), null, transition); break;
                case "Klima_Editor_Page": ContentFrame.Navigate(typeof(Klima_Editor_Page)); break;
            }
        }

        private void MainNav_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked) NavigateToTag("settings");
            else
            {
                string tag = args.InvokedItemContainer?.Tag?.ToString() ?? string.Empty;
                NavigateToTag(tag);
            }
        }

        // --- GİRİŞ SİSTEMİ ---

        private void LoginTrigger_Click(object sender, RoutedEventArgs e)
        {
            PasswordInput.Password = "";
            LoginStatusText.Text = "";
            LoginOverlay.Visibility = Visibility.Visible;
            PasswordInput.Focus(FocusState.Programmatic);
        }

        private void LoginCancel_Click(object sender, RoutedEventArgs e)
        {
            LoginOverlay.Visibility = Visibility.Collapsed;
            LoginStatusText.Text = "";
            PasswordInput.Password = "";
        }

        private void LoginConfirm_Click(object sender, RoutedEventArgs e)
        {
            ProcessLogin();
        }

        private void PasswordInput_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                ProcessLogin();
            }
        }

        private DispatcherTimer _triggerMonitorTimer;

        private void InitializeTriggerMonitor()
        {
            _triggerMonitorTimer = new DispatcherTimer();
            _triggerMonitorTimer.Interval = TimeSpan.FromMilliseconds(500);
            _triggerMonitorTimer.Tick += (s, e) =>
            {
                if (GlobalData.IsProcessRunning) return;

                string tagName = GlobalData.Auto_TriggerTag;
                if (string.IsNullOrEmpty(tagName)) return;

                var v = PlcService.Instance?.InputVariables.FirstOrDefault(x => x.Name == tagName) ??
                        PlcService.Instance?.OutputVariables.FirstOrDefault(x => x.Name == tagName) ??
                        GlobalData.GeneralInputVars.FirstOrDefault(x => x.Name == tagName);

                if (v != null)
                {
                    string val = v.Value ?? "0";
                    bool isHigh = (val == "1" || val?.ToLower() == "true");
                    // Kullanıcı 0 veya 1 olduğunu görmek istiyor
                    GlobalData.ProcessStatus = isHigh ? "HAZIR (Sinyal: 1)" : "HAZIR (Sinyal: 0)";
                }
            };
            _triggerMonitorTimer.Start();
        }

        private void ProcessLogin()
        {
            if (UserRoleCombo.SelectedItem is ComboBoxItem selectedItem)
            {
                string role = selectedItem.Content?.ToString() ?? "Operatör";
                string pin = PasswordInput.Password;

                bool authorized = false;
                if (role == "Admin" && pin == "1234") authorized = true;
                else if (role == "Expert" && pin == "0000") authorized = true;
                else if (role == "Operatör") authorized = true;

                if (authorized)
                {
                    ApplyUserRole(role);
                    LoginStatusText.Text = "";
                    PasswordInput.Password = "";
                    LoginOverlay.Visibility = Visibility.Collapsed;
                }
                else
                {
                    LoginStatusText.Text = "? HATALI PIN! TEKRAR DENEYİN.";
                    PasswordInput.Password = "";
                }
            }
        }

        private void ApplyUserRole(string role)
        {
            currentUserRole = role;
            CurrentUserText.Text = "Kullanıcı: " + role;
            CurrentRoleText.Text = role == "Admin" ? "Tam Yetkili Erişim" : "Sınırlı Erişim";
            UserStatusLed.Fill = new SolidColorBrush(Microsoft.UI.Colors.LimeGreen);
            MainNav.IsSettingsVisible = (role == "Admin" || role == "Expert");
        }

        private void Numpad_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) PasswordInput.Password += btn.Content.ToString();
        }

        private void Numpad_Clear_Click(object sender, RoutedEventArgs e) => PasswordInput.Password = "";

        private void Numpad_Backspace_Click(object sender, RoutedEventArgs e)
        {
            if (PasswordInput.Password.Length > 0)
                PasswordInput.Password = PasswordInput.Password.Substring(0, PasswordInput.Password.Length - 1);
        }
    }
}