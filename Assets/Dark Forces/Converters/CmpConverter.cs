using MZZT.DarkForces.FileFormats;
using System;
using UnityEngine;

namespace MZZT.DarkForces.Converters {
	public static class CmpConverter {
		public static byte[] ToByteArray(DfColormap cmp, byte[] palette, int lightLevel, bool transparent, bool bypassCmpDithering) {
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
						litPalette[i * 4 + 3] = palette[i * 4 + 3];
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

		public static byte[] ToByteArray(DfColormap cmp, DfPalette pal, int lightLevel, bool transparent, bool bypassCmpDithering) =>
		 ToByteArray(cmp, PalConverter.ToByteArray(pal), lightLevel, transparent, bypassCmpDithering);
	}
}
