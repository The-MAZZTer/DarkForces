using MZZT.FileFormats;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace MZZT.DarkForces.Showcase {
	public interface IResourceViewer {
		string TabName { get; }
		event EventHandler TabNameChanged;
		Sprite Thumbnail { get; }
		event EventHandler ThumbnailChanged;

		Task LoadAsync(ResourceEditorResource resource, IFile file);

		bool IsDirty { get; }
		event EventHandler IsDirtyChanged;
		void ResetDirty();
	}
}
