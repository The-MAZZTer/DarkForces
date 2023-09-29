using MZZT.Data.Binding;
using MZZT.FileFormats;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace MZZT.DarkForces.Showcase {
	public class GenericViewer : Databind<Raw>, IResourceViewer {
		[Header("Raw"), SerializeField]
		private TMP_InputField text;
		[SerializeField]
		private TMP_Text raw;

		public string TabName => this.filePath == null ? "New" : Path.GetFileName(this.filePath);
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
		private Encoding encoding;
		public Task LoadAsync(ResourceEditorResource resource, IFile file) {
			this.filePath = resource?.Path;

			Raw raw = (Raw)file;

			this.Value = raw;

			string text = null;
			byte[] bom = Encoding.UTF8.GetPreamble();
			bool emit = raw.Data.Take(bom.Length).SequenceEqual(bom);

			Encoding encoding = new UTF8Encoding(emit, true);
			try {
				text = encoding.GetString(raw.Data);
			} catch (ArgumentException) {
			}
			if (text == null) {
				bom = Encoding.Unicode.GetPreamble();
				emit = raw.Data.Take(bom.Length).SequenceEqual(bom);
				encoding = new UnicodeEncoding(false, emit, true);
				try {
					text = encoding.GetString(raw.Data);
				} catch (ArgumentException) {
				}
			}
			if (text == null) {
				bom = Encoding.BigEndianUnicode.GetPreamble();
				emit = raw.Data.Take(bom.Length).SequenceEqual(bom);
				encoding = new UnicodeEncoding(true, emit, true);
				try {
					text = encoding.GetString(raw.Data);
				} catch (ArgumentException) {
				}
			}
			/*if (text == null) {
				bom = Encoding.UTF32.GetPreamble();
				emit = raw.Data.Take(bom.Length).SequenceEqual(bom);
				encoding = new UTF32Encoding(false, emit, true);
				try {
					text = encoding.GetString(raw.Data);
				} catch (ArgumentException) {
				}
			}*/
			if (text != null) {
				this.encoding = encoding;
			}

			this.raw.gameObject.SetActive(text == null);
			this.text.text = text;
			this.text.gameObject.SetActive(text != null);

			return Task.CompletedTask;
		}

		public async void SaveAsync() {
			bool canSave = Directory.Exists(Path.GetDirectoryName(this.filePath));
			if (!canSave) {
				this.SaveAsAsync();
				return;
			}

			using (FileStream stream = new(this.filePath, FileMode.Create, FileAccess.Write, FileShare.None)) {
				if (this.encoding == null) {
					// Writing to the stream is loads faster than to the file. Not sure why. Unity thing probably, doesn't happen on .NET 6.
					using MemoryStream mem = new();
					await this.Value.SaveAsync(mem);

					mem.Position = 0;
					await mem.CopyToAsync(stream);
				} else {
					using StreamWriter writer = new(stream, this.encoding);
					await writer.WriteAsync(this.text.text);
				}
			}

			this.ResetDirty();
		}

		public async void SaveAsAsync() {
			string path = await ResourceEditors.Instance.PickSaveLocationAsync(this.filePath, new[] { "*.FILM", "*.FLM" });
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
