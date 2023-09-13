using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using MZZT.FileFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
	public class FilmViewer : Databind<LandruFilm>, IResourceViewer {
		[Header("Cutscene Script"), SerializeField]
		private TMP_InputField filmLength;
		[SerializeField]
		private FilmObjectList objectList;
		[SerializeField]
		private TMP_Dropdown objectTypesDropdown;
		[SerializeField]
		private Databind objectDetails;
		[SerializeField]
		private FilmCommandList commandList;
		[SerializeField]
		private TMP_Dropdown commandTypesDropdown;
		[SerializeField]
		private Button commandMoveUp;
		[SerializeField]
		private Button commandMoveDown;
		[SerializeField]
		private Databind commandDetails;
		[SerializeField]
		private Toggle commandEnabled;
		[SerializeField]
		private GameObject timeContainer;
		[SerializeField]
		private TMP_InputField time;
		[SerializeField]
		private GameObject moveContainer;
		[SerializeField]
		private TMP_InputField moveX;
		[SerializeField]
		private TMP_InputField moveY;
		[SerializeField]
		private GameObject speedContainer;
		[SerializeField]
		private TMP_InputField speedX;
		[SerializeField]
		private TMP_InputField speedY;
		[SerializeField]
		private GameObject layerContainer;
		[SerializeField]
		private TMP_InputField layer;
		[SerializeField]
		private GameObject frameContainer;
		[SerializeField]
		private TMP_InputField frame;
		[SerializeField]
		private GameObject animateContainer;
		[SerializeField]
		private TMP_InputField animate;
		[SerializeField]
		private GameObject cueContainer;
		[SerializeField]
		private TMP_InputField cue;
		[SerializeField]
		private GameObject windowContainer;
		[SerializeField]
		private TMP_InputField windowMinX;
		[SerializeField]
		private TMP_InputField windowMinY;
		[SerializeField]
		private TMP_InputField windowMaxX;
		[SerializeField]
		private TMP_InputField windowMaxY;
		[SerializeField]
		private GameObject varContainer;
		[SerializeField]
		private TMP_InputField var;
		[SerializeField]
		private GameObject switchContainer;
		[SerializeField]
		private Toggle @switch;
		[SerializeField]
		private GameObject flipContainer;
		[SerializeField]
		private Toggle flipHorizontal;
		[SerializeField]
		private Toggle flipUnknown;
		[SerializeField]
		private GameObject paletteContainer;
		[SerializeField]
		private TMP_InputField palette;
		[SerializeField]
		private GameObject cutContainer;
		[SerializeField]
		private TMP_Dropdown cutMode;
		[SerializeField]
		private TMP_Dropdown cutStyle;
		[SerializeField]
		private GameObject soundContainer;
		[SerializeField]
		private Toggle soundPlay;
		[SerializeField]
		private Slider soundVolStart;
		[SerializeField]
		private TMP_Text soundVolStartText;
		[SerializeField]
		private Slider soundVolEnd;
		[SerializeField]
		private TMP_Text soundVolEndText;
		[SerializeField]
		private Slider soundVolTime;
		[SerializeField]
		private TMP_Text soundVolTimeText;
		[SerializeField]
		private GameObject stereoContainer;
		[SerializeField]
		private Slider stereoPanStart;
		[SerializeField]
		private TMP_Text stereoPanStartText;
		[SerializeField]
		private Slider stereoPanEnd;
		[SerializeField]
		private TMP_Text stereoPanEndText;
		[SerializeField]
		private Slider stereoPanTime;
		[SerializeField]
		private TMP_Text stereoPanTimeText;
		[SerializeField]
		private GameObject loopContainer;
		[SerializeField]
		private TMP_InputField loop;
		[SerializeField]
		private GameObject preloadContainer;
		[SerializeField]
		private TMP_InputField preload;

		public string TabName => this.filePath == null ? "New FILM" : Path.GetFileName(this.filePath);
		public event EventHandler TabNameChanged;

		public Sprite Thumbnail { get; private set; }
#pragma warning disable CS0067
		public event EventHandler ThumbnailChanged;
#pragma warning restore CS0067

		public void ResetDirty() {
			if (!this.IsDirty) {
				return;
			}

			this.IsDirty = false;
			this.IsDirtyChanged?.Invoke(this, new());
		}

		public void OnDirty() {
			if (this.IsDirty) {
				return;
			}

			this.IsDirty = true;
			this.IsDirtyChanged?.Invoke(this, new());
		}
		private bool userInput = true;

		public bool IsDirty { get; private set; }
		public event EventHandler IsDirtyChanged;

		private string filePath;
		public Task LoadAsync(ResourceEditorResource resource, IFile file) {
			this.filePath = resource?.Path;

			try {
				LandruFilm film = (LandruFilm)file;

				if (film.Objects.FirstOrDefault()?.Type != "VIEW") {
					film.Objects.Insert(0, new LandruFilm.FilmObject() {
						Type = "VIEW",
						Name = "untitled",
						Commands = {
							new() {
								Type = LandruFilm.CommandTypes.End
							}
						}
					});
				}
				if (film.Objects.LastOrDefault()?.Type != "END") {
					film.Objects.Add(new LandruFilm.FilmObject() {
						Type = "END",
						Name = "untitled"
					});
				}

				this.Value = film;

				this.filmLength.text = (this.Value.FilmLength / 10f).ToString();
			} finally {
				this.userInput = true;
			}

			this.PopulateObjectTypesDropdown();

			return Task.CompletedTask;
		}

		public void OnFilmLengthChanged(string value) {
			if (!this.userInput) {
				return;
			}

			if (float.TryParse(value, out float result)) {
				int length = Mathf.RoundToInt(result * 10);

				ushort shortLength = (ushort)Math.Clamp(length, ushort.MinValue, ushort.MaxValue); ;

				if (shortLength == this.Value.FilmLength) {
					return;
				}

				this.Value.FilmLength = shortLength;

				this.OnDirty();
			}

			this.filmLength.text = (this.Value.FilmLength / 10f).ToString();
		}

		private void PopulateObjectTypesDropdown() {
			this.objectTypesDropdown.ClearOptions();

			bool hasView = this.Value.Objects.Any(x => x.Type == "VIEW");
			if (!hasView) {
				this.objectTypesDropdown.options.Add(new("VIEW"));
				this.objectTypesDropdown.interactable = false;
			} else {
				this.objectTypesDropdown.options.AddRange(new[] {
					new TMP_Dropdown.OptionData("DELT"),
					new TMP_Dropdown.OptionData("ANIM"),
					new TMP_Dropdown.OptionData("PLTT"),
					new TMP_Dropdown.OptionData("VOIC"),
					new TMP_Dropdown.OptionData("CUST")
				});
				this.objectTypesDropdown.interactable = true;
			}

			this.objectTypesDropdown.value = -1;
			this.objectTypesDropdown.value = 0;
		}

		public void OnSelectedObjectChanged() {
			IDatabind selected = this.objectList.SelectedDatabound;
			if (selected == null) {
				this.objectDetails.gameObject.SetActive(false);
				return;
			}

			this.userInput = false;
			try {
				((IDatabind)this.objectDetails).MemberName = selected.MemberName;
				this.objectDetails.gameObject.SetActive(true);
				((IDatabind)this.objectDetails).Invalidate();

				this.PopulateCommandTypesDropdown();

				if (this.commandList.Count > 0) {
					this.commandList.SelectedIndex = 0;
				}
			} finally {
				this.userInput = true;
			}
		}

		public void AddObject() {
			string type = this.objectTypesDropdown.options[this.objectTypesDropdown.value].text;
			string name = type switch {
				"CUST" => "custom",
				"VIEW" => "untitled",
				"END" => "untitled",
				_ => string.Empty
			};
			LandruFilm.FilmObject obj = new() {
				Type = type,
				Name = name
			};
			if (type != "END") {
				obj.Commands.Add(new LandruFilm.Command() {
					Type = LandruFilm.CommandTypes.End
				});
			}

			if (this.Value.Objects.Count == 0 || this.Value.Objects.Last().Type != "END") {
				this.objectList.AddRange(new[] {
					obj,
					new LandruFilm.FilmObject() {
						Type = "END",
						Name = "untitled"
					}
				});
			} else {
				this.objectList.Insert(this.objectList.Count - 1, obj);
			}

			this.OnDirty();
			this.PopulateObjectTypesDropdown();

			this.objectList.SelectedValue = obj;
		}

		public void OnObjectRemoved(int index, LandruFilm.FilmObject obj) {
			this.OnDirty();
			this.PopulateObjectTypesDropdown();
		}

		public void OnObjectNameChanged() {
			if (!this.userInput) {
				return;
			}

			this.OnDirty();

			this.objectList.SelectedDatabound.Invalidate();
		}

		private void PopulateCommandTypesDropdown() {
			this.commandTypesDropdown.ClearOptions();

			LandruFilm.FilmObject obj = this.objectList.SelectedValue;
			if (obj == null || obj.Type == "END") {
				return;
			}

			this.commandTypesDropdown.options.Add(new("TIME"));

			if (obj.Commands.Any(x => (x.Type & ~LandruFilm.CommandTypes.Disabled) == LandruFilm.CommandTypes.Time)) {
				switch (obj.Type) {
					case "VIEW":
						this.commandTypesDropdown.options.Add(new("CUT"));
						break;
					case "PLTT":
						this.commandTypesDropdown.options.Add(new("PALETTE"));
						break;
					case "CUST":
						this.commandTypesDropdown.options.AddRange(new TMP_Dropdown.OptionData[] {
							new("CUE"),
							new("VAR")
						});
						break;
					case "DELT":
						this.commandTypesDropdown.options.AddRange(new TMP_Dropdown.OptionData[] {
							new("MOVE"),
							new("SPEED"),
							new("LAYER"),
							new("WINDOW"),
							new("SWITCH"),
							new("FLIP")
						});
						break;
					case "ANIM":
						this.commandTypesDropdown.options.AddRange(new TMP_Dropdown.OptionData[] {
							new("MOVE"),
							new("SPEED"),
							new("LAYER"),
							new("FRAME"),
							new("ANIMATE"),
							new("WINDOW"),
							new("SWITCH"),
							new("FLIP")
						});
						break;
					case "VOIC":
						this.commandTypesDropdown.options.AddRange(new TMP_Dropdown.OptionData[] {
							new("SOUND"),
							new("STEREO"),
							new("LOOP"),
							new("PRELOAD")
						});
						break;
				}
			}

			this.commandTypesDropdown.interactable = this.objectTypesDropdown.options.Count > 1;

			this.commandTypesDropdown.value = -1;
			this.commandTypesDropdown.value = 0;
		}

		public void OnSelectedCommandChanged() {
			this.userInput = false;
			try {
				IDatabind selected = this.commandList.SelectedDatabound;
				if (selected == null || selected.Value == null) {
					this.commandDetails.gameObject.SetActive(false);
					return;
				}

				((IDatabind)this.commandDetails).MemberName = selected.MemberName;
				this.commandDetails.gameObject.SetActive(true);
				((IDatabind)this.commandDetails).Invalidate();

				this.UpdateMoveCommandButtons();

				LandruFilm.Command command = (LandruFilm.Command)selected.Value;
				this.commandEnabled.isOn = !command.Type.HasFlag(LandruFilm.CommandTypes.Disabled);

				short[] param = (command.Parameters ?? Enumerable.Empty<short>()).Concat(Enumerable.Repeat<short>(0, 7)).ToArray();
				LandruFilm.CommandTypes type = command.Type & ~LandruFilm.CommandTypes.Disabled;

				switch (type) {
					case LandruFilm.CommandTypes.Time:
						this.time.text = (param[0] / 10f).ToString();
						break;
					case LandruFilm.CommandTypes.Move:
						this.moveX.text = (param[0] + param[2] / 256f).ToString();
						this.moveY.text = (param[1] + param[3] / 256f).ToString();
						break;
					case LandruFilm.CommandTypes.Speed:
						this.speedX.text = ((param[0] + param[2] / 256f) * 10).ToString();
						this.speedY.text = ((param[1] + param[3] / 256f) * 10).ToString();
						break;
					case LandruFilm.CommandTypes.Layer:
						this.layer.text = param[0].ToString();
						break;
					case LandruFilm.CommandTypes.Frame:
						this.frame.text = (param[0] + param[1] / 256f).ToString();
						break;
					case LandruFilm.CommandTypes.Animate:
						this.animate.text = (param[0] + param[1] / 256f).ToString();
						break;
					case LandruFilm.CommandTypes.Cue:
						this.cue.text = param[0].ToString();
						break;
					case LandruFilm.CommandTypes.Window:
						this.windowMinX.text = param[0].ToString();
						this.windowMinY.text = param[1].ToString();
						this.windowMaxX.text = param[2].ToString();
						this.windowMaxY.text = param[3].ToString();
						break;
					case LandruFilm.CommandTypes.Var:
						this.var.text = param[0].ToString();
						break;
					case LandruFilm.CommandTypes.Switch:
						this.@switch.isOn = param[0] != 0;
						break;
					case LandruFilm.CommandTypes.Flip:
						this.flipHorizontal.isOn = param[0] != 0;
						this.flipUnknown.isOn = param[1] != 0;
						break;
					case LandruFilm.CommandTypes.Palette:
						this.palette.text = param[0].ToString();
						break;
					case LandruFilm.CommandTypes.Cut:
						this.cutMode.value = param[1] - 1;
						this.cutStyle.value = param[0];
						break;
					case LandruFilm.CommandTypes.Loop:
						this.loop.text = param[0].ToString();
						break;
					case LandruFilm.CommandTypes.Preload:
						this.preload.text = param[0].ToString();
						break;
					case LandruFilm.CommandTypes.Sound:
						this.soundPlay.isOn = param[0] != 0;
						this.soundVolStart.value = param[1];
						this.soundVolEnd.value = param[2];
						this.soundVolTime.value = param[3];
						break;
					case LandruFilm.CommandTypes.Stereo:
						this.soundPlay.isOn = param[0] != 0;
						this.soundVolStart.value = param[1];
						this.soundVolEnd.value = param[2];
						this.soundVolTime.value = param[3];
						this.stereoPanStart.value = param[4];
						this.stereoPanEnd.value = param[5];
						this.stereoPanTime.value = param[6];
						break;
				}

				this.timeContainer.SetActive(type == LandruFilm.CommandTypes.Time);
				this.moveContainer.SetActive(type == LandruFilm.CommandTypes.Move);
				this.speedContainer.SetActive(type == LandruFilm.CommandTypes.Speed);
				this.layerContainer.SetActive(type == LandruFilm.CommandTypes.Layer);
				this.frameContainer.SetActive(type == LandruFilm.CommandTypes.Frame);
				this.animateContainer.SetActive(type == LandruFilm.CommandTypes.Animate);
				this.cueContainer.SetActive(type == LandruFilm.CommandTypes.Cue);
				this.windowContainer.SetActive(type == LandruFilm.CommandTypes.Window);
				this.varContainer.SetActive(type == LandruFilm.CommandTypes.Var);
				this.switchContainer.SetActive(type == LandruFilm.CommandTypes.Switch);
				this.flipContainer.SetActive(type == LandruFilm.CommandTypes.Flip);
				this.paletteContainer.SetActive(type == LandruFilm.CommandTypes.Palette);
				this.cutContainer.SetActive(type == LandruFilm.CommandTypes.Cut);
				this.loopContainer.SetActive(type == LandruFilm.CommandTypes.Loop);
				this.preloadContainer.SetActive(type == LandruFilm.CommandTypes.Preload);
				this.soundContainer.SetActive(type == LandruFilm.CommandTypes.Sound || type == LandruFilm.CommandTypes.Stereo);
				this.stereoContainer.SetActive(type == LandruFilm.CommandTypes.Stereo);
			} finally {
				this.userInput = true;
			}
		}

		public void AddCommand() {
			LandruFilm.CommandTypes type = this.commandTypesDropdown.options[this.commandTypesDropdown.value].text switch {
				"END" => LandruFilm.CommandTypes.End,
				"TIME" => LandruFilm.CommandTypes.Time,
				"MOVE" => LandruFilm.CommandTypes.Move,
				"SPEED" => LandruFilm.CommandTypes.Speed,
				"LAYER" => LandruFilm.CommandTypes.Layer,
				"FRAME" => LandruFilm.CommandTypes.Frame,
				"ANIMATE" => LandruFilm.CommandTypes.Animate,
				"CUE" => LandruFilm.CommandTypes.Cue,
				"VAR" => LandruFilm.CommandTypes.Var,
				"WINDOW" => LandruFilm.CommandTypes.Window,
				"SWITCH" => LandruFilm.CommandTypes.Switch,
				"FLIP" => LandruFilm.CommandTypes.Flip,
				"PALETTE" => LandruFilm.CommandTypes.Palette,
				"CUT" => LandruFilm.CommandTypes.Cut,
				"LOOP" => LandruFilm.CommandTypes.Loop,
				"PRELOAD" => LandruFilm.CommandTypes.Preload,
				"SOUND" => LandruFilm.CommandTypes.Sound,
				"STEREO" => LandruFilm.CommandTypes.Stereo,
				_ => default
			};

			LandruFilm.Command command = new() {
				Type = type,
				Parameters = (short[])Array.CreateInstance(typeof(short), type switch {
					LandruFilm.CommandTypes.Time => 1,
					LandruFilm.CommandTypes.Move => 4,
					LandruFilm.CommandTypes.Speed => 4,
					LandruFilm.CommandTypes.Layer => 1,
					LandruFilm.CommandTypes.Frame => 2,
					LandruFilm.CommandTypes.Animate => 2,
					LandruFilm.CommandTypes.Cue => 1,
					LandruFilm.CommandTypes.Window => 4,
					LandruFilm.CommandTypes.Var => 1,
					LandruFilm.CommandTypes.Switch => 1,
					LandruFilm.CommandTypes.Flip => 2,
					LandruFilm.CommandTypes.Palette => 1,
					LandruFilm.CommandTypes.Cut => 2,
					LandruFilm.CommandTypes.Sound => 4,
					LandruFilm.CommandTypes.Stereo => 7,
					LandruFilm.CommandTypes.Loop => 1,
					LandruFilm.CommandTypes.Preload => 1,
					_ => 0
				})
			};

			if (type == LandruFilm.CommandTypes.Time) {
				LandruFilm.Command oldTime = this.commandList.LastOrDefault(x => x.Type == LandruFilm.CommandTypes.Time);
				if (oldTime != null) {
					short time = oldTime.Parameters.FirstOrDefault();
					command.Parameters[0] = (short)(time + 1);
				}
			}

			if (this.commandList.Count == 0 || (this.commandList.Last().Type & ~LandruFilm.CommandTypes.Disabled) != LandruFilm.CommandTypes.End) {
				this.commandList.AddRange(new[] {
					command,
					new LandruFilm.Command() {
						Type = LandruFilm.CommandTypes.End
					}
				});
			} else {
				this.commandList.Insert(this.commandList.Count - 1, command);
			}

			if (this.commandList.Count > 2) {
				this.commandList.GetDatabinder(this.commandList.Count - 3).Invalidate();
			}

			this.OnDirty();

			this.commandList.SelectedValue = command;

			this.PopulateCommandTypesDropdown();
		}

		public void OnCommandRemoved(int index, LandruFilm.Command command) {
			this.OnDirty();

			if (index > 0) {
				this.commandList.GetDatabinder(index - 1).Invalidate();
			}

			this.PopulateCommandTypesDropdown();
		}

		private void UpdateMoveCommandButtons() {
			IDatabind selected = this.commandList.SelectedDatabound;
			if (selected == null || selected.Value == null) {
				this.commandMoveUp.interactable = false;
				this.commandMoveDown.interactable = false;
				return;
			}

			Canvas.ForceUpdateCanvases();

			int index = this.commandList.SelectedIndex;

			LandruFilm.Command prev = index > 0 ? this.commandList[index - 1] : null;
			LandruFilm.Command current = (LandruFilm.Command)selected.Value;
			LandruFilm.Command next = index < this.commandList.Count - 1 ? this.commandList[index + 1] : null;

			LandruFilm.CommandTypes prevType = (prev?.Type & ~LandruFilm.CommandTypes.Disabled) ?? LandruFilm.CommandTypes.Disabled;
			LandruFilm.CommandTypes type = current.Type & ~LandruFilm.CommandTypes.Disabled;
			LandruFilm.CommandTypes nextType = (next?.Type & ~LandruFilm.CommandTypes.Disabled) ?? LandruFilm.CommandTypes.Disabled;

			if (prev == null) {
				this.commandMoveUp.interactable = false;
			} else if (type == LandruFilm.CommandTypes.Time && prevType == LandruFilm.CommandTypes.Time) {
				this.commandMoveUp.interactable = false;
			} else if (index <= 1) {
				this.commandMoveUp.interactable = false;
			} else {
				this.commandMoveUp.interactable = true;
			}

			if (next == null) {
				this.commandMoveDown.interactable = false;
			} else if (nextType == LandruFilm.CommandTypes.End) {
				this.commandMoveDown.interactable = false;
			} else if (type == LandruFilm.CommandTypes.Time && nextType == LandruFilm.CommandTypes.Time) {
				this.commandMoveDown.interactable = false;
			} else if (type == LandruFilm.CommandTypes.Time && index == 0) {
				this.commandMoveDown.interactable = false;
			} else {
				this.commandMoveDown.interactable = true;
			}
		}
		
		public void MoveCommandUp() {
			if (!this.commandMoveUp.interactable) {
				return;
			}

			IDatabind selected = this.commandList.SelectedDatabound;
			if (selected == null) {
				return;
			}

			LandruFilm.Command command = (LandruFilm.Command)selected.Value;

			int index = this.commandList.SelectedIndex;

			this.commandList.RemoveAt(index);
			this.commandList.Insert(index - 1, command);

			this.commandList.GetDatabinder(index).Invalidate();
			if (index - 2 >= 0) {
				this.commandList.GetDatabinder(index - 2).Invalidate();
			}

			this.commandList.SelectedIndex = -1;
			this.commandList.SelectedIndex = index - 1;
		}

		public void MoveCommandDown() {
			if (!this.commandMoveDown.interactable) {
				return;
			}

			IDatabind selected = this.commandList.SelectedDatabound;
			if (selected == null) {
				return;
			}

			LandruFilm.Command command = (LandruFilm.Command)selected.Value;

			int index = this.commandList.SelectedIndex;

			this.commandList.RemoveAt(index);
			this.commandList.Insert(index + 1, command);

			this.commandList.GetDatabinder(index).Invalidate();
			if (index + 2 < this.commandList.Count) {
				this.commandList.GetDatabinder(index + 2).Invalidate();
			}

			this.commandList.SelectedIndex = -1;
			this.commandList.SelectedIndex = index + 1;
		}

		public void OnCommandEnabledChanged(bool value) {
			if (!this.userInput) {
				return;
			}

			IDatabind selected = this.commandList.SelectedDatabound;
			if (selected == null) {
				return;
			}

			LandruFilm.Command command = (LandruFilm.Command)selected.Value;
			bool commandEnabled = !command.Type.HasFlag(LandruFilm.CommandTypes.Disabled);
			if (commandEnabled == value) {
				return;
			}

			if (value) {
				command.Type &= ~LandruFilm.CommandTypes.Disabled;
			} else {
				command.Type |= LandruFilm.CommandTypes.Disabled;
			}

			this.OnDirty();

			selected.Invalidate();
		}

		public void OnTimeChanged(string value) {
			if (!this.userInput) {
				return;
			}

			IDatabind selected = this.commandList.SelectedDatabound;
			if (selected == null) {
				return;
			}

			if (!float.TryParse(value, out float floatValue)) {
				return;
			}

			short shortValue = (short)Math.Clamp(Mathf.RoundToInt(floatValue * 10), short.MinValue, short.MaxValue);

			LandruFilm.Command command = (LandruFilm.Command)selected.Value;
			if (command.Parameters.Length >= 1 && command.Parameters[0] == shortValue) {
				return;
			}

			if (command.Parameters.Length < 1) {
				short[] parameters = command.Parameters;
				Array.Resize(ref parameters, 1);
				command.Parameters = parameters;
			}
			command.Parameters[0] = shortValue;

			this.ReorderCommandsByTime();
		}

		private void ReorderCommandsByTime() {
			this.OnDirty();

			Dictionary<LandruFilm.Command, List<LandruFilm.Command>> commands = new();
			List<LandruFilm.Command> currentTime = new();
			foreach (LandruFilm.Command command in this.commandList) {
				switch (command.Type & ~LandruFilm.CommandTypes.Disabled) {
					case LandruFilm.CommandTypes.Time:
						commands[command] = currentTime = new();
						break;
					case LandruFilm.CommandTypes.End:
						break;
					default:
						if (currentTime != null) {
							currentTime.Add(command);
						}
						break;
				}
			}

			LandruFilm.Command current = this.commandList.SelectedValue;

			this.commandList.Clear();
			this.commandList.AddRange(commands.OrderBy(x => x.Key.Parameters.ElementAtOrDefault(0)).Select(x => x.Value.Prepend(x.Key)).SelectMany(x => x));
			this.commandList.SelectedValue = current;
		}

		private void OnPosOrSpeedChanged(string value, int wholePos, int fracPos, int factor) {
			if (!this.userInput) {
				return;
			}

			if (!double.TryParse(value, out double floatValue)) {
				return;
			}

			floatValue /= factor;

			short wholeValue = (short)Math.Clamp(Math.Round(floatValue), short.MinValue, short.MaxValue);
			floatValue = (floatValue - wholeValue) * 256;
			short fracValue = (short)Math.Clamp(Math.Round(floatValue), short.MinValue, short.MaxValue);

			IDatabind selected = this.commandList.SelectedDatabound;
			if (selected == null) {
				return;
			}

			int length = (fracPos / 2 + 1) * 2;

			LandruFilm.Command command = (LandruFilm.Command)selected.Value;
			if (command.Parameters.Length >= length && command.Parameters[wholePos] == wholeValue && command.Parameters[fracPos] == fracValue) {
				return;
			}

			if (command.Parameters.Length < length) {
				short[] parameters = command.Parameters;
				Array.Resize(ref parameters, length);
				command.Parameters = parameters;
			}
			command.Parameters[wholePos] = wholeValue;
			command.Parameters[fracPos] = fracValue;

			this.OnDirty();

			selected.Invalidate();
		}

		public void OnMoveXChanged(string value) => this.OnPosOrSpeedChanged(value, 0, 2, 1);

		public void OnMoveYChanged(string value) => this.OnPosOrSpeedChanged(value, 1, 3, 1);

		public void OnSpeedXChanged(string value) => this.OnPosOrSpeedChanged(value, 0, 2, 10);

		public void OnSpeedYChanged(string value) => this.OnPosOrSpeedChanged(value, 1, 3, 10);

		private void OnIntChanged(string value, int pos, int length) {
			if (!this.userInput) {
				return;
			}

			IDatabind selected = this.commandList.SelectedDatabound;
			if (selected == null) {
				return;
			}

			if (!short.TryParse(value, out short shortValue)) {
				return;
			}

			LandruFilm.Command command = (LandruFilm.Command)selected.Value;
			if (command.Parameters.Length >= length && command.Parameters[pos] == shortValue) {
				return;
			}

			if (command.Parameters.Length < length) {
				short[] parameters = command.Parameters;
				Array.Resize(ref parameters, length);
				command.Parameters = parameters;
			}
			command.Parameters[pos] = shortValue;

			this.OnDirty();

			selected.Invalidate();
		}

		public void OnSingleIntChanged(string value) => this.OnIntChanged(value, 0, 1);

		public void OnFrameOrAnimateChanged(string value) => this.OnPosOrSpeedChanged(value, 0, 1, 1);

		public void OnWindowMinXChanged(string value) => this.OnIntChanged(value, 0, 4);
		public void OnWindowMinYChanged(string value) => this.OnIntChanged(value, 1, 4);
		public void OnWindowMaxXChanged(string value) => this.OnIntChanged(value, 2, 4);
		public void OnWindowMaxYChanged(string value) => this.OnIntChanged(value, 3, 4);

		private void OnToggleChanged(bool value, int pos, int length) {
			if (!this.userInput) {
				return;
			}

			IDatabind selected = this.commandList.SelectedDatabound;
			if (selected == null) {
				return;
			}

			short shortValue = value ? (short)1 : (short)0;

			LandruFilm.Command command = (LandruFilm.Command)selected.Value;
			if (command.Parameters.Length >= length && command.Parameters[pos] == shortValue) {
				return;
			}

			if (command.Parameters.Length < length) {
				short[] parameters = command.Parameters;
				Array.Resize(ref parameters, length);
				command.Parameters = parameters;
			}
			command.Parameters[pos] = shortValue;

			this.OnDirty();

			selected.Invalidate();
		}

		public void OnSwitchChanged(bool value) => this.OnToggleChanged(value, 0, 1);

		public void OnFlipHorizChanged(bool value) => this.OnToggleChanged(value, 0, 2);

		public void OnFlipUnknownChanged(bool value) => this.OnToggleChanged(value, 1, 2);

		public void OnCutModeChanged(int value) {
			if (!this.userInput) {
				return;
			}

			IDatabind selected = this.commandList.SelectedDatabound;
			if (selected == null) {
				return;
			}

			short shortValue = (short)(value + 1);

			LandruFilm.Command command = (LandruFilm.Command)selected.Value;
			if (command.Parameters.Length >= 2 && command.Parameters[1] == shortValue) {
				return;
			}

			if (command.Parameters.Length < 2) {
				short[] parameters = command.Parameters;
				Array.Resize(ref parameters, 2);
				command.Parameters = parameters;
			}
			command.Parameters[1] = shortValue;

			this.OnDirty();

			selected.Invalidate();
		}

		public void OnCutStyleChanged(int value) {
			if (!this.userInput) {
				return;
			}

			IDatabind selected = this.commandList.SelectedDatabound;
			if (selected == null) {
				return;
			}

			short shortValue = (short)value;

			LandruFilm.Command command = (LandruFilm.Command)selected.Value;
			if (command.Parameters.Length >= 2 && command.Parameters[0] == shortValue) {
				return;
			}

			if (command.Parameters.Length < 2) {
				short[] parameters = command.Parameters;
				Array.Resize(ref parameters, 2);
				command.Parameters = parameters;
			}
			command.Parameters[0] = shortValue;

			this.OnDirty();

			selected.Invalidate();
		}

		public void OnSoundPlayChanged(bool value) {
			if (!this.userInput) {
				return;
			}

			IDatabind selected = this.commandList.SelectedDatabound;
			if (selected == null) {
				return;
			}

			LandruFilm.Command command = (LandruFilm.Command)selected.Value;
			int length = (command.Type & ~LandruFilm.CommandTypes.Disabled) == LandruFilm.CommandTypes.Stereo ? 7 : 4;

			this.OnToggleChanged(value, 0, length);
		}

		private void OnPercentChanged(float value, int max, int pos, TMP_Text text) {
			IDatabind selected = this.commandList.SelectedDatabound;
			if (selected == null) {
				return;
			}

			short shortValue = (short)value;

			text.text = (shortValue / (float)max).ToString("p");

			if (!this.userInput) {
				return;
			}

			LandruFilm.Command command = (LandruFilm.Command)selected.Value;
			int length = (command.Type & ~LandruFilm.CommandTypes.Disabled) == LandruFilm.CommandTypes.Stereo ? 7 : 4;
			if (command.Parameters.Length >= length && command.Parameters[pos] == shortValue) {
				return;
			}

			if (command.Parameters.Length < length) {
				short[] parameters = command.Parameters;
				Array.Resize(ref parameters, length);
				command.Parameters = parameters;
			}
			command.Parameters[pos] = shortValue;

			this.OnDirty();

			selected.Invalidate();
		}

		public void OnSoundVolumeStartChanged(float value) => this.OnPercentChanged(value, 127, 1, this.soundVolStartText);

		public void OnSoundVolumeEndChanged(float value) => this.OnPercentChanged(value, 127, 2, this.soundVolEndText);

		public void OnSoundVolumeTimeChanged(float value) => this.OnPercentChanged(value, 1000, 3, this.soundVolTimeText);

		private void OnStereoPanChanged(float value, int pos, TMP_Text text) {
			IDatabind selected = this.commandList.SelectedDatabound;
			if (selected == null) {
				return;
			}

			short shortValue = (short)value;

			text.text = ((shortValue - 50) / (float)50).ToString("p");

			if (!this.userInput) {
				return;
			}

			LandruFilm.Command command = (LandruFilm.Command)selected.Value;
			if (command.Parameters.Length >= 7 && command.Parameters[pos] == shortValue) {
				return;
			}

			if (command.Parameters.Length < 7) {
				short[] parameters = command.Parameters;
				Array.Resize(ref parameters, 7);
				command.Parameters = parameters;
			}
			command.Parameters[pos] = shortValue;

			this.OnDirty();

			selected.Invalidate();
		}

		public void OnStereoPanStartChanged(float value) => this.OnStereoPanChanged(value, 4, this.stereoPanStartText);

		public void OnStereoPanEndChanged(float value) => this.OnStereoPanChanged(value, 5, this.stereoPanEndText);

		public void OnStereoPanTimeChanged(float value) => this.OnPercentChanged(value, 1000, 6, this.stereoPanTimeText);

		public async void SaveAsync() {
			bool canSave = Directory.Exists(Path.GetDirectoryName(this.filePath));
			if (!canSave) {
				this.SaveAsAsync();
				return;
			}

			// Writing to the stream is loads faster than to the file. Not sure why. Unity thing probably, doesn't happen on .NET 6.
			using MemoryStream mem = new();
			await this.Value.SaveAsync(mem);

			mem.Position = 0;
			using FileStream stream = new(this.filePath, FileMode.Create, FileAccess.Write, FileShare.None);
			await mem.CopyToAsync(stream);

			this.ResetDirty();
		}

		public async void SaveAsAsync() {
			string path = await ResourceEditors.Instance.PickSaveLocationAsync(this.filePath, new[] { "*.FILM", "*.FLM" });
			if (string.IsNullOrEmpty(path)) {
				return;
			}
			this.filePath = path;
			this.TabNameChanged?.Invoke(this, new EventArgs());

			bool canSave = Directory.Exists(Path.GetDirectoryName(this.filePath));
			if (!canSave) {
				return;
			}

			this.SaveAsync();
		}
	}
}
