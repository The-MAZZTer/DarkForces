using MZZT.IO.FileSystemProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MZZT.IO.FileProviders {
	public class FileManager : Singleton<FileManager> {
		private IFileSystemProvider provider;
		public IFileSystemProvider Provider {
			get {
				if (this.provider == null) {
#if UNITY_WEBGL && !UNITY_EDITOR
					this.provider = new WebFileSystemProvider();
#else
					this.provider = new PhysicalFileSystemProvider();
#endif
				}
				return this.provider;
			}
			set {
				if (this.provider != null) {
					throw new InvalidOperationException();
				}
				this.provider = value;
			}
		}

		public IVirtualItem GetRoot(VirtualItemTransformHandler transforms = null) {
			FileSystemProviderItemInfo item = this.Provider.GetByPath(string.Empty);
			IVirtualItem ret = transforms?.PerformTransform(this.Provider, item);
			if (ret != null) {
				return ret;
			}
			return item.Type switch {
				FileSystemProviderItemTypes.Folder => new VirtualFolder(this.Provider, item, transforms),
				FileSystemProviderItemTypes.File => new VirtualFile(this.Provider, item, transforms),
				_ => throw new DirectoryNotFoundException("Can't find root folder!"),
			};
		}

		public async IAsyncEnumerable<IVirtualItem> GetHierarchyByPathAsync(string path, VirtualItemTransformHandler transforms = null) {
			IVirtualItem current = this.GetRoot(transforms);
			yield return current;
			if (string.IsNullOrEmpty(path)) {
				yield break;
			}

			foreach (string segment in path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)) {
				if (current is IVirtualFolder folder) {
					current = null;

					IVirtualItem child = await folder.GetChildAsync(segment);
					if (child != null) {
						current = child;
					}
				}

				yield return current;
			}
		}

		public async Task<IVirtualItem> GetByPathAsync(string path, VirtualItemTransformHandler transforms = null) {
			IVirtualItem current = this.GetRoot(transforms);
			if (string.IsNullOrEmpty(path)) {
				return current;
			}

			foreach (string segment in path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)) {
				if (current is IVirtualFolder folder) {
					current = await folder.GetChildAsync(segment);
					if (current == null) {
						break;
					}
				} else {
					current = null;
					break;
				}
			}

			if (current == null) {
				FileSystemProviderItemInfo item = this.Provider.GetByPath(path);
				if (item == null) {
					return null;
				}

				IVirtualItem ret = transforms?.PerformTransform(this.Provider, item);
				if (ret != null) {
					return ret;
				}
				return item.Type switch {
					FileSystemProviderItemTypes.Folder => new VirtualFolder(this.Provider, item, transforms),
					FileSystemProviderItemTypes.File => new VirtualFile(this.Provider, item, transforms),
					_ => null
				};
			}
			return current;
		}

		public FileSystemProviderItemTypes Exists(string path) => this.Provider.Exists(path);
		public bool FileExists(string path) => this.Exists(path) == FileSystemProviderItemTypes.File;
		public bool FolderExists(string path) => this.Exists(path) == FileSystemProviderItemTypes.Folder;

		public async IAsyncEnumerable<string> FolderEnumerateFilesAsync(string path, string wildcard = null, SearchOption option = SearchOption.TopDirectoryOnly) {
			IVirtualItem item = await this.GetByPathAsync(path);
			if (item is not IVirtualFolder folder) {
				yield break;
			}

			Regex wildcardRegex = wildcard != null ? new Regex("^" + Regex.Escape(wildcard).Replace("\\*", ".*").Replace("\\?", ".") + "$", RegexOptions.IgnoreCase) : null;
			await foreach (IVirtualItem child in folder.GetChildrenAsync()) {
				if (child is IVirtualFile childFile) {
					if (wildcardRegex == null || wildcardRegex.IsMatch(childFile.Name)) {
						yield return childFile.FullPath;
					}
				} else if (child is IVirtualFolder childFolder) {
					if (option == SearchOption.AllDirectories) {
						await foreach (string result in this.FolderEnumerateFilesAsync(childFolder.FullPath, wildcard, option)) {
							yield return result;
						}
					}
				}
			}
		}

		public async Task<IVirtualFolder> FolderCreateAsync(string path) {
			IVirtualFolder current = this.GetRoot() as IVirtualFolder;
			string[] parts = path.Split(Path.DirectorySeparatorChar);
			for (int i = 0; i < parts.Length; i++) {
				string segment = parts[i];
				IVirtualItem child = await current.GetChildAsync(segment);
				if (child == null) {
					child = current.CreateFolder(segment);
				}
				current = child as IVirtualFolder;
			}
			return current;
		}

		public void Show(string path) => this.Provider.ShowInFileManager(path);

		public async Task<Stream> NewFileStreamAsync(string path, FileMode mode, FileAccess access, FileShare share) {
			IVirtualItem item = await this.GetByPathAsync(path);
			IVirtualFile file;
			if (item == null) {
				IVirtualFolder folder = await this.GetByPathAsync(Path.GetDirectoryName(path)) as IVirtualFolder;
				if (folder == null) {
					throw new DirectoryNotFoundException();
				}
				return await folder.OpenChildAsync(Path.GetFileName(path), mode, access, share);
			} else {
				file = item as IVirtualFile;
			}
			if (file == null) {
				throw new FileNotFoundException();
			}

			return await file.OpenAsync(mode, access, share);
		}

		public async Task<long> GetSizeAsync(string path) =>
			(await this.GetByPathAsync(path))?.Size ?? 0;
	}
}
