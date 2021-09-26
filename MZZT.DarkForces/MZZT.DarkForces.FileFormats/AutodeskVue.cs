using MZZT.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MZZT.DarkForces.FileFormats {
	/// <summary>
	/// The data from a VUE file.
	/// </summary>
	public class AutodeskVue : TextBasedFile<AutodeskVue>, ICloneable {
		/// <summary>
		/// An individual object moved by a VUE.
		/// </summary>
		public class VueObject : ICloneable {
			public List<Matrix4x4> Frames { get; } = new();

			object ICloneable.Clone() => this.Clone();
			public VueObject Clone() {
				VueObject clone = new();
				clone.Frames.AddRange(this.Frames);
				return clone;
			}
		}

		/// <summary>
		/// Light colors... not used in Dark Forces.
		/// </summary>
		public struct NormalizedRgbColor {
			/// <summary>
			/// Red
			/// </summary>
			public float R { get; set; }
			/// <summary>
			/// Green
			/// </summary>
			public float G { get; set; }
			/// <summary>
			/// Blue
			/// </summary>
			public float B { get; set; }
		}

		/// <summary>
		/// A point light source specified by the VUE, unused in Dark Forces.
		/// </summary>
		public class Light : ICloneable {
			/// <summary>
			/// The frame to show the light on.
			/// </summary>
			public int Frame { get; set; }
			/// <summary>
			/// The name of the light.
			/// </summary>
			public string Name { get; set; }
			/// <summary>
			/// The position of the light.
			/// </summary>
			public Vector3 Position { get; set; }
			/// <summary>
			/// The color of the light.
			/// </summary>
			public NormalizedRgbColor Color { get; set; }
			/// <summary>
			/// Whether shadows can be cast by this light (Expensive in '95!)
			/// </summary>
			public bool CastsShadows { get; set; }

			object ICloneable.Clone() => this.Clone();
			public virtual Light Clone() => new() {
				CastsShadows = this.CastsShadows,
				Color = this.Color,
				Frame = this.Frame,
				Name = this.Name,
				Position = this.Position
			};
		}

		/// <summary>
		/// A spot light specified by the VUE, unused in Dark Forces.
		/// </summary>
		public class Spotlight : Light {
			/// <summary>
			/// Where the light points at.
			/// </summary>
			public Vector3 Target { get; set; }
			/// <summary>
			/// The inner angle, within where the light shines at full intensity.
			/// </summary>
			public float HotAngle { get; set; }
			/// <summary>
			/// The outer angle, outside where the light does not shine.
			/// </summary>
			public float FalloffAngle { get; set; }

			public override Light Clone() => new Spotlight() {
				CastsShadows = this.CastsShadows,
				Color = this.Color,
				FalloffAngle = this.FalloffAngle,
				Frame = this.Frame,
				HotAngle = this.HotAngle,
				Name = this.Name,
				Position = this.Position,
				Target = this.Target
			};
		}

		/// <summary>
		/// Viewport types supported by VUE.
		/// </summary>
		public enum ViewportTypes {
			Top,
			Bottom,
			Left,
			Right,
			Front,
			Back,
			User,
			Camera
		}

		/// <summary>
		/// Represents a camera or other point in 3D space. Unused by Dark Forces. Not all fields are used by all types.
		/// </summary>
		public class Viewport : ICloneable {
			/// <summary>
			/// The type of viewport.
			/// </summary>
			public ViewportTypes Type { get; set; }
			/// <summary>
			/// The position of the viewport.
			/// </summary>
			public Vector3 Position { get; set; }
			/// <summary>
			/// The size of the viewport.
			/// </summary>
			public float Width { get; set; }
			/// <summary>
			/// Pitch of the viewport.
			/// </summary>
			public float HorizontalAngle { get; set; }
			/// <summary>
			/// Pitch of the viewport.
			/// </summary>
			public float VerticalAngle { get; set; }
			/// <summary>
			/// Roll of the viewport.
			/// </summary>
			public float RollAngle { get; set; }
			/// <summary>
			/// Where the camera is pointing at.
			/// </summary>
			public Vector3 Target { get; set; }
			/// <summary>
			/// Focal length of the camera.
			/// </summary>
			public float FocalLength { get; set; }

			object ICloneable.Clone() => this.Clone();
			public Viewport Clone() => new() {
				FocalLength = this.FocalLength,
				HorizontalAngle = this.HorizontalAngle,
				Position = this.Position,
				RollAngle = this.RollAngle,
				Target = this.Target,
				Type = this.Type,
				VerticalAngle = this.VerticalAngle,
				Width = this.Width
			};
		}

		/// <summary>
		/// Some types of VUES have multiple VUE headers; each one can be separated. It looks like Dark Forces assumes they are all one VUE though so this type is deprecated; there's always only one of these.
		/// </summary>
		public class SubVue : ICloneable {
			/// <summary>
			/// Id specified in the VUE.
			/// </summary>
			public int Id { get; set; }

			/// <summary>
			/// Objects which get moved around by this VUE.
			/// </summary>
			public Dictionary<string, VueObject> Objects { get; } = new();
			/// <summary>
			/// Lights specified by this VUE.
			/// </summary>
			public List<Light> Lights { get; } = new();
			/// <summary>
			/// Viewports specified by this VUE.
			/// </summary>
			public Dictionary<int, Viewport> Viewports { get; } = new();

			object ICloneable.Clone() => this.Clone();
			public SubVue Clone() {
				SubVue clone = new() {
					Id = this.Id
				};
				clone.Lights.AddRange(this.Lights.Select(x => x.Clone()));
				foreach ((string key, VueObject value) in this.Objects) {
					clone.Objects[key] = value.Clone();
				}
				foreach ((int key, Viewport value) in this.Viewports) {
					clone.Viewports[key] = value.Clone();
				}
				return clone;
			}
		}

		/// <summary>
		/// Supported file formats.
		/// </summary>
		public enum Formats {
			SingleVue,
			MultiVue
		}

		/// <summary>
		/// Which Dark Forces VUE type is this?
		/// </summary>
		public Formats Format { get; set; }
		// TODO there might only ever be exactly one of these, refactor to remove this list?
		/// <summary>
		/// Deprecated, there's only ever one of these.
		/// </summary>
		public List<SubVue> Vues { get; } = new();

		public override bool CanLoad => true;

		public override async Task LoadAsync(Stream stream) {
			this.ClearWarnings();

			using StreamReader reader = new(stream, Encoding.ASCII, false, 1024, true);

			string[] line = await this.ReadTokenizedLineAsync(reader);
			if (line?[0].ToUpper() == "VERSION") {
				if (line.Length != 2 || line[1] != "201") {
					this.AddWarning("VERSION tag found but version absent or unexepected value.");
				}

				line = await this.ReadTokenizedLineAsync(reader);
			}

			this.Vues.Clear();

			switch (line?[0].ToLower()) {
				case "vue": {
					this.Format = Formats.MultiVue;

					SubVue currentVue = null;

					while (line != null) {
						switch (line[0].ToLower()) {
							// Start of a VUE
							case "vue": {
								if (line.Length != 2 || !int.TryParse(line[1], NumberStyles.Integer, null, out int id)) {
									id = 0;
									this.AddWarning("Missing or unknown vue id after VUE keyword.");
								}

								// Dark Forces seems to ignore these boundaries, so we should too I guess,
								if (currentVue == null) {
									this.Vues.Add(currentVue = new SubVue() {
										Id = id
									});
								}
							} break;
							// A frame of animation.
							case "transform": {
								if (line.Length != 14 ||
									!float.TryParse(line[2], out float m00) || !float.TryParse(line[3], out float m10) ||
									!float.TryParse(line[4], out float m20) || !float.TryParse(line[5], out float m01) ||
									!float.TryParse(line[6], out float m11) || !float.TryParse(line[7], out float m21) ||
									!float.TryParse(line[8], out float m02) || !float.TryParse(line[9], out float m12) ||
									!float.TryParse(line[10], out float m22) || !float.TryParse(line[11], out float m03) ||
									!float.TryParse(line[12], out float m13) || !float.TryParse(line[13], out float m23)) {

									this.AddWarning("Unknown transform statement format.");
									break;
								}
								string id = line[1];
								// Constract the 4x4 matrix used to position, rotate, and scale the 3D object.
								Matrix4x4 transform = new() {
									M11 = m00,
									M21 = m10,
									M31 = m20,
									M41 = 0,
									M12 = m01,
									M22 = m11,
									M32 = m21,
									M42 = 0,
									M13 = m02,
									M23 = m12,
									M33 = m22,
									M43 = 0,
									M14 = m03,
									M24 = m13,
									M34 = m23,
									M44 = 1
								};

								if (!currentVue.Objects.TryGetValue(id, out VueObject obj)) {
									currentVue.Objects[id] = obj = new();
								}
								obj.Frames.Add(transform);
							} break;
							default:
								this.AddWarning("Unrecognized statement.");
								break;
						}

						line = await this.ReadTokenizedLineAsync(reader);
					}
				} break;
				// Indicates alternatve file format.
				case "frame": {
					this.Format = Formats.SingleVue;

					SubVue currentVue = new();
					this.Vues.Add(currentVue);

					int currentFrame = 0;

					while (line != null) {
						switch (line[0].ToLower()) {
							// Frame number
							case "frame": {
								if (line.Length != 2 || !int.TryParse(line[1], NumberStyles.Integer, null, out int index)) {
									this.AddWarning("Unrecognized or missing FRAME index value.");
									currentFrame++;
									break;
								}
								currentFrame = index;
							} break;
							// A frame of animation.
							case "transform": {
								if (line.Length != 14 ||
									!float.TryParse(line[2], out float m00) || !float.TryParse(line[3], out float m10) ||
									!float.TryParse(line[4], out float m20) || !float.TryParse(line[5], out float m01) ||
									!float.TryParse(line[6], out float m11) || !float.TryParse(line[7], out float m21) ||
									!float.TryParse(line[8], out float m02) || !float.TryParse(line[9], out float m12) ||
									!float.TryParse(line[10], out float m22) || !float.TryParse(line[11], out float m03) ||
									!float.TryParse(line[12], out float m13) || !float.TryParse(line[13], out float m23)) {

									this.AddWarning("Unknown transform statement format.");
									break;
								}

								string id = line[1];
								// Constract the 4x4 matrix used to position, rotate, and scale the 3D object.
								Matrix4x4 transform = new() {
									M11 = m00,
									M21 = m10,
									M31 = m20,
									M41 = 0,
									M12 = m01,
									M22 = m11,
									M32 = m21,
									M42 = 0,
									M13 = m02,
									M23 = m12,
									M33 = m22,
									M43 = 0,
									M14 = m03,
									M24 = m13,
									M34 = m23,
									M44 = 1
								};

								if (!currentVue.Objects.TryGetValue(id, out VueObject obj)) {
									currentVue.Objects[id] = obj = new();
								}

								// Let's just ignore frame counts and assume sequential.
								/*while (obj.Frames.Count < currentFrame) {
									obj.Frames.Add(null);
								}*/

								if (obj.Frames.Count >= currentFrame) {
									obj.Frames.Add(transform);
								} else {
									obj.Frames[currentFrame] = transform;
								}
							} break;
							// A light (not used by DF).
							case "light": {
								if (line.Length != 9 ||
									!float.TryParse(line[2], out float x) ||
									!float.TryParse(line[3], out float y) ||
									!float.TryParse(line[4], out float z) ||
									!float.TryParse(line[5], out float r) ||
									!float.TryParse(line[6], out float g) ||
									!float.TryParse(line[7], out float b) ||
									(line[8] != "1" && line[8] != "0")) {

									this.AddWarning("Unknown light statement format.");
									break;
								}

								currentVue.Lights.Add(new() {
									Frame = currentFrame,
									Name = line[1],
									Position = new() {
										X = x,
										Y = -z,
										Z = y
									},
									Color = new() {
										R = r,
										G = g,
										B = b
									},
									CastsShadows = line[8] == "1"
								});
							} break;
							// A spotlight (not used by DF).
							case "spotlight": {
								if (line.Length != 14 ||
									!float.TryParse(line[2], out float x) ||
									!float.TryParse(line[3], out float y) ||
									!float.TryParse(line[4], out float z) ||
									!float.TryParse(line[5], out float toX) ||
									!float.TryParse(line[6], out float toY) ||
									!float.TryParse(line[7], out float toZ) ||
									!float.TryParse(line[8], out float r) ||
									!float.TryParse(line[9], out float g) ||
									!float.TryParse(line[10], out float b) ||
									!float.TryParse(line[11], out float hotAngle) ||
									!float.TryParse(line[12], out float falloffAngle) ||
									(line[13] != "1" && line[13] != "0")) {

									this.AddWarning("Unknown spotlight statement format.");
									break;
								}

								currentVue.Lights.Add(new Spotlight() {
									Frame = currentFrame,
									Name = line[1],
									Position = new() {
										X = x,
										Y = -z,
										Z = y
									},
									Target = new() {
										X = toX,
										Y = -toZ,
										Z = toY,
									},
									Color = new() {
										R = r,
										G = g,
										B = b
									},
									HotAngle = hotAngle,
									FalloffAngle = falloffAngle,
									CastsShadows = line[13] == "1"
								});
							} break;
							// Viewports (not used by DF).
							case "top":
							case "bottom":
							case "left":
							case "right":
							case "front":
							case "back": {
								if (line.Length != 5 ||
									!Enum.TryParse(line[0], true, out ViewportTypes type) ||
									!float.TryParse(line[1], out float x) ||
									!float.TryParse(line[2], out float y) ||
									!float.TryParse(line[3], out float z) ||
									!float.TryParse(line[4], out float width)) {

									this.AddWarning("Unknown orthogonal viewport statement format.");
									break;
								}

								currentVue.Viewports[currentFrame] = new() {
									Type = type,
									Position = new() {
										X = x,
										Y = -z,
										Z = y
									},
									Width = width
								};
							} break;
							// User viewport (not used by DF).
							case "user": {
								if (line.Length != 8 ||
									!float.TryParse(line[1], out float x) ||
									!float.TryParse(line[2], out float y) ||
									!float.TryParse(line[3], out float z) ||
									!float.TryParse(line[4], out float horiz) ||
									!float.TryParse(line[5], out float vert) ||
									!float.TryParse(line[6], out float roll) ||
									!float.TryParse(line[7], out float width)) {

									this.AddWarning("Unknown user viewport statement format.");
									break;
								}

								currentVue.Viewports[currentFrame] = new() {
									Type = ViewportTypes.User,
									Position = new() {
										X = x,
										Y = -z,
										Z = y
									},
									HorizontalAngle = horiz,
									VerticalAngle = vert,
									RollAngle = roll,
									Width = width
								};
							} break;
							// Camera (not used by DF).
							case "camera": {
								if (line.Length != 9 ||
									!float.TryParse(line[1], out float x) ||
									!float.TryParse(line[2], out float y) ||
									!float.TryParse(line[3], out float z) ||
									!float.TryParse(line[4], out float toX) ||
									!float.TryParse(line[5], out float toY) ||
									!float.TryParse(line[6], out float toZ) ||
									!float.TryParse(line[7], out float roll) ||
									!float.TryParse(line[8], out float focal)) {

									this.AddWarning("Unknown camera viewport statement format.");
									break;
								}

								currentVue.Viewports[currentFrame] = new() {
									Type = ViewportTypes.Camera,
									Position = new() {
										X = x,
										Y = -z,
										Z = y
									},
									Target = new() {
										X = toX,
										Y = -toZ,
										Z = toY
									},
									RollAngle = roll,
									FocalLength = focal
								};
							} break;
							default:
								this.AddWarning($"Unrecognized statement {line[0]}.");
								break;
						}

						line = await this.ReadTokenizedLineAsync(reader);
					}
				} break;
				default:
					this.AddWarning($"Unrecognized statement {line?[0]}.");
					break;
			}
		}

		public override bool CanSave => true;

		public override async Task SaveAsync(Stream stream) {
			this.ClearWarnings();

			using StreamWriter writer = new(stream, Encoding.ASCII, 1024, true);

			switch (this.Format) {
				case Formats.MultiVue:
					foreach (SubVue vue in this.Vues) {
						await this.WriteLineAsync(writer, $"vue {vue.Id}");

						foreach ((string id, Matrix4x4 transform) in vue.Objects
							.SelectMany(x => x.Value.Frames.Select((y, i) => (x.Key, y, i)))
							.OrderBy(x => x.i)
							.ThenBy(x => x.Key)
							.Select(x => (x.Key, x.y))) {

							await this.WriteLineAsync(writer,
								$"transform {this.Escape(id)} {transform.M11:0.0000} {transform.M21:0.0000} {transform.M31:0.0000} {transform.M12:0.0000} {transform.M22:0.0000} {transform.M32:0.0000} {transform.M13:0.0000} {transform.M23:0.0000} {transform.M33:0.0000} {transform.M14:0.0000} {transform.M24:0.0000} {transform.M34:0.0000}");
						}
					}
					break;
				case Formats.SingleVue:
					await writer.WriteLineAsync("VERSION 201");

					SubVue v = this.Vues.Single();
					foreach (IGrouping<int, (string id, Matrix4x4 transform, int i)> frame in v.Objects
						.SelectMany(x => x.Value.Frames.Select((y, i) => (x.Key, y, i)))
						.OrderBy(x => x.i)
						.ThenBy(x => x.Key)
						.GroupBy(x => x.i)) {

						await this.WriteLineAsync(writer, $"frame {frame.Key}");
						foreach ((string id, Matrix4x4 transform, int _) in frame) {
							await this.WriteLineAsync(writer,
								$"transform {this.Escape(id)} {transform.M11:0.0000} {transform.M21:0.0000} {transform.M31:0.0000} {transform.M12:0.0000} {transform.M22:0.0000} {transform.M32:0.0000} {transform.M13:0.0000} {transform.M23:0.0000} {transform.M33:0.0000} {transform.M14:0.0000} {transform.M24:0.0000} {transform.M34:0.0000}");
						}

						foreach (Light light in v.Lights.Where(x => x.Frame == frame.Key)) {
							if (light is Spotlight spotlight) {
								await this.WriteLineAsync(writer, $"spotlight {this.Escape(spotlight.Name)} {spotlight.Position.X:0.0000} {spotlight.Position.Z:0.0000} {-spotlight.Position.Y:0.0000} {spotlight.Target.X:0.0000} {spotlight.Target.Z:0.0000} {-spotlight.Target.Y:0.0000} {spotlight.Color.R:0.0000} {spotlight.Color.G:0.0000} {spotlight.Color.B:0.0000} {spotlight.HotAngle:0.0000} {spotlight.FalloffAngle:0.0000} {(spotlight.CastsShadows ? 1 : 0)}");
							} else {
								await this.WriteLineAsync(writer, $"light {this.Escape(light.Name)} {light.Position.X:0.0000} {light.Position.Z:0.0000} {-light.Position.Y:0.0000} {light.Color.R:0.0000} {light.Color.G:0.0000} {light.Color.B:0.0000} {(light.CastsShadows ? 1 : 0)}");
							}
						}

						if (v.Viewports.TryGetValue(frame.Key, out Viewport viewport)) {
							switch (viewport.Type) {
								case ViewportTypes.User:
									await this.WriteLineAsync(writer, $"user {viewport.Position.X:0.0000} {viewport.Position.Z:0.0000} {-viewport.Position.Y:0.0000} {viewport.HorizontalAngle:0.000} {viewport.VerticalAngle:0.000} {viewport.RollAngle:0.000} {viewport.Width:0.0000}");
									break;
								case ViewportTypes.Camera:
									await this.WriteLineAsync(writer, $"camera {viewport.Position.X:0.0000} {viewport.Position.Z:0.0000} {-viewport.Position.Y:0.0000} {viewport.Target.X:0.0000} {viewport.Target.Z:0.0000} {-viewport.Target.Y:0.0000} {viewport.RollAngle:0.000} {viewport.FocalLength:0.0000}");
									break;
								default:
									await this.WriteLineAsync(writer, $"{viewport.Type.ToString().ToLower()} {viewport.Position.X:0.0000} {viewport.Position.Z:0.0000} {-viewport.Position.Y:0.0000} {viewport.Width:0.0000}");
									break;
							}
						}
					}
					break;
			}
		}

		object ICloneable.Clone() => this.Clone();
		public AutodeskVue Clone() {
			AutodeskVue clone = new() {
				Format = this.Format
			};
			clone.Vues.AddRange(this.Vues.Select(x => x.Clone()));
			return clone;
		}
	}
}
