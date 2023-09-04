using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using MZZT.FileFormats;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
	public class ThreeDoViewer : Databind<Df3dObject>, IResourceViewer {
		[Header("3DO"), SerializeField]
		private ThreeDoObjectList objectList;
		public ThreeDoObjectList ObjectList => this.objectList;
		[SerializeField]
		private ThreeDoDetails details;

		[Header("Preview"), SerializeField]
		private Transform previewContainer;
		[SerializeField]
		private Camera preview;
		[SerializeField]
		private Slider lightLevel;
		[SerializeField]
		private RawImage previewRender;
		[SerializeField]
		private ThreeDoModel previewModel;
		public ThreeDoModel PreviewModel => this.previewModel;
		[SerializeField]
		private Shader colorShader;
		[SerializeField]
		private Shader simpleShader;
		[SerializeField]
		private RenderTexture thumbnailRenderer;

		public string TabName => this.filePath == null ? "New 3DO" : Path.GetFileName(this.filePath);
		public event EventHandler TabNameChanged;

		public Sprite Thumbnail { get; private set; }
		public event EventHandler ThumbnailChanged;

		private void SetThumbnail(Sprite thumbnail) {
			this.Thumbnail = thumbnail;
			this.ThumbnailChanged?.Invoke(this, new EventArgs());
		}

		public void ResetDirty() {
			if (!this.IsDirty) {
				return;
			}

			this.IsDirty = false;
			this.IsDirtyChanged?.Invoke(this, new());
		}

		public void OnDirty() {
			if (this.IsDirty) {
				return;
			}

			this.IsDirty = true;
			this.IsDirtyChanged?.Invoke(this, new());
		}

		public bool IsDirty { get; private set; }
		public event EventHandler IsDirtyChanged;

		protected override void Start() {
			base.Start();

			OrbitCamera orbit = this.preview.GetComponent<OrbitCamera>();
			PlayerInput.all[0].actions["Look"].started += orbit.OnLook;
			PlayerInput.all[0].actions["Look"].performed += orbit.OnLook;
			PlayerInput.all[0].actions["Look"].canceled += orbit.OnLook;
			PlayerInput.all[0].actions["ScrollWheel"].started += orbit.OnZoom;
			PlayerInput.all[0].actions["ScrollWheel"].performed += orbit.OnZoom;
			PlayerInput.all[0].actions["ScrollWheel"].canceled += orbit.OnZoom;
		}

		protected override void OnDisable() {
			base.OnDisable();

			if (this.previewContainer == null) {
				return;
			}

			this.previewContainer.gameObject.SetActive(false);
		}

		protected override void OnEnable() {
			base.OnEnable();

			this.previewContainer.gameObject.SetActive(true);
		}

		private void OnDestroy() {
			if (this.previewContainer != null) {
				DestroyImmediate(this.previewContainer.gameObject);
			}

			if (this.preview == null) {
				return;
			}

			if (!this.preview.TryGetComponent(out OrbitCamera orbit)) {
				return;
			}

			if (PlayerInput.all.Count < 1) {
				return;
			}

			PlayerInput.all[0].actions["Look"].started -= orbit.OnLook;
			PlayerInput.all[0].actions["Look"].performed -= orbit.OnLook;
			PlayerInput.all[0].actions["Look"].canceled -= orbit.OnLook;
			PlayerInput.all[0].actions["ScrollWheel"].started -= orbit.OnZoom;
			PlayerInput.all[0].actions["ScrollWheel"].performed -= orbit.OnZoom;
			PlayerInput.all[0].actions["ScrollWheel"].canceled -= orbit.OnZoom;
		}

		private string filePath;
		public async Task LoadAsync(ResourceEditorResource resource, IFile file) {
			this.Value = (Df3dObject)file;
			this.filePath = resource?.Path;

			await this.GeneratePreviewAsync();

			this.OnSelectedItemChanged();
		}

		public async Task GeneratePreviewAsync() {
			this.modelRefresh?.Cancel();

			this.previewContainer.SetParent(null, false);
			this.previewContainer.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
			this.previewContainer.localScale = Vector3.one;

			foreach (Transform child in this.previewModel.transform.Cast<Transform>().ToArray()) {
				DestroyImmediate(child.gameObject);
			}

			Vector3[] pos = this.Value.Objects.SelectMany(x => x.Polygons).SelectMany(x => x.Vertices).Select(x => x.ToUnity() * LevelGeometryGenerator.GEOMETRY_SCALE).ToArray();
			Vector3 center = new(pos.Select(x => x.x).Average(), pos.Select(x => -x.y).Average(), pos.Select(x => x.z).Average());
			float radius = Math.Max(10, pos.Max(x => (center - x).magnitude));
			this.preview.GetComponent<OrbitCamera>().Set(center, radius * 0.4f);

			this.modelRefresh = new CancellationTokenSource();
			try {
				await this.previewModel.SetAsync(this.filePath, this.Value, (int)this.lightLevel.value, this.colorShader, this.simpleShader, this.modelRefresh.Token);
			} catch (OperationCanceledException) {
			}
		}

		private CancellationTokenSource modelRefresh;

		private void ResizePreview(Vector2 size) {
			int width = Mathf.RoundToInt(size.x);
			int height = Mathf.RoundToInt(size.y);
			RenderTexture texture = (RenderTexture)this.previewRender.mainTexture;
			if (texture.width != width || texture.height != height) {
				if (texture.IsCreated()) {
					texture.Release();
				}
				texture.width = width;
				texture.height = height;
				texture.Create();
			}
			this.preview.aspect = size.x / size.y;
		}

		public async void OnLightLevelChangedAsync(float value) {
			this.modelRefresh?.Cancel();
			this.modelRefresh = new CancellationTokenSource();
			try {
				await this.previewModel.RefreshPaletteAsync((int)this.lightLevel.value, this.modelRefresh.Token);
			} catch (OperationCanceledException) {
			}
		}

		public async void OnPaletteChangedAsync(string value) {
			this.OnDirty();

			this.modelRefresh?.Cancel();
			this.modelRefresh = new CancellationTokenSource();
			try {
				await this.previewModel.RefreshPaletteAsync((int)this.lightLevel.value, this.modelRefresh.Token);
			} catch (OperationCanceledException) {
			}
		}

		public async Task RefreshModelAsync() {
			this.modelRefresh?.Cancel();

			Vector3[] pos = this.Value.Objects.SelectMany(x => x.Polygons).SelectMany(x => x.Vertices).Select(x => x.ToUnity() * LevelGeometryGenerator.GEOMETRY_SCALE).ToArray();
			Vector3 center = new(pos.Select(x => x.x).Average(), pos.Select(x => -x.y).Average(), pos.Select(x => x.z).Average());
			this.preview.GetComponent<OrbitCamera>().Set(center);

			this.modelRefresh = new CancellationTokenSource();
			try {
				await this.previewModel.SetAsync(this.filePath, this.Value, (int)this.lightLevel.value, this.colorShader, this.simpleShader, this.modelRefresh.Token);
			} catch (OperationCanceledException) {
			}
		}

		public async void OnObjectRemoved(int index, Df3dObject.Object obj) {
			this.OnDirty();

			await this.RefreshModelAsync();
		}

		public async void OnPolygonRemoved(int index, Df3dObject.Polygon polygon) {
			this.OnDirty();

			await this.RefreshModelAsync();
		}

		public async void OnTextureChangedAsync() {
			this.OnDirty();

			this.modelRefresh?.Cancel();
			this.modelRefresh = new CancellationTokenSource();
			try {
				await this.previewModel.RefreshTexturesAsync(this.objectList.SelectedValue, this.modelRefresh.Token);
			} catch (OperationCanceledException) {
			}
		}

		public async Task OnColorChangedAsync() {
			this.OnDirty();

			this.modelRefresh?.Cancel();
			this.modelRefresh = new CancellationTokenSource();
			try {
				await this.previewModel.RefreshTexturesAsync((Df3dObject.Polygon)this.objectList.SelectedDatabound.Value, this.modelRefresh.Token);
			} catch (OperationCanceledException) {
			}
		}

		public async Task OnShadingModeChangedAsync() {
			this.OnDirty();

			this.modelRefresh?.Cancel();
			this.modelRefresh = new CancellationTokenSource();
			try {
				await this.previewModel.RefreshTexturesAsync((Df3dObject.Polygon)this.objectList.SelectedDatabound.Value, this.modelRefresh.Token);
			} catch (OperationCanceledException) {
			}
		}

		public void OnSelectedItemChanged() {
			IDatabind selected = this.objectList.SelectedDatabound;
			this.details.Value = selected?.Value;
		}

		private void Update() {
			this.preview.targetTexture = this.thumbnailRenderer;
			this.preview.aspect = 1;
			this.preview.Render();

			Texture2D renderedTexture = new(this.thumbnailRenderer.width, this.thumbnailRenderer.height, TextureFormat.RGB24, false);
			RenderTexture activeRenderTexture = RenderTexture.active;
			try {
				RenderTexture.active = this.thumbnailRenderer;

				renderedTexture.ReadPixels(new Rect(0, 0, this.thumbnailRenderer.width, this.thumbnailRenderer.height), 0, 0);
				renderedTexture.Apply();
			} finally {
				RenderTexture.active = activeRenderTexture;
				this.preview.targetTexture = (RenderTexture)this.previewRender.mainTexture;
			}

			this.SetThumbnail(Sprite.Create(renderedTexture, new Rect(0, 0, this.thumbnailRenderer.width, this.thumbnailRenderer.height), new Vector2(0.5f, 0.5f)));

			Vector2 size = ((RectTransform)this.previewRender.transform).rect.size;
			this.ResizePreview(size);
		}

		public async void SaveAsync() {
			bool canSave = Directory.Exists(Path.GetDirectoryName(this.filePath));
			if (!canSave) {
				this.SaveAsAsync();
				return;
			}

			// Writing to the stream is loads faster than to the file. Not sure why. Unity thing probably, doesn't happen on .NET 6.
			using MemoryStream mem = new();
			await this.Value.SaveAsync(mem);

			mem.Position = 0;
			using FileStream stream = new(this.filePath, FileMode.Create, FileAccess.Write, FileShare.None);
			await mem.CopyToAsync(stream);

			this.ResetDirty();
		}

		public async void SaveAsAsync() {
			string path = await ResourceEditors.Instance.PickSaveLocationAsync(this.filePath, new[] { "*.3DO" });
			if (string.IsNullOrEmpty(path)) {
				return;
			}
			this.filePath = path;
			this.TabNameChanged?.Invoke(this, new EventArgs());

			bool canSave = Directory.Exists(Path.GetDirectoryName(this.filePath));
			if (!canSave) {
				return;
			}

			this.SaveAsync();
		}
	}
}
