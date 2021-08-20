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
		private LineProperties adjoined = new LineProperties(new Color(0, 0.5f, 0), 1);
		/// <summary>
		/// Adjoined walls where the sectors are the same floor height.
		/// </summary>
		public LineProperties Adjoined { get => this.adjoined; set => this.adjoined = value; }
		[SerializeField]
		private LineProperties ledge = new LineProperties(new Color(0, 0.5f, 0), 2);
		/// <summary>
		/// Adjoined walls with a step up or down.
		/// </summary>
		public LineProperties Ledge { get => this.ledge; set => this.ledge = value; }
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
		private bool allowLevelToOverrideWallTypes = true;
		/// <summary>
		/// Allow a level's wall flags to override the type of wall we draw.
		/// </summary>
		public bool AllowLevelToOverrideWallTypes { get => this.allowLevelToOverrideWallTypes; set => this.allowLevelToOverrideWallTypes = value; }

		[SerializeField]
		private int[] layers = new int[] { };
		/// <summary>
		/// The layers to treat as "active" for the purposes of drawing.
		/// </summary>
		public int[] Layers { get => this.layers; set => this.layers = value; }

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
		public enum BoundingModes {
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
			FitToActiveLayers,
			/// <summary>
			/// Manually specify bounds.
			/// </summary>
			Manual
		}

		[SerializeField]
		private BoundingModes viewportFitMode;
		/// <summary>
		/// How to adjust the viewport of the map.
		/// </summary>
		public BoundingModes ViewportFitMode { get => this.viewportFitMode; set => this.viewportFitMode = value; }

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
		private Vector4 padding;
		/// <summary>
		/// How much padding to add to the map viewport on all sides.
		/// </summary>
		public Vector4 Padding { get => this.padding; set => this.padding = value; }

		[SerializeField]
		private float zoom = 1;
		/// <summary>
		/// The zoom level if the viewport if not automatic.
		/// </summary>
		public float Zoom { get => this.zoom; set => this.zoom = value; }

		[SerializeField]
		private BoundingModes zoomFitMode = BoundingModes.Manual;
		/// <summary>
		/// Automatically zooms to fit the desired viewport.
		/// </summary>
		public BoundingModes ZoomFitMode { get => this.zoomFitMode; set => this.zoomFitMode = value; }

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

		private (Line[], Vector2Int) GenerateLinesAndViewport(DfLevel level, DfLevelInformation inf) {
			// Filter sectors to visible ones.
			IEnumerable<Sector> visibleSectors;
			if (this.unselectedLayersRenderMode == UnselectedLayersRenderModes.Hide) {
				visibleSectors = level.Sectors.Where(x => Array.IndexOf(this.layers, x.Layer) >= 0).ToArray();
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
					LineProperties properties = default;
					bool isLayer = Array.IndexOf(this.layers, x.x.Layer) >= 0;
					if (this.allowLevelToOverrideWallTypes && x.y.TextureAndMapFlags.HasFlag(WallTextureAndMapFlags.HiddenOnMap)) {
						properties = default;
					} else {
						HashSet<LineProperties> candidates = new HashSet<LineProperties>();
						if (this.unselectedLayersRenderMode == UnselectedLayersRenderModes.ShowInactive && !isLayer) {
							candidates.Add(this.inactiveLayer);
						}
						if (this.allowLevelToOverrideWallTypes && x.y.TextureAndMapFlags.HasFlag(WallTextureAndMapFlags.DoorOnMap)) {
							candidates.Add(this.elevator);
						}
						if (this.allowLevelToOverrideWallTypes && x.y.TextureAndMapFlags.HasFlag(WallTextureAndMapFlags.LedgeOnMap)) {
							candidates.Add(this.ledge);
						}
						if (this.allowLevelToOverrideWallTypes && x.y.TextureAndMapFlags.HasFlag(WallTextureAndMapFlags.NormalOnMap)) {
							candidates.Add(this.unadjoined);
						}
						if (wallsScripts.ContainsKey(x.y)) {
							candidates.Add(this.wallTrigger);
						}
						if (sectorsScripts.TryGetValue(x.x, out DfLevelInformation.Item[] sectorScripts)) {
							if (sectorScripts.Any(x => {
								string[] lines = x.Script.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
								Dictionary<string, string[]> logic = lines.SelectMany(x => TextBasedFile.SplitKeyValuePairs(TextBasedFile.TokenizeLine(x)))
									.GroupBy(x => x.Key.ToUpper()).ToDictionary(x => x.Key, x => x.Last().Value);
								return logic.TryGetValue("CLASS", out string[] strClass) && strClass.Length > 0 && strClass[0].ToLower() == "elevator";
							})) {
								candidates.Add(this.elevator);
							}
							candidates.Add(this.sectorTrigger);
						}
						if (x.y.Adjoined == null) {
							candidates.Add(this.unadjoined);
						} else {
							if (x.y.Adjoined.Sector.Floor.Y != x.x.Floor.Y) {
								candidates.Add(this.ledge);
							}
							candidates.Add(this.adjoined);
						}
						properties = candidates.Where(x => x.Draw).OrderByDescending(x => x.Priority).FirstOrDefault();
					}

					Vector2 left = new Vector2() {
						x = x.y.LeftVertex.Position.X * cos - x.y.LeftVertex.Position.Y * sin,
						y = x.y.LeftVertex.Position.X * sin + x.y.LeftVertex.Position.Y * cos
					};
					Vector2 right = new Vector2() {
						x = x.y.RightVertex.Position.X * cos - x.y.RightVertex.Position.Y * sin,
						y = x.y.RightVertex.Position.X * sin + x.y.RightVertex.Position.Y * cos
					};

					if (!properties.Draw) {
						return default;
					}

					return new Line() {
						Properties = properties,
						LeftVertex = left,
						RightVertex = right
					};
				})
				.Where(x => x.Properties.Draw)
				.ToArray();

			float maxLineWidth = new[] { this.inactiveLayer, this.elevator, this.unadjoined, this.adjoined,
				this.ledge, this.sectorTrigger, this.wallTrigger }.Select(x => x.Width).Max();

			Rect viewport = this.viewport;
			// Determine viewport
			if (this.viewportFitMode != BoundingModes.Manual) {
				IEnumerable<Sector> boundingSectors;
				switch (this.viewportFitMode) {
					case BoundingModes.FitToAllLayers:
						boundingSectors = level.Sectors;
						break;
					case BoundingModes.FitToActiveLayers:
						boundingSectors = level.Sectors.Where(x => Array.IndexOf(this.layers, x.Layer) >= 0).ToArray();
						break;
					default:
						boundingSectors = visibleSectors;
						break;
				}

				// Get bounding box
				Vector2[] vertices = boundingSectors
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

				float minX = vertices.Min(x => x.x - maxLineWidth / 2);
				float maxX = vertices.Max(x => x.x + maxLineWidth / 2);
				float minY = vertices.Min(x => x.y - maxLineWidth / 2);
				float maxY = vertices.Max(x => x.y + maxLineWidth / 2);

				// Add padding
				if (this.paddingUnit == PaddingUnits.GameUnits) {
					minX -= this.padding.x;
					minY -= this.padding.y;
					maxX += this.padding.z;
					maxY += this.padding.w;
				}

				// Adjust for zoom
				minX *= this.zoom;
				minY *= this.zoom;
				maxX *= this.zoom;
				maxY *= this.zoom;

				if (this.paddingUnit == PaddingUnits.Pixels) {
					minX -= this.padding.x;
					minY -= this.padding.y;
					maxX += this.padding.z;
					maxY += this.padding.w;
				}

				viewport = new Rect((minX + maxX) / 2 / this.zoom, (minY + maxY) / 2 / this.zoom, maxX - minX, maxY - minY);
			}

			this.viewport = viewport;

			float zoom = this.zoom;
			if (this.zoomFitMode != BoundingModes.Manual) {
				IEnumerable<Sector> boundingSectors;
				switch (this.zoomFitMode) {
					case BoundingModes.FitToAllLayers:
						boundingSectors = level.Sectors;
						break;
					case BoundingModes.FitToActiveLayers:
						boundingSectors = level.Sectors.Where(x => Array.IndexOf(this.layers, x.Layer) >= 0).ToArray();
						break;
					default:
						boundingSectors = visibleSectors;
						break;
				}

				// Get bounding box
				Vector2[] vertices = boundingSectors
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

				float minX = vertices.Min(x => x.x - maxLineWidth / 2);
				float maxX = vertices.Max(x => x.x + maxLineWidth / 2);
				float minY = vertices.Min(x => x.y - maxLineWidth / 2);
				float maxY = vertices.Max(x => x.y + maxLineWidth / 2);

				// Add padding
				if (this.paddingUnit == PaddingUnits.GameUnits) {
					minX -= this.padding.x;
					minY -= this.padding.y;
					maxX += this.padding.z;
					maxY += this.padding.w;
				}

				float width = Math.Max(maxX - viewport.x, viewport.x - minY) * 2;
				float height = Math.Max(maxY - viewport.y, viewport.y - minY) * 2;

				float viewportWidth = viewport.width;
				float viewportHeight = viewport.height;

				if (this.paddingUnit == PaddingUnits.Pixels) {
					viewportWidth -= this.padding.x;
					viewportHeight -= this.padding.y;
					viewportWidth -= this.padding.z;
					viewportHeight -= this.padding.w;
				}

				float zoomX = viewportWidth / width;
				float zoomY = viewportHeight / height;
				this.zoom = zoom = Mathf.Min(zoomX, zoomY);
			}

			// Pad the viewport if it's not an integer size.
			if (viewport.width % 1 != 0) {
				float delta = 1 - (viewport.width % 1);
				viewport.width += delta;
				viewport.x -= delta / 2;
			}
			if (viewport.height % 1 != 0) {
				float delta = 1 - (viewport.height % 1);
				viewport.height += delta;
				viewport.x -= delta / 2;
			}

			Rect fixedViewport = new Rect(
				Vector2.zero,
				viewport.size
			);

			// Exclude drawing any lines that don't overlap with the viewport.
			lines = lines
				.Select(x => {
					x.LeftVertex = x.LeftVertex * zoom - (viewport.position * zoom - (viewport.size / 2));
					x.RightVertex = x.RightVertex * zoom - (viewport.position * zoom - (viewport.size / 2));
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
			return (lines, new Vector2Int((int)viewport.width, (int)viewport.height));
		}

		/// <summary>
		/// Generate a map texture.
		/// </summary>
		/// <param name="level">The level data.</param>
		/// <param name="inf">The level information.</param>
		/// <returns>The texture.</returns>
		public Texture2D GenerateTexture(DfLevel level, DfLevelInformation inf) {
			(Line[] lines, Vector2Int viewport) = this.GenerateLinesAndViewport(level, inf);

			// Create the SkiaSharp image.
			SKImageInfo info = new SKImageInfo(viewport.x, viewport.y);
			using SKSurface surface = SKSurface.Create(info);
			SKCanvas canvas = surface.Canvas;
			using SKPaint paint = new SKPaint() {
				BlendMode = SKBlendMode.SrcOver,
				IsAntialias = true,
				IsStroke = true,
				StrokeCap = SKStrokeCap.Round
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
			Texture2D texture = new Texture2D(viewport.x, viewport.y, format, false, true) {
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

		/// <summary>
		/// Generate a map and grab the raw PNG data.
		/// </summary>
		/// <param name="level">The level data.</param>
		/// <param name="inf">The level information.</param>
		/// <returns>The PNG data.</returns>
		public SKData GeneratePng(DfLevel level, DfLevelInformation inf) {
			(Line[] lines, Vector2Int viewport) = this.GenerateLinesAndViewport(level, inf);

			// Create the SkiaSharp image.
			SKImageInfo info = new SKImageInfo(viewport.x, viewport.y);
			using SKSurface surface = SKSurface.Create(info);
			SKCanvas canvas = surface.Canvas;
			using SKPaint paint = new SKPaint() {
				BlendMode = SKBlendMode.SrcOver,
				IsAntialias = true,
				IsStroke = true,
				StrokeCap = SKStrokeCap.Round
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

			return pixmap.Encode(SKEncodedImageFormat.Png, 100);
		}
	}
}
