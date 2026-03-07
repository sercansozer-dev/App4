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
                // JobSequence tasima
                var jobName = rfidDef.JobSequence[currentIndex];
                rfidDef.JobSequence.RemoveAt(currentIndex);
                rfidDef.JobSequence.Insert(currentIndex - 1, jobName);
                // SnifferDurations paralel tasima
                if (currentIndex < rfidDef.SnifferDurations.Count)
                {
                    var dur = rfidDef.SnifferDurations[currentIndex];
                    rfidDef.SnifferDurations.RemoveAt(currentIndex);
                    rfidDef.SnifferDurations.Insert(currentIndex - 1, dur);
                }
                // DeviationLimits paralel tasima
                if (currentIndex < rfidDef.DeviationLimits.Count)
                {
                    var lim = rfidDef.DeviationLimits[currentIndex];
                    rfidDef.DeviationLimits.RemoveAt(currentIndex);
                    rfidDef.DeviationLimits.Insert(currentIndex - 1, lim);
                }
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
                // JobSequence tasima
                var jobName = rfidDef.JobSequence[currentIndex];
                rfidDef.JobSequence.RemoveAt(currentIndex);
                rfidDef.JobSequence.Insert(currentIndex + 1, jobName);
                // SnifferDurations paralel tasima
                if (currentIndex < rfidDef.SnifferDurations.Count)
                {
                    var dur = rfidDef.SnifferDurations[currentIndex];
                    rfidDef.SnifferDurations.RemoveAt(currentIndex);
                    rfidDef.SnifferDurations.Insert(currentIndex + 1, dur);
                }
                // DeviationLimits paralel tasima
                if (currentIndex < rfidDef.DeviationLimits.Count)
                {
                    var lim = rfidDef.DeviationLimits[currentIndex];
                    rfidDef.DeviationLimits.RemoveAt(currentIndex);
                    rfidDef.DeviationLimits.Insert(currentIndex + 1, lim);
                }
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
                    int idx = rfidDef.JobSequence.IndexOf(jobName);
                    rfidDef.JobSequence.Remove(jobName);
                    // SnifferDurations paralel silme
                    if (idx >= 0 && idx < rfidDef.SnifferDurations.Count)
                        rfidDef.SnifferDurations.RemoveAt(idx);
                    // DeviationLimits paralel silme
                    if (idx >= 0 && idx < rfidDef.DeviationLimits.Count)
                        rfidDef.DeviationLimits.RemoveAt(idx);
                    App4.Utilities.GlobalData.SaveRfids();
                }
            }
        }

        // Sniffer Suresi Degistiginde Kaydet
        private void SnifferDuration_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (sender.DataContext is App4.Utilities.IndexedJobItem jobItem)
            {
                var rfid = FindParentRfidDef(sender);
                if (rfid != null && jobItem.Index < rfid.SnifferDurations.Count)
                {
                    double newVal = double.IsNaN(args.NewValue) ? 0.0 : args.NewValue;
                    rfid.SnifferDurations[jobItem.Index] = newVal;
                    jobItem.SnifferDuration = newVal;
                    App4.Utilities.GlobalData.SaveRfids();
                }
            }
        }

        // Nokta Sapma Limiti Degistiginde Kaydet
        private void DeviationLimit_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (sender.DataContext is App4.Utilities.IndexedJobItem jobItem)
            {
                var rfid = FindParentRfidDef(sender);
                if (rfid != null && jobItem.Index < rfid.DeviationLimits.Count)
                {
                    double newVal = double.IsNaN(args.NewValue) ? 50.0 : args.NewValue;
                    rfid.DeviationLimits[jobItem.Index] = newVal;
                    jobItem.DeviationLimit = newVal;
                    App4.Utilities.GlobalData.SaveRfids();
                }
            }
        }
    }
}
