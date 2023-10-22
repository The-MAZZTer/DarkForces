using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MZZT.IO.FileSystemProviders {
	public interface IFileSystemProvider {
		FileSystemProviderItemTypes Exists(string path);
		void Delete(string path);
		IEnumerable<FileSystemProviderItemInfo> EnumerateChildren(string path);
		IEnumerable<FileSystemProviderItemInfo> EnumerateFiles(string path);
		IEnumerable<FileSystemProviderItemInfo> EnumerateFolders(string path);
		FileSystemProviderItemInfo CreateFolder(string path);
		FileSystemProviderItemInfo GetByPath(string path);
		Task<Stream> OpenFileAsync(string path, FileMode mode, FileAccess access, FileShare share);
		void ShowInFileManager(string path);
	}
}
