namespace Wallpaperr
{
	using System;
	using System.Windows.Forms;

	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			string file = null;
			if (args != null && args.Length > 0)
			{
				file = args[0];
			}

#if DEBUG
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm(file));
#else
			if (!SingleInstance.Start())
			{
				if (String.IsNullOrEmpty(file))
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
