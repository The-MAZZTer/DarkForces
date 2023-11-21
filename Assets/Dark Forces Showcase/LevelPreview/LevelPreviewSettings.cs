 using System.Runtime.Serialization;

namespace MZZT.DarkForces.Showcase {
	[DataContract]
	public class LevelPreviewSettings {
		[DataMember]
		public string DarkForcesPath { get; set; } = string.Empty;
		[DataMember]
		public string[] DataFiles { get; set; } = new[] {
			@"DARK.GOB",
			@"SOUNDS.GOB",
			@"SPRITES.GOB",
			@"TEXTURES.GOB"
		};
#if !UNITY_WEBGL
		[DataMember]
		public int ApiPort { get; set; } = 8761;
#endif
		[DataMember]
		public float BackgroundR{ get; set; } = 0;
		[DataMember]
		public float BackgroundG { get; set; } = 0;
		[DataMember]
		public float BackgroundB { get; set; } = 0;
		[DataMember]
		public bool ShowWaitBitmap { get; set; } = false;
		[DataMember]
		public float ExtendSkyPit { get; set; } = 100;
		[DataMember]
		public bool ShowSprites { get; set; } = true;
		[DataMember]
		public bool Show3dos { get; set; } = true;
		[DataMember]
		public ObjectGenerator.Difficulties Difficulty { get; set; } = ObjectGenerator.Difficulties.All;
		[DataMember]
		public bool AnimateVues { get; set; } = false;
		[DataMember]
		public bool Animate3doUpdates { get; set; } = true;
		[DataMember]
		public bool FullBrightLighting { get; set; } = false;
		[DataMember]
		public bool BypassColormapDithering { get; set; } = false;
		[DataMember] 
		public bool PlayMusic { get; set; } = false;
		[DataMember]
		public bool PlayFightTrack { get; set; } = false;
		[DataMember]
		public float Volume { get; set; } = 1f;
		[DataMember]
		public int? VisibleLayer { get; set; } = null;
		[DataMember]
		public float LookSensitivityX { get; set; } = 0.25f;
		[DataMember]
		public float LookSensitivityY { get; set; } = 0.25f;
		[DataMember]
		public bool InvertYLook { get; set; } = false;
		[DataMember]
		public float MoveSensitivityX { get; set; } = 0.25f;
		[DataMember]
		public float MoveSensitivityY { get; set; } = 0.25f;
		[DataMember]
		public float MoveSensitivityZ { get; set; } = 0.25f;
		[DataMember]
		public float YawLimitMin { get; set; } = -89.999f;
		[DataMember]
		public float YawLimitMax { get; set; } = 89.999f;
		[DataMember]
		public float RunMultiplier { get; set; } = 2f;
		[DataMember]
		public float ZoomSensitivity { get; set; } = 1f;
		[DataMember]
		public bool UseOrbitCamera { get; set; } = true;
		[DataMember]
		public bool UseMouseCapture { get; set; } = false;
	}
}
