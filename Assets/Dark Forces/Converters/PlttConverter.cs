using MZZT.DarkForces.FileFormats;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

		public static Bitmap ToBitmap(this LandruPalette pltt) {
			Bitmap bitmap = new(16, 16, PixelFormat.Format8bppIndexed);

			System.Drawing.Color[] colors = pltt.ToDrawingColorArray();

			ColorPalette colorPalette = bitmap.Palette;
			for (int j = 0; j < colors.Length; j++) {
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
