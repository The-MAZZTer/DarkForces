using MZZT.DarkForces.FileFormats;
using MZZT.DarkForces.Showcase;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace MZZT.DarkForces {
	/// <summary>
	/// Loads a level and all related data.
	/// </summary>
	public class LevelLoader : Singleton<LevelLoader> {
		/// <summary>
		/// The data from the LEV file.
		/// </summary>
		public DfLevel Level { get; private set; }
		/// <summary>
		/// The data from the INF file.
		/// </summary>
		public DfLevelInformation Information { get; private set; }
		/// <summary>
		/// The data from the O file.
		/// </summary>
		public DfLevelObjects Objects { get; private set; }
		/// <summary>
		/// The data from the PAL file.
		/// </summary>
		public DfPalette Palette { get; private set; }
		/// <summary>
		/// The data from the CMP file.
		/// </summary>
		public DfColormap ColorMap { get; private set; }

		[SerializeField, Header("Level")]
		private int currentLevel = -1;
		/// <summary>
		/// The current level index in the level list.
		/// </summary>
		public int CurrentLevelIndex => this.currentLevel;
		/// <summary>
		/// The current level display name.
		/// </summary>
		public string CurrentLevelName => this.currentLevel >= 0 ?
			this.LevelList.Levels[this.currentLevel].FileName : null;

		/// <summary>
		/// The data from JEVI.LVL.
		/// </summary>
		public DfLevelList LevelList { get; private set; }

		/// <summary>
		/// Load JEDI.LVL.
		/// </summary>
		public async Task LoadLevelListAsync(bool addHiddenLevels = false) {
			this.LevelList = null;

			await PauseMenu.Instance.BeginLoadingAsync();

			try {
				this.LevelList = await FileLoader.Instance.LoadGobFileAsync<DfLevelList>("JEDI.LVL");
			} catch (Exception e) {
				ResourceCache.Instance.AddError("JEDI.LVL", e);

				this.LevelList = new DfLevelList();
			}

			if (addHiddenLevels) {
				// Find the GOB file with the levels.
				string path = Mod.Instance.Gob ?? Path.Combine(FileLoader.Instance.DarkForcesFolder, "DARK.GOB");
				// Find any .LEV files in that GOB.
				string[] levels = FileLoader.Instance.FindGobFiles("*.LEV", path).Select(x => x.ToUpper()).ToArray();
				// Exclude any files in the level list.
				levels = levels.Except(this.LevelList.Levels.Select(x => $"{x.FileName.ToUpper()}.LEV")).ToArray();
				// Add any files in the GOB but not in the level list so the user can view them.
				this.LevelList.Levels.AddRange(levels.Select(x => new DfLevelList.Level() {
					FileName = Path.GetFileNameWithoutExtension(x),
					DisplayName = $"{Path.GetFileNameWithoutExtension(x)} (Unused)"
				}));
			}

			PauseMenu.Instance.EndLoading();
		}

		/// <summary>
		/// Load a level's CMP file.
		/// </summary>
		public async Task LoadColormapAsync() {
			this.ColorMap = null;

			await PauseMenu.Instance.BeginLoadingAsync();

			string levelFile = this.CurrentLevelName;

			this.ColorMap = await ResourceCache.Instance.GetColormapAsync($"{levelFile}.CMP");

			PauseMenu.Instance.EndLoading();
		}

		/// <summary>
		/// Load a level's PAL file.
		/// </summary>
		public async Task LoadPaletteAsync() {
			this.Palette = null;

			await PauseMenu.Instance.BeginLoadingAsync();

			this.Palette = await ResourceCache.Instance.GetPaletteAsync(this.Level.PaletteFile);

			PauseMenu.Instance.EndLoading();
		}

		/// <summary>
		/// Load a level's LEV file.
		/// </summary>
		/// <param name="levelIndex">The index of the level in JEDI.LVL.</param>
		public async Task LoadLevelAsync(int levelIndex) {
			if (Parallaxer.Instance != null) {
				Parallaxer.Instance.Reset();
			}

			this.Level = null;

			this.currentLevel = levelIndex;

			await PauseMenu.Instance.BeginLoadingAsync();

			string levelFile = this.CurrentLevelName;

			try {
				this.Level = await FileLoader.Instance.LoadGobFileAsync<DfLevel>($"{levelFile}.LEV");
			} catch (Exception e) {
				ResourceCache.Instance.AddError($"{levelFile}.LEV", e);
			}
			if (this.Level != null) {
				ResourceCache.Instance.AddWarnings($"{levelFile}.LEV", this.Level);

				if (Parallaxer.Instance != null) {
					Parallaxer.Instance.Parallax = this.Level.Parallax.ToUnity();
				}
			}

			PauseMenu.Instance.EndLoading();
		}

		/// <summary>
		/// Load a level's INF file.
		/// </summary>
		public async Task LoadInformationAsync() {
			this.Information = null;

			await PauseMenu.Instance.BeginLoadingAsync();

			string levelFile = this.CurrentLevelName;

			try {
				this.Information = await FileLoader.Instance.LoadGobFileAsync<DfLevelInformation>($"{levelFile}.INF");
				if (this.Level != null) {
					this.Information.LoadSectorReferences(this.Level);
				}
			} catch (Exception ex) {
				ResourceCache.Instance.AddError($"{levelFile}.INF", ex);
			}
			if (this.Information != null) {
				ResourceCache.Instance.AddWarnings($"{levelFile}.INF", this.Information);
			}

			PauseMenu.Instance.EndLoading();
		}

		/// <summary>
		/// Load a level's O file.
		/// </summary>
		public async Task LoadObjectsAsync() {
			this.Objects = null;

			await PauseMenu.Instance.BeginLoadingAsync();

			string levelFile = this.CurrentLevelName;

			try {
				this.Objects = await FileLoader.Instance.LoadGobFileAsync<DfLevelObjects>($"{levelFile}.O");
			} catch (Exception e) {
				ResourceCache.Instance.AddError($"{levelFile}.O", e);
			}
			if (this.Objects != null) {
				ResourceCache.Instance.AddWarnings($"{levelFile}.O", this.Objects);
			}

			PauseMenu.Instance.EndLoading();
		}

		/// <summary>
		/// Show any accumulated errors/warnings in file loading and clear them out.
		/// </summary>
		/// <param name="name">Filename to associate with the errors, null to use the current level name.</param>
		public async Task ShowWarningsAsync(string name = null) {
			ResourceCache.LoadWarning[] warnings = ResourceCache.Instance.Warnings.ToArray();
			if (warnings.Length == 0) {
				return;
			}

			if (name == null) {
				name = this.CurrentLevelName;
			}
			// Show fatal and non-fatal errors that occurred loading level data and generating Unity objects.
			string fatal = string.Join("\n", warnings
				.Where(x => x.Fatal)
				.Select(x => $"{x.FileName}{(x.Line > 0 ? $":{x.Line}" : "")} - {x.Message}"));
			string warning = string.Join("\n", warnings
				.Where(x => !x.Fatal)
				.Select(x => $"{x.FileName}{(x.Line > 0 ? $":{x.Line}" : "")} - {x.Message}"));
			if (fatal.Length > 0) {
				if (warning.Length > 0) {
					await DfMessageBox.Instance.ShowAsync($"{name} failed to load:\n\n{fatal}\n{warning}");
				} else {
					await DfMessageBox.Instance.ShowAsync($"{name} failed to load:\n\n{fatal}");
				}
			} else {
				await DfMessageBox.Instance.ShowAsync($"{name} loaded with warnings:\n\n{warning}");
			}
			ResourceCache.Instance.ClearWarnings();
		}
	}
}
