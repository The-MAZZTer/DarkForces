using CSharpSynth.Banks;
using CSharpSynth.Midi;
using CSharpSynth.Synthesis;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CSharpSynth.Sequencer {
	public class MidiSequencer {
    //--Variables
    private MidiFile _MidiFile;
    private StreamSynthesizer synth;
    private readonly int[] currentPrograms;
    private readonly List<byte> blockList;
		private MidiSequencerEvent seqEvt;
		private int eventIndex;
    //--Events
    public delegate void NoteOnEventHandler(int channel, int note, int velocity);
    public event NoteOnEventHandler NoteOnEvent;
    public delegate void NoteOffEventHandler(int channel, int note);
    public event NoteOffEventHandler NoteOffEvent;
		//--Public Properties
		public bool IsPlaying { get; private set; } = false;
		public int SampleTime { get; private set; }
		public int EndSampleTime => (int)this._MidiFile.Tracks[this.track].TotalTime;
		public TimeSpan EndTime => new TimeSpan(0, 0, (int)SynthHelper.GetTimeFromSample(this.synth.SampleRate, (int)this._MidiFile.Tracks[this.track].TotalTime));
		public TimeSpan Time {
			get => new TimeSpan(0, 0, (int)SynthHelper.GetTimeFromSample(this.synth.SampleRate, this.SampleTime));
			set => this.SetTime(value);
		}
		public double PitchWheelRange { get; set; } = 2.0;
		//--Public Methods
		public MidiSequencer(StreamSynthesizer synth) {
			this.currentPrograms = new int[16]; //16 channels
      this.synth = synth;
      this.synth.SetSequencer(this);
			this.blockList = new List<byte>();
			this.seqEvt = new MidiSequencerEvent();
    }
    public string GetProgramName(int channel) {
      if (channel == 9) {
				return this.synth.SoundBank.GetInstrument(this.currentPrograms[channel], true).Name;
			} else {
				return this.synth.SoundBank.GetInstrument(this.currentPrograms[channel], false).Name;
			}
		}
		public int GetProgramIndex(int channel) => this.currentPrograms[channel];
		public void SetProgram(int channel, int program) => this.currentPrograms[channel] = program;
		public bool Looping { get; set; } = false;
		private int track;
    public bool LoadMidi(MidiFile midi, int track, bool UnloadUnusedInstruments) {
      if (this.IsPlaying == true) {
				return false;
			}

			this._MidiFile = midi;
      if (this._MidiFile.SequencerReady == false) {
        try {
          this.track = track;
          //Combine all tracks into 1 track that is organized from lowest to highest abs time
          if (this._MidiFile.MidiHeader.MidiFormat != MidiHelper.MidiFormat.MultiSong) {
            _MidiFile.CombineTracks();
            this.track = 0;
          }
					//Convert delta time to sample time
					this.eventIndex = 0;
          uint lastSample = 0;
          for (int x = 0; x < this._MidiFile.Tracks[this.track].MidiEvents.Length; x++) {
						this._MidiFile.Tracks[this.track].MidiEvents[x].deltaTime = lastSample + (uint)this.DeltaTimetoSamples(this._MidiFile.Tracks[this.track].MidiEvents[x].deltaTime);
            lastSample = this._MidiFile.Tracks[this.track].MidiEvents[x].deltaTime;
            //Update tempo
            if (this._MidiFile.Tracks[this.track].MidiEvents[x].midiMetaEvent == MidiHelper.MidiMetaEvent.Tempo) {
							this._MidiFile.BeatsPerMinute = MidiHelper.MicroSecondsPerMinute / Convert.ToUInt32(this._MidiFile.Tracks[this.track].MidiEvents[x].Parameters[0]);
            }
          }
					//Set total time to proper value
					this._MidiFile.Tracks[this.track].TotalTime = this._MidiFile.Tracks[this.track].MidiEvents[this._MidiFile.Tracks[this.track].MidiEvents.Length - 1].deltaTime;
					//reset tempo
					this._MidiFile.BeatsPerMinute = 120;
					//mark midi as ready for sequencing
					this._MidiFile.SequencerReady = true;
        } catch (Exception ex) {
          //UnitySynth
          Debug.Log("Error Loading Midi:\n" + ex.Message);
          return false;
        }
      }
			this.blockList.Clear();
      if (UnloadUnusedInstruments == true) {
        if (this.synth.SoundBank == null) {//If there is no bank warn the developer =)
          Debug.Log("No Soundbank loaded !");
        } else {
          string bankStr = this.synth.SoundBank.BankPath;
					//Remove old bank being used by synth
					this.synth.UnloadBank();
          //Add the bank and switch to it with the synth
          BankManager.AddBank(new InstrumentBank(this.synth.SampleRate, bankStr, this._MidiFile.Tracks[this.track].Programs, this._MidiFile.Tracks[this.track].DrumPrograms));
					this.synth.SwitchBank(BankManager.Count - 1);
        }
      }
      return true;
    }
    public bool LoadMidi(TextAsset file, int track, bool UnloadUnusedInstruments) {
      if (this.IsPlaying == true) {
        return false;
      }

      MidiFile mf;
      try {
        mf = new MidiFile(file);
      } catch (Exception ex) {
        //UnitySynth
        Debug.Log("Error Loading Midi:\n" + ex.Message);
        return false;
      }
      return this.LoadMidi(mf, track, UnloadUnusedInstruments);
    }
    public bool LoadMidi(Stream file, int track, bool UnloadUnusedInstruments) {
      if (this.IsPlaying == true) {
        return false;
      }

      MidiFile mf;
      try {
        mf = new MidiFile(file);
      } catch (Exception ex) {
        //UnitySynth
        Debug.Log("Error Loading Midi:\n" + ex.Message);
        return false;
      }
      return this.LoadMidi(mf, track, UnloadUnusedInstruments);
    }
    public void Play() {
      if (this.IsPlaying == true) {
				return;
			}
			//Clear the current programs for the channels.
			Array.Clear(this.currentPrograms, 0, this.currentPrograms.Length);
			//Clear vol, pan, and tune
			this.ResetControllers();
			//set bpm
			this._MidiFile.BeatsPerMinute = 120;
			//Let the synth know that the sequencer is ready.
			this.eventIndex = 0;
			this.IsPlaying = true;
    }
    public void Pause(bool immediate) {
      this.IsPlaying = false;
      if (immediate) {
        this.synth.NoteOffAll(true);
      } else {
        this.synth.NoteOffAll(false);
      }      
    }
    public void Unpause() {
      this.IsPlaying = true;
    }
    public void Stop(bool immediate) {
			this.IsPlaying = false;
			this.SampleTime = 0;
      if (immediate) {
				this.synth.NoteOffAll(true);
			} else {
				this.synth.NoteOffAll(false);
			}
		}
    public bool IsChannelMuted(int channel) {
      if (this.blockList.Contains((byte)channel)) {
				return true;
			}

			return false;
    }
    public void MuteChannel(int channel) {
      if (channel > -1 && channel < 16) {
				if (this.blockList.Contains((byte)channel) == false) {
					this.blockList.Add((byte)channel);
				}
			}
		}
    public void UnMuteChannel(int channel) {
      if (channel > -1 && channel < 16) {
				this.blockList.Remove((byte)channel);
			}
		}
    public void MuteAllChannels() {
      for (int x = 0; x < 16; x++) {
				this.blockList.Add((byte)x);
			}
		}
		public void UnMuteAllChannels() => this.blockList.Clear();
		public void ResetControllers() {
      //Reset Pan Positions back to 0.0f
      Array.Clear(this.synth.PanPositions, 0, this.synth.PanPositions.Length);
      //Set Tuning Positions back to 0.0f
      Array.Clear(this.synth.TunePositions, 0, this.synth.TunePositions.Length);
      //Reset Vol Positions back to 1.00f
      for (int x = 0; x < this.synth.VolPositions.Length; x++) {
				this.synth.VolPositions[x] = 1.00f;
			}
		}
    public MidiSequencerEvent Process(int frame) {
			this.seqEvt.Events.Clear();
      //stop or loop
      if (this.SampleTime >= (int)this._MidiFile.Tracks[this.track].TotalTime) {
				this.SampleTime = 0;
        if (this.Looping == true) {
          //Clear the current programs for the channels.
          Array.Clear(this.currentPrograms, 0, this.currentPrograms.Length);
					//Clear vol, pan, and tune
					this.ResetControllers();
					//set bpm
					this._MidiFile.BeatsPerMinute = 120;
					//Let the synth know that the sequencer is ready.
					this.eventIndex = 0;
        } else {
					this.IsPlaying = false;
					this.synth.NoteOffAll(true);
          return null;
        }
      }
      while (this.eventIndex < this._MidiFile.Tracks[this.track].EventCount && this._MidiFile.Tracks[this.track].MidiEvents[this.eventIndex].deltaTime < (this.SampleTime + frame)) {
				this.seqEvt.Events.Add(this._MidiFile.Tracks[this.track].MidiEvents[this.eventIndex]);
				this.eventIndex++;
      }
      return this.seqEvt;
    }
		public void IncrementSampleCounter(int amount) => this.SampleTime += amount;
		public void ProcessMidiEvent(MidiEvent midiEvent) {
      if (midiEvent.midiChannelEvent != MidiHelper.MidiChannelEvent.None) {
        switch (midiEvent.midiChannelEvent) {
          case MidiHelper.MidiChannelEvent.Program_Change:
            if (midiEvent.channel != 9) {
              if (midiEvent.parameter1 < this.synth.SoundBank.InstrumentCount) {
								this.currentPrograms[midiEvent.channel] = midiEvent.parameter1;
							}
						} else //its the drum channel
              {
              if (midiEvent.parameter1 < this.synth.SoundBank.DrumCount) {
								this.currentPrograms[midiEvent.channel] = midiEvent.parameter1;
							}
						}
            break;
          case MidiHelper.MidiChannelEvent.Note_On:
            if (this.blockList.Contains(midiEvent.channel)) {
							return;
						}

						this.NoteOnEvent?.Invoke(midiEvent.channel, midiEvent.parameter1, midiEvent.parameter2);

						this.synth.NoteOn(midiEvent.channel, midiEvent.parameter1, midiEvent.parameter2, this.currentPrograms[midiEvent.channel]);
            break;
          case MidiHelper.MidiChannelEvent.Note_Off:
						this.NoteOffEvent?.Invoke(midiEvent.channel, midiEvent.parameter1);

						this.synth.NoteOff(midiEvent.channel, midiEvent.parameter1);
            break;
          case MidiHelper.MidiChannelEvent.Pitch_Bend:
						//Store PitchBend as the # of semitones higher or lower
						this.synth.TunePositions[midiEvent.channel] = (double)midiEvent.Parameters[1] * this.PitchWheelRange;
            break;
          case MidiHelper.MidiChannelEvent.Controller:
            switch (midiEvent.GetControllerType()) {
              case MidiHelper.ControllerType.AllNotesOff:
								this.synth.NoteOffAll(true);
                break;
              case MidiHelper.ControllerType.MainVolume:
								this.synth.VolPositions[midiEvent.channel] = midiEvent.parameter2 / 127.0f;
                break;
              case MidiHelper.ControllerType.Pan:
								this.synth.PanPositions[midiEvent.channel] = (midiEvent.parameter2 - 64) == 63 ? 1.00f : (midiEvent.parameter2 - 64) / 64.0f;
                break;
              case MidiHelper.ControllerType.ResetControllers:
								this.ResetControllers();
                break;
              default:
                break;
            }
            break;
          default:
            break;
        }
      } else {
        switch (midiEvent.midiMetaEvent) {
          case MidiHelper.MidiMetaEvent.Tempo:
						this._MidiFile.BeatsPerMinute = MidiHelper.MicroSecondsPerMinute / Convert.ToUInt32(midiEvent.Parameters[0]);
            break;
          default:
            break;
        }
      }
    }
    public void Dispose() {
			this.Stop(true);
			//Set anything that may become a circular reference to null...
			this.synth = null;
			this._MidiFile = null;
			this.seqEvt = null;
    }
		//--Private Methods
		private int DeltaTimetoSamples(uint DeltaTime) => SynthHelper.GetSampleFromTime(this.synth.SampleRate, (DeltaTime * (60.0f / (((int)this._MidiFile.BeatsPerMinute) * this._MidiFile.MidiHeader.DeltaTiming))));
		private void SetTime(TimeSpan time) {
      int _stime = SynthHelper.GetSampleFromTime(this.synth.SampleRate, (float)time.TotalSeconds);
      if (_stime > this.SampleTime) {
				this.SilentProcess(_stime - this.SampleTime);
      } else if (_stime < this.SampleTime) {//we have to restart the midi to make sure we get the right temp, instrument, etc
				this.synth.Stop();
				this.SampleTime = 0;
        Array.Clear(this.currentPrograms, 0, this.currentPrograms.Length);
				this.ResetControllers();
				this._MidiFile.BeatsPerMinute = 120;
				this.eventIndex = 0;
				this.SilentProcess(_stime);
      }
    }
    private void SilentProcess(int amount) {
      while (this.eventIndex < this._MidiFile.Tracks[this.track].EventCount && this._MidiFile.Tracks[this.track].MidiEvents[this.eventIndex].deltaTime < (this.SampleTime + amount)) {
        if (this._MidiFile.Tracks[this.track].MidiEvents[this.eventIndex].midiChannelEvent != MidiHelper.MidiChannelEvent.Note_On) {
					this.ProcessMidiEvent(this._MidiFile.Tracks[this.track].MidiEvents[this.eventIndex]);
				}

				this.eventIndex++;
      }
			this.SampleTime += amount;
    }
  }
}