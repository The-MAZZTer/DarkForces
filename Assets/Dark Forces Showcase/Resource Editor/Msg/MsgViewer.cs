using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using MZZT.FileFormats;
using MZZT.IO.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace MZZT.DarkForces.Showcase {
	public class MsgViewer : Databind<DfMessages>, IResourceViewer {
		[Header("MSG"), SerializeField]
		private MsgList list;
		[SerializeField]
		private Databind details;
		public Databind Details => this.details;
		[SerializeField]
		private TMP_InputField id;

		public string TabName => this.filePath == null ? "New MSG" : Path.GetFileName(this.filePath);
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

			this.Value = (DfMessages)file;

			this.OnSelectedMsgChanged();
			return Task.CompletedTask;
		}

		public void OnSelectedMsgChanged() {
			IDatabind selected = this.list.SelectedDatabound;
			if (selected == null) {
				this.details.gameObject.SetActive(false);
				return;
			}

			((IDatabind)this.details).MemberName = selected.MemberName;
			this.details.gameObject.SetActive(true);
			((IDatabind)this.details).Invalidate();

			this.id.text = ((KeyValuePair<int, DfMessages.Message>)selected.Value).Key.ToString();
		}

		public void OnIdChanged(string idStr) {
			int.TryParse(idStr, out int id);

			IDatabind selected = this.list.SelectedDatabound;
			KeyValuePair<int, DfMessages.Message> value = (KeyValuePair<int, DfMessages.Message>)selected.Value;

			if (id == value.Key || id <= 0) {
				return;
			}

			Dictionary<int, DfMessages.Message> dictionary = (Dictionary<int, DfMessages.Message>)this.list.Value;
			if (dictionary.Keys.Except(new[] { value.Key }).Contains(id)) {
				return;
			}

			dictionary.Remove(value.Key);
			dictionary[id] = value.Value;

			this.OnDirty();

			list.Invalidate();
			list.SelectedValue = new KeyValuePair<int, DfMessages.Message>(id, value.Value);
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
			string path = await ResourceEditors.Instance.PickSaveLocationAsync(this.filePath, new[] { "*.MSG" });
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
