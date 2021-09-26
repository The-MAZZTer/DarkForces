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
	/// Dark Forces 3DO files.
	/// </summary>
	public class Df3dObject : TextBasedFile<Df3dObject>, ICloneable {
		/// <summary>
		/// The different shading modes specified in 3DOs.
		/// </summary>
		public enum ShadingModes {
			/// <summary>
			/// Solid color.
			/// </summary>
			Flat,
			/// <summary>
			/// Shaded color?
			/// </summary>
			Gouraud,
			/// <summary>
			/// Only show vertices.
			/// </summary>
			Vertex,
			/// <summary>
			/// Textured.
			/// </summary>
			Texture,
			/// <summary>
			/// Mixture of Gouraud and Texture.
			/// </summary>
			GourTex,
			/// <summary>
			/// Possibly a special veriant of texture for flat floor/ceiling surfaces.
			/// </summary>
			Plane
		}

		/// <summary>
		/// A triangle or quad polygon specified by a 3DO.
		/// </summary>
		public class Polygon : ICloneable {
			/// <summary>
			/// The vertices of the polygon.
			/// </summary>
			public List<Vector3> Vertices { get; } = new();
			/// <summary>
			/// A palette indexed color for a texture-less polygon.
			/// </summary>
			public byte Color { get; set; }
			/// <summary>
			/// The shading/texturing mode for the polygon.
			/// </summary>
			public ShadingModes ShadingMode { get; set; }
			/// <summary>
			/// The UV map for the polygon.
			/// </summary>
			public List<Vector2> TextureVertices { get; } = new();

			object ICloneable.Clone() => this.Clone();
			public Polygon Clone() {
				Polygon clone = new() {
					Color = this.Color,
					ShadingMode = this.ShadingMode
				};
				clone.TextureVertices.AddRange(this.TextureVertices);
				clone.Vertices.AddRange(this.Vertices);
				return clone;
			}
		}

		/// <summary>
		/// Collections of polygons into named objects.
		/// </summary>
		public class Object : ICloneable {
			/// <summary>
			/// The object name.
			/// </summary>
			public string Name { get; set; }
			/// <summary>
			/// The texture to use to texture the object/
			/// </summary>
			public string TextureFile { get; set; }

			/// <summary>
			/// The polygons used to constrct the object.
			/// </summary>
			public List<Polygon> Polygons { get; } = new();

			object ICloneable.Clone() => this.Clone();
			public Object Clone() {
				Object clone = new() {
					Name = this.Name,
					TextureFile = this.TextureFile
				};
				clone.Polygons.AddRange(this.Polygons.Select(x => x.Clone()));
				return clone;
			}
		}

		/// <summary>
		/// The internal name of the 3DO.
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// The palette to use for the 3DO.
		/// </summary>
		public string PalFile { get; set; }
		/// <summary>
		/// The objects that make up the 3DO.
		/// </summary>
		public List<Object> Objects { get; } = new();

		public override bool CanLoad => true;

		public override async Task LoadAsync(Stream stream) {
			this.ClearWarnings();

			using StreamReader reader = new(stream, Encoding.ASCII, false, 1024, true);

			string[] line = await this.ReadTokenizedLineAsync(reader);
			if (line.Length < 2 || line[0].ToUpper() != "3DO" || line[1] != "1.2" && line[1] != "1.20" && line[1] != "1.30") {
				this.AddWarning("Invalid or missing 3DO version.");
			} else {
				line = await this.ReadTokenizedLineAsync(reader);
			}

			this.Objects.Clear();
			List<string> textures = new();

			int objectCount = 0;
			while (line != null) {
				bool endOfObject = false;
				switch (line[0].ToUpper()) {
					case "3DONAME": {
						this.Name = string.Join(" ", line.Skip(1));
					} break;
					case "OBJECTS": {
						if (line.Length < 2 || !int.TryParse(line[1], NumberStyles.Integer, null, out objectCount)) {
							this.AddWarning("Object count is not a number.");
						}
					} break;
					case "VERTICES":
					case "POLYGONS":
						break;
					case "PALETTE": {
						if (line.Length < 2) {
							this.AddWarning("Palette definition missing filename.");
							break;
						}
						this.PalFile = line[1];
					} break;
					case "TEXTURES": {
						if (line.Length < 2 || !int.TryParse(line[1], NumberStyles.Integer, null, out int texturesCount)) {
							texturesCount = -1;
							this.AddWarning("Texture count is not a number.");
						}

						// Read texture table.
						while (true) { //for (int i = 0; texturesCount < 0 || (i < texturesCount); i++) {
							line = await this.ReadTokenizedLineAsync(reader);
							if (line == null || line[0].ToUpper() != "TEXTURE:") {
								if (textures.Count < texturesCount) {
									this.AddWarning("Unexpected end of texture declarations.");
								}
								break;
							}
							textures.Add(line.Length < 2 ? null : line[1]);
						}
					} continue;
					case "OBJECT": {
						Object obj = new();
						if (line.Length < 2) {
							this.AddWarning("Object definition missing object name.");
						} else {
							obj.Name = line[1];
						}
						this.Objects.Add(obj);

						List<Vector3> vertices = new();
						List<Polygon> triangles = new();
						List<Polygon> quads = new();
						List<Vector2> textureVertices = new();

						if (!endOfObject) {
							line = await this.ReadTokenizedLineAsync(reader);
						}
						endOfObject = false;
						while (!endOfObject && line != null) {
							switch (line[0].ToUpper()) {
								case "VERTICES": {
									if (line.Length < 2 || !int.TryParse(line[1], NumberStyles.Integer, null, out int vertexCount)) {
										vertexCount = -1;
										this.AddWarning("Vertex count is not a number.");
									}

									while (true) { //for (int i = 0; vertexCount < 0 || (i < vertexCount); i++) {
										line = await this.ReadTokenizedLineAsync(reader);
										if (line == null) {
											if (vertices.Count < vertexCount) {
												this.AddWarning("Unexpected end of vertex declarations.");
											}
											break;
										}
										Dictionary<string, string[]> values = TextBasedFile.SplitKeyValuePairs(line);
										string[] value = values.FirstOrDefault().Value;
										if (value == null) {
											if (vertices.Count < vertexCount) {
												this.AddWarning("Unexpected end of vertex declarations.");
											}
											break;
										}

										if (value.Length < 3 ||
											!int.TryParse(values.FirstOrDefault().Key, NumberStyles.Integer, null, out int index) ||
											index < 0 ||
											!float.TryParse(value[0], out float x) ||
											!float.TryParse(value[1], out float y) ||
											!float.TryParse(value[2], out float z)) {

											this.AddWarning("Vertex format is invalid.");
											continue;
										}

										vertices.Add(new() {
											X = x,
											Y = y,
											Z = z
										});
									}
								} continue;
								case "TRIANGLES": {
									if (line.Length < 2 || !int.TryParse(line[1], NumberStyles.Integer, null, out int triangleCount)) {
										triangleCount = -1;
										this.AddWarning("Triangle count is not a number.");
									}

									while (true) { //for (int i = 0; triangleCount < 0 || (i < triangleCount); i++) {
										line = await this.ReadTokenizedLineAsync(reader);
										if (line == null) {
											if (triangles.Count < triangleCount) {
												this.AddWarning("Unexpected end of triangle declarations.");
											}
											break;
										}
										Dictionary<string, string[]> values = TextBasedFile.SplitKeyValuePairs(line);
										string[] value = values.FirstOrDefault().Value;
										if (value == null) {
											if (triangles.Count < triangleCount) {
												this.AddWarning("Unexpected end of triangle declarations.");
											}
											break;
										}

										if (value.Length < 5 ||
											!int.TryParse(values.FirstOrDefault().Key, NumberStyles.Integer, null, out int index) ||
											index < 0 ||
											!int.TryParse(value[0], NumberStyles.Integer, null, out int v1) ||
											v1 < 0 || v1 >= vertices.Count ||
											!int.TryParse(value[1], NumberStyles.Integer, null, out int v2) ||
											v2 < 0 || v2 >= vertices.Count ||
											!int.TryParse(value[2], NumberStyles.Integer, null, out int v3) ||
											v3 < 0 || v3 >= vertices.Count ||
											!byte.TryParse(value[3], NumberStyles.Integer, null, out byte color) ||
											!Enum.TryParse(value[4], true, out ShadingModes shading)) {

											this.AddWarning("Triangle values invalid.");
											continue;
										}
										Polygon triangle = new() {
											Color = color,
											ShadingMode = shading
										};
										triangle.Vertices.Add(vertices[v1]);
										triangle.Vertices.Add(vertices[v2]);
										triangle.Vertices.Add(vertices[v3]);
										triangles.Add(triangle);
									}
								} continue;
								case "QUADS": {
									if (!int.TryParse(line[1], NumberStyles.Integer, null, out int quadCount)) {
										quadCount = -1;
										throw new FormatException("Quad count is not a number.");
									}

									while (true) { // for (int i = 0; quadCount < 0 || (i < quadCount); i++) {
										line = await this.ReadTokenizedLineAsync(reader);
										if (line == null) {
											if (quads.Count < quadCount) {
												this.AddWarning("Unexpected end of quad declarations.");
											}
											break;
										}
										Dictionary<string, string[]> values = TextBasedFile.SplitKeyValuePairs(line);
										string[] value = values.FirstOrDefault().Value;
										if (value == null) {
											if (quads.Count < quadCount) {
												this.AddWarning("Unexpected end of quad declarations.");
											}
											break;
										}

										if (value.Length < 6 ||
											!int.TryParse(values.FirstOrDefault().Key, NumberStyles.Integer, null, out int index) ||
											index < 0 ||
											!int.TryParse(value[0], NumberStyles.Integer, null, out int v1) ||
											v1 < 0 || v1 >= vertices.Count ||
											!int.TryParse(value[1], NumberStyles.Integer, null, out int v2) ||
											v2 < 0 || v2 >= vertices.Count ||
											!int.TryParse(value[2], NumberStyles.Integer, null, out int v3) ||
											v3 < 0 || v3 >= vertices.Count ||
											!int.TryParse(value[3], NumberStyles.Integer, null, out int v4) ||
											v4 < 0 || v4 >= vertices.Count ||
											!byte.TryParse(value[4], NumberStyles.Integer, null, out byte color) ||
											!Enum.TryParse(value[5], true, out ShadingModes shading)) {

											this.AddWarning("Quad values invalid.");
											continue;
										}
										Polygon quad = new() {
											Color = color,
											ShadingMode = shading
										};
										quad.Vertices.Add(vertices[v1]);
										quad.Vertices.Add(vertices[v2]);
										quad.Vertices.Add(vertices[v3]);
										quad.Vertices.Add(vertices[v4]);
										quads.Add(quad);
									}
								} continue;
								case "TEXTURE": {
									if (line.Length < 2) {
										this.AddWarning("Texture statement is invalid.");
										break;
									}

									// TEXTURE can be a prefix for a number of other sections, so we need another switch.
									switch (line[1].ToUpper()) {
										case "VERTICES": {
											if (line.Length < 3 || !int.TryParse(line[2], NumberStyles.Integer, null, out int vertexCount)) {
												vertexCount = -1;
												this.AddWarning("Texture vertex count is not a number.");
											}

											while (true) { //for (int i = 0; vertexCount < 0 || (i < vertexCount); i++) {
												line = await this.ReadTokenizedLineAsync(reader);
												if (line == null) {
													if (textureVertices.Count < vertexCount) {
														this.AddWarning("Unexpected end of texture vertex declarations.");
													}
													break;
												}
												Dictionary<string, string[]> values = TextBasedFile.SplitKeyValuePairs(line);
												string[] value = values.FirstOrDefault().Value;
												if (value == null) {
													if (textureVertices.Count < vertexCount) {
														this.AddWarning("Unexpected end of texture vertex declarations.");
													}
													break;
												}

												if (value.Length < 2 ||
													!float.TryParse(value[0], out float x) ||
													!float.TryParse(value[1], out float y)) {

													this.AddWarning("Texture vertex format is invalid.");
													continue;
												}
												textureVertices.Add(new() {
													X = x,
													Y = y
												});
											}
										} continue;
										case "TRIANGLES": {
											if (line.Length < 3 || !int.TryParse(line[2], NumberStyles.Integer, null, out int triangleCount)) {
												triangleCount = -1;
												this.AddWarning("Texture triangle count is not a number.");
											}
											if (triangleCount != triangles.Count) {
												triangleCount = triangles.Count;
												this.AddWarning("Texture triangle count doesn't match triangle count.");
											}

											while (true) { // for (int i = 0; triangleCount < 0 || (i < triangleCount); i++) {
												line = await this.ReadTokenizedLineAsync(reader);
												if (line == null) {
													break;
												}
												Dictionary<string, string[]> values = TextBasedFile.SplitKeyValuePairs(line);
												string[] value = values.FirstOrDefault().Value;
												if (value == null) {
													break;
												}

												if (value.Length < 3 ||
													!int.TryParse(values.FirstOrDefault().Key, NumberStyles.Integer, null, out int index) ||
													index < 0 || index >= triangles.Count ||
													!int.TryParse(value[0], NumberStyles.Integer, null, out int v1) ||
													v1 < 0 || v1 >= textureVertices.Count ||
													!int.TryParse(value[1], NumberStyles.Integer, null, out int v2) ||
													v2 < 0 || v2 >= textureVertices.Count ||
													!int.TryParse(value[2], NumberStyles.Integer, null, out int v3) ||
													v3 < 0 || v3 >= textureVertices.Count) {

													this.AddWarning("Texture triangle format is invalid!");
													continue;
												}

												triangles[index].TextureVertices.Add(textureVertices[v1]);
												triangles[index].TextureVertices.Add(textureVertices[v2]);
												triangles[index].TextureVertices.Add(textureVertices[v3]);
											}
											bool missing = false;
											foreach (Polygon triangle in triangles) {
												while (triangle.TextureVertices.Count < 3) {
													missing = true;
													triangle.TextureVertices.Add(new());
												}
											}
											if (missing) {
												this.AddWarning("Unexpected end of texture triangle declarations.");
											}
										} continue;
										case "QUADS": {
											if (line.Length < 3 || !int.TryParse(line[2], NumberStyles.Integer, null, out int quadCount)) {
												quadCount = -1;
												this.AddWarning("Texture quad count is not a number.");
											}
											if (quadCount != quads.Count) {
												quadCount = quads.Count;
												this.AddWarning("Texture quad count doesn't match quad count.");
											}

											while (true) { // for (int i = 0; quadCount < 0 || (i < quadCount); i++) {
												line = await this.ReadTokenizedLineAsync(reader);
												if (line == null) {
													break;
												}
												Dictionary<string, string[]> values = TextBasedFile.SplitKeyValuePairs(line);
												string[] value = values.FirstOrDefault().Value;
												if (value == null) {
													break;
												}

												if (value.Length < 4 ||
													!int.TryParse(values.FirstOrDefault().Key, NumberStyles.Integer, null, out int index) ||
													index < 0 || index >= quads.Count ||
													!int.TryParse(value[0], NumberStyles.Integer, null, out int v1) ||
													v1 < 0 || v1 >= textureVertices.Count ||
													!int.TryParse(value[1], NumberStyles.Integer, null, out int v2) ||
													v2 < 0 || v2 >= textureVertices.Count ||
													!int.TryParse(value[2], NumberStyles.Integer, null, out int v3) ||
													v3 < 0 || v3 >= textureVertices.Count ||
													!int.TryParse(value[3], NumberStyles.Integer, null, out int v4) ||
													v4 < 0 || v4 >= textureVertices.Count) {

													this.AddWarning("Texture quad format is invalid.");
													continue;
												}

												quads[index].TextureVertices.Add(textureVertices[v1]);
												quads[index].TextureVertices.Add(textureVertices[v2]);
												quads[index].TextureVertices.Add(textureVertices[v3]);
												quads[index].TextureVertices.Add(textureVertices[v4]);
											}
											bool missing = false;
											foreach (Polygon quad in quads) {
												while (quad.TextureVertices.Count < 4) {
													missing = true;
													quad.TextureVertices.Add(new());
												}
											}
											if (missing) {
												this.AddWarning("Unexpected end of texture quad declarations.");
											}
										} continue;
										default: {
											if (obj.TextureFile != null) {
												this.AddWarning("Duplicate texture statement.");
												break;
											}
											if (line.Length < 2 || !int.TryParse(line[1], NumberStyles.Integer, null, out int texture)
												|| texture >= textures.Count) {

												this.AddWarning("Texture value is invalid.");
												break;
											}
											obj.TextureFile = texture < 0 ? null : textures[texture];
										} break;
									}
								} break;
								case "OBJECT":
									endOfObject = true;
									break;
								default:
									this.AddWarning($"Unknown statement {line[0]}.");
									break;
							}

							if (!endOfObject) {
								line = await this.ReadTokenizedLineAsync(reader);
							}
						}
						obj.Polygons.AddRange(triangles);
						obj.Polygons.AddRange(quads);
					} continue;
					default:
						this.AddWarning($"Unknown statement {line[0]}.");
						break;
				}

				line = await this.ReadTokenizedLineAsync(reader);
			}

			if (this.Objects.Count != objectCount) {
				this.AddWarning("Object count doesn't match number of objects in file.");
			}
		}

		public override bool CanSave => true;

		public override async Task SaveAsync(Stream stream) {
			this.ClearWarnings();

			using StreamWriter writer = new(stream, Encoding.ASCII, 1024, true);

			foreach (Object obj in this.Objects) {
				int[] vertexCount = obj.Polygons
					.SelectMany(x => x.TextureVertices.Count > 0 ? new[] { x.Vertices.Count, x.TextureVertices.Count } : new[] { x.Vertices.Count })
					.Distinct().ToArray();
				if (vertexCount.Length > 1 || (vertexCount[0] != 3 && vertexCount[0] != 4)) {
					this.AddWarning("All polygons in an object must be either all triangles or all quads.");
				}
			}

			await writer.WriteLineAsync("3DO 1.2");
			await this.WriteLineAsync(writer, $"3DONAME {this.Name}");
			await this.WriteLineAsync(writer, $"OBJECTS {this.Objects.Count}");

			Vector3[][] vertices = this.Objects.Select(x => x.Polygons.SelectMany(x => x.Vertices).Distinct().ToArray()).ToArray();

			await this.WriteLineAsync(writer, $"VERTICES {vertices.Select(x => x.Length).Sum()}");
			await this.WriteLineAsync(writer, $"POLYGONS {this.Objects.Select(x => x.Polygons.Count).Sum()}");
			await this.WriteLineAsync(writer, $"PALETTE {this.PalFile}");

			string[] textures = this.Objects.Select(x => x.TextureFile).Where(x => x != null).Distinct().ToArray();

			await this.WriteLineAsync(writer, $"TEXTURES ${textures.Length}");
			foreach (string texture in textures) {
				await this.WriteLineAsync(writer, $"TEXTURE: ${texture}");
			}

			for (int i = 0; i < this.Objects.Count; i++) {
				Object obj = this.Objects[i];
				Vector3[] objVertices = vertices[i];

				await this.WriteLineAsync(writer, $"OBJECT ${this.Escape(obj.Name)}");
				await this.WriteLineAsync(writer, $"TEXTURE ${(obj.TextureFile != null ? Array.IndexOf(textures, obj.TextureFile) : -1)}");

				await this.WriteLineAsync(writer, $"VERTICES {objVertices.Length}");
				for (int j = 0; j < objVertices.Length; j++) {
					Vector3 vertex = objVertices[j];
					await this.WriteLineAsync(writer, $"{j}: {vertex.X:0.000} {vertex.Y:0.000} {vertex.Z:0.000}");
				}

				Polygon[] triangles = obj.Polygons.Where(x => x.Vertices.Count == 3).ToArray();
				if (triangles.Length > 0) {
					await this.WriteLineAsync(writer, $"TRIANGLES {triangles.Length}");
					for (int j = 0; j < triangles.Length; j++) {
						Polygon triangle = triangles[j];
						await this.WriteLineAsync(writer, $"{j}: {Array.IndexOf(objVertices, triangle.Vertices[0])} {Array.IndexOf(objVertices, triangle.Vertices[1])} {Array.IndexOf(objVertices, triangle.Vertices[2])} {triangle.Color} {triangle.ShadingMode.ToString().ToUpper()}");
					}
				}

				Polygon[] quads = obj.Polygons.Where(x => x.Vertices.Count == 4).ToArray();
				if (quads.Length > 0) {
					await this.WriteLineAsync(writer, $"QUADS {quads.Length}");
					for (int j = 0; j < quads.Length; j++) {
						Polygon quad = quads[j];
						await this.WriteLineAsync(writer, $"{j}: {Array.IndexOf(objVertices, quad.Vertices[0])} {Array.IndexOf(objVertices, quad.Vertices[1])} {Array.IndexOf(objVertices, quad.Vertices[2])} {Array.IndexOf(objVertices, quad.Vertices[3])} {quad.Color} {quad.ShadingMode.ToString().ToUpper()}");
					}
				}

				Vector2[] textureVertices = obj.Polygons.SelectMany(x => x.TextureVertices).Distinct().ToArray();
				if (textureVertices.Length > 0) {
					await this.WriteLineAsync(writer, $"TEXTURE VERTICES {textureVertices.Length}");
					for (int j = 0; j < textureVertices.Length; j++) {
						Vector2 textureVertex = textureVertices[j];
						await this.WriteLineAsync(writer, $"{j}: {textureVertex.X:0.00} {textureVertex.Y:0.00}");
					}
				}

				int textureTriangles = triangles.Count(x => x.TextureVertices.Count == 3);
				if (textureTriangles > 0) {
					await this.WriteLineAsync(writer, $"TEXTURE TRIANGLES {textureTriangles}");
					for (int j = 0; j < triangles.Length; j++) {
						Polygon triangle = triangles[j];
						if (triangle.TextureVertices.Count != 3) {
							continue;
						}
						await this.WriteLineAsync(writer, $"{j}: {Array.IndexOf(textureVertices, triangle.TextureVertices[0])} {Array.IndexOf(textureVertices, triangle.TextureVertices[1])} {Array.IndexOf(textureVertices, triangle.TextureVertices[2])}");
					}
				}

				int textureQuads = triangles.Count(x => x.TextureVertices.Count == 4);
				if (textureQuads > 0) {
					await this.WriteLineAsync(writer, $"TEXTURE QUADS {textureQuads}");
					for (int j = 0; j < quads.Length; j++) {
						Polygon quad = quads[j];
						if (quad.TextureVertices.Count != 4) {
							continue;
						}
						await this.WriteLineAsync(writer, $"{j}: {Array.IndexOf(textureVertices, quad.TextureVertices[0])} {Array.IndexOf(textureVertices, quad.TextureVertices[1])} {Array.IndexOf(textureVertices, quad.TextureVertices[2])} {Array.IndexOf(textureVertices, quad.TextureVertices[3])}");
					}
				}
			}
		}

		object ICloneable.Clone() => this.Clone();
		public Df3dObject Clone() {
			Df3dObject clone = new() {
				Name = this.Name,
				PalFile = this.PalFile
			};
			clone.Objects.AddRange(this.Objects.Select(x => x.Clone()));
			return clone;
		}
	}
}
