namespace CSharpSynth.Midi {
	public class MidiHeader {
    //--Variables
    public int DeltaTiming;
    public MidiHelper.MidiFormat MidiFormat;
    public MidiHelper.MidiTimeFormat TimeFormat;
    //--Public Methods
    public void SetMidiFormat(int format) {
      if (format == 0) {
        this.MidiFormat = MidiHelper.MidiFormat.SingleTrack;
      } else if (format == 1) {
        this.MidiFormat = MidiHelper.MidiFormat.MultiTrack;
      } else if (format == 2) {
        this.MidiFormat = MidiHelper.MidiFormat.MultiSong;
      }
    }
  }
}
