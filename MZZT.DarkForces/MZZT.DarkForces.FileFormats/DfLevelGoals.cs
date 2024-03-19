using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MZZT.DarkForces.FileFormats {
	/// <summary>
	/// A Dark Forces GOL file.
	/// </summary>
	public class DfLevelGoals : TextBasedFile<DfLevelGoals>, ICloneable {
		/// <summary>
		/// Types of goals.
		/// </summary>
		public enum GoalTypes {
			/// <summary>
			/// A sector or wall trigger.
			/// </summary>
			Trigger,
			/// <summary>
			/// An item pickup.
			/// </summary>
			Item
		}

		/// <summary>
		/// Different items which can be goals.
		/// </summary>
		public enum GoalItems {
			/// <summary>
			/// Death Star plans
			/// </summary>
			DeathStarPlans = 0,
			/// <summary>
			/// A vial of Phrink
			/// </summary>
			PhrikMetal = 1,
			/// <summary>
			/// A Nava card.
			/// </summary>
			NavaCard = 2,
			/// <summary>
			/// Data tapes.
			/// </summary>
			DataTapes = 4,
			/// <summary>
			/// A Broken Dark Tropper Weapon
			/// </summary>
			BrokenDTWeapon = 5,
			/// <summary>
			/// Your gear.
			/// </summary>
			YourGear = 6
		}

		/// <summary>
		/// A goal.
		/// </summary>
		public class Goal : ICloneable {
			/// <summary>
			/// The type of goal.
			/// </summary>
			public GoalTypes Type { get; set; }
			/// <summary>
			/// The item needed to fulfill the goal.
			/// </summary>
			public GoalItems Item { get; set; }
			/// <summary>
			/// The goal trigger number associated with this goal.
			/// </summary>
			public int Trigger { get; set; }

			object ICloneable.Clone() => this.Clone();
			public Goal Clone() => new() {
				Item = this.Item,
				Trigger = this.Trigger,
				Type = this.Type
			};
		}

		/// <summary>
		/// The goals in this level.
		/// </summary>
		public List<Goal> Goals { get; } = new();

		public override bool CanLoad => true;
		
		public override async Task LoadAsync(Stream stream) {
			this.ClearWarnings();

			using StreamReader reader = new(stream, Encoding.ASCII, false, 1024, true);

			string[] line = await this.ReadTokenizedLineAsync(reader);
			if (!(line?.Select(x => x.ToUpper()).SequenceEqual(["GOL", "1.0"])) ?? false) {
				this.AddWarning("GOL file header not found!");
			} else {
				line = await this.ReadTokenizedLineAsync(reader);
			}

			this.Goals.Clear();

			while (line != null) {
				Dictionary<string, string[]> values = TextBasedFile.SplitKeyValuePairs(line);
				if (!values.TryGetValue("GOAL", out string[] goalString) || goalString.Length < 1 ||
					goalString[0] != this.Goals.Count.ToString()) {

					this.AddWarning("Missing or invalid goal number.");
				}

				if (values.TryGetValue("ITEM", out string[] itemString) && itemString.Length > 0 &&
					int.TryParse(itemString[0], NumberStyles.Integer, null, out int item)) {

					this.Goals.Add(new() {
						Type = GoalTypes.Item,
						Item = (GoalItems)item
					});
				} else if (values.TryGetValue("TRIG", out string[] triggerString) && triggerString.Length > 0 &&
					int.TryParse(triggerString[0], NumberStyles.Integer, null, out int trigger)) {

					this.Goals.Add(new() {
						Type = GoalTypes.Trigger,
						Trigger = trigger
					});
				} else {
					this.AddWarning("Invalid goal.");
				}

				line = await this.ReadTokenizedLineAsync(reader);
			}
		}

		public override bool CanSave => true;

		public override async Task SaveAsync(Stream stream) {
			this.ClearWarnings();

			using StreamWriter writer = new(stream, Encoding.ASCII, 1024, true);

			await writer.WriteLineAsync("GOL 1.0");
			foreach ((Goal goal, int i) in this.Goals.Select((x, i) => (x, i))) {
				if (goal.Type == GoalTypes.Item) {
					await this.WriteLineAsync(writer, $"GOAL: {i} ITEM: {(int)goal.Item}");
				} else {
					await this.WriteLineAsync(writer, $"GOAL: {i} TRIG: {goal.Trigger}");
				}
			}
		}

		object ICloneable.Clone() => this.Clone();
		public DfLevelGoals Clone() {
			DfLevelGoals clone = new();
			clone.Goals.AddRange(this.Goals.Select(x => x.Clone()));
			return clone;
		}
	}
}
