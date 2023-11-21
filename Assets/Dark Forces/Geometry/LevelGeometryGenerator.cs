using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using static MZZT.DarkForces.FileFormats.DfLevel;
using Debug = UnityEngine.Debug;

namespace MZZT.DarkForces {
	/// <summary>
	/// Generate level geometry.
	/// </summary>
	public class LevelGeometryGenerator : Singleton<LevelGeometryGenerator> {
		/// <summary>
		/// 1 DFU is about 25cm. 1 Unity unit is 1 meter. So about 1/40 scale.
		/// Scale to Unity size for physics reasons.
		/// </summary>
		public const float GEOMETRY_SCALE =  1 / 40f;
		/// <summary>
		/// Scale texture size to match DF.
		/// </summary>
		public const float TEXTURE_SCALE = 1 / 8f;

		[SerializeField, Header("Layers")]
		private bool showAllLayers = true;
		/// <summary>
		/// Show all layers.
		/// </summary>
		public bool ShowAllLayers { get => this.showAllLayers; set => this.showAllLayers = value; }
		[SerializeField]
		private int layer = 0;
		/// <summary>
		/// Show specific layer.
		/// </summary>
		public int Layer { get => this.layer; set => this.layer = value; }

		/// <summary>
		/// Remove all generated geometry.
		/// </summary>
		public void Clear() {
			foreach (GameObject child in this.transform.Cast<Transform>().Select(x => x.gameObject).ToArray()) {
				DestroyImmediate(child);
			}
		}

		/// <summary>
		/// Generate geometry for level.
		/// </summary>
		public async Task GenerateAsync() {
			this.Clear();

			Stopwatch watch = new();
			watch.Start();

			foreach ((Sector sectorInfo, int i) in LevelLoader.Instance.Level.Sectors.Select((x, i) => (x, i))) {
				GameObject sector = new() {
					name = sectorInfo.Name ?? i.ToString(),
					layer = LayerMask.NameToLayer("Geometry")
				};
				sector.transform.SetParent(this.transform);

				SectorRenderer renderer = sector.AddComponent<SectorRenderer>();
				await renderer.RenderAsync(sectorInfo);

				if (!this.showAllLayers) {
					sector.SetActive(this.layer == sectorInfo.Layer);
				}
			}

			watch.Stop();
			Debug.Log($"Level geometry generated in {watch.Elapsed}!");
		}

		public async Task<SectorRenderer> RefreshSectorAsync(int sectorIndex, Sector sectorInfo) {
			if (this.transform.childCount > sectorIndex) {
				this.DeleteSector(sectorIndex);
			}

			GameObject sector = new() {
				name = sectorInfo.Name ?? LevelLoader.Instance.Level.Sectors.IndexOf(sectorInfo).ToString(),
				layer = LayerMask.NameToLayer("Geometry")
			};
			sector.transform.SetParent(this.transform);
			sector.transform.SetSiblingIndex(sectorIndex);

			SectorRenderer renderer = sector.AddComponent<SectorRenderer>();
			await renderer.RenderAsync(sectorInfo);

			if (!this.showAllLayers) {
				sector.SetActive(this.layer == sectorInfo.Layer);
			}
			return renderer;
		}

		public void DeleteSector(int sectorIndex) {
			DestroyImmediate(this.transform.GetChild(sectorIndex).gameObject);
		}

		/// <summary>
		/// Change visibility of sectors based on layer selection.
		/// </summary>
		public void RefreshVisiblity() {
			foreach (SectorRenderer renderer in this.GetComponentsInChildren<SectorRenderer>(true)) {
				if (this.showAllLayers) {
					renderer.gameObject.SetActive(true);
					continue;
				}

				renderer.gameObject.SetActive(renderer.Sector.Layer == this.layer);
			}
		}
	}
}
