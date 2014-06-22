using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Wallpaperr
{
	public partial class MFLForm : Form
	{
		#region Member Fields / Properties

		private WallpaperrLogic logic_;

		private IList<FileInfo> fileList_;

		private List<System.Drawing.Image> thumbs_ = new List<System.Drawing.Image>(0);

		public string FileName { get; private set; }

		private int lastIndex_ = -1;

		#endregion

		#region Constructors

		internal MFLForm(WallpaperrLogic logic)
		{
			logic_ = logic;

			InitializeComponent();
		}

		#endregion

		public void PopulateBox(IList<FileInfo> fileList)
		{
			FileName = null;

			fileList_ = fileList;

			listBox1.DataSource = null;
			listBox1.DisplayMember = "FullName";
			listBox1.DataSource = fileList_;

			if (fileList_.Count > thumbs_.Capacity)
			{
				thumbs_.Capacity = fileList_.Count;
			}

			for (int i = thumbs_.Count; i < fileList_.Count; ++i)
			{
				thumbs_.Add(null);
			}
		}

		private void MFLForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			thumbs_.ForEach((img) =>
			{
				using (img) { }
			});
		}

		private void MFLForm_KeyPress(object sender, KeyPressEventArgs e)
		{
			if ((Keys)e.KeyChar == Keys.Escape)
			{
				Close();
			}
		}

		private void listBox1_KeyPress(object sender, KeyPressEventArgs e)
		{
			if ((Keys)e.KeyChar == Keys.Enter)
			{
				ActivateSelectedItem();
			}
		}

		private void listBox1_DoubleClick(object sender, EventArgs e)
		{
			ActivateSelectedItem();
		}

		private void listBox1_MouseMove(object sender, MouseEventArgs e)
		{
			int index = listBox1.IndexFromPoint(listBox1.PointToClient(Cursor.Position));
			if (index == -1)
			{
				toolTip_.Active = false;
				return;
			}

			if (index == lastIndex_)
			{
				return;
			}

			lastIndex_ = index;

			toolTip_.Active = false;
			toolTip_.Active = true;

			FileInfo fileInfo = (FileInfo)listBox1.Items[index];
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
				thumbs_[index] = new System.Drawing.Bitmap(img, (int)thumbW, (int)thumbH);
			}

			long size = Math.Max(fileInfo.Length, 1024) / 1024;

			string ttText = string.Format(
				"Image Type: {1}{0}Dimensions: {2} x {3}{0}Size: {4} KB",
				Environment.NewLine, ext, dimX, dimY, size);

			toolTip_.SetToolTip(listBox1, ttText);
		}

		private void contextMenuItem_Opening(object sender, System.ComponentModel.CancelEventArgs e)
		{
			int index = listBox1.IndexFromPoint(listBox1.PointToClient(Cursor.Position));
			if (index == -1)
			{
				e.Cancel = true;
				return;
			}

			listBox1.SetSelected(index, true);

			contextMenuItem_.Items[0].ImageScaling = ToolStripItemImageScaling.None;
			contextMenuItem_.Items[0].Image = thumbs_[index];
		}

		private void newWallpaper_Click(object sender, EventArgs e)
		{
			ActivateSelectedItem();
		}

		private void openItem_Click(object sender, EventArgs e)
		{
			if (listBox1.SelectedItem != null)
			{
				FileInfo fileInfo = (FileInfo)listBox1.SelectedItem;
				if (File.Exists(fileInfo.FullName))
				{
					System.Diagnostics.Process.Start(fileInfo.FullName);
				}
			}
		}

		private void openContainingFolder_Click(object sender, EventArgs e)
		{
			if (listBox1.SelectedItem != null)
			{
				FileInfo fileInfo = (FileInfo)listBox1.SelectedItem;
				if (File.Exists(fileInfo.FullName))
				{
					System.Diagnostics.Process.Start(
						"explorer",
						string.Format("/select,\"{0}\"", fileInfo.FullName));
				}
			}
		}

		private void ActivateSelectedItem()
		{
			if (listBox1.SelectedItem == null)
			{
				return;
			}

			FileInfo fileInfo = (FileInfo)listBox1.SelectedItem;
			if (Helpers.Exists(fileInfo))
			{
				FileName = fileInfo.FullName;
				Close();
			}
			else
			{
				MessageBox.Show(
@"The selected file could not be found. It might
have been been moved or deleted. Wallpaperr will
remove it from the master file list.",
					"File Not Found",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);

				logic_.RemoveDeadFile(fileInfo);

				listBox1.DataSource = null;
				listBox1.DisplayMember = "FullName";
				listBox1.DataSource = fileList_;
			}
		}
	}
}
