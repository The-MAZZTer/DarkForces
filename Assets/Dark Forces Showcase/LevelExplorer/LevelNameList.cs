using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;

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
