using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
	public class DataboundRandomizerLevelListSettings : Databind<RandomizerJediLvlSettings> {
		[SerializeField]
		private Toggle includeAllSelectedLevels;
		[SerializeField]
		private LevelNameList levelList;

		protected override void OnEnable() {
			base.OnEnable();

			this.includeAllSelectedLevels.isOn = !this.Value.LevelCount.Enabled;

			string[] levels = this.Value.Levels;
			foreach (IDatabind item in this.levelList.Children) {
				string name = ((DfLevelList.Level)item.Value).FileName;

				Toggle toggle = DataboundListChildToggle.FindToggleFor(item);
				toggle.isOn = levels.Contains(name);
			}
		}

		protected override void Start() {
			base.Start();

			string[] levels = this.Value.Levels;
			foreach (IDatabind item in this.levelList.Children) {
				string name = ((DfLevelList.Level)item.Value).FileName;

				Toggle toggle = DataboundListChildToggle.FindToggleFor(item);
				toggle.isOn = levels.Contains(name);
				toggle.onValueChanged.AddListener(value => this.SaveLevelList());
			}
		}

		private void SaveLevelList() {
			this.Value.Levels = this.levelList.Children.Where(x => DataboundListChildToggle.FindToggleFor(x).isOn).Select(x =>
				((DfLevelList.Level)x.Value).FileName).ToArray();
		}
	}
}
