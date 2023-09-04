using MZZT.DarkForces.FileFormats;
using MZZT.Input;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
	[ProgramHelpInfo(
		ProgramDescription = "A collection of tools and showcases for the MZZT.DarkForces.FileFormats .NET library.",
		HelpFooter = "This tool is provided as-is under the MIT license. Proper usage is the responsibility of the user.",
		ShowHelpOnSyntaxError = false
	)]
	public class Menu : Singleton<Menu> {
		[SerializeField, Header("References")]
		private Image background = null;
		[SerializeField]
		private GameObject menu = null;
		[SerializeField]
		private TMP_Text darkForcesFolderText = null;
		[SerializeField]
		private TMP_Text modText = null;
		[SerializeField]
		private ShowcaseList showcaseList = null;
		[SerializeField]
		private TMP_Text description = null;
		[SerializeField]
		private GameObject actionsBar = null;

		private static bool init;
		private void ParseCommandLine() {
			if (init) {
				return;
			}
			init = true;

			ProgramArguments.AppName = "\"Dark Forces Showcase\"";
			ProgramArguments.OverrideConsoleWidth = 1000;
			ProgramArguments.HelpDescriptionIndent = 36;
#if !UNITY_EDITOR
			if (!ProgramArguments.Inject(this)) {
				if (string.IsNullOrEmpty(ProgramArguments.ParseError)) {
					this.showHelp = ProgramArguments.GetHelp<Menu>();
				} else {
					this.showHelp = ProgramArguments.ParseError + Environment.NewLine + ProgramArguments.GetHelp<Menu>();
				}
			}
#endif

			if (!string.IsNullOrEmpty(this.showHelp)) {
				this.CommandLineDarkForcesFolder = null;
				this.CommandLineTool = null;
				this.CommandLineModFiles = null;
			}
		}

		private string showHelp;

		[ProgramSwitch("darkDir", 'd',
			HelpOrder = 0,
			ValueName = "PATH",
			PrependedGroupName = "Data files:",
			HelpDescription = "Path to the Dark Forces folder. Should contain all GOB files and an LFD folder with all LFD files."
		)]
		public string CommandLineDarkForcesFolder { get; set; }

		[ProgramArgument(
			ValueName = "MODFILE1] [MODFILE2] [etc",
			HelpOrder = 10,
			HelpDescription = @"Override Dark Forces game files with mod files. For LFD files, specify as DFBRIEF.LFD=Path\To\Replacement.LFD. Other files should just specify a path."
		)]
		public string[] CommandLineModFiles { get; set; }

		[ProgramSwitch("tool", 't',
			HelpOrder = 100,
			ValueName = "TOOLNAME",
			PrependedGroupName = "Tools:",
			HelpDescription = "Jump to a specific tool on startup. Values: LevelExplorer, MapGenerator, Randomizer, ResourceDumper"
		)]
		public string CommandLineTool { get; set; }

		[ProgramSwitch("help", '?',
			HelpOrder = 9001,
			PrependedGroupName = "Misc:",
			HelpDescription = "Display this help screen.",
			IgnoreOtherArgs = true
		)]
		public ParseResult ShowHelp() {
			this.showHelp = ProgramArguments.GetHelp<Menu>();
			return new ParseResult() {
				SkipNormalErrorHandling = true
			};
		}

		[ProgramValidateArguments]
		public ParseResult ValidateArguments() {
			if (this.CommandLineDarkForcesFolder != null && !File.Exists(Path.Combine(this.CommandLineDarkForcesFolder, "DARK.GOB"))) {
				this.CommandLineDarkForcesFolder = null;
				return new ParseResult() {
					Error = "Invalid Dark Forces folder."
				};
			}
			if (this.CommandLineTool != null) {
				string[] scenes = Enumerable.Range(0, SceneManager.sceneCountInBuildSettings).Select(x => SceneUtility.GetScenePathByBuildIndex(x)).ToArray();
				string[] names = scenes.Select(x => Path.GetFileNameWithoutExtension(x)).ToArray();
				if (!names.Contains(this.CommandLineTool)) {
					this.CommandLineTool = null;
					return new ParseResult() {
						Error = "Invalid tool name."
					};
				}
			}
			if (this.CommandLineModFiles != null) {
				foreach (string entry in this.CommandLineModFiles) {
					string[] keyValue = entry.Split('=');
					bool isLfd = Path.GetExtension(keyValue[0]).ToUpper() == ".LFD";
					if (keyValue.Length != (isLfd ? 2 : 1)) {
						return new ParseResult() {
							Error = $"Invalid mod file entry {entry}."
						};
					}
					if (isLfd) {
						if (!File.Exists(keyValue[1])) {
							return new ParseResult() {
								Error = $"File not found {keyValue[1]}."
							};
						}
					} else if (!File.Exists(keyValue[0])) {
						return new ParseResult() {
							Error = $"File not found {keyValue[0]}."
						};
					}
				}
			}
			return default;
		}

		private async void Start() {
			this.ParseCommandLine();

			if (string.IsNullOrEmpty(FileLoader.Instance.DarkForcesFolder)) {
				FileLoader.Instance.DarkForcesFolder = this.CommandLineDarkForcesFolder;
				if (string.IsNullOrEmpty(FileLoader.Instance.DarkForcesFolder)) {
					FileLoader.Instance.DarkForcesFolder = PlayerPrefs.GetString("DarkForcesFolder");
					if (string.IsNullOrEmpty(FileLoader.Instance.DarkForcesFolder)) {
						FileLoader.Instance.DarkForcesFolder = await FileLoader.Instance.LocateDarkForcesAsync();
						if (string.IsNullOrEmpty(FileLoader.Instance.DarkForcesFolder)) {
							FileLoader.Instance.DarkForcesFolder = await this.ShowDarkForcesDialogAsync();
							if (string.IsNullOrEmpty(FileLoader.Instance.DarkForcesFolder)) {
								this.menu.SetActive(false);
								FileLoader.Instance.DarkForcesFolder = null;
							}
						}
					}
				}
			}

			this.darkForcesFolderText.text = FileLoader.Instance.DarkForcesFolder ?? "Location not set";
			PlayerPrefs.SetString("DarkForcesFolder", FileLoader.Instance.DarkForcesFolder);

			await FileLoader.Instance.LoadStandardFilesAsync();

			if (this.CommandLineModFiles != null) {
				foreach (string mod in this.CommandLineModFiles) {
					string[] keyValue = mod.Split('=');
					bool isLfd = Path.GetExtension(keyValue[0]).ToUpper() == ".LFD";
					Mod.Instance.List.Add(new ModFile() {
						FilePath = isLfd ? keyValue[1] : keyValue[0],
						Overrides = isLfd ? keyValue[0] : null
					});
				}
				await Mod.Instance.LoadModAsync();
			}

			await this.UpdateModTextAsync();

			await this.StyleMenuAsync();

			if (!string.IsNullOrEmpty(this.showHelp)) {
				await DfMessageBox.Instance.ShowAsync(this.showHelp);
				this.showHelp = null;
			}

			this.showcaseList.AddRange(new[] {
				new Showcase() {
					Name = "Level Explorer",
					SceneName = "LevelExplorer",
					Description = "Renders level geometry, objects, and more, and allows you to fly around in free cam mode to explore.\n\nUse the mouse to look around, arrow or WASD keys to move, mouse 3 and 5 or E and Q to move up and down. Hold Shift for a speed boost. Use Escape to change options or return to this menu.\n\nThis tool could be used as the basis for a level editor 3D preview, or even the basis for a game engine clone."
				},
				new Showcase() {
					Name = "Map Generator",
					SceneName = "MapGenerator",
					Description = "Attempts to emulate the DF automap and various level editors to draw a top-down map view.\n\nClick and drag with the mouse to pan around the map. Use right click and move the mouse left and right to rotate. Use the scroll wheel to zoom. A menu bar on top can be used to adjust options or export a PNG of the map.\n\nThis tool could be used as the basis for an automap or level editor map component."
				},
				new Showcase() {
					Name = "Randomizer",
					SceneName = "Randomizer",
					Description = "Randomizes items and enemies within Dark Forces levels or mod levels. Also allows adjustment of other aspects such as colors, light levels, automap functionality, keys/code drops, and more."
				},
				new Showcase() {
					Name = "Resource Dumper",
					SceneName = "ResourceDumper",
					Description = "Bulk processing of Dark Forces file types. Can dump files from GOBs/LFDs, can convert all image file formats to PNGs, all audio to MIDI/WAV, and all PAL/PLTT/CMP to various palette formats."
				},
				new Showcase() {
					Name = "Resource Editor",
					SceneName = "ResourceEditor",
					Description = "View and edit almost every type of resource in Dark Forces. Export and import to/from modern file formats."
				},
				new Showcase(),
				new Showcase() {
					Name = "Command Line Help",
					Description = ProgramArguments.GetHelp<Menu>()
				}
			});

			this.menu.SetActive(true);

			if (!string.IsNullOrEmpty(this.CommandLineTool)) {
				SceneManager.LoadScene(this.CommandLineTool);
			}
		}

		public async void OnDarkForcesButtonClickAsync() {
			string folder = await this.ShowDarkForcesDialogAsync();
			if (folder == null) {
				return;
			}

			FileLoader.Instance.DarkForcesFolder = folder;
			this.darkForcesFolderText.text = folder;
			PlayerPrefs.SetString("DarkForcesFolder", folder);

			FileLoader.Instance.Clear();
			ResourceCache.Instance.Clear();

			await FileLoader.Instance.LoadStandardFilesAsync();

			this.menu.SetActive(true);

			await this.StyleMenuAsync();
		}

		private async Task<string> ShowDarkForcesDialogAsync() {
			/*bool done = false;
			string folder = null;
			StandaloneFileBrowser.OpenFolderPanelAsync("Select Dark Forces Folder", null, false, folders => {
				if (folders != null && folders.Length > 0) {
					folder = folders[0];
				}

				done = true;
			});

			while (!done) {
				await Task.Delay(25);
			}

			return folder;*/

			string folder = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = false,
				FileSearchPatterns = new[] { "DARK.GOB" },
				SelectButtonText = "Select",
				SelectedFileMustExist = true,
				SelectedPathMustExist = true,
				StartSelectedFile = string.IsNullOrEmpty(FileLoader.Instance.DarkForcesFolder) ? null :
					Path.Combine(FileLoader.Instance.DarkForcesFolder, "DARK.GOB"),
				Title = "Locate Dark Forces",
				ValidateFileName = true
			});
			if (folder == null) {
				return null;
			}

			folder = Path.GetDirectoryName(folder);
			return folder;
		}

		private async Task StyleMenuAsync() {
			if (FileLoader.Instance.DarkForcesFolder != null) {
				LandruPalette pltt = await ResourceCache.Instance.GetPaletteAsync("MENU.LFD", "menu");
				if (pltt != null) {
					LandruDelt cursor = await ResourceCache.Instance.GetDeltAsync("MENU.LFD", "cursor");
					if (cursor != null) {
						Texture2D texture = ResourceCache.Instance.ImportDelt(cursor, pltt, keepTextureReadable: true);
						Cursor.SetCursor(texture, Vector2.zero, CursorMode.Auto);
					}
				}

				pltt = await ResourceCache.Instance.GetPaletteAsync("STANDARD.LFD", "standard");
				if (pltt != null) {
					LandruDelt stars = await ResourceCache.Instance.GetDeltAsync("STANDARD.LFD", "stars");
					if (stars != null) {
						Texture2D texture = ResourceCache.Instance.ImportDelt(stars, pltt);
						Rect rect = new(0, 0, texture.width, texture.height);
						this.background.sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
						this.background.enabled = true;
					}
				}
			}
		}

		private async Task UpdateModTextAsync() {
			if (!Mod.Instance.List.Any()) {
				this.modText.text = "None";
				return;
			}

			string path = Mod.Instance.Gob;
			if (path != null) {
				string text = null;
				try {
					DfLevelList levels = await FileLoader.Instance.LoadGobFileAsync<DfLevelList>("JEDI.LVL");
					text = levels?.Levels.FirstOrDefault()?.DisplayName;
				} catch (Exception e) {
					Debug.LogError(e);
				}
				if (text == null) {
					text = Path.GetFileName(path);
				}
				this.modText.text = text;
				return;
			}

			path = Mod.Instance.List.First().FilePath;
			this.modText.text = Path.GetFileName(path);
		}

		public async void OnModClickedAsync() {
			await Mod.Instance.OnModClickedAsync();

			/*await FileLoader.Instance.AddGobFileAsync(@"D:\dos\programs\games\dark\levels\dt1se\dt1se.gob");
			Mod.Instance.List.Add(new ModFile() {
				FilePath = @"D:\dos\programs\games\dark\levels\dt1se\dt1se.gob"
			});*/

			await this.UpdateModTextAsync();
		}

		public void OnCloseClicked() {
			Application.Quit();
		}

		public void OnSelectedShowcaseChanged() {
			Showcase showcase = this.showcaseList.SelectedValue;

			this.description.text = showcase?.Description ?? "";
			this.actionsBar.SetActive(showcase?.SceneName != null);
		}

		public void OnRunClicked() {
			Showcase showcase = this.showcaseList.SelectedValue;

			SceneManager.LoadScene(showcase.SceneName);
		}
	}
}
