using MZZT.DarkForces.Converters;
using MZZT.DarkForces.FileFormats;
using MZZT.Drawing;
using MZZT.FileFormats;
using MZZT.FileFormats.Audio;
using MZZT.IO.FileProviders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX                                             
using System.Diagnostics;
#endif
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using static MZZT.DarkForces.FileFormats.DfBitmap;
using Debug = UnityEngine.Debug;

namespace MZZT.DarkForces.Showcase {
	public class ResourceDumper : Singleton<ResourceDumper> {
		[SerializeField]
		private TMP_Text nameLabel;

		[SerializeField]
		private GameObject background;

		[SerializeField]
		private DataboundResourceDumperSettings settings;

		public ResourceDumperSettings Settings => this.settings.Value;

		private async Task UpdateModTextAsync() {
			string text;
			if (!Mod.Instance.List.Any()) {
				text = "Dark Forces";
			} else {
				string path = FileLoader.Instance.ModGob;
				text = Path.GetFileName(path);
				if (path != null) {
					try {
						DfLevelList levels = await FileLoader.Instance.LoadGobFileAsync<DfLevelList>("JEDI.LVL");
						text = levels?.Levels.FirstOrDefault()?.DisplayName;
					} catch (Exception e) {
						Debug.LogError(e);
					}
					text ??= Path.GetFileName(path);
				} else {
					path = Mod.Instance.List.First().FilePath;
					text = Path.GetFileName(path);
				}
			}

			this.nameLabel.text = $"{text} Resource Dumper";
		}

		private async void Start() {
			if (!FileLoader.Instance.Gobs.Any()) {
				await FileLoader.Instance.LoadStandardFilesAsync();
			}

			await this.UpdateModTextAsync();

			this.background.SetActive(true);
		}

