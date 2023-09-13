using MZZT.DarkForces.Converters;
using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using MZZT.FileFormats;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Color = UnityEngine.Color;
using Image = UnityEngine.UI.Image;

namespace MZZT.DarkForces.Showcase {
	public class WaxViewer : Databind<DfWax>, IResourceViewer {
		[Header("WAX"), SerializeField]
		private TMP_Dropdown waxList;
		[SerializeField]
		private Toggle autoCompress;
		[SerializeField]
		private Button deleteWax;
		[SerializeField]
		private Button moveWaxUp;
		[SerializeField]
		private Button moveWaxDown;
		[SerializeField]
		private Button addWax;
		[SerializeField]
		private GameObject sequences;
		[SerializeField]
		private TMP_Dropdown sequenceList;
		[SerializeField]
		private Button deleteSequence;
		[SerializeField]
		private Button moveSequenceUp;
		[SerializeField]
		private Button moveSequenceDown;
		[SerializeField]
		private Button addSequence;
		[SerializeField]
		private GameObject frame;
		[SerializeField]
		private Image preview;
		[SerializeField]
		private float previewSensitivity = -10;
		[SerializeField]
		private Button playButton;
		[SerializeField]
		private TMP_Text playImage;
		[SerializeField]
		private TMP_Text pauseImage;
		[SerializeField]
		private Slider frameSlider;
		[SerializeField]
		private TMP_Text frameCounter;
		[SerializeField]
		private Button moveFrameUp;
		[SerializeField]
		private Button importFramePrev;
		[SerializeField]
		private Button importFrameNext;
		[SerializeField]
		private Button moveFrameDown;
		[SerializeField]
		private Button exportFrame;
		[SerializeField]
		private Button deleteFrame;
		[SerializeField]
		private DatabindObject waxDetails;
		[SerializeField]
		private DatabindObject frameDetails;
		[SerializeField]
		private TMP_InputField palette;
		[SerializeField]
		private Slider lightLevel;

		public string TabName => this.filePath == null ? "New WAX" : Path.GetFileName(this.filePath);
		public event EventHandler TabNameChanged;

		public Sprite Thumbnail { get; private set; }
		public event EventHandler ThumbnailChanged;

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

		public bool IsDirty { get; private set; }
		public event EventHandler IsDirtyChanged;

		private string filePath;
		public string FilePath => this.filePath;
		public async Task LoadAsync(ResourceEditorResource resource, IFile file) {
			this.Value = ((DfWax)file).Reduplicate();
			this.filePath = resource?.Path;

			this.PopulateWaxList();
			this.OnSelectedWaxChanged(this.waxList.value = 0);

			await this.RefreshPaletteAsync();
		}

		private static readonly string[] waxLabels = new[] {
			"moving/normal",
			"attack/idle/damaged",
			"dying (melee)",
			"dying",
			"dead",
			"standing",
			"after attack",
			"secondary attack",
			"after secondary",
			"jump",
			"unused",
			"unused",
			"injured",
			"special"
		};

		public async void SaveAsync() {
			bool canSave = Directory.Exists(Path.GetDirectoryName(this.filePath));
			if (!canSave) {
				this.SaveAsAsync();
				return;
			}

			// Writing to the stream is loads faster than to the file. Not sure why. Unity thing probably, doesn't happen on .NET 6.
			using MemoryStream mem = new();
			await this.Value.Deduplicate().SaveAsync(mem);

			mem.Position = 0;
			using FileStream stream = new(this.filePath, FileMode.Create, FileAccess.Write, FileShare.None);
			await mem.CopyToAsync(stream);

			this.ResetDirty();
		}

