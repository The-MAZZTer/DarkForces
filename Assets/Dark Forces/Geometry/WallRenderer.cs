using MZZT.DarkForces.FileFormats;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using static MZZT.DarkForces.FileFormats.DfLevel;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace MZZT.DarkForces {
	/// <summary>
	/// Create a mesh for a vertical wall.
	/// </summary>
	public class WallRenderer : MonoBehaviour {
		/// <summary>
		/// The wall to create a mesh for.
		/// </summary>
		public Wall Wall { get; private set; }

		/// <summary>
		/// Create the mesh.
		/// </summary>
		/// <param name="minY">Floor height</param>
		/// <param name="maxY">Ceiling height</param>
		/// <param name="surface">The wall information</param>
		/// <param name="wall">The wall</param>
		/// <param name="forcePlaneShader">Use the sky/pit shader.</param>
		/// <returns>The created GameObject with mesh.</returns>
		public static async Task<GameObject> CreateMeshAsync(float minY, float maxY, WallSurface surface,
			Wall wall, bool forcePlaneShader = false) {

			string textureFile = surface.TextureFile;
			if (wall.Sector.Flags.HasFlag(SectorFlags.DrawWallsAsSkyPit)) {
				// TODO this is not correct.
				// Should probably create a new shader to have sky texture on upper half of screen and pit on lower half
				// Potentially this material would also be applied to sky and pit surfaces in general.
				// If you get thrown into the sky, or into a pit, do the walls of the sky/pit
				// (past the normal ceiling/floor) only show the sky/pit texture, or both, same as this flag?
				textureFile = wall.Sector.Ceiling.TextureFile;
			}

			ResourceCache cache = ResourceCache.Instance;
			DfBitmap bm = null;
			if (!string.IsNullOrEmpty(textureFile)) {
				bm = await cache.GetBitmapAsync(textureFile);
			} else {
				bm = await cache.GetBitmapAsync("DEFAULT.BM");
			}

			bool transparent = surface == wall.MainTexture &&
				(wall.TextureAndMapFlags & WallTextureAndMapFlags.ShowTextureOnAdjoin) > 0 &&
				wall.Adjoined != null;
			bool usePlaneShader = forcePlaneShader || wall.Sector.Flags.HasFlag(SectorFlags.DrawWallsAsSkyPit);
			Shader shader;
			if (usePlaneShader) {
				shader = cache.PlaneShader;
			} else if (transparent) {
				shader = cache.TransparentShader;
			} else {
				shader = cache.SimpleShader;
			}

			int light = wall.Sector.LightLevel + wall.LightLevel;
			if (light < 0) {
				light = 0;
			} else if (light > 31) {
				light = 31;
			}

			LevelLoader levelLoader = LevelLoader.Instance;
			Material material = bm != null ? cache.GetMaterial(
				cache.ImportBitmap(bm.Pages[0], levelLoader.Palette, light >= 31 ? null : levelLoader.ColorMap,
					usePlaneShader ? 31 : light, transparent),
				shader) : null;
			if (usePlaneShader && material != null) {
				Parallaxer.Instance.AddMaterial(material);
			}

			Vector2 left = wall.LeftVertex.Position.ToUnity();
			Vector2 right = wall.RightVertex.Position.ToUnity();

			GameObject obj = new() {
				name = surface == wall.TopEdgeTexture ? "Top" :
					(surface == wall.BottomEdgeTexture ? "Bot" :
					(surface == wall.MainTexture ? "Mid" :
					"")),
				layer = LayerMask.NameToLayer("Geometry")
			};

			const float geometryScale = LevelGeometryGenerator.GEOMETRY_SCALE;
			// Position on the left vertex.
			// So local space of the left vertex at the floor is 0, 0, 0.
			obj.transform.position = new Vector3(
				left.x * geometryScale,
				-minY * geometryScale,
				left.y * geometryScale
			);

			// Determine the bounds of the wall.
			Vector3[] vertices = new Vector3[] {
				new Vector3(
					0,
					(-maxY + minY) * geometryScale,
					0
				),
				new Vector3(
					(right.x - left.x) * geometryScale,
					(-maxY + minY) * geometryScale,
					(right.y - left.y) * geometryScale
				),
				new Vector3(
					(right.x - left.x) * geometryScale,
					0,
					(right.y - left.y) * geometryScale
				),
				Vector3.zero
			};

			float width = Vector2.Distance(left, right);
			float height = minY - maxY;

			Mesh mesh = new() {
				vertices = vertices,
				triangles = new int[] { 0, 1, 3, 1, 2, 3 }
			};

			const float textureScale = LevelGeometryGenerator.TEXTURE_SCALE;
			if (material != null) {
				Vector2 offset = new(
					surface.TextureOffset.X / material.mainTexture.width / textureScale,
					surface.TextureOffset.Y / material.mainTexture.height / textureScale
				);
				// UVs of 0-1 will stretch the texture to fit.
				// Use the size of the mesh and texture to make each texture pixel a consistent size in the world.
				mesh.uv = new Vector2[] {
					new Vector2(
						offset.x,
						offset.y + height / textureScale / material.mainTexture.height
					),
					new Vector2(
						offset.x + width / textureScale / material.mainTexture.width,
						offset.y + height / textureScale / material.mainTexture.height
					),
					new Vector2(
						offset.x + width / textureScale / material.mainTexture.width,
						offset.y
					),
					offset
				};
			}
			
			WallTextureAndMapFlags flags = wall.TextureAndMapFlags;
			if ((flags & WallTextureAndMapFlags.FlipTextureHorizontally) > 0) {
				mesh.uv = mesh.uv.Select(x => new Vector2(-x.x, x.y)).ToArray();
			}

			mesh.Optimize();
			mesh.RecalculateNormals();

			MeshFilter filter = obj.AddComponent<MeshFilter>();
			filter.sharedMesh = mesh;

			MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
			renderer.sharedMaterial = material;

			obj.AddComponent<MeshCollider>();

			// If there's a sign texture, show it.
			// TODO Do the sign texture in a shader. This would allow it to be properly clipped if it is overlapping
			// edge of the wall. Here I am just making a second mesh for the sign.
			if (!usePlaneShader && wall.SignTexture.TextureFile != null) {
				bm = null;
				if (!string.IsNullOrEmpty(wall.SignTexture.TextureFile)) {
					bm = await cache.GetBitmapAsync(wall.SignTexture.TextureFile);
				}
				if (bm != null) {
					material = cache.GetMaterial(
						cache.ImportBitmap(bm.Pages[0], levelLoader.Palette,
							light >= 31 ? null : levelLoader.ColorMap, light),
						cache.TransparentShader);

					GameObject sign = new() {
						name = "SIGN",
						layer = LayerMask.NameToLayer("Geometry")
					};
					sign.transform.SetParent(obj.transform);

					Vector3 pos = new(
						0,
						-wall.SignTexture.TextureOffset.Y * geometryScale,
						0
					);
					Vector3 wallDirection = (vertices[1] - vertices[0]).normalized * geometryScale;
					pos += wallDirection * (-surface.TextureOffset.X + wall.SignTexture.TextureOffset.X);

					// Position the sign so it's slightly in front of the wall.
					Vector3 normalDirection = Quaternion.Euler(0, 90, 0) * wallDirection;
					pos += normalDirection * 0.001f;

					sign.transform.localPosition = pos;

					vertices = new Vector3[] {
						new Vector3(
							0,
							material.mainTexture.height * textureScale * geometryScale,
							0
						),
						new Vector3(
							material.mainTexture.width * textureScale * wallDirection.x,
							material.mainTexture.height * textureScale * geometryScale,
							material.mainTexture.width * textureScale * wallDirection.z
						),
						new Vector3(
							material.mainTexture.width * textureScale * wallDirection.x,
							0,
							material.mainTexture.width * textureScale * wallDirection.z
						),
						Vector3.zero
					};

					mesh = new Mesh {
						vertices = vertices,
						triangles = new int[] { 0, 1, 3, 1, 2, 3 },
						uv = new Vector2[] {
							new Vector2(0, 1),
							Vector2.one,
							new Vector2(1, 0),
							Vector2.zero
						}
					};

					mesh.Optimize();
					mesh.RecalculateNormals();

					filter = sign.AddComponent<MeshFilter>();
					filter.sharedMesh = mesh;

					renderer = sign.AddComponent<MeshRenderer>();
					renderer.sharedMaterial = material;
				}
			}
			return obj;
		}

		/// <summary>
		/// Generate a wall.
		/// </summary>
		/// <param name="wall">The wall.</param>
		public async Task RenderAsync(Wall wall) {
			this.Wall = wall;

			Sector sector = wall.Sector;
			// If the height of the sector is 0, don't bother creating any walls.
			if (sector.Floor.Y <= sector.Ceiling.Y || (sector.Flags & SectorFlags.SectorIsDoor) > 0) {
				return;
			}
			/*if ((sector.Flags & SectorFlags.DrawWallsAsSkyPit) > 0) {
				return;
			}*/

			float minY = sector.Floor.Y;
			float maxY = sector.Ceiling.Y;

			float adjoinedMinY = minY;
			float adjoinedMaxY = maxY;
			// Adjoined walls aren't visible (usually), but we still want to render top and bottom edges.
			if (wall.Adjoined != null) {
				adjoinedMinY = wall.Adjoined.Sector.Floor.Y;
				adjoinedMaxY = wall.Adjoined.Sector.Ceiling.Y;
				// If the adjoined sector is a door then we should adjust the height of the edges.
				if ((wall.Adjoined.Sector.Flags & SectorFlags.SectorIsDoor) > 0) {
					adjoinedMaxY = adjoinedMinY;
				}
				if (adjoinedMaxY > maxY && (!sector.Flags.HasFlag(SectorFlags.CeilingIsSky) ||
					!wall.Adjoined.Sector.Flags.HasFlag(SectorFlags.AdjoinAdjacentSkies | SectorFlags.CeilingIsSky))) {

					GameObject obj = await CreateMeshAsync(adjoinedMaxY, maxY, wall.TopEdgeTexture, wall);
					obj.transform.SetParent(this.transform, true);
				}
				if (adjoinedMinY < minY && (!sector.Flags.HasFlag( SectorFlags.FloorIsPit) ||
					!wall.Adjoined.Sector.Flags.HasFlag(SectorFlags.AdjoinAdjacentPits | SectorFlags.FloorIsPit))) {

					GameObject obj = await CreateMeshAsync(minY, adjoinedMinY, wall.BottomEdgeTexture, wall);
					obj.transform.SetParent(this.transform, true);
				}
			}

			if (wall.Adjoined == null || (wall.TextureAndMapFlags & WallTextureAndMapFlags.ShowTextureOnAdjoin) > 0) {
				GameObject obj = await CreateMeshAsync(adjoinedMinY, adjoinedMaxY, wall.MainTexture, wall);
				obj.transform.SetParent(this.transform, true);
			}
		}
	}
}
