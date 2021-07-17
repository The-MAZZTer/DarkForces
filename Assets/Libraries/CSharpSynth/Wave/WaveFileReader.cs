using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CSharpSynth.Wave {
  public class WaveFileReader {
    //--Variables
    private readonly BinaryReader BR;
    //--Public Methods
    public WaveFileReader(string filename) {
      //UnitySynth
      //if (Path.GetExtension(filename).ToLower() != ".wav" || File.Exists(filename) == false)
      //    throw new IOException("Invalid wave file!");
      //BR = new System.IO.BinaryReader(System.IO.File.OpenRead(filename));

      //NOTE: WAVE FILES NEED .bytes appended. See http://unity3d.com/support/documentation/Components/class-TextAsset.html
      TextAsset fileName = Resources.Load(filename) as TextAsset;
      //Debug.Log(this.ToString() + " AppDataPath " + Application.dataPath + " Filename: " + filename + " asset.bytes.Length " + asset.bytes.Length.ToString());
      Stream waveFileStream = new MemoryStream(fileName.bytes);
      //Debug.Log(filename);
      this.BR = new BinaryReader(waveFileStream);
    }
    public IChunk[] ReadAllChunks() {
      List<IChunk> CList = new List<IChunk>();
      while (this.BR.BaseStream.Position < this.BR.BaseStream.Length) {
        IChunk tchk = this.ReadNextChunk();
        if (tchk != null) {
          CList.Add(tchk);
        }
      }
      return CList.ToArray();
    }
    public IChunk ReadNextChunk() {
      if (this.BR.BaseStream.Position + 4 >= this.BR.BaseStream.Length) {
        this.BR.BaseStream.Position += 4;
        return null;
      }
      string chkid = (System.Text.Encoding.UTF8.GetString(this.BR.ReadBytes(4), 0, 4)).ToLower();
      switch (chkid) {
        case "riff":
          MasterChunk mc = new MasterChunk {
            chkID = new char[] { 'R', 'I', 'F', 'F' },
            chksize = BitConverter.ToInt32(this.BR.ReadBytes(4), 0),
            WAVEID = this.BR.ReadChars(4)
          };
          return mc;
        case "fact":
          FactChunk fc = new FactChunk {
            chkID = new char[] { 'f', 'a', 'c', 't' },
            chksize = BitConverter.ToInt32(this.BR.ReadBytes(4), 0),
            dwSampleLength = BitConverter.ToInt32(this.BR.ReadBytes(4), 0)
          };
          return fc;
        case "data":
          DataChunk dc = new DataChunk {
            chkID = new char[] { 'd', 'a', 't', 'a' },
            chksize = BitConverter.ToInt32(this.BR.ReadBytes(4), 0)
          };
          if (dc.chksize % 2 == 0) {
            dc.pad = 0;
          } else {
            dc.pad = 1;
          }

          dc.sampled_data = this.BR.ReadBytes(dc.chksize);
          return dc;
        case "fmt ":
          FormatChunk fc2 = new FormatChunk {
            chkID = new char[] { 'f', 'm', 't', ' ' },
            chksize = BitConverter.ToInt32(this.BR.ReadBytes(4), 0),
            wFormatTag = BitConverter.ToInt16(this.BR.ReadBytes(2), 0),
            nChannels = BitConverter.ToInt16(this.BR.ReadBytes(2), 0),
            nSamplesPerSec = BitConverter.ToInt32(this.BR.ReadBytes(4), 0),
            nAvgBytesPerSec = BitConverter.ToInt32(this.BR.ReadBytes(4), 0),
            nBlockAlign = BitConverter.ToInt16(this.BR.ReadBytes(2), 0),
            wBitsPerSample = BitConverter.ToInt16(this.BR.ReadBytes(2), 0)
          };
          if (fc2.wFormatTag != (short)WaveHelper.Format_Code.WAVE_FORMAT_PCM) {
            fc2.cbSize = BitConverter.ToInt16(this.BR.ReadBytes(2), 0);
          }
          if ((ushort)fc2.wFormatTag == (int)WaveHelper.Format_Code.WAVE_FORMAT_EXTENSIBLE) {
            fc2.wValidBitsPerSample = BitConverter.ToInt16(this.BR.ReadBytes(2), 0);
            fc2.dwChannelMask = BitConverter.ToInt32(this.BR.ReadBytes(4), 0);
            fc2.SubFormat = this.BR.ReadChars(16);
          }
          return fc2;
        default:
          break;
      }
      return null;
    }
    public WaveFile ReadWaveFile() => new WaveFile(this.ReadAllChunks());
    public void Close() => this.BR.BaseStream.Dispose();//UnitySynth//BR.Dispose();
  }
}
