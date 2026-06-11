using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
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

        // ═══════════════════════════════════════════════════════════════════════
        // TEK ÖRNEK (SINGLE-INSTANCE) KORUMASI
        // ───────────────────────────────────────────────────────────────────────
        // Uygulama zaten açıkken ikinci kez başlatılırsa (operatör ikona iki kez
        // tıklarsa) yeni süreç AÇILMADAN kapanır. Aksi halde iki instance aynı
        // PLC'nin TEK MC bağlantısını paylaşmaya çalışır ve ikincisi ilkinin
        // heartbeat'ini düşürür (saha notu: .103 PLC tek bağlantı limiti).
        // İkinci başlatmada zaten açık olan pencere öne getirilir.
        // Mutex isim önekisiz → oturum (session) kapsamlı; tek operatörlü endüstriyel
        // PC için doğru kapsam (Global mutex yetki sorunlarından kaçınılır).
        // ═══════════════════════════════════════════════════════════════════════
        private static Mutex _singleInstanceMutex; // süreç ömrü boyunca tutulur (GC olmamalı)
        private const string SingleInstanceMutexName =
            "Simbiosis_SimbiosisLeakTestApp_SingleInstance_9F3C2A71-4B5E-4D2A-9C1F-7E6B0A2D8F44";

        private const int SW_RESTORE = 9;
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hWnd);

        /// <summary>
        /// İlk/tek örnekse true döner ve uygulama normal açılır. Zaten bir örnek
        /// çalışıyorsa mevcut pencereyi öne getirir ve bu süreci sonlandırır
        /// (Environment.Exit — false dönüşü yalnızca savunma amaçlıdır).
        /// </summary>
        private bool EnsureSingleInstance()
        {
            bool createdNew;
            try
            {
                _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out createdNew);
            }
            catch
            {
                // Mutex oluşturulamazsa kilitlenme yaratma — uygulamayı normal aç.
                return true;
            }

            if (createdNew)
                return true; // ilk/tek örnek → normal devam et

            // Zaten çalışan bir örnek var → onu öne getir, bu süreci kapat.
            try { BringExistingInstanceToFront(); } catch { }
            System.Diagnostics.Debug.WriteLine("[SINGLE_INSTANCE] İkinci örnek engellendi — uygulama zaten açık.");
            Environment.Exit(0);
            return false;
        }

        /// <summary>Çalışan diğer örneğin ana penceresini bulur ve öne getirir.</summary>
        private static void BringExistingInstanceToFront()
        {
            var current = Process.GetCurrentProcess();
            foreach (var p in Process.GetProcessesByName(current.ProcessName))
            {
                if (p.Id == current.Id) continue;
                IntPtr h = p.MainWindowHandle;
                if (h != IntPtr.Zero)
                {
                    if (IsIconic(h)) ShowWindow(h, SW_RESTORE); // simge durumundaysa geri yükle
                    SetForegroundWindow(h);
                    return;
                }
            }
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
          // ═══ TEK ÖRNEK KORUMASI: zaten açıksa ikinci örneği engelle (PLC init'ten ÖNCE) ═══
          if (!EnsureSingleInstance())
              return; // ikinci örnek → süreç EnsureSingleInstance içinde zaten sonlandırıldı

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
