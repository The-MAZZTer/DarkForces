using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
	public class CutmuseCueList : DataboundList<DfCutsceneMusicList.Cue> {
		[SerializeField]
		private Button moveUp;
		[SerializeField]
		private Button moveDown;

		protected override void OnInvalidate() {
			base.OnInvalidate();

			this.RefreshMoveButtons();
		}

		protected override void OnSelectedValueChanged() {
			base.OnSelectedValueChanged();

			this.RefreshMoveButtons();
		}

		protected override void OnItemAdded(int index, IDatabind bind) {
			base.OnItemAdded(index, bind);

			this.RefreshMoveButtons();
		}

		protected override void OnItemRemoved(int index, DfCutsceneMusicList.Cue value) {
			base.OnItemRemoved(index, value);

			this.RefreshMoveButtons();
		}

		private void RefreshMoveButtons() {
			int index = ((Component)this.SelectedDatabound)?.transform.GetSiblingIndex() ?? -1;

			this.moveUp.interactable = index > 0;
			this.moveDown.interactable = index >= 0 && index < this.transform.childCount - 1;
		}

		public void MoveUp() {
			int index = ((Component)this.SelectedDatabound).transform.GetSiblingIndex();

			DfCutsceneMusicList.Cue cue = this.SelectedValue;
			List<DfCutsceneMusicList.Cue> list = (List<DfCutsceneMusicList.Cue>)this.Value;
			list.RemoveAt(index);
			list.Insert(index - 1, cue);

			this.GetComponentInParent<CutmuseTxtViewer>().OnDirty();

			((Component)this.SelectedDatabound).transform.SetSiblingIndex(index - 1);

			this.RefreshMoveButtons();
		}

		public void MoveDown() {
			int index = ((Component)this.SelectedDatabound).transform.GetSiblingIndex();

			DfCutsceneMusicList.Cue cue = this.SelectedValue;
			List<DfCutsceneMusicList.Cue> list = (List<DfCutsceneMusicList.Cue>)this.Value;
			list.RemoveAt(index);
			list.Insert(index + 1, cue);

			this.GetComponentInParent<CutmuseTxtViewer>().OnDirty();

			((Component)this.SelectedDatabound).transform.SetSiblingIndex(index + 1);

			this.RefreshMoveButtons();
		}

		public void Add() {
			DfCutsceneMusicList.Cue cue = new();
			this.Value.Add(cue);

			this.GetComponentInParent<CutmuseTxtViewer>().OnDirty();

			this.Invalidate();

			this.SelectedDatabound = this.Children.First(x => x.Value == cue);
		}
	}
}
