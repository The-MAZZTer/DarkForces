using MZZT.Data.Binding;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MZZT.DarkForces.Showcase {
  public class LogicSpawnWeightList : DataboundList<LogicSpawnWeight> {
		[SerializeField]
		private bool items;

		protected override void OnEnable() {
			base.OnEnable();

			this.Clear();
			List<LogicSpawnWeight> list = this.items ?
				Randomizer.Instance.Settings.Object.ItemLogicSpawnWeights :
				Randomizer.Instance.Settings.Object.EnemyLogicSpawnWeights;
			string[] logics = this.items ? RandomizerUi.ITEM_LOGICS : RandomizerUi.ENEMY_LOGICS;
			foreach (string logic in logics) {
				if (list.Any(x => x.Logic == logic)) {
					continue;
				}
				list.Add(new LogicSpawnWeight() {
					Logic = logic,
					Weight = 1,
					Absolute = false
				});
			}
			list.Sort((x, y) => Array.IndexOf(logics, x.Logic) - Array.IndexOf(logics, y.Logic));
			this.AddRange(list);
		}
	}
}
