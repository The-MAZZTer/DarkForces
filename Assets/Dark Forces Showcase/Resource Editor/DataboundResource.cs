using MZZT.Data.Binding;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MZZT.DarkForces.Showcase {
	public class DataboundResource : Databind<ResourceEditorResource>, IPointerClickHandler {

		private const float DOUBLE_CLICK_INTERVAL = 0.5f;

		private float lastClick = float.NegativeInfinity;
		public void OnPointerClick(PointerEventData eventData) {
			if (eventData.button != PointerEventData.InputButton.Left) {
				return;
			}
			if (Time.fixedTime - this.lastClick < DOUBLE_CLICK_INTERVAL) {
				ResourceEditors.Instance.OpenResource(this.Value);
			} else {
				this.lastClick = Time.fixedTime;
			}
		}
	}
}
