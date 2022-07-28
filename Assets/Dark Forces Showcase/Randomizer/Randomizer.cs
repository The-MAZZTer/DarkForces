using MZZT.DarkForces.FileFormats;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using Random = System.Random;

namespace MZZT.DarkForces.Showcase {
	public class Randomizer : Singleton<Randomizer> {
		private Random rng;

		private async void Start() {
			// This is here in case you run directly from this scene instead of the menu.
			if (!FileLoader.Instance.Gobs.Any()) {
				await FileLoader.Instance.LoadStandardGobFilesAsync();
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
		/// Loads in all level data required to reandomize the level.
		/// </summary>
		/// <param name="filename">The base name of the level from the GOB.</param>
		private async Task LoadLevelAsync(string filename) {
			filename = filename.ToLower();
			int index = LevelLoader.Instance.LevelList.Levels
				.Select((x, i) => (x, i))
				.First(x => x.x.FileName.ToLower() == filename).i;

			await PauseMenu.Instance.BeginLoadingAsync();

			await LevelLoader.Instance.LoadLevelAsync(index);
			await LevelLoader.Instance.LoadInformationAsync();
			await LevelLoader.Instance.LoadColormapAsync();
			await LevelLoader.Instance.LoadPaletteAsync();
			await LevelLoader.Instance.LoadObjectsAsync();

			PauseMenu.Instance.EndLoading();
		}

		/// <summary>
		/// Generates a new, random seed. The same seed with the same settings, in the same Showcase version, should produce the same randomized level.
		/// </summary>
		public void GenerateNewSeed() {
			this.Settings.Seed = new Random().Next();
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
			}

			// Put the RNG seed in the level name in case the user selects a different GOB filename.
			levelList.Levels[0].DisplayName = $"Seed: {this.Settings.Seed:X8}";
			return levelList;
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
		private DfLevel RandomizeLevel() {
			RandomizerLevelSettings settings = this.Settings.Level;
			if (settings.MapOverrideMode == MapOverrideModes.None && !settings.LightLevelMultiplier.Enabled &&
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
			new Dictionary<string[], List<DfLevelObjects.Object>>();
		Dictionary<string[], List<DfLevelObjects.Object>> bossSpawnPool =
			new Dictionary<string[], List<DfLevelObjects.Object>>();
		Dictionary<string[], List<DfLevelObjects.Object>> itemSpawnPool =
			new Dictionary<string[], List<DfLevelObjects.Object>>();
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

			Regex officerGenerator = new Regex(@"^GENERATOR\s+I_OFFICER(\w)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
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
		private Dictionary<string, string[]> GetLogicProperties(DfLevelObjects.Object obj) =>
			obj.Logic?.Split('\r', '\n')
				.SelectMany(x => TextBasedFile.SplitKeyValuePairs(TextBasedFile.TokenizeLine(x)))
				.Select(x => (x.Key.ToUpper(), string.Join(" ", x.Value)))
				.GroupBy(x => x.Item1)
				.ToDictionary(x => x.Key, x => x.Select(x => x.Item2).ToArray());

		/// <summary>
		/// Randomize object placements in a level.
		/// </summary>
		/// <returns>The modified O file, or null if there is no randomization to be done.</returns>
		private async Task<DfLevelObjects> RandomizeLevelObjectsAsync() {
			RandomizerObjectSettings settings = this.Settings.Object;

			bool modified = false;

			DfLevelObjects o = LevelLoader.Instance.Objects.Clone();

			// Determine if there are any boss elevator(s) in the level.
			// This is important for determineing if we can randomize a boss or not.
			// If there is no boss elevator, the boss will have no effect when killed, so we can randomize it.
			DfLevelInformation.Item[] bossElev = LevelLoader.Instance.Information.Items
				.Where(x => x.Sector?.Name?.ToUpper() == "BOSS" && x.Wall == null).ToArray();
			DfLevelInformation.Item[] mohcElev = LevelLoader.Instance.Information.Items
				.Where(x => x.Sector?.Name?.ToUpper() == "MOHC" && x.Wall == null).ToArray();

			// Cache bounds of level and sectors to more efficiently locate a sector containing a specific point.
			Rect levelBounds = default;
			Dictionary<DfLevel.Sector, Rect> sectorBounds = new Dictionary<DfLevel.Sector, Rect>();
			// Cache the generated floor/ceiling tris for each sector so we only have to generate them once.
			Dictionary<DfLevel.Sector, int[]> tris = new Dictionary<DfLevel.Sector, int[]>();

			// Use this regex to detect elevator sectors, but only types that change the sector geometry.
			// We don't want to put enemies or items in these sectors since it's easy to break a sector
			// With a first step giving it a small height
			// (placing an enemy/item causes this stop to fail and it will be in the open position).
			Regex elevator = new Regex(@"^\s*class:\s*elevator\s*(basic|inv|move_floor|move_ceiling|basic_auto|morph_move1|morph_move2|morph_spin1|morph_spin2|move_wall|rotate_wall|door|door_mid|door_inv)\b", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

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
					new Dictionary<string[], List<DfLevelObjects.Object>>();
				// Existing locations we removed existing spawned objects from, which we can spawn new objects in.
				List<(System.Numerics.Vector3 position, System.Numerics.Vector3 rotation)> existingSpawns =
					new List<(System.Numerics.Vector3 position, System.Numerics.Vector3 rotation)>();
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
				List<DfLevelObjects.Object> pairedBases = new List<DfLevelObjects.Object>();
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
					modified = true;
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

									Rect bounds = new Rect() {
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
							Vector2 point = new Vector2() {
								x = (float)this.rng.NextDouble() * levelBounds.width + levelBounds.x,
								y = (float)this.rng.NextDouble() * levelBounds.height + levelBounds.y
							};

							// Look for a sector the point falls within.
							List<DfLevel.Sector> candidates = new List<DfLevel.Sector>();
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
									LevelLoader.Instance.Information.Items.Any(x => x.Sector == candidate && x.Wall == null &&
									elevator.IsMatch(x.Script))) {

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

							Rect bounds = new Rect() {
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
						Dictionary<string, RandomRange> randomizedSettings = new Dictionary<string, RandomRange>() {
							["DELAY"] = settings.RandomizeGeneratorsDelay,
							["INTERVAL"] = settings.RandomizeGeneratorsInterval,
							["MIN_DIST"] = settings.RandomizeGeneratorsMinimumDistance,
							["MAX_DIST"] = settings.RandomizeGeneratorsMaximumDistance,
							["WANDER_TIME"] = settings.RandomizeGeneratorsWanderTime
						};

						Dictionary<string, float> floatProperties = new Dictionary<string, float>();
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
							float temp = floatProperties["MAX_DIST"];
							floatProperties["MAX_DIST"] = floatProperties["MIN_DIST"];
							floatProperties["MIN_DIST"] = temp;
						}

						foreach ((string key, float value) in floatProperties) {
							properties[key] = new[] { value.ToString() };
						}

						// integer values
						randomizedSettings = new Dictionary<string, RandomRange>() {
							["MAX_ALIVE"] = settings.RandomizeGeneratorsMaximumAlive,
							["NUM_TERMINATE"] = settings.RandomizeGeneratorsNumberTerminate
						};

						Dictionary<string, int> intProperties = new Dictionary<string, int>();
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
						DfLevelObjects.Object baseObj = new DfLevelObjects.Object() {
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
						modified = true;
					} else {
						spawnPosition.Y = sector.Floor.Y;
					}

					obj.Position = spawnPosition;
					obj.EulerAngles = spawnRotation;

					o.Objects.Add(obj);
					modified = true;
				}
			}

			// Randomize the bosses. We went to treat these separately due to boss elevators blocking level progress.
			// So we do not randomize position, only which boss.
			// We could also add Mohc into the mix, but then you have to rename the elevator, and fix any script that sends messages to it.
			// I decided not to bother with that; Mohc is never reandomized.
			if (settings.RandomizeBosses) {
				// We're doing the same thing we did for enemies but some of the options which aren't as useful are stripped out.
				Dictionary<string[], List<DfLevelObjects.Object>> spawnPool = new Dictionary<string[], List<DfLevelObjects.Object>>();
				Dictionary<string[], List<(System.Numerics.Vector3 position, System.Numerics.Vector3 rotation)>> existingBossSpawns =
					new Dictionary<string[], List<(System.Numerics.Vector3 position, System.Numerics.Vector3 rotation)>>();
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
					modified = true;
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

								Rect bounds = new Rect() {
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
					modified = true;
				}
			}

			// Remove checkpoints
			if (settings.RemoveCheckpoints) {
				foreach (DfLevelObjects.Object obj in o.Objects.Where(x => x.Type == DfLevelObjects.ObjectTypes.Safe).ToArray()) {
					o.Objects.Remove(obj);
					modified = true;
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
					new Dictionary<string[], List<DfLevelObjects.Object>>();
				List<(System.Numerics.Vector3 position, System.Numerics.Vector3 rotation)> existingSpawns =
					new List<(System.Numerics.Vector3 position, System.Numerics.Vector3 rotation)>();
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
					modified = true;
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

									Rect bounds = new Rect() {
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

							Vector2 point = new Vector2() {
								x = (float)this.rng.NextDouble() * levelBounds.width + levelBounds.x,
								y = (float)this.rng.NextDouble() * levelBounds.height + levelBounds.y
							};

							List<DfLevel.Sector> candidates = new List<DfLevel.Sector>();
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
									LevelLoader.Instance.Information.Items.Any(x => x.Sector == candidate && x.Wall == null &&
									elevator.IsMatch(x.Script))) {

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

							Rect bounds = new Rect() {
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
					modified = true;
				}

				//bool infModified = false;
				// Remove key statements from door scripts.
				if (settings.UnlockAllDoorsAndIncludeKeysInSpawnLocationPool) {
					Regex keyRegex = new Regex(@"^\s*key:", RegexOptions.IgnoreCase | RegexOptions.Compiled);
					foreach (DfLevelInformation.Item item in LevelLoader.Instance.Information.Items) {
						item.Script = string.Join(Environment.NewLine, item.Script.Split('\r', '\n')
							.Select(x => keyRegex.IsMatch(x) ? "" : x)
							.Where(x => !string.IsNullOrWhiteSpace(x)));
						//infModified = true;
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

				obj.Position = player.Position;
				obj.Difficulty = award.Difficulty;

				for (int i = 0; i < award.Count; i++) {
					o.Objects.Add(obj.Clone());
					modified = true;
				}
			}

			if (modified) {
				return o;
			}

			return null;
		}

		/// <summary>
		/// If the user is building off of a mod, copy all the mod's files into the randomized GOB.
		/// </summary>
		/// <param name="output">The output GOB.</param>
		private async Task AddDependentModFilesAsync(DfGobContainer output) {
			// Store these paths in case the user loads this GOB in the future, we can pull them.
			this.Settings.ModSourcePaths = Mod.Instance.List.Values.ToDictionary(x => x.FilePath, x => x.Overrides);

			// Get existing files so we don't replace them by accident.
			HashSet<string> files = new HashSet<string>(output.Files.Select(x => x.name.ToUpper()));
			foreach (ModFile modFileInfo in Mod.Instance.List.Values.Reverse()) {
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

		public RandomizerSettings Settings { get; set; }

		private string lastFolder = null;
		
		/// <summary>
		/// Take the settings selected and build the randomized GOB.
		/// </summary>
		public async Task<string> BuildAsync() {
			// Reset campaign spawn pools so they're recalculated again.
			this.enemySpawnPool = null;
			this.bossSpawnPool = null;
			this.itemSpawnPool = null;
			this.levGeneratedGlobalValues = false;
			this.palGeneratedGlobalValues = false;
			this.cmpGeneratedGlobalValues = false;

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
				if (!this.Settings.FixedSeed) {
					this.GenerateNewSeed();
				}

				this.rng = new Random(this.Settings.Seed);

				DfGobContainer gob = new DfGobContainer();

				DfLevelList levels = this.RandomizeJediLvl();
				await gob.AddFileAsync("JEDI.LVL", levels);

				DfCutsceneList cutscenes = this.RandomizeCutscenes();
				if (cutscenes != null) {
					await gob.AddFileAsync("CUTSCENE.LST", cutscenes);
				}

				await this.RandomizeMusicAsync(gob);

				foreach (string levelName in this.Settings.JediLvl.Levels) {
					await this.LoadLevelAsync(levelName);

					DfPalette pal = this.RandomizeLevelPalette();
					if (pal != null) {
						await gob.AddFileAsync($"{levelName}.PAL", pal);
					}

					DfColormap cmp = this.RandomizeLevelColormap();
					if (cmp != null) {
						await gob.AddFileAsync($"{levelName}.CMP", cmp);
					}

					DfLevel level = this.RandomizeLevel();
					if (level != null) {
						await gob.AddFileAsync($"{levelName}.LEV", level);
					}

					DfLevelObjects o = await this.RandomizeLevelObjectsAsync();
					if (o != null) {
						await gob.AddFileAsync($"{levelName}.O", o);
						if (this.Settings.Object.UnlockAllDoorsAndIncludeKeysInSpawnLocationPool) {
							await gob.AddFileAsync($"{levelName}.INF", LevelLoader.Instance.Information);
						}
					}
				}

				await this.AddDependentModFilesAsync(gob);

				DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(RandomizerSettings), new DataContractJsonSerializerSettings() {
					UseSimpleDictionaryFormat = true
				});
				using (MemoryStream jsonStream = new MemoryStream()) {
					serializer.WriteObject(jsonStream, this.Settings);
					jsonStream.Position = 0;

					await gob.AddFileAsync("RNDMIZER.JSO", jsonStream);
				}

				using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None)) {
					await gob.SaveAsync(stream);
				}
			} catch (Exception ex) {
				ResourceCache.Instance.AddError("Randomizer", ex);
				path = null;
			}

			await LevelLoader.Instance.ShowWarningsAsync(Mod.Instance.Gob ?? "DARK.GOB");

			PauseMenu.Instance.EndLoading();

			return path;
		}
	}

	/// <summary>
	/// The randomized settings.
	/// Find the places in the code these settings are used for more details on what they mean.
	/// </summary>
	public class RandomizerSettings : ICloneable {
		public int Version { get; set; } = 1;
		public bool FixedSeed { get; set; } = false;
		public int Seed { get; set; }

		public RandomizerJediLvlSettings JediLvl { get; set; } = new RandomizerJediLvlSettings();
		public RandomizerCutscenesSettings Cutscenes { get; set; } = new RandomizerCutscenesSettings();
		public RandomizerMusicSettings Music { get; set; } = new RandomizerMusicSettings();
		public RandomizerLevelSettings Level { get; set; } = new RandomizerLevelSettings();
		public RandomizerObjectSettings Object { get; set; } = new RandomizerObjectSettings();
		public RandomizerPaletteSettings Palette { get; set; } = new RandomizerPaletteSettings();
		public RandomizerColormapSettings Colormap { get; set; } = new RandomizerColormapSettings();
		public Dictionary<string, string> ModSourcePaths { get; set; }

		object ICloneable.Clone() => this.Clone();
		public RandomizerSettings Clone() => new RandomizerSettings() {
			Colormap = this.Colormap.Clone(),
			Cutscenes = this.Cutscenes.Clone(),
			JediLvl = this.JediLvl.Clone(),
			Level = this.Level.Clone(),
			ModSourcePaths = this.ModSourcePaths.ToDictionary(x => x.Key, x => x.Value),
			Music = this.Music.Clone(),
			Object = this.Object.Clone(),
			Palette = this.Palette.Clone(),
			FixedSeed = this.FixedSeed,
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
		public RandomizerJediLvlSettings Clone() => new RandomizerJediLvlSettings() {
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
		public RandomRange Clone() => new RandomRange() {
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
		public RandomizerCutscenesSettings Clone() => new RandomizerCutscenesSettings() {
			AdjustCutsceneMusicVolume = this.AdjustCutsceneMusicVolume,
			AdjustCutsceneSpeed = this.AdjustCutsceneSpeed.Clone(),
			RemoveCutscenes = this.RemoveCutscenes
		};
	}

	public class RandomizerMusicSettings : ICloneable {
		public bool RandomizeTrackOrder { get; set; }

		object ICloneable.Clone() => this.Clone();
		public RandomizerMusicSettings Clone() => new RandomizerMusicSettings() {
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
		public RandomizerPaletteSettings Clone() => new RandomizerPaletteSettings() {
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
		public RandomizerColormapSettings Clone() => new RandomizerColormapSettings() {
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
		public RandomizerLevelSettings Clone() => new RandomizerLevelSettings() {
			LightLevelMultiplier = this.LightLevelMultiplier.Clone(),
			LightLevelMultiplierPerLevel = this.LightLevelMultiplierPerLevel,
			MapOverrideMode = this.MapOverrideMode,
			RemoveSecrets	= this.RemoveSecrets
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
		public DifficultySpawnWeight Clone() => new DifficultySpawnWeight() {
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
		public LogicSpawnWeight Clone() => new LogicSpawnWeight() {
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
		public ItemAward Clone() => new ItemAward() {
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
		public ObjectTemplate Clone() => new ObjectTemplate() {
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
		public RandomizerObjectSettings Clone() => new RandomizerObjectSettings() {
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