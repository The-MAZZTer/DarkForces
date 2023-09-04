using MZZT.DarkForces.FileFormats;
using System;
using System.Linq;
using UnityEngine;

namespace MZZT.DarkForces.Converters {
	public static class VueConverter {
		public static AnimationClip ToClip(this AutodeskVue.VueObject vue, bool smooth = true) {
			Matrix4x4[] matrices = vue.Frames.Select(x => x.ToUnity()).ToArray();

			// We could be tweaking in/outs for keyframes but I didn't go that far.
			AnimationClip clip = new() {
				legacy = true
			};

			Vector3[] positions = matrices.Select(x => x.GetUnityPositionFromAutodesk() * LevelGeometryGenerator.GEOMETRY_SCALE).ToArray();
			Vector3[] scales = matrices.Select(x => x.lossyScale).ToArray();
			Quaternion[] rotations = matrices.Select(x => x.GetUnityRotationFromAutodesk()).ToArray();

			Type type = typeof(Transform);
			if (!smooth) {
				clip.SetCurve(string.Empty, type, $"{nameof(Transform.localPosition)}.{nameof(Vector3.x)}",
					new AnimationCurve(positions.SelectMany((x, i) => new[] { new Keyframe(i, x.x), new Keyframe(i + 0.999f, x.x) }).ToArray()));
				clip.SetCurve(string.Empty, type, $"{nameof(Transform.localPosition)}.{nameof(Vector3.y)}",
					new AnimationCurve(positions.SelectMany((x, i) => new[] { new Keyframe(i, x.y), new Keyframe(i + 0.999f, x.y) }).ToArray()));
				clip.SetCurve(string.Empty, type, $"{nameof(Transform.localPosition)}.{nameof(Vector3.z)}",
					new AnimationCurve(positions.SelectMany((x, i) => new[] { new Keyframe(i, x.z), new Keyframe(i + 0.999f, x.z) }).ToArray()));
				clip.SetCurve(string.Empty, type, $"{nameof(Transform.localScale)}.{nameof(Vector3.x)}",
					new AnimationCurve(scales.SelectMany((x, i) => new[] { new Keyframe(i, x.x), new Keyframe(i + 0.999f, x.x) }).ToArray()));
				clip.SetCurve(string.Empty, type, $"{nameof(Transform.localScale)}.{nameof(Vector3.y)}",
					new AnimationCurve(scales.SelectMany((x, i) => new[] { new Keyframe(i, x.y), new Keyframe(i + 0.999f, x.y) }).ToArray()));
				clip.SetCurve(string.Empty, type, $"{nameof(Transform.localScale)}.{nameof(Vector3.z)}",
					new AnimationCurve(scales.SelectMany((x, i) => new[] { new Keyframe(i, x.z), new Keyframe(i + 0.999f, x.z) }).ToArray()));
				clip.SetCurve(string.Empty, type, $"{nameof(Transform.localRotation)}.{nameof(Quaternion.w)}",
					new AnimationCurve(rotations.SelectMany((x, i) => new[] { new Keyframe(i, x.w), new Keyframe(i + 0.999f, x.w) }).ToArray()));
				clip.SetCurve(string.Empty, type, $"{nameof(Transform.localRotation)}.{nameof(Quaternion.x)}",
					new AnimationCurve(rotations.SelectMany((x, i) => new[] { new Keyframe(i, x.x), new Keyframe(i + 0.999f, x.x) }).ToArray()));
				clip.SetCurve(string.Empty, type, $"{nameof(Transform.localRotation)}.{nameof(Quaternion.y)}",
					new AnimationCurve(rotations.SelectMany((x, i) => new[] { new Keyframe(i, x.y), new Keyframe(i + 0.999f, x.y) }).ToArray()));
				clip.SetCurve(string.Empty, type, $"{nameof(Transform.localRotation)}.{nameof(Quaternion.z)}",
					new AnimationCurve(rotations.SelectMany((x, i) => new[] { new Keyframe(i, x.z), new Keyframe(i + 0.999f, x.z) }).ToArray()));
			} else {
				clip.SetCurve(string.Empty, type, $"{nameof(Transform.localPosition)}.{nameof(Vector3.x)}",
					new AnimationCurve(positions.Select((x, i) => new Keyframe(i, x.x)).ToArray()));
				clip.SetCurve(string.Empty, type, $"{nameof(Transform.localPosition)}.{nameof(Vector3.y)}",
					new AnimationCurve(positions.Select((x, i) => new Keyframe(i, x.y)).ToArray()));
				clip.SetCurve(string.Empty, type, $"{nameof(Transform.localPosition)}.{nameof(Vector3.z)}",
					new AnimationCurve(positions.Select((x, i) => new Keyframe(i, x.z)).ToArray()));
				clip.SetCurve(string.Empty, type, $"{nameof(Transform.localScale)}.{nameof(Vector3.x)}",
					new AnimationCurve(scales.Select((x, i) => new Keyframe(i, x.x)).ToArray()));
				clip.SetCurve(string.Empty, type, $"{nameof(Transform.localScale)}.{nameof(Vector3.y)}",
					new AnimationCurve(scales.Select((x, i) => new Keyframe(i, x.y)).ToArray()));
				clip.SetCurve(string.Empty, type, $"{nameof(Transform.localScale)}.{nameof(Vector3.z)}",
					new AnimationCurve(scales.Select((x, i) => new Keyframe(i, x.z)).ToArray()));
				clip.SetCurve(string.Empty, type, $"{nameof(Transform.localRotation)}.{nameof(Quaternion.w)}",
					new AnimationCurve(rotations.Select((x, i) => new Keyframe(i, x.w)).ToArray()));
				clip.SetCurve(string.Empty, type, $"{nameof(Transform.localRotation)}.{nameof(Quaternion.x)}",
					new AnimationCurve(rotations.Select((x, i) => new Keyframe(i, x.x)).ToArray()));
				clip.SetCurve(string.Empty, type, $"{nameof(Transform.localRotation)}.{nameof(Quaternion.y)}",
					new AnimationCurve(rotations.Select((x, i) => new Keyframe(i, x.y)).ToArray()));
				clip.SetCurve(string.Empty, type, $"{nameof(Transform.localRotation)}.{nameof(Quaternion.z)}",
					new AnimationCurve(rotations.Select((x, i) => new Keyframe(i, x.z)).ToArray()));
				clip.EnsureQuaternionContinuity();
			}

			return clip;
		}
	}

	public static class MatrixConverter {
		public static Vector3 GetUnityPositionFromAutodesk(this Matrix4x4 matrix) => new(matrix.m03, matrix.m23, matrix.m13);
		public static Quaternion GetUnityRotationFromAutodesk(this Matrix4x4 matrix) => Quaternion.LookRotation(new Vector3(matrix.m01, matrix.m21, matrix.m11), new Vector3(matrix.m02, matrix.m22, matrix.m12));
	}
}
