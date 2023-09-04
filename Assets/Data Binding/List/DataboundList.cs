using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MZZT.DataBinding {
	public class DataboundList<T> : MonoBehaviour, IList<T>, IList {
		[SerializeField]
		protected GameObject databinderTemplate = null;
		public GameObject DatabinderTemplate { get => this.databinderTemplate; set => this.databinderTemplate = value; }

		[SerializeField]
		private ToggleGroup toggleGroup = null;
		public ToggleGroup ToggleGroup { get => this.toggleGroup; set => this.toggleGroup = value; }

		public IEnumerable<Databound<T>> Databinders => this.transform.Cast<Transform>().Select(x => x.GetComponent<Databound<T>>());
		public IEnumerable<T> Values => this.transform.Cast<Transform>().Select(x => x.GetComponent<Databound<T>>().Value);
		public T this[int index] {
			get => this.GetDatabinder(index).Value;
			set {
				int count = this.Count;
				if (index > count) {
					throw new IndexOutOfRangeException();
				} else if (index == count) {
					this.Insert(index, value);
				} else {
					this.GetDatabinder(index).Value = value;
				}
			}
		}
		public Databound<T> GetDatabinder(int index) => this.transform.GetChild(index).GetComponent<Databound<T>>();
		public Databound<T> GetDatabinder(T item) => this.Databinders
			.First(x => (item == null && x.Value == null) || (x.Value != null && x.Value.Equals(item)));

		public bool IsReadOnly => false;
		public virtual int Count => this.transform.childCount;

		public int IndexOf(T item) {
			foreach ((T value, int index) in this.Values.Select((x, i) => (x, i))) {
				if ((value == null && item == null) || (value != null && value.Equals(item))) {
					return index;
				}
			}
			return -1;
		}
		public bool Contains(T item) => this.IndexOf(item) > -1;

		public void CopyTo(T[] array, int arrayIndex) {
			foreach (T value in this.Values) {
				array[arrayIndex++] = value;
			}
		}

		public virtual void Clear() {
			GameObject[] children = this.transform.Cast<Transform>().Select(x => x.gameObject).ToArray();
			foreach (GameObject child in children) {
				DestroyImmediate(child);
			}
		}
		public bool Remove(T item) {
			int index = this.IndexOf(item);
			if (index < 0) {
				return false;
			}
			this.RemoveAt(index);
			return true;
		}
		public virtual void RemoveAt(int index) {
			Databound<T> child = this.transform.GetChild(index).GetComponent<Databound<T>>();
			if (this.ToggleGroup != null) {
				Toggle toggle = child.Toggle;
				if (toggle != null) {
					toggle.onValueChanged.RemoveListener(this.OnToggleValueChanged);
				}
			}

			DestroyImmediate(child.gameObject);
		}

		protected virtual Databound<T> Instantiate(T item) {
			GameObject gameObject = Instantiate(this.databinderTemplate);
			gameObject.transform.SetParent(this.transform, false);
			Databound<T> ret = gameObject.GetComponent<Databound<T>>();
			ret.Value = item;

			if (this.ToggleGroup != null) {
				Toggle toggle = ret.Toggle;
				if (toggle == null) {
					ret.Toggle = toggle = ret.GetComponentInChildren<Toggle>(true);
				}
				if (toggle != null) {
					toggle.onValueChanged.AddListener(this.OnToggleValueChanged);
					toggle.isOn = !this.ToggleGroup.allowSwitchOff && !this.ToggleGroup
						.GetComponentsInChildren<Toggle>(true)
						.Any(x => x.group == this.toggleGroup && x.isOn);
					toggle.group = this.ToggleGroup;
					this.OnToggleValueChanged(toggle.isOn);
				}
			}

			return ret;
		}

		public void Add(T item) => this.Insert(this.Count, item);
		public virtual void Insert(int index, T item) {
			if (index > this.Count || index < 0) {
				throw new IndexOutOfRangeException();
			}

			Databound<T> bind = this.Instantiate(item);
			bind.gameObject.transform.SetSiblingIndex(index);
		}

		public void SortBy(Func<T, IComparable> keySelector) {
			foreach ((Databound<T> bind, int i) in this.Databinders
				.OrderBy(x => keySelector(x.Value))
				.Select((x, i) => (x, i))) {

				bind.transform.SetSiblingIndex(i);
			}
		}
		public void SortByDescending(Func<T, IComparable> keySelector) {
			foreach ((Databound<T> bind, int i) in this.Databinders
				.OrderByDescending(x => keySelector(x.Value))
				.Select((x, i) => (x, i))) {

				bind.transform.SetSiblingIndex(i);
			}
		}

		public void SetIndex(Databound<T> obj, int index) {
			if (index < 0 || index >= this.Count) {
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			obj.transform.SetSiblingIndex(index);
		}
		public void SetIndex(T item, int index) => this.SetIndex(this.GetDatabinder(item), index);
		
		public virtual Databound<T> SelectedDatabound {
			get {
				if (this.ToggleGroup == null) {
					throw new InvalidOperationException();
				}

				Toggle[] toggles = this.ToggleGroup.GetComponentsInChildren<Toggle>(true)
					.Where(x => x.group == this.toggleGroup && x.isOn).ToArray();
				if (toggles.Length == 0) {
					return null;
				}
				if (toggles.Length > 1) {
					toggles = toggles.Where(x => x.isActiveAndEnabled).ToArray();
				}
				if (toggles.Length != 1) {
					return null;
				}

				return toggles[0].GetComponentsInParent<Databound<T>>(true).FirstOrDefault();
			}
			set {
				Databound<T> old = this.SelectedDatabound;
				if (old == value) {
					return;
				}

				Toggle toggle = value != null ? value.Toggle : null;
				Assert.AreEqual(value == null, toggle == null);
				bool allowSwitchOff = this.ToggleGroup.allowSwitchOff;
				try {
					this.ToggleGroup.allowSwitchOff = true;
					foreach (Toggle t in this.ToggleGroup.GetComponentsInChildren<Toggle>(true)
						.Where(x => x.group == this.ToggleGroup)) {

						t.isOn = false;
					}
					if (toggle != null) {
#if DEBUG
						Assert.AreEqual(this.ToggleGroup, toggle.group);
#endif

						toggle.isOn = true;

#if DEBUG
						Assert.IsTrue(this.ToggleGroup.GetComponentsInChildren<Toggle>(true)
							.Single(x => x.group == this.toggleGroup && x.isOn) == toggle);
					} else {
						Assert.IsFalse(this.ToggleGroup.GetComponentsInChildren<Toggle>(true)
							.Any(x => x.group == this.toggleGroup && x.isOn));
#endif
					}
				} finally {
					this.ToggleGroup.allowSwitchOff = allowSwitchOff;
				}
			}
		}
		public T SelectedValue {
			get {
				Databound<T> databound = this.SelectedDatabound;
				if (databound != null) {
					return databound.Value;
				}
				return default;
			}
			set => this.SelectedDatabound = this.GetDatabinder(value);
		}

		public UnityEvent SelectedValueChanged = new();
		private Databound<T> lastSelected;
		private void OnToggleValueChanged(bool value) {
			if (this.SelectedDatabound == this.lastSelected) {
				return;
			}
			this.lastSelected = this.SelectedDatabound;

			this.SelectedValueChanged.Invoke();
		}

		int IList.Add(object value) {
			this.Add((T)value);
			return 1;
		}
		void IList.Clear() => this.Clear();
		bool IList.Contains(object value) => value is T t && this.Contains(t);
		int IList.IndexOf(object value) => value is T t ? this.IndexOf(t) : -1;
		void IList.Insert(int index, object value) => this.Insert(index, (T)value);
		void IList.Remove(object value) => this.Remove((T)value);
		void IList.RemoveAt(int index) => this.RemoveAt(index);

		bool IList.IsFixedSize => false;

		bool IList.IsReadOnly => this.IsReadOnly;

		object IList.this[int index] { get => this[index]; set => this[index] = (T)value; }

		void ICollection.CopyTo(Array array, int index) => this.CopyTo((T[])array, index);

		int ICollection.Count => this.Count;

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot => this;

		public IEnumerator<T> GetEnumerator() => new Enumerator(this);
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

		public class Enumerator : IEnumerator<T>, IEnumerator {
			private readonly DataboundList<T> list;
			private int pos = -1;

			public Enumerator(DataboundList<T> list) => this.list = list;

			public bool MoveNext() {
				this.pos++;
				return this.pos < this.list.transform.childCount;
			}

			public void Reset() => this.pos = -1;

			object IEnumerator.Current => this.Current;

			public T Current {
				get {
					try {
						return this.list.transform.GetChild(this.pos).GetComponent<Databound<T>>().Value;
					} catch (UnityException) {
						throw new InvalidOperationException();
					}
				}
			}

			public void Dispose() { }
		}

		public void AddRange(IEnumerable<T> items) {
			int pos = this.Count;
			foreach (T item in items) {
				this.Insert(pos++, item);
			}
		}

		public void RemoveRange(IEnumerable<T> items) {
			foreach (T item in items) {
				this.Remove(item);
			}
		}
	}
}
