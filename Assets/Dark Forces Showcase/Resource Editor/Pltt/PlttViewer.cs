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
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Color = UnityEngine.Color;

namespace MZZT.DarkForces.Showcase {
	public class PlttViewer : Databind<LandruPalette>, IResourceViewer {
		[Header("PLTT"), SerializeField]
		private TMP_InputField first;
		[SerializeField]
		private TMP_InputField last;
		[SerializeField]
		private PaletteList colors;
		[SerializeField]
		private Databind details;
		[SerializeField]
		private Slider r;
		[SerializeField]
		private Slider g;
		[SerializeField]
		private Slider b;

		public string TabName => this.filePath == null ? "New PLTT" : Path.GetFileName(this.filePath);
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

			this.Value = (LandruPalette)file;

			this.first.text = this.Value.First.ToString();
			this.last.text = (this.Value.First + this.Value.Palette.Length - 1).ToString();

			this.PopulatePalette();

			return Task.CompletedTask;
		}

		private void PopulatePalette() {
			Color[] colors = this.Value.ToUnityColorArray();
			this.colors.Clear();
			this.colors.AddRange(colors.Skip(this.Value.First).Take(this.Value.Last - this.Value.First + 1));
			this.colors.SelectedIndex = 0;

			this.GenerateThumbnail();
		}

		private void GenerateThumbnail() {
			byte[] palette = this.Value.ToByteArray();
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

		public void OnColorsSelectedItemChanged() {
			int index = this.colors.SelectedIndex;
			if (index < 0) {
				this.details.gameObject.SetActive(false);
				return;
			}

			((IDatabind)this.details).Value = this.Value.Palette[index];
			this.details.gameObject.SetActive(true);
		}

		public void OnRChanged() {
			int index = this.colors.SelectedIndex;
			if (index < 0) {
				return;
			}

			RgbColor color = this.Value.Palette[index];
			color.R = (byte)this.r.value;
			this.Value.Palette[index] = color;

			this.OnDirty();

			Color unityColor = color.ToUnityColor();
			this.colors.SelectedDatabound.Value = unityColor;

			this.GenerateThumbnail();
		}

		public void OnGChanged() {
			int index = this.colors.SelectedIndex;
			if (index < 0) {
				return;
			}

			RgbColor color = this.Value.Palette[index];
			color.G = (byte)this.g.value;
			this.Value.Palette[index] = color;

			this.OnDirty();

			Color unityColor = color.ToUnityColor();
			this.colors.SelectedDatabound.Value = unityColor;

			this.GenerateThumbnail();
		}

		public void OnBChanged() {
			int index = this.colors.SelectedIndex;
			if (index < 0) {
				return;
			}

			RgbColor color = this.Value.Palette[index];
			color.B = (byte)this.b.value;
			this.Value.Palette[index] = color;

			this.OnDirty();

			Color unityColor = color.ToUnityColor();
			this.colors.SelectedDatabound.Value = unityColor;
			//((IList<Color>)this.colors.Value)[index] = unityColor;

			this.GenerateThumbnail();
		}

		private string lastFolder;
		public async void ExportAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = false,
				Filters = new[] {
					FileBrowser.FileType.Generate("PNG Image", "*.PNG"),
					FileBrowser.FileType.Generate("JASC PAL", "*.PAL"),
					FileBrowser.FileType.Generate("RGB PAL", "*.PAL"),
					FileBrowser.FileType.Generate("RGBA PAL", "*.PAL")
				},
				SelectButtonText = "Export",
				SelectedPathMustExist = true,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Export to PNG"
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			int filterIndex = FileBrowser.Instance.FilterIndex;
			using Stream output = await FileManager.Instance.NewFileStreamAsync(path, FileMode.Create, FileAccess.Write, FileShare.None);
			switch (filterIndex) {
				case 0:
					Png png = this.Value.ToPng();
					try {
						png.Write(output);
					} catch (Exception ex) {
						await DfMessageBox.Instance.ShowAsync($"Error saving image: {ex.Message}");
					}
					break;
				case 1:
					await this.Value.WriteJascPalAsync(output);
					break;
				case 2:
					await this.Value.WriteRgbPalAsync(output);
					break;
				case 3:
					await this.Value.WriteRgbaPalAsync(output);
					break;
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
			string path = await ResourceEditors.Instance.PickSaveLocationAsync(this.filePath, new[] { "*.PLTT", "*.PLT" });
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

		public void OnFirstChanged(string value) {
			byte oldFirst = this.Value.First;
			byte last = this.Value.Last;
			if (byte.TryParse(value, out byte newFirst) && newFirst != oldFirst && newFirst <= last) {
				RgbColor[] newPalette = new RgbColor[last + 1 - newFirst];
				if (newFirst < oldFirst) {
					Array.Copy(this.Value.Palette, 0, newPalette, oldFirst - newFirst, this.Value.Palette.Length);
				} else {
					Array.Copy(this.Value.Palette, newFirst - oldFirst, newPalette, 0, this.Value.Palette.Length - newFirst + oldFirst);
				}
				this.Value.Palette = newPalette;
				this.Value.First = newFirst;

				this.OnDirty();

				this.PopulatePalette();
			}

			this.first.text = this.Value.First.ToString();
		}

		public void OnLastChanged(string value) {
			byte first = this.Value.First;
			byte oldLast = this.Value.Last;
			if (byte.TryParse(value, out byte newLast) && newLast != oldLast && newLast >= first) {
				RgbColor[] newPalette = this.Value.Palette;
				Array.Resize(ref newPalette, newLast - first + 1);
				this.Value.Palette = newPalette;
				this.Value.Last = newLast;

				this.OnDirty();

				this.PopulatePalette();
			}

			this.last.text = this.Value.Last.ToString();
		}
	}
}
