using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Wallpaperr
{
	static class WallpaperComposer
	{
		private class ComposerData
		{
			public Size Dimensions;
			public Bitmap SourceBitmap;
			public Bitmap DestinationBitmap;
			public bool HasBackground;
			public float BorderSpace;
			public int Thickness;
			public Color BackgroundColor;
			public float BackgroundBlend;

			public ComposerData(Properties.Settings settings)
			{
				BorderSpace = (float)settings.Border / 100f;
				Thickness = (int)settings.Thickness;
				BackgroundColor = settings.BackgroundColor;
				BackgroundBlend = (float)settings.BackgroundBlend / 100f;
			}
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		static extern Int32 SystemParametersInfo(UInt32 uiAction, UInt32 uiParam, String pvParam, UInt32 fWinIni);

		private static Bitmap ValidateBitmap(string fileName)
		{
			Bitmap bmp = null;
			try
			{
				bmp = new Bitmap(fileName);
			}
			catch (Exception)
			{
				bmp = new Bitmap(400, 100);
				using (Graphics g = Graphics.FromImage(bmp))
				{
					g.DrawString(
						"Corrupt Image File!" + Helpers.NL + fileName,
						SystemFonts.DefaultFont,
						Brushes.Red,
						new RectangleF(5, 5, 390, 90));
				}
			}

			return bmp;
		}

		// Creates a bitmap for our background from the files specified in the
		// fileName array.
		public static int MakePicture(string[] fileName, Properties.Settings settings, BackgroundWorker worker)
		{
			double progress = 0;

			worker.ReportProgress((int)(progress = 5));

			Image finalImg = null;

			#region Single Display
			if (settings.SingleMonitor)
			{
				// open source image file
				using (Bitmap srcImg = ValidateBitmap(fileName[0]))
				{
					// create composer data to be passed along
					ComposerData compData = new ComposerData(settings);
					compData.Dimensions = Screen.PrimaryScreen.Bounds.Size;
					compData.SourceBitmap = srcImg;
					compData.DestinationBitmap = new Bitmap(compData.Dimensions.Width, compData.Dimensions.Height);

					// compose this image based on our style
					switch ((BackgroundStyle)settings.Style)
					{
						case BackgroundStyle.Spiffy:
							MakeBackground(compData);
							worker.ReportProgress((int)(progress = 30));
							compData.HasBackground = true;
							MakeForeground(compData);
							break;
						case BackgroundStyle.ZoomOut:
							compData.HasBackground = false;
							MakeForeground(compData);
							break;
						case BackgroundStyle.ZoomIn:
							MakeBackground(compData);
							break;
					}

					// set final image
					finalImg = compData.DestinationBitmap;
				}
			}
			#endregion
			#region Multiple Displays
			else
			{
				// calculate progress bar increment for each image
				double step = 85.0 / (double)Screen.AllScreens.Length;	// 85% total

				// array for holding completed images
				Bitmap[] destImg = new Bitmap[Screen.AllScreens.Length];

				// do multi-threading
				using (AutoResetEvent are = new AutoResetEvent(false))
				{
					// one thread per screen
					int workerThreads = Screen.AllScreens.Length;
					for (int i = 0; i < Screen.AllScreens.Length; ++i)
					{
						// run this delegate on a worker thread
						ThreadPool.QueueUserWorkItem((args) =>
						{
							// get array index from delegate args
							int index = (int)args;

							// open source image file
							using (Bitmap srcImg = ValidateBitmap(fileName[index]))
							{
								Size dimensions = Screen.AllScreens[index].Bounds.Size;

								// create destination bitmap for this image
								destImg[index] = new Bitmap(dimensions.Width, dimensions.Height);

								// create composer data to be passed along
								ComposerData compData = new ComposerData(settings);
								compData.Dimensions = dimensions;
								compData.SourceBitmap = srcImg;
								compData.DestinationBitmap = destImg[index];

								// compose this image based on our style
								switch ((BackgroundStyle)settings.Style)
								{
									case BackgroundStyle.Spiffy:
										MakeBackground(compData);
										Interlocked.Exchange(ref progress, progress + (step / 3.0));
										worker.ReportProgress((int)progress);
										compData.HasBackground = true;
										MakeForeground(compData);
										Interlocked.Exchange(ref progress, progress + (step * 2.0 / 3.0));
										break;
									case BackgroundStyle.ZoomOut:
										compData.HasBackground = false;
										MakeForeground(compData);
										Interlocked.Exchange(ref progress, progress + step);
										break;
									case BackgroundStyle.ZoomIn:
										MakeBackground(compData);
										Interlocked.Exchange(ref progress, progress + step);
										break;
								}
								worker.ReportProgress((int)progress);
							}

							// this thread is finishing, decrement worker thread count
							// and check if all are done
							if (Interlocked.Decrement(ref workerThreads) == 0)
							{
								are.Set();
							}
							// pass array index as argument to delegate
						}, i);
					}

					// wait here until all worker threads finish
					are.WaitOne();
				}

				// get final image size
				Rectangle union = new Rectangle();
				foreach (Screen display in Screen.AllScreens)
				{
					union = Rectangle.Union(union, display.Bounds);
				}

				worker.ReportProgress((int)(progress = 90));

				#region Compose final image
				// create destination bitmap for final image
				finalImg = new Bitmap(union.Width, union.Height);
				using (Graphics g = Graphics.FromImage(finalImg))
				{
					// calculate progress bar increment for each composed piece
					step = 5.0 / (double)Screen.AllScreens.Length;	// 5% total

					int index = 0;
					foreach (Bitmap bmp in destImg)
					{
						// get bounds for this screen
						Rectangle rect = Screen.AllScreens[index++].Bounds;
						g.DrawImageUnscaled(bmp, rect);

						// ensure image tiles correctly
						if (rect.X < 0)
						{
							Rectangle r = rect;
							r.X = union.Width + rect.X;
							g.DrawImageUnscaled(bmp, r);
						}
						if (rect.Y < 0)
						{
							Rectangle r = rect;
							r.Y = union.Height + rect.Y;
							g.DrawImageUnscaled(bmp, r);
						}
						if (rect.X < 0 && rect.Y < 0)
						{
							rect.X = union.Width + rect.X;
							rect.Y = union.Height + rect.Y;
							g.DrawImageUnscaled(bmp, rect);
						}

						worker.ReportProgress((int)(progress += step));

						bmp.Dispose();
					}
				}
				#endregion

			}
			#endregion

			worker.ReportProgress((int)(progress = 95));

			#region Save File
			string outFileName = Helpers.AppDataPath + @"\created.bmp";
			try
			{
				// save image to disk
				finalImg.Save(outFileName, System.Drawing.Imaging.ImageFormat.Bmp);

				worker.ReportProgress((int)(progress = 98));

				// set wallpaper
				SetWallpaper(outFileName);

				worker.ReportProgress((int)(progress = 100));
			}
			catch (Exception ex)
			{
				Helpers.ShowError(String.Format(
					"An exception was thrown while attempting to save{0}" +
					"and set your new wallpaper.{0}" +
					"Exception thrown: {1}{0}" +
					"We'll try again later.",
					Helpers.NL, ex.Message));
			}
			#endregion

			finalImg.Dispose();

			return 0;
		}

		private static void MakeBackground(ComposerData composerData)
		{
			Size dimensions = composerData.Dimensions;
			Bitmap srcImg = composerData.SourceBitmap;
			Bitmap destImage = composerData.DestinationBitmap;

			// calculate scaling factor for this image
			float imgRatio = (float)srcImg.Width / (float)srcImg.Height;
			float aspectRatio = (float)dimensions.Width / (float)dimensions.Height;
			float scale = (aspectRatio < imgRatio)
				? (float)dimensions.Height / (float)srcImg.Height
				: (float)dimensions.Width / (float)srcImg.Width;

			Rectangle dest = new Rectangle(Point.Empty, dimensions);
			// calculate source rectangle for input bitmap
			int w = (int)((float)dimensions.Width / scale);
			int h = (int)((float)dimensions.Height / scale);
			int x = (srcImg.Width - w) / 2;
			int y = (srcImg.Height - h) / 2;
			Point p = new Point(x, y);
			Size sz = new Size(w, h);
			Rectangle src = new Rectangle(p, sz);

			using (Graphics g = Graphics.FromImage(destImage))
			{
				if (composerData.BackgroundBlend < 1f)
				{
					// draw source image onto destination image according to
					// src & dest rectangles
					g.DrawImage(srcImg, dest, src, GraphicsUnit.Pixel);
				}

				if (composerData.BackgroundBlend > 0f)
				{
					Color blend = Color.FromArgb(
						(int)(composerData.BackgroundBlend * 255f),
						composerData.BackgroundColor);
					using (Brush b = new SolidBrush(blend))
					{
						g.FillRectangle(b, dest);
					}
				}
			}
		}

		private static void MakeForeground(ComposerData composerData)
		{
			Size dimensions = composerData.Dimensions;
			Bitmap srcImg = composerData.SourceBitmap;
			Bitmap destImg = composerData.DestinationBitmap;

			// calculate scaling factor for this image
			float imgRatio = (float)srcImg.Width / (float)srcImg.Height;
			float aspectRatio = (float)dimensions.Width / (float)dimensions.Height;
			float scale = (aspectRatio < imgRatio)
				? (float)dimensions.Width / (float)srcImg.Width
				: (float)dimensions.Height / (float)srcImg.Height;
			scale *= 1f - composerData.BorderSpace;

			// calculate source rectangle for input bitmap
			int w = (int)((float)srcImg.Width * scale);
			int h = (int)((float)srcImg.Height * scale);
			int x = (dimensions.Width - w) / 2;
			int y = (dimensions.Height - h) / 2;
			Point pt = new Point(x, y);
			Size sz = new Size(w, h);
			Rectangle dest = new Rectangle(pt, sz);
			Rectangle src = new Rectangle(Point.Empty, srcImg.Size);
			Rectangle edge = dest;
			edge.Inflate(composerData.Thickness, composerData.Thickness);

			using (Graphics g = Graphics.FromImage(destImg))
			{
				if (composerData.HasBackground)
				{
					// draw black & white border
					g.FillRectangle(Brushes.Black, edge);
					g.DrawRectangle(Pens.White, edge);
				}
				else
				{
					// clear image to 'bgColor'
					g.Clear(composerData.BackgroundColor);
				}

				// draw source image onto destination image according to
				// src & dest rectangles
				g.DrawImage(srcImg, dest, src, GraphicsUnit.Pixel);
			}

			// need to do background blur effect?
			if (composerData.HasBackground && composerData.BackgroundBlend < 1f)
			{
				// calculate rectangles for blur effect
				Rectangle[] rects = new Rectangle[4];
				rects[0] = new Rectangle(0, 0, dimensions.Width, edge.Top);
				rects[1] = new Rectangle(0, edge.Top, edge.Left, edge.Height + 1);
				rects[2] = new Rectangle(edge.Right + 1, edge.Top, edge.Left, edge.Height + 1);
				rects[3] = new Rectangle(0, edge.Bottom + 1, dimensions.Width, edge.Top);

				// do multi-threading for blur process
				int radius = 4;
				PaintDotNet.Effects.GaussianBlurEffect.GaussianBlurMT(destImg, radius, rects);
			}
		}

		private static void SetWallpaper(string fileName)
		{
			// get registry entry
			var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);

			// set registry entry
			key.SetValue("WallpaperStyle", "1");
			key.SetValue("TileWallpaper", "1");

			// system parameters values for changing the desktop background
			const uint SPI_SETDESKWALLPAPER = 0x14;
			const uint SPIF_UPDATEINIFILE = 0x01;
			const uint SPIF_SENDWININICHANGE = 0x02;

			// set wallpaper
			SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, fileName, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
		}
	}
}
