using MZZT.DarkForces.FileFormats;
using SkiaSharp;
using System;
using System.Collections;
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

		public static Texture2D ToTexture(this LandruDelt delt, LandruPalette pltt, bool keepTextureReadable = false) =>
			delt.ToTexture(pltt.ToByteArray(), keepTextureReadable);

		public static SKImage ToSKImage(this LandruDelt delt, byte[] palette) {
			byte[] pixels = delt.Pixels;
			BitArray mask = delt.Mask;
			int width = delt.Width;
			int height = delt.Height;

			if (width < 1 || height < 1) {
				return null;
			}
			
			SKData data = SKData.Create(width * height * 4);
			Span<byte> span = data.Span;
			Span<byte> paletteSpan = palette.AsSpan();
			Span<byte> zero = new(new byte[] { 0, 0, 0, 0 });

			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					int offset = y * width + x;
					int destOffset = (height - y - 1) * width + x;
					if (mask[offset]) {
						paletteSpan.Slice(pixels[offset] * 4, 4).CopyTo(span.Slice(destOffset * 4, 4));
					} else {
						zero.CopyTo(span.Slice(destOffset * 4, 4));
					}
				}
			}

			return SKImage.FromPixels(new SKImageInfo(width, height, SKColorType.Rgba8888), data, width * 4);
		}
		public static SKImage ToSKImage(this LandruDelt delt, LandruPalette pltt) =>
			delt.ToSKImage(pltt.ToByteArray());
	}
}
