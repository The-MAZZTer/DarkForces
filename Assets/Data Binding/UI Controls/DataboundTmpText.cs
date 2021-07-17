using MZZT.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DataBinding {
	[RequireComponent(typeof(TMP_Text)),
		HideFieldsInInspector("autoApplyOnChange")]
	public class DataboundTmpText : DataboundUi<string> {
		private TMP_Text text;
		protected TMP_Text Text {
			get {
				if (this.text == null) {
					this.text = this.GetComponent<TMP_Text>();
				}
				return this.text;
			}
		}

		protected override bool IsReadOnly => true;

		protected override string UserEnteredValue {
			get => this.Text.text;
			set => this.Text.text = value;
		}
	}
}
