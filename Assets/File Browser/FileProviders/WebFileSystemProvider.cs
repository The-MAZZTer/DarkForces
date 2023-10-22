using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if UNITY_WEBGL
using System.Runtime.InteropServices;
using System.Threading;
#endif
using System.Threading.Tasks;
using UnityEngine;

namespace MZZT.IO.FileSystemProviders {
	public class WebFileSystemProvider : MemoryFileSystemProvider {
		public static WebFileSystemProvider Instance { get; private set; }

		public WebFileSystemProvider() {
			if (Instance == null) {
				Instance = this;
			}

			MemoryFileSystemItem current = this.root;
			current.Children["Uploads"] = new MemoryFileSystemItem() {
				Name = "Uploads",
				Type = FileSystemProviderItemTypes.Folder,
				AllowedOperations = FileShare.Read | FileShare.Write
			};
			current.Children["Downloads"] = new MemoryFileSystemItem() {
				Name = "Downloads",
				Type = FileSystemProviderItemTypes.Folder,
				AllowedOperations = FileShare.Read | FileShare.Write
			};
		}

#if UNITY_WEBGL
		[DllImport("__Internal")]
		private static extern void DeleteDownloadFile(string path); 
		[DllImport("__Internal")]
		private static extern void CreateDownloadFolder(string path);
		[DllImport("__Internal")]
		private static extern void SetDownloadFile(string path, int length);
		[DllImport("__Internal")]
		private static extern void ShowDownload(string path);
#else
		private static void DeleteDownloadFile(string path) => throw new PlatformNotSupportedException();
		private static void CreateDownloadFolder(string path) => throw new PlatformNotSupportedException();
		private static void SetDownloadFile(string path, int length) => throw new PlatformNotSupportedException();
		private static void ShowDownload(string path) => throw new PlatformNotSupportedException();
#endif

		public void OnBrowserUploadedFiles(WebFileUpload[] files) {
			foreach (WebFileUpload file in files) {
				string path = file.Name.Replace('/', Path.DirectorySeparatorChar);
				string folder = Path.GetDirectoryName(path);

				MemoryFileSystemItem current = this.root;
				current = current.Children["Uploads"];
				foreach (string segment in folder.Split(Path.DirectorySeparatorChar.ToString(), StringSplitOptions.RemoveEmptyEntries)) {
					MemoryFileSystemItem child = current.Children.GetValueOrDefault(segment);

					if (child == null) {
						current.Children[segment] = child = new MemoryFileSystemItem() {
							Name = segment,
							Type = FileSystemProviderItemTypes.Folder
						};
					} else if (child.Type == FileSystemProviderItemTypes.File) {
						current = null;
						break;
					}
					current = child;
				}
				if (current == null) {
					continue;
				}

				string filename = Path.GetFileName(path);
				current.Children[filename] = new MemoryFileSystemItem() {
					Name = filename,
					Type = FileSystemProviderItemTypes.File,
					Size = file.Size
				};
			}
		}

		public void OnBrowserDeleteFile(string path) {
			string[] pathPath = path.Split(Path.DirectorySeparatorChar);
			for (int i = pathPath.Length; i > 0; i--) {
				path = string.Join(Path.DirectorySeparatorChar, pathPath[0..i]);
				if (i < pathPath.Length) {
					bool notEmpty = this.EnumerateChildren(Path.Combine($"{Path.DirectorySeparatorChar}Downloads", path)).Any();
					if (notEmpty) {
						break;
					}
				}

				this.Delete(path);
			}
		}

		protected override void OnFileDeleted(MemoryFileSystemItem itemFile, string fullPath) {
			base.OnFileDeleted(itemFile, fullPath);

			string[] segments = fullPath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
			if (segments.FirstOrDefault() == "Downloads") {
				DeleteDownloadFile(string.Join('/', segments.Skip(1)));
			}
		}

		protected override void OnFolderCreated(MemoryFileSystemItem newFolder, string fullPath) {
			base.OnFolderCreated(newFolder, fullPath);

			string[] segments = fullPath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
			if (segments.FirstOrDefault() == "Downloads") {
				CreateDownloadFolder(string.Join('/', segments.Skip(1)));
			}
		}

		protected override string RootName => "Browser";

		protected override async Task<Stream> GetStreamAsync(string path, MemoryFileSystemItem item, FileMode mode, FileAccess access, FileShare share) {
			string[] segments = path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
			if (segments.FirstOrDefault() == "Uploads" && item.Data == null) {
				WebFileSystemUploader loader = new(string.Join('/', segments.Skip(1)), (int)item.Size);
				byte[] data = await loader.LoadAsync();

				item.Data = new(data);
				//return new WebFileProviderStream(this, item, data, path, mode, access, share);
			}
			return new WebFileProviderStream(this, item, path, mode, access, share);
		}

		public void InternalSetDownloadFile(string path, int size) {
			string[] segments = path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
			SetDownloadFile(string.Join('/', segments.Skip(1)), size);
		}

		private async Task<int> AddToZipAsync(ZipOutputStream zip, string path, string pathInZip, MemoryFileSystemItem item) {
			int count = 0;
			foreach (MemoryFileSystemItem child in item.Children.Values) {
				string subPath = Path.Combine(pathInZip, child.Name);

				if (child.Type == FileSystemProviderItemTypes.Folder) {
					count += await this.AddToZipAsync(zip, Path.Combine(path, child.Name), subPath, child);
				} else {
					ZipEntry entry = new(ZipEntry.CleanName(subPath)) {
						Size = child.Data.Length
					};

					zip.PutNextEntry(entry);
					child.Data.Position = 0;
					await child.Data.CopyToAsync(zip);
					zip.CloseEntry();
					child.Data.Position = 0;
					count++;
				}
			}
			return count;
		}

