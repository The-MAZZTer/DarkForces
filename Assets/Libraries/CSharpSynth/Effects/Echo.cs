using CSharpSynth.Synthesis;
using System;

namespace CSharpSynth.Effects {
  public class Echo : BasicAudioEffect {
    //--Variables
    private readonly int channels;
    private readonly int secondarybufferlen;
    private int secondaryposition;
    private float decay;
    //--Public Properties
    public float Decay {
      get => this.decay;
      set => this.decay = SynthHelper.Clamp(value, 0.0f, 1.0f);
    }
    //--Public Methods
    /// <summary>
    /// A simple echo effect.
    /// </summary>
    /// <param name="synth">A constructed synthesizer instance.</param>
    /// <param name="delaytime">Echo delay in seconds.</param>
    /// <param name="decay">Controls the volume of the echo.</param>
    public Echo(StreamSynthesizer synth, float delaytime, float decay)
            : base() {
      if (delaytime <= 0.0f) {
        throw new ArgumentException("delay time must be positive non-zero for echo effect.");
      }

      this.decay = SynthHelper.Clamp(decay, 0.0f, 1.0f);
      this.EffectBuffer = new float[synth.Channels, SynthHelper.GetSampleFromTime(synth.SampleRate, delaytime)];
      this.channels = this.EffectBuffer.GetLength(0);
      this.secondarybufferlen = this.EffectBuffer.GetLength(1);
    }
    public void ResetEcho() {
      this.secondaryposition = 0;
      Array.Clear(this.EffectBuffer, 0, this.secondarybufferlen * this.channels);
    }
    public override void DoEffect(float[,] inputBuffer) {
      int primarybufferlen = inputBuffer.GetLength(1);
      for (int counter = 0; counter < primarybufferlen; counter++) {
        for (int x = 0; x < this.channels; x++) {
          float mixed = inputBuffer[x, counter] + this.decay * this.EffectBuffer[x, this.secondaryposition];
          this.EffectBuffer[x, this.secondaryposition] = mixed;
          inputBuffer[x, counter] = mixed;
        }
        this.secondaryposition++;
        if (this.secondaryposition == this.secondarybufferlen) {
          this.secondaryposition = 0;
        }
      }
    }
  }
}
