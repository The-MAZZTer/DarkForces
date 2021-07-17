using System.Drawing;

namespace MZZT.Steam {
	[SteamName("gradient")]
	public class SteamGradientRenderRule : SteamRenderRule {
		public SteamGradientRenderRule(string left, string top, string right, string bottom,
			SteamSubstitutionValue<Color> startColor, SteamSubstitutionValue<Color> endColor) : base(left, top, right, bottom) {

			this.StartColor = startColor;
			this.EndColor = endColor;
		}

		public SteamSubstitutionValue<Color> StartColor { get; set; }

		public SteamSubstitutionValue<Color> EndColor { get; set; }
	}
}