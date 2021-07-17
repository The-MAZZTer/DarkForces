using System.Collections.Generic;

namespace MZZT.Steam {
	public interface ISteamResource {
		object PreprocessConditions(IEnumerable<string> conditions);
	}
}