namespace Wallpaperr
{
	partial class MFLForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing )
		{
			if ( disposing && (components != null) )
			{
				components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
			System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
			listBox1 = new System.Windows.Forms.ListBox();
			contextMenuItem = new System.Windows.Forms.ContextMenuStrip(components);
			newWallpaperToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			oToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			toolTip = new System.Windows.Forms.ToolTip(components);
			toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			contextMenuItem.SuspendLayout();
			SuspendLayout();
			// 
			// toolStripSeparator1
			// 
			toolStripSeparator1.Name = "toolStripSeparator1";
			toolStripSeparator1.Size = new System.Drawing.Size(198, 6);
			// 
			// listBox1
			// 
			listBox1.ContextMenuStrip = contextMenuItem;
			listBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			listBox1.HorizontalScrollbar = true;
			listBox1.ItemHeight = 15;
			listBox1.Location = new System.Drawing.Point(0, 0);
			listBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			listBox1.Name = "listBox1";
			listBox1.Size = new System.Drawing.Size(670, 295);
			listBox1.Sorted = true;
			listBox1.TabIndex = 0;
			listBox1.DoubleClick += listBox1_DoubleClick;
			listBox1.KeyPress += listBox1_KeyPress;
			listBox1.MouseMove += listBox1_MouseMove;
			// 
			// contextMenuItem
			// 
			contextMenuItem.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { newWallpaperToolStripMenuItem, toolStripSeparator1, openToolStripMenuItem, oToolStripMenuItem });
			contextMenuItem.Name = "contextMenuItem";
			contextMenuItem.Size = new System.Drawing.Size(202, 76);
			contextMenuItem.Opening += contextMenuItem_Opening;
			// 
			// newWallpaperToolStripMenuItem
			// 
			newWallpaperToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
			newWallpaperToolStripMenuItem.Image = Properties.Resources.random_16x16;
			newWallpaperToolStripMenuItem.Name = "newWallpaperToolStripMenuItem";
			newWallpaperToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
			newWallpaperToolStripMenuItem.Text = "&New Wallpaper";
			newWallpaperToolStripMenuItem.Click += newWallpaper_Click;
			// 
			// openToolStripMenuItem
			// 
			openToolStripMenuItem.Image = Properties.Resources.open_selected_item_16x16;
			openToolStripMenuItem.Name = "openToolStripMenuItem";
			openToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
			openToolStripMenuItem.Text = "&Open";
			openToolStripMenuItem.Click += openItem_Click;
			// 
			// oToolStripMenuItem
			// 
			oToolStripMenuItem.Image = Properties.Resources.GoToParentFolderHS;
			oToolStripMenuItem.Name = "oToolStripMenuItem";
			oToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
			oToolStripMenuItem.Text = "Open &Containing Folder";
			oToolStripMenuItem.Click += openContainingFolder_Click;
			// 
			// MFLForm
			// 
			AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			ClientSize = new System.Drawing.Size(670, 295);
			Controls.Add(listBox1);
			FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			KeyPreview = true;
			Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			Name = "MFLForm";
			ShowInTaskbar = false;
			SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			Text = "Master File List";
			FormClosed += MFLForm_FormClosed;
			KeyPress += MFLForm_KeyPress;
			contextMenuItem.ResumeLayout(false);
			ResumeLayout(false);
		}

		#endregion

		private System.Windows.Forms.ListBox listBox1;
		private System.Windows.Forms.ToolTip toolTip;
		private System.Windows.Forms.ContextMenuStrip contextMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem oToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem newWallpaperToolStripMenuItem;
	}
}