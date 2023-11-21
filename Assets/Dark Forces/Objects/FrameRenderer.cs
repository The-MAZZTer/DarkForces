using MZZT.DarkForces.FileFormats;
using System.Threading.Tasks;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace MZZT.DarkForces {
	/// <summary>
	/// Render a FME in Unity.
	/// </summary>
	public class FrameRenderer : ObjectRenderer {
		public const float FRAME_SCALE = 1f; // ResourceCache.SPRITE_PIXELS_PER_RENDERER will scale instead.

		public override async Task RenderAsync(DfLevelObjects.Object obj) {
			await base.RenderAsync(obj);

			int lightLevel;
			if (this.CurrentSector != null) {
				lightLevel = this.CurrentSector.Sector.LightLevel;
			} else {
				lightLevel = 31;
			}

			this.gameObject.transform.localScale = new Vector3(
				FRAME_SCALE,
				FRAME_SCALE,
				FRAME_SCALE
			);

			ResourceCache cache = ResourceCache.Instance;
			DfFrame fme = await cache.GetFrameAsync(obj.FileName);
			if (fme == null) {
				return;
			}

			LevelLoader levelLoader = LevelLoader.Instance;
			Sprite sprite = cache.ImportFrame(levelLoader.Palette,
				lightLevel >= 31 ? null : levelLoader.ColorMap, fme, lightLevel);

			SpriteRenderer renderer = this.gameObject.AddComponent<SpriteRenderer>();
			renderer.color = Color.white;
			renderer.drawMode = SpriteDrawMode.Simple;
			//renderer.flipX = fme.Flip;
			renderer.sprite = sprite;

			CapsuleCollider collider = this.gameObject.AddComponent<CapsuleCollider>();
			collider.height = renderer.bounds.size.y;
			collider.radius = Mathf.Max(renderer.bounds.size.x, renderer.bounds.size.z) / 4;
			collider.center = this.transform.InverseTransformPoint(renderer.bounds.center);
		}

		private void Update() {
			// Make the sprite face the camera all the time.
			Vector3 face = -Camera.main.transform.forward;
			face.y = 0;
			this.transform.rotation = Quaternion.LookRotation(face, Vector3.up);
		}
	}
}
