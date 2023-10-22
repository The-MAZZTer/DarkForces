using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;

namespace MZZT.IO.FileSystemProviders {
	public class MemoryFileSystemProvider : IFileSystemProvider {
		protected readonly MemoryFileSystemItem root = new() {
			Type = FileSystemProviderItemTypes.Folder
		};

		protected MemoryFileSystemItem[] FindHierarchy(string path) {
			List<MemoryFileSystemItem> items = new();
			MemoryFileSystemItem current = this.root;
			items.Add(current);

			if (!string.IsNullOrEmpty(path)) {
				foreach (string segment in path.Split(Path.DirectorySeparatorChar.ToString(), StringSplitOptions.RemoveEmptyEntries)) {
					if (current != null) {
						if (current.Type == FileSystemProviderItemTypes.File) {
							Debug.Log(path);
							throw new DirectoryNotFoundException();
						}

						current = current.Children.GetValueOrDefault(segment);
					}
					items.Add(current);
				}
			}

			return items.ToArray();
		}

		protected MemoryFileSystemItem Find(string path) => this.FindHierarchy(path).Last();

		public FileSystemProviderItemTypes Exists(string path) => this.Find(path)?.Type ?? FileSystemProviderItemTypes.None;

		public void Delete(string path) {
			MemoryFileSystemItem[] hierarchy = this.FindHierarchy(path);
			MemoryFileSystemItem item = hierarchy.Last();
			MemoryFileSystemItem parent = hierarchy[^2];
			if (parent == null) {
				throw new DirectoryNotFoundException();
			}

			if (item == null) {
				return;
			}

			if (!!item.AllowedOperations.HasFlag(FileShare.Delete) || !item.Shared.HasFlag(FileShare.Delete)) {
				throw new IOException("That item is not deletable.");
			}

			parent.Children.Remove(item.Name);

			this.OnFileDeleted(item, path);
		}

		protected virtual void OnFileDeleted(MemoryFileSystemItem itemFile, string fullPath) { }

		public IEnumerable<FileSystemProviderItemInfo> EnumerateChildren(string path) {
			MemoryFileSystemItem item = this.Find(path);
			if (item == null || item.Type != FileSystemProviderItemTypes.Folder) {
				throw new DirectoryNotFoundException();
			}

			if (!item.AllowedOperations.HasFlag(FileShare.Read)) {
				throw new IOException("Access denied.");
			}

			return item.Children.Values
				.Select(x => new FileSystemProviderItemInfo(x.Type, Path.Combine(path ?? string.Empty, x.Name)) {
					Size = x.Type == FileSystemProviderItemTypes.File ? x.Size : null,
					AllowedOperations = x.AllowedOperations
				});
		}

		public IEnumerable<FileSystemProviderItemInfo> EnumerateFiles(string path) {
			MemoryFileSystemItem item = this.Find(path);
			if (item == null || item.Type != FileSystemProviderItemTypes.Folder) {
				throw new DirectoryNotFoundException();
			}

			if (!item.AllowedOperations.HasFlag(FileShare.Read)) {
				throw new IOException("Access denied.");
			}

			return item.Children.Values
				.Where(x => x.Type == FileSystemProviderItemTypes.File)
				.Select(x => new FileSystemProviderItemInfo(x.Type, Path.Combine(path ?? string.Empty, x.Name)) {
					Size = x.Size,
					AllowedOperations = x.AllowedOperations
				});
		}

		public IEnumerable<FileSystemProviderItemInfo> EnumerateFolders(string path) {
			MemoryFileSystemItem item = this.Find(path);
			if (item == null || item.Type != FileSystemProviderItemTypes.Folder) {
				throw new DirectoryNotFoundException();
			}

			if (!item.AllowedOperations.HasFlag(FileShare.Read)) {
				throw new IOException("Access denied.");
			}

			return item.Children.Values
				.Where(x => x.Type == FileSystemProviderItemTypes.Folder)
				.Select(x => new FileSystemProviderItemInfo(x.Type, Path.Combine(path ?? string.Empty, x.Name)) {
					AllowedOperations = x.AllowedOperations
				});
		}

