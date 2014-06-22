namespace Wallpaperr
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.IO;
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
					return (b.ImageIndex == 0) ? String.Compare(a.Text, b.Text) : -1;
				}

				// folders first, otherwise sort by name
				return (b.ImageIndex == 0) ? 1 : String.Compare(a.Text, b.Text);
			}

			#endregion
		}

		#endregion

		#region Member Fields / Properties

		private bool closeToTray = true;

		private AboutBox aboutBox;

		private WallpaperrLogic logic;

		private string initialFile;

		public ListView ListView
		{
			get { return this.listView; }
		}

		public bool Dirty
		{
			get { return this.buttonApply.Enabled; }
			set { this.buttonApply.Enabled = value; }
		}

		private MFLForm mflFormDialog_;

		private ThumbDictionary thumbDict_ = new ThumbDictionary(0);

		private System.Drawing.Font menuItemFontNormal_;
		private System.Drawing.Font menuItemFontBold_;

		#endregion

		#region Constructors

		public MainForm(string file)
		{
			this.initialFile = file;

			InitializeComponent();

			this.Text = Properties.Resources.AppTitle;
			((Control)this.pictureBoxStyle).AllowDrop = true;
			this.comboBoxTimeUnit.DataSource = TimeUnit.Units;
			this.listView.ListViewItemSorter = new ImageCollectionSorter();

			menuItemFontNormal_ = newRandomWallpaperToolStripMenuItem1.Font;
			menuItemFontBold_ = settingsToolStripMenuItem.Font;
		}

		#endregion

		internal void SaveSettings(Properties.Settings settings)
		{
			settings.Style = (int)(radioSpiffy.Checked
				? BackgroundStyle.Spiffy
				: radioZoomOut.Checked
					? BackgroundStyle.ZoomOut
					: BackgroundStyle.ZoomIn);
			radioStyle_Click(null, null);

			settings.Thickness = numUDThick.Value;
			settings.Border = numUDSpace.Value;
			settings.BackgroundColor = pictureBoxColor.BackColor;

			settings.SingleMonitor = radioSingle.Checked;
			settings.SmartRandom = checkBoxSmartRand.Checked;

			settings.Active = checkBoxActive.Checked;

			settings.Interval = numUDInterval.Value;
			settings.TimerUnit = comboBoxTimeUnit.SelectedIndex;
			timer.Interval = (int)settings.Interval * TimeUnit.Units[settings.TimerUnit].Value;

			settings.IncludeSubdirectory = checkBoxSubdir.Checked;
			if (this.listView.Items.Count > 0)
			{
				WatcherSet.SetIncludeSubdirectories(this.listView.Items, settings.IncludeSubdirectory);
			}
		}

		internal void RestoreSettings(Properties.Settings settings)
		{
			showOnRunToolStripMenuItem.Checked = settings.ShowOnRun;
			doubleClicktoolStripComboBox.SelectedIndex = settings.DoubleClickIndex;

			switch ((BackgroundStyle)settings.Style)
			{
				case BackgroundStyle.Spiffy: radioSpiffy.Checked = true; break;
				case BackgroundStyle.ZoomOut: radioZoomOut.Checked = true; break;
				case BackgroundStyle.ZoomIn: radioZoomIn.Checked = true; break;
			}
			radioStyle_Click(null, null);

			numUDThick.Value = settings.Thickness;
			numUDSpace.Value = settings.Border;
			pictureBoxColor.BackColor = settings.BackgroundColor;

			checkBoxActive.Checked = settings.Active;
			pause_Click(checkBoxActive, null);

			numUDInterval.Value = settings.Interval;
			comboBoxTimeUnit.SelectedIndex = settings.TimerUnit;
			timer.Interval = (int)settings.Interval * TimeUnit.Units[settings.TimerUnit].Value;

			checkBoxSubdir.Checked = settings.IncludeSubdirectory;
			if (this.listView.Items.Count > 0)
			{
				WatcherSet.SetIncludeSubdirectories(this.listView.Items, settings.IncludeSubdirectory);
			}

			if (settings.SingleMonitor)
			{
				radioSingle.Checked = true;
			}
			else
			{
				radioMulti.Checked = true;
			}
			checkBoxSmartRand.Checked = settings.SmartRandom;
			radioDisplays_Click(null, null);
		}

		#region MainForm Events

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == SingleInstance.WM_SHOWFIRSTINSTANCE)
			{
				if (Visible)
				{
					SingleInstance.WinAPI.ShowToFront(Handle);
				}
				else
				{
					Show();
					WindowState = FormWindowState.Normal;
					Activate();
				}
			}
			else if (m.Msg == SingleInstance.WinAPI.WM_COPYDATA)
			{
				string file = SingleInstance.RecieveStringMessage(m);
				if (!String.IsNullOrEmpty(file))
				{
					StartWork(file);
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

			if (!String.IsNullOrEmpty(this.initialFile))
			{
				StartWork(this.initialFile);
			}
		}

		private void MainForm_Shown(object sender, EventArgs e)
		{
			if (!WallpaperrLogic.AppSettings.ShowOnRun)
			{
				showOnRunToolStripMenuItem.Image = Properties.Resources.box_16x16;
				base.Hide();
			}
		}

		private void MainForm_VisibleChanged(object sender, EventArgs e)
		{
			// hiding?
			if (!Visible)
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
				Hide();
			}
			else
			{
				this.logic.SaveSettingsToDisk();

				notifyIcon.Dispose();
			}

			// dispose thumbnail images
			foreach (var img in thumbDict_.Values)
			{
				using (img) { }
			}
			thumbDict_.Clear();
		}

		#endregion

		#region UI Events

		private void addFiles_Click(object sender, EventArgs e)
		{
			openFileDialogItem.ShowDialog();
		}

		private void addFolder_Click(object sender, EventArgs e)
		{
			/*
			if ( folderBrowserDialog_.ShowDialog() == DialogResult.OK )
			{
				string[] folders = new string[] { folderBrowserDialog_.SelectedPath };
				this.logic.AddFolders( folders );
			}
			/*/
			using (var ffd = new dnGREP.FileFolderDialog())
			{
				if (ffd.ShowDialog() == DialogResult.OK)
				{
					string[] folders = new string[] { ffd.SelectedPath };
					this.logic.AddFolders(folders);
				}
			}
			//*/
		}

		private void newRandomWallpaper_Click(object sender, EventArgs e)
		{
			this.logic.NewWallpaper();
		}

		private void exit_Click(object sender, EventArgs e)
		{
			this.closeToTray = false;
			Close();
		}

		private void openCollection_Click(object sender, EventArgs e)
		{
			openFileDialogXml.ShowDialog();
		}

		private void saveCollectionAs_Click(object sender, EventArgs e)
		{
			saveFileDialogXml.ShowDialog();
		}

		private void refreshCollection_Click(object sender, EventArgs e)
		{
			this.logic.UpdateImageList();
		}

		private void viewMasterFileList_Click(object sender, EventArgs e)
		{
			if (mflFormDialog_ == null)
			{
				mflFormDialog_ = new MFLForm(this.logic);
			}

			mflFormDialog_.PopulateBox(this.logic.MasterFileList);
			mflFormDialog_.ShowDialog();
			if (mflFormDialog_.FileName != null)
			{
				StartWork(mflFormDialog_.FileName);
			}
		}

		private void showOnRun_Click(object sender, EventArgs e)
		{
			showOnRunToolStripMenuItem.Image = showOnRunToolStripMenuItem.Checked
				? Properties.Resources.box_check_16x16
				: Properties.Resources.box_16x16;

			WallpaperrLogic.AppSettings.ShowOnRun = showOnRunToolStripMenuItem.Checked;
		}

		private void doubleClick_SelectedIndexChanged(object sender, EventArgs e)
		{
			notifyIcon.DoubleClick -= settings_Click;
			notifyIcon.DoubleClick -= newRandomWallpaper_Click;
			notifyIcon.DoubleClick -= pause_Click;

			settingsToolStripMenuItem.Font = menuItemFontNormal_;
			newRandomWallpaperToolStripMenuItem1.Font = menuItemFontNormal_;
			pauseToolStripMenuItem.Font = menuItemFontNormal_;

			switch (doubleClicktoolStripComboBox.SelectedIndex)
			{
				#region Open Settings

				case 0:
					notifyIcon.DoubleClick += settings_Click;
					settingsToolStripMenuItem.Font = menuItemFontBold_;
					break;

				#endregion

				#region New Random Wallpaper

				case 1:
					notifyIcon.DoubleClick += newRandomWallpaper_Click;
					newRandomWallpaperToolStripMenuItem1.Font = menuItemFontBold_;
					break;

				#endregion

				#region Pause

				case 2:
					notifyIcon.DoubleClick += pause_Click;
					pauseToolStripMenuItem.Font = menuItemFontBold_;
					break;

				#endregion
			}

			// must check for null, because this method is called (indirectly)
			// from logic constructor
			if (this.logic != null)
			{
				WallpaperrLogic.AppSettings.DoubleClickIndex = doubleClicktoolStripComboBox.SelectedIndex;
			}
		}

		private void help_Click(object sender, EventArgs e)
		{
			Help.ShowHelp(this, "http://www.tinyurl.com/wallpaperr");
		}

		private void aboutWallpaperr_Click(object sender, EventArgs e)
		{
			if (this.aboutBox == null)
			{
				this.aboutBox = new AboutBox();
			}

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
			Show();
			WindowState = FormWindowState.Normal;
			Activate();
		}

		private void pause_Click(object sender, EventArgs e)
		{
			bool active = (sender == checkBoxActive)
				? checkBoxActive.Checked
				: pauseToolStripMenuItem.Text.Equals("Unpause");

			checkBoxActive.Checked = active;

			// menu item text & image
			pauseToolStripMenuItem.Text = active
				? "Pause"
				: "Unpause";
			pauseToolStripMenuItem.Image = active
				? Properties.Resources.pause_16x16
				: Properties.Resources.play_16x16;

			// timer on/off
			timer.Enabled = active;
			if (active)
			{
				timer.Start();
			}
			else
			{
				timer.Stop();
			}

			// enable/disable controls
			label6.Enabled =
			numUDInterval.Enabled =
			comboBoxTimeUnit.Enabled = active;

			// change icon
			notifyIcon.Icon = active
				? Properties.Resources.small
				: Properties.Resources.smallPaused;

			// window closed?
			if (!this.Visible)
			{
				// helpers.appsettings.active = active;
				Dirty = false;
			}
		}

		private void newWallpaper_Click(object sender, EventArgs e)
		{
			ActivateFocusedItem();
		}

		private void openItem_Click(object sender, EventArgs e)
		{
			// open each folder/file
			foreach (ListViewItem item in this.listView.SelectedItems)
			{
				string fullName = item.ToolTipText;
				if (Directory.Exists(fullName) || File.Exists(fullName))
				{
					Process.Start(fullName);
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
					Process.Start("explorer", String.Format("/select,\"{0}\"", fullName));
				}
			}
		}

		private void removeItem_Click(object sender, EventArgs e)
		{
			this.logic.RemoveSelectedItems();
		}

		private void color_Click(object sender, EventArgs e)
		{
			colorDialog.Color = pictureBoxColor.BackColor;
			if (colorDialog.ShowDialog() == DialogResult.OK)
			{
				pictureBoxColor.BackColor = colorDialog.Color;
			}
		}

		private void control_Changed(object sender, EventArgs e)
		{
			Dirty = true;
		}

		private void ok_Click(object sender, EventArgs e)
		{
			this.logic.SaveSettings();
			base.Hide();
		}

		private void cancel_Click(object sender, EventArgs e)
		{
			Hide();
		}

		private void apply_Click(object sender, EventArgs e)
		{
			this.logic.SaveSettings();
			this.logic.NewWallpaper();
		}

		private void radioStyle_Click(object sender, EventArgs e)
		{
			label1.Enabled =
			label2.Enabled =
			label7.Enabled =
			numUDThick.Enabled =
			numUDColor.Enabled = radioSpiffy.Checked;

			label3.Enabled =
			label4.Enabled =
			label5.Enabled =
			pictureBoxColor.Enabled =
			numUDSpace.Enabled = radioSpiffy.Checked || radioZoomOut.Checked;

			pictureBoxColor.BorderStyle = pictureBoxColor.Enabled
				? BorderStyle.Fixed3D
				: BorderStyle.None;

			pictureBoxStyle.Image = radioSpiffy.Checked
				? Properties.Resources.spiffy
				: radioZoomOut.Checked
					? Properties.Resources.zoom_out
					: Properties.Resources.zoom_in;
		}

		private void radioDisplays_Click(object sender, EventArgs e)
		{
			checkBoxSmartRand.Enabled = radioMulti.Checked;
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
			string[] dropped = e.Data.GetData(DataFormats.FileDrop) as string[];
			if (dropped != null && dropped.Length > 0)
			{
				string filePath = dropped[0];
				StartWork(filePath);
			}
		}

		#endregion

		public void StartWork(string fileName)
		{
			// currently working?
			if (backgroundWorker.IsBusy)
			{
				Helpers.ShowBusy();
				return;
			}

			timer.Stop();

			notifyIcon.Text = "Wallpaperr [busy]";
			notifyIcon.Icon = Properties.Resources.smallBusy;

			this.Text = "Wallpaperr - Automatic Wallpaper Changer [busy]";

			progressBar.Value = 0;
			progressBar.Show();

			// start operation
			backgroundWorker.RunWorkerAsync(fileName);
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
		}

		private void listView_ItemActivate(object sender, EventArgs e)
		{
			ActivateFocusedItem();
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
			progressBar.Value = Math.Min(e.ProgressPercentage, 100);
		}

		private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				MessageBox.Show(e.Error.Message, "BackgroundWorker Error");
			}

			progressBar.Hide();

			timer.Start();
			timer.Enabled = WallpaperrLogic.AppSettings.Active;

			notifyIcon.Text = "Wallpaperr";
			notifyIcon.Icon = WallpaperrLogic.AppSettings.Active
				? Properties.Resources.small
				: Properties.Resources.smallPaused;

			this.Text = "Wallpaperr - Automatic Wallpaper Changer";
		}

		#endregion

		#region File Dialog Callbacks

		private void openFileDialogItem_FileOk(object sender, CancelEventArgs e)
		{
			this.logic.AddFiles(openFileDialogItem.FileNames);
		}

		private void openFileDialogXML_FileOk(object sender, CancelEventArgs e)
		{
			this.logic.OpenCollection(openFileDialogXml.FileName);
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
			using (FileStream fs = File.Open(saveFileDialogXml.FileName, FileMode.Create))
			{
				var serializer = new System.Xml.Serialization.XmlSerializer(items.GetType());
				serializer.Serialize(fs, items);
				fs.Flush();
			}
		}

		#endregion

		public void ShowEmptyCollection()
		{
			timer.Stop();

			var msg =
@"You do not have any images in your collection.
Would you like to add some files now?";

			DialogResult result = MessageBox.Show(msg, "Hold it!", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (result == DialogResult.Yes)
			{
				openFileDialogItem.ShowDialog();
			}

			timer.Start();
			timer.Enabled = WallpaperrLogic.AppSettings.Active;
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
			FileInfo fileInfo = new FileInfo(item.ToolTipText);
			if (fileInfo.Exists)
			{
				StartWork(fileInfo.FullName);
				return;
			}

			// selected a folder?
			DirectoryInfo dirInfo = new DirectoryInfo(item.ToolTipText);
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
					StartWork(files[index].FullName);
				}
				return;
			}

			// file/folder no longer exists, clean up & remove it
			using (item.Tag as WatcherSet) { }
			item.Remove();
		}
	}
}
