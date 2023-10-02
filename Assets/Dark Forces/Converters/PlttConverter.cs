using Free.Ports.libpng;
using MZZT.DarkForces.FileFormats;
using MZZT.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using Color = UnityEngine.Color;

namespace MZZT.DarkForces.Converters {
	public static class PlttConverter {
		public static byte[] ToByteArray(this LandruPalette pltt) =>
			Enumerable.Repeat<byte>(0, pltt.First * 4).Concat(
				pltt.Palette.SelectMany(x => new byte[] {
					x.R,
					x.G,
					x.B,
					255
				})
			).Concat(Enumerable.Repeat<byte>(0, (255 - pltt.Last) * 4)).ToArray();

		public static System.Drawing.Color[] ToDrawingColorArray(this LandruPalette pltt) =>
			Enumerable.Repeat(System.Drawing.Color.Transparent, pltt.First).Concat(pltt.Palette.Select(x => System.Drawing.Color.FromArgb(
				255,
				x.R,
				x.G,
				x.B
			))).Concat(Enumerable.Repeat(System.Drawing.Color.Transparent, 255 - pltt.Last)).ToArray();

		public static Color[] ToUnityColorArray(this LandruPalette pltt) =>
			Enumerable.Repeat<Color>(default, pltt.First).Concat(
				pltt.Palette.Select(x => new Color(x.R / 255f, x.G / 255f, x.B / 255f, 1f))).Concat(
				Enumerable.Repeat<Color>(default, 255 - pltt.Last)).ToArray();

		public static Png ToPng(this LandruPalette pltt) {
			Png png = new(16, 16, PNG_COLOR_TYPE.PALETTE) {
				Palette = pltt.ToDrawingColorArray()
			};

			for (int y = 0; y < 16; y++) {
				png.Data[y] = Enumerable.Range(y * 16, 16).Select(x => (byte)x).ToArray();
			}

			return png;
		}

		public static async Task WriteJascPalAsync(this LandruPalette pltt, Stream stream) {
			byte[] colors = pltt.ToByteArray();

			using StreamWriter writer = new(stream, Encoding.ASCII);
			await writer.WriteLineAsync("JASC-PAL");
			await writer.WriteLineAsync("0100");
			await writer.WriteLineAsync("256");

			for (int j = 0; j < 256; j++) {
				await writer.WriteLineAsync($"{colors[j * 4]} {colors[j * 4 + 1]} {colors[j * 4 + 2]}");
			}
		}

		public static async Task WriteRgbPalAsync(this LandruPalette pltt, Stream stream) {
			byte[] colors = pltt.ToByteArray();
			for (int j = 0; j < 256; j++) {
				await stream.WriteAsync(colors, j * 4, 3);
			}
		}

		public static async Task WriteRgbaPalAsync(this LandruPalette pltt, Stream stream) {
			byte[] colors = pltt.ToByteArray();
			await stream.WriteAsync(colors);
		}
	}
}
