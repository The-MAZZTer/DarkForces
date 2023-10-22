#if SKIASHARP
using SkiaSharp;
using UnityEngine;

namespace MZZT {
	/// <summary>
	/// Bridges between Unity and SkiaSharp types.
	/// </summary>
	public static class SkiaSharpImportExtensions {
		public static SKColor ToSkia(this Color x) => new(
			(byte)Mathf.Clamp(Mathf.FloorToInt(x.r * 256), 0, 255),
			(byte)Mathf.Clamp(Mathf.FloorToInt(x.g * 256), 0, 255),
			(byte)Mathf.Clamp(Mathf.FloorToInt(x.b * 256), 0, 255),
			(byte)Mathf.Clamp(Mathf.FloorToInt(x.a * 256), 0, 255)
		);

		public static SKPoint ToSkia(this Vector2 x) => new(
			x.x,
			x.y
		);
	}
}
#endif
