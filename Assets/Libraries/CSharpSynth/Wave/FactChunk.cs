namespace CSharpSynth.Wave {
	public class FactChunk : IChunk
    {
        public char[] chkID = new char[4];
        public int chksize = 0;
        public int dwSampleLength = 0;
		public WaveHelper.WaveChunkType GetChunkType() => WaveHelper.WaveChunkType.Fact;
		public string GetChunkId() => new string(this.chkID);
		public int GetChunkSize() => this.chksize;
	}
}
