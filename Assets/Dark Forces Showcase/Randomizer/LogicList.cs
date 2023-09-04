using MZZT.Data.Binding;
using System;
using System.Linq;
using UnityEngine;

namespace MZZT.DarkForces.Showcase {

	public class LogicList : DataboundList<string> {
		[SerializeField]
		private bool items;

		protected override void Start() {
			this.AddRange(this.items ? RandomizerUi.ITEM_LOGICS : RandomizerUi.ENEMY_LOGICS);

			foreach (IDatabind item in this.Children) {
				DataboundListChildToggle.FindToggleFor(item).onValueChanged.AddListener(value => this.SaveLogics());
			}
		}

		private void SaveLogics() {
			string[] values = this.Children
				.Where(x => DataboundListChildToggle.FindToggleFor(x).isOn).Select(x => (string)x.Value).ToArray();
			if (this.items) {
				Randomizer.Instance.Settings.Object.LogicsForItemSpawnLocationPool = values;
			} else {
				Randomizer.Instance.Settings.Object.LogicsForEnemySpawnLocationPool = values;
			}
		}

		protected override void OnEnable() {
			base.OnEnable();

			string[] logics;
			if (this.items) {
				logics = Randomizer.Instance.Settings.Object.LogicsForItemSpawnLocationPool;
			} else {
				logics = Randomizer.Instance.Settings.Object.LogicsForEnemySpawnLocationPool;
			}
			foreach (IDatabind item in this.Children) {
				string name = (string)item.Value;
				DataboundListChildToggle.FindToggleFor(item).isOn = logics.Contains(name);
			}
		}
	}
}
