using MZZT.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MZZT.DarkForces.FileFormats {
	/// <summary>
	/// A Landru PLTT file.
	/// </summary>
	public class LandruPalette : DfFile<LandruPalette>, ICloneable {
		/// <summary>
		/// The PLTT header.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct Header {
			/// <summary>
			/// The first palette index entry included.
			/// </summary>
			public byte First;
			/// <summary>
			/// The last palette index entry included.
			/// </summary>
			public byte Last;
		}

		private Header header;

		/// <summary>
		/// The first palette index entry included.
		/// </summary>
		public byte First {
			get => this.header.First;
			set => this.header.First = value;
		}
		/// <summary>
		/// The last palette index entry included.
		/// </summary>
		public byte Last {
			get => this.header.Last;
			set => this.header.Last = value;
		}

		public LandruPalette() : base() {
			this.First = 0;
			this.Palette = Enumerable.Range(0, 256).Select(_ => new RgbColor()).ToArray();
			this.Last = 255;
		}

		/// <summary>
		/// The palette colors.
		/// </summary>
		public RgbColor[] Palette { get; set; }

		public override bool CanLoad => true;

		public override async Task LoadAsync(Stream stream) {
			this.ClearWarnings();

			this.header = await stream.ReadAsync<Header>();

			List<RgbColor> colors = new();
			for (int i = this.header.First; i <= this.header.Last; i++) {
				colors.Add(await stream.ReadAsync<RgbColor>());
			}
			this.Palette = colors.ToArray();

			if (stream.ReadByte() != 0) {
				this.AddWarning("Missing end of file marker!");
			}
		}

		public override bool CanSave => true;

		public override async Task SaveAsync(Stream stream) {
			this.ClearWarnings();

			int expected = this.header.Last - this.header.First + 1;
			if (this.Palette.Length != expected) {
				this.AddWarning($"Expected {expected} palette entries, found {this.Palette.Length}!");
				this.header.Last = (byte)(this.header.First + this.Palette.Length - 1);
			}

			await stream.WriteAsync(this.header);
			foreach (RgbColor color in this.Palette) {
				await stream.WriteAsync(color);
			}

			stream.WriteByte(0);
		}

		object ICloneable.Clone() => this.Clone();
		public LandruPalette Clone() => new() {
			First = this.First,
			Last = this.Last,
			Palette = this.Palette.ToArray()
		};
	}
}
