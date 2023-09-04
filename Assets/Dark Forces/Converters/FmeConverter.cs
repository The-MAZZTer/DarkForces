using MZZT.DarkForces.FileFormats;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using UnityEngine;
using Color = System.Drawing.Color;

namespace MZZT.DarkForces.Converters {
	public static class FmeConverter {
		public const float SPRITE_PIXELS_PER_UNIT = 400;

		public static Texture2D ToTexture(this DfFrame fme, byte[] palette, bool keepTextureReadable = false) {
			byte[] pixels = fme.Pixels;

			int width = fme.Width;
			int height = fme.Height;

			byte[] buffer = new byte[width * height * 4];
			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					int realx = x;
					if (fme.Flip) {
						realx = width - x - 1;
					}

					Buffer.BlockCopy(palette, pixels[y * width + x] * 4, buffer, (y * width + realx) * 4, 4);
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

		public static Texture2D ToTexture(this DfFrame fme, DfPalette pal, bool keepTextureReadable = false) =>
			fme.ToTexture(pal.ToByteArray(true), keepTextureReadable);

		public static Texture2D ToTexture(this DfFrame fme, DfPalette pal, DfColormap cmp, int lightLevel, bool bypassCmpDithering, bool keepTextureReadable = false) {
			if (lightLevel > 31) {
				lightLevel = 31;
			} else if (lightLevel < 0) {
				lightLevel = 0;
			}

			return fme.ToTexture(cmp.ToByteArray(pal, lightLevel, true, bypassCmpDithering), keepTextureReadable);
		}

		private static Sprite ToSprite(this DfFrame fme, Texture2D texture) =>
			Sprite.Create(texture,
				new Rect(0, 0, fme.Width, fme.Height),
				new Vector2(-fme.InsertionPointX / (float)fme.Width, (fme.Height + fme.InsertionPointY) / (float)fme.Height),
				SPRITE_PIXELS_PER_UNIT);

		public static Sprite ToSprite(this DfFrame fme, byte[] palette, bool keepTextureReadable = false) =>
			fme.ToSprite(fme.ToTexture(palette, keepTextureReadable));

		public static Sprite ToSprite(this DfFrame fme, DfPalette pal, bool keepTextureReadable = false) =>
			fme.ToSprite(fme.ToTexture(pal, keepTextureReadable));

		public static Sprite ToSprite(this DfFrame fme, DfPalette pal, DfColormap cmp, int lightLevel, bool bypassCmpDithering, bool keepTextureReadable = false) =>
			fme.ToSprite(fme.ToTexture(pal, cmp, lightLevel, bypassCmpDithering, keepTextureReadable));

		public static Bitmap ToBitmap(this DfFrame fme, byte[] pal) {
			byte[] pixels = fme.Pixels;

			int width = fme.Width;
			int height = fme.Height;

			Bitmap bitmap = new(width, height, PixelFormat.Format8bppIndexed);

			ColorPalette palette = bitmap.Palette;
			for (int i = 0; i < palette.Entries.Length; i++) {
				palette.Entries[i] = Color.FromArgb(pal[i * 4 + 3], pal[i * 4], pal[i * 4 + 1], pal[i * 4 + 2]);
			}
			bitmap.Palette = palette;

			try {
				BitmapData data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

				for (int y = 0; y < height; y++) {
					if (!fme.Flip) {
						Marshal.Copy(pixels, width * y, data.Scan0 + data.Stride * (height - y - 1), data.Width);
					} else {
						for (int x = 0; x < width; x++) {
							Marshal.Copy(pixels, width * y + x, data.Scan0 + data.Stride * (height - y - 1) + (width - x - 1), 1);
						}
					}
				}

				bitmap.UnlockBits(data);
			} catch (Exception) {
				bitmap.Dispose();
				throw;
			}
			return bitmap;
		}

		public static Bitmap ToBitmap(this DfFrame fme, DfPalette pal) =>
			fme.ToBitmap(pal.ToByteArray(true));

		public static Bitmap ToBitmap(this DfFrame fme, DfPalette pal, DfColormap cmp, int lightLevel, bool bypassCmpDithering) {
			if (lightLevel > 31) {
				lightLevel = 31;
			} else if (lightLevel < 0) {
				lightLevel = 0;
			}

			return fme.ToBitmap(cmp.ToByteArray(pal, lightLevel, true, bypassCmpDithering));
		}
	}
}
