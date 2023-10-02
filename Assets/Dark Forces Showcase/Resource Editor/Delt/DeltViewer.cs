using MZZT.DarkForces.Converters;
using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using MZZT.Drawing;
using MZZT.FileFormats;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MZZT.DarkForces.FileFormats.DfBitmap;
using Color = UnityEngine.Color;
using File = System.IO.File;
using Image = UnityEngine.UI.Image;

namespace MZZT.DarkForces.Showcase {
	public class DeltViewer : Databind<LandruDelt>, IResourceViewer {
		[Header("FME"), SerializeField]
		private Image preview;
		[SerializeField]
		private TMP_InputField palette;

		[SerializeField]
		private Toggle applyOffset;

		[SerializeField]
		private Button export;

		[SerializeField]
		private Button clearMask;
		[SerializeField]
		private Button importMask;
		[SerializeField]
		private Button exportMask;

		public string TabName => this.filePath == null ? "New DELT" : Path.GetFileName(this.filePath);
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

			this.Value = (LandruDelt)file;

			await this.RefreshPaletteAsync();
		}

		private async Task<T> GetFileAsync<T>(string baseFile, string file) where T : File<T>, IDfFile, new() {
			if (Path.GetInvalidPathChars().Intersect(file).Any()) {
				return null;
			}

			if (baseFile != null) {
				string folder = Path.GetDirectoryName(baseFile);
				string path = Path.Combine(folder, file);
				return await DfFile.GetFileFromFolderOrContainerAsync<T>(path) ?? await ResourceCache.Instance.GetAsync<T>(folder, file);
			}

			return await ResourceCache.Instance.GetAsync<T>(Path.GetDirectoryName(file), Path.GetFileName(file));
		}

		private string lastFolder;
		public async void ImportAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = true,
				AllowNavigateLfd = false,
				FileSearchPatterns = new[] { "*.PNG" },
				SelectButtonText = "Import",
				SelectedFileMustExist = true,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Import 8-bit PNG"
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			Png png;
			try {
				using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
				png = new(stream);
			} catch (Exception ex) {
				await DfMessageBox.Instance.ShowAsync($"Error reading file: {ex.Message}");
				return;
			}
			if (png == null) {
				await DfMessageBox.Instance.ShowAsync($"Error reading file.");
				return;
			}

			LandruDelt delt = png.ToDelt();
			if (delt == null) {
				await DfMessageBox.Instance.ShowAsync($"Image must be 256 colors or less to import.");
				return;
			}

			this.Value.Width = delt.Width;
			this.Value.Height = delt.Height;
			this.Value.Pixels = delt.Pixels;

			this.OnDirty();

			this.RefreshPreview();
		}

		public async void ExportAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = false,
				FileSearchPatterns = new[] { "*.PNG" },
				SelectButtonText = "Export",
				SelectedPathMustExist = true,				
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Export to 8-bit Unmasked PNG",
				ValidateFileName = true
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			byte[] bytePalette = this.pltt.ToByteArray();

			Png png = this.Value.ToUnmaskedPng(bytePalette);
			try {
				using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.None);
				png.Write(stream);
			} catch (Exception ex) {
				await DfMessageBox.Instance.ShowAsync($"Error saving image: {ex.Message}");
			}
		}

		public async void BrowsePaletteAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = true,
				FileSearchPatterns = new[] { "*.PLTT", "*.PLT" },
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
					if (File.Exists(folder)) {
						await LandruFileDirectory.ReadAsync(folder, async x => {
							string name = x.Files.FirstOrDefault(x => x.name.ToLower() == file && x.type.ToLower() == "pltt").name;
							if (name == null) {
								name = x.Files.FirstOrDefault(x => x.type.ToLower() == "pltt").name;
							}
							if (name != null) {
								pltt = await x.GetFileAsync<LandruPalette>(name);
							}
						});
					} else if (Directory.Exists(folder)) {
						if (File.Exists(Path.Combine(folder, file + ".PLTT"))) {
							pltt = await LandruPalette.ReadAsync(Path.Combine(folder, file + ".PLTT"));
						}
						if (pltt == null && File.Exists(Path.Combine(folder, file + ".PLT"))) {
							pltt = await LandruPalette.ReadAsync(Path.Combine(folder, file + ".PLT"));
						}
						if (pltt == null) {
							string path = Directory.EnumerateFiles(folder).FirstOrDefault(x => {
								string ext = Path.GetExtension(x).ToLower();
								return ext == ".pltt" || ext == ".plt";
							});
							if (path != null) {
								pltt = await LandruPalette.ReadAsync(path);
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

			this.export.interactable = pltt != null;
		}
		public async void RefreshPaletteUnityAsync() => await this.RefreshPaletteAsync();

		private LandruPalette pltt;

		public void RefreshPreview() {
			if (this.pltt == null || this.Value.Width == 0 || this.Value.Height == 0) {
				this.preview.sprite = null;
				this.preview.color = default;
			} else{
				if (!this.applyOffset.isOn) {
					this.preview.sprite = this.Value.ToTextureWithOffset(this.pltt, false).ToSprite();
				} else {
					this.preview.sprite = this.Value.ToTexture(this.pltt, false).ToSprite();
				}
				this.preview.color = Color.white;

				this.Thumbnail = this.preview.sprite;
				this.ThumbnailChanged?.Invoke(this, new EventArgs());
			}
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
			string path = await ResourceEditors.Instance.PickSaveLocationAsync(this.filePath, new[] { "*.FME" });
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

		public void ClearMask() {
			this.Value.Mask.SetAll(true);

			this.OnDirty();

			this.RefreshPreview();
		}

		public async void ImportMaskAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = true,
				FileSearchPatterns = new[] { "*.DELT", "*.DLT", "*.PNG" },
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
					delt = await DfFile.GetFileFromFolderOrContainerAsync<LandruDelt>(path);
				} catch (Exception ex) {
					await DfMessageBox.Instance.ShowAsync($"Error reading DELT: {ex.Message}");
					return;
				}
				if (delt == null) {
					await DfMessageBox.Instance.ShowAsync($"Error reading DELT.");
					return;
				}

				if (delt.Width != this.Value.Width || delt.Height != this.Value.Height) {
					await DfMessageBox.Instance.ShowAsync($"Imported DELT must be the same size as the current DELT to import its mask.");
					return;
				}

				this.Value.Mask.SetAll(false);
				this.Value.Mask.Or(delt.Mask);
			} else {
				Png png;
				try {
					using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
					png = new(stream);
				} catch (Exception ex) {
					await DfMessageBox.Instance.ShowAsync($"Error reading file: {ex.Message}");
					return;
				}
				if (png == null) {
					await DfMessageBox.Instance.ShowAsync($"Error reading file.");
					return;
				}

				if (png.Width != this.Value.Width || png.Height != this.Value.Height) {
					await DfMessageBox.Instance.ShowAsync($"Image must be the same size as the current DELT to import its mask.");
					return;
				}

				BitArray mask = png.ToDeltMask();
				if (mask == null) {
					await DfMessageBox.Instance.ShowAsync($"Image must have transparency or an alpha channel to import as a mask.");
					return;
				}
				this.Value.Mask = mask;
			}

			this.OnDirty();

			this.RefreshPreview();
		}

		public async void ExportMaskAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = false,
				FileSearchPatterns = new[] { "*.PNG" },
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

			Png png = this.Value.MaskToPng();
			try {
				using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.None);
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
			this.Value.Trim();

			this.OnDirty();

			this.RefreshPreview();
		}
	}
}
