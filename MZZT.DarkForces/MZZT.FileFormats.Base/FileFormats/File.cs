using System;
using System.IO;
using System.Threading.Tasks;

namespace MZZT.FileFormats {
	/// <summary>
	/// Interface for all File&lt;T&gt;s.
	/// </summary>
	public interface IFile {
		/// <summary>
		/// Load file data from the specified Stream.
		/// </summary>
		/// <param name="stream">The Stream to load data from.</param>
		Task LoadAsync(Stream stream);
		/// <summary>
		/// Load file data from the specified file on disk.
		/// </summary>
		/// <param name="filename">The file to load.</param>
		Task LoadAsync(string filename);

		/// <summary>
		/// Whether this File&lt;T&gt; can load data.
		/// </summary>
		bool CanLoad { get; }
		/// <summary>
		/// Whether this File&lt;T&gt; can save data.
		/// </summary>
		bool CanSave { get; }

		/// <summary>
		/// Write file data to the specified Sream.
		/// </summary>
		/// <param name="stream">The Stream to write data to.</param>
		Task SaveAsync(Stream stream);
		/// <summary>
		/// Write file data to the specified file on disk.
		/// </summary>
		/// <param name="filename">The file to save.</param>
		Task SaveAsync(string filename);
	}

	/// <summary>
	/// An abstract class which represents a specific file type, and its deserialized data.
	/// </summary>
	/// <typeparam name="T">The subclass this File is.</typeparam>
	public abstract class File<T> : IFile where T : File<T>, new() {
		/// <summary>
		/// Try to read and parse a file from a Stream.
		/// </summary>
		/// <param name="stream">The Stream to read from.</param>
		/// <returns>The read object, or null if the read failed.</returns>
		public async static Task<T> TryReadAsync(Stream stream) {
			try {
				return await ReadAsync(stream);
			} catch (FormatException) {
			} catch (EndOfStreamException) {
			}
			return null;
		}

		/// <summary>
		/// Try to read and parse a file from disk.
		/// </summary>
		/// <param name="filename">The file path to read from.</param>
		/// <returns>The read object, or null if the read failed.</returns>
		public async static Task<T> TryReadAsync(string filename) {
			try {
				return await ReadAsync(filename);
			} catch (FormatException) {
			} catch (EndOfStreamException) {
			}
			return null;
		}

		/// <summary>
		/// Read and parse a file from a Stream.
		/// </summary>
		/// <param name="stream">The Stream to read.</param>
		/// <returns>The read object.</returns>
		public async static Task<T> ReadAsync(Stream stream) {
			T x = new();
			await x.LoadAsync(stream);
			return x;
		}

		/// <summary>
		/// Read and parse a file from disk.
		/// </summary>
		/// <param name="filename">The file path to read from.</param>
		/// <returns>The read object.</returns>
		public async static Task<T> ReadAsync(string filename) {
			T x = new();
			await x.LoadAsync(filename);
			return x;
		}

		/// <summary>
		/// Creates an empty object.
		/// </summary>
		public File() { }

		/// <summary>
		/// Load data from a file path.
		/// </summary>
		/// <param name="filename">The file path to load from.</param>
		public File(string filename) => this.LoadAsync(filename).GetAwaiter().GetResult();

		/// <summary>
		/// Load data from a Stream.
		/// </summary>
		/// <param name="stream">The Stream to load data from.</param>
		public File(Stream stream) => this.LoadAsync(stream).GetAwaiter().GetResult();

		/// <summary>
		/// Load file data from a Stream.
		/// </summary>
		/// <param name="stream">The Stream to load from.</param>
		public virtual Task LoadAsync(Stream stream) {
			throw new NotSupportedException();
		}

		/// <summary>
		/// Load file data from a file path.
		/// </summary>
		/// <param name="filename">The file path to load from.</param>
		public async Task LoadAsync(string filename) {
			using FileStream stream = new(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
			await this.LoadAsync(stream);
		}

		/// <summary>
		/// Whether this type supports loading data.
		/// </summary>
		public virtual bool CanLoad => false;
		/// <summary>
		/// Whether this type supports saving data.
		/// </summary>
		public virtual bool CanSave => false;

		/// <summary>
		/// Save file data to a Stream.
		/// </summary>
		/// <param name="stream">The Stream to save to.</param>
		public virtual Task SaveAsync(Stream stream) {
			throw new NotSupportedException();
		}

		/// <summary>
		/// Save file data to a file path.
		/// </summary>
		/// <param name="filename">The file path to save to.</param>
		public async Task SaveAsync(string filename) {
			using FileStream stream = new(filename, FileMode.Create, FileAccess.Write, FileShare.None);
			await this.SaveAsync(stream);
		}
	}
}
