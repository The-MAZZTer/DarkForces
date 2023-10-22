using MZZT.DarkForces.Converters;
using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using MZZT.IO.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
	public class VueDetails : Databind<KeyValuePair<string, AutodeskVue.VueObject>> {
		[Header("VUE"), SerializeField]
		private TMP_InputField nameInput;

		[Header("Preview"), SerializeField]
		private Transform previewContainer;
		[SerializeField]
		private Camera preview;
		[SerializeField]
		private Animation threeDo;
		[SerializeField]
		private TextMeshProUGUI previewTime;
		[SerializeField]
		private string previewTimeFormat = @"{0:mm\:ss} / {1:mm\:ss}";
		[SerializeField]
		private Slider previewPosition;
		[SerializeField]
		private Toggle previewSmooth;
		[SerializeField]
		private RawImage previewRender;
		[SerializeField]
		private TextMeshProUGUI playIcon;
		[SerializeField]
		private TextMeshProUGUI pauseIcon;
		[SerializeField]
		private Shader colorShader;
		[SerializeField]
		private Shader simpleShader;
		[SerializeField]
		private RenderTexture thumbnailRenderer;

		[Header("Rebase"), SerializeField]
		private TMP_InputField startX;
		[SerializeField]
		private TMP_InputField startY;
		[SerializeField]
		private TMP_InputField startZ;
		[SerializeField]
		private TMP_InputField rotPitch;
		[SerializeField]
		private TMP_InputField rotYaw;
		[SerializeField]
		private TMP_InputField rotRoll;
		[SerializeField]
		private TMP_InputField scalePosX;
		[SerializeField]
		private TMP_InputField scalePosY;
		[SerializeField]
		private TMP_InputField scalePosZ;
		[SerializeField]
		private TMP_InputField scaleObjX;
		[SerializeField]
		private TMP_InputField scaleObjY;
		[SerializeField]
		private TMP_InputField scaleObjZ;

		private bool userInput = true;

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

		protected override void OnInvalidate() {
			base.OnInvalidate();

			if (!this.userInput) {
				return;
			}

			this.gameObject.SetActive(this.Value.Key != null);
			if (this.Value.Key == null) {
				return;
			}

			this.userInput = false;
			try {
				this.nameInput.text = this.Value.Key;
			} finally {
				this.userInput = true;
			}

			this.threeDo.playAutomatically = false;
			this.threeDo.wrapMode = WrapMode.Loop;

			this.GeneratePreview(true);

			bool hasFrame = this.Value.Value.Frames.Count > 0;
			Matrix4x4 frame = this.Value.Value.Frames.FirstOrDefault().ToUnity();
			Vector3 pos = frame.GetUnityPositionFromAutodesk();
			this.startX.text = (!hasFrame ? 0 : pos.x).ToString("0.0");
			this.startY.text = (!hasFrame ? 0 : -pos.y).ToString("0.0");
			this.startZ.text = (!hasFrame ? 0 : pos.z).ToString("0.0");
		}

		public void OnNameChanged(string value) {
			if (!this.userInput) {
				return;
			}

			if (value == this.Value.Key) {
				return;
			}

			VueViewer viewer = this.GetComponentInParent<VueViewer>();
			VueObjectList list = viewer.Vues;
			Dictionary<string, AutodeskVue.VueObject> objects = (Dictionary<string, AutodeskVue.VueObject>)list.Value;
			if (objects.Keys.Except(new[] { this.Value.Key }).Contains(value)) {
				return;
			}

			AutodeskVue.VueObject vueObject = this.Value.Value;
			objects.Remove(this.Value.Key);
			objects[value] = vueObject;

			this.userInput = false;
			try {
				list.Invalidate();
				this.Value = list.SelectedValue = new KeyValuePair<string, AutodeskVue.VueObject>(value, vueObject);
			} finally {
				this.userInput = true;
			}

			viewer.OnDirty();
		}

		public void OnFramerateChanged(string value) {
			if (!float.TryParse(value, out float framerate) || framerate <= 0) {
				return;
			}

			this.framerate = framerate;

			if (this.vueAnimation != null && this.vueAnimation.speed > 0) {
				this.vueAnimation.speed = framerate;
			}

			this.GetComponentInParent<VueViewer>().OnDirty();
		}

		private float framerate = ThreeDoRenderer.VUE_DEFAULT_FRAMERATE;
		public void GeneratePreview(bool forcePlay = false) {
			AutodeskVue.VueObject obj = this.Value.Value;
			this.previewContainer.gameObject.SetActive(obj != null);

			this.previewContainer.SetParent(null, false);
			this.previewContainer.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
			this.previewContainer.localScale = Vector3.one;

			bool isPlaying = forcePlay || (this.threeDo.isPlaying && this.vueAnimation.speed > 0);

			this.StopPreview();
			this.threeDo.Stop();
			this.vueAnimation = null;
			if (this.threeDo.GetClipCount() > 0) {
				this.threeDo.RemoveClip("VUE");
			}

			if (obj == null) {
				this.previewTime.text = "--:-- / --:--";
				this.vueAnimation = null;
				return;
			}

			Vector3 center;
			float radius;
			LineRenderer lines = this.preview.GetComponent<LineRenderer>();
			if (obj.Frames.Count > 0) {
				Matrix4x4[] frames = obj.Frames.Select(x => x.ToUnity()).ToArray();
				Vector3[] pos = frames.Select(x => x.GetUnityPositionFromAutodesk() * LevelGeometryGenerator.GEOMETRY_SCALE).ToArray();
				center = new(pos.Select(x => x.x).Average(), pos.Select(x => x.y).Average(), pos.Select(x => x.z).Average());
				radius = Math.Max(10, pos.Max(x => (center - x).magnitude));

				this.threeDo.AddClip(obj.ToClip(this.previewSmooth.isOn), "VUE");
				if (isPlaying) {
					this.PlayPreview();
				}

				lines.positionCount = pos.Length;
				lines.SetPositions(pos);
			} else {
				this.vueAnimation = null;
				center = Vector3.zero;
				this.threeDo.transform.position = center;
				radius = 10;

				lines.positionCount = 0;
			}

			this.preview.GetComponent<OrbitCamera>().Set(center, radius * 0.4f);
		}

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

		private AnimationState vueAnimation;
		private void Update() {
			float sliderPos;
			if (this.vueAnimation != null) {
				this.previewTime.text = string.Format(this.previewTimeFormat,
					TimeSpan.FromSeconds((this.vueAnimation.length > 0 ? (this.vueAnimation.time % this.vueAnimation.length) : 0) / this.framerate),
					TimeSpan.FromSeconds(this.vueAnimation.length / this.framerate));
				sliderPos = this.vueAnimation.normalizedTime % 1;
			} else {
				AutodeskVue.VueObject obj = this.Value.Value;
				if (obj != null) {
					this.previewTime.text = string.Format(this.previewTimeFormat, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(obj.Frames.Count / this.framerate));
				} else {
					this.previewTime.text = "--:-- / --:--";
				}
				sliderPos = 0;
			}

			if (this.threeDo.isPlaying && this.vueAnimation.speed > 0 && !this.sliderMouseDown) {
				this.userInput = false;
				try {
					this.previewPosition.value = sliderPos;
				} finally {
					this.userInput = true;
				}
			}

			if (!PlayerInput.all[0].currentActionMap.FindAction("Click").IsPressed()) {
				this.sliderMouseDown = false;
			}

			this.playIcon.gameObject.SetActive(this.isPaused || !this.threeDo.isPlaying || this.vueAnimation.speed == 0);
			this.pauseIcon.gameObject.SetActive(!this.isPaused && this.threeDo.isPlaying && this.vueAnimation.speed > 0);

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

			VueViewer viewer = this.GetComponentInParent<VueViewer>();
			viewer.SetThumbnail(Sprite.Create(renderedTexture, new Rect(0, 0, this.thumbnailRenderer.width, this.thumbnailRenderer.height), new Vector2(0.5f, 0.5f)));

			Vector2 size = ((RectTransform)this.previewRender.transform).rect.size;
			this.ResizePreview(size);
		}

		private bool sliderMouseDown = false;
		public void OnSliderMouseDown() {
			this.sliderMouseDown = true;
		}

		public void OnPreviewSeek(float value) {
			if (!this.userInput) {
				return;
			}

			if (this.vueAnimation == null) {
				if (this.threeDo.GetClipCount() == 0) {
					return;
				}

				this.vueAnimation = this.threeDo.PlayQueued("VUE");
			}

			this.vueAnimation.normalizedTime = value;
			this.threeDo.Sample();
		}

		private bool isPaused = false;
		public void PlayPreview() {
			if (this.isPaused) {
				this.vueAnimation.speed = this.framerate;
				this.isPaused = false;
				return;
			}

			if (this.threeDo.isPlaying && this.vueAnimation.speed > 0) {
				this.vueAnimation.speed = 0;
				this.isPaused = true;

				this.userInput = false;
				try {
					this.previewPosition.value = this.vueAnimation.normalizedTime % 1;
				} finally {
					this.userInput = true;
				}
				return;
			}

			this.StopPreview();

			if (this.vueAnimation == null) {
				this.vueAnimation = this.threeDo.PlayQueued("VUE");
			}
			this.vueAnimation.speed = this.framerate;
		}

		public void StopPreview() {
			if (this.vueAnimation != null) {
				this.vueAnimation.speed = 0;
				this.vueAnimation.time = 0;
				this.threeDo.Sample();

				this.userInput = false;
				try {
					this.previewPosition.value = 0;
				} finally {
					this.userInput = true;
				}
			}

			this.isPaused = false;
		}

		protected override void OnDisable() {
			base.OnDisable();

			if (this.previewContainer == null) {
				return;
			}

			this.StopPreview();

			this.previewContainer.gameObject.SetActive(false);
		}

		protected override void OnEnable() {
			base.OnEnable();

			this.previewContainer.gameObject.SetActive(true);
		}

		public void OnShowPathChanged(bool value) {
			this.preview.GetComponent<LineRenderer>().enabled = value;
		}

		private string lastFolder;
		public async void OnChange3doClickAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new() {
				AllowNavigateGob = true,
				AllowNavigateLfd = false,
				Filters = new[] {
					FileBrowser.FileType.Generate("3D Objects", "*.3DO"),
					FileBrowser.FileType.AllFiles
				},
				SelectButtonText = "Open",
				SelectedFileMustExist = true,
				SelectedPathMustExist = true,
				SelectFolder = false,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = $"Preview with 3DO"
			});

			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			Df3dObject obj = await DfFileManager.Instance.ReadAsync<Df3dObject>(path);
			if (obj == null) {
				await DfMessageBox.Instance.ShowAsync("Could not read file.");
				return;
			}

			foreach (Transform child in this.threeDo.transform.Cast<Transform>().ToArray()) {
				DestroyImmediate(child.gameObject);
			}

			GameObject go = new() {
				name = obj.Name
			};
			go.transform.SetParent(this.threeDo.transform, false);
			ThreeDoModel model = go.AddComponent<ThreeDoModel>();
			await model.SetAsync(path, obj, 31, this.colorShader, this.simpleShader, default);
		}

		private string lastFolder2;
		public async void ExportAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new() {
				AllowNavigateGob = false,
				AllowNavigateLfd = false,
				Filters = new[] {
					FileBrowser.FileType.Generate("VUE File", "*.VUE"),
					FileBrowser.FileType.AllFiles
				},
				SelectButtonText = "Export",
				SelectedFileMustExist = false,
				SelectedPathMustExist = true,
				SelectFolder = false,
				StartPath = this.lastFolder2 ?? FileLoader.Instance.DarkForcesFolder,
				Title = $"Export VUE",
				ValidateFileName = true
			});

			if (path == null) {
				return;
			}

			this.lastFolder2 = Path.GetDirectoryName(path);

			AutodeskVue vue = this.GetComponentInParent<VueViewer>().Value;
			vue = vue.Clone();
			Dictionary<string, AutodeskVue.VueObject> objects = vue.Vues[0].Objects;
			objects.Clear();
			objects[this.Value.Key] = this.Value.Value;

			// Writing to the stream is loads faster than to the file. Not sure why. Unity thing probably, doesn't happen on .NET 6.
			using Stream stream = await FileManager.Instance.NewFileStreamAsync(path, FileMode.Create, FileAccess.Write, FileShare.None);
			if (stream is FileStream) {
				using MemoryStream mem = new();
				await vue.SaveAsync(mem);
				mem.Position = 0;
				await mem.CopyToAsync(stream);
			} else {
				await vue.SaveAsync(stream);
			}
		}

		public async void RebaseAsync() {
			if (
				!float.TryParse(this.startX.text, out float startX) ||
				!float.TryParse(this.startY.text, out float startY) ||
				!float.TryParse(this.startZ.text, out float startZ) ||
				!float.TryParse(this.rotPitch.text, out float rotPitch) ||
				!float.TryParse(this.rotYaw.text, out float rotYaw) ||
				!float.TryParse(this.rotRoll.text, out float rotRoll) ||
				!float.TryParse(this.scalePosX.text, out float scalePosX) ||
				!float.TryParse(this.scalePosY.text, out float scalePosY) ||
				!float.TryParse(this.scalePosZ.text, out float scalePosZ) ||
				!float.TryParse(this.scaleObjX.text, out float scaleObjX) ||
				!float.TryParse(this.scaleObjY.text, out float scaleObjY) ||
				!float.TryParse(this.scaleObjZ.text, out float scaleObjZ)
			) {
				await DfMessageBox.Instance.ShowAsync("Invalid values.");
				return;
			}

			Vector3 start = new(startX, -startY, startZ);
			Quaternion deltaRot = Quaternion.Euler(rotPitch, rotYaw, rotRoll);
			Vector3 scalePos = new(scalePosX, scalePosY, scalePosZ);
			Vector3 scaleObj = new(scaleObjX, scaleObjY, scaleObjZ);

			Vector3 delta = Vector3.zero;

			/*GameObject a = new GameObject() {
				name = "Original"
			};
			GameObject b = new GameObject() {
				name = "Adjusted"
			};*/

			Matrix4x4[] frames = this.Value.Value.Frames.Select((x, i) => {
				Matrix4x4 matrix = x.ToUnity();
				Vector3 pos = matrix.GetUnityPositionFromAutodesk();
				Quaternion rot = matrix.GetUnityRotationFromAutodesk();
				Vector3 scale = matrix.lossyScale;

				pos = deltaRot * pos;
				pos = new Vector3(pos.x * scalePos.x, pos.y * scalePos.y, pos.z * scalePos.z);

				rot = deltaRot * rot;
				scale = new Vector3(scaleObj.x * scale.x, scaleObj.y * scale.y, scaleObj.z * scale.z);

				if (this.Value.Value.Frames[0] == x) {
					delta = start - pos;
				}
				pos += delta;

				// Adjust back to different coordinate system for Autodesk
				pos = new Vector3(pos.x, pos.z, pos.y);
				//rot = new Quaternion(-rot.x, -rot.y, -rot.z, rot.w);

				matrix = Matrix4x4.TRS(pos, rot, scale);
				return new Matrix4x4(
					new Vector4(matrix.m00, matrix.m20, matrix.m10, matrix.m30),
					new Vector4(matrix.m02, matrix.m22, matrix.m12, matrix.m31),
					new Vector4(matrix.m01, matrix.m21, matrix.m11, matrix.m32),
					new Vector4(matrix.m03, matrix.m13, matrix.m23, matrix.m33)
				);
			}).ToArray();

			if (delta == Vector3.zero && deltaRot == Quaternion.identity && scalePos == Vector3.one && scaleObj == Vector3.one) {
				return;
			}

			this.Value.Value.Frames.Clear();
			this.Value.Value.Frames.AddRange(frames.Select(x => x.ToNet()));

			this.GetComponentInParent<VueViewer>().OnDirty();

			this.GeneratePreview();

			this.rotRoll.text = this.rotYaw.text = this.rotPitch.text = "0";
			this.scaleObjZ.text = this.scaleObjY.text = this.scaleObjX.text = this.scalePosZ.text = this.scalePosY.text = this.scalePosX.text = "1";
		}
	}
}