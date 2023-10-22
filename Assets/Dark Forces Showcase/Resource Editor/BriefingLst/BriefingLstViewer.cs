using MZZT.DarkForces.Converters;
using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using MZZT.FileFormats;
using MZZT.IO.FileProviders;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
	public class BriefingLstViewer : Databind<DfBriefingList>, IResourceViewer {
		[Header("Briefing List"), SerializeField]
		private BriefingLstBriefings briefings;
		[SerializeField]
		private Databind details;
		[SerializeField]
		private Image preview;

		public string TabName => this.filePath == null ? "New BRIEFING.LST" : Path.GetFileName(this.filePath);
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

			this.Value = (DfBriefingList)file;

			DfBriefingList.Briefing briefing = this.Value.Briefings.FirstOrDefault();
			if (briefing != null) {
				this.Thumbnail = await this.GeneratePreviewAsync(briefing);
				this.ThumbnailChanged?.Invoke(this, new EventArgs());
			}

			await this.OnSelectedBriefingChangedAsync();
		}

		public async Task OnSelectedBriefingChangedAsync() {
			await this.RefreshThumbnailAsync();

			IDatabind selected = this.briefings.SelectedDatabound;
			if (selected == null) {
				this.details.gameObject.SetActive(false);
				return;
			}

			((IDatabind)this.details).MemberName = selected.MemberName;
			this.details.gameObject.SetActive(true);
			((IDatabind)this.details).Invalidate();
		}

		private async Task RefreshThumbnailAsync() {
			this.preview.gameObject.SetActive(false);
			this.preview.sprite = null;

			IDatabind selected = this.briefings.SelectedDatabound;
			if (selected == null) {
				return;
			}

			this.preview.sprite = await this.GeneratePreviewAsync((DfBriefingList.Briefing)selected.Value);
			this.preview.gameObject.SetActive(this.preview.sprite != null);
		}

		public async void RefreshThumbnailUnityCallbackAsync() {
			await this.RefreshThumbnailAsync();
		}

		public async void OnSelectedBriefingChangedUnityCallbackAsync() {
			await this.OnSelectedBriefingChangedAsync();
		}

		private async Task<Sprite> GeneratePreviewAsync(DfBriefingList.Briefing briefing) {
			string lfd = briefing.LfdFile;
			if (string.IsNullOrWhiteSpace(lfd)) {
				return null;
			}

			LandruPalette pltt = null;
			if (!string.IsNullOrWhiteSpace(briefing.PalFile)) {
				pltt = await FileLoader.Instance.LoadLfdFileAsync<LandruPalette>(lfd, briefing.PalFile);
			}

			if (pltt == null) {
				return null;
			}

			LandruAnimation uiElements = null;
			if (!string.IsNullOrWhiteSpace(briefing.AniFile)) {
				uiElements = await FileLoader.Instance.LoadLfdFileAsync<LandruAnimation>(lfd, briefing.AniFile);
			}

			LandruDelt briefingText = null;
			if (!string.IsNullOrWhiteSpace(briefing.Level)) {
				briefingText = await FileLoader.Instance.LoadLfdFileAsync<LandruDelt>(lfd, briefing.Level);
			}

			Texture2D texture = new(320, 200, TextureFormat.RGBA32, false, true) {
#if UNITY_EDITOR
				alphaIsTransparency = true,
#endif
				filterMode = FilterMode.Point
			};

			byte[] raw = texture.GetRawTextureData();

			byte[] palette = pltt.ToByteArray();

			byte[] color = new byte[] { palette[0], palette[1], palette[2], 255 };
			for (int y = 0; y < 200; y++) {
				for (int x = 0; x < 320; x++) {
					Buffer.BlockCopy(color, 0, raw, (y * 320 + x) * 4, 4);
				}
			}

			LandruDelt background = uiElements?.Pages[0];
			if (background != null) {
				Texture2D backgroundTexture = background.ToTextureWithOffset(palette, true);
				try {
					byte[] backgroundRaw = backgroundTexture.GetRawTextureData();
					for (int y = 0; y < backgroundTexture.height; y++) {
						for (int x = 0; x < backgroundTexture.width; x++) {
							if (backgroundRaw[(y * backgroundTexture.width + x) * 4 + 3] != 0) {
								Buffer.BlockCopy(backgroundRaw, (y * backgroundTexture.width + x) * 4, raw, (y * 320 + x) * 4, 4);
							}
						}
					}
				} finally {
					Destroy(backgroundTexture);
				}
			}

			if (briefingText != null) {
				int width = Math.Min(198, briefingText.Width);
				int height = Math.Min(154, briefingText.Height);

				Texture2D briefingTexture = briefingText.ToTexture(palette, true);
				try {
					byte[] briefingRaw = briefingTexture.GetRawTextureData();
					for (int y = 36; y < 36 + height; y++) {
						int srcY = y - 36 + briefingTexture.height - height;
						for (int x = 110; x < 110 + width; x++) {
							int srcX = x - 110;
							if (briefingRaw[(srcY * briefingTexture.width + srcX) * 4 + 3] != 0) {
								Buffer.BlockCopy(briefingRaw, (srcY * briefingTexture.width + srcX) * 4, raw, (y * 320 + x) * 4, 4);
							} 
						}
					}
				} finally {
					Destroy(briefingTexture);
				}
			}

			texture.LoadRawTextureData(raw);
			texture.Apply(true, true);
			return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

			/*SKImageInfo info = new(320, 200);
			using SKSurface surface = SKSurface.Create(info);
			SKCanvas canvas = surface.Canvas;

			byte[] palette = pltt.ToByteArray();

			canvas.Clear(new SKColor(palette[0], palette[1], palette[2]));

			LandruDelt background = uiElements?.Pages[0];
			if (background != null) {
				canvas.DrawImage(background.ToSKImage(palette), new SKPoint(background.OffsetX, background.OffsetY));
			}
			if (briefingText != null) {
				int width = Math.Min(198, briefingText.Width);
				int height = Math.Min(154, briefingText.Height);
				canvas.DrawImage(briefingText.ToSKImage(palette), new SKRect(0, 0, width, height), new SKRect(110, 11, width + 110, height + 11));
			}

			using SKPixmap pixmap = surface.PeekPixels();

			TextureFormat format = (info.ColorType == SKColorType.Rgba8888) ? TextureFormat.RGBA32 : TextureFormat.BGRA32;
			Texture2D texture = new(320, 200, format, false, true) {
#if UNITY_EDITOR
				alphaIsTransparency = true
#endif
			};

			byte[] pixels = new byte[pixmap.BytesSize];
			IntPtr pointer = pixmap.GetPixels();
			for (int y = 0; y < pixmap.Height; y++) {
				Marshal.Copy(pointer + pixmap.RowBytes * (pixmap.Height - y - 1), pixels, pixmap.RowBytes * y, pixmap.RowBytes);
			}

			texture.LoadRawTextureData(pixels);
			texture.Apply(true, true);
			return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));*/
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
			string path = await ResourceEditors.Instance.PickSaveLocationAsync(this.filePath, new[] { "BRIEFING.LST" });
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
