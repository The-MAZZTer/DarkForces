#if !SKIASHARP
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using UnityEngine;
using Color = SixLabors.ImageSharp.Color;

namespace MZZT {
	/// <summary>
	/// Bridges between Unity and ImageSharp types.
	/// </summary>
	public static class ImageSharpImportExtensions {
		public static Color ToImageSharp(this UnityEngine.Color x) => Color.FromRgba(
			(byte)Mathf.Clamp(Mathf.FloorToInt(x.r * 256), 0, 255),
			(byte)Mathf.Clamp(Mathf.FloorToInt(x.g * 256), 0, 255),
			(byte)Mathf.Clamp(Mathf.FloorToInt(x.b * 256), 0, 255),
			(byte)Mathf.Clamp(Mathf.FloorToInt(x.a * 256), 0, 255)
		);

		public static Texture2D ToTexture<T>(this Image<T> image) where T : unmanaged, IPixel<T> {
			TextureFormat format = typeof(T).Name switch {
				nameof(A8) => TextureFormat.Alpha8,
				nameof(Bgra32) => TextureFormat.BGRA32,
				nameof(Byte4) => TextureFormat.RGBA32,
				nameof(HalfSingle) => TextureFormat.RHalf,
				nameof(HalfVector2) => TextureFormat.RGHalf,
				nameof(HalfVector4) => TextureFormat.RGBAHalf,
				nameof(L16) => TextureFormat.R16,
				nameof(L8) => TextureFormat.R8,
				nameof(La16) => TextureFormat.RG16,
				nameof(La32) => TextureFormat.RGBA32,
				nameof(Rg32) => TextureFormat.RG32,
				nameof(Rgb24) => TextureFormat.RGB24,
				nameof(Rgb48) => TextureFormat.RGB48,
				nameof(Rgba32) => TextureFormat.RGBA32,
				nameof(Rgba64) => TextureFormat.RGBA64,
				nameof(Short2) => TextureFormat.RG32,
				nameof(Short4) => TextureFormat.RGBA64,
				_ => throw new NotSupportedException()
			};
			Texture2D texture = new(image.Width, image.Height, format, false, true) {
#if UNITY_EDITOR
				alphaIsTransparency = true,
#endif
				filterMode = FilterMode.Point
			};

			int bytesPerPixel = image.PixelType.BitsPerPixel / 8;
			byte[] pixels = new byte[image.Width * image.Height * bytesPerPixel];
			image.CopyPixelDataTo(new Span<byte>(pixels));
			byte[] buffer = new byte[image.Width * bytesPerPixel];
			for (int y = 0; y < image.Height / 2; y++) {
				int src = y * image.Width * 4;
				int dest = (image.Height - y - 1) * image.Width * 4;
				Buffer.BlockCopy(pixels, dest, buffer, 0, image.Width * bytesPerPixel);
				Buffer.BlockCopy(pixels, src, pixels, dest, image.Width * bytesPerPixel);
				Buffer.BlockCopy(buffer, 0, pixels, src, image.Width * bytesPerPixel);
			}

			texture.LoadRawTextureData(pixels);
			texture.Apply(true, true);
			return texture;
		}
	}
}
#endif
