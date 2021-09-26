using MZZT.DataBinding;
using System.Linq;
using TMPro;
using UnityEngine;

namespace MZZT.DarkForces.Showcase {
	public class ItemAwardItem : Databound<ItemAward> {
		[SerializeField]
		private TMP_Dropdown logics;

		private void Start() {
			this.logics.ClearOptions();
			this.logics.AddOptions(RandomizerUi.ITEM_LOGICS.ToList());
			this.logics.GetComponent<DataboundTmpStringDropdown>().enabled = true;
		}

		public void Delete() {
			this.GetComponentInParent<ItemAwardList>().Remove(this.Value);
		}
	}
}
