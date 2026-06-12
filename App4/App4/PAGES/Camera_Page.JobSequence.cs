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

        // ═══ REÇETE DÜZENLEME YETKİSİ ═══
        // Operatör reçete kartlarını GÖRÜR; düzenleme (taşı/sil/ekle/değer) yalnız admin.
        private bool RecipeEditAllowed()
        {
            if (App4.Utilities.GlobalData.IsAdminUnlocked) return true;
            try { AddLog("⚠ Reçete düzenleme admin yetkisi gerektirir (PIN ile giriş yapın)."); } catch { }
            return false;
        }

        // Job Yukari Tasima
        private void MoveJobUp(object sender, RoutedEventArgs e)
        {
            if (!RecipeEditAllowed()) return;
            var btn = sender as Button;
            if (btn == null) return;

            int currentIndex = GetIndexFromTag(btn.Tag);
            if (currentIndex < 0) return;

            var rfidDef = FindParentRfidDef(btn);
            if (rfidDef != null && currentIndex > 0 && currentIndex < rfidDef.JobSequence.Count)
            {
                // ═══ ÖNCE paralel koleksiyonlari tasi (RefreshIndexedJobs tetiklenmeden) ═══
                if (currentIndex < rfidDef.SnifferDurations.Count)
                {
                    var dur = rfidDef.SnifferDurations[currentIndex];
                    rfidDef.SnifferDurations.RemoveAt(currentIndex);
                    rfidDef.SnifferDurations.Insert(currentIndex - 1, dur);
                }
                if (currentIndex < rfidDef.DeviationLimits.Count)
                {
                    var lim = rfidDef.DeviationLimits[currentIndex];
                    rfidDef.DeviationLimits.RemoveAt(currentIndex);
                    rfidDef.DeviationLimits.Insert(currentIndex - 1, lim);
                }
                if (currentIndex < rfidDef.DataSourceModes.Count)
                {
                    var mode = rfidDef.DataSourceModes[currentIndex];
                    rfidDef.DataSourceModes.RemoveAt(currentIndex);
                    rfidDef.DataSourceModes.Insert(currentIndex - 1, mode);
                }
                // ═══ SONRA JobSequence tasi (RefreshIndexedJobs dogru verilerle calisir) ═══
                var jobName = rfidDef.JobSequence[currentIndex];
                rfidDef.JobSequence.RemoveAt(currentIndex);
                rfidDef.JobSequence.Insert(currentIndex - 1, jobName);
                App4.Utilities.GlobalData.SaveRfids();
            }
        }

        // Job Asagi Tasima
        private void MoveJobDown(object sender, RoutedEventArgs e)
        {
            if (!RecipeEditAllowed()) return;
            var btn = sender as Button;
            if (btn == null) return;

            int currentIndex = GetIndexFromTag(btn.Tag);
            if (currentIndex < 0) return;

            var rfidDef = FindParentRfidDef(btn);
            if (rfidDef != null && currentIndex < rfidDef.JobSequence.Count - 1)
            {
                // ═══ ÖNCE paralel koleksiyonlari tasi (RefreshIndexedJobs tetiklenmeden) ═══
                if (currentIndex < rfidDef.SnifferDurations.Count)
                {
                    var dur = rfidDef.SnifferDurations[currentIndex];
                    rfidDef.SnifferDurations.RemoveAt(currentIndex);
                    rfidDef.SnifferDurations.Insert(currentIndex + 1, dur);
                }
                if (currentIndex < rfidDef.DeviationLimits.Count)
                {
                    var lim = rfidDef.DeviationLimits[currentIndex];
                    rfidDef.DeviationLimits.RemoveAt(currentIndex);
                    rfidDef.DeviationLimits.Insert(currentIndex + 1, lim);
                }
                if (currentIndex < rfidDef.DataSourceModes.Count)
                {
                    var mode = rfidDef.DataSourceModes[currentIndex];
                    rfidDef.DataSourceModes.RemoveAt(currentIndex);
                    rfidDef.DataSourceModes.Insert(currentIndex + 1, mode);
                }
                // ═══ SONRA JobSequence tasi (RefreshIndexedJobs dogru verilerle calisir) ═══
                var jobName = rfidDef.JobSequence[currentIndex];
                rfidDef.JobSequence.RemoveAt(currentIndex);
                rfidDef.JobSequence.Insert(currentIndex + 1, jobName);
                App4.Utilities.GlobalData.SaveRfids();
            }
        }

        // Job Silme
        private void RemoveJobFromSequence(object sender, RoutedEventArgs e)
        {
            if (!RecipeEditAllowed()) return;
            var btn = sender as Button;
            var jobName = btn?.Tag as string;

            if (btn != null && !string.IsNullOrEmpty(jobName))
            {
                var rfidDef = FindParentRfidDef(btn);
                if (rfidDef != null && rfidDef.JobSequence.Contains(jobName))
                {
                    int idx = rfidDef.JobSequence.IndexOf(jobName);
                    // ═══ ÖNCE paralel koleksiyonlari sil (RefreshIndexedJobs tetiklenmeden) ═══
                    if (idx >= 0 && idx < rfidDef.SnifferDurations.Count)
                        rfidDef.SnifferDurations.RemoveAt(idx);
                    if (idx >= 0 && idx < rfidDef.DeviationLimits.Count)
                        rfidDef.DeviationLimits.RemoveAt(idx);
                    if (idx >= 0 && idx < rfidDef.DataSourceModes.Count)
                        rfidDef.DataSourceModes.RemoveAt(idx);
                    // ═══ SONRA JobSequence'dan sil (RefreshIndexedJobs dogru verilerle calisir) ═══
                    rfidDef.JobSequence.Remove(jobName);
                    App4.Utilities.GlobalData.SaveRfids();
                }
            }
        }

        // ComboBox yüklendiğinde kaydedilmiş değeri seç (x:Bind SelectedItem DataTemplate'de güvenilir değil)
        private void DataSourceModeComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox cb && cb.DataContext is App4.Utilities.IndexedJobItem jobItem)
            {
                // Items içinden eşleşen string'i bul ve seç
                foreach (var item in cb.Items)
                {
                    if (item is string s && s == jobItem.DataSourceMode)
                    {
                        cb.SelectedItem = item;
                        return;
                    }
                }
                // Eşleşme yoksa ilk item'ı seç (SENSOR)
                if (cb.Items.Count > 0) cb.SelectedIndex = 0;
            }
        }

        // Ölçüm Yöntemi Değiştiğinde Kaydet
        private void DataSourceMode_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb && cb.DataContext is App4.Utilities.IndexedJobItem jobItem)
            {
                string newMode = cb.SelectedItem as string;
                if (string.IsNullOrEmpty(newMode)) return;
                if (newMode == jobItem.DataSourceMode) return; // Değişiklik yoksa (Loaded tetiklemesi)

                // Operatör: seçimi geri al, kaydetme (görüntüleme serbest, düzenleme admin)
                if (!RecipeEditAllowed()) { DataSourceModeComboBox_Loaded(cb, null); return; }

                var rfid = FindParentRfidDef(cb);
                if (rfid != null && jobItem.Index < rfid.DataSourceModes.Count)
                {
                    rfid.DataSourceModes[jobItem.Index] = newMode;
                    jobItem.DataSourceMode = newMode;
                    App4.Utilities.GlobalData.SaveRfids();
                }
            }
        }

        // Sniffer Suresi Degistiginde Kaydet + Aktif ise robota/Auto sayfaya propagate et
        private void SnifferDuration_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (sender.DataContext is App4.Utilities.IndexedJobItem jobItem)
            {
                // Operatör: değeri geri al, kaydetme (düzenleme admin)
                if (!App4.Utilities.GlobalData.IsAdminUnlocked)
                {
                    double nv = double.IsNaN(args.NewValue) ? 0.0 : args.NewValue;
                    if (Math.Abs(nv - jobItem.SnifferDuration) > 0.0001) { RecipeEditAllowed(); sender.Value = jobItem.SnifferDuration; }
                    return;
                }

                var rfid = FindParentRfidDef(sender);
                if (rfid != null && jobItem.Index < rfid.SnifferDurations.Count)
                {
                    double newVal = double.IsNaN(args.NewValue) ? 0.0 : args.NewValue;
                    rfid.SnifferDurations[jobItem.Index] = newVal;
                    jobItem.SnifferDuration = newVal;
                    App4.Utilities.GlobalData.SaveRfids();

                    // ═══ AKTİF KART + AKTİF JOB İSE HEMEN ROBOTA/AUTO SAYFAYA PROPAGATE ET ═══
                    PropagateIfActiveJob(rfid, jobItem.Index);
                }
            }
        }

        // Nokta Sapma Limiti Degistiginde Kaydet + Aktif ise robota/Auto sayfaya propagate et
        private void DeviationLimit_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (sender.DataContext is App4.Utilities.IndexedJobItem jobItem)
            {
                // Operatör: değeri geri al, kaydetme (düzenleme admin)
                if (!App4.Utilities.GlobalData.IsAdminUnlocked)
                {
                    double nv = double.IsNaN(args.NewValue) ? 50.0 : args.NewValue;
                    if (Math.Abs(nv - jobItem.DeviationLimit) > 0.0001) { RecipeEditAllowed(); sender.Value = jobItem.DeviationLimit; }
                    return;
                }

                var rfid = FindParentRfidDef(sender);
                if (rfid != null && jobItem.Index < rfid.DeviationLimits.Count)
                {
                    double newVal = double.IsNaN(args.NewValue) ? 50.0 : args.NewValue;
                    rfid.DeviationLimits[jobItem.Index] = newVal;
                    jobItem.DeviationLimit = newVal;
                    App4.Utilities.GlobalData.SaveRfids();

                    // ═══ AKTİF KART + AKTİF JOB İSE HEMEN ROBOTA/AUTO SAYFAYA PROPAGATE ET ═══
                    PropagateIfActiveJob(rfid, jobItem.Index);
                }
            }
        }

        /// <summary>
        /// Değiştirilen Sniffer/Sapma değeri aktif kartın aktif job'una ait ise
        /// hemen GlobalData üzerinden robota ve Auto sayfasına propagate eder.
        /// </summary>
        private void PropagateIfActiveJob(App4.Utilities.RfidDef rfid, int changedIndex)
        {
            try
            {
                // Bu kart aktif kart mı?
                bool isActiveCard = string.Equals(
                    rfid.Id,
                    App4.Utilities.GlobalData.AktuelRfid,
                    StringComparison.OrdinalIgnoreCase);

                if (!isActiveCard) return;

                // Değiştirilen index aktif job index'i mi?
                if (changedIndex == rfid.CurrentJobIndex)
                {
                    // Sniffer + DeviationLimit output'larını güncelle ve robota yaz
                    App4.Utilities.GlobalData.UpdateCurrentJobIndex(rfid.CurrentJobIndex);
                }
            }
            catch { /* sessiz */ }
        }
    }
}
