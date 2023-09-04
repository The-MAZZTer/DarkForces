using MZZT.DarkForces.FileFormats;
using System;
using System.Linq;
using UnityEngine;

namespace MZZT.DarkForces.Converters {
	public static class FontConverter {
		public static Texture2D ToTexture(this LandruFont font, Color color, bool keepTextureReadable = false) {
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
					for (int x = 0; x < c.Width; x++) {
						if (c.Pixels[y * font.BytesPerLine * 8 + x]) {
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

		public static Texture2D CharacterToTexture(this LandruFont.Character c, LandruFont font, Color color, bool keepTextureReadable = false) {
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
				for (int x = 0; x < c.Width; x++) {
					if (c.Pixels[y * font.BytesPerLine * 8 + x]) {
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
	}
}
