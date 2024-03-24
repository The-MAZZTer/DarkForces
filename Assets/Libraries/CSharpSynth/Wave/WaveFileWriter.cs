using System.IO;

namespace CSharpSynth.Wave {
	public class WaveFileWriter
    {
        //--Variables
        private readonly BinaryWriter BW;
        private readonly string fileN;
        private int length;
        private readonly int channels;
        private readonly int bits;
        private readonly int sRate;
        //--Public Methods
        public WaveFileWriter(int sampleRate, int channels, int bitsPerSample, string filename)
        {
			this.BW = new System.IO.BinaryWriter(File.OpenRead(Path.GetDirectoryName(filename) + "RawWaveData_1tmp"));
			this.fileN = filename;
            this.channels = channels;
			this.bits = bitsPerSample;
			this.sRate = sampleRate;
        }
        public void Write(byte[] buffer)
        {
			this.BW.Write(buffer);
			this.length += buffer.Length;
        }
        public void Close()
        {
			this.BW.BaseStream.Dispose();
            //UnitySynth
            // BW.Dispose();
            BinaryWriter bw2 = new(File.OpenRead(Path.GetDirectoryName(this.fileN)));
            bw2.Write(1179011410);
            bw2.Write(44 + this.length - 8);
            bw2.Write(1163280727);
            bw2.Write(544501094);
            bw2.Write(16);
            bw2.Write((short)1);
            bw2.Write((short)this.channels);
            bw2.Write(this.sRate);
            bw2.Write(this.sRate * this.channels * (this.bits / 8));
            bw2.Write((short)(this.channels * (this.bits / 8)));
            bw2.Write((short)this.bits);
            bw2.Write(1635017060);
            bw2.Write(this.length);
            BinaryReader br = new(File.OpenRead(Path.GetDirectoryName(this.fileN) + "RawWaveData_1tmp"));
            for (int x = 0; x < this.length; x++) {
				bw2.Write(br.ReadByte());
			}

			br.BaseStream.Dispose();
            bw2.BaseStream.Dispose();
        }
    }
}
