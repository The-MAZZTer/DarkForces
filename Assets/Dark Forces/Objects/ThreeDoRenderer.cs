using MZZT.DarkForces.FileFormats;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using static MZZT.DarkForces.FileFormats.Df3dObject;
using static MZZT.DarkForces.FileFormats.AutodeskVue;
using Vector3 = UnityEngine.Vector3;

namespace MZZT.DarkForces {
	/// <summary>
	/// Render a 3D object.
	/// </summary>
	public class ThreeDoRenderer : ObjectRenderer {
		public const float ROTATION_SCALE = 1f;
		public const float VUE_DEFAULT_FRAMERATE = 10f;

		/// <summary>
		/// Create the meshes. We can do this before cloning the GameObject, other stuff has to be done after.
		/// </summary>
		/// <param name="threeDo">The 3DO to create meshes from.</param>
		public void CreateGeometry(Df3dObject threeDo) {
			this.gameObject.name = threeDo.Name;

			int vertexColor = threeDo.Objects.SelectMany(x => x.Polygons).FirstOrDefault(x => x.ShadingMode == ShadingModes.Vertex)?.Color ?? -1;
			foreach (Df3dObject.Object obj in threeDo.Objects) {
				foreach (IGrouping<(byte color, ShadingModes mode), Polygon> group in obj.Polygons.GroupBy(x => {
					byte color = x.Color;
					ShadingModes mode = vertexColor < 0 ? x.ShadingMode : ShadingModes.Vertex;
					// Group polygons together by color and mode, color doesn't matter sometimes so pick a constant.
					switch (mode) {
						case ShadingModes.Plane:
							color = 255;
							break;
						case ShadingModes.Texture:
							color = 255;
							break;
						case ShadingModes.Vertex:
							// Vertices always have the first vertex color specified in the file.
							color = (byte)vertexColor;
							break;
					}
					return (color, mode);
				})) {
					// Create a child mesh.
					GameObject polygonGo = new GameObject() {
						name = obj.Name,
						layer = LayerMask.NameToLayer("Objects")
					};
					polygonGo.transform.SetParent(this.gameObject.transform, false);

					ThreeDoPolygonsRenderer renderer = polygonGo.AddComponent<ThreeDoPolygonsRenderer>();
					renderer.Render(obj, group.ToArray());
				}
			}
		}

		public override async Task RenderAsync(DfLevelObjects.Object obj) {
			await base.RenderAsync(obj);

			int lightLevel;
			if (this.CurrentSector != null) {
				lightLevel = this.CurrentSector.Sector.LightLevel;
			} else {
				lightLevel = 31;
			}

			Df3dObject threeDo = await ResourceCache.Instance.Get3dObjectAsync(obj.FileName);
			if (threeDo != null) {
				Queue<ThreeDoPolygonsRenderer> renderers =
					new Queue<ThreeDoPolygonsRenderer>(this.GetComponentsInChildren<ThreeDoPolygonsRenderer>(true));
				int vertexColor = threeDo.Objects.SelectMany(x => x.Polygons).FirstOrDefault(x => x.ShadingMode == ShadingModes.Vertex)?.Color ?? -1;
				foreach (Df3dObject.Object obj2 in threeDo.Objects) {
					// Recreate the polygon groups from the last function.
					foreach (IGrouping<(byte color, ShadingModes mode), Polygon> group in obj2.Polygons.GroupBy(x => {
						byte color = x.Color;
						ShadingModes mode = vertexColor < 0 ? x.ShadingMode : ShadingModes.Vertex;
						switch (mode) {
							case ShadingModes.Plane:
								color = 255;
								break;
							case ShadingModes.Texture:
								color = 255;
								break;
							case ShadingModes.Vertex:
								color = (byte)vertexColor;
								break;
						}
						return (color, mode);
					})) {
						await renderers.Dequeue().ApplyMaterialsAsync(obj2, group.ToArray(), group.Key.mode, group.Key.color, lightLevel);
					}
				}
			}

			await this.ApplyLogicAsync();
		}

		[Flags]
		private enum UpdateFlags {
			Pitch = 0x8,
			Yaw = 0x10,
			Roll = 0x20
		}

		private Vector3 rotationSpeed;

