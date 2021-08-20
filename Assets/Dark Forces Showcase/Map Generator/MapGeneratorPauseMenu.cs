using MZZT.DataBinding;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static MZZT.DarkForces.MapGenerator;

namespace MZZT.DarkForces.Showcase {
	public class MapGeneratorPauseMenu : PauseMenu {
		[SerializeField]
		private Image backgroundImage = null;
		[SerializeField]
		private LevelNameList levelSelection = null;
		[SerializeField]
		private Toggle allLayers = null;
		[SerializeField]
		private LayerList layers = null;
		[SerializeField]
		private Toggle showInactiveLayers = null;
		[SerializeField]
		private Toggle sizeToFit = null;
		[SerializeField]
		private TMP_InputField centerX = null;
		[SerializeField]
		private TMP_InputField centerY = null;
		[SerializeField]
		private TMP_InputField width = null;
		[SerializeField]
		private TMP_InputField height = null;
		[SerializeField]
		private Toggle zoomToFit = null;
		[SerializeField]
		private TMP_InputField zoom = null;
		[SerializeField]
		private Toggle fitVisible = null;
		[SerializeField]
		private Toggle fitActive = null;
		[SerializeField]
		private Toggle fitAll = null;
		[SerializeField]
		private TMP_Dropdown paddingUnits = null;
		[SerializeField]
		private TMP_InputField paddingLeft = null;
		[SerializeField]
		private TMP_InputField paddingTop = null;
		[SerializeField]
		private TMP_InputField paddingRight = null;
		[SerializeField]
		private TMP_InputField paddingBottom = null;
		[SerializeField]
		private TMP_Dropdown themes = null;
		[SerializeField]
		private Toggle allowWallFlagsToOverrideWallTypes = null;
		[SerializeField]
		private TMP_InputField lineWidth = null;
		[SerializeField]
		private TMP_InputField rotation = null;
		[SerializeField]
		private TMP_InputField resolution = null;
		[SerializeField]
		private MapGenerator mapGenerator = null;

		private void PopulateLayers() {
			this.layers.Clear();
			this.layers.AddRange(LevelLoader.Instance.Level.Sectors
				.Select(x => x.Layer)
				.Distinct()
				.OrderBy(x => x));

			int[] activeLayers = this.mapGenerator.Layers;
			foreach (Databound<int> bind in this.layers.Databinders) {
				Toggle toggle = bind.GetComponent<Toggle>();
				toggle.isOn = this.allLayers.isOn || Array.IndexOf(activeLayers, bind.Value) >= 0;
				toggle.onValueChanged.AddListener(this.OnLayerChecked);
			}
		}

		protected override void GenerateMenu() {
			base.GenerateMenu();

			this.PopulateLayers();
		}

		public float Resolution {
			get {
				if (!float.TryParse(this.resolution.text, out float resolution)) {
					resolution = 100;
				}
				resolution /= 100;
				return resolution;
			}
		}

		public override void OnMenuAsync(InputAction.CallbackContext context) {
			if (!MapRenderer.Instance.ControlsEnabled) {
				return;
			}

			base.OnMenuAsync(context);
		}

