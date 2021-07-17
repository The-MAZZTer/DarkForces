using MZZT.DarkForces.FileFormats;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using static MZZT.DarkForces.FileFormats.DfLevel;

namespace MZZT.DarkForces {
	/// <summary>
	/// This class renders map geometry to a texture in an automap style.
	/// </summary>
	public class MapGenerator : MonoBehaviour {
		/// <summary>
		/// Properties for a line's style.
		/// </summary>
		[Serializable]
		public struct LineProperties {
			public LineProperties(Color color, int priority) {
				this.Width = 1;
				this.Color = color;
				this.Priority = priority;
			}

			/// <summary>
			/// The width of the line.
			/// </summary>
			public float Width;
			/// <summary>
			/// The color of the line.
			/// </summary>
			public Color Color;
			/// <summary>
			/// The Z-order of the line.
			/// </summary>
			public int Priority;

			/// <summary>
			/// Whether or not the settings would hide the line from being drawn.
			/// </summary>
			public bool Draw => this.Width > 0 && this.Color.a > 0;
		}

		[SerializeField]
		private LineProperties inactiveLayer = new LineProperties(Color.gray, 0);
		/// <summary>
		/// Walls for sectors on inactive layers.
		/// </summary>
		public LineProperties InactiveLayer { get => this.inactiveLayer; set => this.inactiveLayer = value; }
		[SerializeField]
		private LineProperties adjoinedSameHeight = new LineProperties(new Color(0, 0.5f, 0), 1);
		/// <summary>
		/// Adjoined walls where the sectors are the same floor height.
		/// </summary>
		public LineProperties AdjoinedSameHeight { get => this.adjoinedSameHeight; set => this.adjoinedSameHeight = value; }
		[SerializeField]
		private LineProperties adjoined = new LineProperties(new Color(0, 0.5f, 0), 2);
		/// <summary>
		/// Adjoined walls with a step up or down.
		/// </summary>
		public LineProperties Adjoined { get => this.adjoined; set => this.adjoined = value; }
		[SerializeField]
		private LineProperties unadjoined = new LineProperties(Color.green, 3);
		/// <summary>
		/// Simple unadjoined walls.
		/// </summary>
		public LineProperties Unadjoined { get => this.unadjoined; set => this.unadjoined = value; }
		[SerializeField]
		private LineProperties elevator = new LineProperties(new Color(1, 1, 0), 4);
		/// <summary>
		/// Walls in a elevator sector.
		/// </summary>
		public LineProperties Elevator { get => this.elevator; set => this.elevator = value; }
		[SerializeField]
		private LineProperties sectorTrigger = new LineProperties(Color.cyan, 5);
		/// <summary>
		/// Walls in a trigger sector.
		/// </summary>
		public LineProperties SectorTrigger { get => this.sectorTrigger; set => this.sectorTrigger = value; }
		[SerializeField]
		private LineProperties wallTrigger = new LineProperties(Color.cyan, 6);
		/// <summary>
		/// Trigger walls.
		/// </summary>
		public LineProperties WallTrigger { get => this.wallTrigger; set => this.wallTrigger = value; }

		[SerializeField]
		private int layer = 0;
		/// <summary>
		/// The layer to draw, if only one layer is to be drawn.
		/// </summary>
		public int Layer { get => this.layer; set => this.layer = value; }

		/// <summary>
		/// What to do with unselected layers.
		/// </summary>
		public enum UnselectedLayersRenderModes {
			/// <summary>
			/// Hide unselected layers.
			/// </summary>
			Hide,
			/// <summary>
			/// Draw other layers as inactive.
			/// </summary>
			ShowInactive,
			/// <summary>
			/// Show all layers equally.
			/// </summary>
			ShowColored
		}

		[SerializeField]
		private UnselectedLayersRenderModes unselectedLayersRenderMode = UnselectedLayersRenderModes.ShowInactive;
		/// <summary>
		/// What to do with unselected layers.
		/// </summary>
		public UnselectedLayersRenderModes UnselectedLayersRenderMode { get => this.unselectedLayersRenderMode; set => this.unselectedLayersRenderMode = value; }

