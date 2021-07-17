using UnityEngine;

namespace MZZT.DataBinding {
	public class DataboundPrefab : DataboundMember<GameObject> {
		private GameObject child;
		protected override void OnInvalidate() {
			if (this.child != null) {
				DestroyImmediate(this.child);
			}
			if (this.Value != null) {
				this.child = Instantiate(this.Value);
				this.child.transform.SetParent(this.transform, false);
			}
		}
	}
}
