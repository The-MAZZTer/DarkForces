using CSharpSynth.Midi;
using System.Collections.Generic;

namespace CSharpSynth.Sequencer {
	public class MidiSequencerEvent
    {
        //--Variables
        public List<MidiEvent> Events; //List of Events
																			 //--Public Methods
		public MidiSequencerEvent() => this.Events = new List<MidiEvent>();
	}
}
