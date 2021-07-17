using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DataBinding {
	public interface IDataboundObject {
		object Value { get; set; }
		void Invalidate();
		IEnumerable<IDataboundUi> Ui { get; }
		event EventHandler BeforeValueChanged;
		event EventHandler ValueChanged;
	}

	public class Databound<T> : MonoBehaviour, IDataboundObject {
		private void Start() => this.Invalidate();

		[SerializeField]
		private T value = default;
		public T Value {
			get => this.value;
			set {
				if ((this.value != null && this.value.Equals(value)) || (this.value == null && value == null)) {
					return;
				}

				this.BeforeValueChanged?.Invoke(this, new EventArgs());

				this.value = value;

				this.ValueChanged?.Invoke(this, new EventArgs());

				this.Invalidate();
			}
		}
		object IDataboundObject.Value { get => this.Value; set => this.Value = (T)value; }
		public event EventHandler BeforeValueChanged;
		public event EventHandler ValueChanged;

		[SerializeField]
		private Toggle toggle = null;
		public Toggle Toggle { get => this.toggle; set => this.toggle = value; }

		public virtual void Invalidate() {
			this.gameObject.name = this.value?.ToString() ?? "<No Value>";
		}

		public IEnumerable<IDataboundUi> Ui => this.GetComponentsInChildren<IDataboundUi>();
	}
}
