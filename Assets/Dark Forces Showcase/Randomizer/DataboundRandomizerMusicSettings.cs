using MZZT.DataBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MZZT.DarkForces.Showcase {
	public class DataboundRandomizerMusicSettings : Databound<RandomizerMusicSettings> {
		private void OnEnable() {
			this.Value = Randomizer.Instance.Settings.Music;
		}
	}
}
