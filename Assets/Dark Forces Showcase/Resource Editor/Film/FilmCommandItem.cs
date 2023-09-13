using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
	public class FilmCommandItem : Databind<LandruFilm.Command> {
		[Header("FILM Command"), SerializeField]
		private TMP_Text text;
		[SerializeField]
		private Button removeButton;

		protected override void OnInvalidate() {
			base.OnInvalidate();

			if (this.Value == null) {
				return;
			}

			LandruFilm.CommandTypes type = this.Value.Type;
			bool disabled = type.HasFlag(LandruFilm.CommandTypes.Disabled);
			type &= ~LandruFilm.CommandTypes.Disabled;

			this.text.text = $"{(disabled ? "//" : "")}{type} {string.Join(" ", this.Value.Parameters.Select(x => x.ToString()))}";

			if (type == LandruFilm.CommandTypes.Time) {
				IEnumerable x = (IEnumerable)this.ParentValue;
				int index = x.Cast<LandruFilm.Command>().TakeWhile(x => x != this.Value).Count();
				int count = x.Cast<LandruFilm.Command>().Count();
				bool allowRemove = true;
				if (count > index + 1) {
					LandruFilm.CommandTypes nextType = x.Cast<LandruFilm.Command>().ElementAt(index + 1).Type;
					nextType &= ~LandruFilm.CommandTypes.Disabled;
					allowRemove = nextType == LandruFilm.CommandTypes.Time || nextType == LandruFilm.CommandTypes.End;
				} else {
					allowRemove = true;
				}

				foreach (GameObject child in this.transform.Cast<Transform>().Select(x => x.gameObject)) {
					child.SetActive(true);
				}
				this.removeButton.gameObject.SetActive(allowRemove);
				this.GetComponent<LayoutGroup>().padding = new RectOffset(10, 10, 5, 5);
			} else if (type == LandruFilm.CommandTypes.End) {
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