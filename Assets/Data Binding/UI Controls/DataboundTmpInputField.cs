using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DataBinding {
	[RequireComponent(typeof(TMP_InputField))]
	public class DataboundTmpInputField : DataboundUi<string> {
		protected TMP_InputField InputField => this.Selectable as TMP_InputField;

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
