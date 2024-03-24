using CSharpSynth.Banks;
using CSharpSynth.Effects;
using CSharpSynth.Sequencer;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CSharpSynth.Synthesis {
  public class StreamSynthesizer {
    private float[,] sampleBuffer;
    private Voice[] voicePool;
    private LinkedList<Voice> activeVoices;
    private Stack<Voice> freeVoices;
    private Dictionary<NoteRegistryKey, List<Voice>> keyRegistry;
    private MidiSequencer seq;
    private List<BasicAudioEffect> effects;
    private int samplesperBuffer = 2000;
    private int polyphony = 40; //total number of voices available
                                //Tweakable Parameters, anytime via properties

    private float MainVolume = 1.0f; //Not too high or will cause clipping
                                     //--Public Properties
    public int BufferSize { get; private set; }
    public float[] PanPositions { get; private set; }
    public float[] VolPositions { get; private set; }
    public double[] TunePositions { get; private set; }
    public int MaxPolyPerNote { get; set; } = 2;
    public float MasterVolume {
      get => this.MainVolume;
      set => this.MainVolume = SynthHelper.Clamp(value, 0.0f, 1.0f);
    }
    public int SampleRate { get; private set; } = 44100;
    public int Channels { get; private set; } = 1;
    public InstrumentBank SoundBank { get; private set; }
    //--Public Methods
    //public StreamSynthesizer(int sampleRate, int audioChannels, int bufferSizeInMilliseconds, int maxpoly)
    //{
    //    this.sampleRate = sampleRate;
    //    this.audioChannels = audioChannels;
    //    this.samplesperBuffer = (int)((sampleRate / 1000.0) * bufferSizeInMilliseconds);
    //    this.polyphony = maxpoly;
    //    setupSynth();
    //}
    //UnitySynth
    public StreamSynthesizer(int sampleRate, int audioChannels, int bufferSize, int maxpoly) {
      this.SampleRate = sampleRate;
      this.Channels = audioChannels;
      //UnitySynth
      this.samplesperBuffer = bufferSize;
      this.polyphony = maxpoly;
      this.SetupSynth();
    }

    public bool LoadBank(string filename) {
      //UnitySynth
      //try
      //{
      //    BankManager.addBank(new InstrumentBank(sampleRate, filename));
      //    SwitchBank(BankManager.Count - 1);
      //}
      //catch (Exception ex)
      //{
      //    Debug.Log("Bank load error!\n" + ex.Message + "\n\n" + ex.StackTrace);
      //    return false;
      //}
      //UnitySynth
      BankManager.AddBank(new InstrumentBank(this.SampleRate, filename));
      this.SwitchBank(BankManager.Count - 1);
      return true;
    }
    public bool UnloadBank(int index) {
      if (index < BankManager.Count) {
        if (BankManager.Banks[index] == this.SoundBank) {
          this.SoundBank = null;
        }

        BankManager.RemoveBank(index);
        return true;
      }
      return false;
    }
    public bool UnloadBank() {
      if (this.SoundBank != null) {
        BankManager.RemoveBank(this.SoundBank);
        return true;
      }
      return false;
    }
    public void SwitchBank(int index) {
      if (index < BankManager.Count) {
        this.SoundBank = BankManager.GetBank(index);
      }
    }
    public void SetPan(int channel, float position) {
      if (channel > -1 && channel < this.PanPositions.Length && position >= -1.00f && position <= 1.00f) {
        this.PanPositions[channel] = position;
      }
    }
    public void SetVolume(int channel, float position) {
      if (channel > -1 && channel < this.VolPositions.Length && position >= 0.00f && position <= 1.00f) {
        this.VolPositions[channel] = position;
      }
    }
    public void SetPitchBend(int channel, float semitones) {
      if (channel > -1 && channel < this.TunePositions.Length && semitones >= -12.00f && semitones <= 12.00f) {
        this.TunePositions[channel] = semitones;
      }
    }
    public void SetSequencer(MidiSequencer sequencer) => this.seq = sequencer;
    public void ResetSynthControls() {
      //Reset Pan Positions back to 0.0f
      Array.Clear(this.PanPositions, 0, this.PanPositions.Length);
      //Set Tuning Positions back to 0.0f
      Array.Clear(this.TunePositions, 0, this.TunePositions.Length);
      //Reset Vol Positions back to 1.00f
      for (int x = 0; x < this.VolPositions.Length; x++) {
        this.VolPositions[x] = 1.00f;
      }
    }
    public void Dispose() {
      this.Stop();
      this.sampleBuffer = null;
      this.voicePool = null;
      this.activeVoices.Clear();
      this.freeVoices.Clear();
      this.keyRegistry.Clear();
      this.effects.Clear();
    }
    public void Stop() => this.NoteOffAll(true);
    public void NoteOn(int channel, int note, int velocity, int program) {
      // Grab a free voice
      Voice freeVoice = this.GetFreeVoice();
      if (freeVoice == null) {
        // If there are no free voices steal an active one.
        freeVoice = this.GetUsedVoice(this.activeVoices.First.Value.GetKey());
        // If there are no voices to steal then leave this method.
        if (freeVoice == null) {
          return;
        }
      }
      // Create a key for this event
      NoteRegistryKey r = new((byte)channel, (byte)note);
      // Get the correct instrument depending if it is a drum or not
      if (channel == 9) {
        freeVoice.SetInstrument(this.SoundBank.GetInstrument(program, true));
      } else {
        freeVoice.SetInstrument(this.SoundBank.GetInstrument(program, false));
      }
      // Check if key exists
      if (this.keyRegistry.ContainsKey(r)) {
        if (this.keyRegistry[r].Count >= this.MaxPolyPerNote) {
          this.keyRegistry[r][0].Stop();
          this.keyRegistry[r].RemoveAt(0);
        }
        this.keyRegistry[r].Add(freeVoice);
      } else//The first noteOn of it's own type will create a list for multiple occurences
        {
        List<Voice> Vlist = new(this.MaxPolyPerNote) {
          freeVoice
        };
        this.keyRegistry.Add(r, Vlist);
      }
      freeVoice.Start(channel, note, velocity);
      this.activeVoices.AddLast(freeVoice);
    }
    public void NoteOff(int channel, int note) {
      NoteRegistryKey r = new((byte)channel, (byte)note);
      if (this.keyRegistry.TryGetValue(r, out List<Voice> voice)) {
        if (voice.Count > 0) {
          voice[0].Stop();
          voice.RemoveAt(0);
        }
      }
    }
    public void NoteOffAll(bool immediate) {
      if (this.keyRegistry.Keys.Count == 0 && this.activeVoices.Count == 0) {
        return;
      }

      LinkedListNode<Voice> node = this.activeVoices.First;
      while (node != null) {
        if (immediate) {
          node.Value.StopImmediately();
        } else {
          node.Value.Stop();
        }

        node = node.Next;
      }
      this.keyRegistry.Clear();
    }
    public void GetNext(byte[] buffer) {//Call this to process the next part of audio and return it in raw form.
      this.ClearWorkingBuffer();
      this.FillWorkingBuffer();
      for (int x = 0; x < this.effects.Count; x++) {
        this.effects[x].DoEffect(this.sampleBuffer);
      }
      this.ConvertBuffer(this.sampleBuffer, buffer);
    }

    //UnitySynth
    public void GetNext(float[] buffer) {//Call this to process the next part of audio and return it in raw form.
      this.ClearWorkingBuffer();
      this.FillWorkingBuffer();
      for (int x = 0; x < this.effects.Count; x++) {
        this.effects[x].DoEffect(this.sampleBuffer);
      }
      this.ConvertBuffer(this.sampleBuffer, buffer);
    }

    public void AddEffect(BasicAudioEffect effect) => this.effects.Add(effect);
    public void RemoveEffect(int index) => this.effects.RemoveAt(index);
    public void ClearEffects() => this.effects.Clear();
    //--Private Methods
    private Voice GetFreeVoice() {
      if (this.freeVoices.Count == 0) {
        return null;
      }

      return this.freeVoices.Pop();
    }
    private Voice GetUsedVoice(NoteRegistryKey r) {
      Voice voice;
      if (this.keyRegistry.TryGetValue(r, out List<Voice> voicelist)) {
        if (voicelist.Count > 0) {
          voicelist[0].StopImmediately();
          voice = voicelist[0];
          voicelist.RemoveAt(0);
          this.activeVoices.Remove(voice);
          return voice;
        }
      }
      return null;
    }
    private void ConvertBuffer(float[,] from, byte[] to) {
      const int bytesPerSample = 2; //again we assume 16 bit audio
      int channels = from.GetLength(0);
      int bufferSize = from.GetLength(1);

      // Make sure the buffer sizes are correct
      //UnitySynth
      if (!(to.Length == bufferSize * channels * bytesPerSample)) {
        Debug.Log("Buffer sizes are mismatched.");
      }

      for (int i = 0; i < bufferSize; i++) {
        for (int c = 0; c < channels; c++) {
          // Apply master volume
          float floatSample = from[c, i] * this.MainVolume;

          // Clamp the value to the [-1.0..1.0] range
          floatSample = SynthHelper.Clamp(floatSample, -1.0f, 1.0f);

          // Convert it to the 16 bit [short.MinValue..short.MaxValue] range
          short shortSample = (short)(floatSample >= 0.0f ? floatSample * short.MaxValue : floatSample * short.MinValue * -1);

          // Calculate the right index based on the PCM format of interleaved samples per channel [L-R-L-R]
          int index = i * channels * bytesPerSample + c * bytesPerSample;

          // Store the 16 bit sample as two consecutive 8 bit values in the buffer with regard to endian-ness
          if (!BitConverter.IsLittleEndian) {
            to[index] = (byte)(shortSample >> 8);
            to[index + 1] = (byte)shortSample;
          } else {
            to[index] = (byte)shortSample;
            to[index + 1] = (byte)(shortSample >> 8);
          }
        }
      }
    }

    //UnitySynth
    private void ConvertBuffer(float[,] from, float[] to) {
      const int bytesPerSample = 2; //again we assume 16 bit audio
      int channels = from.GetLength(0);
      int bufferSize = from.GetLength(1);
      int sampleIndex = 0;
      //UnitySynth
      if (!(to.Length == bufferSize * channels * bytesPerSample)) {
        Debug.Log("Buffer sizes are mismatched.");
      }

      for (int i = 0; i < bufferSize; i++) {
        for (int c = 0; c < channels; c++) {
          // Apply master volume
          float floatSample = from[c, i] * this.MainVolume;
          // Clamp the value to the [-1.0..1.0] range
          floatSample = SynthHelper.Clamp(floatSample, -1.0f, 1.0f);
          to[sampleIndex++] = floatSample;
        }
      }
    }

    private void FillWorkingBuffer() {
      // Call Process on all active voices
      LinkedListNode<Voice> node;
      LinkedListNode<Voice> delnode;
      if (this.seq != null && this.seq.IsPlaying)//Use sequencer
      {
        MidiSequencerEvent seqEvent = this.seq.Process(this.samplesperBuffer);
        if (seqEvent == null) {
          return;
        }

        int oldtime = 0;
				for (int x = 0; x < seqEvent.Events.Count; x++) {
					int waitTime = ((int)seqEvent.Events[x].deltaTime - this.seq.SampleTime) - oldtime;
					if (waitTime != 0) {
            node = this.activeVoices.First;
            while (node != null) {
              if (oldtime < 0 || waitTime < 0) {
                throw new Exception("dd");
              }

              node.Value.Process(this.sampleBuffer, oldtime, oldtime + waitTime);
              if (node.Value.IsInUse == false) {
                delnode = node;
                node = node.Next;
                this.freeVoices.Push(delnode.Value);
                this.activeVoices.Remove(delnode);
              } else {
                node = node.Next;
              }
            }
          }
          oldtime += waitTime;
          //Now process the event
          this.seq.ProcessMidiEvent(seqEvent.Events[x]);
        }
        //make sure to finish the processing to the end of the buffer
        if (oldtime < this.samplesperBuffer) {
          node = this.activeVoices.First;
          while (node != null) {
            node.Value.Process(this.sampleBuffer, oldtime, this.samplesperBuffer);
            if (node.Value.IsInUse == false) {
              delnode = node;
              node = node.Next;
              this.freeVoices.Push(delnode.Value);
              this.activeVoices.Remove(delnode);
            } else {
              node = node.Next;
            }
          }
        }
        //increment our sample count
        this.seq.IncrementSampleCounter(this.samplesperBuffer);
      } else //Manual mode
        {
        node = this.activeVoices.First;
        while (node != null) {
          //Process buffer with no interrupt for events
          node.Value.Process(this.sampleBuffer, 0, this.samplesperBuffer);
          if (node.Value.IsInUse == false) {
            delnode = node;
            node = node.Next;
            this.freeVoices.Push(delnode.Value);
            this.activeVoices.Remove(delnode);
          } else {
            node = node.Next;
          }
        }
      }
    }
    private void ClearWorkingBuffer() => Array.Clear(this.sampleBuffer, 0, this.Channels * this.samplesperBuffer);
    private void SetupSynth() {
      //checks
      if (this.SampleRate < 8000 || this.SampleRate > 48000) {
        this.SampleRate = 44100;
        this.samplesperBuffer = (this.SampleRate / 1000) * 50;
        //UnitySynth
        Debug.Log("-----> Invalid Sample Rate! Changed to---->" + this.SampleRate);
        Debug.Log("-----> Invalid Buffer Size! Changed to---->" + 50 + "ms");
      }
      if (this.polyphony < 1 || this.polyphony > 500) {
        this.polyphony = 40;
        Debug.Log("-----> Invalid Max Poly! Changed to---->" + this.polyphony);
      }
      if (this.MaxPolyPerNote < 1 || this.MaxPolyPerNote > this.polyphony) {
        this.MaxPolyPerNote = 2;
        Debug.Log("-----> Invalid Max Note Poly! Changed to---->" + this.MaxPolyPerNote);
      }
      if (this.samplesperBuffer < 100 || this.samplesperBuffer > 500000) {
        this.samplesperBuffer = (int)((this.SampleRate / 1000.0) * 50.0);
        Debug.Log("-----> Invalid Buffer Size! Changed to---->" + 50 + "ms");
      }
      if (this.Channels < 1 || this.Channels > 2) {
        this.Channels = 1;
        Debug.Log("-----> Invalid Audio Channels! Changed to---->" + this.Channels);
      }
      //initialize variables
      this.sampleBuffer = new float[this.Channels, this.samplesperBuffer];
      this.BufferSize = this.Channels * this.samplesperBuffer * 2; //Assuming 16 bit data
                                                                   // Create voice structures
      this.voicePool = new Voice[this.polyphony];
      for (int i = 0; i < this.polyphony; ++i) {
        this.voicePool[i] = new Voice(this);
      }

      this.freeVoices = new Stack<Voice>(this.voicePool);
      this.activeVoices = new LinkedList<Voice>();
      this.keyRegistry = new Dictionary<NoteRegistryKey, List<Voice>>();
      //Setup Channel Data
      this.PanPositions = new float[16];
      this.VolPositions = new float[16];
      for (int x = 0; x < this.VolPositions.Length; x++) {
        this.VolPositions[x] = 1.00f;
      }

      this.TunePositions = new double[16];
      //create effect list
      this.effects = new List<BasicAudioEffect>();
    }
  }
}
