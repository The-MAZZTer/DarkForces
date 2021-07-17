using MZZT.DataBinding;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;

namespace MZZT.DarkForces.Showcase {
	public class ModFileListItem : Databound<ModFile> {
		public override void Invalidate() {
			this.gameObject.name = this.Value?.DisplayName ?? "null";

			base.Invalidate();
		}

		public void OnModRemove() {
			this.GetComponentInParent<ModFileList>().Remove(this.Value);
		}
	}
}
