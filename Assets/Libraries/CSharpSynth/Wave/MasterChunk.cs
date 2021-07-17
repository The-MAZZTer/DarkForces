namespace CSharpSynth.Wave {
	public class MasterChunk : IChunk
    {
        public char[] chkID = new char[4];
        public int chksize = 0;
        public char[] WAVEID = new char[4];
        public int WAVEchunks = 0;
		public WaveHelper.WaveChunkType GetChunkType() => WaveHelper.WaveChunkType.Master;
		public string GetChunkId() => new string(this.chkID);
		public int GetChunkSize() => this.chksize;
	}
}
