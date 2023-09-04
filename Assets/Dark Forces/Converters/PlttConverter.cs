using MZZT.DarkForces.FileFormats;
using System.Linq;
using UnityEngine;

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
			).ToArray();

		public static System.Drawing.Color[] ToDrawingColorArray(this LandruPalette pltt) =>
			Enumerable.Repeat(System.Drawing.Color.Transparent, pltt.First).Concat(pltt.Palette.Select(x => System.Drawing.Color.FromArgb(
				255,
				x.R,
				x.G,
				x.B
			))).ToArray();

		public static Color[] ToUnityColorArray(this LandruPalette pltt) =>
			Enumerable.Repeat<Color>(default, pltt.First).Concat(
				pltt.Palette.Select(x => new Color(x.R / 255f, x.G / 255f, x.B / 255f, 1f))).ToArray();
	}
}
