using System;
using Microsoft.UI.Xaml.Data;

namespace App4.PAGES
{
    // Bu sınıfın "public" olması şarttır, yoksa XAML göremez.
    public class BoolToPlayPauseIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // Eğer işlem çalışıyorsa (True) -> Pause İkonu
            if (value is bool isRunning && isRunning)
            {
                return "\uE769";
            }
            // Çalışmıyorsa (False) -> Play İkonu
            return "\uE768";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}