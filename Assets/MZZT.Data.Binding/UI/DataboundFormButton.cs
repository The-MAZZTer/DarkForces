using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.Data.Binding.UI {
	[RequireComponent(typeof(Button))]
	public abstract class DataboundFormButton : MonoBehaviour {
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
		private Transform form;
		private Transform Form {
			get {
				if (this.form == null) {
					this.form = ((Component)this.GetComponentInParent<IDatabind>()).transform;
				}
				return this.form;
			}
		}

		private DataboundUi[] FormElements => this.Form.GetComponentsInChildren<DataboundUi>();

		protected void Invalidate() {
			this.Button.interactable = this.FormElements.Any(x => x.IsDirty);
		}

		protected abstract void PerformAction(DataboundUi item);
		private void Start() {
			this.Button.onClick.AddListener(() => {
				if (!this.Button.interactable) {
					return;
				}

				DataboundUi[] elements = this.FormElements;
				foreach (DataboundUi element in elements) {
					element.BeginUpdate();
				}
				try {
					foreach (DataboundUi element in elements) {
						this.PerformAction(element);
					}
				} finally {
					foreach (DataboundUi element in elements) {
						element.EndUpdate();
					}
				}
			});
		}

		private void OnEnable() {
			foreach (DataboundUi element in this.FormElements) {
				element.IsDirtyChanged += this.Item_IsDirtyChanged;
			}

			this.Invalidate();
		}

		private void OnDisable() {
			foreach (DataboundUi element in this.FormElements) {
				element.IsDirtyChanged -= this.Item_IsDirtyChanged;
			}
		}

		private void Item_IsDirtyChanged(object sender, EventArgs e) => this.Invalidate();
	}
}
