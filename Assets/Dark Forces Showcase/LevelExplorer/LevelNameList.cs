using MZZT.DataBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MZZT.DarkForces.Showcase {
	public class LevelNameList : DataboundList<int> {
		private void Start() {
			this.Refresh();
		}

		public void Refresh() {
			this.Clear();
			this.AddRange(Enumerable.Range(0, LevelLoader.Instance.LevelList.Levels.Count));
			this.SelectedValue = LevelLoader.Instance.CurrentLevelIndex;
		}
	}
}
