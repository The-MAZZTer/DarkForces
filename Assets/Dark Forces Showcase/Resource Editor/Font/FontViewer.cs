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
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;
using Image = UnityEngine.UI.Image;

namespace MZZT.DarkForces.Showcase {
	public class FontViewer : Databind<LandruFont>, IResourceViewer {
		[Header("FONT"), SerializeField]
		private Databind currentChar;
		private ushort currentIndex;
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
		private Slider r;
		[SerializeField]
		private Slider g;
		[SerializeField]
		private Slider b;
		[SerializeField]
		private TMP_Text first;
		[SerializeField]
		private TMP_Text last;
		[SerializeField]
		private Button export;
		[SerializeField]
		private Button exportAll;

		public string TabName => this.filePath == null ? "New FONT" : Path.GetFileName(this.filePath);
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
		public Task LoadAsync(ResourceEditorResource resource, IFile file) {
			this.filePath = resource?.Path;

			this.Value = (LandruFont)file;

			this.first.text = this.GetCharDescription(this.Value.First);
			this.last.text = this.GetCharDescription((ushort)(this.Value.First + this.Value.Characters.Count - 1));

			this.SelectCharacter(this.Value.First);
			return Task.CompletedTask;
		}

		private LandruFont.Character GetCharacter(ushort index) {
			if (index < this.Value.First || index >= this.Value.First + this.Value.Characters.Count) {
				return null;
			}

			return this.Value.Characters[index - this.Value.First];
		}

		private void SelectCharacter(ushort index) {
			LandruFont.Character current = (LandruFont.Character)((IDatabind)this.currentChar).Value;
			LandruFont.Character c = this.GetCharacter(index);
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

		private void SetCharacters(ushort first, LandruFont.Character[] chars) {
			if (chars.Length == 0) {
				return;
			}

			ushort last = (ushort)(first + chars.Length - 1);

			ushort oldFirst = this.Value.First;
			ushort oldLast = (ushort)(this.Value.First + this.Value.Characters.Count - 1);

			ushort newFirst;
			ushort newLast;
			if (this.Value.Characters.Count > 0) {
				newFirst = Math.Min(oldFirst, first);
				newLast = Math.Max(oldLast, last);

				int addToStart = oldFirst - newFirst;
				int addToEnd = newLast - oldLast;
				if (addToStart > 0) {
					this.Value.Characters.InsertRange(0, Enumerable.Repeat<LandruFont.Character>(null, addToStart).Select(_ => new LandruFont.Character() {
						Width = 0,
						Pixels = new BitArray(0)
					}));
				}
				this.Value.First = newFirst;
				if (addToEnd > 0) {
					this.Value.Characters.AddRange(Enumerable.Repeat<LandruFont.Character>(null, addToEnd).Select(_ => new LandruFont.Character() {
						Width = 0,
						Pixels = new BitArray(0)
					}));
				}
			} else {
				newFirst = first;
				newLast = last;

				this.Value.Characters.InsertRange(0, Enumerable.Repeat<LandruFont.Character>(null, chars.Length).Select(_ => new LandruFont.Character() {
					Width = 0,
					Pixels = new BitArray(0)
				}));
			}

			ushort newHeight = this.Value.Height;
			for (byte i = 0; i < chars.Length; i++) {
				ushort index = (ushort)(first + i);
				LandruFont.Character c = chars[i];
				int stride = Mathf.CeilToInt(c.Width / 8f) * 8;
				ushort oldHeight = (ushort)(c.Width > 0 ? (c.Pixels.Length / stride) : 0);

				if (oldHeight != newHeight) {
					c.Pixels.Length = stride * newHeight;
				}

				this.Value.Characters[index - newFirst] = c;
			}

			this.OnDirty();

			this.SelectCharacter(first);

			this.first.text = this.GetCharDescription(newFirst);
			this.last.text = this.GetCharDescription(newLast);
		}

		private void SetCharacter(ushort index, LandruFont.Character c) => this.SetCharacters(index, new[] { c });

		private void ClearCharacter(ushort index) {
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
				LandruFont.Character c = this.Value.Characters[index - this.Value.First];
				c.Width = 0;
				c.Pixels.Length = 0;
			}

			this.OnDirty();

			if (this.currentIndex == index) {
				this.SelectCharacter(index);
			}

			this.first.text = this.GetCharDescription(this.Value.First);
			this.last.text = this.GetCharDescription((ushort)(this.Value.First + this.Value.Characters.Count - 1));
		}

