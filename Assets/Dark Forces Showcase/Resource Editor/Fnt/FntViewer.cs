using MZZT.DarkForces.Converters;
using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using MZZT.Drawing;
using MZZT.FileFormats;
using MZZT.IO.FileProviders;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;
using Image = UnityEngine.UI.Image;

namespace MZZT.DarkForces.Showcase {
	public class FntViewer : Databind<DfFont>, IResourceViewer {
		[Header("FNT"), SerializeField]
		private Databind currentChar;
		private byte currentIndex;
		[SerializeField]
		private Image preview;
		[SerializeField]
		private TMP_Text previewLabel;

		[SerializeField]
		private Button prev;
		[SerializeField]
		private Button next;
		[SerializeField]
		private Button importChar;
		[SerializeField]
		private Button exportChar;
		[SerializeField]
		private Button deleteChar;

		[SerializeField]
		private TMP_InputField palette;
		[SerializeField]
		private TMP_Text first;
		[SerializeField]
		private TMP_Text last;
		[SerializeField]
		private Button export;
		[SerializeField]
		private Button exportAll;

		public string TabName => this.filePath == null ? "New FNT" : Path.GetFileName(this.filePath);
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

			this.Value = (DfFont)file;

			this.first.text = this.GetCharDescription(this.Value.First);
			this.last.text = this.GetCharDescription((byte)(this.Value.First + this.Value.Characters.Count - 1));

			await this.RefreshPaletteAsync();

			this.SelectCharacter(this.Value.First);
		}

		private DfFont.Character GetCharacter(byte index) {
			if (index < this.Value.First || index >= this.Value.First + this.Value.Characters.Count) {
				return null;
			}

			return this.Value.Characters[index - this.Value.First];
		}

		private void SelectCharacter(byte index) {
			DfFont.Character current = (DfFont.Character)((IDatabind)this.currentChar).Value;
			DfFont.Character c = this.GetCharacter(index);
			if (this.currentIndex == index && c == current) {
				this.RefreshPreview();
				return;
			}

			this.currentIndex = index;
			((IDatabind)this.currentChar).Value = this.GetCharacter(index);
			if (c == current) {
				this.RefreshPreview();
			}
		}

		private void SetCharacters(byte first, DfFont.Character[] chars) {
			if (chars.Length == 0) {
				return;
			}

			byte last = (byte)(first + chars.Length - 1);

			byte oldFirst = this.Value.First;
			byte oldLast = (byte)(this.Value.First + this.Value.Characters.Count - 1);

			byte newFirst;
			byte newLast; 
			if (oldFirst <= oldLast) {
				newFirst = Math.Min(oldFirst, first);
				newLast = Math.Max(oldLast, last);

				int addToStart = oldFirst - newFirst;
				int addToEnd = newLast - oldLast;
				if (addToStart > 0) {
					this.Value.Characters.InsertRange(0, Enumerable.Repeat<DfFont.Character>(null, addToStart).Select(_ => new DfFont.Character() {
						Width = 0,
						Data = Array.Empty<byte>()
					}));
				}
				this.Value.First = newFirst;
				if (addToEnd > 0) {
					this.Value.Characters.AddRange(Enumerable.Repeat<DfFont.Character>(null, addToEnd).Select(_ => new DfFont.Character() {
						Width = 0,
						Data = Array.Empty<byte>()
					}));
				}

			} else {
				newFirst = first;
				newLast = last;

				this.Value.Characters.InsertRange(0, Enumerable.Repeat<DfFont.Character>(null, chars.Length).Select(_ => new DfFont.Character() {
					Width = 0,
					Data = Array.Empty<byte>()
				}));
			}

			ushort newHeight = this.Value.Height;
			for (byte i = 0; i < chars.Length; i++) {
				byte index = (byte)(first + i);
				DfFont.Character c = chars[i];
				byte oldHeight = (byte)(c.Width > 0 ? (c.Data.Length / c.Width) : 0);

				if (oldHeight != newHeight) {
					byte[] a = new byte[newHeight * c.Width];
					for (int x = 0; x < c.Width; x++) {
						Buffer.BlockCopy(c.Data, x * oldHeight + ((newHeight < oldHeight) ? oldHeight - newHeight : 0),
							a, x * newHeight + ((oldHeight < newHeight) ? newHeight - oldHeight : 0),
							Math.Min(oldHeight, newHeight));
					}
					c.Data = a;
				}

				this.Value.Characters[index - newFirst] = c;
			}

			this.OnDirty();

			this.SelectCharacter(first);

			this.first.text = this.GetCharDescription(newFirst);
			this.last.text = this.GetCharDescription(newLast);
		}

