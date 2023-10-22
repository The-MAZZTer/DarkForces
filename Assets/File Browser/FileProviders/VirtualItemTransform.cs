using MZZT.IO.FileSystemProviders;
using System.Collections.Generic;
using System.Linq;

namespace MZZT.IO.FileProviders {
	public abstract class VirtualItemTransform {
		public abstract bool ShouldOverride(FileSystemProviderItemInfo item);
		public abstract IVirtualItem CreateOverride(IFileSystemProvider provider, FileSystemProviderItemInfo item);
	}

	public class VirtualItemTransformHandler {
		private readonly List<VirtualItemTransform> transforms = new();
		public void AddTransform(VirtualItemTransform transform) => this.transforms.Add(transform);

		public IVirtualItem PerformTransform(IFileSystemProvider provider, FileSystemProviderItemInfo item) =>
			this.transforms.Where(x => x.ShouldOverride(item)).Select(x => x.CreateOverride(provider, item)).FirstOrDefault(x => x != null);
	}
}
