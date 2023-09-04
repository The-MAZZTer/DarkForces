using MZZT.Extensions;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MZZT.FileFormats {
	public class Wave : File<Wave> {
		public enum AudioFormats : short {
			Pcm = 0x1
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
		public struct Header {
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public char[] ChunkId;
			public uint ChunkSize;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public char[] Format;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public char[] Subchunk1Id;
			public uint Subchunk1Size;
			public AudioFormats AudioFormat;
			public ushort NumChannels;
			public uint SampleRate;
			public uint ByteRate;
			public ushort BlockAlign;
			public ushort BitsPerSample;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public char[] Subchunk2Id;
			public uint Subchunk2Size;

			public bool IsValid {
				get {
					if (new string(this.ChunkId) != "RIFF") {
						return false;
					}
					if (this.ChunkSize != this.Subchunk2Size + this.Subchunk1Size + 20) {
						return false;
					}
					if (new string(this.Format) != "WAVE") {
						return false;
					}
					if (new string(this.Subchunk1Id) != "fmt ") {
						return false;
					}
					if (this.AudioFormat != AudioFormats.Pcm) {
						return false; // not supported
					}
					if (this.Subchunk1Size != 16) {
						return false;
					}
					if (new string(this.Subchunk2Id) != "data") {
						return false;
					}
					return true;
				}
				set {
					this.ChunkId = value ? "RIFF".ToCharArray() : null;
					this.Format = value ? "WAVE".ToCharArray() : null;
					this.Subchunk1Id = value ? "fmt ".ToCharArray() : null;
					this.AudioFormat = value ? AudioFormats.Pcm : 0;
					this.Subchunk1Size = value ? (uint)16 : 0;
					this.Subchunk2Id = value ? "data".ToCharArray() : null;
					this.ChunkSize = value ? this.Subchunk2Size + this.Subchunk1Size + 20 : 0;
				}
			}
		}

		private Header header;

		public ushort Channels {
			get => this.header.NumChannels;
			set => this.header.NumChannels = value;
		}

		public uint SampleRate {
			get => this.header.SampleRate;
			set => this.header.SampleRate = value;
		}

		public ushort BitsPerSample {
			get => this.header.BitsPerSample;
			set => this.header.BitsPerSample = value;
		}

		public byte[] Data { get; set; }

		public override bool CanLoad => true;

		public override async Task LoadAsync(Stream stream) {
			this.header = await stream.ReadAsync<Header>();
			if (!this.header.IsValid) {
				throw new FormatException($"WAV header invalid or unspported audio format!");
			}

			this.Data = new byte[this.header.Subchunk2Size];
			await stream.ReadAsync(this.Data, 0, (int)this.header.Subchunk2Size);
		}

		public override bool CanSave => true;

		public override async Task SaveAsync(Stream stream) {
			this.header.Subchunk2Size = (uint)this.Data.Length;
			this.header.IsValid = true;
			this.header.ByteRate = (uint)(this.SampleRate * this.Channels * (this.BitsPerSample / 8));
			this.header.BlockAlign = (ushort)(this.Channels * (this.BitsPerSample / 8));

			await stream.WriteAsync(this.header);

			await stream.WriteAsync(this.Data, 0, this.Data.Length);
		}
	}
}
