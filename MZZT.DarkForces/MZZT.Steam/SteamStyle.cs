using System.Drawing;

namespace MZZT.Steam {
	public class SteamStyle : SteamResource<SteamStyle> {
		[SteamName("bgcolor")]
		public SteamSubstitutionValue<Color> BackgroundColor { get; set; } = null;

		[SteamName("corner_rounding")]
		public SteamSubstitutionValue<int> CornerRounding { get; set; } = null;

		[SteamName("render_bg")]
		public SteamValue<SteamSubstitutionValue<SteamRenderRule>[]> BackgroundRenderRules { get; set; } = new SteamValue<SteamSubstitutionValue<SteamRenderRule>[]>(new SteamSubstitutionValue<SteamRenderRule>[] { });

		[SteamName("font-family")]
		public SteamSubstitutionValue<string> FontFamily { get; set; } = null;

		[SteamName("font-size")]
		public SteamSubstitutionValue<int> FontSize { get; set; } = null;

		[SteamName("font-style")]
		public SteamSubstitutionValue<SteamFontStyles> FontStyle { get; set; } = null;

		[SteamName("font-weight")]
		public SteamSubstitutionValue<int> FontWeight { get; set; } = null;

		[SteamName("image")]
		public SteamSubstitutionValue<string> Image { get; set; } = null;

		[SteamName("inset")]
		public SteamSubstitutionValue<Margin> Inset { get; set; } = null;

		[SteamName("inset-bottom")]
		public SteamSubstitutionValue<int> InsetBottom { get; set; } = null;

		[SteamName("inset-left")]
		public SteamSubstitutionValue<int> InsetLeft { get; set; } = null;

		[SteamName("inset-right")]
		public SteamSubstitutionValue<int> InsetRight { get; set; } = null;

		[SteamName("inset-top")]
		public SteamSubstitutionValue<int> InsetTop { get; set; } = null;

		[SteamName("minimum-width")]
		public SteamSubstitutionValue<int> MinimumWidth { get; set; } = null;

		[SteamName("padding")]
		public SteamSubstitutionValue<Margin> Padding { get; set; } = null;

		[SteamName("padding-bottom")]
		public SteamSubstitutionValue<int> PaddingBottom { get; set; } = null;

		[SteamName("padding-left")]
		public SteamSubstitutionValue<int> PaddingLeft { get; set; } = null;

		[SteamName("padding-right")]
		public SteamSubstitutionValue<int> PaddingRight { get; set; } = null;

		[SteamName("padding-top")]
		public SteamSubstitutionValue<int> PaddingTop { get; set; } = null;

		[SteamName("render")]
		public SteamValue<SteamSubstitutionValue<SteamRenderRule>[]> RenderRules { get; set; } = new SteamValue<SteamSubstitutionValue<SteamRenderRule>[]>(new SteamSubstitutionValue<SteamRenderRule>[] { });

		[SteamName("selectedbgcolor")]
		public SteamSubstitutionValue<Color> SelectedBackgroundColor { get; set; } = null;

		[SteamName("selectedtextcolor")]
		public SteamSubstitutionValue<Color> SelectedTextColor { get; set; } = null;

		[SteamName("shadowtextcolor")]
		public SteamSubstitutionValue<Color> ShadowTextColor { get; set; } = null;

		[SteamName("textcolor")]
		public SteamSubstitutionValue<Color> TextColor { get; set; } = null;
	}
}