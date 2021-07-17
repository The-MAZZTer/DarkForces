using System.IO;
using System.Threading.Tasks;

namespace MZZT.FileFormats {
	/// <summary>
	/// Represents raw file data, can be used to load or save any file type as a raw byte array.
	/// </summary>
	public class Raw : File<Raw> {
		/// <summary>
		/// The raw bytes from the file.
		/// </summary>
		public byte[] Data { get; set; }

		public override bool CanLoad => true;

		public override async Task LoadAsync(Stream stream) {
			using MemoryStream mem = new();
			await stream.CopyToAsync(mem);
			this.Data = mem.ToArray();
		}

		public override bool CanSave => true;

		public override async Task SaveAsync(Stream stream) {
			await stream.WriteAsync(this.Data, 0, this.Data.Length);
		}
	}
}
