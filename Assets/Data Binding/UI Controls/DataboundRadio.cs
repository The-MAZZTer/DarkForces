using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DataBinding {
	[RequireComponent(typeof(Toggle))]
	public class DataboundRadio : DataboundUi<int> {
		[SerializeField]
		private int value;

		protected Toggle Toggle => this.Selectable as Toggle;

		private void Start() {
			this.Toggle.onValueChanged.AddListener(value => this.OnUserEnteredValueChanged());
		}

		protected override int UserEnteredValue {
			get => this.Toggle.isOn ? this.value : this.Value;
			set => this.Toggle.isOn = this.value == value;
		}
	}
}
