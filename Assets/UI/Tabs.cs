using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.UI {
	[RequireComponent(typeof(ToggleGroup))]
	public class Tabs : MonoBehaviour {
		[SerializeField]
		private Transform paneContainer;

		private void Start() {
			foreach ((Toggle toggle, GameObject pane) in this.transform.Cast<Transform>()
				.Select(x => x.GetComponent<Toggle>())
				.Zip(this.paneContainer.Cast<Transform>().Select(x => x.gameObject), (x, y) => (x, y))) {

				toggle.onValueChanged.AddListener(value => {
					pane.SetActive(value);
				});

				pane.SetActive(toggle.isOn);
			}
		}
	}
}
