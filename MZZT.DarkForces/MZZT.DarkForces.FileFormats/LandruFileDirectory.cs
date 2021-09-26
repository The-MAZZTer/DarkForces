using MZZT.Extensions;
using MZZT.FileFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MZZT.DarkForces.FileFormats {
	/// <summary>
	/// A Landru LFD file.
	/// </summary>
	public class LandruFileDirectory : DfFile<LandruFileDirectory> {
		/// <summary>
		/// Map of file types to the internal names used.
		/// </summary>
		public readonly static IReadOnlyDictionary<Type, string> FileTypeNames = new Dictionary<Type, string>() {
			[typeof(LandruAnimation)] = "ANIM",
			[typeof(LandruDelt)] = "DELT",
			[typeof(LandruFilm)] = "FILM",
			[typeof(LandruFont)] = "FONT",
			[typeof(DfGeneralMidi)] = "GMID",
			[typeof(LandruPalette)] = "PLTT",
			[typeof(CreativeVoice)] = "VOIC"
		};
		/// <summary>
		/// Map of type names to the internal types used.
		/// </summary>
		public readonly static IReadOnlyDictionary<string, Type> FileTypes = new Dictionary<string, Type>() {
			["ANIM"] = typeof(LandruAnimation),
			["DELT"] = typeof(LandruDelt),
			["FILM"] = typeof(LandruFilm),
			["FONT"] = typeof(LandruFont),
			["GMID"] = typeof(DfGeneralMidi),
			["PLTT"] = typeof(LandruPalette),
			["VOIC"] = typeof(CreativeVoice)
		};

		/// <summary>
		/// A header defining a single file.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
		public struct FileHeader {
			/// <summary>
			/// The file type as a char array.
			/// </summary>
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public char[] RawType;
			/// <summary>
			/// The file name as a char array.
			/// </summary>
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
			public char[] RawName;
			/// <summary>
			/// The size of the file.
			/// </summary>
			public uint Size;

			/// <summary>
			/// The file type.
			/// </summary>
			public string Type {
				get => new(this.RawType);
				set => this.RawType = value.ToCharArray();
			}
			/// <summary>
			/// The file name`.
			/// </summary>
			public string Name {
				get => new(this.RawName);
				set => this.RawName = value.ToCharArray();
			}
		}

		/// <summary>
		/// Try to read and parse a file from a Stream.
		/// </summary>
		/// <param name="stream">The Stream to read from.</param>
		/// <param name="fileHeaderRead">A callback function cakked when all file directory metadata has been read.
		/// The Stream is paused before the end of the file and this callback is used to allow the caller to read whichever files they are interested in.</param>
		/// <returns>The read object, or null if the read failed.</returns>
		public async static Task<LandruFileDirectory> TryReadAsync(Stream stream, Func<LandruFileDirectory, Task> fileHeaderRead) {
			try {
				return await ReadAsync(stream, fileHeaderRead);
			} catch (FormatException) {
			} catch (EndOfStreamException) {
			}
			return null;
		}

		/// <summary>
		/// Try to read and parse a file from disk.
		/// </summary>
		/// <param name="filename">The file path to read from.</param>
		/// <param name="fileHeaderRead">A callback function cakked when all file directory metadata has been read.
		/// The Stream is paused before the end of the file and this callback is used to allow the caller to read whichever files they are interested in.</param>
		/// <returns>The read object, or null if the read failed.</returns>
		public async static Task<LandruFileDirectory> TryReadAsync(string filename, Func<LandruFileDirectory, Task> fileHeaderRead) {
			try {
				return await ReadAsync(filename, fileHeaderRead);
			} catch (FormatException) {
			} catch (EndOfStreamException) {
			}
			return null;
		}

		/// <summary>
		/// Read and parse a file from a Stream.
		/// </summary>
		/// <param name="stream">The Stream to read.</param>
		/// <param name="fileHeaderRead">A callback function cakked when all file directory metadata has been read.
		/// The Stream is paused before the end of the file and this callback is used to allow the caller to read whichever files they are interested in.</param>
		/// <returns>The read object.</returns>
		public async static Task<LandruFileDirectory> ReadAsync(Stream stream, Func<LandruFileDirectory, Task> fileHeaderRead) {
			LandruFileDirectory x = new();
			await x.LoadAsync(stream, fileHeaderRead);
			return x;
		}

		/// <summary>
		/// Read and parse a file from disk.
		/// </summary>
		/// <param name="filename">The file path to read from.</param>
		/// <param name="fileHeaderRead">A callback function cakked when all file directory metadata has been read.
		/// The Stream is paused before the end of the file and this callback is used to allow the caller to read whichever files they are interested in.</param>
		/// <returns>The read object.</returns>
		public async static Task<LandruFileDirectory> ReadAsync(string filename, Func<LandruFileDirectory, Task> fileHeaderRead) {
			LandruFileDirectory x = new();
			await x.LoadAsync(filename, fileHeaderRead);
			return x;
		}

		private readonly List<FileHeader> files = new();
		private readonly List<IFile> fileData = new();
		private Stream stream;
		private uint pos;

		public override bool CanLoad => true;

		/// <summary>
		/// Load file data from a file path.
		/// </summary>
		/// <param name="filename">The file path to load from.</param>
		/// <param name="fileHeaderRead">A callback function cakked when all file directory metadata has been read.
		/// The Stream is paused before the end of the file and this callback is used to allow the caller to read whichever files they are interested in.</param>
		public async Task LoadAsync(string filename, Func<LandruFileDirectory, Task> fileHeaderRead) {
			using FileStream stream = new(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
			await this.LoadAsync(stream, fileHeaderRead);
		}

		public override Task LoadAsync(Stream stream) => this.LoadAsync(stream, null);

		/// <summary>
		/// Load file data from a Stream.
		/// </summary>
		/// <param name="stream">The Stream to load from.</param>
		/// <param name="fileHeaderRead">A callback function cakked when all file directory metadata has been read.
		/// The Stream is paused before the end of the file and this callback is used to allow the caller to read whichever files they are interested in.</param>
		public async Task LoadAsync(Stream stream, Func<LandruFileDirectory, Task> fileHeaderRead) {
			this.ClearWarnings();

			// Unlike GOB the file directory is at the beginning.
			// However LoadAsync assumes we will read to the end of the Stream.
			// The Stream is not guaranteed to be seekable.
			// So we provide a callback which can be used to read individual files (the caller is responsible
			// for reading files in order if the Stream isn't seekable.).

			FileHeader header = await stream.ReadAsync<FileHeader>();
			if (header.Type != "RMAP") {
				throw new FormatException("LFD file format not found!");
			}

			int fileHeaderSize = Marshal.SizeOf<FileHeader>();
			int fileCount = (int)(header.Size / fileHeaderSize);

			int size = fileHeaderSize * fileCount;
			byte[] buffer = new byte[size];
			await stream.ReadAsync(buffer, 0, size);
			this.files.Clear();
			this.fileData.Clear();
			for (int i = 0; i < fileCount; i++) {
				FileHeader file = BinarySerializer.Deserialize<FileHeader>(buffer, fileHeaderSize * i);
				file.Name = file.Name.TrimEnd('\0');
				this.files.Add(file);
				this.fileData.Add(null);
			}

			/*this.files.Clear();
			this.fileData.Clear();
			for (int i = 0; i < fileCount; i++) {
				this.files.Add(await stream.ReadAsync<FileHeader>());
				this.fileData.Add(null);
			}*/

			this.stream = stream;
			this.pos = (uint)Marshal.SizeOf<FileHeader>() + header.Size;
			uint length = this.pos + (uint)files.Sum(x => Marshal.SizeOf<FileHeader>() + x.Size);
			try {
				await fileHeaderRead?.Invoke(this);
			} finally {
				this.stream = null;
			}

			if (this.pos < length) {
				if (stream.CanSeek) {
					stream.Seek(length - this.pos, SeekOrigin.Current);
				} else {
					byte[] data = new byte[length - this.pos];
					await stream.ReadAsync(data, 0, (int)(length - this.pos));
				}
			}
		}

		/// <summary>
		/// The number of files in this LFD.
		/// </summary>
		public int NumFiles => this.files.Count;
		/// <summary>
		/// The information about embedded files.
		/// </summary>
		public IEnumerable<(string name, string type, uint offset, uint size)> Files {
			get {
				uint pos = (uint)Marshal.SizeOf<FileHeader>() * (uint)(this.files.Count + 2);
				foreach (FileHeader file in this.files) {
					yield return (file.Name, file.Type, pos, file.Size);
					pos += file.Size + (uint)Marshal.SizeOf<FileHeader>();
				}
			}
		}

		/// <summary>
		/// Read an embedded file.
		/// </summary>
		/// <param name="name">The name of the embedded file, without type or extension.</param>
		/// <param name="typeName">The type field value.</param>
		/// <returns>A stream for the embedded file.</returns>
		public async Task<Stream> GetFileStreamAsync(string name, string typeName) {
			typeName = typeName.ToUpper();
			uint offset = (uint)Marshal.SizeOf<FileHeader>() * (uint)(this.files.Count + 2);
			long size = -1;
			foreach (FileHeader info in this.files) {
				if (info.Name.ToUpper() == name.ToUpper() && info.Type.ToUpper() == typeName) {
					size = info.Size;
					break;
				}

				offset += (uint)(info.Size + Marshal.SizeOf<FileHeader>());
			}
			if (size < 0) {
				return null;
			}

			if (offset < this.pos) {
				this.pos = (uint)this.stream.Seek((int)offset - (int)this.pos, SeekOrigin.Current);
			} else if (offset > this.pos) {
				if (this.stream.CanSeek) {
					this.pos = (uint)this.stream.Seek(offset - this.pos, SeekOrigin.Current);
				} else {
					byte[] data = new byte[offset - this.pos];
					this.pos += (uint)await this.stream.ReadAsync(data, 0, (int)(offset - this.pos));
				}
			}

			if (this.pos < offset) {
				throw new EndOfStreamException();
			}

			MemoryStream mem = new((int)size);
			try {
				await this.stream.CopyToWithLimitAsync(mem, (int)size);
				this.pos += (uint)size;
				mem.Position = 0;
			} catch (Exception) {
				mem.Dispose();
				throw;
			}
			return mem;
		}

		/// <summary>
		/// Read an embedded file.
		/// </summary>
		/// <param name="name">The name of the embedded file, without type or extension.</param>
		/// <param name="type">The type of the embedded file.</param>
		/// <param name="typeName">The type field value, or null to autodetect based on type.</param>
		/// <returns>The read embedded file object.</returns>
		public async Task<IFile> GetFileAsync(string name, Type type, string typeName = null) {
			if (typeName == null) {
				if (!FileTypeNames.TryGetValue(type, out typeName)) {
					throw new ArgumentException($"Invalid Landru file type.", nameof(type));
				}
			}
			typeName = typeName.ToUpper();

			Stream stream = await this.GetFileStreamAsync(name, typeName);
			if (stream == null) {
				return null;
			}

			using (stream) {
				IFile ret = (IFile)Activator.CreateInstance(type);
				await ret.LoadAsync(stream);
				return ret;
			}
		}

		/// <summary>
		/// Read an embedded file.
		/// </summary>
		/// <param name="name">The name of the embedded file, without type or extension.</param>
		/// <param name="typeName">The type field value.</param>
		/// <returns>The read embedded file object.</returns>
		public async Task<IFile> GetFileAsync(string name, string typeName) {
			typeName = typeName.ToUpper();
			if (!FileTypes.TryGetValue(typeName, out Type type)) {
				throw new ArgumentException($"Invalid Landru file type.", nameof(typeName));
			}

			Stream stream =await this.GetFileStreamAsync(name, typeName);
			if (stream == null) {
				return null;
			}

			using (stream) {
				IFile ret = (IFile)Activator.CreateInstance(type);
				await ret.LoadAsync(stream);
				return ret;
			}
		}

		/// <summary>
		/// Read an embedded file.
		/// </summary>
		/// <typeparam name="T">The type of the embedded file.</typeparam>
		/// <param name="name">The name of the embedded file, without type or extension.</param>
		/// <param name="type">The type field value, or null to autodetect based on type.</param>
		/// <returns>The read embedded file object.</returns>
		public async Task<T> GetFileAsync<T>(string name, string type = null) where T : File<T>, new() {
			return (T)await this.GetFileAsync(name, typeof(T), type);
		}

		public override bool CanSave => true;

		public override async Task SaveAsync(Stream stream) {
			this.ClearWarnings();

			List<MemoryStream> fileData = new();
			try {
				foreach ((IFile file, int i) in this.fileData.Select((x, i) => (x, i))) {
					MemoryStream mem = new();
					await file.SaveAsync(mem);
					mem.Seek(0, SeekOrigin.Begin);
					fileData.Add(mem);

					FileHeader header = this.files[i];
					header.Size = (uint)mem.Length;
					this.files[i] = header;
				}

				await stream.WriteAsync(new FileHeader() {
					Type = "RMAP",
					Name = "resource",
					Size = (uint)(this.files.Count * Marshal.SizeOf<FileHeader>())
				});
				foreach (FileHeader file in this.files) {
					await stream.WriteAsync(file);
				}

				foreach ((FileHeader file, MemoryStream mem) in this.files.Zip(fileData, (x, y) => (x, y))) {
					await stream.WriteAsync(file);
					await mem.CopyToAsync(stream);
				}
			} finally {
				foreach (MemoryStream mem in fileData) {
					mem.Dispose();
				}
			}
		}

		/// <summary>
		/// Adds a file to the LFD.
		/// </summary>
		/// <typeparam name="T">Type of the embedded file.</typeparam>
		/// <param name="name">The file name.</param>
		/// <param name="type">The type/extension of the file.</param>
		/// <param name="file">The file object.</param>
		public void AddFile<T>(string name, string type, T file) where T : File<T>, new() {
			FileHeader header = new() {
				Name = name.Length > 8 ? name.Substring(0, 8) : name,
				Type = (type.Length > 4 ? type.Substring(0, 4) : type).ToUpper(),
				Size = 0
			};

			this.files.Add(header);
			this.fileData.Add(file);
		}

		/// <summary>
		/// Adds a file to the LFD.
		/// </summary>
		/// <typeparam name="T">Type of the embedded file.</typeparam>
		/// <param name="name">The file name.</param>
		/// <param name="type">The type/extension of the file.</param>
		/// <param name="stream">The data stream.</param>
		public async Task AddFileAsync<T>(string name, string type, Stream stream) where T : File<T>, new() {
			Raw raw = await Raw.ReadAsync(stream);
			this.AddFile(name, type, raw);
		}

		/// <summary>
		/// Adds a file to the LFD, detecting the type string.
		/// </summary>
		/// <typeparam name="T">Type of the embedded file.</typeparam>
		/// <param name="name">The file name.</param>
		/// <param name="file">The file object.</param>
		public void AddFile<T>(string name, T file) where T : File<T>, new() {
			if (!FileTypeNames.TryGetValue(typeof(T), out string type)) {
				throw new ArgumentException($"Invalid generic type.");
			}
			this.AddFile(name, type, file);
		}
	}
}
