/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;

namespace PaintDotNet.Effects
{
	public sealed class GaussianBlurEffect
	{
		[DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
		internal static extern unsafe void memset(void* dst, int c, UIntPtr length);

		private static int[] CreateGaussianBlurRow(int amount)
		{
			int size = 1 + (amount * 2);
			int[] weights = new int[size];

			for (int i = 0; i <= amount; ++i)
			{
				// 1 + aa - aa + 2ai - ii
				weights[i] = 16 * (i + 1);
				weights[weights.Length - i - 1] = weights[i];
			}

			return weights;
		}

		public static unsafe void GaussianBlur(Bitmap bmp, int r, Rectangle[] rois)
		{
			if (r == 0)
			{
				return;
			}

			using (Bitmap src = (Bitmap)bmp.Clone())
			{
				Rectangle lockRect = new Rectangle(Point.Empty, bmp.Size);

				BitmapData bmpData = bmp.LockBits(lockRect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
				byte* scan0 = (byte*)bmpData.Scan0.ToPointer();
				int stride = bmpData.Stride;

				BitmapData srcData = src.LockBits(lockRect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
				byte* srcScan0 = (byte*)srcData.Scan0.ToPointer();
				int srcStride = srcData.Stride;

				int[] w = CreateGaussianBlurRow(r);
				int wlen = w.Length;
				int wlen_ = wlen - 1;

				uint localStoreSize = (uint)wlen * 6 * sizeof(long);
				UIntPtr pLength = new UIntPtr(localStoreSize);
				byte* localStore = stackalloc byte[(int)localStoreSize];
				int arraysLength = sizeof(long) * wlen;
				long* wcSums = (long*)(localStore + arraysLength);
				long* bSums = (long*)(localStore + 3 * arraysLength);
				long* gSums = (long*)(localStore + (arraysLength << 2));
				long* rSums = (long*)(localStore + 5 * arraysLength);

				for (int ri = 0; ri < rois.Length; ++ri)
				{
					Rectangle rect = rois[ri];

					if (rect.Height < 1 || rect.Width < 1)
					{
						continue;
					}

					int yLimit = Math.Min(bmp.Height, rect.Bottom);
					for (int y = rect.Top; y < yLimit; ++y)
					{
						memset(localStore, 0, pLength);

						long wcSum = 0;
						long bSum = 0;
						long gSum = 0;
						long rSum = 0;

						uint* dstPtr = (uint*)(scan0 + y * stride) + rect.Left;

						for (int wx = 0; wx < wlen; ++wx)
						{
							wcSums[wx] = 0;
							bSums[wx] = 0;
							gSums[wx] = 0;
							rSums[wx] = 0;

							int srcX = rect.Left + wx - r;
							if (srcX >= 0 && srcX < src.Width)
							{
								for (int wy = 0; wy < wlen; ++wy)
								{
									int srcY = y + wy - r;
									if (srcY >= 0 && srcY < src.Height)
									{
										byte* c = (byte*)((uint*)(srcScan0 + srcY * srcStride) + srcX);

										int wp = w[wy];
										wp *= c[3] + (c[3] >> 7);
										wcSums[wx] += wp;
										wp >>= 8;

										bSums[wx] += wp * c[0];
										gSums[wx] += wp * c[1];
										rSums[wx] += wp * c[2];
									}
								}

								long wwx = w[wx];
								wcSum += wwx * wcSums[wx];
								bSum += wwx * bSums[wx];
								gSum += wwx * gSums[wx];
								rSum += wwx * rSums[wx];
							}
						}

						wcSum >>= 8;

						if (wcSum == 0)
						{
							*dstPtr = 0;
						}
						else
						{
							byte* b = (byte*)dstPtr;
							b[0] = (byte)(bSum / wcSum);
							b[1] = (byte)(gSum / wcSum);
							b[2] = (byte)(rSum / wcSum);
						}

						++dstPtr;

						int xLimit = Math.Min(bmp.Width, rect.Right);
						for (int x = rect.Left + 1; x < xLimit; ++x)
						{
							for (int i = 0; i < wlen_; ++i)
							{
								wcSums[i] = wcSums[i + 1];
								bSums[i] = bSums[i + 1];
								gSums[i] = gSums[i + 1];
								rSums[i] = rSums[i + 1];
							}

							wcSum = 0;
							bSum = 0;
							gSum = 0;
							rSum = 0;

							int wx;
							for (wx = 0; wx < wlen_; ++wx)
							{
								long wwx = w[wx];
								wcSum += wwx * wcSums[wx];
								bSum += wwx * bSums[wx];
								gSum += wwx * gSums[wx];
								rSum += wwx * rSums[wx];
							}

							wcSums[wx] = 0;
							bSums[wx] = 0;
							gSums[wx] = 0;
							rSums[wx] = 0;

							int srcX = x + wx - r;
							if (srcX >= 0 && srcX < src.Width)
							{
								for (int wy = 0; wy < wlen; ++wy)
								{
									int srcY = y + wy - r;
									if (srcY >= 0 && srcY < src.Height)
									{
										byte* c = (byte*)((uint*)(srcScan0 + srcY * srcStride) + srcX);

										int wp = w[wy];

										wp *= c[3] + (c[3] >> 7);
										wcSums[wx] += wp;
										wp >>= 8;

										bSums[wx] += wp * c[0];
										gSums[wx] += wp * c[1];
										rSums[wx] += wp * c[2];
									}
								}

								long wr = w[wx];
								wcSum += wr * wcSums[wx];
								bSum += wr * bSums[wx];
								gSum += wr * gSums[wx];
								rSum += wr * rSums[wx];
							}

							wcSum >>= 8;

							if (wcSum == 0)
							{
								*dstPtr = 0;
							}
							else
							{
								byte* b = (byte*)dstPtr;
								b[0] = (byte)(bSum / wcSum);
								b[1] = (byte)(gSum / wcSum);
								b[2] = (byte)(rSum / wcSum);
							}

							++dstPtr;
						}
					}
				}
				bmp.UnlockBits(bmpData);
				src.UnlockBits(srcData);
			}
		}

		public static unsafe void GaussianBlurMT(Bitmap bmp, int r, Rectangle[] rois)
		{
			if (r == 0)
			{
				return;
			}

			int width = bmp.Width;
			int height = bmp.Height;

			using (Bitmap src = (Bitmap)bmp.Clone())
			{
				Rectangle lockRect = new Rectangle(Point.Empty, bmp.Size);

				BitmapData bmpData = bmp.LockBits(lockRect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
				byte* scan0 = (byte*)bmpData.Scan0.ToPointer();
				int stride = bmpData.Stride;

				BitmapData srcData = src.LockBits(lockRect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
				byte* srcScan0 = (byte*)srcData.Scan0.ToPointer();
				int srcStride = srcData.Stride;

				int[] w = CreateGaussianBlurRow(r);
				int wlen = w.Length;
				int wlen_ = wlen - 1;
				uint localStoreSize = (uint)wlen * 6 * sizeof(long);

				using (AutoResetEvent are = new AutoResetEvent(false))
				{
					int workerThreads = rois.Length;
					for (int ri = 0; ri < rois.Length; ++ri)
					{
						ThreadPool.UnsafeQueueUserWorkItem((args) =>
						{
							Rectangle rect = rois[(int)args];
							rect.Intersect(lockRect);
							if (rect.Height < 1 || rect.Width < 1)
							{
								if (Interlocked.Decrement(ref workerThreads) == 0)
								{
									are.Set();
								}
								return;
							}

							UIntPtr pLength = new UIntPtr(localStoreSize);
							byte* localStore = stackalloc byte[(int)localStoreSize];
							int arraysLength = sizeof(long) * wlen;
							long* wcSums = (long*)(localStore + arraysLength);
							long* bSums = (long*)(localStore + 3 * arraysLength);
							long* gSums = (long*)(localStore + (arraysLength << 2));
							long* rSums = (long*)(localStore + 5 * arraysLength);

							int yLimit = Math.Min(height, rect.Bottom);
							for (int y = rect.Top; y < yLimit; ++y)
							{
								memset(localStore, 0, pLength);

								long wcSum = 0;
								long bSum = 0;
								long gSum = 0;
								long rSum = 0;

								uint* dstPtr = (uint*)(scan0 + y * stride) + rect.Left;

								for (int wx = 0; wx < wlen; ++wx)
								{
									wcSums[wx] = 0;
									bSums[wx] = 0;
									gSums[wx] = 0;
									rSums[wx] = 0;

									int srcX = rect.Left + wx - r;
									if (srcX >= 0 && srcX < width)
									{
										for (int wy = 0; wy < wlen; ++wy)
										{
											int srcY = y + wy - r;
											if (srcY >= 0 && srcY < height)
											{
												byte* c = (byte*)((uint*)(srcScan0 + srcY * srcStride) + srcX);

												int wp = w[wy];
												wp *= c[3] + (c[3] >> 7);
												wcSums[wx] += wp;
												wp >>= 8;

												bSums[wx] += wp * c[0];
												gSums[wx] += wp * c[1];
												rSums[wx] += wp * c[2];
											}
										}

										long wwx = w[wx];
										wcSum += wwx * wcSums[wx];
										bSum += wwx * bSums[wx];
										gSum += wwx * gSums[wx];
										rSum += wwx * rSums[wx];
									}
								}

								wcSum >>= 8;

								if (wcSum == 0)
								{
									*dstPtr = 0;
								}
								else
								{
									byte* b = (byte*)dstPtr;
									b[0] = (byte)(bSum / wcSum);
									b[1] = (byte)(gSum / wcSum);
									b[2] = (byte)(rSum / wcSum);
								}

								++dstPtr;

								int xLimit = Math.Min(width, rect.Right);
								for (int x = rect.Left + 1; x < xLimit; ++x)
								{
									for (int i = 0; i < wlen_; ++i)
									{
										wcSums[i] = wcSums[i + 1];
										bSums[i] = bSums[i + 1];
										gSums[i] = gSums[i + 1];
										rSums[i] = rSums[i + 1];
									}

									wcSum = 0;
									bSum = 0;
									gSum = 0;
									rSum = 0;

									int wx;
									for (wx = 0; wx < wlen_; ++wx)
									{
										long wwx = w[wx];
										wcSum += wwx * wcSums[wx];
										bSum += wwx * bSums[wx];
										gSum += wwx * gSums[wx];
										rSum += wwx * rSums[wx];
									}

									wcSums[wx] = 0;
									bSums[wx] = 0;
									gSums[wx] = 0;
									rSums[wx] = 0;

									int srcX = x + wx - r;
									if (srcX >= 0 && srcX < width)
									{
										for (int wy = 0; wy < wlen; ++wy)
										{
											int srcY = y + wy - r;
											if (srcY >= 0 && srcY < height)
											{
												byte* c = (byte*)((uint*)(srcScan0 + srcY * srcStride) + srcX);

												int wp = w[wy];

												wp *= c[3] + (c[3] >> 7);
												wcSums[wx] += wp;
												wp >>= 8;

												bSums[wx] += wp * c[0];
												gSums[wx] += wp * c[1];
												rSums[wx] += wp * c[2];
											}
										}

										long wr = w[wx];
										wcSum += wr * wcSums[wx];
										bSum += wr * bSums[wx];
										gSum += wr * gSums[wx];
										rSum += wr * rSums[wx];
									}

									wcSum >>= 8;

									if (wcSum == 0)
									{
										*dstPtr = 0;
									}
									else
									{
										byte* b = (byte*)dstPtr;
										b[0] = (byte)(bSum / wcSum);
										b[1] = (byte)(gSum / wcSum);
										b[2] = (byte)(rSum / wcSum);
									}

									++dstPtr;
								}
							}

							if (Interlocked.Decrement(ref workerThreads) == 0)
							{
								are.Set();
							}
						}, ri);
					}

					are.WaitOne();
				}
				bmp.UnlockBits(bmpData);
				src.UnlockBits(srcData);
			}
		}
	}
}
