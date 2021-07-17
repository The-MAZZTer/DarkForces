namespace CSharpSynth.Midi {
	public class MidiTrack {
    //--Variables
    public uint NotesPlayed;
    public ulong TotalTime;
    public byte[] Programs;
    public byte[] DrumPrograms;
    public MidiEvent[] MidiEvents;
		//--Public Properties
		public int EventCount => this.MidiEvents.Length;
		//--Public Methods
		public MidiTrack() {
      this.NotesPlayed = 0;
      this.TotalTime = 0;
    }
    public bool ContainsProgram(byte program) {
      for (int x = 0; x < this.Programs.Length; x++) {
        if (this.Programs[x] == program) {
          return true;
        }
      }
      return false;
    }
    public bool ContainsDrumProgram(byte drumprogram) {
      for (int x = 0; x < this.DrumPrograms.Length; x++) {
        if (this.DrumPrograms[x] == drumprogram) {
          return true;
        }
      }
      return false;
    }
  }
}
