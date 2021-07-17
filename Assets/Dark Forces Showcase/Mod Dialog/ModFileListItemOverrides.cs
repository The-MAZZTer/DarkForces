using MZZT.DataBinding;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MZZT.DarkForces.Showcase {
	public class ModFileListItemOverrides : DataboundTmpStringDropdown {
		private string lastDisplayName;
		protected override void OnInvalidate() {
			if (this.DataboundObject != null && this.DataboundObject.Value != null &&
				this.lastDisplayName != ((ModFile)this.DataboundObject.Value).DisplayName.ToUpper()) {

				this.lastDisplayName = ((ModFile)this.DataboundObject.Value).DisplayName.ToUpper();

				this.Dropdown.ClearOptions();

				HashSet<string> lfds = new HashSet<string>() {
					"DFBRIEF.LFD",
					"FTEXTCRA.LFD"
				};
				lfds.Add(this.lastDisplayName);

				string folder = FileLoader.Instance.DarkForcesFolder;
				if (!string.IsNullOrEmpty(folder)) {
					folder = Path.Combine(folder, "LFD");
					foreach (string lfd in Directory.EnumerateFiles(folder, "*.LFD")) {
						lfds.Add(Path.GetFileName(lfd).ToUpper());
					}

					this.Dropdown.AddOptions(lfds.ToList());
				}
			}


			base.OnInvalidate();
		}
	}
}
