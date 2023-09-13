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
	public class DfFont : DfFile<DfFont>, ICloneable {
		/// <summary>
		/// Data for a single character in the font.
		/// </summary>
		public class Character : ICloneable {
			internal CharHeader header;

			/// <summary>
			/// The width of the character in pixels.
			/// </summary>
			public byte Width {
				get => this.header.Width;
				set => this.header.Width = value;
			}

			/// <summary>
			/// The raw data for the image, in 8-bit pixels, from top to bottom, left to right.
			/// </summary>
			public byte[] Data { get; set; }

			object ICloneable.Clone() => this.Clone();
			public Character Clone() => new() {
				Data = this.Data.ToArray(),
				Width = this.Width
			};
		}

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

		public DfFont() : base() {
			this.First = 33;
			this.Height = 8;
		}

		private Header header;

		/// <summary>
		/// The height of the font in pixels.
		/// </summary>
		public byte Height {
			get => this.header.Height;
			set => this.header.Height = value;
		}

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
		public List<Character> Characters { get; } = new();

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
				Character character = new() {
					header = await stream.ReadAsync<CharHeader>()
				};
				int width = character.Width;

				character.Data = new byte[width * height];
				await stream.ReadAsync(character.Data, 0, width * height);
				this.Characters.Add(character);
			}
		}

		public override bool CanSave => true;

		public override async Task SaveAsync(Stream stream) {
			this.ClearWarnings();

			if (this.Characters.Any(x => x.Data == null)) {
				throw new FormatException("All characters must have their Data field defined.");
			}
			if (this.Characters.Any(x => x.Data.Length != this.Height * x.Width)) {
				throw new FormatException("All characters must have the current Data field size for their width and height.");
			}

			this.header.Last = (byte)(this.First + this.Characters.Count - 1);
			this.header.Magic = MAGIC;

			await stream.WriteAsync(this.header);

			foreach (Character character in this.Characters) {
				await stream.WriteAsync(character.header);

				await stream.WriteAsync(character.Data, 0, character.Data.Length);
			}
		}

		object ICloneable.Clone() => this.Clone();
		public DfFont Clone() {
			DfFont clone = new() {
				First = this.First,
				Height = this.Height
			};
			clone.Characters.AddRange(this.Characters.Select(x => x.Clone()));
			return clone;
		}
	}
}
