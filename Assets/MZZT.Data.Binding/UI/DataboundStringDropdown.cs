using System.Linq;
using TMPro;
using UnityEngine;

namespace MZZT.Data.Binding.UI {
	[RequireComponent(typeof(TMP_Dropdown))]
	public class DataboundStringDropdown : DataboundUi<string> {
		protected TMP_Dropdown Dropdown => this.Selectable as TMP_Dropdown;

		protected override void Start() {
			base.Start();

			this.Dropdown.onValueChanged.AddListener(value => this.OnUserEnteredValueChanged());
		}

		protected override string UserEnteredValue {
			get => this.Dropdown.options[this.Dropdown.value].text;
			set => this.Dropdown.value = this.Dropdown.options.Select((x, i) => (x, i)).FirstOrDefault(x => x.x.text == value).i;
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
