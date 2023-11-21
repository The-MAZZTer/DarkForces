using MZZT.DarkForces.FileFormats;
using System.Linq;
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
	public class ThreeDoPolygonRenderer : MonoBehaviour {
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
		public Polygon[] Polygons { get; private set; }

		/// <summary>
		/// Apply texturing to the polygons.
		/// </summary>
		/// <param name="obj">The 3D object.</param>
		/// <param name="polygons">The polygons.</param>
		/// <param name="mode">The shading mode.</param>
		/// <param name="colorIndex">The color to apply.</param>
		/// <param name="lightLevel">The light level the object is in.</param>
		public async Task ApplyMaterialsAsync(Df3dObject.Object obj, Polygon[] polygons, ShadingModes mode,
			byte colorIndex, int lightLevel) {

			this.Object = obj;
			this.Polygons = polygons;

			if (lightLevel > 31) {
				lightLevel = 31;
			} else if (lightLevel < 0) {
				lightLevel = 0;
			}

			byte[] palette;
			ResourceCache cache = ResourceCache.Instance;
			LevelLoader levelLoader = LevelLoader.Instance;
			if (lightLevel == 31) {
				palette = cache.ImportPalette(levelLoader.Palette, true);
			} else {
				palette = cache.ImportColormap(levelLoader.Palette, levelLoader.ColorMap,
					lightLevel, true);
			}

			if (palette == null) {
				return;
			}

			// Get the color to use.
			Color color = new(palette[colorIndex * 4] / 255f, palette[colorIndex * 4 + 1] / 255f,
				palette[colorIndex * 4 + 2] / 255f, palette[colorIndex * 4 + 3] / 255f);

			if (mode == ShadingModes.Vertex) {
				// Create particles for the vertices.
				ParticleSystemRenderer renderer = this.GetComponent<ParticleSystemRenderer>();
				renderer.sharedMaterial = new Material(cache.ColorShader) {
					color = color
				};

				ParticleSystem particles = this.GetComponent<ParticleSystem>();
				const float geometryScale = LevelGeometryGenerator.GEOMETRY_SCALE;
				particles.SetParticles(polygons.SelectMany(x => x.Vertices.Select(x => new Particle() {
					position = new Vector3(
						x.X * geometryScale,
						-x.Y * geometryScale,
						x.Z * geometryScale
					),
					startSize = THREEDO_VERTEX_SCALE
				})).Distinct().ToArray());
			} else {
				Material material = null;
				switch (mode) {
					case ShadingModes.Flat:
						material = new Material(cache.ColorShader) {
							color = color
						};
						break;
					case ShadingModes.Gouraud:
						// TODO not sure how this is supposed to look?
						material = new Material(cache.ColorShader) {
							color = color
						};
						break;
					case ShadingModes.GourTex:
						// TODO not sure how this is supposed to look?
						if (!string.IsNullOrEmpty(obj.TextureFile)) {
							DfBitmap bm = await cache.GetBitmapAsync(obj.TextureFile);
							if (bm != null) {
								material = cache.GetMaterial(
									cache.ImportBitmap(bm.Pages[0], levelLoader.Palette,
										lightLevel >= 31 ? null : levelLoader.ColorMap, lightLevel),
									cache.SimpleShader);
							}
						}
						break;
					case ShadingModes.Plane:
					case ShadingModes.Texture: 
  					if (!string.IsNullOrEmpty(obj.TextureFile)) {
							DfBitmap bm = await cache.GetBitmapAsync(obj.TextureFile);
							if (bm != null) {
								material = cache.GetMaterial(
									cache.ImportBitmap(bm.Pages[0], levelLoader.Palette,
										lightLevel >= 31 ? null : levelLoader.ColorMap, lightLevel),
									cache.SimpleShader);
							}
						}
						break;
				}

				MeshRenderer renderer = this.GetComponent<MeshRenderer>();
				renderer.sharedMaterial = material;
			}
		}

		/// <summary>
		/// Set up rendering before cloning.
		/// </summary>
		/// <param name="obj">The 3D object.</param>
		/// <param name="polygons">The polygons.</param>
		public void Render(Df3dObject.Object obj, Polygon[] polygons) {
			this.Object = obj;
			this.Polygons = polygons;

			ShadingModes mode = polygons[0].ShadingMode;
			const float geometryScale = LevelGeometryGenerator.GEOMETRY_SCALE;
			Vector3[] vertices = polygons.SelectMany(x => x.Vertices.Select(x => new Vector3(
				x.X * geometryScale,
				-x.Y * geometryScale,
				x.Z * geometryScale
			))).ToArray();

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
			} else {
				int vertexCount = 0;
				Mesh mesh = new() {
					vertices = vertices,
					triangles = polygons.SelectMany(x => {
						int[] ret;
						// Adjust for QUAD or TRIANGLE 3DOs.
						if (x.Vertices.Count == 4) {
							ret = new[] { vertexCount, vertexCount + 1, vertexCount + 3, vertexCount + 1, vertexCount + 2, vertexCount + 3 };
							vertexCount += 4;
						} else {
							ret = new[] { vertexCount, vertexCount + 1, vertexCount + 2 };
							vertexCount += 3;
						}
						return ret;
					}).ToArray()
				};

				if (polygons[0].TextureVertices != null) {
					mesh.uv = polygons.SelectMany(x => x.TextureVertices.Select(x => x.ToUnity())).ToArray();
				} else {
					mesh.uv = Enumerable.Repeat(Vector2.zero, mesh.vertices.Length).ToArray();
				}

				mesh.Optimize();
				mesh.RecalculateNormals();

				MeshFilter filter = this.gameObject.AddComponent<MeshFilter>();
				filter.sharedMesh = mesh;

				this.gameObject.AddComponent<MeshRenderer>();

				this.gameObject.AddComponent<MeshCollider>();
			}
		}

		private void Update() {
			if (this.TryGetComponent<ParticleSystem>(out var particles)) {
				// Local particle simulation space broken in latest Unity...
				// Force redraw particles every frame otherwise rotation doesn't affect them.
				const float geometryScale = LevelGeometryGenerator.GEOMETRY_SCALE;
				particles.SetParticles(this.Polygons.SelectMany(x => x.Vertices.Select(x => new Particle() {
					position = new Vector3(
						x.X * geometryScale,
						-x.Y * geometryScale,
						x.Z * geometryScale
					),
					startSize = THREEDO_VERTEX_SCALE
				})).Distinct().ToArray());
			}
		}
	}
}
