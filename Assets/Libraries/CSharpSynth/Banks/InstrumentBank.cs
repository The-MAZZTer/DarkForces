using CSharpSynth.Banks.Analog;
using CSharpSynth.Banks.Fm;
using CSharpSynth.Banks.Sfz;
using CSharpSynth.Synthesis;
using CSharpSynth.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CSharpSynth.Banks {
  public class InstrumentBank {
    //--Variables
    private readonly List<Instrument> Bank = new List<Instrument>();
    private readonly List<Instrument> DrumBank = new List<Instrument>();

    //--Static Variables
    public static Sample nullSample = new Sample(SynthHelper.DEFAULT_SAMPLERATE);
    //--Public Methods
    public InstrumentBank(int sampleRate, string bankfile) {
      this.SampleRate = sampleRate;
      this.LoadBank(bankfile);
      this.ReCalculateMemoryUsage();
    }
    public InstrumentBank(int sampleRate, string bankfile, byte[] Programs, byte[] DrumPrograms) {
      this.SampleRate = sampleRate;
      this.BankPath = bankfile;
      this.LoadBank(Programs, DrumPrograms);
      this.ReCalculateMemoryUsage();
    }
    public void LoadBank(string bankfile) {
      this.Clear();
      this.Bank.Capacity = BankManager.DEFAULT_BANK_SIZE;
      this.DrumBank.Capacity = BankManager.DEFAULT_DRUMBANK_SIZE;
      for (int x = 0; x < BankManager.DEFAULT_BANK_SIZE; x++) {
        this.Bank.Add(null);
      }

      for (int x = 0; x < BankManager.DEFAULT_DRUMBANK_SIZE; x++) {
        this.DrumBank.Add(null);
      }
      //UnitySynth
      //loadStream(File.Open(bankfile, FileMode.Open), Path.GetDirectoryName(bankfile) + "\\", null, null);
      TextAsset bankFile = Resources.Load(bankfile) as TextAsset;
      Debug.Log("loadBank(string bankfile) " + bankfile);
      Stream bankStream = new MemoryStream(bankFile.bytes);
      this.LoadStream(bankStream, Path.GetDirectoryName(bankfile) + "/", null, null);

      this.BankPath = bankfile;
    }
    public void LoadBank(byte[] Programs, byte[] DrumPrograms) {
      /*if (File.Exists(this.BankPath) == false) {
        return;
      }*/
      if (this.BankPath == null) {
        return;
			}

      this.Clear();
      this.Bank.Capacity = BankManager.DEFAULT_BANK_SIZE;
      this.DrumBank.Capacity = BankManager.DEFAULT_DRUMBANK_SIZE;
      for (int x = 0; x < BankManager.DEFAULT_BANK_SIZE; x++) {
        this.Bank.Add(null);
      }

      for (int x = 0; x < BankManager.DEFAULT_DRUMBANK_SIZE; x++) {
        this.DrumBank.Add(null);
      }
      //UnitySynth
      //loadStream(File.Open(lastbankpath, FileMode.Open), Path.GetDirectoryName(lastbankpath) + "\\", Programs, DrumPrograms);
      TextAsset lastBankPath = Resources.Load(this.BankPath) as TextAsset;
      Debug.Log("loadBank(byte[] Programs, byte[] DrumPrograms) " + this.BankPath);
      Stream bankStream = new MemoryStream(lastBankPath.bytes);
      this.LoadStream(bankStream, Path.GetDirectoryName(this.BankPath) + "/", Programs, DrumPrograms);
    }
    public void LoadStream(Stream bankStream, string directory, byte[] Programs, byte[] DrumPrograms) {
      StreamReader reader = new StreamReader(bankStream);
      List<string> text = new List<string>();
      while (reader.Peek() > -1) {
        text.Add(reader.ReadLine());
      }

      reader.Dispose();
      bankStream.Dispose();
      if (text[0].Trim() != "[BankFile]") {
        throw new Exception("Not a valid BankFile!");
      }

      for (int x = 1; x < text.Count; x++) {//Load each instrument, banks can have mixed instruments!
        string[] split = text[x].Split(new string[] { "/" }, StringSplitOptions.None);
        switch (split[1].Trim().ToLower()) {
          case "analog":
            this.LoadAnalog(split, Programs, DrumPrograms);
            break;
          case "fm":
            this.LoadFm(split, directory, Programs, DrumPrograms);
            break;
          case "sfz":
            this.LoadSfz(split, directory, Programs, DrumPrograms);
            break;
        }
      }
    }
    public void AddInstrument(Instrument inst, bool isDrum) {
      if (isDrum == false) {
        if (this.Bank.Contains(inst) == false) {
          //Resample if necessary
          if (this.SampleRate > 0) {
            inst.EnforceSampleRate(this.SampleRate);
          }

          if (inst.SampleList != null) {
            for (int x = 0; x < inst.SampleList.Length; x++) {//If the instrument contains any new samples get their memory use and add them.
              if (this.SampleNameList.Contains(inst.SampleList[x].Name) == false) {
                this.MemoryUsage += inst.SampleList[x].GetMemoryUsage();
                this.SampleNameList.Add(inst.SampleList[x].Name);
                this.SampleList.Add(inst.SampleList[x]);
              }
            }
          }
        }
        this.Bank.Add(inst);
      } else {
        if (this.DrumBank.Contains(inst) == false) {
          //Resample if necessary
          if (this.SampleRate > 0) {
            inst.EnforceSampleRate(this.SampleRate);
          }

          if (inst.SampleList != null) {
            for (int x = 0; x < inst.SampleList.Length; x++) {
              this.MemoryUsage += inst.SampleList[x].GetMemoryUsage();
              if (this.SampleNameList.Contains(inst.SampleList[x].Name) == false) {
                this.SampleNameList.Add(inst.SampleList[x].Name);
                this.SampleList.Add(inst.SampleList[x]);
              }
            }
          }
        }
        this.DrumBank.Add(inst);
      }
    }
    public Instrument GetInstrument(int index, bool isDrum) {
      if (isDrum == false) {
        return this.Bank[index];
      } else {
        return this.DrumBank[index];
      }
    }
    public List<Instrument> GetInstruments(bool isDrum) {
      if (isDrum == false) {
        return this.Bank;
      } else {
        return this.DrumBank;
      }
    }
    public void RemoveInstrument(int index, bool isDrum) {//Does not delete the index location so the other instruments keep their locations
      if (isDrum == true) {
        this.DrumBank[index] = null;
      } else {
        this.Bank[index] = null;
      }
    }
    public void DeleteUnusedSamples() {//Now that the InstrumentBank keeps a reference to it's samples
                                       //you must call this method after you remove instruments
                                       //to make sure their samples get deleted as well.
                                       //You don't have to use this after calling Clear() however.

      //Delete and Rebuild Sample List
      this.SampleList.Clear();
      this.SampleNameList.Clear();
      for (int x = 0; x < this.Bank.Count; x++) {
        if (this.Bank[x] != null) {
          Sample[] samps = this.Bank[x].SampleList;
          for (int x2 = 0; x2 < samps.Length; x2++) {
            if (this.SampleNameList.Contains(samps[x2].Name) == false) {
              this.SampleNameList.Add(samps[x2].Name);
              this.SampleList.Add(samps[x2]);
            }
          }
        }
      }
      for (int x = 0; x < this.DrumBank.Count; x++) {
        if (this.DrumBank[x] != null) {
          Sample[] samps = this.DrumBank[x].SampleList;
          for (int x2 = 0; x2 < samps.Length; x2++) {
            if (this.SampleNameList.Contains(samps[x2].Name) == false) {
              this.SampleNameList.Add(samps[x2].Name);
              this.SampleList.Add(samps[x2]);
            }
          }
        }
      }
      this.ReCalculateMemoryUsage();
    }
    public void Clear() {
      this.Bank.Clear();
      this.DrumBank.Clear();
      this.SampleNameList.Clear();
      this.SampleList.Clear();
      this.MemoryUsage = 0;
    }
    //--Public Properties
    public int InstrumentCount => this.Bank.Count;
    public string BankPath { get; set; } = "";
    public static Sample DummySample => nullSample;
    public List<string> SampleNameList { get; } = new List<string>();
    public List<Sample> SampleList { get; } = new List<Sample>();
    public int DrumCount => this.DrumBank.Count;
    public int MemoryUsage { get; private set; }
    public int SampleRate { get; set; }
    //--Private Methods
    private void LoadAnalog(string[] args, byte[] Programs, byte[] DrumPrograms) {
      bool ISdrum = args[4] == "d";
      int start = int.Parse(args[2]);
      int end = int.Parse(args[3]);
      List<int> Indices = new List<int>();

      if (ISdrum == false) {
        if (Programs == null) {
          for (int i = start; i <= end; i++) {
            Indices.Add(i);
          }
        } else {
          for (int x2 = 0; x2 < Programs.Length; x2++) {
            if (Programs[x2] >= start && Programs[x2] <= end) {
              Indices.Add(Programs[x2]);
            }
          }
        }
      } else {
        if (DrumPrograms == null) {
          for (int i = start; i <= end; i++) {
            Indices.Add(i);
          }
        } else {
          for (int x2 = 0; x2 < DrumPrograms.Length; x2++) {
            if (DrumPrograms[x2] >= start && Programs[x2] <= end) {
              Indices.Add(DrumPrograms[x2]);
            }
          }
        }
      }

      if (Indices.Count > 0) {
        Instrument inst;
        inst = new AnalogInstrument(SynthHelper.GetTypeFromString(args[0]), this.SampleRate);
        //Resample if necessary
        if (this.SampleRate > 0) {
          inst.EnforceSampleRate(this.SampleRate);
        }
        //Loop through where to add the instruments
        for (int i = 0; i < Indices.Count; i++) {
          //Decide which bank to add too
          if (ISdrum == true) {
            this.DrumBank[Indices[i]] = inst;
          } else {
            this.Bank[Indices[i]] = inst;
          }
        }
      }
    }
    private void LoadFm(string[] args, string bankpath, byte[] Programs, byte[] DrumPrograms) {
      bool ISdrum = args[4] == "d";
      int start = int.Parse(args[2]);
      int end = int.Parse(args[3]);
      List<int> Indices = new List<int>();

      if (ISdrum == false) {
        if (Programs == null) {
          for (int i = start; i <= end; i++) {
            Indices.Add(i);
          }
        } else {
          for (int x2 = 0; x2 < Programs.Length; x2++) {
            if (Programs[x2] >= start && Programs[x2] <= end) {
              Indices.Add(Programs[x2]);
            }
          }
        }
      } else {
        if (DrumPrograms == null) {
          for (int i = start; i <= end; i++) {
            Indices.Add(i);
          }
        } else {
          for (int x2 = 0; x2 < DrumPrograms.Length; x2++) {
            if (DrumPrograms[x2] >= start && Programs[x2] <= end) {
              Indices.Add(DrumPrograms[x2]);
            }
          }
        }
      }

      if (Indices.Count > 0) {
        Instrument inst;
        inst = new FMInstrument(bankpath + args[0] + ".prg", this.SampleRate);
        //Resample if necessary
        if (this.SampleRate > 0) {
          inst.EnforceSampleRate(this.SampleRate);
        }
        //Loop through where to add the instruments
        for (int i = 0; i < Indices.Count; i++) {
          //Decide which bank to add too
          if (ISdrum == true) {
            this.DrumBank[Indices[i]] = inst;
          } else {
            this.Bank[Indices[i]] = inst;
          }
        }
      }
    }
    private void LoadSfz(string[] args, string bankpath, byte[] Programs, byte[] DrumPrograms) {
      bool ISdrum = args[4] == "d";
      int start = int.Parse(args[2]);
      int end = int.Parse(args[3]);
      List<int> Indices = new List<int>();

      if (ISdrum == false) {
        if (Programs == null) {
          for (int i = start; i <= end; i++) {
            Indices.Add(i);
          }
        } else {
          for (int x2 = 0; x2 < Programs.Length; x2++) {
            if (Programs[x2] >= start && Programs[x2] <= end) {
              Indices.Add(Programs[x2]);
            }
          }
        }
      } else {
        if (DrumPrograms == null) {
          for (int i = start; i <= end; i++) {
            Indices.Add(i);
          }
        } else {
          for (int x2 = 0; x2 < DrumPrograms.Length; x2++) {
            if (DrumPrograms[x2] >= start && Programs[x2] <= end) {
              Indices.Add(DrumPrograms[x2]);
            }
          }
        }
      }

      if (Indices.Count > 0) {
        Instrument inst;
        inst = new SfzInstrument(bankpath + args[0] + ".sfz", this.SampleRate, this);
        //Resample if necessary
        if (this.SampleRate > 0) {
          inst.EnforceSampleRate(this.SampleRate);
        }
        //Loop through where to add the instruments
        for (int i = 0; i < Indices.Count; i++) {
          //Decide which bank to add too
          if (ISdrum == true) {
            this.DrumBank[Indices[i]] = inst;
          } else {
            this.Bank[Indices[i]] = inst;
          }
        }
      }
    }
    private void ReCalculateMemoryUsage() {
      /*this.MemoryUsage = 0;
      for (int x = 0; x < this.SampleList.Count; x++) {
        this.MemoryUsage += this.SampleList[x].GetMemoryUsage();
      }*/
      this.MemoryUsage = this.SampleList.Sum(x => x.GetMemoryUsage());
    }
  }
}