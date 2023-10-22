using MZZT.IO.FileProviders;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace MZZT.DarkForces.Showcase {
	public class Mod : Singleton<Mod> {
		// TODO Folder adding, help

		[SerializeField, Header("References")]
		private GameObject background = null;
		[SerializeField]
		private ModFileList list = null;
		public ModFileList List => this.list;

		private ModFile[] oldList;

		public bool Visible { get; private set; }

		public async Task OnModClickedAsync() {
			this.oldList = this.list.ToArray();
			this.Visible = true;
			this.background.SetActive(true);

			while (this.Visible) {
				await Task.Yield();
			}
		}

		private string lastFolder = null;
		public async void OnAddModClickedAsync() {
			string file = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = false,
				SelectButtonText = "Add",
				SelectedFileMustExist = true,
				SelectedPathMustExist = true,
				StartPath = lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Select Mod File",
				ValidateFileName = true
			});

			if (file == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(file);

			ModFile modFile = new() {
				FilePath = file
			};
			switch (Path.GetExtension(file).ToUpper()) {
				case ".LFD":
					if (FileLoader.Instance.DarkForcesFolder != null &&
						FileManager.Instance.FileExists(Path.Combine(FileLoader.Instance.DarkForcesFolder, "LFD", Path.GetFileName(file).ToUpper()))) {

						modFile.Overrides = Path.GetFileName(file).ToUpper();
					} else if (!this.list.Any(x => x.Overrides == "DFBRIEF.LFD")) {
						modFile.Overrides = "DFBRIEF.LFD";
					} else if (!this.list.Any(x => x.Overrides == "FTEXTCRA.LFD")) {
						modFile.Overrides = "FTEXTCRA.LFD";
					} else {
						modFile.Overrides = Path.GetFileName(file).ToUpper();
					}
					break;
			}
			this.list.Add(modFile);
		}

		public void OnModCancel() {
			this.background.SetActive(false);
			this.list.Clear();
			this.list.AddRange(this.oldList);
			this.Visible = false;
		}

		public async void OnModApplyAsync() {
			this.background.SetActive(false);

			await this.LoadModAsync();

			ResourceCache.Instance.Clear();

			this.Visible = false;
		}

		public async Task LoadModAsync() {
			if (this.oldList != null) {
				foreach (ModFile file in this.oldList) {
					switch (Path.GetExtension(file.FilePath).ToUpper()) {
						case ".LFD":
							FileLoader.Instance.RemoveLfd(file.FilePath);
							break;
						default:
							FileLoader.Instance.RemoveGobFile(file.FilePath);
							break;
					}
				}
			}

			foreach (ModFile file in this.list) {
				switch (Path.GetExtension(file.FilePath).ToUpper()) {
					case ".LFD":
						FileLoader.Instance.AddLfd(file.FilePath, file.Overrides);
						break;
					default:
						await FileLoader.Instance.AddGobFileAsync(file.FilePath);
						break;
				}
			}
			this.oldList = this.list.ToArray();
		}

		public string Gob => this.List.FirstOrDefault(x => Path.GetExtension(x.FilePath).ToUpper() == ".GOB")?.FilePath;
	}
}
