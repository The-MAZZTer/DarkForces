using UnityEngine;
using UnityEngine.UI;

namespace MZZT.Data.Binding.UI {
	[RequireComponent(typeof(Slider))]
	public class DataboundSlider : DataboundUi<float> {
		protected Slider Slider => this.Selectable as Slider;

		protected override void Start() {
			this.Slider.wholeNumbers =
				this.Databinder.MemberType != typeof(float) &&
				this.Databinder.MemberType != typeof(double) &&
				this.Databinder.MemberType != typeof(decimal);

			base.Start();

			this.Slider.onValueChanged.AddListener(value => this.OnUserEnteredValueChanged());
		}

		protected override float UserEnteredValue {
			get => this.Slider.value;
			set => this.Slider.value = value;
		}
	}
}
