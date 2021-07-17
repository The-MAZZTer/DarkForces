using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MZZT.DarkForces.FileFormats {
	/// <summary>
	/// A Dark forces JEDI.LVL file.
	/// </summary>
	public class DfLevelList : TextBasedFile<DfLevelList> {
		/// <summary>
		/// A level reference.
		/// </summary>
		public class Level {
			/// <summary>
			/// The name displayed in the menu.
			/// </summary>
			public string DisplayName { get; set; }
			/// <summary>
			/// The level file base name.
			/// </summary>
			public string FileName { get; set; }
			/// <summary>
			/// Paths used by the LucasArts developers to store level data during development, unused in release build.
			/// </summary>
			public List<string> SearchPaths { get; } = new();
		}

		/// <summary>
		/// The levels in this game.
		/// </summary>
		public List<Level> Levels { get; } = new();

		public override bool CanLoad => true;
		
		public override async Task LoadAsync(Stream stream) {
			this.ClearWarnings();

			using StreamReader reader = new(stream, Encoding.ASCII, false, 1024, true);

			bool readExtraLine = false;
			bool eof = false;

			string line = await this.ReadLineAsync(reader);
			while (!eof && line != null && line.Trim() != "\x1A" && line.Trim() == "") {
				line = await this.ReadLineAsync(reader);

				int index = line?.IndexOf("\x1A") ?? -1;
				if (index >= 0) {
					eof = true;
					line = line.Substring(0, index);
				}
			}

			if (line == null) {
				this.AddWarning("Empty file.");
				return;
			}

			string[] tokens = TextBasedFile.TokenizeLine(line);
			if (tokens.Length < 2 || tokens[0].ToUpper() != "LEVELS" ||
				!int.TryParse(tokens[1], NumberStyles.Integer, null, out int count)) {

				count = -1;
				readExtraLine = true;
				this.AddWarning("LEVELS count missing or invalid.");
			}

			this.Levels.Clear();

			// We don't want to use the standard TextBasedFile stuff since we went to delimit on a different character.
			// Possibly in the future there should be a protected property to change the character to delimit on so the code can be shared.
			while (!eof) { //for (int i = 0; count < 0 || (i < count); i++) {
				if (!readExtraLine) {
					line = await this.ReadLineAsync(reader);
					while (!eof && line != null && line.Trim() != "\x1A" && line.Trim() == "") {
						line = await this.ReadLineAsync(reader);

						int index = line?.IndexOf("\x1A") ?? -1;
						if (index >= 0) {
							eof = true;
							line = line.Substring(0, index);
						}
					}
				}
				readExtraLine = false;

				if (eof || line == null) {
					break;
				}

				tokens = line.Split(',').Select(x => x.Trim()).ToArray();
				if (tokens.Length < 2) {
					this.AddWarning("Invalid level format.");
					continue;
				}

				Level level = new() {
					DisplayName = tokens[0],
					FileName = tokens[1]
				};
				if (tokens.Length >= 3) {
					level.SearchPaths.AddRange(tokens[2].Split(';'));
				}
				this.Levels.Add(level);
			}

			if (count > 0 && this.Levels.Count != count) {
				this.AddWarning("LEVELS count doesn't match actual level count.");
			}
		}

		public override bool CanSave => true;

		public override async Task SaveAsync(Stream stream) {
			this.ClearWarnings();

			using StreamWriter writer = new(stream, Encoding.ASCII, 1024, true);

			await this.WriteLineAsync(writer, $"LEVELS {this.Levels.Count}");

			foreach (Level level in this.Levels) {
				await this.WriteLineAsync(writer, $"{level.DisplayName}, {level.FileName}, {string.Join(";", level.SearchPaths)}");
			}
		}
	}
}
