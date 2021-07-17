using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MZZT.DataBinding {
	public class DataboundActiveIfMemberEqual : DataboundMember<string> {
		[SerializeField]
		private bool invert = false;
		[SerializeField]
		private bool isNull = true;
		[SerializeField]
		private string expected = null;

		protected override void OnInvalidate() {
			string value = this.Value;
			string expected = this.isNull ? null : this.expected;
			bool active = value == expected;
			if (this.invert) {
				active = !active;
			}
			this.gameObject.SetActive(active);
		}
	}
}
