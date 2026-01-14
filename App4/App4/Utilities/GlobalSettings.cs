using App4.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App4.Utilities
{
    public class GlobalSettings
    {
        public static class AppState
        {
            public static ProductRecipe? ActiveRecipe { get; set; }
            public static ObservableCollection<ModelLibraryItem> ModelLibrary { get; set; } = new ObservableCollection<ModelLibraryItem>();
        }
    }
}
