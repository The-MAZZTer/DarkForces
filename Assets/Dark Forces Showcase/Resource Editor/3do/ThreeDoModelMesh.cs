using MZZT.DarkForces.Converters;
using MZZT.DarkForces.FileFormats;
using MZZT.DarkForces.Showcase;
using MZZT.FileFormats;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using static MZZT.DarkForces.FileFormats.Df3dObject;
using static UnityEngine.ParticleSystem;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace MZZT.DarkForces {
	/// <summary>
	/// Render a subset of a 3D object.
	/// </summary>
	public class ThreeDoModelMesh : MonoBehaviour {
		/// <summary>
		/// How big a vertex particle should be. Interestingly this is not based on distance, and so lines up with how Dark Forces did it (sort of).
		/// </summary>
		public const float THREEDO_VERTEX_SCALE = 0.002f;

		/// <summary>
		/// The 3D object.
		/// </summary>
		public Df3dObject.Object Object { get; private set; }
		/// <summary>
		/// The polygons from the 3D object we are taking care of.
		/// </summary>
		public Polygon Polygon { get; private set; }
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
		private Shader colorShader;
		private Shader simpleShader;
		private DfBitmap lastBitmap;
		private string lastTextureFile;

		/// <summary>
		/// Set up rendering.
		/// </summary>
		/// <param name="obj">The 3D object.</param>
		/// <param name="polygons">The polygons.</param>
		public async Task RenderAsync(string filePath, Df3dObject threeDo, Df3dObject.Object obj, Polygon polygon, byte colorIndex, byte[] palette, Shader colorShader, Shader simpleShader, CancellationToken token) {
			this.path = filePath;
			this.Object = obj;
			this.Polygon = polygon;
			this.colorShader = colorShader;
			this.simpleShader = simpleShader;

			ShadingModes mode = polygon.ShadingMode;
			Vector3[] vertices = polygon.Vertices.Select(x => new Vector3(
				x.X * LevelGeometryGenerator.GEOMETRY_SCALE,
				-x.Y * LevelGeometryGenerator.GEOMETRY_SCALE,
				x.Z * LevelGeometryGenerator.GEOMETRY_SCALE
			)).ToArray();

			// Get the color to use.
			Color color = new(palette[colorIndex * 4] / 255f, palette[colorIndex * 4 + 1] / 255f,
				palette[colorIndex * 4 + 2] / 255f, palette[colorIndex * 4 + 3] / 255f);

			if (mode == ShadingModes.Vertex) {
				// Configure a particle system.
				ParticleSystem particles = this.gameObject.AddComponent<ParticleSystem>();

				CollisionModule collision = particles.collision;
				collision.enabled = false;

				ColorBySpeedModule colorBySpeed = particles.colorBySpeed;
				colorBySpeed.enabled = false;

				ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
				colorOverLifetime.enabled = false;

				CustomDataModule customData = particles.customData;
				customData.enabled = false;

				EmissionModule emission = particles.emission;
				emission.enabled = false;

				ExternalForcesModule externalForces = particles.externalForces;
				externalForces.enabled = false;

				ForceOverLifetimeModule forceOverLifetime = particles.forceOverLifetime;
				forceOverLifetime.enabled = false;

				InheritVelocityModule inheritVelocity = particles.inheritVelocity;
				inheritVelocity.enabled = false;

				LifetimeByEmitterSpeedModule lifetimeByEmitterSpeed = particles.lifetimeByEmitterSpeed;
				lifetimeByEmitterSpeed.enabled = false;

				LightsModule lights = particles.lights;
				lights.enabled = false;

				LimitVelocityOverLifetimeModule limitVelocityOverLifetime = particles.limitVelocityOverLifetime;
				limitVelocityOverLifetime.enabled = false;

				MainModule main = particles.main;
				main.loop = false;
				main.maxParticles = vertices.Length;
				main.playOnAwake = false;
				main.simulationSpace = ParticleSystemSimulationSpace.Local;

				NoiseModule noise = particles.noise;
				noise.enabled = false;

				RotationBySpeedModule rotationBySpeed = particles.rotationBySpeed;
				rotationBySpeed.enabled = false;

				RotationOverLifetimeModule rotationOverLifetime = particles.rotationOverLifetime;
				rotationOverLifetime.enabled = false;

				ShapeModule shape = particles.shape;
				shape.enabled = false;

				SizeBySpeedModule sizeBySpeed = particles.sizeBySpeed;
				sizeBySpeed.enabled = false;

				SubEmittersModule subEmitters = particles.subEmitters;
				subEmitters.enabled = false;

				TextureSheetAnimationModule textureSheetAnimation = particles.textureSheetAnimation;
				textureSheetAnimation.enabled = false;

				TrailModule trails = particles.trails;
				trails.enabled = false;

				TriggerModule trigger = particles.trigger;
				trigger.enabled = false;

				VelocityOverLifetimeModule velocityOverLifetime = particles.velocityOverLifetime;
				velocityOverLifetime.enabled = false;

				particles.SetParticles(polygon.Vertices.Select(x => new Particle() {
					position = new Vector3(
						x.X * LevelGeometryGenerator.GEOMETRY_SCALE,
						-x.Y * LevelGeometryGenerator.GEOMETRY_SCALE,
						x.Z * LevelGeometryGenerator.GEOMETRY_SCALE
					),
					startSize = THREEDO_VERTEX_SCALE
				}).Distinct().ToArray());

				ParticleSystemRenderer renderer = this.GetComponent<ParticleSystemRenderer>();
				renderer.alignment = ParticleSystemRenderSpace.View;
				renderer.cameraVelocityScale = 0;
				renderer.enabled = true;
				renderer.enableGPUInstancing = true;
				renderer.maxParticleSize = THREEDO_VERTEX_SCALE;
				renderer.minParticleSize = THREEDO_VERTEX_SCALE;
				renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
				renderer.receiveShadows = false;
				renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
				renderer.renderMode = ParticleSystemRenderMode.Billboard;
				renderer.shadowCastingMode = ShadowCastingMode.Off;
				renderer.sortMode = ParticleSystemSortMode.Distance;
#if UNITY_2021_1_OR_NEWER
				renderer.staticShadowCaster = false;
#endif
				// Create particles for the vertices.
				renderer.sharedMaterial = new Material(colorShader) {
					color = color
				};
			} else {
				int[] triangles;
				// Adjust for QUAD or TRIANGLE 3DOs.
				if (polygon.Vertices.Count == 4) {
					triangles = new[] { 0, 1, 3, 1, 2, 3 };
				} else {
					triangles = new[] { 0, 1, 2 };
				}

				Mesh mesh = new() {
					vertices = vertices,
					triangles = triangles
				};

				if (polygon.TextureVertices != null) {
					mesh.uv = polygon.TextureVertices.Select(x => x.ToUnity()).ToArray();
				} else {
					mesh.uv = Enumerable.Repeat(Vector2.zero, mesh.vertices.Length).ToArray();
				}

				mesh.Optimize();
				mesh.RecalculateNormals();

				MeshFilter filter = this.gameObject.AddComponent<MeshFilter>();
				filter.sharedMesh = mesh;

				MeshRenderer renderer = this.gameObject.AddComponent<MeshRenderer>();

				Material material = null;
				switch (mode) {
					case ShadingModes.Flat:
						material = new Material(colorShader) {
							color = color
						};
						break;
					case ShadingModes.Gouraud:
						// TODO not sure how this is supposed to look?
						material = new Material(colorShader) {
							color = color
						};
						break;
					case ShadingModes.GourTex:
						// TODO not sure how this is supposed to look?
						if (!string.IsNullOrEmpty(obj.TextureFile)) {
							DfBitmap bm;
							if (this.lastTextureFile != obj.TextureFile) {
								bm = await this.GetFileAsync<DfBitmap>(filePath, obj.TextureFile);

								token.ThrowIfCancellationRequested();

								this.lastBitmap = bm;
								this.lastTextureFile = obj.TextureFile;
							} else {
								bm = this.lastBitmap;
							}
							if (bm != null) {
								material = new Material(simpleShader) {
									mainTexture = bm.Pages[0].ToTexture(palette, false)
								};
							}
						}
						break;
					case ShadingModes.Plane:
					case ShadingModes.Texture:
						if (!string.IsNullOrEmpty(obj.TextureFile)) {
							DfBitmap bm;
							if (this.lastTextureFile != obj.TextureFile) {
								bm = await this.GetFileAsync<DfBitmap>(filePath, obj.TextureFile);

								token.ThrowIfCancellationRequested();

								this.lastBitmap = bm;
								this.lastTextureFile = obj.TextureFile;
							} else {
								bm = this.lastBitmap;
							}
							if (bm != null) {
								material = new Material(simpleShader) {
									mainTexture = bm.Pages[0].ToTexture(palette, false)
								};
							}
						}
						break;
				}

				renderer.sharedMaterial = material;
			}
		}

		public async Task RefreshPaletteAsync(byte[] palette, int colorIndex, CancellationToken token) {
			ShadingModes mode = this.Polygon.ShadingMode;

			// Get the color to use.
			Color color = new(palette[colorIndex * 4] / 255f, palette[colorIndex * 4 + 1] / 255f,
				palette[colorIndex * 4 + 2] / 255f, palette[colorIndex * 4 + 3] / 255f);

			if (mode == ShadingModes.Vertex) {
				// Configure a particle system.
				ParticleSystemRenderer renderer = this.GetComponent<ParticleSystemRenderer>();
				// Create particles for the vertices.
				renderer.sharedMaterial = new Material(this.colorShader) {
					color = color
				};
			} else {
				MeshRenderer renderer = this.gameObject.GetComponent<MeshRenderer>();

				Material material = null;
				string file = this.Object.TextureFile;

				switch (mode) {
					case ShadingModes.Flat:
						material = new Material(colorShader) {
							color = color
						};
						break;
					case ShadingModes.Gouraud:
						// TODO not sure how this is supposed to look?
						material = new Material(colorShader) {
							color = color
						};
						break;
					case ShadingModes.GourTex:
						// TODO not sure how this is supposed to look?
						if (!string.IsNullOrEmpty(file)) {
							DfBitmap bm;
							if (this.lastTextureFile != file) {
								bm = await this.GetFileAsync<DfBitmap>(this.path, file);

								token.ThrowIfCancellationRequested();

								this.lastBitmap = bm;
								this.lastTextureFile = file;
							} else {
								bm = this.lastBitmap;
							}
							if (bm != null) {
								material = new Material(simpleShader) {
									mainTexture = bm.Pages[0].ToTexture(palette, false)
								};
							}
						}
						break;
					case ShadingModes.Plane:
					case ShadingModes.Texture:
						if (!string.IsNullOrEmpty(file)) {
							DfBitmap bm;
							if (this.lastTextureFile != file) {
								bm = await this.GetFileAsync<DfBitmap>(this.path, file);

								token.ThrowIfCancellationRequested();

								this.lastBitmap = bm;
								this.lastTextureFile = file;
							} else {
								bm = this.lastBitmap;
							}
							if (bm != null) {
								material = new Material(simpleShader) {
									mainTexture = bm.Pages[0].ToTexture(palette, false)
								};
							}
						}
						break;
				}
				renderer.sharedMaterial = material;
			}
		}

		private void Update() {
			if (this.TryGetComponent(out ParticleSystem particles)) {
				// Local particle simulation space broken in latest Unity...
				// Force redraw particles every frame otherwise rotation doesn't affect them.
				particles.SetParticles(this.Polygon.Vertices.Select(x => new Particle() {
					position = new Vector3(
						x.X * LevelGeometryGenerator.GEOMETRY_SCALE,
						-x.Y * LevelGeometryGenerator.GEOMETRY_SCALE,
						x.Z * LevelGeometryGenerator.GEOMETRY_SCALE
					),
					startSize = THREEDO_VERTEX_SCALE
				}).Distinct().ToArray());
			}
		}
	}
}