		/// <summary>
		/// How to adjust the viewport of the map.
		/// </summary>
		public enum ViewportModes {
			/// <summary>
			/// Fit all visible lines.
			/// </summary>
			FitVisible,
			/// <summary>
			/// Fit to all layer bounds even if invisible.
			/// </summary>
			FitToAllLayers,
			/// <summary>
			/// Fit to current layer bounds even if it cuts off unactive layers.
			/// </summary>
			FitToCurrentLayer,
			/// <summary>
			/// Manually specify bounds.
			/// </summary>
			Manual
		}

		[SerializeField]
		private ViewportModes viewportMode;
		/// <summary>
		/// How to adjust the viewport of the map.
		/// </summary>
		public ViewportModes ViewportMode { get => this.viewportMode; set => this.viewportMode = value; }

		/// <summary>
		/// How much padding to add to the map viewport.
		/// </summary>
		public enum PaddingUnits {
			/// <summary>
			/// Texture pixels.
			/// </summary>
			Pixels,
			/// <summary>
			/// DF game units.
			/// </summary>
			GameUnits
		}

		[SerializeField]
		private PaddingUnits paddingUnit;
		/// <summary>
		/// How much padding to add to the map viewport.
		/// </summary>
		public PaddingUnits PaddingUnit { get => this.paddingUnit; set => this.paddingUnit = value; }

		[SerializeField]
		private Rect padding;
		/// <summary>
		/// How much padding to add to the map viewport on all sides.
		/// </summary>
		public Rect Padding { get => this.padding; set => this.padding = value; }

		[SerializeField]
		private float zoom = 1;
		/// <summary>
		/// The zoom level if the viewport if not automatic.
		/// </summary>
		public float Zoom { get => this.zoom; set => this.zoom = value; }

		[SerializeField]
		private bool autoZoomToFit;
		/// <summary>
		/// Automatically zooms to fit the desired viewport.
		/// </summary>
		public bool AutoZoomToFit { get => this.autoZoomToFit; set => this.autoZoomToFit = value; }

		[SerializeField]
		private Rect viewport;
		/// <summary>
		/// Manual viewport.
		/// </summary>
		public Rect Viewport { get => this.viewport; set => this.viewport = value; }

		[SerializeField]
		private float rotation;
		/// <summary>
		/// Rotate the map in degrees.
		/// </summary>
		public float Rotation { get => this.rotation; set => this.rotation = value; }

		/// <summary>
		/// Get the layers which have sectors in them.
		/// </summary>
		/// <param name="level">The level data.</param>
		/// <returns>The layers which have sectors in them.</returns>
		public static IEnumerable<int> GetLayers(DfLevel level) => level.Sectors.Select(x => x.Layer).Distinct();

		private struct Line {
			public LineProperties Properties;
			public Vector2 LeftVertex;
			public Vector2 RightVertex;
		}

