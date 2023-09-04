using UnityEngine;
using UnityEngine.InputSystem;

namespace MZZT.DarkForces.Showcase {
	[RequireComponent(typeof(Camera))]
	public class OrbitCamera : MonoBehaviour {
		[SerializeField]
		private Vector2 lookSensitivity = Vector2.one;
		[SerializeField]
		private float zoomSensitivity = -1;
		[SerializeField]
		private Vector2 yAngleClamp = new(-60, 60);
		[SerializeField]
		private bool invertY = true;

		private bool pointerDown;
		private bool pointerOver;

		private Vector2 lookDelta;
		public void OnLook(InputAction.CallbackContext context) {
			if (!this.pointerDown) {
				return;
			}

			this.lookDelta = context.ReadValue<Vector2>();
		}

		private float zoomDelta;
		public void OnZoom(InputAction.CallbackContext context) {
			if (!this.pointerDown && !this.pointerOver) {
				return;
			}

			this.zoomDelta = context.ReadValue<Vector2>().y;
		}

		private void Look(Vector2 value) {
			value.y *= this.invertY ? -1 : 1;
			this.angles += this.lookSensitivity * Time.deltaTime * new Vector2(value.y, value.x);

			this.angles.x =	Mathf.Clamp(this.angles.x, this.yAngleClamp.x, this.yAngleClamp.y);

			if (this.angles.y < 0f) {
				this.angles.y += 360f;
			} else if (this.angles.y >= 360f) {
				this.angles.y -= 360f;
			}
		}

		private void Zoom(float value) {
			this.distance += this.zoomSensitivity * Time.deltaTime * value;

			if (this.distance < 0.1f) {
				this.distance = 0.1f;
			}
		}

		private Vector3 focus;
		private float distance;
		private Vector2 angles = new(45, 0);
		public void Set(Vector3 focus, float distance) {
			this.focus = focus;
			this.angles = new Vector2(45, 0);
			this.distance = distance;

			this.UpdateCanera();
		}

		public void Set(Vector3 focus) {
			this.focus = focus;

			this.UpdateCanera();
		}

		public void Activate() {
			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Confined;

			this.pointerDown = true;
		}

		public void OnPointerEnter() {
			this.pointerOver = true;
		}

		public void OnPointerExit() {
			this.pointerOver = false;
		}

		private void UpdateCanera() {
			Quaternion rotation = Quaternion.Euler(this.angles);
			this.transform.SetPositionAndRotation(this.focus - (rotation * Vector3.forward) * this.distance, rotation);
		}

		private void Update() {
			if (this.lookDelta != Vector2.zero) {
				this.Look(this.lookDelta);
				this.lookDelta = Vector2.zero;
			}
			if (this.zoomDelta != 0) {
				this.Zoom(this.zoomDelta);
				this.zoomDelta = 0;
			}

			this.UpdateCanera();

			if (this.pointerDown && !PlayerInput.all[0].currentActionMap.FindAction("Click").IsPressed()) {
				this.pointerDown = false;

				Cursor.visible = true;
				Cursor.lockState = CursorLockMode.None;
			}
		}
	}
}