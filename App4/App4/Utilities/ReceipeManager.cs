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
        // Reçete kayýt klasörü (Belgelerim/Simbiosis/Recipes)
        private static readonly string FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Simbiosis", "Recipes");

        // --- REÇETE YÖNETÝMÝ ---

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
                catch (Exception ex) { Debug.WriteLine($"Reçete yükleme hatasý: {ex.Message}"); }
            }
            return list;
        }

        public static void DeleteRecipe(string recipeName)
        {
            string fullPath = Path.Combine(FolderPath, $"{recipeName}.json");
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }

        // --- MODEL KÜTÜPHANESÝ YÖNETÝMÝ ---

        /// <summary>
        /// Utilities/Models klasöründeki .glb dosyalarýný tarar ve kütüphaneye ekler.
        /// </summary>
        public static async Task RefreshModelLibraryAsync()
        {
            string modelsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utilities", "Models");

            // Klasör yoksa oluþtur
            if (!Directory.Exists(modelsPath))
            {
                Directory.CreateDirectory(modelsPath);
                return;
            }

            // Listeyi her taramada temizle ki mükerrer veya silinmiþ kayýtlar kalmasýn
            if (GlobalSettings.AppState.ModelLibrary == null)
                GlobalSettings.AppState.ModelLibrary = new ObservableCollection<ModelLibraryItem>();

            GlobalSettings.AppState.ModelLibrary.Clear();

            // Sadece .glb dosyalarýný bul (Alt klasörler dahil)
            var glbFiles = Directory.GetFiles(modelsPath, "*.glb", SearchOption.AllDirectories);

            foreach (var filePath in glbFiles)
            {
                RegisterToLibrary(filePath);
            }

            await Task.CompletedTask; // Async yapýsýný bozmamak için
        }

        private static void RegisterToLibrary(string glbPath)
        {
            var modelName = Path.GetFileNameWithoutExtension(glbPath);

            // Mükerrer kontrolü yap ve listeye ekle
            if (!GlobalSettings.AppState.ModelLibrary.Any(m => m.ModelName == modelName))
            {
                GlobalSettings.AppState.ModelLibrary.Add(new ModelLibraryItem
                {
                    ModelName = modelName,
                    FilePath = glbPath,
                    IsConverted = true // Manuel yüklendiði için hazýr kabul ediyoruz
                });
            }
        }
    }
}
