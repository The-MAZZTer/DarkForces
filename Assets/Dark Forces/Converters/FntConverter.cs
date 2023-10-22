using Free.Ports.libpng;
using MZZT.DarkForces.FileFormats;
using MZZT.Drawing;
using System;
using System.Linq;
using UnityEngine;

namespace MZZT.DarkForces.Converters {
	public static class FntConverter {
		public static Texture2D ToTexture(this DfFont fnt, byte[] palette, bool keepTextureReadable = false) {
			int width = fnt.Characters.Sum(x => x.Width + fnt.Spacing) - fnt.Spacing;
			int height = fnt.Height;

			byte[] buffer = new byte[width * height * 4];
			int offset = 0;
			foreach (DfFont.Character c in fnt.Characters) {
				for (int y = 0; y < height; y++) {
					for (int x = 0; x < c.Width; x++) {
						Buffer.BlockCopy(palette, c.Data[x * height + y] * 4, buffer, (y * width + x + offset) * 4, 4);
					}
				}
				offset += c.Width + fnt.Spacing;
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

			if (cmp == null) {
				return fnt.ToTexture(pal, forceTransparent, keepTextureReadable);
			} else {
				return fnt.ToTexture(cmp.ToByteArray(pal, lightLevel, forceTransparent, bypassCmpDithering), keepTextureReadable);
			}
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

			if (cmp == null) {
				return c.ToTexture(fnt, pal, forceTransparent, keepTextureReadable);
 			} else {
				return c.ToTexture(fnt, cmp.ToByteArray(pal, lightLevel, forceTransparent, bypassCmpDithering), keepTextureReadable);
			}
		}

		public static Png ToPng(this DfFont fnt, byte[] pal) {
			Png png = new(fnt.Characters.Sum(x => x.Width + fnt.Spacing) - fnt.Spacing, fnt.Height, PNG_COLOR_TYPE.PALETTE) {
				Palette = new System.Drawing.Color[256]
			};
			for (int i = 0; i < 256; i++) {
				png.Palette[i] = System.Drawing.Color.FromArgb(pal[i * 4 + 3], pal[i * 4], pal[i * 4 + 1], pal[i * 4 + 2]);
			}

			int pos = 0;
			foreach (DfFont.Character c in fnt.Characters) {
				for (int y = 0; y < fnt.Height; y++) {
					for (int x = 0; x < c.Width; x++) {
						png.Data[fnt.Height - y - 1][x + pos] = c.Data[x * fnt.Height + y];
					}
				}
				pos += c.Width + fnt.Spacing;
			}
			return png;
		}

		public static Png ToPng(this DfFont fnt, DfPalette pal, bool forceTransparent) =>
			fnt.ToPng(pal.ToByteArray(forceTransparent));

		public static Png ToPng(this DfFont.Character c, int height, byte[] pal) {
			int width = c.Width;

			Png png = new(width, height, PNG_COLOR_TYPE.PALETTE) {
				Palette = new System.Drawing.Color[256]
			};
			for (int i = 0; i < 256; i++) {
				png.Palette[i] = System.Drawing.Color.FromArgb(pal[i * 4 + 3], pal[i * 4], pal[i * 4 + 1], pal[i * 4 + 2]);
			}

			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					png.Data[height - y - 1][x] = c.Data[x * height + y];
				}
			}
			return png;
		}

		public static Png ToPng(this DfFont.Character c, int height, DfPalette pal, bool forceTransparent) =>
			c.ToPng(height, pal.ToByteArray(forceTransparent));

		public static DfFont.Character ToFntCharacter(this Png png, int height) {
			if (png.ColorType != PNG_COLOR_TYPE.PALETTE) {
				return null;
			}

			DfFont.Character c = new() {
				Width = (byte)png.Width,
				Data = new byte[png.Width * height],
			};

			for (int y = 0; y < height && y < png.Height; y++) {
				for (int x = 0; x < c.Width; x++) {
					c.Data[x * height + (height - y - 1)] = png.Data[y][x];
				}
			}
			return c;
		}
	}
}
