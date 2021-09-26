using MZZT.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MZZT.DarkForces.FileFormats {
	/// <summary>
	/// A Landru ANIM file.
	/// </summary>
	public class LandruAnimation : DfFile<LandruAnimation>, ICloneable {
		/// <summary>
		/// The individual frames of animation as DELTs.
		/// </summary>
		public List<LandruDelt> Pages { get; set; }

		public override bool CanLoad => true;

		public override async Task LoadAsync(Stream stream) {
			this.ClearWarnings();

			byte[] buffer = new byte[4];
			await stream.ReadAsync(buffer, 0, 2);
			int count = BitConverter.ToUInt16(buffer, 0);
			this.Pages = new(count);

			for (int i = 0; i < count; i++) {
				await stream.ReadAsync(buffer, 0, 4);
				uint size = BitConverter.ToUInt32(buffer, 0);

				LandruDelt delt = new();
				//using ScopedStream scopedStream = new(stream, size);
				//await delt.LoadAsync(scopedStream);

				using (MemoryStream mem = new((int)size)) {
					await stream.CopyToWithLimitAsync(mem, (int)size);
					mem.Position = 0;
					await delt.LoadAsync(mem);
				}

				this.Pages.Add(delt);

				/*if (scopedStream.Position < size) {
					if (stream.CanSeek) {
						stream.Seek(size - scopedStream.Position, SeekOrigin.Current);
					} else {
						buffer = new byte[size - scopedStream.Position];
						await stream.ReadAsync(buffer, 0, buffer.Length);
					}
				}*/
			}
		}

		public override bool CanSave => true;

		public override async Task SaveAsync(Stream stream) {
			this.ClearWarnings();

			await stream.WriteAsync(BitConverter.GetBytes((ushort)this.Pages.Count), 0, 2);

			foreach (LandruDelt delt in this.Pages) {
				using MemoryStream memory = new();
				await delt.SaveAsync(memory);

				await stream.WriteAsync(BitConverter.GetBytes((uint)memory.Length), 0, 4);

				memory.Position = 0;
				memory.CopyTo(stream);
			}
		}

		object ICloneable.Clone() => this.Clone();
		public LandruAnimation Clone() {
			LandruAnimation clone = new();
			clone.Pages.AddRange(this.Pages.Select(x => x.Clone()));
			return clone;
		}
	}
}
