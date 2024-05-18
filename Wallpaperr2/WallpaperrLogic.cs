namespace Wallpaperr2
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Windows.Controls;
    using System.Xml.Serialization;
    using Goop.Xml.Serialization;

    internal class WallpaperrLogic
    {
        public static Properties.Settings AppSettings
        {
            get { return Properties.Settings.Default; }
        }

        private MainWindow form;

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

        public WallpaperrLogic(MainWindow form)
        {
            this.form = form;

            // set WatcherSet update method
            //WatcherSet.SetUpdateMethod((s, e) => this.UpdateImageList());

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

            //this.form.SaveSettings(AppSettings);

            this.UpdateImageList();

            //this.form.Dirty = false;
        }

        public void RestoreSettings()
        {
            this.RestoreFromTempLists();

            //this.form.RestoreSettings(AppSettings);

            //this.form.Dirty = false;
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
                //this.tempItems = this.form.ListView.Items.Cast<ListViewItem>().ToArray();
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
                //this.form.ListView.BeginUpdate();
                //this.form.ListView.Items.Clear();
                //this.form.ListView.Items.AddRange(this.tempItems);
                //this.form.ListView.EndUpdate();
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

            //this.form.ListView.BeginUpdate();

            foreach (ListViewItem item in this.form.ListBox.SelectedItems)
            {
                FileSystemInfo info = new FileInfo((string)item.ToolTip);
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

                //item.Remove();

                //this.form.Dirty = true;
            }

            //this.form.ListView.EndUpdate();
        }

        public void AddFolders(IEnumerable folders)
        {
            this.MakeTempLists();

            var directoryInfos = folders
                .Cast<string>()
                .TakeWhile(folder => folder != null)
                .Select(folder => new DirectoryInfo(folder))
                .Where(dirInfo => dirInfo.Exists && this.form.library.Add(dirInfo));
            foreach (var dirInfo in directoryInfos)
            {
                // TODO - Create WatcherSet for this directory.
                ////Tag = new WatcherSet(dirInfo.FullName, AppSettings.IncludeSubdirectory),

                // add to lists
                this.folderList.Add(dirInfo);

                //this.form.Dirty = true;
            }
        }

        public void AddFiles(IEnumerable files)
        {
            this.MakeTempLists();

            var fileInfos = files
                .Cast<string>()
                .TakeWhile(file => file != null)
                .Select(file => new FileInfo(file))
                .Where(fileInfo => fileInfo.Exists);
            foreach (var fileInfo in fileInfos)
            {
                switch (fileInfo.Extension.ToLower())
                {
                    case ".xml":
                        this.AddItemsFromCollectionFile(fileInfo.FullName);
                        continue;

                    case ".bmp":
                    case ".png":
                    case ".jpg":
                    case ".jpeg":
                    case ".gif":
                        break;

                    default:
                        continue; // Unsupported file type.
                }

                if (this.form.library.Add(fileInfo))
                {
                    // add to lists
                    this.fileList.Add(fileInfo);

                    //this.form.Dirty = true;
                }
            }
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
                if (info.Attributes.HasFlag(FileAttributes.Directory))
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
            foreach (var tag in this.form.ListBox.Items.Cast<ListViewItem>().Select((item) => item.Tag).OfType<IDisposable>())
            {
                tag.Dispose();
            }

            this.form.ListBox.Items.Clear();
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
            try
            {
                var s = new XmlSerializer(typeof(List<String>));
                var list = s.Deserialize<List<String>>(fileName);
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

        public void SaveItemsToCollectionFile(string fileName)
        {

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
            this.form.library.Remove(fileInfo);
        }

        // Returns an array of file names, the contents of which were pulled
        // from the master file list.
        public string[] GetMoreFiles(string fileName)
        {

            string[] retVal = new string[System.Windows.Forms.Screen.AllScreens.Length];
            retVal[0] = fileName;

            // just one?
            if (AppSettings.SingleMonitor)
            {
                return retVal;
            }

            // Smart random?
            if (AppSettings.SmartRandom)
            {
                FileInfo seed = new FileInfo(fileName);

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
