export class LevelPreview {
	async createUnityInstanceAsync(unityData: UnityData, canvas: HTMLCanvasElement) {
		(<any>window).__levelPreview = this;

		return this.gameInstance = await (<UnityWindow><any>window).createUnityInstance(canvas, unityData);
	}
	private gameInstance?: UnityInstance;

	private lastId: number = 0;
	private pendingCalls: Record<number, (value?: unknown) => void> = {};
	private apiCall(api: string, ...args: string[]) {
		const id = ++this.lastId;
		const promise = new Promise(x => this.pendingCalls[id] = x);
		this.gameInstance!.SendMessage("Level", "OnApiCall", JSON.stringify(<ApiCall>{Id: id, Api: api, Args: args}));
		return promise;
	}

	__onApiCallFinished(id: number) {
		const callback = this.pendingCalls[id];
		delete this.pendingCalls[id];
		callback();
	}

	public quit() {
		return this.apiCall("Quit");
	}

	public captureMouse() {
		return this.apiCall("CaptureMouse");
	}

	public releaseMouse() {
		return this.apiCall("ReleaseMouse");
	}

	public reloadDataFiles() {
		return this.apiCall("ReloadDataFiles");
	}

	public addModFile(path: string) {
		return this.apiCall("AddModFile", path);
	}

	public loadLevelList() {
		return this.apiCall("LoadLevelList");
	}

	public loadLevel(index: number) {
		return this.apiCall("LoadLevel", index.toString());
	}

	public reloadLevelInPlace() {
		return this.apiCall("ReloadLevelInPlace");
	}

	public initEmptyLevel(name: string, musicIndex: number, paletteName: string) {
		return this.apiCall("InitEmptyLevel", name, musicIndex.toString(), paletteName);
	}

	public setDarkForcesPath(path: string) {
		return this.apiCall("SetDarkForcesPath", path);
	}

	public setBackground(r: number, g: number, b: number) {
		return this.apiCall("SetBackground", r.toString(), g.toString(), b.toString());
	}

	public setShowWaitBitmap(value: boolean) {
		return this.apiCall("SetShowWaitBitmap", value.toString());
	}

	public setExtendSkyPit(value: number) {
		return this.apiCall("SetExtendSkyPit", value.toString());
	}

	public setShowSprites(value: boolean) {
		return this.apiCall("SetShowSprites", value.toString());
	}

	public setShow3dos(value: boolean) {
		return this.apiCall("SetShow3dos", value.toString());
	}

	public setDifficulty(value: Difficulties) {
		return this.apiCall("SetDifficulty", value.toString());
	}

	public setAnimateVues(value: boolean) {
		return this.apiCall("SetAnimateVues", value.toString());
	}

	public setAnimate3doUpdates(value: boolean) {
		return this.apiCall("SetAnimate3doUpdates", value.toString());
	}

	public setFullBrightLighting(value: boolean) {
		return this.apiCall("SetFullBrightLighting", value.toString());
	}

	public setBypassColormapDithering(value: boolean) {
		return this.apiCall("SetBypassColormapDithering", value.toString());
	}

	public setPlayMusic(value: boolean) {
		return this.apiCall("SetPlayMusic", value.toString());
	}

	public setPlayFightTrack(value: boolean) {
		return this.apiCall("SetPlayFightTrack", value.toString());
	}

	public setVolume(value: number) {
		return this.apiCall("SetVolume", value.toString());
	}

	public setVisibleLayer(value?: number) {
		if (value === undefined || value === null) {
			return this.apiCall("SetVisibleLayer");
		} else {
			return this.apiCall("SetVisibleLayer", value.toString());
		}
	}

	public setLookSensitivity(x: number, y: number) {
		return this.apiCall("SetLookSensitivity", x.toString(), y.toString());
	}

	public setInvertYLook(value: boolean) {
		return this.apiCall("SetInvertYLook", value.toString());
	}

	public setMoveSensitivity(x: number, y: number, z: number) {
		return this.apiCall("SetMoveSensitivity", x.toString(), y.toString(), z.toString());
	}

	public setYawLimits(min: number, max: number) {
		return this.apiCall("SetYawLimits", min.toString(), max.toString());
	}

	public setRunMultiplier(value: number) {
		return this.apiCall("SetRunMultiplier", value.toString());
	}

	public setZoomSensitivity(value: number) {
		return this.apiCall("SetZoomSensitivity", value.toString());
	}

	public setUseOrbitCamera(value: boolean) {
		return this.apiCall("SetUseOrbitCamera", value.toString());
	}

	public setUseMouseCapture(value: boolean) {
		return this.apiCall("SetUseMouseCapture", value.toString());
	}

	public reloadLevelGeometry(level: LevelInfo) {
		return this.apiCall("ReloadLevelGeometry", JSON.stringify(level));
	}

	public setLevelMetadata(levelFile: string, musicFile: string, paletteFile: string, parallaxX: number, parallaxY: number) {
		return this.apiCall("SetLevelMetadata", levelFile, musicFile, paletteFile, parallaxX.toString(), parallaxY.toString());
	}

