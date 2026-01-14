using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace App4.Models
{
    public class ProductRecipe
    {
        public string RecipeName { get; set; } = "Yeni Model";
        public int PlcModelCode { get; set; } = 0;
        public string GocatorJobName { get; set; } = "Default.job";
        public string StepFilePath { get; set; } = "";
        public ObservableCollection<TargetPoint> TargetPoints { get; set; } = new ObservableCollection<TargetPoint>();
        public DateTime LastModified { get; set; } = DateTime.Now;
    }

    public class TargetPoint
    {
        public string PointName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double RefX { get; set; }
        public double RefY { get; set; }
        public double RefZ { get; set; }
        public double RefA { get; set; }
        public double WaitTime { get; set; } = 2.0;
        public double Speed { get; set; } = 50.0;
    }

    public class ModelLibraryItem
    {
        public string ModelName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public bool IsConverted { get; set; }
    }
}
