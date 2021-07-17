using System.Drawing;

namespace MZZT.Steam {
	[SteamName("gradient_horizontal")]
	public class SteamHorizontalGradientRenderRule : SteamGradientRenderRule {
		public SteamHorizontalGradientRenderRule(string left, string top, string right, string bottom,
			SteamSubstitutionValue<Color> startColor, SteamSubstitutionValue<Color> endColor) : base(left, top, right, bottom, startColor, endColor) {

		}
	}
}