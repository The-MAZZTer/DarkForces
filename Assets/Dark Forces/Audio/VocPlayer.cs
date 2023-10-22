using MZZT.DarkForces.FileFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace MZZT.DarkForces {
	/// <summary>
	/// Player to play VOC files.
	/// With this player the VOC can only be played once at a time.
	/// Might need to revamp this to make it easier to overlap instances of the same VOC.
	/// </summary>
	public class VocPlayer : MonoBehaviour {
		private CreativeVoice voc;
		private AudioClip[] clips;
		private Dictionary<int, int> repeatsLeft;
		/// <summary>
		/// The VOC file to play.
		/// </summary>
		public CreativeVoice Voc {
			get => this.voc;
			set {
				if (this.voc == value) {
					return;
				}

				this.voc = value;

				this.Invalidate();
			}
		}

		public void Invalidate() {
			this.Stop();

			if (this.voc == null) {
				this.clips = null;
				this.repeatsLeft = null;
			} else {
				// Process the VOC into AudioClips.
				this.clips = this.voc.AudioBlocks.Select((x, i) => {
					// Don't need a clip for silence.
					if (x.Type == CreativeVoice.BlockTypes.Silence) {
						return null;
					}

					// DF only uses 8.
					if (x.BitsPerSample != 8) {
						throw new FormatException("Formats other than 8-bits per sample not supported.");
					}

					// Create an AudioCLip with the desired properties.
					AudioClip clip = AudioClip.Create(i.ToString(), x.Data.Length, x.Channels, (int)x.Frequency, false);
					// AudioClips take float samples while VOCs use bytes so convert them.
					clip.SetData(x.Data.Select(x => {
						// 0x80 seems to be 0/silence.
						if (x == 0x80) {
							return 0f;
						} else if (x < 0x80) {
							return (x - 0x80) / (float)0x80;
						} else {
							return (x - 0x80) / (float)0x7F;
						}
					}).ToArray(), 0);
					return clip;
				}).ToArray();
			}
		}

		private AudioSource[] sources;

		[SerializeField]
		private bool localizedSound = true;

		private void Awake() {
			// To smoothly transition from one audio block into the next, you need to have another AudioSource ready to go.
			// Two AudioSources work fine as long as you enqueue the second long enough in advance.
			this.sources = Enumerable.Range(0, 2).Select(x => {
				GameObject child = new() {
					name = "AudioSource"
				};
				child.transform.SetParent(this.transform, false);
				AudioSource source = child.AddComponent<AudioSource>();
				source.playOnAwake = false;
				source.spatialBlend = this.localizedSound ? 1 : 0;
				return source;
			}).ToArray();
		}

		/// <summary>
		/// Play the VOC.
		/// </summary>
		public void Play() {
			this.Stop();

			this.currentClipStartPos = 0;
			this.currentClipStartTime = AudioSettings.dspTime;
			this.nextClipTime = AudioSettings.dspTime;
			this.nextClip = 0;

			// Track how many times we've looped.
			this.repeatsLeft = this.Voc.AudioBlocks.Select((x, i) => {
				if (x.RepeatInfinitely) {
					return (i, -1);
				}
				if (x.RepeatCount > 0) {
					return (i, x.RepeatCount);
				}
				return (i, 0);
			}).Where(x => x.Item2 != 0).ToDictionary(x => x.i, x => x.Item2);
		}

		[SerializeField]
		private bool allowRepeat = true;
		public bool AllowRepeat {
			get => this.allowRepeat;
			set {
				if (this.allowRepeat == value) {
					return;
				}

				this.allowRepeat = value;
			}
		}

		public void Pause() {
			if (this.pauseTime > 0) {
				return;
			}
			foreach (AudioSource source in this.sources) {
				source.Pause();
			}
			this.pauseTime = AudioSettings.dspTime;
		}

		public void Unpause() {
			if (this.pauseTime == 0) {
				return;
			}
			double delta = AudioSettings.dspTime - this.pauseTime;
			this.nextClipTime += delta;
			this.nextQueuedClipTime += delta;
			this.currentClipStartTime += delta;
			this.pauseTime = 0;
			foreach (AudioSource source in this.sources) {
				source.UnPause();
			}
		}

		/// <summary>
		/// Stop the VOC.
		/// </summary>
		public void Stop() {
			foreach (AudioSource source in this.sources) {
				source.Stop();
			}
			this.currentClip = -1;
			this.nextClip = -1;
			this.nextQueuedClip = -1;
			this.nextClipTime = -1;
			this.nextQueuedClipTime = double.PositiveInfinity;
			this.currentClipStartTime = -1;
			this.currentClipStartPos = -1;
			this.pauseTime = 0;
			this.repeatsLeft = null;
		}
		private double pauseTime;

		public void Seek(double time) {
			int index = -1;
			double totalLength = 0;
			for (int i = 0; i < this.clips.Length; i++) {
				double length = this.GetBlockLength(i);
				if (time - length < 0) {
					index = i;
					break;
				}
				totalLength += length;
				time -= length;
			}

			if (index < 0) {
				return;
			}

			this.Stop();

			this.currentClipStartTime = AudioSettings.dspTime;
			this.currentClipStartPos = totalLength;
			this.nextClipTime = AudioSettings.dspTime;
			this.nextClipOffset = time;
			this.nextClip = index;

			if (index != this.currentClipStartPos || this.repeatsLeft == null) {
				// Track how many times we've looped.
				this.repeatsLeft = this.Voc.AudioBlocks.Select((x, i) => {
					if (x.RepeatInfinitely) {
						return (i, -1);
					}
					if (x.RepeatCount > 0) {
						return (i, x.RepeatCount);
					}
					return (i, 0);
				}).Where(x => x.Item2 != 0).ToDictionary(x => x.i, x => x.Item2);
			}
		}

		/// <summary>
		/// Is the VOC playing?
		/// </summary>
		public bool IsPlaying => this.currentClipStartTime >= 0;

		private int currentClip = -1;
		private int nextQueuedClip = -1;
		private double currentClipStartPos = -1;
		private double currentClipStartTime = -1;
		private double nextQueuedClipTime = double.PositiveInfinity;
		private double nextClipTime = -1;
		private int nextClip = -1;
		private int nextSourceIndex = 0;
		private double nextClipOffset = 0;

		public int CurrentBlock => this.currentClip;

		public double CurrentBlockTime {
			get {
				if (this.currentClipStartTime < 0) {
					return 0;
				}

				if (this.pauseTime > 0) {
					return this.pauseTime - this.currentClipStartTime;
				}
				return AudioSettings.dspTime - this.currentClipStartTime;
			}
		}

		public double CurrentTime {
			get {
				if (this.currentClipStartTime < 0) {
					return 0;
				}

				return this.currentClipStartPos + this.CurrentBlockTime;
			}
		}

		private double GetBlockLength(int index) {
			AudioClip clip = this.clips.ElementAtOrDefault(index);
			if (clip != null) {
				return clip.length;
			}

			CreativeVoice.AudioData data = this.voc.AudioBlocks.ElementAtOrDefault(index);
			if (data != null) {
				return (double)data.SilenceLength / data.Frequency;
			}

			return 0;
		}

		public double CurrentBlockLength => this.GetBlockLength(this.currentClip < 0 ? 0 : this.currentClip);

		public double TotalLength => Enumerable.Range(0, this.clips.Length).Select(x => this.GetBlockLength(x)).Sum();

		private void Update() {
			if (this.currentClipStartPos < 0 || this.pauseTime > 0) {
				return;
			}

			// While we have more audio blocks to enqueue.
			// Enqueue the next clip when the current one is 2/3 of the way through.
			// This seems to keep the audio smooth.
			if (this.nextQueuedClipTime == double.PositiveInfinity && (this.currentClip < 0 || AudioSettings.dspTime >= this.nextClipTime -
				(this.clips[this.currentClip]?.length ?? (this.voc.AudioBlocks[this.currentClip].SilenceLength / this.voc.AudioBlocks[this.currentClip].Frequency)) / 3)) {

				this.nextQueuedClipTime = this.nextClipTime;
				this.nextQueuedClip = this.nextClip;

				if (this.nextClip >= 0) {
					AudioClip clip = this.clips[this.nextClip];
					CreativeVoice.AudioData data = this.voc.AudioBlocks[this.nextClip];

					if (clip == null) {
						this.nextClipTime += (double)data.SilenceLength / data.Frequency - this.nextClipOffset;
					} else {
						AudioSource source = this.sources[this.nextSourceIndex];
						this.nextSourceIndex = (this.nextSourceIndex + 1) % this.sources.Length;
						source.clip = clip;
						if (AudioSettings.dspTime > this.nextClipTime) {
							source.time = (float)(this.nextClipOffset + AudioSettings.dspTime - this.nextClipTime);
							source.Play();
						} else {
							source.time = (float)this.nextClipOffset;
							source.PlayScheduled(this.nextClipTime);
						}
						this.nextClipTime += (double)data.Data.Length / data.Frequency - this.nextClipOffset;
					}

					int repeatsLeft = 0;
					if (this.allowRepeat) {
						this.repeatsLeft.TryGetValue(this.nextClip, out repeatsLeft);
					}
					if (repeatsLeft != 0) {
						if (repeatsLeft > 0) {
							this.repeatsLeft[this.nextClip] = repeatsLeft - 1;
						}
						this.nextClip = data.RepeatStart;
					} else {
						this.nextClip++;
						if (this.nextClip >= this.clips.Length) {
							this.nextClip = -1;
						}
					}
				}
			}

			if (AudioSettings.dspTime >= this.nextQueuedClipTime) {
				this.currentClip = this.nextQueuedClip;
				if (this.currentClip < 0) {
					this.Stop();
					this.endReached.Invoke();
					return;
				}

				this.currentClipStartPos = Enumerable.Range(0, this.currentClip).Select(x => this.GetBlockLength(x)).Sum();
				this.currentClipStartTime = this.nextQueuedClipTime - this.nextClipOffset;
				this.nextQueuedClipTime = double.PositiveInfinity;
				this.nextQueuedClip = -1;
				this.nextClipOffset = 0;
			}
		}

		[SerializeField]
		private UnityEvent endReached = new();
	}
}
