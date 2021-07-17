using MZZT.DarkForces.FileFormats;
using MZZT.DarkForces.Showcase;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

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

		private async void Start() {
			await PauseMenu.Instance.BeginLoadingAsync();
			
			ResourceCache.Instance.ClearWarnings();

			// This is here in case you run directly from the LevelExplorer sccene instead of the menu.
			if (!FileLoader.Instance.Gobs.Any()) {
				await FileLoader.Instance.LoadStandardGobFilesAsync();
			}

			// Load the level list and display any warnings.
			try {
				this.LevelList = await FileLoader.Instance.LoadGobFileAsync<DfLevelList>("JEDI.LVL");
			} catch (Exception e) {
				ResourceCache.Instance.AddError("JEDI.LVL", e);

				this.LevelList = new DfLevelList();
			}

			ResourceCache.Instance.AddWarnings("JEDI.LVL", this.LevelList);

			await this.ShowWarningsAsync("JEDI.LVL");

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

			if (this.currentLevel >= 0) {
				await this.LoadAsync(this.currentLevel);
			}

			PauseMenu.Instance.EndLoading();
		}
		
		/// <summary>
		/// Load a level and generate Unity objects.
		/// </summary>
		/// <param name="levelIndex">The index of the level in JEDI.LVL.</param>
		public async Task LoadAsync(int levelIndex) {
			// Clear out existing level data.
			LevelMusic.Instance.Stop();
			Parallaxer.Instance.Reset();
			LevelGeometryGenerator.Instance.Clear();
			ObjectGenerator.Instance.Clear();

			this.ColorMap = null;
			this.Level = null;
			this.Palette = null;
			this.Objects = null;

			this.currentLevel = levelIndex;

			await PauseMenu.Instance.BeginLoadingAsync();

			await LevelMusic.Instance.PlayAsync(levelIndex);

			string levelFile = this.LevelList.Levels[levelIndex].FileName;

			// Load data and generate Unity objects.
			this.ColorMap = await ResourceCache.Instance.GetColormapAsync($"{levelFile}.CMP");
			if (this.ColorMap != null) {
				try {
					this.Level = await FileLoader.Instance.LoadGobFileAsync<DfLevel>($"{levelFile}.LEV");
				} catch (Exception e) {
					ResourceCache.Instance.AddError($"{levelFile}.LEV", e);
				}
				if (this.Level != null) {
					ResourceCache.Instance.AddWarnings($"{levelFile}.LEV", this.Level);

					Parallaxer.Instance.Parallax = this.Level.Parallax.ToUnity();

					this.Palette = await ResourceCache.Instance.GetPaletteAsync(this.Level.PaletteFile);
					if (this.Palette != null) {
						await LevelGeometryGenerator.Instance.GenerateAsync();

						try {
							this.Objects = await FileLoader.Instance.LoadGobFileAsync<DfLevelObjects>($"{levelFile}.O");
						} catch (Exception e) {
							ResourceCache.Instance.AddError($"{levelFile}.O", e);
						}

						if (this.Objects != null) {
							ResourceCache.Instance.AddWarnings($"{levelFile}.O", this.Objects);

							await ObjectGenerator.Instance.GenerateAsync();
						}
					}
				}
			}
 
			await this.ShowWarningsAsync(levelFile);

			PauseMenu.Instance.EndLoading();
			PlayerInput.all[0].SwitchCurrentActionMap("Player");
		}

		private async Task ShowWarningsAsync(string name) {
			ResourceCache.LoadWarning[] warnings = ResourceCache.Instance.Warnings.ToArray();
			if (warnings.Length == 0) {
				return;
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
