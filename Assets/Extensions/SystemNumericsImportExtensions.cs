using UnityEngine;
using NMatrix4x4 = System.Numerics.Matrix4x4;
using NVector2 = System.Numerics.Vector2;
using NVector3 = System.Numerics.Vector3;

namespace MZZT {
	/// <summary>
	/// Bridges between System.Numerics and Unity types.
	/// </summary>
	public static class SystemNumericsImportExtensions {
		public static Matrix4x4 ToUnity(this NMatrix4x4 x) => new Matrix4x4(
			new Vector4(x.M11, x.M21, x.M31, x.M41),
			new Vector4(x.M12, x.M22, x.M32, x.M42),
			new Vector4(x.M13, x.M23, x.M33, x.M43),
			new Vector4(x.M14, x.M24, x.M34, x.M44)
		);

		public static Vector2 ToUnity(this NVector2 x) => new Vector2(x.X, x.Y);
		public static Vector3 ToUnity(this NVector3 x) => new Vector3(x.X, x.Y, x.Z);
	}
}
