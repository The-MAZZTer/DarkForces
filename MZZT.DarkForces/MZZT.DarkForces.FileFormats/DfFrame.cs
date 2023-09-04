using MZZT.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MZZT.DarkForces.FileFormats {
	/// <summary>
	/// A Dark Forces FME file.
	/// </summary>
	public class DfFrame : DfFile<DfFrame>, ICloneable {
		/// <summary>
		/// Frame file header.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct Header {
			/// <summary>
			/// The point the sprite rotates around.
			/// </summary>
			public int InsertionPointX;
			/// <summary>
			/// The point the sprite rotates around.
			/// </summary>
			public int InsertionPointY;
			/// <summary>
			/// Whether the sprite should be flipped horizontally.
			/// </summary>
			public int Flip;
			/// <summary>
			/// Offset to the cell header.
			/// </summary>
			public uint CellOffset;
			/// <summary>
			/// Unknown.
			/// </summary>
			public int UnitWidth;
			/// <summary>
			/// Unknown.
			/// </summary>
			public int UnitHeight;
			/// <summary>
			/// Unknown.
			/// </summary>
			public int Padding;
			/// <summary>
			/// Unknown.
			/// </summary>
			public int Padding2;
		}

		/// <summary>
		/// A header for a frame cell (used by FMEs and WAXs).
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct CellHeader {
			/// <summary>
			/// Width of the image.
			/// </summary>
			public int Width;
			/// <summary>
			/// Height of the image.
			/// </summary>
			public int Height;
			/// <summary>
			/// Is the image RLE compressed?
			/// </summary>
			public int Compressed;
			/// <summary>
			/// The size of the raw byte data.
			/// </summary>
			public uint DataSize;
			/// <summary>
			/// Offset of the column table past the end of the cell header.
			/// </summary>
			public uint ColumnTableOffset;
			/// <summary>
			/// Unknown.
			/// </summary>
			public int Padding;
		}

		internal Header header;
		internal CellHeader cellHeader;

		/// <summary>
		/// The point the sprite rotates around.
		/// </summary>
		public int InsertionPointX {
			get => this.header.InsertionPointX;
			set => this.header.InsertionPointX = value;
		}
		/// <summary>
		/// The point the sprite rotates around.
		/// </summary>
		public int InsertionPointY {
			get => this.header.InsertionPointY;
			set => this.header.InsertionPointY = value;
		}
		/// <summary>
		/// Whether the sprite should be flipped horizontally.
		/// </summary>
		public bool Flip {
			get => this.header.Flip != 0;
			set => this.header.Flip = value ? 1 : 0;
		}

		/// <summary>
		/// Width of the image.
		/// </summary>
		public int Width {
			get => this.cellHeader.Width;
			set => this.cellHeader.Width = value;
		}
		/// <summary>
		/// Height of the image.
		/// </summary>
		public int Height {
			get => this.cellHeader.Height;
			set => this.cellHeader.Height = value;
		}
		/// <summary>
		/// Raw image data.
		/// </summary>
		public byte[] Pixels { get; set; } = Array.Empty<byte>();

		/// <summary>
		/// Whether or not to choose an optimal compression mode automatically.
		/// </summary>
		public bool AutoCompress { get; set; }

		/// <summary>
		/// The compression to use.
		/// </summary>
		public bool Compressed {
			get => this.cellHeader.Compressed != 0;
			set => this.cellHeader.Compressed = value ? 1 : 0;
		}

		internal async Task LoadHeaderAsync(Stream stream) {
			this.header = await stream.ReadAsync<Header>();
		}

		internal async Task LoadCellAsync(Stream stream) {
			this.cellHeader = await stream.ReadAsync<CellHeader>();

			int width = this.cellHeader.Width;
			int height = this.cellHeader.Height;

			this.Pixels = new byte[height * width];

			byte[] buffer;
			if (this.cellHeader.Compressed == 0) {
				buffer = new byte[width * height];
				await stream.ReadAsync(buffer, 0, width * height);
				for (int x = 0; x < width; x++) {
					for (int y = 0; y < height; y++) {
						byte value = buffer[x * height + y];
						this.Pixels[y * width + x] = value;
					}
				}
				return;
			}

			int offset = (int)this.cellHeader.ColumnTableOffset;
			if (offset > 0) {
				buffer = new byte[offset];
				await stream.ReadAsync(buffer, 0, offset);
			}

			buffer = new byte[4 * width];
			await stream.ReadAsync(buffer, 0, 4 * width);
			int[] dataStarts = Enumerable.Range(0, width).Select(x => BitConverter.ToInt32(buffer, x * 4) -
				Marshal.SizeOf<CellHeader>() - offset - 4 * width).ToArray();

			int datasize = (int)this.cellHeader.DataSize - Marshal.SizeOf<CellHeader>() - offset - 4 * width;

			buffer = new byte[datasize];
			await stream.ReadAsync(buffer, 0, datasize);

			for (int x = 0; x < width; x++) {
				int start = dataStarts[x];
				int end = x == width - 1 ? datasize : dataStarts[x + 1];

				int pos = start;
				int y = 0;
				while (pos < end && y < height) {
					byte value = buffer[pos];
					pos++;

					bool rle = value > 128;
					if (rle) {
						value -= 128;
					}

					byte color = 0;
					for (int i = y; i < y + value && i < height; i++) {
						if (!rle) {
							color = buffer[pos];
							pos++;
						}
						this.Pixels[i * width + x] = color;
					}
					y += value;
				}
			}
		}

		public override bool CanLoad => true;

		public override async Task LoadAsync(Stream stream) {
			this.ClearWarnings();

			await this.LoadHeaderAsync(stream);

			int offset = (int)this.header.CellOffset - Marshal.SizeOf<Header>();
			if (offset > 0) {
				byte[] buffer = new byte[offset];
				await stream.ReadAsync(buffer, 0, offset);
			}

			await this.LoadCellAsync(stream);
		}

		public override bool CanSave => true;

		internal async Task SaveHeaderAsync(Stream stream) {
			await stream.WriteAsync(this.header);
		}

		internal async Task SaveCellAsync(Stream stream) {
			int width = this.cellHeader.Width;
			int height = this.cellHeader.Height;

			this.cellHeader.ColumnTableOffset = 0;

			byte[] buffer = new byte[width * height];
			int[] rle0SegmentSizes = new int[width];
			for (int x = 0; x < width; x++) {
				for (int y = 0; y < height; y++) {
					buffer[x * height + y] = this.Pixels[y * width + x];
				}

				int yRle = 0;
				while (yRle < height) {
					(bool isRle, int end) = DfBitmap.GetNextRle(buffer, x, yRle, height, (a, b) => (a == 0 && b == 0));

					int start = x * height + yRle;
					int length = end - start;

					if (isRle) {
						rle0SegmentSizes[x] += 1;
					} else {
						rle0SegmentSizes[x] += length + 1;
					}
					yRle += length;
				}
			}

			int noCompressionSize = width * height;
			int rle0CompressionSize = rle0SegmentSizes.Sum() + width * 4;

			if (this.AutoCompress) {
				this.Compressed = rle0CompressionSize <= noCompressionSize;
			}

			bool compressed = this.Compressed;
			this.cellHeader.DataSize = (uint)(Marshal.SizeOf<CellHeader>() + (compressed ? rle0CompressionSize : noCompressionSize));

			await stream.WriteAsync(this.cellHeader);

			if (!compressed) {
				await stream.WriteAsync(buffer, 0, buffer.Length);
				return;
			}

			int pos = Marshal.SizeOf<CellHeader>() + 4 * width;
			for (int x = 0; x < width; x++) {
				await stream.WriteAsync(BitConverter.GetBytes(pos), 0, 4);
				pos += rle0SegmentSizes[x];
			}

			for (int x = 0; x < width; x++) {
				int y = 0;
				while (y < height) {
					(bool isRle, int end) = DfBitmap.GetNextRle(buffer, x, y, height, (a, b) => a == 0 && b == 0);

					int start = x * height + y;
					int length = end - start;

					if (isRle) {
						stream.WriteByte((byte)(length | 0x80));
					} else {
						stream.WriteByte((byte)length);
						await stream.WriteAsync(buffer, start, length);
					}
					y += length;
				}
			}
		}

		public override async Task SaveAsync(Stream stream) {
			this.ClearWarnings();

			this.header.CellOffset = (uint)Marshal.SizeOf<Header>();

			await this.SaveHeaderAsync(stream);
			await this.SaveCellAsync(stream);
		}

		object ICloneable.Clone() => this.Clone();
		public DfFrame Clone(Dictionary<byte[], byte[]> cellClones = null) {
			DfFrame clone = new() {
				Flip = this.Flip,
				Height = this.Height,
				InsertionPointX = this.InsertionPointX,
				InsertionPointY = this.InsertionPointY,
				Width = this.Width
			};
			if (cellClones?.TryGetValue(this.Pixels, out byte[] cellClone) ?? false) {
				clone.Pixels = cellClone;
			} else {
				clone.Pixels = this.Pixels.ToArray();
				if (cellClones != null) {
					cellClones[this.Pixels] = clone.Pixels;
				}
			}
			return clone;
		}
	}
}
