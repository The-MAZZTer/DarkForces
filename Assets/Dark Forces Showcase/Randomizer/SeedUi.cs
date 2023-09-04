using MZZT.Data.Binding;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace MZZT.DarkForces.Showcase {
	public class SeedUi : Databind<int> {
		[SerializeField]
		private TMP_InputField input;

		protected override void OnEnable() {
			base.OnEnable();

			this.input.text = this.Value.ToString("X8");
		}

		public void OnSeedInputChanged(string value) {
			if (!int.TryParse(value, NumberStyles.HexNumber, null, out int seed)) {
				return;
			}
			this.Value = seed;
		}
	}
}
