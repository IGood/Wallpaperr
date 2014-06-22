using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace dnGREP
{
	public class FileFolderDialog : CommonDialog
	{
		private OpenFileDialog dialog = new OpenFileDialog();
		public OpenFileDialog Dialog
		{
			get { return dialog; }
			set { dialog = value; }
		}

		public new DialogResult ShowDialog()
		{
			return ShowDialog(null);
		}

		public new DialogResult ShowDialog(IWin32Window owner)
		{
			// Set validate names to false otherwise windows will not let you select "Folder Selection."
			dialog.ValidateNames = false;
			dialog.CheckFileExists = false;
			dialog.CheckPathExists = true;

			try
			{
				// Set initial directory (used when dialog.FileName is set from outside)
				if (!String.IsNullOrEmpty(dialog.FileName))
				{
					dialog.InitialDirectory = Directory.Exists(dialog.FileName)
						? dialog.FileName
						: Path.GetDirectoryName(dialog.FileName);
				}
			}
			catch (Exception)
			{
				// Do nothing
			}

			// Always default to Folder Selection.
			dialog.FileName = "Folder Selection.";

			return (owner == null) ? dialog.ShowDialog() : dialog.ShowDialog(owner);
		}

		/// <summary>
		// Helper property. Parses FilePath into either folder path (if Folder Selection. is set)
		// or returns file path
		/// </summary>
		public string SelectedPath
		{
			get
			{
				try
				{
					if (dialog.FileName != null &&
						(dialog.FileName.EndsWith("Folder Selection.") || !File.Exists(dialog.FileName)) &&
						!Directory.Exists(dialog.FileName))
					{
						return Path.GetDirectoryName(dialog.FileName);
					}

					return dialog.FileName;
				}
				catch (Exception)
				{
					return dialog.FileName;
				}
			}
			set
			{
				if (!String.IsNullOrEmpty(value))
				{
					dialog.FileName = value;
				}
			}
		}

		/// <summary>
		/// When multiple files are selected returns them as semi-colon seprated string
		/// </summary>
		public string SelectedPaths
		{
			get
			{
				if (dialog.FileNames != null && dialog.FileNames.Length > 1)
				{
					StringBuilder sb = new StringBuilder();
					foreach (string fileName in dialog.FileNames)
					{
						try
						{
							if (File.Exists(fileName))
							{
								sb.Append(fileName + ";");
							}
						}
						catch (Exception)
						{
							// Go to next
						}
					}

					return sb.ToString();
				}

				return null;
			}
		}

		public override void Reset()
		{
			dialog.Reset();
		}

		protected override bool RunDialog(IntPtr hwndOwner)
		{
			return true;
		}
	}
}
