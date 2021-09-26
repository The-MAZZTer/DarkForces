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
	/// The Dark Forces CUTSCENE.LST file.
	/// </summary>
	public class DfCutsceneList : TextBasedFile<DfCutsceneList>, ICloneable {
		/// <summary>
		/// Definition of a cutscene.
		/// </summary>
		public class Cutscene : ICloneable {
			/// <summary>
			/// The LFD file that contains the cutscene resources.
			/// </summary>
			public string Lfd { get; set; }
			/// <summary>
			/// The FILM file which drives the cutscene.
			/// </summary>
			public string FilmFile { get; set; }
			/// <summary>
			/// The speed of the cutscene.
			/// </summary>
			public int Speed { get; set; } = 10;
			/// <summary>
			/// The next cutscene to play.
			/// </summary>
			public int NextCutscene { get; set; }
			/// <summary>
			/// The next cutscene to play if Escape is pressed.
			/// </summary>
			public int EscapeToCutscene { get; set; }
			/// <summary>
			/// The sequence from CUTMUSE.TXT to use.
			/// </summary>
			public int CutmuseSequence { get; set; }
			/// <summary>
			/// Audio volume.
			/// </summary>
			public int Volume { get; set; } = 100;

			object ICloneable.Clone() => this.Clone();
			public Cutscene Clone() => new() {
				CutmuseSequence = this.CutmuseSequence,
				EscapeToCutscene = this.EscapeToCutscene,
				FilmFile = this.FilmFile,
				Lfd = this.Lfd,
				NextCutscene = this.NextCutscene,
				Speed = this.Speed,
				Volume = this.Volume
			};
		}

		/// <summary>
		/// Cutscenes defined in this file.
		/// </summary>
		public Dictionary<int, Cutscene> Cutscenes { get; } = new();

		public override bool CanLoad => true;

		public override async Task LoadAsync(Stream stream) {
			this.ClearWarnings();

			using StreamReader reader = new(stream, Encoding.ASCII, false, 1024, true);

			string[] line = await this.ReadTokenizedLineAsync(reader);
			if (!(line?.Select(x => x.ToUpper()).SequenceEqual(new[] { "CUT", "1.0" }) ?? false)) {
				this.AddWarning("CUT file header not found.");
			} else {
				line = await this.ReadTokenizedLineAsync(reader);
			}

			bool readExtraLine = false;
			if (line.Length < 2 || line[0].ToUpper() != "CUTS" || !int.TryParse(line[1], NumberStyles.Integer, null, out int count)) {
				count = -1;
				readExtraLine = true;
				this.AddWarning("CUTS count not found or invalid.");
			}

			this.Cutscenes.Clear();

			while (true) { // for (int i = 0; count < 0 || (i < count); i++) {
				if (!readExtraLine) {
					line = await this.ReadTokenizedLineAsync(reader);
				}
				readExtraLine = false;
				if (line == null) {
					break;
				}
				(string idString, string[] values) = TextBasedFile.SplitKeyValuePairs(line).FirstOrDefault();

				int id = -1;
				if (idString != null) {
					if (!int.TryParse(idString, NumberStyles.Integer, null, out id)) {
						id = -1;
					}
				}
				if (id < 0) {
					this.AddWarning("Invalid cutscene id.");
					continue;
				}

				if (values.Length < 7) {
					this.AddWarning("Cutscene missing arguments.");
					continue;
				}

				(string lfd, string film, string speedString, string nextString, string escapeString, string sequenceString,
					string volumeString) = values;

				if (!int.TryParse(speedString, NumberStyles.Integer, null, out int speed) ||
					!int.TryParse(nextString, NumberStyles.Integer, null, out int next) ||
					!int.TryParse(escapeString, NumberStyles.Integer, null, out int escape) ||
					!int.TryParse(sequenceString, NumberStyles.Integer, null, out int sequence) ||
					!int.TryParse(volumeString, NumberStyles.Integer, null, out int volume)) {

					this.AddWarning("Cutscene arguments aren't integers as expected.");
					continue;
				}

				if (speed < 5 || speed > 20) {
					this.AddWarning("Cutscene speeds must be between 5 and 20.");
				}

				this.Cutscenes[id] = new() {
					Lfd = lfd,
					FilmFile = film,
					Speed = speed,
					NextCutscene = next,
					EscapeToCutscene = escape,
					CutmuseSequence = sequence,
					Volume = volume
				};
			}

			if (this.Cutscenes.Count < count) {
				this.AddWarning("CUTS count does not match actual count.");
			}
		}

		public override bool CanSave => true;

		public override async Task SaveAsync(Stream stream) {
			this.ClearWarnings();

			if (this.Cutscenes.Values.Any(x => x.Speed < 5 || x.Speed > 20)) {
				this.AddWarning("Cutscene speeds must be between 5 and 20.");
			}

			using StreamWriter writer = new(stream, Encoding.ASCII, 1024, true);

			await writer.WriteLineAsync("CUT 1.0");

			await this.WriteLineAsync(writer, $"CUTS {this.Cutscenes.Count}");

			foreach ((int id, Cutscene cutscene) in this.Cutscenes) {
				await this.WriteLineAsync(writer,
					$"{id}: {cutscene.Lfd} {cutscene.FilmFile} {cutscene.Speed} {cutscene.NextCutscene} {cutscene.EscapeToCutscene} {cutscene.CutmuseSequence} {cutscene.Volume}");
			}
		}

		object ICloneable.Clone() => this.Clone();
		public DfCutsceneList Clone() {
			DfCutsceneList clone = new();
			foreach ((int key, Cutscene value) in this.Cutscenes) {
				clone.Cutscenes[key] = value.Clone();
			}
			return clone;
		}
	}
}
