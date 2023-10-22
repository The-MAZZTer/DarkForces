using MZZT.Data.Binding;

namespace MZZT.DarkForces.Showcase {
	class FileLocationInfoListItem : Databind<FileLocationInfo> {
		public async void ExportAsync() {
			GobViewer gobViewer = this.GetComponentInParent<GobViewer>();
			if (gobViewer != null) {
				await gobViewer.ExportAsync(this.Value);
				return;
			}

			LfdViewer lfdViewer = this.GetComponentInParent<LfdViewer>();
			if (lfdViewer != null) {
				await lfdViewer.ExportAsync(this.Value);
				return;
			}
		}

		public void Open() {
			string path = this.Value.ResourcePath;
			ResourceEditorResource resource = ResourceEditors.Instance.FindResource(path) ?? new ResourceEditorResource(path, async () => await DfFileManager.Instance.ReadAsync(path), false);
			ResourceEditors.Instance.OpenResource(resource);
		}
	}
}