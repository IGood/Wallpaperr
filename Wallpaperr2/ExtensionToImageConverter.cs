namespace Wallpaperr2
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using Goop.Wpf;

    [ValueConversion(typeof(string), typeof(string))]
    public class ExtensionToImageConverter : ValueConverterExtension<ExtensionToImageConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value as string)
            {
                case ".bmp":
                    return "Images/picture_BMP.ico";

                case ".gif":
                    return "Images/picture_GIF.ico";

                case ".jpg":
                case ".jpeg":
                    return "Images/picture_JPG.ico";

                case ".png":
                    return "Images/picture_PNG.ico";

                default:
                    return "Images/folder_pictures.ico";
            }
        }
    }
}
