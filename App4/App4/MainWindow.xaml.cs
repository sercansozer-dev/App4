using App4.Pages;
using App4.PAGES;
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

        // Bu deðiþken simülasyonun sadece 1 kere çalýþmasýný garanti eder
        private bool _hasRunStartup = false;

        public MainWindow()
        {
            this.InitializeComponent();

            // Pencere Ayarlarý (WindowID ile AppWindow alma)
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            m_AppWindow = AppWindow.GetFromWindowId(windowId);

            ConfigureWindow();

            // AÇILIÞ SÝMÜLASYONU TETÝKLEYÝCÝSÝ
            this.Activated += MainWindow_Activated;
        }

        private async void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            // Eðer daha önce çalýþmadýysa çalýþtýr
            if (!_hasRunStartup)
            {
                _hasRunStartup = true; // Kilidi kapat
                NavigateToTag("auto"); // Arka planda ana sayfayý aç
                await SimulateStartup(); // Simülasyonu baþlat
            }
        }

        private async Task SimulateStartup()
        {
            // Animasyonu baþlat
            PulseLogoAnim.Begin();

            try
            {
                // 1. BAÞLANGIÇ: Her þey görünür
                AppSplashScreen.Visibility = Visibility.Visible;
                AppSplashScreen.Opacity = 1;
                SplashContent.Opacity = 1;
                SplashContent.Visibility = Visibility.Visible;

                // 2. YÜKLEME AÞAMALARI
                SplashStatusText.Text = "Sistem yapýlandýrmasý okunuyor...";
                await Task.Delay(800);

                SplashStatusText.Text = "Gocator 3D Sensör baðlantýsý kuruluyor...";
                await Task.Delay(1000);

                SplashStatusText.Text = "KUKA Robot arayüzü baþlatýlýyor...";
                await Task.Delay(800);

                SplashStatusText.Text = "Arayüz hazýrlanýyor...";
                await Task.Delay(500);

                // 3. LOGOYU VE YAZILARI SÝL (Fade Out)
                for (double i = 1.0; i >= 0; i -= 0.1)
                {
                    SplashContent.Opacity = i;
                    await Task.Delay(10);
                }
                SplashContent.Visibility = Visibility.Collapsed;

                // --- HAYALET GEÇÝÞ (Pencere Boyutlandýrma) ---
                if (m_AppWindow != null)
                {
                    m_AppWindow.Hide();
                    await Task.Delay(200);
                    m_AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                    await Task.Delay(500);
                    m_AppWindow.Show(true);
                }

                // 4. SÝYAH PERDEYÝ KALDIR (Fade Out)
                for (double i = 1.0; i >= 0; i -= 0.1)
                {
                    AppSplashScreen.Opacity = i;
                    await Task.Delay(25);
                }
                AppSplashScreen.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                // Hata durumunda ekraný temizle
                AppSplashScreen.Visibility = Visibility.Collapsed;
            }
        }

        private void ConfigureWindow()
        {
            if (m_AppWindow != null)
            {
                m_AppWindow.Title = "Simbiosis Mekatronik";

                // Splash Ekraný Boyutu (Küçük Baþlasýn)
                int splashWidth = 900;
                int splashHeight = 600;

                m_AppWindow.SetPresenter(AppWindowPresenterKind.Default);
                m_AppWindow.Resize(new Windows.Graphics.SizeInt32(splashWidth, splashHeight));

                // Ekraný Ortala
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
                    Content = "Uygulamadan çýkýþ yapmak üzeresiniz. Onaylýyor musunuz?",
                    PrimaryButtonText = "Evet, Kapat",
                    CloseButtonText = "Ýptal",
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
                case "camera": ContentFrame.Navigate(typeof(Camera_Page), null, transition); break; // Alt çizgiye dikkat!
                case "settings": ContentFrame.Navigate(typeof(Settings_Page), null, transition); break;
                case "robot": ContentFrame.Navigate(typeof(Robot_Page), null, transition); break;
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

        // --- GÝRÝÞ SÝSTEMÝ ---

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
                    LoginStatusText.Text = "? HATALI PIN! TEKRAR DENEYÝN.";
                    PasswordInput.Password = "";
                }
            }
        }

        private void ApplyUserRole(string role)
        {
            currentUserRole = role;
            CurrentUserText.Text = "Kullanýcý: " + role;
            CurrentRoleText.Text = role == "Admin" ? "Tam Yetkili Eriþim" : "Sýnýrlý Eriþim";
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
