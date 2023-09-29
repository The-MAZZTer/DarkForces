using MZZT.DarkForces.Converters;
using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using MZZT.FileFormats;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Color = UnityEngine.Color;

namespace MZZT.DarkForces.Showcase {
	public class CmpViewer : Databind<DfColormap>, IResourceViewer {
		[Header("CMP"), SerializeField]
		private PaletteList colorMap;
		[SerializeField]
		private TMP_Text colorMapLightLevelLabel;
		[SerializeField]
		private Slider colorMapLightLevel;
		[SerializeField]
		private PaletteList palette;
		[SerializeField]
		private PaletteList headlight;
		[SerializeField]
		private TMP_Text headlightValueLabel;
		[SerializeField]
		private Slider headlightValue;
		[SerializeField]
		private GameObject dropdown;
		[SerializeField]
		private TMP_InputField palettePath;

		public string TabName => this.filePath == null ? "New CMP" : Path.GetFileName(this.filePath);
		public event EventHandler TabNameChanged;

		public Sprite Thumbnail { get; private set; }
		public event EventHandler ThumbnailChanged;

		public void ResetDirty() {
			if (!this.IsDirty) {
				return;
			}

			this.IsDirty = false;
			this.IsDirtyChanged?.Invoke(this, new());
		}

		public void OnDirty() {
			if (this.IsDirty) {
				return;
			}

			this.IsDirty = true;
			this.IsDirtyChanged?.Invoke(this, new());
		}

		public bool IsDirty { get; private set; }
		public event EventHandler IsDirtyChanged;

		private string filePath;
		public async Task LoadAsync(ResourceEditorResource resource, IFile file) {
			this.filePath = resource?.Path;

			this.Value = (DfColormap)file;

			this.palettePath.text = this.filePath != null ? Path.GetFileNameWithoutExtension(this.filePath) : "SECBASE";
			await this.RefreshPaletteAsync();
		}

		public async void BrowsePaletteAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = true,
				AllowNavigateLfd = false,
				FileSearchPatterns = new[] { "*.PAL", "*.CMP" },
				SelectButtonText = "Select",
				SelectedFileMustExist = true,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Select palette"
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			path = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
			this.palettePath.text = path;
		}

		private DfPalette pal;
		public async Task RefreshPaletteAsync() {
			string palette = this.palettePath.text;
			if (string.IsNullOrWhiteSpace(palette)) {
				palette = "SECBASE";
			}
			palette = Path.Combine(Path.GetDirectoryName(palette), Path.GetFileNameWithoutExtension(palette));
			DfPalette pal = await this.GetFileAsync<DfPalette>(this.filePath, palette + ".PAL");
			if (pal != null) {
				this.pal = pal;
			}

			// Last ditch effort to be useful, we NEED a palette
			this.pal ??= new DfPalette();

			if (this.pal != null) {
				this.palette.Clear();
				this.palette.AddRange(this.pal.ToUnityColorArray());
			}

			this.OnColorMapLightLevelChanged(this.colorMapLightLevel.value);

			this.OnHeadlightLevelsChanged();
		}
		public async void RefreshPaletteUnityAsync() => await this.RefreshPaletteAsync();

		private async Task<T> GetFileAsync<T>(string baseFile, string file) where T : File<T>, IDfFile, new() {
			if (Path.GetInvalidPathChars().Intersect(file).Any()) {
				return null;
			}

			if (baseFile != null) {
				string folder = Path.GetDirectoryName(baseFile);
				string path = Path.Combine(folder, file);
				return await DfFile.GetFileFromFolderOrContainerAsync<T>(path) ?? await ResourceCache.Instance.GetAsync<T>(file);
			}

			return await ResourceCache.Instance.GetAsync<T>(file);
		}

		public void OnColorMapLightLevelChanged(float level) {
			if (this.pal == null) {
				return;
			}

			Color[] colors = this.Value.ToUnityColorArray(this.pal, (int)level, false, false);
			if (this.colorMap.Count != colors.Length) {
				this.colorMap.Clear();
				this.colorMap.AddRange(colors);
			} else {
				foreach ((IDatabind bind, int i) in this.colorMap.Children.Select((x, i) => (x, i))) {
					bind.Value = colors[i];
				}
			}

			//this.colorMap.Clear();
			//this.colorMap.AddRange(this.Value.ToUnityColorArray(this.pal, (int)level, false, false));

			this.colorMapLightLevelLabel.text = $"Light Level {(int)level}";

			this.GenerateThumbnail();
		}

