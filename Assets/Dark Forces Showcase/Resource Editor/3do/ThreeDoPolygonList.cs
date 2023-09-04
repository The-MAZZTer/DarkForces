using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
	public class ThreeDoPolygonList : DataboundList<Df3dObject.Polygon> {
		public override ToggleGroup ToggleGroup {
			get {
				ToggleGroup group = base.ToggleGroup;
				if (group == null) {
					this.ToggleGroup = group = this.GetComponentInParent<ThreeDoObjectList>().ToggleGroup;
				}
				return group;
			}
			set => base.ToggleGroup = value;
		}

		protected override void OnItemRemoved(int index, Df3dObject.Polygon value) {
			base.OnItemRemoved(index, value);

			this.GetComponentInParent<ThreeDoViewer>().OnPolygonRemoved(index, value);
		}

		protected override void OnSelectedValueChanged() {
			base.OnSelectedValueChanged();

			this.GetComponentInParent<ThreeDoViewer>().OnSelectedItemChanged();
		}
	}
}