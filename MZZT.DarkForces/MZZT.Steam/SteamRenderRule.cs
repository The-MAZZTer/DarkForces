using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MZZT.Steam {
	public abstract class SteamRenderRule {
		private static readonly Regex parser = new(@"(\w+)\((.*?)\)", RegexOptions.Compiled);
		public static SteamRenderRule Parse(string text) {
			Match match = parser.Match(text);
			if (match == null || !match.Success) {
				throw new FormatException($"Invalid format of renderer call: {text}");
			}

			string funcName = match.Groups[1].Value;
			Type renderRule = Assembly.GetExecutingAssembly().GetTypes()
				.Select(x => (x, x.GetCustomAttribute<SteamNameAttribute>()?.Name))
				.Where(x => x.Name != null && string.Compare(x.Name, funcName, true) == 0)
				.Select(x => x.x)
				.FirstOrDefault();
			if (renderRule == null) {
				throw new FormatException($"Unknown render function {funcName} in renderer call: {text}");
			}

			ConstructorInfo constructor = renderRule.GetConstructors(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault();
			if (constructor == null) {
				throw new InvalidOperationException($"Can't find appropriate constructor for {renderRule.Name}!");
			}

			string[] args = match.Groups[2].Value.Split(',').Where(x => !string.IsNullOrEmpty(x)).ToArray();
			ParameterInfo[] parameters = constructor.GetParameters();

			if (args.Length != parameters.Length) {
				throw new FormatException($"Expected {parameters.Length} arguments for {renderRule.Name} constructor, got {args.Length}.");
			}

			List<object> constructorArgs = new();
			foreach ((string arg, ParameterInfo parameter) in args.Zip(parameters, (arg, param) => (arg, param))) {
				if (typeof(SteamValue<string>).IsAssignableFrom(parameter.ParameterType)) {
					SteamValue<string> value = Activator.CreateInstance(parameter.ParameterType) as SteamValue<string>;
					value.DefaultValue = arg;
					constructorArgs.Add(value);
				} else if (parameter.ParameterType == typeof(string)) {
					constructorArgs.Add(arg);
				}
			}

			return constructor.Invoke(constructorArgs.ToArray()) as SteamRenderRule;
		}

		private static Func<Rectangle, int> ParseBounds(string arg) {
			ParameterExpression container = Expression.Parameter(typeof(Rectangle), "container");
			Expression expr = null;

			int pos = 0;
			bool add = false;
			bool subtract = false;
			while (pos < arg.Length) {
				if (char.IsWhiteSpace(arg[pos])) {
					pos++;
				} else if (pos + 1 < arg.Length && (arg.Substring(pos, 2).ToLower() == "x0" || arg.Substring(pos, 2).ToLower() == "y0" ||
					arg.Substring(pos, 2).ToLower() == "x1" || arg.Substring(pos, 2).ToLower() == "y1")) {

					Expression value = container;
					switch (arg.Substring(pos, 2).ToLower()) {
						case "x0":
							value = Expression.Property(value, typeof(Rectangle).GetProperty(nameof(Rectangle.Left), BindingFlags.Instance | BindingFlags.Public));
							break;
						case "y0":
							value = Expression.Property(value, typeof(Rectangle).GetProperty(nameof(Rectangle.Top), BindingFlags.Instance | BindingFlags.Public));
							break;
						case "x1":
							value = Expression.Property(value, typeof(Rectangle).GetProperty(nameof(Rectangle.Right), BindingFlags.Instance | BindingFlags.Public));
							break;
						case "y1":
							value = Expression.Property(value, typeof(Rectangle).GetProperty(nameof(Rectangle.Bottom), BindingFlags.Instance | BindingFlags.Public));
							break;
					}
					if (expr == null) {
						expr = value;
					} else if (add) {
						expr = Expression.Add(expr, value);
					} else if (subtract) {
						expr = Expression.Subtract(expr, value);
					} else {
						throw new FormatException($"Invalid renderer token '{arg[pos]}' at position {pos + 1}.");
					}
					pos += 2;
				} else if (arg[pos] >= '0' && arg[pos] <= '9') {
					int endPos = pos;
					while (endPos < arg.Length && arg[pos] >= '0' && arg[pos] <= '9') {
						endPos++;
					}
					string number = arg.Substring(pos, endPos - pos).TrimEnd();
					pos = endPos;

					Expression value = Expression.Constant(int.Parse(number));
					if (expr == null) {
						expr = value;
					} else if (add) {
						expr = Expression.Add(expr, value);
					} else if (subtract) {
						expr = Expression.Subtract(expr, value);
					} else {
						throw new FormatException($"Invalid renderer token '{arg[pos]}' at position {pos + 1}.");
					}
				} else if (arg[pos] == '+') {
					if (expr == null || add || subtract) {
						throw new FormatException($"Invalid renderer token '{arg[pos]}' at position {pos + 1}.");
					}
					add = true;
					pos++;
				} else if (arg[pos] == '-') {
					if (expr == null || add || subtract) {
						throw new FormatException($"Invalid renderer token '{arg[pos]}' at position {pos + 1}.");
					}
					subtract = true;
					pos++;
				} else {
					throw new FormatException($"Unknown renderer token '{arg[pos]}' at position {pos + 1}.");
				}
			}
			if (expr == null) {
				throw new FormatException($"Invalid empty renderer argument.");
			}
			Func<Rectangle, int> func = Expression.Lambda<Func<Rectangle, int>>(expr, container).Compile();
			return func;
		}

		public SteamRenderRule(string leftExpr, string topExpr, string rightExpr, string bottomExpr) {
			this.LeftExpression = leftExpr;
			this.TopExpression = topExpr;
			this.RightExpression = rightExpr;
			this.BottomExpression = bottomExpr;
		}

		private string leftExpr;
		private Func<Rectangle, int> leftFunc;
		public string LeftExpression {
			get => this.leftExpr;
			set {
				if (this.leftExpr == value) {
					return;
				}

				this.leftExpr = value;
				if (value == null) {
					this.leftFunc = null;
				} else {
					this.leftFunc = ParseBounds(value);
				}
			}
		}

		private string topExpr;
		private Func<Rectangle, int> topFunc;
		public string TopExpression {
			get => this.topExpr;
			set {
				if (this.topExpr == value) {
					return;
				}

				this.topExpr = value;
				if (value == null) {
					this.topFunc = null;
				} else {
					this.topFunc = ParseBounds(value);
				}
			}
		}

		private string rightExpr;
		private Func<Rectangle, int> rightFunc;
		public string RightExpression {
			get => this.rightExpr;
			set {
				if (this.rightExpr == value) {
					return;
				}

				this.rightExpr = value;
				if (value == null) {
					this.rightFunc = null;
				} else {
					this.rightFunc = ParseBounds(value);
				}
			}
		}

		private string bottomExpr;
		private Func<Rectangle, int> bottomFunc;
		public string BottomExpression {
			get => this.bottomExpr;
			set {
				if (this.bottomExpr == value) {
					return;
				}

				this.bottomExpr = value;
				if (value == null) {
					this.bottomFunc = null;
				} else {
					this.bottomFunc = ParseBounds(value);
				}
			}
		}

		public Rectangle GetBounds(Rectangle container) {
			int left = this.leftFunc(container);
			int top = this.topFunc(container);
			int right = this.rightFunc(container);
			int bottom = this.bottomFunc(container);
			return new Rectangle(left, top, right - left, bottom - top);
		}
	}
}