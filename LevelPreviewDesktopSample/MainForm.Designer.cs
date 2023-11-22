namespace MZZT.LevelPreviewDesktopSample {
	partial class MainForm {
		/// <summary>
		///  Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		///  Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			Label labelLevels;
			Label labelLayers;
			Label labelSettingsAppearance;
			Label labelSettingsGeometry;
			Label labelSettingsObjects;
			Label labelSettingsLighting;
			Label labelSettingsMusic;
			Label labelSettingsCamera;
			Label labelLevel;
			Label labelGeometry;
			Label labelGeometryHelp;
			Label labelObjects;
			Label labelObjectsHelp;
			Label labelCamera;
			Label labelSettingsHud;
			this.split = new SplitContainer();
			this.flow = new FlowLayoutPanel();
			this.buttonQuit = new Button();
			this.levelFlow = new FlowLayoutPanel();
			this.layersFlow = new FlowLayoutPanel();
			this.buttonSetBackground0 = new Button();
			this.buttonSetBackground1 = new Button();
			this.buttonSetBackground2 = new Button();
			this.buttonSetShowWaitBitmap0 = new Button();
			this.buttonSetShowWaitBitmap1 = new Button();
			this.buttonSetExtendSkyPit0 = new Button();
			this.buttonSetExtendSkyPit1 = new Button();
			this.buttonSetShowSprites0 = new Button();
			this.buttonSetShowSprites1 = new Button();
			this.buttonSetShow3dos0 = new Button();
			this.buttonSetShow3dos1 = new Button();
			this.buttonSetDifficulty0 = new Button();
			this.buttonSetDifficulty1 = new Button();
			this.buttonSetDifficulty2 = new Button();
			this.buttonSetDifficulty3 = new Button();
			this.buttonSetAnimateVues0 = new Button();
			this.buttonSetAnimateVues1 = new Button();
			this.buttonSetAnimate3doUpdates0 = new Button();
			this.buttonSetAnimate3doUpdates1 = new Button();
			this.buttonSetFullBrightLighting0 = new Button();
			this.buttonSetFullBrightLighting1 = new Button();
			this.buttonSetBypassColorDithering0 = new Button();
			this.buttonSetBypassColorDithering1 = new Button();
			this.buttonSetPlayMusic0 = new Button();
			this.buttonSetPlayMusic1 = new Button();
			this.buttonSetPlayFightTrack0 = new Button();
			this.buttonSetPlayFightTrack1 = new Button();
			this.buttonSetVolume0 = new Button();
			this.buttonSetVolume1 = new Button();
			this.buttonSetLookSensitivity0 = new Button();
			this.buttonSetLookSensitivity1 = new Button();
			this.buttonSetLookSensitivity2 = new Button();
			this.buttonSetInvertYLook0 = new Button();
			this.buttonSetInvertYLook1 = new Button();
			this.buttonSetMoveSensitivity0 = new Button();
			this.buttonSetMoveSensitivity1 = new Button();
			this.buttonSetMoveSensitivity2 = new Button();
			this.buttonSetYawLimits0 = new Button();
			this.buttonSetYawLimits1 = new Button();
			this.buttonSetRunMultiplier0 = new Button();
			this.buttonSetRunMultiplier1 = new Button();
			this.buttonSetZoomSensitivity0 = new Button();
			this.buttonSetZoomSensitivity1 = new Button();
			this.buttonSetOrbitCamera = new Button();
			this.buttonSetUseMouseCapture0 = new Button();
			this.buttonSetUseMouseCapture1 = new Button();
			this.buttonSetShowHud0 = new Button();
			this.buttonSetShowHud1 = new Button();
			this.buttonSetHudAlign0 = new Button();
			this.buttonSetHudAlign1 = new Button();
			this.buttonSetHudFontSize0 = new Button();
			this.buttonSetHudFontSize1 = new Button();
			this.buttonSetHudColor0 = new Button();
			this.buttonSetHudColor1 = new Button();
			this.buttonSetShowHudCoordinates0 = new Button();
			this.buttonSetShowHudCoordinates1 = new Button();
			this.buttonSetHudFpsCoordinates0 = new Button();
			this.buttonSetHudFpsCoordinates1 = new Button();
			this.buttonSetHudOrbitCoordinates0 = new Button();
			this.buttonSetHudOrbitCoordinates1 = new Button();
			this.buttonSetShowHudRaycastHit0 = new Button();
			this.buttonSetShowHudRaycastHit1 = new Button();
			this.buttonSetHudRaycastFloor0 = new Button();
			this.buttonSetHudRaycastFloor1 = new Button();
			this.buttonSetHudRaycastCeiling0 = new Button();
			this.buttonSetHudRaycastCeiling1 = new Button();
			this.buttonSetHudRaycastWall0 = new Button();
			this.buttonSetHudRaycastWall1 = new Button();
			this.buttonSetHudRaycastObject0 = new Button();
			this.buttonSetHudRaycastObject1 = new Button();
			this.buttonReloadLevelInPlace = new Button();
			this.buttonInitEmptyLevel = new Button();
			this.buttonReloadLevelGeometry = new Button();
			this.buttonSetLevelMetadata0 = new Button();
			this.buttonSetLevelMetadata1 = new Button();
			this.buttonReloadSector = new Button();
			this.buttonSetSector = new Button();
			this.buttonMoveSector = new Button();
			this.buttonDeleteSector = new Button();
			this.buttonSetSectorFloor = new Button();
			this.buttonSetSectorCeiling = new Button();
			this.buttonReloadWall = new Button();
			this.buttonInsertWall = new Button();
			this.buttonDeleteWall = new Button();
			this.buttonSetVertex = new Button();
			this.buttonReloadLevelObjects = new Button();
			this.buttonSetObject = new Button();
			this.buttonDeleteObject = new Button();
			this.buttonResetCamera = new Button();
			this.buttonMoveCamera = new Button();
			this.buttonRotateCamera = new Button();
			this.buttonMoveAndRotateCamera = new Button();
			this.buttonPointCameraAt = new Button();
			this.buttonCaptureMouse = new Button();
			this.status = new StatusStrip();
			this.statusText = new ToolStripStatusLabel();
			labelLevels = new Label();
			labelLayers = new Label();
			labelSettingsAppearance = new Label();
			labelSettingsGeometry = new Label();
			labelSettingsObjects = new Label();
			labelSettingsLighting = new Label();
			labelSettingsMusic = new Label();
			labelSettingsCamera = new Label();
			labelLevel = new Label();
			labelGeometry = new Label();
			labelGeometryHelp = new Label();
			labelObjects = new Label();
			labelObjectsHelp = new Label();
			labelCamera = new Label();
			labelSettingsHud = new Label();
			((System.ComponentModel.ISupportInitialize)this.split).BeginInit();
			this.split.Panel2.SuspendLayout();
			this.split.SuspendLayout();
			this.flow.SuspendLayout();
			this.status.SuspendLayout();
			this.SuspendLayout();
			// 
			// labelLevels
			// 
			labelLevels.AutoEllipsis = true;
			labelLevels.AutoSize = true;
			labelLevels.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
			labelLevels.Location = new Point(3, 31);
			labelLevels.Name = "labelLevels";
			labelLevels.Size = new Size(57, 21);
			labelLevels.TabIndex = 1;
			labelLevels.Text = "Levels";
			// 
			// labelLayers
			// 
			labelLayers.AutoEllipsis = true;
			labelLayers.AutoSize = true;
			labelLayers.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
			labelLayers.Location = new Point(3, 58);
			labelLayers.Name = "labelLayers";
			labelLayers.Size = new Size(58, 21);
			labelLayers.TabIndex = 3;
			labelLayers.Text = "Layers";
			// 
			// labelSettingsAppearance
			// 
			labelSettingsAppearance.AutoEllipsis = true;
			labelSettingsAppearance.AutoSize = true;
			labelSettingsAppearance.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
			labelSettingsAppearance.Location = new Point(3, 85);
			labelSettingsAppearance.Name = "labelSettingsAppearance";
			labelSettingsAppearance.Size = new Size(177, 21);
			labelSettingsAppearance.TabIndex = 4;
			labelSettingsAppearance.Text = "Settings - Appearance";
			// 
			// labelSettingsGeometry
			// 
			labelSettingsGeometry.AutoEllipsis = true;
			labelSettingsGeometry.AutoSize = true;
			labelSettingsGeometry.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
			labelSettingsGeometry.Location = new Point(3, 261);
			labelSettingsGeometry.Name = "labelSettingsGeometry";
			labelSettingsGeometry.Size = new Size(162, 21);
			labelSettingsGeometry.TabIndex = 10;
			labelSettingsGeometry.Text = "Settings - Geometry";
			// 
			// labelSettingsObjects
			// 
			labelSettingsObjects.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			labelSettingsObjects.AutoEllipsis = true;
			labelSettingsObjects.AutoSize = true;
			labelSettingsObjects.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
			labelSettingsObjects.Location = new Point(3, 344);
			labelSettingsObjects.Name = "labelSettingsObjects";
			labelSettingsObjects.Size = new Size(272, 21);
			labelSettingsObjects.TabIndex = 13;
			labelSettingsObjects.Text = "Settings - Objects";
			// 
			// labelSettingsLighting
			// 
			labelSettingsLighting.AutoEllipsis = true;
			labelSettingsLighting.AutoSize = true;
			labelSettingsLighting.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
			labelSettingsLighting.Location = new Point(3, 737);
			labelSettingsLighting.Name = "labelSettingsLighting";
			labelSettingsLighting.Size = new Size(150, 21);
			labelSettingsLighting.TabIndex = 26;
			labelSettingsLighting.Text = "Settings - Lighting";
			// 
			// labelSettingsMusic
			// 
			labelSettingsMusic.AutoEllipsis = true;
			labelSettingsMusic.AutoSize = true;
			labelSettingsMusic.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
			labelSettingsMusic.Location = new Point(3, 882);
			labelSettingsMusic.Name = "labelSettingsMusic";
			labelSettingsMusic.Size = new Size(131, 21);
			labelSettingsMusic.TabIndex = 31;
			labelSettingsMusic.Text = "Settings - Music";
			// 
			// labelSettingsCamera
			// 
			labelSettingsCamera.AutoEllipsis = true;
			labelSettingsCamera.AutoSize = true;
			labelSettingsCamera.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
			labelSettingsCamera.Location = new Point(3, 1089);
			labelSettingsCamera.Name = "labelSettingsCamera";
			labelSettingsCamera.Size = new Size(144, 21);
			labelSettingsCamera.TabIndex = 38;
			labelSettingsCamera.Text = "Settings - Camera";
			// 
			// labelLevel
			// 
			labelLevel.AutoEllipsis = true;
			labelLevel.AutoSize = true;
			labelLevel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
			labelLevel.Location = new Point(3, 2402);
			labelLevel.Name = "labelLevel";
			labelLevel.Size = new Size(50, 21);
			labelLevel.TabIndex = 83;
			labelLevel.Text = "Level";
			// 
			// labelGeometry
			// 
			labelGeometry.AutoEllipsis = true;
			labelGeometry.AutoSize = true;
			labelGeometry.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
			labelGeometry.Location = new Point(3, 2485);
			labelGeometry.Name = "labelGeometry";
			labelGeometry.Size = new Size(86, 21);
			labelGeometry.TabIndex = 86;
			labelGeometry.Text = "Geometry";
			// 
			// labelGeometryHelp
			// 
			labelGeometryHelp.AutoEllipsis = true;
			labelGeometryHelp.AutoSize = true;
			labelGeometryHelp.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
			labelGeometryHelp.Location = new Point(3, 2506);
			labelGeometryHelp.Name = "labelGeometryHelp";
			labelGeometryHelp.Size = new Size(268, 30);
			labelGeometryHelp.TabIndex = 87;
			labelGeometryHelp.Text = "Use the first button then mess with the others for examples.";
			// 
			// labelObjects
			// 
			labelObjects.AutoEllipsis = true;
			labelObjects.AutoSize = true;
			labelObjects.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
			labelObjects.Location = new Point(3, 2939);
			labelObjects.Name = "labelObjects";
			labelObjects.Size = new Size(67, 21);
			labelObjects.TabIndex = 101;
			labelObjects.Text = "Objects";
			// 
			// labelObjectsHelp
			// 
			labelObjectsHelp.AutoEllipsis = true;
			labelObjectsHelp.AutoSize = true;
			labelObjectsHelp.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
			labelObjectsHelp.Location = new Point(3, 2960);
			labelObjectsHelp.Name = "labelObjectsHelp";
			labelObjectsHelp.Size = new Size(268, 30);
			labelObjectsHelp.TabIndex = 102;
			labelObjectsHelp.Text = "Use the first button then mess with the others for examples.";
			// 
			// labelCamera
			// 
			labelCamera.AutoEllipsis = true;
			labelCamera.AutoSize = true;
			labelCamera.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
			labelCamera.Location = new Point(3, 3083);
			labelCamera.Name = "labelCamera";
			labelCamera.Size = new Size(68, 21);
			labelCamera.TabIndex = 106;
			labelCamera.Text = "Camera";
			// 
			// labelSettingsHud
			// 
			labelSettingsHud.AutoEllipsis = true;
			labelSettingsHud.AutoSize = true;
			labelSettingsHud.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
			labelSettingsHud.Location = new Point(3, 1637);
			labelSettingsHud.Name = "labelSettingsHud";
			labelSettingsHud.Size = new Size(122, 21);
			labelSettingsHud.TabIndex = 58;
			labelSettingsHud.Text = "Settings - HUD";
			// 
			// split
			// 
			this.split.Dock = DockStyle.Fill;
			this.split.FixedPanel = FixedPanel.Panel2;
			this.split.Location = new Point(0, 0);
			this.split.Name = "split";
			// 
			// split.Panel1
			// 
			this.split.Panel1.SizeChanged += this.split_Panel1_SizeChanged;
			// 
			// split.Panel2
			// 
			this.split.Panel2.Controls.Add(this.flow);
			this.split.Size = new Size(800, 428);
			this.split.SplitterDistance = 500;
			this.split.TabIndex = 1;
			// 
			// flow
			// 
			this.flow.AutoScroll = true;
			this.flow.Controls.Add(this.buttonQuit);
			this.flow.Controls.Add(labelLevels);
			this.flow.Controls.Add(this.levelFlow);
			this.flow.Controls.Add(labelLayers);
			this.flow.Controls.Add(this.layersFlow);
			this.flow.Controls.Add(labelSettingsAppearance);
			this.flow.Controls.Add(this.buttonSetBackground0);
			this.flow.Controls.Add(this.buttonSetBackground1);
			this.flow.Controls.Add(this.buttonSetBackground2);
			this.flow.Controls.Add(this.buttonSetShowWaitBitmap0);
			this.flow.Controls.Add(this.buttonSetShowWaitBitmap1);
			this.flow.Controls.Add(labelSettingsGeometry);
			this.flow.Controls.Add(this.buttonSetExtendSkyPit0);
			this.flow.Controls.Add(this.buttonSetExtendSkyPit1);
			this.flow.Controls.Add(labelSettingsObjects);
			this.flow.Controls.Add(this.buttonSetShowSprites0);
			this.flow.Controls.Add(this.buttonSetShowSprites1);
			this.flow.Controls.Add(this.buttonSetShow3dos0);
			this.flow.Controls.Add(this.buttonSetShow3dos1);
			this.flow.Controls.Add(this.buttonSetDifficulty0);
			this.flow.Controls.Add(this.buttonSetDifficulty1);
			this.flow.Controls.Add(this.buttonSetDifficulty2);
			this.flow.Controls.Add(this.buttonSetDifficulty3);
			this.flow.Controls.Add(this.buttonSetAnimateVues0);
			this.flow.Controls.Add(this.buttonSetAnimateVues1);
			this.flow.Controls.Add(this.buttonSetAnimate3doUpdates0);
			this.flow.Controls.Add(this.buttonSetAnimate3doUpdates1);
			this.flow.Controls.Add(labelSettingsLighting);
			this.flow.Controls.Add(this.buttonSetFullBrightLighting0);
			this.flow.Controls.Add(this.buttonSetFullBrightLighting1);
			this.flow.Controls.Add(this.buttonSetBypassColorDithering0);
			this.flow.Controls.Add(this.buttonSetBypassColorDithering1);
			this.flow.Controls.Add(labelSettingsMusic);
			this.flow.Controls.Add(this.buttonSetPlayMusic0);
			this.flow.Controls.Add(this.buttonSetPlayMusic1);
			this.flow.Controls.Add(this.buttonSetPlayFightTrack0);
			this.flow.Controls.Add(this.buttonSetPlayFightTrack1);
			this.flow.Controls.Add(this.buttonSetVolume0);
			this.flow.Controls.Add(this.buttonSetVolume1);
			this.flow.Controls.Add(labelSettingsCamera);
			this.flow.Controls.Add(this.buttonSetLookSensitivity0);
			this.flow.Controls.Add(this.buttonSetLookSensitivity1);
			this.flow.Controls.Add(this.buttonSetLookSensitivity2);
			this.flow.Controls.Add(this.buttonSetInvertYLook0);
			this.flow.Controls.Add(this.buttonSetInvertYLook1);
			this.flow.Controls.Add(this.buttonSetMoveSensitivity0);
			this.flow.Controls.Add(this.buttonSetMoveSensitivity1);
			this.flow.Controls.Add(this.buttonSetMoveSensitivity2);
			this.flow.Controls.Add(this.buttonSetYawLimits0);
			this.flow.Controls.Add(this.buttonSetYawLimits1);
			this.flow.Controls.Add(this.buttonSetRunMultiplier0);
			this.flow.Controls.Add(this.buttonSetRunMultiplier1);
			this.flow.Controls.Add(this.buttonSetZoomSensitivity0);
			this.flow.Controls.Add(this.buttonSetZoomSensitivity1);
			this.flow.Controls.Add(this.buttonSetOrbitCamera);
			this.flow.Controls.Add(this.buttonSetUseMouseCapture0);
			this.flow.Controls.Add(this.buttonSetUseMouseCapture1);
			this.flow.Controls.Add(labelSettingsHud);
			this.flow.Controls.Add(this.buttonSetShowHud0);
			this.flow.Controls.Add(this.buttonSetShowHud1);
			this.flow.Controls.Add(this.buttonSetHudAlign0);
			this.flow.Controls.Add(this.buttonSetHudAlign1);
			this.flow.Controls.Add(this.buttonSetHudFontSize0);
			this.flow.Controls.Add(this.buttonSetHudFontSize1);
			this.flow.Controls.Add(this.buttonSetHudColor0);
			this.flow.Controls.Add(this.buttonSetHudColor1);
			this.flow.Controls.Add(this.buttonSetShowHudCoordinates0);
			this.flow.Controls.Add(this.buttonSetShowHudCoordinates1);
			this.flow.Controls.Add(this.buttonSetHudFpsCoordinates0);
			this.flow.Controls.Add(this.buttonSetHudFpsCoordinates1);
			this.flow.Controls.Add(this.buttonSetHudOrbitCoordinates0);
			this.flow.Controls.Add(this.buttonSetHudOrbitCoordinates1);
			this.flow.Controls.Add(this.buttonSetShowHudRaycastHit0);
			this.flow.Controls.Add(this.buttonSetShowHudRaycastHit1);
			this.flow.Controls.Add(this.buttonSetHudRaycastFloor0);
			this.flow.Controls.Add(this.buttonSetHudRaycastFloor1);
			this.flow.Controls.Add(this.buttonSetHudRaycastCeiling0);
			this.flow.Controls.Add(this.buttonSetHudRaycastCeiling1);
			this.flow.Controls.Add(this.buttonSetHudRaycastWall0);
			this.flow.Controls.Add(this.buttonSetHudRaycastWall1);
			this.flow.Controls.Add(this.buttonSetHudRaycastObject0);
			this.flow.Controls.Add(this.buttonSetHudRaycastObject1);
			this.flow.Controls.Add(labelLevel);
			this.flow.Controls.Add(this.buttonReloadLevelInPlace);
			this.flow.Controls.Add(this.buttonInitEmptyLevel);
			this.flow.Controls.Add(labelGeometry);
			this.flow.Controls.Add(labelGeometryHelp);
			this.flow.Controls.Add(this.buttonReloadLevelGeometry);
			this.flow.Controls.Add(this.buttonSetLevelMetadata0);
			this.flow.Controls.Add(this.buttonSetLevelMetadata1);
			this.flow.Controls.Add(this.buttonReloadSector);
			this.flow.Controls.Add(this.buttonSetSector);
			this.flow.Controls.Add(this.buttonMoveSector);
			this.flow.Controls.Add(this.buttonDeleteSector);
			this.flow.Controls.Add(this.buttonSetSectorFloor);
			this.flow.Controls.Add(this.buttonSetSectorCeiling);
			this.flow.Controls.Add(this.buttonReloadWall);
			this.flow.Controls.Add(this.buttonInsertWall);
			this.flow.Controls.Add(this.buttonDeleteWall);
			this.flow.Controls.Add(this.buttonSetVertex);
			this.flow.Controls.Add(labelObjects);
			this.flow.Controls.Add(labelObjectsHelp);
			this.flow.Controls.Add(this.buttonReloadLevelObjects);
			this.flow.Controls.Add(this.buttonSetObject);
			this.flow.Controls.Add(this.buttonDeleteObject);
			this.flow.Controls.Add(labelCamera);
			this.flow.Controls.Add(this.buttonResetCamera);
			this.flow.Controls.Add(this.buttonMoveCamera);
			this.flow.Controls.Add(this.buttonRotateCamera);
			this.flow.Controls.Add(this.buttonMoveAndRotateCamera);
			this.flow.Controls.Add(this.buttonPointCameraAt);
			this.flow.Controls.Add(this.buttonCaptureMouse);
			this.flow.Dock = DockStyle.Fill;
			this.flow.FlowDirection = FlowDirection.TopDown;
			this.flow.Location = new Point(0, 0);
			this.flow.Name = "flow";
			this.flow.Size = new Size(296, 428);
			this.flow.TabIndex = 0;
			this.flow.WrapContents = false;
			// 
			// buttonQuit
			// 
			this.buttonQuit.AutoSize = true;
			this.buttonQuit.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonQuit.Location = new Point(3, 3);
			this.buttonQuit.Name = "buttonQuit";
			this.buttonQuit.Size = new Size(40, 25);
			this.buttonQuit.TabIndex = 0;
			this.buttonQuit.Text = "Quit";
			this.buttonQuit.UseVisualStyleBackColor = true;
			this.buttonQuit.Click += this.buttonQuit_Click;
			// 
			// levelFlow
			// 
			this.levelFlow.AutoSize = true;
			this.levelFlow.FlowDirection = FlowDirection.TopDown;
			this.levelFlow.Location = new Point(3, 55);
			this.levelFlow.Name = "levelFlow";
			this.levelFlow.Size = new Size(0, 0);
			this.levelFlow.TabIndex = 2;
			this.levelFlow.WrapContents = false;
			// 
			// layersFlow
			// 
			this.layersFlow.AutoSize = true;
			this.layersFlow.FlowDirection = FlowDirection.TopDown;
			this.layersFlow.Location = new Point(3, 82);
			this.layersFlow.Name = "layersFlow";
			this.layersFlow.Size = new Size(0, 0);
			this.layersFlow.TabIndex = 3;
			this.layersFlow.WrapContents = false;
			// 
			// buttonSetBackground0
			// 
			this.buttonSetBackground0.AutoSize = true;
			this.buttonSetBackground0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetBackground0.Location = new Point(3, 109);
			this.buttonSetBackground0.Name = "buttonSetBackground0";
			this.buttonSetBackground0.Size = new Size(193, 25);
			this.buttonSetBackground0.TabIndex = 5;
			this.buttonSetBackground0.Text = "Set background to black (default)";
			this.buttonSetBackground0.UseVisualStyleBackColor = true;
			this.buttonSetBackground0.Click += this.buttonSetBackground0_Click;
			// 
			// buttonSetBackground1
			// 
			this.buttonSetBackground1.AutoSize = true;
			this.buttonSetBackground1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetBackground1.Location = new Point(3, 140);
			this.buttonSetBackground1.Name = "buttonSetBackground1";
			this.buttonSetBackground1.Size = new Size(157, 25);
			this.buttonSetBackground1.TabIndex = 6;
			this.buttonSetBackground1.Text = "Set background to Control";
			this.buttonSetBackground1.UseVisualStyleBackColor = true;
			this.buttonSetBackground1.Click += this.buttonSetBackground1_Click;
			// 
			// buttonSetBackground2
			// 
			this.buttonSetBackground2.AutoSize = true;
			this.buttonSetBackground2.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetBackground2.Location = new Point(3, 171);
			this.buttonSetBackground2.Name = "buttonSetBackground2";
			this.buttonSetBackground2.Size = new Size(189, 25);
			this.buttonSetBackground2.TabIndex = 7;
			this.buttonSetBackground2.Text = "Set background to random color";
			this.buttonSetBackground2.UseVisualStyleBackColor = true;
			this.buttonSetBackground2.Click += this.buttonSetBackground2_Click;
			// 
			// buttonSetShowWaitBitmap0
			// 
			this.buttonSetShowWaitBitmap0.AutoSize = true;
			this.buttonSetShowWaitBitmap0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetShowWaitBitmap0.Location = new Point(3, 202);
			this.buttonSetShowWaitBitmap0.Name = "buttonSetShowWaitBitmap0";
			this.buttonSetShowWaitBitmap0.Size = new Size(258, 25);
			this.buttonSetShowWaitBitmap0.TabIndex = 8;
			this.buttonSetShowWaitBitmap0.Text = "Don't show WAIT.BM during loading (default)";
			this.buttonSetShowWaitBitmap0.UseVisualStyleBackColor = true;
			this.buttonSetShowWaitBitmap0.Click += this.buttonSetShowWaitBitmap0_Click;
			// 
			// buttonSetShowWaitBitmap1
			// 
			this.buttonSetShowWaitBitmap1.AutoSize = true;
			this.buttonSetShowWaitBitmap1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetShowWaitBitmap1.Location = new Point(3, 233);
			this.buttonSetShowWaitBitmap1.Name = "buttonSetShowWaitBitmap1";
			this.buttonSetShowWaitBitmap1.Size = new Size(179, 25);
			this.buttonSetShowWaitBitmap1.TabIndex = 9;
			this.buttonSetShowWaitBitmap1.Text = "Show WAIT.BM during loading";
			this.buttonSetShowWaitBitmap1.UseVisualStyleBackColor = true;
			this.buttonSetShowWaitBitmap1.Click += this.buttonSetShowWaitBitmap1_Click;
			// 
			// buttonSetExtendSkyPit0
			// 
			this.buttonSetExtendSkyPit0.AutoSize = true;
			this.buttonSetExtendSkyPit0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetExtendSkyPit0.Location = new Point(3, 285);
			this.buttonSetExtendSkyPit0.Name = "buttonSetExtendSkyPit0";
			this.buttonSetExtendSkyPit0.Size = new Size(189, 25);
			this.buttonSetExtendSkyPit0.TabIndex = 11;
			this.buttonSetExtendSkyPit0.Text = "Don't extrude skies/pits (default)";
			this.buttonSetExtendSkyPit0.UseVisualStyleBackColor = true;
			this.buttonSetExtendSkyPit0.Click += this.buttonSetExtendSkyPit0_Click;
			// 
			// buttonSetExtendSkyPit1
			// 
			this.buttonSetExtendSkyPit1.AutoSize = true;
			this.buttonSetExtendSkyPit1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetExtendSkyPit1.Location = new Point(3, 316);
			this.buttonSetExtendSkyPit1.Name = "buttonSetExtendSkyPit1";
			this.buttonSetExtendSkyPit1.Size = new Size(156, 25);
			this.buttonSetExtendSkyPit1.TabIndex = 12;
			this.buttonSetExtendSkyPit1.Text = "Extride skies/pits 100 DFUs";
			this.buttonSetExtendSkyPit1.UseVisualStyleBackColor = true;
			this.buttonSetExtendSkyPit1.Click += this.buttonSetExtendSkyPit1_Click;
			// 
			// buttonSetShowSprites0
			// 
			this.buttonSetShowSprites0.AutoSize = true;
			this.buttonSetShowSprites0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetShowSprites0.Location = new Point(3, 368);
			this.buttonSetShowSprites0.Name = "buttonSetShowSprites0";
			this.buttonSetShowSprites0.Size = new Size(79, 25);
			this.buttonSetShowSprites0.TabIndex = 14;
			this.buttonSetShowSprites0.Text = "Hide sprites";
			this.buttonSetShowSprites0.UseVisualStyleBackColor = true;
			this.buttonSetShowSprites0.Click += this.buttonSetShowSprites0_Click;
			// 
			// buttonSetShowSprites1
			// 
			this.buttonSetShowSprites1.AutoSize = true;
			this.buttonSetShowSprites1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetShowSprites1.Location = new Point(3, 399);
			this.buttonSetShowSprites1.Name = "buttonSetShowSprites1";
			this.buttonSetShowSprites1.Size = new Size(131, 25);
			this.buttonSetShowSprites1.TabIndex = 15;
			this.buttonSetShowSprites1.Text = "Show sprites (default)";
			this.buttonSetShowSprites1.UseVisualStyleBackColor = true;
			this.buttonSetShowSprites1.Click += this.buttonSetShowSprites1_Click;
			// 
			// buttonSetShow3dos0
			// 
			this.buttonSetShow3dos0.AutoSize = true;
			this.buttonSetShow3dos0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetShow3dos0.Location = new Point(3, 430);
			this.buttonSetShow3dos0.Name = "buttonSetShow3dos0";
			this.buttonSetShow3dos0.Size = new Size(73, 25);
			this.buttonSetShow3dos0.TabIndex = 16;
			this.buttonSetShow3dos0.Text = "Hide 3DOs";
			this.buttonSetShow3dos0.UseVisualStyleBackColor = true;
			this.buttonSetShow3dos0.Click += this.buttonSetShow3dos0_Click;
			// 
			// buttonSetShow3dos1
			// 
			this.buttonSetShow3dos1.AutoSize = true;
			this.buttonSetShow3dos1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetShow3dos1.Location = new Point(3, 461);
			this.buttonSetShow3dos1.Name = "buttonSetShow3dos1";
			this.buttonSetShow3dos1.Size = new Size(125, 25);
			this.buttonSetShow3dos1.TabIndex = 17;
			this.buttonSetShow3dos1.Text = "Show 3DOs (default)";
			this.buttonSetShow3dos1.UseVisualStyleBackColor = true;
			this.buttonSetShow3dos1.Click += this.buttonSetShow3dos1_Click;
			// 
			// buttonSetDifficulty0
			// 
			this.buttonSetDifficulty0.AutoSize = true;
			this.buttonSetDifficulty0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetDifficulty0.Location = new Point(3, 492);
			this.buttonSetDifficulty0.Name = "buttonSetDifficulty0";
			this.buttonSetDifficulty0.Size = new Size(94, 25);
			this.buttonSetDifficulty0.TabIndex = 18;
			this.buttonSetDifficulty0.Text = "Difficulty: Easy";
			this.buttonSetDifficulty0.UseVisualStyleBackColor = true;
			this.buttonSetDifficulty0.Click += this.buttonSetDifficulty0_Click;
			// 
			// buttonSetDifficulty1
			// 
			this.buttonSetDifficulty1.AutoSize = true;
			this.buttonSetDifficulty1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetDifficulty1.Location = new Point(3, 523);
			this.buttonSetDifficulty1.Name = "buttonSetDifficulty1";
			this.buttonSetDifficulty1.Size = new Size(116, 25);
			this.buttonSetDifficulty1.TabIndex = 19;
			this.buttonSetDifficulty1.Text = "Difficulty: Medium";
			this.buttonSetDifficulty1.UseVisualStyleBackColor = true;
			this.buttonSetDifficulty1.Click += this.buttonSetDifficulty1_Click;
			// 
			// buttonSetDifficulty2
			// 
			this.buttonSetDifficulty2.AutoSize = true;
			this.buttonSetDifficulty2.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetDifficulty2.Location = new Point(3, 554);
			this.buttonSetDifficulty2.Name = "buttonSetDifficulty2";
			this.buttonSetDifficulty2.Size = new Size(97, 25);
			this.buttonSetDifficulty2.TabIndex = 20;
			this.buttonSetDifficulty2.Text = "Difficulty: Hard";
			this.buttonSetDifficulty2.UseVisualStyleBackColor = true;
			this.buttonSetDifficulty2.Click += this.buttonSetDifficulty2_Click;
			// 
			// buttonSetDifficulty3
			// 
			this.buttonSetDifficulty3.AutoSize = true;
			this.buttonSetDifficulty3.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetDifficulty3.Location = new Point(3, 585);
			this.buttonSetDifficulty3.Name = "buttonSetDifficulty3";
			this.buttonSetDifficulty3.Size = new Size(270, 25);
			this.buttonSetDifficulty3.TabIndex = 21;
			this.buttonSetDifficulty3.Text = "Show all objects regardless of difficulty (default)";
			this.buttonSetDifficulty3.UseVisualStyleBackColor = true;
			this.buttonSetDifficulty3.Click += this.buttonSetDifficulty3_Click;
			// 
			// buttonSetAnimateVues0
			// 
			this.buttonSetAnimateVues0.AutoSize = true;
			this.buttonSetAnimateVues0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetAnimateVues0.Location = new Point(3, 616);
			this.buttonSetAnimateVues0.Name = "buttonSetAnimateVues0";
			this.buttonSetAnimateVues0.Size = new Size(172, 25);
			this.buttonSetAnimateVues0.TabIndex = 22;
			this.buttonSetAnimateVues0.Text = "Don't autoplay VUEs (default)";
			this.buttonSetAnimateVues0.UseVisualStyleBackColor = true;
			this.buttonSetAnimateVues0.Click += this.buttonSetAnimateVues0_Click;
			// 
			// buttonSetAnimateVues1
			// 
			this.buttonSetAnimateVues1.AutoSize = true;
			this.buttonSetAnimateVues1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetAnimateVues1.Location = new Point(3, 647);
			this.buttonSetAnimateVues1.Name = "buttonSetAnimateVues1";
			this.buttonSetAnimateVues1.Size = new Size(94, 25);
			this.buttonSetAnimateVues1.TabIndex = 23;
			this.buttonSetAnimateVues1.Text = "Autoplay VUEs";
			this.buttonSetAnimateVues1.UseVisualStyleBackColor = true;
			this.buttonSetAnimateVues1.Click += this.buttonSetAnimateVues1_Click;
			// 
			// buttonSetAnimate3doUpdates0
			// 
			this.buttonSetAnimate3doUpdates0.AutoSize = true;
			this.buttonSetAnimate3doUpdates0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetAnimate3doUpdates0.Location = new Point(3, 678);
			this.buttonSetAnimate3doUpdates0.Name = "buttonSetAnimate3doUpdates0";
			this.buttonSetAnimate3doUpdates0.Size = new Size(223, 25);
			this.buttonSetAnimate3doUpdates0.TabIndex = 24;
			this.buttonSetAnimate3doUpdates0.Text = "Don't animate 3DOs with UPDATE logic";
			this.buttonSetAnimate3doUpdates0.UseVisualStyleBackColor = true;
			this.buttonSetAnimate3doUpdates0.Click += this.buttonSetAnimate3doUpdates0_Click;
			// 
			// buttonSetAnimate3doUpdates1
			// 
			this.buttonSetAnimate3doUpdates1.AutoSize = true;
			this.buttonSetAnimate3doUpdates1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetAnimate3doUpdates1.Location = new Point(3, 709);
			this.buttonSetAnimate3doUpdates1.Name = "buttonSetAnimate3doUpdates1";
			this.buttonSetAnimate3doUpdates1.Size = new Size(241, 25);
			this.buttonSetAnimate3doUpdates1.TabIndex = 25;
			this.buttonSetAnimate3doUpdates1.Text = "Animate 3DOs with UPDATE logic (default)";
			this.buttonSetAnimate3doUpdates1.UseVisualStyleBackColor = true;
			this.buttonSetAnimate3doUpdates1.Click += this.buttonSetAnimate3doUpdates1_Click;
			// 
			// buttonSetFullBrightLighting0
			// 
			this.buttonSetFullBrightLighting0.AutoSize = true;
			this.buttonSetFullBrightLighting0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetFullBrightLighting0.Location = new Point(3, 761);
			this.buttonSetFullBrightLighting0.Name = "buttonSetFullBrightLighting0";
			this.buttonSetFullBrightLighting0.Size = new Size(153, 25);
			this.buttonSetFullBrightLighting0.TabIndex = 27;
			this.buttonSetFullBrightLighting0.Text = "Show light levels (default)";
			this.buttonSetFullBrightLighting0.UseVisualStyleBackColor = true;
			this.buttonSetFullBrightLighting0.Click += this.buttonSetFullBrightLighting0_Click;
			// 
			// buttonSetFullBrightLighting1
			// 
			this.buttonSetFullBrightLighting1.AutoSize = true;
			this.buttonSetFullBrightLighting1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetFullBrightLighting1.Location = new Point(3, 792);
			this.buttonSetFullBrightLighting1.Name = "buttonSetFullBrightLighting1";
			this.buttonSetFullBrightLighting1.Size = new Size(105, 25);
			this.buttonSetFullBrightLighting1.TabIndex = 28;
			this.buttonSetFullBrightLighting1.Text = "Full bright mode";
			this.buttonSetFullBrightLighting1.UseVisualStyleBackColor = true;
			this.buttonSetFullBrightLighting1.Click += this.buttonSetFullBrightLighting1_Click;
			// 
			// buttonSetBypassColorDithering0
			// 
			this.buttonSetBypassColorDithering0.AutoSize = true;
			this.buttonSetBypassColorDithering0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetBypassColorDithering0.Location = new Point(3, 823);
			this.buttonSetBypassColorDithering0.Name = "buttonSetBypassColorDithering0";
			this.buttonSetBypassColorDithering0.Size = new Size(147, 25);
			this.buttonSetBypassColorDithering0.TabIndex = 29;
			this.buttonSetBypassColorDithering0.Text = "8-bit in-game color map";
			this.buttonSetBypassColorDithering0.UseVisualStyleBackColor = true;
			this.buttonSetBypassColorDithering0.Click += this.buttonSetBypassColorDithering0_Click;
			// 
			// buttonSetBypassColorDithering1
			// 
			this.buttonSetBypassColorDithering1.AutoSize = true;
			this.buttonSetBypassColorDithering1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetBypassColorDithering1.Location = new Point(3, 854);
			this.buttonSetBypassColorDithering1.Name = "buttonSetBypassColorDithering1";
			this.buttonSetBypassColorDithering1.Size = new Size(238, 25);
			this.buttonSetBypassColorDithering1.TabIndex = 30;
			this.buttonSetBypassColorDithering1.Text = "24-bit auto-generated color map (default)";
			this.buttonSetBypassColorDithering1.UseVisualStyleBackColor = true;
			this.buttonSetBypassColorDithering1.Click += this.buttonSetBypassColorDithering1_Click;
			// 
			// buttonSetPlayMusic0
			// 
			this.buttonSetPlayMusic0.AutoSize = true;
			this.buttonSetPlayMusic0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetPlayMusic0.Location = new Point(3, 906);
			this.buttonSetPlayMusic0.Name = "buttonSetPlayMusic0";
			this.buttonSetPlayMusic0.Size = new Size(115, 25);
			this.buttonSetPlayMusic0.TabIndex = 32;
			this.buttonSetPlayMusic0.Text = "Music off (default)";
			this.buttonSetPlayMusic0.UseVisualStyleBackColor = true;
			this.buttonSetPlayMusic0.Click += this.buttonSetPlayMusic0_Click;
			// 
			// buttonSetPlayMusic1
			// 
			this.buttonSetPlayMusic1.AutoSize = true;
			this.buttonSetPlayMusic1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetPlayMusic1.Location = new Point(3, 937);
			this.buttonSetPlayMusic1.Name = "buttonSetPlayMusic1";
			this.buttonSetPlayMusic1.Size = new Size(66, 25);
			this.buttonSetPlayMusic1.TabIndex = 33;
			this.buttonSetPlayMusic1.Text = "Music on";
			this.buttonSetPlayMusic1.UseVisualStyleBackColor = true;
			this.buttonSetPlayMusic1.Click += this.buttonSetPlayMusic1_Click;
			// 
			// buttonSetPlayFightTrack0
			// 
			this.buttonSetPlayFightTrack0.AutoSize = true;
			this.buttonSetPlayFightTrack0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetPlayFightTrack0.Location = new Point(3, 968);
			this.buttonSetPlayFightTrack0.Name = "buttonSetPlayFightTrack0";
			this.buttonSetPlayFightTrack0.Size = new Size(119, 25);
			this.buttonSetPlayFightTrack0.TabIndex = 34;
			this.buttonSetPlayFightTrack0.Text = "Stalk track (default)";
			this.buttonSetPlayFightTrack0.UseVisualStyleBackColor = true;
			this.buttonSetPlayFightTrack0.Click += this.buttonSetPlayFightTrack0_Click;
			// 
			// buttonSetPlayFightTrack1
			// 
			this.buttonSetPlayFightTrack1.AutoSize = true;
			this.buttonSetPlayFightTrack1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetPlayFightTrack1.Location = new Point(3, 999);
			this.buttonSetPlayFightTrack1.Name = "buttonSetPlayFightTrack1";
			this.buttonSetPlayFightTrack1.Size = new Size(73, 25);
			this.buttonSetPlayFightTrack1.TabIndex = 35;
			this.buttonSetPlayFightTrack1.Text = "Fight track";
			this.buttonSetPlayFightTrack1.UseVisualStyleBackColor = true;
			this.buttonSetPlayFightTrack1.Click += this.buttonSetPlayFightTrack1_Click;
			// 
			// buttonSetVolume0
			// 
			this.buttonSetVolume0.AutoSize = true;
			this.buttonSetVolume0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetVolume0.Location = new Point(3, 1030);
			this.buttonSetVolume0.Name = "buttonSetVolume0";
			this.buttonSetVolume0.Size = new Size(82, 25);
			this.buttonSetVolume0.TabIndex = 36;
			this.buttonSetVolume0.Text = "Half volume";
			this.buttonSetVolume0.UseVisualStyleBackColor = true;
			this.buttonSetVolume0.Click += this.buttonSetVolume0_Click;
			// 
			// buttonSetVolume1
			// 
			this.buttonSetVolume1.AutoSize = true;
			this.buttonSetVolume1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetVolume1.Location = new Point(3, 1061);
			this.buttonSetVolume1.Name = "buttonSetVolume1";
			this.buttonSetVolume1.Size = new Size(131, 25);
			this.buttonSetVolume1.TabIndex = 37;
			this.buttonSetVolume1.Text = "Max volume (default)";
			this.buttonSetVolume1.UseVisualStyleBackColor = true;
			this.buttonSetVolume1.Click += this.buttonSetVolume1_Click;
			// 
			// buttonSetLookSensitivity0
			// 
			this.buttonSetLookSensitivity0.AutoSize = true;
			this.buttonSetLookSensitivity0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetLookSensitivity0.Location = new Point(3, 1113);
			this.buttonSetLookSensitivity0.Name = "buttonSetLookSensitivity0";
			this.buttonSetLookSensitivity0.Size = new Size(210, 25);
			this.buttonSetLookSensitivity0.TabIndex = 39;
			this.buttonSetLookSensitivity0.Text = "Reset look/rotate sensitivity (default)";
			this.buttonSetLookSensitivity0.UseVisualStyleBackColor = true;
			this.buttonSetLookSensitivity0.Click += this.buttonSetLookSensitivity0_Click;
			// 
			// buttonSetLookSensitivity1
			// 
			this.buttonSetLookSensitivity1.AutoSize = true;
			this.buttonSetLookSensitivity1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetLookSensitivity1.Location = new Point(3, 1144);
			this.buttonSetLookSensitivity1.Name = "buttonSetLookSensitivity1";
			this.buttonSetLookSensitivity1.Size = new Size(156, 25);
			this.buttonSetLookSensitivity1.TabIndex = 40;
			this.buttonSetLookSensitivity1.Text = "Half look/rotate sensitivity";
			this.buttonSetLookSensitivity1.UseVisualStyleBackColor = true;
			this.buttonSetLookSensitivity1.Click += this.buttonSetLookSensitivity1_Click;
			// 
			// buttonSetLookSensitivity2
			// 
			this.buttonSetLookSensitivity2.AutoSize = true;
			this.buttonSetLookSensitivity2.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetLookSensitivity2.Location = new Point(3, 1175);
			this.buttonSetLookSensitivity2.Name = "buttonSetLookSensitivity2";
			this.buttonSetLookSensitivity2.Size = new Size(172, 25);
			this.buttonSetLookSensitivity2.TabIndex = 43;
			this.buttonSetLookSensitivity2.Text = "Double look/rotate sensitivity";
			this.buttonSetLookSensitivity2.UseVisualStyleBackColor = true;
			this.buttonSetLookSensitivity2.Click += this.buttonSetLookSensitivity2_Click;
			// 
			// buttonSetInvertYLook0
			// 
			this.buttonSetInvertYLook0.AutoSize = true;
			this.buttonSetInvertYLook0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetInvertYLook0.Location = new Point(3, 1206);
			this.buttonSetInvertYLook0.Name = "buttonSetInvertYLook0";
			this.buttonSetInvertYLook0.Size = new Size(149, 25);
			this.buttonSetInvertYLook0.TabIndex = 44;
			this.buttonSetInvertYLook0.Text = "Uninvert Y Look (default)";
			this.buttonSetInvertYLook0.UseVisualStyleBackColor = true;
			this.buttonSetInvertYLook0.Click += this.buttonSetInvertYLook0_Click;
			// 
			// buttonSetInvertYLook1
			// 
			this.buttonSetInvertYLook1.AutoSize = true;
			this.buttonSetInvertYLook1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetInvertYLook1.Location = new Point(3, 1237);
			this.buttonSetInvertYLook1.Name = "buttonSetInvertYLook1";
			this.buttonSetInvertYLook1.Size = new Size(86, 25);
			this.buttonSetInvertYLook1.TabIndex = 45;
			this.buttonSetInvertYLook1.Text = "Invert Y Look";
			this.buttonSetInvertYLook1.UseVisualStyleBackColor = true;
			this.buttonSetInvertYLook1.Click += this.buttonSetInvertYLook1_Click;
			// 
			// buttonSetMoveSensitivity0
			// 
			this.buttonSetMoveSensitivity0.AutoSize = true;
			this.buttonSetMoveSensitivity0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetMoveSensitivity0.Location = new Point(3, 1268);
			this.buttonSetMoveSensitivity0.Name = "buttonSetMoveSensitivity0";
			this.buttonSetMoveSensitivity0.Size = new Size(160, 25);
			this.buttonSetMoveSensitivity0.TabIndex = 46;
			this.buttonSetMoveSensitivity0.Text = "Reset move speed (default)";
			this.buttonSetMoveSensitivity0.UseVisualStyleBackColor = true;
			this.buttonSetMoveSensitivity0.Click += this.buttonSetMoveSensitivity0_Click;
			// 
			// buttonSetMoveSensitivity1
			// 
			this.buttonSetMoveSensitivity1.AutoSize = true;
			this.buttonSetMoveSensitivity1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetMoveSensitivity1.Location = new Point(3, 1299);
			this.buttonSetMoveSensitivity1.Name = "buttonSetMoveSensitivity1";
			this.buttonSetMoveSensitivity1.Size = new Size(106, 25);
			this.buttonSetMoveSensitivity1.TabIndex = 47;
			this.buttonSetMoveSensitivity1.Text = "Half move speed";
			this.buttonSetMoveSensitivity1.UseVisualStyleBackColor = true;
			this.buttonSetMoveSensitivity1.Click += this.buttonSetMoveSensitivity1_Click;
			// 
			// buttonSetMoveSensitivity2
			// 
			this.buttonSetMoveSensitivity2.AutoSize = true;
			this.buttonSetMoveSensitivity2.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetMoveSensitivity2.Location = new Point(3, 1330);
			this.buttonSetMoveSensitivity2.Name = "buttonSetMoveSensitivity2";
			this.buttonSetMoveSensitivity2.Size = new Size(122, 25);
			this.buttonSetMoveSensitivity2.TabIndex = 48;
			this.buttonSetMoveSensitivity2.Text = "Double move speed";
			this.buttonSetMoveSensitivity2.UseVisualStyleBackColor = true;
			this.buttonSetMoveSensitivity2.Click += this.buttonSetMoveSensitivity2_Click;
			// 
			// buttonSetYawLimits0
			// 
			this.buttonSetYawLimits0.AutoSize = true;
			this.buttonSetYawLimits0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetYawLimits0.Location = new Point(3, 1361);
			this.buttonSetYawLimits0.Name = "buttonSetYawLimits0";
			this.buttonSetYawLimits0.Size = new Size(160, 25);
			this.buttonSetYawLimits0.TabIndex = 49;
			this.buttonSetYawLimits0.Text = "Minimal yaw limit (default)";
			this.buttonSetYawLimits0.UseVisualStyleBackColor = true;
			this.buttonSetYawLimits0.Click += this.buttonSetYawLimits0_Click;
			// 
			// buttonSetYawLimits1
			// 
			this.buttonSetYawLimits1.AutoSize = true;
			this.buttonSetYawLimits1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetYawLimits1.Location = new Point(3, 1392);
			this.buttonSetYawLimits1.Name = "buttonSetYawLimits1";
			this.buttonSetYawLimits1.Size = new Size(119, 25);
			this.buttonSetYawLimits1.TabIndex = 50;
			this.buttonSetYawLimits1.Text = "60 degree yaw limit";
			this.buttonSetYawLimits1.UseVisualStyleBackColor = true;
			this.buttonSetYawLimits1.Click += this.buttonSetYawLimits1_Click;
			// 
			// buttonSetRunMultiplier0
			// 
			this.buttonSetRunMultiplier0.AutoSize = true;
			this.buttonSetRunMultiplier0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetRunMultiplier0.Location = new Point(3, 1423);
			this.buttonSetRunMultiplier0.Name = "buttonSetRunMultiplier0";
			this.buttonSetRunMultiplier0.Size = new Size(272, 25);
			this.buttonSetRunMultiplier0.TabIndex = 51;
			this.buttonSetRunMultiplier0.Text = "Double move speed when holding shift (default)";
			this.buttonSetRunMultiplier0.UseVisualStyleBackColor = true;
			this.buttonSetRunMultiplier0.Click += this.buttonSetRunMultiplier0_Click;
			// 
			// buttonSetRunMultiplier1
			// 
			this.buttonSetRunMultiplier1.AutoSize = true;
			this.buttonSetRunMultiplier1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetRunMultiplier1.Location = new Point(3, 1454);
			this.buttonSetRunMultiplier1.Name = "buttonSetRunMultiplier1";
			this.buttonSetRunMultiplier1.Size = new Size(242, 25);
			this.buttonSetRunMultiplier1.TabIndex = 52;
			this.buttonSetRunMultiplier1.Text = "Quadruple move speed when holding shift";
			this.buttonSetRunMultiplier1.UseVisualStyleBackColor = true;
			this.buttonSetRunMultiplier1.Click += this.buttonSetRunMultiplier1_Click;
			// 
			// buttonSetZoomSensitivity0
			// 
			this.buttonSetZoomSensitivity0.AutoSize = true;
			this.buttonSetZoomSensitivity0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetZoomSensitivity0.Location = new Point(3, 1485);
			this.buttonSetZoomSensitivity0.Name = "buttonSetZoomSensitivity0";
			this.buttonSetZoomSensitivity0.Size = new Size(181, 25);
			this.buttonSetZoomSensitivity0.TabIndex = 53;
			this.buttonSetZoomSensitivity0.Text = "Reset zoom sensitivity (default)";
			this.buttonSetZoomSensitivity0.UseVisualStyleBackColor = true;
			this.buttonSetZoomSensitivity0.Click += this.buttonSetZoomSensitivity0_Click;
			// 
			// buttonSetZoomSensitivity1
			// 
			this.buttonSetZoomSensitivity1.AutoSize = true;
			this.buttonSetZoomSensitivity1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetZoomSensitivity1.Location = new Point(3, 1516);
			this.buttonSetZoomSensitivity1.Name = "buttonSetZoomSensitivity1";
			this.buttonSetZoomSensitivity1.Size = new Size(172, 25);
			this.buttonSetZoomSensitivity1.TabIndex = 54;
			this.buttonSetZoomSensitivity1.Text = "Fine-grained zoom sensitivity";
			this.buttonSetZoomSensitivity1.UseVisualStyleBackColor = true;
			this.buttonSetZoomSensitivity1.Click += this.buttonSetZoomSensitivity1_Click;
			// 
			// buttonSetOrbitCamera
			// 
			this.buttonSetOrbitCamera.AutoSize = true;
			this.buttonSetOrbitCamera.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetOrbitCamera.Location = new Point(3, 1547);
			this.buttonSetOrbitCamera.Name = "buttonSetOrbitCamera";
			this.buttonSetOrbitCamera.Size = new Size(158, 25);
			this.buttonSetOrbitCamera.TabIndex = 55;
			this.buttonSetOrbitCamera.Text = "Use orbit controls (default)";
			this.buttonSetOrbitCamera.UseVisualStyleBackColor = true;
			this.buttonSetOrbitCamera.Click += this.buttonSetOrbitCamera_Click;
			// 
			// buttonSetUseMouseCapture0
			// 
			this.buttonSetUseMouseCapture0.AutoSize = true;
			this.buttonSetUseMouseCapture0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetUseMouseCapture0.Location = new Point(3, 1578);
			this.buttonSetUseMouseCapture0.Name = "buttonSetUseMouseCapture0";
			this.buttonSetUseMouseCapture0.Size = new Size(230, 25);
			this.buttonSetUseMouseCapture0.TabIndex = 56;
			this.buttonSetUseMouseCapture0.Text = "Use FPS controls without mouse capture";
			this.buttonSetUseMouseCapture0.UseVisualStyleBackColor = true;
			this.buttonSetUseMouseCapture0.Click += this.buttonSetUseMouseCapture0_Click;
			// 
			// buttonSetUseMouseCapture1
			// 
			this.buttonSetUseMouseCapture1.AutoSize = true;
			this.buttonSetUseMouseCapture1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetUseMouseCapture1.Location = new Point(3, 1609);
			this.buttonSetUseMouseCapture1.Name = "buttonSetUseMouseCapture1";
			this.buttonSetUseMouseCapture1.Size = new Size(212, 25);
			this.buttonSetUseMouseCapture1.TabIndex = 57;
			this.buttonSetUseMouseCapture1.Text = "Use FPS controls with mouse capture";
			this.buttonSetUseMouseCapture1.UseVisualStyleBackColor = true;
			this.buttonSetUseMouseCapture1.Click += this.buttonSetUseMouseCapture1_Click;
			// 
			// buttonSetShowHud0
			// 
			this.buttonSetShowHud0.AutoSize = true;
			this.buttonSetShowHud0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetShowHud0.Location = new Point(3, 1661);
			this.buttonSetShowHud0.Name = "buttonSetShowHud0";
			this.buttonSetShowHud0.Size = new Size(70, 25);
			this.buttonSetShowHud0.TabIndex = 59;
			this.buttonSetShowHud0.Text = "Hide HUD";
			this.buttonSetShowHud0.UseVisualStyleBackColor = true;
			this.buttonSetShowHud0.Click += this.buttonSetShowHud0_Click;
			// 
			// buttonSetShowHud1
			// 
			this.buttonSetShowHud1.AutoSize = true;
			this.buttonSetShowHud1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetShowHud1.Location = new Point(3, 1692);
			this.buttonSetShowHud1.Name = "buttonSetShowHud1";
			this.buttonSetShowHud1.Size = new Size(122, 25);
			this.buttonSetShowHud1.TabIndex = 60;
			this.buttonSetShowHud1.Text = "Show HUD (default)";
			this.buttonSetShowHud1.UseVisualStyleBackColor = true;
			this.buttonSetShowHud1.Click += this.buttonSetShowHud1_Click;
			// 
			// buttonSetHudAlign0
			// 
			this.buttonSetHudAlign0.AutoSize = true;
			this.buttonSetHudAlign0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetHudAlign0.Location = new Point(3, 1723);
			this.buttonSetHudAlign0.Name = "buttonSetHudAlign0";
			this.buttonSetHudAlign0.Size = new Size(162, 25);
			this.buttonSetHudAlign0.TabIndex = 61;
			this.buttonSetHudAlign0.Text = "Align HUD top left (default)";
			this.buttonSetHudAlign0.UseVisualStyleBackColor = true;
			this.buttonSetHudAlign0.Click += this.buttonSetHudAlign0_Click;
			// 
			// buttonSetHudAlign1
			// 
			this.buttonSetHudAlign1.AutoSize = true;
			this.buttonSetHudAlign1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetHudAlign1.Location = new Point(3, 1754);
			this.buttonSetHudAlign1.Name = "buttonSetHudAlign1";
			this.buttonSetHudAlign1.Size = new Size(122, 25);
			this.buttonSetHudAlign1.TabIndex = 62;
			this.buttonSetHudAlign1.Text = "Align HUD top right";
			this.buttonSetHudAlign1.UseVisualStyleBackColor = true;
			this.buttonSetHudAlign1.Click += this.buttonSetHudAlign1_Click;
			// 
			// buttonSetHudFontSize0
			// 
			this.buttonSetHudFontSize0.AutoSize = true;
			this.buttonSetHudFontSize0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetHudFontSize0.Location = new Point(3, 1785);
			this.buttonSetHudFontSize0.Name = "buttonSetHudFontSize0";
			this.buttonSetHudFontSize0.Size = new Size(126, 25);
			this.buttonSetHudFontSize0.TabIndex = 63;
			this.buttonSetHudFontSize0.Text = "Font size 36 (default)";
			this.buttonSetHudFontSize0.UseVisualStyleBackColor = true;
			this.buttonSetHudFontSize0.Click += this.buttonSetHudFontSize0_Click;
			// 
			// buttonSetHudFontSize1
			// 
			this.buttonSetHudFontSize1.AutoSize = true;
			this.buttonSetHudFontSize1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetHudFontSize1.Location = new Point(3, 1816);
			this.buttonSetHudFontSize1.Name = "buttonSetHudFontSize1";
			this.buttonSetHudFontSize1.Size = new Size(78, 25);
			this.buttonSetHudFontSize1.TabIndex = 64;
			this.buttonSetHudFontSize1.Text = "Font size 18";
			this.buttonSetHudFontSize1.UseVisualStyleBackColor = true;
			this.buttonSetHudFontSize1.Click += this.buttonSetHudFontSize1_Click;
			// 
			// buttonSetHudColor0
			// 
			this.buttonSetHudColor0.AutoSize = true;
			this.buttonSetHudColor0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetHudColor0.Location = new Point(3, 1847);
			this.buttonSetHudColor0.Name = "buttonSetHudColor0";
			this.buttonSetHudColor0.Size = new Size(114, 25);
			this.buttonSetHudColor0.TabIndex = 65;
			this.buttonSetHudColor0.Text = "Color red (default)";
			this.buttonSetHudColor0.UseVisualStyleBackColor = true;
			this.buttonSetHudColor0.Click += this.buttonSetHudColor0_Click;
			// 
			// buttonSetHudColor1
			// 
			this.buttonSetHudColor1.AutoSize = true;
			this.buttonSetHudColor1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetHudColor1.Location = new Point(3, 1878);
			this.buttonSetHudColor1.Name = "buttonSetHudColor1";
			this.buttonSetHudColor1.Size = new Size(141, 25);
			this.buttonSetHudColor1.TabIndex = 66;
			this.buttonSetHudColor1.Text = "Color transparent white";
			this.buttonSetHudColor1.UseVisualStyleBackColor = true;
			this.buttonSetHudColor1.Click += this.buttonSetHudColor1_Click;
			// 
			// buttonSetShowHudCoordinates0
			// 
			this.buttonSetShowHudCoordinates0.AutoSize = true;
			this.buttonSetShowHudCoordinates0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetShowHudCoordinates0.Location = new Point(3, 1909);
			this.buttonSetShowHudCoordinates0.Name = "buttonSetShowHudCoordinates0";
			this.buttonSetShowHudCoordinates0.Size = new Size(111, 25);
			this.buttonSetShowHudCoordinates0.TabIndex = 67;
			this.buttonSetShowHudCoordinates0.Text = "Hide camera stats";
			this.buttonSetShowHudCoordinates0.UseVisualStyleBackColor = true;
			this.buttonSetShowHudCoordinates0.Click += this.buttonSetShowHudCoordinates0_Click;
			// 
			// buttonSetShowHudCoordinates1
			// 
			this.buttonSetShowHudCoordinates1.AutoSize = true;
			this.buttonSetShowHudCoordinates1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetShowHudCoordinates1.Location = new Point(3, 1940);
			this.buttonSetShowHudCoordinates1.Name = "buttonSetShowHudCoordinates1";
			this.buttonSetShowHudCoordinates1.Size = new Size(163, 25);
			this.buttonSetShowHudCoordinates1.TabIndex = 68;
			this.buttonSetShowHudCoordinates1.Text = "Show camera stats (default)";
			this.buttonSetShowHudCoordinates1.UseVisualStyleBackColor = true;
			this.buttonSetShowHudCoordinates1.Click += this.buttonSetShowHudCoordinates1_Click;
			// 
			// buttonSetHudFpsCoordinates0
			// 
			this.buttonSetHudFpsCoordinates0.AutoSize = true;
			this.buttonSetHudFpsCoordinates0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetHudFpsCoordinates0.Location = new Point(3, 1971);
			this.buttonSetHudFpsCoordinates0.Name = "buttonSetHudFpsCoordinates0";
			this.buttonSetHudFpsCoordinates0.Size = new Size(185, 25);
			this.buttonSetHudFpsCoordinates0.TabIndex = 69;
			this.buttonSetHudFpsCoordinates0.Text = "Default FPS camera stats format";
			this.buttonSetHudFpsCoordinates0.UseVisualStyleBackColor = true;
			this.buttonSetHudFpsCoordinates0.Click += this.buttonSetHudFpsCoordinates0_Click;
			// 
			// buttonSetHudFpsCoordinates1
			// 
			this.buttonSetHudFpsCoordinates1.AutoSize = true;
			this.buttonSetHudFpsCoordinates1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetHudFpsCoordinates1.Location = new Point(3, 2002);
			this.buttonSetHudFpsCoordinates1.Name = "buttonSetHudFpsCoordinates1";
			this.buttonSetHudFpsCoordinates1.Size = new Size(185, 25);
			this.buttonSetHudFpsCoordinates1.TabIndex = 70;
			this.buttonSetHudFpsCoordinates1.Text = "Altered FPS camera stats format";
			this.buttonSetHudFpsCoordinates1.UseVisualStyleBackColor = true;
			this.buttonSetHudFpsCoordinates1.Click += this.buttonSetHudFpsCoordinates1_Click;
			// 
			// buttonSetHudOrbitCoordinates0
			// 
			this.buttonSetHudOrbitCoordinates0.AutoSize = true;
			this.buttonSetHudOrbitCoordinates0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetHudOrbitCoordinates0.Location = new Point(3, 2033);
			this.buttonSetHudOrbitCoordinates0.Name = "buttonSetHudOrbitCoordinates0";
			this.buttonSetHudOrbitCoordinates0.Size = new Size(191, 25);
			this.buttonSetHudOrbitCoordinates0.TabIndex = 71;
			this.buttonSetHudOrbitCoordinates0.Text = "Default orbit camera stats format";
			this.buttonSetHudOrbitCoordinates0.UseVisualStyleBackColor = true;
			this.buttonSetHudOrbitCoordinates0.Click += this.buttonSetHudOrbitCoordinates0_Click;
			// 
			// buttonSetHudOrbitCoordinates1
			// 
			this.buttonSetHudOrbitCoordinates1.AutoSize = true;
			this.buttonSetHudOrbitCoordinates1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetHudOrbitCoordinates1.Location = new Point(3, 2064);
			this.buttonSetHudOrbitCoordinates1.Name = "buttonSetHudOrbitCoordinates1";
			this.buttonSetHudOrbitCoordinates1.Size = new Size(191, 25);
			this.buttonSetHudOrbitCoordinates1.TabIndex = 72;
			this.buttonSetHudOrbitCoordinates1.Text = "Altered orbit camera stats format";
			this.buttonSetHudOrbitCoordinates1.UseVisualStyleBackColor = true;
			this.buttonSetHudOrbitCoordinates1.Click += this.buttonSetHudOrbitCoordinates1_Click;
			// 
			// buttonSetShowHudRaycastHit0
			// 
			this.buttonSetShowHudRaycastHit0.AutoSize = true;
			this.buttonSetShowHudRaycastHit0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetShowHudRaycastHit0.Location = new Point(3, 2095);
			this.buttonSetShowHudRaycastHit0.Name = "buttonSetShowHudRaycastHit0";
			this.buttonSetShowHudRaycastHit0.Size = new Size(107, 25);
			this.buttonSetShowHudRaycastHit0.TabIndex = 73;
			this.buttonSetShowHudRaycastHit0.Text = "Hide pointer info";
			this.buttonSetShowHudRaycastHit0.UseVisualStyleBackColor = true;
			this.buttonSetShowHudRaycastHit0.Click += this.buttonSetShowHudRaycastHit0_Click;
			// 
			// buttonSetShowHudRaycastHit1
			// 
			this.buttonSetShowHudRaycastHit1.AutoSize = true;
			this.buttonSetShowHudRaycastHit1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetShowHudRaycastHit1.Location = new Point(3, 2126);
			this.buttonSetShowHudRaycastHit1.Name = "buttonSetShowHudRaycastHit1";
			this.buttonSetShowHudRaycastHit1.Size = new Size(159, 25);
			this.buttonSetShowHudRaycastHit1.TabIndex = 74;
			this.buttonSetShowHudRaycastHit1.Text = "Show pointer info (default)";
			this.buttonSetShowHudRaycastHit1.UseVisualStyleBackColor = true;
			this.buttonSetShowHudRaycastHit1.Click += this.buttonSetShowHudRaycastHit1_Click;
			// 
			// buttonSetHudRaycastFloor0
			// 
			this.buttonSetHudRaycastFloor0.AutoSize = true;
			this.buttonSetHudRaycastFloor0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetHudRaycastFloor0.Location = new Point(3, 2157);
			this.buttonSetHudRaycastFloor0.Name = "buttonSetHudRaycastFloor0";
			this.buttonSetHudRaycastFloor0.Size = new Size(146, 25);
			this.buttonSetHudRaycastFloor0.TabIndex = 75;
			this.buttonSetHudRaycastFloor0.Text = "Default floor info format";
			this.buttonSetHudRaycastFloor0.UseVisualStyleBackColor = true;
			this.buttonSetHudRaycastFloor0.Click += this.buttonSetHudRaycastFloor0_Click;
			// 
			// buttonSetHudRaycastFloor1
			// 
			this.buttonSetHudRaycastFloor1.AutoSize = true;
			this.buttonSetHudRaycastFloor1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetHudRaycastFloor1.Location = new Point(3, 2188);
			this.buttonSetHudRaycastFloor1.Name = "buttonSetHudRaycastFloor1";
			this.buttonSetHudRaycastFloor1.Size = new Size(146, 25);
			this.buttonSetHudRaycastFloor1.TabIndex = 76;
			this.buttonSetHudRaycastFloor1.Text = "Altered floor info format";
			this.buttonSetHudRaycastFloor1.UseVisualStyleBackColor = true;
			this.buttonSetHudRaycastFloor1.Click += this.buttonSetHudRaycastFloor1_Click;
			// 
			// buttonSetHudRaycastCeiling0
			// 
			this.buttonSetHudRaycastCeiling0.AutoSize = true;
			this.buttonSetHudRaycastCeiling0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetHudRaycastCeiling0.Location = new Point(3, 2219);
			this.buttonSetHudRaycastCeiling0.Name = "buttonSetHudRaycastCeiling0";
			this.buttonSetHudRaycastCeiling0.Size = new Size(156, 25);
			this.buttonSetHudRaycastCeiling0.TabIndex = 77;
			this.buttonSetHudRaycastCeiling0.Text = "Default ceiling info format";
			this.buttonSetHudRaycastCeiling0.UseVisualStyleBackColor = true;
			this.buttonSetHudRaycastCeiling0.Click += this.buttonSetHudRaycastCeiling0_Click;
			// 
			// buttonSetHudRaycastCeiling1
			// 
			this.buttonSetHudRaycastCeiling1.AutoSize = true;
			this.buttonSetHudRaycastCeiling1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetHudRaycastCeiling1.Location = new Point(3, 2250);
			this.buttonSetHudRaycastCeiling1.Name = "buttonSetHudRaycastCeiling1";
			this.buttonSetHudRaycastCeiling1.Size = new Size(156, 25);
			this.buttonSetHudRaycastCeiling1.TabIndex = 78;
			this.buttonSetHudRaycastCeiling1.Text = "Altered ceiling info format";
			this.buttonSetHudRaycastCeiling1.UseVisualStyleBackColor = true;
			this.buttonSetHudRaycastCeiling1.Click += this.buttonSetHudRaycastCeiling1_Click;
			// 
			// buttonSetHudRaycastWall0
			// 
			this.buttonSetHudRaycastWall0.AutoSize = true;
			this.buttonSetHudRaycastWall0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetHudRaycastWall0.Location = new Point(3, 2281);
			this.buttonSetHudRaycastWall0.Name = "buttonSetHudRaycastWall0";
			this.buttonSetHudRaycastWall0.Size = new Size(142, 25);
			this.buttonSetHudRaycastWall0.TabIndex = 79;
			this.buttonSetHudRaycastWall0.Text = "Default wall info format";
			this.buttonSetHudRaycastWall0.UseVisualStyleBackColor = true;
			this.buttonSetHudRaycastWall0.Click += this.buttonSetHudRaycastWall0_Click;
			// 
			// buttonSetHudRaycastWall1
			// 
			this.buttonSetHudRaycastWall1.AutoSize = true;
			this.buttonSetHudRaycastWall1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetHudRaycastWall1.Location = new Point(3, 2312);
			this.buttonSetHudRaycastWall1.Name = "buttonSetHudRaycastWall1";
			this.buttonSetHudRaycastWall1.Size = new Size(142, 25);
			this.buttonSetHudRaycastWall1.TabIndex = 80;
			this.buttonSetHudRaycastWall1.Text = "Altered wall info format";
			this.buttonSetHudRaycastWall1.UseVisualStyleBackColor = true;
			this.buttonSetHudRaycastWall1.Click += this.buttonSetHudRaycastWall1_Click;
			// 
			// buttonSetHudRaycastObject0
			// 
			this.buttonSetHudRaycastObject0.AutoSize = true;
			this.buttonSetHudRaycastObject0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetHudRaycastObject0.Location = new Point(3, 2343);
			this.buttonSetHudRaycastObject0.Name = "buttonSetHudRaycastObject0";
			this.buttonSetHudRaycastObject0.Size = new Size(154, 25);
			this.buttonSetHudRaycastObject0.TabIndex = 81;
			this.buttonSetHudRaycastObject0.Text = "Default object info format";
			this.buttonSetHudRaycastObject0.UseVisualStyleBackColor = true;
			this.buttonSetHudRaycastObject0.Click += this.buttonSetHudRaycastObject0_Click;
			// 
			// buttonSetHudRaycastObject1
			// 
			this.buttonSetHudRaycastObject1.AutoSize = true;
			this.buttonSetHudRaycastObject1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetHudRaycastObject1.Location = new Point(3, 2374);
			this.buttonSetHudRaycastObject1.Name = "buttonSetHudRaycastObject1";
			this.buttonSetHudRaycastObject1.Size = new Size(154, 25);
			this.buttonSetHudRaycastObject1.TabIndex = 82;
			this.buttonSetHudRaycastObject1.Text = "Altered object info format";
			this.buttonSetHudRaycastObject1.UseVisualStyleBackColor = true;
			this.buttonSetHudRaycastObject1.Click += this.buttonSetHudRaycastObject1_Click;
			// 
			// buttonReloadLevelInPlace
			// 
			this.buttonReloadLevelInPlace.AutoSize = true;
			this.buttonReloadLevelInPlace.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonReloadLevelInPlace.Location = new Point(3, 2426);
			this.buttonReloadLevelInPlace.Name = "buttonReloadLevelInPlace";
			this.buttonReloadLevelInPlace.Size = new Size(80, 25);
			this.buttonReloadLevelInPlace.TabIndex = 84;
			this.buttonReloadLevelInPlace.Text = "Reload level";
			this.buttonReloadLevelInPlace.UseVisualStyleBackColor = true;
			this.buttonReloadLevelInPlace.Click += this.buttonReloadLevelInPlace_Click;
			// 
			// buttonInitEmptyLevel
			// 
			this.buttonInitEmptyLevel.AutoSize = true;
			this.buttonInitEmptyLevel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonInitEmptyLevel.Location = new Point(3, 2457);
			this.buttonInitEmptyLevel.Name = "buttonInitEmptyLevel";
			this.buttonInitEmptyLevel.Size = new Size(98, 25);
			this.buttonInitEmptyLevel.TabIndex = 85;
			this.buttonInitEmptyLevel.Text = "Init empty level";
			this.buttonInitEmptyLevel.UseVisualStyleBackColor = true;
			this.buttonInitEmptyLevel.Click += this.buttonInitEmptyLevel_Click;
			// 
			// buttonReloadLevelGeometry
			// 
			this.buttonReloadLevelGeometry.AutoSize = true;
			this.buttonReloadLevelGeometry.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonReloadLevelGeometry.Location = new Point(3, 2539);
			this.buttonReloadLevelGeometry.Name = "buttonReloadLevelGeometry";
			this.buttonReloadLevelGeometry.Size = new Size(127, 25);
			this.buttonReloadLevelGeometry.TabIndex = 88;
			this.buttonReloadLevelGeometry.Text = "Inject level geometry";
			this.buttonReloadLevelGeometry.UseVisualStyleBackColor = true;
			this.buttonReloadLevelGeometry.Click += this.buttonReloadLevelGeometry_Click;
			// 
			// buttonSetLevelMetadata0
			// 
			this.buttonSetLevelMetadata0.AutoSize = true;
			this.buttonSetLevelMetadata0.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetLevelMetadata0.Location = new Point(3, 2570);
			this.buttonSetLevelMetadata0.Name = "buttonSetLevelMetadata0";
			this.buttonSetLevelMetadata0.Size = new Size(227, 25);
			this.buttonSetLevelMetadata0.TabIndex = 89;
			this.buttonSetLevelMetadata0.Text = "Force SECBASE.PAL and normal parallax";
			this.buttonSetLevelMetadata0.UseVisualStyleBackColor = true;
			this.buttonSetLevelMetadata0.Click += this.buttonSetLevelMetadata0_Click;
			// 
			// buttonSetLevelMetadata1
			// 
			this.buttonSetLevelMetadata1.AutoSize = true;
			this.buttonSetLevelMetadata1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetLevelMetadata1.Location = new Point(3, 2601);
			this.buttonSetLevelMetadata1.Name = "buttonSetLevelMetadata1";
			this.buttonSetLevelMetadata1.Size = new Size(260, 25);
			this.buttonSetLevelMetadata1.TabIndex = 90;
			this.buttonSetLevelMetadata1.Text = "Force SECBASE.PAL and double parallax speed";
			this.buttonSetLevelMetadata1.UseVisualStyleBackColor = true;
			this.buttonSetLevelMetadata1.Click += this.buttonSetLevelMetadata1_Click;
			// 
			// buttonReloadSector
			// 
			this.buttonReloadSector.AutoSize = true;
			this.buttonReloadSector.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonReloadSector.Location = new Point(3, 2632);
			this.buttonReloadSector.Name = "buttonReloadSector";
			this.buttonReloadSector.Size = new Size(102, 25);
			this.buttonReloadSector.TabIndex = 91;
			this.buttonReloadSector.Text = "Replace sector 0";
			this.buttonReloadSector.UseVisualStyleBackColor = true;
			this.buttonReloadSector.Click += this.buttonReloadSector_Click;
			// 
			// buttonSetSector
			// 
			this.buttonSetSector.AutoSize = true;
			this.buttonSetSector.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetSector.Location = new Point(3, 2663);
			this.buttonSetSector.Name = "buttonSetSector";
			this.buttonSetSector.Size = new Size(174, 25);
			this.buttonSetSector.TabIndex = 92;
			this.buttonSetSector.Text = "Randomize sector 0 light level";
			this.buttonSetSector.UseVisualStyleBackColor = true;
			this.buttonSetSector.Click += this.buttonSetSector_Click;
			// 
			// buttonMoveSector
			// 
			this.buttonMoveSector.AutoSize = true;
			this.buttonMoveSector.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonMoveSector.Location = new Point(3, 2694);
			this.buttonMoveSector.Name = "buttonMoveSector";
			this.buttonMoveSector.Size = new Size(91, 25);
			this.buttonMoveSector.TabIndex = 93;
			this.buttonMoveSector.Text = "Move sector 0";
			this.buttonMoveSector.UseVisualStyleBackColor = true;
			this.buttonMoveSector.Click += this.buttonMoveSector_Click;
			// 
			// buttonDeleteSector
			// 
			this.buttonDeleteSector.AutoSize = true;
			this.buttonDeleteSector.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonDeleteSector.Location = new Point(3, 2725);
			this.buttonDeleteSector.Name = "buttonDeleteSector";
			this.buttonDeleteSector.Size = new Size(94, 25);
			this.buttonDeleteSector.TabIndex = 94;
			this.buttonDeleteSector.Text = "Delete sector 0";
			this.buttonDeleteSector.UseVisualStyleBackColor = true;
			this.buttonDeleteSector.Click += this.buttonDeleteSector_Click;
			// 
			// buttonSetSectorFloor
			// 
			this.buttonSetSectorFloor.AutoSize = true;
			this.buttonSetSectorFloor.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetSectorFloor.Location = new Point(3, 2756);
			this.buttonSetSectorFloor.Name = "buttonSetSectorFloor";
			this.buttonSetSectorFloor.Size = new Size(130, 25);
			this.buttonSetSectorFloor.TabIndex = 95;
			this.buttonSetSectorFloor.Text = "Replace sector 0 floor";
			this.buttonSetSectorFloor.UseVisualStyleBackColor = true;
			this.buttonSetSectorFloor.Click += this.buttonSetSectorFloor_Click;
			// 
			// buttonSetSectorCeiling
			// 
			this.buttonSetSectorCeiling.AutoSize = true;
			this.buttonSetSectorCeiling.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetSectorCeiling.Location = new Point(3, 2787);
			this.buttonSetSectorCeiling.Name = "buttonSetSectorCeiling";
			this.buttonSetSectorCeiling.Size = new Size(140, 25);
			this.buttonSetSectorCeiling.TabIndex = 96;
			this.buttonSetSectorCeiling.Text = "Replace sector 0 ceiling";
			this.buttonSetSectorCeiling.UseVisualStyleBackColor = true;
			this.buttonSetSectorCeiling.Click += this.buttonSetSectorCeiling_Click;
			// 
			// buttonReloadWall
			// 
			this.buttonReloadWall.AutoSize = true;
			this.buttonReloadWall.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonReloadWall.Location = new Point(3, 2818);
			this.buttonReloadWall.Name = "buttonReloadWall";
			this.buttonReloadWall.Size = new Size(135, 25);
			this.buttonReloadWall.TabIndex = 97;
			this.buttonReloadWall.Text = "Replace sector 0 wall 0";
			this.buttonReloadWall.UseVisualStyleBackColor = true;
			this.buttonReloadWall.Click += this.buttonReloadWall_Click;
			// 
			// buttonInsertWall
			// 
			this.buttonInsertWall.AutoSize = true;
			this.buttonInsertWall.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonInsertWall.Location = new Point(3, 2849);
			this.buttonInsertWall.Name = "buttonInsertWall";
			this.buttonInsertWall.Size = new Size(79, 25);
			this.buttonInsertWall.TabIndex = 98;
			this.buttonInsertWall.Text = "Insert wall 4";
			this.buttonInsertWall.UseVisualStyleBackColor = true;
			this.buttonInsertWall.Click += this.buttonInsertWall_Click;
			// 
			// buttonDeleteWall
			// 
			this.buttonDeleteWall.AutoSize = true;
			this.buttonDeleteWall.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonDeleteWall.Location = new Point(3, 2880);
			this.buttonDeleteWall.Name = "buttonDeleteWall";
			this.buttonDeleteWall.Size = new Size(83, 25);
			this.buttonDeleteWall.TabIndex = 99;
			this.buttonDeleteWall.Text = "Delete wall 4";
			this.buttonDeleteWall.UseVisualStyleBackColor = true;
			this.buttonDeleteWall.Click += this.buttonDeleteWall_Click;
			// 
			// buttonSetVertex
			// 
			this.buttonSetVertex.AutoSize = true;
			this.buttonSetVertex.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetVertex.Location = new Point(3, 2911);
			this.buttonSetVertex.Name = "buttonSetVertex";
			this.buttonSetVertex.Size = new Size(135, 25);
			this.buttonSetVertex.TabIndex = 100;
			this.buttonSetVertex.Text = "Move sector 0 vertex 0";
			this.buttonSetVertex.UseVisualStyleBackColor = true;
			this.buttonSetVertex.Click += this.buttonSetVertex_Click;
			// 
			// buttonReloadLevelObjects
			// 
			this.buttonReloadLevelObjects.AutoSize = true;
			this.buttonReloadLevelObjects.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonReloadLevelObjects.Location = new Point(3, 2993);
			this.buttonReloadLevelObjects.Name = "buttonReloadLevelObjects";
			this.buttonReloadLevelObjects.Size = new Size(87, 25);
			this.buttonReloadLevelObjects.TabIndex = 103;
			this.buttonReloadLevelObjects.Text = "Inject objects";
			this.buttonReloadLevelObjects.UseVisualStyleBackColor = true;
			this.buttonReloadLevelObjects.Click += this.buttonReloadLevelObjects_Click;
			// 
			// buttonSetObject
			// 
			this.buttonSetObject.AutoSize = true;
			this.buttonSetObject.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonSetObject.Location = new Point(3, 3024);
			this.buttonSetObject.Name = "buttonSetObject";
			this.buttonSetObject.Size = new Size(78, 25);
			this.buttonSetObject.TabIndex = 104;
			this.buttonSetObject.Text = "Set object 4";
			this.buttonSetObject.UseVisualStyleBackColor = true;
			this.buttonSetObject.Click += this.buttonSetObject_Click;
			// 
			// buttonDeleteObject
			// 
			this.buttonDeleteObject.AutoSize = true;
			this.buttonDeleteObject.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonDeleteObject.Location = new Point(3, 3055);
			this.buttonDeleteObject.Name = "buttonDeleteObject";
			this.buttonDeleteObject.Size = new Size(95, 25);
			this.buttonDeleteObject.TabIndex = 105;
			this.buttonDeleteObject.Text = "Delete object 4";
			this.buttonDeleteObject.UseVisualStyleBackColor = true;
			this.buttonDeleteObject.Click += this.buttonDeleteObject_Click;
			// 
			// buttonResetCamera
			// 
			this.buttonResetCamera.AutoSize = true;
			this.buttonResetCamera.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonResetCamera.Location = new Point(3, 3107);
			this.buttonResetCamera.Name = "buttonResetCamera";
			this.buttonResetCamera.Size = new Size(87, 25);
			this.buttonResetCamera.TabIndex = 107;
			this.buttonResetCamera.Text = "Reset camera";
			this.buttonResetCamera.UseVisualStyleBackColor = true;
			this.buttonResetCamera.Click += this.buttonResetCamera_Click;
			// 
			// buttonMoveCamera
			// 
			this.buttonMoveCamera.AutoSize = true;
			this.buttonMoveCamera.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonMoveCamera.Location = new Point(3, 3138);
			this.buttonMoveCamera.Name = "buttonMoveCamera";
			this.buttonMoveCamera.Size = new Size(137, 25);
			this.buttonMoveCamera.TabIndex = 108;
			this.buttonMoveCamera.Text = "Move camera to origin";
			this.buttonMoveCamera.UseVisualStyleBackColor = true;
			this.buttonMoveCamera.Click += this.buttonMoveCamera_Click;
			// 
			// buttonRotateCamera
			// 
			this.buttonRotateCamera.AutoSize = true;
			this.buttonRotateCamera.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonRotateCamera.Location = new Point(3, 3169);
			this.buttonRotateCamera.Name = "buttonRotateCamera";
			this.buttonRotateCamera.Size = new Size(182, 25);
			this.buttonRotateCamera.TabIndex = 109;
			this.buttonRotateCamera.Text = "Rotate camera to point forward";
			this.buttonRotateCamera.UseVisualStyleBackColor = true;
			this.buttonRotateCamera.Click += this.buttonRotateCamera_Click;
			// 
			// buttonMoveAndRotateCamera
			// 
			this.buttonMoveAndRotateCamera.AutoSize = true;
			this.buttonMoveAndRotateCamera.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonMoveAndRotateCamera.Location = new Point(3, 3200);
			this.buttonMoveAndRotateCamera.Name = "buttonMoveAndRotateCamera";
			this.buttonMoveAndRotateCamera.Size = new Size(194, 25);
			this.buttonMoveAndRotateCamera.TabIndex = 110;
			this.buttonMoveAndRotateCamera.Text = "Move and rotate camera to origin";
			this.buttonMoveAndRotateCamera.UseVisualStyleBackColor = true;
			this.buttonMoveAndRotateCamera.Click += this.buttonMoveAndRotateCamera_Click;
			// 
			// buttonPointCameraAt
			// 
			this.buttonPointCameraAt.AutoSize = true;
			this.buttonPointCameraAt.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonPointCameraAt.Location = new Point(3, 3231);
			this.buttonPointCameraAt.Name = "buttonPointCameraAt";
			this.buttonPointCameraAt.Size = new Size(134, 25);
			this.buttonPointCameraAt.TabIndex = 111;
			this.buttonPointCameraAt.Text = "Point camera at origin";
			this.buttonPointCameraAt.UseVisualStyleBackColor = true;
			this.buttonPointCameraAt.Click += this.buttonPointCameraAt_Click;
			// 
			// buttonCaptureMouse
			// 
			this.buttonCaptureMouse.AutoSize = true;
			this.buttonCaptureMouse.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.buttonCaptureMouse.Location = new Point(3, 3262);
			this.buttonCaptureMouse.Name = "buttonCaptureMouse";
			this.buttonCaptureMouse.Size = new Size(98, 25);
			this.buttonCaptureMouse.TabIndex = 112;
			this.buttonCaptureMouse.Text = "Capture mouse";
			this.buttonCaptureMouse.UseVisualStyleBackColor = true;
			this.buttonCaptureMouse.Click += this.buttonCaptureMouse_Click;
			// 
			// status
			// 
			this.status.Items.AddRange(new ToolStripItem[] { this.statusText });
			this.status.Location = new Point(0, 428);
			this.status.Name = "status";
			this.status.Size = new Size(800, 22);
			this.status.TabIndex = 0;
			this.status.Text = "statusStrip1";
			// 
			// statusText
			// 
			this.statusText.Name = "statusText";
			this.statusText.Size = new Size(0, 17);
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new SizeF(7F, 15F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.ClientSize = new Size(800, 450);
			this.Controls.Add(this.split);
			this.Controls.Add(this.status);
			this.Name = "MainForm";
			this.Text = "Level Preview Sample";
			this.Shown += this.MainForm_Shown;
			this.Move += this.MainForm_Move;
			this.split.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)this.split).EndInit();
			this.split.ResumeLayout(false);
			this.flow.ResumeLayout(false);
			this.flow.PerformLayout();
			this.status.ResumeLayout(false);
			this.status.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();
		}

		#endregion
		private SplitContainer split;
		private FlowLayoutPanel flow;
		private Button buttonQuit;
		private FlowLayoutPanel levelFlow;
		private FlowLayoutPanel layersFlow;
		private Button buttonSetBackground0;
		private Button buttonSetBackground1;
		private Button buttonSetBackground2;
		private Button buttonSetShowWaitBitmap0;
		private Button buttonSetShowWaitBitmap1;
		private Button buttonSetExtendSkyPit0;
		private Button buttonSetExtendSkyPit1;
		private Button buttonSetShowSprites0;
		private Button buttonSetShowSprites1;
		private Button buttonSetShow3dos0;
		private Button buttonSetShow3dos1;
		private Button buttonSetDifficulty0;
		private Button buttonSetDifficulty1;
		private Button buttonSetDifficulty2;
		private Button buttonSetDifficulty3;
		private Button buttonSetAnimateVues0;
		private Button buttonSetAnimateVues1;
		private Button buttonSetAnimate3doUpdates0;
		private Button buttonSetAnimate3doUpdates1;
		private Button buttonSetFullBrightLighting0;
		private Button buttonSetFullBrightLighting1;
		private Button buttonSetBypassColorDithering0;
		private Button buttonSetBypassColorDithering1;
		private Button buttonSetPlayMusic0;
		private Button buttonSetPlayMusic1;
		private Button buttonSetPlayFightTrack0;
		private Button buttonSetPlayFightTrack1;
		private Button buttonSetVolume0;
		private Button buttonSetVolume1;
		private Button buttonSetLookSensitivity0;
		private Button buttonSetLookSensitivity1;
		private Button buttonSetLookSensitivity2;
		private Button buttonSetInvertYLook0;
		private Button buttonSetInvertYLook1;
		private Button buttonSetMoveSensitivity0;
		private Button buttonSetMoveSensitivity1;
		private Button buttonSetMoveSensitivity2;
		private Button buttonSetYawLimits0;
		private Button buttonSetYawLimits1;
		private Button buttonSetRunMultiplier0;
		private Button buttonSetRunMultiplier1;
		private Button buttonSetZoomSensitivity0;
		private Button buttonSetZoomSensitivity1;
		private Button buttonSetOrbitCamera;
		private Button buttonSetUseMouseCapture0;
		private Button buttonSetUseMouseCapture1;
		private Button buttonReloadLevelInPlace;
		private Button buttonInitEmptyLevel;
		private Button buttonReloadLevelGeometry;
		private Button buttonSetLevelMetadata0;
		private Button buttonSetLevelMetadata1;
		private Button buttonReloadSector;
		private Button buttonSetSector;
		private Button buttonMoveSector;
		private Button buttonDeleteSector;
		private Button buttonSetSectorFloor;
		private Button buttonSetSectorCeiling;
		private Button buttonReloadWall;
		private Button buttonInsertWall;
		private Button buttonDeleteWall;
		private Button buttonSetVertex;
		private Button buttonReloadLevelObjects;
		private Button buttonSetObject;
		private Button buttonDeleteObject;
		private Button buttonResetCamera;
		private Button buttonMoveCamera;
		private Button buttonRotateCamera;
		private Button buttonMoveAndRotateCamera;
		private Button buttonPointCameraAt;
		private StatusStrip status;
		private ToolStripStatusLabel statusText;
		private Button buttonCaptureMouse;
		private Button buttonSetShowHud0;
		private Button buttonSetShowHud1;
		private Button buttonSetHudAlign0;
		private Button buttonSetHudAlign1;
		private Button buttonSetHudFontSize0;
		private Button buttonSetHudFontSize1;
		private Button buttonSetHudColor0;
		private Button buttonSetHudColor1;
		private Button buttonSetShowHudCoordinates0;
		private Button buttonSetShowHudCoordinates1;
		private Button buttonSetHudFpsCoordinates0;
		private Button buttonSetHudFpsCoordinates1;
		private Button buttonSetHudOrbitCoordinates0;
		private Button buttonSetHudOrbitCoordinates1;
		private Button buttonSetShowHudRaycastHit0;
		private Button buttonSetShowHudRaycastHit1;
		private Button buttonSetHudRaycastFloor0;
		private Button buttonSetHudRaycastFloor1;
		private Button buttonSetHudRaycastCeiling0;
		private Button buttonSetHudRaycastCeiling1;
		private Button buttonSetHudRaycastWall0;
		private Button buttonSetHudRaycastWall1;
		private Button buttonSetHudRaycastObject0;
		private Button buttonSetHudRaycastObject1;
	}
}