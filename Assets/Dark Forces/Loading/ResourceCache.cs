using MZZT.DarkForces.Converters;
using MZZT.DarkForces.FileFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using static MZZT.DarkForces.FileFormats.AutodeskVue;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Vector3 = UnityEngine.Vector3;

namespace MZZT.DarkForces {
	/// <summary>
	/// This class caches loaded file data (so it can be retrieved insted of being loaded from scratch again).
	/// It also handles importing data into formats Unity can use.
	/// </summary>
	public class ResourceCache : Singleton<ResourceCache> {
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

		private readonly Dictionary<string, DfPalette> palCache = new();
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

				if (pal == null) {
					try {
						pal = await FileLoader.Instance.LoadGobFileAsync<DfPalette>("DEFAULT.PAL");
					} catch (Exception e) {
						this.AddError(filename, e);
					}
				}

				this.AddPalette(filename, pal);
			}
			return pal;
		}

		/// <summary>
		/// Forces a PAL into the cache.
		/// </summary>
		/// <param name="filename">The filename to associate with the PAL.</param>
		/// <param name="pal">The PAL.</param>
		public void AddPalette(string filename, DfPalette pal) {
			filename = filename.ToUpper();
			this.AddWarnings(filename, pal);
			this.palCache[filename] = pal;
		}

		private readonly Dictionary<(DfPalette, bool), byte[]> importedPalCache = new();
		/// <summary>
		/// Import a palette into 32-bit RGBA format.
		/// </summary>
		/// <param name="pal">The palette.</param>
		/// <returns>A 256 color 32-bit RGBA palette in a byte array.</returns>
		public byte[] ImportPalette(DfPalette pal, bool transparent = false) {
			if (!this.importedPalCache.TryGetValue((pal, transparent), out byte[] palette)) {
				this.importedPalCache[(pal, transparent)] = palette = PalConverter.ToByteArray(pal, transparent);
			}
			return palette;
		}

		private readonly Dictionary<(string, string), LandruPalette> plttCache = new();
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
					this.AddError(Path.Combine(lfd, filename), e);
				}

				if (pltt == null) {
					try {
						pltt = await FileLoader.Instance.LoadLfdFileAsync<LandruPalette>(lfd, "DEFAULT");
					} catch (Exception e) {
						this.AddError(Path.Combine(lfd, filename), e);
					}
				}

				this.AddPalette(lfd, filename, pltt);
			}
			return pltt;
		}

		/// <summary>
		/// Forces a PLTT into the cache.
		/// </summary>
		/// <param name="lfd">The LFD to associate with the PLTT.</param>
		/// <param name="filename">The filename to associate with the PLTT.</param>
		/// <param name="pltt">The PLTT.</param>
		public void AddPalette(string lfd, string filename, LandruPalette pltt) {
			lfd = lfd.ToUpper();
			filename = filename.ToUpper();
			this.AddWarnings(Path.Combine(lfd, filename), pltt);
			this.plttCache[(lfd, filename)] = pltt;
		}

		private readonly Dictionary<LandruPalette, byte[]> importedPlttCache = new();
		/// <summary>
		/// Import a palette into 32-bit RGBA format.
		/// </summary>
		/// <param name="pltt">The palette.</param>
		/// <returns>A 256 color 32-bit RGBA palette in a byte array.</returns>
		public byte[] ImportPalette(LandruPalette pltt) {
			if (!this.importedPlttCache.TryGetValue(pltt, out byte[] palette)) {
				this.importedPlttCache[pltt] = palette = PlttConverter.ToByteArray(pltt);
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

		private readonly Dictionary<string, DfColormap> cmpCache = new();
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

				if (cmp == null) {
					try {
						cmp = await FileLoader.Instance.LoadGobFileAsync<DfColormap>("DEFAULT.CMP");
					} catch (Exception e) {
						this.AddError(filename, e);
					}
				}

				this.AddColormap(filename, cmp);
			}
			return cmp;
		}

		/// <summary>
		/// Forces a CMP into the cache.
		/// </summary>
		/// <param name="filename">The filename to associate with the CMP.</param>
		/// <param name="cmp">The CMP.</param>
		public void AddColormap(string filename, DfColormap cmp) {
			filename = filename.ToUpper();
			this.AddWarnings(filename, cmp);
			this.cmpCache[filename] = cmp;
		}

		private readonly Dictionary<(DfPalette, DfColormap, int, bool), byte[]> importedCmpCache = new();
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
			if (this.fullBright) {
				lightLevel = 31;
			}

			if (!this.importedCmpCache.TryGetValue((pal, cmp, lightLevel, transparent), out byte[] litPalette)) {
				byte[] palette = this.ImportPalette(pal);

				this.importedCmpCache[(pal, cmp, lightLevel, transparent)] = litPalette = CmpConverter.ToByteArray(cmp, palette, lightLevel, transparent, this.bypassCmpDithering);
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

		private readonly Dictionary<string, DfBitmap> bmCache = new();
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

				if (bm == null) {
					try {
						bm = await FileLoader.Instance.LoadGobFileAsync<DfBitmap>("DEFAULT.BM");
					} catch (Exception e) {
						this.AddError(filename, e);
					}
				}

				this.bmCache[filename] = bm;
			}
			return bm;
		}

		/// <summary>
		/// Forces a BM into the cache.
		/// </summary>
		/// <param name="filename">The filename to associate with the BM.</param>
		/// <param name="bm">The BM.</param>
		public void AddBitmap(string filename, DfBitmap bm) {
			filename = filename.ToUpper();
			this.AddWarnings(filename, bm);
			this.bmCache[filename] = bm;
		}

		private readonly Dictionary<(DfPalette, DfColormap, DfBitmap.Page, int, bool), Texture2D> importedBmCache = new();
		/// <summary>
		/// Generate a texture from a bitmap.
		/// </summary>
		/// <param name="bm">The bitmap.</param>
		/// <param name="pal">The palette.</param>
		/// <param name="cmp">The optional colormap.</param>
		/// <param name="lightLevel">The light level to use.</param>
		/// <param name="forceTransparent">Whether or not to force transparency.</param>
		/// <param name="keepTextureReadable">Whether or not to keep the Texture2D readable after creating it.</param>
		/// <returns>The generated texture.</returns>
		public Texture2D ImportBitmap(DfBitmap.Page page, DfPalette pal, DfColormap cmp = null, int lightLevel = 31, bool forceTransparent = false, bool keepTextureReadable = false) {
			if (lightLevel > 31) {
				lightLevel = 31;
			} else if (lightLevel < 0) {
				lightLevel = 0;
			}

			if (cmp == null) {
				lightLevel = 0;
			}

			forceTransparent = forceTransparent || page.Flags.HasFlag(DfBitmap.Flags.Transparent);

			if (!this.importedBmCache.TryGetValue((pal, cmp, page, lightLevel, forceTransparent), out Texture2D texture)) {
				byte[] palette;
				if (cmp != null) {
					palette = this.ImportColormap(pal, cmp, lightLevel, forceTransparent);
				} else {
					palette = this.ImportPalette(pal, forceTransparent);
				}
				
				this.importedBmCache[(pal, cmp, page, lightLevel, forceTransparent)] = texture = BmConverter.ToTexture(page, palette, keepTextureReadable);
			}

			return texture;
		}

		private readonly Dictionary<(Texture2D, Shader), Material> materials = new();
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

		private readonly Dictionary<(string, string), LandruDelt> deltCache = new();
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
					this.AddError(Path.Combine(lfd, filename), e);
				}

				if (delt == null) {
					try {
						delt = await FileLoader.Instance.LoadLfdFileAsync<LandruDelt>(lfd, "DEFAULT");
					} catch (Exception e) {
						this.AddError(Path.Combine(lfd, filename), e);
					}
				}

				this.AddDelt(lfd, filename, delt);
			}
			return delt;
		}

		/// <summary>
		/// Forces a DELT into the cache.
		/// </summary>
		/// <param name="lfd">The LFD to associate with the DELT.</param>
		/// <param name="filename">The filename to associate with the DELT.</param>
		/// <param name="delt">The DELT.</param>
		public void AddDelt(string lfd, string filename, LandruDelt delt) {
			lfd = lfd.ToUpper();
			filename = filename.ToUpper();
			this.AddWarnings(Path.Combine(lfd, filename), delt);
			this.deltCache[(lfd, filename)] = delt;
		}

		private readonly Dictionary<(LandruPalette, LandruDelt), Texture2D> importedDeltCache = new();
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

				this.importedDeltCache[(pltt, delt)] = texture = DeltConverter.ToTexture(delt, palette, keepTextureReadable);
			}

			return texture;
		}

		private readonly Dictionary<string, DfFrame> fmeCache = new();
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

				if (fme == null) {
					try {
						fme = await FileLoader.Instance.LoadGobFileAsync<DfFrame>("DEFAULT.FME");
					} catch (Exception e) {
						this.AddError(filename, e);
					}
				}

				this.AddFrame(filename, fme);
			}
			return fme;
		}

		/// <summary>
		/// Forces a FME into the cache.
		/// </summary>
		/// <param name="filename">The filename to associate with the FME.</param>
		/// <param name="fme">The FME.</param>
		public void AddFrame(string filename, DfFrame fme) {
			filename = filename.ToUpper();
			this.AddWarnings(filename, fme);
			this.fmeCache[filename] = fme;
		}

		private readonly Dictionary<(DfPalette, DfColormap, DfFrame, int), Sprite> importedFmeCache = new();
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

			if (cmp == null) {
				lightLevel = 0;
			}

			if (!this.importedFmeCache.TryGetValue((pal, cmp, fme, lightLevel), out Sprite sprite)) {
				byte[] palette;
				if (cmp != null) {
					palette = this.ImportColormap(pal, cmp, lightLevel, true);
				} else {
					palette = this.ImportPalette(pal, true);
				}

				this.importedFmeCache[(pal, cmp, fme, lightLevel)] = sprite = FmeConverter.ToSprite(fme, palette, keepTextureReadable);
			}

			return sprite;
		}

		private readonly Dictionary<string, DfWax> waxCache = new();
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

				if (wax == null) {
					try {
						wax = await FileLoader.Instance.LoadGobFileAsync<DfWax>("DEFAULT.WAX");
					} catch (Exception e) {
						this.AddError(filename, e);
					}
				}

				this.AddWax(filename, wax);
			}
			return wax;
		}

		/// <summary>
		/// Forces a WAX into the cache.
		/// </summary>
		/// <param name="filename">The filename to associate with the WAX.</param>
		/// <param name="wax">The WAX.</param>
		public void AddWax(string filename, DfWax wax) {
			filename = filename.ToUpper();
			this.AddWarnings(filename, wax);
			this.waxCache[filename] = wax;
		}

		private readonly Dictionary<(string, string), LandruAnimation> animCache = new();
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
					this.AddError(Path.Combine(lfd, filename), e);
				}

				if (anim == null) {
					try {
						anim = await FileLoader.Instance.LoadLfdFileAsync<LandruAnimation>(lfd, "DEFAULT");
					} catch (Exception e) {
						this.AddError(Path.Combine(lfd, filename), e);
					}
				}

				this.AddAnimation(lfd, filename, anim);
			}
			return anim;
		}

		/// <summary>
		/// Forces a ANIM into the cache.
		/// </summary>
		/// <param name="lfd">The LFD to associate with the ANIM.</param>
		/// <param name="filename">The filename to associate with the ANIM.</param>
		/// <param name="anim">The ANIM.</param>
		public void AddAnimation(string lfd, string filename, LandruAnimation anim) {
			lfd = lfd.ToUpper();
			filename = filename.ToUpper();
			this.AddWarnings(Path.Combine(lfd, filename), anim);
			this.animCache[(lfd, filename)] = anim;
		}

		private readonly Dictionary<string, DfFont> fntCache = new();
		/// <summary>
		/// Retrieves a font.
		/// </summary>
		/// <param name="filename">The filename of the FNT.</param>
		/// <returns>The font data.</returns>
		public async Task<DfFont> GetFontAsync(string filename) {
			filename = filename.ToUpper();
			if (!this.fntCache.TryGetValue(filename, out DfFont fnt)) {
				try {
					fnt = await FileLoader.Instance.LoadGobFileAsync<DfFont>(filename);
				} catch (Exception e) {
					this.AddError(filename, e);
				}

				if (fnt == null) {
					try {
						fnt = await FileLoader.Instance.LoadGobFileAsync<DfFont>("DEFAULT.FNT");
					} catch (Exception e) {
						this.AddError(filename, e);
					}
				}

				this.AddFont(filename, fnt);
			}
			return fnt;
		}

		/// <summary>
		/// Forces a FNT into the cache.
		/// </summary>
		/// <param name="filename">The filename to associate with the FNT.</param>
		/// <param name="fnt">The FNT.</param>
		public void AddFont(string filename, DfFont fnt) {
			filename = filename.ToUpper();
			this.AddWarnings(filename, fnt);
			this.fntCache[filename] = fnt;
		}

		private readonly Dictionary<(string, string), LandruFont> fontCache = new();
		/// <summary>
		/// Retrieves a font.
		/// </summary>
		/// <param name="lfd">The LFD holding the FONT.</param>
		/// <param name="filename">The filename of the FONT.</param>
		/// <returns>The font data.</returns>
		public async Task<LandruFont> GetFontAsync(string lfd, string filename) {
			lfd = lfd.ToUpper();
			filename = filename.ToUpper();
			if (!this.fontCache.TryGetValue((lfd, filename), out LandruFont font)) {
				try {
					font = await FileLoader.Instance.LoadLfdFileAsync<LandruFont>(lfd, filename);
				} catch (Exception e) {
					this.AddError(Path.Combine(lfd, filename), e);
				}

				if (font == null) {
					try {
						font = await FileLoader.Instance.LoadLfdFileAsync<LandruFont>(lfd, "DEFAULT");
					} catch (Exception e) {
						this.AddError(Path.Combine(lfd, filename), e);
					}
				}

				this.AddFont(lfd, filename, font);
			}
			return font;
		}

		/// <summary>
		/// Forces a FONT into the cache.
		/// </summary>
		/// <param name="lfd">The LFD to associate with the FONT.</param>
		/// <param name="filename">The filename to associate with the FONT.</param>
		/// <param name="font">The FONT.</param>
		public void AddFont(string lfd, string filename, LandruFont font) {
			lfd = lfd.ToUpper();
			filename = filename.ToUpper();
			this.AddWarnings(Path.Combine(lfd, filename), font);
			this.fontCache[(lfd, filename)] = font;
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
			this.fntCache.Clear();
			this.fontCache.Clear();
		}

		private readonly Dictionary<string, Df3dObject> threeDoCache = new();
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

				if (threeDo == null) {
					try {
						threeDo = await FileLoader.Instance.LoadGobFileAsync<Df3dObject>("DEFAULT.3DO");
					} catch (Exception e) {
						this.AddError(filename, e);
					}
				}

				this.Add3dObject(filename, threeDo);
			}
			return threeDo;
		}

		/// <summary>
		/// Forces a 3DO into the cache.
		/// </summary>
		/// <param name="filename">The filename to associate with the 3DO.</param>
		/// <param name="threeDo">The 3DO.</param>
		public void Add3dObject(string filename, Df3dObject threeDo) {
			filename = filename.ToUpper();
			this.AddWarnings(filename, threeDo);
			this.threeDoCache[filename] = threeDo;
		}

		private readonly Dictionary<Df3dObject, GameObject> imported3doCache = new();
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

			foreach (GameObject obj in this.imported3doCache.Values) {
				DestroyImmediate(obj);
			}

			this.imported3doCache.Clear();
		}

		private readonly Dictionary<string, AutodeskVue> vueCache = new();
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

				if (vue == null) {
					try {
						vue = await FileLoader.Instance.LoadGobFileAsync<AutodeskVue>("DEFAULT.VUE");
					} catch (Exception e) {
						this.AddError(filename, e);
					}
				}

				this.AddVue(filename, vue);
			}
			return vue;
		}

		/// <summary>
		/// Forces a VUE into the cache.
		/// </summary>
		/// <param name="filename">The filename to associate with the VUE.</param>
		/// <param name="vue">The VUE.</param>
		public void AddVue(string filename, AutodeskVue vue) {
			filename = filename.ToUpper();
			this.AddWarnings(filename, vue);
			this.vueCache[filename] = vue;
		}

		private readonly Dictionary<VueObject, AnimationClip> importedVueCache = new();
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

		private readonly Dictionary<string, CreativeVoice> vocCache = new();
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

				if (voc == null) {
					try {
						voc = await FileLoader.Instance.LoadGobFileAsync<CreativeVoice>("DEFAULT.VOC");
					} catch (Exception e) {
						this.AddError(filename, e);
					}
				}

				this.AddCreativeVoice(filename, voc);
			}
			return voc;
		}

		/// <summary>
		/// Forces a VOC into the cache.
		/// </summary>
		/// <param name="filename">The filename to associate with the VOC.</param>
		/// <param name="voc">The VOC.</param>
		public void AddCreativeVoice(string filename, CreativeVoice voc) {
			filename = filename.ToUpper();
			this.AddWarnings(filename, voc);
			this.vocCache[filename] = voc;
		}

		private readonly Dictionary<(string, string), CreativeVoice> voicCache = new();
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
					this.AddError(Path.Combine(lfd, filename), e);
				}

				if (voic == null) {
					try {
						voic = await FileLoader.Instance.LoadLfdFileAsync<CreativeVoice>(lfd, "DEFAULT");
					} catch (Exception e) {
						this.AddError(Path.Combine(lfd, filename), e);
					}
				}

				this.AddCreativeVoice(lfd, filename, voic);
			}
			return voic;
		}

		/// <summary>
		/// Forces a VOIC into the cache.
		/// </summary>
		/// <param name="lfd">The LFD to associate with the VOIC.</param>
		/// <param name="filename">The filename to associate with the VOIC.</param>
		/// <param name="voic">The VOIC.</param>
		public void AddCreativeVoice(string lfd, string filename, CreativeVoice voic) {
			lfd = lfd.ToUpper();
			filename = filename.ToUpper();
			this.AddWarnings(Path.Combine(lfd, filename), voic);
			this.voicCache[(lfd, filename)] = voic;
		}

		private readonly Dictionary<CreativeVoice, VocPlayer> importedVocCache = new();
		/// <summary>
		/// Generate a GameObject and VocPlayer script with AudioSources to play back the sound.
		/// </summary>
		/// <param name="voc">The VOC data.</param>
		/// <returns>The generated GameObject and VocPlayer script.</returns>
		// TODO probably can just get rid of this function and handle this elsewhere
		public VocPlayer ImportCreativeVoice(CreativeVoice voc) {
			if (!this.importedVocCache.TryGetValue(voc, out VocPlayer player)) {
				GameObject go = new() {
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

		private readonly Dictionary<string, DfGeneralMidi> gmdCache = new();
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

				if (gmd == null) {
					try {
						gmd = await FileLoader.Instance.LoadGobFileAsync<DfGeneralMidi>("DEFAULT.GMD");
					} catch (Exception e) {
						this.AddError(filename, e);
					}
				}

				this.AddGeneralMidi(filename, gmd);
			}
			return gmd;
		}

		/// <summary>
		/// Forces a GMD into the cache.
		/// </summary>
		/// <param name="filename">The filename to associate with the GMD.</param>
		/// <param name="gmd">The GMD.</param>
		public void AddGeneralMidi(string filename, DfGeneralMidi gmd) {
			filename = filename.ToUpper();
			this.AddWarnings(filename, gmd);
			this.gmdCache[filename] = gmd;
		}

		private readonly Dictionary<(string, string), DfGeneralMidi> gmidCache = new();
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
		/// Forces a GMID into the cache.
		/// </summary>
		/// <param name="lfd">The LFD to associate with the GMID.</param>
		/// <param name="filename">The filename to associate with the GMID.</param>
		/// <param name="gmid">The GMID.</param>
		public void AddGeneralMidi(string lfd, string filename, DfGeneralMidi gmid) {
			lfd = lfd.ToUpper();
			filename = filename.ToUpper();
			this.AddWarnings(Path.Combine(lfd, filename), gmid);
			this.gmidCache[(lfd, filename)] = gmid;
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

			((DfPalette pal, DfColormap cmp, DfBitmap.Page page, int lightLevel, bool forceTransparent), Texture2D texture)[] bms =
				this.importedBmCache.Select(x => {
					x.Deconstruct(out (DfPalette, DfColormap, DfBitmap.Page, int, bool) key, out Texture2D value);
					return (key, value);
				}).ToArray();
			this.importedBmCache.Clear();
			Dictionary<Texture2D, Texture2D> textureMap = new();
			foreach (((DfPalette pal, DfColormap cmp, DfBitmap.Page page, int lightLevel, bool forceTransparent), Texture2D texture) in bms) {
				Texture2D newTexture = this.ImportBitmap(page, pal, cmp, lightLevel, forceTransparent);
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
			Dictionary<Sprite, Sprite> spriteMap = new();
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

		private readonly List<LoadWarning> warnings = new();
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