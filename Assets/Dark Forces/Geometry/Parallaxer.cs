using System.Collections.Generic;
using UnityEngine;

namespace MZZT.DarkForces {
	/// <summary>
	/// Handles the parallax effect on sky/pit materials.
	/// </summary>
	public class Parallaxer : Singleton<Parallaxer> {
		private readonly static Vector2 PARALLAX_SCALE = new Vector2(1, -1);

		/// <summary>
		/// The parallax to apply in pixels per degree.
		/// </summary>
		public Vector2 Parallax { get; set; }

		/// <summary>
		/// Remove all materials.
		/// </summary>
		public void Reset() {
			this.materials.Clear();
		}

		private readonly HashSet<Material> materials = new HashSet<Material>();
		/// <summary>
		/// Add a material to apply parallax to.
		/// </summary>
		/// <param name="material">The material to add.</param>
		public void AddMaterial(Material material) {
			this.materials.Add(material);
		}

		private void Update() {
			Camera camera = Camera.main;
			Vector4 screenSize = new Vector4(Display.main.renderingWidth, Display.main.renderingHeight);
			foreach (Material material in materials) {
				Vector4 parallax = new Vector4(
					camera.transform.eulerAngles.y / 360 * this.Parallax.x / material.mainTexture.width * PARALLAX_SCALE.x,
					camera.transform.eulerAngles.x / 360 * this.Parallax.y / material.mainTexture.height * PARALLAX_SCALE.y
				);

				material.SetVector("_Parallax", parallax);
				material.SetVector("_ScreenSize", screenSize);
			}
		}
	}
}
