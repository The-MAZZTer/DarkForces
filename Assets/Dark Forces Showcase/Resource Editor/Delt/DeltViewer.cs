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
using static MZZT.DarkForces.FileFormats.DfBitmap;
using Color = UnityEngine.Color;
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
				return await DfFileManager.Instance.ReadAsync<T>(path) ?? await ResourceCache.Instance.GetAsync<T>(folder, file);
			}

			return await ResourceCache.Instance.GetAsync<T>(Path.GetDirectoryName(file), Path.GetFileName(file));
		}

		private string lastFolder;
		public async void ImportAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = true,
				AllowNavigateLfd = false,
				Filters = new[] {
					FileBrowser.FileType.Generate("PNG Images", "*.PNG"),
					FileBrowser.FileType.AllFiles
				},
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

			LandruDelt delt = png.ToDelt();
			if (delt == null) {
				await DfMessageBox.Instance.ShowAsync($"Image must be 256 colors or less to import.");
				return;
			}

			this.Value.Width = delt.Width;
			this.Value.Height = delt.Height;
			this.Value.Pixels = delt.Pixels;
			this.Value.Mask = delt.Mask;
			this.Value.OffsetX = delt.OffsetX;
			this.Value.OffsetY = delt.OffsetY;

			this.OnDirty();

			this.RefreshPreview();
		}

		public async void ExportAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = false,
				Filters = new[] {
					FileBrowser.FileType.Generate("Unmasked PNG", "*.PNG"),
					FileBrowser.FileType.AllFiles
				},
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
				using Stream stream = await FileManager.Instance.NewFileStreamAsync(path, FileMode.Create, FileAccess.Write, FileShare.None);
				png.Write(stream);
			} catch (Exception ex) {
				await DfMessageBox.Instance.ShowAsync($"Error saving image: {ex.Message}");
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
			string path = await ResourceEditors.Instance.PickSaveLocationAsync(this.filePath, new[] { "*.FME" });
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
			this.Value.Mask.SetAll(true);

			this.OnDirty();

			this.RefreshPreview();
		}

		public async void ImportMaskAsync() {
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

				if (delt.Width != this.Value.Width || delt.Height != this.Value.Height) {
					await DfMessageBox.Instance.ShowAsync($"Imported DELT must be the same size as the current DELT to import its mask.");
					return;
				}

				this.Value.Mask.SetAll(false);
				this.Value.Mask.Or(delt.Mask);
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

			Png png = this.Value.MaskToPng();
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
			this.Value.Trim();

			this.OnDirty();

			this.RefreshPreview();
		}
	}
}
