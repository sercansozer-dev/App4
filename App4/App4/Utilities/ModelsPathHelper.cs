using System;
using System.IO;

namespace App4.Utilities
{
    /// <summary>
    /// 3D model (.glb) dosyalarının saklanacağı YAZILABİLİR klasörü döndürür.
    ///
    /// - Paketlenmiş WinUI 3 kurulumunda (MSIX):
    ///   %LOCALAPPDATA%\Packages\&lt;PackageName&gt;\LocalState\Utilities\Models
    ///   (ApplicationData.Current.LocalFolder — her kullanıcı için izole, update sonrası kalıcı)
    ///
    /// - Unpackaged/dev modda:
    ///   %LOCALAPPDATA%\Simbiosis\App4\Utilities\Models
    ///
    /// İlk çağrıda, kurulum dizinindeki (AppDomain.BaseDirectory\Utilities\Models) mevcut
    /// .glb dosyaları tek seferlik yazılabilir klasöre tohumlanır. Bu sayede:
    ///   1) Kurulum paketiyle gelen varsayılan modeller ilk açılışta erişilebilir olur,
    ///   2) Geliştirme ortamında bin\...\Utilities\Models içine konmuş dosyalar kaybolmaz,
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
            // Öncelik: WinUI 3 paketlenmiş LocalFolder (paket kimliği yoksa istisna fırlatır)
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
                // Paket kimliği yok → unpackaged fallback
            }

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "Simbiosis", "App4", "Utilities", "Models");
        }

        private static void SeedFromInstallFolder(string target)
        {
            string installModelsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utilities", "Models");
            if (!Directory.Exists(installModelsFolder)) return;

            // Eğer install klasörü ile hedef klasör aynıysa (dev çalışmasında olabilir) tohumlamaya gerek yok
            if (Path.GetFullPath(installModelsFolder).Equals(Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
                return;

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
