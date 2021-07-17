using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DataBinding {
	[RequireComponent(typeof(Button))]
	public class DataboundApplyButton : DataboundFormButton {
		protected override void PerformAction(IDataboundUi item) => item.Apply();
	}
}
