using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DataBinding {
	[RequireComponent(typeof(InputField))]
	public class DataboundInputField : DataboundUi<string> {
		protected InputField InputField => this.Selectable as InputField;

		private void Start() {
			this.InputField.onValueChanged.AddListener(value => this.OnUserEnteredValueChanged());
		}

		protected override bool IsReadOnly => base.IsReadOnly || this.InputField.readOnly;

		protected override string UserEnteredValue {
			get => this.InputField.text;
			set => this.InputField.text = value;
		}
	}
}
