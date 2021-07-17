using MZZT.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace MZZT.Input {
	public static class ProgramArguments {
		public static string AppName { get; set; }

		public static object Inject<T>() {
			AppName = AppName ?? Path.GetFileNameWithoutExtension((Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()).ManifestModule.Name);
			return Inject(typeof(T));
		}

		public static bool Inject<T>(T obj) {
			AppName = AppName ?? Path.GetFileNameWithoutExtension((Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()).ManifestModule.Name);
			return Inject(typeof(T), obj);
		}

		public static bool InjectIntoStatic<T>() {
			AppName = AppName ?? Path.GetFileNameWithoutExtension((Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()).ManifestModule.Name);
			return Inject(typeof(T), null);
		}

		public static bool InjectIntoStatic(Type type) {
			AppName = AppName ?? Path.GetFileNameWithoutExtension((Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()).ManifestModule.Name);
			return Inject(type, null);
		}

		public static object Inject(Type type) {
			AppName = AppName ?? Path.GetFileNameWithoutExtension((Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()).ManifestModule.Name);
			object obj = Activator.CreateInstance(type);
			if (!Inject(type, obj)) {
				return null;
			}
			return obj;
		}

		private static readonly Regex numberRegex = new Regex(@"^\d+", RegexOptions.Compiled);

		public static bool Inject(Type type, object obj) {
			AppName = AppName ?? Path.GetFileNameWithoutExtension((Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()).ManifestModule.Name);
			(MemberInfo member, ProgramArgumentAttribute attribute)[] members = type.GetMembers()
				.Select(x => (x, x.GetCustomAttribute<ProgramArgumentAttribute>()))
				.Where(x => x.Item2 != null)
				.ToArray();
			Dictionary<string, (MemberInfo member, ProgramSwitchAttribute attribute)> longToMember = members
				.Where(x => (x.attribute is ProgramSwitchAttribute a) && (a.LongNames?.Length ?? 0) > 0)
				.SelectMany(x => ((ProgramSwitchAttribute)x.attribute).LongNames
					.Select(y => (y, x.member, (ProgramSwitchAttribute)x.attribute)))
				.ToDictionary(x => x.y, x => (x.member, x.Item3));
			Dictionary<char, (MemberInfo member, ProgramSwitchAttribute attribute)> shortToMember = members
				.Where(x => (x.attribute is ProgramSwitchAttribute a) && a.ShortFlag != default)
				.ToDictionary(x => ((ProgramSwitchAttribute)x.attribute).ShortFlag, x => (x.member, (ProgramSwitchAttribute)x.attribute));
			ProgramHelpInfoAttribute info = type.GetCustomAttribute<ProgramHelpInfoAttribute>();
			Queue<(MemberInfo member, ProgramArgumentAttribute attribute)> nonSwitches =
				new Queue<(MemberInfo member, ProgramArgumentAttribute attribute)>(members.Where(x => !(x.attribute is ProgramSwitchAttribute)));
			
			Dictionary<ProgramArgumentAttribute, MemberInfo> required = members.Where(x => x.attribute.Required).ToDictionary(x => x.attribute, x => x.member);

			string[] args = Environment.GetCommandLineArgs().Skip(1).ToArray();

			Dictionary<MemberInfo, object> pendingSets = new Dictionary<MemberInfo, object>();

			List<object> pendingExtras = new List<object>();

			bool ignoreOthers = false;
			bool matchSwitches = true;
			string parseError = null;
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

					if (!longToMember.TryGetValue(name, out (MemberInfo member, ProgramSwitchAttribute attribute) tuple)) {
						parseError = $"Unknown argument \"{name}\".";
						break;
					}

					(MemberInfo member, ProgramSwitchAttribute attribute) = tuple;
					
					Type valueType = GetArgType(member);
					if (valueType != null && !typeof(bool).Equals(valueType) && index < 0) {
						if (i + 1 < args.Length) {
							value = args[++i];
						} else {
							parseError = $"Missing value for argument \"{name}\".";
							break;
						}
					}

					object parsedValue;
					if (value == null && typeof(bool).Equals(valueType)) {
						parsedValue = true;
					} else {
						parsedValue = ConvertArgType(value, valueType);
					}

					if (attribute.IgnoreOtherArgs) {
						ignoreOthers = true;
						pendingSets.Clear();
						pendingSets[member] = parsedValue;
						break;
					}

					pendingSets[member] = parsedValue;
					required.Remove(attribute);
				} else if (matchSwitches && arg.StartsWith("-") && arg.Length > 1) {
					for (int j = 1; j < arg.Length; j++) {
						char key = arg[j];

						if (!shortToMember.TryGetValue(key, out (MemberInfo member, ProgramSwitchAttribute attribute) tuple)) {
							parseError = $"Unknown argument \"{key}\".";
							break;
						}

						(MemberInfo member, ProgramSwitchAttribute attribute) = tuple;

						Type valueType = GetArgType(member);
						string value = null;
						if (valueType != null && !typeof(bool).Equals(valueType)) {
							if (typeof(byte).Equals(valueType) ||
								typeof(sbyte).Equals(valueType) ||
								typeof(short).Equals(valueType) ||
								typeof(ushort).Equals(valueType) ||
								typeof(int).Equals(valueType) ||
								typeof(uint).Equals(valueType) ||
								typeof(long).Equals(valueType) ||
								typeof(ulong).Equals(valueType)) {

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
									parseError = $"Missing value for argument \"{key}\".";
									break;
								}
							}
						}

						object parsedValue = null;
						if (value == null && typeof(bool).Equals(valueType)) {
							parsedValue = true;
						} else if (valueType != null) {
							parsedValue = ConvertArgType(value, valueType);
						}

						if (attribute.IgnoreOtherArgs) {
							ignoreOthers = true;
							pendingSets.Clear();
							pendingSets[member] = parsedValue;
							break;
						}

						pendingSets[member] = parsedValue;
						required.Remove(attribute);
					}

					if (parseError != null || ignoreOthers) {
						break;
					}
				} else {
					if (nonSwitches.Count == 0) {
						parseError = $"Unknown argument \"{arg}\".";
						break;
					}

					(MemberInfo member, ProgramArgumentAttribute attribute) = nonSwitches.Peek();

					Type valueType = GetArgType(member);

					object parsedValue;
					if (nonSwitches.Count == 1) {
						Type nonSwitchValueType = GetArgType(member);
						if (typeof(IEnumerable).IsAssignableFrom(nonSwitchValueType) && (nonSwitchValueType.IsGenericType || nonSwitchValueType.IsArray)) {
							Type elementType = nonSwitchValueType.IsGenericType ? nonSwitchValueType.GetGenericArguments()[0] : nonSwitchValueType.GetElementType();
							parsedValue = ConvertArgType(arg, elementType);

							pendingExtras.Add(parsedValue);
							continue;
						}
					}

					parsedValue = ConvertArgType(arg, valueType);

					nonSwitches.Dequeue();

					pendingSets[member] = parsedValue;
					required.Remove(attribute);
				}
			}

			if (parseError == null && pendingExtras.Count > 0) {
				(MemberInfo member, ProgramArgumentAttribute attribute) = nonSwitches.Dequeue();

				Type nonSwitchValueType = GetArgType(member);
				Type elementType = nonSwitchValueType.IsGenericType ? nonSwitchValueType.GetGenericArguments()[0] : nonSwitchValueType.GetElementType();

				Array extras = Array.CreateInstance(elementType, pendingExtras.Count);
				foreach ((object extra, int i) in pendingExtras.Select((x, i) => (x, i))) {
					extras.SetValue(extra, i);
				}

				pendingSets[member] = extras;
				required.Remove(attribute);
			}

			if (parseError == null && !ignoreOthers && required.Count > 0) {
				(ProgramArgumentAttribute attribute, MemberInfo member) = required.First();
				string name;
				if (attribute is ProgramSwitchAttribute switchAttribute) {
					name = switchAttribute.LongNames.FirstOrDefault() ?? switchAttribute.ShortFlag.ToString();
				} else {
					name = GetArgName(attribute, member, false);
				}
				parseError = $"Required argument \"{name}\" not provided.";
			}

			if (parseError == null) {
				foreach ((MemberInfo member, object value) in pendingSets) {
					ParseResult result = SetValue(obj, member, value);
					if (result.SkipNormalErrorHandling) {
						return false;
					}
					parseError = result.Error;
					if (parseError != null) {
						break;
					}
				}
			}
			if (parseError == null) {
				(MethodInfo validate, ProgramValidateArgumentsAttribute validateAttribute)[] extraValidate = type.GetMethods()
					.Select(x => (x, x.GetCustomAttribute<ProgramValidateArgumentsAttribute>()))
					.Where(x => x.Item2 != null)
					.ToArray();
				if (extraValidate.Length > 1) {
					throw new InvalidOperationException($"Class \"{type.Name}\" has more than one member with the {nameof(ProgramValidateArgumentsAttribute)} attribute.");
				} else if (extraValidate.Length == 1) {
					object ret = extraValidate[0].validate.Invoke(obj, new object[] { });
					if (ret is ParseResult result) {
						if (result.SkipNormalErrorHandling) {
							return false;
						}
						parseError = result.Error;
					}
				}
			}

			ParseError = parseError;

			if (parseError != null) {
				if (info?.ShowHelpOnSyntaxError ?? true) {
					if (!string.IsNullOrWhiteSpace(parseError)) {
						Console.WriteLine(parseError);
					}

					ShowHelp(type);
				}
				return false;
			}

			return true;
		}

		public static string ParseError { get; private set; }

		private static Type GetArgType(MemberInfo member) {
			Type valueType;
			if (member is FieldInfo) {
				FieldInfo field = member as FieldInfo;
				valueType = field.FieldType;
			} else if (member is PropertyInfo) {
				PropertyInfo property = member as PropertyInfo;
				valueType = property.PropertyType;
			} else {
				MethodInfo method = member as MethodInfo;
				ParameterInfo[] parameters = method.GetParameters();
				if (parameters.Length > 1) {
					throw new ArgumentException(
						$"Method \"{member.Name}\" has too many parameters (expected zero or one) to be used in ProgramArguments.");
				}
				valueType = parameters.FirstOrDefault()?.ParameterType;
			}
			return valueType;
		}

		private static string GetArgName(ProgramArgumentAttribute attribute, MemberInfo member, bool showEnumOptions = true) {
			if (attribute.ValueName != null) {
				return attribute.ValueName;
			}

			if (showEnumOptions) {
				Type valueType = GetArgType(member);
				if (valueType.IsEnum) {
					return string.Join("|", Enum.GetNames(valueType));
				}
			}

			if (member is MethodInfo) {
				MethodInfo method = member as MethodInfo;
				ParameterInfo[] parameters = method.GetParameters();
				if (parameters.Length > 1) {
					throw new ArgumentException(
						$"Method \"{member.Name}\" has too many parameters (expected zero or one) to be used in ProgramArguments.");
				} else if (parameters.Length == 1) {
					return parameters[0].Name;
				}
			}
			return member.Name;
		}

		private static object ConvertArgType(string value, Type valueType) {
			if (valueType == null) {
				return value;
			}

			if (valueType.IsEnum) {
				return Enum.Parse(valueType, value, true);
			}

			return Convert.ChangeType(value, valueType);
		}

		private static ParseResult SetValue(object obj, MemberInfo member, object parsedValue) {
			if (member is FieldInfo) {
				FieldInfo field = member as FieldInfo;
				field.SetValue(obj, parsedValue);
			} else if (member is PropertyInfo) {
				PropertyInfo property = member as PropertyInfo;
				property.SetValue(obj, parsedValue);
			} else {
				MethodInfo method = member as MethodInfo;
				object ret;
				if (method.GetParameters().Length > 0) {
					ret = method.Invoke(obj, new object[] { parsedValue });
				} else {
					ret = method.Invoke(obj, new object[] { });
				}

				if (ret is ParseResult parseResult) {
					return parseResult;
				}
			}

			return default;
		}

		public static int HelpDescriptionIndent { get; set; } = 28;

		public static int OverrideConsoleWidth { get; set; }

		public static void ShowHelp<T>() {
			AppName = AppName ?? Path.GetFileNameWithoutExtension((Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()).ManifestModule.Name);
			ShowHelp(typeof(T));
		}

		public static string GetHelp<T>() {
			AppName = AppName ?? Path.GetFileNameWithoutExtension((Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()).ManifestModule.Name);
			return GetHelp(typeof(T));
		}

		private static string WrapText(int startX, int width, int leftMargin, string text) {
			int remaining;
			string[] lines = text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
			StringBuilder display = new StringBuilder(width * lines.Length);
			Regex getChunk;
			foreach (string line in lines) {
				string remainingLine = line;
				do {
					remaining = width - startX;
					if (remaining > width - leftMargin) {
						display.Append(new string(' ', leftMargin - (width - remaining)));
						remaining = width - leftMargin;
					}
					getChunk = new Regex($@"^(.{{1,{remaining}}})(\s+|$)");

					string chunk;
					Match match = getChunk.Match(remainingLine);
					if (match.Success) {
						chunk = match.Groups[1].Value;
						remainingLine = remainingLine.Substring(match.Length);
						remaining -= match.Groups[1].Length;
					} else {
						chunk = remainingLine.Substring(0, Math.Min(remainingLine.Length, remaining));
						remainingLine = remainingLine.Substring(Math.Min(remainingLine.Length, remaining));
						remaining -= chunk.Length;
					}
					display.Append(chunk);
					if (remaining > 0) {
						display.Append(Environment.NewLine);
					}
					startX = 0;
				} while (remainingLine.Length > 0);
			}
			return display.ToString();
		}

		public static string GetHelp(Type type) {
			AppName = AppName ?? Path.GetFileNameWithoutExtension((Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()).ManifestModule.Name);
			int width = OverrideConsoleWidth > 0 ? OverrideConsoleWidth : Console.WindowWidth;

			ProgramHelpInfoAttribute info = type.GetCustomAttribute<ProgramHelpInfoAttribute>();
			(MemberInfo member, ProgramArgumentAttribute attribute)[] members = type.GetMembers()
				.Select(x => (x, x.GetCustomAttribute<ProgramArgumentAttribute>()))
				.Where(x => x.Item2 != null && x.Item2.VisibleInHelp)
				.OrderBy(x => x.Item2.HelpOrder)
				.ToArray();

			(MemberInfo member, ProgramArgumentAttribute attribute)[] extras = members
				.Where(x => !(x.attribute is ProgramSwitchAttribute))
				.ToArray();
			StringBuilder str = new StringBuilder(width);
			str.Append($"Usage: {AppName}{(members.Length > 0 ? " [OPTIONS]" : "")}");
			if (extras.Any()) {
				str.Append(' ');
				str.Append(string.Join(" ", extras.Select(x => $"{(x.attribute.Required ? '{' : '[')}{GetArgName(x.attribute, x.member).ToUpper()}{(x.attribute.Required ? '}' : ']')}")));
			}

			StringBuilder output = new StringBuilder();
			output.Append(WrapText(0, width, 0, str.ToString()));

			if (info?.ProgramDescription != null) {
				output.Append(WrapText(0, width, 0, info.ProgramDescription));
			}
			if (info?.ExampleArguments != null) {
				output.Append(WrapText(0, width, 0, $"Example: {AppName} {info.ExampleArguments}"));
			}

			output.Append(Environment.NewLine);

			bool anyShort = members.Any(x => (x.attribute is ProgramSwitchAttribute switchAttribute) && switchAttribute.ShortFlag > 0);
			bool anyLong = members.Any(x => !(x.attribute is ProgramSwitchAttribute switchAttribute) || (switchAttribute.LongNames?.Length ?? 0) > 0);
			int longColumnsSize = HelpDescriptionIndent - 4 - (anyShort ? 4 : 0);

			foreach ((MemberInfo member, ProgramArgumentAttribute attribute, int i) in members.Select((x, i) => (x.member, x.attribute, i))) {
				if (attribute.PrependedGroupName != null) {
					if (i > 0) {
						output.Append(Environment.NewLine);
					}
					output.Append(WrapText(0, width, 0, attribute.PrependedGroupName));
				}

				StringBuilder display = new StringBuilder("  ", width);
				if (attribute is ProgramSwitchAttribute switchAttirbute) {
					if (switchAttirbute.ShortFlag > 0) {
						display.Append($"-{switchAttirbute.ShortFlag}");
						if (switchAttirbute.LongNames?.Length > 0) {
							display.Append(", ");
						}
					}
					if (switchAttirbute.LongNames?.Length > 0) {
						Type argType = GetArgType(member);
						foreach ((string longForm, int j) in switchAttirbute.LongNames.Select((x, j) => (x, j))) {
							if (j > 0) {
								display.Append(", ");
							}

							display.Append($"--{longForm}");

							if (argType != null && !typeof(bool).Equals(argType)) {
								display.Append($"={GetArgName(attribute, member).ToUpper()}");
							}
						}
					}
				} else {
					display.Append($"{(attribute.Required ? '{' : '[')}{GetArgName(attribute, member).ToUpper()}{(attribute.Required ? '}' : ']')}");
				}

				if (display.Length < HelpDescriptionIndent - 2) {
					display.Append(new string(' ', HelpDescriptionIndent - display.Length));
				} else {
					display.Append("  ");
				}
				display.Append(WrapText(display.Length % width, width, HelpDescriptionIndent, attribute.HelpDescription ?? ""));

				output.Append(display.ToString());
			}

			if (info?.HelpFooter != null) {
				if (members.Any()) {
					output.Append(Environment.NewLine);
				}

				output.Append(WrapText(0, width, 0, info.HelpFooter));
			}

			return output.ToString();
		}

		public static void ShowHelp(Type type) {
			AppName = AppName ?? Path.GetFileNameWithoutExtension((Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()).ManifestModule.Name);
			Console.Write(GetHelp(type));
		}
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property,
		Inherited = false, AllowMultiple = false)]
	public class ProgramSwitchAttribute : ProgramArgumentAttribute {
		public ProgramSwitchAttribute(string name = null, char flag = default) {
			if (name != null) {
				this.LongNames = new[] { name };
			}
			this.ShortFlag = flag;
		}
		public ProgramSwitchAttribute(char flag) {
			this.ShortFlag = flag;
		}

		public string[] LongNames { get; set; }

		public char ShortFlag { get; set; }

		public bool IgnoreOtherArgs { get; set; }
	}

	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public class ProgramHelpInfoAttribute : Attribute {
		public string ProgramDescription { get; set; }

		public string ExampleArguments { get; set; }

		public string HelpFooter { get; set; }

		public bool ShowHelpOnSyntaxError { get; set; } = true;
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property,
		Inherited = false, AllowMultiple = false)]
	public class ProgramArgumentAttribute : Attribute {
		public string ValueName { get; set; }

		public bool Required { get; set; }

		public string PrependedGroupName { get; set; }

		public int HelpOrder { get; set; }

		public string HelpDescription { get; set; }

		public bool VisibleInHelp { get; set; } = true;
	}

	[AttributeUsage(AttributeTargets.Method,
		Inherited = false, AllowMultiple = false)]
	public class ProgramValidateArgumentsAttribute : Attribute {
	}

	public struct ParseResult {
		public string Error;
		public bool SkipNormalErrorHandling;
	}
}
