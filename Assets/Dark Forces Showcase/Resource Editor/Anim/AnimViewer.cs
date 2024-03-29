using MZZT.DarkForces.Converters;
using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using MZZT.Drawing;
using MZZT.FileFormats;
using MZZT.IO.FileProviders;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;
using Image = UnityEngine.UI.Image;

namespace MZZT.DarkForces.Showcase {
	public class AnimViewer : Databind<LandruAnimation>, IResourceViewer {
		[Header("ANIM"), SerializeField]
		private Databind currentPage;
		[SerializeField]
		private Image preview;

		private Color buttonTextColor;
		[SerializeField]
		private Color buttonDisabledTextColor;

		[SerializeField]
		private Button playButton;
		[SerializeField]
		private TMP_Text playImage;
		[SerializeField]
		private TMP_Text pauseImage;
		[SerializeField]
		private Slider frameSlider;
		[SerializeField]
		private float framerate = 10;
		[SerializeField]
		private TMP_Text previewCount;
		[SerializeField]
		private TMP_InputField palette;

		[SerializeField]
		private Button movePrev;
		[SerializeField]
		private Button moveNext;
		[SerializeField]
		private Button export;
		[SerializeField]
		private Button delete;

		[SerializeField]
		private Toggle applyOffset;
		[SerializeField]
		private Button trim;

		[SerializeField]
		private Button clearMask;
		[SerializeField]
		private Button importMask;
		[SerializeField]
		private Button exportMask;

		public string TabName => this.filePath == null ? "New ANIM" : Path.GetFileName(this.filePath);
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

		private bool userInput = true;
		public void OnDirty() {
			if (!this.userInput) {
				return;
			}

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

			this.Value = (LandruAnimation)file;

			await this.RefreshPaletteAsync();

			if (this.Value.Pages.Count > 0) {
				((IDatabind)this.currentPage).Value = this.Value.Pages.First();
			}
		}

		private async Task<T> GetFileAsync<T>(string baseFile, string file) where T : File<T>, IDfFile, new() {
			if (Path.GetInvalidPathChars().Intersect(file).Any()) {
				return null;
			}

			if (baseFile != null) {
				string folder = Path.GetDirectoryName(baseFile);
				string path = Path.Combine(folder, file);
				return await DfFileManager.Instance.ReadAsync<T>(path) ?? await ResourceCache.Instance.GetAsync<T>(folder, file);
			}

			return await ResourceCache.Instance.GetAsync<T>(Path.GetDirectoryName(file), Path.GetFileName(file));
		}

		private void OnPageChanged() {
			if (this.buttonTextColor == default) {
				this.buttonTextColor = this.playButton.GetComponentInChildren<TMP_Text>().color;
			}

			LandruDelt page = (LandruDelt)((IDatabind)this.currentPage).Value;

			int index = this.Value.Pages.IndexOf(page);
			int count = this.Value.Pages.Count;

			this.playButton.interactable = count > 1;
			this.playImage.gameObject.SetActive(!this.playing);
			this.pauseImage.gameObject.SetActive(this.playing);
			this.frameSlider.maxValue = index >= 0 ? count - 1 : 0;
			this.frameSlider.value = index >= 0 ? index : 0;
			this.frameSlider.interactable = index >= 0;
				
			this.movePrev.interactable = index > 0;
			this.movePrev.GetComponentInChildren<TMP_Text>().color = this.movePrev.interactable ? this.buttonTextColor : this.buttonDisabledTextColor;
			this.moveNext.interactable = index < count - 1;
			this.moveNext.GetComponentInChildren<TMP_Text>().color = this.moveNext.interactable ? this.buttonTextColor : this.buttonDisabledTextColor;
			if (page == null) {
				this.previewCount.text = $"{index + 1} / {count}";
			} else {
				this.previewCount.text = $"{page.Width}x{page.Height} | {index + 1} / {count}";
			}
			this.export.interactable = index >= 0;
			this.export.GetComponentInChildren<TMP_Text>().color = this.export.interactable ? this.buttonTextColor : this.buttonDisabledTextColor;
			this.delete.interactable = index >= 0;
			this.delete.GetComponentInChildren<TMP_Text>().color = this.delete.interactable ? this.buttonTextColor : this.buttonDisabledTextColor;
			this.trim.interactable = index >= 0;
			this.trim.GetComponentInChildren<TMP_Text>().color = this.trim.interactable ? this.buttonTextColor : this.buttonDisabledTextColor;

			this.clearMask.interactable = index >= 0;
			this.clearMask.GetComponentInChildren<TMP_Text>().color = this.clearMask.interactable ? this.buttonTextColor : this.buttonDisabledTextColor;
			this.importMask.interactable = index >= 0;
			this.importMask.GetComponentInChildren<TMP_Text>().color = this.importMask.interactable ? this.buttonTextColor : this.buttonDisabledTextColor;
			this.exportMask.interactable = index >= 0;
			this.exportMask.GetComponentInChildren<TMP_Text>().color = this.exportMask.interactable ? this.buttonTextColor : this.buttonDisabledTextColor;
		}

