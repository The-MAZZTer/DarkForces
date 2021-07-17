using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DataBinding {
	[RequireComponent(typeof(Toggle))]
	public class DataboundToggle : DataboundUi<bool> {
		protected Toggle Toggle => this.Selectable as Toggle;

		private void Start() {
			this.Toggle.onValueChanged.AddListener(value => this.OnUserEnteredValueChanged());
		}

		protected override bool UserEnteredValue {
			get => this.Toggle.isOn;
			set => this.Toggle.isOn = value;
		}
	}
}
