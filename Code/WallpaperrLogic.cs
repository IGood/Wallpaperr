namespace Wallpaperr
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Windows.Forms;
	using StringCollection = System.Collections.Specialized.StringCollection;

	internal class WallpaperrLogic
	{
		#region Static Fields / Properties

		public static Properties.Settings AppSettings
		{
			get { return Properties.Settings.Default; }
		}

		#endregion

		#region Member Fields / Properties

		private MainForm form;

		private List<DirectoryInfo> folderList;

		private List<FileInfo> fileList;

		private List<FileInfo> masterFileList;
		public IList<FileInfo> MasterFileList
		{
			get { return this.masterFileList.AsReadOnly(); }
		}

		private List<DirectoryInfo> tempFolders;

		private List<FileInfo> tempFiles;

		private ListViewItem[] tempItems;

		#endregion

		#region Constructors

		public WallpaperrLogic(MainForm form)
		{
			this.form = form;

			// set WatcherSet update method
			WatcherSet.SetUpdateMethod((s, e) => this.UpdateImageList());

			// get folder list from settings
			if (AppSettings.FolderList == null)
			{
				AppSettings.FolderList = new StringCollection();
			}

			this.folderList = new List<DirectoryInfo>(AppSettings.FolderList.Count);

			AddFolders(AppSettings.FolderList);

			// get file list from settings
			if (AppSettings.FileList == null)
			{
				AppSettings.FileList = new StringCollection();
			}

			this.fileList = new List<FileInfo>(AppSettings.FileList.Count);

			AddFiles(AppSettings.FileList);

			// drop backup lists & set controls after initialization
			this.ClearTempLists();
			this.RestoreSettings();

#if DEBUG
			//NewWallpaper();
#endif
		}

		#endregion

		public void SaveSettingsToDisk()
		{
			// store folder list
			AppSettings.FolderList.Clear();
			foreach (DirectoryInfo info in this.folderList)
			{
				AppSettings.FolderList.Add(info.FullName);
			}

			// store file list
			AppSettings.FileList.Clear();
			foreach (FileInfo info in this.fileList)
			{
				AppSettings.FileList.Add(info.FullName);
			}

			// save settings
			AppSettings.Save();
		}

		public void SaveSettings()
		{
			this.ClearTempLists();

			this.form.SaveSettings(AppSettings);

			this.UpdateImageList();

			this.form.Dirty = false;
		}

		public void RestoreSettings()
		{
			this.RestoreFromTempLists();

			this.form.RestoreSettings(AppSettings);

			this.form.Dirty = false;
		}

		private void MakeTempLists()
		{
			if (this.tempFolders == null)
			{
				this.tempFolders = new List<DirectoryInfo>(this.folderList ?? Enumerable.Empty<DirectoryInfo>());
			}

			if (this.tempFiles == null)
			{
				this.tempFiles = new List<FileInfo>(this.fileList ?? Enumerable.Empty<FileInfo>());
			}

			if (this.tempItems == null)
			{
				this.tempItems = this.form.ListView.Items.Cast<ListViewItem>().ToArray();
			}
		}

		private void RestoreFromTempLists()
		{
			if (this.tempFolders != null)
			{
				this.folderList = this.tempFolders;
			}

			if (this.tempFiles != null)
			{
				this.fileList = this.tempFiles;
			}

			if (this.tempItems != null)
			{
				this.form.ListView.BeginUpdate();
				this.form.ListView.Items.Clear();
				this.form.ListView.Items.AddRange(this.tempItems);
				this.form.ListView.EndUpdate();
			}

			this.ClearTempLists();
			this.UpdateImageList();
		}

		public void UpdateImageList()
		{
			// reset master list
			this.masterFileList = new List<FileInfo>(this.fileList);

			// get files from folders
			var foldersFileList = new List<FileInfo>();
			SearchOption searchOption = AppSettings.IncludeSubdirectory ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
			var missingFolders = new List<DirectoryInfo>();
			this.folderList.ForEach((dirInfo) =>
			{
				if (Helpers.Exists(dirInfo))
				{
					foldersFileList.AddRange(Helpers.FileTypes.SelectMany((pattern) => dirInfo.GetFiles(pattern, searchOption)));
				}
				else
				{
					missingFolders.Add(dirInfo);
				}
			});

			// remove missing folders
			missingFolders.ForEach((dirInfo) => this.folderList.Remove(dirInfo));

			// handle duplicate entries
			foldersFileList.ForEach((newInfo) =>
			{
				bool found = this.masterFileList.Any((storedInfo) => storedInfo.FullName == newInfo.FullName);
				if (!found)
				{
					this.masterFileList.Add(newInfo);
				}
			});
		}

		private void ClearTempLists()
		{
			this.tempFolders = null;
			this.tempFiles = null;
			this.tempItems = null;
		}

		public void RemoveSelectedItems()
		{
			this.MakeTempLists();

			this.form.ListView.BeginUpdate();

			foreach (ListViewItem item in this.form.ListView.SelectedItems)
			{
				FileSystemInfo info = new FileInfo(item.ToolTipText);
				IList collection = (info.Attributes == FileAttributes.Directory)
					? this.folderList
					: (IList)this.fileList;
				foreach (FileSystemInfo fsi in collection)
				{
					if (fsi.FullName == info.FullName)
					{
						collection.Remove(fsi);
						break;
					}
				}

				if (item.Tag is IDisposable)
				{
					((IDisposable)item.Tag).Dispose();
				}

				item.Remove();

				this.form.Dirty = true;
			}

			this.form.ListView.EndUpdate();
		}

		public void AddFolders(IEnumerable folders)
		{
			this.MakeTempLists();

			this.form.ListView.BeginUpdate();

			var directoryInfos = folders
				.Cast<string>()
				.TakeWhile((folder) => folder != null)
				.Select((folder) => new DirectoryInfo(folder))
				.Where((dirInfo) => dirInfo.Exists);
			foreach (var dirInfo in directoryInfos)
			{
				// check already exists
				if (Helpers.FindInListView(dirInfo, this.form.ListView) != null)
				{
					continue;
				}

				var item = new ListViewItem(dirInfo.Name, 0)
				{
					Name = dirInfo.Name,
					Tag = new WatcherSet(dirInfo.FullName, AppSettings.IncludeSubdirectory),
					ToolTipText = dirInfo.FullName,
				};

				// add to lists
				this.form.ListView.Items.Add(item);
				this.folderList.Add(dirInfo);

				this.form.Dirty = true;
			}

			this.form.ListView.Sort();
			this.form.ListView.EndUpdate();
		}

		public void AddFiles(IEnumerable files)
		{
			this.MakeTempLists();

			this.form.ListView.BeginUpdate();

			var fileInfos = files
				.Cast<string>()
				.TakeWhile((file) => file != null)
				.Select((file) => new FileInfo(file))
				.Where((fileInfo) => fileInfo.Exists);
			foreach (var fileInfo in fileInfos)
			{
				int imageIndex = 0;
				switch (fileInfo.Extension.ToLower())
				{
					case ".xml":
						this.AddItemsFromCollectionFile(fileInfo.FullName);
						continue;

					case ".bmp":
						imageIndex = 1;
						break;

					case ".png":
						imageIndex = 2;
						break;

					case ".jpg":
					case ".jpeg":
						imageIndex = 3;
						break;

					case ".gif":
						imageIndex = 4;
						break;

					default:
						continue; // unsupported file type
				}

				// check already exists
				if (Helpers.FindInListView(fileInfo, this.form.ListView) != null)
				{
					continue;
				}

				var item = new ListViewItem(fileInfo.Name, imageIndex)
				{
					Name = fileInfo.Name,
					ToolTipText = fileInfo.FullName,
				};

				// add to lists
				this.form.ListView.Items.Add(item);
				this.fileList.Add(fileInfo);

				this.form.Dirty = true;
			}

			this.form.ListView.Sort();
			this.form.ListView.EndUpdate();
		}

		public void AddItemsFromArray(string[] items)
		{
			string[] folders = new string[items.Length];
			int folderCount = 0;

			string[] files = new string[items.Length];
			int fileCount = 0;

			foreach (string thing in items)
			{
				var info = new FileInfo(thing);
				if (info.Attributes == FileAttributes.Directory)
				{
					folders[folderCount++] = thing;
				}
				else
				{
					files[fileCount++] = thing;
				}
			}

			this.AddFolders(folders);
			this.AddFiles(files);
		}

		public void OpenCollection(string fileName)
		{
			// save lists
			this.MakeTempLists();

			// clear collections
			foreach (var tag in this.form.ListView.Items.Cast<ListViewItem>().Select((item) => item.Tag).OfType<IDisposable>())
			{
				tag.Dispose();
			}

			this.form.ListView.Clear();
			this.masterFileList.Clear();
			this.folderList.Clear();
			this.fileList.Clear();

			// load collection
			this.AddItemsFromCollectionFile(fileName);

			// update
			this.UpdateImageList();
		}

		public void AddItemsFromCollectionFile(string fileName)
		{
			using var fs = File.OpenRead(fileName);
			var s = new System.Xml.Serialization.XmlSerializer(typeof(List<string>));
			try
			{
				if (s.Deserialize(fs) is List<string> list)
				{
					AddItemsFromArray(list.ToArray());
				}
			}
			catch (InvalidOperationException)
			{
				Helpers.ShowError("The Collection file is invalid.");
			}
		}

		public void NewWallpaper()
		{
			// make sure there are images
			if (this.masterFileList.Count == 0)
			{
				this.form.ShowEmptyCollection();
			}

			// in case user cancels file browser
			if (this.masterFileList.Count == 0)
			{
				return;
			}

			int randIndex = Helpers.RNG.Next(this.masterFileList.Count);
			FileInfo fileInfo = this.masterFileList[randIndex];
			if (Helpers.Exists(fileInfo))
			{
				this.form.StartWork(fileInfo.FullName);
			}
			else
			{
				this.RemoveDeadFile(fileInfo);
				this.NewWallpaper();
			}
		}

		public void RemoveDeadFile(FileInfo fileInfo)
		{
			this.fileList.Remove(fileInfo);
			this.masterFileList.Remove(fileInfo);
			ListViewItem item = Helpers.FindInListView(fileInfo, this.form.ListView);
			if (item != null)
			{
				item.Remove();
			}
		}

		// Returns an array of file names, the contents of which were pulled
		// from the master file list.
		public string[] GetMoreFiles(string fileName)
		{
			string[] retVal = new string[Screen.AllScreens.Length];
			retVal[0] = fileName;

			// just one?
			if (AppSettings.SingleMonitor)
			{
				return retVal;
			}

			// Smart random?
			if (AppSettings.SmartRandom)
			{
				var seed = new FileInfo(fileName);

				// find files in master list with same directory
				var siblings = new List<FileInfo>();
				var deadFiles = new List<FileInfo>();
				this.masterFileList.ForEach((fileInfo) =>
				{
					if (fileInfo.DirectoryName == seed.DirectoryName &&
						fileInfo.Name != seed.Name)
					{
						// check for dead files
						if (Helpers.Exists(fileInfo))
						{
							siblings.Add(fileInfo);
						}
						else
						{
							deadFiles.Add(fileInfo);
						}
					}
				});

				// remove dead files
				deadFiles.ForEach(this.RemoveDeadFile);

				// found enough siblings?
				if (siblings.Count >= retVal.Length - 1)
				{
					// pick randomly from siblings
					for (int i = 1; i < retVal.Length; ++i)
					{
						int randIndex = Helpers.RNG.Next(siblings.Count);
						FileInfo fileInfo = siblings[randIndex];
						retVal[i] = fileInfo.FullName;
						siblings.RemoveAt(randIndex);
					}

					return retVal;
				}
			}

			// normal random
			for (int i = 1; i < retVal.Length; ++i)
			{
				// empty list?
				if (this.masterFileList.Count == 0)
				{
					// use seed
					retVal[i] = fileName;
					continue;
				}

				// select a valid file
				int randIndex = Helpers.RNG.Next(this.masterFileList.Count);
				FileInfo fileInfo = this.masterFileList[randIndex];
				if (Helpers.Exists(fileInfo))
				{
					retVal[i] = fileInfo.FullName;
				}
				else
				{
					this.RemoveDeadFile(fileInfo);
					--i;
				}
			}

			return retVal;
		}
	}
}
