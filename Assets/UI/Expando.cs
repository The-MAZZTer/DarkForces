using UnityEngine;
using UnityEngine.UI;

namespace MZZT.UI {
	[RequireComponent(typeof(Toggle))]
	public class Expando : MonoBehaviour {
		[SerializeField]
		private GameObject expandedContainer;
		[SerializeField]
		private GameObject collapsedIcon;
		[SerializeField]
		private GameObject expandedIcon;

		private void Start() {
			Toggle toggle = this.GetComponent<Toggle>();
			toggle.onValueChanged.AddListener(this.OnExpandChanged);

			this.OnExpandChanged(toggle.isOn);
		}

		private void OnExpandChanged(bool value) {
			this.expandedContainer.SetActive(value);
			this.collapsedIcon.SetActive(!value);
			this.expandedIcon.SetActive(value);
		}
	}
}
