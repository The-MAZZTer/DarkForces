using MZZT.DarkForces.FileFormats;
using MZZT.DarkForces.IO;
using MZZT.Data.Binding;
using MZZT.FileFormats;
using MZZT.IO.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace MZZT.DarkForces.Showcase {
	class GobViewer : Databind<DfGobContainer>, IResourceViewer {
		[Header("GOB"), SerializeField]
		private DataboundList<FileLocationInfo> list;

		public string TabName => this.filePath == null ? "New GOB" : Path.GetFileName(this.filePath); 
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

			this.Value = (DfGobContainer)file;

			// Clear any data backing the file since we don't need it.
			this.Value.ClearCachedData();

			this.PopulateList();

			return Task.CompletedTask;
		}

		private void PopulateList() {
			this.list.Value = this.Value.Files.Select(x => new FileLocationInfo() {
				Name = x.name,
				SourceFile = this.filePath,
				SourceOffset = x.offset,
				Size = x.size,
				ResourcePath = $"{this.filePath}{Path.DirectorySeparatorChar}{x.name}"
			}).OrderBy(x => x.Name).ToList();
		}

		private string lastFolder;
		public async void AddAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = true,
				AllowNavigateLfd = false,
				SelectButtonText = "Add",
				SelectedFileMustExist = true,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Add File to GOB"
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			VirtualItemTransformHandler transforms = new();
			transforms.AddTransform(new GobTransform());
			long size = (await FileManager.Instance.GetByPathAsync(path, transforms)).Size.Value;
			if (size > uint.MaxValue) {
				await DfMessageBox.Instance.ShowAsync("File is too big to add to a GOB.");
				return;
			}

			this.list.Add(new() {
				Name = Path.GetFileName(path),
				SourceFile = path,
				SourceOffset = 0,
				Size = (uint)size,
				ResourcePath = path
			});

			this.OnDirty();
		}

		public async void MassAddAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = true,
				AllowNavigateLfd = false,
				SelectFolder = true,
				SelectButtonText = "Add",
				SelectedFileMustExist = true,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Add folder to GOB"
			});
			if (path == null) {
				return;
			}

			this.lastFolder = path;

			VirtualItemTransformHandler transforms = new();
			transforms.AddTransform(new GobTransform());
			IVirtualFolder folder = (IVirtualFolder)await FileManager.Instance.GetByPathAsync(path, transforms);
			List<IVirtualFile> files = new();
			await foreach (IVirtualFile file in folder.GetFilesAsync()) {
				if (file.Size > uint.MaxValue) {
					await DfMessageBox.Instance.ShowAsync($"File {file.Name} is too big to add to a GOB.");
					return;
				}

				files.Add(file);
			}
			this.list.AddRange(files.Select(x => new FileLocationInfo() {
				Name = x.Name,
				SourceFile = x.FullPath,
				SourceOffset = 0,
				Size = (uint)x.Size,
				ResourcePath = x.FullPath
			}));

			this.OnDirty();
		}

		public async void SaveAsync() {
			bool canSave = FileManager.Instance.FolderExists(Path.GetDirectoryName(this.filePath));
			if (!canSave) {
				this.SaveAsAsync();
				return;
			}

			DfGobContainer newGob = new();
			byte[] buffer = new byte[this.list.Select(x => x.Size).Max()];
			foreach (FileLocationInfo info in this.list.Value) {
				using Stream addStream = await FileManager.Instance.NewFileStreamAsync(info.SourceFile, FileMode.Open, FileAccess.Read, FileShare.Read);
				addStream.Seek(info.SourceOffset, SeekOrigin.Begin);
				await addStream.ReadAsync(buffer, 0, (int)info.Size);
				using MemoryStream addMem = new(buffer, 0, (int)info.Size);
				await newGob.AddFileAsync(info.Name, addMem);
			}
			buffer = null;

			// Writing to the stream is loads faster than to the file. Not sure why. Unity thing probably, doesn't happen on .NET 6.
			using Stream stream = await FileManager.Instance.NewFileStreamAsync(this.filePath, FileMode.Create, FileAccess.Write, FileShare.None);
			if (stream is FileStream) {
				using MemoryStream mem = new();
				await newGob.SaveAsync(mem);
				mem.Position = 0;
				await mem.CopyToAsync(stream);
			} else {
				await newGob.SaveAsync(stream);
			}

			this.Value = newGob;

			this.Value.Dispose();

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

			string ext = Path.GetExtension(info.Name).TrimStart('.');

			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = false,
				Filters = new[] {
					FileBrowser.FileType.Generate($"{ext} File", $"*.{ext}"),
					FileBrowser.FileType.AllFiles
				},
				SelectButtonText = "Export",
				SelectedPathMustExist = true,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Export file from GOB",
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

		public async void ExportAllAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = false,
				SelectFolder = true,
				SelectButtonText = "Export",
				SelectedPathMustExist = false,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Export all files from GOB",
				ValidateFileName = true
			});
			if (path == null) {
				return;
			}

			this.lastFolder = path;

			if (!FileManager.Instance.FolderExists(path)) {
				await FileManager.Instance.FolderCreateAsync(path);
			}

			foreach (FileLocationInfo info in this.list) {
				string sourcePath = info.ResourcePath;

				Raw file = await DfFileManager.Instance.ReadAsync<Raw>(sourcePath);
				try {
					await DfFileManager.Instance.SaveAsync(file, Path.Combine(path, info.Name));
				} catch (Exception ex) {
					await DfMessageBox.Instance.ShowAsync($"Error saving file {info.Name}: {ex.Message}");
				}
			}
		}
	}

	class FileLocationInfo {
		public string Name { get; set; }
		public string SourceFile { get; set; }
		public string ResourcePath { get; set; }
		public uint SourceOffset { get; set; }
		public uint Size { get; set; }
		public string SizeText {
			get {
				const string suffixes = "BKMGTPY????????";
				int suffixPos = 0;
				double size = this.Size;
				while (size >= 1024) {
					size /= 1024;
					suffixPos++;
				}
				if (suffixPos == 0) {
					return $"{this.Size} bytes";
				}

				return $"{size:0.00}{suffixes[suffixPos]}B";
			}
		}
	}
}
