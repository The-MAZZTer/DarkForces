using MZZT.DarkForces.FileFormats;
using System;
using System.Collections;
using UnityEngine;

namespace MZZT.DarkForces.Converters {
	public static class DeltConverter {
		public static Texture2D ToTexture(LandruDelt delt, byte[] palette, bool keepTextureReadable = false) {
			byte[] pixels = delt.Pixels;
			BitArray mask = delt.Mask;
			int width = delt.Width;
			int height = delt.Height;
			if (width >= 1 && height >= 1) {
				byte[] buffer = new byte[width * height * 4];
				for (int y = 0; y < height; y++) {
					for (int x = 0; x < width; x++) {
						int offset = (height - y - 1) * width + x;
						if (mask[offset]) {
							Buffer.BlockCopy(palette, pixels[offset] * 4, buffer, offset * 4, 4);
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

		public static Texture2D ToTexture(LandruDelt delt, LandruPalette palette, bool keepTextureReadable = false) =>
			ToTexture(delt, PlttConverter.ToByteArray(palette), keepTextureReadable);
	}
}
