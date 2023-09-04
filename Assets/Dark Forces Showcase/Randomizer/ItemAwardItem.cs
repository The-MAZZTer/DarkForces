using MZZT.Data.Binding;
using MZZT.Data.Binding.UI;
using System.Linq;
using TMPro;
using UnityEngine;

namespace MZZT.DarkForces.Showcase {
	public class ItemAwardItem : Databind<ItemAward> {
		[SerializeField]
		private TMP_Dropdown logics;

		protected override void Start() {
			this.logics.ClearOptions();
			this.logics.AddOptions(RandomizerUi.ITEM_LOGICS.ToList());
			this.logics.GetComponent<DataboundStringDropdown>().enabled = true;

			base.Start();
		}

		public void Delete() {
			this.GetComponentInParent<ItemAwardList>().Remove(this.Value);
		}
	}
}
