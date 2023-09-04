using UnityEngine;

namespace MZZT.UI {
	[RequireComponent(typeof(RectTransform))]
	public class CameraViewport : MonoBehaviour {
		[SerializeField]
		private Camera target;

		private bool hasCanvas;
		private Canvas canvas;

		private void OnEnable() {
			this.target.enabled = true;
		}

		private void OnDisable() {
			this.target.enabled = false;
		}

		private Vector3 lastPos;
		private Rect lastRect;
		private void Update() {
			RectTransform transform = (RectTransform)this.transform;
			if (!this.hasCanvas) {
				this.canvas = this.GetComponentInParent<Canvas>(true).rootCanvas;
				this.hasCanvas = true;
			} else {
				if (transform.position == this.lastPos && transform.rect == this.lastRect) {
					return;
				}
			}
			this.lastPos = transform.position;
			this.lastRect = transform.rect;

			Camera camera = this.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : this.canvas.worldCamera;
			Vector2 min = transform.TransformPoint(this.lastRect.min);
			Vector2 max = transform.TransformPoint(this.lastRect.max);
			int displayIndex = this.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? this.canvas.targetDisplay : (int)camera.WorldToScreenPoint(min).z;
			
			min = RectTransformUtility.WorldToScreenPoint(camera, min);
			max = RectTransformUtility.WorldToScreenPoint(camera, max);
			Display display = Display.displays[displayIndex];
			Vector2 displaySize = new(display.renderingWidth, display.renderingHeight);
			this.target.rect = new(min.x / displaySize.x, min.y / displaySize.y, (max.x - min.x) / displaySize.x, (max.y - min.y) / displaySize.y);
		}
	}
}
