using Free.Ports.libpng;
using MZZT.DarkForces.FileFormats;
using MZZT.Drawing;
using System;
using UnityEngine;

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

		public static Png ToPng(this DfBitmap.Page page, byte[] pal) {
			int width = page.Width;
			int height = page.Height;

			Png png = new(width, height, PNG_COLOR_TYPE.PALETTE) {
				Palette = new System.Drawing.Color[256]
			};
			for (int i = 0; i < 256; i++) {
				png.Palette[i] = System.Drawing.Color.FromArgb(pal[i * 4 + 3], pal[i * 4], pal[i * 4 + 1], pal[i * 4 + 2]);
			}

			for (int y = 0; y < height; y++) {
				Buffer.BlockCopy(page.Pixels, y * width, png.Data[height - y - 1], 0, width);
			}
			return png;
		}

		public static Png ToPng(this DfBitmap.Page page, DfPalette pal, bool forceTransparent) =>
			page.ToPng(pal.ToByteArray(forceTransparent || (page.Flags & DfBitmap.Flags.Transparent) > 0));

		public static Png ToPng(this DfBitmap.Page page, DfPalette pal, DfColormap cmp, int lightLevel, bool forceTransparent,
			bool bypassCmpDithering) {

			if (lightLevel > 31) {
				lightLevel = 31;
			} else if (lightLevel < 0) {
				lightLevel = 0;
			}

			if (cmp == null) {
				return page.ToPng(pal, forceTransparent || (page.Flags & DfBitmap.Flags.Transparent) > 0);
			} else {
				return page.ToPng(cmp.ToByteArray(pal, lightLevel, forceTransparent || (page.Flags & DfBitmap.Flags.Transparent) > 0,
					bypassCmpDithering));
			}
		}

		public static DfBitmap.Page ToBmPage(this Png png) {
			if (png.ColorType != PNG_COLOR_TYPE.PALETTE) {
				return null;
			}

			DfBitmap.Page page = new() {
				Flags = (Math.Abs(Math.Log(png.Width, 2) % 1) < 0.001 && Math.Abs(Math.Log(png.Height, 2) % 1) < 0.001) ? DfBitmap.Flags.NotWeapon : 0,
				Width = (ushort)png.Width,
				Height = (ushort)png.Height,
				Pixels = new byte[png.Width * png.Height]
			};

			for (int y = 0; y < png.Height; y++) {
				Buffer.BlockCopy(png.Data[png.Height - y - 1], 0, page.Pixels, y * (int)png.Width, (int)png.Width);
			}
			return page;
		}
	}
}
