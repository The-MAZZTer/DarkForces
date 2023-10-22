using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using MZZT.FileFormats;
using MZZT.IO.FileProviders;
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
	public class VocDetails : MonoBehaviour {
		[SerializeField]
		private GameObject boundaryContainer;
		[SerializeField]
		private Databind blockContainer;
		[SerializeField]
		private DataboundList<string> comments;
		[SerializeField]
		private DataboundList<ushort> markers;
		[SerializeField]
		private Button mergeButton;
		[SerializeField]
		private Color enabledText;
		[SerializeField]
		private Color disabledText;
		[SerializeField]
		private Toggle enableRepeat;
		[SerializeField]
		private TMP_Dropdown repeatStart;
		[SerializeField]
		private Toggle repeatInfinitely;
		[SerializeField]
		private TMP_InputField repeatCount;
		[SerializeField]
		private TextMeshProUGUI length;
		[SerializeField]
		private TMP_InputField silenceLength;

		private int boundaryValue = -1;
		public int BoundaryValue {
			get => this.boundaryValue;
			set {
				if (this.boundaryValue == value) {
					return;
				}

				this.userInput = false;
				VocViewer viewer = this.GetComponentInParent<VocViewer>(true);
				bool isDirty = viewer.IsDirty;
				try {
					this.boundaryValue = value;

					((IDatabind)this.blockContainer).Value = null;

					this.Invalidate();
				} finally {
					this.userInput = true;

					if (!isDirty) {
						viewer.ResetDirty();
					}
				}
			}
		}

		public CreativeVoice.AudioData BlockValue {
			get => (CreativeVoice.AudioData)((IDatabind)this.blockContainer).Value;
			set {
				if (this.BlockValue == value) {
					return;
				}

				this.userInput = false;
				VocViewer viewer = this.GetComponentInParent<VocViewer>(true);
				bool isDirty = viewer.IsDirty;
				try {
					((IDatabind)this.blockContainer).Value = null;

					CreativeVoice voc = this.GetComponentInParent<VocViewer>(true).Value;

					this.repeatStart.value = -1;

					this.repeatStart.ClearOptions();
					this.repeatStart.options.AddRange(voc.AudioBlocks.Select((x, i) => new TMP_Dropdown.OptionData($"Block {i}: {(x.Type == CreativeVoice.BlockTypes.Silence ? "Silence" : "Audio")}")));

					((IDatabind)this.blockContainer).Value = value;
					this.boundaryValue = -1;

					this.Invalidate();
				} finally {
					this.userInput = true;

					if (!isDirty) {
						viewer.ResetDirty();
					}
				}
			}
		}

		private void Invalidate() {
			if (this.BlockValue != null) {
				this.comments.Value = null;
				this.markers.Value = null;

				if (this.BlockValue.Type == CreativeVoice.BlockTypes.Silence) {
					this.userInput = false;
					try {
						this.silenceLength.text = ((float)this.BlockValue.SilenceLength / this.BlockValue.Frequency).ToString("0.0");
					} finally {
						this.userInput = true;
					}
				} else {
					this.length.text = ((float)this.BlockValue.Data.Length / this.BlockValue.Frequency).ToString("0.0");
				}
				this.enableRepeat.isOn = this.BlockValue.RepeatStart >= 0 && this.BlockValue.RepeatCount > 0;
				this.InvalidateRepeat();
			} else if (this.boundaryValue >= 0) {
				CreativeVoice voc = this.GetComponentInParent<VocViewer>(true).Value;
				this.comments.Value = voc.Comments.Where(x => x.BeforeAudioDataIndex == this.boundaryValue).Select(x => x.Value).ToList();
				this.markers.Value = voc.Markers.Where(x => x.BeforeAudioDataIndex == this.boundaryValue).Select(x => x.Value).ToList();

				CreativeVoice.AudioData block1 = voc.AudioBlocks.ElementAtOrDefault(this.boundaryValue - 1);
				CreativeVoice.AudioData block2 = voc.AudioBlocks.ElementAtOrDefault(this.boundaryValue);

				this.mergeButton.interactable = block1 != null && block2 != null && block1.Channels == block2.Channels && block1.Codec == block2.Codec &&
					block1.BitsPerSample == block2.BitsPerSample && Math.Abs(block1.Frequency - block2.Frequency) < block1.Frequency / 100f &&
					(block1.Type == CreativeVoice.BlockTypes.Silence) == (block2.Type == CreativeVoice.BlockTypes.Silence);
				foreach (TextMeshProUGUI text in this.mergeButton.GetComponentsInChildren<TextMeshProUGUI>(true)) {
					text.color = this.mergeButton.interactable ? this.enabledText : this.disabledText;
				}
			}

			this.boundaryContainer.SetActive(this.boundaryValue >= 0);
			this.blockContainer.gameObject.SetActive(this.BlockValue != null);
		}

		public void Merge() {
			int value = this.boundaryValue;
			VocViewer vocViewer = this.GetComponentInParent<VocViewer>(true);
			CreativeVoice voc = vocViewer.Value;

			CreativeVoice.AudioData block1 = voc.AudioBlocks.ElementAtOrDefault(value - 1);
			CreativeVoice.AudioData block2 = voc.AudioBlocks.ElementAtOrDefault(value);

			bool allowed = block1 != null && block2 != null && block1.Channels == block2.Channels && block1.Codec == block2.Codec &&
				block1.BitsPerSample == block2.BitsPerSample && Math.Abs(block1.Frequency - block2.Frequency) < block1.Frequency / 100f &&
				(block1.Type == CreativeVoice.BlockTypes.Silence) == (block2.Type == CreativeVoice.BlockTypes.Silence);
			if (!allowed) {
				return;
			}

			vocViewer.Stop();

			foreach (CreativeVoice.AudioData x in voc.AudioBlocks) {
				if (x.RepeatStart >= value) {
					x.RepeatStart--;
				}
			}
			foreach (CreativeVoice.Comment comment in voc.Comments) {
				if (comment.BeforeAudioDataIndex >= value) {
					comment.BeforeAudioDataIndex--;
				}
			}
			foreach (CreativeVoice.MarkerData marker in voc.Markers) {
				if (marker.BeforeAudioDataIndex >= value) {
					marker.BeforeAudioDataIndex--;
				}
			}

			block1.Data = block1.Data.Concat(block2.Data).ToArray();
			block1.RepeatCount = block2.RepeatCount;
			block1.RepeatStart = block2.RepeatStart;

			voc.AudioBlocks.RemoveAt(value);

			vocViewer.OnDirty();

			vocViewer.InvalidateBlocks();
			vocViewer.SelectBlock(block1);
		}

		private string lastFolder;
		public async void ImportAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				AllowNavigateGob = true,
				AllowNavigateLfd = true,
				Filters = new[] {
					FileBrowser.FileType.Generate("Supported Files", "*.VOC", "*.VOIC", "*.WAV"),
					FileBrowser.FileType.AllFiles
				},
				SelectButtonText = "Import",
				SelectedFileMustExist = true,
				SelectedPathMustExist = true,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Import VOC/WAV"
			});
			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			string ext = Path.GetExtension(path).ToLower();
			bool isVoc = ext == ".voc" || ext == ".voic";
			bool isWav = ext == ".wav";
			if (!isVoc && !isWav) {
				await DfMessageBox.Instance.ShowAsync("Unknown file type. Import was unsuccessful.");
				return;
			}

			IFile file = null;
			if (FileManager.Instance.FolderExists(this.lastFolder)) {
				if (isVoc) {
					file = await DfFileManager.Instance.ReadAsync<CreativeVoice>(path);
				} else  {
					file = await DfFileManager.Instance.ReadAsync<Wave>(path);
				}
			} else if (FileManager.Instance.FileExists(this.lastFolder)) {
				switch (Path.GetExtension(this.lastFolder).ToLower()) {
					case ".gob":
						using (Stream gobStream = await FileManager.Instance.NewFileStreamAsync(this.lastFolder, FileMode.Open, FileAccess.Read, FileShare.None)) {
							DfGobContainer gob = await DfGobContainer.ReadAsync(gobStream, false);
							if (isVoc) {
								file = await gob.GetFileAsync<CreativeVoice>(Path.GetFileName(path), gobStream);
							} else if (isWav) {
								file = await gob.GetFileAsync<Wave>(Path.GetFileName(path), gobStream);
							}
						}
						break;
					case ".lfd":
						await DfFileManager.Instance.ReadLandruFileDirectoryAsync(this.lastFolder, async x => {
							if (isVoc) {
								file = await x.GetFileAsync<CreativeVoice>(Path.GetFileNameWithoutExtension(path));
							} else if (isWav) {
								file = await x.GetFileAsync<Wave>(Path.GetFileNameWithoutExtension(path), "WAVE");
							}
						});
						break;
				}
			}

			if (file == null) {
				await DfMessageBox.Instance.ShowAsync("File does not exist. Import was unsuccessful.");
				return;
			}

			int index = this.boundaryValue;

			VocViewer vocViewer = this.GetComponentInParent<VocViewer>(true);
			CreativeVoice main = vocViewer.Value;
			CreativeVoice.AudioData newBlock = null;
			if (file is CreativeVoice voc) {
				if (voc.AudioBlocks.Any(x => (x.Type != CreativeVoice.BlockTypes.Silence && (x.BitsPerSample != 8 || x.Channels != 1)) || Math.Abs(11025 - x.Frequency) > 11025f / 100)) {
					await DfMessageBox.Instance.ShowAsync("This tool only supports importing 8-bit Mono 11025 Hz audio. Import was unsuccessful.");
					return;
				}

				vocViewer.Stop();

				int offset = voc.AudioBlocks.Count;
				foreach (CreativeVoice.AudioData block in main.AudioBlocks) {
					if (block.RepeatStart >= index) {
						block.RepeatStart += offset;
					}
				}
				foreach (CreativeVoice.Comment comment in main.Comments) {
					if (comment.BeforeAudioDataIndex > index) {
						comment.BeforeAudioDataIndex += offset;
					}
				}
				foreach (CreativeVoice.MarkerData marker in main.Markers) {
					if (marker.BeforeAudioDataIndex > index) {
						marker.BeforeAudioDataIndex += offset;
					}
				}

				foreach (CreativeVoice.AudioData block in voc.AudioBlocks) {
					if (block.RepeatStart >= 0) {
						block.RepeatStart += index;
					}
				}
				main.AudioBlocks.InsertRange(index, voc.AudioBlocks);
				newBlock = voc.AudioBlocks.FirstOrDefault();
				foreach (CreativeVoice.Comment comment in voc.Comments) {
					comment.BeforeAudioDataIndex += offset;
				}
				main.Comments.AddRange(voc.Comments);
				foreach (CreativeVoice.MarkerData marker in voc.Markers) {
					marker.BeforeAudioDataIndex += offset;
				}
				main.Markers.AddRange(voc.Markers);
			} else if (file is Wave wav) {
				if (wav.BitsPerSample != 8 || wav.Channels != 1 || Math.Abs(11025 - wav.SampleRate) > 11025f / 100) {
					await DfMessageBox.Instance.ShowAsync("This tool only supports importing 8-bit Mono 11025 Hz audio. Import was unsuccessful.");
					return;
				}

				vocViewer.Stop();

				foreach (CreativeVoice.AudioData block in main.AudioBlocks) {
					if (block.RepeatStart >= index) {
						block.RepeatStart++;
					}
				}
				foreach (CreativeVoice.Comment comment in main.Comments) {
					if (comment.BeforeAudioDataIndex > index) {
						comment.BeforeAudioDataIndex++;
					}
				}
				foreach (CreativeVoice.MarkerData marker in main.Markers) {
					if (marker.BeforeAudioDataIndex > index) {
						marker.BeforeAudioDataIndex++;
					}
				}
				main.AudioBlocks.Insert(index, newBlock = new CreativeVoice.AudioData() {
					BitsPerSample = (byte)wav.BitsPerSample,
					Channels = (byte)wav.Channels,
					Codec = CreativeVoice.Codecs.Unsigned8BitPcm,
					Data = wav.Data,
					Frequency = wav.SampleRate,
					Type = CreativeVoice.BlockTypes.LegacyAudioData
				});
			}

			vocViewer.OnDirty();

			vocViewer.InvalidateBlocks();
			if (newBlock != null) {
				vocViewer.SelectBlock(newBlock);
			}
			vocViewer.RefreshThumbnail();
			this.Invalidate();
		}

		public void InsertSilence() {
			int index = this.boundaryValue;

			VocViewer vocViewer = this.GetComponentInParent<VocViewer>(true);
			vocViewer.Stop();

			CreativeVoice main = vocViewer.Value;
			foreach (CreativeVoice.AudioData block in main.AudioBlocks) {
				if (block.RepeatStart >= index) {
					block.RepeatStart++;
				}
			}
			foreach (CreativeVoice.Comment comment in main.Comments) {
				if (comment.BeforeAudioDataIndex > index) {
					comment.BeforeAudioDataIndex++;
				}
			}
			foreach (CreativeVoice.MarkerData marker in main.Markers) {
				if (marker.BeforeAudioDataIndex > index) {
					marker.BeforeAudioDataIndex++;
				}
			}
			CreativeVoice.AudioData newBlock = new() {
				Frequency = 11025,
				Type = CreativeVoice.BlockTypes.Silence,
				SilenceLength = 11025
			};
			main.AudioBlocks.Insert(index, newBlock);

			vocViewer.OnDirty();

			vocViewer.InvalidateBlocks();
			vocViewer.SelectBlock(newBlock);
			vocViewer.RefreshThumbnail();
		}

		private bool userInput = true;
		public void OnSilenceLengthChanged(string value) {
			if (!this.userInput || !float.TryParse(value, out float val)) {
				return;
			}

			this.BlockValue.SilenceLength = Mathf.RoundToInt(val * this.BlockValue.Frequency);

			VocViewer vocViewer = this.GetComponentInParent<VocViewer>(true);
			vocViewer.OnDirty();

			vocViewer.Stop();
			vocViewer.InvalidateBlockBar();
			vocViewer.RefreshThumbnail();
		}

		public void InvalidateRepeat() {
			this.repeatStart.interactable = this.enableRepeat.isOn;
			this.repeatCount.interactable = this.enableRepeat.isOn && !this.repeatInfinitely.isOn;

			this.GetComponentInParent<VocViewer>(true).OnDirty();
		}

		private string lastFolder2;
		public async void ExportAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new FileBrowser.FileBrowserOptions() {
				Filters = new[] {
					FileBrowser.FileType.Generate("WAVE File", "*.WAV"),
					FileBrowser.FileType.AllFiles
				},
				SelectButtonText = "Export",
				SelectedFileMustExist = false,
				SelectedPathMustExist = true,
				StartPath = this.lastFolder2 ?? FileLoader.Instance.DarkForcesFolder,
				Title = "Export to WAV",
				ValidateFileName = true
			});
			if (path == null) {
				return;
			}

			this.lastFolder2 = Path.GetDirectoryName(path);

			// Writing to the stream is loads faster than to the file. Not sure why. Unity thing probably, doesn't happen on .NET 6.
			using Stream stream = await FileManager.Instance.NewFileStreamAsync(path, FileMode.Create, FileAccess.Write, FileShare.None);
			if (stream is FileStream) {
				using MemoryStream mem = new();
				await this.BlockValue.ToWave().SaveAsync(mem);
				mem.Position = 0;
				await mem.CopyToAsync(stream);
			} else {
				await this.BlockValue.ToWave().SaveAsync(stream);
			}
		}

		public void Delete() {
			VocViewer vocViewer = this.GetComponentInParent<VocViewer>(true);
			CreativeVoice voc = vocViewer.Value;
			int index = voc.AudioBlocks.IndexOf(this.BlockValue);

			vocViewer.Stop();

			foreach (CreativeVoice.AudioData block in voc.AudioBlocks) {
				if (block.RepeatStart > index) {
					block.RepeatStart--;
				}
			}
			foreach (CreativeVoice.Comment comment in voc.Comments) {
				if (comment.BeforeAudioDataIndex > index) {
					comment.BeforeAudioDataIndex--;
				}
			}
			foreach (CreativeVoice.MarkerData marker in voc.Markers) {
				if (marker.BeforeAudioDataIndex > index) {
					marker.BeforeAudioDataIndex--;
				}
			}

			vocViewer.SelectBoundary(index);

			voc.AudioBlocks.RemoveAt(index);

			vocViewer.InvalidateBlocks();
			vocViewer.RefreshThumbnail();
		}
	}
}
