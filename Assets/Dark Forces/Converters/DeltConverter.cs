using Free.Ports.libpng;
using MZZT.Drawing;
using MZZT.DarkForces.FileFormats;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

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

		public static Png ToUnmaskedPng(this LandruDelt delt, byte[] pal) {
			int width = delt.Width;
			int height = delt.Height;

			Png png = new(width, height, PNG_COLOR_TYPE.PALETTE) {
				Palette = new System.Drawing.Color[256]
			};
			for (int i = 0; i < 256; i++) {
				png.Palette[i] = System.Drawing.Color.FromArgb(pal[i * 4 + 3], pal[i * 4], pal[i * 4 + 1], pal[i * 4 + 2]);
			}
			
			for (int y = 0; y < height; y++) {
				Buffer.BlockCopy(delt.Pixels, y * width, png.Data[y], 0, width);
			}
			return png;
		}

		public static Png ToUnmaskedPng(this LandruDelt delt, LandruPalette pltt) => delt.ToUnmaskedPng(pltt.ToByteArray());

		public static Png MaskToPng(this LandruDelt delt) {
			int width = delt.Width;
			int height = delt.Height;

			Png png = new(width, height, PNG_COLOR_TYPE.PALETTE) {
				Palette = new System.Drawing.Color[] {
					System.Drawing.Color.Transparent,
					System.Drawing.Color.FromArgb(255, 255, 255, 255)
				}
			};

			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					png.Data[y][x] = delt.Mask.Get(y * width + x) ? (byte)1 : (byte)0;
				}
			}
			return png;
		}

		public static Png ToMaskedPng(this LandruDelt delt, byte[] pal) {
			int width = delt.Width;
			int height = delt.Height;

			Png png = new(width, height, PNG_COLOR_TYPE.RGB_ALPHA);

			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					Buffer.BlockCopy(pal, delt.Pixels[y * width + x], png.Data[y], x * 4, 4);
				}
			}
			return png;
		}
		public static Png ToMaskedPng(this LandruDelt delt, LandruPalette pltt) => delt.ToMaskedPng(pltt.ToByteArray());

		public static LandruDelt ToDelt(this Png png) {
			if (png.ColorType != PNG_COLOR_TYPE.PALETTE) {
				return null;
			}

			LandruDelt delt = new() {
				Width = (int)png.Width,
				Height = (int)png.Height,
				Mask = new BitArray((int)(png.Width * png.Height), true),
				Pixels = new byte[png.Width * png.Height]
			};

			for (int y = 0; y < png.Height; y++) {
				Buffer.BlockCopy(png.Data[y], 0, delt.Pixels, (int)(y * png.Width), (int)png.Width);

				for (int x = 0; x < png.Width; x++) {
					delt.Mask[(int)(y * png.Width + x)] = png.Palette[png.Data[y][x]].A > 0x7F;
				}
			}
			return delt;
		}

		public static BitArray ToDeltMask(this Png png) {
			bool palette = png.ColorType.HasFlag(PNG_COLOR_TYPE.PALETTE_MASK);

			byte[] transparent = null;
			if (!png.ColorType.HasFlag(PNG_COLOR_TYPE.ALPHA_MASK) && !palette && png.TransparentColor != null) {
				if (png.ColorType == PNG_COLOR_TYPE.GRAY) {
					transparent = new[] { (byte)png.TransparentColor.Value.gray };
				} else {
					transparent = new[] { (byte)png.TransparentColor.Value.red, (byte)png.TransparentColor.Value.green,
						(byte)png.TransparentColor.Value.blue };
				}
			}

			int pixelSize = png.BytesPerPixel;
			BitArray bits = new((int)(png.Width * png.Height));
			for (int i = 0; i < png.Height; i++) {
				for (int j = 0; j < png.Width; j++) {
					byte[] color = png.Data[i].Skip(j * pixelSize).Take(pixelSize).ToArray();
					byte alpha = 255;
					if (png.ColorType.HasFlag(PNG_COLOR_TYPE.ALPHA_MASK)) {
						alpha = color[color.Length - 1];
					} else if (palette) {
						alpha = png.Palette[color[0]].A;
					} else if (transparent != null) {
						alpha = color.SequenceEqual(transparent) ? (byte)0 : (byte)255;
					}
					bits.Set(i * (int)png.Width + j, alpha > 0x7F);
				}
			}
			return bits;
		}
	}
}
