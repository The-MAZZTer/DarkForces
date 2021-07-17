using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MZZT.Steam {
	public abstract class SteamResource<T> : ISteamResource {
		public T PreprocessConditions(IEnumerable<string> conditions) {
			Type type = typeof(T);
			T ret = Activator.CreateInstance<T>();

			foreach (MemberInfo member in type.GetMembers(BindingFlags.Instance | BindingFlags.Public)
				.Where(x => (x.MemberType & (MemberTypes.Field | MemberTypes.Property)) > 0)) {

				FieldInfo field = member as FieldInfo;
				PropertyInfo property = member as PropertyInfo;

				Type memberType = field?.DeclaringType ?? property?.DeclaringType;
				object value = field?.GetValue(this) ?? property?.GetValue(this) ?? Activator.CreateInstance(memberType);
				if (value is ISteamValue steamValue) {
					value = steamValue.PreprocessConditions(conditions);
				} else if (value is ISteamResource steamResource) {
					value = steamResource.PreprocessConditions(conditions);
				}
				field?.SetValue(ret, value);
				property?.SetValue(ret, value);
			}

			return ret;
		}

		object ISteamResource.PreprocessConditions(IEnumerable<string> conditions) => this.PreprocessConditions(conditions);
	}
}