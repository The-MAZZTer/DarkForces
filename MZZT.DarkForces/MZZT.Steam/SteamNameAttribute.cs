using System;

namespace MZZT.Steam {
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class SteamNameAttribute : Attribute {
		public SteamNameAttribute(string name = null) : base() {
			this.Name = name;
		}

		public string Name { get; set; }
	}
}