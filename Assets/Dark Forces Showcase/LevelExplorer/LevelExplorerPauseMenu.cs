using System;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
	public class LevelExplorerPauseMenu : PauseMenu {
		[SerializeField]
		private LevelNameList levelSelection = null;
		[SerializeField]
		private ToggleGroup difficultySelection = null;
		[SerializeField]
		private Toggle showSprites = null;
		[SerializeField]
		private Toggle show3dos = null;
		[SerializeField]
		private Toggle animate3dos = null;
		[SerializeField]
		private TMP_Dropdown layerSelection = null;
		[SerializeField]
		private ToggleGroup lighting = null;
		[SerializeField]
		private Slider xSen = null;
		[SerializeField]
		private Slider ySen = null;
		[SerializeField]
		private Toggle invertY = null;
		[SerializeField]
		private Slider moveSen = null;
		[SerializeField]
		private Toggle noclip = null;
		[SerializeField]
		private CameraControl cameraControl = null;
		[SerializeField]
		private TMP_Dropdown playMusic = null;
		[SerializeField]
		private Slider volume = null;
		[SerializeField]
		private Toggle warnings = null;

		protected override void Start() {
			base.Start();

			LevelMusic.Instance.GetComponent<AudioSource>().mute = PlayerPrefs.GetInt("PlayMusic", 1) == 0;
			LevelMusic.Instance.GetComponent<AudioSource>().volume = PlayerPrefs.GetFloat("Volume", 1);
		}

		private int[] layers;
		private void PopulateLayers() {
			this.layerSelection.ClearOptions();
			this.layerSelection.options.Add(new TMP_Dropdown.OptionData("All"));
			this.layers = LevelLoader.Instance.Level.Sectors
				.Select(x => x.Layer)
				.Distinct()
				.OrderBy(x => x)
				.ToArray();
			this.layerSelection.AddOptions(this.layers.Select(x => $"Layer {x}").ToList());

			bool showAllLayers = LevelGeometryGenerator.Instance.ShowAllLayers;
			int layer = LevelGeometryGenerator.Instance.Layer;
			if (showAllLayers) {
				this.layerSelection.value = 0;
			} else {
				int index = Array.IndexOf(this.layers, layer);
				this.layerSelection.value = index + 1;
			}
		}

		protected override void GenerateMenu() {
			base.GenerateMenu();

			this.PopulateLayers();

			this.playMusic.value = LevelMusic.Instance.GetComponent<AudioSource>().mute ? 0 : 1;
			this.volume.value = LevelMusic.Instance.GetComponent<AudioSource>().volume;
			this.warnings.isOn = ResourceCache.Instance.ShowWarnings;
		}

		public override void ApplyMenuChanges() {
			base.ApplyMenuChanges();

			PlayerPrefs.SetInt("ShowWarnings", (ResourceCache.Instance.ShowWarnings = this.warnings.isOn) ? 1 : 0);
		}

		public override async Task CloseMenuAsync() {
			Toggle[] toggles;
			bool lightingChanged = false;
			bool levelVisChanged = false;
			bool objectVisChanged = false;

			this.cameraControl.LookSensitivity = new Vector2(this.xSen.value, this.ySen.value);
			this.cameraControl.MoveSensitivity = new Vector2(this.moveSen.value, this.moveSen.value);
			this.cameraControl.UpDownSensitivity = this.moveSen.value;
			this.cameraControl.InvertY = this.invertY.isOn;
			this.cameraControl.GetComponent<CapsuleCollider>().enabled = !this.noclip.isOn;

			bool showAllLayers = this.layerSelection.value == 0;
			int layer = showAllLayers ? 0 : this.layers[this.layerSelection.value - 1];
			if (LevelGeometryGenerator.Instance.ShowAllLayers != showAllLayers || (!showAllLayers &&
				LevelGeometryGenerator.Instance.Layer != layer)) {

				levelVisChanged = true;
				objectVisChanged = true;
				LevelGeometryGenerator.Instance.ShowAllLayers = showAllLayers;
				LevelGeometryGenerator.Instance.Layer = layer;
			}

			toggles = this.lighting.GetComponentsInChildren<Toggle>(true);
			int lighting = toggles.Select((x, i) => (x, i)).First(x => x.x.isOn).i;
			bool fullBright = lighting == 2;
			bool bypassCmp = lighting == 1;

			if (ResourceCache.Instance.FullBright != fullBright) {
				lightingChanged = true;
				ResourceCache.Instance.FullBright = fullBright;
			}
			if (ResourceCache.Instance.BypassCmpDithering != bypassCmp) {
				lightingChanged = true;
				ResourceCache.Instance.BypassCmpDithering = bypassCmp;
			}

			toggles = this.difficultySelection.GetComponentsInChildren<Toggle>(true);
			ObjectGenerator.Difficulties difficulty =
				(ObjectGenerator.Difficulties)toggles.Select((x, i) => (x, i)).First(x => x.x.isOn).i;
			if (ObjectGenerator.Instance.Difficulty != difficulty) {
				objectVisChanged = true;
				ObjectGenerator.Instance.Difficulty = difficulty;
			}

			if (ObjectGenerator.Instance.ShowSprites != this.showSprites.isOn) {
				objectVisChanged = true;
				ObjectGenerator.Instance.ShowSprites = this.showSprites.isOn;
			}

			if (ObjectGenerator.Instance.Show3dos != this.show3dos.isOn) {
				objectVisChanged = true;
				ObjectGenerator.Instance.Show3dos = this.show3dos.isOn;
			}

			ObjectGenerator.Instance.Animate3dos = this.animate3dos.isOn;

			int levelIndex = this.levelSelection.SelectedValue != null ? LevelLoader.Instance.LevelList.Levels.IndexOf(this.levelSelection.SelectedValue) : 0;
			if (levelIndex != LevelLoader.Instance.CurrentLevelIndex) {
				if (lightingChanged) {
					ResourceCache.Instance.Clear();
				}

				await LevelExplorer.Instance.LoadAndRenderLevelAsync(levelIndex);
				PlayerInput.all[0].SwitchCurrentActionMap(this.actionMap);
				this.PopulateLayers();
			} else {
				if (lightingChanged) {
					ResourceCache.Instance.RegenerateMaterials();
				}
				if (levelVisChanged) {
					LevelGeometryGenerator.Instance.RefreshVisiblity();
				}
				if (objectVisChanged) {
					ObjectGenerator.Instance.RefreshVisiblity();
				}
			}

			await base.CloseMenuAsync();
		}

		public override void EndLoading() {
			base.EndLoading();

			if (this.loadingCount == 0) {
				PlayerInput.all[0].SwitchCurrentActionMap(this.actionMap);
			}
		}

		public async void OnPlayMusicValueChangedAsync(int value) {
			int existing = LevelMusic.Instance.IsPlaying ? (LevelMusic.Instance.FightMusic ? 2 : 1) : 0;
			if (existing == value) {
				return;
			}

			PlayerPrefs.SetInt("PlayMusic", (value > 0) ? 1 : 0);

			LevelMusic.Instance.Stop();
			LevelMusic.Instance.FightMusic = value == 2;
			if (value > 0) {
				await LevelMusic.Instance.PlayAsync(LevelLoader.Instance.CurrentLevelIndex);
			}
		}

		public void OnVolumeChanged(float value) {
			LevelMusic.Instance.GetComponent<AudioSource>().volume = value;
			PlayerPrefs.SetFloat("Volume", value);
		}
	}
}
