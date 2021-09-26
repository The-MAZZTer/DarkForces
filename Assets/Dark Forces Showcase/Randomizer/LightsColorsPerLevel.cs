using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
	[RequireComponent(typeof(Toggle))]
	public class LightsColorsPerLevel : MonoBehaviour {
		private void OnEnable() {
			bool perLevel = Randomizer.Instance.Settings.Colormap.RandomizePerLevel ||
				Randomizer.Instance.Settings.Level.LightLevelMultiplierPerLevel ||
				Randomizer.Instance.Settings.Palette.RandomizePerLevel;
			this.GetComponent<Toggle>().isOn = perLevel;
		}

		private void Start() {
			this.GetComponent<Toggle>().onValueChanged.AddListener(value => {
				Randomizer.Instance.Settings.Colormap.RandomizePerLevel = value;
				Randomizer.Instance.Settings.Level.LightLevelMultiplierPerLevel = value;
				Randomizer.Instance.Settings.Palette.RandomizePerLevel = value;
			});
		}
	}
}
