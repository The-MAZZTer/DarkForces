using CSharpSynth.Sequencer;
using CSharpSynth.Synthesis;
using MZZT.DarkForces.FileFormats;
using MZZT.FileFormats.Audio;
using System;
#if UNITY_WEBGL
using System.Diagnostics;
#endif
using System.IO;
#if UNITY_WEBGL
using System.Linq;
#endif
using System.Threading.Tasks;
using UnityEngine;

namespace MZZT.DarkForces {
  /// <summary>
  /// Plays level music in the background.
  /// No support for iMUSE yet. :(
  /// </summary>
  [RequireComponent(typeof(AudioSource))]
  public class LevelMusic : Singleton<LevelMusic> {
    [SerializeField, Header("Music Settings")]
    private string bankFilePath = "GM Bank/gm";

    private float[] sampleBuffer;
    private MidiSequencer midiSequencer;
    private StreamSynthesizer midiStreamSynthesizer;

		/// <summary>
		/// Whether or not the fight music should be played instead of the stalk music.
		/// </summary>
		public bool FightMusic { get; set; }

    /// <summary>
    /// Whether or not the music is currently playing.
    /// </summary>
    public bool IsPlaying { get; private set; }

		private void Start() {
			this.Init();
		}

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
				source.mute = this.GetComponent<AudioSource>().mute;
				return source;
			}).ToArray();
#endif
		}

		/// <summary>
		/// Play music.
		/// </summary>
		/// <param name="level">Which level music to play, starting at 0.</param>
		public async Task PlayAsync(int level) {
			this.Init();

      this.Stop();

			string filename = $"{(this.FightMusic ? "FIGHT" : "STALK")}-{level + 1:00}.GMD";
      DfGeneralMidi gmidi = await ResourceCache.Instance.GetGeneralMidi(filename);
      if (gmidi == null) {
        return;
      }

      // Music player can't handle GMIDI, convert to MIDI.
      Midi midi = gmidi.ToMidi();
			// Remove the weird chunks not standard to MIDI.
			midi.Chunks.Clear();
      using MemoryStream mem = new();
      await midi.SaveAsync(mem);
      mem.Position = 0;

			this.midiSequencer.LoadMidi(mem, 0, false);
			this.midiSequencer.Play();

			this.IsPlaying = true;
#if UNITY_WEBGL
			this.nextClipTime = AudioSettings.dspTime;
			this.nextClipOffset = 0;

			this.nextClip = this.GenerateClip();
#endif
		}

		/// <summary>
		/// Stop playback.
		/// </summary>
		public void Stop() {
      if (this.midiSequencer != null) {
				this.midiSequencer.Stop(true);
			}

			this.IsPlaying = false;
#if UNITY_WEBGL
			if (this.sources != null) {
				Array.Clear(this.sampleBuffer, 0, this.sampleBuffer.Length);
				foreach (AudioSource source in this.sources) {
					source.Stop();
					source.clip.SetData(this.sampleBuffer, 0);
				}
			}
			this.currentClip = null;
			this.nextClip = null;
			this.nextQueuedClip = null;
			this.nextClipTime = -1;
			this.nextQueuedClipTime = double.PositiveInfinity;
			this.nextSourceIndex = 0;
			this.nextClipOffset = 0;
#endif
		}

#if !UNITY_WEBGL
		private void OnAudioFilterRead(float[] data, int _) {
      if (!this.IsPlaying) {
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
		private double nextQueuedClipTime = double.PositiveInfinity;
		private double nextClipTime = -1;
		private AudioClip nextClip;
		private int nextSourceIndex = 0;
		private double nextClipOffset = 0;
#endif

		public bool PlayWhilePaused { get; set; } = true;

    private bool wasPaused = false;
		private void Update() {
      if (this.IsPlaying && !this.PlayWhilePaused) {
        bool isPaused = Time.deltaTime == 0;
        if (this.wasPaused != isPaused) {
          if (isPaused) {
            this.midiSequencer.Pause(true);
          } else {
            this.midiSequencer.Unpause();
          }
        }
        this.wasPaused = isPaused;
      }

#if UNITY_WEBGL
			if (!this.IsPlaying) {
				return;
			}

			if (this.nextQueuedClipTime == double.PositiveInfinity && (this.currentClip == null || AudioSettings.dspTime >= this.nextClipTime -
				this.currentClip.length / 3)) {

				this.nextQueuedClipTime = this.nextClipTime;
				this.nextQueuedClip = this.nextClip;

				AudioSource source = this.sources[this.nextSourceIndex];
				this.nextSourceIndex = (this.nextSourceIndex + 1) % this.sources.Length;
				if (AudioSettings.dspTime > this.nextClipTime) {
					//source.time = (float)(this.nextClipOffset + AudioSettings.dspTime - this.nextClipTime);
					source.time = (float)this.nextClipOffset;
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
				this.currentClip = this.nextQueuedClip;
				this.nextQueuedClipTime = double.PositiveInfinity;
				this.nextQueuedClip = null;
				this.nextClipOffset = 0;
			}
#endif
		}
	}
}
