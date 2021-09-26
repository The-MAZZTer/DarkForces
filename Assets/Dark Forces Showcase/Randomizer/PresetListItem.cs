using MZZT.DataBinding;
using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Xml;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
  public class PresetListItem : Databound<Preset> {
    [SerializeField]
    private TMP_InputField nameField;
		public TMP_InputField NameField => this.nameField;
		[SerializeField]
		private Button deleteButton;
		[SerializeField]
		private Image clickRegion;
		public Image ClickRegion => this.clickRegion;

		private void Start() {
			this.Toggle.onValueChanged.AddListener(value => {
				this.NameField.interactable = value && this.Value != null && this.Value.Settings != null && !this.Value.ReadOnly;
				this.ClickRegion.gameObject.SetActive(!value);
			});

			this.nameField.onValueChanged.AddListener(value => {
				if (this.Value.ReadOnly) {
					return;
				}

				this.Value.Name = value;

				this.GetComponentInParent<PresetList>().SyncToPlayerPrefs();
			});
		}

		public override void Invalidate() {
			base.Invalidate();

			this.name = this.Value?.Name ?? "<None>";

			this.deleteButton.gameObject.SetActive(!(this.Value?.ReadOnly ?? true));
		}

		public void ExportAsync() {
			this.GetComponentInParent<PresetList>().ExportAsync(this.Value);
		}

		public void Delete() {
			if (this.Value?.ReadOnly ?? true) {
				return;
			}

			this.GetComponentInParent<PresetList>().Delete(this.Value);
		}
	}
}
