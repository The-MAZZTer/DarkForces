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
	/// A Landru FONT file.
	/// </summary>
	public class LandruFont : DfFile<LandruFont> {
		/// <summary>
		/// The FONT file header.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct Header {
			/// <summary>
			/// The first character the font supports.
			/// </summary>
			public ushort First;
			/// <summary>
			/// The number of characters included.
			/// </summary>
			public ushort Length;
			/// <summary>
			/// The number of bits each line encodes (eg the max character width);
			/// </summary>
			public ushort BitsPerLine;
			/// <summary>
			/// The height of the font.
			/// </summary>
			public ushort Height;
			/// <summary>
			/// Unknown.
			/// </summary>
			public short Unknown;
			/// <summary>
			/// Unknown.
			/// </summary>
			public short Padding;
		}

		private Header header;

		/// <summary>
		/// The first character the font supports.
		/// </summary>
		public ushort First {
			get => this.header.First;
			set => this.header.First = value;
		}

		/// <summary>
		/// The height of the font.
		/// </summary>
		public ushort Height {
			get => this.header.Height;
			set => this.header.Height = value;
		}

		/// <summary>
		/// A character.
		/// </summary>
		public struct Character {
			/// <summary>
			/// Character width.
			/// </summary>
			public byte Width;
			/// <summary>
			/// The raw 1-bit pixel data.
			/// </summary>
			public BitArray Pixels;
		}

		/// <summary>
		/// The characters in the font.
		/// </summary>
		public List<Character> Characters { get; } = new();

		/// <summary>
		/// The max width of any character in the font.
		/// </summary>
		public byte BitsPerLine => this.Characters.Max(x => x.Width);

		/// <summary>
		/// The minimu required bytes per character line to encode the data.
		/// </summary>
		public byte BytesPerLine => (byte)Math.Ceiling(this.header.BitsPerLine / 8f);

		public override bool CanLoad => true;

		public override async Task LoadAsync(Stream stream) {
			this.ClearWarnings();

			this.Characters.Clear();

			this.header = await stream.ReadAsync<Header>();

			ushort height = this.header.Height;

			byte[] widths = new byte[this.header.Length];
			await stream.ReadAsync(widths, 0, this.header.Length);

			foreach (byte width in widths) {
				int bytes = (int)Math.Ceiling(this.header.BitsPerLine / 8f) * height;
				byte[] buffer = new byte[bytes];
				await stream.ReadAsync(buffer, 0, bytes);
				
				BitArray bits = new(buffer);
				// BitArray stores its bits for each byte in the opposite order which we need to use them to draw.
				// So swap them around here.
				for (int i = 0; i < bits.Length; i += 8) {
					for (int j = 0; j < 4; j++) {
						int rbit = 7 - j;

						bits[i + j] = bits[i + j] ^ bits[i + rbit];
						bits[i + rbit] = bits[i + j] ^ bits[i + rbit];
						bits[i + j] = bits[i + j] ^ bits[i + rbit];
					}
				}

				this.Characters.Add(new() {
					Width = width,
					Pixels = bits
				});
			}
		}

		public override bool CanSave => true;

		public override async Task SaveAsync(Stream stream) {
			this.ClearWarnings();

			this.header.Length = (ushort)this.Characters.Count;
			byte maxWidth = this.BitsPerLine;
			this.header.BitsPerLine = maxWidth;

			int widthBytes = this.BytesPerLine;

			await stream.WriteAsync(this.header);

			byte[] widths = this.Characters.Select(x => x.Width).ToArray();
			await stream.WriteAsync(widths, 0, widths.Length);

			byte[] buffer = new byte[widthBytes * this.header.Height];
			foreach (Character character in this.Characters) {
				BitArray bits = character.Pixels;
				for (int i = 0; i < bits.Length; i += 8) {
					for (int j = 0; j < 4; j++) {
						int rbit = 7 - j;

						bits[i + j] = bits[i + j] ^ bits[i + rbit];
						bits[i + rbit] = bits[i + j] ^ bits[i + rbit];
						bits[i + j] = bits[i + j] ^ bits[i + rbit];
					}
				}

				character.Pixels.CopyTo(buffer, 0);

				for (int i = 0; i < bits.Length; i += 8) {
					for (int j = 0; j < 4; j++) {
						int rbit = 7 - j;

						bits[i + j] = bits[i + j] ^ bits[i + rbit];
						bits[i + rbit] = bits[i + j] ^ bits[i + rbit];
						bits[i + j] = bits[i + j] ^ bits[i + rbit];
					}
				}

				await stream.WriteAsync(buffer, 0, widthBytes * this.header.Height);
			}
		}
	}
}
