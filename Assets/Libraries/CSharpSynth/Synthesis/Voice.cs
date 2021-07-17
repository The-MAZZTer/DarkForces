using CSharpSynth.Banks;
using System;

namespace CSharpSynth.Synthesis {
  public class Voice {
    //--Variables
    private Instrument inst;
    //voice parameters
    private int note;
    private int velocity;
    private int attack;
    private int release;
    private int hold;
    private int decay;
    private int channel;
    private float pan;
    private float rightpan;
    private float leftpan;
    private double variableSampleRate;
    private VoiceState state;
    private readonly StreamSynthesizer synth;
    private double time;
    private float fadeMultiplier;
    private int fadeCounter;
    private int decayCounter;
    private readonly float gainControl = .3f;
    //--Enum
    private enum VoiceState { None, Attack, Sustain, Hold, Release }
    //--Public Methods
    public Voice(StreamSynthesizer synth) {
      this.ResetVoice();
      this.synth = synth;
      this.inst = null;
    }
    public Voice(StreamSynthesizer synth, Instrument inst) {
      this.ResetVoice();
      this.synth = synth;
      this.SetInstrument(inst);
    }
    public void SetInstrument(Instrument inst) {
      if (this.inst != inst) {
        this.inst = inst;
      }
    }
    public Instrument GetInstrument() => this.inst;
    public void Start(int channel, int note, int velocity) {
      this.note = note;
      this.velocity = velocity;
      this.channel = channel;
      this.time = 0.0;
      this.fadeMultiplier = 1.0f;
      this.decayCounter = 0;
      this.fadeCounter = 0;

      //Set note parameters in samples
      this.attack = this.inst.GetAttack(note);
      this.release = this.inst.GetRelease(note);
      this.hold = this.inst.GetHold(note);
      this.decay = this.inst.GetDecay(note);

      //Set counters and initial state
      this.decayCounter = this.decay;
      if (this.attack == 0) {
        this.state = VoiceState.Sustain;
      } else {
        this.state = VoiceState.Attack;
        this.fadeCounter = this.attack;
      }
      this.IsInUse = true;
    }
    public void Stop() {
      if (this.hold == 0) {
        if (this.release == 0) {
          this.state = VoiceState.None;
          this.IsInUse = false;
        } else {
          this.state = VoiceState.Release;
          this.fadeCounter = this.release;
        }
      } else {
        this.state = VoiceState.Hold;
        this.fadeCounter = this.hold;
      }
    }
    public void StopImmediately() {
      this.state = VoiceState.None;
      this.IsInUse = false;
    }
    public bool IsInUse { get; private set; }
    public void SetPan(float pan) {
      if (pan >= -1.0f && pan <= 1.0f && this.pan != pan) {
        this.pan = pan;
        if (pan > 0.0f) {
          this.rightpan = 1.00f;
          this.leftpan = 1.00f - pan;
        } else {
          this.leftpan = 1.0f;
          this.rightpan = 1.00f + pan;
        }
      }
    }
    public float GetPan() => this.pan;
    public NoteRegistryKey GetKey() => new NoteRegistryKey((byte)this.channel, (byte)this.note);
    public void Process(float[,] workingBuffer, int startIndex, int endIndex) {
      if (this.IsInUse) {
        //quick checks to do before we go through our main loop
        if (this.synth.Channels == 2 && this.pan != this.synth.PanPositions[this.channel]) {
          this.SetPan(this.synth.PanPositions[this.channel]);
        }
        //set sampleRate for tune
        this.variableSampleRate = this.synth.SampleRate * Math.Pow(2.0, (this.synth.TunePositions[this.channel] * -1.0) / 12.0);
        //main loop
        for (int i = startIndex; i < endIndex; i++) {
          //manage states and calculate volume level
          switch (this.state) {
            case VoiceState.Attack:
              this.fadeCounter--;
              if (this.fadeCounter <= 0) {
                this.state = VoiceState.Sustain;
                this.fadeMultiplier = 1.0f;
              } else {
                this.fadeMultiplier = 1.0f - (this.fadeCounter / (float)this.attack);
              }
              break;
            case VoiceState.Sustain:
              this.decayCounter--;
              if (this.decayCounter <= 0) {
                this.state = VoiceState.None;
                this.IsInUse = false;
                this.fadeMultiplier = 0.0f;
              } else {
                this.fadeMultiplier = this.decayCounter / (float)this.decay;
              }
              break;
            case VoiceState.Hold:
              this.fadeCounter--;//not used for volume
              this.decayCounter--;
              if (this.decayCounter <= 0) {
                this.state = VoiceState.None;
                this.IsInUse = false;
                this.fadeMultiplier = 0.0f;
              } else if (this.fadeCounter <= 0) {
                this.state = VoiceState.Release;
                this.fadeCounter = this.release;
              } else {
                this.fadeMultiplier = this.decayCounter / (float)this.decay;
              }
              break;
            case VoiceState.Release:
              this.fadeCounter--;
              if (this.fadeCounter <= 0) {
                this.state = VoiceState.None;
                this.IsInUse = false;
              } else {//Multiply decay with fadeout so volume doesn't suddenly rise when releasing notes
                this.fadeMultiplier = (this.decayCounter / (float)this.decay) * (this.fadeCounter / (float)this.release);
              }
              break;
          }
          //end of state management
          //Decide how to sample based on channels available

          //mono output
          if (this.synth.Channels == 1) {
            float sample = this.inst.GetSampleAtTime(this.note, 0, this.synth.SampleRate, ref this.time);
            sample = sample * (this.velocity / 127.0f) * this.synth.VolPositions[this.channel];
            workingBuffer[0, i] += (sample * this.fadeMultiplier * this.gainControl);
          }
          //mono sample to stereo output
          else if (this.synth.Channels == 2 && this.inst.AllSamplesSupportDualChannel() == false) {
            float sample = this.inst.GetSampleAtTime(this.note, 0, this.synth.SampleRate, ref this.time);
            sample = sample * (this.velocity / 127.0f) * this.synth.VolPositions[this.channel];

            workingBuffer[0, i] += (sample * this.fadeMultiplier * this.leftpan * this.gainControl);
            workingBuffer[1, i] += (sample * this.fadeMultiplier * this.rightpan * this.gainControl);
          }
          //both support stereo
          else {

          }
          this.time += 1.0 / this.variableSampleRate;
          //bailout of the loop if there is no reason to continue.
          if (this.IsInUse == false) {
            return;
          }
        }
      }
    }
    //--Private Methods
    private void ResetVoice() {
      this.IsInUse = false;
      this.state = VoiceState.None;
      this.note = 0;
      this.time = 0.0;
      this.fadeMultiplier = 1.0f;
      this.decayCounter = 0;
      this.fadeCounter = 0;
      this.pan = 0.0f;
      this.channel = 0;
      this.rightpan = 1.0f;
      this.leftpan = 1.0f;
      this.velocity = 127;
    }
  }
}
