using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using System.Collections.Generic;

namespace MZZT.DarkForces.Showcase {
	public class CutsceneListItem : Databind<KeyValuePair<int, DfCutsceneList.Cutscene>> {
		public void Remove() {
			CutsceneList parent = (CutsceneList)this.Parent;
			Dictionary<int, DfCutsceneList.Cutscene> dictionary = (Dictionary<int, DfCutsceneList.Cutscene>)parent.Value;
			dictionary.Remove(this.Value.Key);

			CutsceneLstViewer viewer = (CutsceneLstViewer)parent.Parent;
			viewer.OnDirty();

			if (parent.Count == 0) {
				viewer.Details.gameObject.SetActive(false);
			}
			parent.Invalidate();
		}
	}
}
