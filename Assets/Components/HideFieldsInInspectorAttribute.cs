using System;
using System.Collections.Generic;

namespace MZZT.Components {
	// https://forum.unity.com/threads/hiding-inherited-public-variable-in-the-inspector.161828/
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
  public class HideFieldsInInspectorAttribute : Attribute {
    public HideFieldsInInspectorAttribute(params string[] fieldNames) {
      this.FieldNames = new HashSet<string>(fieldNames);
    }
    public HashSet<string> FieldNames { get; private set; }
  }
}
