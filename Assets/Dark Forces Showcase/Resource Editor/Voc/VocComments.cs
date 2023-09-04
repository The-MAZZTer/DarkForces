using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using System.Linq;

namespace MZZT.DarkForces.Showcase {
	public class VocComments : DataboundList<string> {
		public override void RemoveAt(int index) {
			this.GetDatabinder(index).ValueChanged -= this.VocComments_ValueChanged;

			base.RemoveAt(index);

			this.Sync();
		}

		public override void Insert(int index, string item) {
			base.Insert(index, item);

			this.GetDatabinder(index).ValueChanged += this.VocComments_ValueChanged;

			this.Sync();
		}

		protected override void OnBeforeValueChanged() {
			foreach (IDatabind bind in this.Children) {
				bind.ValueChanged -= this.VocComments_ValueChanged;
			}

			base.OnBeforeValueChanged();
		}

		protected override void OnValueChanged() {
			base.OnValueChanged();

			foreach (IDatabind bind in this.Children) {
				bind.ValueChanged += this.VocComments_ValueChanged;
			}
		}

		private void VocComments_ValueChanged(object sender, System.EventArgs e) {
			this.Sync();
		}

		private void Sync() {
			int boundary = this.GetComponentInParent<VocDetails>(true).BoundaryValue;
			CreativeVoice voc = this.GetComponentInParent<VocViewer>(true).Value;
			voc.Comments.RemoveAll(x => x.BeforeAudioDataIndex == boundary);
			voc.Comments.AddRange(this.Select(x => new CreativeVoice.Comment() {
				BeforeAudioDataIndex = boundary,
				Value = x
			}));

			VocViewer vocViewer = this.GetComponentInParent<VocViewer>(true);
			vocViewer.InvalidateBlockBar();
		}

		public void Add() {
			this.Add("");

			this.GetComponentInParent<VocViewer>().OnDirty();
		}
	}
}
