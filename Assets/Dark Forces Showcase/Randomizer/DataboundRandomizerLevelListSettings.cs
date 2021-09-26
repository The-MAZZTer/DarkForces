using MZZT.DataBinding;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
	public class DataboundRandomizerLevelListSettings : Databound<RandomizerJediLvlSettings> {
		[SerializeField]
		private Toggle includeAllSelectedLevels;
		[SerializeField]
		private LevelNameList levelList;

		private void Start() {
			string[] levels = this.Value.Levels;
			foreach (LevelNameListItem item in this.levelList.Databinders) {
				string name = LevelLoader.Instance.LevelList.Levels[item.Value].FileName;
				item.GetComponent<Toggle>().isOn = levels.Contains(name);

				item.GetComponent<Toggle>().onValueChanged.AddListener(value => this.SaveLevelList());
			}
		}

		private void SaveLevelList() {
			this.Value.Levels = this.levelList.Databinders.Where(x => x.GetComponent<Toggle>().isOn).Select(x =>
				LevelLoader.Instance.LevelList.Levels[x.Value].FileName).ToArray();
		}

		private void OnEnable() {
			this.Value = Randomizer.Instance.Settings.JediLvl;

			this.includeAllSelectedLevels.isOn = !this.Value.LevelCount.Enabled;

			string[] levels = this.Value.Levels;
			foreach (LevelNameListItem item in this.levelList.Databinders) {
				string name = LevelLoader.Instance.LevelList.Levels[item.Value].FileName;
				item.GetComponent<Toggle>().isOn = levels.Contains(name);
			}
		}
	}
}
