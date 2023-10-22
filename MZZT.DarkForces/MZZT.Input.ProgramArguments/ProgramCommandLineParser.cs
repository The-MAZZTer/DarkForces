using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MZZT.Input {
	public class ProgramCommandLineParser : IProgramArgumentsParser {
		private static readonly Regex numberRegex = new(@"^\d+", RegexOptions.Compiled);

		public Dictionary<ProgramArgumentAttribute, object> Parse(Dictionary<ProgramArgumentAttribute, ProgramArgumentValueTypes> validArgs) {
			string[] args = Environment.GetCommandLineArgs().Skip(1).ToArray();

			Dictionary<string, ProgramSwitchAttribute> longToMember = validArgs.Keys.OfType<ProgramSwitchAttribute>().Where(x =>
				x.LongNames?.Length > 0).SelectMany(x => x.LongNames.Select(y => (x, y))).ToDictionary(x => x.y, x => x.x);
			Dictionary<char, ProgramSwitchAttribute> shortToMember = validArgs.Keys.OfType<ProgramSwitchAttribute>().Where(x =>
				x.ShortFlag != default).ToDictionary(x => x.ShortFlag);
			Queue<ProgramArgumentAttribute> nonSwitches = new(validArgs.Keys.Where(x => x is not ProgramSwitchAttribute));

			Dictionary<ProgramArgumentAttribute, object> ret = new();
			List<string> pendingExtras = new();

			bool matchSwitches = true;
			for (int i = 0; i < args.Length; i++) {
				string arg = args[i];
				if (matchSwitches && arg.StartsWith("--")) {
					if (arg == "--") {
						matchSwitches = false;
						continue;
					}

					string name = arg.Substring(2);
					string value = null;
					int index = name.IndexOf('=');
					if (index >= 0) {
						value = name.Substring(index + 1);
						if (value.StartsWith("\"")) {
							value = value.Substring(1);
							if (value.EndsWith("\"")) {
								value = value.Substring(0, value.Length - 1);
							}
						}
						name = name.Substring(0, index);
					}
					
					if (!longToMember.TryGetValue(name, out ProgramSwitchAttribute attribute)) {
						throw new FormatException($"Unknown argument \"{name}\".");
					}

					if (validArgs[attribute] == ProgramArgumentValueTypes.None) {
						value = "true";
					} else if (index < 0) {
						if (i + 1 < args.Length) {
							value = args[++i];
						} else {
							throw new FormatException($"Missing value for argument \"{name}\".");
						}
					}

					if (attribute.IgnoreOtherArgs) {
						ret.Clear();
						ret[attribute] = value;
						break;
					}

					ret[attribute] = value;
				} else if (matchSwitches && arg.StartsWith("-") && arg.Length > 1) {
					for (int j = 1; j < arg.Length; j++) {
						char key = arg[j];

						if (!shortToMember.TryGetValue(key, out ProgramSwitchAttribute attribute)) {
							throw new FormatException("Unknown argument \"{key}\".");
						}

						string value = null;
						if (validArgs[attribute] != ProgramArgumentValueTypes.None) {
							if (validArgs[attribute] == ProgramArgumentValueTypes.Integer) {
								Match match = numberRegex.Match(arg.Substring(j + 1));
								if (match.Success) {
									j += match.Length;
									value = match.Value;
								}
							}

							if (value == null) {
								if (i + 1 < args.Length) {
									value = args[++i];
								} else {
									throw new FormatException($"Missing value for argument \"{key}\".");
								}
							}
						} else {
							value = "true";
						}


						if (attribute.IgnoreOtherArgs) {
							ret.Clear();
							ret[attribute] = value;
							break;
						}

						ret[attribute] = value;
					}
				} else {
					if (nonSwitches.Count == 0) {
						throw new($"Unknown argument \"{arg}\".");
					}

					ProgramArgumentAttribute attribute = nonSwitches.Peek();

					if (nonSwitches.Count == 1) {
						pendingExtras.Add(arg);
						continue;
					}

					ret[attribute] = arg;
					nonSwitches.Dequeue();
				}
			}

			if (pendingExtras.Count > 0) {
				ProgramArgumentAttribute attribute = nonSwitches.Dequeue();
				ret[attribute] = pendingExtras.ToArray();
			}

			return ret;
		}
	}
}
