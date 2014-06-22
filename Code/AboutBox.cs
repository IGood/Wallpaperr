namespace Wallpaperr
{
	using System;
	using System.Reflection;
	using System.Windows.Forms;

	partial class AboutBox : Form
	{
		private readonly Assembly executingAsm = Assembly.GetExecutingAssembly();

		public AboutBox()
		{
			this.InitializeComponent();
			this.Text = "About " + AssemblyTitle;
			this.labelProductName.Text = AssemblyProduct;
			this.labelVersion.Text = "Version " + AssemblyVersion;
			this.labelCopyright.Text = AssemblyCopyright;
			this.labelCompanyName.Text = AssemblyCompany;
			this.textBoxDescription.Text = AssemblyDescription;
		}

		#region Assembly Attribute Accessors

		public string AssemblyTitle
		{
			get
			{
				object[] attributes = this.executingAsm.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
				if (attributes.Length > 0)
				{
					var titleAttribute = (AssemblyTitleAttribute)attributes[0];
					if (String.IsNullOrEmpty(titleAttribute.Title) == false)
					{
						return titleAttribute.Title;
					}
				}

				return System.IO.Path.GetFileNameWithoutExtension(this.executingAsm.CodeBase);
			}
		}

		public string AssemblyVersion
		{
			get { return this.executingAsm.GetName().Version.ToString(); }
		}

		public string AssemblyDescription
		{
			get
			{
				object[] attributes = this.executingAsm.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
				return (attributes.Length == 0) ? String.Empty : ((AssemblyDescriptionAttribute)attributes[0]).Description;
			}
		}

		public string AssemblyProduct
		{
			get
			{
				object[] attributes = this.executingAsm.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
				return (attributes.Length == 0) ? String.Empty : ((AssemblyProductAttribute)attributes[0]).Product;
			}
		}

		public string AssemblyCopyright
		{
			get
			{
				object[] attributes = this.executingAsm.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
				return (attributes.Length == 0) ? String.Empty : ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
			}
		}

		public string AssemblyCompany
		{
			get
			{
				object[] attributes = this.executingAsm.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
				return (attributes.Length == 0) ? String.Empty : ((AssemblyCompanyAttribute)attributes[0]).Company;
			}
		}

		#endregion
	}
}
