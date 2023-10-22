using MZZT.DarkForces.FileFormats;
using MZZT.IO.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
	public class RandomizerUi : MonoBehaviour {
		public static readonly string[] ENEMY_LOGICS = new string[] {
			"I_OFFICER",
			"TROOP",
			"STORM1",
			"COMMANDO",
			"BOSSK",
			"G_GUARD",
			"REE_YEES",
			"REE_YEES2",
			"SEWER1",
			"INT_DROID",
			"PROBE_DROID",
			"REMOTE",
			"BOBA_FETT",
			"KELL",
			"D_TROOP1",
			"D_TROOP2",
			"D_TROOP3",
			"BARREL",
			"LAND_MINE",
			"TURRET",
			"MOUSEBOT",
			"WELDER",
			"GENERATOR I_OFFICER",
			"GENERATOR TROOP",
			"GENERATOR STORM1",
			"GENERATOR COMMANDO",
			"GENERATOR BOSSK",
			"GENERATOR G_GUARD",
			"GENERATOR REE_YEES",
			"GENERATOR REE_YEES2",
			"GENERATOR SEWER1",
			"GENERATOR INT_DROID",
			"GENERATOR PROBE_DROID",
			"GENERATOR REMOTE"
		};

		public static readonly string[] ITEM_LOGICS = new string[] {
			"SHIELD",
			"BATTERY",
			"CLEATS",
			"GOGGLES",
			"MASK",
			"MEDKIT",
			"RIFLE",
			"AUTOGUN",
			"FUSION",
			"MORTAR",
			"CONCUSSION",
			"CANNON",
			"ENERGY",
			"DETONATOR",
			"DETONATORS",
			"POWER",
			"MINE",
			"MINES",
			"SHELL",
			"SHELLS",
			"PLASMA",
			"MISSILE",
			"MISSILES",
			"SUPERCHARGE",
			"INVINCIBLE",
			"LIFE",
			"REVIVE"
		};

		public static readonly Dictionary<string, string> DEFAULT_TEMPLATES = new() {
			["SHIELD"] = "IARMOR.WAX",
			["BATTERY"] = "IBATTERY.FME",
			["CLEATS"] = "ICLEATS.FME",
			["GOGGLES"] = "IGOGGLES.FME",
			["MASK"] = "IMASK.FME",
			["MEDKIT"] = "IMEDKIT.FME",
			["RIFLE"] = "IST-GUNI.FME",
			["AUTOGUN"] = "IAUTOGUN.FME",
			["FUSION"] = "IFUSION.FME",
			["MORTAR"] = "IMORTAR.FME",
			["CONCUSSION"] = "ICONCUS.FME",
			["CANNON"] = "ICANNON.FME",
			["ENERGY"] = "IENERGY.FME",
			["DETONATOR"] = "IDET.FME",
			["DETONATORS"] = "IDETS.FME",
			["POWER"] = "IPOWER.FME",
			["MINE"] = "IMINE.FME",
			["MINES"] = "IMINES.FME",
			["SHELL"] = "ISHELL.FME",
			["SHELLS"] = "ISHELLS.FME",
			["PLASMA"] = "IPLAZMA.FME",
			["MISSILE"] = "IMSL.FME",
			["MISSILES"] = "IMSLS.FME",
			["SUPERCHARGE"] = "ICHARGE.FME",
			["INVINCIBLE"] = "IINVINC.WAX",
			["LIFE"] = "ILIFE.WAX",
			["REVIVE"] = "IREVIVE.WAX",
			["BLUE"] = "IKEYB.FME",
			["RED"] = "IKEYR.FME",
			["YELLOW"] = "IKEYY.FME",
			["CODE1"] = "DET_CODE.FME",
			["CODE2"] = "DET_CODE.FME",
			["CODE3"] = "DET_CODE.FME",
			["CODE4"] = "DET_CODE.FME",
			["CODE5"] = "DET_CODE.FME",
			["CODE6"] = "DET_CODE.FME",
			["CODE7"] = "DET_CODE.FME",
			["CODE8"] = "DET_CODE.FME",
			["CODE9"] = "DET_CODE.FME",
			["DATATAPE"] = "IDATA.FME",
			["PLANS"] = "IDPLANS.WAX",
			["DT_WEAPON"] = "IDTGUN.FME",
			["NAVA"] = "INAVA.WAX",
			["PHRIK"] = "IPHRIK.FME",
			["PILE"] = "IPILE.FME",
			["I_OFFICER"] = "OFFCFIN.WAX",
			["I_OFFICERR"] = "OFFCFIN.WAX",
			["I_OFFICERB"] = "OFFCFIN.WAX",
			["I_OFFICERY"] = "OFFCFIN.WAX",
			["I_OFFICER1"] = "OFFCFIN.WAX",
			["I_OFFICER2"] = "OFFCFIN.WAX",
			["I_OFFICER3"] = "OFFCFIN.WAX",
			["I_OFFICER4"] = "OFFCFIN.WAX",
			["I_OFFICER5"] = "OFFCFIN.WAX",
			["I_OFFICER6"] = "OFFCFIN.WAX",
			["I_OFFICER7"] = "OFFCFIN.WAX",
			["I_OFFICER8"] = "OFFCFIN.WAX",
			["I_OFFICER9"] = "OFFCFIN.WAX",
			["TROOP"] = "STORMFIN.WAX",
			["STORM1"] = "STORMFIN.WAX",
			["COMMANDO"] = "COMMANDO.WAX",
			["BOSSK"] = "BOSSK.WAX",
			["G_GUARD"] = "GAMGUARD.WAX",
			["REE_YEES"] = "REEYEES.WAX",
			["REE_YEES2"] = "REEYEES.WAX",
			["SEWER1"] = "SEWERBUG.WAX",
			["INT_DROID"] = "INTDROID.WAX",
			["PROBE_DROID"] = "PROBE.WAX",
			["REMOTE"] = "REMOTE.WAX",
			["BOBA_FETT"] = "BOBAFETT.WAX",
			["KELL"] = "KELL.WAX",
			["D_TROOP1"] = "PHASE1.WAX",
			["D_TROOP2"] = "PHASE2.WAX",
			["D_TROOP3"] = "PHASE3X.WAX",
			["BARREL"] = "BARREL.WAX",
			["LAND_MINE"] = "LANDMINE.FME",
			["TURRET"] = "GUN.3DO",
			["MOUSEBOT"] = "MOUSEBOT.3DO",
			["WELDER"] = "WELDARM.3DO"
		};

		[SerializeField]
		private TMP_Text nameLabel;

		[SerializeField]
		private Toggle loadGobAsMod;

		private async Task UpdateModTextAsync() {
			string text;
			if (!Mod.Instance.List.Any()) {
				text = "Dark Forces";
			} else {
				string path = Mod.Instance.Gob;
				text = Path.GetFileName(path);
				if (path != null) {
					try {
						DfLevelList levels = await FileLoader.Instance.LoadGobFileAsync<DfLevelList>("JEDI.LVL");
						text = levels?.Levels.FirstOrDefault()?.DisplayName;
					} catch (Exception e) {
						Debug.LogError(e);
					}
					if (text == null) {
						text = Path.GetFileName(path);
					}
				} else {
					path = Mod.Instance.List.First().FilePath;
					text = Path.GetFileName(path);
				}
			}

			this.nameLabel.text = $"{text} Randomizer";
		}

		private async void Start() {
			if (!FileLoader.Instance.Gobs.Any()) {
				await FileLoader.Instance.LoadStandardFilesAsync();
			}

			await this.UpdateModTextAsync();

			Stream jsonStream = await FileLoader.Instance.GetGobFileStreamAsync("RNDMIZER.JSO");
			if (jsonStream != null) {
				using (jsonStream) {
					DataContractJsonSerializer serializer = new(typeof(RandomizerSettings), new DataContractJsonSerializerSettings() {
						UseSimpleDictionaryFormat = true
					});

					RandomizerSettings settings = null;
					try {
						settings = (RandomizerSettings)serializer.ReadObject(jsonStream);
					} catch (Exception) {
					}
					if (settings != null) {
						Dictionary<string, string> modPaths = settings.ModSourcePaths;

						Randomizer.Instance.Settings = settings;

						if (modPaths.Any(x => !FileManager.Instance.FileExists(x.Key))) {
							await DfMessageBox.Instance.ShowAsync(
								"You loaded a randomized GOB as a mod. However the original mod files can no longer be loaded. The randomizer settings have been loaded; you can save a preset and return to the main menu to manually load the original mod files if you wish.");
						} else {
							Mod.Instance.List.Clear();
							Mod.Instance.List.AddRange(modPaths.Select(x => new ModFile {
								FilePath = x.Key,
								Overrides = x.Value
							}));
							await Mod.Instance.LoadModAsync();

							await DfMessageBox.Instance.ShowAsync(
								"You loaded a randomized GOB as a mod. The randomizer settings and the original mod files have been loaded automatically.");
						}
					}
				}
			}

			if (Randomizer.Instance.Settings == null) {
				Randomizer.Instance.Settings = DEFAULT_PRESET.Settings.Clone();
			}
		}

		public static readonly Preset DEFAULT_PRESET = new() {
			Name = "Default",
			ReadOnly = true,
			Settings = new() {
				Colormap = new() {
					ForceLightLevel = new() {
						Enabled = false,
						Minimum = 0,
						Maximum = 31
					},
					HeadlightBrightness = new() {
						Enabled = false,
						Minimum = 0,
						Maximum = 31
					},
					HeadlightDistance = new() {
						Enabled = false,
						Minimum = 0,
						Maximum = 127
					},
					RandomizePerLevel = false
				},
				Cutscenes = new() {
					AdjustCutsceneMusicVolume = 1,
					AdjustCutsceneSpeed = new() {
						Enabled = false,
						Minimum = 0,
						Maximum = 1
					},
					RemoveCutscenes = false
				},
				JediLvl = new() {
					LevelCount = new() {
						Enabled = false,
						Minimum = 1,
						Maximum = 1
					},
					Levels = new[] { "SECBASE" },
					RandomizeOrder = false,
					Title = "Seed: {seed}"
				},
				Level = new() {
					LightLevelMultiplier = new() {
						Enabled = false,
						Minimum = 0,
						Maximum = 2
					},
					LightLevelMultiplierPerLevel = false,
					MapOverrideMode = MapOverrideModes.None,
					RemoveSecrets = false
				},
				ModSourcePaths = new(),
				Music = new() {
					RandomizeTrackOrder = false
				},
				Object = new() {
					DefaultLogicFiles = DEFAULT_TEMPLATES.Select(x => new ObjectTemplate() { Logic = x.Key, Filename = x.Value }).ToArray(),
					DifficultyEnemySpawnWeights = new(),
					DifficultyItemSpawnWeights = new(),
					EnemyGenerationPoolSource = EntityGenerationPoolSources.CurrentLevel,
					EnemyLogicSpawnWeights = new(),
					EnemySpawnSources = SpawnSources.ExistingThenRandom,
					ItemAwardFirstLevel = new(),
					ItemAwardOtherLevels = new(),
					ItemGenerationPoolSource = EntityGenerationPoolSources.CurrentLevel,
					ItemLogicSpawnWeights = new(),
					ItemSpawnSources = SpawnSources.ExistingThenRandom,
					LessenEnemyProbabilityWhenSpawned = true,
					LessenItemProbabilityWhenSpawned = true,
					LogicsForEnemySpawnLocationPool = ENEMY_LOGICS,
					LogicsForItemSpawnLocationPool = ITEM_LOGICS,
					MultiLogicEnemyAction = MultiLogicActions.Shuffle,
					NightmareGeneratorsDelay = new() {
						Enabled = false,
						Minimum = 0,
						Maximum = 0
					},
					NightmareGeneratorsInterval = new() {
						Enabled = false,
						Minimum = 0,
						Maximum = 0
					},
					NightmareGeneratorsMaximumAlive = new() {
						Enabled = false,
						Minimum = 1,
						Maximum = 1
					},
					NightmareGeneratorsMaximumDistance = new() {
						Enabled = false,
						Minimum = 32767,
						Maximum = 32767
					},
					NightmareGeneratorsMinimumDistance = new() {
						Enabled = false,
						Minimum = 0,
						Maximum = 0,
					},
					NightmareGeneratorsNumberTerminate = new() {
						Enabled = false,
						Minimum = -1,
						Maximum = -1
					},
					NightmareGeneratorsWanderTime	= new() {
						Enabled = false,
						Minimum = 0,
						Maximum = 0
					},
					NightmareKeepOriginalEnemies = false,
					NightmareMode = false,
					RandomEnemyLocationSelectionMode = RandomLocationSelectionModes.PositionThenSector,
					RandomItemLocationSelectionMode = RandomLocationSelectionModes.PositionThenSector,
					RandomizeBosses = true,
					RandomizeEnemies = true,
					RandomizeEnemyYaw = false,
					RandomizeGeneratorsDelay = new() {
						Enabled = false,
						Minimum = 30,
						Maximum = 30
					},
					RandomizeGeneratorsInterval = new() {
						Enabled = false,
						Minimum = 20,
						Maximum = 20
					},
					RandomizeGeneratorsMaximumAlive = new() {
						Enabled = false,
						Maximum = 3,
						Minimum = 3
					},
					RandomizeGeneratorsMaximumDistance = new() {
						Enabled = false,
						Minimum = 200,
						Maximum = 200
					},
					RandomizeGeneratorsMinimumDistance = new() {
						Enabled = false,
						Minimum = 70,
						Maximum = 70
					},
					RandomizeGeneratorsNumberTerminate = new() {
						Enabled = false,
						Minimum = 8,
						Maximum = 8
					},
					RandomizeGeneratorsWanderTime = new() {
						Enabled = false,
						Minimum = 40,
						Maximum = 40
					},
					RandomizeItems = true,
					RemoveCheckpoints = false,
					ReplaceKeyAndCodeOfficersWithTheirItems = false,
					SpawnDiagonasOnlyInWater = true,
					SpawnItemsInPits = false,
					SpawnItemsInWater = false,
					SpawnOnlyFlyingAndDiagonasInWater = true,
					SpawnOnlyFlyingOverPits = true,
					UnlockAllDoorsAndIncludeKeysInSpawnLocationPool = false
				},
				Palette = new() {
					LightHue = new() {
						Enabled = false,
						Minimum = -180,
						Maximum = 180
					},
					LightLum = new() {
						Enabled = false,
						Minimum = 0,
						Maximum = 2
					},
					LightSat = new() {
						Enabled = false,
						Minimum = 0,
						Maximum = 2
					},
					RandomizeLightColors = false,
					RandomizeOtherColors = false,
					RandomizePerLevel = false
				},
				SaveSettingsToGob =	true,
				FixedSeed = false,
				Version = 1,
				CrossFile = new() {
					MirrorMode = MirrorModes.Disabled,
					MirrorSprites = false,
					MirrorText = false
				}
			}
		};

		public async void BuildAsync() {
			string path = await Randomizer.Instance.BuildAsync();
			if (path == null) {
				return;
			}

			if (this.loadGobAsMod.isOn) {
				Mod.Instance.List.Clear();
				Mod.Instance.List.Add(new ModFile {
					FilePath = path
				});
				await Mod.Instance.LoadModAsync();

				PauseMenu.Instance.OnReturnToMenu();
			}
		}
	}
}
