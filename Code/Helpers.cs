using System;
using System.Windows.Forms;

namespace Wallpaperr
{
	using FileSystemInfo = System.IO.FileSystemInfo;

	static class Helpers
	{
		#region Const / Static Fields

		public static readonly string NL = Environment.NewLine;

		public static readonly string AppDataPath = Application.UserAppDataPath;

		// list of supported image types
		public static readonly string[] FileTypes = {
			"*.bmp",
			"*.png",
			"*.jpg",
			"*.jpeg",
			"*.gif" };

		public static readonly Random RNG = new Random();

		#endregion

		#region Message Boxes

		public static void ShowError(string message, string title)
		{
			MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		public static void ShowError(string message)
		{
			ShowError(message, "Wallpaperr Error");
		}

		public static void ShowInfo(string message)
		{
			MessageBox.Show(message, "Hold It!", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		public static void ShowBusy()
		{
			ShowInfo(
				"Wallpaperr is already busy composing" + NL +
				"a background. Try again later.");
		}

		#endregion

		// Returns true if the file system object exists.
		public static bool Exists(FileSystemInfo fileSystemInfo)
		{
			fileSystemInfo.Refresh();
			return fileSystemInfo.Exists;
		}

		// Searches a ListView's item collection for an entry that corresponds
		// to a specified folder/file.
		public static ListViewItem FindInListView(FileSystemInfo fsi, ListView listView)
		{
			foreach (ListViewItem item in listView.Items.Find(fsi.Name, false))
			{
				if (item.ToolTipText == fsi.FullName)
				{
					return item;
				}
			}
			return null;
		}
	}
}
