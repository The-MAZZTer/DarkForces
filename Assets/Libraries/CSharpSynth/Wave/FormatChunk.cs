namespace CSharpSynth.Wave {
	public class FormatChunk : IChunk
    {
        public char[] chkID = new char[4];
        public int chksize = 0;
        public short wFormatTag = 0;
        public short nChannels = 0;
        public int nSamplesPerSec = 0;
        public int nAvgBytesPerSec = 0;
        public short nBlockAlign = 0;
        public short wBitsPerSample = 0;
        public short cbSize = 0;
        public short wValidBitsPerSample = 0;
        public int dwChannelMask = 0;
        public char[] SubFormat = new char[16];
		public WaveHelper.WaveChunkType GetChunkType() => WaveHelper.WaveChunkType.Format;
		public string GetChunkId() => new string(this.chkID);
		public int GetChunkSize() => this.chksize;
	}
}
