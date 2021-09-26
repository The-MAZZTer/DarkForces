using MZZT.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MZZT.DarkForces.FileFormats {
	/// <summary>
	/// A Dark Forces LEV file.
	/// </summary>
	public class DfLevel : TextBasedFile<DfLevel>, ICloneable {
		/// <summary>
		/// Texture data for a surface.
		/// </summary>
		public class WallSurface : ICloneable {
			/// <summary>
			/// The filename of the texture.
			/// </summary>
			public string TextureFile { get; set; }
			/// <summary>
			/// The offset of the texture.
			/// </summary>
			public Vector2 TextureOffset { get; set; }
			/// <summary>
			/// Unknown.
			/// </summary>
			public int TextureUnknown { get; set; }

			object ICloneable.Clone() => this.Clone();
			public virtual WallSurface Clone() => new() {
				TextureFile = this.TextureFile,
				TextureOffset = this.TextureOffset,
				TextureUnknown = this.TextureUnknown
			};
		}

		/// <summary>
		/// A floor or ceiling surface.
		/// </summary>
		public class HorizontalSurface : WallSurface {
			/// <summary>
			/// The y-level of the surface.
			/// </summary>
			public float Y { get; set; }

			public override WallSurface Clone() => new HorizontalSurface() {
				TextureFile = this.TextureFile,
				TextureOffset = this.TextureOffset,
				TextureUnknown = this.TextureUnknown,
				Y = this.Y
			};
		}

		/// <summary>
		/// Sector flags.
		/// </summary>
		[Flags]
		public enum SectorFlags {
			/// <summary>
			/// Ceiling opens up to the sky.
			/// </summary>
			CeilingIsSky = 0x1,
			/// <summary>
			/// Quick and easy door sector.
			/// </summary>
			SectorIsDoor = 0x2,
			/// <summary>
			/// Walls reflect energy shots.
			/// </summary>
			WallsReflectShots = 0x4,
			/// <summary>
			/// Adjoined sky sectors have their skies adjoined.
			/// </summary>
			AdjoinAdjacentSkies = 0x8,
			/// <summary>
			/// Icy floor.
			/// </summary>
			FloorIsIce = 0x10,
			/// <summary>
			/// Snow floor (no effect?).
			/// </summary>
			FloorIsSnow = 0x20,
			/// <summary>
			/// Sector reacts to explosions.
			/// </summary>
			SectorIsExplodingWall = 0x40,
			/// <summary>
			/// Floor opens up to a pit.
			/// </summary>
			FloorIsPit = 0x80,
			/// <summary>
			/// Adjoined pit sectors have their pits adjoined.
			/// </summary>
			AdjoinAdjacentPits = 0x100,
			/// <summary>
			/// Damage is done if the sector crushes the player.
			/// </summary>
			ElevatorsCrush = 0x200,
			/// <summary>
			/// All non adjoined walls are drawn as sky/pit.
			/// </summary>
			DrawWallsAsSkyPit = 0x400,
			/// <summary>
			/// Sector does low damage.
			/// </summary>
			LowDamage = 0x800,
			/// <summary>
			/// Sector does high damage.
			/// </summary>
			HighDamage = 0x1000,
			/// <summary>
			/// Sector has damaging gas.
			/// </summary>
			GasDamage = 0x1800,
			/// <summary>
			/// Enemies can't trigger triggers.
			/// </summary>
			DenyEnemyTrigger = 0x2000,
			/// <summary>
			/// Enemies can trigger triggers.
			/// </summary>
			AllowEnemyTrigger = 0x4000,
			/// <summary>
			/// Unknown.
			/// </summary>
			Subsector = 0x8000,
			/// <summary>
			/// Unknown.
			/// </summary>
			SafeSector = 0x10000,
			/// <summary>
			/// Unknown.
			/// </summary>
			Rendered = 0x20000,
			/// <summary>
			/// Unknown.
			/// </summary>
			Player = 0x40000,
			/// <summary>
			/// Counts toward secret counter.
			/// </summary>
			Secret = 0x80000
		}

		/// <summary>
		/// Wall texture/map flags.
		/// </summary>
		[Flags]
		public enum WallTextureAndMapFlags {
			/// <summary>
			/// Shows texture even when adjoined.
			/// </summary>
			ShowTextureOnAdjoin = 0x1,
			/// <summary>
			/// Sign is full bright?
			/// </summary>
			IlluminatedSign = 0x2,
			/// <summary>
			/// Flip texture.
			/// </summary>
			FlipTextureHorizontally = 0x4,
			/// <summary>
			/// Elevator adjusts wall light instead of sector light?
			/// </summary>
			ElevatorChangesWallLight = 0x8,
			/// <summary>
			/// Texture anchored at bottom of sector instead of top.
			/// </summary>
			WallTextureAnchored = 0x10,
			/// <summary>
			/// Elevators which morph walls can move this wall.
			/// </summary>
			WallMorphsWithElevator = 0x20,
			/// <summary>
			/// Elevtors which scroll textures affect top edge texture.
			/// </summary>
			ElevatorScrollsTopEdgeTexture = 0x40,
			/// <summary>
			/// Elevtors which scroll textures affect main texture.
			/// </summary>
			ElevatorScrollsMainTexture = 0x80,
			/// <summary>
			/// Elevtors which scroll textures affect bottom edge texture.
			/// </summary>
			ElevatorScrollsBottomEdgeTexture = 0x100,
			/// <summary>
			/// Elevtors which scroll textures affect sign texture.
			/// </summary>
			ElevatorScrollsSignTexture = 0x200,
			/// <summary>
			/// Wall is hidden on map.
			/// </summary>
			HiddenOnMap = 0x400,
			/// <summary>
			/// Wall shown as normal on map.
			/// </summary>
			NormalOnMap = 0x800,
			/// <summary>
			/// Sign texture anchored to top of sector instead of bottom.
			/// </summary>
			SignTextureAnchored = 0x1000,
			/// <summary>
			/// Wall damages player on contact.
			/// </summary>
			DamagePlayer = 0x2000,
			/// <summary>
			/// Show as ledge on map.
			/// </summary>
			LedgeOnMap = 0x4000,
			/// <summary>
			/// Show as door/elevator on map.
			/// </summary>
			DoorOnMap = 0x8000
		}

		/// <summary>
		/// Wall flags dealing with adjoins.
		/// </summary>
		[Flags]
		public enum WallAdjoinFlags {
			/// <summary>
			/// Allow player to walk up regardless of height difference.
			/// </summary>
			SkipStepCheck = 0x1,
			/// <summary>
			/// Player and enemies can't cross.
			/// </summary>
			BlockPlayerAndEnemies = 0x2,
			/// <summary>
			/// Enemies can't cross.
			/// </summary>
			BlockEnemies = 0x4,
			/// <summary>
			/// Weapons fire can't cross.
			/// </summary>
			BlockShots = 0x8
		}

		/// <summary>
		/// Represents a vertex. As a class it's used to track references to the same vertex.
		/// </summary>
		public class Vertex : ICloneable {
			/// <summary>
			/// X and Z position of the vertex.
			/// </summary>
			public Vector2 Position { get; set; }

			object ICloneable.Clone() => this.Clone();
			public Vertex Clone() => new() {
				Position = this.Position
			};
		}

		/// <summary>
		/// A vertical wall.
		/// </summary>
		public class Wall {
			/// <summary>
			/// Create a new wall.
			/// </summary>
			/// <param name="sector">The sector the wall belongs to.</param>
			public Wall(Sector sector) {
				this.Sector = sector;
			}

			/// <summary>
			/// The sector the wall beongs to.
			/// </summary>
			public Sector Sector { get; }

			/// <summary>
			/// The left vertex of this wall (when viewing wall from inside the sector).
			/// </summary>
			public Vertex LeftVertex { get; set; }
			/// <summary>
			/// The right vertex of this wall (when viewing wall from inside the sector).
			/// </summary>
			public Vertex RightVertex { get; set; }
			/// <summary>
			/// The main surface of this wall. When unadjoined it is the whole wall, when adjoined is the part of the wall over the adjoin (if visible).
			/// </summary>
			public WallSurface MainTexture { get; private set; } = new();
			/// <summary>
			/// When adjoined, this represents the part of the wall above the adjoin if any.
			/// </summary>
			public WallSurface TopEdgeTexture { get; private set; } = new();
			/// <summary>
			/// When adjoined, this represents the part of the wall below the adjoin if any.
			/// </summary>
			public WallSurface BottomEdgeTexture { get; private set; } = new();
			/// <summary>
			/// Represents a sign texture on the wall.
			/// </summary>
			public WallSurface SignTexture { get; private set; } = new();

			/// <summary>
			/// The wall this wall is adjoined to.
			/// </summary>
			public Wall Adjoined { get; set; }

			/// <summary>
			/// Flags relating to textures and map.
			/// </summary>
			public WallTextureAndMapFlags TextureAndMapFlags { get; set; }
			/// <summary>
			/// Unknown flags.
			/// </summary>
			public int UnusuedFlags2 { get; set; }
			/// <summary>
			/// Flags relating to adjoins.
			/// </summary>
			public WallAdjoinFlags AdjoinFlags { get; set; }

			/// <summary>
			/// The change in light level of the wall relative to the sector.
			/// </summary>
			public short LightLevel { get; set; }

			public Wall Clone(Sector parent, Dictionary<Wall, Wall> wallClones,
				Dictionary<Vertex, Vertex> vertexClones) {

				Wall clone = new(parent) {
					AdjoinFlags = this.AdjoinFlags,
					BottomEdgeTexture = this.BottomEdgeTexture.Clone(),
					LightLevel = this.LightLevel,
					MainTexture = this.MainTexture.Clone(),
					SignTexture = this.SignTexture.Clone(),
					TextureAndMapFlags = this.TextureAndMapFlags,
					TopEdgeTexture = this.TopEdgeTexture.Clone(),
					UnusuedFlags2 = this.UnusuedFlags2
				};
				wallClones[this] = clone;

				if (!vertexClones.TryGetValue(this.LeftVertex, out Vertex left)) {
					vertexClones[this.LeftVertex] = left = this.LeftVertex.Clone();
				}
				clone.LeftVertex = left;
				if (!vertexClones.TryGetValue(this.RightVertex, out Vertex right)) {
					vertexClones[this.RightVertex] = right = this.RightVertex.Clone();
				}
				clone.RightVertex = right;

				if (this.Adjoined != null && wallClones.TryGetValue(this.Adjoined, out Wall adjoined)) { 
					adjoined.Adjoined = clone;
					clone.Adjoined = adjoined;
				}
				return clone;
			}
		}

		/// <summary>
		/// A room with a consistent floor and ceiling level and walls.
		/// </summary>
		public class Sector {
			/// <summary>
			/// An optional name for the sector, required to add scripts to it or one of its walls.
			/// </summary>
			public string Name { get; set; }
			/// <summary>
			/// The light level of the sector from 0 to 31.
			/// </summary>
			public int LightLevel { get; set; }
			/// <summary>
			/// The floor of the sector.
			/// </summary>
			public HorizontalSurface Floor { get; private set; } = new();
			/// <summary>
			/// The ceiling of the sector.
			/// </summary>
			public HorizontalSurface Ceiling { get; private set; } = new();
			/// <summary>
			/// Allows specifying a Y value relative to floor height to simulate water on the floor or an upper walkway.
			/// </summary>
			public float AltY { get; set; }

			/// <summary>
			/// Sector flags.
			/// </summary>
			public SectorFlags Flags { get; set; }
			/// <summary>
			/// Unknown.
			/// </summary>
			public int UnusuedFlags2 { get; set; }
			/// <summary>
			/// An alternate light level which can be switched to.
			/// </summary>
			public int AltLightLevel { get; set; }

			/// <summary>
			/// The map layer this sector is in.
			/// </summary>
			public int Layer { get; set; }

			/// <summary>
			/// The walls this sector has.
			/// </summary>
			public List<Wall> Walls { get; } = new();

			public Sector Clone(Dictionary<Wall, Wall> wallClones) {
				Dictionary<Vertex, Vertex> vertexClones = new();
				Sector clone = new() {
					AltLightLevel = this.AltLightLevel,
					AltY = this.AltY,
					Ceiling = (HorizontalSurface)this.Ceiling.Clone(),
					Flags = this.Flags,
					Floor = (HorizontalSurface)this.Floor.Clone(),
					Layer = this.Layer,
					LightLevel = this.LightLevel,
					Name = this.Name,
					UnusuedFlags2 = this.UnusuedFlags2
				};
				clone.Walls.AddRange(this.Walls.Select(x => x.Clone(clone, wallClones, vertexClones)));
				return clone;
			}
		}

		/// <summary>
		/// The base name of the level filename.
		/// </summary>
		public string LevelFile { get; set; }
		/// <summary>
		/// The name of the palette to use.
		/// </summary>
		public string PaletteFile { get; set; }
		/// <summary>
		/// The unused name of the music file.
		/// </summary>
		public string MusicFile { get; set; }
		/// <summary>
		/// The amount to move the sky in pixels when the player turns a full circle.
		/// </summary>
		public Vector2 Parallax { get; set; }
		
		/// <summary>
		/// Sectors in this level.
		/// </summary>
		public List<Sector> Sectors { get; } = new();

		public override bool CanLoad => true;
		
		public override async Task LoadAsync(Stream stream) {
			this.ClearWarnings();

			using StreamReader reader = new(stream, Encoding.ASCII, false, 1024, true);

			string[] line = await this.ReadTokenizedLineAsync(reader);
			if (!(line?.Select(x => x.ToUpper()).SequenceEqual(new[] { "LEV", "2.1" }) ?? false)) {
				this.AddWarning("LEV file header not found.");
			} else {
				line = await this.ReadTokenizedLineAsync(reader);
			}

			List<string> textures = new();
			Dictionary<Wall, (int sector, int wall)> adjoins = new();

			int sectorCount = -1;
			this.Sectors.Clear();

			while (line != null) {
				switch (line[0].ToUpper()) {
					case "LEVELNAME": {
						if (line.Length < 2) {
							this.AddWarning("LEVELNAME missing value.");
							break;
						}
						this.LevelFile = line[1];
					} break;
					case "PALETTE": {
						if (line.Length < 2) {
							this.AddWarning("PALETTE missing value.");
							break;
						}
						this.PaletteFile = line[1];
					} break;
					case "MUSIC": {
						if (line.Length < 2) {
							this.AddWarning("MUSIC missing value.");
							break;
						}
						this.MusicFile = line[1];
					} break;
					case "PARALLAX": {
						if (line.Length < 3 || !float.TryParse(line[1], out float x) || !float.TryParse(line[2], out float y)) {
							this.AddWarning("PARALLEX missing or invalid value.");
							break;
						}
						this.Parallax = new() {
							X = x,
							Y = y
						};
					} break;
					case "TEXTURES": {
						if (line.Length < 2 || !int.TryParse(line[1], NumberStyles.Integer, null, out int texturesCount)) {
							texturesCount = -1;
							this.AddWarning("Texture count is not a number.");
						}

						while (true) { //for (int i = 0; texturesCount < 0 || (i < texturesCount); i++) {
							line = await this.ReadTokenizedLineAsync(reader);
							if (line == null || line[0].ToUpper() != "TEXTURE:") {
								if (textures.Count < texturesCount) {
									this.AddWarning("Unexpected end of texture declarations.");
								}
								break;
							}
							textures.Add(line.Length < 2 ? null : line[1]);
						}
					} continue;
					case "NUMSECTORS": {
						if (line.Length < 2 || !int.TryParse(line[1], NumberStyles.Integer, null, out sectorCount)) {
							this.AddWarning("Sector count is not a number!");
							break;
						} else {
							this.Sectors.Capacity = sectorCount;
						}
					} break;
					case "SECTOR": {
						if (line.Length < 2 || line[1] != this.Sectors.Count.ToString()) {
							this.AddWarning("Sector numbers are not consecutive.");
						}

						Sector sector = new();
						this.Sectors.Add(sector);
						List<Vertex> vertices = new();

						line = await this.ReadTokenizedLineAsync(reader);
						bool endOfObject = false;
						while (!endOfObject && line != null) {
							switch (line[0].ToUpper()) {
								case "NAME": {
									if (line.Length < 2) {
										// NAME without value is seen in some generated LEV files so ignore it. It's not really a problem.
										//this.AddWarning("Missing sector name.");
										break;
									}
									sector.Name = line[1];
								} break;
								case "AMBIENT": {
									if (line.Length < 2 || !int.TryParse(line[1], NumberStyles.Integer, null, out int ambient)) {
										this.AddWarning("Ambient value is not a number.");
										break;
									}

									sector.LightLevel = ambient;
								} break;
								case "FLOOR":
								case "CEILING": {
									if (line.Length < 2) {
										this.AddWarning($"Unknown statement {line[0]}.");
										break;
									}

									HorizontalSurface floorCeiling = (line[0].ToUpper() == "CEILING") ? sector.Ceiling : sector.Floor;

									switch (line[1].ToUpper()) {
										case "TEXTURE": {
											if (line.Length < 5 ||
												!int.TryParse(line[2], NumberStyles.Integer, null, out int texture) ||
												texture < 0 || texture >= textures.Count ||
												!float.TryParse(line[3], out float offsetX) ||
												!float.TryParse(line[4], out float offsetZ) ||
												!int.TryParse(line[5], NumberStyles.Integer, null, out int unused)) {

												this.AddWarning("Texture has invalid format!");
												break;
											}
											floorCeiling.TextureFile = textures[texture];
											floorCeiling.TextureOffset = new() {
												X = offsetX,
												Y = offsetZ
											};
											floorCeiling.TextureUnknown = unused;
										} break;
										case "ALTITUDE": {
											if (line.Length < 2 || !float.TryParse(line[2], out float y)) {
												this.AddWarning("Altitude has invalid format!");
												break;
											}
											floorCeiling.Y = y;
										} break;
										default:
											this.AddWarning($"Unknown statement {line[0]} {line[1]}.");
											break;
									}
								} break;
								case "SECOND": {
									if (line.Length < 2) {
										this.AddWarning($"Unknown statement {line[0]}.");
										break;
									}

									switch (line[1].ToUpper()) {
										case "ALTITUDE": {
											if (line.Length < 2 || !float.TryParse(line[2], out float y)) {
												this.AddWarning("Second altitude has invalid format!");
												break;
											}
											sector.AltY = y;
										} break;
										default:
											this.AddWarning($"Unknown statement {line[0]} {line[1]}.");
											break;
									}
								} break;
								case "FLAGS": {
									if (line.Length < 4 ||
										!int.TryParse(line[1], NumberStyles.Integer, null, out int flags1) ||
										!int.TryParse(line[2], NumberStyles.Integer, null, out int flags2) ||
										!int.TryParse(line[3], NumberStyles.Integer, null, out int flags3)) {

										this.AddWarning("Flags has invalid format!");
										break;
									}
									sector.Flags = (SectorFlags)flags1;
									sector.UnusuedFlags2 = flags2;
									sector.AltLightLevel = flags3;
								} break;
								case "LAYER": {
									if (line.Length < 2 || !int.TryParse(line[1], NumberStyles.Integer, null, out int layer)) {
										this.AddWarning("Layer has invalid format.");
										break;
									}

									sector.Layer = layer;
								} break;
								case "VERTICES": {
									if (line.Length < 2 || !int.TryParse(line[1], NumberStyles.Integer, null, out int vertexCount)) {
										vertexCount = -1;
										this.AddWarning("Vertices has invalid count.");
									}

									while (true) { //for (int i = 0; vertexCount < 0 || (i < vertexCount); i++) {
										line = await this.ReadTokenizedLineAsync(reader);
										if (line == null) {
											if (vertices.Count < vertexCount) {
												this.AddWarning("Unexpected end of vertex declarations.");
											}
											break;
										}
										Dictionary<string, string[]> values = TextBasedFile.SplitKeyValuePairs(line);
										if (!values.TryGetValue("X", out string[] xString)) {
											if (vertices.Count < vertexCount) {
												this.AddWarning("Unexpected end of vertex declarations.");
											}
											break;
										}

										if (xString.Length < 1 || !float.TryParse(xString[0], out float x) ||
											!values.TryGetValue("Z", out string[] zString) ||
											zString.Length < 1 || !float.TryParse(zString[0], out float z)) {

											this.AddWarning("Vertex has invalid format.");
											continue;
										}

										vertices.Add(new() {
											Position = new() {
												X = x,
												Y = z
											}
										});
									}
								} continue;
								case "WALLS": {
									if (line.Length < 2 || !int.TryParse(line[1], NumberStyles.Integer, null, out int wallCount)) {
										wallCount = -1;
										this.AddWarning("Walls has invalid count.");
									}

									while (true) { //for (int i = 0; wallCount < 0 || (i < wallCount); i++) {
										line = await this.ReadTokenizedLineAsync(reader);
										if (line == null || line[0].ToUpper() != "WALL") {
											if (sector.Walls.Count < wallCount) {
												this.AddWarning("Unexpected end of wall declarations.");
											}
											break;
										}

										Dictionary<string, string[]> values = TextBasedFile.SplitKeyValuePairs(line);
										if (!values.TryGetValue("LEFT", out string[] leftString)) {
											if (sector.Walls.Count < wallCount) {
												this.AddWarning("Unexpected end of wall declarations.");
											}
											break;
										}

										if (leftString.Length < 1 ||
											!int.TryParse(leftString[0], NumberStyles.Integer, null, out int leftVertex) ||
											leftVertex < 0 || leftVertex >= vertices.Count ||
											!values.TryGetValue("RIGHT", out string[] rightString) || rightString.Length < 1 ||
											!int.TryParse(rightString[0], NumberStyles.Integer, null, out int rightVertex) ||
											rightVertex < 0 || rightVertex >= vertices.Count ||
											!values.TryGetValue("MID", out string[] midString) || midString.Length < 4 ||
											!int.TryParse(midString[0], NumberStyles.Integer, null, out int midTexture) ||
											midTexture >= textures.Count ||
											!float.TryParse(midString[1], out float midOffsetX) ||
											!float.TryParse(midString[2], out float midOffsetZ) ||
											!int.TryParse(midString[3], NumberStyles.Integer, null, out int midUnused) ||
											!values.TryGetValue("TOP", out string[] topString) || topString.Length < 4 ||
											!int.TryParse(topString[0], NumberStyles.Integer, null, out int topTexture) ||
											topTexture >= textures.Count ||
											!float.TryParse(topString[1], out float topOffsetX) ||
											!float.TryParse(topString[2], out float topOffsetZ) ||
											!int.TryParse(topString[3], NumberStyles.Integer, null, out int topUnused) ||
											!values.TryGetValue("BOT", out string[] botString) || botString.Length < 4 ||
											!int.TryParse(botString[0], NumberStyles.Integer, null, out int botTexture) ||
											botTexture >= textures.Count ||
											!float.TryParse(botString[1], out float botOffsetX) ||
											!float.TryParse(botString[2], out float botOffsetZ) ||
											!int.TryParse(botString[3], NumberStyles.Integer, null, out int botUnused) ||
											!values.TryGetValue("SIGN", out string[] signString) || signString.Length < 3 ||
											!int.TryParse(signString[0], NumberStyles.Integer, null, out int signTexture) ||
											signTexture >= textures.Count ||
											!float.TryParse(signString[1], out float signOffsetX) ||
											!float.TryParse(signString[2], out float signOffsetZ) ||
											!values.TryGetValue("ADJOIN", out string[] adjoinString) || adjoinString.Length < 1 ||
											!int.TryParse(adjoinString[0], NumberStyles.Integer, null, out int adjoinSector) ||
											!values.TryGetValue("MIRROR", out string[] mirrorString) || mirrorString.Length < 1 ||
											!int.TryParse(mirrorString[0], NumberStyles.Integer, null, out int adjoinWall) ||
											!values.TryGetValue("FLAGS", out string[] flagsString) || flagsString.Length < 3 ||
											!int.TryParse(flagsString[0], NumberStyles.Integer, null, out int flags1) ||
											!int.TryParse(flagsString[1], NumberStyles.Integer, null, out int flags2) ||
											!int.TryParse(flagsString[2], NumberStyles.Integer, null, out int flags3) ||
											!values.TryGetValue("LIGHT", out string[] lightString) || lightString.Length < 1 ||
											!int.TryParse(lightString[0], NumberStyles.Integer, null, out int light)) {

											this.AddWarning("Wall has invalid format!");
											continue;
										}

										Wall wall = new(sector) {
											LeftVertex = vertices[leftVertex],
											RightVertex = vertices[rightVertex],
											TextureAndMapFlags = (WallTextureAndMapFlags)flags1,
											UnusuedFlags2 = flags2,
											AdjoinFlags = (WallAdjoinFlags)flags3,
											LightLevel = unchecked((short)light)
										};
										wall.MainTexture.TextureFile = midTexture < 0 ? null : textures[midTexture];
										wall.MainTexture.TextureOffset = new() {
											X = midOffsetX,
											Y = midOffsetZ
										};
										wall.MainTexture.TextureUnknown = midUnused;
										wall.TopEdgeTexture.TextureFile = topTexture < 0 ? null : textures[topTexture];
										wall.TopEdgeTexture.TextureOffset = new() {
											X = topOffsetX,
											Y = topOffsetZ
										};
										wall.TopEdgeTexture.TextureUnknown = topUnused;
										wall.BottomEdgeTexture.TextureFile = botTexture < 0 ? null : textures[botTexture];
										wall.BottomEdgeTexture.TextureOffset = new() {
											X = botOffsetX,
											Y = botOffsetZ
										};
										wall.BottomEdgeTexture.TextureUnknown = botUnused;
										wall.SignTexture.TextureFile = signTexture < 0 ? null : textures[signTexture];
										wall.SignTexture.TextureOffset = new() {
											X = signOffsetX,
											Y = signOffsetZ
										};
										sector.Walls.Add(wall);
										
										// We ignore WALK here since we assume it's the same as MIRROR.
										if (adjoinSector >= 0 && adjoinWall >= 0) {
											adjoins[wall] = (adjoinSector, adjoinWall);
										}
									}
								} continue;
								case "SECTOR":
									// This is the next sector, skip to the next loop iteration.
									endOfObject = true;
									break;
								default:
									this.AddWarning($"Unknown statement {line[0]}.");
									break;
							}

							if (!endOfObject) {
								line = await this.ReadTokenizedLineAsync(reader);
							}
						}
					} continue;
					default:
						this.AddWarning($"Unknown statement {line[0]}.");
						break;
				}

				line = await this.ReadTokenizedLineAsync(reader);
			}

			if (this.Sectors.Count < sectorCount) {
				this.AddWarning("NUMSECTORS count doesn't match actual count.");
			}

			// Hook up all the adjoin references.
			foreach ((Wall wall, (int adjoinSector, int adjoinWall)) in adjoins) {
				if (this.Sectors.Count <= adjoinSector || this.Sectors[adjoinSector].Walls.Count <= adjoinWall) {
					this.AddWarning($"Sector {wall.Sector.Name ?? this.Sectors.IndexOf(wall.Sector).ToString()} Wall {wall.Sector.Walls.IndexOf(wall)} is adjoined to non-existant sector {adjoinSector} wall {adjoinWall}.");
					continue;
				}
				wall.Adjoined = this.Sectors[adjoinSector].Walls[adjoinWall];
			}
		}

		public override bool CanSave => true;

		public override async Task SaveAsync(Stream stream) {
			this.ClearWarnings();

			using StreamWriter writer = new(stream, Encoding.ASCII, 1024, true);

			await writer.WriteLineAsync("LEV 2.1");

			await this.WriteLineAsync(writer, $"LEVELNAME {this.LevelFile}");
			await this.WriteLineAsync(writer, $"PALETTE {this.PaletteFile}");
			await this.WriteLineAsync(writer, $"MUSIC {this.MusicFile}");
			await this.WriteLineAsync(writer, $"PARALLAX {this.Parallax.X:0.0000} {this.Parallax.Y:0.0000}");

			string[] textures = this.Sectors.SelectMany(x => new[] { x.Ceiling.TextureFile, x.Floor.TextureFile }
				.Concat(x.Walls.SelectMany(x => new[] { x.BottomEdgeTexture.TextureFile, x.MainTexture.TextureFile, x.SignTexture.TextureFile, x.TopEdgeTexture.TextureFile })))
				.Where(x => x != null).Distinct().ToArray();

			await this.WriteLineAsync(writer, $"TEXTURES {textures.Length}");
			foreach (string texture in textures) {
				await this.WriteLineAsync(writer, $"TEXTURE: {texture}");
			}

			Dictionary<Wall, Sector> wallSectors = this.Sectors.SelectMany(x => x.Walls.Select(y => (y, x))).ToDictionary(x => x.y, x => x.x);

			await this.WriteLineAsync(writer, $"NUMSECTORS {this.Sectors.Count}");
			foreach ((Sector sector, int i) in this.Sectors.Select((x, i) => (x, i))) {
				await this.WriteLineAsync(writer, $"SECTOR {i}");

				if (string.IsNullOrWhiteSpace(sector.Name)) {
					await writer.WriteLineAsync("NAME");
				} else {
					await this.WriteLineAsync(writer, $"NAME {sector.Name}");
				}

				await this.WriteLineAsync(writer, $"AMBIENT {sector.LightLevel}");
				await this.WriteLineAsync(writer, $"FLOOR TEXTURE {(sector.Floor.TextureFile != null ? Array.IndexOf(textures, sector.Floor.TextureFile) : -1)} {sector.Floor.TextureOffset.X:0.00} {sector.Floor.TextureOffset.Y:0.00} {sector.Floor.TextureUnknown}");
				await this.WriteLineAsync(writer, $"FLOOR ALTITUDE {sector.Floor.Y:0.00}");
				await this.WriteLineAsync(writer, $"CEILING TEXTURE {(sector.Ceiling.TextureFile != null ? Array.IndexOf(textures, sector.Ceiling.TextureFile) : -1)} {sector.Ceiling.TextureOffset.X:0.00} {sector.Ceiling.TextureOffset.Y:0.00} {sector.Ceiling.TextureUnknown}");
				await this.WriteLineAsync(writer, $"CEILING ALTITUDE {sector.Ceiling.Y:0.00}");
				await this.WriteLineAsync(writer, $"SECOND ALTITUDE {sector.AltY:0.00}");
				await this.WriteLineAsync(writer, $"FLAGS {(int)sector.Flags} {sector.UnusuedFlags2} {sector.AltLightLevel}");
				await this.WriteLineAsync(writer, $"LAYER {sector.Layer}");

				Vertex[] vertices = sector.Walls.SelectMany(x => new[] { x.LeftVertex, x.RightVertex }).Distinct().ToArray();
				await this.WriteLineAsync(writer, $"VERTICES {vertices.Length}");
				foreach (Vertex vertex in vertices) {
					await this.WriteLineAsync(writer, $"X: {vertex.Position.X:0.00} Z: {vertex.Position.Y:0.00}");
				}

				await this.WriteLineAsync(writer, $"WALLS {sector.Walls.Count}");
				foreach (Wall wall in sector.Walls) {
					int adjoinedSector = wall.Adjoined != null ? this.Sectors.IndexOf(wallSectors[wall.Adjoined]) : -1;
					await this.WriteLineAsync(writer, $"WALL LEFT: {Array.IndexOf(vertices, wall.LeftVertex)} RIGHT: {Array.IndexOf(vertices, wall.RightVertex)} MID: {(wall.MainTexture.TextureFile != null ? Array.IndexOf(textures, wall.MainTexture.TextureFile) : -1)} {wall.MainTexture.TextureOffset.X:0.00} {wall.MainTexture.TextureOffset.Y:0.00} {wall.MainTexture.TextureUnknown} TOP: {(wall.TopEdgeTexture.TextureFile != null ? Array.IndexOf(textures, wall.TopEdgeTexture.TextureFile) : -1)} {wall.TopEdgeTexture.TextureOffset.X:0.00} {wall.TopEdgeTexture.TextureOffset.Y:0.00} {wall.TopEdgeTexture.TextureUnknown} BOT: {(wall.BottomEdgeTexture.TextureFile != null ? Array.IndexOf(textures, wall.BottomEdgeTexture.TextureFile) : -1)} {wall.BottomEdgeTexture.TextureOffset.X:0.00} {wall.BottomEdgeTexture.TextureOffset.Y:0.00} {wall.BottomEdgeTexture.TextureUnknown} SIGN: {(wall.SignTexture.TextureFile != null ? Array.IndexOf(textures, wall.SignTexture.TextureFile) : -1)} {wall.SignTexture.TextureOffset.X:0.00} {wall.SignTexture.TextureOffset.Y:0.00} ADJOIN: {adjoinedSector} MIRROR: {(wall.Adjoined != null ? wallSectors[wall.Adjoined].Walls.IndexOf(wall.Adjoined) : -1)} WALK: {adjoinedSector} FLAGS: {(int)wall.TextureAndMapFlags} {wall.UnusuedFlags2} {(int)wall.AdjoinFlags} LIGHT: {unchecked((ushort)wall.LightLevel)}");
				}
			}
		}

		object ICloneable.Clone() => this.Clone();
		public DfLevel Clone() {
			DfLevel clone = new() {
				LevelFile = this.LevelFile,
				MusicFile = this.MusicFile,
				PaletteFile = this.PaletteFile,
				Parallax = this.Parallax
			};
			Dictionary<Wall, Wall> wallClones = new();
			clone.Sectors.AddRange(this.Sectors.Select(x => x.Clone(wallClones)));
			return clone;
		}
	}
}
