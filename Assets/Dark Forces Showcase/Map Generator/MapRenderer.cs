using SkiaSharp;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
	/// <summary>
	/// Script which powers the Map Generator showcase.
	/// </summary>
	public class MapRenderer : Singleton<MapRenderer> {
		[SerializeField]
		private RawImage image = null;
		[SerializeField]
		private TMP_Text status = null;

		private async void Start() {
			// This is here in case you run directly from the MapGenerator sccene instead of the menu.
			if (!FileLoader.Instance.Gobs.Any()) {
				await FileLoader.Instance.LoadStandardGobFilesAsync();
			}

			await PauseMenu.Instance.BeginLoadingAsync();

			ResourceCache.Instance.ClearWarnings();

			await LevelLoader.Instance.LoadLevelListAsync(true);

			await LevelLoader.Instance.ShowWarningsAsync("JEDI.LVL");

			await ((MapGeneratorPauseMenu)PauseMenu.Instance).ApplySettingsAsync();

			PauseMenu.Instance.EndLoading();
		}

		/// <summary>
		/// Load the level data.
		/// </summary>
		/// <param name="levelIndex">The index of the level in JEDI.LVL</param>
		public async Task LoadLevelAsync(int levelIndex) {
			await LevelLoader.Instance.LoadLevelAsync(levelIndex);
			if (LevelLoader.Instance.Level != null) {
				await LevelLoader.Instance.LoadInformationAsync();
			}
		}

		/// <summary>
		/// Reswts the image view to 0 rotation, centered, 
		/// </summary>
		public void ResetViewToFit() {
			this.image.rectTransform.anchoredPosition = Vector2.zero;
			this.image.rectTransform.localRotation = Quaternion.identity;

			Vector2 size = this.image.rectTransform.sizeDelta;
			Vector2 viewport = ((RectTransform)this.image.rectTransform.parent).sizeDelta;
			Vector2 diff = new Vector2(viewport.x / size.x, viewport.y / size.y);
			float zoom = Mathf.Min(diff.x, diff.y);
			this.image.rectTransform.localScale = new Vector3(zoom, zoom, zoom);
		}

		/// <summary>
		/// Reswts the image view to 0 rotation, centered, 
		/// </summary>
		public void ResetView() {
			this.image.rectTransform.anchoredPosition = Vector2.zero;
			this.image.rectTransform.localRotation = Quaternion.identity;

			float zoom = 1 / ((MapGeneratorPauseMenu)PauseMenu.Instance).Resolution;
			this.image.rectTransform.localScale = new Vector3(zoom, zoom, zoom);
		}

		/// <summary>
		/// Generate the map and display it.
		/// </summary>
		public void Render() {
			MapGenerator map = this.GetComponent<MapGenerator>();

			this.image.texture = map.GenerateTexture(LevelLoader.Instance.Level, LevelLoader.Instance.Information);

			this.image.rectTransform.sizeDelta = new Vector2(
				this.image.texture.width,
				this.image.texture.height
			);

			this.ResetViewToFit();
		}

		public bool ControlsEnabled { get; private set; } = true;

		private Vector2 moveDelta;
		private float rotateDelta;
		public void OnPointerMove(InputAction.CallbackContext context) {
			Vector2 delta = context.ReadValue<Vector2>();
			if (this.moving) {
				this.moveDelta = delta;
			} else {
				this.moveDelta = Vector2.zero;
			}
			if (this.rotating) {
				this.rotateDelta = delta.x;
			} else {
				this.rotateDelta = 0;
			}
		}

		private float zoomDelta;
		public void OnZoom(InputAction.CallbackContext context) {
			this.zoomDelta = context.ReadValue<float>();
		}

		private bool moving;
		public void OnEnableMove(InputAction.CallbackContext context) {
			if (this.rotating) {
				this.moving = false;
				return;
			}

			this.moving = context.ReadValueAsButton();
		}

		private bool rotating;
		public void OnEnableRotate(InputAction.CallbackContext context) {
			if (this.moving) {
				this.rotating = false;
				return;
			}

			this.rotating = context.ReadValueAsButton();
		}

		private Vector2 point;
		public void OnPoint(InputAction.CallbackContext context) {
			this.point = context.ReadValue<Vector2>();
		}

		private void Update() {
			if (!this.ControlsEnabled || this.image.texture == null) {
				return;
			}

			Vector2Int size = new Vector2Int(this.image.texture.width, this.image.texture.height);

			RectTransformUtility.ScreenPointToLocalPointInRectangle(this.image.rectTransform, this.point, null, out Vector2 localPoint);
			bool pointerInBounds = localPoint.x >= this.image.rectTransform.rect.xMin &&
				localPoint.x <= this.image.rectTransform.rect.xMax &&
				localPoint.y >= this.image.rectTransform.rect.yMin &&
				localPoint.y <= this.image.rectTransform.rect.yMax;
			if (pointerInBounds) {
				MapGenerator map = this.GetComponent<MapGenerator>();
				Vector2 pos = map.Viewport.position + localPoint / map.Zoom;
				this.status.text = $"Image Size: {size.x}x{size.y} - DFU: {pos.x:0.0}, {pos.y:0.0}";
			} else {
				this.status.text = $"Image Size: {size.x}x{size.y}";
			}

			if (this.moveDelta != Vector2.zero) {
				this.image.rectTransform.anchoredPosition += this.moveDelta / this.image.GetComponentInParent<Canvas>().transform.localScale;
			}

			if (this.rotateDelta != 0) {
				Vector3 rotation = this.image.rectTransform.localEulerAngles;
				rotation.z -= this.rotateDelta / 2;
				this.image.rectTransform.localEulerAngles = rotation;
			}

			if (this.zoomDelta != 0) {
				float zoom = this.image.rectTransform.localScale.x;
				zoom *= Mathf.Pow(2, this.zoomDelta / 120);
				this.image.rectTransform.localScale = new Vector3(zoom, zoom, zoom);

				if (pointerInBounds) {
					Canvas.ForceUpdateCanvases();
					RectTransformUtility.ScreenPointToLocalPointInRectangle(this.image.rectTransform, this.point, null, out Vector2 localPoint2);
					Vector2 diff = this.image.rectTransform.localRotation * ((localPoint - localPoint2) * zoom);
					this.image.rectTransform.anchoredPosition -= diff;
				}
			}
		}

		private string lastFile = null;
		public async void SaveAsync() {
			this.ControlsEnabled = false;

			string folder = this.lastFile != null ? Path.GetDirectoryName(this.lastFile)
				 : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
			string file = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = false,
				FileSearchPatterns = new[] { "*.PNG" },
				SelectButtonText = "Save",
				SelectedFileMustExist = false,
				SelectedPathMustExist = true,
				StartSelectedFile = Path.Combine(folder, $"{LevelLoader.Instance.CurrentLevelName}.png"),
				Title = "Export Map",
				ValidateFileName = true
			});

			this.ControlsEnabled = true;
			if (file == null) {
				return;
			}

			this.lastFile = file;

			MapGenerator map = this.GetComponent<MapGenerator>();

			using SKData data = map.GeneratePng(LevelLoader.Instance.Level, LevelLoader.Instance.Information);
			using Stream stream = data.AsStream();
			using FileStream fileStream = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None);
			await stream.CopyToAsync(fileStream);
		}
	}
}
