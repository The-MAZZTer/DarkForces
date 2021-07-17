using CSharpSynth.Synthesis;

namespace CSharpSynth.Banks.Analog {
  public class AnalogInstrument : Instrument {
    private int _decay;
    private int _hold;
    //--Public Properties
    public int Attack { get; set; }
    public int Release { get; set; }
    public SynthHelper.WaveFormType WaveForm { get; set; }
    //--Public Methods
    public AnalogInstrument(SynthHelper.WaveFormType waveformtype, int sampleRate)
            : base() {
      //set type
      this.WaveForm = waveformtype;
      this.SampleRate = sampleRate;
      //Proper calculation of voice states
      this.Attack = SynthHelper.GetSampleFromTime(sampleRate, SynthHelper.DEFAULT_ATTACK);
      this.Release = SynthHelper.GetSampleFromTime(sampleRate, SynthHelper.DEFAULT_RELEASE);
      this._decay = SynthHelper.GetSampleFromTime(sampleRate, SynthHelper.DEFAULT_DECAY);
      this._hold = SynthHelper.GetSampleFromTime(sampleRate, SynthHelper.DEFAULT_HOLD);
      //set base attribute name
      this.Name = waveformtype.ToString();
    }
    public override bool AllSamplesSupportDualChannel() => false;
    public override void EnforceSampleRate(int sampleRate) {
      if (sampleRate != this.SampleRate) {
        //Proper calculation of voice states
        this.Attack = SynthHelper.GetSampleFromTime(sampleRate, SynthHelper.DEFAULT_ATTACK);
        this.Release = SynthHelper.GetSampleFromTime(sampleRate, SynthHelper.DEFAULT_RELEASE);
        this._decay = SynthHelper.GetSampleFromTime(sampleRate, SynthHelper.DEFAULT_DECAY);
        this._hold = SynthHelper.GetSampleFromTime(sampleRate, SynthHelper.DEFAULT_HOLD);
        this.SampleRate = sampleRate;
      }
    }
    public override int GetAttack(int note) => this.Attack;
    public override int GetRelease(int note) => this.Release;
    public override int GetDecay(int note) => this._decay;
    public override int GetHold(int note) => this._hold;
    public override float GetSampleAtTime(int note, int channel, int synthSampleRate, ref double time) {
      double freq = SynthHelper.NoteToFrequency(note);
      if (freq * time > 1.0) {
        time = 0.0;
      }

      switch (this.WaveForm) {
        case SynthHelper.WaveFormType.Sine:
          return SynthHelper.Sine(freq, time) * SynthHelper.DEFAULT_AMPLITUDE;
        case SynthHelper.WaveFormType.Sawtooth:
          return SynthHelper.Sawtooth(freq, time) * SynthHelper.DEFAULT_AMPLITUDE;
        case SynthHelper.WaveFormType.Square:
          return SynthHelper.Square(freq, time) * SynthHelper.DEFAULT_AMPLITUDE;
        case SynthHelper.WaveFormType.Triangle:
          return SynthHelper.Triangle(freq, time) * SynthHelper.DEFAULT_AMPLITUDE;
        case SynthHelper.WaveFormType.WhiteNoise:
          return SynthHelper.WhiteNoise(note, time) * SynthHelper.DEFAULT_AMPLITUDE;
        default:
          return 0.0f;
      }
    }
  }
}
