using MZZT.DataBinding;
using TMPro;
using UnityEngine;

namespace MZZT.DarkForces.Showcase {
  public class ResourceDumperInput : Databound<string> {
    [SerializeField]
    private TextMeshProUGUI nameField;

		public override void Invalidate() {
			base.Invalidate();

			this.name = this.Value;
			this.nameField.text = this.Value;
		}

		public void Delete() {
			this.GetComponentInParent<DataboundResourceDumperInputs>().RemoveAt(this.transform.GetSiblingIndex());
		}
	}
}
