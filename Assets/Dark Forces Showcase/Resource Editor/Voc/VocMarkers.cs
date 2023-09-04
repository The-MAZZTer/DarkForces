using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using System.Linq;

namespace MZZT.DarkForces.Showcase {
	public class VocMarkers : DataboundList<ushort> {
		public override void RemoveAt(int index) {
			this.GetDatabinder(index).ValueChanged -= this.VocMarkers_ValueChanged;

			base.RemoveAt(index);

			this.Sync();
		}

		public override void Insert(int index, ushort item) {
			base.Insert(index, item);

			this.GetDatabinder(index).ValueChanged += this.VocMarkers_ValueChanged;

			this.Sync();
		}

		protected override void OnBeforeValueChanged() {
			foreach (IDatabind bind in this.Children) {
				bind.ValueChanged -= this.VocMarkers_ValueChanged;
			}

			base.OnBeforeValueChanged();
		}

		protected override void OnValueChanged() {
			base.OnValueChanged();

			foreach (IDatabind bind in this.Children) {
				bind.ValueChanged += this.VocMarkers_ValueChanged;
			}
		}

		private void VocMarkers_ValueChanged(object sender, System.EventArgs e) {
			this.Sync();
		}

		private void Sync() {
			int boundary = this.GetComponentInParent<VocDetails>(true).BoundaryValue;
			CreativeVoice voc = this.GetComponentInParent<VocViewer>(true).Value;
			voc.Markers.RemoveAll(x => x.BeforeAudioDataIndex == boundary);
			voc.Markers.AddRange(this.Select(x => new CreativeVoice.MarkerData() {
				BeforeAudioDataIndex = boundary,
				Value = x
			}));

			VocViewer vocViewer = this.GetComponentInParent<VocViewer>(true);
			vocViewer.InvalidateBlockBar();
		}

		public void Add() {
			this.Add(0);

			this.GetComponentInParent<VocViewer>().OnDirty();
		}
	}
}