		public async void SaveAsAsync() {
			string path = await ResourceEditors.Instance.PickSaveLocationAsync(this.filePath, new[] { "*.WAX" });
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

		public void OnAutoCompressChanged(bool value) {
			foreach (DfFrame frame in this.Value.Waxes.SelectMany(x => x.Sequences).SelectMany(x => x.Frames)) {
				frame.AutoCompress = value;
			}

			this.OnDirty();
		}

		private string GetWaxLabel(int index) {
			if (index >= waxLabels.Length) {
				return $"{index} - unused";
			}

			return $"{index} - {waxLabels[index]}";
		}

		private void PopulateWaxList() {
			this.waxList.ClearOptions();
			this.waxList.AddOptions(this.Value.Waxes.Select((x, i) => new TMP_Dropdown.OptionData(this.GetWaxLabel(i))).ToList());
		}

		public void OnSelectedWaxChanged(int index) {
			this.playing = false;
			this.frameIndex = 0;

			this.deleteWax.interactable = this.Value.Waxes.Count > 0;
			this.moveWaxUp.interactable = this.Value.Waxes.Count > 0 && index > 0;
			this.moveWaxDown.interactable = this.Value.Waxes.Count > 0 && index < this.Value.Waxes.Count - 1;
			this.addWax.interactable = this.Value.Waxes.Count < 32;

			if (index < 0 || index >= this.Value.Waxes.Count) {
				this.waxDetails.gameObject.SetActive(false);
				this.waxDetails.Value = null;
				this.sequences.SetActive(false);
			} else {
				this.waxDetails.Value = this.Value.Waxes[index];
				this.waxDetails.gameObject.SetActive(true);
				this.sequences.SetActive(true);
			}

			this.PopulateSequenceList();
			this.OnSelectedSequenceChanged(this.sequenceList.value = 0);
		}

		public void DeleteWax() {
			int index = this.waxList.value;
			this.Value.Waxes.RemoveAt(index);

			this.OnDirty();

			this.PopulateWaxList();
			if (index >= this.Value.Waxes.Count) {
				this.waxList.value = index - 1;
			} else {
				this.OnSelectedWaxChanged(index);
			}
			this.addWax.interactable = true;
		}

		public void MoveWaxUp() {
			int index = this.waxList.value;
			DfWax.SubWax wax = this.Value.Waxes[index];
			this.Value.Waxes.RemoveAt(index);
			index--;
			this.Value.Waxes.Insert(index, wax);

			this.OnDirty();

			this.moveWaxUp.interactable = index > 0;
			this.moveWaxDown.interactable = true;

			this.PopulateWaxList();
			this.waxList.value = index;
		}

		public void MoveWaxDown() {
			int index = this.waxList.value;
			DfWax.SubWax wax = this.Value.Waxes[index];
			this.Value.Waxes.RemoveAt(index);
			index++;
			this.Value.Waxes.Insert(index, wax);

			this.OnDirty();

			this.moveWaxDown.interactable = index < this.Value.Waxes.Count - 1;
			this.moveWaxUp.interactable = true;

			this.PopulateWaxList();
			this.waxList.value = index;
		}

		public void AddWax() {
			int index = this.waxList.value;
			if (index < this.Value.Waxes.Count) {
				index++;
			}

			this.Value.Waxes.Insert(index, new DfWax.SubWax());

			this.OnDirty();

			this.PopulateWaxList();
			if (index != this.waxList.value) {
				this.waxList.value = index;
			} else {
				this.OnSelectedWaxChanged(index);
			}
			this.addWax.interactable = this.Value.Waxes.Count < 32;
		}

		private static readonly string[] sequenceLabels = new[] {
			"front",
			"",
			"front-front-left",
			"",
			"front-left",
			"",
			"left-front-left",
			"",
			"left",
			"",
			"left-back-left",
			"",
			"back-left",
			"",
			"back-back-left",
			"",
			"back",
			"",
			"back-back-right",
			"",
			"back-right",
			"",
			"right-back-right",
			"",
			"right",
			"",
			"right-front-right",
			"",
			"front-right",
			"",
			"front-front-right",
			""
		};

		private string GetSequenceLabel(int index) {
			if (index >= sequenceLabels.Length) {
				return $"{index} - unused";
			}

			return $"{index} - {sequenceLabels[index]}";
		}

		private void PopulateSequenceList() {
			this.sequenceList.ClearOptions();

			List<DfWax.SubWax> waxes = this.Value.Waxes;
			List<DfWax.Sequence> sequences;
			int waxIndex = this.waxList.value;
			if (waxIndex >= 0 && waxIndex < waxes.Count) {
				sequences = waxes[this.waxList.value].Sequences;
			} else {
				sequences = new();
			}

			this.sequenceList.AddOptions(sequences.Select((x, i) => new TMP_Dropdown.OptionData(this.GetSequenceLabel(i))).ToList());
		}

		public void OnSelectedSequenceChanged(int index) {
			int waxIndex = this.waxList.value;
			List<DfWax.SubWax> waxes = this.Value.Waxes;
			List<DfWax.Sequence> sequences;
			if (waxIndex >= 0 && waxIndex < waxes.Count) {
				sequences = waxes[this.waxList.value].Sequences;
			} else {
				sequences = new();
			}
			this.deleteSequence.interactable = sequences.Count > 0;
			this.moveSequenceUp.interactable = sequences.Count > 0 && index > 0;
			this.moveSequenceDown.interactable = sequences.Count > 0 && index < sequences.Count - 1;
			this.addSequence.interactable = sequences.Count < 32;

			this.frame.SetActive(index >= 0 && index < sequences.Count);

			this.PopulateFrame();
		}

		public void DeleteSequence() {
			int waxIndex = this.waxList.value;
			int index = this.sequenceList.value;
			this.Value.Waxes[waxIndex].Sequences.RemoveAt(index);

			this.OnDirty();

			this.PopulateSequenceList();
			if (index >= this.Value.Waxes[waxIndex].Sequences.Count) {
				this.sequenceList.value = index - 1;
				if (index - 1 < 0) {
					this.OnSelectedSequenceChanged(index);
				}
			} else {
				this.OnSelectedSequenceChanged(index);
			}
			this.addSequence.interactable = true;
		}

		public void MoveSequenceUp() {
			int waxIndex = this.waxList.value;
			int index = this.sequenceList.value;
			DfWax.Sequence sequence = this.Value.Waxes[waxIndex].Sequences[index];
			this.Value.Waxes[waxIndex].Sequences.RemoveAt(index);
			index--;
			this.Value.Waxes[waxIndex].Sequences.Insert(index, sequence);

			this.OnDirty();

			this.moveSequenceUp.interactable = index > 0;
			this.moveSequenceDown.interactable = true;

			this.PopulateSequenceList();
			this.sequenceList.value = index;
		}

		public void MoveSequenceDown() {
			int waxIndex = this.waxList.value;
			int index = this.sequenceList.value;
			DfWax.Sequence sequence = this.Value.Waxes[waxIndex].Sequences[index];
			this.Value.Waxes[waxIndex].Sequences.RemoveAt(index);
			index++;
			this.Value.Waxes[waxIndex].Sequences.Insert(index, sequence);

			this.OnDirty();

			this.moveSequenceDown.interactable = index < this.Value.Waxes[waxIndex].Sequences.Count - 1;
			this.moveSequenceUp.interactable = true;

			this.PopulateSequenceList();
			this.sequenceList.value = index;
		}

		public void AddSequence() {
			int waxIndex = this.waxList.value;
			List<DfWax.Sequence> sequences = this.Value.Waxes[waxIndex].Sequences;
			int index = this.sequenceList.value;
			if (index < sequences.Count) {
				index++;
			}

			sequences.Insert(index, new DfWax.Sequence());

			this.OnDirty();

			this.PopulateSequenceList();
			if (index != this.sequenceList.value) {
				this.sequenceList.value = index;
			} else {
				this.OnSelectedSequenceChanged(index);
			}
			this.addSequence.interactable = sequences.Count < 32;
		}

		private async Task<T> GetFileAsync<T>(string baseFile, string file) where T : File<T>, IDfFile, new() {
			if (Path.GetInvalidPathChars().Intersect(file).Any()) {
				return null;
			}

			if (baseFile != null) {
				string folder = Path.GetDirectoryName(baseFile);
				string path = Path.Combine(folder, file);
				return await DfFile.GetFileFromFolderOrContainerAsync<T>(path) ?? await ResourceCache.Instance.GetAsync<T>(file);
			}

			return await ResourceCache.Instance.GetAsync<T>(file);
		}

		public async Task RefreshPaletteAsync() {
			string palette = this.palette.text;
			if (string.IsNullOrWhiteSpace(palette)) {
				palette = "SECBASE";
			}
			palette = Path.Combine(Path.GetDirectoryName(palette), Path.GetFileNameWithoutExtension(palette));
			DfPalette pal = await this.GetFileAsync<DfPalette>(this.filePath, palette + ".PAL");
			DfColormap cmp = null;
			if (pal != null) {
				cmp = await this.GetFileAsync<DfColormap>(this.filePath, palette + ".CMP");
			}

			if (pal != null) {
				this.pal = pal;
				this.cmp = cmp;

				this.RefreshPreview();
			}

			DfFrame frame = null;
			List<DfWax.SubWax> waxes = this.Value.Waxes;
			int waxIndex = this.waxList.value;
			int seqIndex = this.sequenceList.value;
			if (waxIndex >= 0 && waxIndex < waxes.Count && seqIndex >= 0 && this.frameIndex >= 0) {
				List<DfWax.Sequence> sequences = waxes[waxIndex].Sequences;
				if (seqIndex < sequences.Count) {
					List<DfFrame> frames = sequences[seqIndex].Frames;
					if (this.frameIndex <= frames.Count) {
						frame = frames[this.frameIndex];
					}
				}
			}

			this.exportFrame.interactable = this.pal != null && frame != null;
			this.lightLevel.interactable = this.cmp != null;
		}
		public async void RefreshPaletteUnityAsync() => await this.RefreshPaletteAsync();

		private DfPalette pal;
		private DfColormap cmp;

		private int frameIndex = 0;
		private void PopulateFrame() {
			DfFrame frame = null;
			int frameIndex = this.frameIndex;
			int waxIndex = this.waxList.value;
			int seqIndex = this.sequenceList.value;
			int frameCount = 0;
			List<DfWax.SubWax> waxes = this.Value.Waxes;
			if (waxIndex >= 0 && seqIndex >= 0 && frameIndex >= 0 && waxIndex < waxes.Count) {
				List<DfWax.Sequence> sequences = waxes[waxIndex].Sequences;
				if (seqIndex < sequences.Count) {
					List<DfFrame> frames = sequences[seqIndex].Frames;
					frameCount = frames.Count;
					if (frameIndex <= frameCount && frameCount > 0) {
						frameIndex %= frameCount;
						frame = frames[frameIndex];
					}
				}
			}

			if (frame == null) {
				this.frameDetails.gameObject.SetActive(false);
				this.frameDetails.Value = null;

				this.playing = false;
				this.frameCounter.text = "0 / 0";

				this.playButton.interactable = false;
				this.playImage.gameObject.SetActive(true);
				this.pauseImage.gameObject.SetActive(false);
				this.frameSlider.interactable = false;
				this.frameSlider.maxValue = 0;
				this.frameSlider.value = 0;

				this.moveFrameUp.interactable = false;
				this.importFramePrev.interactable = true;
				this.importFrameNext.interactable = true;
				this.moveFrameDown.interactable = false;
				this.exportFrame.interactable = false;
				this.deleteFrame.interactable = false;
			} else {
				this.frameDetails.Value = frame;
				this.frameDetails.gameObject.SetActive(true);

				this.frameCounter.text = $"{frame.Width}x{frame.Height} | {frameIndex + 1} / {frameCount}";

				this.playButton.interactable = frameCount > 1;
				this.playImage.gameObject.SetActive(!this.playing);
				this.pauseImage.gameObject.SetActive(this.playing);
				this.frameSlider.maxValue = frameCount - 1;
				this.frameSlider.value = this.frameIndex;
				this.frameSlider.interactable = true;

				this.moveFrameUp.interactable = frameIndex > 0;
				this.importFramePrev.interactable = frameCount < 32;
				this.importFrameNext.interactable = frameCount < 32;
				this.moveFrameDown.interactable = frameIndex < frameCount - 1;
				this.exportFrame.interactable = this.pal != null;
				this.deleteFrame.interactable = true;
			}

			this.RefreshPreview();
		}

		public void RefreshPreview() {
			DfFrame frame = null;
			if (this.pal != null) {
				int waxIndex = this.waxList.value;
				int seqIndex = this.sequenceList.value;
				List<DfWax.SubWax> waxes = this.Value.Waxes;
				if (waxIndex >= 0 && seqIndex >= 0 && this.frameIndex >= 0 && waxIndex < waxes.Count) {
					List<DfWax.Sequence> sequences = waxes[waxIndex].Sequences;
					if (seqIndex < sequences.Count) {
						List<DfFrame> frames = sequences[seqIndex].Frames;
						if (frames.Count > 0) {
							int frameIndex = this.frameIndex % frames.Count;
							frame = this.Value.Waxes[waxIndex].Sequences[seqIndex].Frames[frameIndex];
						}
					}
				}
			}

			if (this.pal == null || frame == null || frame.Width == 0 || frame.Height == 0) {
				this.preview.sprite = null;
				this.preview.color = default;
			} else {
				Sprite sprite;
				if (this.cmp != null) {
					sprite = frame.ToSprite(this.pal, this.cmp, (int)this.lightLevel.value, false, false);
				} else {
					sprite = frame.ToSprite(this.pal, false);
				}

				this.preview.sprite = sprite;
				this.preview.color = Color.white;

				this.Thumbnail = sprite;
				this.ThumbnailChanged?.Invoke(this, new EventArgs());
			}
		}

		private bool playing;
		private float startTime;
		private int startFrame;
		private void Update() {
			if (this.playing) {
				int waxIndex = this.waxList.value;
				int seqIndex = this.sequenceList.value;
				int frameCount = this.Value.Waxes[waxIndex].Sequences[seqIndex].Frames.Count;
				if (frameCount > 0) {
					int framerate = this.Value.Waxes[waxIndex].Framerate;
					int frameIndex = ((int)(((Time.time - startTime) * framerate)) + startFrame) % frameCount;
					if (frameIndex != this.frameIndex) {
						this.frameIndex = frameIndex;
						this.PopulateFrame();
					}
				}
			}

			if (this.lookDelta != 0) {
				this.Look(this.lookDelta);
				this.lookDelta = 0;
			}

			if (this.pointerDown && !PlayerInput.all[0].currentActionMap.FindAction("Click").IsPressed()) {
				this.pointerDown = false;

				Cursor.visible = true;
				Cursor.lockState = CursorLockMode.None;
			}
		}

		public void Play() {
			if (this.playing) {
				this.playing = false;
			} else {
				this.startTime = Time.time;
				this.startFrame = this.frameIndex;
				this.playing = true;
			}
			this.PopulateFrame();
		}

		public void OnSliderValueChanged(float value) {
			int frameIndex = (int)value;
			if (frameIndex == this.frameIndex || frameIndex < 0) {
				return;
			}

			int frameCount = 0;
			List<DfWax.SubWax> waxes = this.Value.Waxes;
			int waxIndex = this.waxList.value;
			int seqIndex = this.sequenceList.value;
			if (waxIndex >= 0 && waxIndex < waxes.Count && seqIndex >= 0 && this.frameIndex >= 0) {
				List<DfWax.Sequence> sequences = waxes[waxIndex].Sequences;
				if (seqIndex < sequences.Count) {
					frameCount = sequences[seqIndex].Frames.Count;
				}
			}

			if (frameIndex >= frameCount) {
				return;
			}

			this.frameIndex = frameIndex;
			if (this.playing) {
				this.startTime = Time.time;
				this.startFrame = frameIndex;
			}
			this.PopulateFrame();
		}

		public void MoveFrameUp() {
			int waxIndex = this.waxList.value;
			int seqIndex = this.sequenceList.value;

			List<DfFrame> frames = this.Value.Waxes[waxIndex].Sequences[seqIndex].Frames;
			DfFrame frame = frames[this.frameIndex];
			frames.RemoveAt(this.frameIndex);
			this.frameIndex--;
			frames.Insert(this.frameIndex, frame);

			this.OnDirty();

			this.moveFrameUp.interactable = this.frameIndex > 0;
			this.moveFrameDown.interactable = true;

			this.PopulateFrame();
		}

		private string lastFolder;
		public async void ImportFramePrevAsync() {
			int index = this.frameIndex - 1;
			if (index < 0) {
				index = 0;
			}
			await this.ImportFrameAsync(index);
		}

		public async void ImportFrameNextAsync() {
			int index = this.frameIndex + 1;

			int waxIndex = this.waxList.value;
			int seqIndex = this.sequenceList.value;

			List<DfFrame> frames = this.Value.Waxes[waxIndex].Sequences[seqIndex].Frames;

			if (index >= frames.Count) {
				index = frames.Count;
			}
			await this.ImportFrameAsync(index);
		}

		public async Task ImportFrameAsync(int index) {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = true,
				AllowNavigateLfd = false,
				FileSearchPatterns = new[] { "*.FME", "*.BMP", "*.GIF", "*.PNG" },
				SelectButtonText = "Import",
				SelectedFileMustExist = true,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Import 8-bit FME, BMP, GIF, or PNG"
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			int waxIndex = this.waxList.value;
			int seqIndex = this.sequenceList.value;

			List<DfFrame> frames = this.Value.Waxes[waxIndex].Sequences[seqIndex].Frames;

			DfFrame frame;
			if (Path.GetExtension(path).ToLower() == ".fme") {
				try {
					frame = await DfFile.GetFileFromFolderOrContainerAsync<DfFrame>(path);
				} catch (Exception ex) {
					await DfMessageBox.Instance.ShowAsync($"Error reading FME: {ex.Message}");
					return;
				}
				if (frame == null) {
					await DfMessageBox.Instance.ShowAsync($"Error reading FME.");
					return;
				}
			} else {
				Bitmap bitmap;
				try {
					bitmap = BitmapLoader.LoadBitmap(path);
				} catch (Exception ex) {
					await DfMessageBox.Instance.ShowAsync($"Error reading file: {ex.Message}");
					return;
				}
				if (bitmap == null) {
					await DfMessageBox.Instance.ShowAsync($"Error reading file.");
					return;
				}
				using (bitmap) {
					if (!bitmap.PixelFormat.HasFlag(PixelFormat.Indexed)) {
						await DfMessageBox.Instance.ShowAsync($"Image must be 256 colors or less to import.");
						return;
					}

					frame = new() {
						Width = bitmap.Width,
						Height = bitmap.Height,
						Pixels = new byte[bitmap.Width * bitmap.Height]
					};

					BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
					for (int i = 0; i < bitmap.Height; i++) {
						Marshal.Copy(data.Scan0 + data.Stride * (bitmap.Height - i - 1), frame.Pixels, i * bitmap.Width, bitmap.Width);
					}
					bitmap.UnlockBits(data);
				}
			}

			frame.AutoCompress = this.autoCompress.isOn;
			frames.Insert(index, frame);

			this.OnDirty();

			this.frameIndex = index;
			this.PopulateFrame();
		}

		public void MoveFrameDown() {
			int waxIndex = this.waxList.value;
			int seqIndex = this.sequenceList.value;

			List<DfFrame> frames = this.Value.Waxes[waxIndex].Sequences[seqIndex].Frames;
			DfFrame frame = frames[this.frameIndex];
			frames.RemoveAt(this.frameIndex);
			this.frameIndex++;
			frames.Insert(this.frameIndex, frame);

			this.OnDirty();

			this.moveFrameDown.interactable = this.frameIndex < frames.Count - 1;
			this.moveFrameUp.interactable = true;

			this.PopulateFrame();
		}

		public async void ExportFrameAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = false,
				FileSearchPatterns = new[] { "*.PNG", "*.FME" },
				SelectButtonText = "Export",
				SelectedPathMustExist = true,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Export to 8-bit FME or PNG",
				ValidateFileName = true
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			int waxIndex = this.waxList.value;
			int seqIndex = this.sequenceList.value;

			DfFrame frame = this.Value.Waxes[waxIndex].Sequences[seqIndex].Frames[this.frameIndex];
			if (Path.GetExtension(path).ToLower() == ".fme") {
				using MemoryStream mem = new();
				await frame.SaveAsync(mem);

				mem.Position = 0;
				using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.None);
				await mem.CopyToAsync(stream);
				return;
			}

