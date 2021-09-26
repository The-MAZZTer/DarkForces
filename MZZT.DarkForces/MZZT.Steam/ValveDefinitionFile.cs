using MZZT.FileFormats;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MZZT.Steam {
	public class ValveDefinitionFile : File<ValveDefinitionFile> {
		public override bool CanLoad => true;

		public override Task LoadAsync(Stream stream) {
			this.Tokens.Clear();
			this.Tokens.AddRange(this.Tokenize(stream).Where(x => !(x is CommentToken)));

			if (this.Tokens.FirstOrDefault() is not StringToken nameToken ||
				this.Tokens.ElementAtOrDefault(1) is not StartChildToken ||
				this.Tokens.LastOrDefault() is not EndChildToken) {

				throw new FormatException("Expected name of file followed by children in resource file.");
			}

			this.RootName = nameToken.Text;
			return Task.CompletedTask;
		}

		public T Detokenize<T>() {
			return this.Parse<T>(this.Tokens, 2, this.Tokens.Count - 2);
		}

		private Token GetToken(List<Token> tokens, int pos, int end) {
			if (pos > end) {
				if (pos > tokens.Count - 1) {
					throw new FormatException($"Unexpected end of stream.");
				} else {
					throw new FormatException($"Unexpected end of block.");
				}
			}

			return tokens[pos];
		}

		private string ParseKey(List<Token> tokens, int pos, int end) {
			Token token = this.GetToken(tokens, pos, end);
			if (token is not StringToken strToken) {
				throw new FormatException($"Unexpected token \"{token}\".");
			}
			string propertyName = strToken.Text;
			if (string.IsNullOrWhiteSpace(propertyName)) {
				throw new FormatException($"Unexpected empty string.");
			}
			return propertyName;
		}

		private string ParseUntilValue(List<Token> tokens, ref int pos, int end) {
			Token token = this.GetToken(tokens, ++pos, end);
			string conditionText = null;
			if (token is ConditionToken conditionToken) {
				conditionText = conditionToken.ConditionText;

				token = this.GetToken(tokens, ++pos, end);
			}

			if (token is AssignmentToken) {
				token = this.GetToken(tokens, ++pos, end);
			}

			if (token is ConditionToken) {
				if (conditionText != null) {
					throw new FormatException($"Unexpected extra condition token \"{token}\".");
				}
				conditionToken = token as ConditionToken;
				conditionText = conditionToken.ConditionText;
			}
			return conditionText;
		}

		private object ParseValue(Type memberType, out Type realType, List<Token> tokens, ref int pos, int end, ref string conditionText) {
			Token token = this.GetToken(tokens, pos, end);

			object value;
			if (token is StartChildToken) {
				int childStart = pos + 1;
				int childEnd = pos;
				int indents = 1;
				for (pos = childStart; pos <= end; pos++) {
					if (tokens[pos] is EndChildToken) {
						indents--;
						if (indents == 0) {
							childEnd = pos - 1;
							break;
						}
					} else if (tokens[pos] is StartChildToken) {
						indents++;
					}
				}

				realType = memberType.GetGenericArguments()[0];
				if (typeof(IDictionary).IsAssignableFrom(realType) || typeof(Array).IsAssignableFrom(realType) || typeof(IList).IsAssignableFrom(realType)) {
					value = this.ParseCollection(realType, tokens, childStart, childEnd);
				} else {
					value = this.Parse(realType, tokens, childStart, childEnd);
				}
				pos = childEnd + 2;
			} else if (token is StringToken) {
				realType = typeof(string);
				value = (token as StringToken).Text;
				pos++;

				if (pos <= end && tokens[pos] is ConditionToken) {
					if (conditionText != null) {
						throw new FormatException($"Unexpected extra condition token \"{token}\".");
					}
					ConditionToken conditionToken = tokens[pos] as ConditionToken;
					conditionText = conditionToken.ConditionText;

					pos++;
				}
			} else {
				throw new FormatException($"Unexpected token \"{token}\".");
			}
			return value;
		}

		private ICollection ParseCollection(Type type, List<Token> tokens, int start, int end) {
			Type valueType;
			if (typeof(IDictionary).IsAssignableFrom(type)) {
				valueType = type.GetGenericArguments()[1];
			} else if (typeof(Array).IsAssignableFrom(type)) {
				valueType = type.GetElementType();
			} else {
				valueType = type.GetGenericArguments()[0];
			}
			if (!typeof(ISteamValue).IsAssignableFrom(valueType)) {
				throw new FormatException($"Collection won't accept values inheriting from ISteamValue.");
			}

			Dictionary<string, ISteamValue> collection = new();

			int pos = start;
			while (pos <= end) {
				string propertyName = this.ParseKey(tokens, pos, end);

				if (!collection.TryGetValue(propertyName, out ISteamValue steamValue)) {
					steamValue = Activator.CreateInstance(valueType) as ISteamValue;
					collection[propertyName] = steamValue;
				}

				string conditionText = this.ParseUntilValue(tokens, ref pos, end);

				object value = this.ParseValue(valueType, out Type realType, tokens, ref pos, end, ref conditionText);

				ICondition condition = steamValue.ConditionalValues.Cast<ICondition>().FirstOrDefault(x => x.ConditionText == conditionText);
				if (condition == null) {
					condition = Activator.CreateInstance(typeof(Condition<>).MakeGenericType(realType), conditionText) as ICondition;
					steamValue.ConditionalValues.Add(condition);
				}
				condition.Value = value;
			}

			if (typeof(IDictionary).IsAssignableFrom(type)) {
				IDictionary ret = Activator.CreateInstance(type) as IDictionary;
				Type keyType = type.GetGenericArguments()[0];
				foreach (KeyValuePair<string, ISteamValue> item in collection) {
					ret[Convert.ChangeType(item.Key, keyType)] = item.Value;
				}
				return ret;
			} else if (typeof(Array).IsAssignableFrom(type)) {
				Array ret = Array.CreateInstance(valueType, collection.Count);
				foreach ((ISteamValue value, int index) in collection.OrderBy(x => int.Parse(x.Key)).Select((x, i) => (x.Value, i))) {
					ret.SetValue(value, index);
				}
				return ret;
			} else {
				IList ret = Activator.CreateInstance(type) as IList;
				foreach (ISteamValue value in collection.OrderBy(x => int.Parse(x.Key)).Select(x => x.Value)) {
					ret.Add(value);
				}
				return ret;
			}
		}

		private object Parse(Type type, List<Token> tokens, int start, int end) {
			object ret = Activator.CreateInstance(type);

			int pos = start;
			while (pos <= end) {
				string propertyName = this.ParseKey(tokens, pos, end);
				MemberInfo member = type.GetMembers(BindingFlags.Public | BindingFlags.Instance)
					.FirstOrDefault(x => (x.MemberType & (MemberTypes.Field | MemberTypes.Property)) > 0 &&
					(string.Compare(x.GetCustomAttribute<SteamNameAttribute>()?.Name, propertyName, true) == 0 ||
					string.Compare(x.Name, propertyName, true) == 0));
				if (member == null) {
					throw new FormatException($"\"{propertyName}\" is not a member of {type.Name}.");
				}
				FieldInfo field = member as FieldInfo;
				PropertyInfo property = member as PropertyInfo;
				Type memberType = property?.PropertyType ?? field?.FieldType;
				if (!typeof(ISteamValue).IsAssignableFrom(memberType)) {
					throw new FormatException($"Can't assign to member \"{member.Name}\" of type \"{type.Name}\" as the member is not a SteamValue type.");
				}

				if ((property?.GetValue(ret) ?? field?.GetValue(ret)) is not ISteamValue steamValue) {
					steamValue = Activator.CreateInstance(memberType) as ISteamValue;
					property?.SetValue(ret, steamValue);
					field?.SetValue(ret, steamValue);
				}

				string conditionText = this.ParseUntilValue(tokens, ref pos, end);

				object value = this.ParseValue(memberType, out Type realType, tokens, ref pos, end, ref conditionText);

				ICondition condition = steamValue.ConditionalValues.Cast<ICondition>().FirstOrDefault(x => x.ConditionText == conditionText);
				if (condition == null) {
					condition = Activator.CreateInstance(typeof(Condition<>).MakeGenericType(realType), conditionText) as ICondition;
					steamValue.ConditionalValues.Add(condition);
				}
				condition.Value = value;
			}
			return ret;
		}

		private TType Parse<TType>(List<Token> tokens, int start, int end) {
			return (TType)this.Parse(typeof(TType), tokens, start, end);
		}

		public string RootName { get; set; }

		public List<Token> Tokens { get; } = new();

		private IEnumerable<Token> Tokenize(Stream stream) {
			using StreamReader reader = new(stream); int peek;
			char next;
			while ((peek = reader.Peek()) >= 0) {
				next = (char)peek;

				Token token;
				if (peek <= 32 || char.IsWhiteSpace(next)) {
					reader.Read();
					continue;
				} else if (tokenMap.TryGetValue(next, out Type tokenType)) {
					token = Activator.CreateInstance(tokenType, reader) as Token;
				} else {
					token = new StringToken(reader);
				}
				if (token.UnexpectedEndOfToken) {
					throw new EndOfStreamException($"No terminator found for {token.GetType().Name}.");
				}
				yield return token;
			}
		}

		public abstract class Token {
			public Token(StreamReader reader, bool consume) {
				if (consume) {
					reader.Read();
				}
			}

			public bool UnexpectedEndOfToken { get; protected set; }
		}

		public class StringToken : Token {
			public StringToken(StreamReader reader) : base(reader, false) {
				StringBuilder builder = new();
				int next;
				char first = (char)reader.Read();
				if (first == '\"') {
					while ((next = reader.Read()) >= 0 && next != '"') {
						char character = (char)next;
						if (character == '\\') {
							character = (char)reader.Read();
						}

						builder.Append(character);
					}
				} else {
					builder.Append(first);

					while ((next = reader.Peek()) > 32 && !tokenMap.ContainsKey((char)next)) {
						builder.Append((char)next);
						reader.Read();
					}
				}
				this.UnexpectedEndOfToken = next == 0;
				this.Text = builder.ToString();
			}

			public string Text { get; private set; }

			public override string ToString() => this.Text;
		}

		public class StartChildToken : Token {
			public StartChildToken(StreamReader reader) : base(reader, true) {
			}

			public override string ToString() => "{";
		}

		public class EndChildToken : Token {
			public EndChildToken(StreamReader reader) : base(reader, true) {
			}

			public override string ToString() => "}";
		}

		public class ConditionToken : Token {
			public ConditionToken(StreamReader reader) : base(reader, true) {
				int next;
				StringBuilder builder = new();
				while ((next = reader.Read()) >= 0 && next != ']') {
					builder.Append((char)next);
				}
				this.UnexpectedEndOfToken = next == 0;
				this.ConditionText = builder.ToString();
			}

			public string ConditionText { get; private set; }

			public override string ToString() => $"[{this.ConditionText}]";
		}

		public class AssignmentToken : Token {
			public AssignmentToken(StreamReader reader) : base(reader, true) {
			}

			public override string ToString() => "=";
		}

		public class CommentToken : Token {
			public CommentToken(StreamReader reader) : base(reader, true) {
				int next;
				while ((next = reader.Read()) >= 0 && (char)next != '\r' && (char)next != '\n') { }
			}

			public override string ToString() => "//";
		}

		private static readonly Dictionary<char, Type> tokenMap = new() {
			['"'] = typeof(StringToken),
			['{'] = typeof(StartChildToken),
			['}'] = typeof(EndChildToken),
			['['] = typeof(ConditionToken),
			['='] = typeof(AssignmentToken),
			['/'] = typeof(CommentToken)
		};
	}
}