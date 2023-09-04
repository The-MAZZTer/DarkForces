using MZZT.Data.Binding.UI;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MZZT.Data.Binding {
	public class Migrator : MonoBehaviour {
		private static readonly Dictionary<Type, Dictionary<Type, Dictionary<string, string>>> map = new Dictionary<Type, Dictionary<Type, Dictionary<string, string>>>() {
			[typeof(DataBinding.DataboundActiveIfMemberEqual)] = new() {
				[typeof(DataboundActiveOnMemberValue)] = new() {
					["invert"] = "isNot",
					["isNull"] = "null",
					["expected"] = "expectedValue"
				}
			},
			[typeof(DataBinding.DataboundApplyButton)] = new() {
				[typeof(DataboundApplyButton)] = new() {
				}
			},
			[typeof(DataBinding.DataboundButton)] = new() {
				[typeof(DataboundButton)] = new() {
					["memberName"] = "methodName"
				}
			},
			[typeof(DataBinding.DataboundColor)] = new() {
				[typeof(DatabindObject)] = new() {
					["memberName"] = "memberName"
				},
				[typeof(DataboundColor)] = new() {
					["redInput"] = "redInput",
					["redSlider"] = "redSlider",
					["greenInput"] = "greenInput",
					["greenSlider"] = "greenSlider",
					["blueInput"] = "blueInput",
					["blueSlider"] = "blueSlider",
					["alphaInput"] = "alphaInput",
					["alphaSlider"] = "alphaSlider",
					["preview"] = "preview"
				}
			},
			[typeof(DataBinding.DataboundDeleteButton)] = new() {
				[typeof(DataboundDeleteButton)] = new() {
				}
			},
			[typeof(DataBinding.DataboundEnumDropdown)] = null,
			[typeof(DataBinding.DataboundFlagToggle)] = new() {
				[typeof(DatabindObject)] = new() {
					["changeListenMode"] = "changeDetectionMode",
					["memberName"] = "memberName",
					["valueChangedEventName"] = "valueChangedEventName"
				},
				[typeof(DataboundFlagToggle)] = new() {
					["autoApplyOnChange"] = "autoApplyOnChange",
					["flag"] = "flag"
				}
			},
			[typeof(DataBinding.DataboundInputField)] = null,
			[typeof(DataBinding.DataboundIntegerDropdown)] = null,
			[typeof(DataBinding.DataboundRadio)] = new() {
				[typeof(DatabindObject)] = new() {
					["changeListenMode"] = "changeDetectionMode",
					["memberName"] = "memberName",
					["valueChangedEventName"] = "valueChangedEventName"
				},
				[typeof(DataboundToggleGroupToggle)] = new() {
					["autoApplyOnChange"] = "autoApplyOnChange",
					["value"] = "value"
				}
			},
			[typeof(DataBinding.DataboundRevertButton)] = new() {
				[typeof(DataboundRevertButton)] = new() {
				}
			},
			[typeof(DataBinding.DataboundScrollbar)] = new() {
				[typeof(DatabindObject)] = new() {
					["changeListenMode"] = "changeDetectionMode",
					["memberName"] = "memberName",
					["valueChangedEventName"] = "valueChangedEventName"
				},
				[typeof(DataboundScrollbar)] = new() {
					["autoApplyOnChange"] = "autoApplyOnChange"
				}
			},
			[typeof(DataBinding.DataboundSlider)] = new() {
				[typeof(DatabindObject)] = new() {
					["changeListenMode"] = "changeDetectionMode",
					["memberName"] = "memberName",
					["valueChangedEventName"] = "valueChangedEventName"
				},
				[typeof(DataboundSlider)] = new() {
					["autoApplyOnChange"] = "autoApplyOnChange"
				}
			},
			[typeof(DataBinding.DataboundStringDropdown)] = null,
			[typeof(DataBinding.DataboundText)] = null,
			[typeof(DataBinding.DataboundTmpEnumDropdown)] = new() {
				[typeof(DatabindObject)] = new() {
					["changeListenMode"] = "changeDetectionMode",
					["memberName"] = "memberName",
					["valueChangedEventName"] = "valueChangedEventName"
				},
				[typeof(DataboundEnumDropdown)] = new() {
					["autoApplyOnChange"] = "autoApplyOnChange"
				}
			},
			[typeof(DataBinding.DataboundTmpInputField)] = new() {
				[typeof(DatabindObject)] = new() {
					["changeListenMode"] = "changeDetectionMode",
					["memberName"] = "memberName",
					["valueChangedEventName"] = "valueChangedEventName"
				},
				[typeof(DataboundInputField)] = new() {
					["autoApplyOnChange"] = "autoApplyOnChange"
				}
			},
			[typeof(DataBinding.DataboundTmpIntegerDropdown)] = new() {
				[typeof(DatabindObject)] = new() {
					["changeListenMode"] = "changeDetectionMode",
					["memberName"] = "memberName",
					["valueChangedEventName"] = "valueChangedEventName"
				},
				[typeof(DataboundIntegerDropdown)] = new() {
					["autoApplyOnChange"] = "autoApplyOnChange"
				}
			},
			[typeof(DataBinding.DataboundTmpStringDropdown)] = new() {
				[typeof(DatabindObject)] = new() {
					["changeListenMode"] = "changeDetectionMode",
					["memberName"] = "memberName",
					["valueChangedEventName"] = "valueChangedEventName"
				},
				[typeof(DataboundStringDropdown)] = new() {
					["autoApplyOnChange"] = "autoApplyOnChange"
				}
			},
			[typeof(DataBinding.DataboundTmpText)] = new() {
				[typeof(DatabindObject)] = new() {
					["changeListenMode"] = "changeDetectionMode",
					["memberName"] = "memberName",
					["valueChangedEventName"] = "valueChangedEventName"
				},
				[typeof(DataboundText)] = new() {
				}
			},
			[typeof(DataBinding.DataboundToggle)] = new() {
				[typeof(DatabindObject)] = new() {
					["changeListenMode"] = "changeDetectionMode",
					["memberName"] = "memberName",
					["valueChangedEventName"] = "valueChangedEventName"
				},
				[typeof(DataboundToggle)] = new() {
					["autoApplyOnChange"] = "autoApplyOnChange"
				}
			}
		};

		[MenuItem("Tools/MZZT.Data.Binding/Migrate Selected")]
		static void Migrate() {
			GameObject obj = Selection.activeGameObject;
			if (obj == null) {
				return;
			}

			foreach ((Type type, Dictionary<Type, Dictionary<string, string>> typeMap) in map) {
				foreach (Component script in obj.GetComponentsInChildren(type)) {
					if (typeMap == null) {
						Debug.LogWarning($"{script.name} has deprecated script {type.Name}, it will need to be replaced.");
						continue;
					}

					GameObject gameObject = script.gameObject;
					SerializedObject serializedObject = new(script);

					foreach ((Type newType, Dictionary<string, string> propertyMap) in typeMap) {
						Component newScript = gameObject.AddComponent(newType);
						SerializedObject newSerializedObject = new(newScript);
						foreach ((string prop, string newProp) in propertyMap) {
							SerializedProperty serializedProperty = serializedObject.FindProperty(prop);
							SerializedProperty newSerializedProperty = newSerializedObject.FindProperty(newProp);

							SerializedPropertyType propertyType = serializedProperty.propertyType;
							switch (propertyType) {
								case SerializedPropertyType.AnimationCurve:
									newSerializedProperty.animationCurveValue = serializedProperty.animationCurveValue;
									break;
								case SerializedPropertyType.ArraySize:
									newSerializedProperty.arraySize = serializedProperty.arraySize;
									break;
								case SerializedPropertyType.Boolean:
									newSerializedProperty.boolValue = serializedProperty.boolValue;
									break;
								case SerializedPropertyType.Bounds:
									newSerializedProperty.boundsValue = serializedProperty.boundsValue;
									break;
								case SerializedPropertyType.BoundsInt:
									newSerializedProperty.boundsIntValue = serializedProperty.boundsIntValue;
									break;
								case SerializedPropertyType.Character:
									newSerializedProperty.stringValue = serializedProperty.stringValue;
									break;
								case SerializedPropertyType.Color:
									newSerializedProperty.colorValue = serializedProperty.colorValue;
									break;
								case SerializedPropertyType.Enum:
									newSerializedProperty.enumValueFlag = serializedProperty.enumValueFlag;
									break;
								case SerializedPropertyType.ExposedReference:
									newSerializedProperty.exposedReferenceValue = serializedProperty.exposedReferenceValue;
									break;
								case SerializedPropertyType.Float:
									newSerializedProperty.floatValue = serializedProperty.floatValue;
									break;
								case SerializedPropertyType.Hash128:
									newSerializedProperty.hash128Value = serializedProperty.hash128Value;
									break;
								case SerializedPropertyType.Integer:
									newSerializedProperty.intValue = serializedProperty.intValue;
									break;
								case SerializedPropertyType.ManagedReference:
									newSerializedProperty.managedReferenceValue = serializedProperty.managedReferenceValue;
									break;
								case SerializedPropertyType.ObjectReference:
									newSerializedProperty.objectReferenceValue = serializedProperty.objectReferenceValue;
									break;
								case SerializedPropertyType.Quaternion:
									newSerializedProperty.quaternionValue = serializedProperty.quaternionValue;
									break;
								case SerializedPropertyType.Rect:
									newSerializedProperty.rectValue = serializedProperty.rectValue;
									break;
								case SerializedPropertyType.RectInt:
									newSerializedProperty.rectIntValue = serializedProperty.rectIntValue;
									break;
								case SerializedPropertyType.String:
									newSerializedProperty.stringValue = serializedProperty.stringValue;
									break;
								case SerializedPropertyType.Vector2:
									newSerializedProperty.vector2Value = serializedProperty.vector2Value;
									break;
								case SerializedPropertyType.Vector2Int:
									newSerializedProperty.vector2IntValue = serializedProperty.vector2IntValue;
									break;
								case SerializedPropertyType.Vector3:
									newSerializedProperty.vector3Value = serializedProperty.vector3Value;
									break;
								case SerializedPropertyType.Vector3Int:
									newSerializedProperty.vector3IntValue = serializedProperty.vector3IntValue;
									break;
								case SerializedPropertyType.Vector4:
									newSerializedProperty.vector4Value = serializedProperty.vector4Value;
									break;
								default:
									newSerializedProperty.stringValue = serializedProperty.stringValue;
									Debug.LogError($"{script.name}.{prop}: Unsupported property type {propertyType}.");
									break;
							}
						}

						if (typeof(DataboundFormButton).IsAssignableFrom(newType)) {
							newSerializedObject.FindProperty("form").objectReferenceValue =
								((Component)gameObject.GetComponentInParent<DataBinding.IDataboundObject>()).transform;
						}

						newSerializedObject.ApplyModifiedProperties();

						Debug.Log($"{script.name}: Migrated properties on {type} to {newType}.");
					}

					DestroyImmediate(script);
				}
			}
		}
	}
}