		private void GenerateThumbnail() {
			if (this.pal == null) {
				return;
			}

			byte[] palette = this.Value.ToByteArray(this.pal, (int)this.colorMapLightLevel.value, false, false);
			int width = 16;
			int height = 16;

			byte[] buffer = new byte[width * height * 4];
			for (int y = 0; y < height; y++) {
				Buffer.BlockCopy(palette, y * width * 4, buffer, (height - y - 1) * width * 4, width * 4);
			}

			Texture2D texture = new(width, height, TextureFormat.RGBA32, false, true) {
#if UNITY_EDITOR
				alphaIsTransparency = true,
#endif
				filterMode = FilterMode.Point
			};
			texture.LoadRawTextureData(buffer);
			texture.Apply(true, true);

			this.Thumbnail = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
			this.ThumbnailChanged?.Invoke(this, new EventArgs());
		}

		private void OnHeadlightLevelsChanged() {
			this.headlight.Clear();
			this.headlight.AddRange(this.Value.HeadlightLightLevels.Select(x => {
				float value = Mathf.Clamp01((31 - x) / 31f);
				return new Color(value, value, value);
			}));
		}

		public void OnColorMapSelectedItemChanged() {
			IDatabind bind = this.colorMap.SelectedDatabound;
			IDatabind bind2 = this.palette.SelectedDatabound;
			if (bind == null || bind2 == null) {
				return;
			}
			int colorMapIndex = this.colorMap.Children.TakeWhile(x => x != bind).Count();
			int paletteIndex = this.palette.Children.TakeWhile(x => x != bind2).Count();

			this.Value.PaletteMaps[(int)this.colorMapLightLevel.value][colorMapIndex] = (byte)paletteIndex;

			this.OnDirty();

			bind.Value = bind2.Value;

			this.colorMap.SelectedDatabound = null;
			this.palette.SelectedDatabound = null;
		}

		private bool userInput = true;
		public void OnHeadlightSelectionChanged() {
			IDatabind bind = this.headlight.SelectedDatabound;
			if (bind == null) {
				this.headlightValue.interactable = false;
				return;
			}
			int index = this.headlight.Children.TakeWhile(x => x != bind).Count();

			byte value = this.Value.HeadlightLightLevels[index];

			this.headlightValue.interactable = true;
			this.userInput = false;
			try {
				this.headlightValue.value = 31 - value;
			} finally {
				this.userInput = true;
			}
			this.headlightValueLabel.text = $"Brightness {value}";
		}

		public void OnHeadlightValueChanged(float value) {
			if (!this.userInput) {
				return;
			}

			IDatabind bind = this.headlight.SelectedDatabound;
			if (bind == null) {
				return;
			}
			int index = this.headlight.Children.TakeWhile(x => x != bind).Count();

			this.Value.HeadlightLightLevels[index] = (byte)(31 - value);

			this.headlightValueLabel.text = $"Brightness {(byte)(31 - value)}";

			value = Mathf.Clamp01(value / 31);
			bind.Value = new Color(value, value, value);

			this.OnDirty();
		}

		public void AutoGenerate() {
			if (this.pal == null) {
				return;
			}

			for (int i = 0; i < 31; i++) {
				float percent = i / 31f;

				byte[] map = this.Value.PaletteMaps[i];

				for (int j = 0; j < 256; j++) {
					if (j < 32) {
						map[j] = (byte)j;
						continue;
					}

					RgbColor fullColor = this.pal.Palette[j];
					RgbColor target = new() {
						R = (byte)Mathf.RoundToInt(fullColor.R * percent),
						G = (byte)Mathf.RoundToInt(fullColor.G * percent),
						B = (byte)Mathf.RoundToInt(fullColor.B * percent),
					};
					int index = this.pal.Palette
						.Select((x, i) => (Math.Abs(target.R - x.R) + Math.Abs(target.G - x.G) + Math.Abs(target.B - x.B), i))
						.OrderBy(x => x.Item1)
						.First().i;
					map[j] = (byte)index;
				}
			}

			this.Value.PaletteMaps[31] = Enumerable.Range(0, 256).Select(x => (byte)x).ToArray();

			this.OnDirty();

			this.OnColorMapLightLevelChanged(this.colorMapLightLevel.value);
		}

