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

			DfBitmap bm = null;
			if (!string.IsNullOrEmpty(surface.TextureFile)) {
				bm = await ResourceCache.Instance.GetBitmapAsync(surface.TextureFile);
			} else {
				bm = await ResourceCache.Instance.GetBitmapAsync("DEFAULT.BM");
			}

			bool transparent = surface == wall.MainTexture &&
				(wall.TextureAndMapFlags & WallTextureAndMapFlags.ShowTextureOnAdjoin) > 0 &&
				wall.Adjoined != null;
			bool usePlaneShader = forcePlaneShader || wall.Sector.Flags.HasFlag(SectorFlags.DrawWallsAsSkyPit);
			Shader shader;
			if (usePlaneShader) {
				shader = ResourceCache.Instance.PlaneShader;
			} else if (transparent) {
				shader = ResourceCache.Instance.TransparentShader;
			} else {
				shader = ResourceCache.Instance.SimpleShader;
			}

			Material material = bm != null ? ResourceCache.Instance.GetMaterial(
				ResourceCache.Instance.ImportBitmap(bm, LevelLoader.Instance.Palette, LevelLoader.Instance.ColorMap,
					usePlaneShader ? 31 : wall.Sector.LightLevel, transparent),
				shader) : null;
			if (usePlaneShader && material != null) {
				Parallaxer.Instance.AddMaterial(material);
			}

			Vector2 left = wall.LeftVertex.Position.ToUnity();
			Vector2 right = wall.RightVertex.Position.ToUnity();

			GameObject obj = new GameObject {
				name = surface == wall.TopEdgeTexture ? "Top" :
					(surface == wall.BottomEdgeTexture ? "Bot" :
					(surface == wall.MainTexture ? "Mid" :
					"")),
				layer = LayerMask.NameToLayer("Geometry")
			};

			// Position on the left vertex.
			// So local space of the left vertex at the floor is 0, 0, 0.
			obj.transform.position = new Vector3(
				left.x * LevelGeometryGenerator.GEOMETRY_SCALE,
				-minY * LevelGeometryGenerator.GEOMETRY_SCALE,
				left.y * LevelGeometryGenerator.GEOMETRY_SCALE
			);

			// Determine the bounds of the wall.
			Vector3[] vertices = new Vector3[] {
				new Vector3(
					0,
					(-maxY + minY) * LevelGeometryGenerator.GEOMETRY_SCALE,
					0
				),
				new Vector3(
					(right.x - left.x) * LevelGeometryGenerator.GEOMETRY_SCALE,
					(-maxY + minY) * LevelGeometryGenerator.GEOMETRY_SCALE,
					(right.y - left.y) * LevelGeometryGenerator.GEOMETRY_SCALE
				),
				new Vector3(
					(right.x - left.x) * LevelGeometryGenerator.GEOMETRY_SCALE,
					0,
					(right.y - left.y) * LevelGeometryGenerator.GEOMETRY_SCALE
				),
				Vector3.zero
			};

			float width = Vector2.Distance(left, right);
			float height = minY - maxY;

			Mesh mesh = new Mesh {
				vertices = vertices,
				triangles = new int[] { 0, 1, 3, 1, 2, 3 }
			};

			if (material != null) {
				Vector2 offset = new Vector2(
					surface.TextureOffset.X / material.mainTexture.width / LevelGeometryGenerator.TEXTURE_SCALE,
					surface.TextureOffset.Y / material.mainTexture.height / LevelGeometryGenerator.TEXTURE_SCALE
				);
				// UVs of 0-1 will stretch the texture to fit.
				// Use the size of the mesh and texture to make each texture pixel a consistent size in the world.
				mesh.uv = new Vector2[] {
					new Vector2(
						offset.x,
						offset.y + height / LevelGeometryGenerator.TEXTURE_SCALE / material.mainTexture.height
					),
					new Vector2(
						offset.x + width / LevelGeometryGenerator.TEXTURE_SCALE / material.mainTexture.width,
						offset.y + height / LevelGeometryGenerator.TEXTURE_SCALE / material.mainTexture.height
					),
					new Vector2(
						offset.x + width / LevelGeometryGenerator.TEXTURE_SCALE / material.mainTexture.width,
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
					bm = await ResourceCache.Instance.GetBitmapAsync(wall.SignTexture.TextureFile);
				}
				if (bm != null) {
					material = ResourceCache.Instance.GetMaterial(
						ResourceCache.Instance.ImportBitmap(bm, LevelLoader.Instance.Palette,
							LevelLoader.Instance.ColorMap, wall.Sector.LightLevel),
						ResourceCache.Instance.TransparentShader);

					GameObject sign = new GameObject() {
						name = "SIGN",
						layer = LayerMask.NameToLayer("Geometry")
					};
					sign.transform.SetParent(obj.transform);

					Vector3 pos = new Vector3(
						0,
						-wall.SignTexture.TextureOffset.Y * LevelGeometryGenerator.GEOMETRY_SCALE,
						0
					);
					Vector3 wallDirection = (vertices[1] - vertices[0]).normalized * LevelGeometryGenerator.GEOMETRY_SCALE;
					pos += wallDirection * (-surface.TextureOffset.X + wall.SignTexture.TextureOffset.X);

					// Position the sign so it's slightly in front of the wall.
					Vector3 normalDirection = Quaternion.Euler(0, 90, 0) * wallDirection;
					pos += normalDirection * 0.001f;

					sign.transform.localPosition = pos;

					vertices = new Vector3[] {
						new Vector3(
							0,
							material.mainTexture.height * LevelGeometryGenerator.TEXTURE_SCALE * LevelGeometryGenerator.GEOMETRY_SCALE,
							0
						),
						new Vector3(
							material.mainTexture.width * LevelGeometryGenerator.TEXTURE_SCALE * wallDirection.x,
							material.mainTexture.height * LevelGeometryGenerator.TEXTURE_SCALE * LevelGeometryGenerator.GEOMETRY_SCALE,
							material.mainTexture.width * LevelGeometryGenerator.TEXTURE_SCALE * wallDirection.z
						),
						new Vector3(
							material.mainTexture.width * LevelGeometryGenerator.TEXTURE_SCALE * wallDirection.x,
							0,
							material.mainTexture.width * LevelGeometryGenerator.TEXTURE_SCALE * wallDirection.z
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
				if (adjoinedMaxY > maxY && (!sector.Flags.HasFlag(SectorFlags.AdjoinAdjacentSkies | SectorFlags.CeilingIsSky) ||
					!wall.Adjoined.Sector.Flags.HasFlag(SectorFlags.CeilingIsSky))) {

					GameObject obj = await CreateMeshAsync(adjoinedMaxY, maxY, wall.TopEdgeTexture, wall);
					obj.transform.SetParent(this.transform, true);
				}
				if (adjoinedMinY < minY && (!sector.Flags.HasFlag(SectorFlags.AdjoinAdjacentPits | SectorFlags.FloorIsPit) ||
					!wall.Adjoined.Sector.Flags.HasFlag(SectorFlags.FloorIsPit))) {

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
