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
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
			this.listBox1 = new System.Windows.Forms.ListBox();
			this.contextMenuItem_ = new System.Windows.Forms.ContextMenuStrip( this.components );
			this.newWallpaperToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.oToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolTip_ = new System.Windows.Forms.ToolTip( this.components );
			toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.contextMenuItem_.SuspendLayout();
			this.SuspendLayout();
			// 
			// listBox1
			// 
			this.listBox1.ContextMenuStrip = this.contextMenuItem_;
			this.listBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listBox1.HorizontalScrollbar = true;
			this.listBox1.Location = new System.Drawing.Point( 0, 0 );
			this.listBox1.Name = "listBox1";
			this.listBox1.Size = new System.Drawing.Size( 574, 251 );
			this.listBox1.Sorted = true;
			this.listBox1.TabIndex = 0;
			this.listBox1.DoubleClick += new System.EventHandler( this.listBox1_DoubleClick );
			this.listBox1.MouseMove += new System.Windows.Forms.MouseEventHandler( this.listBox1_MouseMove );
			this.listBox1.KeyPress += new System.Windows.Forms.KeyPressEventHandler( this.listBox1_KeyPress );
			// 
			// contextMenuItem_
			// 
			this.contextMenuItem_.Items.AddRange( new System.Windows.Forms.ToolStripItem[] {
            this.newWallpaperToolStripMenuItem,
            toolStripSeparator1,
            this.openToolStripMenuItem,
            this.oToolStripMenuItem} );
			this.contextMenuItem_.Name = "contextMenuItem_";
			this.contextMenuItem_.Size = new System.Drawing.Size( 202, 98 );
			this.contextMenuItem_.Opening += new System.ComponentModel.CancelEventHandler( this.contextMenuItem_Opening );
			// 
			// newWallpaperToolStripMenuItem
			// 
			this.newWallpaperToolStripMenuItem.Font = new System.Drawing.Font( "Segoe UI", 9F, System.Drawing.FontStyle.Bold );
			this.newWallpaperToolStripMenuItem.Image = global::Wallpaperr.Properties.Resources.random_16x16;
			this.newWallpaperToolStripMenuItem.Name = "newWallpaperToolStripMenuItem";
			this.newWallpaperToolStripMenuItem.Size = new System.Drawing.Size( 201, 22 );
			this.newWallpaperToolStripMenuItem.Text = "&New Wallpaper";
			this.newWallpaperToolStripMenuItem.Click += new System.EventHandler( this.newWallpaper_Click );
			// 
			// toolStripSeparator1
			// 
			toolStripSeparator1.Name = "toolStripSeparator1";
			toolStripSeparator1.Size = new System.Drawing.Size( 198, 6 );
			// 
			// openToolStripMenuItem
			// 
			this.openToolStripMenuItem.Image = global::Wallpaperr.Properties.Resources.open_selected_item_16x16;
			this.openToolStripMenuItem.Name = "openToolStripMenuItem";
			this.openToolStripMenuItem.Size = new System.Drawing.Size( 201, 22 );
			this.openToolStripMenuItem.Text = "&Open";
			this.openToolStripMenuItem.Click += new System.EventHandler( this.openItem_Click );
			// 
			// oToolStripMenuItem
			// 
			this.oToolStripMenuItem.Image = global::Wallpaperr.Properties.Resources.GoToParentFolderHS;
			this.oToolStripMenuItem.Name = "oToolStripMenuItem";
			this.oToolStripMenuItem.Size = new System.Drawing.Size( 201, 22 );
			this.oToolStripMenuItem.Text = "Open &Containing Folder";
			this.oToolStripMenuItem.Click += new System.EventHandler( this.openContainingFolder_Click );
			// 
			// MFLForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 574, 256 );
			this.Controls.Add( this.listBox1 );
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.KeyPreview = true;
			this.Name = "MFLForm";
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Master File List";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler( this.MFLForm_FormClosed );
			this.KeyPress += new System.Windows.Forms.KeyPressEventHandler( this.MFLForm_KeyPress );
			this.contextMenuItem_.ResumeLayout( false );
			this.ResumeLayout( false );

		}

		#endregion

		private System.Windows.Forms.ListBox listBox1;
		private System.Windows.Forms.ToolTip toolTip_;
		private System.Windows.Forms.ContextMenuStrip contextMenuItem_;
		private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem oToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem newWallpaperToolStripMenuItem;
	}
}