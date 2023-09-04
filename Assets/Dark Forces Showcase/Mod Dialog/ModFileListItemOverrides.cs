using MZZT.Data.Binding.UI;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MZZT.DarkForces.Showcase {
	public class ModFileListItemOverrides : DataboundStringDropdown {
		private string lastDisplayName;
		protected override void Invalidate() {
			if (this.Databinder != null && this.Databinder.Value is string modFile &&
				this.lastDisplayName != modFile?.ToUpper()) {

				this.lastDisplayName = modFile?.ToUpper();

				this.Dropdown.ClearOptions();

				HashSet<string> lfds = new() {
					this.lastDisplayName,
					"DFBRIEF.LFD",
					"FTEXTCRA.LFD"
				};

				string folder = FileLoader.Instance.DarkForcesFolder;
				if (!string.IsNullOrEmpty(folder)) {
					folder = Path.Combine(folder, "LFD");
					foreach (string lfd in Directory.EnumerateFiles(folder, "*.LFD")) {
						lfds.Add(Path.GetFileName(lfd).ToUpper());
					}

					this.Dropdown.AddOptions(lfds.ToList());
				}
			}

			base.Invalidate();
		}
	}
}
