using MZZT.DarkForces.Converters;
using MZZT.DarkForces.FileFormats;
using MZZT.FileFormats;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static MZZT.DarkForces.FileFormats.Df3dObject;

namespace MZZT.DarkForces.Showcase {
	/// <summary>
	/// Render a 3D object.
	/// </summary>
	public class ThreeDoModel : MonoBehaviour {
		private async Task<T> GetFileAsync<T>(string baseFile, string file) where T : File<T>, IDfFile, new() {
			if (Path.GetInvalidPathChars().Intersect(file).Any()) {
				return null;
			}

			if (baseFile != null) {
				string folder = Path.GetDirectoryName(baseFile);
				string path = Path.Combine(folder, file);
				return await DfFileManager.Instance.ReadAsync<T>(path) ?? await ResourceCache.Instance.GetAsync<T>(file);
			}

			return await ResourceCache.Instance.GetAsync<T>(file);
		}

		private string path;
		private Df3dObject threeDo;

		/// <summary>
		/// Create the meshes.
		/// </summary>
		/// <param name="threeDo">The 3DO to create meshes from.</param>
		public async Task SetAsync(string path, Df3dObject threeDo, int lightLevel, Shader colorShader, Shader simpleShader, CancellationToken token) {
			this.gameObject.name = threeDo.Name;

			this.path = path;
			this.threeDo = threeDo;

			byte[] palette = await this.GeneratePaletteAsync(lightLevel, token);

			// TODO optimize this better for refreshing existing 3DOs
			foreach (Transform child in this.transform.Cast<Transform>().ToArray()) {
				DestroyImmediate(child.gameObject);
			}

			int vertexColor = threeDo.Objects.SelectMany(x => x.Polygons).FirstOrDefault(x => x.ShadingMode == ShadingModes.Vertex)?.Color ?? -1;
			foreach (Df3dObject.Object obj in threeDo.Objects) {
				foreach (Polygon polygon in obj.Polygons) {
					byte colorIndex = polygon.Color;
					ShadingModes mode = vertexColor < 0 ? polygon.ShadingMode : ShadingModes.Vertex;
					// Group polygons together by color and mode, color doesn't matter sometimes so pick a constant.
					switch (mode) {
						case ShadingModes.Plane:
							colorIndex = 255;
							break;
						case ShadingModes.Texture:
							colorIndex = 255;
							break;
						case ShadingModes.Vertex:
							// Vertices always have the first vertex color specified in the file.
							colorIndex = (byte)vertexColor;
							break;
					}

					// Create a child mesh.
					GameObject polygonGo = new() {
						name = obj.Name,
						layer = LayerMask.NameToLayer("Objects")
					};
					polygonGo.transform.SetParent(this.gameObject.transform, false);

					ThreeDoModelMesh renderer = polygonGo.AddComponent<ThreeDoModelMesh>();
					await renderer.RenderAsync(path, threeDo, obj, polygon, colorIndex, palette, colorShader, simpleShader, token);
				}
			}
		}

		private async Task<byte[]> GeneratePaletteAsync(int lightLevel, CancellationToken token) {
			string palFile = this.threeDo.PalFile;
			DfPalette pal;
			DfColormap cmp = null;
			byte[] palette;
			if (this.lastPalette == null || palFile != this.lastPaletteFile) {
				pal = await this.GetFileAsync<DfPalette>(this.path, palFile);

				token.ThrowIfCancellationRequested();
				if (pal != null) {
					cmp = await this.GetFileAsync<DfColormap>(this.path, $"{Path.GetFileNameWithoutExtension(palFile)}.CMP");

					token.ThrowIfCancellationRequested();
				} else {
					pal = await this.GetFileAsync<DfPalette>(this.path, "SECBASE.PAL");

					token.ThrowIfCancellationRequested();
					if (pal != null) {
						cmp = await this.GetFileAsync<DfColormap>(this.path, $"SECBASE.CMP");

						token.ThrowIfCancellationRequested();
					}
				}
			} else {
				pal = this.lastPal;
				cmp = this.lastCmp;
			}
			if (this.lastPalette == null || this.lastPaletteFile != palFile || this.lastLightLevel != lightLevel) {
				if (cmp != null) {
					palette = cmp.ToByteArray(pal, lightLevel, false, false);
				} else if (pal != null) {
					palette = pal.ToByteArray();
				} else {
					palette = new byte[256 * 4];
					Array.Fill<byte>(palette, 255);
				}
			} else {
				palette = this.lastPalette;
			}

			this.lastPaletteFile = palFile;
			this.lastPal = pal;
			this.lastCmp = cmp;
			this.lastLightLevel = lightLevel;
			this.lastPalette = palette;

			return palette;
		}

