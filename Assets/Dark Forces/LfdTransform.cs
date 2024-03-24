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
	public class LfdTransform : VirtualItemTransform {
		public override bool ShouldOverride(FileSystemProviderItemInfo item) => item.Type == FileSystemProviderItemTypes.File &&
			Path.GetExtension(item.Name).ToUpper() == ".LFD";

		public override IVirtualItem CreateOverride(IFileSystemProvider provider, FileSystemProviderItemInfo item) =>
			new LfdVirtualFolder(provider, item);
	}

	public class LfdVirtualFolder : IVirtualFolder {
		private readonly IFileSystemProvider provider;
		private readonly FileSystemProviderItemInfo data;
		public LfdVirtualFolder(IFileSystemProvider provider, FileSystemProviderItemInfo data) {
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
			yield return new LfdVirtualFile(this.provider, this.data);

			List<LfdVirtualChild> children = new();
			using (Stream stream = await this.provider.OpenFileAsync(this.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				LandruFileDirectory lfd = await LandruFileDirectory.ReadAsync(stream, lfd => {
					foreach ((string name, string type, uint _, uint size) in lfd.Files) {
						children.Add(new LfdVirtualChild(this.provider, this.data, name, type, size));
					}
					return Task.CompletedTask;
				});
			}
			foreach (LfdVirtualChild child in children) {
				yield return child;
			}
		}
		public async IAsyncEnumerable<IVirtualFolder> GetFoldersAsync() {
			await Task.CompletedTask;
			yield break;
		}

		public void ShowInFileManager() => this.provider.ShowInFileManager(this.data.FullPath);

		public async Task<IVirtualItem> GetChildAsync(string name) {
			if (name == this.data.Name) {
				return new LfdVirtualFile(this.provider, this.data);
			}

			int index = name.IndexOf('.');
			string type = name.Substring(index + 1);
			name = name.Substring(0, index);

			LfdVirtualChild child = null;
			using (Stream stream = await this.provider.OpenFileAsync(this.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				LandruFileDirectory lfd = await LandruFileDirectory.ReadAsync(stream, lfd => {
					(string name, string type, uint offset, uint size) file = lfd.Files.FirstOrDefault(x => x.name == name && x.type == type);
					if (file.name != null) {
						child = new LfdVirtualChild(this.provider, this.data, file.name, file.type, file.size);
					}
					return Task.CompletedTask;
				});
			}

			return child;
		}

		public async Task<Stream> OpenChildAsync(string name, FileMode mode, FileAccess access, FileShare share) {
			if (string.IsNullOrEmpty(name)) {
				return await this.provider.OpenFileAsync(this.FullPath, mode, access, share);
			}

			string type = Path.GetExtension(name).Substring(1);
			name = Path.GetFileNameWithoutExtension(name);

			Stream lfdStream = await this.provider.OpenFileAsync(this.FullPath, FileMode.Open, access | FileAccess.Read, share);
			Stream fileStream = null;
			try {
				// TODO this relies on the file stream being returned as a copy of the original, since we dispose the original.
				switch (mode) {
					case FileMode.Append:
					case FileMode.Open: {
						LandruFileDirectory lfd = await LandruFileDirectory.ReadAsync(lfdStream, async lfd => {
							fileStream = await lfd.GetFileStreamAsync(name, type, lfdStream);
						});
						if (fileStream == null) {
							throw new IOException();
						}
					} break;
					case FileMode.Create:
					case FileMode.Truncate:
						fileStream = new MemoryStream();
						break;
					case FileMode.CreateNew: {
						LandruFileDirectory lfd = await LandruFileDirectory.ReadAsync(lfdStream, async lfd => {
							fileStream = await lfd.GetFileStreamAsync(name, type, lfdStream);
						});
						if (fileStream != null) {
							fileStream.Dispose();
							throw new IOException();
						}
						fileStream = new MemoryStream();
					} break;
					case FileMode.OpenOrCreate: {
						LandruFileDirectory lfd = await LandruFileDirectory.ReadAsync(lfdStream, async lfd => {
							fileStream = await lfd.GetFileStreamAsync(name, type, lfdStream);
						});
						if (fileStream == null) {
							fileStream = new MemoryStream();
						}
					} break;
				}
			} finally {
				if (!access.HasFlag(FileAccess.Write)) {
					lfdStream.Dispose();
					lfdStream = null;
				}
			}
			return new LfdFileStream(lfdStream, name, type, fileStream, mode, access);
		}
	}

	public class LfdVirtualFile : IVirtualFile {
		private readonly IFileSystemProvider provider;
		private readonly FileSystemProviderItemInfo data;
		public LfdVirtualFile(IFileSystemProvider provider, FileSystemProviderItemInfo data) {
			this.provider = provider;
			this.data = data;
		}

		public Task<bool> ExistsAsync() => Task.FromResult(this.provider.Exists(this.data.FullPath) == FileSystemProviderItemTypes.File);
		public string Name => this.data.Name;
		public string DisplayName => this.data.DisplayName;
		public long? Size => this.data.Size;
		public string FullPath => this.data.FullPath;
		public IVirtualFolder Parent => new LfdVirtualFolder(this.provider, this.data);
		public FileShare AllowedOperations => this.data.AllowedOperations & ~FileShare.Delete;

		public Task DeleteAsync() => throw new IOException("Can't delete the LFD virtual file; delete the container instead.");

		public async Task<Stream> OpenAsync(FileMode mode, FileAccess access, FileShare share) =>
			await this.provider.OpenFileAsync(this.FullPath, mode, access, share);

		public void ShowInFileManager() => this.provider.ShowInFileManager(this.data.FullPath);
	}

	public class LfdVirtualChild : IVirtualFile {
		private readonly IFileSystemProvider provider;
		private readonly FileSystemProviderItemInfo data;
		private readonly string name;
		private readonly string type;

		public LfdVirtualChild(IFileSystemProvider provider, FileSystemProviderItemInfo data, string name, string type, uint size) {
			this.provider = provider;
			this.data = data;
			this.name = name;
			this.type = type;
			this.Size = size;
		}

		public async Task<bool> ExistsAsync() {
			if (this.provider.Exists(this.data.FullPath) != FileSystemProviderItemTypes.File) {
				return false;
			}
			LandruFileDirectory lfd;
			using (Stream stream = await this.provider.OpenFileAsync(this.data.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				lfd = await LandruFileDirectory.ReadAsync(stream, lfd => Task.CompletedTask);
			}
			return lfd.Files.Any(x => $"{x.name}.{x.type}" == this.Name);
		}

		public string Name => $"{this.name}.{this.type}";
		public string DisplayName => this.Name;
		public long? Size { get; private set; }
		public string FullPath => Path.Combine(this.data.FullPath, this.Name);
		public IVirtualFolder Parent => new LfdVirtualFolder(this.provider, this.data);
		public FileShare AllowedOperations => this.data.AllowedOperations;

		public async Task DeleteAsync() {
			using Stream stream = await this.provider.OpenFileAsync(this.data.FullPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
			using MemoryStream newStream = new((int)stream.Length);
			LandruFileDirectory newLfd = new();
			LandruFileDirectory lfd = await LandruFileDirectory.ReadAsync(stream, async lfd => {
				foreach ((string name, string type, uint offset, uint size) in lfd.Files) {
					if (name == this.name && type == this.type) {
						continue;
					}

					newLfd.AddFile(name, type, await lfd.GetFileAsync<Raw>(name, type));
				}
			});

			stream.Position = 0;
			stream.SetLength(newStream.Length);
			await newStream.CopyToAsync(stream);
		}

		public async Task<Stream> OpenAsync(FileMode mode, FileAccess access, FileShare share) =>
			await this.Parent.OpenChildAsync(this.Name, mode, access, share);

		public void ShowInFileManager() => this.provider.ShowInFileManager(this.data.FullPath);
	}

	public class LfdFileStream : Stream {
		private readonly Stream lfdStream;
		private readonly string name;
		private readonly string type;
		private readonly Stream fileStream;
		private readonly FileAccess access;
		public LfdFileStream(Stream lfdStream, string name, string type, Stream fileStream, FileMode mode, FileAccess access) {
			this.name = name;
			this.type = type;
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

			this.lfdStream = lfdStream;
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
			if (this.lfdStream != null && this.hasWritten) {
				using (this.lfdStream) {
					this.lfdStream.Position = 0;
					using MemoryStream newStream = new((int)this.lfdStream.Length);
					LandruFileDirectory newLfd = new();
					LandruFileDirectory lfd = await LandruFileDirectory.ReadAsync(this.lfdStream, async lfd => {
						foreach ((string name, string type, uint offset, uint size) in lfd.Files) {
							if (name == this.name && type == this.type) {
								await newLfd.AddFileAsync(name, type, this.fileStream);
							} else {
								newLfd.AddFile(name, type, await lfd.GetFileAsync<Raw>(name, type));
							}
						}
					});

					this.lfdStream.Position = 0;
					this.lfdStream.SetLength(newStream.Length);
					await newStream.CopyToAsync(this.lfdStream);
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
