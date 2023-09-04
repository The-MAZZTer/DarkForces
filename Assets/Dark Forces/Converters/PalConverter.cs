using MZZT.DarkForces.FileFormats;
using System.Linq;
using UnityEngine;

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
	}
}
