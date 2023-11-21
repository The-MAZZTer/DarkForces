using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace MZZT.IO.FileSystemProviders {
	public class UnityWebRequestFileSystemProvider : IFileSystemProvider {
		public FileSystemProviderItemTypes Exists(string path) => throw new PlatformNotSupportedException();
		public void Delete(string path) => throw new PlatformNotSupportedException();

		public IEnumerable<FileSystemProviderItemInfo> EnumerateChildren(string path) => throw new PlatformNotSupportedException();
		public IEnumerable<FileSystemProviderItemInfo> EnumerateFiles(string path) => throw new PlatformNotSupportedException();
		public IEnumerable<FileSystemProviderItemInfo> EnumerateFolders(string path) => throw new PlatformNotSupportedException();

		public FileSystemProviderItemInfo CreateFolder(string path) => throw new PlatformNotSupportedException();

		public FileSystemProviderItemInfo GetByPath(string path) => new(FileSystemProviderItemTypes.File, path) {
			DisplayName = Path.GetFileName(path),
			AllowedOperations = FileShare.Read,
		};

		private static Dictionary<string, byte[]> cache = new();
		public static void ClearFileCache() => cache.Clear();

		public async Task<Stream> OpenFileAsync(string path, FileMode mode, FileAccess access, FileShare share) {
			if (mode != FileMode.Open && mode != FileMode.OpenOrCreate) {
				throw new IOException();
			}
			if (access != FileAccess.Read) {
				throw new IOException();
			}

			if (!cache.TryGetValue(path, out byte[] data)) {
				UnityWebRequest request = UnityWebRequest.Get(path);
				UnityWebRequestAsyncOperation op = request.SendWebRequest();

				while (!op.isDone && Application.isPlaying) {
					await Task.Yield();
				}

				if (!Application.isPlaying) {
					throw new OperationCanceledException();
				}

				if (request.result != UnityWebRequest.Result.Success) {
					throw new IOException(request.error);
				}

				data = request.downloadHandler.data;
				cache[path] = data;
			}

			return new MemoryStream(data);
		}

		public void ShowInFileManager(string path) => throw new PlatformNotSupportedException();
	}
}