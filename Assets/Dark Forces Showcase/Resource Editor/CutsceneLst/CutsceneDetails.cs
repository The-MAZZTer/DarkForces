using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace MZZT.DarkForces.Showcase {
	public class CutsceneDetails : Databind<KeyValuePair<int, DfCutsceneList.Cutscene>> {
		[Header("Cutscene"), SerializeField]
		private TMP_InputField idInput;
		[SerializeField]
		private TMP_Dropdown next;
		[SerializeField]
		private TMP_Dropdown escape;

		private bool userInput = true;

		protected override void OnInvalidate() {
			base.OnInvalidate();

			if (!this.userInput) {
				return;
			}

			this.gameObject.SetActive(this.Value.Key > 0);
			if (this.Value.Key == 0) {
				return;
			}

			CutsceneList cutscenes = this.GetComponentInParent<CutsceneLstViewer>().Cutscenes;

			this.userInput = false;
			try {
				this.next.ClearOptions();
				this.next.options.AddRange(new[] { new TMP_Dropdown.OptionData("None") }
					.Concat(cutscenes.Select(x => new TMP_Dropdown.OptionData($"{x.Key}: {x.Value.FilmFile}"))));
				this.next.value = -1;
				int index = this.Value.Value.NextCutscene == 0 ? 0 : cutscenes.TakeWhile(x => x.Key != this.Value.Value.NextCutscene).Count() + 1;
				if (index >= this.next.options.Count) {
					index = 0;
				}
				this.next.value = index;

				this.escape.ClearOptions();
				this.escape.options.AddRange(new[] { new TMP_Dropdown.OptionData("None") }
					.Concat(cutscenes.Select(x => new TMP_Dropdown.OptionData($"{x.Key}: {x.Value.FilmFile}"))));
				this.escape.value = -1;
				index = this.Value.Value.EscapeToCutscene == 0 ? 0 : cutscenes.TakeWhile(x => x.Key != this.Value.Value.EscapeToCutscene).Count() + 1;
				if (index >= this.escape.options.Count) {
					index = 0;
				}
				this.escape.value = index;

				this.idInput.text = this.Value.Key.ToString();
			} finally {
				this.userInput = true;
			}
		}

		public void OnIdChanged(string value) {
			if (!this.userInput) {
				return;
			}

			int.TryParse(value, out int id);

			if (id == this.Value.Key || id <= 0) {
				return;
			}

			CutsceneLstViewer viewer = this.GetComponentInParent<CutsceneLstViewer>();
			CutsceneList list = viewer.Cutscenes;
			Dictionary<int, DfCutsceneList.Cutscene> cutscenes = (Dictionary<int, DfCutsceneList.Cutscene>)list.Value;
			if (cutscenes.Keys.Except(new[] { this.Value.Key }).Contains(id)) {
				return;
			}

			DfCutsceneList.Cutscene cutscene = this.Value.Value;
			cutscenes.Remove(this.Value.Key);
			cutscenes[id] = cutscene;

			viewer.OnDirty();

			this.userInput = false;
			try {
				list.Invalidate();
				this.Value = list.SelectedValue = new KeyValuePair<int, DfCutsceneList.Cutscene>(id, cutscene);
			} finally {
				this.userInput = true;
			}
		}

		public void OnFilmChanged() {
			if (!this.userInput) {
				return;
			}

			/*if (value == this.Value.Value.FilmFile) {
				return;
			}*/

			CutsceneLstViewer viewer = this.GetComponentInParent<CutsceneLstViewer>();
			CutsceneList list = viewer.Cutscenes;
			
			this.userInput = false;
			try {
				list.Children.First(x => ((KeyValuePair<int, DfCutsceneList.Cutscene>)x.Value).Value == this.Value.Value).Invalidate();
			} finally {
				this.userInput = true;
			}

			viewer.OnDirty();
		}

		public void OnNextChanged(int value) {
			if (!this.userInput) {
				return;
			}

			CutsceneLstViewer viewer = this.GetComponentInParent<CutsceneLstViewer>();
			if (value == 0) {
				this.Value.Value.NextCutscene = 0;
			} else {
				CutsceneList cutscenes = viewer.Cutscenes;
				this.Value.Value.NextCutscene = cutscenes.ElementAt(value - 1).Key;
			}
			viewer.OnDirty();
		}

		public void OnEscapeChanged(int value) {
			if (!this.userInput) {
				return;
			}

			CutsceneLstViewer viewer = this.GetComponentInParent<CutsceneLstViewer>();
			if (value == 0) {
				this.Value.Value.EscapeToCutscene = 0;
			} else {
				CutsceneList cutscenes = viewer.Cutscenes;
				this.Value.Value.EscapeToCutscene = cutscenes.ElementAt(value - 1).Key;
			}
			viewer.OnDirty();
		}
	}
}
