using MZZT.DarkForces.Showcase;
using System.Numerics;
using System.Runtime.InteropServices;

namespace MZZT.LevelPreviewDesktopSample;

public partial class MainForm : Form {
	[Flags]
	private enum SWP : uint {
		ASYNCWINDOWPOS = 0x4000,
		DEFERERASE = 0x2000,
		DRAWFRAME = 0x0020,
		FRAMECHANGED = 0x0020,
		HIDEWINDOW = 0x0080,
		NOACTIVATE = 0x0010,
		NOCOPYBITS = 0x0100,
		NOMOVE = 0x0002,
		NOOWNERZORDER = 0x0200,
		NOREDRAW = 0x0008,
		NOREPOSITION = 0x0200,
		NOSENDCHANGING = 0x0400,
		NOSIZE = 0x0001,
		NOZORDER = 0x0004,
		SHOWWINDOW = 0x0040
	}

	[DllImport("user32.dll", EntryPoint = "SetWindowPos",
		ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
	private extern static bool SetWindowPos(IntPtr hWnd, [Optional] IntPtr hWndInsertAfter,
		int X, int Y, int cx, int cy, SWP uFlags);

	public MainForm() {
		this.InitializeComponent();
	}

	public LevelPreview? LevelPreview { get; set; }

	/*private const int WM_INPUT = 0xFF;
	protected override void WndProc(ref Message m) {
		if (m.Msg == WM_INPUT) {
			_ = this.LevelPreview!.ProcessRawInputAsync(m.LParam);
		}

		base.WndProc(ref m);
	}*/

	private async void MainForm_Shown(object sender, EventArgs e) {
		this.LevelPreview!.CursorLockStateChanged += this.LevelPreview_CursorLockStateChanged;
		this.LevelPreview!.LevelListLoaded += this.LevelPreview_LevelListLoaded;
		this.LevelPreview!.LevelLoaded += this.LevelPreview_LevelLoaded;
		this.LevelPreview!.FloorClicked += this.LevelPreview_FloorClicked;
		this.LevelPreview!.CeilingClicked += this.LevelPreview_CeilingClicked;
		this.LevelPreview!.WallClicked += this.LevelPreview_WallClicked;
		this.LevelPreview!.ObjectClicked += this.LevelPreview_ObjectClicked;

		await this.LevelPreview!.StartAsync(this.split.Panel1.Handle);

		this.ResizeUnity();

		await this.LevelPreview!.ReloadDataFilesAsync();
		await this.LevelPreview!.LoadLevelListAsync();
		await this.LevelPreview!.LoadLevelAsync(0);
	}

	private void MainForm_Move(object sender, EventArgs e) {
		this.UpdateClip();
	}

	private int clipCursor = 0;
	private void LevelPreview_CursorLockStateChanged(object? sender, CursorLockStateEventArgs e) {
		this.clipCursor = e.State;
		this.UpdateClip();
	}

	private void UpdateClip() {
		switch (this.clipCursor) {
			case 0:
				Cursor.Clip = Rectangle.Empty;
				break;
			case 1:
				Cursor.Clip = new Rectangle(this.split.Panel1.PointToScreen(new Point(this.split.Panel1.Width / 2, this.split.Panel1.Height / 2)), new Size(1, 1));
				break;
			case 2:
				Cursor.Clip = this.split.Panel1.RectangleToScreen(new Rectangle(0, 0, this.split.Panel1.Width, this.split.Panel1.Height));
				break;
		}
	}

	private void LevelPreview_LevelListLoaded(object? sender, LevelListEventArgs e) {
		this.levelFlow.Controls.Clear();
		this.levelFlow.Controls.AddRange(e.Levels.Select((x, i) => {
			Button button = new() {
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Text = x.DisplayName
			};
			button.Click += this.Level_Click;
			return button;
		}).ToArray());
	}

	private async void Level_Click(object? sender, EventArgs e) {
		Button button = (Button)sender!;
		int index = button.Parent!.Controls.IndexOf(button);

		await this.LevelPreview!.LoadLevelAsync(index);
	}

	private int? currentLayer;
	private void LevelPreview_LevelLoaded(object? sender, LayersEventArgs e) {
		this.layersFlow.Controls.Clear();
		RadioButton radio = new() {
			AutoSize = true,
			Checked = this.currentLayer == null,
			Text = "All",
		};
		radio.CheckedChanged += this.Layers_CheckedChanged;
		this.layersFlow.Controls.Add(radio);
		this.layersFlow.Controls.AddRange(e.Layers.Select(x => {
			RadioButton radio = new() {
				AutoSize = true,
				Checked = this.currentLayer == x,
				Text = x.ToString()
			};
			radio.CheckedChanged += this.Layers_CheckedChanged;
			return radio;
		}).ToArray());
	}

	private async void Layers_CheckedChanged(object? sender, EventArgs e) {
		RadioButton radio = (RadioButton)sender!;
		if (!radio.Checked) {
			return;
		}

		if (radio.Text == "All") {
			this.currentLayer = null;
			await this.LevelPreview!.SetVisibleLayerAsync(null);
		} else {
			int layer = int.Parse(radio.Text);
			this.currentLayer = layer;
			await this.LevelPreview!.SetVisibleLayerAsync(layer);
		}
	}

	private void split_Panel1_SizeChanged(object sender, EventArgs e) {
		this.ResizeUnity();
		this.UpdateClip();
	}

	private void ResizeUnity() {
		if (this.LevelPreview == null || this.LevelPreview.HasExited || this.LevelPreview.UnityWindow == IntPtr.Zero) {
			return;
		}

		SetWindowPos(this.LevelPreview!.UnityWindow, IntPtr.Zero, 0, 0, this.split.Panel1.Width, this.split.Panel1.Height,
			SWP.DEFERERASE | SWP.NOACTIVATE | SWP.NOCOPYBITS | SWP.NOMOVE | SWP.NOOWNERZORDER | SWP.NOREDRAW | SWP.NOZORDER);
	}

	private async void buttonQuit_Click(object sender, EventArgs e) {
		await this.LevelPreview!.QuitAsync();

		this.clipCursor = 0;
		this.UpdateClip();
	}

	private async void buttonSetBackground0_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetBackgroundAsync(Color.Black);
	}

	private async void buttonSetBackground1_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetBackgroundAsync(SystemColors.Control);
	}

