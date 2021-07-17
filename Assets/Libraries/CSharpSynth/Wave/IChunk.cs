namespace CSharpSynth.Wave {
	public interface IChunk
    {
        WaveHelper.WaveChunkType GetChunkType();
		string GetChunkId();
        int GetChunkSize();
    }
}
