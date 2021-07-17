using System.Collections.Generic;

namespace MZZT.Steam {
	public class SteamStyles : SteamResource<SteamStyles> {
		[SteamName("colors")]
		public SteamValue<Dictionary<string, SteamSubstitutionValue<string>>> Colors { get; set; } =
			new SteamValue<Dictionary<string, SteamSubstitutionValue<string>>>(new Dictionary<string, SteamSubstitutionValue<string>>());

		[SteamName("styles")]
		public SteamValue<Dictionary<string, SteamValue<SteamStyle>>> Styles { get; set; } =
			new SteamValue<Dictionary<string, SteamValue<SteamStyle>>>(new Dictionary<string, SteamValue<SteamStyle>>());
	}
}