using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using System.IO;

namespace MZZT.DarkForces.Showcase {
	class FileLocationInfoListItem : Databind<FileLocationInfo> {
		public async void ExportAsync() => await this.GetComponentInParent<GobViewer>(true).ExportAsync(this.Value);

		public void Open() {
			string path = this.Value.New ? this.Value.SourceFile : Path.Combine(this.Value.SourceFile, this.Value.Name);
			ResourceEditorResource resource = ResourceEditors.Instance.FindResource(path) ?? new ResourceEditorResource(path, async () => await DfFile.GetFileFromFolderOrContainerAsync(path), false);
			ResourceEditors.Instance.OpenResource(resource);
		}
	}
}