using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MZZT.IO.FileProviders {
	public interface IVirtualItem {
		string DisplayName { get; }
		string Name { get; }
		long? Size { get; }
		string FullPath { get; }

		Task<bool> ExistsAsync();

		IVirtualFolder Parent { get; }
		FileShare AllowedOperations { get; }
		Task DeleteAsync();
		void ShowInFileManager();
	}

	public interface IVirtualFile : IVirtualItem {
		Task<Stream> OpenAsync(FileMode mode, FileAccess access, FileShare share);
	}

	public interface IVirtualFolder : IVirtualItem {
		IAsyncEnumerable<IVirtualItem> GetChildrenAsync();
		IAsyncEnumerable<IVirtualFile> GetFilesAsync();
		IAsyncEnumerable<IVirtualFolder> GetFoldersAsync();
		bool CanCreateFolder { get; }
		IVirtualItem CreateFolder(string name);
		Task<IVirtualItem> GetChildAsync(string name);
		Task<Stream> OpenChildAsync(string name, FileMode mode, FileAccess access, FileShare share);
	}
}
