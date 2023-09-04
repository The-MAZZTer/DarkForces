using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using System.Collections.Generic;
using System.Linq;

namespace MZZT.DarkForces.Showcase {
	public class MsgList : DataboundList<KeyValuePair<int, DfMessages.Message>> {
		public void Add() {
			int next = this.Select(x => x.Key).Prepend(0).Max() + 1;

			((Dictionary<int, DfMessages.Message>)this.Value)[next] = new DfMessages.Message();

			this.GetComponentInParent<MsgViewer>().OnDirty();

			this.Invalidate();

			this.SelectedDatabound = this.Children.First(x => ((KeyValuePair<int, DfMessages.Message>)x.Value).Key == next);
		}

		public void InvalidateSelectedItem() {
			this.SelectedDatabound?.Invalidate();
		}
	}
}
