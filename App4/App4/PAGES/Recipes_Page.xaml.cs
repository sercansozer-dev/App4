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
                
                // Allow cross-origin (localui -> localmodels)
                var options = new CoreWebView2EnvironmentOptions { AdditionalBrowserArguments = "--disable-web-security --disable-features=IsolateOrigins,site-per-process" };
                var env = await CoreWebView2Environment.CreateWithOptionsAsync(null, userDataFolder, options);
                
                await PreviewWebView.EnsureCoreWebView2Async(env);

                // A. HTML Klasörü (Temp)
                string htmlFolder = Path.Combine(Path.GetTempPath(), "Simbiosis_HTML");
                if (!Directory.Exists(htmlFolder)) Directory.CreateDirectory(htmlFolder);

                // B. Modeller Klasörü (App BaseDir)
                string modelsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utilities", "Models");
                if (!Directory.Exists(modelsFolder)) Directory.CreateDirectory(modelsFolder);

                // HTML Dosyasýný Oluþtur (Temp içine)
                await CreateRecipeViewerHtml(htmlFolder, "Recipe_Viewer.html", "Recipe 3D Preview");

                // Mappings
                try
                {
                    PreviewWebView.CoreWebView2.SetVirtualHostNameToFolderMapping("localui", htmlFolder, CoreWebView2HostResourceAccessKind.Allow);
                    PreviewWebView.CoreWebView2.SetVirtualHostNameToFolderMapping("localmodels", modelsFolder, CoreWebView2HostResourceAccessKind.Allow);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Virtual host mapping error: {ex.Message}");
                }

                PreviewWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                PreviewWebView.NavigationCompleted += PreviewWebView_NavigationCompleted;

                // Load HTML using file:// protocol
                string recipeHtmlPath = Path.Combine(htmlFolder, "Recipe_Viewer.html");
                PreviewWebView.Source = new Uri($"file:///{recipeHtmlPath.Replace("\\", "/")}");

                await RefreshLibraryList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"!!! [RECIPES KRÝTÝK HATA] : {ex.Message}");
            }
        }

        private async Task CreateRecipeViewerHtml(string folder, string fileName, string title)
        {
            try
            {
                // Dark theme viewer with GLTFLoader
                string htmlContent = $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{title}</title>
    <style> 
        body {{ margin: 0; overflow: hidden; background: #1e1e1e; color: white; font-family: sans-serif; }} 
        canvas {{ display: block; width: 100vw; height: 100vh; outline: none; }} 
        #loading {{ position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%); color: #3498db; font-size: 20px; background: rgba(0,0,0,0.8); padding: 20px; border-radius: 8px; display: none; z-index: 100; }}
        #error {{ position: absolute; bottom: 20px; left: 20px; background: rgba(220, 53, 69, 0.9); color: white; padding: 10px; border-radius: 4px; font-size: 12px; display: none; z-index: 100; }}
        #info {{ position: absolute; top: 10px; left: 10px; font-size: 10px; color: #888; pointer-events: none; }}
        #debug {{ position: absolute; top: 10px; right: 10px; font-size: 10px; color: #666; background: rgba(0,0,0,0.5); padding: 5px 10px; border-radius: 4px; }}
    </style>
    <script src='https://cdnjs.cloudflare.com/ajax/libs/three.js/r128/three.min.js'></script>
    <script src='https://cdn.jsdelivr.net/npm/three@0.128.0/examples/js/controls/OrbitControls.js'></script>
    <script src='https://cdn.jsdelivr.net/npm/three@0.128.0/examples/js/loaders/GLTFLoader.js'></script>
</head>
<body>
    <div id='info'>Recipe Viewer</div>
    <div id='debug'></div>
    <div id='loading'>Yükleniyor...</div>
    <div id='error'></div>
    <script>
        let scene, camera, renderer, controls, currentModel;
        let THREE_LOADED = false;
        const loadingEl = document.getElementById('loading');
        const errorEl = document.getElementById('error');
        const debugEl = document.getElementById('debug');

        function log(msg) {{
            console.log('[RecipeViewer] ' + msg);
            debugEl.textContent = msg;
        }}

        function showError(msg) {{ 
            errorEl.textContent = '? ' + msg; 
            errorEl.style.display = 'block'; 
            console.error('[RecipeViewer] ' + msg);
        }}
        
        function showLoading(v) {{ loadingEl.style.display = v ? 'block' : 'none'; }}

        // Wait for THREE.js to load
        function waitForTHREE(callback, attempt = 0) {{
            if (typeof THREE !== 'undefined' && 
                typeof THREE.OrbitControls !== 'undefined' && 
                typeof THREE.GLTFLoader !== 'undefined') {{
                THREE_LOADED = true;
                log('THREE.js loaded');
                callback();
            }} else if (attempt < 100) {{
                setTimeout(() => waitForTHREE(callback, attempt + 1), 100);
            }} else {{
                showError('THREE.js kütüphaneleri yüklenemedi');
            }}
        }}

        function init() {{
            try {{
                scene = new THREE.Scene();
                scene.background = new THREE.Color(0x181818);
                
                // Isometric view camera setup
                const width = window.innerWidth;
                const height = window.innerHeight;
                const distance = 150;
                const aspect = width / height;
                
                // Orthographic camera for isometric view - properly scaled
                const frustumSize = 200;
                camera = new THREE.OrthographicCamera(
                    -frustumSize * aspect / 2,
                    frustumSize * aspect / 2,
                    frustumSize / 2,
                    -frustumSize / 2,
                    0.1,
                    10000
                );
                
                // Isometric position
                camera.position.set(distance * 0.866, distance * 0.866, distance * 0.866);
                camera.lookAt(0, 0, 0);
                
                renderer = new THREE.WebGLRenderer({{ antialias: true, alpha: true }});
                renderer.setSize(window.innerWidth, window.innerHeight);
                renderer.setPixelRatio(window.devicePixelRatio);
                document.body.appendChild(renderer.domElement);
                
                if (typeof THREE.OrbitControls !== 'undefined') {{
                    controls = new THREE.OrbitControls(camera, renderer.domElement);
                    controls.enableDamping = false;
                    controls.autoRotate = false;
                    controls.enableRotate = false;  // Disable all rotation
                    controls.enableZoom = true;     // Allow zoom only
                    controls.enablePan = false;     // Disable pan
                    controls.target.set(0, 0, 0);
                    controls.update();
                }}
                
                const ambi = new THREE.AmbientLight(0xffffff, 0.7); 
                scene.add(ambi);
                const dir = new THREE.DirectionalLight(0xffffff, 0.8); 
                dir.position.set(distance * 0.866, distance * 1.5, distance * 0.866); 
                scene.add(dir);
                const dir2 = new THREE.DirectionalLight(0xffffff, 0.5); 
                dir2.position.set(-distance * 0.866, -distance * 0.5, -distance * 0.866); 
                scene.add(dir2);

                // Ground plane reference
                const groundGeometry = new THREE.PlaneGeometry(500, 500);
                const groundMaterial = new THREE.MeshStandardMaterial({{ 
                    color: 0x333333,
                    emissive: 0x1a1a1a
                }});
                const groundPlane = new THREE.Mesh(groundGeometry, groundMaterial);
                groundPlane.rotation.x = -Math.PI / 2;
                groundPlane.position.y = -0.1;
                groundPlane.receiveShadow = true;
                scene.add(groundPlane);
                
                window.addEventListener('resize', () => {{
                    const newWidth = window.innerWidth;
                    const newHeight = window.innerHeight;
                    const newAspect = newWidth / newHeight;
                    const frustumSize = 200;
                    
                    camera.left = -frustumSize * newAspect / 2;
                    camera.right = frustumSize * newAspect / 2;
                    camera.top = frustumSize / 2;
                    camera.bottom = -frustumSize / 2;
                    camera.updateProjectionMatrix();
                    
                    renderer.setSize(newWidth, newHeight);
                }});
                
                log('Scene initialized (Isometric)');
                animate();
            }} catch(e) {{ 
                showError('Init Error: ' + e.message); 
            }}
        }}
        
        function loadModel(url) {{
            if(!url) {{
                log('No URL provided');
                return;
            }}
            
            log('Loading: ' + url);
            showLoading(true);
            errorEl.style.display = 'none';
            
            if(currentModel) {{ 
                scene.remove(currentModel); 
                currentModel = null; 
            }}
            
            const loader = new THREE.GLTFLoader();
            loader.load(url, (gltf) => {{
                try {{
                    const model = gltf.scene;
                    currentModel = model;
                    scene.add(model);
                    
                    // Process materials
                    model.traverse((child) => {{
                        if (child.isMesh) {{
                            child.castShadow = true;
                            child.receiveShadow = true;
                        }}
                    }});
                    
                    // Reset model position first
                    model.position.set(0, 0, 0);
                    
                    // Calculate bounding box
                    const box = new THREE.Box3().setFromObject(model);
                    const center = box.getCenter(new THREE.Vector3());
                    const size = box.getSize(new THREE.Vector3());
                    
                    // Position model to sit on ground
                    // Move down so bottom of model is at Y=0
                    model.position.y = -box.min.y;
                    // Center horizontally
                    model.position.x = -center.x;
                    model.position.z = -center.z;
                    
                    // Klima üniteleri dike almak için (X ekseni etrafýnda 90 derece)
                    model.rotation.x = Math.PI / 2;
                    
                    // Y ekseninde aynalamak (mirror)
                    model.scale.y = -1;

                    if(controls) {{ controls.target.set(0, 0, 0); }}
                    showLoading(false);
                    log('Model loaded - on ground');
                }} catch(err) {{
                    showError('Processing: ' + err.message);
                    showLoading(false);
                }}
            }}, (progress) => {{
                const pct = Math.round((progress.loaded / progress.total) * 100);
                log('Loading... ' + pct + '%');
            }}, (err) => {{
                showError('Load Error: ' + err.message);
                showLoading(false);
            }});
        }}

        function animate() {{ 
            requestAnimationFrame(animate); 
            if(controls) controls.update(); 
            renderer.render(scene, camera); 
        }}
        
        // Start initialization
        waitForTHREE(() => {{
            init();
            window.loadModel = loadModel;
            log('Ready');
        }});
    </script>
</body>
</html>";
                string path = Path.Combine(folder, fileName);
                await File.WriteAllTextAsync(path, htmlContent);
                System.Diagnostics.Debug.WriteLine($">>> [RECIPES] HTML created: {path}");
            }
            catch (Exception ex)
            {
                 System.Diagnostics.Debug.WriteLine($"HTML Create Error: {ex.Message}");
            }
        }

        // --- HTML YÜKLENDIKÇE MODELI ÇAÐIR ---
        private async void PreviewWebView_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            if (args.IsSuccess && SelectedRecipe != null && !string.IsNullOrEmpty(SelectedRecipe.StepFilePath))
            {
                // Use full relative path instead of stripping directory
                string relativePath = SelectedRecipe.StepFilePath.Replace("\\", "/");
                await Update3DPreview(relativePath);
            }
        }

        // --- MODELI GÜNCELLE ---
        private async Task Update3DPreview(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) 
            {
                System.Diagnostics.Debug.WriteLine(">>> [3D PREVIEW] No filename provided");
                return;
            }
            
            try
            {
                if (PreviewWebView.CoreWebView2 == null) 
                {
                    System.Diagnostics.Debug.WriteLine(">>> [3D PREVIEW] WebView not ready");
                    return;
                }
                
                string modelsRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utilities", "Models");
                string fullPath = Path.Combine(modelsRoot, fileName.Replace("\\", "/"));
                
                System.Diagnostics.Debug.WriteLine($">>> [3D PREVIEW] Trying: {fullPath}");
                
                // Try exact path first
                if (!File.Exists(fullPath))
                {
                    System.Diagnostics.Debug.WriteLine($">>> [3D PREVIEW] Exact path not found, searching...");
                    // Search for file in Models folder
                    var foundFile = Directory.GetFiles(modelsRoot, Path.GetFileName(fileName), SearchOption.AllDirectories).FirstOrDefault();
                    if (foundFile != null)
                    {
                        fullPath = foundFile;
                        System.Diagnostics.Debug.WriteLine($">>> [3D PREVIEW] Found: {fullPath}");
                    }
                }



                if (File.Exists(fullPath))
                {
                    // Use localmodels:// virtual host mapping for WebView2
                    string relativePath = Path.GetRelativePath(modelsRoot, fullPath).Replace("\\", "/");
                    string modelUri = $"http://localmodels/{relativePath}";
                    string escapedUri = modelUri.Replace("'", "\\'");
                    string jsCode = $@"if(window.loadModel) {{ 
                        console.log('Calling loadModel with: {escapedUri}');
                        window.loadModel('{escapedUri}'); 
                    }} else {{ 
                        console.error('loadModel function not ready'); 
                    }}";
                    
                    System.Diagnostics.Debug.WriteLine($">>> [3D PREVIEW] Loading: {modelUri}");
                    await PreviewWebView.ExecuteScriptAsync(jsCode);
                    TxtPreviewName.Text = fileName;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($">>> [3D PREVIEW] File not found: {fullPath}");
                }
            }
            catch (Exception ex) 
            { 
                System.Diagnostics.Debug.WriteLine($">>> [3D PREVIEW] Error: {ex.Message}\n{ex.StackTrace}"); 
            }
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
                    // Use the stored path directly (relative path)
                    string relativePath = recipe.StepFilePath.Replace("\\", "/");
                    _ = Update3DPreview(relativePath);
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

        private async void LibraryModelList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LibraryModelList.SelectedItem is ModelLibraryItem selectedModel)
            {
                // Get relative path from Utilities/Models to the selected file
                string modelsRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utilities", "Models");
                string relativePath = Path.GetRelativePath(modelsRoot, selectedModel.FilePath).Replace("\\", "/");
                
                TxtStepPath.Text = relativePath;

                if (SelectedRecipe != null)
                    SelectedRecipe.StepFilePath = relativePath;

                _ = Update3DPreview(relativePath);
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
                // Doðru yol: Uygulama dizini içindeki Utilities/Models
                string targetFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utilities", "Models");
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
