using MZZT.Data.Binding;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MZZT.DarkForces.Showcase {
	public class DataboundResourceContainer : Databind<ResourceListContainer> {
		[SerializeField]
		private DataboundList<ResourceEditorResource> children;
		[SerializeField]
		private GameObject expandoCollapsed;
		[SerializeField]
		private GameObject expandoExpanded;

		protected override void Start() {
			this.children.ToggleGroup = this.GetComponentInParent<DataboundList<ResourceListContainer>>().ToggleGroup;

			if (this.Value.Resources.Count == 0) {
				this.expandoCollapsed.SetActive(false);
				this.expandoExpanded.SetActive(false);
			}

			base.Start();
		}

		public void OnExpandChanged(bool value) {
			if (this.Value.Resources.Count == 0) {
				this.expandoCollapsed.SetActive(false);
				this.expandoExpanded.SetActive(false);
				return;
			}

			this.expandoCollapsed.SetActive(!value);
			this.expandoExpanded.SetActive(value);

			this.children.gameObject.SetActive(value);
		}

		private const float DOUBLE_CLICK_INTERVAL = 0.5f;

		private float lastClick = float.NegativeInfinity;
		public void OnPointerClick(BaseEventData eventData) {
			if (((PointerEventData)eventData).button != PointerEventData.InputButton.Left) {
				return;
			}
			if (Time.fixedTime - this.lastClick < DOUBLE_CLICK_INTERVAL) {
				ResourceEditors.Instance.OpenResource(this.Value.Resource);
			} else {
				this.lastClick = Time.fixedTime;
			}
		}
	}
}
