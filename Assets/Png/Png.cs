using Free.Ports.libpng;
using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace MZZT.Drawing {
	public class Png {
		public Png(Stream stream) {
			this.Read(stream);
		}

		public Png(int width, int height, PNG_COLOR_TYPE colorType) {
			this.ColorType = colorType;
			this.Data = (byte[][])Array.CreateInstance(typeof(byte[]), height);

			int pixelSize = this.BytesPerPixel;
			for (int y = 0; y < height; y++) {
				this.Data[y] = new byte[width * pixelSize];
			}
		}

		public int BytesPerPixel => this.ColorType switch {
			PNG_COLOR_TYPE.GRAY => 1,
			PNG_COLOR_TYPE.GRAY_ALPHA => 2,
			PNG_COLOR_TYPE.PALETTE => 1,
			PNG_COLOR_TYPE.RGB => 3,
			PNG_COLOR_TYPE.RGB_ALPHA => 4,
			_ => throw new Exception()
		};

		public byte BitDepth { get; set; } = 8;
		public PNG_COLOR_TYPE ColorType { get; set; } = PNG_COLOR_TYPE.RGB_ALPHA;
		public PNG_INTERLACE InterlaceType { get; set; } = PNG_INTERLACE.NONE;
		public PNG_COMPRESSION_TYPE CompressionType { get; set; } = PNG_COMPRESSION_TYPE.DEFAULT;
		public PNG_FILTER_TYPE FilterType { get; set; } = PNG_FILTER_TYPE.DEFAULT;
		public png_color_16? TransparentColor { get; set; }
		public Color[] Palette { get; set; } = Array.Empty<Color>();

		public uint Height => (uint)this.Data.Length;
		public uint Width => (uint)(this.Data[0].Length / this.BytesPerPixel);
		public byte[][] Data { get; set; } = Array.Empty<byte[]>();

		private void Read(Stream stream) {
			png_struct png = png_struct.png_create_read_struct();
			if (png == null) {
				throw new Exception();
			}

			try {
				png.png_init_io(stream);

				png.png_read_info();

				uint width = png.png_get_image_width();
				uint height = png.png_get_image_height();
				this.BitDepth = png.png_get_bit_depth();
				this.ColorType = png.png_get_color_type();
				this.InterlaceType = png.png_get_interlace_type();
				this.CompressionType = png.png_get_compression_type();
				this.FilterType = png.png_get_filter_type();

				if (this.BitDepth == 16) {
					png.png_set_strip_16();
				}

				if (this.ColorType.HasFlag(PNG_COLOR_TYPE.PALETTE_MASK)) {
					png_color[] palette = new png_color[(int)Math.Pow(2, this.BitDepth)];
					png.png_get_PLTE(ref palette);
					this.Palette = palette.Select(x => Color.FromArgb(x.red, x.green, x.blue)).ToArray();
				} else {
					this.Palette = Array.Empty<Color>();
				}

				if (png.png_get_valid(PNG_INFO.tRNS)) {
					png_color_16 transColor = default!;
					ushort transCount = 0;
					byte[] transBytes = default!;
					png.png_get_tRNS(ref transBytes, ref transCount, ref transColor);

					if (this.ColorType.HasFlag(PNG_COLOR_TYPE.PALETTE_MASK)) {
						for (int i = 0; i < transCount; i++) {
							Color color = this.Palette[i];
							this.Palette[i] = Color.FromArgb(transBytes[i], color.R, color.G, color.B);
						}
						this.TransparentColor = null;
					} else {
						this.TransparentColor = transColor;
					}
				} else {
					this.TransparentColor = null;
				}

				if (!this.ColorType.HasFlag(PNG_COLOR_TYPE.ALPHA_MASK)) {
					png.png_set_filler(0xFF, PNG_FILLER.AFTER);
				}

				png.png_read_update_info();

				this.Data = (byte[][])Array.CreateInstance(typeof(byte[]), height);
				for (int y = 0; y < this.Height; y++) {
					this.Data[y] = new byte[png.png_get_rowbytes()];
				}

				png.png_read_image(this.Data);
			} finally {
				png.png_destroy_read_struct();
			}
		}

		public void ConvertPaletteToRgb() {
			if (this.ColorType.HasFlag(PNG_COLOR_TYPE.PALETTE_MASK)) {
				return;
			}

			bool transparent = this.Palette.Any(x => x.A < 255);
			int bytesPerPixel = transparent ? 4 : 3;

			for (int y = 0; y < this.Height; y++) {
				byte[] row = new byte[this.Data[y].Length * bytesPerPixel];
				for (int x = 0; x < this.Data[y].Length; x++) {
					int index = this.Data[y][x];

					Color color = this.Palette[index];
					row[x * bytesPerPixel] = color.R;
					row[x * bytesPerPixel + 1] = color.G;
					row[x * bytesPerPixel + 2] = color.B;
					if (transparent) {
						row[x * bytesPerPixel + 3] = color.A;
					}
				}
				this.Data[y] = row;
			}

			this.Palette = Array.Empty<Color>();
			this.TransparentColor = null;
			this.ColorType = transparent ? PNG_COLOR_TYPE.RGB_ALPHA : PNG_COLOR_TYPE.RGB;
		}

		public void Write(Stream stream) {
			png_struct png = png_struct.png_create_write_struct();
			if (png == null) {
				throw new Exception();
			}

			try {
				png.png_init_io(stream);

				png.png_set_IHDR(
					this.Width,
					this.Height,
					this.BitDepth,
					this.ColorType,
					this.InterlaceType,
					this.CompressionType,
					this.FilterType
				);

				if (this.ColorType.HasFlag(PNG_COLOR_TYPE.PALETTE_MASK)) {
					png.png_set_PLTE(this.Palette.Select(x => new png_color() {
						red = x.R,
						green = x.G,
						blue = x.B
					}).ToArray());

					if (this.Palette.Any(x => x.A < 255)) {
						png.png_set_tRNS(this.Palette.Select(x => x.A).ToArray());
					}
				} else if (this.TransparentColor != null) {
					png.png_set_tRNS(new png_color_16() {
						red = this.TransparentColor.Value.red,
						green = this.TransparentColor.Value.green,
						blue = this.TransparentColor.Value.blue
					});
				}

				png.png_write_info();

				png.png_write_image(this.Data);
				png.png_write_end();
			} finally {
				png.png_destroy_write_struct();
			}
		}
	}
}
