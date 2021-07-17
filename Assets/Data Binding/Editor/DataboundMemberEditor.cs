using MZZT.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MZZT.DataBinding {
	[CustomEditor(typeof(DataboundMember), true)]
	public class DataboundMemberEditor : Editor {
		public override void OnInspectorGUI() {
			DataboundMember bind = (DataboundMember)this.serializedObject.targetObject;
			IDataboundObject obj = bind.GetComponentInParent<IDataboundObject>();
			Type boundType = null;
			if (obj != null) {
				boundType = obj.GetType();
				while (!boundType.IsGenericType || boundType.IsGenericTypeDefinition || boundType.GetGenericTypeDefinition() != typeof(Databound<>)) {
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

					if ("m_Script" == iterator.propertyPath && !hiddenFields.Contains("m_DataboundType")) {
						EditorGUILayout.LabelField("Databound Type", boundType?.Name ?? "(None)");
					}
				}
				enterChildren = false;
			}
			this.serializedObject.ApplyModifiedProperties();
			EditorGUI.EndChangeCheck();
		}
	}
}
