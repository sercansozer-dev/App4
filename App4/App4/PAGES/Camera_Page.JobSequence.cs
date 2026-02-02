using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;

namespace App4.PAGES
{
    // Partial class - Job Sequence islemleri icin
    public sealed partial class Camera_Page
    {
        // Yardimci metot: VisualTree'den parent RfidDef'i bul
        private App4.Utilities.RfidDef FindParentRfidDef(DependencyObject element)
        {
            var parent = VisualTreeHelper.GetParent(element);
            while (parent != null)
            {
                if (parent is FrameworkElement fe && fe.DataContext is App4.Utilities.RfidDef rfidDef)
                    return rfidDef;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        // Tag'dan index degerini al (int, string veya baska tiplerden)
        private int GetIndexFromTag(object tag)
        {
            if (tag == null) return -1;
            
            // Direkt int ise
            if (tag is int intVal) return intVal;
            
            // String ise parse et
            if (tag is string strVal && int.TryParse(strVal, out int parsed))
                return parsed;
            
            // Baska tipler icin Convert dene
            try
            {
                return Convert.ToInt32(tag);
            }
            catch
            {
                return -1;
            }
        }

        // Job Yukari Tasima
        private void MoveJobUp(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            int currentIndex = GetIndexFromTag(btn.Tag);
            if (currentIndex < 0) return;

            var rfidDef = FindParentRfidDef(btn);
            if (rfidDef != null && currentIndex > 0 && currentIndex < rfidDef.JobSequence.Count)
            {
                var jobName = rfidDef.JobSequence[currentIndex];
                rfidDef.JobSequence.RemoveAt(currentIndex);
                rfidDef.JobSequence.Insert(currentIndex - 1, jobName);
                App4.Utilities.GlobalData.SaveRfids();
            }
        }

        // Job Asagi Tasima
        private void MoveJobDown(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            int currentIndex = GetIndexFromTag(btn.Tag);
            if (currentIndex < 0) return;

            var rfidDef = FindParentRfidDef(btn);
            if (rfidDef != null && currentIndex < rfidDef.JobSequence.Count - 1)
            {
                var jobName = rfidDef.JobSequence[currentIndex];
                rfidDef.JobSequence.RemoveAt(currentIndex);
                rfidDef.JobSequence.Insert(currentIndex + 1, jobName);
                App4.Utilities.GlobalData.SaveRfids();
            }
        }

        // Job Silme
        private void RemoveJobFromSequence(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var jobName = btn?.Tag as string;

            if (btn != null && !string.IsNullOrEmpty(jobName))
            {
                var rfidDef = FindParentRfidDef(btn);
                if (rfidDef != null && rfidDef.JobSequence.Contains(jobName))
                {
                    rfidDef.JobSequence.Remove(jobName);
                    App4.Utilities.GlobalData.SaveRfids();
                }
            }
        }
    }
}
