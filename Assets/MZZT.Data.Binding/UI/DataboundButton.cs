using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.Data.Binding.UI {
	[RequireComponent(typeof(Button))]
	public class DataboundButton : MonoBehaviour {
		[SerializeField]
		private IDatabind databinder;
		public IDatabind Databinder {
			get {
				if (this.databinder == null) {
					this.databinder = this.GetComponentInParent<IDatabind>();
				}
				return this.databinder;
			}
		}

		private Button button;
		protected Button Button {
			get {
				if (this.button == null) {
					this.button = this.GetComponent<Button>();
				}
				return this.button;
			}
		}

		[SerializeField, DataboundMemberName]
		private string methodName = null;
		public string MethodName => this.methodName;
		private MethodInfo method;
		private MethodInfo Method {
			get {
				object obj = this.Databinder.Value;
				if (string.IsNullOrEmpty(this.methodName) || obj == null) {
					return null;
				}

				if (this.method == null) {
					Type objType = obj.GetType();
					this.method = objType.GetMethod(this.MethodName, new Type[] { }, new ParameterModifier[] { });
					if (this.method == null) {
						Debug.LogError($"{objType.Name} doesn't have method {this.MethodName} with no parameters!");
					}
				}
				return this.method;
			}
		}

		private void Start() {
			this.Button.onClick.AddListener(() => {
				if (!this.Button.interactable) {
					return;
				}
				this.Method.Invoke(this.Databinder.Value, new object[] { });
			});
		}
	}
}
