using MZZT.DarkForces.FileFormats;
using System;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
	public class PauseMenu : Singleton<PauseMenu> {
		[SerializeField, Header("References")]
		private GameObject background = null;
		[SerializeField]
		private Image loading = null;
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

		private void Start() {
			LevelMusic.Instance.GetComponent<AudioSource>().mute = PlayerPrefs.GetInt("PlayMusic", 1) == 0;
			LevelMusic.Instance.GetComponent<AudioSource>().volume = PlayerPrefs.GetFloat("Volume", 1);

			PlayerInput.all[0].SwitchCurrentActionMap("Player");
		}

		private int loadingCount;
		public async Task BeginLoadingAsync() {
			if (this.loadingCount == 0) {
				this.MenuOpen = false;
				Time.timeScale = 0;

				if (this.loading.sprite == null) {
					DfPalette waitPal = await ResourceCache.Instance.GetPaletteAsync("WAIT.PAL");
					if (waitPal != null) {
						DfBitmap waitBm = await ResourceCache.Instance.GetBitmapAsync("WAIT.BM");
						if (waitBm != null) {
							Texture2D wait = ResourceCache.Instance.ImportBitmap(waitBm, waitPal);
							Rect rect = new Rect(0, 0, wait.width, wait.height);
							this.loading.sprite = Sprite.Create(wait, rect, new Vector2(0.5f, 0.5f));
						}
					}
				}

				this.loading.gameObject.SetActive(true);
			}
			
			this.loadingCount++;
		}

		public void EndLoading() {
			this.loadingCount--;

			if (this.loadingCount == 0) {
				this.loading.gameObject.SetActive(false);
				Time.timeScale = 1;
			}
		}

		private bool MenuOpen {
			get => this.background.activeSelf;
			set {
				this.background.SetActive(value);
				Time.timeScale = value ? 0 : 1;
				PlayerInput.all[0].SwitchCurrentActionMap(value ? "UI" : "Player");
			}
		}

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

		private void GenerateMenu() {
			this.PopulateLayers();

			this.playMusic.value = LevelMusic.Instance.GetComponent<AudioSource>().mute ? 0 : 1;
			this.volume.value = LevelMusic.Instance.GetComponent<AudioSource>().volume;
			this.warnings.isOn = ResourceCache.Instance.ShowWarnings;
		}

		private bool init = false;
		private int[] layers;
		public async void OnMenuAsync(InputAction.CallbackContext context) {
			if (this.loadingCount > 0 || context.phase != InputActionPhase.Started) {
				return;
			}

			if (this.MenuOpen) {
				await this.CloseMenuAsync();
				return;
			}

			if (!this.init) {
				this.GenerateMenu();
				this.init = true;
			}

			this.MenuOpen = true;
		}

		public void ApplyMenuChanges() {
			PlayerPrefs.SetInt("ShowWarnings", (ResourceCache.Instance.ShowWarnings = this.warnings.isOn) ? 1 : 0);
		}

		public async Task CloseMenuAsync() {
			this.ApplyMenuChanges();

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

			if (this.levelSelection.SelectedValue != LevelLoader.Instance.CurrentLevelIndex) {
				if (lightingChanged) {
					ResourceCache.Instance.Clear();
				}

				await LevelLoader.Instance.LoadAsync(this.levelSelection.SelectedValue);
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

			this.MenuOpen = false;
		}

		public async void OnCloseAsync() {
			await this.CloseMenuAsync();
		}

		public void OnReturnToMenu() {
			this.ApplyMenuChanges();

			SceneManager.LoadScene("Menu");
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
