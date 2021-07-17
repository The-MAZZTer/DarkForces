using System;
using System.IO;

namespace MZZT.IO {
	/// <summary>
	/// A Stream which restricts the range data can be read from or written to an underlying Stream.
	/// WARNING: Very slow on Unity for some reason, faster to just use a MemoryStream and copy data into it.
	/// </summary>
	public class ScopedStream : Stream {
		/// <summary>
		/// Construct a ScopedStream.
		/// </summary>
		/// <param name="baseStream">The Stream to scope, starting at the current position.</param>
		/// <param name="length">The limit of bytes to allow reading/writing.</param>
		public ScopedStream(Stream baseStream, long length) {
			this.BaseStream = baseStream;
			this.Length = length;

			// If we can seek we should record the current position of byte 0 of our scope so we can use baseStream.Position at any point to figure out our scoped position.
			if (baseStream.CanSeek) {
				this.position = baseStream.Position;
			} else {
				// Tally as we go and hope for the best.
				this.position = 0;
			}
		}

		/// <summary>
		/// The underlying Stream used to create this ScopedStream.
		/// </summary>
		public Stream BaseStream { get; }

		public override void Flush() => this.BaseStream.Flush();
		public override int Read(byte[] buffer, int offset, int count) {
			// This function seems slow for some reason, at least in Unity.
			// Maybe CanSeek is slow in FileStreams? We can try caching it?
			int available = (int)Math.Min(count, this.Length - this.Position);
			if (available <= 0) {
				return 0;
			}

			int ret = this.BaseStream.Read(buffer, offset, available);
			if (!this.CanSeek) {
				// Advance our tracked position.
				this.position += ret;
			}
			return ret;
		}
		public override long Seek(long offset, SeekOrigin origin) {
			if (!this.CanSeek) {
				throw new NotSupportedException();
			}

			long newPos = origin switch {
				SeekOrigin.Begin => this.position,
				SeekOrigin.Current => 0,
				SeekOrigin.End => this.position + this.Length,
				_ => throw new ArgumentException("Invalid value", nameof(origin))
			} + offset;
			if (newPos < this.position) {
				newPos = this.position;
			}
			if (newPos > this.position + this.Length) {
				newPos = this.position + this.Length;
			}

			return this.BaseStream.Seek(newPos, SeekOrigin.Begin) - this.position;
		}
		public override void SetLength(long value) {
			if (value != this.Length) {
				throw new NotSupportedException();
			}
		}
		public override void Write(byte[] buffer, int offset, int count) {
			if (this.Length - this.Position > count) {
				throw new NotSupportedException();
			}

			this.BaseStream.Write(buffer, offset, count);
			if (!this.CanSeek) {
				this.position += count;
			}
		}

		public override bool CanRead => this.BaseStream.CanRead;
		public override bool CanSeek => this.BaseStream.CanSeek;
		public override bool CanWrite => this.BaseStream.CanWrite;
		public override long Length { get; }
		private long position;
		public override long Position {
			get {
				if (this.CanSeek) {
					return this.BaseStream.Position - this.position;
				}

				return this.position;
			}
			set {
				if (!this.CanSeek) {
					throw new NotSupportedException();
				}
				if (value < 0 || value > this.Length) {
					throw new ArgumentOutOfRangeException();
				}
				this.BaseStream.Position = this.position + value;
			}
		}
	}
}
