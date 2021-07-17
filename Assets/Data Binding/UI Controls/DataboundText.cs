using MZZT.Components;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DataBinding {
	[RequireComponent(typeof(Text)),
		HideFieldsInInspector("autoApplyOnChange")]
	public class DataboundText : DataboundUi<string> {
		private Text text;
		protected Text Text {
			get {
				if (this.text == null) {
					this.text = this.GetComponent<Text>();
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
