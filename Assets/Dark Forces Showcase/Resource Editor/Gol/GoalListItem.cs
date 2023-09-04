using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;

namespace MZZT.DarkForces.Showcase {
	class GoalListItem : Databind<DfLevelGoals.Goal> {
		public void OnDirty() {
			this.GetComponentInParent<GolViewer>().OnDirty();
		}
	}
}