		private string lastPaletteFile;
		private DfPalette lastPal;
		private DfColormap lastCmp;
		private int lastLightLevel = -1;
		private byte[] lastPalette;
		public byte[] Palette => this.lastPalette;
		public async Task RefreshPaletteAsync(int lightLevel, CancellationToken token) {
			byte[] palette = await this.GeneratePaletteAsync(lightLevel, token);

			int vertexColor = this.threeDo.Objects.SelectMany(x => x.Polygons).FirstOrDefault(x => x.ShadingMode == ShadingModes.Vertex)?.Color ?? -1;
			foreach (ThreeDoModelMesh mesh in this.transform.Cast<Transform>().Select(x => x.GetComponent<ThreeDoModelMesh>())) {
				Polygon polygon = mesh.Polygon;
				byte colorIndex = polygon.Color;
				ShadingModes mode = vertexColor < 0 ? polygon.ShadingMode : ShadingModes.Vertex;
				// Group polygons together by color and mode, color doesn't matter sometimes so pick a constant.
				switch (mode) {
					case ShadingModes.Plane:
						colorIndex = 255;
						break;
					case ShadingModes.Texture:
						colorIndex = 255;
						break;
					case ShadingModes.Vertex:
						// Vertices always have the first vertex color specified in the file.
						colorIndex = (byte)vertexColor;
						break;
				}

				await mesh.RefreshPaletteAsync(palette, colorIndex, token);
			}
		}

		public async Task RefreshTexturesAsync(Df3dObject.Object obj, CancellationToken token) {
			int vertexColor = this.threeDo.Objects.SelectMany(x => x.Polygons).FirstOrDefault(x => x.ShadingMode == ShadingModes.Vertex)?.Color ?? -1;
			foreach (ThreeDoModelMesh mesh in this.transform.Cast<Transform>().Select(x => x.GetComponent<ThreeDoModelMesh>())) {
				Polygon polygon = mesh.Polygon;
				byte colorIndex = polygon.Color;
				ShadingModes mode = vertexColor < 0 ? polygon.ShadingMode : ShadingModes.Vertex;
				// Group polygons together by color and mode, color doesn't matter sometimes so pick a constant.
				switch (mode) {
					case ShadingModes.Plane:
						colorIndex = 255;
						break;
					case ShadingModes.Texture:
						colorIndex = 255;
						break;
					case ShadingModes.Vertex:
						// Vertices always have the first vertex color specified in the file.
						colorIndex = (byte)vertexColor;
						break;
				}

				if (mesh.Object == obj) {
					await mesh.RefreshPaletteAsync(this.lastPalette, colorIndex, token);
				}
			}
		}
		public async Task RefreshTexturesAsync(Polygon selectedPolygon, CancellationToken token) {
			int vertexColor = this.threeDo.Objects.SelectMany(x => x.Polygons).FirstOrDefault(x => x.ShadingMode == ShadingModes.Vertex)?.Color ?? -1;
			foreach (ThreeDoModelMesh mesh in this.transform.Cast<Transform>().Select(x => x.GetComponent<ThreeDoModelMesh>())) {
				Polygon polygon = mesh.Polygon;
				byte colorIndex = polygon.Color;
				ShadingModes mode = vertexColor < 0 ? polygon.ShadingMode : ShadingModes.Vertex;
				// Group polygons together by color and mode, color doesn't matter sometimes so pick a constant.
				switch (mode) {
					case ShadingModes.Plane:
						colorIndex = 255;
						break;
					case ShadingModes.Texture:
						colorIndex = 255;
						break;
					case ShadingModes.Vertex:
						// Vertices always have the first vertex color specified in the file.
						colorIndex = (byte)vertexColor;
						break;
				}

				if (polygon == selectedPolygon) {
					await mesh.RefreshPaletteAsync(this.lastPalette, colorIndex, token);
				}
			}
		}
	}
}