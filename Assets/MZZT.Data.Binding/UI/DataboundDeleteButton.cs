using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace MZZT.Data.Binding.UI {
	[RequireComponent(typeof(Button))]
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

		[SerializeField]
		private Databind obj;
		private IDatabind Object {
			get {
				if (this.obj == null) {
					this.obj = this.GetComponentInParent<Databind>();
				}
				return (IDatabind)this.obj;
			}
		}

		private void Start() {
			IList list = this.Object.Parent as IList;
			Assert.IsNotNull(list);

			if (list != null) {
				this.Button.onClick.AddListener(() => {
					if (!this.Button.interactable) {
						return;
					}

					//list.Remove(this.Object);
					list.RemoveAt(this.obj.transform.GetSiblingIndex());
				});
			}
		}
	}
}
