namespace CSharpSynth.Wave {
	public class DataChunk : IChunk
    {
        //--Variables
        public char[] chkID = new char[4];
        public int chksize = 0;
        public byte[] sampled_data;
        public byte pad = 0;
		//--Public Methods
		public System.IO.Stream GetDataStream() => new System.IO.MemoryStream(this.sampled_data);
		public WaveHelper.WaveChunkType GetChunkType() => WaveHelper.WaveChunkType.Data;
		public string GetChunkId() => new string(this.chkID);
		public int GetChunkSize() => this.chksize;
	}
}
