using MZZT.DarkForces.FileFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

				this.Stop();

				this.voc = value;

				if (value == null) {
					this.clips = null;
					this.repeatsLeft = null;
				} else {
					// Process the VOC into AudioClips.
					this.clips = this.Voc.AudioBlocks.Select((x, i) => {
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

					// Track how many times we've looped.
					this.repeatsLeft = this.Voc.AudioBlocks.Select((x, i) => {
						if (x.RepeatInfinitely) {
							return (i, -1);
						}
						if (x.RepeatCount > 1) {
							return (i, x.RepeatCount);
						}
						return (i, 0);
					}).Where(x => x.Item2 != 0).ToDictionary(x => x.i, x => x.Item2);
				}
			}
		}

		private AudioSource[] sources;

		private void Awake() {
			// To smoothly transition from one audio block into the next, you need to have another AudioSource ready to go.
			// Two AudioSources work fine as long as you enqueue the second long enough in advance.
			this.sources = Enumerable.Range(0, 2).Select(x => {
				GameObject child = new GameObject() {
					name = "AudioSource"
				};
				child.transform.SetParent(this.transform, false);
				AudioSource source = child.AddComponent<AudioSource>();
				source.playOnAwake = false;
				return source;
			}).ToArray();
		}

		private double nextClipTime = -1;
		private int nextClip = -1;
		private int nextSourceIndex = 0;
		/// <summary>
		/// Play the VOC.
		/// </summary>
		public void Play() {
			this.Stop();

			this.nextClipTime = AudioSettings.dspTime;
			this.nextClip = 0;
		}

		/// <summary>
		/// Stop the VOC.
		/// </summary>
		public void Stop() {
			foreach (AudioSource source in this.sources) {
				source.Stop();
			}
			this.nextClip = -1;
			this.nextClipTime = -1;
		}

		/// <summary>
		/// Is the VOC playing?
		/// </summary>
		public bool IsPlaying => this.nextClipTime >= 0;

		private void FixedUpdate() {
			// While we have more audio blocks to enqueue.
			// Enqueue the next clip when the current one is 2/3 of the way through.
			// This seems to keep the audio smooth.
			while (this.nextClip >= 0 && AudioSettings.dspTime >= this.nextClipTime - this.clips[this.nextClip].length / 3) {
				AudioClip clip = this.clips[this.nextClip];
				CreativeVoice.AudioData data = this.voc.AudioBlocks[this.nextClip];

				if (clip == null) {
					this.nextClipTime += (double)data.SilenceLength / data.Frequency;
				} else {
					AudioSource source = this.sources[this.nextSourceIndex];
					this.nextSourceIndex = (this.nextSourceIndex + 1) % this.sources.Length;
					source.clip = clip;
					source.PlayScheduled(this.nextClipTime);
					this.nextClipTime += (double)data.Data.Length / data.Frequency;
				}

				this.repeatsLeft.TryGetValue(this.nextClip, out int playsLeft);
				if (playsLeft > 1 || playsLeft < 0) {
					if (playsLeft > 1) {
						this.repeatsLeft[this.nextClip] = playsLeft - 1;
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
	}
}