		public async void BrowseOutputAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = false,
				SelectButtonText = "Select",
				SelectedFileMustExist = false,
				SelectedPathMustExist = false,
				SelectFolder = true,
				StartPath = this.Settings.BaseOutputFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Select Output Folder"
			});
			if (path == null) {
				return;
			}

			this.Settings.BaseOutputFolder = path;
		}

		public static ResourceTypes GetFileType(string path) {
			string filename = Path.GetFileName(path).ToLower();
			string ext = Path.GetExtension(filename).ToLower();
			switch (ext) {
				case ".3do":
					return ResourceTypes.ThreeDo;
				case ".anm":
				case ".anim":
					return ResourceTypes.Anim;
				case ".bm":
					return ResourceTypes.Bm;
				case ".cmp":
					return ResourceTypes.Cmp;
				case ".dlt":
				case ".delt":
					return ResourceTypes.Delt;
				case ".flm":
				case ".film":
					return ResourceTypes.Film;
				case ".fme":
					return ResourceTypes.Fme;
				case ".fnt":
					return ResourceTypes.Fnt;
				case ".fon":
				case ".font":
					return ResourceTypes.Font;
				case ".gmd":
				case ".gmid":
					return ResourceTypes.Gmd;
				case ".gob":
					return ResourceTypes.OtherInGob;
				case ".gol":
					return ResourceTypes.Gol;
				case ".inf":
					return ResourceTypes.Inf;
				case ".lev":
					return ResourceTypes.Lev;
				case ".lfd":
					return ResourceTypes.OtherInLfd;
				case ".lst":
					if (filename == "briefing.lst") {
						return ResourceTypes.BriefingLst;
					}
					if (filename == "cutscene.lst") {
						return ResourceTypes.CutsceneLst;
					}
					break;
				case ".lvl":
					if (filename == "jedi.lvl") {
						return ResourceTypes.JediLvl;
					}
					break;
				case ".msg":
					return ResourceTypes.Msg;
				case ".o":
					return ResourceTypes.O;
				case ".pal":
					return ResourceTypes.Pal;
				case ".plt":
				case ".pltt":
					return ResourceTypes.Pltt;
				case ".txt":
					if (filename == "cutmuse.txt") {
						return ResourceTypes.CutmuseTxt;
					}
					break;
				case ".voc":
				case ".voic":
					return ResourceTypes.Voc;
				case ".vue":
					return ResourceTypes.Vue;
				case ".wax":
					return ResourceTypes.Wax;
			}
			return 0;
		}

		private string FillTemplate(string template, Dictionary<string, string> substitutions) {
			string ret = template;
			foreach ((string key, string value) in substitutions) {
				ret = ret.Replace($"{{{key}}}", value);
			}
			return ret;
		}

		private string ConvertExtension(string extension) {
			if (extension.ToLower() == "voic") {
				return "voc";
			}

			if (!this.Settings.PreferThreeCharacterExtensions) {
				return extension;
			}

			return extension.ToLower() switch {
				"anim" => "anm",
				"delt" => "dlt",
				"film" => "flm",
				"font" => "fon",
				"gmid" => "gmd",
				"pltt" => "plt",
				_ => extension
			};
		}

		private async Task<Resource> LoadFileAsync(string root, string filePath, Stream stream) {
			string fullPath;
			if (!string.IsNullOrEmpty(filePath)) {
				fullPath = Path.Combine(root, filePath);
			} else {
				fullPath = root;
			}

			Debug.Log(fullPath);

			string pathPart = filePath ?? Path.GetFileName(root);
			string filename = Path.GetFileName(pathPart);
			pathPart = pathPart[..^filename.Length].TrimEnd(Path.DirectorySeparatorChar);

			ResourceTypes type = GetFileType(filename);
			if (!this.Settings.ProcessTypes.HasFlag(type)) {
				if (type != ResourceTypes.Pltt && type != ResourceTypes.Pal && type != ResourceTypes.Cmp) {
					return null;
				}
			} else {
				if (this.Settings.OutputCopyOfInput) {
					Dictionary<string, string> outputParameters = new() {
						["output"] = this.Settings.BaseOutputFolder,
						["inputpath"] = pathPart
					};

					string outputPath = await this.FillOutputTemplateAsync(this.Settings.MiscFilenameFormat, new() {
						["inputname"] = Path.GetFileNameWithoutExtension(filename),
						["inputext"] = Path.GetExtension(filename)[1..],
						["outputext"] = this.ConvertExtension(Path.GetExtension(filename)[1..])
					}, outputParameters);

					using Stream output = await FileManager.Instance.NewFileStreamAsync(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
					await stream.CopyToAsync(output);

					stream.Seek(0, SeekOrigin.Begin);
				}
			}

			IDfFile resourceObject = null;
			switch (type) {
				case ResourceTypes.Anim:
					if (this.Settings.ConvertToPng.HasFlag(type)) {
						LandruAnimation anim = null;
						try {
							resourceObject = anim = await LandruAnimation.ReadAsync(stream);
						} catch (Exception ex) {
							ResourceCache.Instance.AddError(fullPath, ex);
						}
						ResourceCache.Instance.AddAnimation(Path.GetDirectoryName(fullPath), Path.GetFileName(fullPath), anim);
					}
					break;
				case ResourceTypes.Bm:
					if (this.Settings.ConvertToPng.HasFlag(type)) {
						DfBitmap bm = null;
						try {
							resourceObject = bm = await DfBitmap.ReadAsync(stream);
						} catch (Exception ex) {
							ResourceCache.Instance.AddError(fullPath, ex);
						}
						ResourceCache.Instance.AddBitmap(fullPath, bm);
					}
					break;
				case ResourceTypes.Cmp:
					if (this.Settings.ConvertToPng.HasFlag(type) || this.Settings.ConvertCmpToJascPal || this.Settings.ConvertCmpTo24BitPal || this.Settings.ConvertCmpTo32BitPal ||
						string.Compare(Path.GetFileNameWithoutExtension(fullPath), this.Settings.ImageConversionPal, true) == 0) {

						DfColormap cmp = null;
						try {
							resourceObject = cmp = await DfColormap.ReadAsync(stream);
						} catch (Exception ex) {
							ResourceCache.Instance.AddError(fullPath, ex);
						}
						ResourceCache.Instance.AddColormap(fullPath, cmp);
					}
					break;
				case ResourceTypes.Delt:
					if (this.Settings.ConvertToPng.HasFlag(type)) {
						LandruDelt delt = null;
						try {
							resourceObject = delt = await LandruDelt.ReadAsync(stream);
						} catch (Exception ex) {
							ResourceCache.Instance.AddError(fullPath, ex);
						}
						ResourceCache.Instance.AddDelt(Path.GetDirectoryName(fullPath), Path.GetFileName(fullPath), delt);
					}
					break;
				case ResourceTypes.Fme:
					if (this.Settings.ConvertToPng.HasFlag(type)) {
						DfFrame fme = null;
						try {
							resourceObject = fme = await DfFrame.ReadAsync(stream);
						} catch (Exception ex) {
							ResourceCache.Instance.AddError(fullPath, ex);
						}
						ResourceCache.Instance.AddFrame(fullPath, fme);
					}
					break;
				case ResourceTypes.Fnt:
					if (this.Settings.ConvertToPng.HasFlag(type) && (this.Settings.ConvertFntFontToSingleImage || this.Settings.ConvertFntFontToCharacterImages)) {
						DfFont fnt = null;
						try {
							resourceObject = fnt = await DfFont.ReadAsync(stream);
						} catch (Exception ex) {
							ResourceCache.Instance.AddError(fullPath, ex);
						}
						ResourceCache.Instance.AddFont(fullPath, fnt);
					}
					break;
				case ResourceTypes.Font:
					if (this.Settings.ConvertToPng.HasFlag(type) && (this.Settings.ConvertFntFontToSingleImage || this.Settings.ConvertFntFontToCharacterImages)) {
						LandruFont font = null;
						try {
							resourceObject = font = await LandruFont.ReadAsync(stream);
						} catch (Exception ex) {
							ResourceCache.Instance.AddError(fullPath, ex);
						}
						ResourceCache.Instance.AddFont(Path.GetDirectoryName(fullPath), Path.GetFileName(fullPath), font);
					}
					break;
				case ResourceTypes.Gmd:
					if (this.Settings.ConvertGmdToMid) {
						DfGeneralMidi gmd = null;
						try {
							resourceObject = gmd = await DfGeneralMidi.ReadAsync(stream);
						} catch (Exception ex) {
							ResourceCache.Instance.AddError(fullPath, ex);
						}
						ResourceCache.Instance.AddGeneralMidi(fullPath, gmd);
					}
					break;
				case ResourceTypes.Pal:
					if (this.Settings.ConvertToPng.HasFlag(type) || this.Settings.ConvertPalPlttToJascPal || this.Settings.ConvertPalPlttTo24BitPal || this.Settings.ConvertPalPlttTo32BitPal ||
						string.Compare(Path.GetFileNameWithoutExtension(fullPath), this.Settings.ImageConversionPal, true) == 0) {

						DfPalette pal = null;
						try {
							resourceObject = pal = await DfPalette.ReadAsync(stream);
						} catch (Exception ex) {
							ResourceCache.Instance.AddError(fullPath, ex);
						}
						ResourceCache.Instance.AddPalette(fullPath, pal);
					}
					break;
				case ResourceTypes.Pltt:
					/*if (this.Settings.ConvertPalPlttToJascPal || this.Settings.ConvertPalPlttTo24BitPal || this.Settings.ConvertPalPlttTo32BitPal ||
						this.Settings.ImageConversionPltt.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(x => string.Compare(Path.GetFileNameWithoutExtension(fullPath), x, true) == 0)) {*/

						LandruPalette pltt = null;
						try {
							resourceObject = pltt = await LandruPalette.ReadAsync(stream);
						} catch (Exception ex) {
							ResourceCache.Instance.AddError(fullPath, ex);
						}
						ResourceCache.Instance.AddPalette(Path.GetDirectoryName(fullPath), Path.GetFileName(fullPath), pltt);
					//}
					break;
				case ResourceTypes.Voc:
					if (this.Settings.ConvertVocToWav) {
						CreativeVoice voc = null;
						try {
							resourceObject = voc = await CreativeVoice.ReadAsync(stream);
						} catch (Exception ex) {
							ResourceCache.Instance.AddError(fullPath, ex);
						}
						ResourceCache.Instance.AddCreativeVoice(fullPath, voc);
					}
					break;
				case ResourceTypes.Wax:
					if (this.Settings.ConvertToPng.HasFlag(type)) {
						DfWax wax = null;
						try {
							resourceObject = wax = await DfWax.ReadAsync(stream);
						} catch (Exception ex) {
							ResourceCache.Instance.AddError(fullPath, ex);
						}
						ResourceCache.Instance.AddWax(fullPath, wax);
					}
					break;
			}

			if (resourceObject == null || (!this.Settings.ProcessTypes.HasFlag(type) && type != ResourceTypes.Pal && type != ResourceTypes.Cmp && type != ResourceTypes.Pltt)) {
				return null;
			}

			return new Resource(root, filePath, resourceObject);
		}

		private async Task<DfPalette> FindRelatedPalAsync(List<Resource> inputs, string preferred) {
			Resource paletteResource = inputs.FirstOrDefault(x => GetFileType(x.FullPath) == ResourceTypes.Pal && string.Compare(Path.GetFileNameWithoutExtension(x.FullPath), preferred, true) == 0);
			if (paletteResource != null) {
				return (DfPalette)paletteResource.ResourceObject;
			}

			return await ResourceCache.Instance.GetPaletteAsync($"{preferred}.PAL");
		}

		private async Task<DfColormap> FindRelatedCmpAsync(List<Resource> inputs, string preferred) {
			Resource paletteResource = inputs.FirstOrDefault(x => GetFileType(x.FullPath) == ResourceTypes.Cmp && string.Compare(Path.GetFileNameWithoutExtension(x.FullPath), preferred, true) == 0);
			if (paletteResource != null) {
				return (DfColormap)paletteResource.ResourceObject;
			}

			return await ResourceCache.Instance.GetColormapAsync($"{preferred}.CMP");
		}

		private Resource FindRelatedPltt(Resource resource, List<Resource> inputs, string[] preferred) {
			Resource[] matches = inputs.Where(x => x.FileParent == resource.FileParent && GetFileType(x.FullPath) == ResourceTypes.Pltt).ToArray();

			string filename = Path.GetFileNameWithoutExtension(resource.FullPath);
			Resource match = matches.FirstOrDefault(x => string.Compare(Path.GetFileNameWithoutExtension(x.FullPath), filename, true) == 0);
			if (match != null) {
				return match;
			}

			foreach (string pltt in preferred) {
				match = matches.FirstOrDefault(x => string.Compare(Path.GetFileNameWithoutExtension(x.FullPath), pltt, true) == 0);
				if (match != null) {
					return match;
				}
			}

			return matches.FirstOrDefault();
		}

		private async Task<string> FillOutputTemplateAsync(string fileTemplate, Dictionary<string, string> fileParameters, Dictionary<string, string> pathParameters) {
			string outputPath = this.FillTemplate(fileTemplate, fileParameters);
			pathParameters["file"] = outputPath;
			outputPath = this.FillTemplate(this.Settings.BaseOutputFormat, pathParameters);

			string folder = Path.GetDirectoryName(outputPath);
			if (!FileManager.Instance.FolderExists(folder)) {
				await FileManager.Instance.FolderCreateAsync(folder);
			}

			return outputPath;
		}

		private async Task SaveTextureAsPngAsync(Texture2D texture, string outputPath) {
			if (texture == null) {
				return;
			}

			byte[] buffer = texture.EncodeToPNG();
			using Stream output = await FileManager.Instance.NewFileStreamAsync(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
			await output.WriteAsync(buffer, 0, buffer.Length);
		}

		private async Task DumpFileAsync(List<Resource> inputs, Resource resource) {
			if (resource.ResourceObject == null) {
				return;
			}

			string origin = resource.SearchRoot;
			string pathPart = resource.SearchPathPart;
			pathPart ??= Path.GetFileName(origin);
			string filename = Path.GetFileName(resource.FullPath);
			pathPart = pathPart[..^filename.Length].TrimEnd(Path.DirectorySeparatorChar);

			Debug.Log(resource.FullPath);

			Dictionary<string, string> outputParameters = new() {
				["output"] = this.Settings.BaseOutputFolder,
				["inputpath"] = pathPart,
				["file"] = null
			};

			ResourceTypes type = GetFileType(filename);
			switch (type) {
				case ResourceTypes.Anim:
					if (this.Settings.ConvertToPng.HasFlag(type)) {
						LandruAnimation anim = (LandruAnimation)resource.ResourceObject;
						string[] pltts = this.Settings.ImageConversionPltt?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
						Resource pltt = this.FindRelatedPltt(resource, inputs, pltts);
						if (pltt != null) {
							Dictionary<string, string> parameters = new() {
								["inputname"] = Path.GetFileNameWithoutExtension(filename),
								["inputext"] = Path.GetExtension(filename)[1..],
								["palette"] = Path.GetFileNameWithoutExtension(pltt.FullPath),
								["lightlevel"] = "",
								["outputext"] = "png"
							};

							foreach ((LandruDelt delt, int i) in anim.Pages.Select((x, i) => (x, i))) {
								parameters["index"] = i.ToString();
								await this.SaveTextureAsPngAsync(
									ResourceCache.Instance.ImportDelt(delt, (LandruPalette)pltt.ResourceObject, true),
									await this.FillOutputTemplateAsync(this.Settings.ConvertedAnimFilenameFormat, parameters, outputParameters)
								);
							}
						}
					}
					break;
				case ResourceTypes.Bm:
					if (this.Settings.ConvertToPng.HasFlag(type)) {
						DfBitmap bm = (DfBitmap)resource.ResourceObject;
						string preferred = this.Settings.ImageConversionPal;
						DfPalette pal = await this.FindRelatedPalAsync(inputs, preferred);
						if (pal != null) {
							Dictionary<string, string> parameters = new() {
								["inputname"] = Path.GetFileNameWithoutExtension(filename),
								["inputext"] = Path.GetExtension(filename)[1..],
								["palette"] = preferred,
								["lightlevel"] = "",
								["outputext"] = "png"
							};

							DfColormap cmp = null;
							List<int> lightLevels = new();
							if (this.Settings.ImageConversionPaletteMode.HasFlag(ImageConversionPaletteModes.Cmp)) {
								cmp = await this.FindRelatedCmpAsync(inputs, preferred);
								if (cmp != null) {
									for (int i = this.Settings.ImageConversionLightLevelMinimum; i <= this.Settings.ImageConversionLightLevelMaximum; i++) {
										lightLevels.Add(i);
									}
								}
							}
							if (this.Settings.ImageConversionPaletteMode == ImageConversionPaletteModes.TypicalPalette || this.Settings.ImageConversionPaletteMode.HasFlag(ImageConversionPaletteModes.Pal)) {
								lightLevels.Add(-1);
							}

							foreach (int i in lightLevels) {
								parameters["lightlevel"] = i < 0 ? "" : i.ToString();

								foreach ((Page page, int index) in bm.Pages.Select((x, i) => (x, i))) {
									parameters["index"] = index.ToString();

									string outputPath = await this.FillOutputTemplateAsync(this.Settings.ConvertedBmFilenameFormat, parameters, outputParameters);
									Png png = page.ToPng(pal, i < 0 ? null : cmp, i, false, true);
									using Stream stream = await FileManager.Instance.NewFileStreamAsync(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
									png.Write(stream);
								}
							}
						}
					}
					break;
				case ResourceTypes.Cmp:
					if (this.Settings.ConvertCmpToJascPal || this.Settings.ConvertCmpTo24BitPal || this.Settings.ConvertCmpTo32BitPal ||
						this.Settings.ConvertToPng.HasFlag(ResourceTypes.Cmp)) {

						DfColormap cmp = (DfColormap)resource.ResourceObject;
						string name = Path.GetFileNameWithoutExtension(resource.FullPath);
						DfPalette pal = await this.FindRelatedPalAsync(inputs, name);
						if (pal != null) {
							Dictionary<string, string> parameters = new() {
								["inputname"] = Path.GetFileNameWithoutExtension(filename),
								["inputext"] = Path.GetExtension(filename)[1..]
							};

							if (this.Settings.ConvertToPng.HasFlag(ResourceTypes.Cmp)) {
								parameters["format"] = "PNG";
								parameters["outputext"] = "png";

								for (int i = this.Settings.ImageConversionLightLevelMinimum; i <= this.Settings.ImageConversionLightLevelMaximum; i++) {
									byte[] colors = ResourceCache.Instance.ImportColormap(pal, cmp, i, false);

									parameters["lightlevel"] = i.ToString();
									string outputPath = await this.FillOutputTemplateAsync(this.Settings.ConvertedPalPlttFilenameFormat, parameters, outputParameters);

									Png png = cmp.ToPng(pal, i);
									using Stream stream = await FileManager.Instance.NewFileStreamAsync(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
									png.Write(stream);
								}
							}
							if (this.Settings.ConvertCmpToJascPal) {
								parameters["format"] = "JASC";
								parameters["outputext"] = "pal";
								for (int i = this.Settings.ImageConversionLightLevelMinimum; i <= this.Settings.ImageConversionLightLevelMaximum; i++) {
									byte[] colors = ResourceCache.Instance.ImportColormap(pal, cmp, i, false);

									parameters["lightlevel"] = i.ToString();
									string outputPath = await this.FillOutputTemplateAsync(this.Settings.ConvertedPalPlttFilenameFormat, parameters, outputParameters);

									using Stream output = await FileManager.Instance.NewFileStreamAsync(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
									await cmp.WriteJascPalAsync(pal, i, output);
								}
							}
							if (this.Settings.ConvertCmpTo24BitPal) {
								parameters["format"] = "RGB";
								parameters["outputext"] = "pal";
								for (int i = this.Settings.ImageConversionLightLevelMinimum; i <= this.Settings.ImageConversionLightLevelMaximum; i++) {
									byte[] colors = ResourceCache.Instance.ImportColormap(pal, cmp, i, false);

									parameters["lightlevel"] = i.ToString();
									string outputPath = await this.FillOutputTemplateAsync(this.Settings.ConvertedPalPlttFilenameFormat, parameters, outputParameters);

									using Stream output = await FileManager.Instance.NewFileStreamAsync(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
									await cmp.WriteRgbPalAsync(pal, i, output);
								}
							}
							if (this.Settings.ConvertCmpTo32BitPal) {
								parameters["format"] = "RGBA";
								parameters["outputext"] = "pal";
								for (int i = this.Settings.ImageConversionLightLevelMinimum; i <= this.Settings.ImageConversionLightLevelMaximum; i++) {
									byte[] colors = ResourceCache.Instance.ImportColormap(pal, cmp, i, false);

									parameters["lightlevel"] = i.ToString();
									string outputPath = await this.FillOutputTemplateAsync(this.Settings.ConvertedPalPlttFilenameFormat, parameters, outputParameters);

									using Stream output = await FileManager.Instance.NewFileStreamAsync(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
									await cmp.WriteRgbaPalAsync(pal, i, output);
								}
							}
						}
					}
					break;
				case ResourceTypes.Delt:
					if (this.Settings.ConvertToPng.HasFlag(ResourceTypes.Delt)) {
						LandruDelt delt = (LandruDelt)resource.ResourceObject;
						string[] pltts = this.Settings.ImageConversionPltt?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
						Resource pltt = this.FindRelatedPltt(resource, inputs, pltts);
						if (pltt != null) {
							Dictionary<string, string> parameters = new() {
								["inputname"] = Path.GetFileNameWithoutExtension(filename),
								["inputext"] = Path.GetExtension(filename)[1..],
								["palette"] = Path.GetFileNameWithoutExtension(pltt.FullPath),
								["lightlevel"] = "",
								["outputext"] = "png"
							};

							await this.SaveTextureAsPngAsync(
								ResourceCache.Instance.ImportDelt(delt, (LandruPalette)pltt.ResourceObject, true),
								await this.FillOutputTemplateAsync(this.Settings.ConvertedImageFilenameFormat, parameters, outputParameters)
							);
						}
					}
					break;
				case ResourceTypes.Fme:
					if (this.Settings.ConvertToPng.HasFlag(ResourceTypes.Fme)) {
						DfFrame fme = (DfFrame)resource.ResourceObject;
						string preferred = this.Settings.ImageConversionPal;
						DfPalette pal = await this.FindRelatedPalAsync(inputs, preferred);
						if (pal != null) {
							Dictionary<string, string> parameters = new() {
								["inputname"] = Path.GetFileNameWithoutExtension(filename),
								["inputext"] = Path.GetExtension(filename)[1..],
								["palette"] = preferred,
								["lightlevel"] = "",
								["outputext"] = "png"
							};

							DfColormap cmp = null;
							List<int> lightLevels = new();
							if (this.Settings.ImageConversionPaletteMode.HasFlag(ImageConversionPaletteModes.Cmp)) {
								cmp = await this.FindRelatedCmpAsync(inputs, preferred);
								if (cmp != null) {
									for (int i = this.Settings.ImageConversionLightLevelMinimum; i <= this.Settings.ImageConversionLightLevelMaximum; i++) {
										lightLevels.Add(i);
									}
								}
							}
							if (this.Settings.ImageConversionPaletteMode == ImageConversionPaletteModes.TypicalPalette || this.Settings.ImageConversionPaletteMode.HasFlag(ImageConversionPaletteModes.Pal)) {
								lightLevels.Add(-1);
							}

							foreach (int i in lightLevels) {
								parameters["lightlevel"] =  i < 0 ? "" : i.ToString();

								string outputPath = await this.FillOutputTemplateAsync(this.Settings.ConvertedImageFilenameFormat, parameters, outputParameters);
								Png png = fme.ToPng(pal, i < 0 ? null : cmp, i, false);
								using Stream stream = await FileManager.Instance.NewFileStreamAsync(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
								png.Write(stream);
							}
						}
					}
					break;
				case ResourceTypes.Fnt:
					if (this.Settings.ConvertToPng.HasFlag(ResourceTypes.Fnt)) {
						DfFont fnt = (DfFont)resource.ResourceObject;
						string preferred = this.Settings.ImageConversionPal;
						DfPalette pal = await this.FindRelatedPalAsync(inputs, preferred);
						if (pal != null) {
							Dictionary<string, string> parameters = new() {
								["inputname"] = Path.GetFileNameWithoutExtension(filename),
								["inputext"] = Path.GetExtension(filename)[1..],
								["palette"] = preferred,
								["lightlevel"] = "",
								["character"] = "",
								["outputext"] = "png"
							};

							List<(string lightLevel, byte[] colors)> palettes = new();
							if (this.Settings.ImageConversionPaletteMode.HasFlag(ImageConversionPaletteModes.Cmp)) {
								DfColormap cmp = await this.FindRelatedCmpAsync(inputs, preferred);
								if (cmp != null) {
									for (int i = this.Settings.ImageConversionLightLevelMinimum; i <= this.Settings.ImageConversionLightLevelMaximum; i++) {
										byte[] colors = ResourceCache.Instance.ImportColormap(pal, cmp, i, true);
										palettes.Add((i.ToString(), colors));
									}
								}
							}
							if (this.Settings.ImageConversionPaletteMode == ImageConversionPaletteModes.TypicalPalette || this.Settings.ImageConversionPaletteMode.HasFlag(ImageConversionPaletteModes.Pal)) {
								byte[] colors = ResourceCache.Instance.ImportPalette(pal, true);
								palettes.Add(("", colors));
							}

							foreach ((string lightLevel, byte[] colors) in palettes) {
								parameters["lightlevel"] = lightLevel;

								if (this.Settings.ConvertFntFontToSingleImage) {
									parameters["character"] = "";

									string outputPath = await this.FillOutputTemplateAsync(this.Settings.ConvertedImageFilenameFormat, parameters, outputParameters);
									Png png = fnt.ToPng(colors);
									using Stream stream = await FileManager.Instance.NewFileStreamAsync(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
									png.Write(stream);
								}
								if (this.Settings.ConvertFntFontToCharacterImages) {
									foreach ((DfFont.Character c, int i) in fnt.Characters.Select((x, i) => (x, i + fnt.First))) {
										parameters["character"] = i.ToString();

										string outputPath = await this.FillOutputTemplateAsync(this.Settings.ConvertedFntFontFilenameFormat, parameters, outputParameters);
										Png png = c.ToPng(fnt.Height, colors);
										using Stream stream = await FileManager.Instance.NewFileStreamAsync(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
										png.Write(stream);
									}
								}
							}
						}
					}
					break;
				case ResourceTypes.Font:
					if (this.Settings.ConvertToPng.HasFlag(ResourceTypes.Font)) {
						LandruFont font = (LandruFont)resource.ResourceObject;
						Dictionary<string, string> parameters = new() {
							["inputname"] = Path.GetFileNameWithoutExtension(filename),
							["inputext"] = Path.GetExtension(filename)[1..],
							["palette"] = $"#{Math.Clamp(Mathf.FloorToInt(this.Settings.FontColor.r * 256), 0, 255):X2}{Math.Clamp(Mathf.FloorToInt(this.Settings.FontColor.g * 256), 0, 255):X2}{Math.Clamp(Mathf.FloorToInt(this.Settings.FontColor.b * 256), 0, 255):X2}{Math.Clamp(Mathf.FloorToInt(this.Settings.FontColor.a * 256), 0, 255):X2}",
							["lightlevel"] = "",
							["character"] = "",
							["outputext"] = "png"
						};

						if (this.Settings.ConvertFntFontToSingleImage) {
							parameters["character"] = "";

							await this.SaveTextureAsPngAsync(
								font.ToTexture(this.Settings.FontColor, true),
								await this.FillOutputTemplateAsync(this.Settings.ConvertedImageFilenameFormat, parameters, outputParameters)
							);
						}
						if (this.Settings.ConvertFntFontToCharacterImages) {
							foreach ((LandruFont.Character c, int i) in font.Characters.Select((x, i) => (x, i + font.First))) {
								parameters["character"] = i.ToString();

								await this.SaveTextureAsPngAsync(
									c.ToTexture(font, this.Settings.FontColor, true),
									await this.FillOutputTemplateAsync(this.Settings.ConvertedFntFontFilenameFormat, parameters, outputParameters)
								);
							}
						}
					}
					break;
				case ResourceTypes.Gmd:
					if (this.Settings.ConvertGmdToMid) {
						DfGeneralMidi gmd = (DfGeneralMidi)resource.ResourceObject;
						Midi midi = gmd.ToMidi();

						Dictionary<string, string> parameters = new() {
							["inputname"] = Path.GetFileNameWithoutExtension(filename),
							["inputext"] = Path.GetExtension(filename)[1..],
							["outputext"] = "mid"
						};

						string outputPath = await this.FillOutputTemplateAsync(this.Settings.MiscFilenameFormat, parameters, outputParameters);

						using Stream output = await FileManager.Instance.NewFileStreamAsync(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
						midi.Chunks.Clear();
						await midi.SaveAsync(output);
					}
					break;
				case ResourceTypes.Pal:
					if (this.Settings.ConvertPalPlttToJascPal || this.Settings.ConvertPalPlttTo24BitPal || this.Settings.ConvertPalPlttTo32BitPal ||
						this.Settings.ConvertToPng.HasFlag(ResourceTypes.Pal)) {

						DfPalette pal = (DfPalette)resource.ResourceObject;
						byte[] palette = ResourceCache.Instance.ImportPalette(pal);
						Dictionary<string, string> parameters = new() {
							["inputname"] = Path.GetFileNameWithoutExtension(filename),
							["inputext"] = Path.GetExtension(filename)[1..],
							["lightlevel"] = ""
						};
						if (this.Settings.ConvertToPng.HasFlag(ResourceTypes.Pal)) {
							parameters["format"] = "PNG";
							parameters["outputext"] = "png";

							string outputPath = await this.FillOutputTemplateAsync(this.Settings.ConvertedPalPlttFilenameFormat, parameters, outputParameters);

							Png png = pal.ToPng();
							using Stream stream = await FileManager.Instance.NewFileStreamAsync(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
							png.Write(stream);
						}
						if (this.Settings.ConvertPalPlttToJascPal) {
							parameters["format"] = "JASC";
							parameters["outputext"] = "pal";

							string outputPath = await this.FillOutputTemplateAsync(this.Settings.ConvertedPalPlttFilenameFormat, parameters, outputParameters);

							using Stream output = await FileManager.Instance.NewFileStreamAsync(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
							await pal.WriteJascPalAsync(output);
						}
						if (this.Settings.ConvertPalPlttTo24BitPal) {
							parameters["format"] = "RGB";
							parameters["outputext"] = "pal";

							string outputPath = await this.FillOutputTemplateAsync(this.Settings.ConvertedPalPlttFilenameFormat, parameters, outputParameters);

							using Stream output = await FileManager.Instance.NewFileStreamAsync(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
							await pal.WriteRgbPalAsync(output);
						}
						if (this.Settings.ConvertCmpTo32BitPal) {
							parameters["format"] = "RGBA";
							parameters["outputext"] = "pal";

							string outputPath = await this.FillOutputTemplateAsync(this.Settings.ConvertedPalPlttFilenameFormat, parameters, outputParameters);

							using Stream output = await FileManager.Instance.NewFileStreamAsync(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
							await pal.WriteRgbaPalAsync(output);
						}
					}
					break;
				case ResourceTypes.Pltt:
					if (this.Settings.ConvertPalPlttToJascPal || this.Settings.ConvertPalPlttTo24BitPal || this.Settings.ConvertPalPlttTo32BitPal ||
						this.Settings.ConvertToPng.HasFlag(ResourceTypes.Pltt)) {

						LandruPalette pltt = (LandruPalette)resource.ResourceObject;
						byte[] palette = ResourceCache.Instance.ImportPalette(pltt);
						Dictionary<string, string> parameters = new() {
							["inputname"] = Path.GetFileNameWithoutExtension(filename),
							["inputext"] = Path.GetExtension(filename)[1..],
							["lightlevel"] = ""
						};
						if (this.Settings.ConvertToPng.HasFlag(ResourceTypes.Pltt)) {
							parameters["format"] = "PNG";
							parameters["outputext"] = "png";

							string outputPath = await this.FillOutputTemplateAsync(this.Settings.ConvertedPalPlttFilenameFormat, parameters, outputParameters);

							Png png = pltt.ToPng();
							using Stream stream = await FileManager.Instance.NewFileStreamAsync(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
							png.Write(stream);
						};
						if (this.Settings.ConvertPalPlttToJascPal) {
							parameters["format"] = "JASC";
							parameters["outputext"] = "pal";

							string outputPath = await this.FillOutputTemplateAsync(this.Settings.ConvertedPalPlttFilenameFormat, parameters, outputParameters);

							using Stream output = await FileManager.Instance.NewFileStreamAsync(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
							await pltt.WriteJascPalAsync(output);
						}
						if (this.Settings.ConvertPalPlttTo24BitPal) {
							parameters["format"] = "RGB";
							parameters["outputext"] = "pal";

							string outputPath = await this.FillOutputTemplateAsync(this.Settings.ConvertedPalPlttFilenameFormat, parameters, outputParameters);

							using Stream output = await FileManager.Instance.NewFileStreamAsync(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
							await pltt.WriteRgbPalAsync(output);
						}
						if (this.Settings.ConvertCmpTo32BitPal) {
							parameters["format"] = "RGBA";
							parameters["outputext"] = "pal";

							string outputPath = await this.FillOutputTemplateAsync(this.Settings.ConvertedPalPlttFilenameFormat, parameters, outputParameters);

							using Stream output = await FileManager.Instance.NewFileStreamAsync(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
							await pltt.WriteRgbaPalAsync(output);
						}
					}
					break;
				case ResourceTypes.Voc:
					if (this.Settings.ConvertVocToWav) {
						CreativeVoice voc = (CreativeVoice)resource.ResourceObject;
						Dictionary<string, string> parameters = new() {
							["inputname"] = Path.GetFileNameWithoutExtension(filename),
							["inputext"] = Path.GetExtension(filename)[1..],
							["index"] = "",
							["outputext"] = "wav"
						};

						Wave[] waves = voc.ToWaves().ToArray();
						foreach ((Wave wave, int i) in waves.Select((x, i) => (x, i))) {
							if (waves.Length > 1) {
								parameters["index"] = i.ToString();
							}

							string outputPath = await this.FillOutputTemplateAsync(this.Settings.ConvertedVocFilenameFormat, parameters, outputParameters);

							using Stream output = await FileManager.Instance.NewFileStreamAsync(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
							await wave.SaveAsync(output);
						}
					}
					break;
				case ResourceTypes.Wax:
					if (this.Settings.ProcessTypes.HasFlag(ResourceTypes.Wax)) {
						DfWax wax = (DfWax)resource.ResourceObject;
						string preferred = this.Settings.ImageConversionPal;
						DfPalette pal = await this.FindRelatedPalAsync(inputs, preferred);
						if (pal != null) {
							Dictionary<string, string> parameters = new() {
								["inputname"] = Path.GetFileNameWithoutExtension(filename),
								["inputext"] = Path.GetExtension(filename)[1..],
								["palette"] = preferred,
								["lightlevel"] = "",
								["outputext"] = "png"
							};

							DfColormap cmp = null;
							List<int> lightLevels = new();
							if (this.Settings.ImageConversionPaletteMode.HasFlag(ImageConversionPaletteModes.Cmp)) {
								cmp = await this.FindRelatedCmpAsync(inputs, preferred);
								if (cmp != null) {
									for (int i = this.Settings.ImageConversionLightLevelMinimum; i <= this.Settings.ImageConversionLightLevelMaximum; i++) {
										lightLevels.Add(i);
									}
								}
							}
							if (this.Settings.ImageConversionPaletteMode == ImageConversionPaletteModes.TypicalPalette || this.Settings.ImageConversionPaletteMode.HasFlag(ImageConversionPaletteModes.Pal)) {
								lightLevels.Add(-1);
							}

							foreach (int lightLevel in lightLevels) {
								parameters["lightlevel"] = lightLevel < 0 ? "" : lightLevel.ToString();

								Dictionary<byte[], string> uniqueMap = new();
								foreach ((DfWax.SubWax sub, int i) in wax.Waxes.Select((x, i) => (x, i))) {
									parameters["wax"] = i.ToString();

									foreach ((DfWax.Sequence sequence, int j) in sub.Sequences.Select((x, i) => (x, i))) {
										parameters["sequence"] = j.ToString();

										foreach ((DfFrame fme, int k) in sequence.Frames.Select((x, i) => (x, i))) {
											parameters["frame"] = k.ToString();

											string outputPath;
											if (uniqueMap.TryGetValue(fme.Pixels, out string existing)) {
												switch (this.Settings.WaxOutputMode) {
													case WaxOutputModes.NoDuplicates:
														continue;
													case WaxOutputModes.Shortcut:
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
														parameters["outputext"] = "lnk";

														outputPath = await this.FillOutputTemplateAsync(this.Settings.ConvertedWaxFilenameFormat, parameters, outputParameters);

														IShellLink link = (IShellLink)new ShellLink();
														link.SetPath(existing);

														IPersistFile file = (IPersistFile)link;
														file.Save(outputPath, false);
#else
														parameters["outputext"] = "desktop";

														outputPath = await this.FillOutputTemplateAsync(this.Settings.ConvertedWaxFilenameFormat, parameters, outputParameters);

														using (Stream stream = await FileManager.Instance.NewFileStreamAsync(outputPath, FileMode.Create, FileAccess.Write, FileShare.None)) {
															using StreamWriter writer = new(stream, Encoding.UTF8);
															await writer.WriteLineAsync("[Desktop Entry]");
															await writer.WriteLineAsync("Encoding=UTF-8");
															await writer.WriteLineAsync("Version=1.0");
															await writer.WriteLineAsync("Type=Link");
															await writer.WriteLineAsync("Terminal=false");
															await writer.WriteLineAsync($"Exec={existing}");
															await writer.WriteLineAsync($"Name={Path.GetFileNameWithoutExtension(outputPath)}");
															await writer.WriteLineAsync($"Icon={existing}");
														}
#endif
														continue;
													case WaxOutputModes.SymLink:
														parameters["outputext"] = "png";

														outputPath = await this.FillOutputTemplateAsync(this.Settings.ConvertedWaxFilenameFormat, parameters, outputParameters);

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
														if (!CreateSymbolicLink(outputPath, existing, SYMBOLIC_LINK_FLAG.FILE | SYMBOLIC_LINK_FLAG.ALLOW_UNPRIVILEGED_CREATE)) {
															int error = Marshal.GetLastWin32Error();
															ResourceCache.Instance.AddError(outputPath, new Win32Exception(error));
														}
#elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
														Process.Start("ln", $"-s \"{existing}\" \"{outputPath}\"");
#endif
														continue;
													case WaxOutputModes.HardLink:
														parameters["outputext"] = "png";

														outputPath = await this.FillOutputTemplateAsync(this.Settings.ConvertedWaxFilenameFormat, parameters, outputParameters);

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
														if (!CreateHardLink(outputPath, existing)) {
															int error = Marshal.GetLastWin32Error();
															ResourceCache.Instance.AddError(outputPath, new Win32Exception(error));
														}
#elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
														Process.Start("ln", $"\"{existing}\" \"{outputPath}\"");
#endif

														continue;
												}
											}

											parameters["outputext"] = "png";

											outputPath = await this.FillOutputTemplateAsync(this.Settings.ConvertedWaxFilenameFormat, parameters, outputParameters);
											uniqueMap[fme.Pixels] = outputPath;

											await this.SaveTextureAsPngAsync(
												ResourceCache.Instance.ImportFrame(pal, lightLevel < 0 ? null : cmp, fme, lightLevel, true).texture,
												outputPath
											);
										}
									}
								}
							}
						}
					}
					break;
			}
		}

		public async void DumpAsync() {
			this.settings.SaveToPlayerPrefs();

			if (string.IsNullOrWhiteSpace(this.Settings.BaseOutputFolder) || FileManager.Instance.FileExists(this.Settings.BaseOutputFolder)) {
				await DfMessageBox.Instance.ShowAsync("Base Output Folder is set to an invalid location. Please specify a folder.");
				return;
			}
			if (!FileManager.Instance.FolderExists(this.Settings.BaseOutputFolder)) {
				try {
					await FileManager.Instance.FolderCreateAsync(this.Settings.BaseOutputFolder);
				} catch (Exception) {
					await DfMessageBox.Instance.ShowAsync("Could not create base output folder. Please verify the location you specified is accurate.");
					return;
				}
			}

#if UNITY_WEBGL
			if (this.Settings.ProcessTypes.HasFlag(ResourceTypes.Wax) && (this.Settings.WaxOutputMode == WaxOutputModes.HardLink ||
				this.Settings.WaxOutputMode == WaxOutputModes.SymLink)) {

				await DfMessageBox.Instance.ShowAsync("HardLink or SymLink WAX output modes can't be used in WebAssembly.");
				return;
			}
#endif

			await PauseMenu.Instance.BeginLoadingAsync();

			ResourceCache.Instance.ClearWarnings();
			ResourceCache.Instance.Clear();
			ResourceCache.Instance.BypassCmpDithering = false;
			ResourceCache.Instance.FullBright = false;

			List<Resource> inputs = new();
			foreach (string path in this.Settings.Inputs) {
				if (FileManager.Instance.FileExists(path)) {
					Resource resource;
					switch (GetFileType(path)) {
						case ResourceTypes.OtherInGob:
							using (Stream gobStream = await FileManager.Instance.NewFileStreamAsync(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
								DfGobContainer gob = await DfGobContainer.TryReadAsync(gobStream, false);

								foreach (string file in gob.Files.Select(x => x.name)) {
									ResourceTypes type = GetFileType(file);
									if (type == 0) {
										type = ResourceTypes.OtherInGob;
									}
									if (this.Settings.ProcessTypes.HasFlag(type) || type == ResourceTypes.Pltt || type == ResourceTypes.Pal || type == ResourceTypes.Cmp) {
										resource = null;
										using (Stream stream = await gob.GetFileStreamAsync(file, gobStream)) {
											resource = await this.LoadFileAsync(path, file, stream);
										}
										if (resource != null) {
											inputs.Add(resource);
										}
									}
								}
							}
							break;
						case ResourceTypes.OtherInLfd:
							using (Stream lfdStream = await FileManager.Instance.NewFileStreamAsync(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
								await LandruFileDirectory.TryReadAsync(lfdStream, async lfd => {
									foreach ((string fileName, string fileType, uint offset, uint size) in lfd.Files) {
										ResourceTypes type = GetFileType($".{fileType}");
										if (type == 0) {
											type = ResourceTypes.OtherInLfd;
										}
										if (this.Settings.ProcessTypes.HasFlag(type) || type == ResourceTypes.Pltt || type == ResourceTypes.Pal || type == ResourceTypes.Cmp) {
											resource = null;
											using (Stream stream = await lfd.GetFileStreamAsync(fileName, fileType)) {
												resource = await this.LoadFileAsync(path, $"{fileName}.{fileType}", stream);
											}
											if (resource != null) {
												inputs.Add(resource);
											}
										}
									}
								});
							}
							break;
						case 0:
							ResourceCache.Instance.AddWarning(path, "Unknown file extension.");
							break;
						default:
							resource = null;
							using (Stream stream = await FileManager.Instance.NewFileStreamAsync(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
								resource = await this.LoadFileAsync(path, null, stream);
							}
							if (resource != null) {
								inputs.Add(resource);
							}
							break;
					}
				} else {
					if (FileManager.Instance.FolderExists(path)) {
						await foreach (string child in FileManager.Instance.FolderEnumerateFilesAsync(path, "*", SearchOption.AllDirectories)) {
							string childPart = child[path.Length..].Trim(Path.DirectorySeparatorChar);
							ResourceTypes type = GetFileType(child);
							if (this.Settings.AlwaysScanInsideGobs && type == ResourceTypes.OtherInGob) {
								using Stream gobStream = await FileManager.Instance.NewFileStreamAsync(child, FileMode.Open, FileAccess.Read, FileShare.Read);
								DfGobContainer gob = await DfGobContainer.TryReadAsync(gobStream, false);

								foreach (string file in gob.Files.Select(x => x.name)) {
									type = GetFileType(file);
									if (type == 0) {
										type = ResourceTypes.OtherInGob;
									}
									if (this.Settings.ProcessTypes.HasFlag(type) || type == ResourceTypes.Pltt || type == ResourceTypes.Pal || type == ResourceTypes.Cmp) {
										Resource resource = null;
										using (Stream stream = await gob.GetFileStreamAsync(file, gobStream)) {
											resource = await this.LoadFileAsync(path, Path.Combine(childPart, file), stream);
										}
										if (resource != null) {
											inputs.Add(resource);
										}
									}
								}
							} else if (this.Settings.AlwaysScanInsideLfds && type == ResourceTypes.OtherInLfd) {
								using Stream lfdStream = await FileManager.Instance.NewFileStreamAsync(child, FileMode.Open, FileAccess.Read, FileShare.Read);
								await LandruFileDirectory.TryReadAsync(lfdStream, async lfd => {
									foreach ((string fileName, string fileType, uint offset, uint size) in lfd.Files) {
										type = GetFileType($".{fileType}");
										if (type == 0) {
											type = ResourceTypes.OtherInLfd;
										}
										if (this.Settings.ProcessTypes.HasFlag(type) || type == ResourceTypes.Pltt || type == ResourceTypes.Pal || type == ResourceTypes.Cmp) {
											Resource resource = null;
											using (Stream stream = await lfd.GetFileStreamAsync(fileName, fileType)) {
												resource = await this.LoadFileAsync(path, Path.Combine(childPart, $"{fileName}.{fileType}"), stream);
											}
											if (resource != null) {
												inputs.Add(resource);
											}
										}
									}
								});
							} else if (this.Settings.ProcessTypes.HasFlag(type) || type == ResourceTypes.Pltt || type == ResourceTypes.Pal || type == ResourceTypes.Cmp) {
								Resource resource = null;
								using (Stream stream = await FileManager.Instance.NewFileStreamAsync(child, FileMode.Open, FileAccess.Read, FileShare.Read)) {
									resource = await this.LoadFileAsync(path, childPart, stream);
								}
								if (resource != null) {
									inputs.Add(resource);
								}
							}
						}
					} else {
						string parent = Path.GetDirectoryName(path);
						if (FileManager.Instance.FileExists(parent)) {
							string child = Path.GetFileName(path).ToLower();
							switch (GetFileType(parent)) {
								case ResourceTypes.OtherInGob:
									using (Stream gobStream = await FileManager.Instance.NewFileStreamAsync(parent, FileMode.Open, FileAccess.Read, FileShare.Read)) {
										DfGobContainer gob = await DfGobContainer.TryReadAsync(gobStream, false);

										string name = gob.Files.FirstOrDefault(x => x.name.ToLower() == child).name;
										if (name != null) {
											Resource resource = null;
											using (Stream stream = await gob.GetFileStreamAsync(name, gobStream)) {
												resource = await this.LoadFileAsync(path, name, stream);
											}
											if (resource != null) {
												inputs.Add(resource);
											}
										} else {
											ResourceCache.Instance.AddWarning(path, "Can't find file in GOB.");
										}
									}
									break;
								case ResourceTypes.OtherInLfd:
									using (Stream lfdStream = await FileManager.Instance.NewFileStreamAsync(parent, FileMode.Open, FileAccess.Read, FileShare.Read)) {
										await LandruFileDirectory.TryReadAsync(lfdStream, async lfd => {
											(string name, string lfdType, uint _, uint _) = lfd.Files.FirstOrDefault(x => $"{x.name}.{x.type}".ToLower() == child);
											if (name != null) {
												Resource resource = null;
												using (Stream stream = await lfd.GetFileStreamAsync(name, lfdType)) {
													resource = await this.LoadFileAsync(path, $"{name}.{lfdType}", stream);
												}
												if (resource != null) {
													inputs.Add(resource);
												}
											} else {
												ResourceCache.Instance.AddWarning(path, "Can't find file in LFD.");
											}
										});
									}
									break;
								default:
									ResourceCache.Instance.AddWarning(path, "Can't find container of file.");
									break;
							}
						} else {
							ResourceCache.Instance.AddWarning(path, "Can't find file.");
						}
					}
				}
			}

			foreach (Resource resource in inputs) {
				await this.DumpFileAsync(inputs, resource);
			}

			ResourceCache.Instance.Clear();

			ResourceCache.LoadWarning[] warnings = ResourceCache.Instance.Warnings.ToArray();
			if (warnings.Length > 0) {
				string fatal = string.Join("\n", warnings
					.Where(x => x.Fatal)
					.Select(x => $"{x.FileName}{(x.Line > 0 ? $":{x.Line}" : "")} - {x.Message}"));
				string warning = string.Join("\n", warnings
					.Where(x => !x.Fatal)
					.Select(x => $"{x.FileName}{(x.Line > 0 ? $":{x.Line}" : "")} - {x.Message}"));
				if (fatal.Length > 0) {
					if (warning.Length > 0) {
						await DfMessageBox.Instance.ShowAsync($"Problems while dumping resources:\n\n{fatal}\n{warning}");
					} else {
						await DfMessageBox.Instance.ShowAsync($"Problems while dumping resources:\n\n{fatal}");
					}
				} else {
					await DfMessageBox.Instance.ShowAsync($"Problems while dumping resources:\n\n{warning}");
				}
				ResourceCache.Instance.ClearWarnings();
			}

			PauseMenu.Instance.EndLoading();
			try {
				FileManager.Instance.Show(this.Settings.BaseOutputFolder);
			} catch (Exception e) {
				Debug.LogException(e);
			}
		}

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
		[ComImport]
		[Guid("00021401-0000-0000-C000-000000000046")]
		private class ShellLink {
		}

		[ComImport]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		[Guid("000214F9-0000-0000-C000-000000000046")]
		private interface IShellLink {
			void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
			void GetIDList(out IntPtr ppidl);
			void SetIDList(IntPtr pidl);
			void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
			void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
			void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
			void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
			void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
			void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
			void GetHotkey(out short pwHotkey);
			void SetHotkey(short wHotkey);
			void GetShowCmd(out int piShowCmd);
			void SetShowCmd(int iShowCmd);
			void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
			void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
			void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
			void Resolve(IntPtr hwnd, int fFlags);
			void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
		}

		[DllImport("kernel32", SetLastError = true, CharSet = CharSet.Auto)]
		private static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SYMBOLIC_LINK_FLAG dwFlags);

		[Flags]
		private enum SYMBOLIC_LINK_FLAG : int {
			FILE = 0x0,
			DIRECTORY = 0x1,
			ALLOW_UNPRIVILEGED_CREATE = 0x2
		}

		[DllImport("kernel32", SetLastError = true, CharSet = CharSet.Auto)]
		private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes = default);
#endif

		private record Resource {
			public Resource(string searchRoot, string searchPathPart, IDfFile resourceObject) {
				this.SearchRoot = searchRoot;
				this.SearchPathPart = searchPathPart;
				this.ResourceObject = resourceObject;

				if (string.IsNullOrEmpty(this.SearchPathPart)) {
					this.FullPath = this.SearchRoot;
				} else {
					this.FullPath = Path.Combine(this.SearchRoot, this.SearchPathPart);
				}

				this.FileParent = Path.GetDirectoryName(this.FullPath);
			}

			public string SearchRoot { get; }
			public string SearchPathPart { get; }
			public string FullPath { get; }
			public string FileParent { get; }
			public IDfFile ResourceObject { get; }
		}
	}
}
