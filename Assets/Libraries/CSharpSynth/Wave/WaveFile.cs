namespace CSharpSynth.Wave {
	public class WaveFile
    {
        //--Variables
        private readonly IChunk[] WaveChunks;

		//--Public Methods
		public WaveFile(IChunk[] WaveChunks)
        {
            this.WaveChunks = WaveChunks;
			this.SampleData = ((DataChunk)this.GetChunk(WaveHelper.WaveChunkType.Data)).sampled_data;
        }
        public IChunk GetChunk(WaveHelper.WaveChunkType ChunkType)
        {
            for (int x = 0; x < this.WaveChunks.Length; x++)
            {
                if (this.WaveChunks[x].GetChunkType() == ChunkType) {
					return this.WaveChunks[x];
				}
			}
            return null;
        }
        public IChunk GetChunk(int startIndex, WaveHelper.WaveChunkType ChunkType)
        {
            if (startIndex >= this.WaveChunks.Length) {
				return null;
			}

			for (int x = startIndex; x < this.WaveChunks.Length; x++)
            {
                if (this.WaveChunks[x].GetChunkType() == ChunkType) {
					return this.WaveChunks[x];
				}
			}
            return null;
        }
		public byte[] SampleData { get; }
		public int NumberOfChunks => this.WaveChunks.Length;
	}
}
