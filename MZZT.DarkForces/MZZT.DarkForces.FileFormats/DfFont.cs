using MZZT.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MZZT.DarkForces.FileFormats {
	/// <summary>
	/// A Dark Forces FNT file.
	/// </summary>
	public class DfFont : DfFile<DfFont> {
		/// <summary>
		/// The magic number.
		/// </summary>
		public const int MAGIC = 0x15544E46;

		/// <summary>
		/// The file header.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct Header {
			/// <summary>
			/// The magic number.
			/// </summary>
			public int Magic;
			/// <summary>
			/// The font height.
			/// </summary>
			public byte Height;
			/// <summary>
			/// Unknown
			/// </summary>
			public byte Unknown;
			/// <summary>
			/// Unknown
			/// </summary>
			public byte Unknown2;
			/// <summary>
			/// Unknown
			/// </summary>
			public byte Unknown3;
			/// <summary>
			/// The first character in the font.
			/// </summary>
			public byte First;
			/// <summary>
			/// The last character in the font.
			/// </summary>
			public byte Last;
			/// <summary>
			/// Unknown.
			/// </summary>
			public int Padding;
			/// <summary>
			/// Unknown.
			/// </summary>
			public int Padding2;
			/// <summary>
			/// Unknown.
			/// </summary>
			public int Padding3;
			/// <summary>
			/// Unknown.
			/// </summary>
			public int Padding4;
			/// <summary>
			/// Unknown.
			/// </summary>
			public int Padding5;
			/// <summary>
			/// Unknown.
			/// </summary>
			public short Padding6;

			/// <summary>
			/// Whether the header is valid.
			/// </summary>
			public bool IsMagicValid => this.Magic == MAGIC;
		}

		/// <summary>
		/// A header for each character.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct CharHeader {
			/// <summary>
			/// The width of the character.
			/// </summary>
			public byte Width;
		}

		private Header header;

		/// <summary>
		/// The first character in the font.
		/// </summary>
		public byte First {
			get => this.header.First;
			set => this.header.First = value;
		}

		/// <summary>
		/// The raw data for each font character.
		/// </summary>
		public List<byte[,]> Characters { get; set; }

		public override bool CanLoad => true;

		public override async Task LoadAsync(Stream stream) {
			this.ClearWarnings();

			this.Characters.Clear();

			this.header = await stream.ReadAsync<Header>();

			if (!this.header.IsMagicValid) {
				throw new FormatException("FNT file header not found.");
			}

			int height = this.header.Height;
			for (int i = this.header.First; i <= this.header.Last; i++) {
				int width = (await stream.ReadAsync<CharHeader>()).Width;

				byte[] buffer = new byte[width * height];
				await stream.ReadAsync(buffer, 0, width * height);
				byte[,] pixels = new byte[height, width];
				for (int x = 0; x < width; x++) {
					for (int y = 0; y < height; y++) {
						pixels[y, x] = buffer[x * height + y];
					}
				}
				this.Characters.Add(pixels);
			}
		}

		public override bool CanSave => true;

		public override async Task SaveAsync(Stream stream) {
			this.ClearWarnings();

			int count = this.header.Last - this.header.First + 1;
			if (this.Characters.Select(x => x.GetLength(0)).Distinct().Count() > 1) {
				throw new FormatException("All characters must have the same height!");
			}

			this.header.Last = (byte)(this.First + this.Characters.Count - 1);

			int height = this.Characters.First().GetLength(0);
			this.header.Height = (byte)height;
			this.header.Magic = MAGIC;

			await stream.WriteAsync(this.header);

			foreach (byte[,] pixels in this.Characters) {
				int width = pixels.GetLength(1);
				await stream.WriteAsync(new CharHeader() {
					Width = (byte)width
				});

				byte[] buffer = new byte[width * height];
				for (int x = 0; x < width; x++) {
					for (int y = 0; y < height; y++) {
						buffer[x * height + y] = pixels[y, x];
					}
				}
				await stream.WriteAsync(buffer, 0, width * height);
			}
		}
	}
}
