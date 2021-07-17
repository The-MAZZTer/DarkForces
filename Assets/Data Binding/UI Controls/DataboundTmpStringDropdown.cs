using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace MZZT.DataBinding {
	[RequireComponent(typeof(TMP_Dropdown))]
	public class DataboundTmpStringDropdown : DataboundUi<string> {
		protected TMP_Dropdown Dropdown => this.Selectable as TMP_Dropdown;

		private void Start() {
			this.Dropdown.onValueChanged.AddListener(value => this.OnUserEnteredValueChanged());
		}

		protected override string UserEnteredValue {
			get => this.Dropdown.options[this.Dropdown.value].text;
			set => this.Dropdown.value = this.Dropdown.options.Select((x, i) => (x, i)).FirstOrDefault(x => x.x.text == value).i;
		}
	}
}