		public FileSystemProviderItemInfo CreateFolder(string path) {
			MemoryFileSystemItem current = this.root;
			List<string> pathPath = new();
			foreach (string segment in path.Split(Path.DirectorySeparatorChar.ToString(), StringSplitOptions.RemoveEmptyEntries)) {
				MemoryFileSystemItem child = current.Children.GetValueOrDefault(segment);

				pathPath.Add(segment);

				if (child == null) {
					if (!current.AllowedOperations.HasFlag(FileShare.Write)) {
						throw new IOException("Can't create foldee, container is read-only.");
					}

					current.Children[segment] = child = new MemoryFileSystemItem() {
						Name = segment,
						Type = FileSystemProviderItemTypes.Folder
					};

					this.OnFolderCreated(child, string.Join(Path.DirectorySeparatorChar, pathPath));
				} else if (child.Type == FileSystemProviderItemTypes.File) {
					throw new DirectoryNotFoundException();
				}
				current = child;
			}
			return new FileSystemProviderItemInfo(current.Type, string.Join(Path.DirectorySeparatorChar, pathPath));
		}

		protected virtual void OnFolderCreated(MemoryFileSystemItem newFolder, string fullPath) { }

		protected virtual string RootName => "RAM";
		public FileSystemProviderItemInfo GetByPath(string path) {
			if (string.IsNullOrEmpty(path)) {
				return new FileSystemProviderItemInfo(this.root.Type, path) {
					DisplayName = this.RootName,
					Name = Path.DirectorySeparatorChar.ToString(),
					FullPath = Path.DirectorySeparatorChar.ToString(),
					AllowedOperations = FileShare.Read | FileShare.Write
				};
			}

			MemoryFileSystemItem item = this.Find(path);
			if (item == null) {
				return null;
			}

			if (item.Type == FileSystemProviderItemTypes.File) {
				return new FileSystemProviderItemInfo(item.Type, path) {
					Size = item.Size
				};
			} else {
				return new FileSystemProviderItemInfo(item.Type, path);
			}
		}
		
		public async Task<Stream> OpenFileAsync(string path, FileMode mode, FileAccess access, FileShare share) {
			MemoryFileSystemItem[] hierarchy = this.FindHierarchy(path);
			MemoryFileSystemItem item = hierarchy.Last();
			MemoryFileSystemItem parent = hierarchy[^2];
			if (parent == null) {
				throw new DirectoryNotFoundException();
			}

			FileShare currentShare = item != null ? item.Shared : (FileShare.Read | FileShare.Write | FileShare.Delete);
			if ((currentShare & share) != share) {
				throw new IOException("Unable to acquire requested exclusive rights on file.");
			}
			FileShare allowedOperations = item != null ? item.AllowedOperations : parent.AllowedOperations;
			if ((access.HasFlag(FileAccess.Read) && !allowedOperations.HasFlag(FileShare.Read)) ||
				(access.HasFlag(FileAccess.Write) && !allowedOperations.HasFlag(FileShare.Write))) {

				throw new IOException("Access denied.");
			}

			string name = item?.Name ?? Path.GetFileName(path);

			switch (mode) {
				case FileMode.Append:
				case FileMode.Open:
					if (item == null) {
						throw new FileNotFoundException();
					}
					break;
				case FileMode.Create:
					if (item == null) {
						parent.Children[name] = item = new MemoryFileSystemItem() {
							Name = name,
							Type = FileSystemProviderItemTypes.File,
							Size = 0
						};
					} else {
						item.Data?.Dispose();
					}
					item.Data = new();
					break;
				case FileMode.CreateNew:
					if (item != null) {
						throw new IOException("File exists, can't create.");
					}
					parent.Children[name] = item = new MemoryFileSystemItem() {
						Name = name,
						Type = FileSystemProviderItemTypes.File,
						Size = 0,
						Data = new()
					};
					break;
				case FileMode.OpenOrCreate:
					if (item == null) {
						parent.Children[name] = item = new MemoryFileSystemItem() {
							Name = name,
							Type = FileSystemProviderItemTypes.File,
							Size = 0
						};
					}
					break;
				case FileMode.Truncate:
					if (item == null) {
						throw new FileNotFoundException();
					}
					item.Data?.Dispose();
					item.Data = new();
					break;
				default:
					throw new InvalidOperationException("Invalid file mode.");
			}

			return await this.GetStreamAsync(path, item, mode, access, share);
		}

