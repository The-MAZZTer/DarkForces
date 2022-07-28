using MZZT.DataBinding;
using System.IO;

namespace MZZT.DarkForces.Showcase {
	public class DataboundResourceDumperInputs : DataboundList<string> {
		private void Start() {
			this.Refresh();
		}

		private bool userInput = true;
		public void Refresh() {
			this.userInput = false;
			this.Clear();
			this.AddRange(this.GetComponentInParent<Databound<ResourceDumperSettings>>().Value.Inputs);
			this.userInput = true;
		}

		public override void Insert(int index, string item) {
			base.Insert(index, item);

			if (!this.userInput) {
				return;
			}

			this.GetComponentInParent<Databound<ResourceDumperSettings>>().Value.Inputs.Insert(index, item);
		}

		public override void RemoveAt(int index) {
			base.RemoveAt(index);

			if (!this.userInput) {
				return;
			}

			this.GetComponentInParent<Databound<ResourceDumperSettings>>().Value.Inputs.RemoveAt(index);
		}

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
