using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using System.Collections.Generic;

namespace MZZT.DarkForces.Showcase {
	public class MsgListItem : Databind<KeyValuePair<int, DfMessages.Message>> {
		public void Remove() {
			MsgList parent = (MsgList)this.Parent;
			Dictionary<int, DfMessages.Message> dictionary = (Dictionary<int, DfMessages.Message>)parent.Value;
			dictionary.Remove(this.Value.Key);

			MsgViewer viewer = (MsgViewer)parent.Parent;
			viewer.OnDirty();

			if (parent.Count == 0) {
				viewer.Details.gameObject.SetActive(false);
			}
			parent.Invalidate();
		}
	}
}
