using MZZT.Data.Binding;

namespace MZZT.DarkForces.Showcase {
	public class VocCommentMarker : DatabindObject {
		public void OnDirty() {
			this.GetComponentInParent<VocViewer>().OnDirty();
		}
	}
}