using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MZZT.DarkForces.FileFormats {
	/// <summary>
	/// A Dark Forces O file.
	/// </summary>
	public class DfLevelObjects : TextBasedFile<DfLevelObjects> {
		/// <summary>
		/// Different types of objects.
		/// </summary>
		public enum ObjectTypes {
			/// <summary>
			/// Invisible object.
			/// </summary>
			Spirit,
			/// <summary>
			/// Respawn checkpoint.
			/// </summary>
			Safe,
			/// <summary>
			/// Animated sprite.
			/// </summary>
			Sprite,
			/// <summary>
			/// Non-animated sprite.
			/// </summary>
			Frame,
			/// <summary>
			/// 3D model.
			/// </summary>
			ThreeD,
			/// <summary>
			/// Ambient sound.
			/// </summary>
			Sound
		}

		/// <summary>
		/// Difficulty levels for objects.
		/// </summary>
		public enum Difficulties {
			Easy = -1,
			EasyMedium = -2,
			EasyMediumHard = 0,
			MediumHard = 2,
			Hard = 3
		}

		/// <summary>
		/// A level object.
		/// </summary>
		public class Object {
			/// <summary>
			/// Object type.
			/// </summary>
			public ObjectTypes Type { get; set; }
			/// <summary>
			/// The file to use to display the object.
			/// </summary>
			public string FileName { get; set; }
			/// <summary>
			/// The position of the object.
			/// </summary>
			public Vector3 Position { get; set; }
			/// <summary>
			/// The rotation of the object (Pitch, Yaw, Roll).
			/// </summary>
			public Vector3 EulerAngles { get; set; }
			/// <summary>
			/// The difficulties this object shows up on.
			/// </summary>
			public Difficulties Difficulty { get; set; }
			/// <summary>
			/// The logic script this object uses.
			/// </summary>
			public string Logic { get; set; }
		}

		/// <summary>
		/// The level this object file belongs to.
		/// </summary>
		public string LevelFile { get; set; }
		/// <summary>
		/// The objects in this file.
		/// </summary>
		public List<Object> Objects { get; } = new();

		public override bool CanLoad => true;
		
		public override async Task LoadAsync(Stream stream) {
			this.ClearWarnings();

			using StreamReader reader = new(stream, Encoding.ASCII, false, 1024, true);

			string[] line = await this.ReadTokenizedLineAsync(reader);
			if (!(line?.SequenceEqual(new[] { "O", "1.1" }) ?? false)) {
				this.AddWarning("O file format not found!");
			} else {
				line = await this.ReadTokenizedLineAsync(reader);
			}

			this.Objects.Clear();
			Dictionary<ObjectTypes, List<string>> tables = new() {
				[ObjectTypes.Spirit] = new() { null },
				[ObjectTypes.Safe] = new() { null }
			};

			while (line != null) {
				switch (line[0].ToUpper()) {
					case "LEVELNAME": {
						if (line.Length < 2) {
							this.AddWarning("LEVELNAME missing value.");
							break;
						}
						this.LevelFile = line[1];
					} break;
					case "PODS": {
						tables[ObjectTypes.ThreeD] = new();

						if (line.Length < 2 || !int.TryParse(line[1], NumberStyles.Integer, null, out int podCount)) {
							podCount = -1;
							this.AddWarning("3d object file count is not a number!");
						} else {
							tables[ObjectTypes.ThreeD].Capacity = podCount;
						}

						while (true) { //for (int i = 0; podCount < 0 || (i < podCount); i++) {
							line = await this.ReadTokenizedLineAsync(reader);
							if (line == null || line[0].ToUpper() != "POD:") {
								if (tables[ObjectTypes.ThreeD].Count < podCount) {
									this.AddWarning("Unexpected end of 3d object declarations.");
								}
								break;
							}
							tables[ObjectTypes.ThreeD].Add(line[1]);
						}
					} continue;
					case "SPRS": {
						tables[ObjectTypes.Sprite] = new();

						if (line.Length < 2 || !int.TryParse(line[1], NumberStyles.Integer, null, out int spriteCount)) {
							spriteCount = -1;
							this.AddWarning("Sprite file count is not a number!");
						} else {
							tables[ObjectTypes.Sprite].Capacity = spriteCount;
						}

						while (true) { //for (int i = 0; spriteCount < 0 || (i < spriteCount); i++) {
							line = await this.ReadTokenizedLineAsync(reader);
							if (line == null || line[0].ToUpper() != "SPR:") {
								if (tables[ObjectTypes.Sprite].Count < spriteCount) {
									this.AddWarning("Unexpected end of sprite declarations.");
								}
								break;
							}
							tables[ObjectTypes.Sprite].Add(line[1]);
						}
					} continue;
					case "FMES": {
						tables[ObjectTypes.Frame] = new();

						if (line.Length < 2 || !int.TryParse(line[1], NumberStyles.Integer, null, out int frameCount)) {
							frameCount = -1;
							this.AddWarning("Frame file count is not a number!");
						} else {
							tables[ObjectTypes.Frame].Capacity = frameCount;
						}

						while (true) { //for (int i = 0; frameCount < 0 || (i < frameCount); i++) {
							line = await this.ReadTokenizedLineAsync(reader);
							if (line == null || line[0].ToUpper() != "FME:") {
								if (tables[ObjectTypes.Frame].Count < frameCount) {
									this.AddWarning("Unexpected end of frame declarations.");
								}
								break;
							}
							tables[ObjectTypes.Frame].Add(line[1]);
						}
					} continue;
					case "SOUNDS": {
						tables[ObjectTypes.Sound] = new();

						if (line.Length < 2 || !int.TryParse(line[1], NumberStyles.Integer, null, out int soundCount)) {
							soundCount = -1;
							this.AddWarning("Sound file count is not a number!");
						} else {
							tables[ObjectTypes.Sound].Capacity = soundCount;
						}

						while (true) { //for (int i = 0; soundCount < 0 || (i < soundCount); i++) {
							line = await this.ReadTokenizedLineAsync(reader);
							if (line == null || line[0].ToUpper() != "SOUND:") {
								if (tables[ObjectTypes.Frame].Count < soundCount) {
									this.AddWarning("Unexpected end of sound declarations.");
								}
								break;
							}
							tables[ObjectTypes.Sound].Add(line[1]);
						}
					} continue;
					case "OBJECTS": {
						if (line.Length < 2 || !int.TryParse(line[1], NumberStyles.Integer, null, out int objectCount)) {
							objectCount = -1;
							this.AddWarning("Object count is not a number.");
							break;
						} else {
							this.Objects.Capacity = objectCount;
						}

						int[] difficulties = Enum.GetValues(typeof(Difficulties)).Cast<int>().ToArray();

						bool readExtraLine = false;
						while (true) { //for (int i = 0; objectCount < 0 || (i < objectCount); i++) {
							if (!readExtraLine) {
								line = await this.ReadTokenizedLineAsync(reader);
							}
							readExtraLine = false;
							if (line == null) {
								if (this.Objects.Count < objectCount) {
									this.AddWarning("Unexpected end of object declarations.");
								}
								break;
							}

							if (line[0].ToUpper() != "CLASS:") {
								if (this.Objects.Count < objectCount) {
									this.AddWarning("Unexpected end of object declarations.");
								}
								break;
							}

							Dictionary<string, string[]> values = TextBasedFile.SplitKeyValuePairs(line);
							if (line.Length < 18 || !values.TryGetValue("DATA", out string[] dataString) || dataString.Length < 1 ||
								!int.TryParse(dataString[0], NumberStyles.Integer, null, out int dataIndex) ||
								dataIndex < 0 ||
								!values.TryGetValue("X", out string[] xString) || xString.Length < 1 ||
								!float.TryParse(xString[0], out float x) ||
								!values.TryGetValue("Y", out string[] yString) || yString.Length < 1 ||
								!float.TryParse(yString[0], out float y) ||
								!values.TryGetValue("Z", out string[] zString) || zString.Length < 1 ||
								!float.TryParse(zString[0], out float z) ||
								!values.TryGetValue("PCH", out string[] pitchString) || pitchString.Length < 1 ||
								!float.TryParse(pitchString[0], out float pitch) ||
								!values.TryGetValue("YAW", out string[] yawString) || yawString.Length < 1 ||
								!float.TryParse(yawString[0], out float yaw) ||
								!values.TryGetValue("ROL", out string[] rollString) || rollString.Length < 1 ||
								!float.TryParse(rollString[0], out float roll) ||
								!values.TryGetValue("DIFF", out string[] difficultyString) || difficultyString.Length < 1 ||
								!int.TryParse(difficultyString[0], out int difficulty) ||
								!values.TryGetValue("CLASS", out string[] classString) || classString.Length < 1) {

								this.AddWarning("Object format is invalid.");
								continue;
							}

							string stringType = classString[0];
							ObjectTypes type;
							if (stringType.ToUpper() == "3D") {
								type = ObjectTypes.ThreeD;
							} else {
								if (!Enum.TryParse(stringType, true, out type)) {
									this.AddWarning("Object format is invalid.");
									continue;
								}
							}

							List<string> table = tables[type];
							if (type != ObjectTypes.Spirit && type != ObjectTypes.Safe && dataIndex >= table.Count) {
								this.AddWarning("Invalid data value.");
								continue;
							}

							Object obj = new() {
								Type = type,
								FileName = (type == ObjectTypes.Spirit || type == ObjectTypes.Safe) ? null : table[dataIndex],
								Position = new() {
									X = x,
									Y = y,
									Z = z
								},
								EulerAngles = new() {
									X = pitch,
									Y = yaw,
									Z = roll
								},
								Difficulty = Array.IndexOf(difficulties, difficulty) < 0 ? Difficulties.EasyMediumHard : (Difficulties)difficulty
							};
							this.Objects.Add(obj);

							line = await this.ReadTokenizedLineAsync(reader);

							if (line != null && line[0].ToUpper() == "SEQ") {
								StringBuilder logic = new();
								string text = (await reader.ReadLineAsync()).Trim();
								if (text != null) {
									this.CurrentLine++;
								}
								while (text != null && text.ToUpper() != "SEQEND") {
									if (logic.Length > 0) {
										logic.Append("\n");
									}

									logic.Append(text);
									text = (await reader.ReadLineAsync()).Trim();
									if (text != null) {
										this.CurrentLine++;
									}
								}
								obj.Logic = logic.ToString();
							} else {
								readExtraLine = true;
							}
						}
					} break;
					default:
						this.AddWarning($"Unknown statement {line[0]}.");
						break;
				}

				line = await this.ReadTokenizedLineAsync(reader);
			}
		}

		public override bool CanSave => true;

		public override async Task SaveAsync(Stream stream) {
			this.ClearWarnings();

			using StreamWriter writer = new(stream, Encoding.ASCII, 1024, true);

			await writer.WriteLineAsync("O 1.1");

			await this.WriteLineAsync(writer, $"LEVELNAME {this.LevelFile}");

			Dictionary<ObjectTypes, string[]> tables = new();
			foreach (ObjectTypes type in (ObjectTypes[])Enum.GetValues(typeof(ObjectTypes))) {
				tables[type] = this.Objects.Where(x => x.Type == type).Select(x => x.FileName).Distinct().ToArray();
			}

			await this.WriteLineAsync(writer, $"PODS {tables[ObjectTypes.ThreeD].Length}");
			foreach (string filename in tables[ObjectTypes.ThreeD]) {
				await this.WriteLineAsync(writer, $"POD: {filename}");
			}

			await this.WriteLineAsync(writer, $"SPRS {tables[ObjectTypes.Sprite].Length}");
			foreach (string filename in tables[ObjectTypes.Sprite]) {
				await this.WriteLineAsync(writer, $"SPR: {filename}");
			}

			await this.WriteLineAsync(writer, $"FMES {tables[ObjectTypes.Frame].Length}");
			foreach (string filename in tables[ObjectTypes.Frame]) {
				await this.WriteLineAsync(writer, $"FME: {filename}");
			}

			await this.WriteLineAsync(writer, $"SOUNDS {tables[ObjectTypes.Sound].Length}");
			foreach (string filename in tables[ObjectTypes.Sound]) {
				await this.WriteLineAsync(writer, $"SOUND: {filename}");
			}

			await this.WriteLineAsync(writer, $"OBJECTS {this.Objects.Count}");
			foreach (Object obj in this.Objects) {
				await this.WriteLineAsync(writer, $"CLASS: {(obj.Type == ObjectTypes.ThreeD ? "3DO" : obj.Type.ToString().ToUpper())} DATA: {Array.IndexOf(tables[obj.Type], obj.FileName)} X: {obj.Position.X:0.00} Y: {obj.Position.Y:0.00} Z: {obj.Position.Z:0.00} PCH: {obj.EulerAngles.X:0.00} YAW: {obj.EulerAngles.Y:0.00} ROL: {obj.EulerAngles.Z:0.00} DIFF: {(int)obj.Difficulty}");
				if (obj.Logic != null) {
					await writer.WriteLineAsync("SEQ");
					await writer.WriteLineAsync(obj.Logic);
					await writer.WriteLineAsync("SEQEND");
				}
			}
		}
	}
}
