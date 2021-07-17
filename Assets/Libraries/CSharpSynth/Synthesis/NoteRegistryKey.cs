using System;

namespace CSharpSynth.Synthesis {
  //UnitySynth
  //public struct NoteRegistryKey : IEquatable<NoteRegistryKey>
  public struct NoteRegistryKey {
    //--Public Properties
    public byte Note { get; }
    public byte Channel { get; }
    //--Public Methods
    public NoteRegistryKey(byte channel, byte note) {
      this.Note = note;
      this.Channel = channel;
    }
    public override bool Equals(object obj) {
			if (obj is NoteRegistryKey r) {
				return r.Channel == this.Channel && r.Note == this.Note;
			}
			return false;
    }
    public bool Equals(NoteRegistryKey obj) => obj.Channel == this.Channel && obj.Note == this.Note;
    public override int GetHashCode() => BitConverter.ToInt32(new byte[4] { this.Note, this.Channel, 0, 0 }, 0);
  }
}
