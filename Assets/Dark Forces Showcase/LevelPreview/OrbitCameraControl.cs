using UnityEngine;
using UnityEngine.InputSystem;

namespace MZZT {
	public class OrbitCameraControl : MonoBehaviour {
		[SerializeField]
		private Vector2 lookSensitivity = Vector2.one;
		public Vector2 LookSensitivity {
			get => this.lookSensitivity;
			set => this.lookSensitivity = value;
		}
		[SerializeField]
		private Vector2 moveSensitivity = Vector2.one;
		public Vector2 MoveSensitivity {
			get => this.moveSensitivity;
			set => this.moveSensitivity = value;
		}
		[SerializeField]
		private float runningMultiplier = 2;
		public float RunningMultiplier {
			get => this.runningMultiplier;
			set => this.runningMultiplier = value;
		}
		[SerializeField]
		private float upDownSensitivity = 1;
		public float UpDownSensitivity {
			get => this.upDownSensitivity;
			set => this.upDownSensitivity = value;
		}
		[SerializeField]
		private float zoomSensitivity = 1;
		public float ZoomSensitivity {
			get => this.zoomSensitivity;
			set => this.zoomSensitivity = value;
		}
		[SerializeField]
		private Vector2 yAngleClamp = new(-60, 60);
		public Vector2 VerticalAngleClamp {
			get => this.yAngleClamp;
			set => this.yAngleClamp = value;
		}
		[SerializeField]
		private bool invertY = true;
		public bool InvertY {
			get => this.invertY;
			set => this.invertY = value;
		}

		private Vector2 lookDelta;
		public void OnLook(InputAction.CallbackContext context) {
			this.lookDelta = context.ReadValue<Vector2>();
		}

		private Vector2 moveDelta;
		public void OnMove(InputAction.CallbackContext context) {
			this.moveDelta = context.ReadValue<Vector2>();
		}

		private float upDownDelta;
		public void OnUpDown(InputAction.CallbackContext context) {
			this.upDownDelta = context.ReadValue<float>();
		}

		private bool running;
		public void OnRun(InputAction.CallbackContext context) {
			this.running = context.ReadValueAsButton();
		}

		private bool holdingToMove;
		public void OnHoldingToMove(InputAction.CallbackContext context) {
			Vector2 pos = Pointer.current.position.ReadValue();
			Rect screen = new(0, 0, Screen.width, Screen.height);
			bool cursorInWindow = screen.Contains(pos);

			if (context.started && cursorInWindow) {
				this.holdingToMove = true;
			} else if (context.canceled) {
				this.holdingToMove = false;
			}
		}

		private bool holdingToLook;
		public void OnHoldingToLook(InputAction.CallbackContext context) {
			Vector2 pos = Pointer.current.position.ReadValue();
			Rect screen = new(0, 0, Screen.width, Screen.height);
			bool cursorInWindow = screen.Contains(pos);

			if (context.started && cursorInWindow) {
				this.holdingToLook = context.ReadValueAsButton();
			} else if (context.canceled) {
				this.holdingToLook = context.ReadValueAsButton();
			}
		}

		private float zoomDelta;
		public void OnZoom(InputAction.CallbackContext context) {
			this.zoomDelta = context.ReadValue<float>();
		}

		private Vector3 focusPoint;
		public Vector3 FocusPoint {
			get => this.focusPoint;
			set {
				if (this.focusPoint == value) {
					return;
				}

				this.focusPoint = value;

				Camera.main.transform.LookAt(value);
			}
		}

		private void Orbit(Vector2 value) {
			Transform camera = Camera.main.transform;

			float distance = (camera.position - this.focusPoint).magnitude;

			camera.Rotate(Vector3.up, value.x * this.lookSensitivity.x, Space.World);

			float yAngle = (camera.rotation.eulerAngles.x + 180) % 360 - 180;
			float yDelta = (-value.y * this.lookSensitivity.y * (this.invertY ? -1 : 1) + 180) % 360 - 180;
			float yFinalAngle = Mathf.Clamp(yAngle + yDelta, this.yAngleClamp.x, this.yAngleClamp.y);

			Vector3 rotation = camera.rotation.eulerAngles;
			rotation.x = yFinalAngle;
			camera.rotation = Quaternion.Euler(rotation);

			camera.position = this.focusPoint - (camera.forward * distance);
		}

		private void MoveWasd(Vector2 value) {
			Transform camera = Camera.main.transform;

			Vector3 direction = (this.running ? this.runningMultiplier : 1) * Time.deltaTime *
				((this.moveSensitivity.x *  value.y * camera.forward) +
				(this.moveSensitivity.y * value.x * camera.right));
			camera.position += direction;
			this.focusPoint += direction;
		}

		private void MoveDrag(Vector2 value) {
			Transform camera = Camera.main.transform;

			Vector3 direction = Time.deltaTime *
				((this.moveSensitivity.x * -value.y * camera.up) +
				(this.upDownSensitivity * -value.x * camera.right));
			camera.position += direction;
			this.focusPoint += direction;
		}

		private void MoveUpDown(float value) {
			Transform camera = Camera.main.transform;

			Vector3 direction = 
				(this.running ? this.runningMultiplier : 1) * this.upDownSensitivity * Time.deltaTime * value *
				Vector3.down;
			camera.position += direction;
			this.focusPoint += direction;
		}

		private void Zoom(float value) {
			Transform camera = Camera.main.transform;

			float distance = (camera.position - this.focusPoint).magnitude;
			distance += value * this.zoomSensitivity;
			if (distance < 0) {
				distance = 0;
			}

			camera.position = this.focusPoint - (camera.forward * distance);
		}

		private void Update() {
			Cursor.lockState = Time.timeScale > 0 && (this.holdingToLook || this.holdingToMove) ? CursorLockMode.Locked : CursorLockMode.None;
			Cursor.visible = Time.timeScale <= 0 || (!this.holdingToLook && !this.holdingToMove);

			if (Time.timeScale > 0) {
				if (this.moveDelta != Vector2.zero) {
					this.MoveWasd(this.moveDelta);
				}
				if (this.holdingToMove && this.lookDelta != Vector2.zero) {
					this.MoveDrag(this.lookDelta);
				}
				if (this.holdingToLook && this.lookDelta != Vector2.zero) {
					this.Orbit(this.lookDelta);
				}
				if (this.upDownDelta != 0) {
					this.MoveUpDown(this.upDownDelta);
				}
				if (this.zoomDelta != 0) {
					this.Zoom(this.zoomDelta);
				}
			}
		}
	}
}
