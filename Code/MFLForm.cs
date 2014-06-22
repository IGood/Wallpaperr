namespace Wallpaperr
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Windows.Forms;
	using System.Collections.Generic;

	public partial class MFLForm : Form
	{
		#region Member Fields / Properties

		private readonly List<System.Drawing.Image> thumbs = new List<System.Drawing.Image>(0);

		private WallpaperrLogic logic;

		private IList<FileInfo> fileList;

		public string FileName { get; private set; }

		private int lastIndex = -1;

		#endregion

		#region Constructors

		internal MFLForm(WallpaperrLogic logic)
		{
			this.logic = logic;

			this.InitializeComponent();
		}

		#endregion

		public void PopulateBox(IList<FileInfo> fileList)
		{
			this.FileName = null;

			this.fileList = fileList;

			this.listBox1.DataSource = null;
			this.listBox1.DisplayMember = "FullName";
			this.listBox1.DataSource = this.fileList;

			if (this.fileList.Count > this.thumbs.Capacity)
			{
				this.thumbs.Capacity = this.fileList.Count;
			}

			for (int i = this.thumbs.Count; i < this.fileList.Count; ++i)
			{
				this.thumbs.Add(null);
			}
		}

		private void MFLForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			foreach (var img in this.thumbs.Where((img) => img != null))
			{
				img.Dispose();
			}
		}

		private void MFLForm_KeyPress(object sender, KeyPressEventArgs e)
		{
			if ((Keys)e.KeyChar == Keys.Escape)
			{
				this.Close();
			}
		}

		private void listBox1_KeyPress(object sender, KeyPressEventArgs e)
		{
			if ((Keys)e.KeyChar == Keys.Enter)
			{
				this.ActivateSelectedItem();
			}
		}

		private void listBox1_DoubleClick(object sender, EventArgs e)
		{
			this.ActivateSelectedItem();
		}

		private void listBox1_MouseMove(object sender, MouseEventArgs e)
		{
			int index = this.listBox1.IndexFromPoint(this.listBox1.PointToClient(Cursor.Position));
			if (index == -1)
			{
				this.toolTip.Active = false;
				return;
			}

			if (index == this.lastIndex)
			{
				return;
			}

			this.lastIndex = index;

			this.toolTip.Active = false;
			this.toolTip.Active = true;

			FileInfo fileInfo = (FileInfo)this.listBox1.Items[index];
			if (!Helpers.Exists(fileInfo))
			{
				return;
			}

			string ext = fileInfo.Extension.Substring(1).ToUpper();

			int dimX;
			int dimY;
			using (var img = System.Drawing.Image.FromFile(fileInfo.FullName))
			{
				dimX = img.Width;
				dimY = img.Height;
				const float maxDims = 200;
				float scale = Math.Min(maxDims / dimX, maxDims / dimY);
				float thumbW = dimX * scale;
				float thumbH = dimY * scale;
				this.thumbs[index] = new System.Drawing.Bitmap(img, (int)thumbW, (int)thumbH);
			}

			long size = Math.Max(fileInfo.Length, 1024) / 1024;

			var fmt =
@"Image Type: {0}
Dimensions: {1} x {2}
Size: {3} KB";

			this.toolTip.SetToolTip(this.listBox1, string.Format(fmt, ext, dimX, dimY, size));
		}

		private void contextMenuItem_Opening(object sender, System.ComponentModel.CancelEventArgs e)
		{
			int index = this.listBox1.IndexFromPoint(this.listBox1.PointToClient(Cursor.Position));
			if (index == -1)
			{
				e.Cancel = true;
				return;
			}

			this.listBox1.SetSelected(index, true);

			this.contextMenuItem.Items[0].ImageScaling = ToolStripItemImageScaling.None;
			this.contextMenuItem.Items[0].Image = this.thumbs[index];
		}

		private void newWallpaper_Click(object sender, EventArgs e)
		{
			this.ActivateSelectedItem();
		}

		private void openItem_Click(object sender, EventArgs e)
		{
			var fileInfo = this.listBox1.SelectedItem as FileInfo;
			if (fileInfo != null && Helpers.Exists(fileInfo))
			{
				System.Diagnostics.Process.Start(fileInfo.FullName);
			}
		}

		private void openContainingFolder_Click(object sender, EventArgs e)
		{
			var fileInfo = this.listBox1.SelectedItem as FileInfo;
			if (fileInfo != null && Helpers.Exists(fileInfo))
			{
				System.Diagnostics.Process.Start("explorer", string.Format("/select,\"{0}\"", fileInfo.FullName));
			}
		}

		private void ActivateSelectedItem()
		{
			var fileInfo = this.listBox1.SelectedItem as FileInfo;
			if (fileInfo == null)
			{
				return;
			}

			if (Helpers.Exists(fileInfo))
			{
				this.FileName = fileInfo.FullName;
				this.Close();
			}
			else
			{
				var msg =
@"The selected file could not be found. It might
have been been moved or deleted. Wallpaperr will
remove it from the master file list.";

				MessageBox.Show(msg, "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);

				this.logic.RemoveDeadFile(fileInfo);

				this.listBox1.DataSource = null;
				this.listBox1.DisplayMember = "FullName";
				this.listBox1.DataSource = this.fileList;
			}
		}
	}
}
