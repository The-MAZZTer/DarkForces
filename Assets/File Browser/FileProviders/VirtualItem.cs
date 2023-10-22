using MZZT.IO.FileSystemProviders;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MZZT.IO.FileProviders {
	public abstract class VirtualFileSystemItem {
		protected readonly IFileSystemProvider provider;
		protected readonly VirtualItemTransformHandler transforms;
		protected readonly FileSystemProviderItemInfo data;
		public VirtualFileSystemItem(IFileSystemProvider provider, FileSystemProviderItemInfo data, VirtualItemTransformHandler transforms = null) {
			this.provider = provider;
			this.transforms = transforms;
			this.data = data;
		}
		public VirtualFileSystemItem(IFileSystemProvider provider, string path, VirtualItemTransformHandler transforms = null) :
			this(provider, provider.GetByPath(path), transforms) { }

		public string FullPath => this.data.FullPath;

		public string DisplayName => this.data.DisplayName;
		public string Name => this.data.Name;
		public long? Size => this.data.Size;

		public IVirtualFolder Parent => string.IsNullOrEmpty(this.FullPath) ? null :
			new VirtualFolder(this.provider, Path.GetDirectoryName(this.FullPath), this.transforms);

		public FileShare AllowedOperations => this.data.AllowedOperations;

		public Task DeleteAsync() {
			this.provider.Delete(this.FullPath);
			return Task.CompletedTask;
		}

		public void ShowInFileManager() => this.provider.ShowInFileManager(this.FullPath);
	}

	public class VirtualFile : VirtualFileSystemItem, IVirtualFile {
		public VirtualFile(IFileSystemProvider provider, FileSystemProviderItemInfo data, VirtualItemTransformHandler transforms = null) :
			base(provider, data, transforms) {

			if (this.data?.Type != FileSystemProviderItemTypes.File) {
				throw new FileNotFoundException();
			}
		}
		public VirtualFile(IFileSystemProvider provider, string path, VirtualItemTransformHandler transforms = null) :
			base(provider, path, transforms) {

			if (this.data?.Type != FileSystemProviderItemTypes.File) {
				throw new FileNotFoundException();
			}
		}

		public Task<bool> ExistsAsync() => Task.FromResult(this.provider.Exists(this.data.FullPath) == FileSystemProviderItemTypes.File);

		public async Task<Stream> OpenAsync(FileMode mode, FileAccess access, FileShare share) =>
			await this.provider.OpenFileAsync(this.FullPath, mode, access, share);
	}

	public class VirtualFolder : VirtualFileSystemItem, IVirtualFolder {
		public VirtualFolder(IFileSystemProvider provider, FileSystemProviderItemInfo data, VirtualItemTransformHandler transforms = null) :
			base(provider, data, transforms) {

			if (this.data?.Type != FileSystemProviderItemTypes.Folder) {
				throw new DirectoryNotFoundException();
			}
		}
		public VirtualFolder(IFileSystemProvider provider, string path, VirtualItemTransformHandler transforms = null) :
			base(provider, path, transforms) {

			if (this.data?.Type != FileSystemProviderItemTypes.Folder) {
				throw new DirectoryNotFoundException();
			}
		}

		public Task<bool> ExistsAsync() => Task.FromResult(this.provider.Exists(this.data.FullPath) == FileSystemProviderItemTypes.Folder);

		public async IAsyncEnumerable<IVirtualItem> GetChildrenAsync() {
			foreach (IVirtualItem item in this.provider.EnumerateChildren(this.FullPath).Select(x => {
				IVirtualItem ret = this.transforms?.PerformTransform(this.provider, x);
				if (ret != null) {
					return ret;
				}

				switch (x.Type) {
					case FileSystemProviderItemTypes.File:
						return new VirtualFile(this.provider, x, this.transforms);
					case FileSystemProviderItemTypes.Folder:
						return new VirtualFolder(this.provider, x, this.transforms);
					default:
						throw new InvalidDataException();
				}
			})) {
				yield return item;
			}
			await Task.CompletedTask;
		}
		public async IAsyncEnumerable<IVirtualFile> GetFilesAsync() {
			await foreach (IVirtualItem item in this.GetChildrenAsync()) {
				if (item is IVirtualFile file) {
					yield return file;
				}
			}
		}
		public async IAsyncEnumerable<IVirtualFolder> GetFoldersAsync() {
			await foreach (IVirtualItem item in this.GetChildrenAsync()) {
				if (item is IVirtualFolder folder) {
					yield return folder;
				}
			}
		}

		public bool CanCreateFolder => this.AllowedOperations.HasFlag(FileShare.Write);
		public IVirtualItem CreateFolder(string name) {
			string path = Path.Combine(this.FullPath, name);
			FileSystemProviderItemInfo folder = this.provider.CreateFolder(path);
			IVirtualItem ret = this.transforms?.PerformTransform(this.provider, folder);
			if (ret != null) {
				return ret;
			}
			return new VirtualFolder(this.provider, path, this.transforms);
		}

		public Task<IVirtualItem> GetChildAsync(string name) {
			string path = Path.Combine(this.FullPath, name);
			FileSystemProviderItemInfo file = this.provider.GetByPath(path);
			if (file != null) {
				IVirtualItem ret = this.transforms?.PerformTransform(this.provider, file);
				if (ret != null) {
					return Task.FromResult(ret);
				}
				switch (file.Type) {
					case FileSystemProviderItemTypes.File:
						return Task.FromResult<IVirtualItem>(new VirtualFile(this.provider, file, this.transforms));
					case FileSystemProviderItemTypes.Folder:
						return Task.FromResult<IVirtualItem>(new VirtualFolder(this.provider, file, this.transforms));
					default:
						throw new InvalidDataException();
				}
			}
			return Task.FromResult<IVirtualItem>(null);
		}
		public async Task<Stream> OpenChildAsync(string name, FileMode mode, FileAccess access, FileShare share) =>
			await this.provider.OpenFileAsync(Path.Combine(this.FullPath, name), mode, access, share);
	}
}
