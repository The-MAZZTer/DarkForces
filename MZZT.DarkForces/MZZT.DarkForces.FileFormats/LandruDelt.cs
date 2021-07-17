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
	public class LandruDelt : DfFile<LandruDelt> {
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
			get => this.header.Width;
			set => this.header.Width = value;
		}
		/// <summary>
		/// The height of the image.
		/// </summary>
		public int Height {
			get => this.header.Height;
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

		public override bool CanLoad => true;

		public override async Task LoadAsync(Stream stream) {
			this.ClearWarnings();

			this.header = await stream.ReadAsync<Header>();

			int width = this.header.Width;
			int height = this.header.Height;

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
				for (int i = (height - y - 1) * width + x; i < (height - y - 1) * width + x + pixelCount; i++) {
					this.Mask.Set(i, true);
				}

				if (!header.IsRle) {
					byte[] buffer = new byte[pixelCount];
					await stream.ReadAsync(buffer, 0, pixelCount);

					Buffer.BlockCopy(buffer, 0, this.Pixels, (height - y - 1) * width + x, pixelCount);
				} else {
					int end = x + pixelCount;
					while (x < end) {
						byte count = (byte)stream.ReadByte();

						bool isRle = (count & 0x1) != 0;
						count >>= 1;
						if (!isRle) {
							byte[] buffer = new byte[count];
							await stream.ReadAsync(buffer, 0, count);
							Buffer.BlockCopy(buffer, 0, this.Pixels, (height - y - 1) * width + x, count);
						} else {
							byte value = (byte)stream.ReadByte();
							for (int i = x; i < x + count; i++) {
								this.Pixels[(height - y - 1) * width + i] = value;
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

			int width = this.Pixels.GetLength(1);
			int height = this.Pixels.GetLength(0);

			this.header.Width = width;
			this.header.Height = height;

			await stream.WriteAsync(this.header);

			for (int y = 0; y < height; y++) {
				int minx = 0;
				int maxx = width - 1;

				while (minx < maxx && !this.Mask[(height - y - 1) * height + minx]) {
					minx++;
				}
				if (minx >= maxx) {
					continue;
				}
				while (!this.Mask[(height - y - 1) * height + maxx]) {
					maxx--;
				}

				(int start, int end)[] segments = this.GetLineSegments(y);

				// Unlike with BM and FME, we can't easily determine how big the resulting data will be ahead of time
				// so just process it both ways and pick the shorter one.
				byte[] raw = await this.EncodeRawAsync(segments, y);
				byte[] rle = await this.EncodeRleAsync(segments[0].start, segments.Last().end, y);
				if (rle.Length < raw.Length) {
					await stream.WriteAsync(rle, 0, rle.Length);
				} else {
					await stream.WriteAsync(raw, 0, raw.Length);
				}
			}

			stream.WriteByte(0);
			stream.WriteByte(0);
		}

		private (int start, int end)[] GetLineSegments(int y) {
			int width = this.Pixels.GetLength(1);
			int height = this.Pixels.GetLength(0);

			int x = 0;
			List<(int start, int end)> segments = new();

			while (x < width) {
				while (x < width && !this.Mask[(height - y - 1) * width + x]) {
					x++;
				}

				if (x >= width) {
					break;
				}

				int start = x;
				int end = Math.Min(x + 127, width);

				while (x < end) {
					while (x < end && this.Mask[(height - y - 1) * width + x]) {
						x++;
					}

					if (x >= end) {
						break;
					}

					int count = 0;
					int x2 = x;
					while (x2 < end && !this.Mask[(height - y - 1) * width + x2]) {
						count++;
						x2++;
					}

					if (count <= 6) {
						x += count;
					} else {
						break;
					}
				}
				end = x;

				while (!this.Mask[(height - y - 1) * width + end - 1]) {
					end--;
				}

				segments.Add((start, end));
			}

			return segments.ToArray();
		}

		private async Task<byte[]> EncodeRawAsync((int start, int end)[] segments, int y) {
			int width = this.Pixels.GetLength(1);
			int height = this.Pixels.GetLength(0);

			using MemoryStream stream = new();
			foreach ((int start, int end) in segments) {
				LineSectionHeader header = new() {
					X = (ushort)(start + this.header.OffsetX),
					Y = (ushort)(y + this.header.OffsetY),
					IsRle = false,
					PixelCount = (ushort)(end - start)
				};

				await stream.WriteAsync(header);

				byte[] buffer = new byte[end - start];
				Buffer.BlockCopy(this.Pixels, (height - y - 1) * width + start, buffer, 0, end - start);
				await stream.WriteAsync(buffer, 0, end - start);
			}
			return stream.ToArray();
		}

		private async Task<byte[]> EncodeRleAsync(int start, int end, int y) {
			int width = this.Pixels.GetLength(1);
			int height = this.Pixels.GetLength(0);

			using MemoryStream stream = new();
			LineSectionHeader header = new() {
				X = (ushort)(start + this.header.OffsetX),
				Y = (ushort)(y + this.header.OffsetY),
				IsRle = true,
				PixelCount = (ushort)(end - start)
			};

			await stream.WriteAsync(header);

			int x = start;
			while (x < end) {
				if (x == end - 1) {
					stream.WriteByte(2);
					stream.WriteByte(this.Pixels[(height - y - 1) * width + x]);
					break;
				}

				byte color = this.Pixels[(height - y - 1) * width + x];
				x++;
				byte nextColor = this.Pixels[(height - y - 1) * width + x];

				int count;
				if (color == nextColor) {
					count = 1;
					while (color == nextColor && count < 128 && x < end) {
						count++;
						x++;
						if (x < end) {
							nextColor = this.Pixels[(height - y - 1) * width + x];
						}
					}

					stream.WriteByte((byte)(count * 2 + 1));
					stream.WriteByte(color);
				}

				count = 0;
				int x2 = x;
				while (color != nextColor && count <= 128 && x2 < end) {
					x2++;
					if (x2 < end) {
						color = nextColor;
						nextColor = this.Pixels[(height - y - 1) * width + x2];
					} else {
						x2++;
					}
				}

				if (x2 > x) {
					stream.WriteByte((byte)((x2 - x) * 2));
					byte[] buffer = new byte[x2 - x];
					Buffer.BlockCopy(this.Pixels, (height - y - 1) * width + x, buffer, 0, x2 - x);
					await stream.WriteAsync(buffer, 0, x2 - x);
				}
			}

			return stream.ToArray();
		}
	}
}
