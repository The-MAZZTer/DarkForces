using MZZT.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static MZZT.FileFormats.Wave;

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
#if !IL2CPP
				ChunkHeader chunkHeader = await stream.ReadAsync<ChunkHeader>(Endianness.Big);
#else
				ChunkHeader chunkHeader = await stream.ReadAsync<ChunkHeader>();
				byte[] a = BitConverter.GetBytes(chunkHeader.Size);
				Array.Reverse(a);
				chunkHeader.Size = BitConverter.ToInt32(a, 0);
#endif

				string type = new(chunkHeader.Type);
				switch (type) {
					case "MThd": {
#if !IL2CPP
						this.tracksHeader = await stream.ReadAsync<TracksHeader>(Endianness.Big);
#else
						this.tracksHeader = await stream.ReadAsync<TracksHeader>();
						a = BitConverter.GetBytes(this.tracksHeader.Division);
						Array.Reverse(a);
						this.tracksHeader.Division = BitConverter.ToUInt16(a, 0);
						a = BitConverter.GetBytes((short)this.tracksHeader.Format);
						Array.Reverse(a);
						this.tracksHeader.Format = (Formats)BitConverter.ToInt16(a, 0);
						a = BitConverter.GetBytes(this.tracksHeader.Tracks);
						Array.Reverse(a);
						this.tracksHeader.Tracks = BitConverter.ToInt16(a, 0);
#endif
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
			int size;
#if IL2CPP
			byte[] a;
#endif
			foreach ((string type, byte[] value) in
				this.Chunks.SelectMany(x => x.Value.Select(y => (x.Key, y)))) {

				size = value.Length;
#if !IL2CPP
				await stream.WriteAsync(new ChunkHeader() {
					Type = type.ToCharArray(),
					Size = size
				}, Endianness.Big);
#else
				a = BitConverter.GetBytes(size);
				Array.Reverse(a);
				size = BitConverter.ToInt32(a, 0);
				await stream.WriteAsync(new ChunkHeader() {
					Type = type.ToCharArray(),
					Size = size
				});
#endif
				await stream.WriteAsync(value, 0, value.Length);
			}

			size = Marshal.SizeOf<TracksHeader>();
#if !IL2CPP
			await stream.WriteAsync(new ChunkHeader() {
				Type = "MThd".ToCharArray(),
				Size = size
			}, Endianness.Big);
#else
			a = BitConverter.GetBytes(size);
			Array.Reverse(a);
			size = BitConverter.ToInt32(a, 0);
			await stream.WriteAsync(new ChunkHeader() {
				Type = "MThd".ToCharArray(),
				Size = size
			});
#endif

			this.tracksHeader.Tracks = (short)this.TrackData.Count;

#if !IL2CPP
			await stream.WriteAsync(this.tracksHeader, Endianness.Big);
#else
			TracksHeader tracksHeader = new() {
				Division = this.tracksHeader.Division,
				Format = this.tracksHeader.Format,
				Tracks = this.tracksHeader.Tracks
			};
			a = BitConverter.GetBytes(tracksHeader.Division);
			Array.Reverse(a);
			tracksHeader.Division = BitConverter.ToUInt16(a, 0);
			a = BitConverter.GetBytes((short)tracksHeader.Format);
			Array.Reverse(a);
			tracksHeader.Format = (Formats)BitConverter.ToInt16(a, 0);
			a = BitConverter.GetBytes(tracksHeader.Tracks);
			Array.Reverse(a);
			tracksHeader.Tracks = BitConverter.ToInt16(a, 0);
			await stream.WriteAsync(tracksHeader);
#endif

			foreach (byte[] data in this.TrackData) {
				size = data.Length;
#if !IL2CPP
				await stream.WriteAsync(new ChunkHeader() {
						Type = "MTrk".ToCharArray(),
					Size = size
				}, Endianness.Big);
#else
				a = BitConverter.GetBytes(size);
				Array.Reverse(a);
				size = BitConverter.ToInt32(a, 0);
				await stream.WriteAsync(new ChunkHeader() {
					Type = "MTrk".ToCharArray(),
					Size = size
				});
#endif
				await stream.WriteAsync(data, 0, data.Length);
			}
		}
	}
}
