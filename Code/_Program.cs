[assembly: System.Runtime.InteropServices.Guid("3a5e2d31-3b81-4353-949c-427d9698a88a")]

namespace Wallpaperr
{
	using System;
	using System.Linq;
	using System.Windows.Forms;

	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			string file = args?.FirstOrDefault();

			HighDpiMode highDpiMode = HighDpiMode.PerMonitorV2;
#if DEBUG
			Application.SetHighDpiMode(highDpiMode);
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm(file));
#else
			if (!SingleInstance.Start())
			{
				if (string.IsNullOrEmpty(file))
				{
					SingleInstance.ShowFirstInstance();
				}
				else
				{
					int targetHWnd = SingleInstance.WinAPI.FindWindow(null, Properties.Resources.AppTitle);
					if (targetHWnd != 0)
					{
						SingleInstance.SendStringMessage((IntPtr)targetHWnd, file);
					}
				}
				return;
			}

			Application.SetHighDpiMode(highDpiMode );
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			try
			{
				Application.Run(new MainForm(file));
			}
			catch (Exception e)
			{
				for (var inner = e; inner != null; inner = inner.InnerException)
				{
					MessageBox.Show(
						$"{inner.Message}{Environment.NewLine}{inner.StackTrace}",
						"Wallpaperr Error",
						MessageBoxButtons.OK,
						MessageBoxIcon.Error);
				}
			}

			SingleInstance.Stop();
#endif
		}
	}
}
