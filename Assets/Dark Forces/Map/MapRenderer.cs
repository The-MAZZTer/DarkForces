using MZZT.DarkForces.FileFormats;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DarkForces {
	/// <summary>
	/// This class loads level data and starts the map generation script.
	/// </summary>
	[RequireComponent(typeof(RectTransform), typeof(MapGenerator), typeof(RawImage))]
	public class MapRenderer : MonoBehaviour {
		[SerializeField]
		private int levelIndex = 0;

		[SerializeField]
		private float renderFactor = 1;

		private DfLevel lev;
		private DfLevelInformation inf;
		private async void Start() {
			DfLevelList lvl = await FileLoader.Instance.LoadGobFileAsync<DfLevelList>("JEDI.LVL");
			string levelFile = lvl.Levels[levelIndex].FileName;

			// TODO Add try/catch for fatal errors and display warnings.

			this.lev = await FileLoader.Instance.LoadGobFileAsync<DfLevel>($"{levelFile}.LEV");
			this.inf = await FileLoader.Instance.LoadGobFileAsync<DfLevelInformation>($"{levelFile}.INF");
			this.inf.LoadSectorReferences(this.lev);

			// RenderFactor renders the map at a higher resolution and scales it down for display.
			if (this.renderFactor != 1) {
				MapGenerator map = this.GetComponent<MapGenerator>();

				map.Zoom *= this.renderFactor;

				MapGenerator.LineProperties prop = map.Adjoined;
				prop.Width *= this.renderFactor;
				map.Adjoined = prop;

				prop = map.AdjoinedSameHeight;
				prop.Width *= this.renderFactor;
				map.AdjoinedSameHeight = prop;

				prop = map.Elevator;
				prop.Width *= this.renderFactor;
				map.Elevator = prop;

				prop = map.InactiveLayer;
				prop.Width *= this.renderFactor;
				map.InactiveLayer = prop;

				prop = map.SectorTrigger;
				prop.Width *= this.renderFactor;
				map.SectorTrigger = prop;

				prop = map.Unadjoined;
				prop.Width *= this.renderFactor;
				map.Unadjoined = prop;

				prop = map.WallTrigger;
				prop.Width *= this.renderFactor;
				map.WallTrigger = prop;
			}

			this.Render();
		}

		private void Render() {
			MapGenerator map = this.GetComponent<MapGenerator>();

			RawImage image = this.GetComponent<RawImage>();
			image.texture = map.Generate(this.lev, this.inf);

			((RectTransform)this.transform).sizeDelta = new Vector2(
				image.texture.width / this.renderFactor,
				image.texture.height / this.renderFactor
			);
		}
	}
}
