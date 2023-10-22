using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using MZZT.FileFormats;
using MZZT.IO.FileProviders;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
	public class VocViewer : Databind<CreativeVoice>, IResourceViewer {
		[Header("VOC"), SerializeField]
		private TMP_Dropdown blockDropdown;
		[SerializeField]
		private ToggleGroup blockBar;
		[SerializeField]
		private VocBlockBarItem blockBarItemTemplate;
		[SerializeField]
		private VocDetails details;
		[SerializeField]
		private VocPlayer player;
		[SerializeField]
		private Slider slider;
		[SerializeField]
		private TextMeshProUGUI playIcon;
		[SerializeField]
		private TextMeshProUGUI pauseIcon;
		[SerializeField]
		private TextMeshProUGUI noRepeatIcon;
		[SerializeField]
		private TextMeshProUGUI status;
		[SerializeField]
		private string statusFormat;
		[SerializeField]
		private Image waveform;

		public string TabName => this.filePath == null ? "New VOC" : Path.GetFileName(this.filePath);
		public event EventHandler TabNameChanged;

		public Sprite Thumbnail { get; private set; }
		public event EventHandler ThumbnailChanged;

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

		private string filePath;
		public Task LoadAsync(ResourceEditorResource resource, IFile file) {
			this.filePath = resource?.Path;

			this.Value = (CreativeVoice)file;

			this.RefreshThumbnail();

			this.InvalidateBlockBar();
			this.PopulateBlockDropdown();

			this.Stop();
			this.player.Voc = this.Value;
			return Task.CompletedTask;
		}

		public void InvalidateBlockBar() {
			this.player.Invalidate();

			this.userInput = false;
			try {
				foreach (GameObject child in this.blockBar.transform.Cast<Transform>().Select(x => x.gameObject).ToArray()) {
					DestroyImmediate(child);
				}

				for (int i = 0; i <= this.Value.AudioBlocks.Count; i++) {
					GameObject gameObject = Instantiate(this.blockBarItemTemplate.gameObject);
					gameObject.transform.SetParent(this.blockBar.transform, false);
					VocBlockBarItem item = gameObject.GetComponent<VocBlockBarItem>();
					item.BoundaryToggle.onValueChanged.AddListener(this.OnBlockBarToggleChanged);
					item.BlockToggle.onValueChanged.AddListener(this.OnBlockBarToggleChanged);
					item.BoundaryToggle.group = this.blockBar;
					item.BlockToggle.group = this.blockBar;
					item.Index = i;
				}
			} finally {
				this.userInput = true;
			}

			this.AdjustBlockBarSelectedItem();
		}

		private void AdjustBlockBarSelectedItem() {
			CreativeVoice.AudioData block = this.details.BlockValue;
			int boundary = this.details.BoundaryValue;

			this.userInput = false;
			try {
				if (block != null) {
					int index = this.Value.AudioBlocks.IndexOf(block);
					VocBlockBarItem item = this.blockBar.transform.GetChild(index).GetComponent<VocBlockBarItem>();
					item.BlockToggle.isOn = true;
				} else if (boundary >= 0) {
					VocBlockBarItem item = this.blockBar.transform.GetChild(boundary).GetComponent<VocBlockBarItem>();
					item.BoundaryToggle.isOn = true;
				}
			} finally {
				this.userInput = true;
			}
		}

		public void OnBlockBarToggleChanged(bool value) {
			if (!value || !this.userInput) {
				return;
			}

			Toggle toggle = this.blockBar.GetComponentsInChildren<Toggle>(true).First(x => x.group == this.blockBar && x.isOn);
			VocBlockBarItem item = toggle.GetComponentInParent<VocBlockBarItem>(true);
			int index = item.Index;

			if (toggle == item.BlockToggle) {
				this.details.BlockValue = this.Value.AudioBlocks[index];
			} else {
				this.details.BoundaryValue = index;
			}

			this.AdjustBlockDropdownSelectedIndex();
		}

		private void PopulateBlockDropdown() {
			this.blockDropdown.ClearOptions();
			this.blockDropdown.options.AddRange(this.Value.AudioBlocks.SelectMany((x, i) => {
				return new[] {
					new TMP_Dropdown.OptionData($"Block Boundary {i}"),
					new TMP_Dropdown.OptionData($"Block {i}: {(x.Type == CreativeVoice.BlockTypes.Silence ? "Silence" : "Audio")}")
				};
			}).Append(new TMP_Dropdown.OptionData($"Block Boundary {this.Value.AudioBlocks.Count}")));
			this.AdjustBlockDropdownSelectedIndex();
		}

		private void AdjustBlockDropdownSelectedIndex() {
			int index = this.details.BoundaryValue;
			CreativeVoice.AudioData block = this.details.BlockValue;

			this.userInput = false;
			try {
				this.blockDropdown.value = -1;
				if (index >= 0) {
					this.blockDropdown.value = index * 2;
				} else if (block != null) {
					this.blockDropdown.value = this.Value.AudioBlocks.IndexOf(block) * 2 + 1;
				}
			} finally {
				this.userInput = true;
			}
		}

		private bool userInput = true;
		public void OnBlockDropdownSelectedItemChanged() {
			if (!this.userInput) {
				return;
			}

			VocBlockBarItem item = this.blockBar.transform.GetChild(this.blockDropdown.value / 2).GetComponent<VocBlockBarItem>();
			if (this.blockDropdown.value % 2 == 0) {
				item.BoundaryToggle.isOn = true;
			} else {
				item.BlockToggle.isOn = true;
			}
		}

		public Sprite GenerateWaveform(int width, int height, int block = -1) {
			byte[] buffer = new byte[width * height * 4];

			int startBlock = block == -1 ? 0 : block;
			int endBlock = block == -1 ? this.Value.AudioBlocks.Count : block;

			int[] samples = this.Value.AudioBlocks.Skip(startBlock).Take(endBlock - startBlock + 1).Select(x => x.Type switch {
				CreativeVoice.BlockTypes.Silence => x.SilenceLength,
				_ => x.Data.Length
			}).ToArray();
			int totalSamples = samples.Sum();

			if (samples.Length > 0) {
				for (int i = 0; i < width; i++) {
					int startSample = Mathf.RoundToInt(((float)totalSamples / width) * i);
					int endSample = Mathf.RoundToInt(((float)totalSamples / width) * i + 1);
					int sampleCount = endSample - startSample;
					if (sampleCount == 0) {
						sampleCount = 1;
					}
					int currIndex = 0;
					for (int j = 0, total = 0; j < samples.Length; total += samples[j], j++) {
						if (total + samples[j] > startSample) {
							currIndex = j;
							startSample -= total;
							break;
						}
					}

					sbyte max = 0;
					sbyte min = 0;

					do {
						CreativeVoice.AudioData curr = this.Value.AudioBlocks[startBlock + currIndex];
						int blockLen = curr.Type == CreativeVoice.BlockTypes.Silence ? curr.SilenceLength : curr.Data.Length;
						int len = Math.Min(sampleCount, blockLen - startSample);
						if (curr.Type != CreativeVoice.BlockTypes.Silence) {
							for (int j = startSample; j < startSample + len; j++) {
								sbyte data = (sbyte)(curr.Data[j] - 0x80);
								max = Math.Max(max, data);
								min = Math.Min(min, data);
							}
						}
						sampleCount -= len;
						if (sampleCount > 0) {
							currIndex++;
						}
					} while (sampleCount > 0 && currIndex <= endBlock);

					float normalizedMax = 0;
					float normalizedMin = 0;
					if (max < 0) {
						normalizedMax = max / (float)0x80;
					} else if (max > 0) {
						normalizedMax = max / (float)0x7f;
					}
					if (min < 0) {
						normalizedMin = min / (float)0x80;
					} else if (min > 0) {
						normalizedMin = min / (float)0x7f;
					}

					int top = Mathf.RoundToInt((height / 2f) - (normalizedMax * (height / 2f)));
					int bottom = Mathf.RoundToInt((height / 2f) - (normalizedMin * (height / 2f)));
					for (int j = top; j <= bottom; j++) {
						Array.Fill<byte>(buffer, 255, (j * width * 4) + (i * 4), 4);
					}
				}
			}

			Texture2D texture = new(width, height, TextureFormat.RGBA32, false, true) {
#if UNITY_EDITOR
				alphaIsTransparency = true,
#endif
				filterMode = FilterMode.Point
			};
			texture.LoadRawTextureData(buffer);
			texture.Apply(true, true);
			return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
		}

		public void RefreshThumbnail() {
			this.Thumbnail = this.waveform.sprite = this.GenerateWaveform(256, 256);
			this.ThumbnailChanged?.Invoke(this, new EventArgs());
		}

		public async void SaveAsync() {
			bool canSave = FileManager.Instance.FolderExists(Path.GetDirectoryName(this.filePath));
			if (!canSave) {
				this.SaveAsAsync();
				return;
			}

			// Writing to the stream is loads faster than to the file. Not sure why. Unity thing probably, doesn't happen on .NET 6.
			using Stream stream = await FileManager.Instance.NewFileStreamAsync(this.filePath, FileMode.Create, FileAccess.Write, FileShare.None);
			if (stream is FileStream) {
				using MemoryStream mem = new();
				await this.Value.SaveAsync(mem);
				mem.Position = 0;
				await mem.CopyToAsync(stream);
			} else {
				await this.Value.SaveAsync(stream);
			}

			this.ResetDirty();
		}

		public async void SaveAsAsync() {
			string path = await ResourceEditors.Instance.PickSaveLocationAsync(this.filePath, new[] { "*.VOC" });
			if (string.IsNullOrEmpty(path)) {
				return;
			}
			this.filePath = path;
			this.TabNameChanged?.Invoke(this, new EventArgs());

			bool canSave = FileManager.Instance.FolderExists(Path.GetDirectoryName(this.filePath));
			if (!canSave) {
				return;
			}

			this.SaveAsync();
		}

		private bool isPaused = false;
		public void PlayPause() {
			if (this.isPaused) {
				this.player.Unpause();
				this.isPaused = false;
			} else if (this.player.IsPlaying) {
				this.player.Pause();
				this.isPaused = true;
			} else {
				this.player.Seek(this.slider.value * this.player.TotalLength);
				this.isPaused = false;
			}
		}

		public void Stop() {
			this.player.Stop();
			this.isPaused = false;

			this.slider.value = 0;
		}

		private bool init2;
		private void Update() {
			if (!this.init2) {
				if (this.Value.AudioBlocks.Count > 0) {
					this.SelectBlock(this.Value.AudioBlocks[0]);
				} else {
					this.SelectBoundary(0);
				}
				this.init2 = true;
			}

			double len = this.player.TotalLength;
			double pos = this.player.IsPlaying ? this.player.CurrentTime : (this.slider.value * len);

			this.status.text = string.Format(this.statusFormat, TimeSpan.FromSeconds(pos), TimeSpan.FromSeconds(len), TimeSpan.FromSeconds(this.player.CurrentBlockTime), TimeSpan.FromSeconds(this.player.CurrentBlockLength));
			this.playIcon.gameObject.SetActive(!this.player.IsPlaying || this.isPaused);
			this.pauseIcon.gameObject.SetActive(this.player.IsPlaying && !this.isPaused);

			if (this.player.IsPlaying && !this.sliderMouseDown) {
				this.userInput = false;
				try {
					this.slider.value = (float)(pos / len);
				} finally {
					this.userInput = true;
				}
			}

			if (!PlayerInput.all[0].currentActionMap.FindAction("Click").IsPressed()) {
				this.sliderMouseDown = false;
			}
		}

		public void OnPlayerEndReached() {
			this.slider.value = 0;
		}

		private bool sliderMouseDown = false;
		public void OnSliderMouseDown() {
			this.sliderMouseDown = true;
		}

		public void OnSliderValueChanged(float value) {
			if (!this.userInput) {
				return;
			}

			if (this.player.IsPlaying && !this.isPaused) {
				this.player.Seek(this.slider.value * this.player.TotalLength);
			}
		}

		public void OnRepeatChanged(bool value) {
			this.noRepeatIcon.gameObject.SetActive(!value);

			this.player.AllowRepeat = value;
		}

		public void SplitCurrentBlock() {
			double pos = this.slider.value * this.player.TotalLength;
			double time = 0;
			int index = -1;
			CreativeVoice.AudioData block;
			int blockLength;
			do {
				index++;
				block = this.Value.AudioBlocks[index];
				blockLength = block.Type == CreativeVoice.BlockTypes.Silence ? block.SilenceLength : block.Data.Length;
				time += blockLength;
			} while (time <= pos);
			pos -= time - blockLength;

			int samplePos = Mathf.RoundToInt((float)(pos * block.Frequency));
			if (samplePos <= 0 || samplePos >= blockLength) {
				return;
			}

			this.Stop();

			CreativeVoice.AudioData newBlock = block.Clone();
			if (block.Type == CreativeVoice.BlockTypes.Silence) {
				block.SilenceLength = samplePos;
				newBlock.SilenceLength = blockLength - samplePos;
			} else {
				newBlock.Data = block.Data.Skip(samplePos).ToArray();
				block.Data = block.Data.Take(samplePos).ToArray();
			}

			foreach (CreativeVoice.AudioData x in this.Value.AudioBlocks) {
				if (x.RepeatStart > index) {
					x.RepeatStart++;
				}
			}
			foreach (CreativeVoice.Comment comment in this.Value.Comments) {
				if (comment.BeforeAudioDataIndex > index) {
					comment.BeforeAudioDataIndex++;
				}
			}
			foreach (CreativeVoice.MarkerData marker in this.Value.Markers) {
				if (marker.BeforeAudioDataIndex > index) {
					marker.BeforeAudioDataIndex++;
				}
			}

			newBlock.RepeatCount = block.RepeatCount;
			newBlock.RepeatStart = block.RepeatStart;
			block.RepeatCount = 0;
			block.RepeatStart = -1;

			this.Value.AudioBlocks.Insert(index + 1, newBlock);

			this.OnDirty();

			this.InvalidateBlocks();

			this.SelectBoundary(index + 1);
		}

		public void InvalidateBlocks() {
			this.InvalidateBlockBar();
			this.PopulateBlockDropdown();
		}

		public void SelectBlock(CreativeVoice.AudioData block) {
			int index = this.Value.AudioBlocks.IndexOf(block);
			this.blockBar.transform.GetChild(index).GetComponent<VocBlockBarItem>().BlockToggle.isOn = true;
		}

		public void SelectBoundary(int index) {
			this.blockBar.transform.GetChild(index).GetComponent<VocBlockBarItem>().BoundaryToggle.isOn = true;
		}
	}
}
