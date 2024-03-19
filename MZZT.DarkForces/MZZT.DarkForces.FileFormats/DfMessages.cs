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
	/// A Dark Forces MSG file.
	/// </summary>
	public class DfMessages : TextBasedFile<DfMessages>, ICloneable {
		/// <summary>
		/// A message.
		/// </summary>
		public class Message : ICloneable {
			/// <summary>
			/// The priority of the message.
			/// </summary>
			public byte Priority { get; set; }
			/// <summary>
			/// The text of the message.
			/// </summary>
			public string Text { get; set; }

			object ICloneable.Clone() => this.Clone();
			public Message Clone() => new() {
				Priority = this.Priority,
				Text = this.Text
			};
		}

		/// <summary>
		/// Messages, indexed by unique id.
		/// </summary>
		public Dictionary<int, Message> Messages { get; } = new();

		public override bool CanLoad => true;

		public override async Task LoadAsync(Stream stream) {
			this.ClearWarnings();

			using StreamReader reader = new(stream, Encoding.ASCII, false, 1024, true);

			bool readExtraLine = false;

			string[] line = await this.ReadTokenizedLineAsync(reader);
			if (!(line?.Select(x => x.ToUpper()).SequenceEqual(["MSG", "1.0"]) ?? false)) {
				this.AddWarning("MSG file header not found.");
			} else {
				readExtraLine = true;
				line = await this.ReadTokenizedLineAsync(reader);
			}

			int count = -1;
			if (line?[0]?.ToUpper() == "MSGS") {
				if (line.Length < 2 || !int.TryParse(line[1], NumberStyles.Integer, null, out count)) {
					count = -1;
					this.AddWarning("MSGS count missing or invalid.");
				}

				readExtraLine = true;
				line = await this.ReadTokenizedLineAsync(reader);
			} else {
				this.AddWarning("MSGS count missing or invalid.");
			}

			this.Messages.Clear();

			while (true) { //for (int i = 0; count < 0 || (i < count); i++) {
				if (!readExtraLine) {
					line = await this.ReadTokenizedLineAsync(reader);
				}
				readExtraLine = false;
				if (line == null) {
					this.AddWarning("Unexpected end of message declarations.");
					break;
				}

				if (line.Length == 1 && line[0].ToUpper() == "END") {
					break;
				}

				if (line.Length < 3 || !line[1].EndsWith(":") ||
					!int.TryParse(line[0], NumberStyles.Integer, null, out int id) ||
					!byte.TryParse(line[1].Substring(0, line[1].Length - 1), NumberStyles.Integer, null, out byte priority)) {

					this.AddWarning("Message format invalid!");
					continue;
				}

				this.Messages[id] = new() {
					Priority = priority,
					Text = line[2]
				};
			}

			if (this.Messages.Count < count) {
				this.AddWarning("MSGS count does not match actual count.");
			}
		}

		public override bool CanSave => true;

		public override async Task SaveAsync(Stream stream) {
			this.ClearWarnings();

			using StreamWriter writer = new(stream, Encoding.ASCII, 1024, true);

			await writer.WriteLineAsync("MSG 1.0");

			await this.WriteLineAsync(writer, $"MSGS {this.Messages.Count}");

			foreach ((int id, Message message) in this.Messages) {
				await this.WriteLineAsync(writer, $"{id} {message.Priority}: {this.Escape(message.Text)}");
			}

			await writer.WriteLineAsync("END");

			await writer.WriteAsync('\x1A');
		}

		object ICloneable.Clone() => this.Clone();
		public DfMessages Clone() {
			DfMessages clone = new();
			foreach ((int key, Message value) in this.Messages) {
				clone.Messages[key] = value.Clone();
			}
			return clone;
		}
	}
}
