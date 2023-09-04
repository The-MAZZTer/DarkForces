using MZZT.Data.Binding;
using System.Collections.Generic;
using UnityEngine;

namespace MZZT.DarkForces.Showcase {
  public class ItemAwardList : DataboundList<ItemAward> {
		[SerializeField]
		private bool otherLevels;

		private bool populating = false;
		protected override void OnEnable() {
			base.OnEnable();

			this.populating = true;
			List<ItemAward> list = this.otherLevels ?
				Randomizer.Instance.Settings.Object.ItemAwardOtherLevels :
				Randomizer.Instance.Settings.Object.ItemAwardFirstLevel;
			this.Clear();
			this.AddRange(list);
			this.populating = false;
		}

		public override void Insert(int index, ItemAward item) {
			base.Insert(index, item);

			if (this.populating) {
				return;
			}

			List<ItemAward> list = this.otherLevels ?
				Randomizer.Instance.Settings.Object.ItemAwardOtherLevels :
				Randomizer.Instance.Settings.Object.ItemAwardFirstLevel;
			list.Insert(index, item);
		}

		public override void RemoveAt(int index) {
			base.RemoveAt(index);

			if (this.populating) {
				return;
			}

			List<ItemAward> list = this.otherLevels ?
				Randomizer.Instance.Settings.Object.ItemAwardOtherLevels :
				Randomizer.Instance.Settings.Object.ItemAwardFirstLevel;
			list.RemoveAt(index);
		}

		public void Add() {
			this.Add(new ItemAward());
		}

		public override void Clear() {
			base.Clear();

			if (this.populating) {
				return;
			}

			List<ItemAward> list = this.otherLevels ?
				Randomizer.Instance.Settings.Object.ItemAwardOtherLevels :
				Randomizer.Instance.Settings.Object.ItemAwardFirstLevel;
			list.Clear();
		}
	}
}
