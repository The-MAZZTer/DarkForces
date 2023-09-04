using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using TMPro;
using UnityEngine;

namespace MZZT.DarkForces.Showcase {
	public class ThreeDoPolygon : Databind<Df3dObject.Polygon> {
		[Header("3DO Polygon"), SerializeField]
		private TextMeshProUGUI nameField;

		protected override void OnInvalidate() {
			base.OnInvalidate();

			if (this.Value == null) {
				return;
			}

			this.nameField.text = this.Value.Vertices.Count switch {
				3 => "TRI",
				4 => "QUAD",
				_ => this.Value.Vertices.Count.ToString()
			};
		}
	}
}