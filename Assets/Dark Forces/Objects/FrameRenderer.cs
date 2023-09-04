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

			DfFrame fme = await ResourceCache.Instance.GetFrameAsync(obj.FileName);
			if (fme == null) {
				return;
			}

			Sprite sprite = ResourceCache.Instance.ImportFrame(LevelLoader.Instance.Palette,
				lightLevel >= 31 ? null : LevelLoader.Instance.ColorMap, fme, lightLevel);

			SpriteRenderer renderer = this.gameObject.AddComponent<SpriteRenderer>();
			renderer.color = Color.white;
			renderer.drawMode = SpriteDrawMode.Simple;
			//renderer.flipX = fme.Flip;
			renderer.sprite = sprite;
		}

		private void Update() {
			// Make the sprite face the camera all the time.
			Vector3 face = -Camera.main.transform.forward;
			face.y = 0;
			this.transform.rotation = Quaternion.LookRotation(face, Vector3.up);
		}
	}
}
