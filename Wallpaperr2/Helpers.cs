namespace Wallpaperr2
{
    using System;
    using System.IO;
    using System.Windows;

    internal static class Helpers
    {
        public static readonly string AppDataPath = System.Windows.Forms.Application.UserAppDataPath;

        // list of supported image types
        public static readonly string[] FileTypes =
        {
            "*.bmp",
            "*.png",
            "*.jpg",
            "*.jpeg",
            "*.gif",
        };

        public static readonly string ImageFilesFilter;

        public static readonly Random RNG = new Random();

        static Helpers()
        {
            string patterns = string.Join("; ", FileTypes);
            ImageFilesFilter = $"Images ({patterns})|{patterns}|All Files (*.*)|*.*";
        }

        public static void ShowError(string message, string title = "Wallpaperr Error")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static void ShowInfo(string message)
        {
            MessageBox.Show(message, "Hold It!", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void ShowBusy()
        {
            ShowInfo(
@"Wallpaperr is already busy composing
a background. Try again later.");
        }

        /// <summary>
        /// Returns <c>true</c> if the file system object exists.
        /// </summary>
        public static bool Exists(FileSystemInfo fileSystemInfo)
        {
            fileSystemInfo.Refresh();
            return fileSystemInfo.Exists;
        }
    }
}
