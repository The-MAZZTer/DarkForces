using MZZT.DarkForces.FileFormats;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Color = UnityEngine.Color;

namespace MZZT.DarkForces.Converters {
	public static class CmpConverter {
		public static byte[] ToByteArray(this DfColormap cmp, byte[] palette, int lightLevel, bool transparent, bool bypassCmpDithering) {
			if (lightLevel > 31) {
				lightLevel = 31;
			} else if (lightLevel < 0) {
				lightLevel = 0;
			}

			byte[][] cmpData = cmp.PaletteMaps;

			byte[] litPalette = new byte[256 * 4];
			byte[] map = cmpData[lightLevel];
			// If we want to generate a 24-bit colormap (no difference at light level 0 or 31).
			if (bypassCmpDithering && lightLevel > 0 && lightLevel < 31) {
				// Check each color in the CMP light level and see where it lies on a scale of the color at
				// light level 0 and the color at light level 31, to get an idea of how lit the current
				// light level is.
				float totalWeight = 0;
				float count = 0;
				for (int i = 0; i < 256; i++) {
					byte targetIndex = cmpData[0][i];
					byte targetR = palette[targetIndex * 4];
					byte targetG = palette[targetIndex * 4 + 1];
					byte targetB = palette[targetIndex * 4 + 2];

					byte fullIndex = cmpData[31][i];
					byte fullR = palette[fullIndex * 4];
					byte fullG = palette[fullIndex * 4 + 1];
					byte fullB = palette[fullIndex * 4 + 2];

					// No difference between fully lit and fully dark so skip this color.
					if (fullR == targetR && fullG == targetG && fullB == targetB) {
						continue;
					}

					// Find the currnet light level from 0 to 1, where 0 is fully dark and 1 is fully lit.
					// Add this to the totalWeight.
					byte index = map[i];
					byte r = palette[index * 4];
					byte g = palette[index * 4 + 1];
					byte b = palette[index * 4 + 2];
					if (fullR != targetR) {
						float clamp = Mathf.Clamp01((float)(r - targetR) / (fullR - targetR));
						totalWeight += clamp;
						count++;
					}
					if (fullG != targetG) {
						float clamp = Mathf.Clamp01((float)(g - targetG) / (fullG - targetG));
						totalWeight += clamp;
						count++;
					}
					if (fullB != targetB) {
						float clamp = Mathf.Clamp01((float)(b - targetB) / (fullB - targetB));
						totalWeight += clamp;
						count++;
					}
				}

				// The final weight will be the average % close to fully light (as opposed to fully dark)
				// this light level is.
				float weight = 1;
				if (count > 0) {
					weight = totalWeight / count;
				}

				for (int i = 0; i < 256; i++) {
					if (transparent && i == 0) {
						litPalette[0] = 0;
						litPalette[1] = 0;
						litPalette[2] = 0;
						litPalette[3] = 0;
					} else {
						byte targetIndex = cmpData[0][i];
						byte targetR = palette[targetIndex * 4];
						byte targetG = palette[targetIndex * 4 + 1];
						byte targetB = palette[targetIndex * 4 + 2];

						byte fullIndex = cmpData[31][i];
						byte fullR = palette[fullIndex * 4];
						byte fullG = palette[fullIndex * 4 + 1];
						byte fullB = palette[fullIndex * 4 + 2];

						// Weighted average between light and dark.
						litPalette[i * 4] = (byte)(fullR * weight
							+ targetR * (1 - weight));
						litPalette[i * 4 + 1] = (byte)(fullG * weight
							+ targetG * (1 - weight));
						litPalette[i * 4 + 2] = (byte)(fullB * weight
							+ targetB * (1 - weight));
						// Copy alpha
						litPalette[i * 4 + 3] = palette[map[i] * 4 + 3];
					}
				}
			} else {
				for (int i = 0; i < 256; i++) {
					if (transparent && i == 0) {
						litPalette[0] = 0;
						litPalette[1] = 0;
						litPalette[2] = 0;
						litPalette[3] = 0;
					} else {
						Buffer.BlockCopy(palette, map[i] * 4, litPalette, i * 4, 4);
					}
				}
			}
			return litPalette;
		}

		public static byte[] ToByteArray(this DfColormap cmp, DfPalette pal, int lightLevel, bool transparent, bool bypassCmpDithering) =>
			cmp.ToByteArray(pal.ToByteArray(), lightLevel, transparent, bypassCmpDithering);

