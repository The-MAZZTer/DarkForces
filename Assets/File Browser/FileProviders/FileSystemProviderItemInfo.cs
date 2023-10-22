using System;
using System.IO;

namespace MZZT.IO.FileSystemProviders {
	public enum FileSystemProviderItemTypes {
		None,
		File,
		Folder
	}

	public class FileSystemProviderItemInfo {
		public FileSystemProviderItemInfo(FileSystemProviderItemTypes type, string path) {
			this.Type = type;
			this.FullPath = path ?? string.Empty;
			this.Name = Path.GetFileName(this.FullPath);
			this.DisplayName = this.Name;
		}

		public FileSystemProviderItemTypes Type { get; }
		public string FullPath { get; set; }
		public string Name { get; set; }

		private string displayName;
		public string DisplayName {
			get {
				if (this.displayName == null && this.FetchExpensiveFields	!= null) {
					this.FetchExpensiveFields(this);
					this.FetchExpensiveFields = null;
				}
				return this.displayName;
			}
			set => this.displayName = value;
		}

		private long? size;
		public long? Size {
			get {
				if (this.size == null && this.FetchExpensiveFields != null) {
					this.FetchExpensiveFields(this);
					this.FetchExpensiveFields = null;
				}
				return this.size;
			}
			set => this.size = value;
		}

		public FileShare AllowedOperations { get; set; } = FileShare.Read | FileShare.Write | FileShare.Delete;

		public Action<FileSystemProviderItemInfo> FetchExpensiveFields { get; set; }
	}
}

