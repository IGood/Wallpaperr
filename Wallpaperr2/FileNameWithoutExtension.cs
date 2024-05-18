namespace Wallpaperr2
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Data;
    using Goop.Wpf;

    [ValueConversion(typeof(string), typeof(string))]
    public class FileNameWithoutExtension : ValueConverterExtension<FileNameWithoutExtension>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string filePath)
            {
                return Path.GetFileNameWithoutExtension(filePath);
            }

            return DependencyProperty.UnsetValue;
        }
    }
}