		protected virtual Task<Stream> GetStreamAsync(string path, MemoryFileSystemItem item, FileMode mode, FileAccess access, FileShare share) =>
			Task.FromResult<Stream>(new MemoryFileSystemProviderStream(item, item.Data, mode, access, share));

		public virtual void ShowInFileManager(string path) { }
	}

	public class MemoryFileSystemItem : IDisposable {
		public string Name { get; set; } = string.Empty;
		public FileSystemProviderItemTypes Type { get; set; }
		public long Size { get; set; }
		public Dictionary<string, MemoryFileSystemItem> Children { get; } = new();
		public FileShare Shared { get; set; } = FileShare.Read | FileShare.Write | FileShare.Delete;
		public FileShare AllowedOperations { get; set; } = FileShare.Read | FileShare.Write | FileShare.Delete;
		public MemoryStream Data { get; set; }

		private bool disposedValue;
		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				if (disposing) {
					if (this.Data != null) {
						this.Data.Dispose();
					}

					foreach (MemoryFileSystemItem child in this.Children.Values) {
						child.Dispose();
					}
				}

				disposedValue = true;
			}
		}

		public void Dispose() {
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}

	public class MemoryFileSystemProviderStream : Stream {
		private readonly MemoryFileSystemItem item;
		private readonly FileAccess access;
		private readonly FileShare shareLock;

		public MemoryFileSystemProviderStream(MemoryFileSystemItem item, Stream stream, FileMode mode, FileAccess access, FileShare share) {
			this.item = item;
			this.Stream = stream;
			this.access = access;

			this.shareLock = item.Shared ^ share;
			item.Shared = share;

			this.position = mode == FileMode.Append ? this.Length : 0;
		}
		protected Stream Stream { get; set; }
		private long position = 0;

		public async override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) {
			long position = this.Stream.Position;
			this.Stream.Seek(this.position, SeekOrigin.Begin);
			try {
				await this.Stream.CopyToAsync(destination, bufferSize, cancellationToken);
			} finally {
				this.position = this.Stream.Position;
				this.Stream.Seek(position, SeekOrigin.Begin);
			}
		}

		public override void Flush() => this.Stream.Flush();
		public override async Task FlushAsync(CancellationToken cancellationToken) => await this.Stream.FlushAsync(cancellationToken);

		public override int Read(Span<byte> buffer) {
			if (!this.CanRead) {
				throw new InvalidOperationException("Can't read stream; stream is not readable.");
			}

			long position = this.Stream.Position;
			this.Stream.Seek(this.position, SeekOrigin.Begin);
			try {
				return this.Stream.Read(buffer);
			} finally {
				this.position = this.Stream.Position;
				this.Stream.Seek(position, SeekOrigin.Begin);
			}
		}

		public override int Read(byte[] buffer, int offset, int count) {
			if (!this.CanRead) {
				throw new InvalidOperationException("Can't read stream; stream is not readable.");
			}
			long position = this.Stream.Position;
			this.Stream.Seek(this.position, SeekOrigin.Begin);
			try {
				return this.Stream.Read(buffer, offset, count);
			} finally {
				this.position = this.Stream.Position;
				this.Stream.Seek(position, SeekOrigin.Begin);
			}
		}

		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
			if (!this.CanRead) {
				throw new InvalidOperationException("Can't read stream; stream is not readable.");
			}
			long position = this.Stream.Position;
			this.Stream.Seek(this.position, SeekOrigin.Begin);
			try {
				return await this.Stream.ReadAsync(buffer, offset, count, cancellationToken);
			} finally {
				this.position = this.Stream.Position;
				this.Stream.Seek(position, SeekOrigin.Begin);
			}
		}

		public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) {
			if (!this.CanRead) {
				throw new InvalidOperationException("Can't read stream; stream is not readable.");
			}
			long position = this.Stream.Position;
			this.Stream.Seek(this.position, SeekOrigin.Begin);
			try {
				return await this.Stream.ReadAsync(buffer, cancellationToken);
			} finally {
				this.position = this.Stream.Position;
				this.Stream.Seek(position, SeekOrigin.Begin);
			}
		}

		public override int ReadByte() {
			if (!this.CanRead) {
				throw new InvalidOperationException("Can't read stream; stream is not readable.");
			}
			long position = this.Stream.Position;
			this.Stream.Seek(this.position, SeekOrigin.Begin);
			try {
				return this.Stream.ReadByte();
			} finally {
				this.position = this.Stream.Position;
				this.Stream.Seek(position, SeekOrigin.Begin);
			}
		}

		public override long Seek(long offset, SeekOrigin origin) {
			switch (origin) {
				case SeekOrigin.Begin:
					this.position = offset;
					break;
				case SeekOrigin.Current:
					this.position += offset;
					break;
				case SeekOrigin.End:
					this.position = this.Length + offset;
					break;
			}

			this.position = Math.Clamp(this.position, 0, this.Length);

			return this.position;
		}

		public override void SetLength(long value) {
			if (!this.CanWrite) {
				throw new InvalidOperationException("Can't set stream length; stream is not writable.");
			}
			this.Stream.SetLength(value);
			this.item.Size = value;

			this.position = Math.Clamp(this.position, 0, value);
		}

		public override void Write(byte[] buffer, int offset, int count) {
			if (!this.CanWrite) {
				throw new InvalidOperationException("Can't write to stream; stream is not writable.");
			}
			long position = this.Stream.Position;
			this.Stream.Seek(this.position, SeekOrigin.Begin);
			try {
				this.Stream.Write(buffer, offset, count);
			} finally {
				this.position = this.Stream.Position;
				this.Stream.Seek(position, SeekOrigin.Begin);
				this.item.Size = this.Length;
			}
		}

		public override void Write(ReadOnlySpan<byte> buffer) {
			if (!this.CanWrite) {
				throw new InvalidOperationException("Can't write to stream; stream is not writable.");
			}
			long position = this.Stream.Position;
			this.Stream.Seek(this.position, SeekOrigin.Begin);
			try {
				this.Stream.Write(buffer);
			} finally {
				this.position = this.Stream.Position;
				this.Stream.Seek(position, SeekOrigin.Begin);
				this.item.Size = this.Length;
			}
		}

		public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
			if (!this.CanWrite) {
				throw new InvalidOperationException("Can't write to stream; stream is not writable.");
			}
			long position = this.Stream.Position;
			this.Stream.Seek(this.position, SeekOrigin.Begin);
			try {
				await this.Stream.WriteAsync(buffer, offset, count, cancellationToken);
			} finally {
				this.position = this.Stream.Position;
				this.Stream.Seek(position, SeekOrigin.Begin);
				this.item.Size = this.Length;
			}
		}

		public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) {
			if (!this.CanWrite) {
				throw new InvalidOperationException("Can't write to stream; stream is not writable.");
			}
			long position = this.Stream.Position;
			this.Stream.Seek(this.position, SeekOrigin.Begin);
			try {
				await this.Stream.WriteAsync(buffer, cancellationToken);
			} finally {
				this.position = this.Stream.Position;
				this.Stream.Seek(position, SeekOrigin.Begin);
				this.item.Size = this.Length;
			}
		}

		public override void WriteByte(byte value) {
			if (!this.CanWrite) {
				throw new InvalidOperationException("Can't write to stream; stream is not writable.");
			}
			long position = this.Stream.Position;
			this.Stream.Seek(this.position, SeekOrigin.Begin);
			try {
				this.Stream.WriteByte(value);
			} finally {
				this.position = this.Stream.Position;
				this.Stream.Seek(position, SeekOrigin.Begin);
				this.item.Size = this.Length;
			}
		}

		public override bool CanRead => this.access.HasFlag(FileAccess.Read) && this.Stream.CanRead;
		public override bool CanSeek => this.Stream.CanSeek;
		public override bool CanTimeout => this.Stream.CanTimeout;
		public override bool CanWrite => this.access.HasFlag(FileAccess.Write) && this.Stream.CanWrite;
		public override long Length => this.Stream.Length;
		public override long Position {
			get => this.position;
			set => this.position = Math.Clamp(value, 0, this.Length);
		}

		public override ValueTask DisposeAsync() {
			this.item.Shared |= this.shareLock;

			return base.DisposeAsync();
		}

		protected override void Dispose(bool disposing) {
			this.item.Shared |= this.shareLock;

			base.Dispose(disposing);
		}
	}
}
