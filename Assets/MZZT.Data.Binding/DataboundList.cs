using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace MZZT.Data.Binding {
	public class DataboundList<T> : Databind<ICollection<T>>, IList<T>, IList {
		protected override ICollection<T> CreateInstance() => new List<T>();

		[Header("List"), SerializeField]
		protected GameObject childTemplate = null;

		[SerializeField]
		private ToggleGroup toggleGroup = null;

		[SerializeField]
		private UnityEvent<int, IDatabind> itemAdded;

		[SerializeField]
		private UnityEvent<int, T> itemRemoved;

		public virtual ToggleGroup ToggleGroup {
			get => this.toggleGroup;
			set {
				if (this.toggleGroup == value) {
					return;
				}

				if (value == null && this.toggleGroup != null) {
					foreach (Toggle toggle in this.toggles.Values) {
						toggle.onValueChanged.RemoveListener(this.OnToggleValueChanged);
					}
				} else if (this.toggleGroup == null && value != null) {
					foreach (Toggle toggle in this.toggles.Values) {
						toggle.onValueChanged.AddListener(this.OnToggleValueChanged);
					}
				}

				this.toggleGroup = value;
			}
		}

		public IEnumerable<IDatabind> Children => this.transform.Cast<Transform>().Select(x => x.GetComponent<IDatabind>());
		public virtual T this[int index] {
			get {
				if (this.Value is IList<T> list) {
					return list[index];
				}
				return (T)this.GetDatabinder(index).Value;
			} 
			set {
				if (this.Value is not IList<T> list) {
					throw new InvalidOperationException();
				}
				
				int count = this.Count;
				if (index < 0 || index > count) {
					throw new IndexOutOfRangeException();
				} else if (index == count) {
					this.Insert(index, value);
				} else {
					list[index] = value;
					this.GetDatabinder(index).Value = value;
				}
			}
		}
		public IDatabind GetDatabinder(int index) => this.transform.GetChild(index).GetComponent<IDatabind>();
		public IDatabind GetDatabinderFromItem(T item) => this.Children
			.First(x => (item == null && x.Value == null) || (x.Value != null && x.Value.Equals(item)));

		public bool IsReadOnly => this.Value.IsReadOnly;
		public int Count => this.Value?.Count ?? 0;

		public int IndexOf(T item) {
			if (this.Value is IList<T> list) {
				return list.IndexOf(item);
			}

			int pos = this.Value.TakeWhile(x => (item == null && x == null) || (x != null && x.Equals(item))).Count();
			if (pos >= this.Count) {
				return -1;
			}
			return pos;
		}
		public int IndexOf(IDatabind item) {
			if (!this.Contains(item)) {
				return -1;
			}
			return ((Component)item).transform.GetSiblingIndex();
		}

		public bool Contains(T item) => this.Value.Contains(item);
		public bool Contains(IDatabind item) => item.Parent == (IDatabind)this;

		public void CopyTo(T[] array, int arrayIndex) => this.Value.CopyTo(array, arrayIndex);

		public virtual void Clear() {
			this.toggles.Clear();
			this.Value.Clear();
			foreach (GameObject child in this.transform.Cast<Transform>().Select(x => x.gameObject).ToArray()) {
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

		public bool Remove(IDatabind item) {
			int index = this.IndexOf(item);
			if (index < 0) {
				return false;
			}
			this.RemoveAt(index);
			return true;
		}

		public virtual void RemoveAt(int index) {
			if (this.Value is not IList<T> list) {
				throw new InvalidOperationException();
			}

			T value = list[index];

			list.RemoveAt(index);

			GameObject child = this.transform.GetChild(index).gameObject;

			IDatabind bind = child.GetComponent<IDatabind>();
			if (this.toggles.TryGetValue(bind, out Toggle toggle)) {
				this.toggles.Remove(bind);

				if (this.ToggleGroup != null) {
					if (toggle.isOn && this.Count == 0) {
						this.SelectedIndex = -1;
					}

					toggle.onValueChanged.RemoveListener(this.OnToggleValueChanged);
				}
			}

			DestroyImmediate(child);

			foreach ((IDatabind x, int i) in this.transform.Cast<Transform>().Select((x, i) => (x.GetComponent<IDatabind>(), i)).Skip(index)) {
				x.MemberName = i.ToString();
			}

			this.OnItemRemoved(index, value);
		}

		protected virtual void OnItemRemoved(int index, T value) {
			this.itemRemoved.Invoke(index, value);
		}

		private readonly Dictionary<IDatabind, Toggle> toggles = new();

		protected virtual IDatabind Instantiate(int index) {
			((Behaviour)this.childTemplate.GetComponent<IDatabind>()).enabled = false;
			GameObject gameObject = Instantiate(this.childTemplate);
			gameObject.transform.SetParent(this.transform, false);
			IDatabind ret = gameObject.GetComponent<IDatabind>();
			ret.ValueStorageMethod = DatabindingDefaultValueStorageMethods.BindToParentKey;
			ret.MemberName = index.ToString();

			Toggle toggle = DataboundListChildToggle.FindToggleFor(ret);
			if (toggle != null) {
				this.toggles[ret] = toggle;

				if (this.ToggleGroup != null) {
					toggle.isOn = !this.ToggleGroup.allowSwitchOff && this.toggles.Values.Any(x => x.isOn);
					toggle.group = this.ToggleGroup;

					toggle.onValueChanged.AddListener(this.OnToggleValueChanged);
					this.OnToggleValueChanged(toggle.isOn);
				}
			}

			((Behaviour)ret).enabled = true;
			return ret;
		}

		public void Add(T item) => this.Insert(this.Count, item);
		public virtual void Insert(int index, T item) {
			if (this.Value is not IList<T> list) {
				throw new InvalidOperationException();
			}

			if (index < 0 || index > this.Count) {
				throw new IndexOutOfRangeException();
			}

			list.Insert(index, item);
			IDatabind bind = this.Instantiate(index);

			if (index < this.Count - 1) {
				((Component)bind).gameObject.transform.SetSiblingIndex(index);

				foreach ((IDatabind x, int i) in this.Children.Select((x, i) => (x, i)).Skip(index + 1)) {
					x.MemberName = i.ToString();
				}
			}

			this.OnItemAdded(index, bind);
		}

		protected virtual void OnItemAdded(int index, IDatabind bind) {
			this.itemAdded.Invoke(index, bind);
		}

		public void SortBy(Func<T, IComparable> keySelector) {
			T[] sorted = this.Value.OrderBy(keySelector).ToArray();
			this.Value.Clear();
			foreach (T item in sorted) {
				this.Value.Add(item);
			}
			foreach ((IDatabind bind, int i) in this.Children
				.OrderBy(x => keySelector((T)x.Value))
				.Select((x, i) => (x, i))) {

				((Component)bind).transform.SetSiblingIndex(i);
				bind.MemberName = i.ToString();
			}
		}
		public void SortByDescending(Func<T, IComparable> keySelector) {
			T[] sorted = this.Value.OrderByDescending(keySelector).ToArray();
			this.Value.Clear();
			foreach (T item in sorted) {
				this.Value.Add(item);
			}
			foreach ((IDatabind bind, int i) in this.Children
				.OrderByDescending(x => keySelector((T)x.Value))
				.Select((x, i) => (x, i))) {

				((Component)bind).transform.SetSiblingIndex(i);
				bind.MemberName = i.ToString();
			}
		}

		public void SetIndex(IDatabind obj, int index) {
			if (this.Value is not IList<T> list) {
				throw new InvalidOperationException();
			}

			Transform transform = ((Component)obj).transform;
			if (transform.parent != this.transform) {
				throw new ArgumentException("That is not a child of this list!", nameof(obj));
			}

			if (index < 0 || index >= this.Count) {
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			T value = (T)obj.Value;
			int oldIndex = transform.GetSiblingIndex();

			list.RemoveAt(oldIndex);
			list.Insert(index, value);

			transform.SetSiblingIndex(index);
			foreach ((IDatabind x, int i) in this.Children.Select((x, i) => (x, i)).Skip(Math.Min(oldIndex, index))) {
				x.MemberName = i.ToString();
			}
		}
		public void SetIndexFromItem(T item, int index) => this.SetIndex(this.GetDatabinderFromItem(item), index);

		public virtual int SelectedIndex {
			get {
				if (this.ToggleGroup == null) {
					throw new InvalidOperationException();
				}

				(IDatabind bind, Toggle on) = this.toggles.FirstOrDefault(x => x.Value.isOn);
				if (on == null) {
					return -1;
				}

				return ((Component)bind).transform.GetSiblingIndex();
			}
			set {
				int old = this.SelectedIndex;
				if (old == value) {
					return;
				}

				bool allowSwitchOff = this.ToggleGroup.allowSwitchOff;
				try {
					this.ToggleGroup.allowSwitchOff = true;
					Toggle toggle = null;
					foreach ((IDatabind x, int i) in this.Children.Select((x, i) => (x, i))) {
						if (!this.toggles.TryGetValue(x, out Toggle current)) {
							continue;
						}
						current.isOn = false;
						if (i == value) {
							toggle = current;
						}
					}
					if (toggle != null) {
						toggle.isOn = true;
					}
				} finally {
					this.ToggleGroup.allowSwitchOff = allowSwitchOff;
				}
			}
		}

		public virtual IDatabind SelectedDatabound {
			get {
				if (this.ToggleGroup == null) {
					throw new InvalidOperationException();
				}

				KeyValuePair<IDatabind, Toggle>[] toggles = this.toggles.Where(x => x.Value.isOn).ToArray();
				if (toggles.Length == 0) {
					return null;
				}
				if (toggles.Length > 1) {
					toggles = toggles.Where(x => x.Value.isActiveAndEnabled).ToArray();
				}
				if (toggles.Length != 1) {
					return null;
				}
				return toggles[0].Key;
			}
			set {
				IDatabind old = this.SelectedDatabound;
				if (old == value) {
					return;
				}

				Toggle toggle = value != null ?  this.toggles.GetValueOrDefault(value) : null;
				Assert.AreEqual(value == null, toggle == null);
				bool allowSwitchOff = this.ToggleGroup.allowSwitchOff;
				try {
					this.ToggleGroup.allowSwitchOff = true;
					foreach (Toggle t in this.toggles.Values) {
						t.isOn = false;
					}
					if (toggle != null) {
#if DEBUG
						Assert.AreEqual(this.ToggleGroup, toggle.group);
#endif

						toggle.isOn = true;

#if DEBUG
						Assert.IsTrue(this.toggles.Values.Single(x => x.isOn) == toggle);
					} else {
						Assert.IsFalse(this.toggles.Values.Any(x => x.isOn));
#endif
					}
				} finally {
					this.ToggleGroup.allowSwitchOff = allowSwitchOff;
				}
			}
		}
		public T SelectedValue {
			get {
				IDatabind databound = this.SelectedDatabound;
				if (databound != null) {
					return (T)databound.Value;
				}
				return default;
			}
			set => this.SelectedDatabound = this.GetDatabinderFromItem(value);
		}

		public UnityEvent SelectedValueChanged = new();
		private IDatabind lastSelected;
		private void OnToggleValueChanged(bool value) {
			if (this.SelectedDatabound == this.lastSelected) {
				return;
			}
			this.lastSelected = this.SelectedDatabound;

			this.OnSelectedValueChanged();
		}

		protected virtual void OnSelectedValueChanged() {
			this.SelectedValueChanged.Invoke();
		}

		int IList.Add(object value) {
			this.Add((T)value);
			return 1;
		}
		void IList.Clear() => this.Clear();
		bool IList.Contains(object value) {
			if (value is T val) {
				return this.Contains(val);
			} else if (value is IDatabind item) {
				return this.Contains(item);
			} else {
				return false;
			}
		}
		int IList.IndexOf(object value) {
			if (value is T val) {
				return this.IndexOf(val);
			} else if (value is IDatabind item) {
				return this.IndexOf(item);
			} else {
				return -1;
			}
		}
		void IList.Insert(int index, object value) => this.Insert(index, (T)value);
		void IList.Remove(object value) {
			if (value is T val) {
				this.Remove(val);
			} else if (value is IDatabind item) {
				this.Remove(item);
			}
		}
		void IList.RemoveAt(int index) => this.RemoveAt(index);

		bool IList.IsFixedSize => this.Value is IList ilist && ilist.IsFixedSize;

		bool IList.IsReadOnly => this.IsReadOnly;

		object IList.this[int index] { get => this[index]; set => this[index] = (T)value; }

		void ICollection.CopyTo(Array array, int index) => this.CopyTo((T[])array, index);

		int ICollection.Count => this.Count;

		bool ICollection.IsSynchronized => this.Value is ICollection icollection && icollection.IsSynchronized;

		object ICollection.SyncRoot => this.Value is ICollection icollection ? icollection.SyncRoot : this;

		public IEnumerator<T> GetEnumerator() => this.Value.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

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

		protected override void OnInvalidate() {
			while (this.transform.childCount > this.Count) {
				DestroyImmediate(this.transform.GetChild(this.transform.childCount - 1).gameObject);
			}

			base.OnInvalidate();

			if (this.Value != null) {
				while (this.transform.childCount < this.Count) {
					this.Instantiate(this.transform.childCount);
				}
			}

			if (this.toggleGroup != null) {
				this.SelectedValueChanged.Invoke();
			}
		}
	}
}
