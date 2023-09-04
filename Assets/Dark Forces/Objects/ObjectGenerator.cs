using MZZT.DarkForces.FileFormats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MZZT.DarkForces {
	/// <summary>
	/// Generate all the Unoty objects for all the Dark Forces level objects.
	/// </summary>
	public class ObjectGenerator : Singleton<ObjectGenerator> {
		/// <summary>
		/// Kyle's eye height from the ground (for the camera).
		/// </summary>
		public static readonly Vector3 KYLE_EYE_POSITION = new(0, -5.8f, 0);

		/// <summary>
		/// The difficulty to use to determine which objects to show.
		/// </summary>
		public enum Difficulties {
			None,
			Easy,
			Medium,
			Hard,
			All
		}

		[SerializeField, Header("Visibility")]
		private Difficulties difficulty;
		/// <summary>
		/// The difficulty to use to determine which objects to show.
		/// </summary>
		public Difficulties Difficulty {
			get => this.difficulty;
			set => this.difficulty = value;
		}

		[SerializeField]
		private bool showSprites = true;
		/// <summary>
		/// Whether or not to show sprites (FME/WAX).
		/// </summary>
		public bool ShowSprites {
			get => this.showSprites;
			set => this.showSprites = value;
		}

		[SerializeField]
		private bool show3dos = true;
		/// <summary>
		/// Whether or not to show 3DOs.
		/// </summary>
		public bool Show3dos {
			get => this.show3dos;
			set => this.show3dos = value;
		}

		[SerializeField]
		private bool animate3dos = true;
		/// <summary>
		/// Whether or not to forcibly play VUEs on a loop (for showcase)..
		/// </summary>
		public bool Animate3dos {
			get => this.animate3dos;
			set => this.animate3dos = value;
		}

		/// <summary>
		/// Delete all objects.
		/// </summary>
		public void Clear() {
			foreach (GameObject child in this.transform.Cast<Transform>().Select(x => x.gameObject).ToArray()) {
				DestroyImmediate(child);
			}
		}

		// Map the desired difficulty value into the values objects will specify for difficulty.
		private HashSet<DfLevelObjects.Difficulties> TranslateDifficulty() =>
			this.difficulty switch {

			Difficulties.None => new HashSet<DfLevelObjects.Difficulties>() {
			},
			Difficulties.Easy => new HashSet<DfLevelObjects.Difficulties>() {
					DfLevelObjects.Difficulties.Easy,
					DfLevelObjects.Difficulties.EasyMedium,
					DfLevelObjects.Difficulties.EasyMediumHard
				},
			Difficulties.Medium => new HashSet<DfLevelObjects.Difficulties>() {
					DfLevelObjects.Difficulties.EasyMedium,
					DfLevelObjects.Difficulties.EasyMediumHard,
					DfLevelObjects.Difficulties.MediumHard
				},
			Difficulties.Hard => new HashSet<DfLevelObjects.Difficulties>() {
					DfLevelObjects.Difficulties.EasyMediumHard,
					DfLevelObjects.Difficulties.MediumHard,
					DfLevelObjects.Difficulties.Hard
				},
			_ => new HashSet<DfLevelObjects.Difficulties>(
				Enum.GetValues(typeof(DfLevelObjects.Difficulties)).Cast<DfLevelObjects.Difficulties>()
			)
		};

		/// <summary>
		/// Generate Unity objects for all Dark Forces level objects.
		/// </summary>
		public async Task GenerateAsync() {
			this.Clear();

			HashSet<DfLevelObjects.Difficulties> allowedDifficulties = this.TranslateDifficulty();

			Stopwatch watch = new();
			watch.Start();
			foreach (DfLevelObjects.Object obj in LevelLoader.Instance.Objects.Objects) {
				GameObject go;
				ObjectRenderer renderer;
				switch (obj.Type) {
					// TODO default colliders when no custom ones specified
					// Do most objects even use cylinders? Some objects on DF's LACDS map show up as triangles.
					// Maybe the C in CDS stands for collider.
					// Sounds like the default size is based on the sprite size.
					case DfLevelObjects.ObjectTypes.Frame:
						go = new GameObject() {
							layer = LayerMask.NameToLayer("Objects")
						};
						renderer = go.AddComponent<FrameRenderer>();
						break;
					case DfLevelObjects.ObjectTypes.Sprite:
						go = new GameObject() {
							layer = LayerMask.NameToLayer("Objects")
						};
						renderer = go.AddComponent<WaxRenderer>();
						break;
					case DfLevelObjects.ObjectTypes.ThreeD:
						Df3dObject threeDo = await ResourceCache.Instance.Get3dObjectAsync(obj.FileName);
						if (threeDo == null) {
							continue;
						}

						go = ResourceCache.Instance.Import3dObject(threeDo);
						go = Instantiate(go);
						go.layer = LayerMask.NameToLayer("Objects");
						renderer = go.GetComponent<ThreeDoRenderer>();
						break;
					default:
						go = new GameObject() {
							layer = LayerMask.NameToLayer("Objects")
						};
						renderer = go.AddComponent<ObjectRenderer>();
						break;
				}

				await renderer.RenderAsync(obj);
				this.SetVisible(renderer, allowedDifficulties);
				go.transform.SetParent(this.transform, true);
			}

			watch.Stop();
			Debug.Log($"Objects generated in {watch.Elapsed}!");
		}

		private void SetVisible(ObjectRenderer renderer, HashSet<DfLevelObjects.Difficulties> allowedDifficulties) {
			if ((renderer is FrameRenderer || renderer is WaxRenderer) && !this.showSprites) {
				renderer.gameObject.SetActive(false);
				return;
			}
			if (renderer is ThreeDoRenderer && !this.show3dos) {
				renderer.gameObject.SetActive(false);
				return;
			}

			DfLevelObjects.Object obj = renderer.Object;
			if (!allowedDifficulties.Contains(obj.Difficulty)) {
				renderer.gameObject.SetActive(false);
				return;
			}

			SectorRenderer sector = renderer.CurrentSector;
			bool sectorVisible = LevelGeometryGenerator.Instance.ShowAllLayers || (sector != null &&
				sector.Sector.Layer == LevelGeometryGenerator.Instance.Layer);
			renderer.gameObject.SetActive(sectorVisible);
		}

		/// <summary>
		/// Change objects visiblitiy based on difficulty and other flags.
		/// </summary>
		public void RefreshVisiblity() {
			HashSet<DfLevelObjects.Difficulties> allowedDifficulties = this.TranslateDifficulty();
			
			foreach (ObjectRenderer renderer in this.GetComponentsInChildren<ObjectRenderer>(true)) {
				this.SetVisible(renderer, allowedDifficulties);
			}
		}
	}
}
