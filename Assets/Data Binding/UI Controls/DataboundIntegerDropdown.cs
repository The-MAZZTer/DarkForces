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
	public class DataboundIntegerDropdown : DataboundUi<int> {
		protected Dropdown Dropdown => this.Selectable as Dropdown;

		private void Start() {
			this.Dropdown.onValueChanged.AddListener(value => this.OnUserEnteredValueChanged());
		}

		protected override int UserEnteredValue {
			get => this.Dropdown.value;
			set => this.Dropdown.value = value;
		}
	}
}
