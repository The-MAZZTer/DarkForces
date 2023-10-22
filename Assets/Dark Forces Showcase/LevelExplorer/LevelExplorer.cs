using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace MZZT.DarkForces.Showcase {
	/// <summary>
	/// Script which powers the Level Explorer showcase.
	/// </summary>
	public class LevelExplorer : Singleton<LevelExplorer> {
		public static string StartLevel { get; set; }

		private async void Start() {
			// This is here in case you run directly from the LevelExplorer sccene instead of the menu.
			if (!FileLoader.Instance.Gobs.Any()) {
				await FileLoader.Instance.LoadStandardFilesAsync();
			}

			await PauseMenu.Instance.BeginLoadingAsync();

			ResourceCache.Instance.ClearWarnings();

			bool mute = PlayerPrefs.GetInt("PlayMusic", 1) == 0;
			float volume = PlayerPrefs.GetFloat("Volume", 1);
			foreach (AudioSource source in LevelMusic.Instance.GetComponentsInChildren<AudioSource>(true)) {
				source.mute = mute;
				source.volume = volume;
			}

			await LevelLoader.Instance.LoadLevelListAsync(true);

			await LevelLoader.Instance.ShowWarningsAsync("JEDI.LVL");

			int index = -1;
			if (StartLevel != null) {
				index = LevelLoader.Instance.LevelList.Levels.TakeWhile(x => string.Compare(x.FileName, StartLevel, true) != 0).Count();
				if (index >= LevelLoader.Instance.LevelList.Levels.Count) {
					index = -1;
				}
				StartLevel = null;
			}
			if (index < 0 && LevelLoader.Instance.CurrentLevelIndex >= 0) {
				index = LevelLoader.Instance.CurrentLevelIndex;
			}
			if (index >= 0) {
				await this.LoadAndRenderLevelAsync(index);
			}

			PauseMenu.Instance.EndLoading();
		}

		/// <summary>
		/// Load a level and generate Unity objects.
		/// </summary>
		public async Task LoadAndRenderLevelAsync(int levelIndex) {
			// Clear out existing level data.
			LevelMusic.Instance.Stop();
			LevelGeometryGenerator.Instance.Clear();
			ObjectGenerator.Instance.Clear();

			await PauseMenu.Instance.BeginLoadingAsync();

			await LevelMusic.Instance.PlayAsync(levelIndex);

			await LevelLoader.Instance.LoadLevelAsync(levelIndex);
			if (LevelLoader.Instance.Level != null) {
				await LevelLoader.Instance.LoadColormapAsync();
				if (LevelLoader.Instance.ColorMap != null) {
					await LevelLoader.Instance.LoadPaletteAsync();
					if (LevelLoader.Instance.Palette != null) {
						await LevelGeometryGenerator.Instance.GenerateAsync();

						await LevelLoader.Instance.LoadObjectsAsync();
						if (LevelLoader.Instance.Objects != null) {
							await ObjectGenerator.Instance.GenerateAsync();
						}
					}
				}
			}

			await LevelLoader.Instance.ShowWarningsAsync();

			PauseMenu.Instance.EndLoading();
		}
	}
}
