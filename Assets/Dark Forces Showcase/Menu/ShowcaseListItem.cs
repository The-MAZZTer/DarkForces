using MZZT.DataBinding;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
	public class ShowcaseListItem : Databound<Showcase> {
		public override void Invalidate() {
			base.Invalidate();

			this.gameObject.name = this.Value.Name ?? "<Spacer>";
			bool isSpacer = this.Value.Name == null;
			this.GetComponent<LayoutElement>().flexibleHeight = isSpacer ? 1 : 0;
			this.GetComponent<Image>().enabled = !isSpacer;

			Toggle toggle = this.Toggle;
			toggle.graphic.enabled = !isSpacer;
			toggle.enabled = !isSpacer;
		}
	}
}
