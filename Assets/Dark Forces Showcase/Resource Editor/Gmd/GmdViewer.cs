using CSharpSynth.Sequencer;
using CSharpSynth.Synthesis;
using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using MZZT.FileFormats;
using MZZT.FileFormats.Audio;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static MZZT.DarkForces.FileFormats.DfBitmap;

namespace MZZT.DarkForces.Showcase {
	public class GmdViewer : Databind<DfGeneralMidi>, IResourceViewer {
		[Header("GMD"), SerializeField]
		private Slider slider;
		[SerializeField]
		private TextMeshProUGUI playIcon;
		[SerializeField]
		private TextMeshProUGUI pauseIcon;
		[SerializeField]
		private TextMeshProUGUI noRepeatIcon;
		[SerializeField]
		private TextMeshProUGUI status;
		[SerializeField]
		private string statusFormat;
		[SerializeField]
		private Button prev;
		[SerializeField]
		private Button movePrev;
		[SerializeField]
		private TextMeshProUGUI trackCount;
		[SerializeField]
		private Button moveNext;
		[SerializeField]
		private Button next;
		[SerializeField]
		private Button export;
		[SerializeField]
		private Button delete;
		[SerializeField]
		private TMP_InputField mdpg;
		[SerializeField, Header("Music Settings")]
		private string bankFilePath = "GM Bank/gm";

		private float[] sampleBuffer;
		private MidiSequencer midiSequencer;
		private StreamSynthesizer midiStreamSynthesizer;

		public string TabName => this.filePath == null ? "New GMD" : Path.GetFileName(this.filePath);
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

		public bool IsDirty { get; private set; }
		public event EventHandler IsDirtyChanged;

		private string filePath;
		public async Task LoadAsync(ResourceEditorResource resource, IFile file) {
			this.filePath = resource?.Path;

			this.GetComponent<AudioSource>().spatialize = false;

			if (this.midiSequencer == null) {
				this.midiStreamSynthesizer = new StreamSynthesizer(44100, 2, 1024, 40);

				this.sampleBuffer = new float[this.midiStreamSynthesizer.BufferSize];

				this.midiStreamSynthesizer.LoadBank(this.bankFilePath);

				this.midiSequencer = new MidiSequencer(this.midiStreamSynthesizer) {
					Looping = true
				};
			}

			this.Value = (DfGeneralMidi)file;

			this.mdpg.text = string.Join("", this.Value.Mdpg.Select(x => x.ToString("X2")));

			if (this.Value.TrackData.Count > 0) {
				await this.LoadTrackAsync(0);
			} else {
				this.OnTrackChanged();
			}
		}

		private async Task LoadTrackAsync(int track) {
			this.Stop();

			// Music player can't handle GMIDI, convert to MIDI.
			Midi midi = this.Value.ToMidi();
			// Remove the weird chunks not standard to MIDI.
			midi.Chunks.Clear();
			using MemoryStream mem = new();
			await midi.SaveAsync(mem);
			mem.Position = 0;

			this.midiSequencer.LoadMidi(mem, track, false);

			this.track = track;
			this.OnTrackChanged();
		}
		private int track = -1;

		private void OnTrackChanged() {
			this.trackCount.text = $"Track {this.track + 1} / {this.Value.TrackData.Count}";
			this.prev.interactable = this.track > 0;
			this.next.interactable = this.track < this.Value.TrackData.Count - 1;
			this.movePrev.interactable = this.track > 0;
			this.moveNext.interactable = this.track < this.Value.TrackData.Count - 1;

			this.export.interactable = this.track >= 0;
			this.delete.interactable = this.track >= 0;
		}

		public async void PrevAsync() {
			bool isPlaying = this.midiSequencer.IsPlaying && !this.isPaused;

			await this.LoadTrackAsync(this.track - 1);

			if (isPlaying) {
				this.PlayPause();
			}
		}

		public async void NextAsync() {
			bool isPlaying = this.midiSequencer.IsPlaying && !this.isPaused;

			await this.LoadTrackAsync(this.track + 1);

			if (isPlaying) {
				this.PlayPause();
			}
		}

		public void MovePrevious() {
			if (this.track <= 0) {
				return;
			}

			byte[] swap = this.Value.TrackData[this.track];
			this.Value.TrackData[this.track] = this.Value.TrackData[this.track - 1];
			this.Value.TrackData[this.track - 1] = swap;

			this.OnDirty();

			this.track--;
			this.OnTrackChanged();
		}

		public void MoveNext() {
			if (this.track >= this.Value.TrackData.Count - 1) {
				return;
			}

			byte[] swap = this.Value.TrackData[this.track];
			this.Value.TrackData[this.track] = this.Value.TrackData[this.track + 1];
			this.Value.TrackData[this.track + 1] = swap;

			this.OnDirty();

			this.track++;
			this.OnTrackChanged();
		}

		private bool isPaused = false;
		public void PlayPause() {
			if (this.track < 0) {
				return;
			}

			if (this.isPaused) {
				this.midiSequencer.Unpause();
				this.isPaused = false;
			} else if (this.midiSequencer.IsPlaying) {
				this.midiSequencer.Pause(true);
				this.isPaused = true;
			} else {
				this.midiSequencer.Play();

				if (this.slider.value > 0) {
					this.midiSequencer.Time = this.midiSequencer.EndTime * this.slider.value;
				}
			}
		}

