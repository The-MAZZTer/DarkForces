using CSharpSynth.Sequencer;
using CSharpSynth.Synthesis;
using MZZT.DarkForces.FileFormats;
using MZZT.FileFormats.Audio;
using System;
using System.IO;
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

    private void Start() {
      this.GetComponent<AudioSource>().spatialize = false;

      this.midiStreamSynthesizer = new StreamSynthesizer(44100, 2, 1024, 40);
      this.sampleBuffer = new float[this.midiStreamSynthesizer.BufferSize];

      this.midiStreamSynthesizer.LoadBank(this.bankFilePath);

      this.midiSequencer = new MidiSequencer(this.midiStreamSynthesizer) {
        Looping = true
      };
    }

    /// <summary>
    /// Whether or not the fight music should be played instead of the stalk music.
    /// </summary>
    public bool FightMusic { get; set; }

    /// <summary>
    /// Whether or not the music is currently playing.
    /// </summary>
    public bool IsPlaying { get; private set; }

    /// <summary>
    /// Play music.
    /// </summary>
    /// <param name="level">Which level music to play, starting at 0.</param>
    public async Task PlayAsync(int level) {
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
    }

    /// <summary>
    /// Stop playback.
    /// </summary>
    public void Stop() {
      this.midiSequencer.Stop(true);
      this.IsPlaying = false;
    }

    private void OnAudioFilterRead(float[] data, int _) {
      if (!this.IsPlaying) {
        Array.Clear(data, 0, data.Length);
        return;
			}

      this.midiStreamSynthesizer.GetNext(this.sampleBuffer);

      Buffer.BlockCopy(this.sampleBuffer, 0, data, 0, sizeof(float) * data.Length);
    }

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
    }
	}
}
