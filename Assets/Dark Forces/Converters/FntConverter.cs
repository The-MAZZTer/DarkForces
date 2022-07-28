using MZZT.DarkForces.FileFormats;
using System;
using System.Linq;
using UnityEngine;

namespace MZZT.DarkForces.Converters {
	public static class FntConverter {
		public static Texture2D ToTexture(DfFont fnt, byte[] palette, bool keepTextureReadable = false) {
			int width = fnt.Characters.Sum(x => x.Width);
			int height = fnt.Height;

			byte[] buffer = new byte[width * height * 4];
			int offset = 0;
			foreach (DfFont.Character c in fnt.Characters) {
				for (int y = 0; y < height; y++) {
					for (int x = 0; x < c.Width; x++) {
						Buffer.BlockCopy(palette, c.Data[x * height + y] * 4, buffer, (y * width + x + offset) * 4, 4);
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

		public static Texture2D ToTexture(DfFont fnt, DfPalette pal, bool forceTransparent, bool keepTextureReadable = false) =>
			ToTexture(fnt, PalConverter.ToByteArray(pal, forceTransparent), keepTextureReadable);

		public static Texture2D ToTexture(DfFont fnt, DfPalette pal, DfColormap cmp, int lightLevel, bool forceTransparent, bool bypassCmpDithering, bool keepTextureReadable = false) {
			if (lightLevel > 31) {
				lightLevel = 31;
			} else if (lightLevel < 0) {
				lightLevel = 0;
			}

			return ToTexture(fnt, CmpConverter.ToByteArray(cmp, pal, lightLevel, forceTransparent, bypassCmpDithering), keepTextureReadable);
		}

		public static Texture2D CharacterToTexture(DfFont fnt, DfFont.Character c, byte[] palette, bool keepTextureReadable = false) {
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

		public static Texture2D CharacterToTexture(DfFont fnt, DfFont.Character c, DfPalette pal, bool forceTransparent, bool keepTextureReadable = false) =>
			CharacterToTexture(fnt, c, PalConverter.ToByteArray(pal, forceTransparent), keepTextureReadable);

		public static Texture2D CharacterToTexture(DfFont fnt, DfFont.Character c, DfPalette pal, DfColormap cmp, int lightLevel, bool forceTransparent, bool bypassCmpDithering, bool keepTextureReadable = false) {
			if (lightLevel > 31) {
				lightLevel = 31;
			} else if (lightLevel < 0) {
				lightLevel = 0;
			}

			return CharacterToTexture(fnt, c, CmpConverter.ToByteArray(cmp, pal, lightLevel, forceTransparent, bypassCmpDithering), keepTextureReadable);
		}
	}
}
