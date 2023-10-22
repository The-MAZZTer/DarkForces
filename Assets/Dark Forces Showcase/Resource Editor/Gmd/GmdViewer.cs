using CSharpSynth.Sequencer;
using CSharpSynth.Synthesis;
using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using MZZT.FileFormats;
using MZZT.FileFormats.Audio;
using MZZT.IO.FileProviders;
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

		private void Init() {
			if (this.midiSequencer != null) {
				return;
			}

			this.GetComponent<AudioSource>().spatialize = false;

#if !UNITY_WEBGL
			this.midiStreamSynthesizer = new StreamSynthesizer(44100, 2, 1024, 40);
#else
			this.midiStreamSynthesizer = new StreamSynthesizer(44100, 2, 102400, 40);
#endif
			this.sampleBuffer = new float[this.midiStreamSynthesizer.BufferSize];

			this.midiStreamSynthesizer.LoadBank(this.bankFilePath);

			this.midiSequencer = new MidiSequencer(this.midiStreamSynthesizer) {
				Looping = true
			};

#if UNITY_WEBGL
			this.sources = Enumerable.Range(0, 2).Select(x => {
				GameObject child = new() {
					name = "AudioSource"
				};
				child.transform.SetParent(this.transform, false);
				AudioSource source = child.AddComponent<AudioSource>();
				source.playOnAwake = false;
				source.spatialBlend = 0;
				source.spatialize = false;
				source.volume = this.GetComponent<AudioSource>().volume;
				source.clip = AudioClip.Create(x.ToString(), this.sampleBuffer.Length, 2, 44100, false);
				return source;
			}).ToArray();
#endif
		}

		protected override void Start() {
			this.Init();

			base.Start();
		}

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

			this.Value = (DfGeneralMidi)file;

			this.mdpg.text = string.Join("", this.Value.Mdpg.Select(x => x.ToString("X2")));

			this.Init();

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

			(this.Value.TrackData[this.track - 1], this.Value.TrackData[this.track]) = (this.Value.TrackData[this.track], this.Value.TrackData[this.track - 1]);
			this.OnDirty();

			this.track--;
			this.OnTrackChanged();
		}

		public void MoveNext() {
			if (this.track >= this.Value.TrackData.Count - 1) {
				return;
			}

			(this.Value.TrackData[this.track + 1], this.Value.TrackData[this.track]) = (this.Value.TrackData[this.track], this.Value.TrackData[this.track + 1]);
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

#if UNITY_WEBGL
				if (this.pauseTime == 0) {
					return;
				}
				double delta = AudioSettings.dspTime - this.pauseTime;
				this.nextClipTime += delta;
				this.nextQueuedClipTime += delta;
				this.currentClipStartTime += delta;
				this.pauseTime = 0;
				foreach (AudioSource source in this.sources) {
					source.UnPause();
				}
#endif
			} else if (this.midiSequencer.IsPlaying) {
				this.midiSequencer.Pause(true);
				this.isPaused = true;

#if UNITY_WEBGL
				if (this.pauseTime > 0) {
					return;
				}
				foreach (AudioSource source in this.sources) {
					source.Pause();
				}
				this.pauseTime = AudioSettings.dspTime;
#endif
			} else {
				this.midiSequencer.Play();

				if (this.slider.value > 0) {
					this.midiSequencer.Time = this.midiSequencer.EndTime * this.slider.value;
				}

#if UNITY_WEBGL
				this.currentClipStartPos = 0;
				this.currentClipStartTime = AudioSettings.dspTime;
				this.nextClipTime = AudioSettings.dspTime;
				this.nextClipOffset = 0;

				this.nextClip = this.GenerateClip();
#endif
			}
		}

#if !UNITY_WEBGL
		private void OnAudioFilterRead(float[] data, int _) {
			if (!this.midiSequencer.IsPlaying) {
				Array.Clear(data, 0, data.Length);
				return;
			}

			this.midiStreamSynthesizer.GetNext(this.sampleBuffer);

			Buffer.BlockCopy(this.sampleBuffer, 0, data, 0, sizeof(float) * data.Length);
		}
