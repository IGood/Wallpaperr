using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Wallpaperr
{
	using StringCollection = System.Collections.Specialized.StringCollection;

	internal class WallpaperrLogic
	{
		#region Member Fields / Properties

		private MainForm form_;

		private List<DirectoryInfo> folderList_;
		private List<FileInfo> fileList_;

		private List<FileInfo> masterFileList_;
		public IList<FileInfo> MasterFileList
		{
			get { return masterFileList_.AsReadOnly(); }
		}

		private Properties.Settings appSettings_ = Properties.Settings.Default;
		public Properties.Settings AppSettings
		{
			get { return appSettings_; }
		}

		private List<DirectoryInfo> tempFolders_;
		private List<FileInfo> tempFiles_;
		private ListViewItem[] tempItems_;

		#endregion

		#region Constructors

		public WallpaperrLogic(MainForm form)
		{
			form_ = form;

			// set WatcherSet update method
			WatcherSet.SetUpdateMethod((s, e) => UpdateImageList());

			// get folder list from settings
			if (appSettings_.FolderList == null)
			{
				appSettings_.FolderList = new StringCollection();
			}
			folderList_ = new List<DirectoryInfo>(appSettings_.FolderList.Count);
			AddFolders(appSettings_.FolderList);

			// get file list from settings
			if (appSettings_.FileList == null)
			{
				appSettings_.FileList = new StringCollection();
			}
			fileList_ = new List<FileInfo>(appSettings_.FileList.Count);
			AddFiles(appSettings_.FileList);

			// drop backup lists & set controls after initialization
			ClearTempLists();
			RestoreSettings();

#if DEBUG
			//NewWallpaper();
#endif
		}

		#endregion

		public void SaveSettingsToDisk()
		{
			// store folder list
			appSettings_.FolderList.Clear();
			foreach (DirectoryInfo info in folderList_)
			{
				appSettings_.FolderList.Add(info.FullName);
			}

			// store file list
			appSettings_.FileList.Clear();
			foreach (FileInfo info in fileList_)
			{
				appSettings_.FileList.Add(info.FullName);
			}

			// save settings
			appSettings_.Save();
		}

		public void SaveSettings()
		{
			ClearTempLists();

			form_.SaveSettings(appSettings_);

			UpdateImageList();

			form_.Dirty = false;
		}

		public void RestoreSettings()
		{
			RestoreFromTempLists();

			form_.RestoreSettings(appSettings_);

			form_.Dirty = false;
		}

		private void MakeTempLists()
		{
			if (tempFolders_ == null)
			{
				if (folderList_ == null)
				{
					tempFolders_ = new List<DirectoryInfo>();
				}
				else
				{
					tempFolders_ = new List<DirectoryInfo>(folderList_);
				}
			}

			if (tempFiles_ == null)
			{
				if (fileList_ == null)
				{
					tempFiles_ = new List<FileInfo>();
				}
				else
				{
					tempFiles_ = new List<FileInfo>(fileList_);
				}
			}

			if (tempItems_ == null)
			{
				tempItems_ = new ListViewItem[form_.ListView.Items.Count];
				form_.ListView.Items.CopyTo(tempItems_, 0);
			}
		}

		private void RestoreFromTempLists()
		{
			if (tempFolders_ != null)
			{
				folderList_ = tempFolders_;
			}

			if (tempFiles_ != null)
			{
				fileList_ = tempFiles_;
			}

			if (tempItems_ != null)
			{
				form_.ListView.Items.Clear();
				form_.ListView.Items.AddRange(tempItems_);
			}

			ClearTempLists();
			UpdateImageList();
		}

		public void UpdateImageList()
		{
			// reset master list
			masterFileList_ = new List<FileInfo>(fileList_);

			// get files from folders
			var foldersFileList = new List<FileInfo>();
			SearchOption searchOption = appSettings_.IncludeSubdirectory ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
			var missingFolders = new List<DirectoryInfo>();
			folderList_.ForEach((dirInfo) =>
			{
				if (Helpers.Exists(dirInfo))
				{
					foreach (string pattern in Helpers.FileTypes)
					{
						foldersFileList.AddRange(dirInfo.GetFiles(pattern, searchOption));
					}
				}
				else
				{
					missingFolders.Add(dirInfo);
				}
			});

			// remove missing folders
			missingFolders.ForEach((dirInfo) => folderList_.Remove(dirInfo));

			// handle duplicate entries
			foldersFileList.ForEach((newInfo) =>
			{
				bool found = false;
				foreach (FileInfo storedInfo in masterFileList_)
				{
					if (storedInfo.FullName == newInfo.FullName)
					{
						found = true;
						break;
					}
				}
				if (!found)
				{
					masterFileList_.Add(newInfo);
				}
			});
		}

		private void ClearTempLists()
		{
			tempFolders_ = null;
			tempFiles_ = null;
			tempItems_ = null;
		}

		public void RemoveSelectedItems()
		{
			MakeTempLists();

			foreach (ListViewItem item in form_.ListView.SelectedItems)
			{
				FileSystemInfo info = new FileInfo(item.ToolTipText);
				IList collection = (info.Attributes == FileAttributes.Directory)
					? folderList_
					: (IList)fileList_;
				foreach (FileSystemInfo fsi in collection)
				{
					if (fsi.FullName == info.FullName)
					{
						collection.Remove(fsi);
						break;
					}
				}

				using (item.Tag as WatcherSet) { }
				item.Remove();

				form_.Dirty = true;
			}
		}

		public void AddFolders(IEnumerable folders)
		{
			MakeTempLists();

			foreach (string folder in folders)
			{
				if (folder == null)
				{
					break;
				}

				DirectoryInfo dirInfo = new DirectoryInfo(folder);
				if (!dirInfo.Exists)
				{
					continue;
				}

				ListViewItem item = new ListViewItem(dirInfo.Name, 0);
				item.Name = item.Text;
				item.ToolTipText = dirInfo.FullName;

				// check already exists
				if (Helpers.FindInListView(dirInfo, form_.ListView) != null)
				{
					continue;
				}

				item.Tag = new WatcherSet(dirInfo.FullName, appSettings_.IncludeSubdirectory);

				// add to lists
				form_.ListView.Items.Add(item);
				folderList_.Add(dirInfo);

				form_.Dirty = true;
			}

			form_.ListView.Sort();
		}

		public void AddFiles(IEnumerable files)
		{
			MakeTempLists();

			foreach (string file in files)
			{
				if (file == null)
				{
					break;
				}

				FileInfo fileInfo = new FileInfo(file);
				if (!fileInfo.Exists)
				{
					continue;
				}

				int imageIndex = 0;
				switch (fileInfo.Extension.ToLower())
				{
					case ".xml":
						AddItemsFromCollectionFile(fileInfo.FullName);
						continue;
					case ".bmp": imageIndex = 1; break;
					case ".png": imageIndex = 2; break;
					case ".jpg":
					case ".jpeg": imageIndex = 3; break;
					case ".gif": imageIndex = 4; break;
					default: continue; // unsupported file type
				}
				ListViewItem item = new ListViewItem(fileInfo.Name, imageIndex);
				item.Name = item.Text;
				item.ToolTipText = fileInfo.FullName;

				// check already exists
				if (Helpers.FindInListView(fileInfo, form_.ListView) != null)
				{
					continue;
				}

				// add to lists
				form_.ListView.Items.Add(item);
				fileList_.Add(fileInfo);

				form_.Dirty = true;
			}

			form_.ListView.Sort();
		}

		public void AddItemsFromArray(string[] items)
		{
			string[] folders = new string[items.Length];
			int folderCount = 0;

			string[] files = new string[items.Length];
			int fileCount = 0;

			foreach (string thing in items)
			{
				FileInfo info = new FileInfo(thing);
				if (info.Attributes == FileAttributes.Directory)
				{
					folders[folderCount++] = thing;
				}
				else
				{
					files[fileCount++] = thing;
				}
			}

			AddFolders(folders);
			AddFiles(files);
		}

		public void OpenCollection(string fileName)
		{
			// save lists
			MakeTempLists();

			// clear collections
			foreach (ListViewItem item in form_.ListView.Items)
			{
				using (item.Tag as WatcherSet) { }
			}
			form_.ListView.Clear();
			masterFileList_.Clear();
			folderList_.Clear();
			fileList_.Clear();

			// load collection
			AddItemsFromCollectionFile(fileName);

			// update
			UpdateImageList();
		}

		public void AddItemsFromCollectionFile(string fileName)
		{
			using (FileStream fs = File.OpenRead(fileName))
			{
				var s = new System.Xml.Serialization.XmlSerializer(typeof(List<String>));
				try
				{
					var list = s.Deserialize(fs) as List<String>;
					if (list != null)
					{
						AddItemsFromArray(list.ToArray());
					}
				}
				catch (InvalidOperationException)
				{
					Helpers.ShowError("The Collection file is invalid.");
				}
			}
		}

		public void NewWallpaper()
		{
			// make sure there are images
			if (masterFileList_.Count == 0)
			{
				form_.ShowEmptyCollection();
			}
			// in case user cancels file browser
			if (masterFileList_.Count == 0)
			{
				return;
			}

			int randIndex = Helpers.RNG.Next(masterFileList_.Count);
			FileInfo fileInfo = masterFileList_[randIndex];
			if (Helpers.Exists(fileInfo))
			{
				form_.StartWork(fileInfo.FullName);
			}
			else
			{
				RemoveDeadFile(fileInfo);
				NewWallpaper();
			}
		}

		public void RemoveDeadFile(FileInfo fileInfo)
		{
			fileList_.Remove(fileInfo);
			masterFileList_.Remove(fileInfo);
			ListViewItem item = Helpers.FindInListView(fileInfo, form_.ListView);
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
			if (appSettings_.SingleMonitor)
			{
				return retVal;
			}

			// Smart random?
			if (appSettings_.SmartRandom)
			{
				FileInfo seed = new FileInfo(fileName);

				// find files in master list with same directory
				var siblings = new List<FileInfo>();
				var deadFiles = new List<FileInfo>();
				masterFileList_.ForEach((fileInfo) =>
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
				deadFiles.ForEach((fileInfo) => RemoveDeadFile(fileInfo));

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
				if (masterFileList_.Count == 0)
				{
					// use seed
					retVal[i] = fileName;
					continue;
				}

				// select a valid file
				int randIndex = Helpers.RNG.Next(masterFileList_.Count);
				FileInfo fileInfo = masterFileList_[randIndex];
				if (Helpers.Exists(fileInfo))
				{
					retVal[i] = fileInfo.FullName;
				}
				else
				{
					RemoveDeadFile(fileInfo);
					--i;
				}
			}

			return retVal;
		}
	}
}
