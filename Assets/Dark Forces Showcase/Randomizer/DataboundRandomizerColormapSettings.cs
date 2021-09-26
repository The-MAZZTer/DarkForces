using MZZT.DataBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MZZT.DarkForces.Showcase {
	public class DataboundRandomizerColormapSettings : Databound<RandomizerColormapSettings> {
		private void OnEnable() {
			this.Value = Randomizer.Instance.Settings.Colormap;
		}
	}
}
