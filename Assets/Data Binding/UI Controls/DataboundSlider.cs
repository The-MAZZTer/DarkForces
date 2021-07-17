using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace MZZT.DataBinding {
	[RequireComponent(typeof(Slider))]
	public class DataboundSlider : DataboundUi<float> {
		protected Slider Slider => this.Selectable as Slider;

		private void Start() {
			this.Slider.wholeNumbers =
				this.MemberType != typeof(float) &&
				this.MemberType != typeof(double) &&
				this.MemberType != typeof(decimal);

			this.Slider.onValueChanged.AddListener(value => this.OnUserEnteredValueChanged());
		}

		protected override bool IsReadOnly => base.IsReadOnly;

		protected override float UserEnteredValue {
			get => this.Slider.value;
			set => this.Slider.value = value;
		}
	}
}
