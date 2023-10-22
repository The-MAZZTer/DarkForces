using MZZT.Data.Binding.UI;
using MZZT.IO.FileProviders;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MZZT.DarkForces.Showcase {
	public class ModFileListItemOverrides : DataboundStringDropdown {
		private string lastDisplayName;
		protected override async void Invalidate() {
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
					await foreach (string lfd in FileManager.Instance.FolderEnumerateFilesAsync(folder, "*.LFD")) {
						lfds.Add(Path.GetFileName(lfd).ToUpper());
					}

					this.Dropdown.AddOptions(lfds.ToList());
				}
			}

			base.Invalidate();
		}
	}
}
