using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace Wallpaperr
{
	using ThumbDictionary = Dictionary<ListViewItem, System.Drawing.Image>;

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

		private bool closeToTray_ = true;
		private AboutBox aboutBox_;
		private WallpaperrLogic logic_ = null;

		private string initialFile_ = null;

		public ListView ListView
		{
			get { return listView_; }
		}

		public bool Dirty
		{
			get { return buttonApply_.Enabled; }
			set { buttonApply_.Enabled = value; }
		}

		private MFLForm mflFormDialog_;

		private ThumbDictionary thumbDict_ = new ThumbDictionary(0);

		private System.Drawing.Font menuItemFontNormal_;
		private System.Drawing.Font menuItemFontBold_;

		#endregion

		#region Constructors

		public MainForm(string file)
		{
			initialFile_ = file;

			InitializeComponent();

			this.Text = Properties.Resources.AppTitle;
			((Control)this.pictureBoxStyle_).AllowDrop = true;
			this.comboBoxTimeUnit_.DataSource = TimeUnit.Units;
			this.listView_.ListViewItemSorter = new ImageCollectionSorter();

			menuItemFontNormal_ = newRandomWallpaperToolStripMenuItem1.Font;
			menuItemFontBold_ = settingsToolStripMenuItem.Font;
		}

		#endregion

		internal void SaveSettings(Properties.Settings settings)
		{
			settings.Style = (int)(radioSpiffy_.Checked
				? BackgroundStyle.Spiffy
				: radioZoomOut_.Checked
					? BackgroundStyle.ZoomOut
					: BackgroundStyle.ZoomIn);
			radioStyle_Click(null, null);

			settings.Thickness = numUDThick_.Value;
			settings.Border = numUDSpace_.Value;
			settings.BackgroundColor = pictureBoxColor_.BackColor;

			settings.SingleMonitor = radioSingle_.Checked;
			settings.SmartRandom = checkBoxSmartRand_.Checked;

			settings.Active = checkBoxActive_.Checked;

			settings.Interval = numUDInterval_.Value;
			settings.TimerUnit = comboBoxTimeUnit_.SelectedIndex;
			timer_.Interval = (int)settings.Interval * TimeUnit.Units[settings.TimerUnit].Value;

			settings.IncludeSubdirectory = checkBoxSubdir_.Checked;
			if (listView_.Items.Count > 0)
			{
				WatcherSet.SetIncludeSubdirectories(listView_.Items, settings.IncludeSubdirectory);
			}
		}

		internal void RestoreSettings(Properties.Settings settings)
		{
			showOnRunToolStripMenuItem.Checked = settings.ShowOnRun;
			doubleClicktoolStripComboBox.SelectedIndex = settings.DoubleClickIndex;

			switch ((BackgroundStyle)settings.Style)
			{
				case BackgroundStyle.Spiffy: radioSpiffy_.Checked = true; break;
				case BackgroundStyle.ZoomOut: radioZoomOut_.Checked = true; break;
				case BackgroundStyle.ZoomIn: radioZoomIn_.Checked = true; break;
			}
			radioStyle_Click(null, null);

			numUDThick_.Value = settings.Thickness;
			numUDSpace_.Value = settings.Border;
			pictureBoxColor_.BackColor = settings.BackgroundColor;

			checkBoxActive_.Checked = settings.Active;
			pause_Click(checkBoxActive_, null);

			numUDInterval_.Value = settings.Interval;
			comboBoxTimeUnit_.SelectedIndex = settings.TimerUnit;
			timer_.Interval = (int)settings.Interval * TimeUnit.Units[settings.TimerUnit].Value;

			checkBoxSubdir_.Checked = settings.IncludeSubdirectory;
			if (listView_.Items.Count > 0)
			{
				WatcherSet.SetIncludeSubdirectories(listView_.Items, settings.IncludeSubdirectory);
			}

			if (settings.SingleMonitor)
			{
				radioSingle_.Checked = true;
			}
			else
			{
				radioMulti_.Checked = true;
			}
			checkBoxSmartRand_.Checked = settings.SmartRandom;
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
			logic_ = new WallpaperrLogic(this);

			if (!String.IsNullOrEmpty(initialFile_))
			{
				StartWork(initialFile_);
			}
		}

		private void MainForm_Shown(object sender, EventArgs e)
		{
			if (!logic_.AppSettings.ShowOnRun)
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
				logic_.RestoreSettings();
			}
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (e.CloseReason != CloseReason.UserClosing)
			{
				closeToTray_ = false;
			}

			if (closeToTray_)
			{
				e.Cancel = true;
				Hide();
			}
			else
			{
				logic_.SaveSettingsToDisk();

				notifyIcon_.Dispose();
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
			openFileDialogItem_.ShowDialog();
		}

		private void addFolder_Click(object sender, EventArgs e)
		{
			/*
			if ( folderBrowserDialog_.ShowDialog() == DialogResult.OK )
			{
				string[] folders = new string[] { folderBrowserDialog_.SelectedPath };
				logic_.AddFolders( folders );
			}
			/*/
			using (var ffd = new dnGREP.FileFolderDialog())
			{
				if (ffd.ShowDialog() == DialogResult.OK)
				{
					string[] folders = new string[] { ffd.SelectedPath };
					logic_.AddFolders(folders);
				}
			}
			//*/
		}

		private void newRandomWallpaper_Click(object sender, EventArgs e)
		{
			logic_.NewWallpaper();
		}

		private void exit_Click(object sender, EventArgs e)
		{
			closeToTray_ = false;
			Close();
		}

		private void openCollection_Click(object sender, EventArgs e)
		{
			openFileDialogXML_.ShowDialog();
		}

		private void saveCollectionAs_Click(object sender, EventArgs e)
		{
			saveFileDialogXML_.ShowDialog();
		}

		private void refreshCollection_Click(object sender, EventArgs e)
		{
			logic_.UpdateImageList();
		}

		private void viewMasterFileList_Click(object sender, EventArgs e)
		{
			if (mflFormDialog_ == null)
			{
				mflFormDialog_ = new MFLForm(logic_);
			}

			mflFormDialog_.PopulateBox(logic_.MasterFileList);
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

			logic_.AppSettings.ShowOnRun = showOnRunToolStripMenuItem.Checked;
		}

		private void doubleClick_SelectedIndexChanged(object sender, EventArgs e)
		{
			notifyIcon_.DoubleClick -= settings_Click;
			notifyIcon_.DoubleClick -= newRandomWallpaper_Click;
			notifyIcon_.DoubleClick -= pause_Click;

			settingsToolStripMenuItem.Font = menuItemFontNormal_;
			newRandomWallpaperToolStripMenuItem1.Font = menuItemFontNormal_;
			pauseToolStripMenuItem.Font = menuItemFontNormal_;

			switch (doubleClicktoolStripComboBox.SelectedIndex)
			{
				#region Open Settings

				case 0:
					notifyIcon_.DoubleClick += settings_Click;
					settingsToolStripMenuItem.Font = menuItemFontBold_;
					break;

				#endregion

				#region New Random Wallpaper

				case 1:
					notifyIcon_.DoubleClick += newRandomWallpaper_Click;
					newRandomWallpaperToolStripMenuItem1.Font = menuItemFontBold_;
					break;

				#endregion

				#region Pause

				case 2:
					notifyIcon_.DoubleClick += pause_Click;
					pauseToolStripMenuItem.Font = menuItemFontBold_;
					break;

				#endregion
			}

			// must check for null, because this method is called (indirectly)
			// from logic constructor
			if (logic_ != null)
			{
				logic_.AppSettings.DoubleClickIndex = doubleClicktoolStripComboBox.SelectedIndex;
			}
		}

		private void help_Click(object sender, EventArgs e)
		{
			Help.ShowHelp(this, "http://www.tinyurl.com/wallpaperr");
		}

		private void aboutWallpaperr_Click(object sender, EventArgs e)
		{
			if (aboutBox_ == null)
			{
				aboutBox_ = new AboutBox();
			}

			// already open somewhere?
			if (aboutBox_.Visible)
			{
				aboutBox_.Activate();
			}
			else
			{
				aboutBox_.ShowDialog();
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
			bool active = (sender == checkBoxActive_)
				? checkBoxActive_.Checked
				: pauseToolStripMenuItem.Text.Equals("Unpause");

			checkBoxActive_.Checked = active;

			// menu item text & image
			pauseToolStripMenuItem.Text = active
				? "Pause"
				: "Unpause";
			pauseToolStripMenuItem.Image = active
				? Properties.Resources.pause_16x16
				: Properties.Resources.play_16x16;

			// timer on/off
			timer_.Enabled = active;
			if (active)
			{
				timer_.Start();
			}
			else
			{
				timer_.Stop();
			}

			// enable/disable controls
			label6.Enabled =
			numUDInterval_.Enabled =
			comboBoxTimeUnit_.Enabled = active;

			// change icon
			notifyIcon_.Icon = active
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
			foreach (ListViewItem item in listView_.SelectedItems)
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
			foreach (ListViewItem item in listView_.SelectedItems)
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
			logic_.RemoveSelectedItems();
		}

		private void color_Click(object sender, EventArgs e)
		{
			colorDialog_.Color = pictureBoxColor_.BackColor;
			if (colorDialog_.ShowDialog() == DialogResult.OK)
			{
				pictureBoxColor_.BackColor = colorDialog_.Color;
			}
		}

		private void control_Changed(object sender, EventArgs e)
		{
			Dirty = true;
		}

		private void ok_Click(object sender, EventArgs e)
		{
			logic_.SaveSettings();
			base.Hide();
		}

		private void cancel_Click(object sender, EventArgs e)
		{
			Hide();
		}

		private void apply_Click(object sender, EventArgs e)
		{
			logic_.SaveSettings();
			logic_.NewWallpaper();
		}

		private void radioStyle_Click(object sender, EventArgs e)
		{
			label1.Enabled =
			label2.Enabled =
			label7.Enabled =
			numUDThick_.Enabled =
			numUDColor_.Enabled = radioSpiffy_.Checked;

			label3.Enabled =
			label4.Enabled =
			label5.Enabled =
			pictureBoxColor_.Enabled =
			numUDSpace_.Enabled = radioSpiffy_.Checked || radioZoomOut_.Checked;

			pictureBoxColor_.BorderStyle = pictureBoxColor_.Enabled
				? BorderStyle.Fixed3D
				: BorderStyle.None;

			pictureBoxStyle_.Image = radioSpiffy_.Checked
				? Properties.Resources.spiffy
				: radioZoomOut_.Checked
					? Properties.Resources.zoom_out
					: Properties.Resources.zoom_in;
		}

		private void radioDisplays_Click(object sender, EventArgs e)
		{
			checkBoxSmartRand_.Enabled = radioMulti_.Checked;
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
			if (backgroundWorker_.IsBusy)
			{
				Helpers.ShowBusy();
				return;
			}

			timer_.Stop();

			notifyIcon_.Text = "Wallpaperr [busy]";
			notifyIcon_.Icon = Properties.Resources.smallBusy;

			this.Text = "Wallpaperr - Automatic Wallpaper Changer [busy]";

			progressBar_.Value = 0;
			progressBar_.Show();

			// start operation
			backgroundWorker_.RunWorkerAsync(fileName);
		}

		#region ListView Callbacks

		private void contextMenuItem_Opening(object sender, CancelEventArgs e)
		{
			// nothing selected?
			if (listView_.SelectedItems.Count == 0)
			{
				e.Cancel = true;
				return;
			}

			contextMenuItem_.Items[0].Image = Properties.Resources.random_16x16;

			if (listView_.SelectedItems.Count == 1)
			{
				ListViewItem item = listView_.FocusedItem;
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

					contextMenuItem_.Items[0].ImageScaling = ToolStripItemImageScaling.None;
					contextMenuItem_.Items[0].Image = img;
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
			var p = listView_.PointToClient(new System.Drawing.Point(e.X, e.Y));
			var lvi = listView_.GetItemAt(p.X, p.Y);
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
				logic_.AddItemsFromArray(dropped);
			}
		}

		private void listView_KeyDown(object sender, KeyEventArgs e)
		{
			e.Handled = listView_KeyDown_KeyData(e.KeyData);

			if (!e.Handled)
			{
				e.Handled = listView_KeyDown_KeyCode(e.KeyCode);
			}
		}

		private bool listView_KeyDown_KeyData(Keys KeyData)
		{
			switch (KeyData)
			{
				case Keys.A | Keys.Control:
					foreach (ListViewItem item in listView_.Items)
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
					logic_.RemoveSelectedItems();
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
			string[] files = logic_.GetMoreFiles((string)e.Argument);
			e.Result = WallpaperComposer.MakePicture(files, logic_.AppSettings, (BackgroundWorker)sender);
		}

		private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			progressBar_.Value = Math.Min(e.ProgressPercentage, 100);
		}

		private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				MessageBox.Show(e.Error.Message, "BackgroundWorker Error");
			}

			progressBar_.Hide();

			timer_.Start();
			timer_.Enabled = logic_.AppSettings.Active;

			notifyIcon_.Text = "Wallpaperr";
			notifyIcon_.Icon = logic_.AppSettings.Active
				? Properties.Resources.small
				: Properties.Resources.smallPaused;

			this.Text = "Wallpaperr - Automatic Wallpaper Changer";
		}

		#endregion

		#region File Dialog Callbacks

		private void openFileDialogItem_FileOk(object sender, CancelEventArgs e)
		{
			logic_.AddFiles(openFileDialogItem_.FileNames);
		}

		private void openFileDialogXML_FileOk(object sender, CancelEventArgs e)
		{
			logic_.OpenCollection(openFileDialogXML_.FileName);
		}

		private void saveFileDialogXML_FileOk(object sender, CancelEventArgs e)
		{
			var items = new List<string>(listView_.Items.Count);

			// get items' full names
			foreach (ListViewItem item in listView_.Items)
			{
				items.Add(item.ToolTipText);
			}

			// save list to file
			using (FileStream fs = File.Open(saveFileDialogXML_.FileName, FileMode.Create))
			{
				var serializer = new System.Xml.Serialization.XmlSerializer(items.GetType());
				serializer.Serialize(fs, items);
				fs.Flush();
			}
		}

		#endregion

		public void ShowEmptyCollection()
		{
			timer_.Stop();

			DialogResult result = MessageBox.Show(
				"You do not have any images in your collection." + Helpers.NL +
				"Would you like to add some files now?",
				"Hold it!",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Question);
			if (result == DialogResult.Yes)
			{
				openFileDialogItem_.ShowDialog();
			}

			timer_.Start();
			timer_.Enabled = logic_.AppSettings.Active;
		}

		private void ActivateFocusedItem()
		{
			/*
			ListViewItem item = listView_.FocusedItem;
			/*/
			int index = Helpers.RNG.Next(listView_.SelectedItems.Count);
			ListViewItem item = listView_.SelectedItems[index];
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
				SearchOption searchOption = logic_.AppSettings.IncludeSubdirectory ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
				var files = new List<FileInfo>();
				foreach (string pattern in Helpers.FileTypes)
				{
					files.AddRange(dirInfo.GetFiles(pattern, searchOption));
				}

				// no files found?
				if (files.Count == 0)
				{
					Helpers.ShowInfo(
						"No supported file types were found" + Helpers.NL +
						"in the selected folder.");
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
