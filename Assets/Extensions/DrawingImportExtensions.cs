using UnityEngine;

namespace MZZT {
	/// <summary>
	/// Bridges between System.Drawing and UnityEngine
	/// </summary>
	public static class DrawingImportExtensions {
		public static System.Drawing.Color ToDrawing(this Color x) => System.Drawing.Color.FromArgb(
			Mathf.Clamp(Mathf.FloorToInt(x.a * 256), 0, 255),
			Mathf.Clamp(Mathf.FloorToInt(x.r * 256), 0, 255),
			Mathf.Clamp(Mathf.FloorToInt(x.g * 256), 0, 255),
			Mathf.Clamp(Mathf.FloorToInt(x.b * 256), 0, 255)
		);
	}
}
