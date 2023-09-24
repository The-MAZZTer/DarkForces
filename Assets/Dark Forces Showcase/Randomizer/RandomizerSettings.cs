using MZZT.DarkForces.FileFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MZZT.DarkForces.Showcase {
	/// <summary>
	/// The randomized settings.
	/// Find the places in the code these settings are used for more details on what they mean.
	/// </summary>
	public class RandomizerSettings : ICloneable {
		public int Version { get; set; } = 1;
		public bool FixedSeed { get; set; } = false;
		public int Seed { get; set; }
		public bool SaveSettingsToGob { get; set; } = true;

		public RandomizerJediLvlSettings JediLvl { get; set; } = new RandomizerJediLvlSettings();
		public RandomizerCutscenesSettings Cutscenes { get; set; } = new RandomizerCutscenesSettings();
		public RandomizerMusicSettings Music { get; set; } = new RandomizerMusicSettings();
		public RandomizerLevelSettings Level { get; set; } = new RandomizerLevelSettings();
		public RandomizerObjectSettings Object { get; set; } = new RandomizerObjectSettings();
		public RandomizerPaletteSettings Palette { get; set; } = new RandomizerPaletteSettings();
		public RandomizerColormapSettings Colormap { get; set; } = new RandomizerColormapSettings();
		public Dictionary<string, string> ModSourcePaths { get; set; }

		object ICloneable.Clone() => this.Clone();
		public RandomizerSettings Clone() => new() {
			Colormap = this.Colormap.Clone(),
			Cutscenes = this.Cutscenes.Clone(),
			JediLvl = this.JediLvl.Clone(),
			Level = this.Level.Clone(),
			ModSourcePaths = this.ModSourcePaths.ToDictionary(x => x.Key, x => x.Value),
			Music = this.Music.Clone(),
			Object = this.Object.Clone(),
			Palette = this.Palette.Clone(),
			FixedSeed = this.FixedSeed,
			SaveSettingsToGob = this.SaveSettingsToGob,
			Seed = this.Seed,
			Version = this.Version
		};
	}

	public class RandomizerJediLvlSettings : ICloneable {
		public RandomRange LevelCount { get; set; } = new RandomRange() {
			Enabled = false,
			Minimum = 1,
			Maximum = 1
		};
		public string[] Levels { get; set; } = new[] { "SECBASE" };
		public bool RandomizeOrder { get; set; }

		object ICloneable.Clone() => this.Clone();
		public RandomizerJediLvlSettings Clone() => new() {
			LevelCount = this.LevelCount.Clone(),
			Levels = this.Levels.ToArray(),
			RandomizeOrder = this.RandomizeOrder
		};
	}

	public class RandomRange : ICloneable {
		public bool Enabled { get; set; } = false;
		public float Minimum { get; set; } = 1;
		public float Maximum { get; set; } = 1;

		object ICloneable.Clone() => this.Clone();
		public RandomRange Clone() => new() {
			Enabled = this.Enabled,
			Minimum = this.Minimum,
			Maximum = this.Maximum
		};
	}

	public class RandomizerCutscenesSettings : ICloneable {
		public bool RemoveCutscenes { get; set; }
		public RandomRange AdjustCutsceneSpeed { get; set; } = new RandomRange();
		public float AdjustCutsceneMusicVolume { get; set; } = 1;

		object ICloneable.Clone() => this.Clone();
		public RandomizerCutscenesSettings Clone() => new() {
			AdjustCutsceneMusicVolume = this.AdjustCutsceneMusicVolume,
			AdjustCutsceneSpeed = this.AdjustCutsceneSpeed.Clone(),
			RemoveCutscenes = this.RemoveCutscenes
		};
	}

	public class RandomizerMusicSettings : ICloneable {
		public bool RandomizeTrackOrder { get; set; }

		object ICloneable.Clone() => this.Clone();
		public RandomizerMusicSettings Clone() => new() {
			RandomizeTrackOrder = this.RandomizeTrackOrder
		};
	}

	public class RandomizerPaletteSettings : ICloneable {
		public bool RandomizePerLevel { get; set; }
		public RandomRange LightHue { get; set; } = new RandomRange() {
			Enabled = false,
			Minimum = -180,
			Maximum = 180
		};
		public RandomRange LightSat { get; set; } = new RandomRange() {
			Enabled = false,
			Minimum = 0,
			Maximum = 2
		};
		public RandomRange LightLum { get; set; } = new RandomRange() {
			Enabled = false,
			Minimum = 0,
			Maximum = 2
		};
		public bool RandomizeLightColors { get; set; }
		public bool RandomizeOtherColors { get; set; }

		object ICloneable.Clone() => this.Clone();
		public RandomizerPaletteSettings Clone() => new() {
			LightHue = this.LightHue.Clone(),
			LightLum = this.LightLum.Clone(),
			LightSat = this.LightSat.Clone(),
			RandomizeLightColors = this.RandomizeLightColors,
			RandomizeOtherColors = this.RandomizeOtherColors,
			RandomizePerLevel = this.RandomizePerLevel
		};
	}

	public class RandomizerColormapSettings : ICloneable {
		public bool RandomizePerLevel { get; set; }
		public RandomRange ForceLightLevel { get; set; } = new RandomRange() {
			Enabled = false,
			Minimum = 0,
			Maximum = 31
		};
		public RandomRange HeadlightBrightness { get; set; } = new RandomRange() {
			Enabled = false,
			Minimum = 0,
			Maximum = 31,
		};
		public RandomRange HeadlightDistance { get; set; } = new RandomRange() {
			Enabled = false,
			Minimum = 0,
			Maximum = 127,
		};

		object ICloneable.Clone() => this.Clone();
		public RandomizerColormapSettings Clone() => new() {
			ForceLightLevel = this.ForceLightLevel.Clone(),
			HeadlightBrightness = this.HeadlightBrightness.Clone(),
			HeadlightDistance = this.HeadlightDistance.Clone(),
			RandomizePerLevel = this.RandomizePerLevel
		};
	}

	public enum MapOverrideModes {
		[DataMember(Name = "None")]
		None,
		[DataMember(Name = "Remove overrides")]
		RemoveOverrides,
		[DataMember(Name = "Hide map")]
		HideMap
	}

	public class RandomizerLevelSettings : ICloneable {
		public MapOverrideModes MapOverrideMode { get; set; }
		public RandomRange LightLevelMultiplier { get; set; } = new RandomRange() {
			Enabled = false,
			Minimum = 0,
			Maximum = 1
		};
		public bool LightLevelMultiplierPerLevel { get; set; }
		public bool RemoveSecrets { get; set; }

		object ICloneable.Clone() => this.Clone();
		public RandomizerLevelSettings Clone() => new() {
			LightLevelMultiplier = this.LightLevelMultiplier.Clone(),
			LightLevelMultiplierPerLevel = this.LightLevelMultiplierPerLevel,
			MapOverrideMode = this.MapOverrideMode,
			RemoveSecrets = this.RemoveSecrets
		};
	}

	public enum EntityGenerationPoolSources {
		[DataMember(Name = "None")]
		None,
		[DataMember(Name = "Current level")]
		CurrentLevel,
		[DataMember(Name = "Selected levels")]
		SelectedLevels,
		[DataMember(Name = "All levels")]
		AllLevels
	}

	public enum RandomLocationSelectionModes {
		[DataMember(Name = "Position, then sector")]
		PositionThenSector,
		[DataMember(Name = "Sector, then position")]
		SectorThenPosition
	}

	public class DifficultySpawnWeight : ICloneable {
		public DfLevelObjects.Difficulties Difficulty { get; set; }
		public float Weight { get; set; }
		public bool Absolute { get; set; }

		object ICloneable.Clone() => this.Clone();
		public DifficultySpawnWeight Clone() => new() {
			Difficulty = this.Difficulty,
			Weight = this.Weight,
			Absolute = this.Absolute
		};

		public string DifficultyDisplayName => this.Difficulty switch {
			DfLevelObjects.Difficulties.Easy => "Easy",
			DfLevelObjects.Difficulties.EasyMedium => "Easy, Medium",
			DfLevelObjects.Difficulties.EasyMediumHard => "Easy, Medium, Hard",
			DfLevelObjects.Difficulties.MediumHard => "Medium, Hard",
			DfLevelObjects.Difficulties.Hard => "Hard",
			_ => throw new NotImplementedException()
		};
	}

	public class LogicSpawnWeight : ICloneable {
		public string Logic { get; set; }
		public float Weight { get; set; }
		public bool Absolute { get; set; }

		object ICloneable.Clone() => this.Clone();
		public LogicSpawnWeight Clone() => new() {
			Logic = this.Logic,
			Weight = this.Weight,
			Absolute = this.Absolute
		};
	}

	[DataContract]
	public class ItemAward : ICloneable {
		[DataMember]
		public DfLevelObjects.Difficulties Difficulty { get; set; }
		[DataMember]
		public string Logic { get; set; }
		[DataMember]
		public int Count { get; set; }

		object ICloneable.Clone() => this.Clone();
		public ItemAward Clone() => new() {
			Difficulty = this.Difficulty,
			Logic = this.Logic,
			Count = this.Count
		};

		public int DifficultyDropdown {
			get => this.Difficulty switch {
				DfLevelObjects.Difficulties.Easy => 0,
				DfLevelObjects.Difficulties.EasyMedium => 1,
				DfLevelObjects.Difficulties.EasyMediumHard => 2,
				DfLevelObjects.Difficulties.MediumHard => 3,
				DfLevelObjects.Difficulties.Hard => 4,
				_ => throw new NotImplementedException()
			};
			set => this.Difficulty = value switch {
				0 => DfLevelObjects.Difficulties.Easy,
				1 => DfLevelObjects.Difficulties.EasyMedium,
				2 => DfLevelObjects.Difficulties.EasyMediumHard,
				3 => DfLevelObjects.Difficulties.MediumHard,
				4 => DfLevelObjects.Difficulties.Hard,
				_ => throw new NotImplementedException()
			};
		}
	}

	public class ObjectTemplate : ICloneable {
		public string Logic { get; set; }
		public string Filename { get; set; }

		object ICloneable.Clone() => this.Clone();
		public ObjectTemplate Clone() => new() {
			Logic = this.Logic,
			Filename = this.Filename
		};
	}

	public enum MultiLogicActions {
		[DataMember(Name = "Keep")]
		Keep,
		[DataMember(Name = "Remove")]
		Remove,
		[DataMember(Name = "Shuffle")]
		Shuffle
	}

	public enum SpawnSources {
		[DataMember(Name = "Existing, then random")]
		ExistingThenRandom,
		[DataMember(Name = "Only random")]
		OnlyRandom,
		[DataMember(Name = "Existing and random")]
		ExistingAndRandom
	}

	public class RandomizerObjectSettings : ICloneable {
		public bool RandomizeEnemies { get; set; } = true;
		public string[] LogicsForEnemySpawnLocationPool { get; set; } = Array.Empty<string>();
		public MultiLogicActions MultiLogicEnemyAction { get; set; } = MultiLogicActions.Shuffle;
		public EntityGenerationPoolSources EnemyGenerationPoolSource { get; set; }
		public bool RandomizeBosses { get; set; } = true;
		public SpawnSources EnemySpawnSources { get; set; }
		public RandomLocationSelectionModes RandomEnemyLocationSelectionMode { get; set; }
		public bool SpawnDiagonasOnlyInWater { get; set; } = true;
		public bool SpawnOnlyFlyingAndDiagonasInWater { get; set; } = true;
		public bool SpawnOnlyFlyingOverPits { get; set; } = true;
		public bool RandomizeEnemyYaw { get; set; }
		public bool LessenEnemyProbabilityWhenSpawned { get; set; } = true;
		public List<DifficultySpawnWeight> DifficultyEnemySpawnWeights { get; set; } = new List<DifficultySpawnWeight>();
		public List<LogicSpawnWeight> EnemyLogicSpawnWeights { get; set; } = new List<LogicSpawnWeight>();
		public RandomRange RandomizeGeneratorsDelay { get; set; } = new RandomRange();
		public RandomRange RandomizeGeneratorsInterval { get; set; } = new RandomRange();
		public RandomRange RandomizeGeneratorsMinimumDistance { get; set; } = new RandomRange();
		public RandomRange RandomizeGeneratorsMaximumDistance { get; set; } = new RandomRange();
		public RandomRange RandomizeGeneratorsMaximumAlive { get; set; } = new RandomRange();
		public RandomRange RandomizeGeneratorsNumberTerminate { get; set; } = new RandomRange();
		public RandomRange RandomizeGeneratorsWanderTime { get; set; } = new RandomRange();
		public ObjectTemplate[] DefaultLogicFiles { get; set; }

		public bool ReplaceKeyAndCodeOfficersWithTheirItems { get; set; }
		public bool UnlockAllDoorsAndIncludeKeysInSpawnLocationPool { get; set; }

		public bool NightmareMode { get; set; }
		public bool NightmareKeepOriginalEnemies { get; set; }
		public RandomRange NightmareGeneratorsDelay { get; set; } = new RandomRange();
		public RandomRange NightmareGeneratorsInterval { get; set; } = new RandomRange();
		public RandomRange NightmareGeneratorsMinimumDistance { get; set; } = new RandomRange();
		public RandomRange NightmareGeneratorsMaximumDistance { get; set; } = new RandomRange();
		public RandomRange NightmareGeneratorsMaximumAlive { get; set; } = new RandomRange();
		public RandomRange NightmareGeneratorsNumberTerminate { get; set; } = new RandomRange();
		public RandomRange NightmareGeneratorsWanderTime { get; set; } = new RandomRange();

		public bool RandomizeItems { get; set; } = true;
		public string[] LogicsForItemSpawnLocationPool { get; set; } = Array.Empty<string>();
		public EntityGenerationPoolSources ItemGenerationPoolSource { get; set; }
		public SpawnSources ItemSpawnSources { get; set; }
		public RandomLocationSelectionModes RandomItemLocationSelectionMode { get; set; }
		public bool SpawnItemsInWater { get; set; }
		public bool SpawnItemsInPits { get; set; }
		public bool LessenItemProbabilityWhenSpawned { get; set; } = true;
		public bool RemoveCheckpoints { get; set; }
		public List<DifficultySpawnWeight> DifficultyItemSpawnWeights { get; set; } = new List<DifficultySpawnWeight>();
		public List<LogicSpawnWeight> ItemLogicSpawnWeights { get; set; } = new List<LogicSpawnWeight>();
		public List<ItemAward> ItemAwardFirstLevel { get; set; } = new List<ItemAward>();
		public List<ItemAward> ItemAwardOtherLevels { get; set; } = new List<ItemAward>();

		object ICloneable.Clone() => this.Clone();
		public RandomizerObjectSettings Clone() => new() {
			DefaultLogicFiles = this.DefaultLogicFiles.Select(x => x.Clone()).ToArray(),
			DifficultyEnemySpawnWeights = this.DifficultyEnemySpawnWeights.Select(x => x.Clone()).ToList(),
			DifficultyItemSpawnWeights = this.DifficultyItemSpawnWeights.Select(x => x.Clone()).ToList(),
			EnemyGenerationPoolSource = this.EnemyGenerationPoolSource,
			EnemyLogicSpawnWeights = this.EnemyLogicSpawnWeights.Select(x => x.Clone()).ToList(),
			MultiLogicEnemyAction = this.MultiLogicEnemyAction,
			ItemAwardFirstLevel = this.ItemAwardFirstLevel.Select(x => x.Clone()).ToList(),
			ItemAwardOtherLevels = this.ItemAwardOtherLevels.Select(x => x.Clone()).ToList(),
			ItemGenerationPoolSource = this.ItemGenerationPoolSource,
			ItemLogicSpawnWeights = this.ItemLogicSpawnWeights.Select(x => x.Clone()).ToList(),
			LessenEnemyProbabilityWhenSpawned = this.LessenEnemyProbabilityWhenSpawned,
			LessenItemProbabilityWhenSpawned = this.LessenItemProbabilityWhenSpawned,
			LogicsForEnemySpawnLocationPool = this.LogicsForEnemySpawnLocationPool.ToArray(),
			LogicsForItemSpawnLocationPool = this.LogicsForItemSpawnLocationPool.ToArray(),
			NightmareGeneratorsDelay = this.NightmareGeneratorsDelay.Clone(),
			NightmareGeneratorsInterval = this.NightmareGeneratorsInterval.Clone(),
			NightmareGeneratorsMaximumAlive = this.NightmareGeneratorsMaximumAlive.Clone(),
			NightmareGeneratorsMaximumDistance = this.NightmareGeneratorsMaximumDistance.Clone(),
			NightmareGeneratorsMinimumDistance = this.NightmareGeneratorsMinimumDistance.Clone(),
			NightmareGeneratorsNumberTerminate = this.NightmareGeneratorsNumberTerminate.Clone(),
			NightmareGeneratorsWanderTime = this.NightmareGeneratorsWanderTime.Clone(),
			NightmareKeepOriginalEnemies = this.NightmareKeepOriginalEnemies,
			NightmareMode = this.NightmareMode,
			RandomEnemyLocationSelectionMode = this.RandomEnemyLocationSelectionMode,
			RandomItemLocationSelectionMode = this.RandomItemLocationSelectionMode,
			RandomizeBosses = this.RandomizeBosses,
			RandomizeEnemies = this.RandomizeEnemies,
			RandomizeEnemyYaw = this.RandomizeEnemyYaw,
			RandomizeGeneratorsDelay = this.RandomizeGeneratorsDelay.Clone(),
			RandomizeGeneratorsInterval = this.RandomizeGeneratorsInterval.Clone(),
			RandomizeGeneratorsMaximumAlive = this.RandomizeGeneratorsMaximumAlive.Clone(),
			RandomizeGeneratorsMaximumDistance = this.RandomizeGeneratorsMaximumDistance.Clone(),
			RandomizeGeneratorsMinimumDistance = this.RandomizeGeneratorsMinimumDistance.Clone(),
			RandomizeGeneratorsNumberTerminate = this.RandomizeGeneratorsNumberTerminate.Clone(),
			RandomizeGeneratorsWanderTime = this.RandomizeGeneratorsWanderTime.Clone(),
			RandomizeItems = this.RandomizeItems,
			RemoveCheckpoints = this.RemoveCheckpoints,
			ReplaceKeyAndCodeOfficersWithTheirItems = this.ReplaceKeyAndCodeOfficersWithTheirItems,
			SpawnDiagonasOnlyInWater = this.SpawnDiagonasOnlyInWater,
			SpawnItemsInPits = this.SpawnItemsInPits,
			SpawnItemsInWater = this.SpawnItemsInWater,
			SpawnOnlyFlyingAndDiagonasInWater = this.SpawnOnlyFlyingAndDiagonasInWater,
			SpawnOnlyFlyingOverPits = this.SpawnOnlyFlyingOverPits,
			UnlockAllDoorsAndIncludeKeysInSpawnLocationPool = this.UnlockAllDoorsAndIncludeKeysInSpawnLocationPool,
			EnemySpawnSources = this.EnemySpawnSources,
			ItemSpawnSources = this.ItemSpawnSources
		};
	}
}
