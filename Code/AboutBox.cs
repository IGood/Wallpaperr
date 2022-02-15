namespace Wallpaperr
{
	using System.Reflection;
	using System.Windows.Forms;

	partial class AboutBox : Form
	{
		private readonly Assembly executingAsm = Assembly.GetExecutingAssembly();

		public AboutBox()
		{
			this.InitializeComponent();
			this.Text = "About " + this.AssemblyTitle;
			this.labelProductName.Text = this.AssemblyProduct;
			this.labelVersion.Text = "Version " + this.AssemblyVersion;
			this.labelCopyright.Text = this.AssemblyCopyright;
			this.labelCompanyName.Text = this.AssemblyCompany;
			this.textBoxDescription.Text = this.AssemblyDescription;
		}

		#region Assembly Attribute Accessors

		public string AssemblyTitle
		{
			get
			{
				string title = this.executingAsm.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;
				if (string.IsNullOrEmpty(title))
				{
					title = System.IO.Path.GetFileNameWithoutExtension(this.executingAsm.Location);
				}

				return title;
			}
		}

		public string AssemblyVersion => this.executingAsm.GetName().Version.ToString();

		public string AssemblyDescription => this.executingAsm.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? string.Empty;

		public string AssemblyProduct => this.executingAsm.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? string.Empty;

		public string AssemblyCopyright => this.executingAsm.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? string.Empty;

		public string AssemblyCompany => this.executingAsm.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? string.Empty;

		#endregion
	}
}
