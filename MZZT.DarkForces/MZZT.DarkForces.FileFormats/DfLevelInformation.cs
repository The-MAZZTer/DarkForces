using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using static MZZT.DarkForces.FileFormats.DfLevel;

namespace MZZT.DarkForces.FileFormats {
	/// <summary>
	/// A Dark Forces INF file.
	/// </summary>
	public class DfLevelInformation : TextBasedFile<DfLevelInformation>, ICloneable {
		/// <summary>
		/// Types of scripts.
		/// </summary>
		public enum ScriptTypes {
			/// <summary>
			/// Level script (for ambient sounds).
			/// </summary>
			Level,
			/// <summary>
			/// Sector script.
			/// </summary>
			Sector,
			/// <summary>
			/// Wall script.
			/// </summary>
			Line
		}

		/// <summary>
		/// A script.
		/// </summary>
		public class Item : ICloneable {
			/// <summary>
			/// The type of script.
			/// </summary>
			public ScriptTypes Type { get; set; }
			/// <summary>
			/// The sector name the script is associated with.
			/// </summary>
			public string SectorName { get; set; }
			/// <summary>
			/// The wall number the script is associated with.
			/// </summary>
			public int WallNum { get; set; }
			/*/// <summary>
			/// The sector the script is associated with. Call LoadSectorReferences to populate.
			/// </summary>
			public Sector Sector { get; set; }
			/// <summary>
			/// The wall the script is associated with.. Call LoadSectorReferences to populate.
			/// </summary>
			public Wall Wall { get; set; }*/
			/// <summary>
			/// The script body.
			/// </summary>
			public string Script { get; set; }

			object ICloneable.Clone() => this.Clone();
			public Item Clone() => new() {
				Script = this.Script,
				//Sector = this.Sector,
				SectorName = this.SectorName,
				//Wall = this.Wall,
				WallNum = this.WallNum,
				Type = this.Type
			};
		}

		/// <summary>
		/// The level name.
		/// </summary>
		public string LevelFile { get; set; }
		/// <summary>
		/// Scripts in the level.
		/// </summary>
		public List<Item> Items { get; } = new();

		/*/// <summary>
		/// Assign scripts proper sector and wall references.
		/// </summary>
		/// <param name="level">The level geometry.</param>
		public void LoadSectorReferences(DfLevel level) {
			Dictionary<string, Sector> sectorMap = level.Sectors
				.Where(x => x.Name != null)
				.GroupBy(x => x.Name.ToLower())
				.ToDictionary(x => x.Key, x => x.First());
			foreach (Item item in this.Items) {
				switch (item.Type) {
					case ScriptTypes.Sector:
						sectorMap.TryGetValue(item.SectorName.ToLower(), out Sector sector);
						item.Sector = sector;
						break;
					case ScriptTypes.Line:
						sectorMap.TryGetValue(item.SectorName.ToLower(), out sector);
						item.Sector = sector;
						if (sector != null && sector.Walls.Count > item.WallNum) {
							item.Wall = sector.Walls[item.WallNum];
						}
						break;
				}
			}
		}*/

		public override bool CanLoad => true;
		
