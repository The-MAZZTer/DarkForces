using Free.Ports.libpng;
using System;
using UnityEngine;

namespace MZZT {
	public static class LibPngImportExtensions {
		public static png_color ToPngColor(this Color x) => new() {
			red = Math.Clamp((byte)Mathf.FloorToInt(x.r * 256), (byte)0, (byte)255),
			green = Math.Clamp((byte)Mathf.FloorToInt(x.g * 256), (byte)0, (byte)255),
			blue = Math.Clamp((byte)Mathf.FloorToInt(x.b * 256), (byte)0, (byte)255)
		};
	}
}