		public async Task ApplySettingsAsync() {
			await this.BeginLoadingAsync();

			bool showInactiveLayers = this.showInactiveLayers.isOn;

			float resolution = this.Resolution;

			bool sizeToFit = this.sizeToFit.isOn;
			float.TryParse(this.centerX.text, out float centerX);
			float.TryParse(this.centerY.text, out float centerY);
			Vector2 center = new Vector2(centerX, centerY);
			int.TryParse(this.width.text, NumberStyles.Integer, null, out int width);
			int.TryParse(this.height.text, NumberStyles.Integer, null, out int height);
			Vector2 size = new Vector2(width, height);
			size *= resolution;
			bool zoomToFit = this.zoomToFit.isOn;
			if (!float.TryParse(this.zoom.text, out float zoom)) {
				zoom = 100;
			}
			zoom *= resolution / 100;

			BoundingModes fitType;
			if (this.fitVisible.isOn) {
				fitType = BoundingModes.FitVisible;
			} else if (this.fitActive.isOn) {
				fitType = BoundingModes.FitToActiveLayers;
			} else if (this.fitAll.isOn) {
				fitType = BoundingModes.FitToAllLayers;
			} else {
				fitType = BoundingModes.Manual;
			}

			PaddingUnits paddingUnits = this.paddingUnits.value != 0 ? PaddingUnits.GameUnits : PaddingUnits.Pixels;
			float.TryParse(this.paddingLeft.text, out float paddingLeft);
			float.TryParse(this.paddingTop.text, out float paddingTop);
			float.TryParse(this.paddingRight.text, out float paddingRight);
			float.TryParse(this.paddingBottom.text, out float paddingBottom);
			Vector4 padding = new Vector4(paddingLeft, paddingBottom, paddingRight, paddingTop);
			if (paddingUnits == PaddingUnits.Pixels) {
				padding *= resolution;
			}

			if (!float.TryParse(this.lineWidth.text, out float lineWidth)) {
				lineWidth = 1;
			}
			lineWidth *= resolution;
			Color background;
			LineProperties inactive;
			LineProperties adjoined;
			LineProperties ledge;
			LineProperties unadjoined;
			LineProperties elevator;
			LineProperties sectorTrigger;
			LineProperties wallTrigger;
			switch (this.themes.value) {
				case 1: // WDFUSE
					background = Color.black;
					inactive = new LineProperties() {
						Color = new Color(0.5f, 0.5f, 0.5f),
						Priority = 6,
						Width = lineWidth
					};
					adjoined = new LineProperties() {
						Color = new Color(0, 0.5f, 0),
						Priority = 0,
						Width = lineWidth
					};
					ledge = new LineProperties() {
						Color = new Color(0, 0.5f, 0),
						Priority = 1,
						Width = lineWidth
					};
					unadjoined = new LineProperties() {
						Color = new Color(0, 1, 0),
						Priority = 2,
						Width = lineWidth
					};
					elevator = new LineProperties() {
						Color = new Color(1, 1, 0),
						Priority = 5,
						Width = lineWidth
					};
					sectorTrigger = new LineProperties() {
						Color = new Color(0, 1, 1),
						Priority = 4,
						Width = lineWidth
					};
					wallTrigger = new LineProperties() {
						Color = new Color(0, 1, 1),
						Priority = 3,
						Width = lineWidth
					};
					break;
				case 2: // Parchment
					background = new Color(0.9f, 0.8f, 0.5f);
					inactive = new LineProperties() {
						Color = new Color(0, 0, 0, 0.5f),
						Priority = 2,
						Width = lineWidth / 2
					};
					adjoined = new LineProperties() {
						Color = default,
						Priority = -1,
						Width = 0
					};
					ledge = new LineProperties() {
						Color = new Color(0, 0, 0),
						Priority = 0,
						Width = lineWidth /2f
					};
					unadjoined = new LineProperties() {
						Color = new Color(0, 0, 0),
						Priority = 1,
						Width = lineWidth
					};
					elevator = new LineProperties() {
						Color = default,
						Priority = -1,
						Width = 0
					};
					sectorTrigger = new LineProperties() {
						Color = default,
						Priority = -1,
						Width = 0
					};
					wallTrigger = new LineProperties() {
						Color = default,
						Priority = -1,
						Width = 0
					};
					break;
				case 3: // Printout
					background = Color.white;
					inactive = new LineProperties() {
						Color = new Color(0, 0, 0, 0.333f),
						Priority = 5,
						Width = lineWidth
					};
					adjoined = new LineProperties() {
						Color = default,
						Priority = -1,
						Width = 0
					};
					ledge = new LineProperties() {
						Color = new Color(0, 0, 0, 0.667f),
						Priority = 0,
						Width = lineWidth
					};
					unadjoined = new LineProperties() {
						Color = new Color(0, 0, 0),
						Priority = 1,
						Width = lineWidth
					};
					elevator = new LineProperties() {
						Color = new Color(1, 0, 0),
						Priority = 4,
						Width = lineWidth
					};
					sectorTrigger = new LineProperties() {
						Color = new Color(1, 0, 0),
						Priority = 3,
						Width = lineWidth
					};
					wallTrigger = new LineProperties() {
						Color = new Color(1, 0, 0),
						Priority = 2,
						Width = lineWidth
					};
					break;
				default: // DF Automap
					background = Color.black;
					inactive = new LineProperties() {
						Color = new Color(0.5f, 0.5f, 0.5f),
						Priority = 3,
						Width = lineWidth
					};
					adjoined = new LineProperties() {
						Color = default,
						Priority = -1,
						Width = 0
					};
					ledge = new LineProperties() {
						Color = new Color(0, 0.5f, 0),
						Priority = 0,
						Width = lineWidth
					};
					unadjoined = new LineProperties() {
						Color = new Color(0, 1, 0),
						Priority = 2,
						Width = lineWidth
					};
					elevator = new LineProperties() {
						Color = new Color(1, 1, 0),
						Priority = 1,
						Width = lineWidth
					};
					sectorTrigger = new LineProperties() {
						Color = default,
						Priority = -1,
						Width = 0
					};
					wallTrigger = new LineProperties() {
						Color = default,
						Priority = -1,
						Width = 0
					};
					break;
			}
			bool allowWallFlagsToOverrideWallTypes = this.allowWallFlagsToOverrideWallTypes.isOn;
			float.TryParse(this.rotation.text, out float rotation);

			bool regenerateMap = false;

			if (this.levelSelection.SelectedValue != LevelLoader.Instance.CurrentLevelIndex) {
				await MapRenderer.Instance.LoadLevelAsync(this.levelSelection.SelectedValue);

				await LevelLoader.Instance.ShowWarningsAsync();

				this.PopulateLayers();
				regenerateMap = true;
			}

			int[] layers = this.layers.Databinders.Where(x => x.GetComponent<Toggle>().isOn)
				.Select(x => x.Value).OrderBy(x => x).ToArray();
			if (!this.mapGenerator.Layers.SequenceEqual(layers)) {
				this.mapGenerator.Layers = layers;
				regenerateMap = true;
			}
			if (showInactiveLayers != (this.mapGenerator.UnselectedLayersRenderMode == UnselectedLayersRenderModes.ShowInactive)) {
				this.mapGenerator.UnselectedLayersRenderMode = showInactiveLayers ? UnselectedLayersRenderModes.ShowInactive : UnselectedLayersRenderModes.Hide;
				regenerateMap = true;
			}
			if ((sizeToFit ? fitType : BoundingModes.Manual) != this.mapGenerator.ViewportFitMode) {
				this.mapGenerator.ViewportFitMode = sizeToFit ? fitType : BoundingModes.Manual;
				regenerateMap = true;
			}
			Rect viewport = new Rect(center, size);
			if (!sizeToFit && viewport != this.mapGenerator.Viewport) {
				this.mapGenerator.Viewport = viewport;
				regenerateMap = true;
			}
			if ((zoomToFit ? fitType : BoundingModes.Manual) != this.mapGenerator.ZoomFitMode) {
				this.mapGenerator.ZoomFitMode = zoomToFit ? fitType : BoundingModes.Manual;
				regenerateMap = true;
			}
			if (!zoomToFit && zoom != this.mapGenerator.Zoom) {
				this.mapGenerator.Zoom = zoom;
				regenerateMap = true;
			}
			if (paddingUnits != this.mapGenerator.PaddingUnit) {
				this.mapGenerator.PaddingUnit = paddingUnits;
				regenerateMap = true;
			}
			if (padding != this.mapGenerator.Padding) {
				this.mapGenerator.Padding = padding;
				regenerateMap = true;
			}
			this.backgroundImage.color = background;
			if (inactive.Color != this.mapGenerator.InactiveLayer.Color ||
				inactive.Priority != this.mapGenerator.InactiveLayer.Priority ||
				inactive.Width != this.mapGenerator.InactiveLayer.Width) {

				this.mapGenerator.InactiveLayer = inactive;
				regenerateMap = true;
			}
			if (adjoined.Color != this.mapGenerator.Adjoined.Color ||
				adjoined.Priority != this.mapGenerator.Adjoined.Priority ||
				adjoined.Width != this.mapGenerator.Adjoined.Width) {

				this.mapGenerator.Adjoined = adjoined;
				regenerateMap = true;
			}
			if (ledge.Color != this.mapGenerator.Ledge.Color ||
				ledge.Priority != this.mapGenerator.Ledge.Priority ||
				ledge.Width != this.mapGenerator.Ledge.Width) {

				this.mapGenerator.Ledge = ledge;
				regenerateMap = true;
			}
			if (unadjoined.Color != this.mapGenerator.Unadjoined.Color ||
				unadjoined.Priority != this.mapGenerator.Unadjoined.Priority ||
				unadjoined.Width != this.mapGenerator.Unadjoined.Width) {

				this.mapGenerator.Unadjoined = unadjoined;
				regenerateMap = true;
			}
			if (elevator.Color != this.mapGenerator.Elevator.Color ||
				elevator.Priority != this.mapGenerator.Elevator.Priority ||
				elevator.Width != this.mapGenerator.Elevator.Width) {

				this.mapGenerator.Elevator = elevator;
				regenerateMap = true;
			}
			if (sectorTrigger.Color != this.mapGenerator.SectorTrigger.Color ||
				sectorTrigger.Priority != this.mapGenerator.SectorTrigger.Priority ||
				sectorTrigger.Width != this.mapGenerator.SectorTrigger.Width) {

				this.mapGenerator.SectorTrigger = sectorTrigger;
				regenerateMap = true;
			}
			if (wallTrigger.Color != this.mapGenerator.WallTrigger.Color ||
				wallTrigger.Priority != this.mapGenerator.WallTrigger.Priority ||
				wallTrigger.Width != this.mapGenerator.WallTrigger.Width) {

				this.mapGenerator.WallTrigger = wallTrigger;
				regenerateMap = true;
			}
			if (allowWallFlagsToOverrideWallTypes != this.mapGenerator.AllowLevelToOverrideWallTypes) {
				this.mapGenerator.AllowLevelToOverrideWallTypes = allowWallFlagsToOverrideWallTypes;
				regenerateMap = true;
			}
			if (-rotation != this.mapGenerator.Rotation) {
				this.mapGenerator.Rotation = -rotation;
				regenerateMap = true;
			}

			if (regenerateMap) {
				MapRenderer.Instance.Render();
			}

			this.EndLoading();
		}

