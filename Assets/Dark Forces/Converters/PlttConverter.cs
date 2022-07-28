using MZZT.DarkForces.FileFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MZZT.DarkForces.Converters {
	public static class PlttConverter {
		public static byte[] ToByteArray(LandruPalette pltt) =>
			Enumerable.Repeat<byte>(0, pltt.First * 4).Concat(
				pltt.Palette.SelectMany(x => new byte[] {
					x.R,
					x.G,
					x.B,
					255
				})
			).ToArray();
	}
}
