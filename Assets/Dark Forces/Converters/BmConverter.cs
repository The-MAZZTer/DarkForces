using MZZT.DarkForces.FileFormats;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using UnityEngine;
using Color = System.Drawing.Color;

namespace MZZT.DarkForces.Converters {
	public static class BmConverter {
		public static Texture2D ToTexture(this DfBitmap.Page page, byte[] palette, bool keepTextureReadable = false) {
			byte[] pixels = page.Pixels;

			int width = page.Width;
			int height = page.Height;

			byte[] buffer = new byte[width * height * 4];
			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					Buffer.BlockCopy(palette, pixels[y * width + x] * 4, buffer, (y * width + x) * 4, 4);
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

		public static Texture2D ToTexture(this DfBitmap.Page page, DfPalette pal, bool forceTransparent, bool keepTextureReadable = false) =>
			page.ToTexture(pal.ToByteArray(forceTransparent || (page.Flags & DfBitmap.Flags.Transparent) > 0), keepTextureReadable);

		public static Texture2D ToTexture(this DfBitmap.Page page, DfPalette pal, DfColormap cmp, int lightLevel, bool forceTransparent, bool bypassCmpDithering, bool keepTextureReadable = false) {
			if (lightLevel > 31) {
				lightLevel = 31;
			} else if (lightLevel < 0) {
				lightLevel = 0;
			}

			return page.ToTexture(cmp.ToByteArray(pal, lightLevel, forceTransparent || (page.Flags & DfBitmap.Flags.Transparent) > 0,
				bypassCmpDithering), keepTextureReadable);
		}

		public static Bitmap ToBitmap(this DfBitmap.Page page, byte[] pal) {
			byte[] pixels = page.Pixels;

			int width = page.Width;
			int height = page.Height;

			Bitmap bitmap = new(width, height, PixelFormat.Format8bppIndexed);

			ColorPalette palette = bitmap.Palette;
			for (int i = 0; i < palette.Entries.Length; i++) {
				palette.Entries[i] = Color.FromArgb(pal[i * 4 + 3], pal[i * 4], pal[i * 4 + 1], pal[i * 4 + 2]);
			}
			bitmap.Palette = palette;

			try {
				BitmapData data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
				
				for (int y = 0; y < height; y++) {
					Marshal.Copy(pixels, width * y, data.Scan0 + data.Stride * (height - y - 1), data.Width);
				}

				bitmap.UnlockBits(data);
			} catch (Exception) {
				bitmap.Dispose();
				throw;
			}
			return bitmap;
		}

		public static Bitmap ToBitmap(this DfBitmap.Page page, DfPalette pal, bool forceTransparent) =>
			page.ToBitmap(pal.ToByteArray(forceTransparent || (page.Flags & DfBitmap.Flags.Transparent) > 0));

		public static Bitmap ToBitmap(this DfBitmap.Page page, DfPalette pal, DfColormap cmp, int lightLevel, bool forceTransparent, bool bypassCmpDithering) {
			if (lightLevel > 31) {
				lightLevel = 31;
			} else if (lightLevel < 0) {
				lightLevel = 0;
			}

			if (cmp == null) {
				return page.ToBitmap(pal, forceTransparent || (page.Flags & DfBitmap.Flags.Transparent) > 0);
			} else {
				return page.ToBitmap(cmp.ToByteArray(pal, lightLevel, forceTransparent || (page.Flags & DfBitmap.Flags.Transparent) > 0,
					bypassCmpDithering));
			}
		}
	}
}