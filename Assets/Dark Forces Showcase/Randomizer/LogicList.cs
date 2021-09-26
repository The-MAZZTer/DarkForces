using MZZT.DataBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {

	public class LogicList : DataboundList<string> {
		[SerializeField]
		private bool items;

		private void Start() {
			this.AddRange(this.items ? RandomizerUi.ITEM_LOGICS : RandomizerUi.ENEMY_LOGICS);

			foreach (LogicItem item in this.Databinders) {
				item.GetComponent<Toggle>().onValueChanged.AddListener(value => this.SaveLogics());
			}
		}

		private void SaveLogics() {
			string[] values = this.Databinders
				.Where(x => x.GetComponent<Toggle>().isOn).Select(x => x.Value).ToArray();
			if (this.items) {
				Randomizer.Instance.Settings.Object.LogicsForItemSpawnLocationPool = values;
			} else {
				Randomizer.Instance.Settings.Object.LogicsForEnemySpawnLocationPool = values;
			}
		}

		private void OnEnable() {
			string[] logics;
			if (this.items) {
				logics = Randomizer.Instance.Settings.Object.LogicsForItemSpawnLocationPool;
			} else {
				logics = Randomizer.Instance.Settings.Object.LogicsForEnemySpawnLocationPool;
			}
			foreach (LogicItem item in this.Databinders) {
				string name = item.Value;
				item.GetComponent<Toggle>().isOn = logics.Contains(name);
			}
		}
	}
}
