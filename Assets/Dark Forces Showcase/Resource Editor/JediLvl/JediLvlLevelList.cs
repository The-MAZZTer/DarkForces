using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
	class JediLvlLevelList : DataboundList<DfLevelList.Level> {
		[Header("JEDI.LVL"), SerializeField]
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

		public void MoveUp() {
			int index = this.SelectedIndex;
			if (index <= 0) {
				return;
			}

			DfLevelList.Level selected = this.Value.ElementAt(index);
			this.RemoveAt(index);
			this.Insert(index - 1, selected);

			this.GetComponentInParent<JediLvlViewer>().OnDirty();

			this.SelectedIndex = index - 1;
		}

		public void MoveDown() {
			int index = this.SelectedIndex;
			if (index < 0 || index >= this.Count - 1) {
				return;
			}

			DfLevelList.Level selected = this.Value.ElementAt(index);
			this.RemoveAt(index);
			this.Insert(index + 1, selected);

			this.GetComponentInParent<JediLvlViewer>().OnDirty();

			this.SelectedIndex = index + 1;
		}

		public void Add() {
			this.Add(new DfLevelList.Level());

			this.GetComponentInParent<JediLvlViewer>().OnDirty();

			this.SelectedIndex = this.Count - 1;
		}

		public void InvalidateSelectedItem() {
			this.SelectedDatabound?.Invalidate();
		}
	}
}