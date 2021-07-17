namespace MZZT.Steam {
	[SteamName("image")]
	public class SteamImageRenderRule : SteamRenderRule {
		public SteamImageRenderRule(string left, string top, string right, string bottom,
			SteamSubstitutionValue<string> path) : base(left, top, right, bottom) {

			this.Path = path;
		}

		public SteamSubstitutionValue<string> Path { get; set; }
	}
}