using System;

namespace MZZT.Steam {
	[Flags]
	public enum SteamFontStyles {
		[SteamName]
		Normal,
		[SteamName("italic")]
		Italic = 1,
		[SteamName("uppercase")]
		Uppercase = 2,
		[SteamName("underline")]
		Underline = 4
	}
}