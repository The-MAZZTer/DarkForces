using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using MZZT.FileFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace MZZT.DarkForces.Showcase {
	public class VueViewer : Databind<AutodeskVue>, IResourceViewer {
		[Header("VUE"), SerializeField]
		private VueObjectList vues;
		public VueObjectList Vues => this.vues;
		[SerializeField]
		private VueDetails details;

		public string TabName => this.filePath == null ? "New VUE" : Path.GetFileName(this.filePath);
		public event EventHandler TabNameChanged;

		public Sprite Thumbnail { get; private set; }
		public event EventHandler ThumbnailChanged;

		public void SetThumbnail(Sprite thumbnail) {
			this.Thumbnail = thumbnail;
			this.ThumbnailChanged?.Invoke(this, new EventArgs());
		}

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

			this.Value = (AutodeskVue)file;

			if (this.Value.Vues.Count < 1) {
				this.Value.Vues.Add(new());
			}

			this.OnSelectedItemChanged();
			return Task.CompletedTask;
		}

		public void OnSelectedItemChanged() {
			IDatabind selected = this.vues.SelectedDatabound;
			this.details.Value = (KeyValuePair<string, AutodeskVue.VueObject>)(selected != null ? selected.Value : default(KeyValuePair<string, AutodeskVue.VueObject>));
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
			string path = await ResourceEditors.Instance.PickSaveLocationAsync(this.filePath, new[] { "*.VUE" });
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

		private string lastFolder;
		public async void ImportAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new() {
				AllowNavigateGob = true,
				AllowNavigateLfd = false,
				FileSearchPatterns = new[] { "*.VUE" },
				SelectButtonText = "Import",
				SelectedFileMustExist = true,
				SelectedPathMustExist = true,
				SelectFolder = false,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = $"Import VUE"
			});

			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			AutodeskVue vue = await DfFile.GetFileFromFolderOrContainerAsync<AutodeskVue>(path);
			if (vue == null) {
				await DfMessageBox.Instance.ShowAsync("Could not read file.");
				return;
			}

			foreach ((string key, AutodeskVue.VueObject obj) in vue.Vues[0].Objects) {
				this.Value.Vues[0].Objects[key] = obj;

				this.vues.Invalidate();
			}

			this.OnDirty();
		}
	}
}
