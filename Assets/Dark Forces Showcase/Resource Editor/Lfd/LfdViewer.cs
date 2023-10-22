using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using MZZT.FileFormats;
using MZZT.IO.FileProviders;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace MZZT.DarkForces.Showcase {
	class LfdViewer : Databind<LandruFileDirectory>, IResourceViewer {
		[Header("LFD"), SerializeField]
		private DataboundList<FileLocationInfo> list;

		public string TabName => this.filePath == null ? "New LFD" : Path.GetFileName(this.filePath); 
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

			this.Value = (LandruFileDirectory)file;

			this.PopulateList();

			return Task.CompletedTask;
		}

		private void PopulateList() {
			this.list.Value = this.Value.Files.Select(x => new FileLocationInfo() {
				Name = $"{x.name}.{x.type}",
				SourceFile = this.filePath,
				SourceOffset = x.offset,
				Size = x.size,
				ResourcePath = $"{this.filePath}{Path.DirectorySeparatorChar}{x.name}.{x.type}"
			}).OrderBy(x => x.Name).ToList();
		}

		private string lastFolder;
		public async void AddAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = true,
				SelectButtonText = "Add",
				SelectedFileMustExist = true,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Add File to LFD"
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			long size = await FileManager.Instance.GetSizeAsync(path);
			if (size > uint.MaxValue) {
				await DfMessageBox.Instance.ShowAsync("File is too big to add to a LFD.");
				return;
			}

			string name = Path.GetFileName(path);
			name = Path.GetFileNameWithoutExtension(name) + Path.GetExtension(name).ToLower() switch {
				".anm" => ".ANIM",
				".dlt" => ".DELT",
				".flm" => ".FILM",
				".fon" => ".FONT",
				".gmd" => ".GMID",
				".plt" => ".PLTT",
				".voc" => ".VOIC",
				_ => Path.GetExtension(name)
			};

			this.list.Add(new() {
				Name = name,
				SourceFile = path,
				SourceOffset = 0,
				Size = (uint)size,
				ResourcePath = path
			});

			this.OnDirty();
		}

		public async void SaveAsync() {
			bool canSave = FileManager.Instance.FolderExists(Path.GetDirectoryName(this.filePath));
			if (!canSave) {
				this.SaveAsAsync();
				return;
			}

			LandruFileDirectory newLfd = new();
			byte[] buffer = new byte[this.list.Select(x => x.Size).Max()];
			foreach (FileLocationInfo info in this.list.Value) {
				using Stream addStream = await FileManager.Instance.NewFileStreamAsync(info.SourceFile, FileMode.Open, FileAccess.Read, FileShare.Read);
				addStream.Seek(info.SourceOffset, SeekOrigin.Begin);
				await addStream.ReadAsync(buffer, 0, (int)info.Size);
				using MemoryStream addMem = new(buffer, 0, (int)info.Size);
				string ext = Path.GetExtension(info.Name);
				if (ext.Length > 0) {
					ext = ext.Substring(1);
				}
				await newLfd.AddFileAsync(Path.GetFileNameWithoutExtension(info.Name), ext, addMem);
			}
			buffer = null;

			// Writing to the stream is loads faster than to the file. Not sure why. Unity thing probably, doesn't happen on .NET 6.
			using Stream stream = await FileManager.Instance.NewFileStreamAsync(this.filePath, FileMode.Create, FileAccess.Write, FileShare.None);
			if (stream is FileStream) {
				using MemoryStream mem = new();
				await newLfd.SaveAsync(mem);
				mem.Position = 0;
				await mem.CopyToAsync(stream);
			} else {
				await newLfd.SaveAsync(stream);
			}

			this.Value = newLfd;

			this.ResetDirty();

			this.PopulateList();
		}

		public async void SaveAsAsync() {
			string path = await ResourceEditors.Instance.PickSaveLocationAsync(this.filePath, new[] { "*.GOB" });
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

		public async Task ExportAsync(FileLocationInfo info) {
			string sourcePath = info.ResourcePath;

			string ext = Path.GetExtension(sourcePath).TrimStart('.');
			string altExt = ext.ToUpper() switch {
				"ANIM" => "ANM",
				"DELT" => "DLT",
				"FILM" => "FLM",
				"FONT" => "FON",
				"GMID" => "GMD",
				"PLTT" => "PLT",
				"VOIC" => "VOC",
				_ => null
			};

			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = false,
				Filters = new[] {
					altExt != null ? FileBrowser.FileType.Generate($"{ext} File", $"*.{ext}", $"*.{altExt}") :
						FileBrowser.FileType.Generate($"{ext} File", $"*.{ext}"),
					FileBrowser.FileType.AllFiles
				},
				SelectButtonText = "Export",
				SelectedPathMustExist = true,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Export file from LFD",
				ValidateFileName = true
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			Raw file = await DfFileManager.Instance.ReadAsync<Raw>(sourcePath);
			try {
				await DfFileManager.Instance.SaveAsync(file, path);
			} catch (Exception ex) {
				await DfMessageBox.Instance.ShowAsync($"Error saving file: {ex.Message}");
			}
		}
	}
}
