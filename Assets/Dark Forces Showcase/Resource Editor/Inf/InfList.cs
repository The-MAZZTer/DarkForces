using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;

namespace MZZT.DarkForces.Showcase {
	class InfList : DataboundList<DfLevelInformation.Item> {
		public void AddLevel() {
			this.Add(new DfLevelInformation.Item() {
				Type = DfLevelInformation.ScriptTypes.Level
			});

			this.GetComponentInParent<InfViewer>().OnDirty();

			this.SelectedIndex = this.Count - 1;
		}

		public void AddSector() {
			this.Add(new DfLevelInformation.Item() {
				Type = DfLevelInformation.ScriptTypes.Sector
			});

			this.GetComponentInParent<InfViewer>().OnDirty();

			this.SelectedIndex = this.Count - 1;
		}

		public void AddWall() {
			this.Add(new DfLevelInformation.Item() {
				Type = DfLevelInformation.ScriptTypes.Line
			});

			this.GetComponentInParent<InfViewer>().OnDirty();

			this.SelectedIndex = this.Count - 1;
		}

		public void InvalidateSelectedItem() {
			this.SelectedDatabound?.Invalidate();
		}
	}
}