using System.Collections;
using System.Collections.Generic;

namespace MZZT.Steam {
	public interface ISteamValue {
		IList ConditionalValues { get; }
		object PreprocessConditions(IEnumerable<string> conditions);
	}
}