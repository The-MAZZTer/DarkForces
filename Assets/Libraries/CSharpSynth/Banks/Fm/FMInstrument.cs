using CSharpSynth.Synthesis;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CSharpSynth.Banks.Fm {
  public class FMInstrument : Instrument {
    private SynthHelper.WaveFormType modWaveType;
    private int _decay;
    private int _hold;
    private double start_time;
    private double end_time;
    private bool looping;
    private Envelope env;
    //modulator parameters
    private IFMComponent mamp;
    private IFMComponent mfreq;
    //--Public Properties
    public int Attack { get; set; }
    public int Release { get; set; }
    public SynthHelper.WaveFormType WaveForm { get; set; }
    //--Public Methods
    public FMInstrument(string fmProgramFile, int sampleRate)
            : base() {
      this.SampleRate = sampleRate;
      //Proper calculation of voice states
      this.Attack = SynthHelper.GetSampleFromTime(sampleRate, SynthHelper.DEFAULT_ATTACK);
      this.Release = SynthHelper.GetSampleFromTime(sampleRate, SynthHelper.DEFAULT_RELEASE);
      this._decay = SynthHelper.GetSampleFromTime(sampleRate, SynthHelper.DEFAULT_DECAY);
      this._hold = SynthHelper.GetSampleFromTime(sampleRate, SynthHelper.DEFAULT_HOLD);
      //open fm program file
      this.LoadProgramFile(fmProgramFile);
      //set base attribute name
      this.Name = Path.GetFileNameWithoutExtension(fmProgramFile);
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
      //time
      if (time > this.end_time) {
        if (this.looping) {
          time = this.start_time;
        } else {
          time = this.end_time;
          return 0.0f;
        }
      }
      double freq = SynthHelper.NoteToFrequency(note);
      //modulation
      switch (this.modWaveType) {
        case SynthHelper.WaveFormType.Sine:
          freq += (SynthHelper.Sine(this.mfreq.DoProcess(freq), time) * this.mamp.DoProcess(SynthHelper.DEFAULT_AMPLITUDE));
          break;
        case SynthHelper.WaveFormType.Sawtooth:
          freq += (SynthHelper.Sawtooth(this.mfreq.DoProcess(freq), time) * this.mamp.DoProcess(SynthHelper.DEFAULT_AMPLITUDE));
          break;
        case SynthHelper.WaveFormType.Square:
          freq += (SynthHelper.Square(this.mfreq.DoProcess(freq), time) * this.mamp.DoProcess(SynthHelper.DEFAULT_AMPLITUDE));
          break;
        case SynthHelper.WaveFormType.Triangle:
          freq += (SynthHelper.Triangle(this.mfreq.DoProcess(freq), time) * this.mamp.DoProcess(SynthHelper.DEFAULT_AMPLITUDE));
          break;
        case SynthHelper.WaveFormType.WhiteNoise:
          freq += (SynthHelper.WhiteNoise(0, time) * this.mamp.DoProcess(SynthHelper.DEFAULT_AMPLITUDE));
          break;
        default:
          break;
      }
      //carrier
      switch (this.WaveForm) {
        case SynthHelper.WaveFormType.Sine:
          return SynthHelper.Sine(freq, time) * this.env.DoProcess(time);
        case SynthHelper.WaveFormType.Sawtooth:
          return SynthHelper.Sawtooth(freq, time) * this.env.DoProcess(time);
        case SynthHelper.WaveFormType.Square:
          return SynthHelper.Square(freq, time) * this.env.DoProcess(time);
        case SynthHelper.WaveFormType.Triangle:
          return SynthHelper.Triangle(freq, time) * this.env.DoProcess(time);
        case SynthHelper.WaveFormType.WhiteNoise:
          return SynthHelper.WhiteNoise(note, time) * this.env.DoProcess(time);
        default:
          return 0.0f;
      }
    }
    private void LoadProgramFile(string file) {
      //UnitySynth
      StreamReader reader = new StreamReader(File.Open(Application.dataPath + "/Resources/" + file, FileMode.Open));
      //Debug.Log(this.ToString() + " AppDataPath " + Application.dataPath + " Filename: " + file);
      //StreamReader reader = new StreamReader(Application.dataPath + "/Resources/" + file);

      if (!reader.ReadLine().Trim().ToUpper().Equals("[FM INSTRUMENT]")) {
        reader.Dispose();
        throw new Exception("Invalid Program file: Incorrect Header!");
      }
      string[] args = reader.ReadLine().Split(new string[] { "|" }, StringSplitOptions.None);
      if (args.Length < 4) {
        reader.Dispose();
        throw new Exception("Invalid Program file: Parameters are missing");
      }
      this.WaveForm = SynthHelper.GetTypeFromString(args[0]);
      this.modWaveType = SynthHelper.GetTypeFromString(args[1]);
      this.mfreq = this.GetOpsAndValues(args[2], true);
      this.mamp = this.GetOpsAndValues(args[3], false);
      args = reader.ReadLine().Split(new string[] { "|" }, StringSplitOptions.None);
      if (args.Length < 3) {
        reader.Dispose();
        throw new Exception("Invalid Program file: Parameters are missing");
      }
      if (int.Parse(args[0]) == 0) {
        this.looping = true;
      }

      this.start_time = double.Parse(args[1]);
      this.end_time = double.Parse(args[2]);
      args = reader.ReadLine().Split(new string[] { "|" }, StringSplitOptions.None);
      if (args.Length < 3) {
        reader.Dispose();
        throw new Exception("Invalid Program file: Parameters are missing");
      }
      switch (args[0].ToLower().Trim()) {
        case "fadein":
          this.env = Envelope.CreateBasicFadeIn(double.Parse(args[2]));
          break;
        case "fadeout":
          this.env = Envelope.CreateBasicFadeOut(double.Parse(args[2]));
          break;
        case "fadein&out":
          double p = double.Parse(args[2]) / 2.0;
          this.env = Envelope.CreateBasicFadeInAndOut(p, p);
          break;
        default:
          this.env = Envelope.CreateBasicConstant();
          break;
      }
      this.env.Peak = double.Parse(args[1]);
      reader.Dispose();
    }
    private IFMComponent GetOpsAndValues(string arg, bool isFrequencyFunction) {
      arg += "    ";
      char[] chars = arg.ToCharArray();
      List<byte> opList = new List<byte>();
      List<double> valueList = new List<double>();
      string start = arg.Substring(0, 4).ToLower();
      if (isFrequencyFunction) {
        if (!start.Contains("freq")) {//if "freq" isnt used then we make sure the value passed in is negated by *0;
          opList.Add(0);
          valueList.Add(0);
        }
      } else {
        if (!start.Contains("amp")) {//if "amp" isnt used then we make sure the value passed in is negated by *0;
          opList.Add(0);
          valueList.Add(0);
        }
      }
      bool opOcurred = false;
      bool neg = false;
      for (int x = 0; x < arg.Length; x++) {
        switch (chars[x]) {
          case '*':
            if (opOcurred == false) {
              opList.Add(0);
              opOcurred = true;
            }
            break;
          case '/':
            if (opOcurred == false) {
              opList.Add(1);
              opOcurred = true;
            }
            break;
          case '+':
            if (opOcurred == false) {
              opList.Add(2);
              opOcurred = true;
            }
            break;
          case '-':
            if (opOcurred == true) {
              neg = !neg;
            } else {
              opList.Add(3);
              opOcurred = true;
            }
            break;
          default:
            string number = "";
            while (char.IsDigit(chars[x]) || chars[x] == '.') {
              number += chars[x];
              x++;
              if (x >= chars.Length) {
                break;
              }
            }
            if (number.Length > 0) {
              x--;
              opOcurred = false;
              if (neg) {
                number = "-" + number;
              }

              neg = false;
              valueList.Add(double.Parse(number));
            }
            break;
        }
      }
      while (opList.Count < valueList.Count) {
        opList.Add(2);
      }

      if (isFrequencyFunction) {
        return new ModulatorFrequencyFunction(opList.ToArray(), valueList.ToArray());
      } else {
        return new ModulatorAmplitudeFunction(opList.ToArray(), valueList.ToArray());
      }
    }
    //--Private Classes
    private class ModulatorFrequencyFunction : IFMComponent {
      private readonly byte[] ops; //0 = "*", 1 = "/", 2 = "+", 3 = "-"
      private readonly double[] values;
      public ModulatorFrequencyFunction(byte[] ops, double[] values) {
        if (ops.Length != values.Length) {
          throw new Exception("Invalid FM frequency function.");
        }

        this.ops = ops;
        this.values = values;
      }
      public double DoProcess(double value) {
        for (int x = 0; x < this.ops.Length; x++) {
          switch (this.ops[x]) {
            case 0:
              value *= this.values[x];
              break;
            case 1:
              value /= this.values[x];
              break;
            case 2:
              value += this.values[x];
              break;
            case 3:
              value -= this.values[x];
              break;
          }
        }
        return value;
      }
    }
    private class ModulatorAmplitudeFunction : IFMComponent {
      private readonly byte[] ops; //0 = "*", 1 = "/", 2 = "+", 3 = "-"
      private readonly double[] values;
      public ModulatorAmplitudeFunction(byte[] ops, double[] values) {
        if (ops.Length != values.Length) {
          throw new Exception("Invalid FM Amplitude function.");
        }

        this.ops = ops;
        this.values = values;
      }
      public double DoProcess(double value) {
        for (int x = 0; x < this.ops.Length; x++) {
          switch (this.ops[x]) {
            case 0:
              value *= this.values[x];
              break;
            case 1:
              value /= this.values[x];
              break;
            case 2:
              value += this.values[x];
              break;
            case 3:
              value -= this.values[x];
              break;
          }
        }
        return value;
      }
    }
  }
}