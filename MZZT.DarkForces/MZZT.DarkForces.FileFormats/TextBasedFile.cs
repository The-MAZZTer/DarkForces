using MZZT.FileFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MZZT.DarkForces.FileFormats {
	/// <summary>
	/// Helper functions for TextBasedFile<&lt;T&gt;.
	/// </summary>
	public static class TextBasedFile {
		/// <summary>
		/// Take an input line and split it by whitespace into words.
		/// </summary>
		/// <param name="line">The input line.</param>
		/// <param name="expectedFirstTokens">Throw an exception if the first tokens don't match this input. This should no longer be used in favor of more forgiving parsing.</param>
		/// <param name="count">Throw an exception if the number of tokens doesn't match this number. This should no longer be used in favor of more forgiving parsing.</param>
		/// <returns>An array of words.</returns>
		public static string[] TokenizeLine(string line, string expectedFirstTokens = null, int count = 0) {
			List<string> ret = new();
			int pos = 0;
			// Process the whole line.
			while (pos < line.Length) {
				// Look for a quotation mark, to allow embedding whitespace in tokens.
				int index = line.IndexOf('"', pos);
				if (index >= 0) {
					// Add all the tokens before the quotation mark before we process it.
					ret.AddRange(line.Substring(pos, index - pos).Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
					int end = line.IndexOf('"', index + 1);
					if (end < 0) {
						end = line.Length;
					}
					// Add the entire quoted string.
					ret.Add(line.Substring(index + 1, end - index - 1));
					pos = end + 1;
				} else {
					// No more quotes, add the rest of the tokens.
					ret.AddRange(line.Substring(pos).Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
					break;
				}
			}

			// We want to enforce the first few tokens are specific values.
			// I stopped using this code since it makes file parsing less forgiving.
			if (expectedFirstTokens != null) {
				// Tokenize that string.
				string[] tokens = expectedFirstTokens.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

				// Ensure the first few tokens match.
				if (ret.Count == 0) {
					throw new FormatException($"Expected \"{expectedFirstTokens}\", but reached end of stream!");
				}
				if (!ret.Take(tokens.Length).SequenceEqual(tokens)) {
					throw new FormatException($"Expected \"{expectedFirstTokens}\", found \"{string.Join(" ", ret.Take(tokens.Length))}\"!");
				}
			}
			// Ensure the number of tokens is what we are expecting.
			if (count > 0 && ret.Count < count) {
				throw new FormatException($"Expected {count} tokens, found {ret.Count}!");
			}

			return ret.ToArray();
		}

		/// <summary>
		/// Organizes data from a tokenized line using keys in the format "KEY:" and associating any values that follow it.
		/// </summary>
		/// <param name="line">The tokenized line as input.</param>
		/// <returns>A dictionary of keys along with the values following them on the line.</returns>
		public static Dictionary<string, string[]> SplitKeyValuePairs(string[] line) {
			Dictionary<string, string[]> ret = new();
			string currentKey = null;
			List<string> currentValue = new();
			for (int i = 0; i < line.Length; i++) {
				// Is the current token a key?
				int index = line[i].IndexOf(":");
				if (index >= 0) {
					if (currentKey != null) {
						ret[currentKey] = currentValue.ToArray();
					}

					currentKey = line[i].Substring(0, index).ToUpper();
					currentValue.Clear();
					if (index < line[i].Length - 1) {
						currentValue.Add(line[i].Substring(index + 1));
					}
					continue;
				}

				// If not, it's a value, add to the current key's entry.
				if (currentKey != null) {
					currentValue.Add(line[i]);
				}
			}
			if (currentKey != null) {
				ret[currentKey] = currentValue.ToArray();
			}

			return ret;
		}
	}

	/// <summary>
	/// Represents the base class for all text-based DF file formats.
	/// </summary>
	/// <typeparam name="T">A concrete subclass type.</typeparam>
	public abstract class TextBasedFile<T> : DfFile<T> where T : File<T>, new() {
		protected override void AddWarning(string warning, int line = 0) {
			base.AddWarning(warning, line > 0 ? line : this.CurrentLine);
		}

		protected int CurrentLine { get; private set; }
		protected void ResetCurrentLine() {
			this.CurrentLine = 0;
		}
		protected void IncrementCurrentLine() {
			this.CurrentLine++;
		}

		private bool inComment = false;

		private bool eof = false;
		protected async Task<string[]> ReadTokenizedLineAsync(StreamReader reader, string expectedFirstTokens = null, int count = 0) {
			// Remember if we hit the end of file before and abort.
			if (this.eof) {
				return null;
			}

			// Read a line that's not empty or has an end-of-file marker in it.
			string line = await this.ReadLineAsync(reader);
			while (line != null && line.Trim() != "\x1A" && line.Trim() == "") {
				line = await this.ReadLineAsync(reader);

				// If we find an end-of-file marker remember it.
				int index = line?.IndexOf("\x1A") ?? -1;
				if (index >= 0) {
					this.eof = true;
					line = line.Substring(0, index);
				}
			}

			if (line == null || line.Trim() == "\x1A" || line.Trim() == "") {
				return null;
			}

			return TextBasedFile.TokenizeLine(line, expectedFirstTokens, count);
		}

		protected async Task<string> ReadLineAsync(StreamReader reader) {
			string line = await reader.ReadLineAsync();
			if (line != null) {
				this.CurrentLine++;
			}

			// Skip any comments in the file.
			// Only /* */ are multiline so we remember when we were reading one of those.
			int end;
			if (this.inComment) {
				end = line?.IndexOf("*/") ?? -1;
				while (end < 0 && line != null) {
					line = await reader.ReadLineAsync();
					if (line != null) {
						this.CurrentLine++;
					}
					end = line?.IndexOf("*/") ?? -1;
				}
				line = line?.Substring(end + 2);
				this.inComment = false;
			}

			if (line == null) {
				return null;
			}

			int pos = 0;
			while (true) {
				// Look for characters of interest.
				int index = line.IndexOfAny(new[] { '"', '/', '#' }, pos);
				if (index < 0) {
					return line;
				}
				char c = line[index];
				// If we find a quote, don't check anything within the quoted string for comments.
				if (c == '"') {
					end = line.IndexOf('"', index + 1);
					if (end < 0) {
						return line;
					}
					pos = end + 1;
				} else if (c == '#') {
					// Remove anything from the # after.
					return line.Substring(0, index);
				} else if (line.Length <= index + 1) {
					// // and /* are two characters so if there's only one character it can't be a comment.
					return line;
				} else if (line.Substring(index, 2) == "//") {
					// Remove anything from the // after.
					return line.Substring(0, index);
				} else if (line.Substring(index, 2) == "/*") {
					// Find the end of the /* */ comment. If we can't, it's multiline so remember that.
					end = line.IndexOf("*/", index + 2);
					if (end < 0) {
						this.inComment = true;
						return line.Substring(0, index);
					}

					line = line.Substring(0, index) + line.Substring(end + 2);
					pos = index;
				} else {
					// False alarm, keep looking.
					pos = index + 1;
				}
			}
		}

		protected async Task WriteLineAsync(StreamWriter writer, FormattableString format) {
			// Check the arguments to our FormattableString.
			foreach (string str in format.GetArguments().Where(x => x is char || x is string).Select(x => x.ToString().Trim())) {
				if (string.IsNullOrWhiteSpace(str)) {
					throw new FormatException("Can't write an empty string!");
				}
				if (str.IndexOfAny(new[] { '\r', '\n' }) >= 0) {
					throw new FormatException("Tried to write a field containing newlines, this is not permitted!");
				}
				if (str.EndsWith(":")) {
					throw new FormatException("Tried to write a field ending with :, this is not permitted!");
				}

				bool quoted = str.Length >= 2 && str[0] == '"' && str[str.Length - 1] == '"';
				if (!quoted && str.IndexOfAny(new[] { ' ', '\t'}) >= 0) {
					throw new FormatException("Tried to write a field with whitespace in it, this is not permitted!");
				}
				int index = str.IndexOf('"', 1);
				if (quoted) {
					if (index < str.Length - 1) {
						throw new FormatException("Tried to write a field with a \" in it, this is not permitted!");
					}
				} else {
					if (index >= 0) {
						throw new FormatException("Tried to write a field with a \" in it, this is not permitted!");
					}
				}
			}

			await writer.WriteLineAsync(format.ToString());
			this.CurrentLine++;
			return;
		}

		protected string Unescape(string value) {
			bool quoted = value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"';
			if (!quoted) {
				this.AddWarning("Value is not escaped.");
				return value;
			}

			return value.Substring(1, value.Length - 2).Replace("\\n", "\n");
		}

		protected string Escape(string value) {
			bool quoted = value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"';
			if (quoted) {
				this.AddWarning("Value is escaped.");
				return value;
			}

			return $"\"{value.Replace("\n", "\\n")}\"";
		}

		public override async Task LoadAsync(Stream stream) {
			this.ResetCurrentLine();

			await base.LoadAsync(stream);
		}

		public override async Task SaveAsync(Stream stream) {
			this.ResetCurrentLine();

			await base.SaveAsync(stream);
		}
	}
}
