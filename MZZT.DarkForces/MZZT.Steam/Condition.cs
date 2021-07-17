using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace MZZT.Steam {
	public class Condition<T> : ICondition {
		public Condition(string condition) {
			this.ConditionText = condition;
		}

		private string conditionText;
		public string ConditionText {
			get => this.conditionText;
			set {
				if (this.conditionText == value) {
					return;
				}

				this.conditionText = value;
				this.func = null;
			}
		}
		private Func<IEnumerable<string>, bool> func;

		public Func<IEnumerable<string>, bool> Evaluate {
			get {
				if (this.func == null) {
					if (this.ConditionText == null) {
						this.func = new Func<IEnumerable<string>, bool>(x => true);
						return this.func;
					}

					ParameterExpression conditions = Expression.Parameter(typeof(IEnumerable<string>), "conditions");
					Expression state = null;

					int pos = 0;
					bool and = false;
					bool or = false;
					bool not = false;
					while (pos < this.ConditionText.Length) {
						if (char.IsWhiteSpace(this.ConditionText[pos])) {
							pos++;
						} else if (this.ConditionText[pos] == '!') {
							if (not) {
								throw new FormatException($"Invalid conditional token '{this.ConditionText[pos]}' at position {pos + 1}.");
							}
							not = true;
							pos++;
						} else if (this.ConditionText[pos] == '$') {
							pos++;
							int endPos = pos;
							while (endPos < this.ConditionText.Length &&
								this.ConditionText[endPos] != '!' &&
								this.ConditionText[endPos] != '&' &&
								this.ConditionText[endPos] != '|') {

								endPos++;
							}
							string name = this.ConditionText.Substring(pos, endPos - pos).TrimEnd();
							pos = endPos;

							Expression value = Expression.ArrayIndex(conditions, Expression.Constant(name));
							if (not) {
								value = Expression.Not(value);
								not = false;
							}
							if (state == null) {
								state = value;
							} else if (and) {
								state = Expression.And(state, value);
							} else if (or) {
								state = Expression.Or(state, value);
							} else {
								throw new FormatException($"Invalid conditional token '{this.ConditionText[pos]}' at position {pos + 1}.");
							}
						} else if (pos + 1 < this.ConditionText.Length && this.ConditionText.Substring(pos, 2) == "&&") {
							if (state == null || and || or || not) {
								throw new FormatException($"Invalid conditional token '{this.ConditionText[pos]}' at position {pos + 1}.");
							}
							and = true;
							pos += 2;
						} else if (pos + 1 < this.ConditionText.Length && this.ConditionText.Substring(pos, 2) == "||") {
							if (state == null || and || or || not) {
								throw new FormatException($"Invalid conditional token '{this.ConditionText[pos]}' at position {pos + 1}.");
							}
							or = true;
							pos += 2;
						} else {
							throw new FormatException($"Unknown conditional token '{this.ConditionText[pos]}' at position {pos + 1}.");
						}
					}
					if (state == null) {
						throw new FormatException($"Invalid empty conditional.");
					}
					this.func = Expression.Lambda<Func<IEnumerable<string>, bool>>(state, conditions).Compile();
				}
				return this.func;
			}
		}

		public T Value { get; set; }
		object ICondition.Value { get => this.Value; set => this.Value = (T)value; }
	}
}