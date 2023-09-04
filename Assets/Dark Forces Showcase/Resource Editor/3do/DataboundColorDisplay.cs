using MZZT.Data.Binding;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
	public class DataboundColorDisplay : Databind<Color> {
		[Header("Color"), SerializeField]
		private Graphic target;

		protected override void OnInvalidate() {
			base.OnInvalidate();

			if (this.target == null) {
				this.target = this.GetComponent<Graphic>();
			}

			this.target.color = this.Value;
		}
	}
}
