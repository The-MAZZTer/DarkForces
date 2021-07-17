using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace MZZT.DataBinding {
	public class DataboundListGroups<T> : DataboundList<ListGroup<T>> {
		public IEnumerable<DataboundListGroup<T>> GroupDatabinders => base.Databinders.Cast<DataboundListGroup<T>>();
		public IEnumerable<ListGroup<T>> Groups => this.Values;
		public IEnumerable<Databound<T>> ItemDatabinders => this.GroupDatabinders.SelectMany(x => x.DataboundList.Databinders);
		public IEnumerable<T> Items => this.GroupDatabinders.SelectMany(x => x.DataboundList.Values);

		private DataboundList<T> GetGroupList(ListGroup<T> group) => this.GroupDatabinders.First(x => x.Value == group).DataboundList;

		public override int Count => this.GroupDatabinders.Sum(x => x.DataboundList.Count);
		public bool Contains(T item) => this.GroupDatabinders.Any(x => x.DataboundList.Contains(item));
		public void CopyTo(T[] array, int arrayIndex) {
			foreach (T value in this.Items) {
				array[arrayIndex++] = value;
			}
		}

		public void Clear(ListGroup<T> group) => this.GetGroupList(group).Clear();
		public bool Remove(T item) => this.GroupDatabinders.Any(x => x.DataboundList.Remove(item));
		public void Add(T item, ListGroup<T> group) => this.GetGroupList(group).Add(item);

		public void SortBy(Func<T, IComparable> keySelector) {
			foreach (DataboundListGroup<T> group in this.GroupDatabinders) {
				group.DataboundList.SortBy(keySelector);
			}
		}
		public void SortByDescending(Func<T, IComparable> keySelector) {
			foreach (DataboundListGroup<T> group in this.GroupDatabinders) {
				group.DataboundList.SortByDescending(keySelector);
			}
		}

		protected override Databound<ListGroup<T>> Instantiate(ListGroup<T> item) {
			DataboundListGroup<T> bind = base.Instantiate(item) as DataboundListGroup<T>;
			bind.DataboundList.ToggleGroup = this.ToggleGroup;
			return bind;
		}

		public Databound<T> SelectedItemDatabound {
			get {
				if (this.ToggleGroup == null) {
					throw new InvalidOperationException();
				}

#pragma warning disable UNT0008 // Unity objects should not use null propagation
#pragma warning disable UNT0007 // Do not use null coalescing on Unity objects
				return this.ToggleGroup.ActiveToggles().FirstOrDefault()?.GetComponentsInParent<Databound<T>>(true).FirstOrDefault() ?? null;
#pragma warning restore UNT0007 // Do not use null coalescing on Unity objects
#pragma warning restore UNT0008 // Unity objects should not use null propagation
			}
			set {
				Databound<T> old = this.SelectedItemDatabound;
				if (old == value) {
					return;
				}

				Toggle toggle = value.GetComponentInChildren<Toggle>(true);
				Assert.IsNotNull(toggle);
				bool allowSwitchOff = this.ToggleGroup.allowSwitchOff;
				this.ToggleGroup.allowSwitchOff = true;
				foreach (Toggle t in this.ToggleGroup.ActiveToggles().ToArray()) {
					t.isOn = false;
				}
				toggle.isOn = true;
				this.ToggleGroup.allowSwitchOff = allowSwitchOff;
			}
		}
		public T SelectedItem {
			get {
				Databound<T> databound = this.SelectedItemDatabound;
				if (databound != null) {
					return databound.Value;
				}
				return default;
			}
			set => this.SelectedItemDatabound = this.ItemDatabinders
				.First(x => (value == null && x.Value == null) || (x.Value != null && x.Value.Equals(value)));
		}
	}

	public class ListGroup<T> : INotifyPropertyChanged {
		[SerializeField]
		private string caption;
		public string Caption {
			get => this.caption;
			set {
				if (this.caption == value) {
					return;
				}
				this.caption = value;

				this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Caption)));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}

	public class DataboundListGroup<T> : Databound<ListGroup<T>> {
		[SerializeField]
		private GameObject databoundList = null;
		public DataboundList<T> DataboundList {
			get => this.databoundList.GetComponentInChildren<DataboundList<T>>();
			set => this.databoundList = value.gameObject;
		}
	}
}
