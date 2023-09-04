using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MZZT {
	/// <summary>
	/// I want my Toggles!
	/// </summary>
	public static class ToggleGroupExtensions {
		private static FieldInfo m_Toggles;
		public static IEnumerable<Toggle> GetToggles(this ToggleGroup group) {
			if (m_Toggles == null) {
				m_Toggles = typeof(ToggleGroup).GetField(nameof(m_Toggles), BindingFlags.Instance | BindingFlags.NonPublic);
			}

			if (m_Toggles != null) {
#if DEBUG
				try {
#endif
					return (IEnumerable<Toggle>)m_Toggles.GetValue(group);
#if DEBUG
				} catch (Exception ex) {
					Debug.LogException(ex);
				}
#endif
			}

			Debug.LogWarning("ToggleGroup.m_Toggles is gone, update ToggleGroupExtensions!");

			return Object.FindObjectsOfType<Toggle>(true).Where(x => x.group == group);
		}
	}
}
