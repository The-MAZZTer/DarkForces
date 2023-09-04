using MZZT.Data.Binding;
using System.IO;

namespace MZZT.DarkForces.Showcase {
	public class DataboundResourceDumperInputs : DataboundList<string> {
		private string lastAddPath;
		public async void AddFileAsync() {
			if (string.IsNullOrEmpty(this.lastAddPath)) {
				this.lastAddPath = FileLoader.Instance.DarkForcesFolder;
			}

			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = true,
				AllowNavigateLfd = true,
				FileSearchPatterns = new[] { "*" },
				SelectButtonText = "Add",
				SelectedFileMustExist = true,
				SelectedPathMustExist = true,
				StartPath = this.lastAddPath,
				Title = "Add File"
			});
			if (path == null) {
				return;
			}
			this.lastAddPath = Path.GetDirectoryName(path);

			this.Add(path);
		}
		public async void AddFolderAsync() {
			if (string.IsNullOrEmpty(this.lastAddPath)) {
				this.lastAddPath = FileLoader.Instance.DarkForcesFolder;
			}

			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				FileSearchPatterns = new[] { "*.GOB", "*.LFD" },
				SelectButtonText = "Add",
				SelectedFileMustExist = true,
				SelectedPathMustExist = true,
				SelectFolder = true,
				StartPath = this.lastAddPath,
				Title = "Add Folder/GOB/LFD"
			});
			if (path == null) {
				return;
			}
			this.lastAddPath = Path.GetDirectoryName(path);

			this.Add(path);
		}

	}
}
