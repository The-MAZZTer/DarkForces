using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DataBinding {
	[RequireComponent(typeof(Button))]
	public class DataboundButton : DataboundMember {
		private Button button;
		protected Button Button {
			get {
				if (this.button == null) {
					this.button = this.GetComponent<Button>();
				}
				return this.button;
			}
		}

		private void Start() {
			this.Button.onClick.AddListener(() => {
				if (!(this.Member is MethodInfo method)) {
					throw new NotSupportedException();
				}

				method.Invoke(this.DataboundObject.Value, new object[] { });
			});
		}
	}
}