		private void SetCharacter(byte index, DfFont.Character c) => this.SetCharacters(index, new[] { c });

		private void ClearCharacter(byte index) {
			if (index == this.Value.First) {
				this.Value.First++;
				this.Value.Characters.RemoveAt(0);

				while (this.Value.Characters.Count > 0 && this.Value.Characters[0].Width == 0) {
					this.Value.First++;
					this.Value.Characters.RemoveAt(0);
				}
			} else if (index == this.Value.First + this.Value.Characters.Count - 1) {
				this.Value.Characters.RemoveAt(this.Value.Characters.Count - 1);

				while (this.Value.Characters.Count > 0 && this.Value.Characters[^1].Width == 0) {
					this.Value.Characters.RemoveAt(this.Value.Characters.Count - 1);
				}
			} else {
				DfFont.Character c = this.Value.Characters[index - this.Value.First];
				c.Width = 0;
				c.Data = Array.Empty<byte>();
			}

			this.OnDirty();

			if (this.currentIndex == index) {
				this.SelectCharacter(index);
			}

			this.first.text = this.GetCharDescription(this.Value.First);
			this.last.text = this.GetCharDescription((byte)(this.Value.First + this.Value.Characters.Count - 1));
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

		private string GetCharDescription(byte index) => $"'{Encoding.UTF8.GetString(new[] { index })}' {index} 0x{index:X2}";

		private void OnCharChanged() {
			DfFont.Character c = (DfFont.Character)((IDatabind)this.currentChar).Value;
			byte index = this.currentIndex;
			byte min = 32;
			byte max = 127;

			this.prev.interactable = index > min;
			this.next.interactable = index < max;
			if (c == null) {
				this.previewLabel.text = $"{this.GetCharDescription(index)} - Not included";
			} else {
				this.previewLabel.text = $"{this.GetCharDescription(index)} - {c.Width}x{this.Value.Height}";
			}
			this.exportChar.interactable = c != null && c.Width > 0 && this.pal != null;
			this.deleteChar.interactable = c != null && c.Width > 0;

			this.export.interactable = this.Value.Characters.Count > 0;
			this.exportAll.interactable = this.Value.Characters.Count > 0;
		}

		public void GoPrevious() {
			byte index = this.currentIndex;
			if (index <= 32) {
				return;
			}

			index--;
			this.SelectCharacter(index);
		}

		public void GoNext() {
			byte index = this.currentIndex;
			if (index >= 127) {
				return;
			}

			index++;
			this.SelectCharacter(index);
		}

		private string lastFolder;
		public async void ImportAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = true,
				AllowNavigateLfd = false,
				Filters = new[] {
					FileBrowser.FileType.Generate("Supported Files", "*.FNT", "*.PNG"),
					FileBrowser.FileType.AllFiles
				},
				SelectButtonText = "Import",
				SelectedFileMustExist = true,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Import 8-bit FNT or PNG"
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			if (Path.GetExtension(path).ToLower() == ".fnt") {
				DfFont fnt;
				try {
					fnt = await DfFileManager.Instance.ReadAsync<DfFont>(path);
				} catch (Exception ex) {
					await DfMessageBox.Instance.ShowAsync($"Error reading FNT: {ex.Message}");
					return;
				}
				if (fnt == null) {
					await DfMessageBox.Instance.ShowAsync($"Error reading FNT.");
					return;
				}

				this.SetCharacters(fnt.First, fnt.Characters.ToArray());
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

				DfFont.Character c = png.ToFntCharacter(this.Value.Height);
				if (c == null) {
					await DfMessageBox.Instance.ShowAsync($"Image must be 256 colors or less to import.");
					return;
				}

				this.SetCharacter(this.currentIndex, c);
			}
		}

