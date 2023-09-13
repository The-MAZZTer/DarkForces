using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
	public class FilmObjectItem : Databind<LandruFilm.FilmObject> {
		[Header("FILM Object"), SerializeField]
		private TMP_Text text;
		[SerializeField]
		private Button removeButton;

		protected override void OnInvalidate() {
			base.OnInvalidate();

			if (this.Value == null) {
				return;
			}

			this.text.text = this.Value.Type + ' ' + this.Value.Name;

			if (this.Value.Type == "VIEW") {
				foreach (GameObject child in this.transform.Cast<Transform>().Select(x => x.gameObject)) {
					child.SetActive(true);
				}
				this.removeButton.gameObject.SetActive(false);
				this.GetComponent<LayoutGroup>().padding = new RectOffset(10, 10, 5, 5);
			} else if (this.Value.Type == "END") {
				foreach (GameObject child in this.transform.Cast<Transform>().Select(x => x.gameObject)) {
					child.SetActive(false);
				}
				this.GetComponent<LayoutGroup>().padding = new RectOffset(0, 0, 0, 0);
			} else {
				foreach (GameObject child in this.transform.Cast<Transform>().Select(x => x.gameObject)) {
					child.SetActive(true);
				}
				this.removeButton.gameObject.SetActive(true);
				this.GetComponent<LayoutGroup>().padding = new RectOffset(10, 10, 5, 5);
			}
		}
	}
}