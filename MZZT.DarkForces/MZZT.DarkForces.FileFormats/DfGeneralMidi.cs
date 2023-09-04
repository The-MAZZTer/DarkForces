using MZZT.Extensions;
using MZZT.FileFormats.Audio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static MZZT.FileFormats.Audio.Midi;

namespace MZZT.DarkForces.FileFormats {
	/// <summary>
	/// A Dark Forces GMD/GMID file.
	/// </summary>
	public class DfGeneralMidi : DfFile<DfGeneralMidi>, ICloneable {
		/// <summary>
		/// GMID header.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
		private struct Header {
			/// <summary>
			/// Magic value.
			/// </summary>
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public char[] Magic;
			/// <summary>
			/// Size of data.
			/// </summary>
			public int Size;

			/// <summary>
			/// Whether or not the magic value is valid.
			/// </summary>
			public bool IsMagicValid {
				get => new string(this.Magic) == "MIDI";
				set {
					if (value) {
						this.Magic = "MIDI".ToCharArray();
					} else {
						this.Magic = null;
					}
				}
			}
		}

		/// <summary>
		/// Import a Midi object data.
		/// </summary>
		/// <param name="midi">The midi object.</param>
		/// <returns>The DFGeneralMidi created.</returns>
		public static DfGeneralMidi FromMidi(Midi midi) {
			DfGeneralMidi ret = new();
			ret.LoadFromMidi(midi);
			return ret;
		}

		/// <summary>
		/// Unknown contents of the MDPG chunk.
		/// </summary>
		public byte[] Mdpg { get; set; } = Array.Empty<byte>();
		/// <summary>
		/// The MIDI format.
		/// </summary>
		public Formats Format { get; set; } = Formats.MultiSong;
		/// <summary>
		/// The tempo of the music.
		/// </summary>
		public short Tempo { get; set; } = 0x1E0;
		/// <summary>
		/// Whether the tempo is frames per second or ticks per beat.
		/// </summary>
		public bool TempoIsFramesPerSecond { get; set; }
		/// <summary>
		/// The raw data for each track.
		/// </summary>
		public List<byte[]> TrackData { get; } = new List<byte[]>();

		public override bool CanLoad => true;

		private void LoadFromMidi(Midi midi) {
			if (midi.Chunks.TryGetValue("MDpg", out List<byte[]> mdpg)) {
				this.Mdpg = mdpg.FirstOrDefault();
			}

			this.Tempo = midi.Tempo;
			this.TrackData.AddRange(midi.TrackData);
		}

		public override async Task LoadAsync(Stream stream) {
			this.ClearWarnings();

			Header header = await stream.ReadAsync<Header>(Endianness.Big);
			if (!header.IsMagicValid) {
				throw new FormatException("GMID header not found!");
			}

			this.Mdpg = null;
			this.Tempo = 0x1E0;
			this.TrackData.Clear();

			//Midi midi = await Midi.ReadAsync(new ScopedStream(stream, header.Size));
			Midi midi;
			using (MemoryStream mem = new(header.Size)) {
				await stream.CopyToWithLimitAsync(mem, header.Size);
				mem.Position = 0;
				midi = await Midi.ReadAsync(mem);
			}
			if (midi.Chunks.TryGetValue("MDpg", out List<byte[]> mdpg)) {
				this.Mdpg = mdpg.FirstOrDefault();
			}

			this.Format = midi.Format;
			this.Tempo = midi.Tempo;
			this.TempoIsFramesPerSecond = midi.TempoIsFramesPerSecond;
			this.TrackData.AddRange(midi.TrackData);
		}

		public override bool CanSave => true;

		public override async Task SaveAsync(Stream stream) {
			this.ClearWarnings();

			int size = 0;
			if (this.Mdpg != null && this.Mdpg.Length > 0) {
				size += Marshal.SizeOf<ChunkHeader>() + this.Mdpg.Length;
			}
			size += Marshal.SizeOf<ChunkHeader>() + Marshal.SizeOf<TracksHeader>();
			size += this.TrackData.Sum(x => Marshal.SizeOf<ChunkHeader>() + x.Length);

			await stream.WriteAsync(new Header() {
				IsMagicValid = true,
				Size = size
			});

			Midi midi = this.ToMidi();
			await midi.SaveAsync(stream);
		}

		public Midi ToMidi() {
			Midi midi = new() {
				Tempo = this.Tempo,
				Format = this.Format,
				TempoIsFramesPerSecond = this.TempoIsFramesPerSecond
			};
			if (this.Mdpg != null && this.Mdpg.Length > 0) {
				midi.Chunks.Add("Mdpg", new() {
					this.Mdpg
				});
			}
			midi.TrackData.AddRange(this.TrackData);
			return midi;
		}

		object ICloneable.Clone() => this.Clone();
		public DfGeneralMidi Clone() {
			DfGeneralMidi clone = new() {
				Format = this.Format,
				Mdpg = this.Mdpg.ToArray(),
				Tempo = this.Tempo,
				TempoIsFramesPerSecond = this.TempoIsFramesPerSecond
			};
			clone.TrackData.AddRange(this.TrackData.Select(x => x.ToArray()));
			return clone;
		}
	}
}
