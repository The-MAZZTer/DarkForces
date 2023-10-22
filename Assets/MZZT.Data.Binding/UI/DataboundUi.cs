using System;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.Data.Binding.UI {
	public abstract class DataboundUi : MonoBehaviour {
		private Selectable selectable;
		protected Selectable Selectable {
			get {
				if (this.selectable == null) {
					this.selectable = this.GetComponent<Selectable>();
				}
				return this.selectable;
			}
		}

		[SerializeField]
		private Databind databinder;
		protected IDatabind Databinder {
			get {
				if (this.databinder == null) {
					this.databinder = this.GetComponent<Databind>();
				}
				return (IDatabind)this.databinder;
			}
		}

		private bool isDirty;
		public bool IsDirty {
			get => this.isDirty;
			protected set {
				if (this.isDirty == value) {
					return;
				}
				this.isDirty = value;

				this.IsDirtyChanged?.Invoke(this, new EventArgs());
			}
		}
		public event EventHandler IsDirtyChanged;

		protected abstract bool CheckIsDirty();

		protected virtual bool IsReadOnly => this.Selectable == null || !this.Selectable.interactable;

		public event EventHandler UserEnteredValueChanged;

		protected bool userInput = true;
		protected void OnUserEnteredValueChanged() {
			if (!this.userInput) {
				return;
			}

			this.IsDirty = this.CheckIsDirty();

			this.UserEnteredValueChanged?.Invoke(this, new EventArgs());

			if (!this.AutoApplyOnChange || this.IsReadOnly || !this.IsDirty) {
				return;
			}

			this.Apply();
		}

		[SerializeField]
		private bool autoApplyOnChange;
		public bool AutoApplyOnChange { get => this.autoApplyOnChange; set => this.autoApplyOnChange = value; }

		public abstract void Apply();
		public abstract void Revert();

		protected virtual void Start() {
			this.Databinder.Invalidated += this.Databinder_Invalidated;
			this.Invalidate();
		}

		protected virtual void OnDestroy() {
			this.Databinder.Invalidated -= this.Databinder_Invalidated;
		}

		private void Databinder_Invalidated(object sender, EventArgs e) {
			this.Invalidate();
		}

		protected abstract void Invalidate();


		protected int deferLevel = 0;
		protected bool invalidatePending = false;
		public void BeginUpdate() {
			this.deferLevel++;
		}

		public void EndUpdate() {
			if (--this.deferLevel == 0 && this.invalidatePending) {
				this.invalidatePending = false;
				this.Invalidate();
			}
		}
	}

	public abstract class DataboundUi<T> : DataboundUi {
		protected T Value {
			get {
				object ret = this.Databinder.Value;
				if (ret == null) {
					return default;
				}
				Type type = typeof(T);
				if (type == typeof(string) && ret != null) {
					ret = ret.ToString();
				} else if (type.IsEnum) {
					type = type.GetEnumUnderlyingType();
				}
				return (T)Convert.ChangeType(ret, type);
			}
			set => this.Databinder.Value = value;
		}

		protected override bool CheckIsDirty() {
			T value = this.Value;
			T user = this.UserEnteredValue;
			return (value == null || !value.Equals(user)) && (value != null || user != null);
		}

		protected abstract T UserEnteredValue { get; set; }

		public override void Apply() {
			if (!this.IsDirty) {
				return;
			}

			this.Value = this.UserEnteredValue;
			this.IsDirty = false;
			//this.Databinder.Invalidate();
		}

		public override void Revert() {
			if (!this.IsDirty) {
				return;
			}

			this.userInput = false;
			try {
				this.UserEnteredValue = this.Value;
			} finally {
				this.userInput = true;
			}
			this.IsDirty = false;
		}

		protected override void Invalidate() {
			if (this.deferLevel > 0) {
				this.invalidatePending = true;
				return;
			}

			if (this.IsDirty) {
				return;
			}

			if (this.Databinder == null) {
				return;
			}

			this.userInput = false;
			try {
				this.UserEnteredValue = this.Value;
			} finally {
				this.userInput = true;
			}
		}
	}
}