		public override async Task LoadAsync(Stream stream) {
			this.ClearWarnings();

			using StreamReader reader = new(stream, Encoding.ASCII, false, 1024, true);

			string[] line = await this.ReadTokenizedLineAsync(reader);
			if (!(line?.Select(x => x.ToUpper()).SequenceEqual(new[] { "INF", "1.0" }) ?? false)) {
				this.AddWarning("INF file format not found!");
			} else {
				line = await this.ReadTokenizedLineAsync(reader);
			}

			this.Items.Clear();

			int lastWall = -1;
			while (line != null) {
				switch (line[0].ToUpper()) {
					case "LEVELNAME": {
						if (line.Length < 2) {
							this.AddWarning("LEVELNAME statement missing value.");
							break;
						}
						this.LevelFile = line[1];
					} break;
					case "ITEMS": {
						if (line.Length < 2 || !int.TryParse(line[1], NumberStyles.Integer, null, out int count)) {
							count = -1;
							this.AddWarning("Item count is not a number!");
						} else {
							this.Items.Capacity = count;
						}

						while (true) { // for (int i = 0; count < 0 || (i < count); i++) {
							line = await this.ReadTokenizedLineAsync(reader);
							if (line == null) {
								if (this.Items.Count < count) {
									this.AddWarning("Unexpected end of item declarations.");
								}
								break;
							}

							Dictionary<string, string[]> values = TextBasedFile.SplitKeyValuePairs(line);
							if (!values.TryGetValue("ITEM", out string[] itemString)) {
								if (this.Items.Count < count) {
									this.AddWarning("Unexpected end of item declarations.");
								}
								break;
							}

							if (itemString.Length < 1 || !Enum.TryParse(itemString[0], true, out ScriptTypes type)) {
								this.AddWarning("Item format is invalid.");
								continue;
							}
							Item item = new() {
								Type = type
							};
							switch (type) {
								case ScriptTypes.Sector: {
									if (!values.TryGetValue("NAME", out string[] nameString) ||
										nameString.Length < 1) {

										this.AddWarning("Invalid sector definition.");
										continue;
									}
									item.SectorName = nameString[0];
								} break;
								case ScriptTypes.Line: {
									if (!values.TryGetValue("NAME", out string[] nameString) ||
										nameString.Length < 1) {

										this.AddWarning("Invalid sector definition.");
										continue;
									}
									item.SectorName = nameString[0];

									if (!values.TryGetValue("NUM", out string[] numString) ||
										numString.Length < 1 ||
										!int.TryParse(numString[0], NumberStyles.Integer, null, out int num)) {

										if (lastWall >= 0) {
											this.AddWarning("Invalid wall definition, assuming same as last wall number.");
											num = lastWall;
										} else {
											this.AddWarning("Invalid wall definition.");
											continue;
										}
									}
									lastWall = num;
									item.WallNum = num;
								} break;
							}
							this.Items.Add(item);

							line = await this.ReadTokenizedLineAsync(reader);

							if (line == null || line[0].ToUpper() != "SEQ") {
								this.AddWarning("Missing SEQ.");
								continue;
							}

							string text = (await reader.ReadLineAsync())?.TrimEnd();
							if (text != null) {
								this.IncrementCurrentLine();
							}
							List<string> lines = new List<string>();
							while (text != null && text.Trim().ToUpper() != "SEQEND") {
								lines.Add(text);
								text = (await reader.ReadLineAsync())?.TrimEnd();
								if (text != null) {
									this.IncrementCurrentLine();
								}
							}

							int strip = 0;
							while (lines[0].Length > strip && char.IsWhiteSpace(lines[0][strip]) &&
								lines.Select(x => x.ElementAtOrDefault(strip)).Distinct().Count() == 1) {

								strip++;
							}
							IEnumerable<string> script = lines;
							if (strip > 0) {
								script = script.Select(x => x.Substring(strip));
							}

							item.Script = string.Join("\n", script);
						}
					} continue;
				}

				line = await this.ReadTokenizedLineAsync(reader);
			}
		}

		public override bool CanSave => true;

		/*private void SaveSectorReferences() {
			foreach (Item item in this.Items) {
				if (item.Sector != null) {
					item.SectorName = item.Sector.Name;
					if (item.Wall != null) {
						item.WallNum = item.Sector.Walls.IndexOf(item.Wall);
					}
				}
			}
		}*/

		public override async Task SaveAsync(Stream stream) {
			//this.SaveSectorReferences();
			this.ClearWarnings();

			using StreamWriter writer = new(stream, Encoding.ASCII, 1024, true);

			await writer.WriteLineAsync("INF 1.0");

			await this.WriteLineAsync(writer, $"LEVELNAME {this.LevelFile}");

			await this.WriteLineAsync(writer, $"items {this.Items.Count}");

			foreach (Item item in this.Items) {
				switch (item.Type) {
					case ScriptTypes.Level:
						await writer.WriteLineAsync("item: level");
						break;
					case ScriptTypes.Sector:
						await this.WriteLineAsync(writer, $"item: sector name: {item.SectorName}");
						break;
					case ScriptTypes.Line:
						await this.WriteLineAsync(writer, $"item: line name: {item.SectorName} num: {item.WallNum}");
						break;
				}

				await writer.WriteLineAsync("seq");
				await writer.WriteLineAsync(item.Script);
				await writer.WriteLineAsync("seqend");
			}
		}

		object ICloneable.Clone() => this.Clone();
		public DfLevelInformation Clone() {
			DfLevelInformation clone = new() {
				LevelFile = this.LevelFile
			};
			clone.Items.AddRange(this.Items.Select(x => x.Clone()));
			return clone;
		}
	}
}
