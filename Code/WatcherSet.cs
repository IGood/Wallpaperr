using System;
using System.IO;
using System.Timers;

namespace Wallpaperr
{
	class WatcherSet : IDisposable
	{
		#region Static Members

		private static Timer timer_;

		#endregion

		#region Static Constructor

		static WatcherSet()
		{
			timer_ = new Timer(TimeSpan.FromSeconds(30).TotalMilliseconds);
			timer_.Enabled = false;
			timer_.AutoReset = false;
		}

		#endregion

		#region Member Fields / Properties

		private FileSystemWatcher[] watchers_;

		#endregion

		#region Constructors

		public WatcherSet(string path, bool include)
		{
			int count = Helpers.FileTypes.Length;
			watchers_ = new FileSystemWatcher[count];
			for (int i = 0; i < count; ++i)
			{
				watchers_[i] = new FileSystemWatcher(path, Helpers.FileTypes[i]);
				watchers_[i].NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName;
				watchers_[i].IncludeSubdirectories = include;
				watchers_[i].Created += onSomeEvent;
				watchers_[i].Renamed += onSomeEvent;
				watchers_[i].EnableRaisingEvents = true;
			}
		}

		#endregion

		private static void onSomeEvent(object sender, FileSystemEventArgs e)
		{
			if (timer_.Enabled)
			{
				timer_.Stop();
			}
			timer_.Start();
		}

		public static void SetIncludeSubdirectories(System.Collections.IEnumerable items, bool include)
		{
			foreach (System.Windows.Forms.ListViewItem item in items)
			{
				WatcherSet watcherSet = item.Tag as WatcherSet;
				if (watcherSet != null)
				{
					foreach (FileSystemWatcher watcher in watcherSet.watchers_)
					{
						watcher.IncludeSubdirectories = include;
					}
				}
			}
		}

		#region IDisposable Methods

		void IDisposable.Dispose()
		{
			foreach (FileSystemWatcher watcher in watchers_)
			{
				watcher.Dispose();
			}
		}

		#endregion

		#region Static Methods

		public static void SetUpdateMethod(ElapsedEventHandler action)
		{
			timer_.Elapsed += action;
		}

		#endregion
	}
}
