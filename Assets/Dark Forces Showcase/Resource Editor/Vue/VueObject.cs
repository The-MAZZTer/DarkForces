using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using System.Collections.Generic;

namespace MZZT.DarkForces.Showcase {
	public class VueObject : Databind<KeyValuePair<string, AutodeskVue.VueObject>> {
		public void Remove() {
			VueObjectList parent = (VueObjectList)this.Parent;
			Dictionary<string, AutodeskVue.VueObject> dictionary = (Dictionary<string, AutodeskVue.VueObject>)parent.Value;
			dictionary.Remove(this.Value.Key);

			this.GetComponentInParent<VueViewer>().OnDirty();

			parent.Invalidate();
		}
	}
}
