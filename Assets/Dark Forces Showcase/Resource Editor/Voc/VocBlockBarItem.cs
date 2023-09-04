using MZZT.DarkForces.FileFormats;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
	public class VocBlockBarItem : MonoBehaviour {
		[SerializeField]
		private Toggle boundaryToggle;
		public Toggle BoundaryToggle => this.boundaryToggle;
		[SerializeField]
		private Toggle blockToggle;
		public Toggle BlockToggle => this.blockToggle;

		[SerializeField]
		private TextMeshProUGUI boundaryText;
		[SerializeField]
		private string boundaryEmpty;
		[SerializeField]
		private string boundaryFull;

		[SerializeField]
		private Color darkSilence;
		[SerializeField]
		private Color lightSilence;
		[SerializeField]
		private Color darkAudio;
		[SerializeField]
		private Color lightAudio;

		private int index = -1;
		public int Index {
			get => this.index;
			set {
				if (this.index == value) {
					return;
				}

				this.index = value;
				this.Invalidate();
			}
		}

		public void Invalidate() {
			CreativeVoice voc = this.GetComponentInParent<VocViewer>(true).Value;
			bool boundaryHasItems = voc.Comments.Any(x => x.BeforeAudioDataIndex == this.index) || voc.Markers.Any(x => x.BeforeAudioDataIndex == this.index);
			this.boundaryText.text = boundaryHasItems ? this.boundaryFull : this.boundaryEmpty;

			CreativeVoice.AudioData block = voc.AudioBlocks.ElementAtOrDefault(this.index);
			this.blockToggle.gameObject.SetActive(block != null);
			LayoutElement layout = this.GetComponent<LayoutElement>();
			if (block != null) {
				this.blockToggle.colors = new ColorBlock() {
					normalColor = block.Type == CreativeVoice.BlockTypes.Silence ? this.darkSilence : this.darkAudio,
					highlightedColor = block.Type == CreativeVoice.BlockTypes.Silence ? this.lightSilence : this.lightAudio,
					pressedColor = block.Type == CreativeVoice.BlockTypes.Silence ? this.lightSilence : this.lightAudio,
					selectedColor = block.Type == CreativeVoice.BlockTypes.Silence ? this.darkSilence : this.darkAudio,
					colorMultiplier = 1
				};
				int length = block.Type == CreativeVoice.BlockTypes.Silence ? block.SilenceLength : block.Data.Length;
				layout.flexibleWidth = length;
			} else {
				layout.flexibleWidth = 0;
			}
		}
	}
}
