namespace Wallpaperr
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Timers;

	sealed class WatcherSet : IDisposable
	{
		#region Static Members

		private static readonly Timer Timer = new(TimeSpan.FromSeconds(30).TotalMilliseconds) { AutoReset = false, Enabled = false };

		#endregion

		#region Member Fields / Properties

		private readonly FileSystemWatcher[] watchers;

		#endregion

		#region Constructors

		public WatcherSet(string path, bool include)
		{
			this.watchers = Helpers
				.FileTypes
				.Select((fileType) =>
				{
					var watcher = new FileSystemWatcher(path, fileType)
					{
						EnableRaisingEvents = true,
						IncludeSubdirectories = include,
						NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName,
					};

					watcher.Created += onSomeEvent;
					watcher.Renamed += onSomeEvent;

					return watcher;
				})
				.ToArray();

			static void onSomeEvent(object sender, FileSystemEventArgs e)
			{
				if (Timer.Enabled)
				{
					Timer.Stop();
				}

				Timer.Start();
			}
		}

		#endregion


		public static void SetIncludeSubdirectories(System.Collections.IEnumerable items, bool include)
		{
			var fileSystemWatchers = items
				.Cast<System.Windows.Forms.ListViewItem>()
				.Select((item) => item.Tag)
				.OfType<WatcherSet>()
				.SelectMany((watcherSet) => watcherSet.watchers);

			foreach (var watcher in fileSystemWatchers)
			{
				watcher.IncludeSubdirectories = include;
			}
		}

		#region IDisposable Methods

		public void Dispose()
		{
			foreach (var watcher in this.watchers)
			{
				watcher.Dispose();
			}
		}

		#endregion

		#region Static Methods

		public static void SetUpdateMethod(ElapsedEventHandler action)
		{
			Timer.Elapsed += action;
		}

		#endregion
	}
}
