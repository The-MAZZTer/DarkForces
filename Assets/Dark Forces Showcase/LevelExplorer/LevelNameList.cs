using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MZZT.DarkForces.Showcase {
	public class LevelNameList : DataboundList<DfLevelList.Level> {
		protected override void Start() {
			this.Value = LevelLoader.Instance.LevelList.Levels;
			if (this.ToggleGroup != null) {
				this.SelectedValue = this[LevelLoader.Instance.CurrentLevelIndex];
			}
		}
	}
}
