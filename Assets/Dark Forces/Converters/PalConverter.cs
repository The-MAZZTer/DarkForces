using MZZT.DarkForces.FileFormats;
using System.Linq;
using UnityEngine;

namespace MZZT.DarkForces.Converters {
	public static class PalConverter {
		public static byte[] ToByteArray(DfPalette pal, bool transparent = false) =>
			pal.Palette.SelectMany((x, i) => {
				if (transparent && i == 0) {
					return new byte[] { 0, 0, 0, 0 };
				}
				return new byte[] {
					(byte)Mathf.Clamp(Mathf.Round(x.R * 255 / 63f), 0, 255),
					(byte)Mathf.Clamp(Mathf.Round(x.G * 255 / 63f), 0, 255),
					(byte)Mathf.Clamp(Mathf.Round(x.B * 255 / 63f), 0, 255),
					255
				};
			}).ToArray();
	}
}
