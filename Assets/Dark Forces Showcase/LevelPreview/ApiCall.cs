using System.Runtime.Serialization;

namespace MZZT.DarkForces.Showcase {
	[DataContract]
	class ApiCall {
		[DataMember]
		public int Id { get; set; }
		[DataMember]
		public string Api { get; set; }
		[DataMember]
		public string[] Args { get; set; }
	}
}
