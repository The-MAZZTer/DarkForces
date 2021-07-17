using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace MZZT.DataBinding {
	[RequireComponent(typeof(Dropdown))]
	public class DataboundStringDropdown : DataboundUi<string> {
		protected Dropdown Dropdown => this.Selectable as Dropdown;

		private void Start() {
			this.Dropdown.onValueChanged.AddListener(value => this.OnUserEnteredValueChanged());
		}

		protected override string UserEnteredValue {
			get => this.Dropdown.options[this.Dropdown.value].text;
			set => this.Dropdown.value = this.Dropdown.options.Select((x, i) => (x, i)).FirstOrDefault(x => x.x.text == value).i;
		}
	}
}
