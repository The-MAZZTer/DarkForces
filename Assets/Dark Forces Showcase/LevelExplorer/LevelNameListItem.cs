using MZZT.DataBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace MZZT.DarkForces.Showcase {
	public class LevelNameListItem : Databound<int> {
		[SerializeField, Header("References")]
		private TMP_Text text = null;

		public override void Invalidate() {
			base.Invalidate();

			this.text.text = LevelLoader.Instance.LevelList.Levels[this.Value].DisplayName;
		}
	}
}
