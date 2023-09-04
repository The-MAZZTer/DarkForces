// https://stackoverflow.com/questions/24074641/how-to-read-8-bit-png-image-as-8-bit-png-image-only
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>
/// Image loading toolset class which corrects the bug that prevents paletted PNG images with transparency from being loaded as paletted.
/// </summary>
public class BitmapLoader {
	private static byte[] PNG_IDENTIFIER = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

	/// <summary>
	/// Loads an image, checks if it is a PNG containing palette transparency, and if so, ensures it loads correctly.
	/// The theory can be found at http://www.libpng.org/pub/png/book/chapter08.html
	/// </summary>
	/// <param name="filename">Filename to load</param>
	/// <returns>The loaded image</returns>
	public static Bitmap LoadBitmap(string filename) {
		byte[] data = File.ReadAllBytes(filename);
		return LoadBitmap(data);
	}

	/// <summary>
	/// Loads an image, checks if it is a PNG containing palette transparency, and if so, ensures it loads correctly.
	/// The theory can be found at http://www.libpng.org/pub/png/book/chapter08.html
	/// </summary>
	/// <param name="data">File data to load</param>
	/// <returns>The loaded image</returns>
	public static Bitmap LoadBitmap(byte[] data) {
		byte[] transparencyData = null;
		if (data.Length > PNG_IDENTIFIER.Length) {
			// Check if the image is a PNG.
			byte[] compareData = new byte[PNG_IDENTIFIER.Length];
			Array.Copy(data, compareData, PNG_IDENTIFIER.Length);
			if (PNG_IDENTIFIER.SequenceEqual(compareData)) {
				// Check if it contains a palette.
				// I'm sure it can be looked up in the header somehow, but meh.
				int plteOffset = FindChunk(data, "PLTE");
				if (plteOffset != -1) {
					// Check if it contains a palette transparency chunk.
					int trnsOffset = FindChunk(data, "tRNS");
					if (trnsOffset != -1) {
						// Get chunk
						int trnsLength = GetChunkDataLength(data, trnsOffset);
						transparencyData = new byte[trnsLength];
						Array.Copy(data, trnsOffset + 8, transparencyData, 0, trnsLength);
						// filter out the palette alpha chunk, make new data array
						byte[] data2 = new byte[data.Length - (trnsLength + 12)];
						Array.Copy(data, 0, data2, 0, trnsOffset);
						int trnsEnd = trnsOffset + trnsLength + 12;
						Array.Copy(data, trnsEnd, data2, trnsOffset, data.Length - trnsEnd);
						data = data2;
					}
				}
			}
		}
		Bitmap loadedImage;
		using (MemoryStream ms = new(data)) {
			loadedImage = new Bitmap(ms);
		}
		ColorPalette pal = loadedImage.Palette;
		if (pal.Entries.Length == 0 || transparencyData == null) {
			return loadedImage;
		}
		for (int i = 0; i < pal.Entries.Length; i++) {
			if (i >= transparencyData.Length) {
				break;
			}
			Color col = pal.Entries[i];
			pal.Entries[i] = Color.FromArgb(transparencyData[i], col.R, col.G, col.B);
		}
		loadedImage.Palette = pal;
		return loadedImage;
	}

	/// <summary>
	/// Finds the start of a png chunk. This assumes the image is already identified as PNG.
	/// It does not go over the first 8 bytes, but starts at the start of the header chunk.
	/// </summary>
	/// <param name="data">The bytes of the png image</param>
	/// <param name="chunkName">The name of the chunk to find.</param>
	/// <returns>The index of the start of the png chunk, or -1 if the chunk was not found.</returns>
	private static int FindChunk(byte[] data, string chunkName) {
		if (chunkName.Length != 4)
			throw new ArgumentException("Chunk must be 4 characters!", "chunkName");
		byte[] chunkNamebytes = Encoding.ASCII.GetBytes(chunkName);
		if (chunkNamebytes.Length != 4)
			throw new ArgumentException("Chunk must be 4 characters!", "chunkName");
		int offset = PNG_IDENTIFIER.Length;
		int end = data.Length;
		byte[] testBytes = new byte[4];
		// continue until either the end is reached, or there is not enough space behind it for reading a new chunk
		while (offset + 12 <= end) {
			Array.Copy(data, offset + 4, testBytes, 0, 4);
			// Alternative for more visual debugging:
			//String currentChunk = Encoding.ASCII.GetString(testBytes);
			//if (chunkName.Equals(currentChunk))
			//    return offset;
			if (chunkNamebytes.SequenceEqual(testBytes))
				return offset;
			int chunkLength = GetChunkDataLength(data, offset);
			// chunk size + chunk header + chunk checksum = 12 bytes.
			offset += 12 + chunkLength;
		}
		return -1;
	}

	private static int GetChunkDataLength(byte[] data, int offset) {
		if (offset + 4 > data.Length)
			throw new IndexOutOfRangeException("Bad chunk size in png image.");
		// Don't want to use BitConverter; then you have to check platform endianness and all that mess.
		int length = data[offset + 3] + (data[offset + 2] << 8) + (data[offset + 1] << 16) + (data[offset] << 24);
		if (length < 0)
			throw new IndexOutOfRangeException("Bad chunk size in png image.");
		return length;
	}
}