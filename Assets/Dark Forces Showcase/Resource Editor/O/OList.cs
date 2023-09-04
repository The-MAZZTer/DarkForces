using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;

namespace MZZT.DarkForces.Showcase {
	class OList : DataboundList<DfLevelObjects.Object> {
		public void Add() {
			this.Add(new DfLevelObjects.Object());

			this.GetComponentInParent<OViewer>().OnDirty();

			this.SelectedIndex = this.Count - 1;
		}

		public void InvalidateSelectedItem() {
			this.SelectedDatabound?.Invalidate();
		}
	}
}