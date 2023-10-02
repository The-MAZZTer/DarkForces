using MZZT.DarkForces.Converters;
using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using MZZT.Drawing;
using MZZT.FileFormats;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;
using Image = UnityEngine.UI.Image;

namespace MZZT.DarkForces.Showcase {
	public class BmViewer : Databind<DfBitmap>, IResourceViewer {
		[Header("BM"), SerializeField]
		private Databind currentPage;
		[SerializeField]
		private Toggle autoCompress;
		[SerializeField]
		private Image preview;
		[SerializeField]
		private TMP_Text previewCount;
		[SerializeField]
		private TMP_InputField palette;

		private Color buttonTextColor;
		[SerializeField]
		private Color buttonDisabledTextColor;

		[SerializeField]
		private Button prev;
		[SerializeField]
		private Button next;
		[SerializeField]
		private Button movePrev;
		[SerializeField]
		private Button moveNext;
		[SerializeField]
		private Button export;
		[SerializeField]
		private Button delete;
		[SerializeField]
		private Slider lightLevel;

		[SerializeField]
		private TMP_Dropdown compression;
		[SerializeField]
		private TMP_InputField framerate;

		public string TabName => this.filePath == null ? "New BM" : Path.GetFileName(this.filePath);
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

			this.Value = (DfBitmap)file;

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
				return await DfFile.GetFileFromFolderOrContainerAsync<T>(path) ?? await ResourceCache.Instance.GetAsync<T>(file);
			}

