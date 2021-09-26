using MZZT.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MZZT.DarkForces.FileFormats {
	/// <summary>
	/// A Dark Forces CUTMUSE.TXT file.
	/// </summary>
	public class DfCutsceneMusicList : TextBasedFile<DfCutsceneMusicList>, ICloneable {
		/// <summary>
		/// A cutscene sequence.
		/// </summary>
		public class Sequence : ICloneable {
			/// <summary>
			/// A list of music cues.
			/// </summary>
			public List<Cue> Cues { get; } = new();

			object ICloneable.Clone() => this.Clone();
			public Sequence Clone() {
				Sequence clone = new();
				clone.Cues.AddRange(this.Cues.Select(x => x.Clone()));
				return clone;
			}
		}

		/// <summary>
		/// A music cue.
		/// </summary>
		public class Cue : ICloneable {
			/// <summary>
			/// The GMID file to play.
			/// </summary>
			public string GmdFile { get; set; }
			/// <summary>
			/// The chunk id (track number?) to play.
			/// </summary>
			public char StartChunk { get; set; }
			/// <summary>
			/// The position within the chunk.
			/// </summary>
			public int StartPosition { get; set; }
			/// <summary>
			/// The chunk id (track number?) to stop in.
			/// </summary>
			public char EndChunk { get; set; }
			/// <summary>
			/// The position within the chunk.
			/// </summary>
			public int EndPosition { get; set; }

			object ICloneable.Clone() => this.Clone();
			public Cue Clone() => new() {
				EndChunk = this.EndChunk,
				EndPosition = this.EndPosition,
				GmdFile = this.GmdFile,
				StartChunk = this.StartChunk,
				StartPosition = this.StartPosition
			};
		}

		/// <summary>
		/// Music seqeucnes in the file.
		/// </summary>
		public Dictionary<int, Sequence> Sequences { get; } = new();

		public override bool CanLoad => true;

		public override async Task LoadAsync(Stream stream) {
			this.ClearWarnings();

			using StreamReader reader = new(stream, Encoding.ASCII, false, 1024, true);

			this.Sequences.Clear();

			Sequence currentSequence = null;
			Cue currentCue = null;

			while (true) {
				string[] line = await this.ReadTokenizedLineAsync(reader);
				if (line == null) {
					break;
				}

				Dictionary<string, string[]> values = TextBasedFile.SplitKeyValuePairs(line);
				if (values.TryGetValue("SEQUENCE", out string[] sequences)) {
					if (sequences.Length < 1 || !int.TryParse(sequences[0], NumberStyles.Integer, null, out int sequence)) {
						this.AddWarning("Invalid SEQUENCE: value.");
						continue;
					}

					this.Sequences[sequence] = currentSequence = new();
				} else if (values.TryGetValue("CUE", out string[] cues)) {
					if (currentSequence == null) {
						this.AddWarning("Unexpected CUE: statement.");
						continue;
					}
					if (cues.Length < 1) {
						this.AddWarning("Invalid CUE: value.");
						continue;
					}

					currentSequence.Cues.Add(currentCue = new() {
						GmdFile = cues[0]
					});
				} else {
					if (currentCue == null) {
						this.AddWarning("Expected SEQUENCE: or CUE:.");
						continue;
					}

					if (line.Length < 4 || line[0].Length != 1 || !int.TryParse(line[1], NumberStyles.Integer, null, out int startPos)
						|| line[2].Length != 1 || !int.TryParse(line[3], NumberStyles.Integer, null, out int endPos)) {

						this.AddWarning("Invalid chunk values.");
						continue;
					}

					currentCue.StartChunk = line[0][0];
					currentCue.StartPosition = startPos;
					currentCue.EndChunk = line[2][0];
					currentCue.EndPosition = endPos;
				}
			}
		}

		public override bool CanSave => true;

		public override async Task SaveAsync(Stream stream) {
			this.ClearWarnings();

			using StreamWriter writer = new(stream, Encoding.ASCII, 1024, true);

			foreach ((int id, Sequence sequence) in this.Sequences) {
				await this.WriteLineAsync(writer, $"SEQUENCE: {id}");

				foreach (Cue cue in sequence.Cues) {
					await this.WriteLineAsync(writer, $"CUE: {cue.GmdFile}");
					await this.WriteLineAsync(writer, $"{cue.StartChunk} {cue.StartPosition} {cue.EndChunk} {cue.EndPosition}");
				}
			}
		}

		object ICloneable.Clone() => this.Clone();
		public DfCutsceneMusicList Clone() {
			DfCutsceneMusicList clone = new();
			foreach ((int key, Sequence value) in this.Sequences) {
				clone.Sequences[key] = value.Clone();
			}
			return clone;
		}
	}
}
