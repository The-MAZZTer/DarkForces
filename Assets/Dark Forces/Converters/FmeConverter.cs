using Free.Ports.libpng;
using MZZT.DarkForces.FileFormats;
using MZZT.Drawing;
using System;
using System.Drawing;
using UnityEngine;
using static MZZT.DarkForces.FileFormats.AutodeskVue;

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

		public static Png ToPng(this DfFrame fme, byte[] pal) {
			int width = fme.Width;
			int height = fme.Height;

			Png png = new(width, height, PNG_COLOR_TYPE.PALETTE) {
				Palette = new System.Drawing.Color[256]
			};
			for (int i = 0; i < 256; i++) {
				png.Palette[i] = System.Drawing.Color.FromArgb(pal[i * 4 + 3], pal[i * 4], pal[i * 4 + 1], pal[i * 4 + 2]);
			}

			for (int y = 0; y < height; y++) {
				Buffer.BlockCopy(fme.Pixels, y * width, png.Data[height - y - 1], 0, width);
				if (fme.Flip) {
					Array.Reverse(png.Data[height - y - 1]);
				}
			}
			return png;
		}

		public static Png ToPng(this DfFrame fme, DfPalette pal) => fme.ToPng(pal.ToByteArray(true));

		public static Png ToPng(this DfFrame fme, DfPalette pal, DfColormap cmp, int lightLevel, bool bypassCmpDithering) {
			if (lightLevel > 31) {
				lightLevel = 31;
			} else if (lightLevel < 0) {
				lightLevel = 0;
			}

			if (cmp == null) {
				return fme.ToPng(pal);
			} else {
				return fme.ToPng(cmp.ToByteArray(pal, lightLevel, true, bypassCmpDithering));
			}
		}

		public static DfFrame ToFrame(this Png png) {
			if (png.ColorType != PNG_COLOR_TYPE.PALETTE) {
				return null;
			}

			DfFrame frame = new() {
				Width = (int)png.Width,
				Height = (int)png.Height,
				Pixels = new byte[png.Width * png.Height]
			};

			for (int y = 0; y < png.Height; y++) {
				Buffer.BlockCopy(png.Data[png.Height - y - 1], 0, frame.Pixels, y * (int)png.Width, (int)png.Width);
			}
			return frame;
		}
	}
}
