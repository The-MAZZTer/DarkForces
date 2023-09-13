using MZZT.DarkForces.FileFormats;
using System;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using static MZZT.DarkForces.FileFormats.AutodeskVue;

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


		public static Bitmap ToBitmap(this LandruFont font, UnityEngine.Color color) {
			Bitmap bitmap = new(font.Characters.Sum(x => x.Width + 1) - 1, font.Height, PixelFormat.Format1bppIndexed);

			ColorPalette palette = bitmap.Palette;
			palette.Entries[0] = System.Drawing.Color.FromArgb(0, 0, 0, 0);
			palette.Entries[1] = color.ToDrawing();
			bitmap.Palette = palette;

			try {
				BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);

				int pos = 0;
				foreach (LandruFont.Character c in font.Characters) {
					int stride = Mathf.CeilToInt(c.Width / 8f);
					byte[] buffer = new byte[font.Height * stride];
					c.Pixels.CopyTo(buffer, 0);

					for (int y = 0; y < font.Height; y++) {
						for (int x = 0; x < c.Width; x += 8) {
							byte value = buffer[y * stride + x / 8];
							int offset = data.Stride * y + (pos + x) / 8;
							if (pos % 8 == 0) {
								Marshal.WriteByte(data.Scan0, offset, value);
							} else {
								byte existing = Marshal.ReadByte(data.Scan0, offset);

								Marshal.WriteByte(data.Scan0, offset, (byte)(existing | (value >> (pos % 8))));
								Marshal.WriteByte(data.Scan0, offset + 1, (byte)((value << (8 - (pos % 8)) & 0xFF)));
							}
						}
					}
					pos += c.Width + 1;
				}

				bitmap.UnlockBits(data);
			} catch (Exception) {
				bitmap.Dispose();
				throw;
			}
			return bitmap;
		}


		public static Bitmap ToBitmap(this LandruFont.Character c, int height, UnityEngine.Color color) {
			int width = c.Width;

			Bitmap bitmap = new(width, height, PixelFormat.Format1bppIndexed);

			ColorPalette palette = bitmap.Palette;
			palette.Entries[0] = System.Drawing.Color.FromArgb(0, 0, 0, 0);
			palette.Entries[1] = color.ToDrawing();
			bitmap.Palette = palette;

			try {
				BitmapData data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);

				int stride = Mathf.CeilToInt(width / 8f);
				byte[] buffer = new byte[height * stride];
				c.Pixels.CopyTo(buffer, 0);

				for (int y = 0; y < height; y++) {
					Marshal.Copy(buffer, stride * y, data.Scan0 + y * data.Stride, stride);
				}

				bitmap.UnlockBits(data);
			} catch (Exception) {
				bitmap.Dispose();
				throw;
			}
			return bitmap;
		}
	}
}
