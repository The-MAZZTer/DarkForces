using System;
using System.Collections.Generic;

namespace MZZT.Steam {
	public interface ICondition {
		string ConditionText { get; }
		Func<IEnumerable<string>, bool> Evaluate { get; }
		object Value { get; set; }
	}
}