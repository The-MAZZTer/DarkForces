using UnityEngine;
using UnityEngine.UI;

namespace MZZT.Data.Binding.UI {
	[RequireComponent(typeof(Toggle))]
	public class DataboundFlagToggle : DataboundUi<long> {
		[Header("Flag"), SerializeField]
		private long flag;
		[SerializeField]
		private bool invert;

		protected Toggle Toggle => this.Selectable as Toggle;

		protected override void Start() {
			base.Start();

			this.Toggle.onValueChanged.AddListener(value => this.OnUserEnteredValueChanged());
		}

		protected override long UserEnteredValue {
			get => (this.Toggle.isOn ^ this.invert) ? (this.Value | this.flag) : (this.Value & ~this.flag);
			set => this.Toggle.isOn = ((value & this.flag) == this.flag) ^ this.invert;
		}
	}
}