		private string GetCharDescription(ushort index) => $"'{Encoding.UTF8.GetString(new[] { (byte)index })}' {index} 0x{index:X2}";

		private void OnCharChanged() {
			LandruFont.Character c = (LandruFont.Character)((IDatabind)this.currentChar).Value;
			ushort index = this.currentIndex;
			ushort min = 32;
			ushort max = 127;

			this.prev.interactable = index > min;
			this.next.interactable = index < max;
			if (c == null) {
				this.previewLabel.text = $"{this.GetCharDescription(index)} - Not included";
			} else {
				this.previewLabel.text = $"{this.GetCharDescription(index)} - {c.Width}x{this.Value.Height}";
			}
			this.exportChar.interactable = c != null && c.Width > 0;
			this.deleteChar.interactable = c != null && c.Width > 0;

			this.export.interactable = this.Value.Characters.Count > 0;
			this.exportAll.interactable = this.Value.Characters.Count > 0;
		}

		public void GoPrevious() {
			ushort index = this.currentIndex;
			if (index <= 32) {
				return;
			}

			index--;
			this.SelectCharacter(index);
		}

		public void GoNext() {
			ushort index = this.currentIndex;
			if (index >= 127) {
				return;
			}

			index++;
			this.SelectCharacter(index);
		}

		private string lastFolder;
		public async void ImportAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = true,
				Filters = new[] {
					FileBrowser.FileType.Generate("Supported Files", "*.FON", "*.FONT", "*.PNG"),
					FileBrowser.FileType.AllFiles
				},
				SelectButtonText = "Import",
				SelectedFileMustExist = true,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Import 1-bit FONT or PNG"
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			if (Path.GetExtension(path).ToLower() == ".font" || Path.GetExtension(path).ToLower() == ".fon") {
				LandruFont font;
				try {
					font = await DfFileManager.Instance.ReadAsync<LandruFont>(path);
				} catch (Exception ex) {
					await DfMessageBox.Instance.ShowAsync($"Error reading FONT: {ex.Message}");
					return;
				}
				if (font == null) {
					await DfMessageBox.Instance.ShowAsync($"Error reading FONT.");
					return;
				}

				this.SetCharacters(font.First, font.Characters.ToArray());
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

				LandruFont.Character c = png.ToFontCharacter(this.Value.Height);
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

		private async Task ExportCharacterAsync(LandruFont.Character c, string path) {
			if (c == null || c.Width == 0 || path == null) {
				return;
			}

			Png png = c.ToPng(this.Value.Height, new Color(this.r.value, this.g.value, this.b.value));
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
				Title = "Export character to 1-bit PNG",
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
				Title = "Export characters to 1-bit PNGs",
				ValidateFileName = true
			});
			if (path == null) {
				return;
			}

			this.lastFolder = path;

			for (ushort i = this.Value.First; i < this.Value.First + this.Value.Characters.Count; i++) {
				await this.ExportCharacterAsync(this.GetCharacter(i), $"{path}{Path.DirectorySeparatorChar}{i}.PNG");
			}
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
				Title = "Export font to 1-bit PNG",
				ValidateFileName = true
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			Png png = this.Value.ToPng(new Color(this.r.value, this.g.value, this.b.value));
			try {
				using Stream stream = await FileManager.Instance.NewFileStreamAsync(path, FileMode.Create, FileAccess.Write, FileShare.None);
				png.Write(stream);
			} catch (Exception ex) {
				await DfMessageBox.Instance.ShowAsync($"Error saving image: {ex.Message}");
			}
		}
		
		public void RefreshPreview() {
			LandruFont.Character c = (LandruFont.Character)((IDatabind)this.currentChar).Value;

			if (c == null || c.Width == 0) {
				this.preview.sprite = null;
				this.preview.color = default;
			} else {
				this.preview.sprite = c.ToTexture(this.Value, new Color(this.r.value, this.g.value, this.b.value), false).ToSprite();
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
			string path = await ResourceEditors.Instance.PickSaveLocationAsync(this.filePath, new[] { "*.FONT", "*.FON" });
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
			ushort height = this.Value.Height;
			foreach (LandruFont.Character c in this.Value.Characters) {
				int stride = Mathf.CeilToInt(c.Width / 8f) * 8;
				ushort oldHeight = (ushort)(c.Width > 0 ? (c.Pixels.Length / stride) : 0);
				if (oldHeight != height) {
					c.Pixels.Length = stride * height;
				}
			}

			this.OnDirty();

			this.SelectCharacter(this.currentIndex);
		}
	}
}
