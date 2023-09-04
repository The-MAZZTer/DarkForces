using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using static MZZT.DarkForces.FileFormats.DfLevel;

namespace MZZT.DarkForces {
	/// <summary>
	/// Creates geometry for a sector.
	/// </summary>
	public class SectorRenderer : MonoBehaviour {
		/// <summary>
		/// The sector associated with this renderer.
		/// </summary>
		public Sector Sector { get; private set; }

		/// <summary>
		/// Create geometry for the sector.
		/// </summary>
		/// <param name="sector">The sector.</param>
		public async Task RenderAsync(Sector sector) {
			this.Sector = sector;

			this.gameObject.name = sector.Name ?? LevelLoader.Instance.Level.Sectors.IndexOf(sector).ToString();

			this.transform.position = new Vector3(0, -sector.Floor.Y * LevelGeometryGenerator.GEOMETRY_SCALE, 0);

			FloorCeilingRenderer floorCeiling = this.gameObject.AddComponent<FloorCeilingRenderer>();
			await floorCeiling.RenderAsync(sector);

			foreach ((Wall wallInfo, int j) in sector.Walls.Select((x, i) => (x, i))) {
				GameObject wall = new() {
					name = j.ToString(),
					layer = LayerMask.NameToLayer("Geometry")
				};
				wall.transform.SetParent(this.transform, false);

				WallRenderer wallRenderer = wall.AddComponent<WallRenderer>();
				await wallRenderer.RenderAsync(wallInfo);
			}
		}
	}
}
