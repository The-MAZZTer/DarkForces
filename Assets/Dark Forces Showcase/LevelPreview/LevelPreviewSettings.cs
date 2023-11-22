 using System.Runtime.Serialization;
using TMPro;

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
		[DataMember]
		public bool ShowHud { get; set; } = true;
		[DataMember]
		public float HudColorR { get; set; } = 1;
		[DataMember]
		public float HudColorG { get; set; } = 0;
		[DataMember]
		public float HudColorB { get; set; } = 0;
		[DataMember]
		public float HudColorA { get; set; } = 1;
		[DataMember]
		public TextAlignmentOptions HudAlign { get; set; } = TextAlignmentOptions.TopLeft;
		[DataMember]
		public float HudFontSize { get; set; } = 36;
		[DataMember]
		public bool ShowHudCoordinates { get; set; } = true;
		[DataMember]
		public string HudFpsCoordinates { get; set; } = "Pos: {x:F2} {y:F2} {z:F2}\nRot: {pitch:F2} {yaw:F2}";
		[DataMember]
		public string HudOrbitCoordinates { get; set; } = "Focus: {x:F2} {y:F2} {z:F2}\nRot: {pitch:F2} {yaw:F2}\nDist: {distance:F2}";
		[DataMember]
		public bool ShowHudRaycastHit { get; set; } = true;
		[DataMember]
		public string HudRaycastFloor { get; set; } = "Sector {sector} Floor\nCursor: {hitX:F2} {hitY:F2} {hitZ:F2}\nTexture: {textureFile}\nLight: {light}\nLayer: {layer}\nFlags: {flags}";
		[DataMember]
		public string HudRaycastCeiling { get; set; } = "Sector {sector} Ceiling\nCursor: {hitX:F2} {hitY:F2} {hitZ:F2}\nTexture: {textureFile}\nLight: {light}\nLayer: {layer}\nFlags: {flags}";
		[DataMember]
		public string HudRaycastWall { get; set; } = "Sector {sector} Wall {wall}\nCursor: {hitX:F2} {hitY:F2} {hitZ:F2}\nLeft: {x1:F2} {z1:F2} Right: {x2:F2} {z2:F2}\nAdjoin: {adjoinSector} {adjoinWall}\nWall Light: {wallLight}\nMain Texture: {midTextureFile}\nTop Texture: {topTextureFile}\nBot Texture: {botTextureFile}\nSign Texture: {signTextureFile}\nTexture/Map Flags: {textureMapFlags}\nAdjoin Flags: {adjoinFlags}";
		[DataMember]
		public string HudRaycastObject { get; set; } = "Sector {sector} Object {object} Type {type}\nCursor: {hitX:F2} {hitY:F2} {hitZ:F2}\nFile: {filename}\nPosition: {x:F2} {y:F2} {z:F2}\nRotation: {pitch:F2} {yaw:F2} {roll:F2}\nDifficulty: {difficulty}\n{logic}";
	}
}
