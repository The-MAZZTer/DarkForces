using MZZT.Data.Binding;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
  public class ResourceEditorTabItem : Databind<ResourceEditorTab> {
		[Header("Tab"), SerializeField]
		private Image icon;
		[SerializeField]
		private TMP_Text closeCleanImage;
		[SerializeField]
		private TMP_Text closeDirtyImage;

		public void SetIcon(Sprite icon) {
			this.icon.sprite = icon;
			this.icon.gameObject.SetActive(true);
		}

		public void SetIsDirty(bool dirty) {
			this.closeDirtyImage.gameObject.SetActive(dirty);
			this.closeCleanImage.gameObject.SetActive(!dirty);
		}

		public async void Close() {
      await ResourceEditors.Instance.CloseTabAsync(this);
		}
  }
}