		// Apply any animations specified in the INF.
		private async Task ApplyLogicAsync() {
			// Organize the logic text so we can search through it easily.
			string[] lines = this.Object.Logic?.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>(); ;
			Dictionary<string, string[]> logic = lines.SelectMany(x => TextBasedFile.SplitKeyValuePairs(TextBasedFile.TokenizeLine(x)))
				.GroupBy(x => x.Key.ToUpper()).ToDictionary(x => x.Key, x => x.Last().Value);

			this.rotationSpeed = Vector3.zero;

			if (!logic.TryGetValue("LOGIC", out string[] logicType)) {
				return;
			}
			switch (logicType.FirstOrDefault()?.ToUpper()) {
				// Update will rotate the object.
				case "UPDATE":
					if (!logic.TryGetValue("FLAGS", out string[] strUpdateFlags)) {
						return;
					}
					if (!int.TryParse(strUpdateFlags[0], NumberStyles.Integer, null, out int intUpdateFlags)) {
						return;
					}
					UpdateFlags updateFlags = (UpdateFlags)intUpdateFlags;

					Vector3 rotationSpeed = new Vector3();
					if ((updateFlags & UpdateFlags.Pitch) > 0 && logic.TryGetValue("D_PITCH", out string[] strPitch) &&
						float.TryParse(strPitch[0], out float pitchSpeed)) {

						rotationSpeed.x = pitchSpeed;
					}
					if ((updateFlags & UpdateFlags.Yaw) > 0 && logic.TryGetValue("D_YAW", out string[] strYaw) &&
						float.TryParse(strYaw[0], out float yawSpeed)) {

						rotationSpeed.y = yawSpeed;
					}
					if ((updateFlags & UpdateFlags.Roll) > 0 && logic.TryGetValue("D_ROLL", out string[] strRoll) &&
						float.TryParse(strRoll[0], out float rollSpeed)) {

						rotationSpeed.z = rollSpeed;
					}
					this.rotationSpeed = rotationSpeed;
					break;
				// This will play an animation or two.
				case "KEY":
					if (!logic.TryGetValue("VUE", out string[] strVue)) {
						return;
					}
					logic.TryGetValue("VUE_APPEND", out string[] strVue2);
					if (!logic.TryGetValue("FRAME_RATE", out string[] strFrameRate) || !float.TryParse(strFrameRate[0], out float frameRate)) {
						frameRate = VUE_DEFAULT_FRAMERATE;
					}

					AutodeskVue vue = await ResourceCache.Instance.GetVueAsync(strVue[0]);
					AutodeskVue vue2 = null;
					if (strVue2 != null) {
						vue2 = await ResourceCache.Instance.GetVueAsync(strVue2[0]);
					}

					if (vue == null) {
						break;
					}

					VueObject vueObject;
					if (strVue.Length < 2) {
						// TODO This only happens for CARGOALL.VUE which also happens to have a unique format compared to other VUEs.
						// Possibly I need to stitch all the sub VUEs together to animate this properly? I think it's supposed
						// to loop infinitely and right now it does not do it properly.
						vueObject = vue.Vues.FirstOrDefault()?.Objects.FirstOrDefault().Value;
					} else {
						vueObject = vue.Vues.FirstOrDefault()?.Objects.FirstOrDefault(x => x.Key.ToUpper() == strVue[1].ToUpper()).Value;
					}
					if (vueObject == null) {
						ResourceCache.Instance.AddWarning(strVue[0], "Couldn't load any usable animations.");
					}

					VueObject vueObject2 = null;
					if (vue2 != null) {
						if (strVue2.Length < 2) {
							vueObject2 = vue2.Vues.FirstOrDefault()?.Objects.FirstOrDefault().Value;
						} else {
							vueObject2 = vue2.Vues.FirstOrDefault()?.Objects.FirstOrDefault(x => x.Key.ToUpper() == strVue[1].ToUpper()).Value;
						}
						if (vueObject2 == null) {
							ResourceCache.Instance.AddWarning(strVue2[0], "Couldn't load any usable animations.");
						}
					}

					AnimationClip clip = null;
					if (vueObject != null) {
						clip = ResourceCache.Instance.ImportVue(vueObject);
					}
					AnimationClip clip2 = null;
					if (vueObject2 != null) {
						clip2 = ResourceCache.Instance.ImportVue(vueObject2);
					}

					if (clip == null && clip2 == null) {
						break;
					}

					Animation animation = this.gameObject.AddComponent<Animation>();
					animation.playAutomatically = false;
					animation.wrapMode = WrapMode.Once;
					if (clip != null) {
						animation.AddClip(clip, "VUE");
					}
					if (clip2 != null) {
						animation.AddClip(clip2, "VUE_APPEND");
					}

					this.animationSpeed = frameRate;
					this.twoVues = clip2 != null;
					break;
			}
		}

		private float animationSpeed = VUE_DEFAULT_FRAMERATE;
		private bool twoVues = false;
		private void Update() {
			Animation animation = this.GetComponent<Animation>();
			if (ObjectGenerator.Instance.Animate3dos) {
				this.transform.Rotate(ROTATION_SCALE * Time.deltaTime * this.rotationSpeed, Space.World);

				if (animation != null && !animation.isPlaying) {
					animation.PlayQueued("VUE").speed = this.animationSpeed;
					if (this.twoVues) {
						animation.PlayQueued("VUE_APPEND").speed = this.animationSpeed;
					}
				}
			} else {
				if (animation != null && animation.isPlaying) {
					animation.Stop();
				}

				this.transform.rotation = Quaternion.Euler(this.Object.EulerAngles.ToUnity());
			}
		}
	}
}
