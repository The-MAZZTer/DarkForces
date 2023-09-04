using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using NVector3 = System.Numerics.Vector3;

namespace MZZT.DarkForces.Showcase {
	public class ThreeDoDetails : Databind<object> {
		[Header("3DO"), SerializeField]
		private GameObject threeDoDetails;
		[SerializeField]
		private GameObject objectDetails;
		[SerializeField]
		private GameObject polygonDetails;

		[Header("Rebase"), SerializeField]
		private TMP_InputField posX;
		[SerializeField]
		private TMP_InputField posY;
		[SerializeField]
		private TMP_InputField posZ;
		[SerializeField]
		private TMP_InputField rotPitch;
		[SerializeField]
		private TMP_InputField rotYaw;
		[SerializeField]
		private TMP_InputField rotRoll;
		[SerializeField]
		private TMP_InputField scaleX;
		[SerializeField]
		private TMP_InputField scaleY;
		[SerializeField]
		private TMP_InputField scaleZ;

		[Header("Colors"), SerializeField]
		private PaletteList colorList;

		private bool userInput = true;

		protected override void OnInvalidate() {
			if (!this.userInput) {
				return;
			}

			this.userInput = false;
			try {
				this.threeDoDetails.SetActive(false);
				this.objectDetails.SetActive(false);
				this.polygonDetails.SetActive(false);

				this.colorList.Clear();

				base.OnInvalidate();

				if (this.Value is Df3dObject.Polygon polygon) {
					this.PopulateColors(polygon);
				}

				this.threeDoDetails.SetActive(this.Value is Df3dObject);
				this.objectDetails.SetActive(this.Value is Df3dObject.Object);
				this.polygonDetails.SetActive(this.Value is Df3dObject.Polygon);
			} finally {
				this.userInput = true;
			}
		}

		private void PopulateColors(Df3dObject.Polygon polygon) {
			try {
				ThreeDoViewer viewer = this.GetComponentInParent<ThreeDoViewer>();
				byte[] paletteBytes = viewer.PreviewModel.Palette;
				Color[] palette = new Color[paletteBytes.Length / 4];

				for (int i = 0; i < palette.Length; i++) {
					palette[i] = new Color(
						paletteBytes[i * 4] / 255f,
						paletteBytes[i * 4 + 1] / 255f,
						paletteBytes[i * 4 + 2] / 255f,
						paletteBytes[i * 4 + 3] / 255f
					);
				}

				this.colorList.AddRange(palette);

				this.colorList.SelectedDatabound = this.colorList.Children.ElementAt(polygon.Color);
			} catch (OperationCanceledException) {
			}
		}

		public async void OnColorChangedAsync() {
			if (!this.userInput) {
				return;
			}

			Df3dObject.Polygon polygon = (Df3dObject.Polygon)this.Value;
			byte color = (byte)((Component)this.colorList.SelectedDatabound).transform.GetSiblingIndex();

			if (polygon.Color == color) {
				return;
			}

			polygon.Color = color;

			await this.GetComponentInParent<ThreeDoViewer>().OnColorChangedAsync();
		}

		public async void OnShadingModeChangedAsync() {
			if (!this.userInput) {
				return;
			}

			await this.GetComponentInParent<ThreeDoViewer>().OnShadingModeChangedAsync();
		}

		public async void RebaseAsync() {
			if (
				!float.TryParse(this.posX.text, out float posX) ||
				!float.TryParse(this.posY.text, out float posY) ||
				!float.TryParse(this.posZ.text, out float posZ) ||
				!float.TryParse(this.rotPitch.text, out float rotPitch) ||
				!float.TryParse(this.rotYaw.text, out float rotYaw) ||
				!float.TryParse(this.rotRoll.text, out float rotRoll) ||
				!float.TryParse(this.scaleX.text, out float scaleX) ||
				!float.TryParse(this.scaleY.text, out float scaleY) ||
				!float.TryParse(this.scaleZ.text, out float scaleZ)
			) {
				await DfMessageBox.Instance.ShowAsync("Invalid values.");
				return;
			}

			Vector3 deltaPos = new(posX, posY, posZ);
			Quaternion deltaRot = Quaternion.Euler(rotPitch, rotYaw, rotRoll);
			Vector3 deltaScale = new(scaleX, scaleY, scaleZ);

			if (deltaPos == Vector3.zero && deltaRot == Quaternion.identity && deltaScale == Vector3.one) {
				return;
			}

			Df3dObject threeDo = (Df3dObject)this.Value;
			foreach (Df3dObject.Object obj in threeDo.Objects) {
				foreach (Df3dObject.Polygon polygon in obj.Polygons) {
					foreach ((NVector3 x, int i) in polygon.Vertices.Select((x, i) => (x, i)).ToArray()) {
						Vector3 pos = x.ToUnity();
						pos = deltaRot * pos;
						pos = new Vector3(pos.x * deltaScale.x, pos.y * deltaScale.y, pos.z * deltaScale.z);
						pos += deltaPos;
						polygon.Vertices[i] = pos.ToNet();
					}
				}
			}
			
			this.GetComponentInParent<ThreeDoViewer>().OnDirty();

			await this.GetComponentInParent<ThreeDoViewer>().RefreshModelAsync();

			this.rotRoll.text = this.rotYaw.text = this.rotPitch.text = this.posZ.text = this.posY.text = this.posX.text = "0";
			this.scaleZ.text = this.scaleY.text = this.scaleX.text = "1";
		}

		public void OnNameChanged() {
			if (!this.userInput) {
				return;
			}

			this.GetComponentInParent<ThreeDoViewer>().ObjectList.SelectedDatabound?.Invalidate();

			this.GetComponentInParent<ThreeDoViewer>().OnDirty();
		}
	}
}