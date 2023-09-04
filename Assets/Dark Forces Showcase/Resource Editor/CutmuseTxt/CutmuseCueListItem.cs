using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using System.Collections.Generic;

namespace MZZT.DarkForces.Showcase {
	public class CutmuseCueListItem : Databind<DfCutsceneMusicList.Cue> {
		public void Remove() {
			CutmuseCueList parent = (CutmuseCueList)this.Parent;
			List<DfCutsceneMusicList.Cue> dictionary = (List<DfCutsceneMusicList.Cue>)parent.Value;
			dictionary.Remove(this.Value);

			CutmuseTxtViewer viewer = (CutmuseTxtViewer)parent.Parent.Parent.Parent.Parent;
			viewer.OnDirty();

			if (parent.Count == 0) {
				viewer.CueDetails.gameObject.SetActive(false);
			}
			parent.Invalidate();
		}
	}
}
