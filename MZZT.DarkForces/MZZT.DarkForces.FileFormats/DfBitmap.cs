using MZZT.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MZZT.DarkForces.FileFormats {
	/// <summary>
	/// Dark Forces BM files.
	/// </summary>
	public class DfBitmap : DfFile<DfBitmap>, ICloneable {
		/// <summary>
		/// The magic number in a BM header.
		/// </summary>
		public const int MAGIC = 0x1E204D42;

		/// <summary>
		/// The BM header.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct Header {
			/// <summary>
			/// The magic number.
			/// </summary>
			public int Magic;
			/// <summary>
			/// The width of the image.
			/// </summary>
			public ushort Width;
			/// <summary>
			/// The height of the image.
			/// </summary>
			public ushort Height;
			/// <summary>
			/// -2 for a multi-BM.
			/// </summary>
			public short IdemX;
			/// <summary>
			/// The number of pages for a multi-BM.
			/// </summary>
			public short IdemY;
			/// <summary>
			/// Properties of the BM.
			/// </summary>
			public Flags Flags;
			/// <summary>
			/// The log of the height, for images with a height of a power of two.
			/// </summary>
			public byte LogSizeY;
			/// <summary>
			/// The type of compression used on the BM data.
			/// </summary>
			public CompressionModes Compression;
			/// <summary>
			/// The size of the BM data.
			/// </summary>
			public int DataSize;
			/// <summary>
			/// Unknown values.
			/// </summary>
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
			public byte[] Padding;

			/// <summary>
			/// Validate the header.
			/// </summary>
			public bool IsMagicValid => this.Magic == MAGIC;
			/// <summary>
			/// Determine if the header data indicates a multi-BM file or not.
			/// </summary>
			public bool IsMultiPage => this.Width == 1 && this.Height != 1;
			/// <summary>
			/// Determine the number of images in this BM.
			/// </summary>
			public int Pages => this.IsMultiPage ? this.IdemY : 1;
		}

		/// <summary>
		/// A header for a multi-BM.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct MultiHeader {
			/// <summary>
			/// A framerate to play back an animated BM at.
			/// </summary>
			public byte Framerate;
			/// <summary>
			/// Unknown.
			/// </summary>
			public byte Unknown;
		}

		/// <summary>
		/// A header for a page of a multi-BM.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct MultiPageHeader {
			/// <summary>
			/// The width of this page.
			/// </summary>
			public ushort Width;
			/// <summary>
			/// The height of this page.
			/// </summary>
			public ushort Height;
			/// <summary>
			/// Unknown, same as width.
			/// </summary>
			public short IdemX;
			/// <summary>
			/// Unknown, same as height.
			/// </summary>
			public short IdemY;
			/// <summary>
			/// The size of the BM data.
			/// </summary>
			public int DataSize;
			/// <summary>
			/// The log of the height, for images with a height of a power of two.
			/// </summary>
			public byte LogSizeY;
			/// <summary>
			/// Unknown.
			/// </summary>
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public byte[] Padding;
			/// <summary>
			/// Unknown.
			/// </summary>
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public byte[] Unknown;
			/// <summary>
			/// Unknown.
			/// </summary>
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
			public byte[] Padding2;
			/// <summary>
			/// Properties of the page.
			/// </summary>
			public Flags Flags;
			/// <summary>
			/// Unknown.
			/// </summary>
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public byte[] Padding3;
		}
		
		/// <summary>
		/// Represents an image in the BM.
		/// </summary>
		public class Page : ICloneable {
			internal MultiPageHeader Header;

			/// <summary>
			/// Properties of the page.
			/// </summary>
			public Flags Flags {
				get => this.Header.Flags;
				set => this.Header.Flags = value;
			}
			/// <summary>
			/// Width of the image.
			/// </summary>
			public ushort Width {
				get => this.Header.Width;
				set => this.Header.Width = value;
			}
			/// <summary>
			/// Height of the image.
			/// </summary>
			public ushort Height {
				get => this.Header.Height;
				set => this.Header.Height = value;
			}

			/// <summary>
			/// Raw pixel data, top to bottom. left to right, as palette indices.
			/// </summary>
			public byte[] Pixels;

			object ICloneable.Clone() => this.Clone();
			public Page Clone() => new() {
				Flags = this.Flags,
				Height = this.Height,
				Pixels = this.Pixels.ToArray(),
				Width = this.Width
			};
		}

		/// <summary>
		/// Possible property flags on a BM.
		/// </summary>
		[Flags]
		public enum Flags : byte {
			/// <summary>
			/// Color 0 is transparent.
			/// </summary>
			Transparent = 0x8,
			/// <summary>
			/// BM is not a weapon but a normal texture (thus must have power of two height).
			/// </summary>
			NotWeapon = 0x36
		}

		/// <summary>
		/// Types of compression.
		/// </summary>
		public enum CompressionModes : short {
			/// <summary>
			/// No compression.
			/// </summary>
			None = 0,
			/// <summary>
			/// Run length encoding.
			/// </summary>
			Rle = 1,
			/// <summary>
			/// Run length encoding but only for color 0 pixels.
			/// </summary>
			Rle0 = 2
		}

		public override bool CanLoad => true;

		public override async Task LoadAsync(Stream stream) {
			this.ClearWarnings();

			this.header = await stream.ReadAsync<Header>();
			if (!this.header.IsMagicValid) {
				throw new FormatException("BM file header not found.");
			}

			Page page;
			if (this.header.IsMultiPage) {
				this.multiHeader = await stream.ReadAsync<MultiHeader>();

				int pages = this.header.Pages;
				this.Pages.Clear();
				this.Pages.Capacity = pages;

				byte[] buffer = new byte[4 * pages];
				await stream.ReadAsync(buffer, 0, 4 * pages);
				uint[] pageDataOffsets = Enumerable.Range(0, pages).Select(x => BitConverter.ToUInt32(buffer, x * 4)).ToArray();

				uint offsetPos = (uint)pages * 4;
				for (int i = 0; i < pages; i++) {
					page = new();

					int advance = (int)(pageDataOffsets[i] - offsetPos);
					if (advance != 0) {
						if (stream.CanSeek) {
							stream.Seek(advance, SeekOrigin.Current);
						} else if (advance > 0) {
							buffer = new byte[advance];
							if (stream.Read(buffer, 0, advance) < advance) {
								throw new EndOfStreamException();
							}
							buffer = null;
						}
					}
					offsetPos += (uint)advance;

					page.Header = await stream.ReadAsync<MultiPageHeader>();
					offsetPos += (uint)Marshal.SizeOf<MultiPageHeader>();

					page.Pixels = await this.ReadRawPixelDataAsync(stream, page.Header.Width, page.Header.Height);
					offsetPos += (uint)(page.Header.Width * page.Header.Height);

					this.Pages.Add(page);
				}
				return;
			}

			this.multiHeader = default;

			page = new() {
				Header = new() {
					DataSize = this.header.DataSize,
					Flags = this.header.Flags,
					Height = this.header.Height,
					IdemX = this.header.IdemX,
					IdemY = this.header.IdemY,
					LogSizeY = this.header.LogSizeY,
					Width = this.header.Width
				}
			};

			if (this.header.Compression == CompressionModes.None) {
				page.Pixels = await this.ReadRawPixelDataAsync(stream, this.header.Width, this.header.Height);
			} else {
				page.Pixels = await this.ReadCompressedPixelDataAsync(stream);
			}

			this.Pages.Clear();
			this.Pages.Add(page);
		}

		private async Task<byte[]> ReadRawPixelDataAsync(Stream stream, ushort width, ushort height) {
			// Images are stored in horizontal pages with vertical lines,
			// but the standard format is veritcal pages with horizintal lines, so we're going to adjust it.
			byte[] ret = new byte[width * height];
			byte[] buffer = new byte[width * height];
			if (await stream.ReadAsync(buffer, 0, width * height) < width * height) {
				throw new EndOfStreamException();
			}
			int pos = 0;
			for (int x = 0; x < width; x++) {
				for (int y = 0; y < height; y++) {
					ret[y * width + x] = buffer[pos++];
				}
			}
			return ret;
		}

		private async Task<byte[]> ReadCompressedPixelDataAsync(Stream stream) {
			ushort width = this.header.Width;
			ushort height = this.header.Height;

			byte[] ret = new byte[height * width];
			byte[] data = new byte[this.header.DataSize];
			if (await stream.ReadAsync(data, 0, this.header.DataSize) != this.header.DataSize) {
				throw new EndOfStreamException();
			}

			byte[] buffer = new byte[4 * width];
			await stream.ReadAsync(buffer, 0, 4 * width);
			uint[] dataStarts = Enumerable.Range(0, width).Select(x => BitConverter.ToUInt32(buffer, x * 4)).ToArray();

			for (int x = 0; x < width; x++) {
				uint pos = dataStarts[x];
				uint dataEnd = x >= width - 1 ? (uint)data.LongLength : dataStarts[x + 1];

				int y = 0;
				while (pos < dataEnd && y < height) {
					byte length = data[pos];
					pos++;

					bool useRle = length > 128;
					if (useRle) {
						length -= 128;
					}

					byte color = 0;
					if (useRle && this.header.Compression != CompressionModes.Rle0) {
						color = data[pos];
						pos++;
					}

					for (int i = y; i < y + length && i < height; i++) {
						if (!useRle) {
							color = data[pos];
							pos++;
						}

						ret[i * width + x] = color;
					}
					y += length;
				}
			}
			return ret;
		}

		private Header header;
		private MultiHeader multiHeader;
		/// <summary>
		/// The images stored in this BM.
		/// </summary>
		public List<Page> Pages { get; } = new(); 

		/// <summary>
		/// The framerate for a multipage animated BM.
		/// </summary>
		public byte Framerate {
			get => this.multiHeader.Framerate;
			set => this.multiHeader.Framerate = value;
		}

		/// <summary>
		/// Whether or not to choose an optimal compression mode automatically.
		/// </summary>
		public bool AutoCompress { get; set; }

		/// <summary>
		/// The compression to use for single-page BMs.
		/// </summary>
		public CompressionModes Compression {
			get {
				if (this.Pages.Count != 1) {
					return CompressionModes.None;
				}
				return this.header.Compression;
			}
			set => this.header.Compression = value;
		}

		public override bool CanSave => true;

		private int GetNoCompressionSize() {
			Page page = this.Pages[0];
			int width = page.Width;
			int height = page.Height;
			return width * height;
		}

		private int[] GetRleCompressionSizes(byte[] buffer) {
			Page page = this.Pages[0];
			int width = page.Width;
			int height = page.Height;

			// We can precompute the size of an RLE compressed image and figure out which is the most
			// optimal compression method.
			int[] rleSegmentSizes = new int[width];
			for (int x = 0; x < width; x++) {
				int yRle = 0;
				while (yRle < height) {
					(bool isRle, int end) = GetNextRle(buffer, x, yRle, height);

					int start = x * height + yRle;
					int length = end - start;

					if (isRle) {
						rleSegmentSizes[x] += 2;
					} else {
						rleSegmentSizes[x] += length + 1;
					}
					yRle += length;
				}
			}
			return rleSegmentSizes;
		}

		private int[] GetRle0CompressionSizes(byte[] buffer) {
			Page page = this.Pages[0];
			int width = page.Width;
			int height = page.Height;

			// We can precompute the size of an RLE compressed image and figure out which is the most
			// optimal compression method.
			int[] rle0SegmentSizes = new int[width];
			for (int x = 0; x < width; x++) {
				int yRle = 0;
				while (yRle < height) {
					(bool isRle, int end) = GetNextRle(buffer, x, yRle, height, (a, b) => (a == 0 && b == 0));

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
			return rle0SegmentSizes;
		}

		public override async Task SaveAsync(Stream stream) {
			this.ClearWarnings();

			if (this.Pages.Count == 0) {
				throw new FormatException("BMs must have at least one page!");
			}

			this.header.Magic = MAGIC;
			if (this.Pages.Count != 1) {
				int size = Marshal.SizeOf<MultiHeader>() + this.Pages.Sum(x => 4 + Marshal.SizeOf<MultiPageHeader>() + x.Pixels.Length);
				if (size > ushort.MaxValue) {
					throw new FormatException("MultiBMs do not support that much data!");
				}

				this.header.Width = 1;
				this.header.Height = (ushort)size;
				this.header.IdemX = -2;
				this.header.IdemY = (short)this.Pages.Count;
				this.header.Flags = 0;
				this.header.LogSizeY = 0;
				this.header.Compression = CompressionModes.None;
				this.header.DataSize = 0;

				await stream.WriteAsync(this.header);

				this.multiHeader.Unknown = 2;

				await stream.WriteAsync(this.multiHeader);

				uint offset = (uint)this.Pages.Count * 4;
				foreach (Page page in this.Pages) {
					await stream.WriteAsync(BitConverter.GetBytes(offset), 0, 4);
					offset += (uint)(Marshal.SizeOf<MultiPageHeader>() + page.Pixels.Length);
				}

				foreach (Page page in this.Pages) {
					int width = page.Width;
					int height = page.Height;
					page.Header.IdemX = (short)width;
					page.Header.IdemY = (short)height;
					page.Header.DataSize = width * height;
					page.Header.LogSizeY = 0;

					await stream.WriteAsync(page.Header);

					byte[] buffer = new byte[width * height];
					for (int x = 0; x < width; x++) {
						for (int y = 0; y < height; y++) {
							buffer[x * height + y] = page.Pixels[y * width + x];
						}
					}
					await stream.WriteAsync(buffer, 0, width * height);
				}
			} else {
				Page page = this.Pages[0];
				int width = page.Width;
				int height = page.Height;
				if (width == 1 && height != 1) {
					throw new FormatException("BMs cannot have a width of 1 and a height of > 1 unless it is a multi-page BM.");
				}

				this.header.Width = (ushort)width;
				this.header.Height = (ushort)height;
				this.header.IdemX = (short)width;
				this.header.IdemY = (short)height;
				this.header.Flags = page.Header.Flags;
				if ((this.header.Flags & Flags.NotWeapon) == 0) {
					this.header.LogSizeY = 0;
				} else {
					double log = Math.Log(height, 2);
					if (Math.Abs(log % 1) > 0.001) {
						throw new FormatException("BMs must have a height that is a power of two, unless they are weapon BMs.");
					}
					this.header.LogSizeY = (byte)log;
				}

				byte[] buffer = new byte[width * height];
				for (int x = 0; x < width; x++) {
					for (int y = 0; y < height; y++) {
						buffer[x * height + y] = page.Pixels[y * width + x];
					}
				}

				int noCompressionSize = -1;
				int rleCompressionSize = -1;
				int[] rleSegmentSizes = null;
				int rle0CompressionSize = -1;
				int[] rle0SegmentSizes = null;
				if (this.AutoCompress) {
					noCompressionSize = this.GetNoCompressionSize();
					rleSegmentSizes = this.GetRleCompressionSizes(buffer);
					rleCompressionSize = rleSegmentSizes.Sum();
					rle0SegmentSizes = this.GetRle0CompressionSizes(buffer);
					rle0CompressionSize = rle0SegmentSizes.Sum();

					if (noCompressionSize <= rleCompressionSize + (4 * width) && noCompressionSize <= rle0CompressionSize + (4 * width)) {
						this.header.Compression = CompressionModes.None;
					} else if (rleCompressionSize <= rle0CompressionSize) {
						this.header.Compression = CompressionModes.Rle;
					} else {
						this.header.Compression = CompressionModes.Rle0;
					}
				}

				switch (this.header.Compression) {
					case CompressionModes.None:
						if (noCompressionSize < 0) {
							noCompressionSize = this.GetNoCompressionSize();
						}
						this.header.DataSize = noCompressionSize;
						await stream.WriteAsync(this.header);

						await stream.WriteAsync(buffer, 0, buffer.Length);
						break;
					case CompressionModes.Rle:
						if (rleCompressionSize < 0) {
							rleSegmentSizes = this.GetRleCompressionSizes(buffer);
							rleCompressionSize = rleSegmentSizes.Sum();
						}
						this.header.DataSize = rleCompressionSize;
						await stream.WriteAsync(this.header);

						for (int x = 0; x < width; x++) {
							int y = 0;
							while (y < height) {
								(bool isRle, int end) = GetNextRle(buffer, x, y, height);

								int start = x * height + y;
								int length = end - start;

								if (isRle) {
									stream.WriteByte((byte)(length | 0x80));
									stream.WriteByte(buffer[start]);
								} else {
									stream.WriteByte((byte)length);
									await stream.WriteAsync(buffer, start, length);
								}
								y += length;
							}
						}

						int pos = 0;
						for (int x = 0; x < width; x++) {
							await stream.WriteAsync(BitConverter.GetBytes(pos), 0, 4);
							pos += rleSegmentSizes[x];
						}
						break;
					case CompressionModes.Rle0:
						if (rle0CompressionSize < 0) {
							rle0SegmentSizes = this.GetRle0CompressionSizes(buffer);
							rle0CompressionSize = rle0SegmentSizes.Sum();
						}
						this.header.DataSize = rle0CompressionSize;
						await stream.WriteAsync(this.header);

						for (int x = 0; x < width; x++) {
							int y = 0;
							while (y < height) {
								(bool isRle, int end) = GetNextRle(buffer, x, y, height, (a, b) => a == 0 && b == 0);

								int start = x * height + y;
								int length = end - start;

								if (x == 15) {
									Debug.WriteLine(length);
								}

								if (isRle) {
									stream.WriteByte((byte)(length | 0x80));
								} else {
									stream.WriteByte((byte)length);
									await stream.WriteAsync(buffer, start, length);
								}
								y += length;
							}
						}

						pos = 0;
						for (int x = 0; x < width; x++) {
							await stream.WriteAsync(BitConverter.GetBytes(pos), 0, 4);
							pos += rle0SegmentSizes[x];
						}
						break;
				}
			}
		}

		internal static (bool isRle, int end) GetNextRle(byte[] buffer, int x, int y, int height, Func<byte, byte, bool> rleTest = null) {
			int start = x * height + y;
			int max = (x + 1) * height;

			rleTest ??= (a, b) => a == b;

			if (max - start <= 1) {
				return (false, max);
			}

			byte first = buffer[start];
			byte second = buffer[start + 1];
			if (rleTest(first, second)) {
				int end = start + 2;
				while (end < max && (end - start) < 127 && rleTest(first, second)) {
					second = buffer[end];
					end++;
				}
				if (!rleTest(first, second)) {
					end--;
				}

				return (true, end);
			} else {
				int end = start + 2;
				while (end < max && (end - start) < 127 && !rleTest(first, second)) {
					first = second;
					second = buffer[end];
					end++;
				}
				if (rleTest(first, second)) {
					end -= 2;
				}

				return (false, end);
			}
		}

		object ICloneable.Clone() => this.Clone();
		public DfBitmap Clone() {
			DfBitmap clone = new() {
				Framerate = this.Framerate,
				Compression = this.Compression,
				AutoCompress = this.AutoCompress
			};
			clone.Pages.AddRange(this.Pages.Select(x => x.Clone()));
			return clone;
		}
	}
}
