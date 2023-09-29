using MZZT.DarkForces.Converters;
using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace MZZT.DarkForces.Showcase {
	public class Randomizer : Singleton<Randomizer> {
		private const float X_OFFSET = 0f;

		private Random rng;

		[SerializeField]
		private DatabindObject settingsObject;
		[SerializeField]
		private SeedUi seedInput;

		private async void Start() {
			// This is here in case you run directly from this scene instead of the menu.
			if (!FileLoader.Instance.Gobs.Any()) {
				await FileLoader.Instance.LoadStandardFilesAsync();
			}

			await PauseMenu.Instance.BeginLoadingAsync();

			ResourceCache.Instance.ClearWarnings();

			if (await this.LoadSettingsAsync()) {
				await LevelLoader.Instance.ShowWarningsAsync("RNDMIZER.JSO");
			}

			await LevelLoader.Instance.LoadLevelListAsync(true);
			this.cutscenes = await FileLoader.Instance.LoadGobFileAsync<DfCutsceneList>("CUTSCENE.LST");

			await LevelLoader.Instance.ShowWarningsAsync(Mod.Instance.Gob ?? "DARK.GOB");

			PauseMenu.Instance.EndLoading();
		}

		private DfCutsceneList cutscenes;

		private async Task<bool> LoadSettingsAsync() {
			Stream stream = await FileLoader.Instance.GetGobFileStreamAsync("RNDMIZER.JSO");
			if (stream == null) {
				return false;
			}

			using (stream) {
				DataContractJsonSerializer serializer = new(typeof(RandomizerSettings), new DataContractJsonSerializerSettings() {
					UseSimpleDictionaryFormat = true
				});

				try {
					this.Settings = (RandomizerSettings)serializer.ReadObject(stream);
				} catch (Exception ex) {
					Debug.LogException(ex);
					ResourceCache.Instance.AddError("RNDMIZER.JSO", ex);
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Loads in all level data required to randomize the level.
		/// </summary>
		/// <param name="filename">The base name of the level from the GOB.</param>
		private async Task<bool> LoadLevelAsync(string filename) {
			filename = filename.ToLower();
			(DfLevelList.Level level, int index) = LevelLoader.Instance.LevelList.Levels
				.Select((x, i) => (x, i))
				.FirstOrDefault(x => x.x.FileName.ToLower() == filename);
			if (level == null) {
				return false;
			}

			await PauseMenu.Instance.BeginLoadingAsync();

			await LevelLoader.Instance.LoadLevelAsync(index);
			await LevelLoader.Instance.LoadInformationAsync();
			await LevelLoader.Instance.LoadColormapAsync();
			await LevelLoader.Instance.LoadPaletteAsync();
			await LevelLoader.Instance.LoadObjectsAsync();

			PauseMenu.Instance.EndLoading();
			return true;
		}

		/// <summary>
		/// Generates a new, random seed. The same seed with the same settings, in the same Showcase version, should produce the same randomized level.
		/// </summary>
		public void GenerateNewSeed() {
			this.Settings.Seed = new Random().Next();
			this.seedInput.Invalidate();
		}

		private float RandomizeRange(RandomRange range, float def = 0) {
			if (!range.Enabled) {
				return def;
			}

			if (range.Minimum == range.Maximum) {
				return range.Minimum;
			}

			return (float)this.rng.NextDouble() * (range.Maximum -
				range.Minimum) + range.Minimum;
		}
		private int RandomizeIntRange(RandomRange range, int def = 0) {
			if (!range.Enabled) {
				return def;
			}

			if (range.Minimum == range.Maximum) {
				return (int)range.Minimum;
			}

			return this.rng.Next((int)range.Minimum, (int)range.Maximum + 1);
		}

		/// <summary>
		/// Applies randomizer settings to JEDI.LVL.
		/// </summary>
		/// <returns>A randomized JEDI.LVL.</returns>
		private DfLevelList RandomizeJediLvl() {
			bool modified = false;

			RandomizerJediLvlSettings settings = this.Settings.JediLvl;
			if (settings.Levels.Length < 1) {
				throw new ArgumentException("You must select at least one level!");
			}

			if (LevelLoader.Instance.LevelList == null) {
				throw new FormatException("Can't read JEDI.LVL, unable to apply randomizer settings!");
			}
			
			// Strip out the levels the user doesn't want.
			DfLevelList levelList = LevelLoader.Instance.LevelList.Clone();
			for (int i = 0; i < levelList.Levels.Count; i++) {
				if (!settings.Levels.Contains(levelList.Levels[i].FileName.ToUpper())) {
					levelList.Levels.RemoveAt(i);
					modified = true;
					i--;
				}
			}

			if (levelList.Levels.Count < 1) {
				throw new ArgumentException("Could not find all of those levels in JEDI.LVL!");
			}

			int count = this.RandomizeIntRange(settings.LevelCount, levelList.Levels.Count);

			// Remove extra entries
			while (count < levelList.Levels.Count) {
				int index = this.rng.Next(levelList.Levels.Count);
				levelList.Levels.RemoveAt(index);
				modified = true;
			}

			if (settings.RandomizeOrder) {
				// Randomize order by creating a list of levels, and taking a random item out until the list is empty.
				List<DfLevelList.Level> pending = levelList.Levels.ToList();
				for (int i = 0; i < levelList.Levels.Count - 1; i++) {
					int index = this.rng.Next(pending.Count);
					levelList.Levels[i] = pending[index];
					pending.RemoveAt(index);
				}
				// When there's only one item left, there's no randomness needed.
				levelList.Levels[levelList.Levels.Count - 1] = pending[0];
				modified = true;
			}

			// Put the RNG seed in the level name in case the user selects a different GOB filename.
			if (!string.IsNullOrEmpty(settings.Title)) {
				levelList.Levels[0].DisplayName = settings.Title.Replace("{seed}", this.Settings.Seed.ToString("X8"));
				modified = true;
			}
			return modified ? levelList : null;
		}

		/// <summary>
		/// Customizes CUTSCENE.LST based on selected settings.
		/// </summary>
		/// <returns>A customized CUTSCENE.LST, or null if no customizations needed.</returns>
		private DfCutsceneList RandomizeCutscenes() {
			RandomizerCutscenesSettings settings = this.Settings.Cutscenes;
			if (!settings.RemoveCutscenes && !settings.AdjustCutsceneSpeed.Enabled &&
				settings.AdjustCutsceneMusicVolume == 1) {

				// If the settings are all defaults, nothing to change.
				return null;
			}

			// Copy the object so we don't modify the base object, in case it's used elsewhere later.
			DfCutsceneList cutscenes = this.cutscenes.Clone();
			if (settings.RemoveCutscenes) {
				cutscenes.Cutscenes.Clear();
			} else {
				foreach (DfCutsceneList.Cutscene cutscene in cutscenes.Cutscenes.Values) {
					float speed = this.RandomizeRange(settings.AdjustCutsceneSpeed, 1);
					cutscene.Speed = Mathf.RoundToInt(Mathf.Clamp(cutscene.Speed * speed, 5, 20));
					cutscene.Volume = Mathf.RoundToInt(cutscene.Volume * settings.AdjustCutsceneMusicVolume);
				}
			}

			return cutscenes;
		}

		/// <summary>
		/// Randomize the music in the GOB.
		/// </summary>
		/// <param name="gob">The GOB file to add the music files to.</param>
		private async Task RandomizeMusicAsync(DfGobContainer gob) {
			RandomizerMusicSettings settings = this.Settings.Music;
			if (!settings.RandomizeTrackOrder) {
				// Use the music files from the base GOB or SOUNDS.GOB.
				return;
			}

			int trackCount = LevelLoader.Instance.LevelList.Levels.Count;
			int levelCount = this.Settings.JediLvl.Levels.Length;

			// Create a list of track numbers, and pull out random ones for each level.
			List<int> remaining = Enumerable.Range(0, trackCount).ToList();

			for (int i = 0; i < levelCount; i++) {
				int track;
				// If we're out of tracks, duplicate random ones.
				if (remaining.Count == 0) {
					track = this.rng.Next(trackCount);
				} else {
					int next = this.rng.Next(remaining.Count);
					track = remaining[next];
					remaining.RemoveAt(next);
				}

				// Only replace the music file if it's different.
				if (track != i) {
					using (Stream stream = await FileLoader.Instance.GetGobFileStreamAsync($"STALK-{track + 1:00}.GMD")) {
						await gob.AddFileAsync($"STALK-{i + 1:00}.GMD", stream);
					}

					using (Stream stream = await FileLoader.Instance.GetGobFileStreamAsync($"FIGHT-{track + 1:00}.GMD")) {
						await gob.AddFileAsync($"FIGHT-{i + 1:00}.GMD", stream);
					}
				}
			}
		}

		private RgbColor AdjustColor(RgbColor color, float hueShift, float saturationScale, float valueScale) {
			float vsu = valueScale * saturationScale * Mathf.Cos(hueShift * Mathf.PI / 180);
			float vsw = valueScale * saturationScale * Mathf.Sin(hueShift * Mathf.PI / 180);

			return new RgbColor() {
				R = (byte)Mathf.Clamp(
					(.299f * valueScale + .701f * vsu + .168f * vsw) * color.R +
					(.587f * valueScale - .587f * vsu + .330f * vsw) * color.G +
					(.114f * valueScale - .114f * vsu - .497f * vsw) * color.B,
				0, 63),
				G = (byte)Mathf.Clamp(
					(.299f * valueScale - .299f * vsu - .328f * vsw) * color.R +
					(.587f * valueScale + .413f * vsu + .035f * vsw) * color.G +
					(.114f * valueScale + .886f * vsu - .203f * vsw) * color.B,
				0, 63),
				B = (byte)Mathf.Clamp(
					(.299f * valueScale - .3f * vsu + 1.25f * vsw) * color.R +
					(.587f * valueScale - .588f * vsu - 1.05f * vsw) * color.G +
					(.114f * valueScale + .886f * vsu - .203f * vsw) * color.B,
				0, 63)
			};
		}

		private bool palGeneratedGlobalValues;
		private float palHueShift;
		private float palSaturationScale;
		private float palValueScale;
		/// <summary>
		/// Modify the PAL file based on settings.
		/// </summary>
		/// <returns>Modified PAL file, or null if no modifications necessary.</returns>
		private DfPalette RandomizeLevelPalette() {
			RandomizerPaletteSettings settings = this.Settings.Palette;
			if ((!settings.RandomizeLightColors && !settings.RandomizeOtherColors) ||
				(!settings.LightHue.Enabled && !settings.LightLum.Enabled &&
				!settings.LightSat.Enabled)) {

				// No modifications needed.
				return null;
			}

			// Make a copy to modify.
			DfPalette pal = LevelLoader.Instance.Palette.Clone();

			// Calculate the amount to adjust the colors.
			if (!settings.RandomizePerLevel && !this.palGeneratedGlobalValues) {
				this.palGeneratedGlobalValues = true;
				this.palHueShift = this.RandomizeRange(settings.LightHue, 0);
				this.palSaturationScale = this.RandomizeRange(settings.LightSat, 1);
				this.palValueScale = this.RandomizeRange(settings.LightLum, 1);
			}
			float hueShift;
			float saturationScale;
			float valueScale;
			if (!settings.RandomizePerLevel) {
				hueShift = this.palHueShift;
				saturationScale = this.palSaturationScale;
				valueScale = this.palValueScale;
			} else {
				hueShift = this.RandomizeRange(settings.LightHue, 0);
				saturationScale = this.RandomizeRange(settings.LightSat, 1);
				valueScale = this.RandomizeRange(settings.LightLum, 1);
			}

			if (settings.RandomizeLightColors) {
				for (int i = 0; i < 32; i++) {
					pal.Palette[i] = this.AdjustColor(pal.Palette[i], hueShift, saturationScale, valueScale);
				}
			}
			if (settings.RandomizeOtherColors) {
				for (int i = 32; i < 256; i++) {
					pal.Palette[i] = this.AdjustColor(pal.Palette[i], hueShift, saturationScale, valueScale);
				}
			}

			return pal;
		}

		private bool cmpGeneratedGlobalValues;
		private int cmpForceLightLevel;
		private int cmpHeadlightDistance;
		private int cmpHeadlightBrightness;
		/// <summary>
		/// Customize the CMP based no user settings.
		/// </summary>
		/// <returns>The modified CMP, or null if no modiifcations needed.</returns>
		private DfColormap RandomizeLevelColormap() {
			RandomizerColormapSettings settings = this.Settings.Colormap;
			if (!settings.HeadlightBrightness.Enabled && !settings.HeadlightDistance.Enabled &&
				!settings.ForceLightLevel.Enabled) {

				// No modifications needed.
				return null;
			}

			// Make a copy to modify.
			DfColormap cmp = LevelLoader.Instance.ColorMap.Clone();

			if (!settings.RandomizePerLevel && !this.cmpGeneratedGlobalValues) {
				this.cmpGeneratedGlobalValues = true;

				this.cmpForceLightLevel = this.RandomizeIntRange(settings.ForceLightLevel, -1);
				this.cmpHeadlightDistance = this.RandomizeIntRange(settings.HeadlightDistance, -1);
				this.cmpHeadlightBrightness = this.RandomizeIntRange(settings.HeadlightBrightness, -1);
			}
			int forceLightLevel;
			int headlightDistance;
			int headlightBrightness;
			if (!settings.RandomizePerLevel) {
				forceLightLevel = this.cmpForceLightLevel;
				headlightDistance = this.cmpHeadlightDistance;
				headlightBrightness = this.cmpHeadlightBrightness;
			} else {
				forceLightLevel = this.RandomizeIntRange(settings.ForceLightLevel, -1);
				headlightDistance = this.RandomizeIntRange(settings.HeadlightDistance, -1);
				headlightBrightness = this.RandomizeIntRange(settings.HeadlightBrightness, -1);
			}

			// Copy the light level entry to all other light levels.
			// This makes it look like the light in the map is always at that light level.
			// However enemy AI (how easily it can see you depending on light) is unaffected.
			if (forceLightLevel >= 0) {
				for (int i = 0; i < 32; i++) {
					if (forceLightLevel == i) {
						continue;
					}
					Buffer.BlockCopy(cmp.PaletteMaps[forceLightLevel], 0, cmp.PaletteMaps[i], 0, 256);
				}
			}

			// Generate headlight values
			if (headlightDistance >= 0 || headlightBrightness >= 0) {
				if (headlightDistance < 0) {
					headlightDistance = 127;
				}
				if (headlightBrightness < 0) {
					headlightBrightness = 0;
				}

				for (int i = 0; i <= headlightDistance; i++) {
					cmp.HeadlightLightLevels[i] = (byte)Mathf.RoundToInt(Mathf.Lerp(headlightBrightness, 0x1F,
						(float)i / headlightDistance));
				}
			}

			return cmp;
		}

		private bool levGeneratedGlobalValues;
		private float levLightLevelMultiplier;
		/// <summary>
		/// Customize the LEV based on the user's preferences.
		/// </summary>
		/// <returns>The customized LEV or null if no customizations needed.</returns>
		private DfLevel RandomizeLevel(bool alwaysGenerate) {
			RandomizerLevelSettings settings = this.Settings.Level;
			if (!alwaysGenerate && settings.MapOverrideMode == MapOverrideModes.None && !settings.LightLevelMultiplier.Enabled &&
				!settings.RemoveSecrets) {

				// No modification needed.
				return null;
			}

			DfLevel level = LevelLoader.Instance.Level.Clone();

			// Change the map flags on all walls.
			switch (settings.MapOverrideMode) {
				case MapOverrideModes.HideMap:
					foreach (DfLevel.Wall wall in level.Sectors.SelectMany(x => x.Walls)) {
						// Force the hidden on map flag.
						wall.TextureAndMapFlags = wall.TextureAndMapFlags & ~(DfLevel.WallTextureAndMapFlags.DoorOnMap |
							DfLevel.WallTextureAndMapFlags.LedgeOnMap | DfLevel.WallTextureAndMapFlags.NormalOnMap) |
							DfLevel.WallTextureAndMapFlags.HiddenOnMap;
					}
					break;
				case MapOverrideModes.RemoveOverrides:
					foreach (DfLevel.Wall wall in level.Sectors.SelectMany(x => x.Walls)) {
						// Remove all map override flags.
						wall.TextureAndMapFlags &= ~(DfLevel.WallTextureAndMapFlags.DoorOnMap |
							DfLevel.WallTextureAndMapFlags.LedgeOnMap | DfLevel.WallTextureAndMapFlags.NormalOnMap |
							DfLevel.WallTextureAndMapFlags.HiddenOnMap);
					}
					break;
			}

			// Modify light levels based on a multiplier.
			// This just affedcts the LEV and doesn't try to adjust INF elevator lights.
			// If this would be added, elevator light speed and stops would need to be multiplied.
			// Possibly stops should not be clamped to keep travel times constant... experiementation is needed to see if that would work.
			if (!settings.LightLevelMultiplierPerLevel && !this.levGeneratedGlobalValues) {
				this.levGeneratedGlobalValues = true;
				this.levLightLevelMultiplier = this.RandomizeRange(settings.LightLevelMultiplier, 1);
			}

			if (settings.LightLevelMultiplier.Enabled) {
				float factor;
				if (!settings.LightLevelMultiplierPerLevel) {
					factor = this.levLightLevelMultiplier;
				} else {
					factor = this.RandomizeRange(settings.LightLevelMultiplier, 1);
				}

				foreach (DfLevel.Sector sector in level.Sectors) {
					int newLightLevel = Mathf.RoundToInt(Mathf.Clamp(sector.LightLevel * factor, 0, 31));

					foreach (DfLevel.Wall wall in sector.Walls) {
						int lightLevel = sector.LightLevel + wall.LightLevel;

						lightLevel = Mathf.RoundToInt(Mathf.Clamp(lightLevel * factor, -31, 31));
						wall.LightLevel = (short)(lightLevel - newLightLevel);
					}

					sector.LightLevel = newLightLevel;
					sector.AltLightLevel = Mathf.RoundToInt(Mathf.Clamp(sector.AltLightLevel * factor, 0, 31));
				}
			}

			if (settings.RemoveSecrets) {
				foreach (DfLevel.Sector sector in level.Sectors) {
					sector.Flags &= ~DfLevel.SectorFlags.Secret;
				}
			}

			return level;
		}

		/// <summary>
		/// All logics which can signal a boss elevator when BOSS: true is specified
		/// </summary>
		private static readonly string[] BOSS_LOGICS = new[] {
			"BOBA_FETT",
			"KELL",
			"D_TROOP1",
			"D_TROOP2"
		};

		/// <summary>
		/// All logics which can signal a mohc elevator when BOSS: true is specified
		/// </summary>
		private static readonly string[] MOHC_LOGICS = new[] {
			"D_TROOP3"
		};

		/// <summary>
		/// Logics intended to swim in water.
		/// </summary>
		private static readonly string[] WATER_LOGICS = new[] {
			"SEWER1",
			"GENERATOR SEWER1"
		};

		/// <summary>
		/// Logics which can fly.
		/// </summary>
		private static readonly string[] FLYING_LOGICS = new[] {
			"INT_DROID",
			"PROBE_DROID",
			"REMOTE",
			"GENERATOR INT_DROID",
			"GENERATOR PROBE_DROID",
			"GENERATOR REMOTE"
		};

		/// <summary>
		/// Logics which are attached to a separate base object which should be created.
		/// </summary>
		private static readonly string[] ATTACHED_LOGICS = new[] {
			"TURRET",
			"WELDER"
		};

		// If the campaign pool is used, these objects will store the pool for the whole campaign.
		// It's not necessary to compute them more than once.
		Dictionary<string[], List<DfLevelObjects.Object>> enemySpawnPool =
			new();
		Dictionary<string[], List<DfLevelObjects.Object>> bossSpawnPool =
			new();
		Dictionary<string[], List<DfLevelObjects.Object>> itemSpawnPool =
			new();
		/// <summary>
		/// Indexes all the objects of a type (enemy, boss, item) in a level for use in the randomized pool of objects..
		/// </summary>
		/// <param name="spawnPool">The pool to add to.</param>
		/// <param name="spawnLocationPoolLogics">Objects of these logic tpyes will be added to the pool.</param>
		/// <param name="anyBoss">Whether or not we have a boss elevator.</param>
		/// <param name="anyMohc">Whether or not we have a mohc elevator.</param>
		/// <param name="o">The O file for the level.</param>
		/// <param name="findBosses">The value of the BOSS flag on the objects to look for.</param>
		private void AddLevelToSpawnMap(Dictionary<string[], List<DfLevelObjects.Object>> spawnPool,
			IEnumerable<string> spawnLocationPoolLogics, bool anyBoss, bool anyMohc,
			DfLevelObjects o, bool findBosses, bool findItems) {

			RandomizerObjectSettings settings = this.Settings.Object;

			Regex officerGenerator = new(@"^GENERATOR\s+I_OFFICER(\w)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
			foreach (DfLevelObjects.Object obj in o.Objects) {
				// Grab all the properties of the object's sequence block.
				Dictionary<string, string[]> properties = this.GetLogicProperties(obj);
				if (properties == null) {
					continue;
				}

				// Get all LOGIC and TYPE entries.
				properties.TryGetValue("LOGIC", out string[] logics);
				properties.TryGetValue("TYPE", out string[] types);
				// Get the list of logics this object has by merging the two lists.
				logics = (logics ?? Enumerable.Empty<string>()).Concat(types ?? Enumerable.Empty<string>())
					.Select(x => {
						x = x.ToUpper();
						// Remove any ITEM prefix on item logics.
						if (x.StartsWith("ITEM ")) {
							x = x.Substring(5).TrimStart();
						}
						return x;
					})
					// Ignore ANIM logic, we don't want to differentiate it.
					.Where(x => x != "ANIM")
					// Make the ordering deterministic.
					.OrderBy(x => x)
					.ToArray();

				// Remove enemies with keys and replace with the actual keys.
				if (settings.ReplaceKeyAndCodeOfficersWithTheirItems) {
					bool any = false;
					foreach ((char c, string logic) in new Dictionary<char, string>() {
						['R'] = "RED",
						['Y'] = "YELLOW",
						['B'] = "BLUE",
						['1'] = "CODE1",
						['2'] = "CODE2",
						['3'] = "CODE3",
						['4'] = "CODE4",
						['5'] = "CODE5",
						['6'] = "CODE6",
						['7'] = "CODE7",
						['8'] = "CODE8",
						['9'] = "CODE9"
					}) {
						if (logics.Contains($"I_OFFICER{c}") || logics.Contains($"GENERATOR I_OFFICER{c}")) {
							any = true;
							break;
						}
					}
					if (any) {
						continue;
					}
				}

				// If there's no logics or no logics we care about, leave this object alone.
				if (logics.Length <= 0 || !logics.Intersect(spawnLocationPoolLogics).Any()) {
					continue;
				}

				if (!findItems) {
					bool boss = false;
					if (properties.TryGetValue("BOSS", out string[] bosses)) {
						boss = bosses.Any(x => x.ToUpper() == "TRUE");
					}

					// Ignore bosses or only consider bosses, depending on the flag.
					if (findBosses) {
						if (!boss || ((!logics.Intersect(BOSS_LOGICS).Any() || !anyBoss) &&
							(!logics.Intersect(MOHC_LOGICS).Any() || !anyMohc))) {

							continue;
						}
					} else {
						if (boss && logics.Intersect(BOSS_LOGICS).Any() && anyBoss) {
							continue;
						}

						if (boss && logics.Intersect(MOHC_LOGICS).Any() && anyMohc) {
							continue;
						}
					}

					if (!findBosses) {
						// We only get here if we have a normal officer (other variants can't be selected from the UI).
						// However other variants can be added by other code if we disable keys and such.
						// So we should spawn those as normal officers.

						bool modifyLogic = false;
						for (int i = 0; i < logics.Length; i++) {
							if (logics[i].StartsWith("I_OFFICER")) {
								logics[i] = "I_OFFICER";
								modifyLogic = true;
							}
							if (officerGenerator.IsMatch(logics[i])) {
								logics[i] = "GENERATOR I_OFFICER";
								modifyLogic = true;
							}
						}
						logics = logics.OrderBy(x => x).Distinct().ToArray();

						if (modifyLogic) {
							properties.Remove("TYPE");
							properties["LOGIC"] = logics;
							obj.Logic = string.Join(Environment.NewLine, properties.SelectMany(x => x.Value.Select(y => $"{x.Key}: {y}")));
						}
					}

					if (settings.MultiLogicEnemyAction == MultiLogicActions.Keep && logics.Length > 1) {
						continue;
					}
				}

				// Create a new pool for this set of logics.
				string[] key = spawnPool.Keys.FirstOrDefault(x => x.SequenceEqual(logics));
				if (key == null) {
					key = logics;
					spawnPool[key] = new List<DfLevelObjects.Object>();
				}
				spawnPool[key].Add(obj);
			}
		}

		// https://stackoverflow.com/questions/2049582/how-to-determine-if-a-point-is-in-a-2d-triangle
		private float GetLineSide(System.Numerics.Vector2 p1, System.Numerics.Vector2 p2, System.Numerics.Vector2 p3) {
			return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
		}

		private bool PointInTriangle(System.Numerics.Vector2 pt, System.Numerics.Vector2 v1,
			System.Numerics.Vector2 v2, System.Numerics.Vector2 v3) {

			// v1, v2, v3 must be clockwise from the perspective of coordinates increasing down and to the right,
			// otherwise this function always returns false.

			float d1, d2, d3;
			bool has_neg, has_pos;

			d1 = this.GetLineSide(pt, v1, v2);
			d2 = this.GetLineSide(pt, v2, v3);
			d3 = this.GetLineSide(pt, v3, v1);

			has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
			has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

			return !(has_neg && has_pos);
		}

		/// <summary>
		/// Split the lines and then split "key: value" pairs.
		/// Then group by the keys (in case of multiple LOGIC keys).
		/// </summary>
		/// <param name="obj">The object to get the properties of.</param>
		/// <returns>The lookup for properties and value(s).</returns>
		private Dictionary<string, string[]> GetLogicProperties(DfLevelObjects.Object o) =>
			o.Logic?.Split('\r', '\n')
				.SelectMany(x => TextBasedFile.SplitKeyValuePairs(TextBasedFile.TokenizeLine(x)))
				.Select(x => (x.Key.ToUpper(), string.Join(" ", x.Value)))
				.GroupBy(x => x.Item1)
				.ToDictionary(x => x.Key, x => x.Select(x => x.Item2).ToArray());

		/// <summary>
		/// Randomize object placements in a level.
		/// </summary>
		/// <returns>The modified O file, or null if there is no randomization to be done.</returns>
		private async Task<(DfLevelObjects, DfLevelInformation)> RandomizeLevelObjectsAsync(bool alwaysGenerate) {
			RandomizerObjectSettings settings = this.Settings.Object;

			bool modifiedO = alwaysGenerate;
			bool modifiedInf = alwaysGenerate;

			DfLevelObjects o = LevelLoader.Instance.Objects.Clone();
			DfLevelInformation inf = LevelLoader.Instance.Information.Clone();

			// Determine if there are any boss elevator(s) in the level.
			// This is important for determineing if we can randomize a boss or not.
			// If there is no boss elevator, the boss will have no effect when killed, so we can randomize it.
			DfLevelInformation.Item[] bossElev = inf.Items
				.Where(x => x.Type == DfLevelInformation.ScriptTypes.Sector && x.SectorName?.ToUpper() == "BOSS").ToArray();
			DfLevelInformation.Item[] mohcElev = inf.Items
				.Where(x => x.Type == DfLevelInformation.ScriptTypes.Sector && x.SectorName?.ToUpper() == "MOHC").ToArray();

			// Cache bounds of level and sectors to more efficiently locate a sector containing a specific point.
			Rect levelBounds = default;
			Dictionary<DfLevel.Sector, Rect> sectorBounds = new();
			// Cache the generated floor/ceiling tris for each sector so we only have to generate them once.
			Dictionary<DfLevel.Sector, int[]> tris = new();

			// Use this regex to detect elevator sectors, but only types that change the sector geometry.
			// We don't want to put enemies or items in these sectors since it's easy to break a sector
			// With a first step giving it a small height
			// (placing an enemy/item causes this stop to fail and it will be in the open position).
			Regex elevator = new(@"^\s*class:\s*elevator\s*(basic|inv|move_floor|move_ceiling|basic_auto|morph_move1|morph_move2|morph_spin1|morph_spin2|move_wall|rotate_wall|door|door_mid|door_inv)\b", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

			Dictionary<string, string> templates = settings.DefaultLogicFiles.ToDictionary(x => x.Logic, x => x.Filename);

			if (settings.RandomizeEnemies) {
				// If we are unlocking all doors, include imperial officers with keys in the list of logics to remove.
				List<string> spawnLocationPoolLogics = settings.LogicsForEnemySpawnLocationPool.ToList();
				if (settings.UnlockAllDoorsAndIncludeKeysInSpawnLocationPool &&
					spawnLocationPoolLogics.Contains("I_OFFICER")) {

					spawnLocationPoolLogics.AddRange(new[] {
						"I_OFFICERR",
						"I_OFFICERY",
						"I_OFFICERB",
					});
				}
				if (settings.UnlockAllDoorsAndIncludeKeysInSpawnLocationPool &&
					spawnLocationPoolLogics.Contains("GENERATOR I_OFFICER")) {

					spawnLocationPoolLogics.AddRange(new[] {
						"GENERATOR I_OFFICERR",
						"GENERATOR I_OFFICERY",
						"GENERATOR I_OFFICERB",
					});
				}

				// The pool of objects we'll select from to spawn.
				Dictionary<string[], List<DfLevelObjects.Object>> spawnPool =
					new();
				// Existing locations we removed existing spawned objects from, which we can spawn new objects in.
				List<(System.Numerics.Vector3 position, System.Numerics.Vector3 rotation)> existingSpawns =
					new();
				// Find all objects we're interested in adding to the spawn pool.
				foreach (DfLevelObjects.Object obj in o.Objects) {
					// Grab all sequences from the object.
					Dictionary<string, string[]> properties = this.GetLogicProperties(obj);
					if (properties == null) {
						// No sequences, so we can't have a logic match.
						continue;
					}

					// Get all logics assigned to this object.
					properties.TryGetValue("LOGIC", out string[] logics);
					properties.TryGetValue("TYPE", out string[] types);
					logics = (logics ?? Enumerable.Empty<string>()).Concat(types ?? Enumerable.Empty<string>())
						.Select(x => x.ToUpper()).ToArray();

					// Remove enemies with keys and replace with the actual keys.
					if (settings.ReplaceKeyAndCodeOfficersWithTheirItems) {
						bool any = false;
						foreach ((char c, string logic) in new Dictionary<char, string>() {
							['R'] = "RED",
							['Y'] = "YELLOW",
							['B'] = "BLUE",
							['1'] = "CODE1",
							['2'] = "CODE2",
							['3'] = "CODE3",
							['4'] = "CODE4",
							['5'] = "CODE5",
							['6'] = "CODE6",
							['7'] = "CODE7",
							['8'] = "CODE8",
							['9'] = "CODE9"
						}) {
							if (logics.Contains($"I_OFFICER{c}") || logics.Contains($"GENERATOR I_OFFICER{c}")) {
								string filename = templates[logic];
								DfLevelObjects.ObjectTypes type;
								switch (Path.GetExtension(filename)) {
									case ".FME":
										type = DfLevelObjects.ObjectTypes.Frame;
										break;
									case ".WAX":
										type = DfLevelObjects.ObjectTypes.Sprite;
										break;
									case ".3DO":
										type = DfLevelObjects.ObjectTypes.ThreeD;
										break;
									default:
										type = DfLevelObjects.ObjectTypes.Spirit;
										filename = null;
										break;
								}

								obj.FileName = filename;
								obj.Logic = $"LOGIC: {logic}";
								obj.Type = type;
								any = true;
								break;
							}
						}
						if (any) {
							continue;
						}
					}

					// Only look for logics we're interested in.
					if (logics.Length <= 0 || !logics.Intersect(spawnLocationPoolLogics).Any()) {
						continue;
					}

					// We may want to ignore enemies with multiple logics.
					if (settings.MultiLogicEnemyAction == MultiLogicActions.Keep && logics.Length > 1) {
						continue;
					}

					// Is this a boss?
					bool boss = false;
					if (properties.TryGetValue("BOSS", out string[] bosses)) {
						boss = bosses.Any(x => x.ToUpper() == "TRUE");
					}

					// If there's no boss elevator it doesn't matter if it's a boss.
					if (boss && logics.Intersect(BOSS_LOGICS).Any() && bossElev.Length > 0) {
						continue;
					}

					if (boss && logics.Intersect(MOHC_LOGICS).Any() && mohcElev.Length > 0) {
						continue;
					}

					// Ignore special officer types for the purposes of grouping by logic.
					bool modifyLogic = false;
					for (int i = 0; i < logics.Length; i++) {
						if (logics[i].StartsWith("I_OFFICER")) {
							logics[i] = "I_OFFICER";
							modifyLogic = true;
						}
						if (logics[i].StartsWith("GENERATOR I_OFFICER")) {
							logics[i] = "GENERATOR I_OFFICER";
							modifyLogic = true;
						}
					}
					logics = logics.OrderBy(x => x).Distinct().ToArray();

					if (modifyLogic) {
						properties.Remove("TYPE");
						properties["LOGIC"] = logics;
						obj.Logic = string.Join(Environment.NewLine, properties.SelectMany(x => x.Value.Select(y => $"{x.Key}: {y}")));
					}

					// Add this object location to the spawn location pool.
					existingSpawns.Add((obj.Position, obj.EulerAngles));
					if (settings.MultiLogicEnemyAction == MultiLogicActions.Remove && logics.Length > 1) {
						continue;
					}

					// Add this object to the spawn pool.
					if (!spawnPool.TryGetValue(logics, out List<DfLevelObjects.Object> objects)) {
						spawnPool[logics] = objects = new List<DfLevelObjects.Object>();
					}
					objects.Add(obj);
				}

				// Find bases that belong to turrets/welding arms.
				// Existing bases should be removed, and when spawning replacements we should also spawn bases for them.
				List<DfLevelObjects.Object> pairedBases = new();
				DfLevelObjects.Object[] turrets = spawnPool.Where(x => x.Key.Contains("TURRET"))
					.SelectMany(x => x.Value).ToArray();
				List<DfLevelObjects.Object> bases = o.Objects
					.Where(x => x.Type == DfLevelObjects.ObjectTypes.ThreeD && x.FileName.ToUpper() == "BASE.3DO")
					.ToList();
				foreach (DfLevelObjects.Object turret in turrets) {
					// Find the closest base for each turret.
					DfLevelObjects.Object closest = bases.OrderBy(x => (x.Position - turret.Position).Length())
						.FirstOrDefault();
					// Bases are typically offset 1 unit Y from the turret. Don't remove one that's too far.
					if (closest != null && (closest.Position - turret.Position).Length() < 2) {
						pairedBases.Add(closest);
						bases.Remove(closest);
					}
				}
				DfLevelObjects.Object[] welders = spawnPool.Where(x => x.Key.Contains("WELDER"))
					.SelectMany(x => x.Value).ToArray();
				bases = o.Objects
					.Where(x => x.Type == DfLevelObjects.ObjectTypes.ThreeD && x.FileName.ToUpper() == "WELDBASE.3DO")
					.ToList();
				foreach (DfLevelObjects.Object welder in welders) {
					// Find the closest base for each welder.
					DfLevelObjects.Object closest = bases.OrderBy(x => (x.Position - welder.Position).Length())
						.FirstOrDefault();
					// Bases are typically offset 6 unit Y from the welder, also small X/Z offset?
					if (closest != null && (closest.Position - welder.Position).Length() < 8) {
						pairedBases.Add(closest);
						bases.Remove(closest);
					}
				}

				// Remove all objects we added to the spawn pool in preparation for adding replacements.
				foreach (DfLevelObjects.Object obj in spawnPool.SelectMany(x => x.Value).Concat(pairedBases)) {
					o.Objects.Remove(obj);
					modifiedO = true;
				}

				// How many objects to spawn for each difficulty?
				Dictionary<DfLevelObjects.Difficulties, int> difficultyCounts =
					spawnPool.SelectMany(x => x.Value).GroupBy(x => x.Difficulty).ToDictionary(x => x.Key, x => x.Count());
				// Match the original spawn rates, except adjust according to settings.
				foreach (DifficultySpawnWeight weight in settings.DifficultyEnemySpawnWeights) {
					difficultyCounts.TryGetValue(weight.Difficulty, out int count);
					if (weight.Absolute) {
						count = 1;
					}
					int total = Mathf.RoundToInt(count * weight.Weight);
					if (total <= 0) {
						difficultyCounts.Remove(weight.Difficulty);
					} else {
						difficultyCounts[weight.Difficulty] = total;
					}
				}

				// Where should we pull a list of enemies from?
				switch (settings.EnemyGenerationPoolSource) {
					case EntityGenerationPoolSources.CurrentLevel:
						// We already generated this list.
						break;
					case EntityGenerationPoolSources.SelectedLevels:
						// Pull from all selected levels.
						if (this.enemySpawnPool == null) {
							this.enemySpawnPool = spawnPool;

							foreach (string levelName in this.Settings.JediLvl.Levels
								.Where(x => x.ToUpper() != LevelLoader.Instance.CurrentLevelName.ToUpper())) {

								DfLevelObjects objects;
								try {
									objects = await FileLoader.Instance.LoadGobFileAsync<DfLevelObjects>($"{levelName}.O");
								} catch (Exception ex) {
									Debug.LogException(ex);
									continue;
								}
								this.AddLevelToSpawnMap(this.enemySpawnPool, spawnLocationPoolLogics, bossElev.Length > 0,
									mohcElev.Length > 0, objects, false, false);
							}
						}
						spawnPool = this.enemySpawnPool;
						break;
					case EntityGenerationPoolSources.AllLevels:
						// Get object definitions from all levels in the GOB and use them for the spawn pool.
						// Phase 2s in SECBASE? There's a chance!
						if (this.enemySpawnPool == null) {
							this.enemySpawnPool = spawnPool;

							int index = LevelLoader.Instance.CurrentLevelIndex;
							foreach (string levelName in LevelLoader.Instance.LevelList.Levels
								.Select((x, i) => (x.FileName, i))
								.Where(x => x.i != index)
								.Select(x => x.FileName)) {

								DfLevelObjects objects;
								try {
									objects = await FileLoader.Instance.LoadGobFileAsync<DfLevelObjects>($"{levelName}.O");
								} catch (Exception ex) {
									Debug.LogException(ex);
									continue;
								}
								this.AddLevelToSpawnMap(this.enemySpawnPool, spawnLocationPoolLogics, bossElev.Length > 0,
									mohcElev.Length > 0, objects, false, false);
							}
						}
						spawnPool = this.enemySpawnPool;
						break;
					default:
						// No list. We can still use default templates.
						spawnPool.Clear();
						break;
				}

				// How many objects of each logic to spawn?
				// This can be either weighted rng (so you won't always get the exact same count of each logic).
				// Or the same count of each logic in random positions.
				// Adjust according to settings.
				Dictionary<string[], int> logicWeights = spawnPool.ToDictionary(x => x.Key, x => x.Value.Count);
				foreach (LogicSpawnWeight weight in settings.EnemyLogicSpawnWeights) {
					(string[] logics, int count) = logicWeights.FirstOrDefault(x => x.Key.Length == 1 && x.Key[0] == weight.Logic);
					if (logics == null) {
						logics = new[] { weight.Logic };
						count = 0;
					}
					if (weight.Absolute) {
						count = 1;
					}
					int total = Mathf.RoundToInt(count * weight.Weight);
					if (total <= 0) {
						logicWeights.Remove(logics);
					} else {
						logicWeights[logics] = total;
					}
				}

				// If we are removing counts from each list, abort if either is empty, since we can't add more.
				while (difficultyCounts.Any() && logicWeights.Any()) {
					// Determine if we want to... and can... use pooled spawn points and/or random spawn points.
					bool useExistingSpawn = settings.EnemySpawnSources != SpawnSources.OnlyRandom && existingSpawns.Count > 0;
					bool useRandomSpawn = settings.EnemySpawnSources != SpawnSources.ExistingThenRandom || !useExistingSpawn;
					if (useExistingSpawn && useRandomSpawn) {
						// If we could use either, randomly choose one or the other.
						if (this.rng.Next(2) > 0) {
							useExistingSpawn = false;
						} else {
							useRandomSpawn = false;
						}
					}

					System.Numerics.Vector3 spawnPosition = default;
					System.Numerics.Vector3 spawnRotation = default;
					DfLevel.Sector sector = null;
					string[][] sectorLogics = null;
					bool sky = false;
					bool pit = false;
					// We need to find a sector and a spawn point before continuing.
					while (sector == null) {
						// If we run out of existing spawns while looking, switch to random.
						if (useExistingSpawn && existingSpawns.Count == 0) {
							useExistingSpawn = false;
							useRandomSpawn = true;
						}

						if (useExistingSpawn) {
							// Grab an existing spawn point, then remove it so we don't reuse it.
							int index = this.rng.Next(existingSpawns.Count);
							(spawnPosition, spawnRotation) = existingSpawns[index];
							existingSpawns.RemoveAt(index);

							// Figure out which sector it's in.
							foreach (DfLevel.Sector candidate in LevelLoader.Instance.Level.Sectors) {
								// If it's not between the floor and ceiling we can rule it out immediately.
								if (spawnPosition.Y > candidate.Floor.Y || spawnPosition.Y < candidate.Ceiling.Y) {
									continue;
								}

								// Split the sector into triangles.
								if (!tris.TryGetValue(candidate, out int[] sectorTris)) {
									tris[candidate] = sectorTris = FloorCeilingRenderer.SplitIntoFloorTris(candidate);
								}

								// Check each triangle to see if the point is inside it.
								bool inSector = false;
								for (int i = 0; i < sectorTris.Length; i += 3) {
									System.Numerics.Vector2[] tri = new[] {
										candidate.Walls[sectorTris[i]].LeftVertex.Position,
										candidate.Walls[sectorTris[i + 1]].LeftVertex.Position,
										candidate.Walls[sectorTris[i + 2]].LeftVertex.Position
									};

									// Make sure it is a triangle (otherwise we will never find a point inside it).
									if (tri.Distinct().Count() < 3) {
										continue;
									}

									Rect bounds = new() {
										xMin = tri.Min(x => x.X),
										yMin = tri.Min(x => x.Y),
										xMax = tri.Max(x => x.X),
										yMax = tri.Max(x => x.Y)
									};

									// Quick check, if it's not in the bounding box it can't be in the triangle.
									if (!bounds.Contains(new Vector2(spawnPosition.X, spawnPosition.Z))) {
										continue;
									}

									if (this.PointInTriangle(new System.Numerics.Vector2(spawnPosition.X, spawnPosition.Z),
										tri[2], tri[1], tri[0])) {

										inSector = true;
										break;
									}
								}

								if (inSector) {
									sector = candidate;
									break;
								}
							}
						} else if (settings.RandomEnemyLocationSelectionMode != RandomLocationSelectionModes.SectorThenPosition) {
							// Pick a random point. Select the point and then figure out if it's in a sector (if it's not, repeat until we find one)..

							// This is one way to do random points. It will result in an even spread of items/enemies based on surface area.
							if (levelBounds == default) {
								// Cache level bounds so we know where to point a point from.
								levelBounds = new Rect() {
									xMin = LevelLoader.Instance.Level.Sectors.SelectMany(x => x.Walls).Min(x => x.LeftVertex.Position.X),
									yMin = LevelLoader.Instance.Level.Sectors.SelectMany(x => x.Walls).Min(x => x.LeftVertex.Position.Y),
									xMax = LevelLoader.Instance.Level.Sectors.SelectMany(x => x.Walls).Max(x => x.LeftVertex.Position.X),
									yMax = LevelLoader.Instance.Level.Sectors.SelectMany(x => x.Walls).Max(x => x.LeftVertex.Position.Y)
								};
							}

							// Pick a random X Z point.
							Vector2 point = new() {
								x = (float)this.rng.NextDouble() * levelBounds.width + levelBounds.x,
								y = (float)this.rng.NextDouble() * levelBounds.height + levelBounds.y
							};

							// Look for a sector the point falls within.
							List<DfLevel.Sector> candidates = new();
							foreach (DfLevel.Sector candidate in LevelLoader.Instance.Level.Sectors) {
								if (!sectorBounds.TryGetValue(candidate, out Rect bounds)) {
									sectorBounds[candidate] = bounds = new Rect() {
										xMin = candidate.Walls.Min(x => x.LeftVertex.Position.X),
										yMin = candidate.Walls.Min(x => x.LeftVertex.Position.Y),
										xMax = candidate.Walls.Max(x => x.LeftVertex.Position.X),
										yMax = candidate.Walls.Max(x => x.LeftVertex.Position.Y)
									};
								}

								// Quick check to eliminate obvious out of bounds points.
								if (!bounds.Contains(point)) {
									continue;
								}

								// Other easy criteria.
								// If the sector is too short to hold an enemy (8 is a typical height of an open door).
								// Doors are height 0.
								// Elevator doors could start at height 0, placing enemies or items on them will cause them to start open instead.
								// We also rule out elevators that move walls to prevent items/enemies from being inaccessible.
								if (candidate.Floor.Y - candidate.Ceiling.Y < 8 ||
									candidate.Flags.HasFlag(DfLevel.SectorFlags.SectorIsDoor) ||
									inf.Items.Any(x => x.Type == DfLevelInformation.ScriptTypes.Sector &&
									x.SectorName == candidate.Name && elevator.IsMatch(x.Script))) {

									continue;
								}

								// Get triangles for this sector.
								if (!tris.TryGetValue(candidate, out int[] sectorTris)) {
									tris[candidate] = sectorTris = FloorCeilingRenderer.SplitIntoFloorTris(candidate);
								}

								// Determine if the point is in any of the triangles.
								bool inSector = false;
								for (int i = 0; i < sectorTris.Length; i += 3) {
									System.Numerics.Vector2[] tri = new[] {
										candidate.Walls[sectorTris[i]].LeftVertex.Position,
										candidate.Walls[sectorTris[i + 1]].LeftVertex.Position,
										candidate.Walls[sectorTris[i + 2]].LeftVertex.Position
									};

									if (tri.Distinct().Count() < 3) {
										continue;
									}

									bounds = new Rect() {
										xMin = tri.Min(x => x.X),
										yMin = tri.Min(x => x.Y),
										xMax = tri.Max(x => x.X),
										yMax = tri.Max(x => x.Y)
									};

									if (!bounds.Contains(point)) {
										continue;
									}

									if (this.PointInTriangle(new System.Numerics.Vector2(point.x, point.y),
										tri[2], tri[1], tri[0])) {

										inSector = true;
										break;
									}
								}

								if (inSector) {
									candidates.Add(candidate);
								}
							}

							// If sectors overlap, the item could show up in both. Work around this by eliminating overlapping sectors in favor of
							// the lower one (most enemies/items will go on the floor).
							if (candidates.Count > 1) {
								for (int i = 0; i < candidates.Count - 1; i++) {
									DfLevel.Sector candidate = candidates[i];
									for (int j = i + 1; j < candidates.Count; j++) {
										DfLevel.Sector candidate2 = candidates[j];
										if ((candidate2.Floor.Y < candidate.Floor.Y && candidate2.Floor.Y > candidate.Ceiling.Y) ||
											(candidate2.Ceiling.Y < candidate.Floor.Y && candidate2.Ceiling.Y > candidate.Ceiling.Y) ||
											(candidate.Floor.Y < candidate2.Floor.Y && candidate.Floor.Y > candidate2.Ceiling.Y) ||
											(candidate.Ceiling.Y < candidate2.Floor.Y && candidate.Ceiling.Y > candidate2.Ceiling.Y)) {

											if (candidate2.Floor.Y > candidate.Floor.Y) {
												candidates.RemoveAt(i);
												i--;
												break;
											} else {
												candidates.RemoveAt(j);
												j--;
											}
										}
									}
								}
							}

							// If we found any sectors, choose one at random.
							if (candidates.Count > 1) {
								sector = candidates[this.rng.Next(candidates.Count)];
							} else if (candidates.Count > 0) {
								sector = candidates[0];
							}
							if (sector != null) {
								// Most things spawn on the floor (which is partly why we need the sector in the first place).
								spawnPosition = new System.Numerics.Vector3(point.x, sector.Floor.Y, point.y);
							}
						} else {
							// Pick a sector first, and then a position inside the sector later.
							// This is quicker than above, but also means spawns are evenly distributed across sectors instead of space.
							// So areas with lots of small sectors (eg stairs) will have more spawns than wide open areas.
							while (sector == null || sector.Floor.Y - sector.Ceiling.Y < 8 || sector.Flags.HasFlag(DfLevel.SectorFlags.SectorIsDoor)) {
								sector = LevelLoader.Instance.Level.Sectors[this.rng.Next(LevelLoader.Instance.Level.Sectors.Count)];
							}
						}

						// If the point we selected is not in a sector, try again.
						if (sector == null) {
							continue;
						}

						bool water = sector.AltY > 0;
						sky = sector.Flags.HasFlag(DfLevel.SectorFlags.CeilingIsSky);
						pit = sector.Flags.HasFlag(DfLevel.SectorFlags.FloorIsPit);

						IEnumerable<string[]> logics = logicWeights.Keys;
						if (!water && settings.SpawnDiagonasOnlyInWater) {
							logics = logics.Where(x => !WATER_LOGICS.Any(y => x[0].Contains(y)));
						}
						if (water && settings.SpawnOnlyFlyingAndDiagonasInWater) {
							logics = logics.Where(x => WATER_LOGICS.Any(y => x[0].Contains(y)) || FLYING_LOGICS.Any(y => x[0].Contains(y)));
						}
						if (pit && settings.SpawnOnlyFlyingOverPits) {
							logics = logics.Where(x => FLYING_LOGICS.Any(y => x[0].Contains(y)));
						}
						// Don't spawn welders/turrets where there's nowhere to place them!
						if (sky && pit) {
							logics = logics.Where(x => !ATTACHED_LOGICS.Any(y => x[0].Contains(y)));
						}

						// If we can't spawn anything there, try for a new sector.
						if (!logics.Any()) {
							sector = null;
							continue;
						}

						sectorLogics = logics.ToArray();

						// If we don't already have a spawn position, we need to find one.
						if (useRandomSpawn && settings.RandomEnemyLocationSelectionMode == RandomLocationSelectionModes.SectorThenPosition) {
							if (!tris.TryGetValue(sector, out int[] sectorTris)) {
								tris[sector] = sectorTris = FloorCeilingRenderer.SplitIntoFloorTris(sector);
							}

							if (sectorTris.Length < 3) {
								sector = null;
								continue;
							}

							// Pick a random triangle to put the point in.
							int index = this.rng.Next(sectorTris.Length / 3) * 3;
							System.Numerics.Vector2[] tri;
							do {
								tri = new[] {
									sector.Walls[sectorTris[index]].LeftVertex.Position,
									sector.Walls[sectorTris[index + 1]].LeftVertex.Position,
									sector.Walls[sectorTris[index + 2]].LeftVertex.Position
								};

								if (tri.Distinct().Count() < 3) {
									tri = null;
								}
							} while (tri == null);

							Rect bounds = new() {
								xMin = tri.Min(x => x.X),
								yMin = tri.Min(x => x.Y),
								xMax = tri.Max(x => x.X),
								yMax = tri.Max(x => x.Y)
							};

							// Pick a random point until we find one inside the triangle.
							System.Numerics.Vector2 candidate;
							do {
								candidate = new System.Numerics.Vector2() {
									X = (float)this.rng.NextDouble() * bounds.width + bounds.x,
									Y = (float)this.rng.NextDouble() * bounds.height + bounds.y
								};
							} while (!this.PointInTriangle(candidate, tri[2], tri[1], tri[0]));
							float height = sector.Floor.Y - sector.Ceiling.Y;
							spawnPosition = new System.Numerics.Vector3(candidate.X, sector.Floor.Y, candidate.Y);
						}
					}

					if (sector == null) {
						break;
					}

					// Randomize yaw if requested or required
					if (settings.RandomizeEnemyYaw || useRandomSpawn) {
						spawnRotation = new System.Numerics.Vector3(spawnRotation.X, (float)(this.rng.NextDouble() * 360),
							spawnRotation.Z);
					}

					// Pick out a logic based on the weights of each logic.
					// Find the total of the weights and select a random value in that range.
					int totalWeights = sectorLogics.Sum(x => {
						logicWeights.TryGetValue(x, out int weight);
						return weight;
					});
					int logicValue = this.rng.Next(totalWeights);
					string[] logic = null;
					// Figure out which logic we selected with the value.
					for (int i = 0; i < sectorLogics.Length; i++) {
						logicWeights.TryGetValue(sectorLogics[i], out int weight);
						logicValue -= weight;
						if (logicValue <= 0) {
							logic = sectorLogics[i];
							break;
						}
					}
					// If we somehow have an invalid value just take the last one.
					logic ??= sectorLogics.Last();

					// Get a rnadom object from the spawn pool, or a default template.
					DfLevelObjects.Object obj;
					if (spawnPool.TryGetValue(logic, out List<DfLevelObjects.Object> objects)) {
						obj = objects[this.rng.Next(objects.Count)].Clone();
					} else {
						string filename = templates[logic[0]];
						DfLevelObjects.ObjectTypes type;
						switch (Path.GetExtension(filename)) {
							case ".FME":
								type = DfLevelObjects.ObjectTypes.Frame;
								break;
							case ".WAX":
								type = DfLevelObjects.ObjectTypes.Sprite;
								break;
							case ".3DO":
								type = DfLevelObjects.ObjectTypes.ThreeD;
								break;
							default:
								type = DfLevelObjects.ObjectTypes.Spirit;
								filename = null;
								break;
						}
						obj = new DfLevelObjects.Object() {
							Logic = string.Join(Environment.NewLine, logic.Select(x => $"LOGIC: {x}")),
							Type = type,
							FileName = filename
						};
					}

					// If this setting is used every enemy in the pool (though possibly not with the same templates) will be used up until
					// we hit the enemy limit desired.
					// For example with this setting off, if we spawn 100 items and our item pool has 100 items in it, some items may be
					// spawned extra times with other items never spawned (such as a 1-up with 1% chance of spawning; there's a 37% chance
					// it will never be spawned).
					// If the setting is on the 1-up will be guaranteed to be spawned eventually if we spawn 100 items as other items
					// get removed from the pool.
					if (settings.LessenEnemyProbabilityWhenSpawned) {
						if (--logicWeights[logic] <= 0) {
							logicWeights.Remove(logic);
							spawnPool.Remove(logic);
						}
					}

					// Assign special randomized properties to generators.
					if (logic[0].StartsWith("GENERATOR ")) {
						Dictionary<string, string[]> properties = this.GetLogicProperties(obj);

						// floating point values
						Dictionary<string, RandomRange> randomizedSettings = new() {
							["DELAY"] = settings.RandomizeGeneratorsDelay,
							["INTERVAL"] = settings.RandomizeGeneratorsInterval,
							["MIN_DIST"] = settings.RandomizeGeneratorsMinimumDistance,
							["MAX_DIST"] = settings.RandomizeGeneratorsMaximumDistance,
							["WANDER_TIME"] = settings.RandomizeGeneratorsWanderTime
						};

						Dictionary<string, float> floatProperties = new();
						foreach ((string key, RandomRange generatorSetting) in randomizedSettings) {
							float prop = 0;
							if (properties.ContainsKey(key)) {
								if (float.TryParse(properties[key][0], out float result)) {
									floatProperties[key] = prop = result;
								}
							}

							floatProperties[key] = this.RandomizeRange(new RandomRange() {
								Enabled = generatorSetting.Enabled || !floatProperties.ContainsKey(key),
								Minimum = generatorSetting.Minimum,
								Maximum = generatorSetting.Maximum
							}, prop);
						}
						// Swap values to keep the minimum smaller.
						if (floatProperties["MAX_DIST"] < floatProperties["MIN_DIST"]) {
							(floatProperties["MIN_DIST"], floatProperties["MAX_DIST"]) = (floatProperties["MAX_DIST"], floatProperties["MIN_DIST"]);
						}

						foreach ((string key, float value) in floatProperties) {
							properties[key] = new[] { value.ToString() };
						}

						// integer values
						randomizedSettings = new Dictionary<string, RandomRange>() {
							["MAX_ALIVE"] = settings.RandomizeGeneratorsMaximumAlive,
							["NUM_TERMINATE"] = settings.RandomizeGeneratorsNumberTerminate
						};

						Dictionary<string, int> intProperties = new();
						foreach ((string key, RandomRange generatorSetting) in randomizedSettings) {
							int prop = 0;
							if (properties.ContainsKey(key)) {
								if (int.TryParse(properties[key][0], NumberStyles.Integer, null, out int result)) {
									intProperties[key] = prop = result;
								}
							}

							intProperties[key] = this.RandomizeIntRange(new RandomRange() {
								Enabled = generatorSetting.Enabled || !intProperties.ContainsKey(key),
								Minimum = generatorSetting.Minimum,
								Maximum = generatorSetting.Maximum
							}, prop);
						}

						foreach ((string key, float value) in intProperties) {
							properties[key] = new[] { value.ToString() };
						}

						obj.Logic = string.Join(Environment.NewLine, properties.SelectMany(x => x.Value.Select(y => $"{x.Key}: {y}")));
					}

					// Pick a random difficulty used in the original level and track how many of them we spawn.
					obj.Difficulty = difficultyCounts.Keys.ElementAt(this.rng.Next(difficultyCounts.Count));
					if (--difficultyCounts[obj.Difficulty] <= 0) {
						difficultyCounts.Remove(obj.Difficulty);
					}

					if (FLYING_LOGICS.Contains(logic[0])) {
						// If a logic can fly, spawn it somewhere above the ground.
						spawnPosition.Y = (float)this.rng.NextDouble() * (sector.Floor.Y - sector.Ceiling.Y) + sector.Ceiling.Y;
					} else if (ATTACHED_LOGICS.Contains(logic[0])) {
						// If this is a turret or welder, cosnider attaching it to the ceiling if there is no sky.
						spawnRotation.X = 0;

						// Create a base object
						DfLevelObjects.Object baseObj = new() {
							Difficulty = obj.Difficulty,
							FileName = logic[0] == "WELDER" ? "WELDBASE.3DO" : "BASE.3DO",
							Type = DfLevelObjects.ObjectTypes.ThreeD
						};
						System.Numerics.Vector3 basePosition = spawnPosition;

						// Don't attach anywhere it doesn't make sense.
						bool ceiling;
						if (sky) {
							ceiling = false;
						} else if (pit) {
							ceiling = true;
						} else {
							ceiling = this.rng.Next(2) > 0;
						}

						int offset = logic[0] == "WELDER" ? 6 : 1;
						if (ceiling) {
							spawnPosition.Y = sector.Ceiling.Y + offset;
							spawnRotation.Z = logic[0] == "WELDER" ? 180 : 0;
							basePosition.Y = sector.Ceiling.Y;
						} else {
							spawnPosition.Y = sector.Floor.Y - offset;
							spawnRotation.Z = logic[0] == "WELDER" ? 0 : 180;
							basePosition.Y = sector.Floor.Y;
						}
						baseObj.Position = basePosition;
						baseObj.EulerAngles = spawnRotation;
						o.Objects.Add(baseObj);
						modifiedO = true;
					} else {
						spawnPosition.Y = sector.Floor.Y;
					}

					obj.Position = spawnPosition;
					obj.EulerAngles = spawnRotation;

					o.Objects.Add(obj);
					modifiedO = true;
				}
			}

			// Randomize the bosses. We went to treat these separately due to boss elevators blocking level progress.
			// So we do not randomize position, only which boss.
			// We could also add Mohc into the mix, but then you have to rename the elevator, and fix any script that sends messages to it.
			// I decided not to bother with that; Mohc is never reandomized.
			if (settings.RandomizeBosses) {
				// We're doing the same thing we did for enemies but some of the options which aren't as useful are stripped out.
				Dictionary<string[], List<DfLevelObjects.Object>> spawnPool = new();
				Dictionary<string[], List<(System.Numerics.Vector3 position, System.Numerics.Vector3 rotation)>> existingBossSpawns =
					new();
				foreach (DfLevelObjects.Object obj in o.Objects) {
					Dictionary<string, string[]> properties = this.GetLogicProperties(obj);
					if (properties == null) {
						continue;
					}

					properties.TryGetValue("LOGIC", out string[] logics);
					properties.TryGetValue("TYPE", out string[] types);
					logics = (logics ?? Enumerable.Empty<string>()).Concat(types ?? Enumerable.Empty<string>())
						.Select(x => x.ToUpper()).ToArray();

					if (logics.Length <= 0 || !logics.Intersect(BOSS_LOGICS.Concat(MOHC_LOGICS)).Any()) {
						continue;
					}

					// This time we want bosses.
					bool boss = false;
					if (properties.TryGetValue("BOSS", out string[] bosses)) {
						boss = bosses.Any(x => x.ToUpper() == "TRUE");
					}

					if (!boss || ((!logics.Intersect(BOSS_LOGICS).Any() || bossElev.Length <= 0) &&
						(!logics.Intersect(MOHC_LOGICS).Any() || mohcElev.Length <= 0))) {

						continue;
					}

					if (!existingBossSpawns.TryGetValue(logics, out List<(System.Numerics.Vector3 position, System.Numerics.Vector3 rotation)> spawns)) {
						existingBossSpawns[logics] = spawns = new List<(System.Numerics.Vector3 position, System.Numerics.Vector3 rotation)>();
					}
					spawns.Add((obj.Position, obj.EulerAngles));

					if (!spawnPool.TryGetValue(logics, out List<DfLevelObjects.Object> objects)) {
						spawnPool[logics] = objects = new List<DfLevelObjects.Object>();
					}
					objects.Add(obj);
				}

				bool bossEnemies = spawnPool.Keys.Any(x => x[0] != MOHC_LOGICS[0]);
				bool mohcEnemies = spawnPool.Keys.Any(x => x[0] == MOHC_LOGICS[0]);

				Dictionary<DfLevelObjects.Difficulties, int> difficultyCounts = spawnPool.SelectMany(x => x.Value).GroupBy(x => x.Difficulty).ToDictionary(x => x.Key, x => x.Count());

				// I think there will actually always be elevators at this point.
				// This just removes mohc from randomizing.
				// Possibly later we can tackle that.
				if ((bossEnemies && bossElev.Length > 0) || (mohcEnemies && mohcElev.Length > 0)) {
					foreach (string[] logic in existingBossSpawns.Keys.Where(x => x.Contains(MOHC_LOGICS[0])).ToArray()) {
						existingBossSpawns.Remove(logic);
					}
				}

				foreach (DfLevelObjects.Object obj in spawnPool.SelectMany(x => x.Value)) {
					o.Objects.Remove(obj);
					modifiedO = true;
				}

				switch (settings.EnemyGenerationPoolSource) {
					case EntityGenerationPoolSources.CurrentLevel:
						break;
					case EntityGenerationPoolSources.SelectedLevels:
						if (this.bossSpawnPool == null) {
							this.bossSpawnPool = spawnPool;

							foreach (string levelName in this.Settings.JediLvl.Levels
								.Where(x => x.ToUpper() != LevelLoader.Instance.CurrentLevelName.ToUpper())) {

								DfLevelObjects objects;
								try {
									objects = await FileLoader.Instance.LoadGobFileAsync<DfLevelObjects>($"{levelName}.O");
								} catch (Exception ex) {
									Debug.LogException(ex);
									continue;
								}
								this.AddLevelToSpawnMap(this.bossSpawnPool, BOSS_LOGICS.Concat(MOHC_LOGICS),
									bossElev.Length > 0, mohcElev.Length > 0, objects, true, false);
							}
						}
						spawnPool = this.bossSpawnPool;
						break;
					case EntityGenerationPoolSources.AllLevels:
						if (this.bossSpawnPool == null) {
							this.bossSpawnPool = spawnPool;

							int index = LevelLoader.Instance.CurrentLevelIndex;
							foreach (string levelName in LevelLoader.Instance.LevelList.Levels
								.Select((x, i) => (x.FileName, i))
								.Where(x => x.i != index)
								.Select(x => x.FileName)) {

								DfLevelObjects objects;
								try {
									objects = await FileLoader.Instance.LoadGobFileAsync<DfLevelObjects>($"{levelName}.O");
								} catch (Exception ex) {
									Debug.LogException(ex);
									continue;
								}
								this.AddLevelToSpawnMap(this.bossSpawnPool, BOSS_LOGICS.Concat(MOHC_LOGICS),
									bossElev.Length > 0, mohcElev.Length > 0, objects, true, false);
							}
						}
						spawnPool = this.bossSpawnPool;
						break;
					default:
						spawnPool.Clear();
						break;
				}

				if ((bossEnemies && bossElev.Length > 0) || (mohcEnemies && mohcElev.Length > 0)) {
					foreach (string[] logic in spawnPool.Keys.Where(x => x.Contains(MOHC_LOGICS[0])).ToArray()) {
						spawnPool.Remove(logic);
					}
					mohcEnemies = false;
				}

				List<(System.Numerics.Vector3 position, System.Numerics.Vector3 rotation)> existingSpawns =
					existingBossSpawns.SelectMany(x => x.Value).ToList();
				Dictionary<string[], int> logicWeights = spawnPool.ToDictionary(x => x.Key, x => x.Value.Count);

				while (difficultyCounts.Any() && logicWeights.Any() && existingSpawns.Count > 0) {
					System.Numerics.Vector3 spawnPosition = default;
					System.Numerics.Vector3 spawnRotation = default;
					DfLevel.Sector sector = null;
					string[][] sectorLogics = null;
					while (sector == null && existingSpawns.Count > 0) {
						int index = this.rng.Next(existingSpawns.Count);
						(spawnPosition, spawnRotation) = existingSpawns[index];
						existingSpawns.RemoveAt(index);

						// Figure out which sector it's in.
						foreach (DfLevel.Sector candidate in LevelLoader.Instance.Level.Sectors) {
							// If it's not between the floor and ceiling we can rule it out immediately.
							if (spawnPosition.Y > candidate.Floor.Y || spawnPosition.Y < candidate.Ceiling.Y) {
								continue;
							}

							// Split the sector into triangles.
							if (!tris.TryGetValue(candidate, out int[] sectorTris)) {
								tris[candidate] = sectorTris = FloorCeilingRenderer.SplitIntoFloorTris(candidate);
							}

							// Check each triangle to see if the point is inside it.
							bool inSector = false;
							for (int i = 0; i < sectorTris.Length; i += 3) {
								System.Numerics.Vector2[] tri = new[] {
									candidate.Walls[sectorTris[i]].LeftVertex.Position,
									candidate.Walls[sectorTris[i + 1]].LeftVertex.Position,
									candidate.Walls[sectorTris[i + 2]].LeftVertex.Position
								};

								// Make sure it is a triangle (otherwise we will never find a point inside it).
								if (tri.Distinct().Count() < 3) {
									continue;
								}

								Rect bounds = new() {
									xMin = tri.Min(x => x.X),
									yMin = tri.Min(x => x.Y),
									xMax = tri.Max(x => x.X),
									yMax = tri.Max(x => x.Y)
								};

								// Quick check, if it's not in the bounding box it can't be in the triangle.
								if (!bounds.Contains(new Vector2(spawnPosition.X, spawnPosition.Z))) {
									continue;
								}

								if (this.PointInTriangle(new System.Numerics.Vector2(spawnPosition.X, spawnPosition.Z),
									tri[2], tri[1], tri[0])) {

									inSector = true;
									break;
								}
							}

							if (inSector) {
								sector = candidate;
								break;
							}
						}

						IEnumerable<string[]> logics = spawnPool.Keys;
						sectorLogics = logics.ToArray();
					}

					if (sector == null) {
						break;
					}

					int totalWeights = sectorLogics.Sum(x => {
						logicWeights.TryGetValue(x, out int weight);
						return weight;
					});
					int logicValue = this.rng.Next(totalWeights);
					string[] logic = null;
					for (int i = 0; i < sectorLogics.Length; i++) {
						logicWeights.TryGetValue(sectorLogics[i], out int weight);
						logicValue -= weight;
						if (logicValue <= 0) {
							logic = sectorLogics[i];
							break;
						}
					}
					logic ??= sectorLogics.Last();

					DfLevelObjects.Object obj;
					if (spawnPool.TryGetValue(logic, out List<DfLevelObjects.Object> objects)) {
						obj = objects[this.rng.Next(objects.Count)].Clone();
					} else {
						string filename = templates[logic[0]];
						DfLevelObjects.ObjectTypes type;
						switch (Path.GetExtension(filename)) {
							case ".FME":
								type = DfLevelObjects.ObjectTypes.Frame;
								break;
							case ".WAX":
								type = DfLevelObjects.ObjectTypes.Sprite;
								break;
							case ".3DO":
								type = DfLevelObjects.ObjectTypes.ThreeD;
								break;
							default:
								type = DfLevelObjects.ObjectTypes.Spirit;
								filename = null;
								break;
						}
						obj = new DfLevelObjects.Object() {
							Logic = string.Join(Environment.NewLine, logic.Select(x => $"LOGIC: {x}")) +
								Environment.NewLine + "BOSS: TRUE",
							Type = type,
							FileName = filename
						};
					}

					if (settings.LessenEnemyProbabilityWhenSpawned) {
						if (--logicWeights[logic] <= 0) {
							logicWeights.Remove(logic);
							spawnPool.Remove(logic);
						}
					}

					obj.Position = spawnPosition;
					obj.EulerAngles = spawnRotation;

					obj.Difficulty = difficultyCounts.Keys.ElementAt(this.rng.Next(difficultyCounts.Count));
					if (--difficultyCounts[obj.Difficulty] <= 0) {
						difficultyCounts.Remove(obj.Difficulty);
					}

					o.Objects.Add(obj);
					modifiedO = true;
				}
			}

			// Remove checkpoints
			if (settings.RemoveCheckpoints) {
				foreach (DfLevelObjects.Object obj in o.Objects.Where(x => x.Type == DfLevelObjects.ObjectTypes.Safe).ToArray()) {
					o.Objects.Remove(obj);
					modifiedO = true;
				}
			}

			if (settings.NightmareMode) {
				foreach (DfLevelObjects.Object obj in o.Objects.ToArray()) {
					if (obj.Type != DfLevelObjects.ObjectTypes.Sprite && obj.Type != DfLevelObjects.ObjectTypes.Frame &&
						obj.Type != DfLevelObjects.ObjectTypes.ThreeD) {

						continue;
					}

					Dictionary<string, string[]> properties = this.GetLogicProperties(obj);
					if (properties == null) {
						continue;
					}

					properties.TryGetValue("LOGIC", out string[] logics);
					properties.TryGetValue("TYPE", out string[] types);
					// Get the list of logics this object has by merging the two lists.
					logics = (logics ?? Enumerable.Empty<string>()).Concat(types ?? Enumerable.Empty<string>())
						.Select(x => x.ToUpper())
						.OrderBy(x => x)
						.ToArray();

					if (logics.Length == 0) {
						continue;
					}

					string logic = logics.First().Trim().ToUpper();
					bool supported = logic switch {
						"I_OFFICER" => true,
						"I_OFFICERB" => true,
						"I_OFFICERY" => true,
						"I_OFFICERR" => true,
						"I_OFFICER1" => true,
						"I_OFFICER2" => true,
						"I_OFFICER3" => true,
						"I_OFFICER4" => true,
						"I_OFFICER5" => true,
						"I_OFFICER6" => true,
						"I_OFFICER7" => true,
						"I_OFFICER8" => true,
						"I_OFFICER9" => true,
						"TROOP" => true,
						"STORM1" => true,
						"COMMANDO" => true,
						"BOSSK" => true,
						"G_GUARD" => true,
						"REE_YEES" => true,
						"REE_YEES2" => true,
						"SEWER1" => true,
						"INT_DROID" => true,
						"PROBE_DROID" => true,
						"REMOTE" => true,
						_ => false
					};
					if (!supported) {
						continue;
					}

					DfLevelObjects.Object target;
					if (settings.NightmareKeepOriginalEnemies) {
						target = new() {
							Difficulty = obj.Difficulty,
							EulerAngles = obj.EulerAngles,
							FileName = obj.FileName,
							Position = obj.Position,
							Type = obj.Type
						};
						LevelLoader.Instance.Objects.Objects.Add(target);
					} else {
						target = obj;
					}

					properties.Remove("TYPE");
					properties["LOGIC"] = new[] { $"GENERATOR {logic}" };

					// floating point values
					Dictionary<string, RandomRange> randomizedSettings = new() {
						["DELAY"] = settings.NightmareGeneratorsDelay,
						["INTERVAL"] = settings.NightmareGeneratorsInterval,
						["MIN_DIST"] = settings.NightmareGeneratorsMinimumDistance,
						["MAX_DIST"] = settings.NightmareGeneratorsMaximumDistance,
						["WANDER_TIME"] = settings.NightmareGeneratorsWanderTime
					};

					Dictionary<string, float> floatProperties = new();
					foreach ((string key, RandomRange generatorSetting) in randomizedSettings) {
						float prop = 0;
						if (properties.ContainsKey(key)) {
							if (float.TryParse(properties[key][0], out float result)) {
								floatProperties[key] = prop = result;
							}
						}

						floatProperties[key] = this.RandomizeRange(new RandomRange() {
							Enabled = true,
							Minimum = generatorSetting.Minimum,
							Maximum = generatorSetting.Maximum
						}, prop);
					}
					// Swap values to keep the minimum smaller.
					if (floatProperties["MAX_DIST"] < floatProperties["MIN_DIST"]) {
						(floatProperties["MIN_DIST"], floatProperties["MAX_DIST"]) = (floatProperties["MAX_DIST"], floatProperties["MIN_DIST"]);
					}

					foreach ((string key, float value) in floatProperties) {
						properties[key] = new[] { value.ToString() };
					}

					// integer values
					randomizedSettings = new Dictionary<string, RandomRange>() {
						["MAX_ALIVE"] = settings.NightmareGeneratorsMaximumAlive,
						["NUM_TERMINATE"] = settings.NightmareGeneratorsNumberTerminate
					};

					Dictionary<string, int> intProperties = new();
					foreach ((string key, RandomRange generatorSetting) in randomizedSettings) {
						int prop = 0;
						if (properties.ContainsKey(key)) {
							if (int.TryParse(properties[key][0], NumberStyles.Integer, null, out int result)) {
								intProperties[key] = prop = result;
							}
						}

						intProperties[key] = this.RandomizeIntRange(new RandomRange() {
							Enabled = true,
							Minimum = generatorSetting.Minimum,
							Maximum = generatorSetting.Maximum
						}, prop);
					}

					foreach ((string key, float value) in intProperties) {
						properties[key] = new[] { value.ToString() };
					}

					obj.Logic = string.Join(Environment.NewLine, properties.SelectMany(x => x.Value.Select(y => $"{x.Key}: {y}")));
					modifiedO = true;
				}
			}

			if (settings.RandomizeItems) {
				// Now we want to handle items.
				// Items work much the same as enemy spawns.

				List<string> spawnLocationPoolLogics = settings.LogicsForItemSpawnLocationPool.ToList();

				// If all doors are unlocked, don't spawn any keys.
				if (settings.UnlockAllDoorsAndIncludeKeysInSpawnLocationPool) {
					spawnLocationPoolLogics.AddRange(new[] {
						"BLUE",
						"YELLOW",
						"RED",
					});
				}

				Dictionary<string[], List<DfLevelObjects.Object>> spawnPool =
					new();
				List<(System.Numerics.Vector3 position, System.Numerics.Vector3 rotation)> existingSpawns =
					new();
				foreach (DfLevelObjects.Object obj in o.Objects) {
					Dictionary<string, string[]> properties = this.GetLogicProperties(obj);
					if (properties == null) {
						continue;
					}

					properties.TryGetValue("LOGIC", out string[] logics);
					properties.TryGetValue("TYPE", out string[] types);
					logics = (logics ?? Enumerable.Empty<string>()).Concat(types ?? Enumerable.Empty<string>())
						.Select(x => {
							x = x.ToUpper();
							if (x.StartsWith("ITEM ")) {
								x = x.Substring(5).TrimStart();
							}
							return x;
						})
						.Where(x => x != "ANIM")
						.ToArray();

					if (logics.Length <= 0 || !logics.Intersect(spawnLocationPoolLogics).Any()) {
						continue;
					}

					existingSpawns.Add((obj.Position, obj.EulerAngles));

					if (!spawnPool.TryGetValue(logics, out List<DfLevelObjects.Object> objects)) {
						spawnPool[logics] = objects = new List<DfLevelObjects.Object>();
					}
					objects.Add(obj);
				}

				foreach (DfLevelObjects.Object obj in spawnPool.SelectMany(x => x.Value)) {
					o.Objects.Remove(obj);
					modifiedO = true;
				}

				// Don't actually spawn keys.
				if (settings.UnlockAllDoorsAndIncludeKeysInSpawnLocationPool) {
					foreach (string[] key in spawnPool.Keys.Where(x => x.Any(y => y == "BLUE" || y == "YELLOW" || y == "RED")).ToArray()) {
						spawnPool.Remove(key);
					}
				}

				Dictionary<DfLevelObjects.Difficulties, int> difficultyCounts =
					spawnPool.SelectMany(x => x.Value).GroupBy(x => x.Difficulty).ToDictionary(x => x.Key, x => x.Count());
				foreach (DifficultySpawnWeight weight in settings.DifficultyItemSpawnWeights) {
					difficultyCounts.TryGetValue(weight.Difficulty, out int count);
					if (weight.Absolute) {
						count = 1;
					}
					int total = Mathf.RoundToInt(count * weight.Weight);
					if (total <= 0) {
						difficultyCounts.Remove(weight.Difficulty);
					} else {
						difficultyCounts[weight.Difficulty] = total;
					}
				}

				switch (settings.ItemGenerationPoolSource) {
					case EntityGenerationPoolSources.CurrentLevel:
						this.itemSpawnPool = spawnPool;
						break;
					case EntityGenerationPoolSources.SelectedLevels:
						if (this.itemSpawnPool == null) {
							this.itemSpawnPool = spawnPool;

							foreach (string levelName in this.Settings.JediLvl.Levels
								.Where(x => x.ToUpper() != LevelLoader.Instance.CurrentLevelName.ToUpper())) {

								DfLevelObjects objects;
								try {
									objects = await FileLoader.Instance.LoadGobFileAsync<DfLevelObjects>($"{levelName}.O");
								} catch (Exception ex) {
									Debug.LogException(ex);
									continue;
								}
								this.AddLevelToSpawnMap(this.itemSpawnPool, spawnLocationPoolLogics, false, false, objects, false, true);
							}
						}
						spawnPool = this.itemSpawnPool;
						break;
					case EntityGenerationPoolSources.AllLevels:
						if (this.itemSpawnPool == null) {
							this.itemSpawnPool = spawnPool;

							int index = LevelLoader.Instance.CurrentLevelIndex;
							foreach (string levelName in LevelLoader.Instance.LevelList.Levels
								.Select((x, i) => (x.FileName, i))
								.Where(x => x.i != index)
								.Select(x => x.FileName)) {

								DfLevelObjects objects;
								try {
									objects = await FileLoader.Instance.LoadGobFileAsync<DfLevelObjects>($"{levelName}.O");
								} catch (Exception ex) {
									Debug.LogException(ex);
									continue;
								}
								this.AddLevelToSpawnMap(this.itemSpawnPool, spawnLocationPoolLogics, false, false, objects, false, true);
							}
						}
						spawnPool = this.itemSpawnPool;
						break;
					default:
						this.itemSpawnPool = spawnPool;
						spawnPool.Clear();
						break;
				}

				Dictionary<string[], int> logicWeights = spawnPool.ToDictionary(x => x.Key, x => x.Value.Count);
				foreach (LogicSpawnWeight weight in settings.ItemLogicSpawnWeights) {
					(string[] logics, int count) = logicWeights.FirstOrDefault(x => x.Key.Length == 1 && x.Key[0] == weight.Logic);
					if (logics == null) {
						logics = new[] { weight.Logic };
						count = 0;
					}
					if (weight.Absolute) {
						count = 1;
					}
					int total = Mathf.RoundToInt(count * weight.Weight);
					if (total <= 0) {
						logicWeights.Remove(logics);
					} else {
						logicWeights[logics] = total;
					}
				}

				while (difficultyCounts.Any() && logicWeights.Any()) {
					bool useExistingSpawn = settings.ItemSpawnSources != SpawnSources.OnlyRandom && existingSpawns.Count > 0;
					bool useRandomSpawn = settings.ItemSpawnSources != SpawnSources.ExistingThenRandom || !useExistingSpawn;
					if (useExistingSpawn && useRandomSpawn) {
						if (this.rng.Next(2) > 0) {
							useExistingSpawn = false;
						} else {
							useRandomSpawn = false;
						}
					}

					System.Numerics.Vector3 spawnPosition = default;
					System.Numerics.Vector3 spawnRotation = default;
					DfLevel.Sector sector = null;
					string[][] sectorLogics = null;
					while (sector == null) {
						if (useExistingSpawn && existingSpawns.Count == 0) {
							useExistingSpawn = false;
							useRandomSpawn = true;
						}

						if (useExistingSpawn) {
							int index = this.rng.Next(existingSpawns.Count);
							(spawnPosition, spawnRotation) = existingSpawns[index];
							existingSpawns.RemoveAt(index);

							foreach (DfLevel.Sector candidate in LevelLoader.Instance.Level.Sectors) {
								if (spawnPosition.Y > candidate.Floor.Y || spawnPosition.Y < candidate.Ceiling.Y) {
									continue;
								}

								if (!tris.TryGetValue(candidate, out int[] sectorTris)) {
									tris[candidate] = sectorTris = FloorCeilingRenderer.SplitIntoFloorTris(candidate);
								}

								bool inSector = false;
								for (int i = 0; i < sectorTris.Length; i += 3) {
									System.Numerics.Vector2[] tri = new[] {
										candidate.Walls[sectorTris[i]].LeftVertex.Position,
										candidate.Walls[sectorTris[i + 1]].LeftVertex.Position,
										candidate.Walls[sectorTris[i + 2]].LeftVertex.Position
									};

									if (tri.Distinct().Count() < 3) {
										continue;
									}

									Rect bounds = new() {
										xMin = tri.Min(x => x.X),
										yMin = tri.Min(x => x.Y),
										xMax = tri.Max(x => x.X),
										yMax = tri.Max(x => x.Y)
									};

									if (!bounds.Contains(new Vector2(spawnPosition.X, spawnPosition.Z))) {
										continue;
									}

									if (this.PointInTriangle(new System.Numerics.Vector2(spawnPosition.X, spawnPosition.Z),
										tri[2], tri[1], tri[0])) {

										inSector = true;
										break;
									}
								}

								if (inSector) {
									sector = candidate;
									break;
								}
							}
						} else if (settings.RandomEnemyLocationSelectionMode != RandomLocationSelectionModes.SectorThenPosition) {
							if (levelBounds == default) {
								levelBounds = new Rect() {
									xMin = LevelLoader.Instance.Level.Sectors.SelectMany(x => x.Walls).Min(x => x.LeftVertex.Position.X),
									yMin = LevelLoader.Instance.Level.Sectors.SelectMany(x => x.Walls).Min(x => x.LeftVertex.Position.Y),
									xMax = LevelLoader.Instance.Level.Sectors.SelectMany(x => x.Walls).Max(x => x.LeftVertex.Position.X),
									yMax = LevelLoader.Instance.Level.Sectors.SelectMany(x => x.Walls).Max(x => x.LeftVertex.Position.Y)
								};
							}

							Vector2 point = new() {
								x = (float)this.rng.NextDouble() * levelBounds.width + levelBounds.x,
								y = (float)this.rng.NextDouble() * levelBounds.height + levelBounds.y
							};

							List<DfLevel.Sector> candidates = new();
							foreach (DfLevel.Sector candidate in LevelLoader.Instance.Level.Sectors) {
								if (!sectorBounds.TryGetValue(candidate, out Rect bounds)) {
									sectorBounds[candidate] = bounds = new Rect() {
										xMin = candidate.Walls.Min(x => x.LeftVertex.Position.X),
										yMin = candidate.Walls.Min(x => x.LeftVertex.Position.Y),
										xMax = candidate.Walls.Max(x => x.LeftVertex.Position.X),
										yMax = candidate.Walls.Max(x => x.LeftVertex.Position.Y)
									};
								}

								if (!bounds.Contains(point)) {
									continue;
								}

								// Items can fit into smaller gaps than enemies.
								// Place into the smallest gap a player can enter to maximize the chance they can reach it.
								if (candidate.Floor.Y - candidate.Ceiling.Y < 3 ||
									candidate.Flags.HasFlag(DfLevel.SectorFlags.SectorIsDoor) ||
									inf.Items.Any(x => x.Type == DfLevelInformation.ScriptTypes.Sector &&
									x.SectorName == candidate.Name && elevator.IsMatch(x.Script))) {

									continue;
								}

								if (!tris.TryGetValue(candidate, out int[] sectorTris)) {
									tris[candidate] = sectorTris = FloorCeilingRenderer.SplitIntoFloorTris(candidate);
								}

								bool inSector = false;
								for (int i = 0; i < sectorTris.Length; i += 3) {
									System.Numerics.Vector2[] tri = new[] {
										candidate.Walls[sectorTris[i]].LeftVertex.Position,
										candidate.Walls[sectorTris[i + 1]].LeftVertex.Position,
										candidate.Walls[sectorTris[i + 2]].LeftVertex.Position
									};

									if (tri.Distinct().Count() < 3) {
										continue;
									}

									bounds = new Rect() {
										xMin = tri.Min(x => x.X),
										yMin = tri.Min(x => x.Y),
										xMax = tri.Max(x => x.X),
										yMax = tri.Max(x => x.Y)
									};

									if (!bounds.Contains(point)) {
										continue;
									}

									if (this.PointInTriangle(new System.Numerics.Vector2(point.x, point.y),
										tri[2], tri[1], tri[0])) {

										inSector = true;
										break;
									}
								}

								if (inSector) {
									candidates.Add(candidate);
								}
							}

							if (candidates.Count > 1) {
								for (int i = 0; i < candidates.Count - 1; i++) {
									DfLevel.Sector candidate = candidates[i];
									for (int j = i + 1; j < candidates.Count; j++) {
										DfLevel.Sector candidate2 = candidates[j];
										if ((candidate2.Floor.Y < candidate.Floor.Y && candidate2.Floor.Y > candidate.Ceiling.Y) ||
											(candidate2.Ceiling.Y < candidate.Floor.Y && candidate2.Ceiling.Y > candidate.Ceiling.Y) ||
											(candidate.Floor.Y < candidate2.Floor.Y && candidate.Floor.Y > candidate2.Ceiling.Y) ||
											(candidate.Ceiling.Y < candidate2.Floor.Y && candidate.Ceiling.Y > candidate2.Ceiling.Y)) {

											if (candidate2.Floor.Y > candidate.Floor.Y) {
												candidates.RemoveAt(i);
												i--;
												break;
											} else {
												candidates.RemoveAt(j);
												j--;
											}
										}
									}
								}
							}

							if (candidates.Count > 1) {
								sector = candidates[this.rng.Next(candidates.Count)];
							} else if (candidates.Count > 0) {
								sector = candidates[0];
							}
							if (sector != null) {
								spawnPosition = new System.Numerics.Vector3(point.x, sector.Floor.Y, point.y);
							}
						} else {
							while (sector == null || sector.Floor.Y - sector.Ceiling.Y < 3 || sector.Flags.HasFlag(DfLevel.SectorFlags.SectorIsDoor)) {
								sector = LevelLoader.Instance.Level.Sectors[this.rng.Next(LevelLoader.Instance.Level.Sectors.Count)];
							}
						}

						if (sector == null) {
							continue;
						}

						bool water = sector.AltY > 0;
						bool pit = sector.Flags.HasFlag(DfLevel.SectorFlags.FloorIsPit);

						if (water && !settings.SpawnItemsInWater) {
							sector = null;
							continue;
						}
						if (pit && !settings.SpawnItemsInPits) {
							sector = null;
							continue;
						}

						sectorLogics = logicWeights.Keys.ToArray();

						if (useRandomSpawn && settings.RandomItemLocationSelectionMode == RandomLocationSelectionModes.SectorThenPosition) {
							if (!tris.TryGetValue(sector, out int[] sectorTris)) {
								tris[sector] = sectorTris = FloorCeilingRenderer.SplitIntoFloorTris(sector);
							}

							if (sectorTris.Length < 3) {
								sector = null;
								continue;
							}
							
							int index = this.rng.Next(sectorTris.Length / 3) * 3;
							System.Numerics.Vector2[] tri;
							do {
								tri = new[] {
									sector.Walls[sectorTris[index]].LeftVertex.Position,
									sector.Walls[sectorTris[index + 1]].LeftVertex.Position,
									sector.Walls[sectorTris[index + 2]].LeftVertex.Position
								};

								if (tri.Distinct().Count() < 3) {
									tri = null;
								}
							} while (tri == null);

							Rect bounds = new() {
								xMin = tri.Min(x => x.X),
								yMin = tri.Min(x => x.Y),
								xMax = tri.Max(x => x.X),
								yMax = tri.Max(x => x.Y)
							};

							System.Numerics.Vector2 candidate;
							do {
								candidate = new System.Numerics.Vector2() {
									X = (float)this.rng.NextDouble() * bounds.width + bounds.x,
									Y = (float)this.rng.NextDouble() * bounds.height + bounds.y
								};
							} while (!this.PointInTriangle(candidate, tri[2], tri[1], tri[0]));
							float height = sector.Floor.Y - sector.Ceiling.Y;
							spawnPosition = new System.Numerics.Vector3(candidate.X, sector.Floor.Y, candidate.Y);
						}
					}

					if (sector == null) {
						break;
					}

					int totalWeights = sectorLogics.Sum(x => {
						logicWeights.TryGetValue(x, out int weight);
						return weight;
					});
					int logicValue = this.rng.Next(totalWeights);
					string[] logic = null;
					for (int i = 0; i < sectorLogics.Length; i++) {
						logicWeights.TryGetValue(sectorLogics[i], out int weight);
						logicValue -= weight;
						if (logicValue <= 0) {
							logic = sectorLogics[i];
							break;
						}
					}
					logic ??= sectorLogics.Last();

					DfLevelObjects.Object obj;
					if (spawnPool.TryGetValue(logic, out List<DfLevelObjects.Object> objects)) {
						obj = objects[this.rng.Next(objects.Count)].Clone();
					} else {
						string filename = templates[logic[0]];
						DfLevelObjects.ObjectTypes type;
						switch (Path.GetExtension(filename)) {
							case ".FME":
								type = DfLevelObjects.ObjectTypes.Frame;
								break;
							case ".WAX":
								type = DfLevelObjects.ObjectTypes.Sprite;
								break;
							case ".3DO":
								type = DfLevelObjects.ObjectTypes.ThreeD;
								break;
							default:
								type = DfLevelObjects.ObjectTypes.Spirit;
								filename = null;
								break;
						}

						// Sprites must have LOGIC: ANIM to animate.
						obj = new DfLevelObjects.Object() {
							Logic = string.Join(Environment.NewLine, logic.Select(x => $"LOGIC: {x}")) +
								(type == DfLevelObjects.ObjectTypes.Sprite ? (Environment.NewLine + "LOGIC: ANIM") : ""),
							Type = type,
							FileName = filename
						};
					}

					if (settings.LessenItemProbabilityWhenSpawned) {
						if (--logicWeights[logic] <= 0) {
							logicWeights.Remove(logic);
							spawnPool.Remove(logic);
						}
					}

					obj.Position = spawnPosition;
					obj.EulerAngles = spawnRotation;

					obj.Difficulty = difficultyCounts.Keys.ElementAt(this.rng.Next(difficultyCounts.Count));
					if (--difficultyCounts[obj.Difficulty] <= 0) {
						difficultyCounts.Remove(obj.Difficulty);
					}

					o.Objects.Add(obj);
					modifiedO = true;
				}

				//bool infModified = false;
				// Remove key statements from door scripts.
				if (settings.UnlockAllDoorsAndIncludeKeysInSpawnLocationPool) {
					Regex keyRegex = new(@"^\s*key:", RegexOptions.IgnoreCase | RegexOptions.Compiled);
					foreach (DfLevelInformation.Item item in inf.Items) {
						item.Script = string.Join(Environment.NewLine, item.Script.Split('\r', '\n')
							.Select(x => keyRegex.IsMatch(x) ? "" : x)
							.Where(x => !string.IsNullOrWhiteSpace(x)));
						modifiedInf = true;
					}
				}
			}

			// Award items to the player when they spawn.
			List<ItemAward> spawnItems = LevelLoader.Instance.CurrentLevelIndex > 0 ?
				settings.ItemAwardOtherLevels : settings.ItemAwardFirstLevel;

			// Find the player so we can spawn items at that spot.
			DfLevelObjects.Object player = null;
			foreach (DfLevelObjects.Object obj in o.Objects) {
				Dictionary<string, string[]> properties = this.GetLogicProperties(obj);
				if (properties == null) {
					continue;
				}

				properties.TryGetValue("LOGIC", out string[] logics);
				properties.TryGetValue("TYPE", out string[] types);
				logics = (logics ?? Enumerable.Empty<string>()).Concat(types ?? Enumerable.Empty<string>())
					.Select(x => x.ToUpper()).ToArray();
				if (logics.Any(x => x == "PLAYER")) {
					player = obj;
					break;
				}
			}

			// Add the items.
			foreach (ItemAward award in spawnItems) {
				List<DfLevelObjects.Object> objects = this.itemSpawnPool?
					.FirstOrDefault(x => x.Key.Length == 1 && x.Key[0] == award.Logic).Value;
				DfLevelObjects.Object obj;
				if (objects != null) {
					obj = objects[this.rng.Next(objects.Count)].Clone();
				} else {
					string filename = templates[award.Logic];
					DfLevelObjects.ObjectTypes type;
					switch (Path.GetExtension(filename)) {
						case ".FME":
							type = DfLevelObjects.ObjectTypes.Frame;
							break;
						case ".WAX":
							type = DfLevelObjects.ObjectTypes.Sprite;
							break;
						case ".3DO":
							type = DfLevelObjects.ObjectTypes.ThreeD;
							break;
						default:
							type = DfLevelObjects.ObjectTypes.Spirit;
							filename = null;
							break;
					}
					obj = new DfLevelObjects.Object() {
						Logic = $"LOGIC: {award.Logic}" +
							(type == DfLevelObjects.ObjectTypes.Sprite ? (Environment.NewLine + "LOGIC: ANIM") : ""),
						Type = type,
						FileName = filename
					};
				}

				obj.Position = player?.Position ?? System.Numerics.Vector3.Zero;
				obj.Difficulty = award.Difficulty;

				for (int i = 0; i < award.Count; i++) {
					o.Objects.Add(obj.Clone());
					modifiedO = true;
				}
			}

			return ((modifiedO ? o : null, modifiedInf ? inf : null));
		}

		private static readonly char[] FILENAME_CHARS = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C',
			'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
		private readonly Dictionary<string, string> lastGeneratedMirroredFilenames = new();
		private string GenerateMirroredFilename(string filename) {
			string ext = Path.GetExtension(filename).ToUpper();
			string last = this.lastGeneratedMirroredFilenames.GetValueOrDefault(ext);
			if (last == null) {
				last = string.Empty;
			} else {
				int pos = last.Length - 1;
				while (pos >= 0 && pos < last.Length) {
					char currChar = last[pos];
					int index = Array.IndexOf(FILENAME_CHARS, currChar);
					index++;
					if (index >= FILENAME_CHARS.Length) {
						pos--;
						continue;
					} else {
						last = last.Substring(0, pos) + FILENAME_CHARS[index] + new string(FILENAME_CHARS[0], last.Length - pos - 1);
						pos++;
					}
				}
				if (pos < 0) {
					last = new string(FILENAME_CHARS[0], last.Length + 1);
				}
			}

			this.lastGeneratedMirroredFilenames[ext] = last;
			return 'M' + new string('~', 7 - last.Length) + last + ext;
		}

		private readonly Dictionary<string, string> mirrorFileMap = new();
		private async Task<string> MirrorFileAsync(DfGobContainer gob, string filename) {
			if (this.mirrorFileMap.TryGetValue(filename.ToUpper(), out string newFilename)) {
				return newFilename;
			}

			IDfFile file = null;
			try {
				file = (IDfFile)(await FileLoader.Instance.LoadGobFileAsync(filename));
			} catch (Exception e) {
				ResourceCache.Instance.AddError(filename, e);
			}
			if (file == null) {
				mirrorFileMap[filename.ToUpper()] = filename;
				return filename;
			} 
			ResourceCache.Instance.AddWarnings(filename, file);

			switch (Path.GetExtension(filename).ToUpper()) {
				case ".3DO": {
					Df3dObject threeDo = ((Df3dObject)file).Clone();
					foreach (Df3dObject.Object obj in threeDo.Objects) {
						foreach (Df3dObject.Polygon polygon in obj.Polygons) {
							System.Numerics.Vector3[] vertices = polygon.Vertices.Select(x => new System.Numerics.Vector3(-x.X, x.Y, x.Z)).Reverse().ToArray();
							polygon.Vertices.Clear();
							polygon.Vertices.AddRange(vertices);
							polygon.TextureVertices.Reverse();
						}
					}
					newFilename = this.GenerateMirroredFilename(filename);
					await gob.AddFileAsync(newFilename, threeDo);
					mirrorFileMap[filename.ToUpper()] = newFilename;
					return newFilename;
				}
				case ".BM": {
					DfBitmap bm = ((DfBitmap)file).Clone();
					bm.AutoCompress = true;
					foreach (DfBitmap.Page page in bm.Pages) {
						for (int i = 0; i < page.Pixels.Length; i += page.Width) {
							Array.Reverse(page.Pixels, i, page.Width);
						}
					}
					newFilename = this.GenerateMirroredFilename(filename);
					await gob.AddFileAsync(newFilename, bm);
					mirrorFileMap[filename.ToUpper()] = newFilename;
					return newFilename;
				}
				case ".FME": {
					DfFrame fme = ((DfFrame)file).Clone();
					fme.AutoCompress = true;
					fme.Flip = !fme.Flip;
					fme.InsertionPointX = -fme.Width - fme.InsertionPointX;
					newFilename = this.GenerateMirroredFilename(filename);
					await gob.AddFileAsync(newFilename, fme);
					mirrorFileMap[filename.ToUpper()] = newFilename;
					return newFilename;
				}
				case ".FNT": {
					DfFont font = ((DfFont)file).Clone();
					foreach (DfFont.Character c in font.Characters) {
						for (int x = 0; x < c.Width / 2; x++) {
							for (int y = 0; y < font.Height; y++) {
								(c.Data[x * font.Height + y], c.Data[(c.Width - x - 1) * font.Height + y]) =
									(c.Data[(c.Width - x - 1) * font.Height + y], c.Data[x * font.Height + y]);
							}
						}
					}
					await gob.AddFileAsync(filename, font);
					mirrorFileMap[filename.ToUpper()] = filename;
					return filename;
				}
				case ".MSG": {
					DfMessages msg = ((DfMessages)file).Clone();
					foreach (DfMessages.Message message in msg.Messages.Values) {
						char[] chars = message.Text.ToCharArray();
						Array.Reverse(chars);
						message.Text = new(chars);
					}
					await gob.AddFileAsync(filename, msg);
					mirrorFileMap[filename.ToUpper()] = filename;
					return filename;
				}
				case ".WAX": {
					DfWax wax = ((DfWax)file).Clone();
					foreach (DfFrame fme in wax.Waxes.SelectMany(x => x.Sequences).SelectMany(x => x.Frames).Distinct()) {
						fme.AutoCompress = true;
						fme.Flip = !fme.Flip;
						fme.InsertionPointX = -fme.Width - fme.InsertionPointX;
					}
					foreach (DfWax.SubWax subWax in wax.Waxes) {
						for (int i = 1; i < 15; i++) {
							int otherI = 32 - i;
							if (subWax.Sequences.Count <= otherI) {
								continue;
							}
							(subWax.Sequences[i], subWax.Sequences[otherI]) = (subWax.Sequences[otherI], subWax.Sequences[i]);
						}
					}
					newFilename = this.GenerateMirroredFilename(filename);
					await gob.AddFileAsync(newFilename, wax);
					mirrorFileMap[filename.ToUpper()] = newFilename;
					return newFilename;
				}
				case ".VUE": {
					AutodeskVue vue = ((AutodeskVue)file).Clone();
					foreach (AutodeskVue.SubVue subVue in vue.Vues) {
						foreach (AutodeskVue.Light light in subVue.Lights) {
							light.Position = new System.Numerics.Vector3(-light.Position.X + X_OFFSET, light.Position.Y, light.Position.Z);
						}
						foreach (AutodeskVue.VueObject obj in subVue.Objects.Values) {
							Matrix4x4[] frames = obj.Frames.Select((x, i) => {
								Matrix4x4 matrix = x.ToUnity();
								Vector3 pos = matrix.GetUnityPositionFromAutodesk();
								Quaternion rot = matrix.GetUnityRotationFromAutodesk();
								Vector3 scale = matrix.lossyScale;

								pos = new Vector3(-pos.x + X_OFFSET, pos.y, pos.z);
								Vector3 euler = rot.eulerAngles;
								euler = new Vector3(euler.x, -euler.y, euler.z);
								rot = Quaternion.Euler(euler);

								// Adjust back to different coordinate system for Autodesk
								pos = new Vector3(pos.x, pos.z, pos.y);

								matrix = Matrix4x4.TRS(pos, rot, scale);
								return new Matrix4x4(
									new Vector4(matrix.m00, matrix.m20, matrix.m10, matrix.m30),
									new Vector4(matrix.m02, matrix.m22, matrix.m12, matrix.m31),
									new Vector4(matrix.m01, matrix.m21, matrix.m11, matrix.m32),
									new Vector4(matrix.m03, matrix.m13, matrix.m23, matrix.m33)
								);
							}).ToArray();

							obj.Frames.Clear();
							obj.Frames.AddRange(frames.Select(x => x.ToNet()));
						}
						foreach (AutodeskVue.Viewport viewport in subVue.Viewports.Values) {
							viewport.HorizontalAngle = -viewport.HorizontalAngle;
							viewport.Position = new System.Numerics.Vector3(-viewport.Position.X + X_OFFSET, viewport.Position.Y, viewport.Position.Z);
						}
					}
					newFilename = this.GenerateMirroredFilename(filename);
					await gob.AddFileAsync(newFilename, vue);
					mirrorFileMap[filename.ToUpper()] = newFilename;
					return newFilename;
				}
				default: {
					mirrorFileMap[filename.ToUpper()] = filename;
					return filename;
				}
			}
		}

		private IEnumerable<KeyValuePair<string, string>> GetScriptProperties(DfLevelInformation.Item item) =>
			item.Script?.Split('\r', '\n')
				.SelectMany(x => TextBasedFile.SplitKeyValuePairs(TextBasedFile.TokenizeLine(x)))
				.Select(x => new KeyValuePair<string, string>(x.Key.ToLower(), string.Join(" ", x.Value)));

		private async Task MirrorAsync(DfGobContainer gob, DfLevel level, DfLevelInformation inf, DfLevelObjects o) {
			RandomizerCrossFileSettings settings = this.Settings.CrossFile;
			Dictionary<string, int> textureWidths = new();

			foreach (DfLevel.Sector sector in level.Sectors) {
				sector.Ceiling.TextureFile = await this.MirrorFileAsync(gob, sector.Ceiling.TextureFile);
				sector.Ceiling.TextureOffset = new System.Numerics.Vector2(-sector.Ceiling.TextureOffset.X, sector.Ceiling.TextureOffset.Y);
				sector.Floor.TextureFile = await this.MirrorFileAsync(gob, sector.Floor.TextureFile);
				sector.Floor.TextureOffset = new System.Numerics.Vector2(-sector.Floor.TextureOffset.X, sector.Floor.TextureOffset.Y);

				foreach (DfLevel.Vertex vertex in sector.Walls.SelectMany(x => new[] { x.LeftVertex, x.RightVertex }).Distinct()) {
					vertex.Position = new System.Numerics.Vector2(-vertex.Position.X + X_OFFSET, vertex.Position.Y);
				}

				foreach (DfLevel.Wall wall in sector.Walls) {
					wall.TextureAndMapFlags ^= DfLevel.WallTextureAndMapFlags.FlipTextureHorizontally;

					(wall.RightVertex, wall.LeftVertex) = (wall.LeftVertex, wall.RightVertex);

					float wallLength = (wall.RightVertex.Position - wall.LeftVertex.Position).Length();

					int textureWidth;
					foreach (var texture in new[] { wall.BottomEdgeTexture, wall.MainTexture, wall.TopEdgeTexture }) {
						textureWidth = 0;
						if (!string.IsNullOrEmpty(texture.TextureFile) &&
							!textureWidths.TryGetValue(texture.TextureFile.ToUpper(), out textureWidth)) {

							DfBitmap bitmap = null;
							try {
								bitmap = await FileLoader.Instance.LoadGobFileAsync<DfBitmap>(texture.TextureFile);
							} catch (Exception e) {
								ResourceCache.Instance.AddError(texture.TextureFile, e);
							}

							if (bitmap != null) {
								textureWidth = bitmap.Pages[0].Width;
							}

							textureWidths[texture.TextureFile.ToUpper()] = textureWidth;
						}

						texture.TextureOffset = new System.Numerics.Vector2((textureWidth / 8f) - wallLength - texture.TextureOffset.X, texture.TextureOffset.Y);
					}

					textureWidth = 0;
					if (!string.IsNullOrEmpty(wall.SignTexture.TextureFile) &&
						!textureWidths.TryGetValue(wall.SignTexture.TextureFile.ToUpper(), out textureWidth)) {

						DfBitmap bitmap = null;
						try {
							bitmap = await FileLoader.Instance.LoadGobFileAsync<DfBitmap>(wall.SignTexture.TextureFile);
						} catch (Exception e) {
							ResourceCache.Instance.AddError(wall.SignTexture.TextureFile, e);
						}

						if (bitmap != null) {
							textureWidth = bitmap.Pages[0].Width;
						}

						textureWidths[wall.SignTexture.TextureFile.ToUpper()] = textureWidth;
					}

					wall.SignTexture.TextureOffset = new System.Numerics.Vector2((textureWidth / 8f) - wall.SignTexture.TextureOffset.X, wall.SignTexture.TextureOffset.Y);
				}
			}

			foreach (DfLevelInformation.Item item in inf.Items) {
				if (item.Type == DfLevelInformation.ScriptTypes.Level) {
					continue;
				}

				List<KeyValuePair<string, string>> properties = this.GetScriptProperties(item).ToList();
				string classProp = properties.FirstOrDefault(x => x.Key == "class").Value;
				if (classProp == null) {
					continue;
				}

				string[] infClass = classProp.Split(' ');
				if (infClass.Length < 2 || infClass[0].ToLower() != "elevator") {
					continue;
				}

				for (int i = 0; i < properties.Count; i++) {
					KeyValuePair<string, string> property = properties[i];
					switch (infClass[1].ToUpper()) {
						case "SCROLL_FLOOR":
						case "SCROLL_CEILING":
						case "MORPH_MOVE1":
						case "MORPH_MOVE2":
						case "MOVE_WALL":
						case "SCROLL_WALL":
							if (property.Key == "angle") {
								if (double.TryParse(property.Value, out double value)) {
									value = -value;
									property = new KeyValuePair<string, string>(property.Key, value.ToString());
									properties[i] = property;
								}
							}
							break;
						case "MORPH_SPIN1":
						case "MORPH_SPIN2":
						case "ROTATE_WALL":
							if (property.Key == "stop") {
								string[] stopSplit = property.Value.Split(' ');
								bool relative = false;
								if (stopSplit[0].StartsWith('@')) {
									relative = true;
									stopSplit[0] = stopSplit[0].Substring(1);
								}
								if (double.TryParse(stopSplit[0], out double stopValue)) {
									stopValue = -stopValue;
									stopSplit[0] = $"{(relative ? "@" : string.Empty)}{stopValue}";
									property = new KeyValuePair<string, string>(property.Key, string.Join(' ', stopSplit));
									properties[i] = property;
								}
							} else if (property.Key == "center") {
								string[] centerSplit = property.Value.Split(' ');
								if (double.TryParse(centerSplit[0], out double centerX)) {
									centerX = -centerX + X_OFFSET;
									centerSplit[0] = centerX.ToString();
									property = new KeyValuePair<string, string>(property.Key, string.Join(' ', centerSplit));
									properties[i] = property;
								}
							}
							break;
					}
					if (property.Key == "message") {
						string[] messageSplit = property.Value.Split(' ');
						if (messageSplit.Length < 5 || !messageSplit[1].Contains('(') ||
							(messageSplit[2].ToUpper() != "clear_bits" && messageSplit[2].ToUpper() != "set_bits") || messageSplit[3] != "1") {

							continue;
						}

						bool set = messageSplit[2].ToUpper() == "set_bits";
						if (!int.TryParse(messageSplit[4], out int flags)) {
							continue;
						}
						if (!((DfLevel.WallTextureAndMapFlags)flags).HasFlag(DfLevel.WallTextureAndMapFlags.FlipTextureHorizontally)) {
							continue;
						}
						flags ^= (int)DfLevel.WallTextureAndMapFlags.FlipTextureHorizontally;
						if (flags == 0) {
							messageSplit[4] = ((int)DfLevel.WallTextureAndMapFlags.FlipTextureHorizontally).ToString();
							if (set) {
								messageSplit[2] = "clear_bits";
							} else {
								messageSplit[2] = "set_bits";
							}
							property = new KeyValuePair<string, string>(property.Key, string.Join(' ', messageSplit));
							properties[i] = property;
						} else {
							messageSplit[4] = flags.ToString();
							property = new KeyValuePair<string, string>(property.Key, string.Join(' ', messageSplit));
							properties[i] = property;
							i++;
							property = new KeyValuePair<string, string>(property.Key, $"{messageSplit[0]} {messageSplit[1]} {(set ? "clear_bits" : "set_bits")} 1 4");
							properties.Insert(i, property);
						}
					}
				}

				item.Script = string.Join(Environment.NewLine, properties.Select(x => $"{x.Key}: {x.Value}"));
			}

			foreach (DfLevelObjects.Object obj in o.Objects) {
				obj.EulerAngles = new System.Numerics.Vector3(obj.EulerAngles.X, -obj.EulerAngles.Y, obj.EulerAngles.Z);

				Dictionary<string, string[]> properties = this.GetLogicProperties(obj);
				if (properties != null) {
					properties.TryGetValue("LOGIC", out string[] logics);
					properties.TryGetValue("TYPE", out string[] types);

					if ((logics ?? Enumerable.Empty<string>()).Concat(types ?? Enumerable.Empty<string>()).Any(x => x.ToUpper() == "UPDATE")) {
						if (properties.TryGetValue("D_YAW", out string[] yaws)) {
							for (int i = 0; i < yaws.Length; i++) {
								string yaw = yaws[i];

								if (double.TryParse(yaw, out double value)) {
									value = -value;
									yaws[i] = value.ToString();
								}
							}
							properties["D_YAW"] = yaws;
						}
					}

					if ((logics ?? Enumerable.Empty<string>()).Concat(types ?? Enumerable.Empty<string>()).Any(x => x.ToUpper() == "KEY")) {
						if (properties.TryGetValue("VUE", out string[] vues)) {
							for (int i = 0; i < vues.Length; i++) {
								string[] vue = vues[i].Split(' ');
								vue[0] = await this.MirrorFileAsync(gob, vue[0]);
								if (vue.Length >= 2) {
									vue[1] = $"\"{vue[1]}\"";
								}
								vues[i] = string.Join(' ', vue);
							}
							properties["VUE"] = vues;
						}
						if (properties.TryGetValue("VUE_APPEND", out string[] vues2)) {
							for (int i = 0; i < vues2.Length; i++) {
								string[] vue = vues2[i].Split(' ');
								vue[0] = await this.MirrorFileAsync(gob, vue[0]);
								if (vue.Length >= 2) {
									vue[1] = $"\"{vue[1]}\"";
								} 
								vues2[i] = string.Join(' ', vue);
							}
							properties["VUE_APPEND"] = vues2;
						}
					}

					obj.Logic = string.Join(Environment.NewLine, properties.SelectMany(x => x.Value.Select(y => $"{x.Key}: {y}")));
				}

				obj.Position = new System.Numerics.Vector3(-obj.Position.X + X_OFFSET, obj.Position.Y, obj.Position.Z);

				if ((obj.Type == DfLevelObjects.ObjectTypes.ThreeD || (settings.MirrorSprites &&
					(obj.Type == DfLevelObjects.ObjectTypes.Frame || obj.Type == DfLevelObjects.ObjectTypes.Sprite))) && obj.FileName != null) {

					obj.FileName = await this.MirrorFileAsync(gob, obj.FileName);
				}
			}
		}

		private async Task RandomizeCrossFileAsync(DfGobContainer gob) {
			RandomizerCrossFileSettings settings = this.Settings.CrossFile;
			if (settings.MirrorText) {
				await this.MirrorFileAsync(gob, "GLOWING.FNT");
				await this.MirrorFileAsync(gob, "TEXT.MSG");
			}
		}

		/// <summary>
		/// If the user is building off of a mod, copy all the mod's files into the randomized GOB.
		/// </summary>
		/// <param name="output">The output GOB.</param>
		private async Task AddDependentModFilesAsync(DfGobContainer output) {
			// Store these paths in case the user loads this GOB in the future, we can pull them.
			this.Settings.ModSourcePaths = Mod.Instance.List.Value.ToDictionary(x => x.FilePath, x => x.Overrides);

			// Get existing files so we don't replace them by accident.
			HashSet<string> files = new(output.Files.Select(x => x.name.ToUpper()));
			foreach (ModFile modFileInfo in Mod.Instance.List.Value.Reverse()) {
				string name = Path.GetFileName(modFileInfo.FilePath).ToUpper();
				string ext = Path.GetExtension(name);
				// We don't care about LFDs, or files we already replaced.
				if (ext == ".LFD") {
					continue;
				}

				if (files.Contains(name)) {
					continue;
				}

				// Single standalone file, add it to the GOB.
				if (ext != ".GOB") {
					using Stream stream = await FileLoader.Instance.GetGobFileStreamAsync(name);
					await output.AddFileAsync(name, stream);
					continue;
				}

				// Pull all files from the GOB and add to our GOB.
				foreach (string gobFile in FileLoader.Instance.FindGobFiles("*", modFileInfo.FilePath)) {
					name = gobFile.ToUpper();
					// If we already added this one skip it.
					if (files.Contains(name)) {
						continue;
					}

					using Stream stream = await FileLoader.Instance.GetGobFileStreamAsync(name);
					await output.AddFileAsync(name, stream);
				}
			}
		}

		private RandomizerSettings settings;
		public RandomizerSettings Settings {
			get => this.settings;
			set {
				if (this.settings == value) {
					return;
				}
				this.settings = value;
				this.settingsObject.Value = value;
			}
		}

		private string lastFolder = null;
		
		/// <summary>
		/// Take the settings selected and build the randomized GOB.
		/// </summary>
		public async Task<string> BuildAsync() {
			this.mirrorFileMap.Clear();
			this.lastGeneratedMirroredFilenames.Clear();

			// Reset campaign spawn pools so they're recalculated again.
			this.enemySpawnPool = null;
			this.bossSpawnPool = null;
			this.itemSpawnPool = null;
			this.levGeneratedGlobalValues = false;
			this.palGeneratedGlobalValues = false;
			this.cmpGeneratedGlobalValues = false;

			if (!this.Settings.FixedSeed) {
				this.GenerateNewSeed();
			}

			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = false,
				FileSearchPatterns = new[] { "*.GOB" },
				SelectButtonText = "Save",
				SelectedFileMustExist = false,
				SelectedPathMustExist = true,
				StartSelectedFile = Path.Combine(this.lastFolder ?? FileLoader.Instance.DarkForcesFolder, $"{this.Settings.Seed:X8}.GOB"),
				Title = "Save Randomized GOB",
				ValidateFileName = true
			});
			if (path == null) {
				return null;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			await PauseMenu.Instance.BeginLoadingAsync();

			ResourceCache.Instance.ClearWarnings();

			try {
				this.rng = new Random(this.Settings.Seed);

				DfGobContainer gob = new();

				DfLevelList levels = this.RandomizeJediLvl();
				if (levels != null) {
					await gob.AddFileAsync("JEDI.LVL", levels);
				}

				DfCutsceneList cutscenes = this.RandomizeCutscenes();
				if (cutscenes != null) {
					await gob.AddFileAsync("CUTSCENE.LST", cutscenes);
				}

				await this.RandomizeMusicAsync(gob);

				foreach (string levelName in this.Settings.JediLvl.Levels) {
					if (!(await this.LoadLevelAsync(levelName))) {
						continue;
					}

					DfPalette pal = this.RandomizeLevelPalette();
					if (pal != null) {
						await gob.AddFileAsync($"{levelName}.PAL", pal);
					}

					DfColormap cmp = this.RandomizeLevelColormap();
					if (cmp != null) {
						await gob.AddFileAsync($"{levelName}.CMP", cmp);
					}

					RandomizerCrossFileSettings settings = this.Settings.CrossFile;
					bool mirror = settings.MirrorMode switch {
						MirrorModes.Enabled => true,
						MirrorModes.Random => this.rng.Next(2) > 0,
						_ => false
					};

					DfLevel level = this.RandomizeLevel(mirror);
					(DfLevelObjects o, DfLevelInformation inf) = await this.RandomizeLevelObjectsAsync(mirror);
					if (mirror) {
						await this.MirrorAsync(gob, level, inf, o);
					}

					if (level != null) {
						await gob.AddFileAsync($"{levelName}.LEV", level);
					}
					if (o != null) {
						await gob.AddFileAsync($"{levelName}.O", o);
					}
					if (inf != null) {
						await gob.AddFileAsync($"{levelName}.INF", inf);
					}
				}

				await this.RandomizeCrossFileAsync(gob);

				await this.AddDependentModFilesAsync(gob);

				if (this.Settings.SaveSettingsToGob) {
					DataContractJsonSerializer serializer = new(typeof(RandomizerSettings), new DataContractJsonSerializerSettings() {
						UseSimpleDictionaryFormat = true
					});
					using MemoryStream jsonStream = new();
					serializer.WriteObject(jsonStream, this.Settings);
					jsonStream.Position = 0;

					await gob.AddFileAsync("RNDMIZER.JSO", jsonStream);
				}

				this.mirrorFileMap.Clear();
				this.lastGeneratedMirroredFilenames.Clear();

				using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.None);
				await gob.SaveAsync(stream);
			} catch (Exception ex) {
				ResourceCache.Instance.AddError("Randomizer", ex);
				path = null;
			}

			await LevelLoader.Instance.ShowWarningsAsync(Mod.Instance.Gob ?? "DARK.GOB");

			PauseMenu.Instance.EndLoading();

			return path;
		}
	}
}
