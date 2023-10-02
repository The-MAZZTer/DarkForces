using MZZT.DarkForces.FileFormats;
using System;
using System.Linq;
using UnityEngine;
using MZZT.Drawing;
using Free.Ports.libpng;
using System.Collections;

namespace MZZT.DarkForces.Converters {
	public static class FontConverter {
		public static Texture2D ToTexture(this LandruFont font, UnityEngine.Color color, bool keepTextureReadable = false) {
			int width = font.Characters.Sum(x => x.Width);
			int height = font.Height;

			byte[] buffer = new byte[width * height * 4];

			byte[] foreColor = new byte[] {
				(byte)Mathf.Clamp(Mathf.Floor(color.r * 256), 0, 255),
				(byte)Mathf.Clamp(Mathf.Floor(color.g * 256), 0, 255),
				(byte)Mathf.Clamp(Mathf.Floor(color.b * 256), 0, 255),
				(byte)Mathf.Clamp(Mathf.Floor(color.a * 256), 0, 255)
			};

			int offset = 0;
			foreach (LandruFont.Character c in font.Characters) {
				for (int y = 0; y < height; y++) {
					int stride = y * Mathf.CeilToInt(c.Width / 8f) * 8;
					for (int x = 0; x < c.Width; x++) {
						int part = x % 8;
						if (c.Pixels[stride + x - part + (7 - part)]) {
							Buffer.BlockCopy(foreColor, 0, buffer, ((height - y - 1) * (width * 4)) + ((offset + x) * 4), 4);
						}
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

		public static Texture2D ToTexture(this LandruFont.Character c, LandruFont font, UnityEngine.Color color, bool keepTextureReadable = false) {
			int width = c.Width;
			int height = font.Height;

			byte[] buffer = new byte[width * height * 4];

			byte[] foreColor = new byte[] {
				(byte)Mathf.Clamp(Mathf.Floor(color.r * 256), 0, 255),
				(byte)Mathf.Clamp(Mathf.Floor(color.g * 256), 0, 255),
				(byte)Mathf.Clamp(Mathf.Floor(color.b * 256), 0, 255),
				(byte)Mathf.Clamp(Mathf.Floor(color.a * 256), 0, 255)
			};

			for (int y = 0; y < height; y++) {
				int stride = y * Mathf.CeilToInt(c.Width / 8f) * 8;
				for (int x = 0; x < c.Width; x++) {
					int part = x % 8;
					if (c.Pixels[stride + x - part + (7 - part)]) {
						Buffer.BlockCopy(foreColor, 0, buffer, ((height - y - 1) * (width * 4)) + (x * 4), 4);
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

		public static Png ToPng(this LandruFont font, UnityEngine.Color color) {
			Png png = new(font.Characters.Sum(x => x.Width + 1) - 1, font.Height, PNG_COLOR_TYPE.PALETTE) {
				Palette = new System.Drawing.Color[] {
					System.Drawing.Color.Transparent,
					color.ToDrawing()
				}
			};

			int pos = 0;
			foreach (LandruFont.Character c in font.Characters) {
				for (int y = 0; y < font.Height; y++) {
					for (int x = 0; x < c.Width; x ++) {
						png.Data[y][pos + x] = c.Pixels[(font.Height - y - 1) * c.Width + x] ? (byte)1 : (byte)1;
					}
				}
				pos += c.Width + 1;
			}
			return png;
		}

		public static Png ToPng(this LandruFont.Character c, int height, Color color) {
			Png png = new(c.Width, height, PNG_COLOR_TYPE.PALETTE) {
				Palette = new System.Drawing.Color[] {
					System.Drawing.Color.Transparent,
					color.ToDrawing()
				}
			};

			for (int y = 0; y < height; y++) {
				for (int x = 0; x < c.Width; x++) {
					png.Data[y][x] = c.Pixels[(height - y - 1) * c.Width + x] ? (byte)1 : (byte)0;
				}
			}
			return png;
		}

		public static LandruFont.Character ToFontCharacter(this Png png, int height) {
			if (png.ColorType != PNG_COLOR_TYPE.PALETTE) {
				return null;
			}

			LandruFont.Character c = new() {
				Width = (byte)png.Width ,
				Pixels = new BitArray((int)png.Width * height),
			};

			for (int y = 0; y < height && y < png.Height; y++) {
				for (int x = 0; x < c.Width; x++) {
					c.Pixels[y * (int)png.Width + x] = png.Data[height - y - 1][x] > 0;
				}
			}
			return c;
		}
	}
}
