using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App4
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public static Window? m_window;
        // Bu de�i�kenin ba��nda 'public' yazmal�!
        public Window MainWindow { get; private set; }
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        // ═══ ERKEN CRASH LOG YOLU (ConfigBaseDir'e bağımlı değil — sabit yol) ═══
        private static readonly string _startupLogPath =
            @"C:\Simbiosis\SimbiosisLeakTestApp\startup-crash.log";

        private static void LogStartupError(string stage, Exception ex)
        {
            try
            {
                var dir = System.IO.Path.GetDirectoryName(_startupLogPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"=== {DateTime.Now:yyyy-MM-dd HH:mm:ss} [{stage}] ===");
                sb.AppendLine($"Type     : {ex?.GetType().FullName}");
                sb.AppendLine($"Message  : {ex?.Message}");
                sb.AppendLine($"Source   : {ex?.Source}");
                sb.AppendLine($"HResult  : 0x{ex?.HResult:X8}");
                sb.AppendLine($"Stack    :");
                sb.AppendLine(ex?.StackTrace ?? "(no stack)");
                if (ex?.InnerException != null)
                {
                    sb.AppendLine("--- InnerException ---");
                    sb.AppendLine($"Type   : {ex.InnerException.GetType().FullName}");
                    sb.AppendLine($"Message: {ex.InnerException.Message}");
                    sb.AppendLine(ex.InnerException.StackTrace);
                }
                sb.AppendLine();
                File.AppendAllText(_startupLogPath, sb.ToString());
            }
            catch { /* son çare: sessiz */ }
        }

        public App()
        {
            // ═══ TÜM AppDomain-düzeyi crash'leri yakala ═══
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                LogStartupError("AppDomain.UnhandledException", e.ExceptionObject as Exception
                    ?? new Exception("non-Exception throw: " + e.ExceptionObject));

            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                LogStartupError("TaskScheduler.UnobservedTaskException", e.Exception);
                e.SetObserved();
            };

            try
            {
                InitializeComponent();

                // WinUI 3 UI thread exception handler
                this.UnhandledException += (s, e) =>
                {
                    LogStartupError("Application.UnhandledException", e.Exception);
                    e.Handled = true; // uygulamayı hemen kapatmak yerine devam ettir
                };
            }
            catch (Exception ex)
            {
                LogStartupError("App.ctor", ex);
                throw;
            }
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
          try
          {
            App4.Utilities.GlobalData.Initialize();
            m_window = new MainWindow();
            MainWindow = m_window;

            // ═══ UYGULAMA KAPANIRKEN PLC TAG EŞLEŞTİRMELERİNİ KAYDET ═══
            m_window.Closed += (s, e) =>
            {
                try
                {
                    App4.Utilities.GlobalData.SavePlcVariableTagsToFile();
                    System.Diagnostics.Debug.WriteLine("[APP_CLOSE] PlcVariable tag eşleştirmeleri kaydedildi.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[APP_CLOSE] PlcTag kayıt hatası: {ex.Message}");
                }

                // Robot Input/Output degiskenlerini kaydet
                try
                {
                    App4.Utilities.KukaRobotManager.Instance?.SaveRobotVariables();
                    System.Diagnostics.Debug.WriteLine("[APP_CLOSE] Robot degiskenleri kaydedildi.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[APP_CLOSE] Robot degisken kayıt hatası: {ex.Message}");
                }
            };

            m_window.Activate();
          }
          catch (Exception ex)
          {
            LogStartupError("OnLaunched", ex);
            // Kullanıcıya en azından bir şey göster — sessiz çıkışı engelle
            try
            {
                var errWindow = new Window
                {
                    Title = "Simbiosis Leak Test App — Başlatma Hatası"
                };
                var tb = new Microsoft.UI.Xaml.Controls.TextBlock
                {
                    Text = "Uygulama başlatılamadı. Detay:\n\n" +
                           ex.GetType().FullName + "\n" +
                           ex.Message + "\n\n" +
                           "Log: " + _startupLogPath,
                    TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                    Margin = new Microsoft.UI.Xaml.Thickness(20)
                };
                errWindow.Content = tb;
                errWindow.Activate();
                m_window = errWindow;
                MainWindow = errWindow;
            }
            catch { throw; }
          }
        }
    }
}
