using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using MZZT.FileFormats;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace MZZT.DarkForces.Showcase {
	public class OViewer : Databind<DfLevelObjects>, IResourceViewer {
		[Header("O"), SerializeField]
		private OList list;
		[SerializeField]
		private Databind details;
		[SerializeField]
		private GameObject fileName;

		public string TabName => this.filePath == null ? "New O" : Path.GetFileName(this.filePath);
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

		private string filePath;
		public Task LoadAsync(ResourceEditorResource resource, IFile file) {
			this.filePath = resource?.Path;

			this.Value = (DfLevelObjects)file;

			this.OnSelectedItemChanged();

			return Task.CompletedTask;
		}

		public void OnSelectedItemChanged() {
			IDatabind selected = this.list.SelectedDatabound;
			if (selected == null) {
				this.details.gameObject.SetActive(false);
				return;
			}

			((IDatabind)this.details).MemberName = selected.MemberName;
			this.details.gameObject.SetActive(true);
			((IDatabind)this.details).Invalidate();

			DfLevelObjects.Object o = ((DfLevelObjects.Object)selected.Value);
			if (o.Type == DfLevelObjects.ObjectTypes.Safe || o.Type == DfLevelObjects.ObjectTypes.Spirit) {
				this.fileName.SetActive(false);
			} else {
				this.fileName.SetActive(true);
			}
		}

		public void OnTypeValueChanged() {
			IDatabind selected = this.list.SelectedDatabound;
			if (selected == null) {
				return;
			}

			DfLevelObjects.Object o = ((DfLevelObjects.Object)selected.Value);
			if (o.Type == DfLevelObjects.ObjectTypes.Safe || o.Type == DfLevelObjects.ObjectTypes.Spirit) {
				this.fileName.SetActive(false);
				o.FileName = null;
			} else {
				this.fileName.SetActive(true);
			}

			this.OnDirty();
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
			string path = await ResourceEditors.Instance.PickSaveLocationAsync(this.filePath, new[] { "*.O" });
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
