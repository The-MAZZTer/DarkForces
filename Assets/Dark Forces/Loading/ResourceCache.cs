using MZZT.DarkForces.FileFormats;
using MZZT.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using static MZZT.DarkForces.FileFormats.AutodeskVue;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace MZZT.DarkForces {
	/// <summary>
	/// This class caches loaded file data (so it can be retrieved insted of being loaded from scratch again).
	/// It also handles importing data into formats Unity can use.
	/// </summary>
	public class ResourceCache : Singleton<ResourceCache> {
		public const float SPRITE_PIXELS_PER_UNIT = 400;

		/// <summary>
		/// Whether or not file load warnings will be tracked. Fatal erorrs always are.
		/// </summary>
		public bool ShowWarnings { get; set; } = true;

		[SerializeField, Header("Lighting")]
		private bool fullBright = false;
		/// <summary>
		/// Whether or not to always process colormaps at their full light level.
		/// </summary>
		public bool FullBright {
			get => this.fullBright;
			set => this.fullBright = value;
		}
		
		[SerializeField]
		private bool bypassCmpDithering = false;
		/// <summary>
		/// Whether or not to generate a 24-bit colormap instead of using the full 8-bit one.
		/// </summary>
		public bool BypassCmpDithering {
			get => this.bypassCmpDithering;
			set => this.bypassCmpDithering = value;
		}

		[SerializeField, Header("Shaders")]
		private Shader simpleShader = null;
		/// <summary>
		/// The shader used for simple textures and sprites.
		/// </summary>
		public Shader SimpleShader => this.simpleShader;
		[SerializeField]
		private Shader transparentShader = null;
		/// <summary>
		/// The shader used for transparent wall textures.
		/// </summary>
		public Shader TransparentShader => this.transparentShader;
		[SerializeField]
		private Shader colorShader = null;
		/// <summary>
		/// The shader used for soild colors.
		/// </summary>
		public Shader ColorShader => this.colorShader;
		[SerializeField]
		private Shader planeShader = null;
		/// <summary>
		/// The shader used for the sky and pits.
		/// </summary>
		public Shader PlaneShader => this.planeShader;

		private void Start() {
			this.ShowWarnings = PlayerPrefs.GetInt("ShowWarnings", 1) > 0;
		}

		private readonly Dictionary<string, DfPalette> palCache = new Dictionary<string, DfPalette>();
		/// <summary>
		/// Retrieves a palette.
		/// </summary>
		/// <param name="filename">The filename of the PAL.</param>
		/// <returns>The palette data.</returns>
		public async Task<DfPalette> GetPaletteAsync(string filename) {
			filename = filename.ToUpper();
			if (!this.palCache.TryGetValue(filename, out DfPalette pal)) {
				try {
					pal = await FileLoader.Instance.LoadGobFileAsync<DfPalette>(filename);
				} catch (Exception e) {
					this.AddError(filename, e);
				}
				this.AddWarnings(filename, pal);

				if (pal == null) {
					try {
						pal = await FileLoader.Instance.LoadGobFileAsync<DfPalette>("DEFAULT.PAL");
					} catch (Exception e) {
						this.AddError(filename, e);
					}
					this.AddWarnings(filename, pal);
				}

				this.palCache[filename] = pal;
			}
			return pal;
		}

		private readonly Dictionary<DfPalette, byte[]> importedPalCache = new Dictionary<DfPalette, byte[]>();
		/// <summary>
		/// Import a palette into 32-bit RGBA format.
		/// </summary>
		/// <param name="pal">The palette.</param>
		/// <returns>A 256 color 32-bit RGBA palette in a byte array.</returns>
		public byte[] ImportPalette(DfPalette pal) {
			if (!this.importedPalCache.TryGetValue(pal, out byte[] palette)) {
				this.importedPalCache[pal] = palette = pal.Palette.SelectMany(x => new byte[] {
					(byte)Mathf.Clamp(Mathf.Round(x.R * 255 / 63f), 0, 255),
					(byte)Mathf.Clamp(Mathf.Round(x.G * 255 / 63f), 0, 255),
					(byte)Mathf.Clamp(Mathf.Round(x.B * 255 / 63f), 0, 255),
					255
				}).ToArray();
			}
			return palette;
		}

		private readonly Dictionary<(string, string), LandruPalette> plttCache = new Dictionary<(string, string), LandruPalette>();
		/// <summary>
		/// Retrieves a palette.
		/// </summary>
		/// <param name="lfd">The LFD holding the PLTT.</param>
		/// <param name="filename">The filename of the PLTT.</param>
		/// <returns>The palette data.</returns>
		public async Task<LandruPalette> GetPaletteAsync(string lfd, string filename) {
			lfd = lfd.ToUpper();
			filename = filename.ToUpper();
			if (!this.plttCache.TryGetValue((lfd, filename), out LandruPalette pltt)) {
				try {
					pltt = await FileLoader.Instance.LoadLfdFileAsync<LandruPalette>(lfd, filename);
				} catch (Exception e) {
					this.AddError(filename, e);
				}
				this.AddWarnings(filename, pltt);

				if (pltt == null) {
					try {
						pltt = await FileLoader.Instance.LoadLfdFileAsync<LandruPalette>(lfd, "DEFAULT");
					} catch (Exception e) {
						this.AddError(filename, e);
					}
				}

				this.plttCache[(lfd, filename)] = pltt;
			}
			return pltt;
		}

		private readonly Dictionary<LandruPalette, byte[]> importedPlttCache = new Dictionary<LandruPalette, byte[]>();
		/// <summary>
		/// Import a palette into 32-bit RGBA format.
		/// </summary>
		/// <param name="pltt">The palette.</param>
		/// <returns>A 256 color 32-bit RGBA palette in a byte array.</returns>
		public byte[] ImportPalette(LandruPalette pltt) {
			if (!this.importedPlttCache.TryGetValue(pltt, out byte[] palette)) {
				this.importedPlttCache[pltt] = palette =Enumerable.Repeat<byte>(0, pltt.First * 4).Concat(
					pltt.Palette.SelectMany(x => new byte[] {
						x.R,
						x.G,
						x.B,
						255
					})
				).ToArray();
			}
			return palette;
		}

		/// <summary>
		/// Clear all cached palette data.
		/// </summary>
		public void ClearPalettes() {
			this.palCache.Clear();
			this.importedPalCache.Clear();
			this.plttCache.Clear();
			this.importedPlttCache.Clear();
		}

		private readonly Dictionary<string, DfColormap> cmpCache = new Dictionary<string, DfColormap>();
		/// <summary>
		/// Retrieves a colormap.
		/// </summary>
		/// <param name="filename">The filename of the CMP.</param>
		/// <returns>The colormap data.</returns>
		public async Task<DfColormap> GetColormapAsync(string filename) {
			filename = filename.ToUpper();
			if (!this.cmpCache.TryGetValue(filename, out DfColormap cmp)) {
				try {
					cmp = await FileLoader.Instance.LoadGobFileAsync<DfColormap>(filename);
				} catch (Exception e) {
					this.AddError(filename, e);
				}
				this.AddWarnings(filename, cmp);

				if (cmp == null) {
					try {
						cmp = await FileLoader.Instance.LoadGobFileAsync<DfColormap>("DEFAULT.CMP");
					} catch (Exception e) {
						this.AddError(filename, e);
					}
					this.AddWarnings(filename, cmp);
				}

				this.cmpCache[filename] = cmp;
			}
			return cmp;
		}

		private readonly Dictionary<(DfPalette, DfColormap, int, bool), byte[]> importedCmpCache =
			new Dictionary<(DfPalette, DfColormap, int, bool), byte[]>();
		/// <summary>
		/// Map a palette through a colormap light level to a 32-bit RGBA palette.
		/// </summary>
		/// <param name="pal">The palette.</param>
		/// <param name="cmp">The colormap.</param>
		/// <param name="lightLevel">The light level to use.</param>
		/// <param name="transparent">Whether or not color index 0 should be transparent.</param>
		/// <returns>A 256 color 32-bit RGBA palette in a byte array.</returns>
		public byte[] ImportColormap(DfPalette pal, DfColormap cmp, int lightLevel, bool transparent) {
			if (lightLevel > 31) {
				lightLevel = 31;
			} else if (lightLevel < 0) {
				lightLevel = 0;
			}

			if (!this.importedCmpCache.TryGetValue((pal, cmp, lightLevel, transparent), out byte[] litPalette)) {
				byte[] palette = this.ImportPalette(pal);

				byte[][] cmpData = cmp.PaletteMaps;

				litPalette = new byte[256 * 4];
				byte[] map = cmpData[this.fullBright ? 31 : lightLevel];
				// If we want to generate a 24-bit colormap (no difference at light level 0 or 31).
				if (this.bypassCmpDithering && !this.fullBright && lightLevel > 0 && lightLevel < 31) {
					// Check each color in the CMP light level and see where it lies on a scale of the color at
					// light level 0 and the color at light level 31, to get an idea of how lit the current
					// light level is.
					float totalWeight = 0;
					float count = 0;
					for (int i = 0; i < 256; i++) {
						byte targetIndex = cmpData[0][i];
						byte targetR = palette[targetIndex * 4];
						byte targetG = palette[targetIndex * 4 + 1];
						byte targetB = palette[targetIndex * 4 + 2];

						byte fullIndex = cmpData[31][i];
						byte fullR = palette[fullIndex * 4];
						byte fullG = palette[fullIndex * 4 + 1];
						byte fullB = palette[fullIndex * 4 + 2];

						// No difference between fully lit and fully dark so skip this color.
						if (fullR == targetR && fullG == targetG && fullB == targetB) {
							continue;
						}

						// Find the currnet light level from 0 to 1, where 0 is fully dark and 1 is fully lit.
						// Add this to the totalWeight.
						byte index = map[i];
						byte r = palette[index * 4];
						byte g = palette[index * 4 + 1];
						byte b = palette[index * 4 + 2];
						if (fullR != targetR) {
							float clamp = Mathf.Clamp01((float)(r - targetR) / (fullR - targetR));
							totalWeight += clamp;
							count++;
						}
						if (fullG != targetG) {
							float clamp = Mathf.Clamp01((float)(g - targetG) / (fullG - targetG));
							totalWeight += clamp;
							count++;
						}
						if (fullB != targetB) {
							float clamp = Mathf.Clamp01((float)(b - targetB) / (fullB - targetB));
							totalWeight += clamp;
							count++;
						}
					}

					// The final weight will be the average % close to fully light (as opposed to fully dark)
					// this light level is.
					float weight = 1;
					if (count > 0) {
						weight = totalWeight / count;
					}

					for (int i = 0; i < 256; i++) {
						if (transparent && i == 0) {
							litPalette[0] = 0;
							litPalette[1] = 0;
							litPalette[2] = 0;
							litPalette[3] = 0;
						} else {
							byte targetIndex = cmpData[0][i];
							byte targetR = palette[targetIndex * 4];
							byte targetG = palette[targetIndex * 4 + 1];
							byte targetB = palette[targetIndex * 4 + 2];

							byte fullIndex = cmpData[31][i];
							byte fullR = palette[fullIndex * 4];
							byte fullG = palette[fullIndex * 4 + 1];
							byte fullB = palette[fullIndex * 4 + 2];

							// Weighted average between light and dark.
							litPalette[i * 4] = (byte)(fullR * weight
								+ targetR * (1 - weight));
							litPalette[i * 4 + 1] = (byte)(fullG * weight
								+ targetG * (1 - weight));
							litPalette[i * 4 + 2] = (byte)(fullB * weight
								+ targetB * (1 - weight));
							// Copy alpha
							litPalette[i * 4 + 3] = palette[i * 4 + 3];
						}
					}
				} else {
					for (int i = 0; i < 256; i++) {
						if (transparent && i == 0) {
							litPalette[0] = 0;
							litPalette[1] = 0;
							litPalette[2] = 0;
							litPalette[3] = 0;
						} else {
							Buffer.BlockCopy(palette, map[i] * 4, litPalette, i * 4, 4);
						}
					}
				}

				this.importedCmpCache[(pal, cmp, lightLevel, transparent)] = litPalette;
			}
			return litPalette;
		}


		/// <summary>
		/// Clear all cached colormap data.
		/// </summary>
		public void ClearColormaps() {
			this.cmpCache.Clear();
			this.importedCmpCache.Clear();
		}

		private readonly Dictionary<string, DfBitmap> bmCache = new Dictionary<string, DfBitmap>();
		/// <summary>
		/// Retrieves a bitmap.
		/// </summary>
		/// <param name="filename">The filename of the BM.</param>
		/// <returns>The bitmap data.</returns>
		public async Task<DfBitmap> GetBitmapAsync(string filename) {
			filename = filename.ToUpper();
			if (!this.bmCache.TryGetValue(filename, out DfBitmap bm)) {
				try {
					bm = await FileLoader.Instance.LoadGobFileAsync<DfBitmap>(filename);
				} catch (Exception e) {
					this.AddError(filename, e);
				}
				this.AddWarnings(filename, bm);

				if (bm == null) {
					try {
						bm = await FileLoader.Instance.LoadGobFileAsync<DfBitmap>("DEFAULT.BM");
					} catch (Exception e) {
						this.AddError(filename, e);
					}
					this.AddWarnings(filename, bm);
				}

				this.bmCache[filename] = bm;
			}
			return bm;
		}

		private readonly Dictionary<(DfPalette, DfColormap, DfBitmap, int, bool), Texture2D> importedBmCache =
			new Dictionary<(DfPalette, DfColormap, DfBitmap, int, bool), Texture2D>();
		/// <summary>
		/// Generate a texture from a bitmap.
		/// </summary>
		/// <param name="bm">The bitmap.</param>
		/// <param name="pal">The palette.</param>
		/// <param name="cmp">The optional colormap.</param>
		/// <param name="lightLevel">The light level to use.</param>
		/// <param name="forceTransparent">Whether or not to force transparency/</param>
		/// <param name="keepTextureReadable">Whether or not to keep the Texture2D readable after creating it.</param>
		/// <returns>The generated texture.</returns>
		public Texture2D ImportBitmap(DfBitmap bm, DfPalette pal, DfColormap cmp = null, int lightLevel = 31, bool forceTransparent = false, bool keepTextureReadable = false) {
			if (lightLevel > 31) {
				lightLevel = 31;
			} else if (lightLevel < 0) {
				lightLevel = 0;
			}

			if (cmp == null) {
				lightLevel = 0;
				forceTransparent = false;
			}

			if (!this.importedBmCache.TryGetValue((pal, cmp, bm, lightLevel, forceTransparent), out Texture2D texture)) {
				byte[] palette;
				if (cmp != null) {
					palette = this.ImportColormap(pal, cmp, lightLevel, forceTransparent || (bm.Pages[0].Flags & DfBitmap.Flags.Transparent) > 0);
				} else {
					palette = this.ImportPalette(pal);
				}

				byte[] pixels = bm.Pages[0].Pixels;

				int width = bm.Pages[0].Width;
				int height = bm.Pages[0].Height;

				byte[] buffer = new byte[width * height * 4];
				for (int y = 0; y < height; y++) {
					for (int x = 0; x < width; x++) {
						Buffer.BlockCopy(palette, pixels[y * width + x] * 4, buffer, (y * width + x) * 4, 4);
					}
				}

				texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true) {
#if UNITY_EDITOR
					alphaIsTransparency = true,
#endif
					filterMode = FilterMode.Point
				};
				texture.LoadRawTextureData(buffer);
				texture.Apply(true, !keepTextureReadable);

				this.importedBmCache[(pal, cmp, bm, lightLevel, forceTransparent)] = texture;
			}

			return texture;
		}

		private readonly Dictionary<(Texture2D, Shader), Material> materials =
			new Dictionary<(Texture2D, Shader), Material>();
		/// <summary>
		/// Generate material.
		/// </summary>
		/// <param name="texture">Source texture.</param>
		/// <param name="shader">Shader to use.</param>
		/// <returns>The generated material</returns>
		public Material GetMaterial(Texture2D texture, Shader shader) {
			if (!this.materials.TryGetValue((texture, shader), out Material material)) {
				this.materials[(texture, shader)] = material = new Material(shader) {
					mainTexture = texture
				};
			}

			return material;
		}

		private readonly Dictionary<(string, string), LandruDelt> deltCache = new Dictionary<(string, string), LandruDelt>();
		/// <summary>
		/// Retrieves a delt.
		/// </summary>
		/// <param name="lfd">The LFD holding the DELT.</param>
		/// <param name="filename">The filename of the DELT.</param>
		/// <returns>The delt data.</returns>
		public async Task<LandruDelt> GetDeltAsync(string lfd, string filename) {
			lfd = lfd.ToUpper();
			filename = filename.ToUpper();
			if (!this.deltCache.TryGetValue((lfd, filename), out LandruDelt delt)) {
				try {
					delt = await FileLoader.Instance.LoadLfdFileAsync<LandruDelt>(lfd, filename);
				} catch (Exception e) {
					this.AddError(@$"{lfd}\{filename}", e);
				}
				this.AddWarnings(@$"{lfd}\{filename}", delt);

				if (delt == null) {
					try {
						delt = await FileLoader.Instance.LoadLfdFileAsync<LandruDelt>(lfd, "DEFAULT");
					} catch (Exception e) {
						this.AddError(@$"{lfd}\{filename}", e);
					}
					this.AddWarnings(@$"{lfd}\{filename}", delt);
				}

				this.deltCache[(lfd, filename)] = delt;
			}
			return delt;
		}

		private readonly Dictionary<(LandruPalette, LandruDelt), Texture2D> importedDeltCache =
			new Dictionary<(LandruPalette, LandruDelt), Texture2D>();
		/// <summary>
		/// Generate a texture from a delt.
		/// </summary>
		/// <param name="delt">The dekt.</param>
		/// <param name="pltt">The palette.</param>
		/// <param name="keepTextureReadable">Whether or not to keep the Texture2D readable after creating it.</param>
		/// <returns>The generated texture.</returns>
		public Texture2D ImportDelt(LandruDelt delt, LandruPalette pltt, bool keepTextureReadable = false) {
			if (!this.importedDeltCache.TryGetValue((pltt, delt), out Texture2D texture)) {
				byte[] palette = this.ImportPalette(pltt);

				byte[] pixels = delt.Pixels;
				BitArray mask = delt.Mask;
				int width = delt.Width;
				int height = delt.Height;
				if (width >= 1 && height >= 1) {
					byte[] buffer = new byte[width * height * 4];
					for (int y = 0; y < height; y++) {
						for (int x = 0; x < width; x++) {
							int offset = (height - y - 1) * width + x;
							if (mask[offset]) {
								Buffer.BlockCopy(palette, pixels[offset] * 4, buffer, offset * 4, 4);
							}
						}
					}

					texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true) {
#if UNITY_EDITOR
						alphaIsTransparency = true,
#endif
						filterMode = FilterMode.Point
					};
					texture.LoadRawTextureData(buffer);
					texture.Apply(true, !keepTextureReadable);
				}

				this.importedDeltCache[(pltt, delt)] = texture;
			}

			return texture;
		}

		private readonly Dictionary<string, DfFrame> fmeCache = new Dictionary<string, DfFrame>();
		/// <summary>
		/// Retrieves a frame.
		/// </summary>
		/// <param name="filename">The filename of the FME.</param>
		/// <returns>The frame data.</returns>
		public async Task<DfFrame> GetFrameAsync(string filename) {
			filename = filename.ToUpper();
			if (!this.fmeCache.TryGetValue(filename, out DfFrame fme)) {
				try {
					fme = await FileLoader.Instance.LoadGobFileAsync<DfFrame>(filename);
				} catch (Exception e) {
					this.AddError(filename, e);
				}
				this.AddWarnings(filename, fme);

				if (fme == null) {
					try {
						fme = await FileLoader.Instance.LoadGobFileAsync<DfFrame>("DEFAULT.FME");
					} catch (Exception e) {
						this.AddError(filename, e);
					}
					this.AddWarnings(filename, fme);
				}

				this.fmeCache[filename] = fme;
			}
			return fme;
		}

		private readonly Dictionary<(DfPalette, DfColormap, DfFrame, int), Sprite> importedFmeCache =
			new Dictionary<(DfPalette, DfColormap, DfFrame, int), Sprite>();
		/// <summary>
		/// Generate a sprite from a frame.
		/// </summary>
		/// <param name="pal">The palette.</param>
		/// <param name="cmp">The colormap.</param>
		/// <param name="fme">The frame.</param>
		/// <param name="lightLevel">The light level to use.</param>
		/// <param name="keepTextureReadable">Whether or not to keep the Texture2D readable after creating it.</param>
		/// <returns>The generated sprite.</returns>
		public Sprite ImportFrame(DfPalette pal, DfColormap cmp, DfFrame fme, int lightLevel, bool keepTextureReadable = false) {
			if (lightLevel > 31) {
				lightLevel = 31;
			} else if (lightLevel < 0) {
				lightLevel = 0;
			}

			if (!this.importedFmeCache.TryGetValue((pal, cmp, fme, lightLevel), out Sprite sprite)) {
				byte[] palette = this.ImportColormap(pal, cmp, lightLevel, true);

				byte[] pixels = fme.Pixels;

				int width = fme.Width;
				int height = fme.Height;

				byte[] buffer = new byte[width * height * 4];
				for (int y = 0; y < height; y++) {
					for (int x = 0; x < width; x++) {
						Buffer.BlockCopy(palette, pixels[y * width + x] * 4, buffer, (y * width + x) * 4, 4);
					}
				}

				Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true) {
#if UNITY_EDITOR
					alphaIsTransparency = true,
#endif
					filterMode = FilterMode.Point
				};
				texture.LoadRawTextureData(buffer);
				texture.Apply(true, !keepTextureReadable);

				this.importedFmeCache[(pal, cmp, fme, lightLevel)] = sprite = Sprite.Create(texture,
					new Rect(0, 0, width, height),
					new Vector2(-fme.InsertionPointX / (float)width, (height + fme.InsertionPointY) / (float)height),
					SPRITE_PIXELS_PER_UNIT);
			}

			return sprite;
		}

		private readonly Dictionary<string, DfWax> waxCache = new Dictionary<string, DfWax>();
		/// <summary>
		/// Retrieves a wax.
		/// </summary>
		/// <param name="filename">The filename of the WAX.</param>
		/// <returns>The wax data.</returns>
		public async Task<DfWax> GetWaxAsync(string filename) {
			filename = filename.ToUpper();
			if (!this.waxCache.TryGetValue(filename, out DfWax wax)) {
				try {
					wax = await FileLoader.Instance.LoadGobFileAsync<DfWax>(filename);
				} catch (Exception e) {
					this.AddError(filename, e);
				}
				this.AddWarnings(filename, wax);

				if (wax == null) {
					try {
						wax = await FileLoader.Instance.LoadGobFileAsync<DfWax>("DEFAULT.WAX");
					} catch (Exception e) {
						this.AddError(filename, e);
					}
					this.AddWarnings(filename, wax);
				}

				this.waxCache[filename] = wax;
			}
			return wax;
		}

		private readonly Dictionary<(string, string), LandruAnimation> animCache = new Dictionary<(string, string), LandruAnimation>();
		/// <summary>
		/// Retrieves an animation.
		/// </summary>
		/// <param name="lfd">The LFD holding the ANIM.</param>
		/// <param name="filename">The filename of the ANIM.</param>
		/// <returns>The animation data.</returns>
		public async Task<LandruAnimation> GetAnimationAsync(string lfd, string filename) {
			lfd = lfd.ToUpper();
			filename = filename.ToUpper();
			if (!this.animCache.TryGetValue((lfd, filename), out LandruAnimation anim)) {
				try {
					anim = await FileLoader.Instance.LoadLfdFileAsync<LandruAnimation>(lfd, filename);
				} catch (Exception e) {
					this.AddError(@$"{lfd}\{filename}", e);
				}
				this.AddWarnings(@$"{lfd}\{filename}", anim);

				if (anim == null) {
					try {
						anim = await FileLoader.Instance.LoadLfdFileAsync<LandruAnimation>(lfd, "DEFAULT");
					} catch (Exception e) {
						this.AddError(@$"{lfd}\{filename}", e);
					}
					this.AddWarnings(@$"{lfd}\{filename}", anim);
				}

				this.animCache[(lfd, filename)] = anim;
			}
			return anim;
		}

		/// <summary>
		/// Clear all cached image data.
		/// </summary>
		public void ClearImages() {
			this.bmCache.Clear();
			this.importedBmCache.Clear();
			this.materials.Clear();
			this.deltCache.Clear();
			this.importedDeltCache.Clear();
			this.fmeCache.Clear();
			this.importedBmCache.Clear();
			this.waxCache.Clear();
			this.animCache.Clear();
		}

		private readonly Dictionary<string, Df3dObject> threeDoCache = new Dictionary<string, Df3dObject>();
		/// <summary>
		/// Retrieves a 3D object.
		/// </summary>
		/// <param name="filename">The filename of the 3DO.</param>
		/// <returns>The 3D object data.</returns>
		public async Task<Df3dObject> Get3dObjectAsync(string filename) {
			filename = filename.ToUpper();
			if (!this.threeDoCache.TryGetValue(filename, out Df3dObject threeDo)) {
				try {
					threeDo = await FileLoader.Instance.LoadGobFileAsync<Df3dObject>(filename);
				} catch (Exception e) {
					this.AddError(filename, e);
				}
				this.AddWarnings(filename, threeDo);

				if (threeDo == null) {
					try {
						threeDo = await FileLoader.Instance.LoadGobFileAsync<Df3dObject>("DEFAULT.3DO");
					} catch (Exception e) {
						this.AddError(filename, e);
					}
					this.AddWarnings(filename, threeDo);
				}

				this.threeDoCache[filename] = threeDo;
			}
			return threeDo;
		}

		private readonly Dictionary<Df3dObject, GameObject> imported3doCache =
			new Dictionary<Df3dObject, GameObject>();
		/// <summary>
		/// Generate a GameObject prefab with meshes for the 3D object.
		/// </summary>
		/// <param name="threeDo">The 3DO data.</param>
		/// <returns>The generated GameObject.</returns>
		public GameObject Import3dObject(Df3dObject threeDo) {
			if (!this.imported3doCache.TryGetValue(threeDo, out GameObject gameObject)) {
				gameObject = new GameObject() {
					name = threeDo.Name,
					layer = LayerMask.NameToLayer("Objects")
				};
				gameObject.transform.SetParent(this.transform, false);
				gameObject.SetActive(false);

				ThreeDoRenderer renderer = gameObject.AddComponent<ThreeDoRenderer>();
				renderer.CreateGeometry(threeDo);

				this.imported3doCache[threeDo] = gameObject;
			}

			return gameObject;
		}

		/// <summary>
		/// Clear all cached 3D object data.
		/// </summary>
		public void Clear3dObjects() {
			this.threeDoCache.Clear();

			foreach (GameObject obj in this.imported3doCache.Values.ToArray()) {
				DestroyImmediate(obj);
			}

			this.imported3doCache.Clear();
		}

		private readonly Dictionary<string, AutodeskVue> vueCache = new Dictionary<string, AutodeskVue>();
		/// <summary>
		/// Retrieves a 3D object animation.
		/// </summary>
		/// <param name="filename">The filename of the VUE.</param>
		/// <returns>The 3D object animation data.</returns>
		public async Task<AutodeskVue> GetVueAsync(string filename) {
			filename = filename.ToUpper();
			if (!this.vueCache.TryGetValue(filename, out AutodeskVue vue)) {
				try {
					vue = await FileLoader.Instance.LoadGobFileAsync<AutodeskVue>(filename);
				} catch (Exception e) {
					this.AddError(filename, e);
				}
				this.AddWarnings(filename, vue);

				if (vue == null) {
					try {
						vue = await FileLoader.Instance.LoadGobFileAsync<AutodeskVue>("DEFAULT.VUE");
					} catch (Exception e) {
						this.AddError(filename, e);
					}
					this.AddWarnings(filename, vue);
				}

				this.vueCache[filename] = vue;
			}
			return vue;
		}

		private readonly Dictionary<VueObject, AnimationClip> importedVueCache =
			new Dictionary<VueObject, AnimationClip>();
		/// <summary>
		/// Generate an AnimationClip for the 3D object animation.
		/// </summary>
		/// <param name="vue">The VUE data.</param>
		/// <returns>The generated AnimationClip.</returns>
		public AnimationClip ImportVue(VueObject vue) {
			if (!this.importedVueCache.TryGetValue(vue, out AnimationClip clip)) {
				Matrix4x4[] transform = vue.Frames.Select(x => x.ToUnity()).ToArray();

				// We could be tweaking in/outs for keyframes but I didn't go that far.
				clip = new AnimationClip {
					legacy = true
				};
				clip.SetCurve("", typeof(Transform), $"{nameof(Transform.localPosition)}.{nameof(Vector3.x)}",
					new AnimationCurve(transform.Select((x, i) => new Keyframe(i, x.m03 * LevelGeometryGenerator.GEOMETRY_SCALE)).ToArray()));
				clip.SetCurve("", typeof(Transform), $"{nameof(Transform.localPosition)}.{nameof(Vector3.y)}",
					new AnimationCurve(transform.Select((x, i) => new Keyframe(i, x.m23 * LevelGeometryGenerator.GEOMETRY_SCALE)).ToArray()));
				clip.SetCurve("", typeof(Transform), $"{nameof(Transform.localPosition)}.{nameof(Vector3.z)}",
					new AnimationCurve(transform.Select((x, i) => new Keyframe(i, x.m13 * LevelGeometryGenerator.GEOMETRY_SCALE)).ToArray()));
				clip.SetCurve("", typeof(Transform), $"{nameof(Transform.localScale)}.{nameof(Vector3.x)}",
					new AnimationCurve(transform.Select((x, i) => new Keyframe(i,
					new Vector4(x.m00, x.m10, x.m20, x.m30).magnitude)).ToArray()));
				clip.SetCurve("", typeof(Transform), $"{nameof(Transform.localScale)}.{nameof(Vector3.y)}",
					new AnimationCurve(transform.Select((x, i) => new Keyframe(i,
					new Vector4(x.m01, x.m11, x.m21, x.m31).magnitude)).ToArray()));
				clip.SetCurve("", typeof(Transform), $"{nameof(Transform.localScale)}.{nameof(Vector3.z)}",
					new AnimationCurve(transform.Select((x, i) => new Keyframe(i,
					new Vector4(x.m02, x.m12, x.m22, x.m32).magnitude)).ToArray()));
				clip.SetCurve("", typeof(Transform), $"{nameof(Transform.localRotation)}.{nameof(Quaternion.w)}",
					new AnimationCurve(transform.Select((x, i) => new Keyframe(i,
					Quaternion.LookRotation(new Vector3(x.m01, x.m21, x.m11), new Vector3(x.m02, x.m22, x.m12)).w)
					).ToArray()));
				clip.SetCurve("", typeof(Transform), $"{nameof(Transform.localRotation)}.{nameof(Quaternion.x)}",
					new AnimationCurve(transform.Select((x, i) => new Keyframe(i,
					Quaternion.LookRotation(new Vector3(x.m01, x.m21, x.m11), new Vector3(x.m02, x.m22, x.m12)).x)
					).ToArray()));
				clip.SetCurve("", typeof(Transform), $"{nameof(Transform.localRotation)}.{nameof(Quaternion.y)}",
					new AnimationCurve(transform.Select((x, i) => new Keyframe(i,
					Quaternion.LookRotation(new Vector3(x.m01, x.m21, x.m11), new Vector3(x.m02, x.m22, x.m12)).y)
					).ToArray()));
				clip.SetCurve("", typeof(Transform), $"{nameof(Transform.localRotation)}.{nameof(Quaternion.z)}",
					new AnimationCurve(transform.Select((x, i) => new Keyframe(i,
					Quaternion.LookRotation(new Vector3(x.m01, x.m21, x.m11), new Vector3(x.m02, x.m22, x.m12)).z)
					).ToArray()));

				clip.EnsureQuaternionContinuity();

				this.importedVueCache[vue] = clip;
			}

			return clip;
		}

		/// <summary>
		/// Clear all cached 3D animation object data.
		/// </summary>
		public void ClearVues() {
			this.vueCache.Clear();
			this.importedVueCache.Clear();
		}

		private readonly Dictionary<string, CreativeVoice> vocCache = new Dictionary<string, CreativeVoice>();
		/// <summary>
		/// Retrieves a voice audio file.
		/// </summary>
		/// <param name="filename">The filename of the VOC.</param>
		/// <returns>The voice audio file data.</returns>
		public async Task<CreativeVoice> GetCreativeVoiceAsync(string filename) {
			filename = filename.ToUpper();
			if (!this.vocCache.TryGetValue(filename, out CreativeVoice voc)) {
				try {
					voc = await FileLoader.Instance.LoadGobFileAsync<CreativeVoice>(filename);
				} catch (Exception e) {
					this.AddError(filename, e);
				}
				this.AddWarnings(filename, voc);

				if (voc == null) {
					try {
						voc = await FileLoader.Instance.LoadGobFileAsync<CreativeVoice>("DEFAULT.VOC");
					} catch (Exception e) {
						this.AddError(filename, e);
					}
					this.AddWarnings(filename, voc);
				}

				this.vocCache[filename] = voc;
			}
			return voc;
		}

		private readonly Dictionary<(string, string), CreativeVoice> voicCache = new Dictionary<(string, string), CreativeVoice>();
		/// <summary>
		/// Retrieves a voice audio file.
		/// </summary>
		/// <param name="lfd">The LFD holding the VOIC.</param>
		/// <param name="filename">The filename of the VOIC.</param>
		/// <returns>The voice audio file data.</returns>
		public async Task<CreativeVoice> GetCreativeVoiceAsync(string lfd, string filename) {
			lfd = lfd.ToUpper();
			filename = filename.ToUpper();
			if (!this.voicCache.TryGetValue((lfd, filename), out CreativeVoice voic)) {
				try {
					voic = await FileLoader.Instance.LoadLfdFileAsync<CreativeVoice>(lfd, filename);
				} catch (Exception e) {
					this.AddError(@$"{lfd}\{filename}", e);
				}
				this.AddWarnings(@$"{lfd}\{filename}", voic);

				if (voic == null) {
					try {
						voic = await FileLoader.Instance.LoadLfdFileAsync<CreativeVoice>(lfd, "DEFAULT");
					} catch (Exception e) {
						this.AddError(@$"{lfd}\{filename}", e);
					}
					this.AddWarnings(@$"{lfd}\{filename}", voic);
				}

				this.voicCache[(lfd, filename)] = voic;
			}
			return voic;
		}

		private readonly Dictionary<CreativeVoice, VocPlayer> importedVocCache = new Dictionary<CreativeVoice, VocPlayer>();
		/// <summary>
		/// Generate a GameObject and VocPlayer script with AudioSources to play back the sound.
		/// </summary>
		/// <param name="voc">The VOC data.</param>
		/// <returns>The generated GameObject and VocPlayer script.</returns>
		// TODO probably can just get rid of this function and handle this elsewhere
		public VocPlayer ImportCreativeVoice(CreativeVoice voc) {
			if (!this.importedVocCache.TryGetValue(voc, out VocPlayer player)) {
				GameObject go = new GameObject() {
					name = "Voc"
				};
				go.transform.SetParent(this.transform, false);

				player = go.AddComponent<VocPlayer>();
				player.Voc = voc;

				this.importedVocCache[voc] = player;
			}

			return player;
		}

		/// <summary>
		/// Clear all cached voice audio data.
		/// </summary>
		public void ClearCreativeVoices() {
			this.vocCache.Clear();
			this.voicCache.Clear();

			foreach (GameObject obj in this.importedVocCache.Values.Select(x => x.gameObject).ToArray()) {
				DestroyImmediate(obj);
			}

			this.importedVocCache.Clear();
		}

		private readonly Dictionary<string, DfGeneralMidi> gmdCache = new Dictionary<string, DfGeneralMidi>();
		/// <summary>
		/// Retrieves a general MIDI file.
		/// </summary>
		/// <param name="filename">The filename of the GMD.</param>
		/// <returns>The general MIDI data.</returns>
		public async Task<DfGeneralMidi> GetGeneralMidi(string filename) {
			filename = filename.ToUpper();
			if (!this.gmdCache.TryGetValue(filename, out DfGeneralMidi gmd)) {
				try {
					gmd = await FileLoader.Instance.LoadGobFileAsync<DfGeneralMidi>(filename);
				} catch (Exception e) {
					this.AddError(filename, e);
				}
				this.AddWarnings(filename, gmd);

				if (gmd == null) {
					try {
						gmd = await FileLoader.Instance.LoadGobFileAsync<DfGeneralMidi>("DEFAULT.GMD");
					} catch (Exception e) {
						this.AddError(filename, e);
					}
					this.AddWarnings(filename, gmd);
				}

				this.gmdCache[filename] = gmd;
			}
			return gmd;
		}

		private readonly Dictionary<(string, string), DfGeneralMidi> gmidCache = new Dictionary<(string, string), DfGeneralMidi>();
		/// <summary>
		/// Retrieves a general MIDI file.
		/// </summary>
		/// <param name="lfd">The LFD holding the GMID.</param>
		/// <param name="filename">The filename of the GMID.</param>
		/// <returns>The general MIDI data.</returns>
		public async Task<DfGeneralMidi> GetGeneralMidi(string lfd, string filename) {
			lfd = lfd.ToUpper();
			filename = filename.ToUpper();
			if (!this.gmidCache.TryGetValue((lfd, filename), out DfGeneralMidi gmid)) {
				try {
					gmid = await FileLoader.Instance.LoadLfdFileAsync<DfGeneralMidi>(lfd, filename);
				} catch (Exception e) {
					this.AddError(@$"{lfd}\{filename}", e);
				}
				this.AddWarnings(@$"{lfd}\{filename}", gmid);

				if (gmid == null) {
					try {
						gmid = await FileLoader.Instance.LoadLfdFileAsync<DfGeneralMidi>(lfd, "DEFAULT");
					} catch (Exception e) {
						this.AddError(@$"{lfd}\{filename}", e);
					}
					this.AddWarnings(@$"{lfd}\{filename}", gmid);
				}

				this.gmidCache[(lfd, filename)] = gmid;
			}
			return gmid;
		}


		/// <summary>
		/// Clear all cached general MIDI data.
		/// </summary>
		public void ClearGeneralMidis() {
			this.gmdCache.Clear();
			this.gmidCache.Clear();
		}
		
		/// <summary>
		/// Clear all cached data.
		/// </summary>
		public void Clear() {
			this.ClearPalettes();
			this.ClearColormaps();
			this.ClearImages();
			this.Clear3dObjects();
			this.ClearVues();
			this.ClearCreativeVoices();
			this.ClearGeneralMidis();
		}

		/// <summary>
		/// Regenerate materials and sprites for changed lighting settings.
		/// </summary>
		public void RegenerateMaterials() {
			(DfPalette pal, DfColormap cmp, int lightLevel, bool transparent)[] cmpKeys =
				this.importedCmpCache.Keys.ToArray();
			this.importedCmpCache.Clear();
			foreach ((DfPalette pal, DfColormap cmp, int lightLevel, bool transparent) in cmpKeys) {
				this.ImportColormap(pal, cmp, lightLevel, transparent);
			}

			((DfPalette pal, DfColormap cmp, DfBitmap bm, int lightLevel, bool forceTransparent), Texture2D texture)[] bms =
				this.importedBmCache.Select(x => {
					x.Deconstruct(out (DfPalette, DfColormap, DfBitmap, int, bool) key, out Texture2D value);
					return (key, value);
				}).ToArray();
			this.importedBmCache.Clear();
			Dictionary<Texture2D, Texture2D> textureMap = new Dictionary<Texture2D, Texture2D>();
			foreach (((DfPalette pal, DfColormap cmp, DfBitmap bm, int lightLevel, bool forceTransparent), Texture2D texture) in bms) {
				Texture2D newTexture = this.ImportBitmap(bm, pal, cmp, lightLevel, forceTransparent);
				textureMap[texture] = newTexture;
			}

			int count = 0;
			foreach (((Texture2D matTexture, Shader shader), Material material) in this.materials.ToArray()) {
				if (textureMap.TryGetValue(matTexture, out Texture2D newTexture)) {
					this.materials.Remove((matTexture, shader));
					material.mainTexture = newTexture;
					this.materials[(newTexture, shader)] = material;
					count++;
				}
			}

			Debug.Log($"Adjusted {count} shared materials for new lighting.");

			((DfPalette pal, DfColormap cmp, DfFrame fme, int lightLevel), Sprite sprite)[] fmes =
				this.importedFmeCache.Select(x => {
					x.Deconstruct(out (DfPalette, DfColormap, DfFrame, int) key, out Sprite value);
					return (key, value);
				}).ToArray();
			this.importedFmeCache.Clear();
			Dictionary<Sprite, Sprite> spriteMap = new Dictionary<Sprite, Sprite>();
			foreach (((DfPalette pal, DfColormap cmp, DfFrame fme, int lightLevel), Sprite sprite) in fmes) {
				Sprite newSprite = this.ImportFrame(pal, cmp, fme, lightLevel);
				spriteMap[sprite] = newSprite;
			}

			count = 0;
			foreach (SpriteRenderer renderer in SceneManager.GetActiveScene()
				.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<SpriteRenderer>(true))) {

				if (renderer.sprite == null) {
					continue;
				}

				if (spriteMap.TryGetValue(renderer.sprite, out Sprite newSprite)) {
					renderer.sprite = newSprite;
					count++;
				}
			}

			Debug.Log($"Adjusted {count} sprites for new lighting.");
		}

		/// <summary>
		/// A warning which occurred during loading or rendering.
		/// </summary>
		public struct LoadWarning {
			public string FileName;
			public int Line;
			public string Message;
			public bool Fatal;
		}

		private readonly List<LoadWarning> warnings = new List<LoadWarning>();
		/// <summary>
		/// Get the current list of warnings.
		/// </summary>
		public IEnumerable<LoadWarning> Warnings => this.warnings;

		/// <summary>
		/// Clear the warnings.
		/// </summary>
		public void ClearWarnings() {
			this.warnings.Clear();
		}

		/// <summary>
		/// Add warnings from a file loading.
		/// </summary>
		/// <param name="fileName">The name of the file.</param>
		/// <param name="file">The file data which contains the warnings.</param>
		public void AddWarnings(string fileName, IDfFile file) {
			if (!this.ShowWarnings || file == null) {
				return;
			}

			foreach (Warning warning in file.Warnings) {
				Debug.LogWarning($"{fileName}{(warning.Line > 0 ? $":{warning.Line}" : "")} - {warning.Message}");
			}
			this.warnings.AddRange(file.Warnings.Select(x => new LoadWarning() {
				FileName = fileName,
				Line = x.Line,
				Message = x.Message
			}));
		}

		/// <summary>
		/// Add a warning for file rendering.
		/// </summary>
		/// <param name="fileName">The name of the file.</param>
		/// <param name="message">The warning message.</param>
		public void AddWarning(string fileName, string message) {
			if (!this.ShowWarnings) {
				return;
			}

			Debug.LogWarning($"{fileName} - {message}");
			this.warnings.Add(new LoadWarning() {
				FileName = fileName,
				Message = message
			});
		}

		/// <summary>
		/// Add a fatal error for file rendering.
		/// </summary>
		/// <param name="fileName">The name of the file.</param>
		/// <param name="e">The exception that ocurred.</param>
		public void AddError(string fileName, Exception e) {
			Debug.LogError(e);
			this.warnings.Add(new LoadWarning() {
				FileName = fileName,
				Message = e.Message,
				Fatal = true
			});
		}
	}
}