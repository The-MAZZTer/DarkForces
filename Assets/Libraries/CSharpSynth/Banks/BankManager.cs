using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CSharpSynth.Banks {
  public static class BankManager {
    public const int DEFAULT_BANK_SIZE = 256; //midi standard only needs 0-127. The rest is extra space.
    public const int DEFAULT_DRUMBANK_SIZE = 128;
    //--Static Properties
    public static int Count => Banks.Count;
    public static List<InstrumentBank> Banks { get; } = new List<InstrumentBank>();
    //--Public Static Methods
    public static void AddBank(InstrumentBank bank) => Banks.Add(bank);
    public static void RemoveBank(int index) {
      Banks[index].Clear();
      Banks.RemoveAt(index);
    }
    public static void RemoveBank(InstrumentBank bank) {
      int index = Banks.IndexOf(bank);
      if (index > -1) {
        RemoveBank(index);
      }
    }
    public static void RemoveBank(string bankname) {
      InstrumentBank bank = GetBank(bankname);
      if (bank != null) {
        RemoveBank(bank);
      }
    }
    public static int GetBankIndex(string bankname) {
      bankname = /*Path.GetFileName(*/bankname/*)*/.ToLower();
      /*for (int x = 0; x < Banks.Count; x++) {
        if (Path.GetFileName(Banks[x].BankPath).ToLower().Equals(bankname)) {
          return x;
        }
      }
      return -1;*/
      (InstrumentBank bank, int index) = Banks
        .Select((x, i) => (x, i)).FirstOrDefault(x => x.x.BankPath.ToLower() == bankname);
      return bank == null ? -1 : index;
    }
    public static int GetBankIndex(InstrumentBank bank) => Banks.IndexOf(bank);
    public static InstrumentBank GetBank(int index) => Banks[index];
    public static InstrumentBank GetBank(string bankname) {
      /*int index = GetBankIndex(bankname);
      if (index > -1) {
        return Banks[index];
      }
      return null;*/
      bankname = /*Path.GetFileName(*/bankname/*)*/.ToLower();
      return Banks.FirstOrDefault(x => x.BankPath.ToLower() == bankname);
    }
    public static void Clear() {
      for (int x = 0; x < Banks.Count; x++) {
        Banks[x].Clear();
      }
      Banks.Clear();
    }
  }
}
