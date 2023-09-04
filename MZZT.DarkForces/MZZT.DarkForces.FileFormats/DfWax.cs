using MZZT.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MZZT.DarkForces.FileFormats {
	/// <summary>
	/// A Dark Forces WAX file.
	/// </summary>
	public class DfWax : DfFile<DfWax>, ICloneable {
		/// <summary>
		/// WAX file header.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct Header {
			/// <summary>
			/// WAX file type version.
			/// </summary>
			public int Version;
			/// <summary>
			/// Number of total unique animation sequences in this file.
			/// </summary>
			public int SequenceCount;
			/// <summary>
			/// Number of total unique frames (FMEs) in this file.
			/// </summary>
			public int FrameCount;
			/// <summary>
			/// Number of total unique frame cells in this file.
			/// </summary>
			public int CellCount;  
			/// <summary>
			/// Horizontal scale of the WAX.
			/// </summary>
			public int XScale;
			/// <summary>
			/// Vertical scale of the WAX.
			/// </summary>
			public int YScale;
			/// <summary>
			/// Extra light.
			/// </summary>
			public int ExtraLight;
			/// <summary>
			/// Unknown.
			/// </summary>
			public int Padding;
			/// <summary>
			/// Offsets in the file to sprite headers. Each different state of the sprite needs a different sub WAX.
			/// </summary>
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
			public int[] WaxPointers;
		}

		/// <summary>
		/// Sub header for a sprite displayed in a specific state.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct WaxHeader {
			/// <summary>
			/// Width in the world.
			/// </summary>
			public int WorldWidth;
			/// <summary>
			/// Height in the world.
			/// </summary>
			public int WorldHeight;
			/// <summary>
			/// Framerate of animation.
			/// </summary>
			public int Framerate;
			/// <summary>
			/// Number of frames.
			/// </summary>
			public int NFrames;
			/// <summary>
			/// Unknown.
			/// </summary>
			public int Padding;
			/// <summary>
			/// Unknown.
			/// </summary>
			public int Padding2;
			/// <summary>
			/// Unknown.
			/// </summary>
			public int Padding3;
			/// <summary>
			/// Offsets to animation sequence headers.
			/// </summary>
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
			public int[] SequencePointers;
		}

		/// <summary>
		/// Header for animation sequences.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct SequenceHeader {
			/// <summary>
			/// Unknown.
			/// </summary>
			public int Padding;
			/// <summary>
			/// Unknown.
			/// </summary>
			public int Padding2;
			/// <summary>
			/// Unknown.
			/// </summary>
			public int Padding3;
			/// <summary>
			/// Unknown.
			/// </summary>
			public int Padding4;
			/// <summary>
			/// Offsets to frame headers.
			/// </summary>
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
			public int[] FramePointers;
		}

		private Header header;

		/// <summary>
		/// Sprite states in this WAX file. 
		/// </summary>
		public List<SubWax> Waxes { get; } = new();

		/// <summary>
		/// A sprite state.
		/// </summary>
		public class SubWax : ICloneable {
			internal WaxHeader header;

			/// <summary>
			/// Width in the world.
			/// </summary>
			public int WorldWidth {
				get => this.header.WorldWidth;
				set => this.header.WorldWidth = value;
			}
			/// <summary>
			/// Height in the world.
			/// </summary>
			public int WorldHeight {
				get => this.header.WorldHeight;
				set => this.header.WorldHeight = value;
			}
			/// <summary>
			/// Framerate of animation.
			/// </summary>
			public int Framerate {
				get => this.header.Framerate;
				set => this.header.Framerate = value;
			}

			/// <summary>
			/// Animation sequences in this sprite. Typically there are 32, for different angles of view.
			/// </summary>
			public List<Sequence> Sequences { get; } = new();

			object ICloneable.Clone() => this.Clone();
			public SubWax Clone(Dictionary<Sequence, Sequence> sequenceClones = null,
				Dictionary<DfFrame, DfFrame> frameClones = null,
				Dictionary<byte[], byte[]> cellClones = null) {

				sequenceClones ??= new();
				frameClones ??= new();
				cellClones ??= new();
				
				SubWax clone = new() {
					Framerate = this.Framerate,
					WorldHeight = this.WorldHeight,
					WorldWidth = this.WorldWidth
				};
				foreach (Sequence sequence in this.Sequences) {
					if (!sequenceClones.TryGetValue(sequence, out Sequence sequenceClone)) {
						sequenceClone = sequence.Clone(frameClones, cellClones);
					}
					clone.Sequences.Add(sequenceClone);
				}
				return clone;
			}
		}

		/// <summary>
		/// The animation of a sprite for a specific angle of view.
		/// </summary>
		public class Sequence : ICloneable {
			internal SequenceHeader header;

			/// <summary>
			/// The frames of animation for this sequence.
			/// </summary>
			public List<DfFrame> Frames { get; } = new();

			object ICloneable.Clone() => this.Clone();
			public Sequence Clone(Dictionary<DfFrame, DfFrame> frameClones = null,
				Dictionary<byte[], byte[]> cellClones = null) {

				frameClones ??= new();
				cellClones ??= new();

				Sequence clone = new();
				foreach (DfFrame frame in this.Frames) {
					if (!frameClones.TryGetValue(frame, out DfFrame frameClone)) {
						frameClone = frame.Clone(cellClones);
					}
					clone.Frames.Add(frameClone);
				}
				return clone;
			}
		}

		public override bool CanLoad => true;

		public override async Task LoadAsync(Stream stream) {
			this.ClearWarnings();

			this.header = await stream.ReadAsync<Header>();

			this.Waxes.Clear();

			// WAX files can define multiple States/Animations/Frames (WAXes/Sequences/Frames)
			// that point to the same locations in the file to reuse them.
			// We can do this on the OOP side by reusing the same object for multiple references.

			Dictionary<int, SubWax> waxes = new();
			foreach (int pointer in this.header.WaxPointers.Where(x => x > 0)) {
				if (!waxes.TryGetValue(pointer, out SubWax wax)) {
					waxes[pointer] = wax = new SubWax();
				}
				this.Waxes.Add(wax);
			}

			int pos = Marshal.SizeOf<Header>();

			int nullSeq = 0;

			Dictionary<int, Sequence> sequences = new();
			foreach ((int waxpointer, SubWax wax) in waxes.OrderBy(x => x.Key)) {
				int offset = waxpointer - pos;
				if (offset > 0) {
					byte[] buffer = new byte[offset];
					await stream.ReadAsync(buffer, 0, offset);
				}

				wax.header = await stream.ReadAsync<WaxHeader>();

				pos = waxpointer + Marshal.SizeOf<WaxHeader>();

				foreach (int pointer in wax.header.SequencePointers.Where(x => x > 0)) {
					if (!sequences.TryGetValue(pointer, out Sequence sequence)) {
						sequences[pointer] = sequence = new Sequence();
					}
					wax.Sequences.Add(sequence);
				}

				nullSeq += wax.header.SequencePointers.Where(x => x == 0).Count();
			}

			int nullFme = 0;

			Dictionary<int, DfFrame> frames = new();
			foreach ((int sequencepointer, Sequence sequence) in sequences.OrderBy(x => x.Key)) {
				int offset = sequencepointer - pos;
				if (offset > 0) {
					byte[] buffer = new byte[offset];
					await stream.ReadAsync(buffer, 0, offset);
				}

				sequence.header = await stream.ReadAsync<SequenceHeader>();

				pos = sequencepointer + Marshal.SizeOf<SequenceHeader>();

				foreach (int pointer in sequence.header.FramePointers.Where(x => x > 0)) {
					if (!frames.TryGetValue(pointer, out DfFrame fme)) {
						frames[pointer] = fme = new DfFrame();
					}
					sequence.Frames.Add(fme);
				}

				nullFme += sequence.header.FramePointers.Where(x => x == 0).Count();
			}

			foreach ((int fmepointer, DfFrame fme) in frames.OrderBy(x => x.Key)) {
				int offset = fmepointer - pos;
				if (offset > 0) {
					byte[] buffer = new byte[offset];
					await stream.ReadAsync(buffer, 0, offset);
				}

				await fme.LoadHeaderAsync(stream);

				pos = fmepointer + Marshal.SizeOf<DfFrame.Header>();
			}

			// Cells can also share references even when their frames don't.
			// We handle this by assigning the same header/pixel array references to the different frames.

			foreach ((int cellpointer, DfFrame[] fmes) in frames.Values
				.GroupBy(x => x.header.CellOffset)
				.Select(x => ((int)x.Key, x.ToArray()))
				.OrderBy(x => x.Item1)) {

				int offset = cellpointer - pos;
				if (offset > 0) {
					byte[] buffer = new byte[offset];
					await stream.ReadAsync(buffer, 0, offset);
				}

				DfFrame fme = fmes.First();
				await fme.LoadCellAsync(stream);

				int datasize = fme.cellHeader.Compressed > 0 ? (int)fme.cellHeader.DataSize :
					(Marshal.SizeOf<DfFrame.CellHeader>() + fme.Width * fme.Height);
				pos = cellpointer + datasize;

				foreach (DfFrame other in fmes.Skip(1)) {
					other.cellHeader = fme.cellHeader;
					other.Pixels = fme.Pixels;
				}
			}
		}

		public override bool CanSave => true;

		public override async Task SaveAsync(Stream stream) {
			this.ClearWarnings();

			this.header.Version = 0x11000;
			// Deduplicate references here.
			Sequence[] sequences = this.Waxes.Distinct().SelectMany(x => x.Sequences).Distinct().ToArray();
			DfFrame[] frames = sequences.SelectMany(x => x.Frames).Distinct().ToArray();
			DfFrame[] cells = frames.GroupBy(x => x.Pixels).Select(x => x.First()).ToArray();

			this.header.SequenceCount = sequences.Length;
			this.header.FrameCount = frames.Length;
			this.header.CellCount = cells.Length;

			int pos = Marshal.SizeOf<Header>();

			Dictionary<SubWax, int> waxPointers = new();
			foreach (SubWax wax in this.Waxes) {
				if (waxPointers.ContainsKey(wax)) {
					continue;
				}

				waxPointers[wax] = pos;
				pos += Marshal.SizeOf<WaxHeader>();
			}

			this.header.WaxPointers = this.Waxes.Select(x => waxPointers[x]).Concat(Enumerable.Repeat(0, 32 - this.Waxes.Count)).ToArray();

			await stream.WriteAsync(this.header);

			Dictionary<Sequence, int> sequencePointers = sequences.Select(x => {
				int ret = pos;
				pos += Marshal.SizeOf<SequenceHeader>();
				return (x, ret);
			}).ToDictionary(x => x.x, x => x.ret);

			foreach (SubWax wax in waxPointers.Keys) {
				wax.header.SequencePointers = wax.Sequences.Select(x => sequencePointers[x])
					.Concat(Enumerable.Repeat(0, 32 - wax.Sequences.Count)).ToArray();

				await stream.WriteAsync(wax.header);
			}

			Dictionary<DfFrame, int> framePointers = frames.Select(x => {
				int ret = pos;
				pos += Marshal.SizeOf<DfFrame.Header>();
				return (x, ret);
			}).ToDictionary(x => x.x, x => x.ret);

			foreach (Sequence sequence in sequences) {
				sequence.header.FramePointers = sequence.Frames.Select(x => framePointers[x])
					.Concat(Enumerable.Repeat(0, 32 - sequence.Frames.Count)).ToArray();

				await stream.WriteAsync(sequence.header);
			}

			Dictionary<byte[], MemoryStream> cellData = new();
			Dictionary<byte[], int> cellPointers = new();

			foreach (DfFrame cell in cells) {
				MemoryStream mem = new();
				await cell.SaveCellAsync(mem);
				mem.Position = 0;

				cellData[cell.Pixels] = mem;
				cellPointers[cell.Pixels] = pos;
				pos += (int)mem.Length;
			}

			foreach (DfFrame frame in frames) {
				frame.header.CellOffset = (uint)cellPointers[frame.Pixels];

				await frame.SaveHeaderAsync(stream);
			}

			foreach (DfFrame cell in cells) {
				using (cellData[cell.Pixels]) {
					await cellData[cell.Pixels].CopyToAsync(stream);
				}
			}
		}

		object ICloneable.Clone() => this.Clone();
		public DfWax Clone() {
			DfWax clone = new();
			Dictionary<SubWax, SubWax> waxClones = new();
			Dictionary<Sequence, Sequence> sequenceClones = new();
			Dictionary<DfFrame, DfFrame> frameClones = new();
			Dictionary<byte[], byte[]> cellClones = new();
			foreach (SubWax wax in this.Waxes) {
				if (!waxClones.TryGetValue(wax, out SubWax waxClone)) {
					waxClone = wax.Clone(sequenceClones, frameClones, cellClones);
				}
				clone.Waxes.Add(waxClone);
			}
			return clone;
		}

		/// <summary>
		/// Finds duplicate data and deduplicates it (allows sharing of WAXes, Sequences, Frames, and Cells).
		/// </summary>
		/// <returns>dEduplicated DfWax clone.</returns>
		public DfWax Deduplicate() {
			HashSet<SubWax> waxes = new();
			HashSet<Sequence> sequences = new();
			HashSet<DfFrame> frames = new();
			HashSet<byte[]> cells = new();

			DfWax clone = this.Clone();
			foreach ((SubWax wax, int i) in clone.Waxes.ToArray().Select((x, i) => (x, i))) {
				foreach ((Sequence sequence, int j) in wax.Sequences.ToArray().Select((x, i) => (x, i))) {
					foreach ((DfFrame frame, int k) in sequence.Frames.ToArray().Select((x, i) => (x, i))) {
						foreach (byte[] existing in cells) {
							if (frame.Pixels.SequenceEqual(existing)) {
								frame.Pixels = existing;
								break;
							}
						}
						cells.Add(frame.Pixels);

						foreach (DfFrame existing in frames) {
							if (existing.AutoCompress == frame.AutoCompress &&
								(existing.Compressed == frame.Compressed || existing.AutoCompress) &&
								existing.Flip == frame.Flip &&
								existing.Height == frame.Height &&
								existing.InsertionPointX == frame.InsertionPointX &&
								existing.InsertionPointY == frame.InsertionPointY &&
								existing.Pixels == frame.Pixels &&
								existing.Width == frame.Width) {

								sequence.Frames[k] = existing;
								break;
							}
						}
						frames.Add(sequence.Frames[k]);
					}

					foreach (Sequence existing in sequences) {
						if (existing.Frames.SequenceEqual(sequence.Frames)) {
							wax.Sequences[j] = existing;
							break;
						}
					}
					sequences.Add(wax.Sequences[j]);
				}

				foreach (SubWax existing in waxes) {
					if (existing.Framerate == wax.Framerate &&
						existing.WorldHeight == wax.WorldHeight &&
						existing.WorldWidth == wax.WorldWidth &&
						existing.Sequences.SequenceEqual(wax.Sequences)) {

						clone.Waxes[i] = existing;
						break;
					}
				}
				waxes.Add(clone.Waxes[i]);
			}
			return clone;
		}

		/// <summary>
		/// Finds deduplicated data and reduplicates it for easy editing of specific items (you can call Deduplicate later).
		/// </summary>
		/// <returns>Reduplicated DfWax clone.</returns>
		public DfWax Reduplicate() {
			DfWax clone = new();
			foreach (SubWax wax in this.Waxes) {
				SubWax cloneWax = new() {
					Framerate = wax.Framerate,
					WorldHeight = wax.WorldHeight,
					WorldWidth = wax.WorldWidth
				};
				foreach (Sequence sequence in wax.Sequences) {
					Sequence cloneSequence = new();
					foreach (DfFrame frame in sequence.Frames) {
						cloneSequence.Frames.Add(new() {
							AutoCompress = frame.AutoCompress,
							Compressed = frame.Compressed,
							Flip = frame.Flip,
							Height = frame.Height,
							InsertionPointX = frame.InsertionPointX,
							InsertionPointY = frame.InsertionPointY,
							Width = frame.Width,
							Pixels = frame.Pixels.ToArray()
						});
					}
					cloneWax.Sequences.Add(cloneSequence);
				}
				clone.Waxes.Add(cloneWax);
			}
			return clone;
		}
	}
}
