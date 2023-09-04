using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using System.Collections.Generic;
using System.Linq;

namespace MZZT.DarkForces.Showcase {
	public class CutsceneList : DataboundList<KeyValuePair<int, DfCutsceneList.Cutscene>> {
		public void Add() {
			int next = this.Select(x => x.Key).Prepend(0).Max() + 1;

			((Dictionary<int, DfCutsceneList.Cutscene>)this.Value)[next] = new DfCutsceneList.Cutscene() {
				Lfd = "",
				FilmFile = "",
				Speed = 10,
				Volume = 100
			};

			this.GetComponentInParent<CutsceneLstViewer>().OnDirty();

			this.Invalidate();

			this.SelectedDatabound = this.Children.First(x => ((KeyValuePair<int, DfCutsceneList.Cutscene>)x.Value).Key == next);
		}
	}
}