		private string lastFolder;
		public async void ExportToPngAsync() {
			if (this.pal == null) {
				return;
			}

			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = false,
				FileSearchPatterns = new[] { "*.BMP", "*.GIF", "*.PNG" },
				SelectButtonText = "Export",
				SelectedPathMustExist = true,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Export to BMP/GIF/PNG"
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			using Bitmap bitmap = this.Value.ToBitmap(this.pal, (int)this.colorMapLightLevel.value);
			bitmap.Save(path);
		}

		public async void ExportToJascPalAsync() {
			if (this.pal == null) {
				return;
			}

			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = false,
				FileSearchPatterns = new[] { "*.PAL" },
				SelectButtonText = "Export",
				SelectedPathMustExist = true,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Export to JASC PAL"
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			using FileStream output = new(path, FileMode.Create, FileAccess.Write, FileShare.None);
			await this.Value.WriteJascPalAsync(this.pal, (int)this.colorMapLightLevel.value, output);
		}

		public async void ExportToRgbPalAsync() {
			if (this.pal == null) {
				return;
			}

			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = false,
				FileSearchPatterns = new[] { "*.PAL" },
				SelectButtonText = "Export",
				SelectedPathMustExist = true,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Export to 24-bit PAL"
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			using FileStream output = new(path, FileMode.Create, FileAccess.Write, FileShare.None);
			await this.Value.WriteRgbPalAsync(this.pal, (int)this.colorMapLightLevel.value, output);
		}

		public void SetHeadlightsDefault() {
			float delta = 31f / 128;
			this.Value.HeadlightLightLevels = Enumerable.Range(1, 128).Select(x => (byte)Mathf.RoundToInt(x * delta)).ToArray();

			this.OnDirty();

			this.OnHeadlightLevelsChanged();
		}

		public void SetHeadlightsConst(int i) {
			this.Value.HeadlightLightLevels = Enumerable.Repeat(i, 128).Select(x => (byte)x).ToArray();

			this.OnDirty();

			this.OnHeadlightLevelsChanged();
		}

		public async void ExportToRgbaPalAsync() {
			if (this.pal == null) {
				return;
			}

			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = false,
				FileSearchPatterns = new[] { "*.PAL" },
				SelectButtonText = "Export",
				SelectedPathMustExist = true,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Export to 32-bit PAL"
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			using FileStream output = new(path, FileMode.Create, FileAccess.Write, FileShare.None);
			await this.Value.WriteRgbaPalAsync(this.pal, (int)this.colorMapLightLevel.value, output);
		}

		public async void SaveAsync() {
			bool canSave = Directory.Exists(Path.GetDirectoryName(this.filePath));
			if (!canSave) {
				this.SaveAsAsync();
				return;
			}

			// Writing to the stream is loads faster than to the file. Not sure why. Unity thing probably, doesn't happen on .NET 6.
			using MemoryStream mem = new();
			await this.Value.SaveAsync(mem);

			mem.Position = 0;
			using FileStream stream = new(this.filePath, FileMode.Create, FileAccess.Write, FileShare.None);
			await mem.CopyToAsync(stream);

			this.ResetDirty();
		}

		public async void SaveAsAsync() {
			string path = await ResourceEditors.Instance.PickSaveLocationAsync(this.filePath, new[] { "*.CMP" });
			if (string.IsNullOrEmpty(path)) {
				return;
			}
			this.filePath = path;
			this.TabNameChanged?.Invoke(this, new EventArgs());

			bool canSave = Directory.Exists(Path.GetDirectoryName(this.filePath));
			if (!canSave) {
				return;
			}

			this.SaveAsync();
		}

		private bool closeNext;
		private void Update() {
			if (PlayerInput.all[0].currentActionMap.FindAction("Click").WasReleasedThisFrame()) {
				if (this.closeNext) {
					this.dropdown.SetActive(false);
					this.closeNext = false;
				}
				if (this.dropdown.activeInHierarchy) {
					this.closeNext = true;
				}
			}
		}
	}
}
