using MZZT.DarkForces.FileFormats;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace MZZT.DarkForces {
	/// <summary>
	/// Render a WAX in Unity.
	/// </summary>
	public class WaxRenderer : ObjectRenderer {
		public const float WAX_SCALE = 1 / 65536f; // ResourceCache.SPRITE_PIXELS_PER_RENDERER will scale as well.

		public override async Task RenderAsync(DfLevelObjects.Object obj) {
			await base.RenderAsync(obj);

			int lightLevel;
			if (this.CurrentSector != null) {
				lightLevel = this.CurrentSector.Sector.LightLevel;
			} else {
				lightLevel = 31;
			}

			DfWax wax = await ResourceCache.Instance.GetWaxAsync(obj.FileName);
			if (wax == null) {
				return;
			}

			// Only using WAX state 0 for this showcase.

			this.gameObject.transform.localScale = new Vector3(
				WAX_SCALE * wax.Waxes[0].WorldWidth,
				WAX_SCALE * wax.Waxes[0].WorldHeight,
				WAX_SCALE * wax.Waxes[0].WorldWidth
			);

			// Only use frame 0 of the animation.
			// TODO Make it actually animate over time (though I think WAX 0 has only one frame per sequence usually).

			this.frames = wax.Waxes[0].Sequences.Select(x => x.Frames[0]).ToArray();

			this.sprites = this.frames.Select(x => ResourceCache.Instance.ImportFrame(LevelLoader.Instance.Palette,
				lightLevel >= 31 ? null : LevelLoader.Instance.ColorMap, x, lightLevel)).ToArray();

			SpriteRenderer renderer = this.gameObject.AddComponent<SpriteRenderer>();
			renderer.color = Color.white;
			renderer.drawMode = SpriteDrawMode.Simple;

			this.UpdateSprite();
		}

		private void UpdateSprite() {
			// Get the proper sequence for the angle the camera can see.
			SpriteRenderer renderer = this.GetComponent<SpriteRenderer>();

			Vector3 euler = this.Object.EulerAngles.ToUnity();
			euler = new Vector3(-euler.x, euler.y, euler.z);
			Vector3 forward = Quaternion.Euler(euler) * Vector3.forward;
			Vector3 camera = -Camera.main.transform.forward;
			camera = new Vector3(camera.x, 0, camera.z);
			float angle = Vector3.SignedAngle(camera, forward, Vector3.up);
			int index = (int)((-angle + 360 + 5.625) % 360 / 11.25f);
			//renderer.flipX = this.frames[index].Flip;
			renderer.sprite = this.sprites[index];
		}

		private DfFrame[] frames;
		private Sprite[] sprites;

		private void Update() {
			if (this.frames == null) {
				return;
			}

			// Make the sprite face the camera all the time.
			Vector3 face = -Camera.main.transform.forward;
			face.y = 0;
			this.transform.rotation = Quaternion.LookRotation(face, Vector3.up);

			this.UpdateSprite();
		}
	}
}
