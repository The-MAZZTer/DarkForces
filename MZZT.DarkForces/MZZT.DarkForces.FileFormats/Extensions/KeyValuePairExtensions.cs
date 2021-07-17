using System.Collections.Generic;

namespace MZZT.Extensions {
	public static class KeyValuePairExtensions {
		public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> me, out TKey key, out TValue value) {
			key = me.Key;
			value = me.Value;
		}
	}
}
