using MZZT.DarkForces.FileFormats;
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
	public static class PalConverter {
		public static byte[] ToByteArray(this DfPalette pal, bool transparent = false) =>
			pal.Palette.SelectMany((x, i) => {
				if (transparent && i == 0) {
					return new byte[] { 0, 0, 0, 0 };
				}
				return x.ToByteArray();
			}).ToArray();

		public static byte[] ToByteArray(this RgbColor color) => new byte[] {
			(byte)Mathf.Clamp(Mathf.Round(color.R * 255 / 63f), 0, 255),
			(byte)Mathf.Clamp(Mathf.Round(color.G * 255 / 63f), 0, 255),
			(byte)Mathf.Clamp(Mathf.Round(color.B * 255 / 63f), 0, 255),
			255
		};

		public static System.Drawing.Color[] ToDrawingColorArray(this DfPalette pal, bool transparent = false) =>
			pal.Palette.Select((x, i) => {
				if (transparent && i == 0) {
					return System.Drawing.Color.Transparent;
				}
				return x.ToDrawingColor();
			}).ToArray();

		public static System.Drawing.Color ToDrawingColor(this RgbColor color) => System.Drawing.Color.FromArgb(
			255,
			(byte)Mathf.Clamp(Mathf.Round(color.R * 255 / 63f), 0, 255),
			(byte)Mathf.Clamp(Mathf.Round(color.G * 255 / 63f), 0, 255),
			(byte)Mathf.Clamp(Mathf.Round(color.B * 255 / 63f), 0, 255)
		);

		public static Color[] ToUnityColorArray(this DfPalette pal, bool transparent = false) =>
			pal.Palette.Select((x, i) => { 
				if (transparent && i == 0) {
					return default;
				}
				return x.ToUnityColor();
			}).ToArray();

		public static Color ToUnityColor(this RgbColor color) => new(color.R / 63f, color.G / 63f, color.B / 63f, 1f);

		public static Bitmap ToBitmap(this DfPalette pal) {
			Bitmap bitmap = new(16, 16, PixelFormat.Format8bppIndexed);

			System.Drawing.Color[] colors = pal.ToDrawingColorArray(false);

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

		public async static Task WriteJascPalAsync(this DfPalette pal, Stream stream) {
			byte[] colors = pal.ToByteArray(false);
			using StreamWriter writer = new(stream, Encoding.ASCII);
			await writer.WriteLineAsync("JASC-PAL");
			await writer.WriteLineAsync("0100");
			await writer.WriteLineAsync("256");

			for (int j = 0; j < 256; j++) {
				await writer.WriteLineAsync($"{colors[j * 4]} {colors[j * 4 + 1]} {colors[j * 4 + 2]}");
			}
		}

		public async static Task WriteRgbPalAsync(this DfPalette pal, Stream stream) {
			byte[] colors = pal.ToByteArray(false);
			for (int j = 0; j < 256; j++) {
				await stream.WriteAsync(colors, j * 4, 3);
			}
		}

		public async static Task WriteRgbaPalAsync(this DfPalette pal, Stream stream) {
			byte[] colors = pal.ToByteArray(false);
			await stream.WriteAsync(colors);
		}
	}
}
