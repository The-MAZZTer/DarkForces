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
					if (new string(this.Subchunk1Id) != "data") {
						return false;
					}
					return true;
				}
				set {
					this.ChunkId = "RIFF".ToCharArray();
					this.Format = "WAVE".ToCharArray();
					this.Subchunk1Id = "fmt ".ToCharArray();
					this.AudioFormat = AudioFormats.Pcm;
					this.Subchunk1Size = 16;
					this.Subchunk2Id = "data".ToCharArray();
					this.ChunkSize = this.Subchunk2Size + this.Subchunk1Size + 20;
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
			Header header = await stream.ReadAsync<Header>();
			if (!header.IsValid) {
				throw new FormatException($"WAV header invalid or unspported audio format!");
			}

			this.Data = new byte[header.Subchunk2Size];
			await stream.ReadAsync(this.Data, 0, (int)header.Subchunk2Size);
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
