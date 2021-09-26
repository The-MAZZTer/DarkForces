using MZZT.DataBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MZZT.DarkForces.Showcase {
	public class DataboundRandomizerPaletteSettings : Databound<RandomizerPaletteSettings> {
		private void OnEnable() {
			this.Value = Randomizer.Instance.Settings.Palette;
		}
	}
}