	public reloadSector(index: number, sector: SectorInfo) {
		return this.apiCall("ReloadSector", index.toString(), JSON.stringify(sector));
	}

	public setSector(index: number, sector: SectorInfo) {
		return this.apiCall("SetSector", index.toString(), JSON.stringify(sector));
	}

	public moveSector(index: number, x: number, y: number, z: number) {
		return this.apiCall("MoveSector", index.toString(), x.toString(), y.toString(), z.toString());
	}

	public deleteSector(index: number) {
		return this.apiCall("DeleteSector", index.toString());
	}

	public setSectorFloor(index: number, floor: HorizontalSurfaceInfo) {
		return this.apiCall("SetSectorFloor", index.toString(), JSON.stringify(floor));
	}

	public setSectorCeiling(index: number, ceiling: HorizontalSurfaceInfo) {
		return this.apiCall("SetSectorCeiling", index.toString(), JSON.stringify(ceiling));
	}

	public reloadWall(sectorIndex: number, wallIndex: number, wall: WallInfo) {
		return this.apiCall("ReloadWall", sectorIndex.toString(), wallIndex.toString(), JSON.stringify(wall));
	}

	public insertWall(sectorIndex: number, wallIndex: number, wall: WallInfo) {
		return this.apiCall("InsertWall", sectorIndex.toString(), wallIndex.toString(), JSON.stringify(wall));
	}

	public deleteWall(sectorIndex: number, wallIndex: number) {
		return this.apiCall("DeleteWall", sectorIndex.toString(), wallIndex.toString());
	}

	public setVertex(sectorIndex: number, wallIndex: number, rightVertex: boolean, x: number, z: number) {
		return this.apiCall("SetVertex", sectorIndex.toString(), wallIndex.toString(), rightVertex.toString(), x.toString(), z.toString());
	}

	public reloadLevelObjects(objects: ObjectInfo[]) {
		return this.apiCall("ReloadLevelObjects", JSON.stringify(objects));
	}

	public setObject(index: number, object: ObjectInfo) {
		return this.apiCall("SetObject", index.toString(), JSON.stringify(object));
	}

	public deleteObject(index: number) {
		return this.apiCall("DeleteObject", index.toString());
	}

	public resetCamera() {
		return this.apiCall("ResetCamera");
	}

	public moveCamera(x: number, y: number, z: number) {
		return this.apiCall("MoveCamera", x.toString(), y.toString(), z.toString());
	}

	public rotateCamera(w: number, x: number, y: number, z: number) {
		return this.apiCall("RotateCamera", w.toString(), x.toString(), y.toString(), z.toString());
	}

	public rotateCameraEuler(pitch: number, yaw: number, roll: number) {
		return this.apiCall("RotateCameraEuler", pitch.toString(), yaw.toString(), roll.toString());
	}

	public moveAndRotateCamera(posX: number, posY: number, posZ: number, rotW: number, rotX: number, rotY: number, rotZ: number) {
		return this.apiCall("MoveAndRotateCamera", posX.toString(), posY.toString(), posZ.toString(),
			rotW.toString(), rotX.toString(), rotY.toString(), rotZ.toString());
	}

	public moveAndRotateCameraEuler(x: number, y: number, z: number, pitch: number, yaw: number, roll: number) {
		return this.apiCall("MoveAndRotateCameraEuler", x.toString(), y.toString(), z.toString(),
			pitch.toString(), yaw.toString(), roll.toString());
	}

	public pointCameraAt(x: number, y: number, z: number) {
		return this.apiCall("PointCameraAt", x.toString(), y.toString(), z.toString());
	}

	__onEvent(eventName: string, ...args: string[]) {
		switch (eventName) {
			case "OnReady":
				this.onReady?.call(this);
				break;
			case "OnLoadError":
				this.onLoadError?.call(this, args[0], parseInt(args[1], 10), args[2]);
				break;
			case "OnLoadWarning":
				this.onLoadWarning?.call(this, args[0], parseInt(args[1], 10), args[2]);
				break;
			case "OnLevelListLoaded":
				this.onLevelListLoaded?.call(this, JSON.parse(args[0]));
				break;
			case "OnLevelLoaded":
				this.onLevelLoaded?.call(this, JSON.parse(args[0]));
				break;
			case "OnFloorClicked":
				this.onFloorClicked?.call(this, parseInt(args[0], 10));
				break;
			case "OnCeilingClicked":
				this.onCeilingClicked?.call(this, parseInt(args[0], 10));
				break;
			case "OnWallClicked":
				this.onWallClicked?.call(this, parseInt(args[0], 10), parseInt(args[1], 10));
				break;
			case "OnObjectClicked":
				this.onObjectClicked?.call(this, parseInt(args[0], 10));
				break;
		}
	}
	public onReady?: () => void;
	public onLoadError?: (fileName: string, line: number, message: string) => void;
	public onLoadWarning?: (fileName: string, line: number, message: string) => void;
	public onLevelListLoaded?: (levels: LevelListLevelInfo[]) => void;
	public onLevelLoaded?: (layers: number[]) => void;
	public onFloorClicked?: (sectorIndex: number) => void;
	public onCeilingClicked?: (sectorIndex: number) => void;
	public onWallClicked?: (sectorIndex: number, wallIndex: number) => void;
	public onObjectClicked?: (objectClicked: number) => void;
}

