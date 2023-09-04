using UnityEngine;
using UnityEngine.UI;

namespace MZZT.Data.Binding.UI {
	[RequireComponent(typeof(Toggle))]
	public class DataboundToggleGroupToggle : DataboundUi<int> {
		[SerializeField]
		private int value;

		protected Toggle Toggle => this.Selectable as Toggle;

		protected override void Start() {
			base.Start();

			this.Toggle.onValueChanged.AddListener(value => this.OnUserEnteredValueChanged());
		}

		protected override int UserEnteredValue {
			get => this.Toggle.isOn ? this.value : this.Value;
			set => this.Toggle.isOn = this.value == value;
		}
	}
}
