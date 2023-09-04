using MZZT.DarkForces.FileFormats;
using System;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
	public class DfMessageBox : Singleton<DfMessageBox> {
		[SerializeField, Header("References")]
		private GameObject background = null;
		[SerializeField]
		private Image window = null;
		[SerializeField]
		private ScrollRect scroll = null;
		[SerializeField]
		private TMP_Text text = null;
		[SerializeField]
		private Button button = null;

		private bool loadedResources = false;
		public async Task ShowAsync(string text) {
			if (PlayerInput.all.Count > 0) {
				PlayerInput.all[0].SwitchCurrentActionMap("UI");
			}

			this.window.gameObject.SetActive(false);
			this.background.SetActive(true);

			this.text.text = text;
			((RectTransform)this.scroll.viewport.transform.GetChild(0)).anchoredPosition = Vector2.zero;

			if (!this.loadedResources) {
				this.loadedResources = true;

				LandruPalette pltt = null;
				LandruAnimation okDialog = null;
				try {
					pltt = await ResourceCache.Instance.GetPaletteAsync("MENU.LFD", "menu");
					if (pltt != null) {
						okDialog = await ResourceCache.Instance.GetAnimationAsync("MENU.LFD", "okaydlg");
						if (okDialog != null && okDialog.Pages.Count >= 1) {
							Texture2D dialog = ResourceCache.Instance.ImportDelt(okDialog.Pages[0], pltt);
							Rect rect = new(0, 0, dialog.width, dialog.height);
							Sprite sprite = Sprite.Create(dialog, rect, new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4() {
								w = 18,
								x = 64,
								y = 26,
								z = 43
							});
							this.window.color = Color.white;
							this.window.type = Image.Type.Sliced;
							this.window.sprite = sprite;
						} else {
							throw new FileNotFoundException();
						}
					} else {
						throw new FileNotFoundException();
					}
				} catch (Exception e) {
					this.window.color = Color.black;
					this.window.type = Image.Type.Simple;
					this.window.sprite = null;

					this.loadedResources = false;
					Debug.LogError(e);
				}

				try {
					if (pltt != null && okDialog != null && okDialog.Pages.Count >= 3) {
						Texture2D ok = ResourceCache.Instance.ImportDelt(okDialog.Pages[2], pltt);
						Texture2D okHover = ResourceCache.Instance.ImportDelt(okDialog.Pages[1], pltt);

						Rect rect = new(0, 0, ok.width, ok.height);
						Sprite okSprite = Sprite.Create(ok, rect, new Vector2(0.5f, 0.5f));
						rect = new Rect(0, 0, okHover.width, okHover.height);
						Sprite okHoverSprite = Sprite.Create(okHover, rect, new Vector2(0.5f, 0.5f));

						this.button.image.color = Color.white;
						this.button.image.sprite = okSprite;
						this.button.spriteState = new SpriteState() {
							highlightedSprite = okSprite,
							disabledSprite = okSprite,
							pressedSprite = okHoverSprite,
							selectedSprite = okSprite
						};
					} else {
						throw new FileNotFoundException();
					}
				} catch (Exception e) {
					this.button.image.color = Color.green;
					this.button.image.sprite = null;
					this.button.spriteState = default;

					this.loadedResources = false;
					Debug.LogError(e);
				}
			}

			Vector2 maxSize = this.GetComponent<CanvasScaler>().referenceResolution /
				this.window.rectTransform.localScale;
			Vector2 minSize = Vector2.zero;
			if (this.window.sprite != null) {
				minSize = new Vector2(this.window.sprite.border.x + this.window.sprite.border.z,
					this.window.sprite.border.w + this.window.sprite.border.y);
			}

			this.window.gameObject.SetActive(true);
			this.window.rectTransform.sizeDelta = maxSize;

			Canvas.ForceUpdateCanvases();

			Vector2 textSize = (this.text.rectTransform.rect.size + new Vector2(20, 10)) /
				this.window.rectTransform.localScale;
			Vector2 viewportSize = this.scroll.viewport.rect.size;
			Vector2 windowSize = this.window.rectTransform.rect.size;
			Vector2 desiredSize = windowSize - viewportSize + textSize;
			desiredSize.x = Mathf.Clamp(desiredSize.x, minSize.x, maxSize.x);
			desiredSize.y = Mathf.Clamp(desiredSize.y, minSize.y, maxSize.y);
			this.window.rectTransform.sizeDelta = desiredSize;

			// Work around Unity bug with cross-scene canvases
			InputSystemUIInputModule input = EventSystem.current.GetComponent<InputSystemUIInputModule>();
			input.enabled = false;
			input.enabled = true;

			while (this.background.activeInHierarchy && this.isActiveAndEnabled) {
				await Task.Delay(25);
			}
		}
	}
}