		private void OnAudioFilterRead(float[] data, int _) {
			if (!this.midiSequencer.IsPlaying) {
				Array.Clear(data, 0, data.Length);
				return;
			}

			this.midiStreamSynthesizer.GetNext(this.sampleBuffer);

			Buffer.BlockCopy(this.sampleBuffer, 0, data, 0, sizeof(float) * data.Length);
		}

		public void Stop() {
			this.midiSequencer.Stop(true);
			this.isPaused = false;

			this.slider.value = 0;
		}

		private bool userInput = true;
		private void Update() {
			if (this.track >= 0) {
				int len = this.midiSequencer.EndSampleTime;
				int pos = this.midiSequencer.SampleTime;

				if (this.midiSequencer.IsPlaying && !this.sliderMouseDown) {
					this.userInput = false;
					try {
						this.slider.value = ((float)pos / len);
					} finally {
						this.userInput = true;
					}
				}

				this.status.text = string.Format(this.statusFormat, this.midiSequencer.Time, this.midiSequencer.EndTime);
				this.playIcon.gameObject.SetActive(!this.midiSequencer.IsPlaying || this.isPaused);
				this.pauseIcon.gameObject.SetActive(this.midiSequencer.IsPlaying && !this.isPaused);
			}

			if (!PlayerInput.all[0].currentActionMap.FindAction("Click").IsPressed()) {
				this.sliderMouseDown = false;
			}
		}

		private bool sliderMouseDown = false;
		public void OnSliderMouseDown() {
			this.sliderMouseDown = true;
		}

		public void OnSliderValueChanged(float value) {
			if (!this.userInput) {
				return;
			}

			this.midiSequencer.Time = this.midiSequencer.EndTime * value;
		}

		public void OnRepeatChanged(bool value) {
			this.noRepeatIcon.gameObject.SetActive(!value);

			this.midiSequencer.Looping = value;
		}

		private string lastFolder;

		public async void ImportPreviousAsync() {
			if (this.track < 0) {
				this.track = 0;
			}
			await this.ImportAsync(track);
		}

		public async void ImportNextAsync() {
			await this.ImportAsync(this.track + 1);
		}

		public async Task ImportAsync(int track) {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = true,
				AllowNavigateLfd = false,
				FileSearchPatterns = new[] { "*.MID" },
				SelectButtonText = "Import",
				SelectedFileMustExist = true,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Import MIDI"
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			Midi midi = await Midi.ReadAsync(path);
			if (this.Value.TrackData.Count == 0) {
				this.Value.Format = midi.Format;
				this.Value.Tempo = midi.Tempo;
				this.Value.TempoIsFramesPerSecond = midi.TempoIsFramesPerSecond;
			}

			this.Value.TrackData.InsertRange(track, midi.TrackData);

			this.OnDirty();

			await this.LoadTrackAsync(track);
		}

		public async void Delete() {
			if (this.track < 0) {
				return;
			}

			this.Value.TrackData.RemoveAt(this.track);

			this.OnDirty();

			if (this.track >= this.Value.TrackData.Count) {
				this.track--;
			}

			if (this.track >= 0) {
				await this.LoadTrackAsync(this.track);
			} else {
				this.OnTrackChanged();
			}
		}

		public async void ExportAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = false,
				FileSearchPatterns = new[] { "*.MID" },
				SelectButtonText = "Export",
				SelectedPathMustExist = true,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Export to MIDI",
				ValidateFileName = true
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			Midi midi = this.Value.ToMidi();
			midi.Chunks.Clear();
			try {
				await midi.SaveAsync(path);
			} catch (Exception ex) {
				await DfMessageBox.Instance.ShowAsync($"Error saving MIDI: {ex.Message}");
			}
		}

		public async void ExportTrackAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = false,
				FileSearchPatterns = new[] { "*.MID" },
				SelectButtonText = "Export",
				SelectedPathMustExist = true,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Export track to MIDI",
				ValidateFileName = true
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			Midi midi = this.Value.ToMidi();
			for (int i = midi.TrackData.Count - 1; i >= 0; i--) {
				if (i != this.track) {
					midi.TrackData.RemoveAt(i);
				}
			}
			midi.Chunks.Clear();
			try {
				await midi.SaveAsync(path);
			} catch (Exception ex) {
				await DfMessageBox.Instance.ShowAsync($"Error saving MIDI: {ex.Message}");
			}
		}

		public void OnMdpgChanged(string text) {
			text = new Regex("[^0-9A-F]+", RegexOptions.IgnoreCase | RegexOptions.Compiled).Replace(text, "");
			List<byte> bytes = new();
			for (int i = 0; i < text.Length - 1; i += 2) {
				if (!byte.TryParse(text.Substring(i, 2), NumberStyles.HexNumber, null, out byte b)) {
					return;
				}

				bytes.Add(b);
			}
			this.Value.Mdpg = bytes.ToArray();

			this.OnDirty();
		}

		public void OnMdpgEndEdit(string text) {
			text = new Regex("[^0-9A-F]+", RegexOptions.IgnoreCase | RegexOptions.Compiled).Replace(text, "");
			this.mdpg.text = text;
		}

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
			string path = await ResourceEditors.Instance.PickSaveLocationAsync(this.filePath, new[] { "*.GMD", "*.GMID" });
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
