using App4.Models;
using App4.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace App4.Utilities
{
    public static class RecipeManager
    {
        // Re�ete kay�t klas�r� (Belgelerim/Simbiosis/Recipes)
        private static readonly string FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Simbiosis", "Recipes");

        // --- RE�ETE Y�NET�M� ---

        public static async Task SaveRecipeAsync(ProductRecipe recipe)
        {
            if (!Directory.Exists(FolderPath)) Directory.CreateDirectory(FolderPath);

            string fileName = $"{recipe.RecipeName}.json";
            string fullPath = Path.Combine(FolderPath, fileName);

            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(recipe, options);

            await File.WriteAllTextAsync(fullPath, jsonString);
        }

        public static async Task<List<ProductRecipe>> LoadAllRecipesAsync()
        {
            var list = new List<ProductRecipe>();
            if (!Directory.Exists(FolderPath)) return list;

            string[] files = Directory.GetFiles(FolderPath, "*.json");
            foreach (var file in files)
            {
                try
                {
                    string jsonString = await File.ReadAllTextAsync(file);
                    var recipe = JsonSerializer.Deserialize<ProductRecipe>(jsonString);
                    if (recipe != null) list.Add(recipe);
                }
                catch (Exception ex) { Debug.WriteLine($"Re�ete y�kleme hatas�: {ex.Message}"); }
            }
            return list;
        }

        public static void DeleteRecipe(string recipeName)
        {
            string fullPath = Path.Combine(FolderPath, $"{recipeName}.json");
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }

        // --- MODEL K�T�PHANES� Y�NET�M� ---

        /// <summary>
        /// Utilities/Models klas�r�ndeki .glb dosyalar�n� tarar ve k�t�phaneye ekler.
        /// </summary>
        public static async Task RefreshModelLibraryAsync()
        {
            // Yazılabilir kullanıcı klasörü (MSIX kurulumunda install dir read-only olduğu için)
            string modelsPath = ModelsPathHelper.GetModelsFolder();

            // GetModelsFolder zaten klasörü oluşturur; yine de güvence:
            if (!Directory.Exists(modelsPath))
            {
                Directory.CreateDirectory(modelsPath);
                return;
            }

            // Listeyi her taramada temizle ki m�kerrer veya silinmi� kay�tlar kalmas�n
            if (GlobalSettings.AppState.ModelLibrary == null)
                GlobalSettings.AppState.ModelLibrary = new ObservableCollection<ModelLibraryItem>();

            GlobalSettings.AppState.ModelLibrary.Clear();

            // Sadece .glb dosyalar�n� bul (Alt klas�rler dahil)
            var glbFiles = Directory.GetFiles(modelsPath, "*.glb", SearchOption.AllDirectories);

            foreach (var filePath in glbFiles)
            {
                RegisterToLibrary(filePath);
            }

            await Task.CompletedTask; // Async yap�s�n� bozmamak i�in
        }

        private static void RegisterToLibrary(string glbPath)
        {
            var modelName = Path.GetFileNameWithoutExtension(glbPath);

            // M�kerrer kontrol� yap ve listeye ekle
            if (!GlobalSettings.AppState.ModelLibrary.Any(m => m.ModelName == modelName))
            {
                GlobalSettings.AppState.ModelLibrary.Add(new ModelLibraryItem
                {
                    ModelName = modelName,
                    FilePath = glbPath,
                    IsConverted = true // Manuel y�klendi�i i�in haz�r kabul ediyoruz
                });
            }
        }
    }
}
