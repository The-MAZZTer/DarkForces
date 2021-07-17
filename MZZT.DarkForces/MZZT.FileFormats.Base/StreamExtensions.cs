using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MZZT.Extensions {
	/// <summary>
	/// Extensions for the Stream class.
	/// </summary>
	public static class StreamExtensions {
		/// <summary>
		/// Reads an object from binary Stream data. If the end of Stream is reached mid-read, the remainder of the object will have default values.
		/// </summary>
		/// <typeparam name="T">The type of the struct to read.</typeparam>
		/// <param name="stream">The Stream to read from.</param>
		/// <param name="endianness">The endianness of the stored data.</param>
		/// <param name="limit">An optional limit on the number of bytes to read.</param>
		/// <returns>A tuple of the object read and the number of bytes consumed in reading it.</returns>
		public static async Task<(T, int)> ReadWithSizeAsync<T>(this Stream stream,
			Endianness endianness = Endianness.Keep, int limit = -1)
			where T : struct {

			int size = Marshal.SizeOf<T>();
			if (size == 0) {
				throw new ArgumentOutOfRangeException(typeof(T).Name, "Structure has no size, nothing to read!");
			}
			// Always limit to the size of the struct at largest.
			if (limit < 0 || limit > size) {
				limit = size;
			}
			byte[] buffer = new byte[size];
			int read = await stream.ReadAsync(buffer, 0, limit);
			if (read == 0) {
				return (new T(), read);
			}

			return (BinarySerializer.Deserialize<T>(buffer, 0, endianness), read);
		}

		/// <summary>
		/// Reads an object from binary Stream data. If the end of Stream is reached mid-read, the remainder of the object will have default values.
		/// </summary>
		/// <typeparam name="T">The type of the struct to read.</typeparam>
		/// <param name="stream">The Stream to read from.</param>
		/// <param name="endianness">The endianness of the stored data</param>
		/// <param name="limit">An optional limit on the number of bytes to read.</param>
		/// <returns>The read object.</returns>
		public static async Task<T> ReadAsync<T>(this Stream stream, Endianness endianness = Endianness.Keep,
			int limit = -1) where T : struct {

			(T ret, int length) = await stream.ReadWithSizeAsync<T>(endianness, limit);
			if (length == 0) {
				// If we couldn't read any part of the object, throw an exception, otherwise allow it.
				throw new EndOfStreamException($"Can't read a {typeof(T).Name}, end of stream!");
			}
			return ret;
		}

		/// <summary>
		/// Reads an object from binary Stream data. If the end of Stream is reached mid-read, the remainder of the object will have default values.
		/// </summary>
		/// <typeparam name="T">The type of the struct to read.</typeparam>
		/// <param name="stream">The Stream to read from.</param>
		/// <param name="endianness">The endianness of the stored data</param>
		/// <param name="limit">An optional limit on the number of bytes to read.</param>
		/// <returns>The read object.</returns>
		public static T Read<T>(this Stream stream, Endianness endianness = Endianness.Keep,
			int limit = -1) where T : struct =>

			stream.ReadAsync<T>(endianness, limit).GetAwaiter().GetResult();

		/// <summary>
		/// Writes an object to a Stream.
		/// </summary>
		/// <typeparam name="T">The type of the object.</typeparam>
		/// <param name="stream">The Stream to write to.</param>
		/// <param name="value">The object to write.</param>
		/// <param name="endianness">The endianness to write the data with.</param>
		/// <param name="limit">An optional limit of the nunber of bytes to write.</param>
		public static async Task WriteAsync<T>(this Stream stream, T value,
			Endianness endianness = Endianness.Keep, int limit = -1) where T : struct {

			int size = Marshal.SizeOf<T>();
			// Always limit to the size of the struct at largest.
			if (limit < 0 || limit > size) {
				limit = size;
			}

			byte[] buffer = BinarySerializer.Serialize<T>(value, endianness);

			await stream.WriteAsync(buffer, 0, limit);
		}

		/// <summary>
		/// Writes an object to a Stream.
		/// </summary>
		/// <typeparam name="T">The type of the object.</typeparam>
		/// <param name="stream">The Stream to write to.</param>
		/// <param name="value">The object to write.</param>
		/// <param name="endianness">The endianness to write the data with.</param>
		/// <param name="limit">An optional limit of the nunber of bytes to write.</param>
		public static void Write<T>(this Stream stream, T value, Endianness endianness = Endianness.Keep,
			int limit = -1) where T : struct =>

			stream.WriteAsync(value, endianness, limit).GetAwaiter().GetResult();

		// This is the default size .NET Core uses.
		private const int BUFFER_SIZE = 81920;

		/// <summary>
		/// Copy one Stream to another, but only a certain number of bytes.
		/// </summary>
		/// <param name="stream">The Stream to copy from.</param>
		/// <param name="dest">The Stream to copy to.</param>
		/// <param name="limit">The number of bytes to copy.</param>
		public static async Task CopyToWithLimitAsync(this Stream stream, Stream dest, int limit) {
			if (limit == 0) {
				return;
			}

			byte[] buffer = new byte[Math.Min(limit, BUFFER_SIZE)];
			int remaining = limit;
			int bytesRead;
			while (remaining > 0) {
				// Copy BUFFER_SIZE bytes at a time (unless there is less remaining).
				bytesRead = await stream.ReadAsync(buffer, 0, Math.Min(remaining, BUFFER_SIZE));
				if (bytesRead == 0) {
					break;
				}
				await dest.WriteAsync(buffer, 0, bytesRead);
				remaining -= bytesRead;
			}
		}

		/// <summary>
		/// Copy one Stream to another, but only a certain number of bytes.
		/// </summary>
		/// <param name="stream">The Stream to copy from.</param>
		/// <param name="dest">The Stream to copy to.</param>
		/// <param name="limit">The number of bytes to copy.</param>
		public static void CopyToWithLimit(this Stream stream, Stream dest, int limit) {
			if (limit == 0) {
				return;
			}

			byte[] buffer = new byte[BUFFER_SIZE];
			int remaining = limit;
			int bytesRead;
			while (remaining > 0) {
				// Copy BUFFER_SIZE bytes at a time (unless there is less remaining).
				bytesRead = stream.Read(buffer, 0, Math.Min(remaining, BUFFER_SIZE));
				if (bytesRead == 0) {
					break;
				}
				dest.Write(buffer, 0, bytesRead);
				remaining -= bytesRead;
			}
		}
	}
}
