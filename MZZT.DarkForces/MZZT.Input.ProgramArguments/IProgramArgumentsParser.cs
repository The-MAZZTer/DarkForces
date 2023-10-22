using System.Collections.Generic;

namespace MZZT.Input {
	public interface IProgramArgumentsParser {
		Dictionary<ProgramArgumentAttribute, object> Parse(Dictionary<ProgramArgumentAttribute, ProgramArgumentValueTypes> validArgs);
	}

	public enum ProgramArgumentValueTypes {
		None,
		Integer,
		Other
	}
}
