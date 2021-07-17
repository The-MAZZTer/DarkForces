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
	/// A Dark Forces GOB file.
	/// </summary>
	public class DfGobContainer : DfFile<DfGobContainer>, IDisposable {
		/// <summary>
		/// Map of file names/extensions to types.
		/// </summary>
		public readonly static IReadOnlyDictionary<string, Type> FileTypes = new Dictionary<string, Type>() {
			[".3DO"] = typeof(Df3dObject),
			[".BM"] = typeof(DfBitmap),
			["BRIEFING.LST"] = typeof(DfBriefingList),
			[".CMP"] = typeof(DfColormap),
			["CUTMUSE.TXT"] = typeof(DfCutsceneMusicList),
			["CUTSCENE.LST"] = typeof(DfCutsceneList),
			[".FME"] = typeof (DfFrame),
			[".FNT"] = typeof(DfFont),
			[".GMD"] = typeof(DfGeneralMidi),
			[".GOL"] = typeof(DfLevelGoals),
			[".INF"] = typeof(DfLevelInformation),
			[".MSG"] = typeof(DfMessages),
			[".O"] = typeof(DfLevelObjects),
			["JEDI.LVL"] = typeof(DfLevelList),
			[".LEV"] = typeof(DfLevel),
			[".PAL"] = typeof(DfPalette),
			[".VOC"] = typeof(CreativeVoice),
			[".VUE"] = typeof(AutodeskVue),
			[".WAX"] = typeof(DfWax)
		};

		/// <summary>
		/// Magic header number in a GOB.
		/// </summary>
		public const int MAGIC = 0x0A424F47;

		/// <summary>
		/// GOB header.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct Header {
			/// <summary>
			/// The magic identifier.
			/// </summary>
			public int Magic;
			/// <summary>
			/// The offset to the footer.
			/// </summary>
			public uint FooterPointer;

			public bool IsMagicValid {
				get => this.Magic == MAGIC;
				set {
					if (value) {
						this.Magic = MAGIC;
					} else {
						this.Magic = 0;
					}
				}
			}
		}

		/// <summary>
		/// File information structure at the end of the GOB.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
		public struct FileTrailer {
			/// <summary>
			/// The location of the file in the GOB.
			/// </summary>
			public uint FilePointer;
			/// <summary>
			/// The size of the file.
			/// </summary>
			public uint FileSize;
			/// <summary>
			/// The name of the file.
			/// </summary>
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 13)]
			public string FileName;
		}

		/// <summary>
		/// Try to read and parse a file from a Stream.
		/// </summary>
		/// <param name="stream">The Stream to read from.</param>
		/// <param name="cacheFileData">Whether or not the file data should be read from the GOB or skipped over.</param>
		/// <returns>The read object, or null if the read failed.</returns>
		public async static Task<DfGobContainer> TryReadAsync(Stream stream, bool cacheFileData) {
			try {
				return await ReadAsync(stream, cacheFileData);
			} catch (FormatException) {
			} catch (EndOfStreamException) {
			}
			return null;
		}

		/// <summary>
		/// Try to read and parse a file from disk.
		/// </summary>
		/// <param name="filename">The file path to read from.</param>
		/// <param name="cacheFileData">Whether or not the file data should be read from the GOB or skipped over.</param>
		/// <returns>The read object, or null if the read failed.</returns>
		public async static Task<DfGobContainer> TryReadAsync(string filename, bool cacheFileData) {
			try {
				return await ReadAsync(filename, cacheFileData);
			} catch (FormatException) {
			} catch (EndOfStreamException) {
			}
			return null;
		}

		/// <summary>
		/// Read and parse a file from a Stream.
		/// </summary>
		/// <param name="stream">The Stream to read.</param>
		/// <param name="cacheFileData">Whether or not the file data should be read from the GOB or skipped over.</param>
		/// <returns>The read object.</returns>
		public async static Task<DfGobContainer> ReadAsync(Stream stream, bool cacheFileData) {
			DfGobContainer x = new();
			await x.LoadAsync(stream, cacheFileData);
			return x;
		}

		/// <summary>
		/// Read and parse a file from disk.
		/// </summary>
		/// <param name="filename">The file path to read from.</param>
		/// <param name="cacheFileData">Whether or not the file data should be read from the GOB or skipped over.</param>
		/// <returns>The read object.</returns>
		public async static Task<DfGobContainer> ReadAsync(string filename, bool cacheFileData) {
			DfGobContainer x = new();
			await x.LoadAsync(filename, cacheFileData);
			return x;
		}

		private readonly List<FileTrailer> files = new();
		private MemoryStream data;

		public override bool CanLoad => true;

		/// <summary>
		/// Load file data from a file path.
		/// </summary>
		/// <param name="filename">The file path to load from.</param>
		/// <param name="cacheFileData">Whether or not the file data should be read from the GOB or skipped over.</param>
		public async Task LoadAsync(string filename, bool cacheFileData) {
			using FileStream stream = new(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
			await this.LoadAsync(stream, cacheFileData);
		}

		public override Task LoadAsync(Stream stream) => this.LoadAsync(stream, true);

		/// <summary>
		/// Load file data from a Stream.
		/// </summary>
		/// <param name="stream">The Stream to load from.</param>
		/// <param name="cacheFileData">Whether or not the file data should be read from the GOB or skipped over.</param>
		public async Task LoadAsync(Stream stream, bool cacheFileData) {
			this.ClearWarnings();

			Header header = await stream.ReadAsync<Header>();
			if (!header.IsMagicValid) {
				throw new FormatException("GOB file format not found!");
			}

			this.data?.Dispose();
			this.data = null;

			uint dataSize = header.FooterPointer - (uint)Marshal.SizeOf<Header>();

			// The file directory is at the end of the GOB. If we are using a Stream that can't seek,
			// this means we can't go back to fetch file data. So we have to pull it now.
			// This flag indicates if the caller needs the file data, or just the file metadata.
			if (cacheFileData || !stream.CanSeek) {
				byte[] data = new byte[dataSize];

				await stream.ReadAsync(data, 0, (int)dataSize);
				if (cacheFileData) {
					this.data = new(data);
				}
			} else {
				stream.Seek(dataSize, SeekOrigin.Current);
			}

			byte[] buffer = new byte[4];
			await stream.ReadAsync(buffer, 0, 4);
			int fileCount = BitConverter.ToInt32(buffer, 0);

			int trailerSize = Marshal.SizeOf<FileTrailer>();
			int size = trailerSize * fileCount;
			buffer = new byte[size];
			await stream.ReadAsync(buffer, 0, size);
			this.files.Clear();
			for (int i = 0; i < fileCount; i++) {
				FileTrailer file = BinarySerializer.Deserialize<FileTrailer>(buffer, trailerSize * i);
				file.FileName = file.FileName.TrimEnd('\0');
				this.files.Add(file);
			}

			/*this.files = new();
			for (int i = 0; i < fileCount; i++) {
				this.files.Add(await stream.ReadAsync<FileTrailer>());
			}*/
		}

		/// <summary>
		/// The number of files stored in the GOB.
		/// </summary>
		public int NumFiles => this.files.Count;
		/// <summary>
		/// The information about each file stored in the GOB.
		/// </summary>
		public IEnumerable<(string name, uint offset, uint size)> Files {
			get {
				foreach (FileTrailer file in this.files) {
					yield return (file.FileName, file.FilePointer, file.FileSize);
				}
			}
		}

		/// <summary>
		/// Retrieves a file frem the GOB.
		/// </summary>
		/// <param name="type">The type of the file, inherited from File&lt;T&gt;</param>
		/// <param name="name">The name of the file.</param>
		/// <param name="stream">An optional Stream, required if the file data was not read in with the GOB.
		/// The Stream should be the same one passed to LoadAsync and should be seekable.</param>
		/// <returns>The requested file data.</returns>
		public async Task<IFile> GetFileAsync(Type type, string name, Stream stream = null) {
			FileTrailer info = this.files.FirstOrDefault(x => x.FileName.ToUpper() == name.ToUpper());
			if (info.FilePointer == 0) {
				return null;
			}

			// If the original Stream is provided we can go back and get the file contents.
			// Of course if we cached the file data we don't need to use that Stream.
			Stream mem = null;
			try {
				if (this.data != null) {
					this.data.Seek(info.FilePointer - Marshal.SizeOf<Header>(), SeekOrigin.Begin);
					//ScopedStream scoped = new(this.data, info.FileSize);
					//mem = scoped;
					mem = new MemoryStream((int)info.FileSize);
					await this.data.CopyToWithLimitAsync(this.data, (int)info.FileSize);
					mem.Position = 0;
				} else {
					stream.Seek(info.FilePointer, SeekOrigin.Begin);
					//using ScopedStream scoped = new(stream, info.FileSize);
					mem = new MemoryStream((int)info.FileSize);
					await stream.CopyToWithLimitAsync(mem, (int)info.FileSize);
					mem.Position = 0;
				}

				IFile ret = (IFile)Activator.CreateInstance(type);
				await ret.LoadAsync(mem);
				return ret;
			} finally {
				if (mem != null && mem != this.data) {
					mem.Dispose();
				}
			}
		}

		/// <summary>
		/// Retrieves a file frem the GOB.
		/// </summary>
		/// <typeparam name="T">The type of the file, inherited from File&lt;T&gt;</typeparam>
		/// <param name="name">The name of the file.</param>
		/// <param name="stream">An optional Stream, required if the file data was not read in with the GOB.
		/// The Stream should be the same one passed to LoadAsync and should be seekable.</param>
		/// <returns>The requested file data.</returns>
		public async Task<T> GetFileAsync<T>(string name, Stream stream = null) where T : File<T>, new() {
			return (T)await this.GetFileAsync(typeof(T), name, stream);
		}

		public override bool CanSave => true;

		public override async Task SaveAsync(Stream stream) {
			this.ClearWarnings();

			if (this.data.Length != this.files.Sum(x => x.FileSize)) {
				throw new FormatException("File sizes don't match data blob size!");
			}

			await stream.WriteAsync(new Header() {
				IsMagicValid = true,
				FooterPointer = (uint)Marshal.SizeOf<Header>() + (uint)this.data.Length
			});

			this.data.Seek(0, SeekOrigin.Begin);
			await this.data.CopyToAsync(stream);

			uint pos = (uint)Marshal.SizeOf<Header>();
			foreach (FileTrailer file in this.files) {
				await stream.WriteAsync(new FileTrailer() {
					FilePointer = pos,
					FileSize = file.FileSize,
					FileName = file.FileName
				});
				pos += file.FileSize;
			}
		}

		/// <summary>
		/// Adds a new file to the GOB. The GOB file data must have been read (or a new GOB created).
		/// </summary>
		/// <param name="name">The name of the file.</param>
		/// <param name="file">The file to write.</param>
		public async Task AddFileAsync(string name, IFile file) {
			if (this.data == null) {
				this.data = new();
			}

			this.data.Seek(0, SeekOrigin.End);
			uint pos = (uint)this.data.Length;
			await file.SaveAsync(this.data);

			this.files.Add(new() {
				FilePointer = pos,
				FileSize = (uint)this.data.Length - pos,
				FileName = name.Length > 12 ? name.Substring(0, 12) : name
			});
		}

		private bool disposedValue;

		protected virtual void Dispose(bool disposing) {
			if (!this.disposedValue) {
				if (disposing) {
					this.data?.Dispose();
					this.data = null;
				}

				this.disposedValue = true;
			}
		}

		public void Dispose() {
			this.Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
