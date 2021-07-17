using CSharpSynth.Synthesis;
using CSharpSynth.Wave;

namespace CSharpSynth.Banks {
	public abstract class Instrument
    {
		//--Virtual Methods
		public virtual float GetSampleAtTime(int note, int channel, int synthSampleRate, ref double time) => 0.0f;
		public virtual int GetAttack(int note) => SynthHelper.GetSampleFromTime(this.SampleRate, SynthHelper.DEFAULT_ATTACK);
		public virtual int GetRelease(int note) => SynthHelper.GetSampleFromTime(this.SampleRate, SynthHelper.DEFAULT_RELEASE);
		public virtual int GetHold(int note) => SynthHelper.GetSampleFromTime(this.SampleRate, SynthHelper.DEFAULT_HOLD);
		public virtual int GetDecay(int note) => SynthHelper.GetSampleFromTime(this.SampleRate, SynthHelper.DEFAULT_DECAY);
		//--Abstract Methods
		public abstract void EnforceSampleRate(int sampleRate);
        public abstract bool AllSamplesSupportDualChannel();
		//--Public Properties
		public string Name { get; set; }
		public Sample[] SampleList { get; set; }
		public int SampleRate { get; set; }
	}
}
