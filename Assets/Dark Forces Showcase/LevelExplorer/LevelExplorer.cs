using System.Linq;
using System.Threading.Tasks;

namespace MZZT.DarkForces.Showcase {
	/// <summary>
	/// Script which powers the Level Explorer showcase.
	/// </summary>
	public class LevelExplorer : Singleton<LevelExplorer> {
		private async void Start() {
			// This is here in case you run directly from the LevelExplorer sccene instead of the menu.
			if (!FileLoader.Instance.Gobs.Any()) {
				await FileLoader.Instance.LoadStandardGobFilesAsync();
			}

			await PauseMenu.Instance.BeginLoadingAsync();

			ResourceCache.Instance.ClearWarnings();

			await LevelLoader.Instance.LoadLevelListAsync(true);

			await LevelLoader.Instance.ShowWarningsAsync("JEDI.LVL");

			if (LevelLoader.Instance.CurrentLevelIndex >= 0) {
				await this.LoadAndRenderLevelAsync(LevelLoader.Instance.CurrentLevelIndex);
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
