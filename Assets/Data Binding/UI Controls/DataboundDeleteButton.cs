using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DataBinding {
	public class DataboundDeleteButton : MonoBehaviour {
		private Button button;
		private Button Button {
			get {
				if (this.button == null) {
					this.button = this.GetComponent<Button>();
				}
				return this.button;
			}
		}

		private IDataboundObject obj;
		private IDataboundObject Object {
			get {
				if (this.obj == null) {
					this.obj = this.GetComponentInParent<IDataboundObject>();
				}
				return this.obj;
			}
		}

		private IList list;
		private IList List {
			get {
				if (this.list == null) {
					this.list = ((Component)(this.Object)).GetComponentInParent<IList>();
				}
				return this.list;
			}
		}

		private void Start() {
			this.Button.onClick.AddListener(() => {
				if (!this.Button.interactable) {
					return;
				}

				this.List.Remove(this.Object);
			});
		}
	}
}
