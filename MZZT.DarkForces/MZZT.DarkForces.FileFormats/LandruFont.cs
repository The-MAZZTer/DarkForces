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
	public class LandruFont : DfFile<LandruFont>, ICloneable {
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
		public class Character : ICloneable {
			/// <summary>
			/// Character width.
			/// </summary>
			public byte Width { get; set; }
			/// <summary>
			/// The raw 1-bit pixel data.
			/// </summary>
			public BitArray Pixels { get; set; }

			object ICloneable.Clone() => this.Clone();
			public Character Clone() => new() {
				Pixels = new BitArray(this.Pixels),
				Width = this.Width
			};
		}

		public LandruFont() : base() {
			this.First = 32;
			this.Height = 8;
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
		/// The minimum required bytes per character line to encode the data.
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

			int widthBytes = (int)Math.Ceiling(this.header.BitsPerLine / 8f);

			foreach (byte width in widths) {
				int charWidthBytes = (int)Math.Ceiling(width / 8f);

				byte[] buffer;
				int bytes = widthBytes * height;
				if (charWidthBytes == widthBytes) {
					buffer = new byte[bytes];
					await stream.ReadAsync(buffer, 0, bytes);
				} else {
					buffer = new byte[charWidthBytes * height];

					for (int y = 0; y < this.header.Height; y++) {
						await stream.ReadAsync(buffer, y * charWidthBytes, charWidthBytes);
						for (int i = charWidthBytes; i < widthBytes; i++) {
							stream.ReadByte();
						}
					}
				}

				this.Characters.Add(new() {
					Width = width,
					Pixels = new(buffer)
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

				character.Pixels.CopyTo(buffer, 0);

				int charWidthBytes = (int)Math.Ceiling(character.Width / 8f);
				if (widthBytes == charWidthBytes) {
					await stream.WriteAsync(buffer, 0, widthBytes * this.header.Height);
				} else {
					for (int y = 0; y < this.header.Height; y++) {
						await stream.WriteAsync(buffer, y * charWidthBytes, charWidthBytes);
						for (int i = charWidthBytes; i < widthBytes; i++) {
							stream.WriteByte(0);
						}
					}
				}
			}
		}

		object ICloneable.Clone() => this.Clone();
		public LandruFont Clone() {
			LandruFont clone = new() {
				First = this.First,
				Height = this.Height
			};
			clone.Characters.AddRange(this.Characters.Select(x => x.Clone()));
			return clone;
		}
	}
}
