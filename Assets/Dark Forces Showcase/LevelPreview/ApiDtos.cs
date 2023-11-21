using System.Runtime.Serialization;
using static MZZT.DarkForces.FileFormats.DfLevel;
using static MZZT.DarkForces.FileFormats.DfLevelObjects;

namespace MZZT.DarkForces.Showcase {
	[DataContract]
	public class LevelListLevelInfo {
		[DataMember]
		public string FileName { get; set; }
		[DataMember]
		public string DisplayName { get; set; }
	}

	[DataContract]
	public class LevelInfo {
		[DataMember]
		public string LevelFile { get; set; }
		[DataMember]
		public string PaletteFile { get; set; }
		[DataMember]
		public string MusicFile { get; set; }
		[DataMember]
		public float ParallaxX { get; set; }
		[DataMember]
		public float ParallaxY { get; set; }
		[DataMember]
		public SectorInfo[] Sectors { get; set; }
	}

	[DataContract]
	public class WallSurfaceInfo {
		[DataMember]
		public string TextureFile { get; set; }
		[DataMember]
		public float TextureOffsetX { get; set; }
		[DataMember]
		public float TextureOffsetY { get; set; }
		[DataMember]
		public int TextureUnknown { get; set; }
	}

	[DataContract]
	public class HorizontalSurfaceInfo : WallSurfaceInfo {
		[DataMember]
		public float Y { get; set; }
	}

	[DataContract]
	public class WallInfo {
		[DataMember]
		public float LeftVertexX { get; set; }
		[DataMember]
		public float LeftVertexZ { get; set; }
		[DataMember]
		public float RightVertexX { get; set; }
		[DataMember]
		public float RightVertexZ { get; set; }
		[DataMember]
		public WallSurfaceInfo MainTexture { get; set; }
		[DataMember]
		public WallSurfaceInfo TopEdgeTexture { get; set; }
		[DataMember]
		public WallSurfaceInfo BottomEdgeTexture { get; set; } 
		[DataMember]
		public WallSurfaceInfo SignTexture { get; set; }
		[DataMember]
		public int AdjoinedSector { get; set; } = -1;
		[DataMember]
		public int AdjoinedWall { get; set; } = -1;
		[DataMember]
		public WallTextureAndMapFlags TextureAndMapFlags { get; set; }
		[DataMember]
		public int UnusedFlags2 { get; set; }
		[DataMember]
		public WallAdjoinFlags AdjoinFlags { get; set; }
		[DataMember]
		public short LightLevel { get; set; }
	}

	[DataContract]
	public class SectorInfo {
		[DataMember]
		public string Name { get; set; }
		[DataMember]
		public int LightLevel { get; set; }
		[DataMember]
		public HorizontalSurfaceInfo Floor { get; set; }
		[DataMember]
		public HorizontalSurfaceInfo Ceiling { get; set; }
		[DataMember]
		public float AltY { get; set; }
		[DataMember]
		public SectorFlags Flags { get; set; }
		[DataMember]
		public int UnusedFlags2 { get; set; }
		[DataMember]
		public int AltLightLevel { get; set; }
		[DataMember]
		public int Layer { get; set; }
		[DataMember]
		public WallInfo[] Walls { get; set; }
	}

	[DataContract]
	public class ObjectInfo {
		[DataMember]
		public ObjectTypes Type { get; set; }
		[DataMember]
		public string FileName { get; set; }
		[DataMember]
		public float PositionX { get; set; }
		[DataMember]
		public float PositionY { get; set; }
		[DataMember]
		public float PositionZ { get; set; }
		[DataMember]
		public float Pitch{ get; set; }
		[DataMember]
		public float Yaw { get; set; }
		[DataMember]
		public float Roll{ get; set; }
		[DataMember]
		public Difficulties Difficulty { get; set; }
		[DataMember]
		public string Logic { get; set; }
	}
}
