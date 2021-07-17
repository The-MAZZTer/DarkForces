using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace MZZT.DataBinding {
	[RequireComponent(typeof(Scrollbar))]
	public class DataboundScrollbar : DataboundUi<float> {
		protected Scrollbar Scrollbar => this.Selectable as Scrollbar;

		private void Start() {
			this.Scrollbar.onValueChanged.AddListener(value => this.OnUserEnteredValueChanged());
		}

		protected override bool IsReadOnly => base.IsReadOnly;

		protected override float UserEnteredValue {
			get => this.Scrollbar.value;
			set => this.Scrollbar.SetValueWithoutNotify(value);
		}
	}
}