	private async void buttonSetBackground2_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetBackgroundAsync(Color.FromArgb(Random.Shared.Next(256), Random.Shared.Next(256), Random.Shared.Next(256)));
	}

	private async void buttonSetShowWaitBitmap0_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetShowWaitBitmapAsync(false);
	}

	private async void buttonSetShowWaitBitmap1_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetShowWaitBitmapAsync(true);
	}

	private async void buttonSetExtendSkyPit0_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetExtendSkyPitAsync(0);
	}

	private async void buttonSetExtendSkyPit1_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetExtendSkyPitAsync(100);
	}

	private async void buttonSetShowSprites0_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetShowSpritesAsync(false);
	}

	private async void buttonSetShowSprites1_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetShowSpritesAsync(true);
	}

	private async void buttonSetShow3dos0_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetShow3dosAsync(false);
	}

	private async void buttonSetShow3dos1_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetShow3dosAsync(true);
	}

	private async void buttonSetDifficulty0_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetDifficultyAsync(Difficulties.Easy);
	}

	private async void buttonSetDifficulty1_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetDifficultyAsync(Difficulties.Medium);
	}

	private async void buttonSetDifficulty2_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetDifficultyAsync(Difficulties.Hard);
	}

	private async void buttonSetDifficulty3_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetDifficultyAsync(Difficulties.All);
	}

	private async void buttonSetAnimateVues0_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetAnimateVuesAsync(false);
	}

	private async void buttonSetAnimateVues1_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetAnimateVuesAsync(true);
	}

	private async void buttonSetAnimate3doUpdates0_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetAnimate3doUpdatesAsync(false);
	}

	private async void buttonSetAnimate3doUpdates1_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetAnimate3doUpdatesAsync(true);
	}

	private async void buttonSetFullBrightLighting0_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetFullBrightLightingAsync(false);
	}

	private async void buttonSetFullBrightLighting1_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetFullBrightLightingAsync(true);
	}

	private async void buttonSetBypassColorDithering0_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetBypassColormapDitheringAsync(false);
	}

	private async void buttonSetBypassColorDithering1_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetBypassColormapDitheringAsync(true);
	}

	private async void buttonSetPlayMusic0_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetPlayMusicAsync(false);
	}

	private async void buttonSetPlayMusic1_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetPlayMusicAsync(true);
	}

	private async void buttonSetPlayFightTrack0_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetPlayFightTrackAsync(false);
	}

	private async void buttonSetPlayFightTrack1_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetPlayFightTrackAsync(true);
	}

	private async void buttonSetVolume0_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetVolumeAsync(0.5f);
	}

	private async void buttonSetVolume1_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetVolumeAsync(1);
	}

	private async void buttonSetLookSensitivity0_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetLookSensitivityAsync(new(0.25f));
	}

	private async void buttonSetLookSensitivity1_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetLookSensitivityAsync(new(0.125f));
	}

	private async void buttonSetLookSensitivity2_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetLookSensitivityAsync(new(0.5f));
	}

	private async void buttonSetInvertYLook0_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetInvertYLookAsync(false);
	}

	private async void buttonSetInvertYLook1_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetInvertYLookAsync(true);
	}

	private async void buttonSetMoveSensitivity0_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetMoveSensitivityAsync(new(1));
	}

	private async void buttonSetMoveSensitivity1_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetMoveSensitivityAsync(new(0.5f));
	}

	private async void buttonSetMoveSensitivity2_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetMoveSensitivityAsync(new(2));
	}

	private async void buttonSetYawLimits0_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetYawLimitsAsync(-89.999f, 89.999f);
	}

	private async void buttonSetYawLimits1_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetYawLimitsAsync(-30, 30);
	}

	private async void buttonSetRunMultiplier0_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetRunMultiplierAsync(2);
	}

	private async void buttonSetRunMultiplier1_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetRunMultiplierAsync(4);
	}

	private async void buttonSetZoomSensitivity0_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetZoomSensitivityAsync(-0.01f);
	}

	private async void buttonSetZoomSensitivity1_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetZoomSensitivityAsync(-0.001f);
	}

	private async void buttonSetOrbitCamera_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetUseOrbitCameraAsync(true);
	}

	private async void buttonSetUseMouseCapture0_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetUseMouseCaptureAsync(false);
		await this.LevelPreview!.SetUseOrbitCameraAsync(false);
	}

	private async void buttonSetUseMouseCapture1_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetUseMouseCaptureAsync(true);
		await this.LevelPreview!.SetUseOrbitCameraAsync(false);
	}

	private async void buttonReloadLevelInPlace_Click(object sender, EventArgs e) {
		await this.LevelPreview!.ReloadLevelInPlaceAsync();
	}

	private async void buttonInitEmptyLevel_Click(object sender, EventArgs e) {
		await this.LevelPreview!.InitEmptyLevelAsync("SECBASE", 0, "SECBASE");
	}

	private LevelInfo TestLevel = new() {
		LevelFile = "SECBASE",
		PaletteFile = "SECBASE.PAL",
		MusicFile = "STALK-01.GMD",
		ParallaxX = 1024,
		ParallaxY = 1024,
		Sectors = [new() {
			LightLevel = 31,
			Floor = {
				TextureFile = "DEFAULT.BM",
				TextureOffsetX = 0,
				TextureOffsetY = 0,
				TextureUnknown = 0,
				Y = 0
			},
			Ceiling = {
				TextureFile = "DEFAULT.BM",
				TextureOffsetX = 0,
				TextureOffsetY = 0,
				TextureUnknown=  0,
				Y = -64
			},
			AltY = 0,
			Flags = SectorFlags.CeilingIsSky,
			UnusedFlags2 = 0,
			AltLightLevel = 31,
			Layer = 0,
			Walls = [new() {
				LeftVertexX = -32,
				LeftVertexZ = 32,
				RightVertexX = 32,
				RightVertexZ = 32,
				MainTexture = {
					TextureFile = "DEFAULT.BM",
					TextureOffsetX = 0,
					TextureOffsetY = 0,
					TextureUnknown = 0
				},
				TopEdgeTexture = {
					TextureFile = null,
					TextureOffsetX = 0,
					TextureOffsetY = 0,
					TextureUnknown = 0
				},
				BottomEdgeTexture = {
					TextureFile = null,
					TextureOffsetX = 0,
					TextureOffsetY = 0,
					TextureUnknown = 0
				},
				SignTexture = {
					TextureFile = null,
					TextureOffsetX = 0,
					TextureOffsetY = 0,
					TextureUnknown = 0
				},
				AdjoinedSector = -1,
				AdjoinedWall = -1,
				TextureAndMapFlags = 0,
				UnusedFlags2 = 0,
				AdjoinFlags = 0,
				LightLevel = 0
			},
				new() {
					LeftVertexX = 32,
					LeftVertexZ = 32,
					RightVertexX = 32,
					RightVertexZ = -32,
					MainTexture = {
					TextureFile = "DEFAULT.BM",
					TextureOffsetX = 0,
					TextureOffsetY = 0,
					TextureUnknown = 0
				},
					TopEdgeTexture = {
					TextureFile = null,
					TextureOffsetX = 0,
					TextureOffsetY = 0,
					TextureUnknown = 0
				},
					BottomEdgeTexture = {
					TextureFile = null,
					TextureOffsetX = 0,
					TextureOffsetY = 0,
					TextureUnknown = 0
				},
					SignTexture = {
					TextureFile = null,
					TextureOffsetX = 0,
					TextureOffsetY = 0,
					TextureUnknown = 0
				},
					AdjoinedSector = -1,
					AdjoinedWall = -1,
					TextureAndMapFlags = 0,
					UnusedFlags2 = 0,
					AdjoinFlags = 0,
					LightLevel = 0
				},
				new() {
					LeftVertexX = 32,
					LeftVertexZ = -32,
					RightVertexX = -32,
					RightVertexZ = -32,
					MainTexture = {
					TextureFile = "DEFAULT.BM",
					TextureOffsetX = 0,
					TextureOffsetY = 0,
					TextureUnknown = 0
				},
					TopEdgeTexture = {
					TextureFile = null,
					TextureOffsetX = 0,
					TextureOffsetY = 0,
					TextureUnknown = 0
				},
					BottomEdgeTexture = {
					TextureFile = null,
					TextureOffsetX = 0,
					TextureOffsetY = 0,
					TextureUnknown = 0
				},
					SignTexture = {
					TextureFile = null,
					TextureOffsetX = 0,
					TextureOffsetY = 0,
					TextureUnknown = 0
				},
					AdjoinedSector = -1,
					AdjoinedWall = -1,
					TextureAndMapFlags = 0,
					UnusedFlags2 = 0,
					AdjoinFlags = 0,
					LightLevel = 0
				},
				new() {
					LeftVertexX = -32,
					LeftVertexZ = -32,
					RightVertexX = -32,
					RightVertexZ = 32,
					MainTexture = {
					TextureFile = "DEFAULT.BM",
					TextureOffsetX = 0,
					TextureOffsetY = 0,
					TextureUnknown = 0
				},
					TopEdgeTexture = {
					TextureFile = null,
					TextureOffsetX = 0,
					TextureOffsetY = 0,
					TextureUnknown = 0
				},
					BottomEdgeTexture = {
					TextureFile = null,
					TextureOffsetX = 0,
					TextureOffsetY = 0,
					TextureUnknown = 0
				},
					SignTexture = {
					TextureFile = null,
					TextureOffsetX = 0,
					TextureOffsetY = 0,
					TextureUnknown = 0
				},
					AdjoinedSector = -1,
					AdjoinedWall = -1,
					TextureAndMapFlags = 0,
					UnusedFlags2 = 0,
					AdjoinFlags = 0,
					LightLevel = 0
				}]
		}]
	};

	private async void buttonReloadLevelGeometry_Click(object sender, EventArgs e) {
		await this.LevelPreview!.ReloadLevelGeometryAsync(this.TestLevel);
	}

	private async void buttonSetLevelMetadata0_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetLevelMetadataAsync("SECBASE", "STALK-01.GMD", "SECBASE.PAL", new(1024));
	}

	private async void buttonSetLevelMetadata1_Click(object sender, EventArgs e) {
		await this.LevelPreview!.SetLevelMetadataAsync("SECBASE", "STALK-01.GMD", "SECBASE.PAL", new(2048));
	}

	private async void buttonReloadSector_Click(object sender, EventArgs e) {
		float x1 = (float)Random.Shared.NextDouble() * 16 + 16;
		float z1 = (float)Random.Shared.NextDouble() * 16 + 16;
		float x2 = (float)Random.Shared.NextDouble() * 16 + 16;
		float z2 = (float)Random.Shared.NextDouble() * 16 + 16;
		float x3 = (float)Random.Shared.NextDouble() * 16 + 16;
		float z3 = (float)Random.Shared.NextDouble() * 16 + 16;
		float x4 = (float)Random.Shared.NextDouble() * 16 + 16;
		float z4 = (float)Random.Shared.NextDouble() * 16 + 16;

		SectorInfo sector = this.TestLevel.Sectors[0];
		WallInfo wall0 = sector.Walls[0];
		wall0.LeftVertexX = -x1;
		wall0.LeftVertexZ = z1;
		wall0.RightVertexX = x2;
		wall0.RightVertexZ = z2;
		WallInfo wall1 = sector.Walls[1];
		wall1.LeftVertexX = x2;
		wall1.LeftVertexZ = z2;
		wall1.RightVertexX = x3;
		wall1.RightVertexZ = -z3;
		WallInfo wall2 = sector.Walls[2];
		wall2.LeftVertexX = x3;
		wall2.LeftVertexZ = -z3;
		wall2.RightVertexX = -x4;
		wall2.RightVertexZ = -z4;
		WallInfo wall3 = sector.Walls[3];
		wall3.LeftVertexX = -x4;
		sector.Walls[3].LeftVertexZ = -z4;
		sector.Walls[3].RightVertexX = -x1;
		sector.Walls[3].RightVertexZ = z1;

		await this.LevelPreview!.ReloadSectorAsync(0, sector);
	}

	private async void buttonSetSector_Click(object sender, EventArgs e) {
		int light = Random.Shared.Next(0, 32);

		SectorInfo sector = this.TestLevel.Sectors[0];
		sector.LightLevel = light;

		await this.LevelPreview!.SetSectorAsync(0, sector);
	}

	private async void buttonMoveSector_Click(object sender, EventArgs e) {
		float x = (float)Random.Shared.NextDouble() * 128 - 64;
		float y = (float)Random.Shared.NextDouble() * 128 - 64;
		float z = (float)Random.Shared.NextDouble() * 128 - 64;

		await this.LevelPreview!.MoveSectorAsync(0, new(x, y, z));
	}

	private async void buttonDeleteSector_Click(object sender, EventArgs e) {
		await this.LevelPreview!.DeleteSectorAsync(0);
	}

	private async void buttonSetSectorFloor_Click(object sender, EventArgs e) {
		HorizontalSurfaceInfo floor = this.TestLevel.Sectors[0].Floor;
		floor.Y += (float)Random.Shared.NextDouble() * 64;

		await this.LevelPreview!.SetSectorFloorAsync(0, floor);
	}

	private async void buttonSetSectorCeiling_Click(object sender, EventArgs e) {
		HorizontalSurfaceInfo ceiling = this.TestLevel.Sectors[0].Ceiling;
		ceiling.Y -= (float)Random.Shared.NextDouble() * 64;

		await this.LevelPreview!.SetSectorCeilingAsync(0, ceiling);
	}

	private async void buttonReloadWall_Click(object sender, EventArgs e) {
		float x1 = (float)Random.Shared.NextDouble() * 16 + 16;
		float z1 = (float)Random.Shared.NextDouble() * 16 + 16;
		float x2 = (float)Random.Shared.NextDouble() * 16 + 16;
		float z2 = (float)Random.Shared.NextDouble() * 16 + 16;

		SectorInfo sector = this.TestLevel.Sectors[0];
		WallInfo wall = sector.Walls[0];
		wall.LeftVertexX = -x1;
		wall.LeftVertexZ = z1;
		wall.RightVertexX = x2;
		wall.RightVertexZ = z2;
		WallInfo nextWall = sector.Walls[1];
		nextWall.LeftVertexX = x2;
		nextWall.LeftVertexZ = z2;
		WallInfo prevWall = sector.Walls[3];
		prevWall.RightVertexX = -x1;
		prevWall.RightVertexZ = z1;

		short light = (short)Random.Shared.Next(0, 32);

		wall.LightLevel = light;

		await this.LevelPreview!.ReloadWallAsync(0, 0, wall);
	}

	private async void buttonInsertWall_Click(object sender, EventArgs e) {
		SectorInfo sector = this.TestLevel.Sectors[0];
		WallInfo nextWall = sector.Walls[0];
		nextWall.LeftVertexX = 0;
		nextWall.LeftVertexZ = 0;
		WallInfo wall = new() {
			LeftVertexX = sector.Walls[3].RightVertexX,
			LeftVertexZ = sector.Walls[3].RightVertexZ,
			RightVertexX = 0,
			RightVertexZ = 0,
			MainTexture = {
				TextureFile ="DEFAULT.BM",
				TextureOffsetX = 0,
				TextureOffsetY = 0,
				TextureUnknown = 0
			},
			TopEdgeTexture = {
				TextureFile = null,
				TextureOffsetX = 0,
				TextureOffsetY = 0,
				TextureUnknown = 0
			},
			BottomEdgeTexture = {
				TextureFile = null,
				TextureOffsetX = 0,
				TextureOffsetY = 0,
				TextureUnknown = 0
			},
			SignTexture = {
				TextureFile = null,
				TextureOffsetX = 0,
				TextureOffsetY = 0,
				TextureUnknown = 0
			},
			AdjoinedSector = -1,
			AdjoinedWall = -1,
			TextureAndMapFlags = 0,
			UnusedFlags2 = 0,
			AdjoinFlags = 0,
			LightLevel = 0
		};
		sector.Walls.Insert(4, wall);

		await this.LevelPreview!.InsertWallAsync(0, 4, wall);
	}

	private async void buttonDeleteWall_Click(object sender, EventArgs e) {
		SectorInfo sector = this.TestLevel.Sectors[0];
		sector.Walls.RemoveAt(4);

		await this.LevelPreview!.DeleteWallAsync(0, 4);
	}

	private async void buttonSetVertex_Click(object sender, EventArgs e) {
		float x = (float)Random.Shared.NextDouble() * 16 + 16;
		float z = (float)Random.Shared.NextDouble() * 16 + 16;

		var sector = this.TestLevel.Sectors[0];
		WallInfo nextWall = sector.Walls[0];
		nextWall.LeftVertexX = -x;
		nextWall.LeftVertexZ = z;
		WallInfo prevWall = sector.Walls[^1];
		prevWall.RightVertexX = -x;
		prevWall.RightVertexZ = z;

		await this.LevelPreview!.SetVertexAsync(0, 0, false, new(-x, z));
	}

	private async void buttonReloadLevelObjects_Click(object sender, EventArgs e) {
		ObjectInfo[] objects = [new() {
			Type = ObjectTypes.Spirit,
			PositionX = 0,
			PositionY = 0,
			PositionZ = 0,
			Pitch = 0,
			Yaw = 0,
			Roll = 0,
			Difficulty = ObjectDifficulties.EasyMediumHard,
			Logic = "LOGIC: PLAYER\r\nEYE: TRUE"
		},
			new() {
				Type = ObjectTypes.Sprite,
				FileName = "STORMFIN.WAX",
				PositionX = -8,
				PositionY = 0,
				PositionZ = -8,
				Pitch = 0,
				Yaw = 0,
				Roll = 0,
				Difficulty = ObjectDifficulties.EasyMediumHard,
				Logic = "TYPE: TROOP"
			},
			new() {
				Type = ObjectTypes.Frame,
				FileName = "IENERGY.FME",
				PositionX = 8,
				PositionY = 0,
				PositionZ = -8,
				Pitch = 0,
				Yaw = 0,
				Roll = 0,
				Difficulty = ObjectDifficulties.EasyMediumHard,
				Logic = "LOGIC: ITEM ENERGY"
			},
			new() {
				Type = ObjectTypes.ThreeD,
				FileName = "KYL3DO.3DO",
				PositionX = 0,
				PositionY = -32,
				PositionZ = 0,
				Pitch = 0,
				Yaw = 0,
				Roll = 0,
				Difficulty = ObjectDifficulties.EasyMediumHard,
				Logic = ""
			}];

		await this.LevelPreview!.ReloadLevelObjectsAsync(objects);
	}

	private async void buttonSetObject_Click(object sender, EventArgs e) {
		ObjectInfo obj = new() {
			Type = ObjectTypes.Sprite,
			FileName = "IDPLANS.WAX",
			PositionX = 8,
			PositionY = 0,
			PositionZ = 8,
			Pitch = 0,
			Yaw = 0,
			Roll = 0,
			Difficulty = ObjectDifficulties.EasyMediumHard,
			Logic = "LOGIC: PLANS"
		};

		await this.LevelPreview!.SetObjectAsync(4, obj);
	}

	private async void buttonDeleteObject_Click(object sender, EventArgs e) {
		await this.LevelPreview!.DeleteObjectAsync(4);
	}

	private async void buttonResetCamera_Click(object sender, EventArgs e) {
		await this.LevelPreview!.ResetCameraAsync();
	}

	private async void buttonMoveCamera_Click(object sender, EventArgs e) {
		await this.LevelPreview!.MoveCameraAsync(Vector3.Zero);
	}

	private async void buttonRotateCamera_Click(object sender, EventArgs e) {
		await this.LevelPreview!.RotateCameraAsync(Vector3.Zero);
	}

	private async void buttonMoveAndRotateCamera_Click(object sender, EventArgs e) {
		await this.LevelPreview!.MoveAndRotateCameraAsync(Vector3.Zero, Vector3.Zero);
	}

	private async void buttonPointCameraAt_Click(object sender, EventArgs e) {
		await this.LevelPreview!.PointCameraAtAsync(Vector3.Zero);
	}

	private async void buttonCaptureMouse_Click(object sender, EventArgs e) {
		await this.LevelPreview!.CaptureMouseAsync();
	}

	private void LevelPreview_FloorClicked(object? sender, SectorEventArgs e) {
		this.statusText.Text = $"Sector {e.SectorIndex} floor clicked.";
	}

	private void LevelPreview_CeilingClicked(object? sender, SectorEventArgs e) {
		this.statusText.Text = $"Sector {e.SectorIndex} ceiling clicked.";
	}

	private void LevelPreview_WallClicked(object? sender, WallEventArgs e) {
		this.statusText.Text = $"Sector {e.SectorIndex} wall {e.WallIndex} clicked.";
	}

	private void LevelPreview_ObjectClicked(object? sender, ObjectEventArgs e) {
		this.statusText.Text = $"Object {e.ObjectIndex} clicked.";
	}
}
