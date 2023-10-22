using MZZT.DarkForces.FileFormats;
using MZZT.FileFormats;
using MZZT.IO.FileProviders;
using MZZT.IO.FileSystemProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MZZT.DarkForces.IO {
	public class GobTransform : VirtualItemTransform {
		public override bool ShouldOverride(FileSystemProviderItemInfo item) => item.Type == FileSystemProviderItemTypes.File &&
			Path.GetExtension(item.Name).ToUpper() == ".GOB";

		public override IVirtualItem CreateOverride(IFileSystemProvider provider, FileSystemProviderItemInfo item) =>
			new GobVirtualFolder(provider, item);
	}

	public class GobVirtualFolder : IVirtualFolder {
		private readonly IFileSystemProvider provider;
		private readonly FileSystemProviderItemInfo data;
		public GobVirtualFolder(IFileSystemProvider provider, FileSystemProviderItemInfo data) {
			this.provider = provider;
			this.data = data;
		}

		public bool CanCreateFolder => false;
		public IVirtualItem CreateFolder(string name) => null;

		public Task<bool> ExistsAsync() => Task.FromResult(this.provider.Exists(this.data.FullPath) == FileSystemProviderItemTypes.File);
		public string Name => this.data.Name;
		public string DisplayName => this.data.DisplayName;
		public long? Size => this.data.Size;
		public string FullPath => this.data.FullPath;
		public IVirtualFolder Parent => string.IsNullOrEmpty(this.FullPath) ? null :
			new VirtualFolder(this.provider, Path.GetDirectoryName(this.FullPath));

		public FileShare AllowedOperations => this.data.AllowedOperations;

		public Task DeleteAsync() {
			this.provider.Delete(this.FullPath);
			return Task.CompletedTask;
		}

		public async IAsyncEnumerable<IVirtualItem> GetChildrenAsync() {
			await foreach (IVirtualFile file in this.GetFilesAsync()) {
				yield return file;
			}
		}
		public async IAsyncEnumerable<IVirtualFile> GetFilesAsync() {
			yield return new GobVirtualFile(this.provider, this.data);

			using Stream stream = await this.provider.OpenFileAsync(this.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
			using DfGobContainer gob = await DfGobContainer.ReadAsync(stream, false);

			foreach ((string name, uint _, uint size) in gob.Files) {
				yield return new GobVirtualChild(this.provider, this.data, name, size);
			}
		}
		public async IAsyncEnumerable<IVirtualFolder> GetFoldersAsync() {
			await Task.CompletedTask;
			yield break;
		}

		public void ShowInFileManager() => this.provider.ShowInFileManager(this.data.FullPath);

		public async Task<IVirtualItem> GetChildAsync(string name) {
			if (string.IsNullOrEmpty(name)) {
				return new GobVirtualFile(this.provider, this.data);
			}

			using Stream stream = await this.provider.OpenFileAsync(this.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
			using DfGobContainer gob = await DfGobContainer.ReadAsync(stream, false);

			(string name, uint offset, uint size) file = gob.Files.FirstOrDefault(x => x.name == name);
			if (file.name != null) {
				return new GobVirtualChild(this.provider, this.data, file.name, file.size);
			}
			return null;
		}

		public async Task<Stream> OpenChildAsync(string name, FileMode mode, FileAccess access, FileShare share) {
			if (string.IsNullOrEmpty(name)) {
				return await this.provider.OpenFileAsync(this.FullPath, mode, access, share);
			}

			Stream gobStream = await this.provider.OpenFileAsync(this.FullPath, FileMode.Open, access | FileAccess.Read, share);
			Stream fileStream = null;
			try {
				// TODO this relies on the file stream being returned as a copy of the original, since we dispose the original.
				switch (mode) {
					case FileMode.Append:
					case FileMode.Open:
						using (DfGobContainer gob = await DfGobContainer.ReadAsync(gobStream, false)) {
							fileStream = await gob.GetFileStreamAsync(name, gobStream);
						}
						if (fileStream == null) {
							throw new IOException();
						}
						break;
					case FileMode.Create:
					case FileMode.Truncate:
						fileStream = new MemoryStream();
						break;
					case FileMode.CreateNew:
						using (DfGobContainer gob = await DfGobContainer.ReadAsync(gobStream, false)) {
							fileStream = await gob.GetFileStreamAsync(name, gobStream);
						}
						if (fileStream != null) {
							fileStream.Dispose();
							throw new IOException();
						}
						fileStream = new MemoryStream();
						break;
					case FileMode.OpenOrCreate:
						using (DfGobContainer gob = await DfGobContainer.ReadAsync(gobStream, false)) {
							fileStream = await gob.GetFileStreamAsync(name, gobStream);
						}
						if (fileStream == null) {
							fileStream = new MemoryStream();
						}
						break;
				}
			} finally {
				if (!access.HasFlag(FileAccess.Write)) {
					gobStream.Dispose();
					gobStream = null;
				}
			}
			return new GobFileStream(gobStream, this.Name, fileStream, mode, access);
		}
	}

	public class GobVirtualFile : IVirtualFile {
		private readonly IFileSystemProvider provider;
		private readonly FileSystemProviderItemInfo data;
		public GobVirtualFile(IFileSystemProvider provider, FileSystemProviderItemInfo data) {
			this.provider = provider;
			this.data = data;
		}

		public Task<bool> ExistsAsync() => Task.FromResult(this.provider.Exists(this.data.FullPath) == FileSystemProviderItemTypes.File);
		public string Name => this.data.Name;
		public string DisplayName => this.data.DisplayName;
		public long? Size => this.data.Size;
		public string FullPath => this.data.FullPath;
		public IVirtualFolder Parent => new GobVirtualFolder(this.provider, this.data);
		public FileShare AllowedOperations => this.data.AllowedOperations & ~FileShare.Delete;

		public Task DeleteAsync() => throw new IOException("Can't delete the GOB virtual file; delete the container instead.");

		public async Task<Stream> OpenAsync(FileMode mode, FileAccess access, FileShare share) =>
			await this.provider.OpenFileAsync(this.FullPath, mode, access, share);

		public void ShowInFileManager() => this.provider.ShowInFileManager(this.data.FullPath);
	}

	public class GobVirtualChild : IVirtualFile {
		private readonly IFileSystemProvider provider;
		private readonly FileSystemProviderItemInfo data;

		public GobVirtualChild(IFileSystemProvider provider, FileSystemProviderItemInfo data, string name, uint size) {
			this.provider = provider;
			this.data = data;
			this.Name = name;
			this.Size = size;
		}

		public async Task<bool> ExistsAsync() {
			if (this.provider.Exists(this.data.FullPath) != FileSystemProviderItemTypes.File) {
				return false;
			}
			DfGobContainer gob;
			using (Stream stream = await this.provider.OpenFileAsync(this.data.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				gob = await DfGobContainer.ReadAsync(stream, false);
			}
			using (gob) {
				return gob.Files.Any(x => x.name == this.Name);
			}
		}

		public string Name { get; private set; }
		public string DisplayName => this.Name;
		public long? Size { get; private set; }
		public string FullPath => Path.Combine(this.data.FullPath, this.Name);
		public IVirtualFolder Parent => new GobVirtualFolder(this.provider, this.data);
		public FileShare AllowedOperations => this.data.AllowedOperations;

		public async Task DeleteAsync() {
			using Stream stream = await this.provider.OpenFileAsync(this.data.FullPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
			using DfGobContainer gob = await DfGobContainer.ReadAsync(stream, false);
			using MemoryStream newStream = new((int)stream.Length);
			using (DfGobContainer newGob = new()) {
				foreach ((string name, uint offset, uint size) in gob.Files) {
					if (name == this.Name) {
						continue;
					}

					await newGob.AddFileAsync(name, await gob.GetFileAsync<Raw>(name, stream));
				}
			}

			stream.Position = 0;
			stream.SetLength(newStream.Length);
			await newStream.CopyToAsync(stream);
		}

		public async Task<Stream> OpenAsync(FileMode mode, FileAccess access, FileShare share) =>
			await this.Parent.OpenChildAsync(this.Name, mode, access, share);

		public void ShowInFileManager() => this.provider.ShowInFileManager(this.data.FullPath);
	}

	public class GobFileStream : Stream {
		private readonly Stream gobStream;
		private readonly string filename;
		private readonly Stream fileStream;
		private readonly FileAccess access;
		public GobFileStream(Stream gobStream, string filename, Stream fileStream, FileMode mode, FileAccess access) {
			this.filename = filename;
			this.fileStream = fileStream;
			this.access = access;

			fileStream.Position = mode == FileMode.Append ? fileStream.Length : 0;

			this.hasWritten = mode switch {
				FileMode.Append => false,
				FileMode.Create => true,
				FileMode.CreateNew => true,
				FileMode.Open => false,
				FileMode.OpenOrCreate => false,
				FileMode.Truncate => true,
				_ => false
			};

			this.gobStream = gobStream;
		}
		private bool hasWritten;

		public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) {
			if (!this.CanRead) {
				throw new InvalidOperationException("Can't read stream; stream is not readable.");
			}
			await this.fileStream.CopyToAsync(destination, bufferSize, cancellationToken);
		}

		public override void Flush() => this.fileStream.Flush();

		public override async Task FlushAsync(CancellationToken cancellationToken) =>
			await this.fileStream.FlushAsync(cancellationToken);

		public override int Read(byte[] buffer, int offset, int count) {
			if (!this.CanRead) {
				throw new InvalidOperationException("Can't read stream; stream is not readable.");
			}
			return this.fileStream.Read(buffer, offset, count);
		}

		public override int Read(Span<byte> buffer) {
			if (!this.CanRead) {
				throw new InvalidOperationException("Can't read stream; stream is not readable.");
			}
			return this.fileStream.Read(buffer);
		}

		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
			if (!this.CanRead) {
				throw new InvalidOperationException("Can't read stream; stream is not readable.");
			}
			return await this.fileStream.ReadAsync(buffer, offset, count, cancellationToken);
		}

		public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) {
			if (!this.CanRead) {
				throw new InvalidOperationException("Can't read stream; stream is not readable.");
			}
			return await this.fileStream.ReadAsync(buffer, cancellationToken);
		}

		public override int ReadByte() {
			if (!this.CanRead) {
				throw new InvalidOperationException("Can't read stream; stream is not readable.");
			}
			return this.fileStream.ReadByte();
		}

		public override long Seek(long offset, SeekOrigin origin) =>
			this.fileStream.Seek(offset, origin);

		public override void SetLength(long value) {
			if (!this.CanWrite) {
				throw new InvalidOperationException("Can't set stream length; stream is not writable.");
			}
			this.fileStream.SetLength(value);
			this.hasWritten = true;
		}

		public override void Write(byte[] buffer, int offset, int count) {
			if (!this.CanWrite) {
				throw new InvalidOperationException("Can't write to stream; stream is not writable.");
			}
			this.fileStream.Write(buffer, offset, count);
			this.hasWritten = true;
		}

		public override void Write(ReadOnlySpan<byte> buffer) {
			if (!this.CanWrite) {
				throw new InvalidOperationException("Can't write to stream; stream is not writable.");
			}
			this.fileStream.Write(buffer);
			this.hasWritten = true;
		}

		public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
			if (!this.CanWrite) {
				throw new InvalidOperationException("Can't write to stream; stream is not writable.");
			}
			await this.fileStream.WriteAsync(buffer, offset, count, cancellationToken);
			this.hasWritten = true;
		}

		public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) {
			if (!this.CanWrite) {
				throw new InvalidOperationException("Can't write to stream; stream is not writable.");
			}
			await this.fileStream.WriteAsync(buffer, cancellationToken);
			this.hasWritten = true;
		}

		public override void WriteByte(byte value) {
			if (!this.CanWrite) {
				throw new InvalidOperationException("Can't write to stream; stream is not writable.");
			}
			this.fileStream.WriteByte(value);
			this.hasWritten = true;
		}

		public override bool CanRead => this.access.HasFlag(FileAccess.Read) && this.fileStream.CanRead;
		public override bool CanSeek => this.fileStream.CanSeek;
		public override bool CanTimeout => this.fileStream.CanTimeout;
		public override bool CanWrite => this.access.HasFlag(FileAccess.Write) && this.fileStream.CanWrite;
		public override long Length => this.fileStream.Length;
		public override long Position {
			get => this.fileStream.Position;
			set => this.fileStream.Position = value;
		}
		
		private async Task CloseAsync() {
			if (this.gobStream != null && this.hasWritten) {
				using (this.gobStream) {
					this.gobStream.Position = 0;
					using DfGobContainer gob = await DfGobContainer.ReadAsync(this.gobStream, false);
					using MemoryStream newStream = new((int)this.gobStream.Length);
					using (DfGobContainer newGob = new()) {
						foreach ((string name, uint offset, uint size) in gob.Files) {
							if (name == this.filename) {
								await newGob.AddFileAsync(name, this.fileStream);
							} else {
								await newGob.AddFileAsync(name, await gob.GetFileAsync<Raw>(name, this.gobStream));
							}
						}
					}

					this.gobStream.Position = 0;
					this.gobStream.SetLength(newStream.Length);
					await newStream.CopyToAsync(this.gobStream);
				}
			}

			this.fileStream.Close();
			base.Close();
		}

		private Task closeTask;
		public override void Close() {
			if (this.closeTask != null) {
				return;
			}

			this.closeTask = this.CloseAsync();
		}

		public override async ValueTask DisposeAsync() {
			if (this.closeTask != null) {
				await this.closeTask;
			}
			await this.fileStream.DisposeAsync();
			await base.DisposeAsync();
		}

		protected override void Dispose(bool disposing) {
			if (this.closeTask != null) {
				_ = this.DisposeAsync();
				return;
			}

			this.fileStream.Dispose();
			base.Dispose(disposing);
		}
	}
}
