using MZZT.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MZZT.DarkForces.FileFormats {
	/// <summary>
	/// A Landru DELT file.
	/// </summary>
	public class LandruDelt : DfFile<LandruDelt>, ICloneable {
		/// <summary>
		/// The header of the file.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct Header {
			/// <summary>
			/// Where the image should be displayed on the screen.
			/// </summary>
			public short OffsetX;
			/// <summary>
			/// Where the image should be displayed on the screen.
			/// </summary>
			public short OffsetY;
			/// <summary>
			/// Where the lower right corner of the image will be displayed on the screen.
			/// </summary>
			public short MaxX;
			/// <summary>
			/// Where the lower right corner of the image will be displayed on the screen.
			/// </summary>
			public short MaxY;

			/// <summary>
			/// The width of the image.
			/// </summary>
			public int Width {
				get => this.MaxX + 1 - this.OffsetX;
				set => this.MaxX = (short)(value - 1 + this.OffsetX);
			}
			/// <summary>
			/// The height of the image.
			/// </summary>
			public int Height {
				get => this.MaxY + 1 - this.OffsetY;
				set => this.MaxY = (short)(value - 1 + this.OffsetY);
			}
		}

		/// <summary>
		/// A header defining where to start drawing a horizontal line of pixels.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct LineSectionHeader {
			/// <summary>
			/// Bit field controlling whether run length encoding applies to this section, and how many pixels to draw in total.
			/// </summary>
			public ushort RleAndPixelCount;
			/// <summary>
			/// The starting location in the image.
			/// </summary>
			public ushort X;
			/// <summary>
			/// The starting location in the image.
			/// </summary>
			public ushort Y;

			/// <summary>
			/// Whether or not this section is run length encoded.
			/// </summary>
			public bool IsRle {
				get => (this.RleAndPixelCount & 0x1) != 0;
				set {
					if (value) {
						this.RleAndPixelCount |= 0x1;
					} else {
						this.RleAndPixelCount &= 0xFFFE;
					}
				}
			}

			/// <summary>
			/// The numebr of pixels to draw in this horizontal line.
			/// </summary>
			public ushort PixelCount {
				get => (ushort)(this.RleAndPixelCount >> 1);
				set {
					this.RleAndPixelCount &= 0x1;
					this.RleAndPixelCount |= (ushort)(value << 1);
				}
			}
		}

		private Header header;

		/// <summary>
		/// Where the image should be displayed on the screen.
		/// </summary>
		public short OffsetX {
			get => this.header.OffsetX;
			set {
				int width = this.header.Width;
				this.header.OffsetX = value;
				this.header.Width = width;
			}
		}
		/// <summary>
		/// Where the image should be displayed on the screen.
		/// </summary>
		public short OffsetY {
			get => this.header.OffsetY;
			set {
				int height = this.header.Height;
				this.header.OffsetY = value;
				this.header.Height = height;
			}
		}

		/// <summary>
		/// The width of the image.
		/// </summary>
		public int Width {
			get => Math.Max(0, this.header.Width);
			set => this.header.Width = value;
		}
		/// <summary>
		/// The height of the image.
		/// </summary>
		public int Height {
			get => Math.Max(0, this.header.Height);
			set => this.header.Height = value;
		}

		/// <summary>
		/// A bitmask used to determine which areas are/should be defined by line sections.
		/// Dark Forces just seems to have color 0 be transparent when encoding, so this may not be needed...
		/// </summary>
		public BitArray Mask { get; set; }
		/// <summary>
		/// Raw pixel data.
		/// </summary>
		public byte[] Pixels { get; set; }

		public void Trim() {
			int minx = 0;
			int miny = 0;
			int maxx = this.Width;
			int maxy = this.Height;

			bool resized = false;
			while (miny < maxy) {
				bool clear = true;
				for (int i = minx; i < maxx; i++) {
					if (this.Mask[miny * this.Width + i]) {
						clear = false;
						break;
					}
				}
				if (clear) {
					miny++;
					resized = true;
				} else {
					break;
				}
			}

			while (miny < maxy) {
				bool clear = true;
				for (int i = minx; i < maxx; i++) {
					if (this.Mask[(maxy - 1) * this.Width + i]) {
						clear = false;
						break;
					}
				}
				if (clear) {
					maxy--;
					resized = true;
				} else {
					break;
				}
			}

			while (minx < maxx && maxy > miny) {
				bool clear = true;
				for (int i = miny; i < maxy; i++) {
					if (this.Mask[i * this.Width + minx]) {
						clear = false;
						break;
					}
				}
				if (clear) {
					minx++;
					resized = true;
				} else {
					break;
				}
			}

			while (minx < maxx && maxy > miny) {
				bool clear = true;
				for (int i = miny; i < maxy; i++) {
					if (this.Mask[i * this.Width + (maxx - 1)]) {
						clear = false;
						break;
					}
				}
				if (clear) {
					maxx--;
					resized = true;
				} else {
					break;
				}
			}

			if (miny >= maxy || minx >= maxx) {
				this.Width = 1;
				this.Height = 1;
				this.OffsetX = 0;
				this.OffsetY = 0;
				this.Pixels = new byte[] { 0 };
				this.Mask = new BitArray(new bool[] { false });
				return;
			}

			if (!resized) {
				return;
			}

			byte[] newPixels = new byte[(maxx - minx) * (maxy - miny)];
			BitArray newMask = new((maxx - minx) * (maxy - miny));
			for (int i = 0; i < maxy - miny; i++) {
				Buffer.BlockCopy(this.Pixels, (i + miny) * this.Width + minx, newPixels, i * (maxx - minx), maxx - minx);
				for (int j = 0; j < maxx - minx; j++) {
					if (this.Mask[(i + miny) * this.Width + minx + j]) {
						newMask[i * (maxx - minx) + j] = true;
					}
				}
			}
			this.Pixels = newPixels;
			this.Mask = newMask;

			this.OffsetX += (short)minx;
			this.OffsetY += (short)miny;

			this.Width = maxx - minx;
			this.Height = maxy - miny;
		}

		public override bool CanLoad => true;

		public override async Task LoadAsync(Stream stream) {
			this.ClearWarnings();

			this.header = await stream.ReadAsync<Header>();

			int width = this.Width;
			int height = this.Height;

			this.Pixels = new byte[height * width];
			this.Mask = new BitArray(height * width);

			while (true) {
				LineSectionHeader header = await stream.ReadAsync<LineSectionHeader>();
				if (header.RleAndPixelCount == 0) {
					return;
				}

				int y = header.Y - this.header.OffsetY;
				int x = header.X - this.header.OffsetX;

				int pixelCount = header.PixelCount;

				for (int i = y * width + x; i < y * width + x + pixelCount; i++) {
					this.Mask[i] = true;
				}

				if (!header.IsRle) {
					await stream.ReadAsync(this.Pixels, y * width + x, pixelCount);
				} else {
					int byteCount = 0;

					int end = x + pixelCount;
					while (x < end) {
						byte count = (byte)stream.ReadByte();
						byteCount++;

						bool isRle = (count & 0x1) != 0;
						count >>= 1;

						if (!isRle) {
							await stream.ReadAsync(this.Pixels, y * width + x, count);

							byteCount += count;
						} else {
							byte value = (byte)stream.ReadByte();
							byteCount++;
							for (int i = x; i < x + count; i++) {
								this.Pixels[y * width + i] = value;
							}
						}
						x += count;
					}
				}
			}
		}

		public override bool CanSave => true;

		public override async Task SaveAsync(Stream stream) {
			this.ClearWarnings();

			if (this.Width * this.Height != this.Pixels.Length || this.Pixels.Length != this.Mask.Length) {
				throw new FormatException("Width and Height must match pixel and mask size.");
			}

			await stream.WriteAsync(this.header);

			int height = this.Height;

			for (int y = 0; y < height; y++) {
				(int start, int end)[] segments = this.GetLineSegments(y);

				foreach ((int start, int end) in segments) {
					// Unlike with BM and FME, we can't easily determine how big the resulting data will be ahead of time
					// so just process it both ways and pick the shorter one.
					byte[] raw = await this.EncodeRawAsync(start, end, y);
					byte[] rle = await this.EncodeRleAsync(start, end, y);

					if (rle.Length < raw.Length) {
						await stream.WriteAsync(rle, 0, rle.Length);
					} else {
						await stream.WriteAsync(raw, 0, raw.Length);
					}
				}
			}

			stream.WriteByte(0);
			stream.WriteByte(0);
		}

		private (int start, int end)[] GetLineSegments(int y) {
			int width = this.Width;

			List<(int start, int end)> segments = new();

			int x = 0;
			while (x < width) {
				while (x < width && !this.Mask[y * width + x]) {
					x++;
				}

				if (x >= width) {
					break;
				}

				int start = x;
				int end = Math.Min(x + 32767, width);

				while (x < end && this.Mask[y * width + x]) {
					x++;
				}
				end = x;

				segments.Add((start, end));
			}

			return segments.ToArray();
		}

		private async Task<byte[]> EncodeRawAsync(int start, int end, int y) {
			int width = this.Width;

			using MemoryStream stream = new();
			LineSectionHeader header = new() {
				X = (ushort)(start + this.header.OffsetX),
				Y = (ushort)(y + this.header.OffsetY),
				IsRle = false,
				PixelCount = (ushort)(end - start)
			};

			await stream.WriteAsync(header);

			byte[] buffer = new byte[end - start];
			await stream.WriteAsync(this.Pixels, y * width + start, end - start);
			return stream.ToArray();
		}

		private async Task<byte[]> EncodeRleAsync(int start, int end, int y) {
			int width = this.Width;

			using MemoryStream stream = new();
			LineSectionHeader header = new() {
				X = (ushort)(start + this.header.OffsetX),
				Y = (ushort)(y + this.header.OffsetY),
				IsRle = true,
				PixelCount = (ushort)(end - start)
			};

			await stream.WriteAsync(header);

			int x = start;
			int lastSegmentEnd = x;
			int rawLength;
			while (x < end) {
				byte color = this.Pixels[y * width + x];
				int runEnd = x;
				do {
					runEnd++;
				} while (runEnd < end && runEnd - x < 127 && this.Pixels[y * width + runEnd] == color);
				int rleLength = runEnd - x;

				rawLength = x - lastSegmentEnd;
				if (rleLength > 2 || (rleLength > 1 && (rawLength % 127) == 0)) {
					while (rawLength > 0) {
						int length = Math.Min(rawLength, 127);
						stream.WriteByte((byte)(length * 2));
						await stream.WriteAsync(this.Pixels, y * width + lastSegmentEnd, length);

						lastSegmentEnd += length;
						rawLength -= length;
					}

					stream.WriteByte((byte)(rleLength * 2 + 1));
					stream.WriteByte(color);

					lastSegmentEnd = runEnd;
				}
				x = runEnd;
			}

			rawLength = x - lastSegmentEnd;
			while (rawLength > 0) {
				int length = Math.Min(rawLength, 127);
				stream.WriteByte((byte)(length * 2));
				await stream.WriteAsync(this.Pixels, y * width + lastSegmentEnd, length);

				lastSegmentEnd += length;
				rawLength -= length;
			}

			return stream.ToArray();
		}

		object ICloneable.Clone() => this.Clone();
		public LandruDelt Clone() => new() {
			Height = this.Height,
			Mask = this.Mask != null ? new BitArray(this.Mask) : null,
			OffsetX = this.OffsetX,
			OffsetY = this.OffsetY,
			Pixels = this.Pixels != null ? this.Pixels.ToArray() : null,
			Width = this.Width
		};
	}
}