		/// <summary>
		/// Generate a map texture.
		/// </summary>
		/// <param name="level">The level data.</param>
		/// <param name="inf">The level information.</param>
		/// <returns>The texture.</returns>
		public Texture2D Generate(DfLevel level, DfLevelInformation inf) {
			// Group sectors by layer.
			Dictionary<int, Sector[]> byLayer = level.Sectors
				.GroupBy(x => x.Layer).ToDictionary(x => x.Key, x => x.ToArray());

			// Filter sectors to visible ones.
			IEnumerable<Sector> visibleSectors;
			if (this.unselectedLayersRenderMode == UnselectedLayersRenderModes.Hide) {
				if (byLayer.TryGetValue(this.layer, out Sector[] sectors)) {
					visibleSectors = sectors;
				} else {
					visibleSectors = Enumerable.Empty<Sector>();
				}
			} else {
				visibleSectors = level.Sectors;
			}

			Dictionary<Wall, DfLevelInformation.Item[]> wallsScripts = inf.Items
				.Where(x => x.Type == DfLevelInformation.ScriptTypes.Line)
				.GroupBy(x => x.Wall)
				.ToDictionary(x => x.Key, x => x.ToArray());
			Dictionary<Sector, DfLevelInformation.Item[]> sectorsScripts = inf.Items
				.Where(x => x.Type == DfLevelInformation.ScriptTypes.Sector)
				.GroupBy(x => x.Sector)
				.ToDictionary(x => x.Key, x => x.ToArray());

			// Plan ALL the lines!
			Line[] lines = visibleSectors
				.SelectMany(x => x.Walls.Select(y => (x, y)))
				.Select(x => {
					float angle = Mathf.Deg2Rad * this.rotation;
					float sin = Mathf.Sin(angle);
					float cos = Mathf.Cos(angle);

					// Figure out which color etc we are using for this wall.
					LineProperties properties;
					bool isLayer = x.x.Layer == this.layer;
					if (this.unselectedLayersRenderMode == UnselectedLayersRenderModes.ShowInactive && !isLayer) {
						properties = this.inactiveLayer;
					} else if (wallsScripts.ContainsKey(x.y)) {
						properties = this.wallTrigger;
					} else if (sectorsScripts.TryGetValue(x.x, out DfLevelInformation.Item[] sectorScripts)) {
						if (sectorScripts.Any(x => {
							string[] lines = x.Script.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
							Dictionary<string, string[]> logic = lines.SelectMany(x => TextBasedFile.SplitKeyValuePairs(TextBasedFile.TokenizeLine(x)))
								.GroupBy(x => x.Key.ToUpper()).ToDictionary(x => x.Key, x => x.Last().Value);
							return logic.TryGetValue("CLASS", out string[] strClass) && strClass.Length > 0 && strClass[0].ToLower() == "elevator";
						})) {
							properties = this.elevator;
						} else {
							properties = this.sectorTrigger;
						}
					} else if (x.y.Adjoined == null) {
						properties = this.unadjoined;
					} else if (x.y.Adjoined.Sector.Floor.Y == x.x.Floor.Y) {
						properties = this.adjoinedSameHeight;
					} else {
						properties = this.adjoined;
					}

					Vector2 left = new Vector2() {
						x = x.y.LeftVertex.Position.X * cos - x.y.LeftVertex.Position.Y * sin,
						y = x.y.LeftVertex.Position.X * sin + x.y.LeftVertex.Position.Y * cos
					};
					Vector2 right = new Vector2() {
						x = x.y.RightVertex.Position.X * cos - x.y.RightVertex.Position.Y * sin,
						y = x.y.RightVertex.Position.X * sin + x.y.RightVertex.Position.Y * cos
					};

					return new Line() {
						Properties = properties,
						LeftVertex = left,
						RightVertex = right
					};
				})
				.ToArray();

			Vector2[] vertices;
			Rect viewport = this.viewport;
			// Determine viewport
			if (this.viewportMode != ViewportModes.Manual) {
				IEnumerable<Sector> boundingSectors;
				switch (this.viewportMode) {
					case ViewportModes.FitToAllLayers:
						boundingSectors = level.Sectors;
						break;
					case ViewportModes.FitToCurrentLayer:
						if (byLayer.TryGetValue(this.layer, out Sector[] sectors)) {
							boundingSectors = sectors;
						} else {
							boundingSectors = Enumerable.Empty<Sector>();
						}
						break;
					default:
						boundingSectors = visibleSectors;
						break;
				}

				// Get bounding box
				vertices = boundingSectors
					.SelectMany(x => x.Walls)
					.SelectMany(x => new[] {
						x.LeftVertex.Position.ToUnity(),
						x.RightVertex.Position.ToUnity()
					})
					.Select(x => {
						float angle = Mathf.Deg2Rad * this.rotation;
						float sin = Mathf.Sin(angle);
						float cos = Mathf.Cos(angle);

						return new Vector2() {
							x = x.x * cos - x.y * sin,
							y = x.x * sin + x.y * cos
						};
					})
					.Distinct()
					.ToArray();

				float minX = vertices.Min(x => x.x);
				float maxX = vertices.Max(x => x.x);
				float minY = vertices.Min(x => x.y);
				float maxY = vertices.Max(x => x.y);

				// Add padding
				if (this.paddingUnit == PaddingUnits.GameUnits) {
					minX -= this.padding.xMin;
					minY -= this.padding.yMin;
					maxX += this.padding.xMax;
					maxY += this.padding.yMax;
				}

				// Adjust for zoom
				minX *= this.zoom;
				minY *= this.zoom;
				maxX *= this.zoom;
				maxY *= this.zoom;

				if (this.paddingUnit == PaddingUnits.Pixels) {
					minX -= this.padding.xMin;
					minY -= this.padding.yMin;
					maxX += this.padding.xMax;
					maxY += this.padding.yMax;
				}

				viewport = new Rect(minX, minY, maxX - minX, maxY - minY);
			} else {
				// We'll still need these later.
				vertices = lines
					.SelectMany(x => new[] {
						x.LeftVertex,
						x.RightVertex
					})
					.Distinct()
					.ToArray();
			}

			// Pad the viewport if it's not an integer size.
			if (viewport.width % 1 != 0) {
				float delta = viewport.width % 1;
				viewport.width += delta;
				viewport.x -= delta / 2;
			}
			if (viewport.height % 1 != 0) {
				float delta = viewport.height % 1;
				viewport.height += delta;
				viewport.x -= delta / 2;
			}

			float zoom = this.zoom;
			if (this.autoZoomToFit) {
				// Get bounding box of vertices.
				float minX = vertices.Min(x => x.x);
				float maxX = vertices.Max(x => x.x);
				float minY = vertices.Min(x => x.y);
				float maxY = vertices.Max(x => x.y);

				// Adjust for padding
				if (this.paddingUnit == PaddingUnits.GameUnits) {
					minX -= this.padding.xMin;
					minY -= this.padding.yMin;
					maxX += this.padding.xMax;
					maxY += this.padding.yMax;
				} else if (this.paddingUnit == PaddingUnits.Pixels) {
					viewport.x += this.padding.x;
					viewport.y += this.padding.y;
					viewport.width -= this.padding.width;
					viewport.height -= this.padding.height;
				}

				float zoomX = viewport.x / (maxX - minX);
				float zoomY = viewport.y / (maxY - minY);
				zoom = Mathf.Min(zoomX, zoomY);

				Vector2 center = new Vector2((minX + maxX) / 2, (minY + maxY) / 2);
				viewport.x = center.x * zoom - viewport.width / 2;
				viewport.y = center.y * zoom - viewport.height / 2;
			}

			Rect fixedViewport = new Rect(
				Vector2.zero,
				viewport.size
			);

			// Exclude drawing any lines that don't overlap with the viewport.
			lines = lines
				.Select(x => {
					x.LeftVertex = x.LeftVertex * zoom - viewport.position;
					x.RightVertex = x.RightVertex * zoom - viewport.position;
					return x;
				})
				.Where(x => {
					if (!x.Properties.Draw) {
						return false;
					}

					Vector2 pos = new Vector2(
						Mathf.Min(x.LeftVertex.x, x.RightVertex.x),
						Mathf.Min(x.LeftVertex.y, x.RightVertex.y)
					);
					Rect bounds = new Rect(
						pos,
						new Vector2(
							Mathf.Max(x.LeftVertex.x, x.RightVertex.x) - pos.x,
							Mathf.Max(x.LeftVertex.y, x.RightVertex.y) - pos.y
						)
					);
					return bounds.Overlaps(fixedViewport);
				})
				.OrderBy(x => x.Properties.Priority)
				.ToArray();

			// Create the SkiaSharp image.
			SKImageInfo info = new SKImageInfo((int)viewport.width, (int)viewport.height);
			using SKSurface surface = SKSurface.Create(info);
			SKCanvas canvas = surface.Canvas;
			using SKPaint paint = new SKPaint() {
				BlendMode = SKBlendMode.SrcOver,
				IsAntialias = true,
				IsStroke = true
			};

			// Draw all the lines
			foreach (Line line in lines) {
				paint.Color = line.Properties.Color.ToSkia();
				paint.StrokeWidth = line.Properties.Width;

				canvas.DrawLine(new SKPoint(
					line.LeftVertex.x,
					info.Height - line.LeftVertex.y
				), new SKPoint(
					line.RightVertex.x,
					info.Height - line.RightVertex.y
				), paint);
			}

			// Grab the resulting raster data and convert to a texture.
			using SKPixmap pixmap = surface.PeekPixels();

			TextureFormat format = (info.ColorType == SKColorType.Rgba8888) ? TextureFormat.RGBA32 : TextureFormat.BGRA32;
			Texture2D texture = new Texture2D((int)viewport.width, (int)viewport.height, format, false, true) {
#if UNITY_EDITOR
					alphaIsTransparency = true
#endif
			};

			byte[] pixels = new byte[pixmap.BytesSize];
			IntPtr pointer = pixmap.GetPixels();
			for (int y = 0; y < pixmap.Height; y++) {
				Marshal.Copy(pointer + pixmap.RowBytes * (pixmap.Height - y - 1), pixels, pixmap.RowBytes * y, pixmap.RowBytes);
			}

			texture.LoadRawTextureData(pixels);
			texture.Apply(true, true);
			return texture;
		}
	}
}
