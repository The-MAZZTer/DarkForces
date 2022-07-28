using MZZT.DarkForces.FileFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MZZT.DarkForces.Converters {
	public static class BmConverter {
		public static Texture2D ToTexture(DfBitmap bm, byte[] palette, bool keepTextureReadable = false) {
			byte[] pixels = bm.Pages[0].Pixels;

			int width = bm.Pages[0].Width;
			int height = bm.Pages[0].Height;

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

		public static Texture2D ToTexture(DfBitmap bm, DfPalette pal, bool forceTransparent, bool keepTextureReadable = false) =>
			ToTexture(bm, PalConverter.ToByteArray(pal, forceTransparent), keepTextureReadable);

		public static Texture2D ToTexture(DfBitmap bm, DfPalette pal, DfColormap cmp, int lightLevel, bool forceTransparent, bool bypassCmpDithering, bool keepTextureReadable = false) {
			if (lightLevel > 31) {
				lightLevel = 31;
			} else if (lightLevel < 0) {
				lightLevel = 0;
			}

			return ToTexture(bm, CmpConverter.ToByteArray(cmp, pal, lightLevel, forceTransparent || (bm.Pages[0].Flags & DfBitmap.Flags.Transparent) > 0,
				bypassCmpDithering), keepTextureReadable);
		}
	}
}
