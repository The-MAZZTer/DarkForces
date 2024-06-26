using MZZT.DarkForces.Converters;
using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using MZZT.Drawing;
using MZZT.FileFormats;
using MZZT.IO.FileProviders;
using System;
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
	public class FmeViewer : Databind<DfFrame>, IResourceViewer {
		[Header("FME"), SerializeField]
		private Toggle autoCompress;
		[SerializeField]
		private Image preview;
		[SerializeField]
		private TMP_InputField palette;

		[SerializeField]
		private Button import;
		[SerializeField]
		private Button export;
		[SerializeField]
		private Slider lightLevel;

		[SerializeField]
		private Toggle compressed;

		public string TabName => this.filePath == null ? "New FME" : Path.GetFileName(this.filePath);
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

			this.Value = (DfFrame)file;

			await this.RefreshPaletteAsync();
		}

		private async Task<T> GetFileAsync<T>(string baseFile, string file) where T : File<T>, IDfFile, new() {
			if (Path.GetInvalidPathChars().Intersect(file).Any()) {
				return null;
			}

			if (baseFile != null) {
				string folder = Path.GetDirectoryName(baseFile);
				string path = Path.Combine(folder, file);
				return await DfFileManager.Instance.ReadAsync<T>(path) ?? await ResourceCache.Instance.GetAsync<T>(file);
			}

			return await ResourceCache.Instance.GetAsync<T>(file);
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

			DfFrame frame = png.ToFrame();
			if (frame == null) {
				await DfMessageBox.Instance.ShowAsync($"Image must be 256 colors or less to import.");
				return;
			}

			this.Value.Width = frame.Width;
			this.Value.Height = frame.Height;
			this.Value.Pixels = frame.Pixels;

			this.OnDirty();

			this.RefreshPreview();
		}

		public async void ExportAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = false,
				Filters = new[] {
					FileBrowser.FileType.Generate("PNG Image", "*.PNG"),
					FileBrowser.FileType.AllFiles
				},
				SelectButtonText = "Export",
				SelectedPathMustExist = true,				
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Export to 8-bit PNG",
				ValidateFileName = true
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			byte[] bytePalette;
			if (this.cmp != null) {
				bytePalette = this.cmp.ToByteArray(this.pal, (int)this.lightLevel.value, true, false);
			} else {
				bytePalette = this.pal.ToByteArray(false);
			}

			Png png = this.Value.ToPng(bytePalette);
			try {
				using Stream stream = await FileManager.Instance.NewFileStreamAsync(path, FileMode.Create, FileAccess.Write, FileShare.None);
				png.Write(stream);
			} catch (Exception ex) {
				await DfMessageBox.Instance.ShowAsync($"Error saving image: {ex.Message}");
			}
		}

		public async void BrowsePaletteAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = true,
				AllowNavigateLfd = false,
				Filters = new[] {
					FileBrowser.FileType.Generate("Palette Files", "*.PAL", "*.CMP"),
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

			path = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
			this.palette.text = path;
		}

		public void OnAutoCompressChanged(bool value) {
			this.Value.AutoCompress = value;
			this.compressed.interactable = !value;

			this.OnDirty();
		}

		public async Task RefreshPaletteAsync() {
			string palette = this.palette.text;
			if (string.IsNullOrWhiteSpace(palette)) {
				palette = "SECBASE";
			}
			palette = Path.Combine(Path.GetDirectoryName(palette), Path.GetFileNameWithoutExtension(palette));
			DfPalette pal = await this.GetFileAsync<DfPalette>(this.filePath, palette + ".PAL");
			DfColormap cmp = null;
			if (pal != null) {
				cmp = await this.GetFileAsync<DfColormap>(this.filePath, palette + ".CMP");
			}

			if (pal != null) {
				this.pal = pal;
				this.cmp = cmp;

				this.RefreshPreview();
			}

			this.export.interactable = this.pal != null;
			this.lightLevel.interactable = this.cmp != null;
		}
		public async void RefreshPaletteUnityAsync() => await this.RefreshPaletteAsync();

		private DfPalette pal;
		private DfColormap cmp;

		public void RefreshPreview() {
			if (this.pal == null || this.Value.Width == 0 || this.Value.Height == 0) {
				this.preview.sprite = null;
				this.preview.color = default;
			} else {
				Sprite sprite;
				if (this.cmp != null) {
					sprite = this.Value.ToSprite(this.pal, this.cmp, (int)this.lightLevel.value, false, false);
				} else {
					sprite = this.Value.ToSprite(this.pal, false);
				}

				this.preview.sprite = sprite;
				this.preview.color = Color.white;

				this.Thumbnail = sprite;
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
	}
}