		public void Play() {
			if (this.playing) {
				this.playing = false;
			} else {
				LandruDelt page = (LandruDelt)((IDatabind)this.currentPage).Value;
				int index = this.Value.Pages.IndexOf(page);

				this.startTime = Time.time;
				this.startFrame = index;
				this.playing = true;
			}
		}

		public void OnSliderValueChanged(float value) {
			LandruDelt page = (LandruDelt)((IDatabind)this.currentPage).Value;
			int index = this.Value.Pages.IndexOf(page);

			int frameIndex = (int)value;
			if (frameIndex == index || frameIndex < 0) {
				return;
			}

			int frameCount = this.Value.Pages.Count;
			if (frameIndex >= frameCount) {
				return;
			}

			if (this.playing) {
				this.startTime = Time.time;
				this.startFrame = frameIndex;
			}

			this.userInput = false;
			try {
				((IDatabind)this.currentPage).Value = this.Value.Pages[frameIndex];
			} finally {
				this.userInput = true;
			}
		}

		private bool playing;
		private float startTime;
		private int startFrame;
		private void Update() {
			if (this.playing) {
				int frameCount = this.Value.Pages.Count;
				if (frameCount > 0) {
					float framerate = this.framerate;
					int frameIndex = ((int)(((Time.time - startTime) * framerate)) + startFrame) % frameCount;

					LandruDelt page = (LandruDelt)((IDatabind)this.currentPage).Value;
					int index = this.Value.Pages.IndexOf(page);

					if (frameIndex != index) {
						this.userInput = false;
						try {
							((IDatabind)this.currentPage).Value = this.Value.Pages[frameIndex];
						} finally {
							this.userInput = true;
						}
					}
				}
			}
		}

		public void MovePrevious() {
			LandruDelt page = (LandruDelt)((IDatabind)this.currentPage).Value;
			int index = this.Value.Pages.IndexOf(page);
			if (index <= 0) {
				return;
			}

			(this.Value.Pages[index - 1], this.Value.Pages[index]) = (this.Value.Pages[index], this.Value.Pages[index - 1]);
			this.OnDirty();

			this.OnPageChanged();
		}

		public void MoveNext() {
			LandruDelt page = (LandruDelt)((IDatabind)this.currentPage).Value;
			int index = this.Value.Pages.IndexOf(page);
			if (index >= this.Value.Pages.Count - 1) {
				return;
			}

			(this.Value.Pages[index + 1], this.Value.Pages[index]) = (this.Value.Pages[index], this.Value.Pages[index + 1]);
			this.OnDirty();

			this.OnPageChanged();
		}

		private string lastFolder;
		public async void ImportPreviousAsync() {
			LandruDelt page = (LandruDelt)((IDatabind)this.currentPage).Value;
			int index = this.Value.Pages.IndexOf(page);
			if (index < 0) {
				index = 0;
			}
			await this.ImportAsync(index);
		}

		public async void ImportNextAsync() {
			LandruDelt page = (LandruDelt)((IDatabind)this.currentPage).Value;
			int index = this.Value.Pages.IndexOf(page) + 1;
			await this.ImportAsync(index);
		}

		public async Task ImportAsync(int index) {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = true,
				Filters = new[] {
					FileBrowser.FileType.Generate("Supported Files", "*.ANIM", "*.ANM", "*.DELT", "*.DLT", "*.PNG"),
					FileBrowser.FileType.AllFiles
				},
				SelectButtonText = "Import",
				SelectedFileMustExist = true,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Import 8-bit ANIM, DELT, or PNG"
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);
 
