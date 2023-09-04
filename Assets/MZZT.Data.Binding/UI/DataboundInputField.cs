using TMPro;
using UnityEngine;

namespace MZZT.Data.Binding.UI {
	[RequireComponent(typeof(TMP_InputField))]
	public class DataboundInputField : DataboundUi<string> {
		private TMP_InputField InputField => this.Selectable as TMP_InputField;

		protected override void Start() {
			base.Start();

			this.InputField.onValueChanged.AddListener(value => this.OnUserEnteredValueChanged());
		}

		protected override bool IsReadOnly => base.IsReadOnly || this.InputField.readOnly;

		protected override string UserEnteredValue {
			get => this.InputField.text;
			set => this.InputField.text = value;
		}
	}
}