		public static System.Drawing.Color[] ToDrawingColorArray(this DfColormap cmp, System.Drawing.Color[] palette, int lightLevel, bool transparent, bool bypassCmpDithering) {
			if (lightLevel > 31) {
				lightLevel = 31;
			} else if (lightLevel < 0) {
				lightLevel = 0;
			}

			byte[][] cmpData = cmp.PaletteMaps;

			System.Drawing.Color[] litPalette = new System.Drawing.Color[256];
			byte[] map = cmpData[lightLevel];
			// If we want to generate a 24-bit colormap (no difference at light level 0 or 31).
			if (bypassCmpDithering && lightLevel > 0 && lightLevel < 31) {
				// Check each color in the CMP light level and see where it lies on a scale of the color at
				// light level 0 and the color at light level 31, to get an idea of how lit the current
				// light level is.
				float totalWeight = 0;
				float count = 0;
				for (int i = 0; i < 256; i++) {
					byte targetIndex = cmpData[0][i];
					System.Drawing.Color target = palette[targetIndex];

					byte fullIndex = cmpData[31][i];
					System.Drawing.Color full = palette[fullIndex];

					// No difference between fully lit and fully dark so skip this color.
					if (full == target) {
						continue;
					}

					// Find the currnet light level from 0 to 1, where 0 is fully dark and 1 is fully lit.
					// Add this to the totalWeight.
					byte index = map[i];
					System.Drawing.Color color = palette[index];
					if (full.R != target.R) {
						float clamp = Mathf.Clamp01((color.R - target.R) / (full.R - target.R));
						totalWeight += clamp;
						count++;
					}
					if (full.G != target.G) {
						float clamp = Mathf.Clamp01((color.G - target.G) / (full.G - target.G));
						totalWeight += clamp;
						count++;
					}
					if (full.B != target.B) {
						float clamp = Mathf.Clamp01((color.B - target.B) / (full.B - target.B));
						totalWeight += clamp;
						count++;
					}
				}

				// The final weight will be the average % close to fully light (as opposed to fully dark)
				// this light level is.
				float weight = 1;
				if (count > 0) {
					weight = totalWeight / count;
				}

				for (int i = 0; i < 256; i++) {
					if (transparent && i == 0) {
						litPalette[0] = default;
					} else {
						byte targetIndex = cmpData[0][i];
						System.Drawing.Color target = palette[targetIndex];

						byte fullIndex = cmpData[31][i];
						System.Drawing.Color full = palette[fullIndex];

						// Weighted average between light and dark.
						litPalette[i] = System.Drawing.Color.FromArgb(
							palette[map[i]].A,
							Mathf.RoundToInt(full.R * weight + target.R + (1 - weight)),
							Mathf.RoundToInt(full.G * weight + target.G + (1 - weight)),
							Mathf.RoundToInt(full.B * weight + target.B + (1 - weight))
						);
					}
				}
			} else {
				for (int i = 0; i < 256; i++) {
					if (transparent && i == 0) {
						litPalette[0] = default;
					} else {
						litPalette[i] = palette[map[i]];
					}
				}
			}
			return litPalette;
		}

		public static System.Drawing.Color[] ToDrawingColorArray(this DfColormap cmp, DfPalette pal, int lightLevel, bool transparent, bool bypassCmpDithering) =>
			cmp.ToDrawingColorArray(pal.ToDrawingColorArray(), lightLevel, transparent, bypassCmpDithering);

