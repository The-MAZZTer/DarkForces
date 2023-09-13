using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MZZT {
	/// <summary>
	/// I want my Toggles!
	/// </summary>
	public static class ToggleGroupExtensions {
		//private static FieldInfo m_Toggles;
		public static IEnumerable<Toggle> GetToggles(this ToggleGroup group) {
			// This method doesn't work if the objects are in the process of being enabled... they are not considered part of the group then!
			/*if (m_Toggles == null) {
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

			Debug.LogWarning("ToggleGroup.m_Toggles is gone, update ToggleGroupExtensions!");*/

			return Object.FindObjectsOfType<Toggle>(true).Where(x => x.group == group);
		}
	}
}
