using System;
using UnityEngine;

namespace MZZT.Components {
	[Serializable]
	public class InspectorKeyValuePair<TKey, TValue> {
		[SerializeField]
		private TKey key = default;
		public TKey Key => this.key;
		[SerializeField]
		private TValue value = default;
		public TValue Value => this.value;
	}
}