			byte[] bytePalette;
			if (this.cmp != null) {
				bytePalette = this.cmp.ToByteArray(this.pal, (int)this.lightLevel.value, true, false);
			} else {
				bytePalette = this.pal.ToByteArray(false);
			}

			using Bitmap bitmap = frame.ToBitmap(bytePalette);
			try {
				bitmap.Save(path);
			} catch (Exception ex) {
				await DfMessageBox.Instance.ShowAsync($"Error saving image: {ex.Message}");
			}
		}

		public void DeleteFrame() {
			int waxIndex = this.waxList.value;
			int seqIndex = this.sequenceList.value;

			List<DfFrame> frames = this.Value.Waxes[waxIndex].Sequences[seqIndex].Frames;
			frames.RemoveAt(this.frameIndex);

			this.OnDirty();

			this.moveFrameDown.interactable = this.frameIndex < frames.Count - 1;
			this.moveFrameUp.interactable = true;

			if (this.frameIndex >= frames.Count && this.frameIndex > 0) {
				this.frameIndex--;
			} 

			this.PopulateFrame();
		}

		public async void BrowsePaletteAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = true,
				AllowNavigateLfd = false,
				FileSearchPatterns = new[] { "*.PAL", "*.CMP" },
				SelectButtonText = "Select",
				SelectedFileMustExist = true,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Select palette"
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			path = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
			this.palette.text = path;
		}

		private bool pointerDown;

		protected override void Start() {
			base.Start();

			PlayerInput.all[0].actions["Look"].started += this.OnLook;
			PlayerInput.all[0].actions["Look"].performed += this.OnLook;
			PlayerInput.all[0].actions["Look"].canceled += this.OnLook;
		}

		private void OnDestroy() {
			if (PlayerInput.all.Count < 1) {
				return;
			}

			PlayerInput.all[0].actions["Look"].started -= this.OnLook;
			PlayerInput.all[0].actions["Look"].performed -= this.OnLook;
			PlayerInput.all[0].actions["Look"].canceled -= this.OnLook;
		}

		private float lookDelta;
		public void OnLook(InputAction.CallbackContext context) {
			if (!this.pointerDown) {
				return;
			}

			this.lookDelta = context.ReadValue<Vector2>().x;
		}

		private float lookFraction;
		private void Look(float value) {
			float delta = this.lookFraction + (this.previewSensitivity * Time.deltaTime * value);
			int intDelta = Mathf.RoundToInt(delta);
			this.lookFraction = delta - intDelta;
			if (intDelta == 0) {
				return;
			}

			int waxIndex = this.waxList.value;
			List<DfWax.Sequence> sequences = this.Value.Waxes[waxIndex].Sequences;
			int seqIndex = this.sequenceList.value;
			seqIndex = (seqIndex + intDelta) % sequences.Count;
			if (seqIndex < 0) {
				seqIndex += sequences.Count;
			}
			this.sequenceList.value = seqIndex;
		}

		public void ActivateLook() {
			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Confined;

			this.pointerDown = true;
		}
	}
}
