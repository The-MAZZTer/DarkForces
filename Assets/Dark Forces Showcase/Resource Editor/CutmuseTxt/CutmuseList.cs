using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using System.Collections.Generic;
using System.Linq;

namespace MZZT.DarkForces.Showcase {
	public class CutmuseList : DataboundList<KeyValuePair<int, DfCutsceneMusicList.Sequence>> {
		public void Add() {
			int next = this.Select(x => x.Key).Prepend(0).Max() + 1;

			((Dictionary<int, DfCutsceneMusicList.Sequence>)this.Value)[next] = new DfCutsceneMusicList.Sequence();

			this.GetComponentInParent<CutmuseTxtViewer>().OnDirty();

			this.Invalidate();

			this.SelectedDatabound = this.Children.First(x => ((KeyValuePair<int, DfCutsceneMusicList.Sequence>)x.Value).Key == next);
		}
	}
}
