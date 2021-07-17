using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MZZT.Steam {
	public class SteamValue<T> : ISteamValue {
		public SteamValue() { }

		public SteamValue(T value) {
			this.DefaultValue = value;
		}

		public T DefaultValue {
			get {
				Condition<T> condition = this.ConditionalValues.FirstOrDefault(x => x.ConditionText == null);
				if (condition == null) {
					return default;
				}
				return condition.Value;
			}
			set {
				if (Equals(this.DefaultValue, value)) {
					return;
				}

				Condition<T> condition = this.ConditionalValues.FirstOrDefault(x => x.ConditionText == null);
				if (condition == null) {
					condition = new Condition<T>(null);
					this.ConditionalValues.Add(condition);
				}
				condition.Value = value;
			}
		}

		public static HashSet<string> Conditions { get; private set; } = new HashSet<string>();

		public List<Condition<T>> ConditionalValues { get; private set; } = new List<Condition<T>>();

		private object Merge(IEnumerable<string> conditions, Type type, Array objects) {
			if (objects.Length == 0) {
				return null;
			} else if (objects.Length == 1) {
				return objects.Cast<object>().First();
			}

			if (typeof(IDictionary).IsAssignableFrom(type)) {
				IDictionary dest = Activator.CreateInstance(type) as IDictionary;

				foreach (object key in objects.Cast<IDictionary>().SelectMany(x => x.Keys as IEnumerable<object>).Distinct()) {
					object[] values = objects.Cast<IDictionary>().Select(x => x.Contains(key) ? x[key] : null).Where(x => x != null).ToArray();

					dest[key] = this.Merge(conditions, type.GetGenericArguments()[1], values);
				}

				return dest;
			} else if (typeof(Array).IsAssignableFrom(type)) {
				Array src = objects.Cast<Array>().Last();
				Array dest = Array.CreateInstance(type.GetElementType(), src.Length);

				for (int i = 0; i < src.Length; i++) {
					object obj = src.GetValue(i);
					if (obj == null) {
						dest.SetValue(null, i);
						continue;
					}

					type = obj.GetType();
					if (typeof(ISteamValue).IsAssignableFrom(type)) {
						dest.SetValue((obj as ISteamValue).PreprocessConditions(conditions), i);
					} else if (typeof(ISteamResource).IsAssignableFrom(type)) {
						dest.SetValue((obj as ISteamResource).PreprocessConditions(conditions), i);
					} else {
						dest.SetValue(obj, i);
					}
				}

				return dest;
			} else if (typeof(IList).IsAssignableFrom(type)) {
				IList src = objects.Cast<IList>().Last();
				IList dest = Activator.CreateInstance(type, src.Count) as IList;

				foreach (object obj in src) {
					if (obj == null) {
						dest.Add(null);
						continue;
					}

					type = obj.GetType();
					if (typeof(ISteamValue).IsAssignableFrom(type)) {
						dest.Add((obj as ISteamValue).PreprocessConditions(conditions));
					} else if (typeof(ISteamResource).IsAssignableFrom(type)) {
						dest.Add((obj as ISteamResource).PreprocessConditions(conditions));
					} else {
						dest.Add(obj);
					}
				}

				return dest;
			} else if (typeof(ISteamResource).IsAssignableFrom(type)) {
				ISteamResource dest = Activator.CreateInstance(type) as ISteamResource;

				foreach (MemberInfo member in type.GetMembers(BindingFlags.Instance | BindingFlags.Public)
					.Where(x => (x.MemberType & (MemberTypes.Field | MemberTypes.Property)) > 0)) {

					FieldInfo field = member as FieldInfo;
					PropertyInfo property = member as PropertyInfo;

					Type memberType = field?.DeclaringType ?? property?.DeclaringType;
					object[] values = objects.Cast<object>().Select(x => field?.GetValue(x) ?? property?.GetValue(x) ?? Activator.CreateInstance(memberType)).Where(x => x != null).ToArray();
					object value = this.Merge(conditions, memberType, values);

					field?.SetValue(dest, value);
					property?.SetValue(dest, value);
				}

				dest.PreprocessConditions(conditions);
				return dest;
			} else {
				return objects.Cast<object>().Last();
			}
		}

		public SteamValue<T> PreprocessConditions(IEnumerable<string> conditions) {
			SteamValue<T> ret = Activator.CreateInstance(this.GetType()) as SteamValue<T>;

			T[] objects = this.ConditionalValues.Where(x => x.Evaluate(conditions)).Select(x => x.Value).ToArray();

			Condition<T> final = new(null);
			ret.ConditionalValues.Add(final);

			Type type = typeof(T);
			final.Value = (T)this.Merge(conditions, type, objects);
			return ret;
		}
		object ISteamValue.PreprocessConditions(IEnumerable<string> conditions) => this.PreprocessConditions(conditions);

		public virtual T Value {
			get {
				T value = default;
				foreach (Condition<T> condition in this.ConditionalValues) {
					if (condition.Evaluate(Conditions)) {
						value = condition.Value;
						break;
					}
				}

				return value;
			}
		}

		IList ISteamValue.ConditionalValues => this.ConditionalValues;
	}
}