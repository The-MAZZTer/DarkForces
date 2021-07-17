using System.Drawing;

namespace MZZT.Steam {
	[SteamName("fill")]
	public class SteamFillRenderRule : SteamRenderRule {
		public SteamFillRenderRule(string left, string top, string right, string bottom,
			SteamSubstitutionValue<Color> color) : base(left, top, right, bottom) {

			this.Color = color;
		}

		public SteamSubstitutionValue<Color> Color { get; set; }
	}
}