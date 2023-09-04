using MZZT.Components;
using TMPro;
using UnityEngine;

namespace MZZT.Data.Binding.UI {
	[RequireComponent(typeof(TMP_Text)),
		HideFieldsInInspector("autoApplyOnChange")]
	public class DataboundText : DataboundUi<string> {
		private TMP_Text text;
		protected TMP_Text Text {
			get {
				if (this.text == null) {
					this.text = this.GetComponent<TMP_Text>();
				}
				return this.text;
			}
		}

		[SerializeField]
		private string format = null;

		protected override bool IsReadOnly => true;

		private string value;
		protected override string UserEnteredValue {
			get => this.value;
			set {
				this.value = value;

				if (string.IsNullOrWhiteSpace(this.format)) {
					this.Text.text = value;
				} else {
					this.Text.text = string.Format(this.format, value);
				}
			}
		}
	}
}
