using TMPro;
using UnityEngine;

namespace MZZT.Data.Binding.UI {
	[RequireComponent(typeof(TMP_Dropdown))]
	public class DataboundIntegerDropdown : DataboundUi<int> {
		protected TMP_Dropdown Dropdown => this.Selectable as TMP_Dropdown;

		protected override void Start() {
			base.Start();

			this.Dropdown.onValueChanged.AddListener(value => this.OnUserEnteredValueChanged());
		}

		protected override int UserEnteredValue {
			get => this.Dropdown.value;
			set => this.Dropdown.value = value;
		}

		private void Update() {
			int value = this.Dropdown.value;
			if (this.Dropdown.captionText.text == "" && !string.IsNullOrEmpty(this.Dropdown.options[value].text)) {
				this.userInput = false;
				try {
					this.Dropdown.value = -1;
					this.Dropdown.value = value;
				} finally {
					this.userInput = true;
				}
			}
		}
	}
}
