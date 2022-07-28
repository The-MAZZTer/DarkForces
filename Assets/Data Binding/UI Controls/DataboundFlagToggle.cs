using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DataBinding {
	[RequireComponent(typeof(Toggle))]
	public class DataboundFlagToggle : DataboundUi<long> {
		[SerializeField]
		private long flag;

		protected Toggle Toggle => this.Selectable as Toggle;

		private void Start() {
			this.Toggle.onValueChanged.AddListener(value => this.OnUserEnteredValueChanged());
		}

		protected override long UserEnteredValue {
			get => this.Toggle.isOn ? (this.Value | this.flag) : (this.Value & ~this.flag);
			set => this.Toggle.isOn = (value & this.flag) == this.flag;
		}
	}
}
