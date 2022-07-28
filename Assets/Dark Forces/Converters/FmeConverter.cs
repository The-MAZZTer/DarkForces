using MZZT.DarkForces.FileFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MZZT.DarkForces.Converters {
	public static class FmeConverter {
		public const float SPRITE_PIXELS_PER_UNIT = 400;

		public static Texture2D ToTexture(DfFrame fme, byte[] palette, bool keepTextureReadable = false) {
			byte[] pixels = fme.Pixels;

			int width = fme.Width;
			int height = fme.Height;

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

		public static Texture2D ToTexture(DfFrame fme, DfPalette pal, bool keepTextureReadable = false) =>
			ToTexture(fme, PalConverter.ToByteArray(pal, true), keepTextureReadable);

		public static Texture2D ToTexture(DfFrame fme, DfPalette pal, DfColormap cmp, int lightLevel, bool bypassCmpDithering, bool keepTextureReadable = false) {
			if (lightLevel > 31) {
				lightLevel = 31;
			} else if (lightLevel < 0) {
				lightLevel = 0;
			}

			return ToTexture(fme, CmpConverter.ToByteArray(cmp, pal, lightLevel, true, bypassCmpDithering), keepTextureReadable);
		}

		private static Sprite ToSprite(DfFrame fme, Texture2D texture) =>
			Sprite.Create(texture,
				new Rect(0, 0, fme.Width, fme.Height),
				new Vector2(-fme.InsertionPointX / (float)fme.Width, (fme.Height + fme.InsertionPointY) / (float)fme.Height),
				SPRITE_PIXELS_PER_UNIT);

		public static Sprite ToSprite(DfFrame fme, byte[] palette, bool keepTextureReadable = false) =>
			ToSprite(fme, ToTexture(fme, palette, keepTextureReadable));

		public static Sprite ToSprite(DfFrame fme, DfPalette pal, bool keepTextureReadable = false) =>
			ToSprite(fme, ToTexture(fme, pal, keepTextureReadable));

		public static Sprite ToSprite(DfFrame fme, DfPalette pal, DfColormap cmp, int lightLevel, bool bypassCmpDithering, bool keepTextureReadable = false) =>
			ToSprite(fme, ToTexture(fme, pal, cmp, lightLevel, bypassCmpDithering, keepTextureReadable));
	}
}
