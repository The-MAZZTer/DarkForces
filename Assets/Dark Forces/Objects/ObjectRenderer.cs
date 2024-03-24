﻿using MZZT.DarkForces.FileFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace MZZT.DarkForces {
	/// <summary>
	/// Base class for rendering an object in Unity.
	/// </summary>
	public class ObjectRenderer : MonoBehaviour {
		public static ObjectRenderer Eye { get; private set; }

		/// <summary>
		/// The object we're rendering.
		/// </summary>
		public DfLevelObjects.Object Object { get; private set; }

		/// <summary>
		/// The sector we think the object is currently in.
		/// </summary>
		public SectorRenderer CurrentSector { get; protected set; }

		/// <summary>
		/// Generate the visual representation of the object in Unity.
		/// </summary>
		/// <param name="obj">The object.</param>
		public virtual Task RenderAsync(DfLevelObjects.Object obj) {
			this.Object = obj;

			this.gameObject.name = string.IsNullOrEmpty(obj.FileName) ? obj.Type.ToString() : $"{obj.Type} - {obj.FileName}";

			const float geometryScale = LevelGeometryGenerator.GEOMETRY_SCALE;
			Vector3 position = new(
				obj.Position.X * geometryScale,
				-obj.Position.Y * geometryScale,
				obj.Position.Z * geometryScale
			);
			Vector3 euler = obj.EulerAngles.ToUnity();
			euler = new Vector3(-euler.x, euler.y, euler.z);
			Quaternion rotation = Quaternion.Euler(euler);
			this.gameObject.transform.SetPositionAndRotation(position, rotation);

			// Grab item logic.
			string[] lines = obj.Logic?.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
			Dictionary<string, string[]> logic = lines.SelectMany(x => TextBasedFile.SplitKeyValuePairs(TextBasedFile.TokenizeLine(x)))
				.GroupBy(x => x.Key.ToUpper()).ToDictionary(x => x.Key, x => x.Last().Value);

			float radius = 0;
			float height = 0;

			// Look down and then up and try to find a sector floor/ceiling. Then verify we're in the proper Y range.
			SectorRenderer sector = this.FindCurrentSector();
			this.CurrentSector = sector;
			if (sector == null) {
				ResourceCache.Instance.AddWarning($"{LevelLoader.Instance.CurrentLevelName}.O",
					$"Failed to find sector for object {LevelLoader.Instance.Objects.Objects.IndexOf(obj)}.");
			}

			// TODO default radius/height values (for sprites).

			if (logic.TryGetValue("LOGIC", out string[] strLogic) && strLogic.Length > 0) {
				if (strLogic[0] == "PLAYER") {
					radius = 2.5f;
					height = 6.8f;
				}
			}

			if (logic.TryGetValue("RADIUS", out string[] strRadius) && strRadius.Length > 0) {
				float.TryParse(strRadius[0], out radius);
			}
			if (logic.TryGetValue("HEIGHT", out string[] strHeight) && strHeight.Length > 0) {
				float.TryParse(strHeight[0], out height);
			}
			if (radius > 0 && height != 0) {
				CapsuleCollider collider = this.gameObject.AddComponent<CapsuleCollider>();
				collider.center = new Vector3(0, -height / 2 * geometryScale, 0);
				collider.height = Mathf.Abs(height * geometryScale);
				collider.radius = radius * geometryScale;
			}

			bool eye = false;
			if (logic.TryGetValue("EYE", out string[] strEye) && strEye.Length > 0) {
				bool.TryParse(strEye[0], out eye);
			}
			if (eye) {
				Eye = this;
			}

			return Task.CompletedTask;
		}

		protected SectorRenderer FindCurrentSector() {
			SectorRenderer sector = null;
			int layer = LayerMask.GetMask("Geometry");
			float y = this.Object.Position.Y;
			if (Physics.Raycast(new Ray(this.transform.position, Vector3.down), out RaycastHit hit, float.PositiveInfinity, layer)) {
				sector = hit.collider.GetComponentInParent<SectorRenderer>();
				if (y > sector.Sector.Floor.Y || y < sector.Sector.Ceiling.Y) {
					sector = null;
				}
			}
			if (sector == null) {
				if (Physics.Raycast(new Ray(this.transform.position, Vector3.up), out hit, float.PositiveInfinity, layer)) {
					sector = hit.collider.GetComponentInParent<SectorRenderer>();
					if (y > sector.Sector.Floor.Y || y < sector.Sector.Ceiling.Y) {
						sector = null;
					}
				}
			}
			return sector;
		}

		private void OnDestroy() {
			if (Eye == this) {
				Eye = null;
			}
		}
	}
}
