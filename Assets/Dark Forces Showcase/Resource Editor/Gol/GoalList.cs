using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
	class GoalList : DataboundList<DfLevelGoals.Goal> {
		[Header("GOL"), SerializeField]
		private Button moveUp;
		[SerializeField]
		private Button moveDown;

		protected override void OnEnable() {
			base.OnEnable();

			int index = this.SelectedIndex;
			this.moveUp.interactable = index > 0;
			this.moveDown.interactable = index >= 0 && index < this.Count - 1;
		}

		protected override void OnSelectedValueChanged() {
			base.OnSelectedValueChanged();

			int index = this.SelectedIndex;
			this.moveUp.interactable = index > 0;
			this.moveDown.interactable = index >= 0 && index < this.Count - 1;
		}

		public void AddItem() {
			this.Add(new DfLevelGoals.Goal() {
				Type = DfLevelGoals.GoalTypes.Item
			});

			this.GetComponentInParent<GolViewer>().OnDirty();

			this.SelectedIndex = this.Count - 1;
		}

		public void AddTrigger() {
			this.Add(new DfLevelGoals.Goal() {
				Type = DfLevelGoals.GoalTypes.Trigger
			});

			this.GetComponentInParent<GolViewer>().OnDirty();

			this.SelectedIndex = this.Count - 1;
		}

		public void MoveUp() {
			int index = this.SelectedIndex;
			if (index <= 0) {
				return;
			}

			DfLevelGoals.Goal selected = this.Value.ElementAt(index);
			this.RemoveAt(index);
			this.Insert(index - 1, selected);

			this.GetComponentInParent<GolViewer>().OnDirty();

			this.SelectedIndex = index - 1;
		}

		public void MoveDown() {
			int index = this.SelectedIndex;
			if (index < 0 || index >= this.Count - 1) {
				return;
			}

			DfLevelGoals.Goal selected = this.Value.ElementAt(index);
			this.RemoveAt(index);
			this.Insert(index + 1, selected);

			this.GetComponentInParent<GolViewer>().OnDirty();

			this.SelectedIndex = index + 1;
		}
	}
}
