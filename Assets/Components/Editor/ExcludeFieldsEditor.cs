using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MZZT.Components {
  // https://forum.unity.com/threads/hiding-inherited-public-variable-in-the-inspector.161828/
  [CustomEditor(typeof(MonoBehaviour), true)]
  public class ExcludeFieldsEditor : Editor {
    private void OnEnable() =>
      this.excludedFields = target.GetType()
        .GetCustomAttributes<HideFieldsInInspectorAttribute>(false)
        .FirstOrDefault()?.FieldNames.ToArray();
    private string[] excludedFields;

    public override void OnInspectorGUI() {
      if ((this.excludedFields?.Length ?? 0) > 0) {
        this.serializedObject.Update();
        EditorGUI.BeginChangeCheck();
        DrawPropertiesExcluding(this.serializedObject, this.excludedFields);
        if (EditorGUI.EndChangeCheck()) {
          this.serializedObject.ApplyModifiedProperties();
        }
      } else {
        base.OnInspectorGUI();
      }
    }
  }
}
