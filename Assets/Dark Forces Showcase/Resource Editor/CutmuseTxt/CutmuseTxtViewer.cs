using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using MZZT.FileFormats;
using MZZT.IO.FileProviders;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace MZZT.DarkForces.Showcase {
	public class CutmuseTxtViewer : Databind<DfCutsceneMusicList>, IResourceViewer {
		[Header("Cutscene Music List"), SerializeField]
		private CutmuseList cutscenes;
		public CutmuseList Cutscenes => this.cutscenes;
		[SerializeField]
		private CutmuseCueList cues;
		[SerializeField]
		private Databind details;
		public Databind Details => this.details;
		[SerializeField]
		private Databind cueDetails;
		public Databind CueDetails => this.cueDetails;

		public string TabName => this.filePath == null ? "New CUTMUSE.TXT" : Path.GetFileName(this.filePath);
		public event EventHandler TabNameChanged;

		public Sprite Thumbnail { get; private set; }
#pragma warning disable CS0067
		public event EventHandler ThumbnailChanged;
#pragma warning restore CS0067

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

		public DfCutsceneList CutsceneList { get; private set; }

		private string filePath;
		public async Task LoadAsync(ResourceEditorResource resource, IFile file) {
			this.filePath = resource?.Path;

			this.CutsceneList = await FileLoader.Instance.LoadGobFileAsync<DfCutsceneList>("CUTSCENE.LST");

			this.Value = (DfCutsceneMusicList)file;

			this.OnSelectedCutsceneChanged();
		}

		public void OnSelectedCutsceneChanged() {
			IDatabind selected = this.cutscenes.SelectedDatabound;
			if (selected == null) {
				this.details.gameObject.SetActive(false);
				return;
			}

			((IDatabind)this.details).MemberName = selected.MemberName;
			this.details.gameObject.SetActive(true);
			((IDatabind)this.details).Invalidate();
		}

		public void OnSelectedCueChanged() {
			IDatabind selected = this.cues.SelectedDatabound;
			if (selected == null) {
				this.cueDetails.gameObject.SetActive(false);
				return;
			}

			((IDatabind)this.cueDetails).MemberName = selected.MemberName;
			this.cueDetails.gameObject.SetActive(true);
			((IDatabind)this.cueDetails).Invalidate();
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
			string path = await ResourceEditors.Instance.PickSaveLocationAsync(this.filePath, new[] { "CUTMUSE.TXT" });
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

		public void OnCueDetailsChanged() {
			this.OnDirty();

			this.cues.SelectedDatabound.Invalidate();
		}
	}
}
