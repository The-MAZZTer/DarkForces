using MZZT.DarkForces.FileFormats;
using System;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using Color = System.Drawing.Color;

namespace MZZT.DarkForces.Converters {
	public static class FntConverter {
		public static Texture2D ToTexture(this DfFont fnt, byte[] palette, bool keepTextureReadable = false) {
			int width = fnt.Characters.Sum(x => x.Width);
			int height = fnt.Height;

			byte[] buffer = new byte[width * height * 4];
			int offset = 0;
			foreach (DfFont.Character c in fnt.Characters) {
				for (int y = 0; y < height; y++) {
					for (int x = 0; x < c.Width; x++) {
						Buffer.BlockCopy(palette, c.Data[x * height + y] * 4, buffer, (y * width + x + offset) * 4, 4);
					}
				}
				offset += c.Width;
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

		public static Texture2D ToTexture(this DfFont fnt, DfPalette pal, bool forceTransparent, bool keepTextureReadable = false) =>
			fnt.ToTexture(pal.ToByteArray(forceTransparent), keepTextureReadable);

		public static Texture2D ToTexture(this DfFont fnt, DfPalette pal, DfColormap cmp, int lightLevel, bool forceTransparent, bool bypassCmpDithering, bool keepTextureReadable = false) {
			if (lightLevel > 31) {
				lightLevel = 31;
			} else if (lightLevel < 0) {
				lightLevel = 0;
			}

			return fnt.ToTexture(cmp.ToByteArray(pal, lightLevel, forceTransparent, bypassCmpDithering), keepTextureReadable);
		}

		public static Texture2D ToTexture(this DfFont.Character c, DfFont fnt, byte[] palette, bool keepTextureReadable = false) {
			int width = c.Width;
			int height = fnt.Height;

			byte[] buffer = new byte[width * height * 4];
			for (int y = 0; y < height; y++) {
				for (int x = 0; x < c.Width; x++) {
					Buffer.BlockCopy(palette, c.Data[x * height + y] * 4, buffer, (y * width + x) * 4, 4);
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

		public static Texture2D ToTexture(this DfFont.Character c, DfFont fnt, DfPalette pal, bool forceTransparent, bool keepTextureReadable = false) =>
			c.ToTexture(fnt, pal.ToByteArray(forceTransparent), keepTextureReadable);

		public static Texture2D ToTexture(this DfFont.Character c, DfFont fnt, DfPalette pal, DfColormap cmp, int lightLevel, bool forceTransparent, bool bypassCmpDithering, bool keepTextureReadable = false) {
			if (lightLevel > 31) {
				lightLevel = 31;
			} else if (lightLevel < 0) {
				lightLevel = 0;
			}

			return c.ToTexture(fnt, cmp.ToByteArray(pal, lightLevel, forceTransparent, bypassCmpDithering), keepTextureReadable);
		}

		public static Bitmap ToBitmap(this DfFont font, byte[] pal) {
 			Bitmap bitmap = new(font.Characters.Sum(x => x.Width), font.Height, PixelFormat.Format8bppIndexed);

			ColorPalette palette = bitmap.Palette;
			for (int i = 0; i < palette.Entries.Length; i++) {
				palette.Entries[i] = Color.FromArgb(pal[i * 4 + 3], pal[i * 4], pal[i * 4 + 1], pal[i * 4 + 2]);
			}
			bitmap.Palette = palette;

			try {
				BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

				int pos = 0;
				foreach (DfFont.Character c in font.Characters) {
					for (int y = 0; y < bitmap.Height; y++) {
						for (int x = 0; x < c.Width; x++) {
							Marshal.WriteByte(data.Scan0, y * data.Stride + x + pos, c.Data[x * bitmap.Height + (bitmap.Height - y - 1)]);
						}
					}
					pos += c.Width;
				}

				bitmap.UnlockBits(data);
			} catch (Exception) {
				bitmap.Dispose();
				throw;
			}
			return bitmap;
		}

		public static Bitmap ToBitmap(this DfFont font,  DfPalette pal, bool forceTransparent) =>
			font.ToBitmap(pal.ToByteArray(forceTransparent));

		public static Bitmap ToBitmap(this DfFont.Character c, int height, byte[] pal) {
			int width = c.Width;

			Bitmap bitmap = new(width, height, PixelFormat.Format8bppIndexed);

			ColorPalette palette = bitmap.Palette;
			for (int i = 0; i < palette.Entries.Length; i++) {
				palette.Entries[i] = Color.FromArgb(pal[i * 4 + 3], pal[i * 4], pal[i * 4 + 1], pal[i * 4 + 2]);
			}
			bitmap.Palette = palette;

			try {
				BitmapData data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

				for (int y = 0; y < height; y++) {
					for (int x = 0; x < width; x++) {
						Marshal.WriteByte(data.Scan0, y * data.Stride + x, c.Data[x * height + (height - y - 1)]);
					}
				}

				bitmap.UnlockBits(data);
			} catch (Exception) {
				bitmap.Dispose();
				throw;
			}
			return bitmap;
		}

		public static Bitmap ToBitmap(this DfFont.Character c, int height, DfPalette pal, bool forceTransparent) =>
			c.ToBitmap(height, pal.ToByteArray(forceTransparent));
	}
}
