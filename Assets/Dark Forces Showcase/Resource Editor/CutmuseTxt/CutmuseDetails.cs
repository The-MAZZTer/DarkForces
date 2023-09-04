using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace MZZT.DarkForces.Showcase {
	public class CutmuseDetails : Databind<KeyValuePair<int, DfCutsceneMusicList.Sequence>> {
		[Header("Sequence"), SerializeField]
		private TMP_InputField idInput;

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

			this.userInput = false;
			try {
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

			CutmuseList list = this.GetComponentInParent<CutmuseTxtViewer>().Cutscenes;
			Dictionary<int, DfCutsceneMusicList.Sequence> cutscenes = (Dictionary<int, DfCutsceneMusicList.Sequence>)list.Value;
			if (cutscenes.Keys.Except(new[] { this.Value.Key }).Contains(id)) {
				return;
			}

			DfCutsceneMusicList.Sequence cutscene = this.Value.Value;
			cutscenes.Remove(this.Value.Key);
			cutscenes[id] = cutscene;

			this.GetComponentInParent<CutmuseTxtViewer>().OnDirty();

			this.userInput = false;
			try {
				list.Invalidate();
				this.Value = list.SelectedValue = new KeyValuePair<int, DfCutsceneMusicList.Sequence>(id, cutscene);
			} finally {
				this.userInput = true;
			}
		}
	}
}