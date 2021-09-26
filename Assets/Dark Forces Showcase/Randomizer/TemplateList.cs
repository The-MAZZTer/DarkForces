using MZZT.DataBinding;

namespace MZZT.DarkForces.Showcase {
  public class TemplateList : DataboundList<ObjectTemplate> {
		private void OnEnable() {
			this.Clear();
			this.AddRange(Randomizer.Instance.Settings.Object.DefaultLogicFiles);
		}
	}
}
