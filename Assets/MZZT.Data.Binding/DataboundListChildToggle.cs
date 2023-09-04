using UnityEngine;
using UnityEngine.UI;

namespace MZZT.Data.Binding {
	[RequireComponent(typeof(Databind))]
	public class DataboundListChildToggle : MonoBehaviour {
		public static Toggle FindToggleFor(IDatabind bind) {
			Component component = (Component)bind;
			if (component.TryGetComponent(out DataboundListChildToggle helper)) {
				return helper.Toggle;
			}
			return component.GetComponent<Toggle>();
		}

		[SerializeField]
		private Toggle toggle;
		public Toggle Toggle {
			get {
				if (this.toggle == null) {
					this.toggle = this.GetComponentInChildren<Toggle>(true);
				}
				return this.toggle;
			}
		}
	}
}