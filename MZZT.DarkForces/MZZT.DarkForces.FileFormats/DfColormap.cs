using MZZT.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MZZT.DarkForces.FileFormats {
	/// <summary>
	/// A Dark Forces CMP file.
	/// </summary>
	public class DfColormap : DfFile<DfColormap>, ICloneable {
		/// <summary>
		/// The full file data.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct Data {
			/// <summary>
			/// The colormaps for each light level.
			/// </summary>
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256 * 32)]
			public byte[] PaletteMaps;
			/// <summary>
			/// The headlight light levels.
			/// </summary>
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
			public byte[] HeadlightLightLevels;
		}

		/// <summary>
		/// The colormaps for each light level.
		/// </summary>
		public byte[][] PaletteMaps { get; set; }
		/// <summary>
		/// The headlight light levels.
		/// </summary>
		public byte[] HeadlightLightLevels { get; set; }

		public override bool CanLoad => true;

		public override async Task LoadAsync(Stream stream) {
			this.ClearWarnings();

			(Data data, int size) = await stream.ReadWithSizeAsync<Data>();
			if (size < Marshal.SizeOf<Data>()) {
				this.AddWarning("Unexpected end of file.");
			}

			this.PaletteMaps = (byte[][])Array.CreateInstance(typeof(byte[]), 32);
			for (int i = 0; i < 32; i++) {
				this.PaletteMaps[i] = new byte[256];
				Buffer.BlockCopy(data.PaletteMaps, 256 * i, this.PaletteMaps[i], 0, 256);
			}
			this.HeadlightLightLevels = data.HeadlightLightLevels;
		}

		public override bool CanSave => true;

		public override async Task SaveAsync(Stream stream) {
			this.ClearWarnings();

			if (this.PaletteMaps.Length != 32 || this.PaletteMaps.Any(x => x.Length != 256)) {
				throw new FormatException("Expected 32 light levels and a 256 color map in PaletteMaps!");
			}
			if (this.HeadlightLightLevels.Length != 128) {
				throw new FormatException("Expected 128 light levels for the headlight!");
			}
			/*if (this.HeadlightLightLevels.Any(x => x > 0x1F)) {
				throw new FormatException($"All light values in headlight map must be <= 0x1F!");
			}*/

			Data data = new() {
				PaletteMaps = new byte[256 * 32],
				HeadlightLightLevels = this.HeadlightLightLevels
			};
			for (int i = 0; i < 32; i++) {
				Buffer.BlockCopy(this.PaletteMaps[i], 0, data.PaletteMaps, 256 * i, 256);
			}

			await stream.WriteAsync(data);
		}

		object ICloneable.Clone() => this.Clone();
		public DfColormap Clone() => new() {
			HeadlightLightLevels = this.HeadlightLightLevels.ToArray(),
			PaletteMaps = this.PaletteMaps.Select(x => x.ToArray()).ToArray()
		};
	}
}
