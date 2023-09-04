using UnityEngine;
using UnityEngine.UI;

namespace MZZT.Data.Binding.UI {
	[RequireComponent(typeof(Scrollbar))]
	public class DataboundScrollbar : DataboundUi<float> {
		protected Scrollbar Scrollbar => this.Selectable as Scrollbar;

		protected override void Start() {
			base.Start();

			this.Scrollbar.onValueChanged.AddListener(value => this.OnUserEnteredValueChanged());
		}

		protected override float UserEnteredValue {
			get => this.Scrollbar.value;
			set => this.Scrollbar.SetValueWithoutNotify(value);
		}
	}
}
