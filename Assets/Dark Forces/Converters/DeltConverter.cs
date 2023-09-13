using MZZT.DarkForces.FileFormats;
using SkiaSharp;
using System;
using System.Collections;
using System.Drawing.Imaging;
using System.Drawing;
using System.Runtime.InteropServices;
using UnityEngine;
using Color = System.Drawing.Color;

namespace MZZT.DarkForces.Converters {
	public static class DeltConverter {
		public static Texture2D ToTexture(this LandruDelt delt, byte[] palette, bool keepTextureReadable = false) {
			byte[] pixels = delt.Pixels;
			BitArray mask = delt.Mask;
			int width = delt.Width;
			int height = delt.Height;
			if (width >= 1 && height >= 1) {
				byte[] buffer = new byte[width * height * 4];
				for (int y = 0; y < height; y++) {
					for (int x = 0; x < width; x++) {
						int offset = y * width + x;
						int destOffset = (height - y - 1) * width + x;
						if (mask[offset]) {
							Buffer.BlockCopy(palette, pixels[offset] * 4, buffer, destOffset * 4, 4);
						}
					}
				}

				Texture2D texture = new(width, height, TextureFormat.RGBA32, false, true) {
#if UNITY_EDITOR
					alphaIsTransparency = true,
#endif
					filterMode = FilterMode.Point
				};
				texture.LoadRawTextureData(buffer);
				texture.Apply(true, !keepTextureReadable);
				return texture;
			}

			return null;
		}

		public static Texture2D ToTexture(this LandruDelt delt, LandruPalette pltt, bool keepTextureReadable = false) =>
			delt.ToTexture(pltt.ToByteArray(), keepTextureReadable);

		public static Texture2D ToTextureWithOffset(this LandruDelt delt, byte[] palette, bool keepTextureReadable = false) {
			byte[] pixels = delt.Pixels;
			BitArray mask = delt.Mask;
			byte[] buffer = new byte[320 * 200 * 4];
			for (int y = 0; y < 200; y++) {
				int deltY = y - delt.OffsetY;

				bool inRangeY = deltY >= 0 && deltY < delt.Height;

				for (int x = 0; x < 320; x++) {
					int offset = y * 320 + x;
					int destOffset = (200 - y - 1) * 320 + x;

					int deltX = x - delt.OffsetX;

					if (inRangeY && deltX >= 0 && deltX < delt.Width) {
						int deltOffset = deltY * delt.Width + deltX;
						if (mask[deltOffset]) {
							Buffer.BlockCopy(palette, pixels[deltOffset] * 4, buffer, destOffset * 4, 4);
						}
					}
				}
			}

			Texture2D texture = new(320, 200, TextureFormat.RGBA32, false, true) {
#if UNITY_EDITOR
				alphaIsTransparency = true,
#endif
				filterMode = FilterMode.Point
			};
			texture.LoadRawTextureData(buffer);
			texture.Apply(true, !keepTextureReadable);
			return texture;
		}

		public static Texture2D ToTextureWithOffset(this LandruDelt delt, LandruPalette pltt, bool keepTextureReadable = false) =>
			delt.ToTextureWithOffset(pltt.ToByteArray(), keepTextureReadable);

		public static Bitmap ToUnmaskedBitmap(this LandruDelt delt, byte[] pal) {
			byte[] pixels = delt.Pixels;

			int width = delt.Width;
			int height = delt.Height;

			Bitmap bitmap = new(width, height, PixelFormat.Format8bppIndexed);

			ColorPalette palette = bitmap.Palette;
			for (int i = 1; i < palette.Entries.Length; i++) {
				palette.Entries[i] = Color.FromArgb(pal[i * 4 + 3], pal[i * 4], pal[i * 4 + 1], pal[i * 4 + 2]);
			}
			bitmap.Palette = palette;

			try {
				BitmapData data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

				for (int y = 0; y < height; y++) {
					Marshal.Copy(pixels, width * y, data.Scan0 + data.Stride * y, data.Width);
				}

				bitmap.UnlockBits(data);
			} catch (Exception) {
				bitmap.Dispose();
				throw;
			}
			return bitmap;
		}

		public static Bitmap ToUnmaskedBitmap(this LandruDelt delt, LandruPalette pltt) => delt.ToUnmaskedBitmap(pltt.ToByteArray());

		public static Bitmap MaskToBitmap(this LandruDelt delt) {
			int width = delt.Width;
			int height = delt.Height;

			Bitmap bitmap = new(width, height, PixelFormat.Format1bppIndexed);

			ColorPalette palette = bitmap.Palette;
			palette.Entries[0] = Color.FromArgb(0, 0, 0, 0);
			palette.Entries[1] = Color.FromArgb(255, 255, 255, 255);
			bitmap.Palette = palette;

			try {
				BitmapData data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

				for (int y = 0; y < height; y++) {
					for (int x = 0; x < width; x++) {
						Marshal.WriteByte(data.Scan0 + data.Stride * y + x, (byte)(delt.Mask.Get(y * width + x) ? 1 : 0));
					}
				}

				bitmap.UnlockBits(data);
			} catch (Exception) {
				bitmap.Dispose();
				throw;
			}
			return bitmap;
		}

		public static SKImage ToSKImage(this LandruDelt delt, byte[] palette) {
			byte[] pixels = delt.Pixels;
			BitArray mask = delt.Mask;
			int width = delt.Width;
			int height = delt.Height;

			if (width < 1 || height < 1) {
				return null;
			}
			
			SKData data = SKData.Create(width * height * 4);
			Span<byte> span = data.Span;
			Span<byte> paletteSpan = palette.AsSpan();
			Span<byte> zero = new(new byte[] { 0, 0, 0, 0 });

			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					int offset = y * width + x;
					if (mask[offset]) {
						paletteSpan.Slice(pixels[offset] * 4, 4).CopyTo(span.Slice(offset * 4, 4));
					} else {
						zero.CopyTo(span.Slice(offset * 4, 4));
					}
				}
			}

			return SKImage.FromPixels(new SKImageInfo(width, height, SKColorType.Rgba8888), data, width * 4);
		}
		public static SKImage ToSKImage(this LandruDelt delt, LandruPalette pltt) =>
			delt.ToSKImage(pltt.ToByteArray());
	}
}