#else
		private AudioClip GenerateClip() {
			this.midiStreamSynthesizer.GetNext(this.sampleBuffer);

			AudioClip clip = this.sources[(this.nextSourceIndex + 1) % this.sources.Length].clip;
			clip.SetData(this.sampleBuffer, 0);

			return clip;
		}

		private AudioSource[] sources;
		private AudioClip currentClip;
		private AudioClip nextQueuedClip;
		private double currentClipStartPos = -1;
		private double currentClipStartTime = -1;
		private double nextQueuedClipTime = double.PositiveInfinity;
		private double nextClipTime = -1;
		private AudioClip nextClip;
		private int nextSourceIndex = 0;
		private double nextClipOffset = 0;
		private double pauseTime;
#endif

		public void Stop() {
			this.midiSequencer.Stop(true);
			this.isPaused = false;

			this.slider.value = 0;
#if UNITY_WEBGL
			if (this.sources != null) {
				foreach (AudioSource source in this.sources) {
					source.Stop();
				}
			}
			this.currentClip = null;
			this.nextClip = null;
			this.nextQueuedClip = null;
			this.nextClipTime = -1;
			this.nextQueuedClipTime = double.PositiveInfinity;
			this.currentClipStartTime = -1;
			this.currentClipStartPos = -1;
			this.pauseTime = 0;
#endif
		}

		private bool userInput = true;
		private void Update() {
			if (this.track >= 0) {
#if !UNITY_WEBGL
				int len = this.midiSequencer.EndSampleTime;
				int pos = this.midiSequencer.SampleTime;
#else
				double len = this.midiSequencer.EndTime.TotalSeconds;
				double blockTime = 0;
				if (this.currentClipStartTime >= 0) {
					if (this.pauseTime > 0) {
						blockTime = this.pauseTime - this.currentClipStartTime;
					} else {
						blockTime = AudioSettings.dspTime - this.currentClipStartTime;
					}
				}
				double pos = this.currentClipStartPos < 0 ? 0 : (this.currentClipStartPos + blockTime);
#endif

				if (this.midiSequencer.IsPlaying && !this.sliderMouseDown) {
					this.userInput = false;
					try {
#if !UNITY_WEBGL
						this.slider.value = ((float)pos / len);
#else
						this.slider.value = (float)(pos / len);
#endif
					} finally {
						this.userInput = true;
					}
				}

#if !UNITY_WEBGL
				this.status.text = string.Format(this.statusFormat, this.midiSequencer.Time, this.midiSequencer.EndTime);
#else
				this.status.text = string.Format(this.statusFormat, TimeSpan.FromSeconds(pos), this.midiSequencer.EndTime);
#endif
				this.playIcon.gameObject.SetActive(!this.midiSequencer.IsPlaying || this.isPaused);
				this.pauseIcon.gameObject.SetActive(this.midiSequencer.IsPlaying && !this.isPaused);
			}

			if (!PlayerInput.all[0].currentActionMap.FindAction("Click").IsPressed()) {
				this.sliderMouseDown = false;
			}

#if UNITY_WEBGL
			bool isPlaying = this.midiSequencer.IsPlaying && !this.isPaused;
			if (!isPlaying) {
				return;
			}

			if (this.nextQueuedClipTime == double.PositiveInfinity && (this.currentClip == null || AudioSettings.dspTime >= this.nextClipTime -
				this.currentClip.length / 3)) {

				this.nextQueuedClipTime = this.nextClipTime;
				this.nextQueuedClip = this.nextClip;

				AudioSource source = this.sources[this.nextSourceIndex];
				this.nextSourceIndex = (this.nextSourceIndex + 1) % this.sources.Length;
				if (AudioSettings.dspTime > this.nextClipTime) {
					source.time = (float)(this.nextClipOffset + AudioSettings.dspTime - this.nextClipTime);
					source.Play();
				} else {
					source.time = (float)this.nextClipOffset;
					source.PlayScheduled(this.nextClipTime);
				}

#if UNITY_EDITOR
				this.nextClipTime += this.nextClip.length / 4;
#else
				this.nextClipTime += this.nextClip.length / 2;
#endif
				this.nextClip = this.GenerateClip();
			}

			if (AudioSettings.dspTime >= this.nextQueuedClipTime) {
				if (this.currentClip != null) {
#if UNITY_EDITOR
					this.currentClipStartPos += this.currentClip.length / 4;
#else
					this.currentClipStartPos += this.currentClip.length / 2;
#endif
				}
				this.currentClipStartPos %= this.midiSequencer.EndTime.TotalSeconds;
				this.currentClip = this.nextQueuedClip;
				this.currentClipStartTime = this.nextQueuedClipTime - this.nextClipOffset;
				this.nextQueuedClipTime = double.PositiveInfinity;
				this.nextQueuedClip = null;
				this.nextClipOffset = 0;
			}
#endif
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
#if UNITY_WEBGL
			this.currentClipStartTime = AudioSettings.dspTime;
			this.currentClipStartPos = (this.midiSequencer.EndTime * value).TotalSeconds;
			this.nextClipTime = AudioSettings.dspTime;
			this.nextClipOffset = 0;
			this.nextClip = this.GenerateClip();
#endif
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
				AllowNavigateGob = false,
				AllowNavigateLfd = false,
				Filters = new[] {
					FileBrowser.FileType.Generate("MIDI Files", "*.MID"),
					FileBrowser.FileType.AllFiles
				},
				SelectButtonText = "Import",
				SelectedFileMustExist = true,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Import MIDI"
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			Midi midi = await DfFileManager.Instance.ReadAsync<Midi>(path);
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
				Filters = new[] {
					FileBrowser.FileType.Generate("MIDI File", "*.MID"),
					FileBrowser.FileType.AllFiles
				},
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
				await DfFileManager.Instance.SaveAsync(midi, path);
			} catch (Exception ex) {
				await DfMessageBox.Instance.ShowAsync($"Error saving MIDI: {ex.Message}");
			}
		}

		public async void ExportTrackAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = false,
				AllowNavigateLfd = false,
				Filters = new[] {
					FileBrowser.FileType.Generate("MIDI File", "*.MID"),
					FileBrowser.FileType.AllFiles
				},
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
				await DfFileManager.Instance.SaveAsync(midi, path);
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
			bool canSave = FileManager.Instance.FolderExists(Path.GetDirectoryName(this.filePath));
			if (!canSave) {
				this.SaveAsAsync();
				return;
			}

			// Writing to the stream is loads faster than to the file. Not sure why. Unity thing probably, doesn't happen on .NET 6.
			using Stream stream = await FileManager.Instance.NewFileStreamAsync(this.filePath, FileMode.Create, FileAccess.Write, FileShare.None);
			if (stream is FileStream) {
				using MemoryStream mem = new();
				await this.Value.SaveAsync(mem);
				mem.Position = 0;
				await mem.CopyToAsync(stream);
			} else {
				await this.Value.SaveAsync(stream);
			}

			this.ResetDirty();
		}

		public async void SaveAsAsync() {
			string path = await ResourceEditors.Instance.PickSaveLocationAsync(this.filePath, new[] { "*.GMD", "*.GMID" });
			if (string.IsNullOrEmpty(path)) {
				return;
			}
			this.filePath = path;
			this.TabNameChanged?.Invoke(this, new EventArgs());

			bool canSave = FileManager.Instance.FolderExists(Path.GetDirectoryName(this.filePath));
			if (!canSave) {
				return;
			}

			this.SaveAsync();
		}
	}
}
