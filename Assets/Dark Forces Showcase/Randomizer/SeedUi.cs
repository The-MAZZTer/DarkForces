using MZZT.DataBinding;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace MZZT.DarkForces.Showcase {
	public class SeedUi : Databound<RandomizerSettings> {
		[SerializeField]
		private TMP_InputField input;

		private void OnEnable() {
			this.Value = Randomizer.Instance.Settings;
			this.input.text = this.Value.Seed.ToString("X8");
		}

		public void OnSeedInputChanged(string value) {
			if (!int.TryParse(value, NumberStyles.HexNumber, null, out int seed)) {
				return;
			}
			Randomizer.Instance.Settings.Seed = seed;
		}
	}
}
