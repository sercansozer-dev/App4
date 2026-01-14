using App4.Models;
using App4.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Windows.Storage;
using static App4.Utilities.GlobalSettings;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App4.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Recipes_Page : Page
    {
        private ObservableCollection<ProductRecipe>? AllRecipes;
        private ProductRecipe? SelectedRecipe;

        public Recipes_Page()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(">>> [RECIPES] Sayfa Yükleniyor...");

            try
            {
                // 1. LÝSTELERÝ HAZIRLA
                AllRecipes = new ObservableCollection<ProductRecipe>();
                RecipesList.ItemsSource = AllRecipes;

                // 2. REÇETELERÝ YÜKLE
                try
                {
                    var list = await RecipeManager.LoadAllRecipesAsync();
                    if (list != null)
                    {
                        foreach (var item in list)
                        {
                            if (item.TargetPoints != null && !(item.TargetPoints is ObservableCollection<TargetPoint>))
                                item.TargetPoints = new ObservableCollection<TargetPoint>(item.TargetPoints);

                            AllRecipes.Add(item);
                        }
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Reçete yükleme hatasý: " + ex.Message); }

                // 3. WEBVIEW2 BAÞLATMA
                System.Diagnostics.Debug.WriteLine(">>> [RECIPES] WebView2 Hazýrlanýyor...");

                string userDataFolder = Path.Combine(Path.GetTempPath(), "Simbiosis_WebView2_Cache");
                var env = await CoreWebView2Environment.CreateWithOptionsAsync(null, userDataFolder, null);
                await PreviewWebView.EnsureCoreWebView2Async(env);

                // =================================================================================
                // DÜZELTME BURADA: Senin masaüstündeki klasör yolunu buraya da ekledik.
                // =================================================================================
                string baseFolder = @"C:\Users\Simbiosis\Desktop\Project\App2\App2\App2\Utilities\Models";

                // Klasör yoksa varsayýlaný kullan (Yedek plan)
                if (!Directory.Exists(baseFolder))
                {
                    baseFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utilities", "Models");
                }

                PreviewWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "localproject",
                    baseFolder,
                    CoreWebView2HostResourceAccessKind.Allow);

                PreviewWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                PreviewWebView.NavigationCompleted += PreviewWebView_NavigationCompleted;

                // HTML'i yükle
                PreviewWebView.Source = new Uri("https://localproject/ThreeJS_Viewer.html");

                await RefreshLibraryList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"!!! [RECIPES KRÝTÝK HATA] : {ex.Message}");
            }
        }

        // --- HTML YÜKLENDIKÇE MODELI ÇAÐIR ---
        private async void PreviewWebView_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            if (args.IsSuccess && SelectedRecipe != null && !string.IsNullOrEmpty(SelectedRecipe.StepFilePath))
            {
                string temizAd = Path.GetFileName(SelectedRecipe.StepFilePath);
                await Update3DPreview(temizAd);
            }
        }

        // --- MODELI GÜNCELLE ---
        private async Task Update3DPreview(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;
            try
            {
                // WebView hazýr mý emin ol
                if (PreviewWebView.CoreWebView2 == null) return;

                string url = $"https://localproject/{fileName}";
                System.Diagnostics.Debug.WriteLine($">>> [3D PREVIEW] Model Yükleniyor: {url}");

                await PreviewWebView.ExecuteScriptAsync($"if(window.loadModel) {{ window.loadModel('{url}'); }}");
                TxtPreviewName.Text = fileName;
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("3D Yükleme Hatasý: " + ex.Message); }
        }

        // --- DÝÐER FONKSIYONLAR (Ayný Kalýyor) ---
        private void CoreWebView2_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    string json = args.TryGetWebMessageAsString();
                    var pointData = System.Text.Json.JsonSerializer.Deserialize<PointMessage>(json);

                    if (pointData?.type == "NEW_POINT" && SelectedRecipe != null)
                    {
                        if (SelectedRecipe.TargetPoints == null)
                            SelectedRecipe.TargetPoints = new ObservableCollection<TargetPoint>();

                        var newPoint = new TargetPoint
                        {
                            PointName = $"Nokta {SelectedRecipe.TargetPoints.Count + 1}",
                            Description = "3D Editörden Seçildi",
                            RefX = pointData.data.x,
                            RefY = pointData.data.y,
                            RefZ = pointData.data.z,
                            Speed = 50,
                            WaitTime = 2
                        };

                        SelectedRecipe.TargetPoints.Add(newPoint);
                    }
                }
                catch { }
            });
        }

        private void RecipesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RecipesList.SelectedItem is ProductRecipe recipe)
            {
                SelectedRecipe = recipe;
                GlobalSettings.AppState.ActiveRecipe = recipe;

                DetailPanel.Visibility = Visibility.Visible;
                TxtName.Text = recipe.RecipeName;
                NumPlcCode.Value = recipe.PlcModelCode;
                TxtJob.Text = recipe.GocatorJobName;
                TxtStepPath.Text = recipe.StepFilePath;

                if (recipe.TargetPoints == null)
                    recipe.TargetPoints = new ObservableCollection<TargetPoint>();

                PointsList.ItemsSource = recipe.TargetPoints;

                if (!string.IsNullOrEmpty(recipe.StepFilePath))
                {
                    string temizAd = Path.GetFileName(recipe.StepFilePath);
                    _ = Update3DPreview(temizAd);
                }
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedRecipe == null) return;

            SelectedRecipe.RecipeName = TxtName.Text;
            SelectedRecipe.PlcModelCode = (int)NumPlcCode.Value;
            SelectedRecipe.GocatorJobName = TxtJob.Text;
            SelectedRecipe.StepFilePath = TxtStepPath.Text;
            SelectedRecipe.LastModified = DateTime.Now;

            if (PointsList.ItemsSource is ObservableCollection<TargetPoint> pointsCollection)
            {
                SelectedRecipe.TargetPoints = pointsCollection;
            }

            await RecipeManager.SaveRecipeAsync(SelectedRecipe);
            await ShowMessage("Baþarýlý", $"{SelectedRecipe.RecipeName} kaydedildi.");
        }

        // --- KÜTÜPHANE VE DOSYA SEÇÝMÝ ---
        private async Task RefreshLibraryList()
        {
            await RecipeManager.RefreshModelLibraryAsync();
            LibraryModelList.ItemsSource = GlobalSettings.AppState.ModelLibrary;
        }

        private void LibraryModelList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LibraryModelList.SelectedItem is ModelLibraryItem selectedModel)
            {
                string sadeceDosyaAdi = Path.GetFileName(selectedModel.FilePath);
                TxtStepPath.Text = sadeceDosyaAdi;

                if (SelectedRecipe != null)
                    SelectedRecipe.StepFilePath = sadeceDosyaAdi;

                _ = Update3DPreview(sadeceDosyaAdi);
            }
        }

        // --- BUTTON HANDLERS (Boþ veya basit olanlar) ---
        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            if (AllRecipes == null) { AllRecipes = new ObservableCollection<ProductRecipe>(); RecipesList.ItemsSource = AllRecipes; }
            var newRecipe = new ProductRecipe { RecipeName = "Yeni_" + DateTime.Now.ToString("HHmm"), TargetPoints = new ObservableCollection<TargetPoint>() };
            AllRecipes.Add(newRecipe); RecipesList.SelectedItem = newRecipe; DetailPanel.Visibility = Visibility.Visible;
        }
        private void BtnAddPoint_Click(object sender, RoutedEventArgs e) { if (SelectedRecipe?.TargetPoints != null) SelectedRecipe.TargetPoints.Add(new TargetPoint { PointName = "P" + (SelectedRecipe.TargetPoints.Count + 1), Speed = 50, WaitTime = 2 }); }
        private void BtnDeletePoint_Click(object sender, RoutedEventArgs e) { var p = (sender as Button)?.DataContext as TargetPoint; if (p != null) SelectedRecipe?.TargetPoints.Remove(p); }
        private void BtnDelete_Click(object sender, RoutedEventArgs e) { if (SelectedRecipe != null) { RecipeManager.DeleteRecipe(SelectedRecipe.RecipeName); AllRecipes?.Remove(SelectedRecipe); DetailPanel.Visibility = Visibility.Collapsed; } }
        private async void BtnDeleteLibraryModel_Click(object sender, RoutedEventArgs e) { var m = (sender as Button)?.DataContext as ModelLibraryItem; if (m != null && File.Exists(m.FilePath)) { File.Delete(m.FilePath); await RefreshLibraryList(); } }
        private void BtnOpen3DEditor_Click(object sender, RoutedEventArgs e) { /* Navigasyon kodu buraya gelecek */ }

        private async void BtnBrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add(".glb");
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.m_window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var files = await picker.PickMultipleFilesAsync();
            if (files.Count > 0)
            {
                // Burasý da senin masaüstü klasörüne kopyalayacak
                string targetFolder = @"C:\Users\Simbiosis\Desktop\Project\App2\App2\App2\Utilities\Models";
                if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);

                foreach (var f in files)
                {
                    try { File.Copy(f.Path, Path.Combine(targetFolder, f.Name), true); } catch { }
                }
                await RefreshLibraryList();
            }
        }

        private async void BtnShowHidden_Click(object sender, RoutedEventArgs e) { try { await PreviewWebView.ExecuteScriptAsync("window.showAllParts()"); } catch { } }
        private async void BtnTransferPoints_Click(object sender, RoutedEventArgs e) { /* Transfer kodu */ }
        private async void PointsList_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (PointsList.SelectedItem is TargetPoint p) await PreviewWebView.ExecuteScriptAsync($"window.focusPoint({p.RefX},{p.RefY},{p.RefZ})"); }

        private async Task ShowMessage(string title, string content)
        {
            var dialog = new ContentDialog { Title = title, Content = content, CloseButtonText = "Tamam", XamlRoot = this.Content.XamlRoot };
            await dialog.ShowAsync();
        }

        public class PointMessage { public string? type { get; set; } public PointCoords? data { get; set; } }
        public class PointCoords { public double x { get; set; } public double y { get; set; } public double z { get; set; } }
        public class PointCoordsData { public string? x { get; set; } public string? y { get; set; } public string? z { get; set; } }
    }
}
