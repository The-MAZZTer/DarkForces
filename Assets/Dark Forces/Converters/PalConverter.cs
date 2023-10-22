using Free.Ports.libpng;
using MZZT.DarkForces.FileFormats;
using MZZT.Drawing;
using System.IO;
using System.Linq;
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

		public static Png ToPng(this DfPalette pal) {
			Png png = new(16, 16, PNG_COLOR_TYPE.PALETTE) {
				Palette = pal.ToDrawingColorArray(false)
			};

			for (int y = 0; y < 16; y++) {
				png.Data[y] = Enumerable.Range(y * 16, 16).Select(x => (byte)x).ToArray();
			}

			return png;
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