		public async Task OnBrowserDownloadFileAsync(string path) {
			MemoryFileSystemItem file = this.Find(Path.Combine($"{Path.DirectorySeparatorChar}Downloads", path));
			if (file == null) {
				throw new FileNotFoundException();
			}

			if (file.Type == FileSystemProviderItemTypes.File) {
				WebFileSystemDownloader loader = new(path, file.Data.ToArray());
				await loader.SendAsync();
				return;
			}

			using MemoryStream zipStream = new();
			using ZipOutputStream zip = new(zipStream);
			zip.SetLevel(9);

			int count = await this.AddToZipAsync(zip, path, string.Empty, file);

			zip.Finish();

			if (count > 0) {
				WebFileSystemDownloader loader = new(path, zipStream.ToArray());
				await loader.SendAsync();
			}
		}

		public override void ShowInFileManager(string path) {
			string[] segments = path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
			if (segments.FirstOrDefault() == "Downloads") {
				ShowDownload(string.Join('/', segments.Skip(1)));
			}
		}

		private bool disposedValue;
		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				if (disposing) {
					if (Instance == this) {
						Instance = null;
					}
				}

				disposedValue = true;
			}
		}

		public void Dispose() {
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}

	public class WebFileProviderStream : MemoryFileSystemProviderStream {
		public WebFileProviderStream(WebFileSystemProvider owner, MemoryFileSystemItem item, string path, FileMode mode, FileAccess access, FileShare share) :
			base(item, item.Data, mode, access, share) {

			this.owner = owner;
			this.path = path;
			this.streamOwner = false;
		}

		public WebFileProviderStream(WebFileSystemProvider owner, MemoryFileSystemItem item, byte[] data, string path, FileMode mode, FileAccess access, FileShare share) :
			base(item, new MemoryStream(data), mode, access, share) {

			this.owner = owner;
			this.path = path;
			this.streamOwner = true;
		}

		private readonly bool streamOwner;
		private readonly WebFileSystemProvider owner;
		private readonly string path;

		public override void Close() {
			if (this.path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() == "Downloads") {
				this.owner.InternalSetDownloadFile(this.path, (int)this.Stream.Length);
			}

			if (this.streamOwner) {
				this.Stream.Close();
			}
			base.Close();
		}

		protected override void Dispose(bool disposing) {
			base.Dispose(disposing);

			if (this.streamOwner) {
				this.Stream.Dispose();
			}
		}

		public override async ValueTask DisposeAsync() {
			await base.DisposeAsync();

			if (this.streamOwner) {
				await this.Stream.DisposeAsync();
			}
		}
	}

	public class WebFileSystemUploader {
		private static int lastHandle = 0;
		private static readonly Dictionary<int, WebFileSystemUploader> loaders = new();

		private readonly byte[] buffer;
		private readonly int handle;
		private readonly string path;
		private readonly TaskCompletionSource<byte[]> taskSource = new();

		public WebFileSystemUploader(string path, int size) {
			this.handle = ++lastHandle;
			this.path = path;
			this.buffer = new byte[size];
			loaders.Add(handle, this);
		}

		public Task<byte[]> LoadAsync() {
			GetUploadFileContents(this.path, this.handle, this.buffer, this.buffer.Length, OnDataLoaded);
			return taskSource.Task;
		}

		private bool TryCompleteTask(bool success) {
			if (success) {
				return this.taskSource.TrySetResult(this.buffer);
			} else {
				return this.taskSource.TrySetException(new FileNotFoundException(this.path));
			}
		}

		[AOT.MonoPInvokeCallback(typeof(Action<int, bool>))]
		private static void OnDataLoaded(int handle, bool success) {
			try {
				if (!loaders.Remove(handle, out WebFileSystemUploader request)) {
					Debug.LogError("Can't find data loader.");
					return;
				}
				request.TryCompleteTask(success);
			} catch (Exception ex) {
				Debug.LogException(ex);
			}
		}

#if UNITY_WEBGL
		[DllImport("__Internal")]
		private static extern void GetUploadFileContents(string path, int handle, byte[] buffer, int bufferLength, Action<int, bool> callback);
#else
		private static void GetUploadFileContents(string path, int handle, byte[] buffer, int bufferLength, Action<int, bool> callback) => throw new PlatformNotSupportedException();
#endif
	}

	public class WebFileSystemDownloader {
		private static int lastHandle = 0;
		private static readonly Dictionary<int, WebFileSystemDownloader> loaders = new();

		private readonly byte[] buffer;
		private readonly int handle;
		private readonly string path;
		private readonly TaskCompletionSource<bool> taskSource = new();

		public WebFileSystemDownloader(string path, byte[] buffer) {
			this.handle = ++lastHandle;
			this.path = path;
			this.buffer = buffer;
			loaders.Add(handle, this);
		}

		public Task SendAsync() {
			Download(this.path, this.handle, this.buffer, this.buffer.Length, OnDataLoaded);
			return taskSource.Task;
		}

		private bool TryCompleteTask() {
			return this.taskSource.TrySetResult(true);
		}

		[AOT.MonoPInvokeCallback(typeof(Action<int, bool>))]
		private static void OnDataLoaded(int handle) {
			try {
				if (!loaders.Remove(handle, out WebFileSystemDownloader request)) {
					Debug.LogError("Can't find data loader.");
					return;
				}
				request.TryCompleteTask();
			} catch (Exception ex) {
				Debug.LogException(ex);
			}
		}

#if UNITY_WEBGL
		[DllImport("__Internal")]
		private static extern void Download(string path, int handle, byte[] buffer, int length, Action<int> callback);
#else
		private static void Download(string path, int handle, byte[] buffer, int length, Action<int> callback) => throw new PlatformNotSupportedException();
#endif
	}
}
