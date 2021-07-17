using System;
using System.IO;
using UnityEngine;

namespace CSharpSynth.Wave {
	public class Sample
    {
        //--Variables
        private float[,] data;

		//--Public Methods
		public Sample(string filename)
        {
			//UnitySynth - remove non Unity file path check
			//if (System.IO.File.Exists(filename) == false)
			//    throw new System.IO.FileNotFoundException("Sample not found: " + Path.GetFileNameWithoutExtension(filename));
			this.Name = Path.GetFileNameWithoutExtension(filename);
            Debug.Log("filename: " + filename + " name " + this.Name);
            WaveFileReader WaveReader = new WaveFileReader(filename);
            IChunk[] chunks = WaveReader.ReadAllChunks();
            WaveReader.Close(); //Close the reader and the underlying stream.
            DataChunk dChunk = null;
            FormatChunk fChunk = null;
            for (int x = 0; x < chunks.Length; x++)
            {
                if (chunks[x].GetChunkType() == WaveHelper.WaveChunkType.Format) {
					fChunk = (FormatChunk)chunks[x];
				} else if (chunks[x].GetChunkType() == WaveHelper.WaveChunkType.Data) {
					dChunk = (DataChunk)chunks[x];
				}
			}
            if (fChunk == null || dChunk == null) {
				throw new ArgumentException("Wave file is in unrecognized format!");
			}

			if (fChunk.wBitsPerSample != 16) {
				WaveHelper.ChangeBitsPerSample(fChunk, dChunk, 16);
			}

			//int channels = fChunk.nChannels;
			this.SampleRate = fChunk.nSamplesPerSec;
			this.OriginalSampleRate = this.SampleRate;
			this.data = WaveHelper.GetSampleData(fChunk, dChunk);
        }
        public Sample(int sampleRate)
        {
			this.data = new float[2, 1];
			this.data[0, 0] = 0.0f;
			this.data[1, 0] = 0.0f;
            this.SampleRate = sampleRate;
			this.OriginalSampleRate = sampleRate;
			this.Name = "";
        }
        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(Sample))
            {
                Sample s = (Sample)obj;
                if (this.Name.Equals(s.Name) && (this.SamplesPerChannel == s.SamplesPerChannel) 
                    && (this.NumberofChannels == s.NumberofChannels) && (this.SampleRate == s.SampleRate)) {
					return true;
				}
			}
            return false;
        }
		public override int GetHashCode() => base.GetHashCode();
		public int NumberofChannels => this.data.GetLength(0);
		public string Name { get; }
		public bool IsDualChannel => this.data.GetLength(0) == 2;
		public int SampleRate { get; set; }
		public int OriginalSampleRate { get; }
		public int SamplesPerChannel => this.data.GetLength(1);
		public float GetSample(int channel, int sample) => this.data[channel, sample];
		public void SetSample(int channel, int sample, float value) => this.data[channel, sample] = value;
		public float[,] GetAllSampleData() => this.data;
		public void SetAllSampleData(float[,] value) => this.data = value;
		public int GetMemoryUsage() => sizeof(float) * this.data.GetLength(0) * this.data.GetLength(1);
	}
}
