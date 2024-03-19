using MZZT.DarkForces.FileFormats;
using MZZT.DarkForces.IO;
using MZZT.FileFormats;
using MZZT.IO.FileProviders;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace MZZT.DarkForces.Showcase {
	public class DfFileManager : Singleton<DfFileManager> {
		private VirtualItemTransformHandler transforms;

		public async Task<IFile> ReadAsync(string path) {
			Type type = DfFile.DetectFileTypeByName(path);
			if (type == null) {
				return null;
			}

			if (this.transforms == null) {
				this.transforms = new();
				this.transforms.AddTransform(new GobTransform());
				this.transforms.AddTransform(new LfdTransform());
			}

			IVirtualItem item = await FileManager.Instance.GetByPathAsync(path, this.transforms);
			if (item == null) {
				return null;
			}

			if (item is IVirtualFolder folder && item is not VirtualFolder) {
				item = await folder.GetChildAsync(null);
			}

			if (item is IVirtualFile file) {
				// Faster for Unity to load into a memory buffer first before parsing.

				Stream stream = await file.OpenAsync(FileMode.Open, FileAccess.Read, FileShare.Read);
				if (stream is FileStream) {
					MemoryStream mem = new((int)file.Size.Value);
					stream.Position = 0;
					await stream.CopyToAsync(mem);
					mem.Position = 0;
					stream.Dispose();
					stream = mem;
				}
				using (stream) {
					IFile ret = (IFile)Activator.CreateInstance(type);
					await ret.LoadAsync(stream);
					return ret;
				}
			}

			return null;
		}

		public async Task<T> ReadAsync<T>(string path) where T : File<T>, IFile, new() {
			if (this.transforms == null) {
				this.transforms = new();
				this.transforms.AddTransform(new GobTransform());
				this.transforms.AddTransform(new LfdTransform());
			}

			IVirtualItem item = await FileManager.Instance.GetByPathAsync(path, this.transforms);
			if (item == null) {
				return null;
			}

			if (item is IVirtualFolder folder && item is not VirtualFolder) {
				item = await folder.GetChildAsync(null);
			}

			if (item is IVirtualFile file) {
				Stream stream = await file.OpenAsync(FileMode.Open, FileAccess.Read, FileShare.Read);
				if (stream is FileStream) {
					MemoryStream mem = new((int)file.Size.Value);
					stream.Position = 0;
					await stream.CopyToAsync(mem);
					mem.Position = 0;
					stream.Dispose();
					stream = mem;
				}
				using (stream) {
					T ret = new();
					await ret.LoadAsync(stream);
					return ret;
				}
			}

			return null;
		}

		public async Task<LandruFileDirectory> ReadLandruFileDirectoryAsync(string path, Func<LandruFileDirectory, Task> callback) {
			if (this.transforms == null) {
				this.transforms = new();
				this.transforms.AddTransform(new GobTransform());
				this.transforms.AddTransform(new LfdTransform());
			}

			IVirtualItem item = await FileManager.Instance.GetByPathAsync(path, this.transforms);
			if (item == null) {
				return null;
			}

			if (item is IVirtualFolder folder && item is not VirtualFolder) {
				item = await folder.GetChildAsync(null);
			}

			if (item is IVirtualFile file) {
				Stream stream = await file.OpenAsync(FileMode.Open, FileAccess.Read, FileShare.Read);
				if (stream is FileStream) {
					MemoryStream mem = new((int)file.Size.Value);
					stream.Position = 0;
					await stream.CopyToAsync(mem);
					mem.Position = 0;
					stream.Dispose();
					stream = mem;
				}
				using (stream) {
					return await LandruFileDirectory.ReadAsync(stream, callback);
				}
			}

			return null;
		}

		public async Task SaveAsync(IFile data, string path) {
			if (this.transforms == null) {
				this.transforms = new();
				this.transforms.AddTransform(new GobTransform());
				this.transforms.AddTransform(new LfdTransform());
			}

			IVirtualItem item = await FileManager.Instance.GetByPathAsync(path, this.transforms);
			if (item is IVirtualFolder) {
				throw new FileNotFoundException();
			}

			Stream stream;
			if (item is not IVirtualFile file) {
				item = await FileManager.Instance.FolderCreateAsync(Path.GetDirectoryName(path));
				if (item is not IVirtualFolder folder) {
					throw new DirectoryNotFoundException();
				}
				stream = await folder.OpenChildAsync(Path.GetFileName(path), FileMode.Create, FileAccess.Write, FileShare.None);
			} else {
				stream = await file.OpenAsync(FileMode.Create, FileAccess.Write, FileShare.None);
			}

			using (stream) {
				await data.SaveAsync(stream);
			}
		}
	}
}