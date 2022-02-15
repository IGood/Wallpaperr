namespace Wallpaperr
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Windows.Forms;
	using ThumbDictionary = System.Collections.Generic.Dictionary<System.Windows.Forms.ListViewItem, System.Drawing.Image>;

	public partial class MainForm : Form
	{
		#region Internal Classes

		private class ImageCollectionSorter : System.Collections.IComparer
		{
			#region IComparer Implementation

			public int Compare(object x, object y)
			{
				var a = (ListViewItem)x;
				var b = (ListViewItem)y;

				// folders have index 0
				if (a.ImageIndex == 0)
				{
					// both folders? sort by name, otherwise folders first
					return (b.ImageIndex == 0) ? string.Compare(a.Text, b.Text) : -1;
				}

				// folders first, otherwise sort by name
				return (b.ImageIndex == 0) ? 1 : string.Compare(a.Text, b.Text);
			}

			#endregion
		}

		#endregion

		#region Member Fields / Properties

		private bool closeToTray = true;

		private AboutBox aboutBox;

		private WallpaperrLogic logic;

		private readonly string initialFile;

		public ListView ListView => this.listView;

		public bool Dirty
		{
			get { return this.buttonApply.Enabled; }
			set { this.buttonApply.Enabled = value; }
		}

		private MFLForm mflFormDialog_;

		private readonly ThumbDictionary thumbDict_ = new(0);

		private readonly System.Drawing.Font menuItemFontNormal_;
		private readonly System.Drawing.Font menuItemFontBold_;

		#endregion

		#region Constructors

		public MainForm(string file)
		{
			this.initialFile = file;

			this.InitializeComponent();

			this.Text = Properties.Resources.AppTitle;
			((Control)this.pictureBoxStyle).AllowDrop = true;
			this.comboBoxTimeUnit.DataSource = TimeUnit.Units;
			this.listView.ListViewItemSorter = new ImageCollectionSorter();

			this.menuItemFontNormal_ = this.newRandomWallpaperToolStripMenuItem1.Font;
			this.menuItemFontBold_ = this.settingsToolStripMenuItem.Font;
		}

		#endregion

		internal void SaveSettings(Properties.Settings settings)
		{
			settings.Style = (int)(this.radioSpiffy.Checked
				? BackgroundStyle.Spiffy
				: this.radioZoomOut.Checked
					? BackgroundStyle.ZoomOut
					: BackgroundStyle.ZoomIn);
			this.radioStyle_Click();

			settings.Thickness = this.numUDThick.Value;
			settings.Border = this.numUDSpace.Value;
			settings.BackgroundColor = this.pictureBoxColor.BackColor;

			settings.SingleMonitor = this.radioSingle.Checked;
			settings.SmartRandom = this.checkBoxSmartRand.Checked;

			settings.Active = this.checkBoxActive.Checked;

			settings.Interval = this.numUDInterval.Value;
			settings.TimerUnit = this.comboBoxTimeUnit.SelectedIndex;
			this.timer.Interval = (int)settings.Interval * TimeUnit.Units[settings.TimerUnit].Value;

			settings.IncludeSubdirectory = this.checkBoxSubdir.Checked;
			if (this.listView.Items.Count > 0)
			{
				WatcherSet.SetIncludeSubdirectories(this.listView.Items, settings.IncludeSubdirectory);
			}
		}

		internal void RestoreSettings(Properties.Settings settings)
		{
			this.showOnRunToolStripMenuItem.Checked = settings.ShowOnRun;
			this.doubleClicktoolStripComboBox.SelectedIndex = settings.DoubleClickIndex;

			switch ((BackgroundStyle)settings.Style)
			{
				case BackgroundStyle.Spiffy: this.radioSpiffy.Checked = true; break;
				case BackgroundStyle.ZoomOut: this.radioZoomOut.Checked = true; break;
				case BackgroundStyle.ZoomIn: this.radioZoomIn.Checked = true; break;
			}
			this.radioStyle_Click();

			this.numUDThick.Value = settings.Thickness;
			this.numUDSpace.Value = settings.Border;
			this.pictureBoxColor.BackColor = settings.BackgroundColor;

			this.checkBoxActive.Checked = settings.Active;
			this.pause_Click(this.checkBoxActive);

			this.numUDInterval.Value = settings.Interval;
			this.comboBoxTimeUnit.SelectedIndex = settings.TimerUnit;
			this.timer.Interval = (int)settings.Interval * TimeUnit.Units[settings.TimerUnit].Value;

			this.checkBoxSubdir.Checked = settings.IncludeSubdirectory;
			if (this.listView.Items.Count > 0)
			{
				WatcherSet.SetIncludeSubdirectories(this.listView.Items, settings.IncludeSubdirectory);
			}

			if (settings.SingleMonitor)
			{
				this.radioSingle.Checked = true;
			}
			else
			{
				this.radioMulti.Checked = true;
			}
			this.checkBoxSmartRand.Checked = settings.SmartRandom;
			this.radioDisplays_Click();
		}

		#region MainForm Events

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == SingleInstance.WM_SHOWFIRSTINSTANCE)
			{
				if (this.Visible)
				{
					SingleInstance.WinAPI.ShowToFront(this.Handle);
				}
				else
				{
					this.Show();
					this.WindowState = FormWindowState.Normal;
					this.Activate();
				}
			}
			else if (m.Msg == SingleInstance.WinAPI.WM_COPYDATA)
			{
				string file = SingleInstance.RecieveStringMessage(m);
				if (!string.IsNullOrEmpty(file))
				{
					this.StartWork(file);
				}
			}
			else
			{
				base.WndProc(ref m);
			}
		}

		private void MainForm_Load(object sender, EventArgs e)
		{
			this.logic = new WallpaperrLogic(this);

			if (!string.IsNullOrEmpty(this.initialFile))
			{
				this.StartWork(this.initialFile);
			}
		}

		private void MainForm_Shown(object sender, EventArgs e)
		{
			if (!WallpaperrLogic.AppSettings.ShowOnRun)
			{
				this.showOnRunToolStripMenuItem.Image = Properties.Resources.box_16x16;
				this.Hide();
			}
		}

		private void MainForm_VisibleChanged(object sender, EventArgs e)
		{
			// hiding?
			if (!this.Visible)
			{
				this.logic.RestoreSettings();
			}
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (e.CloseReason != CloseReason.UserClosing)
			{
				this.closeToTray = false;
			}

			if (this.closeToTray)
			{
				e.Cancel = true;
				this.Hide();
			}
			else
			{
				this.logic.SaveSettingsToDisk();

				this.notifyIcon.Dispose();
			}

			// dispose thumbnail images
			foreach (var img in this.thumbDict_.Values)
			{
				img.Dispose();
			}

			this.thumbDict_.Clear();
		}

		#endregion

		#region UI Events

		private void addFiles_Click(object sender, EventArgs e)
		{
			this.openFileDialogItem.ShowDialog();
		}

		private void addFolder_Click(object sender, EventArgs e)
		{
			using var ffd = new dnGREP.FileFolderDialog();
			if (ffd.ShowDialog() == DialogResult.OK)
			{
				this.logic.AddFolders(new[] { ffd.SelectedPath });
			}
		}

		private void newRandomWallpaper_Click(object sender, EventArgs e)
		{
			this.logic.NewWallpaper();
		}

		private void exit_Click(object sender, EventArgs e)
		{
			this.closeToTray = false;
			this.Close();
		}

		private void openCollection_Click(object sender, EventArgs e)
		{
			this.openFileDialogXml.ShowDialog();
		}

		private void saveCollectionAs_Click(object sender, EventArgs e)
		{
			this.saveFileDialogXml.ShowDialog();
		}

		private void refreshCollection_Click(object sender, EventArgs e)
		{
			this.logic.UpdateImageList();
		}

		private void viewMasterFileList_Click(object sender, EventArgs e)
		{
			this.mflFormDialog_ ??= new MFLForm(this.logic);
			this.mflFormDialog_.PopulateBox(this.logic.MasterFileList);
			this.mflFormDialog_.ShowDialog();
			if (this.mflFormDialog_.FileName != null)
			{
				this.StartWork(this.mflFormDialog_.FileName);
			}
		}

		private void showOnRun_Click(object sender, EventArgs e)
		{
			this.showOnRunToolStripMenuItem.Image = this.showOnRunToolStripMenuItem.Checked
				? Properties.Resources.box_check_16x16
				: Properties.Resources.box_16x16;

			WallpaperrLogic.AppSettings.ShowOnRun = this.showOnRunToolStripMenuItem.Checked;
		}

		private void doubleClick_SelectedIndexChanged(object sender, EventArgs e)
		{
			foreach (var handler in new EventHandler[] { this.settings_Click, this.newRandomWallpaper_Click, this.pause_Click })
			{
				this.notifyIcon.DoubleClick -= handler;
			}

			foreach (var menuItem in new[] { this.settingsToolStripMenuItem, this.newRandomWallpaperToolStripMenuItem1, this.pauseToolStripMenuItem })
			{
				menuItem.Font = this.menuItemFontNormal_;
			}

			switch (this.doubleClicktoolStripComboBox.SelectedIndex)
			{
				// Open Settings
				case 0:
					this.notifyIcon.DoubleClick += this.settings_Click;
					this.settingsToolStripMenuItem.Font = this.menuItemFontBold_;
					break;

				// New Random Wallpaper
				case 1:
					this.notifyIcon.DoubleClick += this.newRandomWallpaper_Click;
					this.newRandomWallpaperToolStripMenuItem1.Font = this.menuItemFontBold_;
					break;

				// Pause
				case 2:
					this.notifyIcon.DoubleClick += this.pause_Click;
					this.pauseToolStripMenuItem.Font = this.menuItemFontBold_;
					break;
			}

			// must check for null, because this method is called (indirectly)
			// from logic constructor
			if (this.logic != null)
			{
				WallpaperrLogic.AppSettings.DoubleClickIndex = this.doubleClicktoolStripComboBox.SelectedIndex;
			}
		}

		private void useLegacyTiling_Click(object sender, EventArgs e)
		{
			this.useLegacyTilingToolStripMenuItem.Image = this.useLegacyTilingToolStripMenuItem.Checked
				? Properties.Resources.box_check_16x16
				: Properties.Resources.box_16x16;

			WallpaperrLogic.AppSettings.UseLegacyTiling = this.useLegacyTilingToolStripMenuItem.Checked;
		}

		private void help_Click(object sender, EventArgs e)
		{
			Help.ShowHelp(this, "https://github.com/IGood/Wallpaperr");
		}

		private void aboutWallpaperr_Click(object sender, EventArgs e)
		{
			this.aboutBox ??= new AboutBox();

			// already open somewhere?
			if (this.aboutBox.Visible)
			{
				this.aboutBox.Activate();
			}
			else
			{
				this.aboutBox.ShowDialog();
			}
		}

		private void settings_Click(object sender, EventArgs e)
		{
			this.Show();
			this.WindowState = FormWindowState.Normal;
			this.Activate();
		}

		private void pause_Click(object sender, EventArgs e = null)
		{
			bool active = (sender == this.checkBoxActive)
				? this.checkBoxActive.Checked
				: this.pauseToolStripMenuItem.Text.Equals("Unpause");

			this.checkBoxActive.Checked = active;

			// menu item text & image
			this.pauseToolStripMenuItem.Text = active
				? "Pause"
				: "Unpause";
			this.pauseToolStripMenuItem.Image = active
				? Properties.Resources.pause_16x16
				: Properties.Resources.play_16x16;

			// timer on/off
			this.timer.Enabled = active;
			if (active)
			{
				this.timer.Start();
			}
			else
			{
				this.timer.Stop();
			}

			// enable/disable controls
			this.label6.Enabled =
			this.numUDInterval.Enabled =
			this.comboBoxTimeUnit.Enabled = active;

			// change icon
			this.notifyIcon.Icon = active
				? Properties.Resources.small
				: Properties.Resources.smallPaused;

			// window closed?
			if (!this.Visible)
			{
				// helpers.appsettings.active = active;
				this.Dirty = false;
			}
		}

		private void newWallpaper_Click(object sender, EventArgs e)
		{
			this.ActivateFocusedItem();
		}

		private void openItem_Click(object sender, EventArgs e)
		{
			// open each folder/file
			foreach (ListViewItem item in this.listView.SelectedItems)
			{
				string fullName = item.ToolTipText;
				if (Directory.Exists(fullName) || File.Exists(fullName))
				{
					Process.Start(new ProcessStartInfo
					{
						FileName = fullName,
						UseShellExecute = true,
					});
				}
			}
		}

		private void openContainingFolder_Click(object sender, EventArgs e)
		{
			// open containing folder for each folder/file
			foreach (ListViewItem item in this.listView.SelectedItems)
			{
				string fullName = item.ToolTipText;
				if (Directory.Exists(fullName) || File.Exists(fullName))
				{
					Process.Start("explorer", $"/select,\"{fullName}\"");
				}
			}
		}

		private void removeItem_Click(object sender, EventArgs e)
		{
			this.logic.RemoveSelectedItems();
		}

		private void color_Click(object sender, EventArgs e)
		{
			this.colorDialog.Color = this.pictureBoxColor.BackColor;
			if (this.colorDialog.ShowDialog() == DialogResult.OK)
			{
				this.pictureBoxColor.BackColor = this.colorDialog.Color;
			}
		}

		private void control_Changed(object sender, EventArgs e)
		{
			this.Dirty = true;
		}

		private void ok_Click(object sender, EventArgs e)
		{
			this.logic.SaveSettings();
			this.Hide();
		}

		private void cancel_Click(object sender, EventArgs e)
		{
			this.Hide();
		}

		private void apply_Click(object sender, EventArgs e)
		{
			this.logic.SaveSettings();
			this.logic.NewWallpaper();
		}

		private void radioStyle_Click(object sender = null, EventArgs e = null)
		{
			this.label1.Enabled =
			this.label2.Enabled =
			this.label7.Enabled =
			this.numUDThick.Enabled =
			this.numUDColor.Enabled = this.radioSpiffy.Checked;

			this.label3.Enabled =
			this.label4.Enabled =
			this.label5.Enabled =
			this.pictureBoxColor.Enabled =
			this.numUDSpace.Enabled = this.radioSpiffy.Checked || this.radioZoomOut.Checked;

			this.pictureBoxColor.BorderStyle = this.pictureBoxColor.Enabled
				? BorderStyle.Fixed3D
				: BorderStyle.None;

			this.pictureBoxStyle.Image = this.radioSpiffy.Checked
				? Properties.Resources.spiffy
				: this.radioZoomOut.Checked
					? Properties.Resources.zoom_out
					: Properties.Resources.zoom_in;
		}

		private void radioDisplays_Click(object sender = null, EventArgs e = null)
		{
			this.checkBoxSmartRand.Enabled = this.radioMulti.Checked;
		}

		#endregion

		#region PictureBox Style Events

		private void pictureBoxStyle_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
			{
				e.Effect = DragDropEffects.Copy;
			}
		}

		private void pictureBoxStyle_DragDrop(object sender, DragEventArgs e)
		{
			string filePath = (e.Data.GetData(DataFormats.FileDrop) as string[])?.FirstOrDefault();
			if (filePath != null)
			{
				this.StartWork(filePath);
			}
		}

		#endregion

		public void StartWork(string fileName)
		{
			// currently working?
			if (this.backgroundWorker.IsBusy)
			{
				Helpers.ShowBusy();
				return;
			}

			this.timer.Stop();

			this.notifyIcon.Text = "Wallpaperr [busy]";
			this.notifyIcon.Icon = Properties.Resources.smallBusy;

			this.Text = "Wallpaperr - Automatic Wallpaper Changer [busy]";

			this.progressBar.Value = 0;
			this.progressBar.Show();

			// start operation
			this.backgroundWorker.RunWorkerAsync(fileName);
		}

		#region ListView Callbacks

		private void contextMenuItem_Opening(object sender, CancelEventArgs e)
		{
			// nothing selected?
			if (this.listView.SelectedItems.Count == 0)
			{
				e.Cancel = true;
				return;
			}

			this.contextMenuItem.Items[0].Image = Properties.Resources.random_16x16;

			if (this.listView.SelectedItems.Count == 1)
			{
				ListViewItem item = this.listView.FocusedItem;
				var fileInfo = new FileInfo(item.ToolTipText);
				if (fileInfo.Exists)
				{
					if (!this.thumbDict_.TryGetValue(item, out var img))
					{
						using var temp = System.Drawing.Image.FromFile(fileInfo.FullName);
						const float MaxDims = 150;
						float scale = Math.Min(MaxDims / temp.Width, MaxDims / temp.Height);
						float thumbW = temp.Width * scale;
						float thumbH = temp.Height * scale;
						img = new System.Drawing.Bitmap(temp, (int)thumbW, (int)thumbH);
						this.thumbDict_.Add(item, img);
					}

					this.contextMenuItem.Items[0].ImageScaling = ToolStripItemImageScaling.None;
					this.contextMenuItem.Items[0].Image = img;
				}
			}
		}

		private void listView_ItemActivate(object sender, EventArgs e)
		{
			this.ActivateFocusedItem();
		}

		private void listView_DragEnter(object sender, DragEventArgs e)
		{
			// dragging folders/files?
			if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
			{
				e.Effect = DragDropEffects.Copy;
			}
		}

		private void listView_DragDrop(object sender, DragEventArgs e)
		{
			string[] dropped = e.Data.GetData(DataFormats.FileDrop) as string[];

			// dropped on a folder?
			var p = this.listView.PointToClient(new System.Drawing.Point(e.X, e.Y));
			var lvi = this.listView.GetItemAt(p.X, p.Y);
			if (lvi != null)
			{
				string path = lvi.ToolTipText;
				if (Directory.Exists(path))
				{
					DialogResult dRes = MessageBox.Show(
						"Move file here?",
						"Wallpaperr",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Question);
					if (dRes == DialogResult.Yes)
					{
						MessageBox.Show("(U_U)");
					}
				}
			}
			else
			{
				this.logic.AddItemsFromArray(dropped);
			}
		}

		private void listView_KeyDown(object sender, KeyEventArgs e)
		{
			e.Handled = this.listView_KeyDown_KeyData(e.KeyData);

			if (!e.Handled)
			{
				e.Handled = this.listView_KeyDown_KeyCode(e.KeyCode);
			}
		}

		private bool listView_KeyDown_KeyData(Keys KeyData)
		{
			switch (KeyData)
			{
				case Keys.A | Keys.Control:
					foreach (ListViewItem item in this.listView.Items)
					{
						item.Selected = true;
					}
					break;
				default:
					return false;
			}
			return true;
		}

		private bool listView_KeyDown_KeyCode(Keys KeyCode)
		{
			switch (KeyCode)
			{
				case Keys.Delete:
					this.logic.RemoveSelectedItems();
					break;
				default:
					return false;
			}
			return true;
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
			this.progressBar.Value = Math.Min(e.ProgressPercentage, 100);
		}

		private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				MessageBox.Show(e.Error.Message, "BackgroundWorker Error");
			}

			this.progressBar.Hide();

			this.timer.Start();
			this.timer.Enabled = WallpaperrLogic.AppSettings.Active;

			this.notifyIcon.Text = "Wallpaperr";
			this.notifyIcon.Icon = WallpaperrLogic.AppSettings.Active
				? Properties.Resources.small
				: Properties.Resources.smallPaused;

			this.Text = "Wallpaperr - Automatic Wallpaper Changer";
		}

		#endregion

		#region File Dialog Callbacks

		private void openFileDialogItem_FileOk(object sender, CancelEventArgs e)
		{
			this.logic.AddFiles(this.openFileDialogItem.FileNames);
		}

		private void openFileDialogXML_FileOk(object sender, CancelEventArgs e)
		{
			this.logic.OpenCollection(this.openFileDialogXml.FileName);
		}

		private void saveFileDialogXML_FileOk(object sender, CancelEventArgs e)
		{
			var items = new List<string>(this.listView.Items.Count);

			// get items' full names
			foreach (ListViewItem item in this.listView.Items)
			{
				items.Add(item.ToolTipText);
			}

			// save list to file
			using FileStream fs = File.Open(this.saveFileDialogXml.FileName, FileMode.Create);
			var serializer = new System.Xml.Serialization.XmlSerializer(items.GetType());
			serializer.Serialize(fs, items);
			fs.Flush();
		}

		#endregion

		public void ShowEmptyCollection()
		{
			this.timer.Stop();

			var msg =
@"You do not have any images in your collection.
Would you like to add some files now?";

			DialogResult result = MessageBox.Show(msg, "Hold it!", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (result == DialogResult.Yes)
			{
				this.openFileDialogItem.ShowDialog();
			}

			this.timer.Start();
			this.timer.Enabled = WallpaperrLogic.AppSettings.Active;
		}

		private void ActivateFocusedItem()
		{
			/*
			ListViewItem item = this.listView_.FocusedItem;
			/*/
			int index = Helpers.RNG.Next(this.listView.SelectedItems.Count);
			ListViewItem item = this.listView.SelectedItems[index];
			//*/

			// selected a file?
			var fileInfo = new FileInfo(item.ToolTipText);
			if (fileInfo.Exists)
			{
				this.StartWork(fileInfo.FullName);
				return;
			}

			// selected a folder?
			var dirInfo = new DirectoryInfo(item.ToolTipText);
			if (dirInfo.Exists)
			{
				// get contained files
				SearchOption searchOption = WallpaperrLogic.AppSettings.IncludeSubdirectory ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
				var files = new List<FileInfo>();
				foreach (string pattern in Helpers.FileTypes)
				{
					files.AddRange(dirInfo.GetFiles(pattern, searchOption));
				}

				// no files found?
				if (files.Count == 0)
				{
					var msg =
@"No supported file types were found
in the selected folder.";
					Helpers.ShowInfo(msg);
				}
				else
				{
					index = Helpers.RNG.Next(files.Count);
					this.StartWork(files[index].FullName);
				}
				return;
			}

			// file/folder no longer exists, clean up & remove it
			(item.Tag as WatcherSet)?.Dispose();
			item.Remove();
		}
	}
}
