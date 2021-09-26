using MZZT.DarkForces.FileFormats;
using MZZT.DataBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MZZT.DarkForces.Showcase {
  public class DifficultySpawnWeightList : DataboundList<DifficultySpawnWeight> {
		[SerializeField]
		private bool items;

		private void OnEnable() {
			this.Clear();
			List<DifficultySpawnWeight> list = this.items ?
				Randomizer.Instance.Settings.Object.DifficultyItemSpawnWeights :
				Randomizer.Instance.Settings.Object.DifficultyEnemySpawnWeights;
			foreach (DfLevelObjects.Difficulties difficulty in Enum.GetValues(typeof(DfLevelObjects.Difficulties))) {
				if (list.Any(x => x.Difficulty == difficulty)) {
					continue;
				}
				list.Add(new DifficultySpawnWeight() {
					Difficulty = difficulty,
					Weight = 1,
					Absolute = false
				});
			}
			this.AddRange(list.OrderBy(x => x.Difficulty));
		}
	}
}
