using MZZT.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MZZT.DarkForces.FileFormats {
	/// <summary>
	/// A palette entry.
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
	public struct RgbColor {
		/// <summary>
		/// Red level.
		/// </summary>
		public byte R;
		/// <summary>
		/// Green level.
		/// </summary>
		public byte G;
		/// <summary>
		/// Blue level.
		/// </summary>
		public byte B;
	}

	/// <summary>
	/// A Dark Forces PAL file.
	/// </summary>
	public class DfPalette : DfFile<DfPalette>, ICloneable {
		/// <summary>
		/// Palette data.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct Data {
			/// <summary>
			/// Palette entries. Color values are from 0 - 63 for each channel.
			/// The two high bits per channel seem to shift over the color index used for that color on the menu screen.
			/// Generally this makes the grayscale higher or even fall out of the grayscale palette area entirely.
			/// Interestingly during menu fade or screen flash, these bits don't get masked out it looks like.
			/// So accidental use of value 64 can appear "correct" during flashes.
			/// These bits do look like they control the fade out color of the menu, but this could just be
			/// a side effect of the above bug.
			/// </summary>
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
			public RgbColor[] Palette;
		}

		/// <summary>
		/// The palette data.
		/// </summary>
		public RgbColor[] Palette { get; set; } = new RgbColor[256];

		public override bool CanLoad => true;

		public override async Task LoadAsync(Stream stream) {
			this.ClearWarnings();

			this.Palette = (await stream.ReadAsync<Data>()).Palette;
		}

		public override bool CanSave => true;

		public override async Task SaveAsync(Stream stream) {
			this.ClearWarnings();

			if (this.Palette.Length != 256) {
				this.AddWarning("Palette should have 256 entries!");
			}
			/*if (this.Palette.Any(x => x.R > 0x3F || x.G > 0x3F || x.B > 0x3F)) {
				this.AddWarning("All color values in palette must be <= 0x3F!");
			}*/

			await stream.WriteAsync(new Data() {
				Palette = this.Palette
			});
		}

		object ICloneable.Clone() => this.Clone();
		public DfPalette Clone() => new() {
			Palette = this.Palette.ToArray()
		};
	}
}