interface UnityWindow extends Window {
	createUnityInstance: (canvas: HTMLCanvasElement, data: UnityData) => Promise<UnityInstance>;
	unityData: UnityData;
};

type UnityData = {
	dataUrl: string;
	frameworkUrl: string;
	workerUrl?: string;
	codeUrl?: string;
	memoryUrl?: string;
	symbolsUrl?: string;
	streamingAssetsUrl: string;
	companyName: string;
	productName: string;
	productVersion: string;
	matchWebGLToCanvasSize?: boolean;
	devicePixelRatio?: number;
};

type UnityInstance = {
	SendMessage: (gameObject: string, funcName: string, value?: string | number | null) => void;
};

type ApiCall = {
	Id: number;
	Api: string;
	Args: string[];
}

enum Difficulties {
	None,
	Easy,
	Medium,
	Hard,
	All
};

type LevelListLevelInfo = {
	FileName: string;
	DisplayName: string;
};

type LevelInfo = {
	LevelFile: string;
	PaletteFile: string;
	MusicFile: string;
	ParallaxX: number;
	ParallaxY: number;
	Sectors: SectorInfo[];
};

type WallSurfaceInfo = {
	TextureFile: string;
	TextureOffsetX: number;
	TextureOffsetY: number;
	TextureUnknown: number;
};

type HorizontalSurfaceInfo = WallSurfaceInfo & {
	Y: number
};

enum WallTextureAndMapFlags {
	ShowTextureOnAdjoin = 0x1,
	IlluminatedSign = 0x2,
	FlipTextureHorizontally = 0x4,
	ElevatorChangesWallLight = 0x8,
	WallTextureAnchored = 0x10,
	WallMorphsWithElevator = 0x20,
	ElevatorScrollsTopEdgeTexture = 0x40,
	ElevatorScrollsMainTexture = 0x80,
	ElevatorScrollsBottomEdgeTexture = 0x100,
	ElevatorScrollsSignTexture = 0x200,
	HiddenOnMap = 0x400,
	NormalOnMap = 0x800,
	SignTextureAnchored = 0x1000,
	DamagePlayer = 0x2000,
	LedgeOnMap = 0x4000,
	DoorOnMap = 0x8000
};

enum WallAdjoinFlags {
	SkipStepCheck = 0x1,
	BlockPlayerAndEnemies = 0x2,
	BlockEnemies = 0x4,
	BlockShots = 0x8
};

type WallInfo = {
	LeftVertexX: number;
	LeftVertexZ: number;
	RightVertexX: number;
	RightVertexZ: number;
	MainTexture: WallSurfaceInfo;
	TopEdgeTexture: WallSurfaceInfo;
	BottomEdgeTexture: WallSurfaceInfo;
	SignTexture: WallSurfaceInfo;
	AdjoinedSector: number;
	AdjoinedWall: number;
	TextureAndMapFlags: WallTextureAndMapFlags;
	UnusedFlags2: number;
	AdjoinFlags: WallAdjoinFlags;
	LightLevel: number;
};

enum SectorFlags {
	CeilingIsSky = 0x1,
	SectorIsDoor = 0x2,
	WallsReflectShots = 0x4,
	AdjoinAdjacentSkies = 0x8,
	FloorIsIce = 0x10,
	FloorIsSnow = 0x20,
	SectorIsExplodingWall = 0x40,
	FloorIsPit = 0x80,
	AdjoinAdjacentPits = 0x100,
	ElevatorsCrush = 0x200,
	DrawWallsAsSkyPit = 0x400,
	LowDamage = 0x800,
	HighDamage = 0x1000,
	GasDamage = 0x1800,
	DenyEnemyTrigger = 0x2000,
	AllowEnemyTrigger = 0x4000,
	Subsector = 0x8000,
	SafeSector = 0x10000,
	Rendered = 0x20000,
	Player = 0x40000,
	Secret = 0x80000
};

type SectorInfo = {
	Name: string;
	LightLevel: number;
	Floor: HorizontalSurfaceInfo;
	Ceiling: HorizontalSurfaceInfo;
	AltY: number;
	Flags: SectorFlags;
	UnusedFlags2: number;
	AltLightLevel: number;
	Layer: number;
	Walls: WallInfo[];
};

enum ObjectTypes {
	Spirit,
	Safe,
	Sprite,
	Frame,
	ThreeD,
	Sound
};

enum ObjectDifficulties {
	Easy = -1,
	EasyMedium = -2,
	EasyMediumHard = 0,
	MediumHard = 2,
	Hard = 3
};

type ObjectInfo = {
	Type: ObjectTypes;
	FileName: string;
	PositionX: number;
	PositionY: number;
	PositionZ: number;
	Pitch: number;
	Yaw: number;
	Roll: number;
	Difficulty: ObjectDifficulties;
	Logic: string;
};