			string ext = Path.GetExtension(path).ToLower();
			if (ext == ".anim" || ext == ".anm") {
				LandruAnimation anim;
				try {
					anim = await DfFileManager.Instance.ReadAsync<LandruAnimation>(path);
				} catch (Exception ex) {
					await DfMessageBox.Instance.ShowAsync($"Error reading AMIM: {ex.Message}");
					return;
				}
				if (anim == null) {
					await DfMessageBox.Instance.ShowAsync($"Error reading ANIM.");
					return;
				}

				this.Value.Pages.InsertRange(index, anim.Pages);
			} else if (ext == ".delt" || ext == ".dlt") {
				LandruDelt delt;
				try {
					delt = await DfFileManager.Instance.ReadAsync<LandruDelt>(path);
				} catch (Exception ex) {
					await DfMessageBox.Instance.ShowAsync($"Error reading DELT: {ex.Message}");
					return;
				}
				if (delt == null) {
					await DfMessageBox.Instance.ShowAsync($"Error reading DELT.");
					return;
				}

				this.Value.Pages.Insert(index, delt);
			} else {
				Png png;
				try {
					using Stream stream = await FileManager.Instance.NewFileStreamAsync(path, FileMode.Open, FileAccess.Read, FileShare.Read);
					png = new(stream);
				} catch (Exception ex) {
					await DfMessageBox.Instance.ShowAsync($"Error reading file: {ex.Message}");
					return;
				}
				if (png == null) {
					await DfMessageBox.Instance.ShowAsync($"Error reading file.");
					return;
				}

				LandruDelt page = png.ToDelt();
				if (page == null) {
					await DfMessageBox.Instance.ShowAsync($"Image must be 256 colors or less to import.");
					return;
				}

				this.Value.Pages.Insert(index, page);
			}

			this.OnDirty();

			this.userInput = false;
			try {
				((IDatabind)this.currentPage).Value = this.Value.Pages[index];
			} finally {
				this.userInput = true;
			}
		}

		public void Delete() {
			LandruDelt page = (LandruDelt)((IDatabind)this.currentPage).Value;
			if (page == null) {
				return;
			}
			int index = this.Value.Pages.IndexOf(page);

			this.Value.Pages.Remove(page);

			this.OnDirty();

			if (index >= this.Value.Pages.Count) {
				index--;
			}

			this.userInput = false;
			try {
				((IDatabind)this.currentPage).Value = index >= 0 ? this.Value.Pages[index] : null;
			} finally {
				this.userInput = true;
			}
		}

		public async void ExportAsync() {
			LandruDelt page = (LandruDelt)((IDatabind)this.currentPage).Value;
			if (page == null) {
				return;
			}

			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = false,
				Filters = new[] {
					FileBrowser.FileType.Generate("Supported Files", "*.DELT", "*.DLT", "*.PNG"),
					FileBrowser.FileType.Generate("Unmasked PNG", "*.PNG"),
					FileBrowser.FileType.Generate("Landru DELT", "*.DELT", "*.DLT"),
					FileBrowser.FileType.AllFiles
				},
				SelectButtonText = "Export",
				SelectedPathMustExist = true,				
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Export to 8-bit DELT or Unmasked PNG",
				ValidateFileName = true
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			int filterIndex = FileBrowser.Instance.FilterIndex;
			if (filterIndex < 1 || filterIndex > 2) {
				filterIndex = (Path.GetExtension(path).ToLower() != ".png") ? 2 : 1;
			}

			if (filterIndex == 2) {
				try {
					await DfFileManager.Instance.SaveAsync(page, path);
				} catch (Exception ex) {
					await DfMessageBox.Instance.ShowAsync($"Error saving image: {ex.Message}");
				}
			} else {
				if (this.pltt == null) {
					await DfMessageBox.Instance.ShowAsync($"You must select a PLTT to export as PNG.");
					return;
				}

				byte[] bytePalette = this.pltt.ToByteArray();

				Png png = page.ToUnmaskedPng(bytePalette);
				try {
					using Stream stream = await FileManager.Instance.NewFileStreamAsync(path, FileMode.Create, FileAccess.Write, FileShare.None);
					png.Write(stream);
				} catch (Exception ex) {
					await DfMessageBox.Instance.ShowAsync($"Error saving image: {ex.Message}");
				}
			}
		}

