using MZZT.Data.Binding;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
  public class PresetListItem : Databind<Preset> {
    [SerializeField]
    private TMP_InputField nameField;
		public TMP_InputField NameField => this.nameField;
		[SerializeField]
		private Button deleteButton;
		[SerializeField]
		private Image clickRegion;
		public Image ClickRegion => this.clickRegion;

		protected override void Start() {
			base.Start();

			DataboundListChildToggle.FindToggleFor(this).onValueChanged.AddListener(value => {
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

		protected override void OnInvalidate() {
			this.deleteButton.gameObject.SetActive(!(this.Value?.ReadOnly ?? true));

			base.OnInvalidate();

			this.name = this.Value?.Name ?? "<None>";
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
