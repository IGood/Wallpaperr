namespace Wallpaperr
{
	using System;
	using System.ComponentModel;
	using System.Drawing;
	using System.Drawing.Imaging;
	using System.Linq;
	using System.Runtime.InteropServices;
	using System.Threading;
	using System.Windows.Forms;
	using System.Windows.Media.Imaging;

	static class WallpaperComposer
	{
		private static readonly bool WindowsVersionSupportsJpg = Environment.OSVersion.Version >= new Version(6, 0);

		private static Bitmap ValidateBitmap(string fileName)
		{
			Bitmap bmp;
			try
			{
				bmp = new Bitmap(fileName);
			}
			catch (Exception)
			{
				bmp = new Bitmap(400, 100);

				using var g = Graphics.FromImage(bmp);
				g.DrawString(
					"Corrupt Image File!" + Environment.NewLine + fileName,
					SystemFonts.DefaultFont,
					Brushes.Red,
					new RectangleF(5, 5, 390, 90));
			}

			return bmp;
		}

		// Creates a bitmap for our background from the files specified in the
		// fileName array.
		public static int MakePicture(string[] fileName, Properties.Settings settings, BackgroundWorker worker)
		{
			double progress = 0;

			worker.ReportProgress((int)(progress = 5));

			if (TryFastPath(fileName[0], settings))
			{
				worker.ReportProgress((int)(progress = 100));
				return 0;
			}

			Image finalImg = null;

			#region Single Display
			if (settings.SingleMonitor)
			{
				// open source image file
				using Bitmap srcImg = ValidateBitmap(fileName[0]);

				// create composer data to be passed along
				var compData = new ComposerData(settings);
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
			#endregion
			#region Multiple Displays
			else
			{
				// calculate progress bar increment for each image
				double step = 85.0 / (double)Screen.AllScreens.Length;  // 85% total

				// array for holding completed images
				var destImg = new Bitmap[Screen.AllScreens.Length];

				// do multi-threading
				using (var are = new AutoResetEvent(false))
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
								var compData = new ComposerData(settings);
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
				using (var g = Graphics.FromImage(finalImg))
				{
					// calculate progress bar increment for each composed piece
					step = 5.0 / (double)Screen.AllScreens.Length;  // 5% total

					int index = 0;
					foreach (Bitmap bmp in destImg)
					{
						// get bounds for this screen
						Rectangle rect = Screen.AllScreens[index++].Bounds;

						if (settings.UseLegacyTiling)
						{
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
						}
						else
						{
							// align the image onto the final canvas
							rect.X -= union.X;
							rect.Y -= union.Y;
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

			#region Save & Set
			string fileExt = (WindowsVersionSupportsJpg ? "jpg" : "bmp");
			string outFileName = $@"{Helpers.AppDataPath}\created.{fileExt}";

			try
			{
				// save image to disk
				if (WindowsVersionSupportsJpg)
				{
					var codecs = ImageCodecInfo.GetImageEncoders();
					var imgCodecInfo = codecs.FirstOrDefault((codec) => codec.FormatID.Equals(ImageFormat.Jpeg.Guid));
					if (imgCodecInfo == null)
					{
						finalImg.Save(outFileName, ImageFormat.Jpeg);
					}
					else
					{
						// has 1 slot by default
						using var encoderParams = new EncoderParameters();
						encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 100L);
						finalImg.Save(outFileName, imgCodecInfo, encoderParams);
					}
				}
				else
				{
					finalImg.Save(outFileName, ImageFormat.Bmp);
				}

				worker.ReportProgress((int)(progress = 98));

				// set wallpaper
				SetWallpaper(outFileName);

				worker.ReportProgress((int)(progress = 100));
			}
			catch (Exception ex)
			{
				string message =
$@"An exception was thrown while attempting to save
and set your new wallpaper.
Exception thrown: {ex.Message}
File(s):
{string.Join(Environment.NewLine, fileName)}
We'll try again later.";
				Helpers.ShowError(message);
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

			using var g = Graphics.FromImage(destImage);

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
				using Brush b = new SolidBrush(blend);
				g.FillRectangle(b, dest);
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

			if (composerData.MaxScale.HasValue)
			{
				scale = Math.Min(scale, composerData.MaxScale.Value);
			}

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

			using (var g = Graphics.FromImage(destImg))
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
				Rectangle[] rects =
				{
					new Rectangle(0, 0, dimensions.Width, edge.Top),
					new Rectangle(0, edge.Top, edge.Left, edge.Height + 1),
					new Rectangle(edge.Right + 1, edge.Top, edge.Left, edge.Height + 1),
					new Rectangle(0, edge.Bottom + 1, dimensions.Width, edge.Top),
				};

				// do multi-threading for blur process
				int radius = 4;
				PaintDotNet.Effects.GaussianBlurEffect.GaussianBlurMT(destImg, radius, rects);
			}
		}

		private static void SetWallpaper(string fileName)
		{
			// get registry entry
			using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
			{
				// set registry entry
				key.SetValue("WallpaperStyle", "0");
				key.SetValue("TileWallpaper", "1");
			}

			// system parameters values for changing the desktop background
			const uint SPI_SETDESKWALLPAPER = 0x14;
			const uint SPIF_UPDATEINIFILE = 0x01;
			const uint SPIF_SENDWININICHANGE = 0x02;

			// set wallpaper
			NativeMethods.SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, fileName, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
		}

		/// <summary>
		/// If we're doing a single monitor with no border and the image is the correct size,
		/// then we can simply set the wallpaper to the source file.
		/// </summary>
		private static bool TryFastPath(string fileName, Properties.Settings settings)
		{
			if (settings.SingleMonitor)
			{
				var style = (BackgroundStyle)settings.Style;
				if (style == BackgroundStyle.ZoomIn || settings.Border == 0)
				{
					try
					{
						BitmapDecoder decoder = BitmapDecoder.Create(new Uri(fileName), BitmapCreateOptions.None, BitmapCacheOption.None);
						BitmapFrame frame = decoder.Frames[0];
						Size destSize = Screen.PrimaryScreen.Bounds.Size;
						if (frame.PixelWidth == destSize.Width &&
							frame.PixelHeight == destSize.Height)
						{
							SetWallpaper(fileName);
							return true;
						}
					}
					catch { }
				}
			}

			return false;
		}

		private static class NativeMethods
		{
			[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, string pvParam, uint fWinIni);
		}

		private class ComposerData
		{
			public Size Dimensions;
			public Bitmap SourceBitmap;
			public Bitmap DestinationBitmap;
			public bool HasBackground;
			public float BorderSpace;
			public float? MaxScale;
			public int Thickness;
			public Color BackgroundColor;
			public float BackgroundBlend;

			public ComposerData(Properties.Settings settings)
			{
				this.BorderSpace = (float)settings.Border / 100f;
				/*
				this.MaxScale = settings.UseMaxScale ? (float)settings.MaxScale / 100f : default(float?);
				/*/
				this.MaxScale = 1f;
				//*/
				this.Thickness = (int)settings.Thickness;
				this.BackgroundColor = settings.BackgroundColor;
				this.BackgroundBlend = (float)settings.BackgroundBlend / 100f;
			}
		}
	}
}
