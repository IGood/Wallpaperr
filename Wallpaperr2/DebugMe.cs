namespace Wallpaperr2
{
    using System;
    using Goop.Wpf;

    public class DebugMe : ValueConverterExtension<DebugMe>
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}
