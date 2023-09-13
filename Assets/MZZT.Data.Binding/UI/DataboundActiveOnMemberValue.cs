using System;
using System.Linq;
using UnityEngine;

namespace MZZT.Data.Binding.UI {
	public class DataboundActiveOnMemberValue : MonoBehaviour {
		[SerializeField]
		private Databind databinder;
		public IDatabind Databinder {
			get {
				if (this.databinder == null) {
					this.databinder = this.GetComponentInParent<Databind>();
				}
				return (IDatabind)this.databinder;
			}
		}

		[SerializeField]
		private bool isNot = false;
		[SerializeField]
		private string expectedValue = null;
		[SerializeField]
		private bool @null = false;
		[SerializeField]
		private GameObject target;

		private void Start() {
			this.Databinder.Invalidated += this.Databinder_Invalidated;
			this.Databinder_Invalidated(this.Databinder, new EventArgs());
		}

		private void OnDestroy() {
			if (this.Databinder != null) {
				this.Databinder.Invalidated -= this.Databinder_Invalidated;
			}
		}

		public bool ShouldBeActive {
			get {
				string value = this.Databinder?.Value?.ToString();
				string expected = this.@null ? null : this.expectedValue;
				bool active = value == expected;
				if (this.isNot) {
					active = !active;
				}
				return active;
			}
		}

		private void Databinder_Invalidated(object sender, EventArgs e) {
			this.target.SetActive(this.ShouldBeActive);
		}
	}
}
