using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;

namespace MZZT.DarkForces.Showcase {
	public class BriefingLstBriefings : DataboundList<DfBriefingList.Briefing> {
		public void Add() {
			DfBriefingList.Briefing briefing = new();
			this.Add(briefing);

			this.GetComponentInParent<BriefingLstViewer>().OnDirty();

			this.SelectedValue = briefing;
		}
	}
}
