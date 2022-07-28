using MZZT.DataBinding;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MZZT.DarkForces.Showcase {
	public class DataboundRandomRange : Databound<RandomRange> {
		[SerializeField]
		private string memberName;

		private void Start() {
			IDataboundObject databound = this.GetComponentsInParent<IDataboundObject>(true).First(x => (object)x != this);
			databound.ValueChanged += this.Databound_ValueChanged;
			this.OnValueChanged();
		}

		private void Databound_ValueChanged(object sender, EventArgs e) {
			this.OnValueChanged();
		}

		private void OnValueChanged() {
			IDataboundObject databound = this.GetComponentsInParent<IDataboundObject>(true).First(x => (object)x != this);
			object value = databound.Value;
			if (value == null) {
				return;
			}

			MemberInfo member = value.GetType().GetMember(this.memberName).First();

			this.Value = (RandomRange)(member switch {
				FieldInfo field => field.GetValue(value),
				PropertyInfo property => property.GetValue(value),
				MethodInfo method => method.Invoke(value, Array.Empty<object>()),
				_ => throw new NotImplementedException()
			});
		}
	}
}
