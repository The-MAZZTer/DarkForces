using UnityEngine;

namespace MZZT {
	public static class Texture2DExtensions {
		public static Sprite ToSprite(this Texture2D texture) =>
			Sprite.Create(texture,
				new Rect(0, 0, texture.width, texture.height),
				new Vector2(0.5f, 0.5f));
	}
}
