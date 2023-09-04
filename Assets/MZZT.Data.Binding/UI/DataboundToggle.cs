using UnityEngine;
using UnityEngine.UI;

namespace MZZT.Data.Binding.UI {
	[RequireComponent(typeof(Toggle))]
	public class DataboundToggle : DataboundUi<bool> {
		protected Toggle Toggle => this.Selectable as Toggle;

		protected override void Start() {
			base.Start();

			this.Toggle.onValueChanged.AddListener(value => this.OnUserEnteredValueChanged());
		}

		protected override bool UserEnteredValue {
			get => this.Toggle.isOn;
			set => this.Toggle.isOn = value;
		}
	}
}
