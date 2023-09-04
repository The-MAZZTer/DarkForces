using UnityEngine;
using UnityEngine.UI;

namespace MZZT.Data.Binding.UI {
	[RequireComponent(typeof(Button))]
	public class DataboundRevertButton : DataboundFormButton {
		protected override void PerformAction(DataboundUi item) => item.Revert();
	}
}
