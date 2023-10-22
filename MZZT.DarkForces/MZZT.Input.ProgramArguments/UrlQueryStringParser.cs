using MZZT.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace MZZT.Input {
	public class UrlQueryStringParser : IProgramArgumentsParser {
		public UrlQueryStringParser(Uri uri) {
			this.query = new();
			this.extras = new();

			string query = uri.Query;
			if (string.IsNullOrEmpty(query)) {
				return;
			}

			foreach ((string key, string value) in query.TrimStart('?').Split('&').Select(x => {
				int index = x.IndexOf('=');
				if (index < 0) {
					return (null, WebUtility.UrlDecode(x));
				}
				return (WebUtility.UrlDecode(x.Substring(0, index)), WebUtility.UrlDecode(x.Substring(index + 1)));
			})) {
				if (key != null) {
					this.query[key] = value;
				} else if (key != "arg") {
					this.query[value] = "true";
				} else {
					this.extras.Enqueue(value);
				}
			}
		}

		private Dictionary<string, string> query;
		private Queue<string> extras;
		public Dictionary<ProgramArgumentAttribute, object> Parse(Dictionary<ProgramArgumentAttribute, ProgramArgumentValueTypes> validArgs) {
			Dictionary<string, ProgramArgumentAttribute> map = new();
			foreach (ProgramSwitchAttribute attribute in validArgs.Keys.OfType<ProgramSwitchAttribute>().Where(x => x.LongNames != null)) {
				foreach (string name in attribute.LongNames) {
					if (!map.ContainsKey(name)) {
						map[name] = attribute;
					}
				}
			}
			foreach (ProgramSwitchAttribute attribute in validArgs.Keys.OfType<ProgramSwitchAttribute>().Where(x => x.ShortFlag != default)) {
				string flag = attribute.ShortFlag.ToString();
				if (!map.ContainsKey(flag)) {
					map[flag] = attribute;
				}
			}

			Dictionary<ProgramArgumentAttribute, object> ret = new();
			foreach ((string key, string value) in this.query) {
				if (map.TryGetValue(key, out ProgramArgumentAttribute attribute)) {
					if (validArgs[attribute] != ProgramArgumentValueTypes.None && value == null) {
						throw new FormatException($"Missing value for argument \"{key}\".");
					}
					ret[attribute] = value;
				} else {
					throw new FormatException($"Unknown argument \"{key}\".");
				}
			}


			Queue<ProgramArgumentAttribute> nonSwitches = new(validArgs.Keys.Where(x => x is not ProgramSwitchAttribute));
			if (nonSwitches.Count == 0 && this.extras.Count > 0) {
				throw new($"Unknown argument \"{this.extras.First()}\".");
			}

			foreach (ProgramArgumentAttribute attribute in nonSwitches) {
				if (this.extras.Count == 0) {
					break;
				}

				if (nonSwitches.Count == 0) {
					ret[attribute] = this.extras.ToArray();
				} else {
					ret[attribute] = this.extras.Dequeue();
				}
			}
			return ret;
		}
	}
}
