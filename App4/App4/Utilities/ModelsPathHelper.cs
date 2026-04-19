using System;
using System.IO;

namespace App4.Utilities
{
    /// <summary>
    /// 3D model (.glb) dosyalarının saklanacağı YAZILABİLİR klasörü döndürür.
    ///
    /// HEDEF (kurulum sonrası):
    ///   C:\Simbiosis\SimbiosisLeakTestApp\Models
    ///   — Config klasörüyle AYNI mantık (kullanıcı tarafından kolayca bulunur,
    ///     Klima Editörü'nden yeni model eklenirken yazılabilir, update/reinstall'da
    ///     korunur — .iss 'onlyifdoesntexist' flag'i sayesinde).
    ///
    /// FALLBACK (paket/MSIX modu veya C:\ yazma izni yoksa):
    ///   ApplicationData.Current.LocalFolder\Utilities\Models
    ///   — veya %LOCALAPPDATA%\Simbiosis\App4\Utilities\Models
    ///
    /// İlk çağrıda, kurulum dizinindeki (AppDomain.BaseDirectory) {app}\Models
    /// veya {app}\Utilities\Models altındaki .glb dosyaları yazılabilir klasöre
    /// tohumlanır. Bu sayede:
    ///   1) Kurulum paketiyle gelen varsayılan modeller ilk açılışta erişilebilir olur,
    ///   2) Geliştirme ortamında bin\...\Models içine konmuş dosyalar kaybolmaz,
    ///   3) Kullanıcı Klima Editörü'nden yeni model yüklediğinde yazma izin sorunu yaşanmaz.
    /// </summary>
    public static class ModelsPathHelper
    {
        private static string? _cachedPath;
        private static readonly object _lock = new object();

        /// <summary>
        /// Yazılabilir model klasörünün tam yolunu döndürür (yoksa oluşturur, ilk seferde tohumlar).
        /// </summary>
        public static string GetModelsFolder()
        {
            if (_cachedPath != null) return _cachedPath;

            lock (_lock)
            {
                if (_cachedPath != null) return _cachedPath;

                string target = ResolveWritableRoot();

                try
                {
                    if (!Directory.Exists(target)) Directory.CreateDirectory(target);
                    SeedFromInstallFolder(target);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ModelsPathHelper] Seed/create hatası: {ex.Message}");
                }

                _cachedPath = target;
                System.Diagnostics.Debug.WriteLine($"[ModelsPathHelper] Models klasörü: {_cachedPath}");
                return _cachedPath;
            }
        }

        private static string ResolveWritableRoot()
        {
            // ÖNCELİK 1 (ASIL): C:\Simbiosis\SimbiosisLeakTestApp\Models
            //   — Config klasörüyle aynı pattern. Installer bu klasörü oluşturuyor
            //     ve "users-modify" izni veriyor; ayrıca model seed'i de buraya kopyalıyor.
            //   — Kullanıcı için görünür ve yönetilebilir tek yer.
            const string primaryPath = @"C:\Simbiosis\SimbiosisLeakTestApp\Models";
            try
            {
                if (!Directory.Exists(primaryPath))
                    Directory.CreateDirectory(primaryPath);

                // Yazma testi — permission sorunlarını erkenden yakala
                string testFile = Path.Combine(primaryPath, ".write_test");
                File.WriteAllText(testFile, "");
                File.Delete(testFile);
                return primaryPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ModelsPathHelper] C:\\Simbiosis erişilemedi, fallback: {ex.Message}");
            }

            // ÖNCELİK 2: WinUI 3 paketlenmiş LocalFolder (MSIX senaryosu)
            try
            {
                var local = Windows.Storage.ApplicationData.Current.LocalFolder;
                if (local != null && !string.IsNullOrEmpty(local.Path))
                {
                    return Path.Combine(local.Path, "Utilities", "Models");
                }
            }
            catch
            {
                // Paket kimliği yok -> LocalAppData fallback
            }

            // ÖNCELİK 3: LocalAppData (unpackaged, C:\ yazılamaz)
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "Simbiosis", "App4", "Utilities", "Models");
        }

        private static void SeedFromInstallFolder(string target)
        {
            // Modellerin kurulum klasöründe bulunabileceği iki yol:
            //   1) {app}\Models          <- csproj Content Include ile publish'e çıkan yer (ASIL)
            //   2) {app}\Utilities\Models <- eski layout (geriye dönük uyumluluk)
            // Her iki klasörü de tara; hangisinde .glb varsa onları kullan.
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidates = new[]
            {
                Path.Combine(baseDir, "Models"),
                Path.Combine(baseDir, "Utilities", "Models")
            };

            foreach (var installModelsFolder in candidates)
            {
                if (!Directory.Exists(installModelsFolder)) continue;

                // Eğer install klasörü ile hedef klasör aynıysa (dev çalışmasında olabilir) tohumlamaya gerek yok
                if (Path.GetFullPath(installModelsFolder).Equals(Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var src in Directory.GetFiles(installModelsFolder, "*.glb", SearchOption.AllDirectories))
                {
                    try
                    {
                        string rel = Path.GetRelativePath(installModelsFolder, src);
                        string dst = Path.Combine(target, rel);
                        string? dstDir = Path.GetDirectoryName(dst);
                        if (!string.IsNullOrEmpty(dstDir) && !Directory.Exists(dstDir))
                            Directory.CreateDirectory(dstDir);

                        // Sadece yoksa kopyala — kullanıcının düzenlediği/sildiği dosyaları ezme
                        if (!File.Exists(dst))
                        {
                            File.Copy(src, dst, false);
                            System.Diagnostics.Debug.WriteLine($"[ModelsPathHelper] Seed: {rel}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ModelsPathHelper] Seed kopyalama hatası ({src}): {ex.Message}");
                    }
                }
            }
        }
    }
}
