namespace Wallpaperr2
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Shell;
    using System.Windows.Threading;
    using Goop.Linq;
    using Goop.ObjectModel;
    using Goop.Wpf;
    using Microsoft.Win32;
    using Cmd = Goop.Wpf.RoutedCommandUtilities<MainWindow>;
    using DP = Goop.Wpf.DependencyPropertyUtilities<MainWindow>;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static RoutedUICommand AddFiles = Cmd.CreateUI("Add _Files...", nameof(AddFiles), new KeyGesture(Key.F, ModifierKeys.Control));
        public static RoutedUICommand AddFolder = Cmd.CreateUI("Add Folde_r...", nameof(AddFolder), new KeyGesture(Key.F, ModifierKeys.Control | ModifierKeys.Shift));
        public static RoutedUICommand NewRandomWallpaper = Cmd.CreateUI("New Random _Wallpaper", nameof(NewRandomWallpaper), new KeyGesture(Key.F5));
        public static RoutedUICommand Quit = Cmd.CreateUI("_Quit", nameof(Quit));
        public static RoutedUICommand About = Cmd.CreateUI("_About Wallpaperr", nameof(About));
        public static RoutedUICommand ShowInExplorer = Cmd.CreateUI("Show In _Explorer", nameof(ShowInExplorer));
        public static RoutedCommand TogglePaused = Cmd.Create(nameof(TogglePaused));

        private bool closeToTray = true;

        private readonly DispatcherTimer timer = new DispatcherTimer();

        private readonly BackgroundWorker backgroundWorker = new BackgroundWorker { WorkerReportsProgress = true };

        private readonly WallpaperrLogic logic;

        public readonly ObservableHashSet<FileSystemInfo> library = new ObservableHashSet<FileSystemInfo>(FileSystemInfoComparer.Default);

        public MainWindow()
        {
            PropertyChangedEventManager.AddHandler(
                Properties.Settings.Default,
                delegate
                {
                    this.timer.IsEnabled = Properties.Settings.Default.IsActive;
                    this.CoerceValue(TrayIconSourceProperty);
                },
                nameof(Properties.Settings.Default.IsActive));

            EventHandler<PropertyChangedEventArgs> updateTimerInterval = delegate
            {
                this.timer.Interval = Properties.Settings.Default.Interval.ToTimeSpan(Properties.Settings.Default.IntervalUnit);
            };

            PropertyChangedEventManager.AddHandler(
                Properties.Settings.Default,
                updateTimerInterval,
                nameof(Properties.Settings.Default.Interval));

            PropertyChangedEventManager.AddHandler(
                Properties.Settings.Default,
                updateTimerInterval,
                nameof(Properties.Settings.Default.IntervalUnit));

            updateTimerInterval(null, null);
            this.timer.Tick += delegate { NewRandomWallpaper.Execute(null, null); };

            this.backgroundWorker.DoWork += this.backgroundWorker_DoWork;
            this.backgroundWorker.ProgressChanged += this.backgroundWorker_ProgressChanged;
            this.backgroundWorker.RunWorkerCompleted += this.backgroundWorker_RunWorkerCompleted;

            this.logic = new WallpaperrLogic(this);

            this.TrayIconCommand = ApplicationCommands.Properties;

            this.BackgroundStyle.PropertyChanged += delegate
            {
                Properties.Settings.Default.BackgroundStyle = this.BackgroundStyle;
            };

            this.Library = this.library;

            this.InitializeComponent();

            this.CoerceValue(TrayIconSourceProperty);
        }

        public TaskbarItemProgressState ProgressBarState
        {
            get => (TaskbarItemProgressState)(this.GetValue(ProgressBarStateProperty));
            set => this.SetValue(ProgressBarStateProperty, value);
        }
        public static DependencyProperty ProgressBarStateProperty = DP.Register(_ => _.ProgressBarState, TaskbarItemProgressState.None);

        public Visibility ProgressBarVisibility
        {
            get => (Visibility)(this.GetValue(ProgressBarVisibilityProperty));
            set => this.SetValue(ProgressBarVisibilityProperty, value);
        }
        public static DependencyProperty ProgressBarVisibilityProperty = DP.Register(_ => _.ProgressBarVisibility, Visibility.Hidden, OnProgressBarVisibilityChanged);
        private static void OnProgressBarVisibilityChanged(MainWindow self, DependencyPropertyChangedEventArgs e)
        {
            self.CoerceValue(TrayIconSourceProperty);
        }

        public double ProgressBarValue
        {
            get => (double)(this.GetValue(ProgressBarValueProperty));
            set => this.SetValue(ProgressBarValueProperty, value);
        }
        public static DependencyProperty ProgressBarValueProperty = DP.Register(_ => _.ProgressBarValue);

        public string TrayIconSource
        {
            get => (string)(this.GetValue(TrayIconSourceProperty));
            set => this.SetValue(TrayIconSourceProperty, value);
        }
        public static DependencyProperty TrayIconSourceProperty = DP.Register(_ => _.TrayIconSource, null, null, CoerceTrayIconSource);
        private static object CoerceTrayIconSource(MainWindow self, string baseValue)
        {
            if (self.ProgressBarVisibility == Visibility.Visible)
            {
                return "Images/smallBusy.ico";
            }

            if (Properties.Settings.Default.IsActive)
            {
                return "Images/small.ico";
            }

            return "Images/smallPaused.ico";
        }

        public ICommand TrayIconCommand
        {
            get => (ICommand)(this.GetValue(TrayIconCommandProperty));
            set => this.SetValue(TrayIconCommandProperty, value);
        }
        public static DependencyProperty TrayIconCommandProperty = DP.Register(_ => _.TrayIconCommand);

        public BindableEnum<BackgroundStyle> BackgroundStyle { get; } = BindableEnum.Create(Wallpaperr2.BackgroundStyle.Spiffy);

        public IEnumerable Library
        {
            get => (IEnumerable)(this.GetValue(LibraryProperty));
            set => this.SetValue(LibraryProperty, value);
        }
        public static DependencyProperty LibraryProperty = DP.Register(_ => _.Library);

        private void root_OnClosing(object sender, CancelEventArgs e)
        {
            Properties.Settings.Default.Reload();

            if (this.closeToTray)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void Close_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
            e.Handled = true;
        }

        private void Quit_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.closeToTray = false;
            this.Close();
            e.Handled = true;
        }

        private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.FileOk += delegate { this.logic.AddItemsFromCollectionFile(ofd.FileName); };
            ofd.ShowDialog();
            e.Handled = true;
        }

        private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var sfd = new SaveFileDialog();
            sfd.FileOk += delegate { this.logic.SaveItemsToCollectionFile(sfd.FileName); };
            sfd.ShowDialog();
            e.Handled = true;
        }

        private void AddFiles_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = Helpers.ImageFilesFilter,
                Multiselect = true,
            };
            ofd.FileOk += delegate { this.logic.AddFiles(ofd.FileNames); };
            ofd.ShowDialog();
            e.Handled = true;
        }

        private void AddFolders_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // TODO - Folder browser dialog.
            e.Handled = true;
        }

        private void DeleteItem_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.ListBox.SelectedItem != null;
            e.Handled = true;
        }

        private void DeleteItem_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is FileSystemInfo single)
            {
                this.library.Remove(single);
            }
            else if (e.Parameter is IEnumerable multiple)
            {
                multiple.OfType<FileSystemInfo>().ForEach(this.library.Remove);
            }

            e.Handled = true;
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
            e.Handled = true;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
            this.Close();
            e.Handled = true;
        }

        private void TogglePaused_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Properties.Settings.Default.IsActive = !Properties.Settings.Default.IsActive;

            if (this.IsVisible == false)
            {
                // If the window currently closed to the system tray, save settings.
                Properties.Settings.Default.Save();
            }

            e.Handled = true;
        }

        private void Properties_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
            e.Handled = true;
        }

        private void pictureBoxStyle_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
            {
                e.Effects = DragDropEffects.Copy;
            }

            e.Handled = true;
        }

        private void pictureBoxStyle_DragDrop(object sender, DragEventArgs e)
        {
            string[] dropped = e.Data.GetData(DataFormats.FileDrop) as string[];
            string filePath = dropped?[0];
            if (filePath != null)
            {
                StartWork(filePath);
            }

            e.Handled = true;
        }

        public void StartWork(string fileName)
        {
            // currently working?
            if (this.backgroundWorker.IsBusy)
            {
                Helpers.ShowBusy();
                return;
            }

            this.timer.Stop();

            //notifyIcon.Text = "Wallpaperr [busy]";
            //notifyIcon.Icon = Properties.Resources.smallBusy;

            this.Title = "Wallpaperr - Automatic Wallpaper Changer [busy]";

            this.ProgressBarValue = 0;
            this.ProgressBarVisibility = Visibility.Visible;
            this.ProgressBarState = TaskbarItemProgressState.Normal;

            // start operation
            this.backgroundWorker.RunWorkerAsync(fileName);
        }

        #region ListView Callbacks

        private void contextMenuItem_Opening(object sender, CancelEventArgs e)
        {
            /*
            // nothing selected?
            if (this.listView.SelectedItems.Count == 0)
            {
                e.Cancel = true;
                return;
            }

            contextMenuItem.Items[0].Image = Properties.Resources.random_16x16;

            if (this.listView.SelectedItems.Count == 1)
            {
                ListViewItem item = this.listView.FocusedItem;
                FileInfo fileInfo = new FileInfo(item.ToolTipText);
                if (fileInfo.Exists)
                {
                    System.Drawing.Image img = null;
                    if (!thumbDict_.TryGetValue(item, out img))
                    {
                        using (var temp = System.Drawing.Image.FromFile(fileInfo.FullName))
                        {
                            const float maxDims = 150;
                            float scale = Math.Min(maxDims / temp.Width, maxDims / temp.Height);
                            float thumbW = temp.Width * scale;
                            float thumbH = temp.Height * scale;
                            img = new System.Drawing.Bitmap(temp, (int)thumbW, (int)thumbH);
                            thumbDict_.Add(item, img);
                        }
                    }

                    contextMenuItem.Items[0].ImageScaling = ToolStripItemImageScaling.None;
                    contextMenuItem.Items[0].Image = img;
                }
            }
            //*/
        }

        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            NewRandomWallpaper.Execute((sender as FrameworkElement)?.DataContext, null);
            e.Handled = true;
        }

        private void listView_DragEnter(object sender, DragEventArgs e)
        {
            // Dragging folders/files?
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
            {
                e.Effects = DragDropEffects.Copy;
            }

            e.Handled = true;
        }

        private void listView_DragDrop(object sender, DragEventArgs e)
        {
            string[] dropped = e.Data.GetData(DataFormats.FileDrop) as string[];
            this.logic.AddItemsFromArray(dropped);
            e.Handled = true;
        }

        private void listView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && this.ListBox.SelectedItems.Count != 0)
            {
                NewRandomWallpaper.Execute(this.ListBox.SelectedItems, null);
                e.Handled = true;
            }
        }

        #endregion

        #region BackgroundWorker Callbacks

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string[] files = this.logic.GetMoreFiles((string)e.Argument);
            e.Result = WallpaperComposer.MakePicture(files, WallpaperrLogic.AppSettings, (BackgroundWorker)sender);
        }

        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.ProgressBarValue = e.ProgressPercentage / 100.0;
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message, "BackgroundWorker Error");
            }

            this.ProgressBarVisibility = Visibility.Hidden;
            this.ProgressBarState = TaskbarItemProgressState.None;

            this.timer.Start();
            this.timer.IsEnabled = WallpaperrLogic.AppSettings.IsActive;

            //notifyIcon.Text = "Wallpaperr";
            //notifyIcon.Icon = WallpaperrLogic.AppSettings.IsActive ? Properties.Resources.small : Properties.Resources.smallPaused;

            this.Title = "Wallpaperr - Automatic Wallpaper Changer";
        }

        #endregion

        public void ShowEmptyCollection()
        {
            this.timer.Stop();

            const string msg =
@"You do not have any images in your collection.
Would you like to add some files now?";

            var result = MessageBox.Show(msg, "Hold it!", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                AddFiles.Execute(null, null);
            }

            this.timer.Start();
            this.timer.IsEnabled = WallpaperrLogic.AppSettings.IsActive;
        }

        private void NewRandomWallpaper_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter == null)
            {
                this.logic.NewWallpaper();
                return;
            }

            FileSystemInfo info = null;
            if (e.Parameter is FileSystemInfo single)
            {
                info = single;
            }
            else if (e.Parameter is IEnumerable multiple)
            {
                FileSystemInfo[] infos = multiple.OfType<FileSystemInfo>().ToArray();
                info = infos?.ElementAtOrDefault(Helpers.RNG.Next(infos.Length));
            }

            if (info?.Exists != true)
            {
                return;
            }

            if (info.Attributes.HasFlag(FileAttributes.Directory))
            {
                // Selected a folder...
                SearchOption searchOption = WallpaperrLogic.AppSettings.IncludeSubdirectory ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var files = new List<FileInfo>();
                foreach (string pattern in Helpers.FileTypes)
                {
                    files.AddRange(((DirectoryInfo)info).GetFiles(pattern, searchOption));
                }

                string fullName = files.ElementAtOrDefault(Helpers.RNG.Next(files.Count))?.FullName;
                if (fullName != null)
                {
                    this.StartWork(fullName);
                }
                else
                {
                    // No files found.
                    const string msg =
@"No supported file types were found
in the selected folder.";
                    Helpers.ShowInfo(msg);
                }
            }
            else
            {
                // Selected a file...
                StartWork(info.FullName);
            }
        }
    }
}
