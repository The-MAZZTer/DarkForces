using MZZT.DarkForces.FileFormats;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
	/// <summary>
	/// Powers the ESC menu on showcases.
	/// </summary>
	public class PauseMenu : Singleton<PauseMenu> {
		[SerializeField, Header("References")]
		private GameObject background = null;
		[SerializeField]
		private Image loading = null;
		[SerializeField]
		protected string uiMap = "UI";
		[SerializeField]
		protected string actionMap = null;
		[SerializeField]
		private bool enableLoadingScreen = true;
		public bool EnableLoadingScreen {
			get => this.enableLoadingScreen;
			set => this.enableLoadingScreen = value;
		}

		protected virtual void Start() {
			PlayerInput.all[0].SwitchCurrentActionMap(this.actionMap);
		}

		protected int loadingCount;
		private TaskCompletionSource<bool> taskSource;
		public virtual async Task BeginLoadingAsync() {
			if (this.loadingCount == 0 && this.enableLoadingScreen) {
				if (this.background != null) {
					this.MenuOpen = false;
				}
				this.IsInUI = true;

				if (this.loading.sprite == null) {
					DfPalette waitPal = await ResourceCache.Instance.GetPaletteAsync("WAIT.PAL");
					if (waitPal != null) {
						DfBitmap waitBm = await ResourceCache.Instance.GetBitmapAsync("WAIT.BM");
						if (waitBm != null) {
							Texture2D wait = ResourceCache.Instance.ImportBitmap(waitBm.Pages[0], waitPal);
							Rect rect = new(0, 0, wait.width, wait.height);
							this.loading.sprite = Sprite.Create(wait, rect, new Vector2(0.5f, 0.5f));
							this.loading.color = Color.white;
						}
					}
				}

				this.loading.gameObject.SetActive(true);
				this.loadingCount++;

				this.taskSource = new TaskCompletionSource<bool>();
				await this.taskSource.Task;
			} else {
				this.loadingCount++;
			}
		}

		public virtual void EndLoading() {
			this.loadingCount--;

			if (this.loadingCount == 0 && this.loading.gameObject.activeSelf) {
				this.loading.gameObject.SetActive(false);
				this.IsInUI = false;
			}
		}

		private bool IsInUI {
			get => Time.timeScale == 0;
			set {
				if (this.IsInUI == value) {
					return;
				}

				Time.timeScale = value ? 0 : 1;
				PlayerInput.all[0].SwitchCurrentActionMap(value ? this.uiMap : this.actionMap);
				//PlayerInput.all[0].uiInputModule.enabled = false;
			}
		}

		private bool MenuOpen {
			get => this.background.activeSelf;
			set {
				if (this.MenuOpen == value) {
					return;
				}

				this.background.SetActive(value);
			}
		}

		private int taskCount = 0;
		protected virtual void Update() {
			//PlayerInput.all[0].uiInputModule.enabled = true;

			if (this.taskSource != null) {
				this.taskCount++;

				if (this.taskCount >= 2) {
					this.taskSource.SetResult(true);
					this.taskSource = null;
					this.taskCount = 0;
				}
			}
		}

		protected virtual void GenerateMenu() {
		}

		private bool init = false;
		public virtual void OpenMenu() {
			if (!this.init) {
				this.GenerateMenu();
				this.init = true;
			}

			this.IsInUI = true;
			this.MenuOpen = true;
		}

		public async virtual void OnMenuAsync(InputAction.CallbackContext context) {
			if (this.loadingCount > 0 || context.phase != InputActionPhase.Performed) {
				return;
			}

			if (this.MenuOpen) {
				await this.CloseMenuAsync();
				return;
			}

			this.OpenMenu();
		}

		public virtual void ApplyMenuChanges() {
		}

		public virtual Task CloseMenuAsync() {
			this.ApplyMenuChanges();

			this.IsInUI = this.loadingCount > 0;
			this.MenuOpen = false;
			return Task.CompletedTask;
		}

		public async void OnCloseAsync() {
			await this.CloseMenuAsync();
		}

		public void OnReturnToMenu() {
			this.ApplyMenuChanges();

			SceneManager.LoadScene("Menu");
		}

		protected override void OnDestroy() {
			base.OnDestroy();

			Time.timeScale = 1;
		}
	}
}
