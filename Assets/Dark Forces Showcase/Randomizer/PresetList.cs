using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
	public class Preset {
		public string Name { get; set; }
		public RandomizerSettings Settings { get; set; }
		public bool ReadOnly { get; set; }
	}

  public class PresetList : DataboundList<Preset> {
		[SerializeField]
		private Button applyButton;

		private Toggle lastToggle;
		private void Update() {
			Toggle toggle = this.ToggleGroup.GetFirstActiveToggle();
			if (toggle == this.lastToggle) {
				return;
			}

			this.lastToggle = toggle;
			this.UpdateButtons();
		}

		private void UpdateButtons() {
			PresetListItem value = this.lastToggle?.GetComponent<PresetListItem>();
			this.applyButton.interactable = value?.Value.Settings != null;
		}

		protected override void Start() {
			this.AddDefaults();
			this.AddFromPlayerPrefs();
		}

		private void AddFromPlayerPrefs() {
			DataContractJsonSerializer serializer = new(typeof(RandomizerSettings), new DataContractJsonSerializerSettings() {
				UseSimpleDictionaryFormat = true
			});
						
			for (int i = 0; true; i++) {
				string name = PlayerPrefs.GetString($"Randomizer.Prefab.{i}.Name", null);
				if (string.IsNullOrEmpty(name)) {
					break;
				}

				string json = PlayerPrefs.GetString($"Randomizer.Prefab.{i}.Settings", null);

				RandomizerSettings settings = null;
				using (XmlReader reader = XmlReader.Create(new StringReader(json))) {
					try {
						settings = (RandomizerSettings)serializer.ReadObject(reader);
					} catch (Exception) {
					}
				}
				if (settings == null) {
					continue;
				}

				this.Add(new Preset {
					Name = name,
					ReadOnly = false,
					Settings = settings
				});
			}
		}

		public void SyncToPlayerPrefs() {
			DataContractJsonSerializer serializer = new(typeof(RandomizerSettings), new DataContractJsonSerializerSettings() {
				UseSimpleDictionaryFormat = true
			});

			int count = 0;
			foreach ((Preset preset, int i) in this.Value.Where(x => !x.ReadOnly).Select((x, i) => (x, i))) {
				PlayerPrefs.SetString($"Randomizer.Prefab.{i}.Name", preset.Name);
				
				StringWriter stringWriter = new();
				using (XmlWriter writer = XmlWriter.Create(stringWriter)) {
					try {
						serializer.WriteObject(writer, preset.Settings);
					} catch (Exception) {
					}
				}
				string json = stringWriter.ToString();
				PlayerPrefs.SetString($"Randomizer.Prefab.{i}.Settings", json);
				count = i + 1;
			}

			while (true) {
				string name = PlayerPrefs.GetString($"Randomizer.Prefab.{count}.Name", null);

				PlayerPrefs.DeleteKey($"Randomizer.Prefab.{count}.Name");
				PlayerPrefs.DeleteKey($"Randomizer.Prefab.{count}.Settings");

				if (string.IsNullOrEmpty(name)) {
					return;
				}

				count++;
			}
		}

		private string lastImportPath;
		public async void ImportAsync() {
			if (string.IsNullOrEmpty(this.lastImportPath)) {
				this.lastImportPath = FileLoader.Instance.DarkForcesFolder;
			}

			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				 AllowNavigateGob = true,
				 AllowNavigateLfd = false,
				 FileSearchPatterns = new[] { "*.GOB", "RNDMIZER.JSO", "*.JSON" },
				 SelectButtonText = "Import",
				 SelectedFileMustExist = true,
				 SelectedPathMustExist = true,
				 StartPath = this.lastImportPath,
				 Title = "Import Randomizer Preset"
			});
			if (path == null) {
				return;
			}
			this.lastImportPath = Path.GetDirectoryName(path);

			DataContractJsonSerializer serializer = new(typeof(RandomizerSettings), new DataContractJsonSerializerSettings() {
				UseSimpleDictionaryFormat = true
			});

			ResourceCache.Instance.ClearWarnings();
			RandomizerSettings settings = null;

			Stream stream;
			if (File.Exists(this.lastImportPath)) {
				using (FileStream gobStream = new(this.lastImportPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
					DfGobContainer gob = await DfGobContainer.ReadAsync(gobStream);
					stream = await gob.GetFileStreamAsync(Path.GetFileName(path), gobStream);
				}
			} else {
				stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			}

			using (stream) {
				try {
					settings = (RandomizerSettings)serializer.ReadObject(stream);
				} catch (Exception ex) {
					ResourceCache.Instance.AddError(Path.GetFileName(path), ex);
				}
			}
			await LevelLoader.Instance.ShowWarningsAsync(Path.GetFileName(path));
			if (settings == null) {
				return;
			}

			Preset preset = new() {
				Name = Path.GetFileNameWithoutExtension(path),
				Settings = settings,
				ReadOnly = false
			};
			this.Add(preset);

			this.SelectedValue = preset;

			this.SyncToPlayerPrefs();
		}

		public async void ExportAsync(Preset preset) {
			if (string.IsNullOrEmpty(this.lastImportPath)) {
				this.lastImportPath = FileLoader.Instance.DarkForcesFolder;
			}

			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = false,
				FileSearchPatterns = new[] { "*.JSON" },
				SelectButtonText = "Export",
				SelectedFileMustExist = false,
				SelectedPathMustExist = true,
				StartPath = this.lastImportPath,
				StartSelectedFile = $"{preset.Name}.json",
				Title = "Export Randomizer Preset",
				ValidateFileName = true
			});
			if (path == null) {
				return;
			}
			this.lastImportPath = Path.GetDirectoryName(path);

			DataContractJsonSerializer serializer = new(typeof(RandomizerSettings), new DataContractJsonSerializerSettings() {
				UseSimpleDictionaryFormat = true
			});

			using (FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.None)) {
				try {
					serializer.WriteObject(stream, preset.Settings);
				} catch (Exception) {
				}
			}
		}

		public void Save() {
			RandomizerSettings settings = Randomizer.Instance.Settings.Clone();

			Preset preset = new() {
				Name = "New Preset",
				Settings = settings,
				ReadOnly = false
			};
			this.Add(preset);

			this.SelectedValue = preset;

			this.SyncToPlayerPrefs();
		}

		public void Delete(Preset preset) {
			this.Remove(preset);

			this.SyncToPlayerPrefs();
		}

		public void Apply() {
			Preset preset = this.SelectedValue;
			if (preset?.Settings == null) {
				return;
			}

			Randomizer.Instance.Settings = preset.Settings.Clone();
		}

		private void AddDefaults() {
			this.AddRange(new[] { RandomizerUi.DEFAULT_PRESET, new Preset {
				Name = "Darker Forces",
				ReadOnly = true,
				Settings = new RandomizerSettings() {
					Version = 1,
					FixedSeed = false,
					SaveSettingsToGob = true,
					Seed = 0x00000000,
					Colormap = new RandomizerColormapSettings() {
						ForceLightLevel = new RandomRange() {
							Enabled = false,
							Minimum = 0,
							Maximum = 31
						},
						HeadlightBrightness = new RandomRange() {
							Enabled = false,
							Minimum = 0,
							Maximum = 31
						},
						HeadlightDistance = new RandomRange() {
							Enabled = false,
							Minimum = 0,
							Maximum = 127,
						},
						RandomizePerLevel = false
					},
					Cutscenes = new RandomizerCutscenesSettings() {
						AdjustCutsceneMusicVolume = 1,
						AdjustCutsceneSpeed = new RandomRange() {
							Enabled = false,
							Minimum = 1,
							Maximum = 1
						},
						RemoveCutscenes = false
					},
					JediLvl = new RandomizerJediLvlSettings() {
						LevelCount = new RandomRange() {
							Enabled = false,
							Minimum = 1,
							Maximum = 1
						},
						Levels = new[] { "SECBASE" },
						RandomizeOrder = false
					},
					Level = new RandomizerLevelSettings() {
						MapOverrideMode = MapOverrideModes.None,
						LightLevelMultiplier = new RandomRange() {
							Enabled = true,
							Minimum = 0.5f,
							Maximum = 0.5f
						},
						LightLevelMultiplierPerLevel = false,
						RemoveSecrets = false
					},
					ModSourcePaths = new Dictionary<string, string>(),
					Music = new RandomizerMusicSettings() {
						RandomizeTrackOrder = false
					},
					Object = new RandomizerObjectSettings() {
						DefaultLogicFiles = RandomizerUi.DEFAULT_TEMPLATES.Select(x => new ObjectTemplate() { Logic = x.Key, Filename = x.Value}).ToArray(),
						DifficultyEnemySpawnWeights = new List<DifficultySpawnWeight>() {
							new DifficultySpawnWeight() { Difficulty = DfLevelObjects.Difficulties.Easy, Weight = 0, Absolute = true },
							new DifficultySpawnWeight() { Difficulty = DfLevelObjects.Difficulties.EasyMedium, Weight = 0, Absolute = true },
							new DifficultySpawnWeight() { Difficulty = DfLevelObjects.Difficulties.EasyMediumHard, Weight = 15, Absolute = true },
							new DifficultySpawnWeight() { Difficulty = DfLevelObjects.Difficulties.MediumHard, Weight = 10, Absolute = true },
							new DifficultySpawnWeight() { Difficulty = DfLevelObjects.Difficulties.Hard, Weight = 15, Absolute = true }
						},
						DifficultyItemSpawnWeights = new List<DifficultySpawnWeight>() {
							new DifficultySpawnWeight() { Difficulty = DfLevelObjects.Difficulties.Easy, Weight = 10, Absolute = true },
							new DifficultySpawnWeight() { Difficulty = DfLevelObjects.Difficulties.EasyMedium, Weight = 10, Absolute = true },
							new DifficultySpawnWeight() { Difficulty = DfLevelObjects.Difficulties.EasyMediumHard, Weight = 20, Absolute = true },
							new DifficultySpawnWeight() { Difficulty = DfLevelObjects.Difficulties.MediumHard, Weight = 0, Absolute = true },
							new DifficultySpawnWeight() { Difficulty = DfLevelObjects.Difficulties.Hard, Weight = 0, Absolute = true }
						},
						EnemyGenerationPoolSource = EntityGenerationPoolSources.None,
						EnemyLogicSpawnWeights = new List<LogicSpawnWeight>() {
							new LogicSpawnWeight() { Logic = "D_TROOP1", Weight = 25, Absolute = true },
							new LogicSpawnWeight() { Logic = "D_TROOP2", Weight = 14, Absolute = true },
							new LogicSpawnWeight() { Logic = "D_TROOP3", Weight = 1, Absolute = true }
						},
						MultiLogicEnemyAction = MultiLogicActions.Shuffle,
						ItemAwardFirstLevel = new List<ItemAward>() {
							new ItemAward() { Difficulty = DfLevelObjects.Difficulties.EasyMedium, Logic = "CANNON", Count = 1 },
							new ItemAward() { Difficulty = DfLevelObjects.Difficulties.Easy, Logic = "LIFE", Count = 2},
							new ItemAward() { Difficulty = DfLevelObjects.Difficulties.EasyMedium, Logic = "PLASMA", Count = 5 },
							new ItemAward() { Difficulty = DfLevelObjects.Difficulties.Easy, Logic = "MISSILES", Count = 2 },
							new ItemAward() { Difficulty = DfLevelObjects.Difficulties.EasyMedium, Logic = "REVIVE", Count = 1 },
							new ItemAward() { Difficulty = DfLevelObjects.Difficulties.Easy, Logic = "INVINCIBLE", Count = 1 }
						},
						ItemAwardOtherLevels = new List<ItemAward>() {
							new ItemAward() { Difficulty = DfLevelObjects.Difficulties.EasyMedium, Logic = "LIFE", Count = 1 },
							new ItemAward() { Difficulty = DfLevelObjects.Difficulties.Easy, Logic = "LIFE", Count = 2},
							new ItemAward() { Difficulty = DfLevelObjects.Difficulties.EasyMedium, Logic = "PLASMA", Count = 5 },
							new ItemAward() { Difficulty = DfLevelObjects.Difficulties.Easy, Logic = "MISSILES", Count = 2 },
							new ItemAward() { Difficulty = DfLevelObjects.Difficulties.EasyMedium, Logic = "REVIVE", Count = 1 },
							new ItemAward() { Difficulty = DfLevelObjects.Difficulties.Easy, Logic = "INVINCIBLE", Count = 1 }
						},
						ItemGenerationPoolSource = EntityGenerationPoolSources.None, // OK
						ItemLogicSpawnWeights = new List<LogicSpawnWeight>() {
							new LogicSpawnWeight() { Logic = "SHIELD", Weight = 10, Absolute = true },
							new LogicSpawnWeight() { Logic = "MEDKIT", Weight = 9, Absolute = true },
							new LogicSpawnWeight() { Logic = "CANNON", Weight = 1, Absolute = true },
							new LogicSpawnWeight() { Logic = "PLASMA", Weight = 10, Absolute = true },
							new LogicSpawnWeight() { Logic = "MISSILE", Weight = 4, Absolute = true },
							new LogicSpawnWeight() { Logic = "MISSILES", Weight = 2, Absolute = true },
							new LogicSpawnWeight() { Logic = "SUPERCHARGE", Weight = 1, Absolute = true },
							new LogicSpawnWeight() { Logic = "INVINCIBLE", Weight = 1, Absolute = true },
							new LogicSpawnWeight() { Logic = "LIFE", Weight = 1, Absolute = true },
							new LogicSpawnWeight() { Logic = "REVIVE", Weight = 1, Absolute = true }
						},
						LessenEnemyProbabilityWhenSpawned = true,
						LessenItemProbabilityWhenSpawned = true,
						LogicsForEnemySpawnLocationPool = RandomizerUi.ENEMY_LOGICS.ToArray(),
						LogicsForItemSpawnLocationPool = RandomizerUi.ITEM_LOGICS.ToArray(),
						RandomEnemyLocationSelectionMode = RandomLocationSelectionModes.PositionThenSector,
						RandomItemLocationSelectionMode = RandomLocationSelectionModes.PositionThenSector,
						RandomizeBosses = true,
						RandomizeEnemies = true,
						RandomizeEnemyYaw = false,
						RandomizeGeneratorsDelay = new RandomRange() {
							Maximum = 30,
							Minimum = 30,
							Enabled = false
						},
						RandomizeGeneratorsInterval = new RandomRange() {
							Maximum = 20,
							Minimum = 20,
							Enabled = false
						},
						RandomizeGeneratorsMaximumAlive = new RandomRange() {
							Maximum = 3,
							Minimum = 3,
							Enabled = false
						},
						RandomizeGeneratorsMaximumDistance = new RandomRange() {
							Maximum = 200,
							Minimum = 200,
							Enabled = false
						},
						RandomizeGeneratorsMinimumDistance = new RandomRange() {
							Maximum = 70,
							Minimum = 70,
							Enabled = false
						},
						RandomizeGeneratorsNumberTerminate = new RandomRange() {
							Maximum = 8,
							Minimum = 8,
							Enabled = false
						},
						RandomizeGeneratorsWanderTime = new RandomRange() {
							Maximum = 40,
							Minimum = 40,
							Enabled = false
						},
						RandomizeItems = true,
						RemoveCheckpoints = false,
						ReplaceKeyAndCodeOfficersWithTheirItems = false,
						SpawnDiagonasOnlyInWater = true,
						SpawnItemsInPits = false,
						SpawnItemsInWater = false,
						SpawnOnlyFlyingAndDiagonasInWater = true,
						SpawnOnlyFlyingOverPits = true,
						UnlockAllDoorsAndIncludeKeysInSpawnLocationPool = true,
						EnemySpawnSources = SpawnSources.ExistingThenRandom,
						ItemSpawnSources = SpawnSources.ExistingThenRandom
					},
					Palette = new RandomizerPaletteSettings() {
						LightHue = new RandomRange() {
							Enabled = false,
							Minimum = -180,
							Maximum = 180
						},
						LightLum = new RandomRange() {
							Enabled = false,
							Minimum = 0,
							Maximum = 2
						},
						LightSat = new RandomRange() {
							Enabled = false,
							Minimum = 0,
							Maximum = 2
						},
						RandomizeLightColors = false,
						RandomizeOtherColors = false,
						RandomizePerLevel = true
					}
				},
			}, new Preset {
				Name = "Darkest Forces",
				ReadOnly = true,
				Settings = new RandomizerSettings() {
					Version = 1,
					FixedSeed = false,
					SaveSettingsToGob = true,
					Seed = 0x00000000,
					Colormap = new RandomizerColormapSettings() {
						ForceLightLevel = new RandomRange() {
							Enabled = false,
							Minimum = 0,
							Maximum = 31
						},
						HeadlightBrightness = new RandomRange() {
							Enabled = true,
							Minimum = 31,
							Maximum = 31
						},
						HeadlightDistance = new RandomRange() {
							Enabled = false,
							Minimum = 0,
							Maximum = 127,
						},
					},
					Cutscenes = new RandomizerCutscenesSettings() {
						AdjustCutsceneMusicVolume = 1,
						AdjustCutsceneSpeed = new RandomRange() {
							Enabled = false,
							Minimum = 1,
							Maximum = 1
						},
						RemoveCutscenes = false
					},
					JediLvl = new RandomizerJediLvlSettings() {
						LevelCount = new RandomRange() {
							Enabled = false,
							Minimum = 1,
							Maximum = 1
						},
						Levels = new[] { "SECBASE" },
						RandomizeOrder = false
					},
					Level = new RandomizerLevelSettings() {
						MapOverrideMode = MapOverrideModes.HideMap,
						LightLevelMultiplier = new RandomRange() {
							Enabled = true,
							Minimum = 0.25f,
							Maximum = 0.25f
						},
						LightLevelMultiplierPerLevel = false,
						RemoveSecrets = false
					},
					ModSourcePaths = new Dictionary<string, string>(),
					Music = new RandomizerMusicSettings() {
						RandomizeTrackOrder = false
					},
					Object = new RandomizerObjectSettings() {
						DefaultLogicFiles = RandomizerUi.DEFAULT_TEMPLATES.Select(x => new ObjectTemplate() { Logic = x.Key, Filename = x.Value}).ToArray(),
						DifficultyEnemySpawnWeights = new List<DifficultySpawnWeight>() {
							new DifficultySpawnWeight() { Difficulty = DfLevelObjects.Difficulties.Easy, Weight = 0, Absolute = true },
							new DifficultySpawnWeight() { Difficulty = DfLevelObjects.Difficulties.EasyMedium, Weight = 0, Absolute = true },
							new DifficultySpawnWeight() { Difficulty = DfLevelObjects.Difficulties.EasyMediumHard, Weight = 75, Absolute = true },
							new DifficultySpawnWeight() { Difficulty = DfLevelObjects.Difficulties.MediumHard, Weight = 0, Absolute = true },
							new DifficultySpawnWeight() { Difficulty = DfLevelObjects.Difficulties.Hard, Weight = 0, Absolute = true }
						},
						DifficultyItemSpawnWeights = new List<DifficultySpawnWeight>() {
							new DifficultySpawnWeight() { Difficulty = DfLevelObjects.Difficulties.Easy, Weight = 25, Absolute = true },
							new DifficultySpawnWeight() { Difficulty = DfLevelObjects.Difficulties.EasyMedium, Weight = 25, Absolute = true },
							new DifficultySpawnWeight() { Difficulty = DfLevelObjects.Difficulties.EasyMediumHard, Weight = 50, Absolute = true },
							new DifficultySpawnWeight() { Difficulty = DfLevelObjects.Difficulties.MediumHard, Weight = 0, Absolute = true },
							new DifficultySpawnWeight() { Difficulty = DfLevelObjects.Difficulties.Hard, Weight = 0, Absolute = true }
						},
						EnemyGenerationPoolSource = EntityGenerationPoolSources.None,
						EnemyLogicSpawnWeights = new List<LogicSpawnWeight>() {
							new LogicSpawnWeight() { Logic = "MOUSEBOT", Weight = 75, Absolute = true }
						},
						MultiLogicEnemyAction = MultiLogicActions.Shuffle,
						ItemAwardFirstLevel = new List<ItemAward>(),
						ItemAwardOtherLevels = new List<ItemAward>(),
						ItemGenerationPoolSource = EntityGenerationPoolSources.None,
						ItemLogicSpawnWeights = new List<LogicSpawnWeight>() {
							new LogicSpawnWeight() { Logic = "RIFLE", Weight = 5, Absolute = true },
							new LogicSpawnWeight() { Logic = "AUTOGUN", Weight = 5, Absolute = true },
							new LogicSpawnWeight() { Logic = "FUSION", Weight = 5, Absolute = true },
							new LogicSpawnWeight() { Logic = "MORTAR", Weight = 5, Absolute = true },
							new LogicSpawnWeight() { Logic = "CONCUSSION", Weight = 5, Absolute = true },
							new LogicSpawnWeight() { Logic = "CANNON", Weight = 5, Absolute = true },
							new LogicSpawnWeight() { Logic = "ENERGY", Weight = 10, Absolute = true },
							new LogicSpawnWeight() { Logic = "DETONATOR", Weight = 5, Absolute = true },
							new LogicSpawnWeight() { Logic = "DETONATORS", Weight = 5, Absolute = true },
							new LogicSpawnWeight() { Logic = "POWER", Weight = 10, Absolute = true },
							new LogicSpawnWeight() { Logic = "MINE", Weight = 5, Absolute = true },
							new LogicSpawnWeight() { Logic = "MINES", Weight = 5, Absolute = true },
							new LogicSpawnWeight() { Logic = "SHELL", Weight = 5, Absolute = true },
							new LogicSpawnWeight() { Logic = "SHELLS", Weight = 5, Absolute = true },
							new LogicSpawnWeight() { Logic = "PLASMA", Weight = 10, Absolute = true },
							new LogicSpawnWeight() { Logic = "MISSILE", Weight = 5, Absolute = true },
							new LogicSpawnWeight() { Logic = "MISSILES", Weight = 5, Absolute = true }
						},
						LessenEnemyProbabilityWhenSpawned = true,
						LessenItemProbabilityWhenSpawned = true,
						LogicsForEnemySpawnLocationPool = RandomizerUi.ENEMY_LOGICS.ToArray(),
						LogicsForItemSpawnLocationPool = RandomizerUi.ITEM_LOGICS.ToArray(),
						RandomEnemyLocationSelectionMode = RandomLocationSelectionModes.PositionThenSector,
						RandomItemLocationSelectionMode = RandomLocationSelectionModes.PositionThenSector,
						RandomizeBosses = true,
						RandomizeEnemies = true,
						RandomizeEnemyYaw = false,
						RandomizeGeneratorsDelay = new RandomRange() {
							Maximum = 30,
							Minimum = 30,
							Enabled = false
						},
						RandomizeGeneratorsInterval = new RandomRange() {
							Maximum = 20,
							Minimum = 20,
							Enabled = false
						},
						RandomizeGeneratorsMaximumAlive = new RandomRange() {
							Maximum = 3,
							Minimum = 3,
							Enabled = false
						},
						RandomizeGeneratorsMaximumDistance = new RandomRange() {
							Maximum = 200,
							Minimum = 200,
							Enabled = false
						},
						RandomizeGeneratorsMinimumDistance = new RandomRange() {
							Maximum = 70,
							Minimum = 70,
							Enabled = false
						},
						RandomizeGeneratorsNumberTerminate = new RandomRange() {
							Maximum = 8,
							Minimum = 8,
							Enabled = false
						},
						RandomizeGeneratorsWanderTime = new RandomRange() {
							Maximum = 40,
							Minimum = 40,
							Enabled = false
						},
						RandomizeItems = true,
						RemoveCheckpoints = false,
						ReplaceKeyAndCodeOfficersWithTheirItems = false,
						SpawnDiagonasOnlyInWater = true,
						SpawnItemsInPits = false,
						SpawnItemsInWater = false,
						SpawnOnlyFlyingAndDiagonasInWater = true,
						SpawnOnlyFlyingOverPits = true,
						UnlockAllDoorsAndIncludeKeysInSpawnLocationPool = true,
						EnemySpawnSources = SpawnSources.ExistingThenRandom,
						ItemSpawnSources = SpawnSources.ExistingThenRandom
					},
					Palette = new RandomizerPaletteSettings() {
						LightHue = new RandomRange() {
							Enabled = false,
							Minimum = -180,
							Maximum = 180
						},
						LightLum = new RandomRange() {
							Enabled = false,
							Minimum = 0,
							Maximum = 2
						},
						LightSat = new RandomRange() {
							Enabled = false,
							Minimum = 0,
							Maximum = 2
						},
						RandomizeLightColors = false,
						RandomizeOtherColors = false,
						RandomizePerLevel = true
					}
				}
			} });
		}
  }
}