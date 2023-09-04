using MZZT.Data.Binding;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
	public class ShowcaseListItem : Databind<Showcase> {
		protected override void OnInvalidate() {
			bool isSpacer = this.Value.Name == null;
			this.GetComponent<LayoutElement>().flexibleHeight = isSpacer ? 1 : 0;
			this.GetComponent<Image>().enabled = !isSpacer;

			Toggle toggle = DataboundListChildToggle.FindToggleFor(this);
			toggle.graphic.enabled = !isSpacer;
			toggle.enabled = !isSpacer;

			base.OnInvalidate();

			this.gameObject.name = this.Value.Name ?? "<Spacer>";
		}
	}
}
