using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DataBinding {
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

		private IDataboundObject obj;
		private IDataboundObject Object {
			get {
				if (this.obj == null) {
					this.obj = this.GetComponentInParent<IDataboundObject>();
				}
				return this.obj;
			}
		}

		private IDataboundUi[] ui;
		private IDataboundUi[] Ui {
			get {
				if (this.ui == null) {
					this.ui = this.Object.Ui.ToArray();
				}
				return this.ui;
			}
		}

		protected void Invalidate() {
			this.Button.interactable = this.Ui.Any(x => x.IsDirty);
		}

		protected abstract void PerformAction(IDataboundUi item);
		private void Start() {
			this.Button.onClick.AddListener(() => {
				if (!this.Button.interactable) {
					return;
				}

				IDataboundUi[] ui = this.Ui;
				foreach (IDataboundUi item in ui) {
					item.BeginUpdate();
				}
				try {
					foreach (IDataboundUi item in ui) {
						this.PerformAction(item);
					}
				} finally {
					foreach (IDataboundUi item in ui) {
						item.EndUpdate();
					}
				}
			});
		}

		private void OnEnable() {
			this.Object.ValueChanged += this.Object_ValueChanged;
			foreach (IDataboundUi item in this.Ui) {
				item.IsDirtyChanged += this.Item_IsDirtyChanged;
			}

			this.Invalidate();
		}

		private void OnDisable() {
			this.Object.ValueChanged -= this.Object_ValueChanged;
			foreach (IDataboundUi item in this.Ui) {
				item.IsDirtyChanged -= this.Item_IsDirtyChanged;
			}

		}

		private void Object_ValueChanged(object sender, EventArgs e) => this.Invalidate();
		private void Item_IsDirtyChanged(object sender, EventArgs e) => this.Invalidate();
	}
}
