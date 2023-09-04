using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace MZZT.Data.Binding.UI {
	[RequireComponent(typeof(TMP_Dropdown))]
	public class DataboundEnumDropdown : DataboundUi<int>  {
		protected TMP_Dropdown Dropdown => this.Selectable as TMP_Dropdown;

		private int[] indexToEnumValue;
		private Dictionary<int, int> enumToIndexValue;

		private void Init() {
			if (this.enumToIndexValue != null) {
				return;
			}

			Type type = this.Databinder.MemberType;
			Assert.IsTrue(type.IsEnum);

			(TMP_Dropdown.OptionData option, int value)[] values = Enum.GetValues(type).Cast<object>().Select(value => {
				string name = Enum.GetName(type, value);
				FieldInfo field = type.GetField(name);
				name = field.GetCustomAttribute<XmlEnumAttribute>()?.Name ?? name;
				name = field.GetCustomAttribute<DataMemberAttribute>()?.Name ?? name;
				name = field.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? name;

				return (new TMP_Dropdown.OptionData(name), Convert.ToInt32(value));
			}).ToArray();
			this.indexToEnumValue = values.Select(x => x.value).ToArray();
			this.enumToIndexValue = this.indexToEnumValue.Select((value, index) => (value, index)).ToDictionary(x => x.value, x => x.index);

			this.Dropdown.ClearOptions();
			this.Dropdown.AddOptions(values.Select(x => x.option).ToList());

			this.Dropdown.onValueChanged.AddListener(value => this.OnUserEnteredValueChanged());
		}

		protected override void Start() {
			base.Start();

			this.Init();
		}

		protected override int UserEnteredValue {
			get {
				this.Init();
				return this.indexToEnumValue[this.Dropdown.value];
			}
			set {
				this.Init();
				this.Dropdown.value = this.enumToIndexValue[value];
			}
		}

		private void Update() {
			int value = this.Dropdown.value;
			if (this.Dropdown.captionText.text == "" && !string.IsNullOrEmpty(this.Dropdown.options[value].text)) {
				this.userInput = false;
				try {
					this.Dropdown.value = -1;
					this.Dropdown.value = value;
				} finally {
					this.userInput = true;
				}
			}
		}
	}
}