		public static Color[] ToUnityColorArray(this DfColormap cmp, Color[] palette, int lightLevel, bool transparent, bool bypassCmpDithering) {
			if (lightLevel > 31) {
				lightLevel = 31;
			} else if (lightLevel < 0) {
				lightLevel = 0;
			}

			byte[][] cmpData = cmp.PaletteMaps;

			Color[] litPalette = new Color[256];
			byte[] map = cmpData[lightLevel];
			// If we want to generate a 24-bit colormap (no difference at light level 0 or 31).
			if (bypassCmpDithering && lightLevel > 0 && lightLevel < 31) {
				// Check each color in the CMP light level and see where it lies on a scale of the color at
				// light level 0 and the color at light level 31, to get an idea of how lit the current
				// light level is.
				float totalWeight = 0;
				float count = 0;
				for (int i = 0; i < 256; i++) {
					byte targetIndex = cmpData[0][i];
					Color target = palette[targetIndex];

					byte fullIndex = cmpData[31][i];
					Color full = palette[fullIndex];

					// No difference between fully lit and fully dark so skip this color.
					if (full == target) {
						continue;
					}

					// Find the currnet light level from 0 to 1, where 0 is fully dark and 1 is fully lit.
					// Add this to the totalWeight.
					byte index = map[i];
					Color color = palette[index];
					if (full.r != target.r) {
						float clamp = Mathf.Clamp01((color.r - target.r) / (full.r - target.r));
						totalWeight += clamp;
						count++;
					}
					if (full.g != target.g) {
						float clamp = Mathf.Clamp01((color.g - target.g) / (full.g - target.g));
						totalWeight += clamp;
						count++;
					}
					if (full.b != target.b) {
						float clamp = Mathf.Clamp01((color.b - target.b) / (full.b - target.b));
						totalWeight += clamp;
						count++;
					}
				}

				// The final weight will be the average % close to fully light (as opposed to fully dark)
				// this light level is.
				float weight = 1;
				if (count > 0) {
					weight = totalWeight / count;
				}

				for (int i = 0; i < 256; i++) {
					if (transparent && i == 0) {
						litPalette[0] = default;
					} else {
						byte targetIndex = cmpData[0][i];
						Color target = palette[targetIndex];

						byte fullIndex = cmpData[31][i];
						Color full = palette[fullIndex];

						// Weighted average between light and dark.
						litPalette[i] = new Color(
							full.r * weight + target.r + (1 - weight),
							full.g * weight + target.g + (1 - weight),
							full.g * weight + target.b + (1 - weight),
							palette[map[i]].a
						);
					}
				}
			} else {
				for (int i = 0; i < 256; i++) {
					if (transparent && i == 0) {
						litPalette[0] = default;
					} else {
						litPalette[i] = palette[map[i]];
					}
				}
			}
			return litPalette;
		}

		public static Color[] ToUnityColorArray(this DfColormap cmp, DfPalette pal, int lightLevel, bool transparent, bool bypassCmpDithering) =>
			cmp.ToUnityColorArray(pal.ToUnityColorArray(), lightLevel, transparent, bypassCmpDithering);

		public static Bitmap ToBitmap(this DfColormap cmp, DfPalette pal, int lightLevel) {
			Bitmap bitmap = new(16, 16, PixelFormat.Format8bppIndexed);

			System.Drawing.Color[] colors = cmp.ToDrawingColorArray(pal, lightLevel, false, false);

			ColorPalette colorPalette = bitmap.Palette;
			for (int j = 0; j < pal.Palette.Length; j++) {
				colorPalette.Entries[j] = colors[j];
			}
			bitmap.Palette = colorPalette;

			BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

			for (int y = 0; y < bitmap.Height; y++) {
				byte[] bytes = Enumerable.Range(y * bitmap.Width, bitmap.Width).Select(x => (byte)x).ToArray();
				Marshal.Copy(bytes, 0, data.Scan0 + data.Stride * y, bytes.Length);
			}

			bitmap.UnlockBits(data);
			return bitmap;
		}

		public static async Task WriteJascPalAsync(this DfColormap cmp, DfPalette pal, int lightLevel, Stream stream) {
			byte[] colors = cmp.ToByteArray(pal, lightLevel, false, false);
			using StreamWriter writer = new(stream, Encoding.ASCII);
			await writer.WriteLineAsync("JASC-PAL");
			await writer.WriteLineAsync("0100");
			await writer.WriteLineAsync("256");

			for (int j = 0; j < 256; j++) {
				await writer.WriteLineAsync($"{colors[j * 4]} {colors[j * 4 + 1]} {colors[j * 4 + 2]}");
			}
		}

		public static async Task WriteRgbPalAsync(this DfColormap cmp, DfPalette pal, int lightLevel, Stream stream) {
			byte[] colors = cmp.ToByteArray(pal, lightLevel, false, false);
			for (int j = 0; j < 256; j++) {
				await stream.WriteAsync(colors, j * 4, 3);
			}
		}

		public static async Task WriteRgbaPalAsync(this DfColormap cmp, DfPalette pal, int lightLevel, Stream stream) {
			byte[] colors = cmp.ToByteArray(pal, lightLevel, false, false);
			await stream.WriteAsync(colors);
		}
	}
}