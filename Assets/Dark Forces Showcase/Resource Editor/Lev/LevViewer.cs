using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using MZZT.FileFormats;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
	class LevViewer : Databind<DfLevel>, IResourceViewer {
		[Header("LEV"), SerializeField]
		private Button launchLevelExplorer;
		[SerializeField]
		private TMP_Text wallCount;
		[SerializeField]
		private TMP_Text adjoinCount;
		[SerializeField]
		private TMP_Text textureCount;
		[SerializeField]
		private TMP_Text signCount;
		[SerializeField]
		private TMP_Text layerCount;
		[SerializeField]
		private TMP_Text sectorHeightAverage;
		[SerializeField]
		private TMP_Text wallAverage;
		[SerializeField]
		private TMP_Text adjoinAverage;
		[SerializeField]
		private TMP_Text sectorLightAverage;
		[SerializeField]
		private TMP_Text wallLightAverage;

		public string TabName => this.filePath == null ? "New LEV" : Path.GetFileName(this.filePath);
#pragma warning disable CS0067
		public event EventHandler TabNameChanged;
#pragma warning restore CS0067

		public Sprite Thumbnail { get; private set; }
#pragma warning disable CS0067
		public event EventHandler ThumbnailChanged;
#pragma warning restore CS0067

		public void ResetDirty() {
			if (!this.IsDirty) {
				return;
			}

			this.IsDirty = false;
			this.IsDirtyChanged?.Invoke(this, new());
		}

		public void OnDirty() {
			if (this.IsDirty) {
				return;
			}

			this.IsDirty = true;
			this.IsDirtyChanged?.Invoke(this, new());
		}

		public bool IsDirty { get; private set; }
		public event EventHandler IsDirtyChanged;

		private string filePath;
		public Task LoadAsync(ResourceEditorResource resource, IFile file) {
			this.filePath = resource?.Path;

			this.Value = (DfLevel)file;

			this.wallCount.text = $"Walls: {this.Value.Sectors.Sum(x => x.Walls.Count)}";
			this.adjoinCount.text = $"Adjoins: {this.Value.Sectors.Sum(x => x.Walls.Count(x => x.Adjoined != null))}";
			int textureCount = this.Value.Sectors.SelectMany(x => new[] { x.Ceiling.TextureFile, x.Floor.TextureFile }
				.Concat(x.Walls.SelectMany(x => new[] { x.BottomEdgeTexture.TextureFile, x.MainTexture.TextureFile, x.SignTexture.TextureFile, x.TopEdgeTexture.TextureFile })))
				.Where(x => x != null)
				.Distinct()
				.Count();
			this.textureCount.text = $"Textures: {textureCount}";
			this.signCount.text = $"Signs: {this.Value.Sectors.Sum(x => x.Walls.Where(x => x.SignTexture.TextureFile != null).Count())}";
			this.layerCount.text = $"Layers: {this.Value.Sectors.Select(x => x.Layer).Distinct().Sum()}";
			this.sectorHeightAverage.text = $"Sector Height: {this.Value.Sectors.Average(x => x.Floor.Y - x.Ceiling.Y):0.0}";
			this.wallAverage.text = $"Walls Per Sector: {this.Value.Sectors.Average(x => x.Walls.Count):0.0}";
			this.adjoinAverage.text = $"Adjoins Per Sector: {this.Value.Sectors.Average(x => x.Walls.Where(x => x.Adjoined != null).Count()):0.0}";
			this.sectorLightAverage.text = $"Sector Light: {this.Value.Sectors.Average(x => x.LightLevel):0.0}";
			double wallLightAverage = this.Value.Sectors.SelectMany(x => x.Walls.Select(y => (x.LightLevel, y)))
				.Average(x => (Math.Clamp(x.LightLevel + x.y.LightLevel, 0, 31)));
			this.wallLightAverage.text = $"Wall Light: {wallLightAverage:0.0}";

			this.launchLevelExplorer.interactable = resource?.PartOfCurrentMod ?? false;

			return Task.CompletedTask;
		}

		public void LaunchLevelExplorer() {
			LevelExplorer.StartLevel = Path.GetFileNameWithoutExtension(this.filePath);
			SceneManager.LoadScene("LevelExplorer");
		}
	}
}
