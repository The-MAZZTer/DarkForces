using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;

namespace MZZT.Steam {
	public class SteamSubstitutionValue<T> : SteamValue<string> {
		public SteamSubstitutionValue() : base() { }

		public SteamSubstitutionValue(T value) : base() {
			this.DefaultValue = value?.ToString() ?? throw new ArgumentNullException(nameof(value));
		}

		public SteamSubstitutionValue(string value) : base() {
			this.DefaultValue = value ?? throw new ArgumentNullException(nameof(value));
		}

		public static Dictionary<string, string> StringMap { get; private set; } = new Dictionary<string, string>();

		protected static Regex FourNumbersRegex = new(@"^\s*(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s*$", RegexOptions.Compiled);

		public override string Value {
			get {
				string value = base.Value;

				while (StringMap.TryGetValue(value, out string newValue)) {
					value = newValue;
				}

				return value;
			}
		}

		public T SubstitutedValue {
			get {
				string value = this.Value;

				if (typeof(T).IsEnum) {
					string[] values = value.TrimEnd(';').Split(',');
					int intValue = 0;
					foreach (string stringFlag in values) {
						int flag;
						try {
							flag = (int)Enum.Parse(typeof(T), stringFlag);
						} catch (ArgumentException) {
							continue;
						}
						intValue |= flag;
					}
					return (T)Convert.ChangeType(intValue, typeof(T));
				} else if (typeof(T) == typeof(Color)) {
					Match match = FourNumbersRegex.Match(value);
					if (match == null || !match.Success) {
						return default;
					}
					int.TryParse(match.Groups[1].Value, out int r);
					int.TryParse(match.Groups[2].Value, out int g);
					int.TryParse(match.Groups[3].Value, out int b);
					int.TryParse(match.Groups[4].Value, out int a);
					return (T)Convert.ChangeType(Color.FromArgb(a, r, g, b), typeof(T));
				} else if (typeof(T) == typeof(Margin)) {
					Match match = FourNumbersRegex.Match(value);
					if (match == null || !match.Success) {
						return default;
					}
					int.TryParse(match.Groups[1].Value, out int left);
					int.TryParse(match.Groups[2].Value, out int top);
					int.TryParse(match.Groups[3].Value, out int right);
					int.TryParse(match.Groups[4].Value, out int bottom);
					return (T)Convert.ChangeType(new Margin() { Left = left, Top = top, Right = right, Bottom = bottom }, typeof(T));
				} else if (typeof(SteamRenderRule).IsAssignableFrom(typeof(T))) {
					return (T)(object)SteamRenderRule.Parse(value);
				}

				return (T)Convert.ChangeType(value, typeof(T));
			}
		}
	}
}