		public void Delete() {
			this.ClearCharacter(this.currentIndex);
		}

		private async Task ExportCharacterAsync(DfFont.Character c, string path) {
			if (c == null || c.Width == 0 || this.pal == null || path == null) {
				return;
			}

			Png png = c.ToPng(this.Value.Height, this.pal, true);
			try {
				using Stream stream = await FileManager.Instance.NewFileStreamAsync(path, FileMode.Create, FileAccess.Write, FileShare.None);
				png.Write(stream);
			} catch (Exception ex) {
				await DfMessageBox.Instance.ShowAsync($"Error saving image: {ex.Message}");
			}
		}

		public async void ExportCharacterAsync() {
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
				Title = "Export character to 8-bit PNG",
				ValidateFileName = true
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			await this.ExportCharacterAsync(this.GetCharacter(this.currentIndex), path);
		}

		public async void ExportCharactersAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = false,
				SelectButtonText = "Export",
				SelectFolder = true,
				SelectedPathMustExist = true,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Export characters to 8-bit PNGs",
				ValidateFileName = true
			});
			if (path == null) {
				return;
			}

			this.lastFolder = path;

			for (byte i = this.Value.First; i < this.Value.First + this.Value.Characters.Count; i++) {
				await this.ExportCharacterAsync(this.GetCharacter(i), $"{path}{Path.DirectorySeparatorChar}{i}.PNG");
			}
		}

		public async void ExportAsync() {
			if (this.pal == null) {
				return;
			}

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
				Title = "Export font to 8-bit PNG",
				ValidateFileName = true
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			Png png = this.Value.ToPng(this.pal, true);
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
					FileBrowser.FileType.Generate("Palette Files", "*.PAL"),
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

		public async Task RefreshPaletteAsync() {
			string palette = this.palette.text;
			if (string.IsNullOrWhiteSpace(palette)) {
				palette = "SECBASE";
			}
			palette = Path.Combine(Path.GetDirectoryName(palette), Path.GetFileNameWithoutExtension(palette));
			DfPalette pal = await this.GetFileAsync<DfPalette>(this.filePath, palette + ".PAL");
			if (pal != null) {
				this.pal = pal;

				this.RefreshPreview();
			}

			DfFont.Character c = (DfFont.Character)((IDatabind)this.currentChar).Value;
			this.export.interactable = this.pal != null;
			this.exportAll.interactable = this.pal != null;
			this.exportChar.interactable = c != null && c.Width > 0 && this.pal != null;
		}
		public async void RefreshPaletteUnityAsync() => await this.RefreshPaletteAsync();

		private DfPalette pal;
		
		public void RefreshPreview() {
			DfFont.Character c = (DfFont.Character)((IDatabind)this.currentChar).Value;

			if (c == null || c.Width == 0 || this.pal == null) {
				this.preview.sprite = null;
				this.preview.color = default;
			} else {
				this.preview.sprite = c.ToTexture(this.Value, this.pal, true, false).ToSprite(); ;
				this.preview.color = Color.white;

				this.Thumbnail = this.preview.sprite;
				this.ThumbnailChanged?.Invoke(this, new EventArgs());
			}

			this.OnCharChanged();
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
			string path = await ResourceEditors.Instance.PickSaveLocationAsync(this.filePath, new[] { "*.FNT" });
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

		public void OnHeightChanged() {
			byte height = this.Value.Height;
			foreach (DfFont.Character c in this.Value.Characters) {
				byte oldHeight = (byte)(c.Width > 0 ? (c.Data.Length / c.Width) : 0);
				if (oldHeight != height) {
					byte[] a = new byte[height * c.Width];
					for (int x = 0; x < c.Width; x++) {
						Buffer.BlockCopy(c.Data, x * oldHeight + ((height < oldHeight) ? oldHeight - height : 0),
							a, x * height + ((oldHeight < height) ? height - oldHeight : 0),
							Math.Min(oldHeight, height));
					}
					c.Data = a;
				}
			}

			this.OnDirty();

			this.SelectCharacter(this.currentIndex);
		}
	}
}