		public async void BrowsePaletteAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = true,
				Filters = new[] {
					FileBrowser.FileType.Generate("Palette Files", "*.PLTT", "*.PLT"),
					FileBrowser.FileType.AllFiles
				},
				SelectButtonText = "Select",
				SelectedFileMustExist = true,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Select palette"
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			this.palette.text = path;
		}

		public async Task RefreshPaletteAsync() {
			string palette = this.palette.text;
			LandruPalette pltt = null;

			if (this.filePath != null) {
				if (string.IsNullOrWhiteSpace(palette)) {
					string folder = Path.GetDirectoryName(this.filePath);
					string file = Path.GetFileNameWithoutExtension(this.filePath).ToLower();
					if (FileManager.Instance.FileExists(folder)) {
						await DfFileManager.Instance.ReadLandruFileDirectoryAsync(folder, async x => {
							string name = x.Files.FirstOrDefault(x => x.name.ToLower() == file && x.type.ToLower() == "pltt").name;
							if (name == null) {
								name = x.Files.FirstOrDefault(x => x.type.ToLower() == "pltt").name;
							}
							if (name != null) {
								pltt = await x.GetFileAsync<LandruPalette>(name);
							}
						});
					} else if (FileManager.Instance.FolderExists(folder)) {
						if (FileManager.Instance.FileExists(Path.Combine(folder, file + ".PLTT"))) {
							pltt = await DfFileManager.Instance.ReadAsync<LandruPalette>(Path.Combine(folder, file + ".PLTT"));
						}
						if (pltt == null && FileManager.Instance.FileExists(Path.Combine(folder, file + ".PLT"))) {
							pltt = await DfFileManager.Instance.ReadAsync<LandruPalette>(Path.Combine(folder, file + ".PLT"));
						}
						if (pltt == null) {
							string path = null;
							await foreach (string searchFile in FileManager.Instance.FolderEnumerateFilesAsync(folder)) {
								string ext = Path.GetExtension(searchFile).ToLower();
								if (ext == ".pltt" || ext == ".plt") {
									path = searchFile;
									break;
								}
							}
							if (path != null) {
								pltt = await DfFileManager.Instance.ReadAsync<LandruPalette>(path);
							}
						}
					}
				} else {
					pltt = await this.GetFileAsync<LandruPalette>(this.filePath, palette);
				}
			}

			if (pltt != null) {
				this.pltt = pltt;
			}

			this.RefreshPreview();

			LandruDelt page = (LandruDelt)((IDatabind)this.currentPage).Value;

			this.export.interactable = page != null;
		}
		public async void RefreshPaletteUnityAsync() => await this.RefreshPaletteAsync();

		private LandruPalette pltt;

		public void RefreshPreview() {
			LandruDelt page = (LandruDelt)((IDatabind)this.currentPage).Value;

			if (page == null || this.pltt == null) {
				this.preview.sprite = null;
				this.preview.color = default;
			} else {
				if (!this.applyOffset.isOn) {
					this.preview.sprite = page.ToTextureWithOffset(this.pltt, false).ToSprite();
				} else {
					this.preview.sprite = page.ToTexture(this.pltt, false).ToSprite();
				}
				this.preview.color = Color.white;

				this.Thumbnail = this.preview.sprite;
				this.ThumbnailChanged?.Invoke(this, new EventArgs());
			}

			this.OnPageChanged();
		}

		public async void SaveAsync() {
			bool canSave = FileManager.Instance.FolderExists(Path.GetDirectoryName(this.filePath));
			if (!canSave) {
				this.SaveAsAsync();
				return;
			}

			// Writing to the stream is loads faster than to the file. Not sure why. Unity thing probably, doesn't happen on .NET 6.
			using Stream stream = await FileManager.Instance.NewFileStreamAsync(this.filePath, FileMode.Create, FileAccess.Write, FileShare.None);
			if (stream is FileStream) {
				using MemoryStream mem = new();
				await this.Value.SaveAsync(mem);
				mem.Position = 0;
				await mem.CopyToAsync(stream);
			} else {
				await this.Value.SaveAsync(stream);
			}

			this.ResetDirty();
		}

		public async void SaveAsAsync() {
			string path = await ResourceEditors.Instance.PickSaveLocationAsync(this.filePath, new[] { "*.ANIM", "*.ANM" });
			if (string.IsNullOrEmpty(path)) {
				return;
			}
			this.filePath = path;
			this.TabNameChanged?.Invoke(this, new EventArgs());

			bool canSave = FileManager.Instance.FolderExists(Path.GetDirectoryName(this.filePath));
			if (!canSave) {
				return;
			}

			this.SaveAsync();
		}

		public void ClearMask() {
			LandruDelt page = (LandruDelt)((IDatabind)this.currentPage).Value;
			if (page == null) {
				return;
			}

			page.Mask.SetAll(true);

			this.OnDirty();

			this.RefreshPreview();
		}

		public async void ImportMaskAsync() {
			LandruDelt page = (LandruDelt)((IDatabind)this.currentPage).Value;
			if (page == null) {
				return;
			}

			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = true,
				Filters = new[] {
					FileBrowser.FileType.Generate("Supported Files", "*.DELT", "*.DLT", "*.PNG"),
					FileBrowser.FileType.AllFiles
				},
				SelectButtonText = "Import Mask",
				SelectedFileMustExist = true,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Import Mask From DELT or PNG"
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			string ext = Path.GetExtension(path).ToLower();

			if (ext == ".delt" || ext == ".dlt") {
				LandruDelt delt;
				try {
					delt = await DfFileManager.Instance.ReadAsync<LandruDelt>(path);
				} catch (Exception ex) {
					await DfMessageBox.Instance.ShowAsync($"Error reading DELT: {ex.Message}");
					return;
				}
				if (delt == null) {
					await DfMessageBox.Instance.ShowAsync($"Error reading DELT.");
					return;
				}

				if (delt.Width != page.Width || delt.Height != page.Height) {
					await DfMessageBox.Instance.ShowAsync($"DELT must be the same size as the current page to import its mask.");
					return;
				}

				page.Mask.SetAll(false);
				page.Mask.Or(delt.Mask);
			} else {
				Png png;
				try {
					using Stream stream = await FileManager.Instance.NewFileStreamAsync(path, FileMode.Open, FileAccess.Read, FileShare.Read);
					png = new(stream);
				} catch (Exception ex) {
					await DfMessageBox.Instance.ShowAsync($"Error reading file: {ex.Message}");
					return;
				}
				if (png == null) {
					await DfMessageBox.Instance.ShowAsync($"Error reading file.");
					return;
				}

				if (png.Width != page.Width || png.Height != page.Height) {
					await DfMessageBox.Instance.ShowAsync($"Image must be the same size as the current DELT to import its mask.");
					return;
				}

				BitArray mask = png.ToDeltMask();
				if (mask == null) {
					await DfMessageBox.Instance.ShowAsync($"Image must have transparency or an alpha channel to import as a mask.");
					return;
				}
				page.Mask = mask;
			}

			this.OnDirty();

			this.RefreshPreview();
		}

		public async void ExportMaskAsync() {
			LandruDelt page = (LandruDelt)((IDatabind)this.currentPage).Value;
			if (page == null) {
				return;
			}

			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = false,
				Filters = new[] {
					FileBrowser.FileType.Generate("PNG Mask", "*.PNG"),
					FileBrowser.FileType.AllFiles
				},
				SelectButtonText = "Export Mask",
				SelectedPathMustExist = true,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Export Mask to 1-bit PNG",
				ValidateFileName = true
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			Png png = page.MaskToPng();
			try {
				using Stream stream = await FileManager.Instance.NewFileStreamAsync(path, FileMode.Create, FileAccess.Write, FileShare.None);
				png.Write(stream);
			} catch (Exception ex) {
				await DfMessageBox.Instance.ShowAsync($"Error saving image: {ex.Message}");
			}
		}

		public void OnOffsetChanged() {
			this.OnDirty();

			if (!this.applyOffset.isOn) {
				this.RefreshPreview();
			}
		}

		public void Trim() {
			LandruDelt page = (LandruDelt)((IDatabind)this.currentPage).Value;
			if (page == null) {
				return;
			}

			page.Trim();

			this.OnDirty();

			this.RefreshPreview();
		}
	}
}
