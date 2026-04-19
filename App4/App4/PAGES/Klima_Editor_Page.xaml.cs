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

        private async void ModelYukleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileOpenPicker();
                picker.ViewMode = PickerViewMode.List;
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                // Sistem genelinde yalnızca .glb destekleniyor (RecipeManager.RefreshModelLibraryAsync .glb tarar).
                picker.FileTypeFilter.Add(".glb");

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.m_window);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var file = await picker.PickSingleFileAsync();
                if (file == null) return;

                string targetFolder = App4.Utilities.ModelsPathHelper.GetModelsFolder();
                if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);

                string targetPath = Path.Combine(targetFolder, file.Name);
                File.Copy(file.Path, targetPath, true);

                // Global model kütüphanesini yenile — Otomatik sayfa ve Reçeteler dropdown'ları bu listeyi kullanır.
                await App4.Utilities.RecipeManager.RefreshModelLibraryAsync();

                // Sol paneli diskten yeniden tara (sıralı ve kalıcı — restart sonrası da aynı görünür).
                RefreshModelKutuphanesiFromDisk();

                await ShowMessage("Model Yüklendi", $"\"{file.Name}\" kütüphaneye eklendi. Otomatik sayfadaki RFID-Model dropdown'larında artık görünür.");
            }
            catch (Exception ex)
            {
                await ShowMessage("Hata", $"Model yüklenemedi: {ex.Message}");
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
