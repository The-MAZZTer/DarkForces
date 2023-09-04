using MZZT.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MZZT.FileFormats.Audio {
	public class Midi : File<Midi> {
		[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
		public struct ChunkHeader {
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public char[] Type;
			public int Size;
		}

		public enum TempoFormats {
			TicksPerBeat,
			FramesPerSecond
		}

		public enum Formats : short {
			SingleTrack = 0,
			MultiTrack = 1,
			MultiSong = 2
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct TracksHeader {
			public Formats Format;
			public short Tracks;
			public ushort Division;

			public short Tempo {
				get => (short)(this.Division & 0x7FFF);
				set => this.Division = (ushort)((this.Division & 0x8000) | (value & 0x7FFF));
			}

			public bool TempoIsFramesPerSecond {
				get => (this.Division & 0x8000) > 0;
				set => this.Division = (ushort)((this.Division & 0x7FFF) | (value ? 0x8000 : 0));
			}
		}

		private TracksHeader tracksHeader;

		public Formats Format {
			get => this.tracksHeader.Format;
			set => this.tracksHeader.Format = value;
		}
		public short Tempo {
			get => this.tracksHeader.Tempo;
			set => this.tracksHeader.Tempo = value;
		}
		public bool TempoIsFramesPerSecond {
			get => this.tracksHeader.TempoIsFramesPerSecond;
			set => this.tracksHeader.TempoIsFramesPerSecond = value;
		}
		public List<byte[]> TrackData { get; } = new();
		public Dictionary<string, List<byte[]>> Chunks { get; } = new();

		public override bool CanLoad => true;

		public override async Task LoadAsync(Stream stream) {
			this.Tempo = 0x1E0;
			this.Chunks.Clear();
			this.TrackData.Clear();

			int trackCount = 1;

			while (this.TrackData.Count < trackCount) {
				ChunkHeader chunkHeader = await stream.ReadAsync<ChunkHeader>(Endianness.Big);

				string type = new(chunkHeader.Type);
				switch (type) {
					case "MThd": {
						this.tracksHeader = await stream.ReadAsync<TracksHeader>(Endianness.Big);
						trackCount = this.tracksHeader.Tracks;
					} break;
					case "MTrk": {
						byte[] data = new byte[chunkHeader.Size];
						await stream.ReadAsync(data, 0, chunkHeader.Size);
						this.TrackData.Add(data);
					} break;
					default: {
						if (!this.Chunks.TryGetValue(type, out List<byte[]> values)) {
							this.Chunks[type] = values = new();
						}

						byte[] data = new byte[chunkHeader.Size];
						await stream.ReadAsync(data, 0, chunkHeader.Size);
						values.Add(data);
					} break;
				}
			}
		}

		public override bool CanSave => true;

		public override async Task SaveAsync(Stream stream) {
			foreach ((string type, byte[] value) in
				this.Chunks.SelectMany(x => x.Value.Select(y => (x.Key, y)))) {

				await stream.WriteAsync(new ChunkHeader() {
					Type = type.ToCharArray(),
					Size = value.Length
				}, Endianness.Big);
				await stream.WriteAsync(value, 0, value.Length);
			}

			await stream.WriteAsync(new ChunkHeader() {
				Type = "MThd".ToCharArray(),
				Size = Marshal.SizeOf<TracksHeader>()
			}, Endianness.Big);
			this.tracksHeader.Tracks = (short)this.TrackData.Count;
			await stream.WriteAsync(this.tracksHeader, Endianness.Big);

			foreach (byte[] data in this.TrackData) {
				await stream.WriteAsync(new ChunkHeader() {
					Type = "MTrk".ToCharArray(),
					Size = data.Length
				}, Endianness.Big);
				await stream.WriteAsync(data, 0, data.Length);
			}
		}
	}
}
