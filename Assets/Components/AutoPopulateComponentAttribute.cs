using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MZZT.Components {
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	public class AutoPopulateComponentAttribute : Attribute {
		public AutoPopulateComponentAttribute(AutoPopulateComponentTargets searchIn = AutoPopulateComponentTargets.Current) {
			this.SearchIn = searchIn;
		}

		public AutoPopulateComponentTargets SearchIn { get; set; }

		public bool IncludeInactiveChildren { get; set; }

		public static void Init(Component obj) {
			foreach ((MemberInfo member, AutoPopulateComponentAttribute attribute) in obj.GetType()
				.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
				.Where(x => x.MemberType == MemberTypes.Field || x.MemberType == MemberTypes.Property)
				.Select(x => (x, x.GetCustomAttribute<AutoPopulateComponentAttribute>()))
				.Where(x => x.Item2 != null)) {

				attribute.Populate(obj, member);
			}
		}

		public void Populate(Component obj, MemberInfo member) {
			Type type = null;
			switch (member.MemberType) {
				case MemberTypes.Field:
					type = ((FieldInfo)member).FieldType;
					break;
				case MemberTypes.Property:
					type = ((PropertyInfo)member).PropertyType;
					break;
			}
			if (!typeof(Component).IsAssignableFrom(type)) {
				Debug.LogError($"ComponentFindDefaultAttribute: {obj.gameObject.name}.{obj.GetType().Name}.{member.Name} wants a {type.Name} but that's not a Component!");
				return;
			}

			Component oldValue = null;
			switch (member.MemberType) {
				case MemberTypes.Field:
					oldValue = (Component)((FieldInfo)member).GetValue(obj);
					break;
				case MemberTypes.Property:
					oldValue = (Component)((PropertyInfo)member).GetValue(obj);
					break;
			}

			if (oldValue != null) {
				return;
			}

			Component component;
			switch (this.SearchIn) {
				case AutoPopulateComponentTargets.Children:
					component = obj.GetComponentInChildren(type, this.IncludeInactiveChildren);
					break;
				case AutoPopulateComponentTargets.Parent:
					component = obj.GetComponentInParent(type);
					break;
				default:
					component = obj.GetComponent(type);
					break;
			}

			if (component == null) {
				//Debug.LogError($"ComponentFindDefaultAttribute: {obj.gameObject.name}.{obj.GetType().Name}.{member.Name} can't find the requested {type.Name}!");
				return;
			}

			switch (member.MemberType) {
				case MemberTypes.Field:
					((FieldInfo)member).SetValue(obj, component);
					break;
				case MemberTypes.Property:
					((PropertyInfo)member).SetValue(obj, component);
					break;
			}
		}
	}

	public enum AutoPopulateComponentTargets {
		Current,
		Parent,
		Children
	}

	public abstract class ComponentPopulatorMonoBehaviour : MonoBehaviour {
		protected virtual void Awake() {
			AutoPopulateComponentAttribute.Init(this);
		}
	}
}
