using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MZZT.DarkForces.FileFormats {
	/// <summary>
	/// The Dark Forces BRIEFINGS.LST.
	/// </summary>
	public class DfBriefingList : TextBasedFile<DfBriefingList>, ICloneable {
		/// <summary>
		/// Defines the files to use for a level briefing.
		/// </summary>
		public class Briefing : ICloneable {
			/// <summary>
			/// The level base name.
			/// </summary>
			public string Level { get; set; }
			/// <summary>
			/// The LFD file to load the briefing from (only DFBRIEF.LFD is supported).
			/// </summary>
			public string LfdFile { get; set; }
			/// <summary>
			/// The ANIM file from the LFD to use for the briefing.
			/// </summary>
			public string AniFile { get; set; }
			/// <summary>
			/// The PLTT file from the LFD to use for the briefing.
			/// </summary>
			public string PalFile { get; set; }

			object ICloneable.Clone() => this.Clone();
			public Briefing Clone() => new() {
				AniFile = this.AniFile,
				Level = this.Level,
				LfdFile = this.LfdFile,
				PalFile = this.PalFile
			};
		}

		/// <summary>
		/// The list of briefings.
		/// </summary>
		public List<Briefing> Briefings { get; } = new();

		public override bool CanLoad => true;

		public override async Task LoadAsync(Stream stream) {
			this.ClearWarnings();

			using StreamReader reader = new(stream, Encoding.ASCII, false, 1024, true);

			string[] line = await this.ReadTokenizedLineAsync(reader);
			if (!(line?.Select(x => x.ToUpper()).SequenceEqual(new[] { "BRF", "1.0" }) ?? false)) {
				this.AddWarning("BRF file header not found.");
			} else {
				line = await this.ReadTokenizedLineAsync(reader);
			}

			bool readExtraLine = false;
			if (line.Length < 2 || line[0].ToUpper() != "BRIEFS" || !int.TryParse(line[1], NumberStyles.Integer, null, out int count)) {
				count = -1;
				readExtraLine = true;
				this.AddWarning("BRIEFS count not found or invalid.");
			}

			this.Briefings.Clear();

			while (true) { //for (int i = 0; count < 0 || (i < count); i++) {
				if (!readExtraLine) {
					line = await this.ReadTokenizedLineAsync(reader);
				}
				readExtraLine = false;
				if (line == null) {
					break;
				}
				Dictionary<string, string[]> values = TextBasedFile.SplitKeyValuePairs(line);
				values.TryGetValue("LEV", out string[] level);
				values.TryGetValue("LFD", out string[] lfd);
				values.TryGetValue("ANI", out string[] ani);
				values.TryGetValue("PAL", out string[] pal);

				this.Briefings.Add(new() {
					Level = level.FirstOrDefault(),
					LfdFile = lfd.FirstOrDefault(),
					AniFile = ani.FirstOrDefault(),
					PalFile = pal.FirstOrDefault()
				});
			}

			if (this.Briefings.Count < count) {
				this.AddWarning("BRIEFS count doesn't match actual count.");
			}
		}

		public override bool CanSave => true;

		public override async Task SaveAsync(Stream stream) {
			this.ClearWarnings();

			using StreamWriter writer = new(stream, Encoding.ASCII, 1024, true);

			await writer.WriteLineAsync("BRF 1.0");

			await this.WriteLineAsync(writer, $"BRIEFS {this.Briefings.Count}");

			foreach (Briefing brief in this.Briefings) {
				await this.WriteLineAsync(writer,
					$"LEV: {brief.Level} LFD: {brief.LfdFile} ANI: {brief.AniFile} PAL: {brief.PalFile}");
			}
		}

		object ICloneable.Clone() => this.Clone();
		public DfBriefingList Clone() {
			DfBriefingList clone = new();
			clone.Briefings.AddRange(this.Briefings.Select(x => x.Clone()));
			return clone;
		}
	}
}
