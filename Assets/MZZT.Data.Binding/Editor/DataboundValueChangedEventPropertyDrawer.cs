using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MZZT.Data.Binding {
	[CustomPropertyDrawer(typeof(DataboundValueChangedEventAttribute))]
	public class DataboundValueChangedEventPropertyDrawer : PropertyDrawer {
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			EditorGUI.BeginProperty(position, label, property);

			position = EditorGUI.PrefixLabel(position, label);

			EditorGUI.BeginChangeCheck();

			IDatabind bind = (IDatabind)property.serializedObject.targetObject;
			IDatabind obj = null;
			if (bind.ValueStorageMethod == DatabindingDefaultValueStorageMethods.BindToParentMember || bind.ValueStorageMethod == DatabindingDefaultValueStorageMethods.BindToParentKey) {
				obj = bind.Parent ?? ((Component)bind).GetComponentsInParent<IDatabind>(true).FirstOrDefault(x => x != bind);
			}

			Type boundType = null;
			if (obj != null) {
				boundType = obj.GetType();
				while (!boundType.IsGenericType || boundType.IsGenericTypeDefinition || boundType.GetGenericTypeDefinition() != typeof(Databind<>)) {
					boundType = boundType.BaseType;
				}
				boundType = boundType.GenericTypeArguments[0];
			}

			List<(GUIContent content, string id)> options = new();
			if (boundType == null) {
				options.Add((new GUIContent("(No databound object found.)"), ""));
			} else {
				options.Add((new GUIContent("(None)"), ""));
				//MethodInfo eventHandler = typeof(DataboundMember<>).GetMethod("ValueChanged", BindingFlags.NonPublic | BindingFlags.Instance);
				MethodInfo eventHandler = this.GetType().GetMethod(nameof(this.DummyEventHandler), BindingFlags.NonPublic | BindingFlags.Instance);
				/*Type notifyPropertyChanged = typeof(INotifyPropertyChanged);
				EventInfo propertyChanged = notifyPropertyChanged.GetEvent(nameof(INotifyPropertyChanged.PropertyChanged));
				MethodInfo[] propertyChangedMethods;
				if (notifyPropertyChanged.IsAssignableFrom(boundType)) {
					propertyChangedMethods = boundType.GetInterfaceMap(notifyPropertyChanged).TargetMethods;
				} else {
					propertyChangedMethods = new MethodInfo[] { };
				}*/
				options.AddRange(boundType.GetEvents()
					.Where(x => /*!propertyChangedMethods.Contains(x.AddMethod) &&*/ Delegate.CreateDelegate(x.EventHandlerType, this, eventHandler, false) != null)
					.Select(x => (new GUIContent(x.Name), x.Name))
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

		private void DummyEventHandler(object sender, EventArgs e) { }
	}
}
