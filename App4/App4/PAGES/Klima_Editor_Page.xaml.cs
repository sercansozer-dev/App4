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
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App4.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Klima_Editor_Page : Page
    {
        public Klima_Editor_Page()
        {
            InitializeComponent();
            this.Loaded += Klima_Editor_Page_Loaded;
        }

        private void Klima_Editor_Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Model kütüphanesini diskten doldur — yüklenen modeller restart sonrası da görünsün.
            RefreshModelKutuphanesiFromDisk();
        }

        private void RefreshModelKutuphanesiFromDisk()
        {
            try
            {
                ModelKutuphanesiList.Items.Clear();

                string modelsFolder = App4.Utilities.ModelsPathHelper.GetModelsFolder();
                if (!Directory.Exists(modelsFolder)) return;

                var glbFiles = Directory.GetFiles(modelsFolder, "*.glb", SearchOption.AllDirectories)
                                        .OrderBy(p => Path.GetFileName(p))
                                        .ToArray();

                foreach (var file in glbFiles)
                {
                    AddModelToList(Path.GetFileName(file));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[KlimaEditor] Model listesi doldurma hatası: {ex.Message}");
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  WIN32 OPEN FILE DIALOG (comdlg32)
        //  WinUI 3 FileOpenPicker unpackaged modda bazi PC'lerde sessizce
        //  kapaniyor/izin vermiyor. Win32 GetOpenFileName stabiller — her
        //  Windows surumunde ve her kurulum modunda calisir.
        // ═════════════════════════════════════════════════════════════════
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct OpenFileName
        {
            public int      lStructSize;
            public IntPtr   hwndOwner;
            public IntPtr   hInstance;
            public string   lpstrFilter;
            public string?  lpstrCustomFilter;
            public int      nMaxCustFilter;
            public int      nFilterIndex;
            public IntPtr   lpstrFile;
            public int      nMaxFile;
            public IntPtr   lpstrFileTitle;
            public int      nMaxFileTitle;
            public string?  lpstrInitialDir;
            public string?  lpstrTitle;
            public int      Flags;
            public short    nFileOffset;
            public short    nFileExtension;
            public string?  lpstrDefExt;
            public IntPtr   lCustData;
            public IntPtr   lpfnHook;
            public string?  lpTemplateName;
            public IntPtr   pvReserved;
            public int      dwReserved;
            public int      FlagsEx;
        }

        [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetOpenFileName(ref OpenFileName ofn);

        private const int OFN_FILEMUSTEXIST    = 0x00001000;
        private const int OFN_PATHMUSTEXIST    = 0x00000800;
        private const int OFN_HIDEREADONLY     = 0x00000004;
        private const int OFN_EXPLORER         = 0x00080000;
        private const int OFN_NOCHANGEDIR      = 0x00000008;

        private static string? PickGlbFileWin32(IntPtr ownerHwnd)
        {
            IntPtr buf = Marshal.AllocHGlobal(260 * 2 * sizeof(char)); // 260 wide chars, double it for safety
            try
            {
                // Buffer null-terminated olmali
                Marshal.WriteInt16(buf, 0, 0);

                var ofn = new OpenFileName
                {
                    lStructSize     = Marshal.SizeOf<OpenFileName>(),
                    hwndOwner       = ownerHwnd,
                    lpstrFilter     = "GLB Model Dosyalari (*.glb)\0*.glb\0Tum Dosyalar (*.*)\0*.*\0\0",
                    nFilterIndex    = 1,
                    lpstrFile       = buf,
                    nMaxFile        = 520,
                    lpstrTitle      = "Klima Modeli Sec (.glb)",
                    lpstrDefExt     = "glb",
                    Flags           = OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_HIDEREADONLY | OFN_EXPLORER | OFN_NOCHANGEDIR,
                };

                if (GetOpenFileName(ref ofn))
                {
                    string? path = Marshal.PtrToStringUni(buf);
                    return string.IsNullOrWhiteSpace(path) ? null : path;
                }
                return null;  // Kullanici iptal etti
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        private async void ModelYukleButton_Click(object sender, RoutedEventArgs e)
        {
            string? pickedPath = null;
            try
            {
                // Win32 GetOpenFileName: unpackaged WinUI 3 icin en stabil secenek
                IntPtr hwnd = IntPtr.Zero;
                if (App.m_window != null)
                    hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.m_window);

                pickedPath = PickGlbFileWin32(hwnd);
                if (string.IsNullOrEmpty(pickedPath)) return; // iptal

                if (!File.Exists(pickedPath))
                {
                    await ShowMessage("Hata", $"Secilen dosya bulunamadi:\n{pickedPath}");
                    return;
                }

                string fileName = Path.GetFileName(pickedPath);
                string targetFolder = App4.Utilities.ModelsPathHelper.GetModelsFolder();
                if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);

                string targetPath = Path.Combine(targetFolder, fileName);
                File.Copy(pickedPath, targetPath, true);

                // Global model kütüphanesini yenile — Otomatik sayfa ve Reçeteler dropdown'ları bu listeyi kullanır.
                await App4.Utilities.RecipeManager.RefreshModelLibraryAsync();

                // Sol paneli diskten yeniden tara (sıralı ve kalıcı — restart sonrası da aynı görünür).
                RefreshModelKutuphanesiFromDisk();

                await ShowMessage("Model Yüklendi", $"\"{fileName}\" kütüphaneye eklendi. Otomatik sayfadaki RFID-Model dropdown'larında artık görünür.");
            }
            catch (Exception ex)
            {
                await ShowMessage("Hata", $"Model yüklenemedi: {ex.GetType().Name}: {ex.Message}\n\nKaynak: {pickedPath ?? "(secim yapilamadi)"}");
            }
        }

        private void AddModelToList(string fileName)
        {
            string baseName = Path.GetFileNameWithoutExtension(fileName);

            var iconBorder = new Border
            {
                Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x25, 0x25, 0x28)),
                CornerRadius = new CornerRadius(6),
                Width = 40,
                Height = 40
            };
            iconBorder.Child = new FontIcon
            {
                Glyph = "\uF158",
                Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x00, 0x78, 0xD4)),
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var title = new TextBlock
            {
                Text = baseName,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                FontSize = 13,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            };

            var subtitle = new TextBlock
            {
                Text = Path.GetExtension(fileName).TrimStart('.').ToUpperInvariant(),
                Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x66, 0x66, 0x66)),
                FontSize = 10
            };

            var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            textStack.Children.Add(title);
            textStack.Children.Add(subtitle);

            var rowStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
            rowStack.Children.Add(iconBorder);
            rowStack.Children.Add(textStack);

            var item = new ListViewItem
            {
                Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x1E, 0x1E, 0x20)),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(0, 4, 0, 4),
                Padding = new Thickness(12),
                Content = rowStack,
                Tag = fileName
            };

            ModelKutuphanesiList.Items.Add(item);
        }

        private async Task ShowMessage(string title, string content)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "Tamam",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
