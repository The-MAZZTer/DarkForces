using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.Data.Binding.UI {
	public class DataboundColor : DataboundUi<Color> {
		[SerializeField]
		private TMP_InputField redInput;
		[SerializeField]
		private Slider redSlider;
		[SerializeField]
		private TMP_InputField greenInput;
		[SerializeField]
		private Slider greenSlider;
		[SerializeField]
		private TMP_InputField blueInput;
		[SerializeField]
		private Slider blueSlider;
		[SerializeField]
		private TMP_InputField alphaInput;
		[SerializeField]
		private Slider alphaSlider;
		[SerializeField]
		private Image preview;

		protected override void Start() {
			base.Start();

			if (this.redInput != null) {
				this.redInput.onValueChanged.AddListener(value => this.Input_ValueChanged(value, this.redSlider, "r"));
			}
			if (this.redSlider != null) {
				this.redSlider.onValueChanged.AddListener(value => this.Slider_ValueChanged(value, this.redInput, "r"));
			}
			if (this.greenInput != null) {
				this.greenInput.onValueChanged.AddListener(value => this.Input_ValueChanged(value, this.greenSlider, "g"));
			}
			if (this.greenSlider != null) {
				this.greenSlider.onValueChanged.AddListener(value => this.Slider_ValueChanged(value, this.greenInput, "g"));
			}
			if (this.blueInput != null) {
				this.blueInput.onValueChanged.AddListener(value => this.Input_ValueChanged(value, this.blueSlider, "b"));
			}
			if (this.blueSlider != null) {
				this.blueSlider.onValueChanged.AddListener(value => this.Slider_ValueChanged(value, this.blueInput, "b"));
			}
			if (this.alphaInput != null) {
				this.alphaInput.onValueChanged.AddListener(value => this.Input_ValueChanged(value, this.alphaSlider, "a"));
			}
			if (this.alphaSlider != null) {
				this.alphaSlider.onValueChanged.AddListener(value => this.Slider_ValueChanged(value, this.alphaInput, "a"));
			}
		}

		private void Input_ValueChanged(string value, Slider slider, string fieldName) {
			if (this.noUserInput > 0 || !float.TryParse(value, out float floatValue)) {
				return;
			}

			floatValue = Mathf.Clamp01(floatValue);

			this.noUserInput++;
			try {
				if (slider != null) {
					slider.value = floatValue;
				}
				this.OnUserEnteredValueChanged();
			} finally {
				this.noUserInput--;
			}
		}

		private void Slider_ValueChanged(float value, TMP_InputField input, string fieldName) {
			if (this.noUserInput > 0) {
				return;
			}

			value = Mathf.Clamp01(value);

			this.noUserInput++;
			try {
				if (input != null) {
					input.text = value.ToString("#.###");
				}
				this.OnUserEnteredValueChanged();
			} finally {
				this.noUserInput--;
			}
		}

		private int noUserInput = 0;

		protected override Color UserEnteredValue {
			get => new(this.redSlider.value, this.greenSlider.value, this.blueSlider.value, this.alphaSlider.value);
			set {
				if (this.preview != null) {
					this.preview.color = this.Value;
				}

				this.noUserInput++;
				try {
					if (this.redInput != null) {
						this.redInput.text = value.r.ToString("#.###");
					}
					if (this.redSlider != null) {
						this.redSlider.value = value.r;
					}
					if (this.greenInput != null) {
						this.greenInput.text = value.g.ToString("#.###");
					}
					if (this.greenSlider != null) {
						this.greenSlider.value = value.g;
					}
					if (this.blueInput != null) {
						this.blueInput.text = value.b.ToString("#.###");
					}
					if (this.blueSlider != null) {
						this.blueSlider.value = value.b;
					}
					if (this.alphaInput != null) {
						this.alphaInput.text = value.a.ToString("#.###");
					}
					if (this.alphaSlider != null) {
						this.alphaSlider.value = value.a;
					}
				} finally {
					this.noUserInput--;
				}
			}
		}
	}
}