		public override async Task CloseMenuAsync() {
			await this.BeginLoadingAsync();

			await this.ApplySettingsAsync();

			await base.CloseMenuAsync();

			this.EndLoading();
		}

		private bool ignore = false;
		public void OnAllLayersChecked(bool value) {
			if (this.ignore) {
				return;
			}

			this.ignore = true;
			try {
				foreach (Databound<int> x in this.layers.Databinders) {
					x.GetComponent<Toggle>().isOn = value;
				}
			} finally {
				this.ignore = false;
			}
		}

		private void OnLayerChecked(bool _) {
			if (this.ignore) {
				return;
			}

			this.ignore = true;
			try {
				this.allLayers.isOn = this.layers.Databinders.All(x => x.GetComponent<Toggle>().isOn);
			} finally {
				this.ignore = false;
			}
		}

		public void OnSizeToFitChecked(bool value) {
			if (value) {
				this.zoomToFit.isOn = false;
				this.zoom.interactable = true;
			}
			this.centerX.interactable = !value;
			this.centerY.interactable = !value;
			this.width.interactable = !value;
			this.height.interactable = !value;
		}

		public void OnZoomToFitChecked(bool value) {
			if (value) {
				this.sizeToFit.isOn = false;
				this.centerX.interactable = true;
				this.centerY.interactable = true;
				this.width.interactable = true;
				this.height.interactable = true;
			}
			this.zoom.interactable = !value;
		}
	}
}