			return await ResourceCache.Instance.GetAsync<T>(file);
		}

		private void OnPageChanged() {
			if (this.buttonTextColor == default) {
				this.buttonTextColor = this.prev.GetComponentInChildren<TMP_Text>().color;
			}

			DfBitmap.Page page = (DfBitmap.Page)((IDatabind)this.currentPage).Value;

			int index = this.Value.Pages.IndexOf(page);
			int count = this.Value.Pages.Count;
			this.prev.interactable = index > 0;
			this.prev.GetComponentInChildren<TMP_Text>().color = this.prev.interactable ? this.buttonTextColor : this.buttonDisabledTextColor;
			this.movePrev.interactable = index > 0;
			this.movePrev.GetComponentInChildren<TMP_Text>().color = this.movePrev.interactable ? this.buttonTextColor : this.buttonDisabledTextColor;
			this.next.interactable = index < count - 1;
			this.next.GetComponentInChildren<TMP_Text>().color = this.next.interactable ? this.buttonTextColor : this.buttonDisabledTextColor;
			this.moveNext.interactable = index < count - 1;
			this.moveNext.GetComponentInChildren<TMP_Text>().color = this.moveNext.interactable ? this.buttonTextColor : this.buttonDisabledTextColor;
			if (page == null) {
				this.previewCount.text = $"{index + 1} / {count}";
			} else {
				this.previewCount.text = $"{page.Width}x{page.Height} | {index + 1} / {count}";
			}
			this.export.interactable = index >= 0 && this.pal != null;
			this.export.GetComponentInChildren<TMP_Text>().color = this.export.interactable ? this.buttonTextColor : this.buttonDisabledTextColor;
			this.delete.interactable = index >= 0;
			this.delete.GetComponentInChildren<TMP_Text>().color = this.delete.interactable ? this.buttonTextColor : this.buttonDisabledTextColor;

			this.framerate.interactable = count > 1;

			this.compression.interactable = !this.autoCompress.isOn && this.Value.Pages.Count == 1;
		}

		public void GoPrevious() {
			DfBitmap.Page page = (DfBitmap.Page)((IDatabind)this.currentPage).Value;
			int index = this.Value.Pages.IndexOf(page);
			if (index <= 0) {
				return;
			}

			this.userInput = false;
			try {
				((IDatabind)this.currentPage).Value = this.Value.Pages[index - 1];
			} finally {
				this.userInput = true;
			}
		}

		public void GoNext() {
			DfBitmap.Page page = (DfBitmap.Page)((IDatabind)this.currentPage).Value;
			int index = this.Value.Pages.IndexOf(page);
			if (index >= this.Value.Pages.Count - 1) {
				return;
			}

			this.userInput = false;
			try {
				((IDatabind)this.currentPage).Value = this.Value.Pages[index + 1];
			} finally {
				this.userInput = true;
			}
		}

		public void MovePrevious() {
			DfBitmap.Page page = (DfBitmap.Page)((IDatabind)this.currentPage).Value;
			int index = this.Value.Pages.IndexOf(page);
			if (index <= 0) {
				return;
			}

			(this.Value.Pages[index - 1], this.Value.Pages[index]) = (this.Value.Pages[index], this.Value.Pages[index - 1]);
			this.OnDirty();

			this.OnPageChanged();
		}

		public void MoveNext() {
			DfBitmap.Page page = (DfBitmap.Page)((IDatabind)this.currentPage).Value;
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
			DfBitmap.Page page = (DfBitmap.Page)((IDatabind)this.currentPage).Value;
			int index = this.Value.Pages.IndexOf(page);
			if (index < 0) {
				index = 0;
			}
			await this.ImportAsync(index);
		}

		public async void ImportNextAsync() {
			DfBitmap.Page page = (DfBitmap.Page)((IDatabind)this.currentPage).Value;
			int index = this.Value.Pages.IndexOf(page) + 1;
			await this.ImportAsync(index);
		}

		public async Task ImportAsync(int index) {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = true,
				AllowNavigateLfd = false,
				FileSearchPatterns = new[] { "*.BM", "*.PNG"},
				SelectButtonText = "Import",
				SelectedFileMustExist = true,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Import 8-bit BM or PNG"
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			if (Path.GetExtension(path).ToLower() == ".bm") {
				DfBitmap bm;
				try {
					bm = await DfFile.GetFileFromFolderOrContainerAsync<DfBitmap>(path);
				} catch (Exception ex) {
					await DfMessageBox.Instance.ShowAsync($"Error reading BM: {ex.Message}");
					return;
				}
				if (bm == null) {
					await DfMessageBox.Instance.ShowAsync($"Error reading BM.");
					return;
				}

				this.Value.Pages.InsertRange(index, bm.Pages);
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

				DfBitmap.Page page = png.ToBmPage();
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
			DfBitmap.Page page = (DfBitmap.Page)((IDatabind)this.currentPage).Value;
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

		public async void Export() {
			DfBitmap.Page page = (DfBitmap.Page)((IDatabind)this.currentPage).Value;
			if (page == null) {
				return;
			}

			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = false,
				FileSearchPatterns = new[] { "*.PNG" },
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
				bytePalette = this.cmp.ToByteArray(this.pal, (int)this.lightLevel.value, page.Flags.HasFlag(DfBitmap.Flags.Transparent), false);
			} else {
				bytePalette = this.pal.ToByteArray(page.Flags.HasFlag(DfBitmap.Flags.Transparent));
			}

			Png png = page.ToPng(bytePalette);
			try {
				using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.None);
				png.Write(stream);
			} catch (Exception ex) {
				await DfMessageBox.Instance.ShowAsync($"Error saving image: {ex.Message}");
			}
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
			this.palette.text = path;
		}

		public void OnAutoCompressChanged(bool value) {
			this.Value.AutoCompress = value;
			this.compression.interactable = !value && this.Value.Pages.Count == 1;

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

			DfBitmap.Page page = (DfBitmap.Page)((IDatabind)this.currentPage).Value;

			this.export.interactable = page != null && this.pal != null;
			this.lightLevel.interactable = this.cmp != null;
		}
		public async void RefreshPaletteUnityAsync() => await this.RefreshPaletteAsync();

		private DfPalette pal;
		private DfColormap cmp;

		public void RefreshPreview() {
			DfBitmap.Page page = (DfBitmap.Page)((IDatabind)this.currentPage).Value;

			if (page == null || this.pal == null) {
				this.preview.sprite = null;
				this.preview.color = default;
			} else {
				Texture2D texture;
				if (this.cmp != null) {
					texture = page.ToTexture(this.pal, this.cmp, (int)this.lightLevel.value, false, false, false);
				} else {
					texture = page.ToTexture(this.pal, false, false);
				}

				this.preview.sprite = texture.ToSprite();
				this.preview.color = Color.white;

				this.Thumbnail = this.preview.sprite;
				this.ThumbnailChanged?.Invoke(this, new EventArgs());
			}

			this.OnPageChanged();
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
			string path = await ResourceEditors.Instance.PickSaveLocationAsync(this.filePath, new[] { "*.BM" });
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
	}
}
