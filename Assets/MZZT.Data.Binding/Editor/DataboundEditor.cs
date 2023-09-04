using MZZT.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MZZT.Data.Binding {
	[CustomEditor(typeof(Databind), true)]
	public class DataboundEditor : Editor {
		public override void OnInspectorGUI() {
			IDatabind bind = (IDatabind)this.serializedObject.targetObject;
			IDatabind obj;
			if (bind.ValueStorageMethod == DatabindingDefaultValueStorageMethods.BindToParentMember || bind.ValueStorageMethod == DatabindingDefaultValueStorageMethods.BindToParentKey) {
				obj = bind.Parent ?? ((Component)bind).GetComponentsInParent<IDatabind>(true).FirstOrDefault(x => x != bind);
			} else {
				obj = null;
			}
			Type boundType = null;
			if (obj != null) {
				boundType = obj.GetType();
				while (!boundType.IsGenericType || boundType.IsGenericTypeDefinition || boundType.GetGenericTypeDefinition() != typeof(Databind<>)) {
					boundType = boundType.BaseType;
				}
				boundType = boundType.GenericTypeArguments[0];
			}

			HashSet<string> hiddenFields = bind.GetType().GetCustomAttribute<HideFieldsInInspectorAttribute>()?.FieldNames ?? new HashSet<string>();

			EditorGUI.BeginChangeCheck();
			this.serializedObject.UpdateIfRequiredOrScript();
			SerializedProperty iterator = this.serializedObject.GetIterator();
			bool enterChildren = true;
			while (iterator.NextVisible(enterChildren)) {
				using (new EditorGUI.DisabledScope("m_Script" == iterator.propertyPath)) {
					if (!hiddenFields.Contains(iterator.propertyPath)) {
						EditorGUILayout.PropertyField(iterator, true);
					}

					if ("m_Script" == iterator.propertyPath && !hiddenFields.Contains("__ParentType")) {
						EditorGUILayout.LabelField("Parent Type", boundType?.Name ?? "(None)");
					}
				}
				enterChildren = false;
			}
			this.serializedObject.ApplyModifiedProperties();
			EditorGUI.EndChangeCheck();
		}
	}
}
