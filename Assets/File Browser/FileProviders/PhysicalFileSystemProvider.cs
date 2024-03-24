using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;

namespace MZZT.IO.FileSystemProviders {
	public class PhysicalFileSystemProvider : IFileSystemProvider {
		public FileSystemProviderItemTypes Exists(string path) {
			if (File.Exists(path)) {
				return FileSystemProviderItemTypes.File;
			} else if (Directory.Exists(path)) {
				return FileSystemProviderItemTypes.Folder;
			} else {
				return FileSystemProviderItemTypes.None;
			}
		}

		public void Delete(string path) {
			switch (this.Exists(path)) {
				case FileSystemProviderItemTypes.Folder:
					Directory.Delete(path, true);
					break;
				case FileSystemProviderItemTypes.File:
					File.Delete(path);
					break;
				default:
					throw new FileNotFoundException();
			}
		}

		public IEnumerable<FileSystemProviderItemInfo> EnumerateChildren(string path) {
			if (string.IsNullOrEmpty(path)) {
				return this.EnumerateFolders(path);
			}

			try {
				return Directory.EnumerateFileSystemEntries(path).Select(x => this.GetByPath(x)!);
			} catch (IOException) {
				return Enumerable.Empty<FileSystemProviderItemInfo>();
			}
		}

		public IEnumerable<FileSystemProviderItemInfo> EnumerateFiles(string path) {
			if (string.IsNullOrEmpty(path)) {
				return Enumerable.Empty<FileSystemProviderItemInfo>();
			}

			try { 
				return Directory.EnumerateFiles(path).Select(x => this.GetByPath(x)!);
			} catch (IOException) {
				return Enumerable.Empty<FileSystemProviderItemInfo>();
			}
		}

		public IEnumerable<FileSystemProviderItemInfo> EnumerateFolders(string path) {
			if (string.IsNullOrEmpty(path)) {
				bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
				if (isWindows) {
					return DriveInfo.GetDrives().Select(x => this.GetByPath(x.Name)!);
				}

				path = Path.VolumeSeparatorChar.ToString();
			}

			try {
				return Directory.EnumerateDirectories(path).Select(x => new FileSystemProviderItemInfo(
					FileSystemProviderItemTypes.Folder, x));
			} catch (IOException) {
				return Enumerable.Empty<FileSystemProviderItemInfo>();
			}
		}

		public FileSystemProviderItemInfo CreateFolder(string path) {
			Directory.CreateDirectory(path);
			return new FileSystemProviderItemInfo(FileSystemProviderItemTypes.Folder, path);
		}

		public FileSystemProviderItemInfo GetByPath(string path) {
			if (string.IsNullOrEmpty(path)) {
				bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
				return new FileSystemProviderItemInfo(FileSystemProviderItemTypes.Folder, isWindows ? path : Path.VolumeSeparatorChar.ToString()) {
					DisplayName = isWindows ? "Computer" : Path.VolumeSeparatorChar.ToString(),
					AllowedOperations = FileShare.Read | (isWindows ? 0 : (FileShare.Write | FileShare.Delete))
				};
			}

			bool isRoot = Path.GetPathRoot(path) == path;

			FileSystemProviderItemTypes type = isRoot ? FileSystemProviderItemTypes.Folder : this.Exists(path);
			if (type == FileSystemProviderItemTypes.None) {
				return null;
			}

			FileSystemProviderItemInfo info = new(type, path);

			if (!isRoot) {
				if (type == FileSystemProviderItemTypes.File) {
					info.Size = null;
					info.FetchExpensiveFields = x => {
						x.Size = new FileInfo(x.FullPath).Length;
					};
				}
			} else {
				if (!path.EndsWith(Path.DirectorySeparatorChar)) {
					path += Path.DirectorySeparatorChar;
				}
				info.Name = path;
				info.FullPath = path;
				info.DisplayName = null;
				info.Size = null;

				info.FetchExpensiveFields = x => {
					DriveInfo drive = new(x.FullPath);
					string displayName = drive.Name;
					string volumeLabel = null;
					try {
						volumeLabel = drive.VolumeLabel;
					} catch (UnauthorizedAccessException) {
					} catch (IOException) {
					}
					if (volumeLabel != null && volumeLabel != drive.Name) {
						displayName = $"{volumeLabel} ({displayName})";
					}

					x.DisplayName = displayName;

					try {
						x.Size = drive.TotalSize;
					} catch (UnauthorizedAccessException) {
					} catch (IOException) {
					}
				};
			}

			return info;
		}

		public Task<Stream> OpenFileAsync(string path, FileMode mode, FileAccess access, FileShare share) =>
			Task.FromResult<Stream>(File.Open(path, mode, access, share));


		public void ShowInFileManager(string path) {
			switch (this.Exists(path)) {
				case FileSystemProviderItemTypes.File:
					try {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
						Process.Start("explorer.exe", $"/select, \"{path}\"");
#elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
						Process.Start("xdg-open", $"\"{Path.GetDirectoryName(path)}\"");
#endif
					} catch (Exception e) {
						Debug.LogException(e);
					}
					break;
				case FileSystemProviderItemTypes.Folder:
					try {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
						Process.Start(path);
#elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
						Process.Start("xdg-open", $"\"{path}\"");
#endif
					} catch (Exception e) {
						Debug.LogException(e);
					}
					break;
			}
		}

		private bool? isCaseSensitive;
		public bool IsCaseSensitive {
			get {
				if (this.isCaseSensitive == null) {
					try {
						string name = Path.GetTempFileName();
						this.isCaseSensitive = !File.Exists(name.ToLower()) || !File.Exists(name.ToUpper());
						File.Delete(name);
					} catch (IOException) {
						if (this.isCaseSensitive == null) {
							return !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
						}
					}
				}
				return this.isCaseSensitive.Value;
			}
		}
	}
}
