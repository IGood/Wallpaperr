using System;
using System.Runtime.InteropServices;

namespace Wallpaperr
{
	static class SingleInstance
	{
		#region Internal Classes

		public static class WinAPI
		{
			#region Internal Classes

			public struct CopyDataStruct
			{
				public IntPtr dwData;
				public int cbData;
				public IntPtr lpData;
			}

			#endregion

			#region Constants

			public const int HWND_BROADCAST = 0xFFFF;
			public const int SW_SHOWNORMAL = 1;
			public const int WM_COPYDATA = 0x004A;

			#endregion

			#region Extern Methods

			[DllImport("kernel32", SetLastError = true)]
			public static extern IntPtr LocalAlloc(int flag, int size);

			[DllImport("kernel32", SetLastError = true)]
			public static extern IntPtr LocalFree(IntPtr p);

			[DllImport("user32")]
			public static extern int FindWindow(string strClassName, string strWindowName);

			[DllImport("user32")]
			public static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);

			[DllImport("user32")]
			public static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, ref CopyDataStruct lParam);

			[DllImport("user32")]
			private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

			[DllImport("user32")]
			private static extern bool SetForegroundWindow(IntPtr hWnd);

			[DllImport("user32")]
			private static extern int RegisterWindowMessage(string message);

			#endregion

			public static int RegisterWindowMessage(string format, params object[] args)
			{
				string message = String.Format(format, args);
				return RegisterWindowMessage(message);
			}

			public static void ShowToFront(IntPtr window)
			{
				ShowWindow(window, SW_SHOWNORMAL);
				SetForegroundWindow(window);
			}
		}

		#endregion

		#region Member Fields / Properties

		public static readonly int WM_SHOWFIRSTINSTANCE;
		private static System.Threading.Mutex mutex_;

		#endregion

		#region Static Constructor

		static SingleInstance()
		{
			WM_SHOWFIRSTINSTANCE = WinAPI.RegisterWindowMessage("WM_SHOWFIRSTINSTANCE|{0}", GetGUID());
		}

		#endregion

		#region Methods

		public static bool Start()
		{
			string mutexName = @"Local\" + GetGUID();
			bool onlyInstance = false;
			mutex_ = new System.Threading.Mutex(true, mutexName, out onlyInstance);
			return onlyInstance;
		}

		public static void ShowFirstInstance()
		{
			WinAPI.PostMessage(
				(IntPtr)WinAPI.HWND_BROADCAST,
				WM_SHOWFIRSTINSTANCE,
				IntPtr.Zero,
				IntPtr.Zero);
		}

		public static void SendStringMessage(IntPtr targetHWnd, string message)
		{
			WinAPI.CopyDataStruct copyData = new WinAPI.CopyDataStruct();
			try
			{
				copyData.cbData = (message.Length + 1) * 2;
				copyData.lpData = WinAPI.LocalAlloc(0x40, copyData.cbData);
				Marshal.Copy(message.ToCharArray(), 0, copyData.lpData, message.Length);
				copyData.dwData = (IntPtr)1;

				WinAPI.SendMessage(targetHWnd, WinAPI.WM_COPYDATA, IntPtr.Zero, ref copyData);
			}
			finally
			{
				if (copyData.lpData != IntPtr.Zero)
				{
					WinAPI.LocalFree(copyData.lpData);
					copyData.lpData = IntPtr.Zero;
				}
			}
		}

		public static string RecieveStringMessage(System.Windows.Forms.Message msg)
		{
			try
			{
				WinAPI.CopyDataStruct copyData = (WinAPI.CopyDataStruct)Marshal.PtrToStructure(msg.LParam, typeof(WinAPI.CopyDataStruct));
				return Marshal.PtrToStringUni(copyData.lpData);
			}
			catch
			{
				return null;
			}
		}

		public static void Stop()
		{
			mutex_.ReleaseMutex();
		}

		private static string GetGUID()
		{
			object[] attributes = System.Reflection.Assembly.GetEntryAssembly().GetCustomAttributes(typeof(GuidAttribute), false);
			return (attributes.Length == 0)
				? String.Empty
				: ((GuidAttribute)attributes[0]).Value;
		}

		#endregion
	}
}
