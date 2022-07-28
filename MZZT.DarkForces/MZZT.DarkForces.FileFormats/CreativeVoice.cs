using MZZT.Extensions;
using MZZT.FileFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MZZT.DarkForces.FileFormats {
	/// <summary>
	/// Represents data for a VOC/VOIC file.
	/// </summary>
	public class CreativeVoice : DfFile<CreativeVoice>, ICloneable {
		/// <summary>
		/// The file header of the VOC.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
		private struct Header {
			/// <summary>
			/// The magic value.
			/// </summary>
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
			public char[] FileType;
			/// <summary>
			/// Size of the header.
			/// </summary>
			public ushort Size;
			/// <summary>
			/// File version.
			/// </summary>
			public ushort Version;
			/// <summary>
			/// Checksum of the version.
			/// </summary>
			public ushort Checksum;

			/// <summary>
			/// Validate the data in this struct. or force it to be valid.
			/// </summary>
			public bool IsValid {
				get {
					if (new string(this.FileType) != "Creative Voice File\x1A") {
						return false;
					}
					if (this.Size != Marshal.SizeOf<Header>()) {
						return false;
					}
					if (this.Checksum != unchecked((ushort)(~this.Version + 0x1234))) {
						return false;
					}
					return true;
				}
				set {
					if (value) {
						this.FileType = "Creative Voice File\x1A".ToCharArray();
						this.Size = (ushort)Marshal.SizeOf<Header>();
						this.Checksum = unchecked((ushort)(~this.Version + 0x1234));
					} else {
						this.FileType = null;
						this.Size = 0;
						this.Checksum = 0;
					}
				}
			}
		}

		/// <summary>
		/// Minimal header of a VOC block.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct BlockHeader {
			public BlockTypes Type;
		}

		/// <summary>
		/// Different VOC block types.
		/// </summary>
		public enum BlockTypes : byte {
			Terminator = 0x0,
			LegacyAudioData = 0x1,
			LegacyAudioDataContinued = 0x2,
			Silence = 0x3,
			Marker = 0x4,
			Text = 0x5,
			Repeat = 0x6,
			EndRepeat = 0x7,
			LegacyAudioProperties = 0x8,
			AudioData = 0x9
		}

		/// <summary>
		/// The various VOC codecs supported by the legacy block types. Dark Forces only supports Unsigned8BitPcm.
		/// </summary>
		private enum LegacyCodecs : byte {
			Unsigned8BitPcm = 0x0,
			Unsigned4BitTo8BitCreativeAdPcm = 0x1,
			Unsigned3BitTo8BitCreativeAdPcm = 0x2,
			Unsigned2BitTo8BitCreativeAdPcm = 0x3,
			Signed16BitPcm = 0x4,
			Alaw = 0x6,
			Ulaw = 0x7
		}

		/// <summary>
		/// Header for old format for storing audio data.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct LegacyAudioDataHeader {
			/// <summary>
			/// Field used to calculate frequency/sample rate.
			/// </summary>
			public byte FrequencyDivisor;
			/// <summary>
			/// The codec used to encode the audio data in this block.
			/// </summary>
			public LegacyCodecs Codec;

			/// <summary>
			/// The frequency/sample rate.
			/// </summary>
			public uint Frequency {
				get => (uint)Math.Round(1_000_000f / (256 - this.FrequencyDivisor));
				set => this.FrequencyDivisor = (byte)(256 - Math.Round(1_000_000f / value));
			}
		}

		/// <summary>
		/// An audio block used to encode silence efficiently.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct SilenceBlock {
			/// <summary>
			/// The length of the silence in samples, minus one.
			/// </summary>
			public ushort LengthMinusOne;
			/// <summary>
			/// Field used to calculate frequency/sample rate (used to determine time length).
			/// </summary>
			public byte FrequencyDivisor;

			/// <summary>
			/// The length of the silence in samples.
			/// </summary>
			public int Length {
				get => this.LengthMinusOne + 1;
				set => this.LengthMinusOne = (ushort)(value - 1);
			}

			/// <summary>
			/// The frequency/sample rate.
			/// </summary>
			public uint Frequency {
				get => (uint)Math.Round(1_000_000f / (256 - this.FrequencyDivisor));
				set => this.FrequencyDivisor = (byte)(256 - Math.Round(1_000_000f / value));
			}
		}

		/// <summary>
		/// Configures audio block settings for following blocks.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct LegacyAudioPropertiesBlock {
			/// <summary>
			/// Field used to calculate frequency/sample rate (used to determine time length).
			/// </summary>
			public ushort FrequencyDivisor;
			/// <summary>
			/// The codec used to encode the audio data in this block.
			/// </summary>
			public LegacyCodecs Codec;
			/// <summary>
			/// Number of sound channels minus one.
			/// </summary>
			public byte ChannelsMinusOne;

			/// <summary>
			/// The frequency/sample rate.
			/// </summary>
			public uint Frequency {
				get => (uint)Math.Round(256_000_000f / (this.Channels * (65536 - this.FrequencyDivisor)));
				set => this.FrequencyDivisor = (ushort)(65536 - Math.Round(256_000_000f / value / this.Channels));
			}

			/// <summary>
			/// Number of sound channels.
			/// </summary>
			public byte Channels {
				get => (byte)(this.ChannelsMinusOne + 1);
				set => this.ChannelsMinusOne = (byte)(value - 1);
			}
		}

		/// <summary>
		/// Header for audio data block.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct AudioDataHeader {
			/// <summary>
			/// The frequency/sample rate.
			/// </summary>
			public uint Frequency;
			/// <summary>
			/// Bits used to store each sample (based on codec).
			/// </summary>
			public byte BitsPerSample;
			/// <summary>
			/// Number of sound channels.
			/// </summary>
			public byte Channels;
			/// <summary>
			/// The codec used to encode the audio data in this block.
			/// </summary>
			public Codecs Codec;
			public uint Reserved;
		}

		/// <summary>
		/// The various VOC codecs. Dark Forces only supports Unsigned8BitPcm.
		/// </summary>
		public enum Codecs : ushort {
			Unsigned8BitPcm = 0x0,
			Unsigned4BitTo8BitCreativeAdPcm = 0x1,
			Unsigned3BitTo8BitCreativeAdPcm = 0x2,
			Unsigned2BitTo8BitCreativeAdPcm = 0x3,
			Signed16BitPcm = 0x4,
			Alaw = 0x6,
			Ulaw = 0x7,
			Unsigned4BitTo16BitCreativeAdPcm = 0x200
		}

		/// <summary>
		/// A block of audio data.
		/// </summary>
		public class AudioData : ICloneable {
			/// <summary>
			/// The type of block. Not all fields are applicable for all blocks/
			/// </summary>
			public BlockTypes Type { get; set; }

			/// <summary>
			/// The frequency/sample rate.
			/// </summary>
			public uint Frequency { get; set; }
			/// <summary>
			/// Bits used to store each sample (based on codec).
			/// </summary>
			public byte BitsPerSample { get; set; }
			/// <summary>
			/// Number of sound channels.
			/// </summary>
			public byte Channels { get; set; }
			/// <summary>
			/// The codec used to encode the audio data in this block.
			/// </summary>
			public Codecs Codec { get; set; }
			/// <summary>
			/// The raw audio samples, for block types which include raw audio data.
			/// </summary>
			public byte[] Data { get; set; }

			/// <summary>
			/// For the silence block type, how many samples the silence lasts.
			/// </summary>
			public int SilenceLength { get; set; }

			/// <summary>
			/// The index in the AudioBlocks array the player should jump to after playing this block.
			/// </summary>
			public int RepeatStart { get; set; }
			/// <summary>
			/// The number of times RepeatStart to this current block should be played.
			/// </summary>
			public int RepeatCount { get; set; }

			/// <summary>
			/// Whether or not the RepeatStart to current block should be repeated infinitely.
			/// </summary>
			public bool RepeatInfinitely {
				get => this.RepeatCount >= ushort.MaxValue;
				set => this.RepeatCount = value ? ushort.MaxValue : 0;
			}

			object ICloneable.Clone() => this.Clone();
			public AudioData Clone() => new() {
				BitsPerSample = this.BitsPerSample,
				Channels = this.Channels,
				Codec = this.Codec,
				Data = this.Data.ToArray(),
				Frequency = this.Frequency,
				RepeatCount = this.RepeatCount,
				RepeatStart = this.RepeatStart,
				SilenceLength = this.SilenceLength,
				Type = this.Type
			};
		}

		/// <summary>
		/// The audio blocks associated with this VOC.
		/// </summary>
		public List<AudioData> AudioBlocks { get; } = new List<AudioData>();

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct Marker {
			public ushort Value;
		}

		/// <summary>
		/// A marker from the VOC.
		/// </summary>
		public struct MarkerData {
			/// <summary>
			/// Which audio block, based on index, this marker comes before.
			/// </summary>
			public int BeforeAudioDataIndex;
			/// <summary>
			/// The marker data.
			/// </summary>
			public ushort Value;
		}

		/// <summary>
		/// Markers present in the VOC.
		/// </summary>
		public List<MarkerData> Markers { get; } = new List<MarkerData>();

		/// <summary>
		/// Comments from the VOC.
		/// </summary>
		public struct Comment {
			/// <summary>
			/// Which audio block, based on index, this comment comes before.
			/// </summary>
			public int BeforeAudioDataIndex;
			/// <summary>
			/// The comment.
			/// </summary>
			public string Value;
		}

		/// <summary>
		/// Comments present in the VOC.
		/// </summary>
		public List<Comment> Comments { get; } = new List<Comment>();

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct RepeatBlock {
			public ushort CountMinusOne;

			public int Count {
				get => this.CountMinusOne + 1;
				set => this.CountMinusOne = (ushort)(value - 1);
			}
		}

		public override bool CanLoad => true;

		public override async Task LoadAsync(Stream stream) {
			this.ClearWarnings();

			Header header = await stream.ReadAsync<Header>();
			if (!header.IsValid) {
				throw new FormatException("VOC file header not found.");
			}

			this.AudioBlocks.Clear();
			this.Markers.Clear();
			this.Comments.Clear();

			int lastRepeat = -1;
			int repeatCount = 0;
			BlockHeader blockHeader;
			AudioData lastProperties = null;
			while (true) {
				// Keep reading blocks until the end.
				blockHeader = await stream.ReadAsync<BlockHeader>();
				if (blockHeader.Type == BlockTypes.Terminator) {
					if (this.AudioBlocks.Count == 0) {
						this.AddWarning("File has no audio blocks?");
					}
					break;
				}

				switch (blockHeader.Type) {
					// Audio data plus a header.
					case BlockTypes.LegacyAudioData: {
						byte[] sizeBytes = new byte[4];
						await stream.ReadAsync(sizeBytes, 0, 3);
						int size = BitConverter.ToInt32(sizeBytes, 0);

						LegacyAudioDataHeader dataHeader = await stream.ReadAsync<LegacyAudioDataHeader>();

						AudioData data = lastProperties = new AudioData() {
							Type = blockHeader.Type,
							BitsPerSample = dataHeader.Codec switch {
								LegacyCodecs.Unsigned2BitTo8BitCreativeAdPcm => 2,
								LegacyCodecs.Unsigned3BitTo8BitCreativeAdPcm => 3,
								LegacyCodecs.Unsigned4BitTo8BitCreativeAdPcm => 4,
								LegacyCodecs.Signed16BitPcm => 16,
								_ => 8
							},
							Channels = 1,
							Codec = (Codecs)dataHeader.Codec,
							Frequency = dataHeader.Frequency
						};

						size -= Marshal.SizeOf<LegacyAudioDataHeader>();
						byte[] bytes = new byte[size];
						await stream.ReadAsync(bytes, 0, size);
						data.Data = bytes;

						this.AudioBlocks.Add(data);
					} break;
					// Audio data, reuse previous header.
					case BlockTypes.LegacyAudioDataContinued: {
						byte[] sizeBytes = new byte[4];
						await stream.ReadAsync(sizeBytes, 0, 3);
						int size = BitConverter.ToInt32(sizeBytes, 0);

						AudioData last = lastProperties;
						if (last == null) {
							throw new FormatException($"Unexpected LegacyAudioDataContinued block... no previous LegacyAudioData block!");
						}

						AudioData data = new() {
							Type = blockHeader.Type,
							BitsPerSample = last.BitsPerSample,
							Channels = last.Channels,
							Codec = last.Codec,
							Frequency = last.Frequency
						};

						byte[] bytes = new byte[size];
						await stream.ReadAsync(bytes, 0, size);
						data.Data = bytes;

						this.AudioBlocks.Add(data);
					} break;
					// Silence
					case BlockTypes.Silence: {
						byte[] sizeBytes = new byte[4];
						await stream.ReadAsync(sizeBytes, 0, 3);
						int size = BitConverter.ToInt32(sizeBytes, 0);

						SilenceBlock block = await stream.ReadAsync<SilenceBlock>(limit: size);

						AudioData data = new() {
							Type = blockHeader.Type,
							Frequency = block.Frequency,
							SilenceLength = block.Length
						};

						this.AudioBlocks.Add(data);
					} break;
					// A marker.
					case BlockTypes.Marker: {
						byte[] sizeBytes = new byte[4];
						await stream.ReadAsync(sizeBytes, 0, 3);
						int size = BitConverter.ToInt32(sizeBytes, 0);

						Marker marker = await stream.ReadAsync<Marker>(limit: size);

						MarkerData data = new() {
							BeforeAudioDataIndex = this.AudioBlocks.Count,
							Value = marker.Value
						};

						this.Markers.Add(data);
					} break;
					// A comment.
					case BlockTypes.Text: {
						byte[] sizeBytes = new byte[4];
						await stream.ReadAsync(sizeBytes, 0, 3);
						int size = BitConverter.ToInt32(sizeBytes, 0);

						byte[] bytes = new byte[size];
						await stream.ReadAsync(bytes, 0, size);
						string text = Encoding.ASCII.GetString(bytes).TrimEnd('\0');

						Comment data = new() {
							BeforeAudioDataIndex = this.AudioBlocks.Count,
							Value = text
						};

						this.Comments.Add(data);
					} break;
					// Indicates the start of a loop.
					case BlockTypes.Repeat: {
						byte[] sizeBytes = new byte[4];
						await stream.ReadAsync(sizeBytes, 0, 3);
						int size = BitConverter.ToInt32(sizeBytes, 0);

						RepeatBlock repeat = await stream.ReadAsync<RepeatBlock>(limit: size);

						// I think it's safe to assume you can't nest repeat blocks so this should be fine.
						lastRepeat = this.AudioBlocks.Count;
						repeatCount = repeat.Count;
					} break;
					// Indicates the end of a loop.
					case BlockTypes.EndRepeat: {
						if (lastRepeat < 0) {
							this.AddWarning("Unexpected EndRepeat block without previous Repeat block.");
							break;
						}

						if (this.AudioBlocks.Count > lastRepeat) {
							AudioData last = this.AudioBlocks.Last();
							last.RepeatStart = lastRepeat;
							last.RepeatCount = repeatCount;
						}

						lastRepeat = -1;
						repeatCount = -1;
					} break;
					// More detailed audio properites, but no audio data.
					case BlockTypes.LegacyAudioProperties: {
						byte[] sizeBytes = new byte[4];
						await stream.ReadAsync(sizeBytes, 0, 3);
						int size = BitConverter.ToInt32(sizeBytes, 0);

						LegacyAudioPropertiesBlock block = await stream.ReadAsync<LegacyAudioPropertiesBlock>(limit: size);

						lastProperties = new AudioData() {
							Type = blockHeader.Type,
							BitsPerSample = block.Codec switch {
								LegacyCodecs.Unsigned2BitTo8BitCreativeAdPcm => 2,
								LegacyCodecs.Unsigned3BitTo8BitCreativeAdPcm => 3,
								LegacyCodecs.Unsigned4BitTo8BitCreativeAdPcm => 4,
								LegacyCodecs.Signed16BitPcm => 16,
								_ => 8
							},
							Channels = block.Channels,
							Codec = (Codecs)block.Codec,
							Frequency = block.Frequency
						};
					} break;
					// A more detailed modern block type which supports more options.
					case BlockTypes.AudioData: {
						byte[] sizeBytes = new byte[4];
						await stream.ReadAsync(sizeBytes, 0, 3);
						int size = BitConverter.ToInt32(sizeBytes, 0);

						AudioDataHeader dataHeader = await stream.ReadAsync<AudioDataHeader>();

						AudioData data = lastProperties = new AudioData() {
							Type = blockHeader.Type,
							BitsPerSample = dataHeader.Codec switch {
								Codecs.Unsigned2BitTo8BitCreativeAdPcm => 2,
								Codecs.Unsigned3BitTo8BitCreativeAdPcm => 3,
								Codecs.Unsigned4BitTo8BitCreativeAdPcm => 4,
								Codecs.Unsigned4BitTo16BitCreativeAdPcm => 4,
								Codecs.Signed16BitPcm => 16,
								_ => 8
							},
							Channels = dataHeader.Channels,
							Codec = dataHeader.Codec,
							Frequency = dataHeader.Frequency
						};

						size -= Marshal.SizeOf<AudioDataHeader>();
						byte[] bytes = new byte[size];
						await stream.ReadAsync(bytes, 0, size);
						data.Data = bytes;

						this.AudioBlocks.Add(data);
					} break;
					default: {
						this.AddWarning($"Unknown VOC block type {blockHeader.Type}, skipping.");

						byte[] sizeBytes = new byte[4];
						await stream.ReadAsync(sizeBytes, 0, 3);
						int size = BitConverter.ToInt32(sizeBytes, 0);
						if (stream.CanSeek) {
							stream.Seek(size, SeekOrigin.Current);
						} else {
							byte[] bytes = new byte[size];
							await stream.ReadAsync(bytes, 0, size);
						}
					} break;
				}
			}
		}

		public override bool CanSave => true;

		public override async Task SaveAsync(Stream stream) {
			this.ClearWarnings();

			Header header = new() {
				Version = this.AudioBlocks.Any(x => x.Type == BlockTypes.AudioData) ? (ushort)0x114 : (ushort)0x10A,
				IsValid = true
			};
			await stream.WriteAsync(header);

			Dictionary<int, RepeatBlock[]> repeatStarts = this.AudioBlocks
				.Where(x => x.RepeatStart >= 0)
				.GroupBy(x => x.RepeatStart)
				.ToDictionary(x => x.Key, x => x
					.Select(x => new RepeatBlock() {
						Count = x.RepeatCount
					})
					.ToArray());

			BlockHeader blockHeader;
			for (int i = 0; i <= this.AudioBlocks.Count; i++) {
				foreach (Comment comment in this.Comments.Where(x => x.BeforeAudioDataIndex == i)) {
					blockHeader = new() {
						Type = BlockTypes.Text
					};
					await stream.WriteAsync(blockHeader);

					int size = comment.Value.Length + 1;
					await stream.WriteAsync(BitConverter.GetBytes(size), 0, 3);

					await stream.WriteAsync(Encoding.ASCII.GetBytes(comment.Value + '\0'), 0, comment.Value.Length + 1);
				}

				foreach (MarkerData marker in this.Markers.Where(x => x.BeforeAudioDataIndex == i)) {
					blockHeader = new() {
						Type = BlockTypes.Marker
					};
					await stream.WriteAsync(blockHeader);

					int size = Marshal.SizeOf<Marker>();
					await stream.WriteAsync(BitConverter.GetBytes(size), 0, 3);

					Marker markerBlock = new() {
						Value = marker.Value
					};
					await stream.WriteAsync(markerBlock);
				}

				if (repeatStarts.TryGetValue(i, out RepeatBlock[] repeats)) {
					foreach (RepeatBlock repeatBlock in repeats) {
						blockHeader = new() {
							Type = BlockTypes.Repeat
						};
						await stream.WriteAsync(blockHeader);

						int size = Marshal.SizeOf<RepeatBlock>();
						await stream.WriteAsync(BitConverter.GetBytes(size), 0, 3);

						await stream.WriteAsync(repeatBlock);
					}
				}

				if (i >= this.AudioBlocks.Count) {
					break;
				}

				AudioData block = this.AudioBlocks[i];
				blockHeader = new() {
					Type = block.Type
				};
				await stream.WriteAsync(blockHeader);

				switch (block.Type) {
					case BlockTypes.LegacyAudioData: {
						int size = Marshal.SizeOf<LegacyAudioDataHeader>() + block.Data.Length;
						await stream.WriteAsync(BitConverter.GetBytes(size), 0, 3);

						LegacyAudioDataHeader data = new() {
							Codec = (LegacyCodecs)block.Codec,
							Frequency = block.Frequency
						};
						await stream.WriteAsync(data);
						await stream.WriteAsync(block.Data, 0, block.Data.Length);
					} break;
					case BlockTypes.LegacyAudioDataContinued: {
						int size = block.Data.Length;
						await stream.WriteAsync(BitConverter.GetBytes(size), 0, 3);

						await stream.WriteAsync(block.Data, 0, block.Data.Length);
					} break;
					case BlockTypes.Silence: {
						int size = Marshal.SizeOf<SilenceBlock>();
						await stream.WriteAsync(BitConverter.GetBytes(size), 0, 3);

						SilenceBlock data = new() {
							Length = block.SilenceLength,
							Frequency = block.Frequency
						};
						await stream.WriteAsync(data);
					} break;
					case BlockTypes.LegacyAudioProperties: {
						int size = Marshal.SizeOf<LegacyAudioPropertiesBlock>() + block.Data.Length;
						await stream.WriteAsync(BitConverter.GetBytes(size), 0, 3);

						LegacyAudioPropertiesBlock data = new() {
							Channels = block.Channels,
							Codec = (LegacyCodecs)block.Codec,
							Frequency = block.Frequency
						};
						await stream.WriteAsync(data);

					} break;
					case BlockTypes.AudioData: {
						int size = Marshal.SizeOf<AudioDataHeader>() + block.Data.Length;
						await stream.WriteAsync(BitConverter.GetBytes(size), 0, 3);

						AudioDataHeader data = new() {
							BitsPerSample = block.BitsPerSample,
							Channels = block.Channels,
							Codec = block.Codec,
							Frequency = block.Frequency,
							Reserved = 0
						};
						await stream.WriteAsync(data);
						await stream.WriteAsync(block.Data, 0, block.Data.Length);
					} break;
					default:
						this.AddWarning($"Unexpected block type {block.Type}, skipping.");
						break;
				}

				if (block.RepeatStart >= 0) {
					blockHeader = new() {
						Type = BlockTypes.EndRepeat
					};
					await stream.WriteAsync(blockHeader);
				}
			}

			blockHeader = new() {
				Type = BlockTypes.Terminator
			};
			await stream.WriteAsync(blockHeader);
		}

		object ICloneable.Clone() => this.Clone();
		public CreativeVoice Clone() {
			CreativeVoice clone = new();
			clone.AudioBlocks.AddRange(this.AudioBlocks.Select(x => x.Clone()));
			clone.Comments.AddRange(this.Comments);
			clone.Markers.AddRange(this.Markers);
			return clone;
		}

		public IEnumerable<Wave> ToWaves() {
			List<AudioData> datas = this.AudioBlocks;
			for (int i = 0; i < datas.Count; i++) {
				AudioData data = datas[i];
				if (data.Type == BlockTypes.Silence) {
					AudioData source;
					if (i > 0) {
						source = datas[i - 1];
					} else {
						source = datas.Skip(i).FirstOrDefault(x => x.Type != BlockTypes.Silence);
					}
					if (source == null) {
						datas.Clear();
						break;
					}

					data.BitsPerSample = source.BitsPerSample;
					data.Channels = source.Channels;
					data.Codec = source.Codec;
					data.Frequency = source.Frequency;
					// Only works for 8-bit
					data.Data = Enumerable.Repeat<byte>(0x80, data.SilenceLength * data.Channels * data.BitsPerSample / 8).ToArray();
				}

				if (data.RepeatCount > 1) {
					int count = Math.Min(5, data.RepeatCount);
					for (int j = 1; j < count; j++) {
						datas.InsertRange(i + 1, datas.Skip(data.RepeatStart).Take(i - data.RepeatStart + 1));
					}
					i += (count - 1) * (i - data.RepeatStart + 1);
				}
			}

			int index = 0;
			while (datas.Count > 0) {
				index++;

				AudioData[] wavData = datas.TakeWhile(x =>
					x.BitsPerSample == datas[0].BitsPerSample &&
					x.Channels == datas[0].Channels &&
					x.Codec == datas[0].Codec &&
					x.Frequency == datas[0].Frequency
				).ToArray();
				datas.RemoveRange(0, wavData.Length);

				yield return new Wave() {
					BitsPerSample = wavData[0].BitsPerSample,
					Channels = wavData[0].Channels,
					SampleRate = wavData[0].Frequency,
					Data = wavData.SelectMany(x => x.Data).ToArray()
				};
			}
		}
	}
}
