using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MZZT.DataBinding {
	[CustomPropertyDrawer(typeof(DataboundMemberNameAttribute))]
	public class DataboundMemberNamePropertyDrawer : PropertyDrawer {
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			EditorGUI.BeginProperty(position, label, property);

			position = EditorGUI.PrefixLabel(position, label);

			EditorGUI.BeginChangeCheck();

			DataboundMember bind = (DataboundMember)property.serializedObject.targetObject;
			Type memberType = bind.GetType();
			while (!memberType.IsGenericType || memberType.IsGenericTypeDefinition || memberType.GetGenericTypeDefinition() != typeof(DataboundMember<>)) {
				memberType = memberType.BaseType;
			}
			memberType = memberType.GenericTypeArguments[0];

			IDataboundObject obj = bind.GetComponentInParent<IDataboundObject>();
			Type boundType = null;
			if (obj != null) {
				boundType = obj.GetType();
				while (!boundType.IsGenericType || boundType.IsGenericTypeDefinition || boundType.GetGenericTypeDefinition() != typeof(Databound<>)) {
					boundType = boundType.BaseType;
				}
				boundType = boundType.GenericTypeArguments[0];
			}

			List<(GUIContent content, string id)> options = new List<(GUIContent, string)>();
			if (boundType == null) {
				options.Add((new GUIContent("(No databound object found.)"), ""));
			} else {
				if (memberType.IsAssignableFrom(boundType)) {
					options.Add((new GUIContent($"({boundType.Name})"), ""));
				} else {
					options.Add((new GUIContent($"(None)"), ""));
				}
				options.AddRange(boundType.GetMembers().Where(x => {
					if (x is FieldInfo fieldInfo) {
						return memberType.IsAssignableFrom(fieldInfo.FieldType);
					} else if (x is PropertyInfo propertyInfo) {
						return memberType.IsAssignableFrom(propertyInfo.PropertyType);
					} else if (x is MethodInfo methodInfo) {
						if (methodInfo.GetParameters().Length != 0 || !memberType.IsAssignableFrom(methodInfo.ReturnType)) {
							return false;
						}
						if (methodInfo.Name.StartsWith("get_")) {
							string propertyName = methodInfo.Name.Substring(4);
							return methodInfo.DeclaringType.GetProperty(propertyName) == null;
						}
						return true;
					} else {
						return false;
					}
				})
					.Select(x => (new GUIContent($"{x.Name}{(x is MethodInfo ? "()" : "")}"), x.Name))
					.OrderBy(x => x.Name));
			}

			int index = 0;
			if (!string.IsNullOrEmpty(property.stringValue)) {
				index = options.Select(x => x.id).TakeWhile(x => x != property.stringValue).Count();
				if (index >= options.Count) {
					options.Add((new GUIContent($"{property.stringValue} (Missing)"), property.stringValue));
				}
			}

			index = EditorGUI.Popup(new Rect(position.xMax - position.height, position.y, position.height, position.height), index, options.Select(x => x.content).ToArray());
			if (index > -1 && index < options.Count) {
				property.stringValue = options[index].id;
			}

			position.width -= position.height;

			string input = EditorGUI.TextField(position, property.stringValue);
			if (EditorGUI.EndChangeCheck()) {
				property.stringValue = input;
			}

			EditorGUI.EndProperty();
		}
	}
}
