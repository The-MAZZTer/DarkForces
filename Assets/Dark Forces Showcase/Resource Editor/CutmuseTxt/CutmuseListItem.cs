using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace MZZT.DarkForces.Showcase {
	public class CutmuseListItem : Databind<KeyValuePair<int, DfCutsceneMusicList.Sequence>> {
		[SerializeField]
		private TMP_Text text;

		protected override void OnInvalidate() {
			base.OnInvalidate();
			int id = this.Value.Key;
			this.text.text = $"Unknown";

			DfCutsceneList list = this.GetComponentInParent<CutmuseTxtViewer>().CutsceneList;
			if (list != null) {
				DfCutsceneList.Cutscene cutscene = list.Cutscenes.Values.FirstOrDefault(x => x.CutmuseSequence == id);
				if (cutscene != null) {
					this.text.text = cutscene.FilmFile;
				}
			}
		}

		public void Remove() {
			CutmuseList parent = (CutmuseList)this.Parent;
			Dictionary<int, DfCutsceneMusicList.Sequence> dictionary = (Dictionary<int, DfCutsceneMusicList.Sequence>)parent.Value;
			dictionary.Remove(this.Value.Key);

			CutmuseTxtViewer viewer = (CutmuseTxtViewer)parent.Parent;
			viewer.OnDirty();

			if (parent.Count == 0) {
				viewer.Details.gameObject.SetActive(false);
			}
			parent.Invalidate();
		}
	}
}
