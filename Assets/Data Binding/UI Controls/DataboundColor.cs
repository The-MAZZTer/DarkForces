using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT.DataBinding {
	public class DataboundColor : Databound<Color> {
		[SerializeField]
		private string memberName;

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

		private void Start() {
			IDataboundObject databound = this.GetComponentsInParent<IDataboundObject>(true).First(x => (object)x != this);
			databound.ValueChanged += this.Databound_ValueChanged;
			this.OnValueChanged();

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

			this.ValueChanged += this.DataboundColor_ValueChanged;
			this.DataboundColor_ValueChanged(this, new EventArgs());
		}

		private void Input_ValueChanged(string value, Slider slider, string fieldName) {
			if (this.noUserInput > 0 || !float.TryParse(value, out float floatValue)) {
				return;
			}

			floatValue = Mathf.Clamp01(floatValue);

			Color color = this.Value;
			FieldInfo field = color.GetType().GetField(fieldName);
			field.SetValueDirect(__makeref(color), floatValue);

			this.noUserInput++;
			try {
				this.Value = color;
				if (slider != null) {
					slider.value = floatValue;
				}
			} finally {
				this.noUserInput--;
			}
		}

		private void Slider_ValueChanged(float value, TMP_InputField input, string fieldName) {
			if (this.noUserInput > 0) {
				return;
			}

			value = Mathf.Clamp01(value);

			Color color = this.Value;
			FieldInfo field = color.GetType().GetField(fieldName);
			field.SetValueDirect(__makeref(color), value);

			this.noUserInput++;
			try {
				this.Value = color;
				if (input != null) {
					input.text = value.ToString("#.###");
				}
			} finally {
				this.noUserInput--;
			}
		}

		private int noUserInput = 0;
		private void DataboundColor_ValueChanged(object sender, EventArgs e) {
			if (this.preview != null) {
				this.preview.color = this.Value;
			}

			if (this.noUserInput > 0) {
				return;
			}

			this.noUserInput++;
			try {
				if (this.redInput != null) {
					this.redInput.text = this.Value.r.ToString("#.###");
				}
				if (this.redSlider != null) {
					this.redSlider.value = this.Value.r;
				}
				if (this.greenInput != null) {
					this.greenInput.text = this.Value.g.ToString("#.###");
				}
				if (this.greenSlider != null) {
					this.greenSlider.value = this.Value.g;
				}
				if (this.blueInput != null) {
					this.blueInput.text = this.Value.b.ToString("#.###");
				}
				if (this.blueSlider != null) {
					this.blueSlider.value = this.Value.b;
				}
				if (this.alphaInput != null) {
					this.alphaInput.text = this.Value.a.ToString("#.###");
				}
				if (this.alphaSlider != null) {
					this.alphaSlider.value = this.Value.a;
				}
			} finally {
				this.noUserInput--;
			}
		}

		private void Databound_ValueChanged(object sender, EventArgs e) {
			this.OnValueChanged();
		}

		private void OnValueChanged() {
			IDataboundObject databound = this.GetComponentsInParent<IDataboundObject>(true).First(x => (object)x != this);
			object value = databound.Value;
			if (value == null) {
				return;
			}

			MemberInfo member = value.GetType().GetMember(this.memberName).First();

			this.Value = (Color)(member switch {
				FieldInfo field => field.GetValue(value),
				PropertyInfo property => property.GetValue(value),
				MethodInfo method => method.Invoke(value, Array.Empty<object>()),
				_ => throw new NotImplementedException()
			});
		}
	}
}
