# Level Preview Tool

This tool is separate from the rest of the Dark Forces Showcase, and is intended for embedding the 3D level preview provided by Level Explorer in websites and desktop tools.

The tool is built for Windows and WebGL. Currently nothing should be stopping you from also building for Linux, I just don't have a sample for embedding.

The WebGL form can be embedded in websites for use as a download preview or level viewer.

The Windows form can be embedcded into an application window and controlled by the application for use as a 3D preview for a level editor or something.

## Samples

First, I recommend you take a look at the sample you're interested in, then look at the source code to see how it is put together. Of course this document will also walk through how it is set up.

The latest downloads are here: https://github.com/The-MAZZTer/DarkForces/releases

The first thing you want to do is configure settings.json found in the download. While each setting will be documented later, the important setting is the Dark Forces path. Set this appropriately for your local install to test.

For the WebGL sample you will need to host it on a web server like IIS or Apache. Any web server will do. Then navigate to the appropriate path on the webserver and the sample should run fully functional.

For the Windows port simply run the included EXE after configuring the Dark Forces path.

The pane on the right shows samples of all the API commands you can issue, including APIs tailored for both level preview and level editor functionality. It will also be populated with the list of levels from DARK.GOB and the list of layers from the current level.

The status bar on the bottom shows the last item you have clicked. This is very basic functionality but can be used to determine if the user wants to select something.

The sample will only show levels from Dark Forces, but APIs can be used to load in mods like in the full Dark Forces Showcase (more on that later).

## Deconstructing the Samples

The best way to learn how to use this tool is to see how the samples leverage it.

First, there is a wrapper file that helps simplify the interactions with the code for both JavaScript/TypeScript and C#. Of course you can port the script to other languages as well. The two scripts are different since the API interfaces for both builds are different by necessity; the C# script also leverages Windows APIs so won't work for other platforms.

### WebGL Getting Started

We can see how the WebGL script is used in index.html. However there is a lot of API test code mixed in so here is the bare minimum.  

First, drop the settings.json file into the same folder as your HTML page. Configure it appropriately for the settings yu want Level Preview to use.

Next, include the level preview script in your project.

```html
<script type="text/javascript" src="levelPreview.js"></script>
```

You will also need the standard Untiy boilerplate for launching a Unity app in WebGL. For example here is the WebGL template used by the sample. See the Unity docs for more info.

```javascript
			var unityData = {
				dataUrl: "Build/{{{ DATA_FILENAME }}}",
				frameworkUrl: "Build/{{{ FRAMEWORK_FILENAME }}}",
#if USE_THREADS
				workerUrl: "Build/{{{ WORKER_FILENAME }}}",
#endif
#if USE_WASM
				codeUrl: "Build/{{{ CODE_FILENAME }}}",
#endif
#if MEMORY_FILENAME
				memoryUrl: "Build/{{{ MEMORY_FILENAME }}}",
#endif
#if SYMBOLS_FILENAME
				symbolsUrl: "Build/{{{ SYMBOLS_FILENAME }}}",
#endif
				streamingAssetsUrl: "StreamingAssets",
				companyName: {{{ JSON.stringify(COMPANY_NAME) }}},
				productName: {{{ JSON.stringify(PRODUCT_NAME) }}},
				productVersion: {{{ JSON.stringify(PRODUCT_VERSION) }}},
				// matchWebGLToCanvasSize: false, // Uncomment this to separately control WebGL canvas render size and DOM element size.
				// devicePixelRatio: 1, // Uncomment this to override low DPI rendering on high DPI displays.
			};
```

Instantiate the LevelPreview object:

```javascript
const levelPreview = new LevelPreview();
```

Then we want to wait for the onReady event, so we know when the API is ready to receive commands. Here are the minimal commands needed to load SECBASE. Be sure to wait for each Promise returned before calling the next API,

```javascript
levelPreview.onReady = async () => {
	await levelPreview.reloadDataFiles();
	await levelPreview.loadLevelList();
	await levelPreview.loadLevel(0);
};
```

Finally, start Unity. Pass in the data object \Unity requires, and a reference to the canvas to use to display Unity. See the Unity docs for more info.

```javascript
await levelPreview.createUnityInstanceAsync(unityData, document.getElementById("unity-canvas"));
```

### Windows Getting Started

This applies to C# applications. You will need to roll your own solution for other languages, but you can of course use the C# script as a basis.

First you will want to place the settings.json into the same folder as your executable. Configure it appropriately for the settings you want Level Preview to have.

Next, include the LevelPreview.cs script in your project. Keep in mind it was written for .NET 8. If you use it in an earlier version of .NET you may have to pull in a newer C# language revision to your project or adjust the class's code.

The LevelPreview object can be instantiated at any time. It impelemnts IDisposable and will automatically shut down Unity when disposed.

```csharp
using LevelPreview preview = new();
```

If you do not want Unity ended at the end of the current block, then you should not use the using keyword. However you should store the object somewhere and `.Dispose()` of it later, such as when the window is closed by the user. In the sample project, using is used to dispose the object when `Application.Run()` returns, which will only be once the winedow is closed.

Before we do start Unity, we will need a place to put it. Unity will embed itself into any window or control.

For WinForms you can place a panel (for the sample, I use a panel that is part of a SplitPanel) and then you can control that panel to gain some limited control over the Unity content. There are some shortcomings which we will address later. You can take the .Handle of the control to use and pass it in to StartAsync.

Unlike the WebGL port, because of the way the desktop port works, listening to the Ready event isn't possible. However the StartAsync method will complete once the API is ready so it is not necessary, so be sure to await it.

```csharp
await preview.StartAsync(this.split.Panel1.Handle);
```

For WPF and other frameworks, if they are "windowless" you can find and pass in the main window handle, however Unity will cover the entire window by default. You can use Windows APIs to adjust this behavior (see below). For frameworks that are not windowless you can use the same procedure as with WinForms.

After this command, here's the bare minimum to get SECBASE going:

```csharp
await preview.ReloadDataFilesAsync();
await preview.LoadLevelListAsync();
await preview.LoadLevelAsync(0);
```

This works, but we will need to do a bit more work.

#### Unity does not resize

To fix this issue, we need to subscribe to the Resize (or equivilent) event for our container control we embedded Unity in. Once it fires, we can use SetWindowPos Windows API on the `.UnityWindow` property of the LevelPreview object to resize or move Unity.

For example this resizes Unity but does not move it. If you are in a windowless framework  you may want to move it too.

```csharp
private void ResizeUnity() {
	if (this.LevelPreview == null || this.LevelPreview.HasExited || this.LevelPreview.UnityWindow == IntPtr.Zero) {
		return;
	}

	SetWindowPos(this.LevelPreview!.UnityWindow, IntPtr.Zero, 0, 0, this.split.Panel1.Width, this.split.Panel1.Height,
		SWP.DEFERERASE | SWP.NOACTIVATE | SWP.NOCOPYBITS | SWP.NOMOVE | SWP.NOOWNERZORDER | SWP.NOREDRAW | SWP.NOZORDER);
}
```

#### Unity cannot capture the cursor

Not sure why this happens, but we can control this through WinForms. Your UI framework of choice may also have a mechanism for this.

Level Preview will emit an event which we can listen to to determine if we need to capture the cursor. Here is what the sample does:

```csharp
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
```

#### Unity crashes the entire app

The Level Preview wrapper script launches Unity in-process in order to work around some nasty issues regarding raw input (see below). However this means Unity may do things like bring down your app if it crashes, which includes if you forget to add the _Data folder.

You can launch it out of process. Create a fresh build of the Level Preview tool (instructions brlow) so you have an EXE, which is not included in the sample. Then make sure settings.json is next to that EXE and not your own app's. There is commented out code in the Level Preview wrapper script to support this scenario that you will need to re-enable, however note the following additional problem:

Crashing shouldn't happen regardless as long as the _Data folder is in the proper location.

#### When running out-of-process, keyboard input doesn't work.

This might also affect other input devices like gamepads, I don't know. It doesn't seem to affect the mouse.

There is commended out code in the Sample in both the form and the LevelPreview script to handle capturing and forwarding raw input. It works fine but I think it is better to not have to deal with that in the first place. But you do you. I kept the code in, commented, in case it is useful.

#### When publishing .NET self-contained, starting LevelPreview crashes my app!

When publishing self-contained, standard .NET DLLs are placed in the application folder. I suspect this conflicts with Unity's use of the Mono framework which uses DLLs of the same name. If you want to publish self-contained, you must also enable single-file publish to avoid this.

## Copyright Concerns

Disclaimer: I make no claims as to what commercial files you can and cannot distribute with this tool and maintain fair use status. It is your responsibility to control what files you distribute over the internet and determine whether you have rights to do so.

Hosting WebGL on a server means you will to provide any commercial Dark Forces files needed to display the level you want to view. Typically these are the four main GOB files, or specifically the files in them.

If you are not playing music, you may leave off SOUNDS.GOB. If you do not want to display sprite objects, you may leave off SPRITES.GOB.

TEXTURES.GOB is required for textures. DARK.GOB provides 3DOs, PALs, CMPs, and other key files mods may leverage.

You may create custom GOBs which cut down on the files included. settings.json will list the GOB files that will be loaded before mods are specified through the API. However note that DARK.GOB is always assumed to contain the base JEDI.LVL and Level Preview may not work properly if this assumption is broken. You may of course move any other files around as you wish. For example you may want to remove the LEV, GOL, INF, and O files from the commercial game to avoid including the commercial levels, if your goal is to just view mods.

## Settings

Setttngs can be modified in settings.json to take effect on Level Preview startup or modified at runtime using API calls (below).

For complex type defintions, see the wrapper class source code.

### DarkForcesPath

#### Type

string (path)

#### Description

The path to the folder containing the standard Dark Forces GOB files.

On Windows this should be a relative or absolute local path pointing to the folder. On WebGL it should by a relative or absolute URL. In both cases a relative path will be resolved relative to the folder containing settings.json.
 
Note that any relative-pathed mod files you add will be resolved relative to this folder.

### ApiPort

#### Type

ushort (port number)

#### Description

Windows only. No effect on WebGL.

The Windows version uses a HTTP server to exposee API calls. This controls the port number it will use. The server is only usable by the local PC. This setting can only be changed in the settings.json file.

### DataFiles

#### Type

string[] (filenames)

#### Description

The list of standard GOB files that should be loaded from the Dark Forces folder. Useful if you don't want to provide them al or want to provide custom ones. Currently DARK.GOB must be present and always contain JEDI.LVL if you want to view standard levels. This setting can only be changed in the settings.json file.

### BackgroundR, BackgroundG, BackgroundB

#### Type

float (from 0 to 1)

#### Description

Together these three values conrol the clear color of the camera, which is the color displayed behind all the level geometry in the void.

### ShowWaitBitmap

#### Type

bool

#### Description

Controls whether WAIT.BM is displayed while levels are loading.

### ExtendSkyPit

#### Type

float (DFUs)

#### Descriptions

Controls how far skies/pits will be extruded to show where Kyle can be flung/fall during gameplay. Tends to obscure views in orbit camera mode so set to 0 by default. DF uses a value of 100.

### ShowSprites

#### Type

bool

#### Description

Whether or not to show sprites (WAXs and FMEs).

### Show3dos

#### Type

bool

#### Description

Whether or not to show 3D models (3DOs).

### Difficulty

#### Type

Difficulties

#### Description

Controls which objects are displayed based on game difficulty. All shows all objects regardless.

### AnimateVues

#### Type

bool

#### Description

Whether or not VUEs will automatically play and loop.

### Animate3doUpdates

#### Type

bool

#### Description

Whether or not 3DOs will be given rotation animation according to their object definitions.

### FullBrightLighting

#### Type

bool

#### Description

Allows you to turn off lighting and make everything show as light level 31.

### BypassColormapDithering

#### Type

bool

#### Description

If false, applies CMP light level data. If true, only applies CMP for light level 0. For all others, colors are interpolated evenly betwen light levels (as in Dark Forces, light level 31 always uses the PAL valuhs directly).

### PlayMusic

#### Type

bool

#### Description

Whether or not to play the level music.

### PlayFightMusic

#### Type

bool

#### Description

Whether or not the fight or stalk music should be played, if music is enabled.

### Volume

#### Type

float (0 to 1)

#### Description

Adjust music volume.

### VisibleLayer

#### Type

int? (null or a layer number)

#### Description

Control which layer is displayed. Null value displays all layers.

### LookSensitivityX, LookSensitivityY

#### Type

float

#### Description

Controls sensitivity of mouse movements when used to rotate the camera. Recommended value is 0.25 for both axes.

### InvertYLook

#### Type

bool

#### Description

Whether or not the Y-Axis is inverted for looking. Recommended always false for orbit camera, and as-preferred for FPS camera.

### MoveSensitivityX, MoveSensitivityY, MoveSensitivityZ

#### Type

float

#### Description

The sensitivity of the mouse when used to move the camera. Recommended value 1 for all axes.

### YawLimitMin, YawLimitMax

#### Type

float (-90 to 90)

#### Description

Controls how far up and down the user can tilt the camera. It's not recommended to allow values of -90 and 90 since the camera can flip over. So the defaults are -89.999 and 89.999.

### RunMultipliaer

#### Type

float (multiplier)

#### Description

How much the movement speed should be multiplied by when Shift is held.

### ZoomSensitivity

#### Type

float

#### Description

How far the camera moves in orbit mode when using the scroll wheel. Recommended value is -0.01.

### UseOrbitCamera

#### Type

bool

#### Description

Which camera system should be used.

Orbit camera focuses on a point (by default, the mathematical center of the level). Holding the left mouse button and moving the mouse will spin the camera around that point. Holding the right mouse button and moving the mouse moves tha focus point and camera on the camera's X and Y axes. Using the scroll wheel will move the camera closer or farther from the focus point.

The FPS camera provides flying FPS controls. If the mouse is locked (see next section) moving the mouse will look, otherwise holding left click will allow you to look. WASD or the arrow keys will move the camera. Q and E or Page Up and Page Down move up and down.

### UseMouseCapture

#### Type

bool

#### Description

Whether or not to use mouse capture for the FPS camera. Ignored for the orbit camera.

You can click on the window to capture the mouse and then look around freely with it. You can press ESC to release the mouse.

If this option is off, you must hold left click to temporarily capture the mouse and look around.

### ShowHud

#### Type

bool

#### Description

Show or hide the label that contains camera statistics and hover information.

### HudAlign

#### Type

int / string

#### Description

The valid values are listed here:

https://docs.unity3d.com/Packages/com.unity.textmeshpro@2.0/api/TMPro.TextAlignmentOptions.html

For API use you may pass in the string or numeric value. For the settings.json you must use the numeric value.

The default is TopLeft / 257.

### HudFontSize

#### Type

float

#### Description

Adjusts the maximum font size used by the HUD. The default is 36.

### HudColorR / HudColorG / HudColorB / HudColorA

#### Type

float (0 to 1)

#### Description

Adjusts the color of the HUD text, including its opacity.

### ShowHudCoordinates

#### Type

bool

#### Description

Whether or not camera stats are shown on the HUD.

### HudFpsCoordinates

#### Type

string

#### Description

The format string used for camera stats when using the FPS camera.

The string can follow all conventions allowed by the .NET string.Format function: https://learn.microsoft.com/en-us/dotnet/api/system.string.format?view=net-8.0

A major difference is instead of using numbered arguments, named arguments are used, still inside {}.

The list of allowed placeholders is as follows.

* x - The x coordinate of the camera in DFUs.
* y - The y coordinate of the camera in DFUs.
* z - The z coordinate of the camera in DFUs.
* pitch - The pitch of the camera.
* yaw - The yaw of the camera.
* roll - The roll of the camera (should always be 0).

### HudOrbitCoordinates

#### Type

string

#### Description

The format string used for camera stats when using the orbit camera.

The string can follow all conventions allowed by the .NET string.Format function: https://learn.microsoft.com/en-us/dotnet/api/system.string.format?view=net-8.0

A major difference is instead of using numbered arguments, named arguments are used, still inside {}.

The list of allowed placeholders is as follows.

* x - The x coordinate of the point the camera orbits around in DFUs.
* y - The y coordinate of the point the camera orbits around in DFUs.
* z - The z coordinate of the point the camera orbits around in DFUs.
* pitch - The pitch of the camera.
* yaw - The yaw of the camera.
* roll - The roll of the camera (should always be 0).
* distance - The distance from the camera to its focus point in DFUs.

### ShowHudRaycastHit

#### Type

bool

#### Description

Whether or not information about geometry/objects the pointer is over is shown on the HUD.

### HudRaycastFloor

#### Type

string

#### Description

The format string used to display information about a floor the pointer is aiming at.

The string can follow all conventions allowed by the .NET string.Format function: https://learn.microsoft.com/en-us/dotnet/api/system.string.format?view=net-8.0

A major difference is instead of using numbered arguments, named arguments are used, still inside {}.

The list of allowed placeholders is as follows.

* hitX - The x coordinate the pointer is aiming at in DFUs.
* hitY - The y coordinate the pointer is aiming at in DFUs.
* hitZ - The z coordinate the pointer is aiming at in DFUs.
* sectorIndex - The index number of the sector the pointer is aiming at.
* sectorName - The name of the sector the pointer is aiming at, or a blank string if it doesn't have a name.
* sector - The name of the sector the pointer is aiming at, or the index number if it doesn't have a name.
* altLight - The alt light level of the sector.
* flags - A text list of the flags on the sector.
* layer - The layer the sector is on.
* light - The light level of the sector.
* unusedFlags - The integer value of UnusedFlags2 for the sector.
* walls - The number of walls the sector has.
* vertices - The number of vertices the sector has.
* textureFile - The texture of the floor.
* textureOffsetX - The texture offset x of the floor.
* textureOffsetZ - The texture offset z of the floor.
* textureUnknown - The integer value of the floor's TextureUnknown field.
* y - The height of the floor in DFUs.

### HudRaycastCeiling

#### Type

string

#### Description

The format string used to display information about a ceiling the pointer is aiming at.

The string can follow all conventions allowed by the .NET string.Format function: https://learn.microsoft.com/en-us/dotnet/api/system.string.format?view=net-8.0

A major difference is instead of using numbered arguments, named arguments are used, still inside {}.

The list of allowed placeholders is as follows.

* hitX - The x coordinate the pointer is aiming at in DFUs.
* hitY - The y coordinate the pointer is aiming at in DFUs.
* hitZ - The z coordinate the pointer is aiming at in DFUs.
* sectorIndex - The index number of the sector the pointer is aiming at.
* sectorName - The name of the sector the pointer is aiming at, or a blank string if it doesn't have a name.
* sector - The name of the sector the pointer is aiming at, or the index number if it doesn't have a name.
* altLight - The alt light level of the sector.
* flags - A text list of the flags on the sector.
* layer - The layer the sector is on.
* light - The light level of the sector.
* unusedFlags - The integer value of UnusedFlags2 for the sector.
* walls - The number of walls the sector has.
* vertices - The number of vertices the sector has.
* textureFile - The texture of the ceiling.
* textureOffsetX - The texture offset x of the ceiling.
* textureOffsetZ - The texture offset z of the ceiling.
* textureUnknown - The integer value of the ceiling's TextureUnknown field.
* y - The height of the ceiling in DFUs.

### HudRaycastWall

#### Type

string

#### Description

The format string used to display information about a wall the pointer is aiming at.

The string can follow all conventions allowed by the .NET string.Format function: https://learn.microsoft.com/en-us/dotnet/api/system.string.format?view=net-8.0

A major difference is instead of using numbered arguments, named arguments are used, still inside {}.

The list of allowed placeholders is as follows.

* hitX - The x coordinate the pointer is aiming at in DFUs.
* hitY - The y coordinate the pointer is aiming at in DFUs.
* hitZ - The z coordinate the pointer is aiming at in DFUs.
* sectorIndex - The index number of the sector the pointer is aiming at.
* sectorName - The name of the sector the pointer is aiming at, or a blank string if it doesn't have a name.
* sector - The name of the sector the pointer is aiming at, or the index number if it doesn't have a name.
* wall - The index number of the wall the pointer is aiming at.
* altLight - The alt light level of the sector.
* flags - A text list of the flags on the sector.
* layer - The layer the sector is on.
* light - The light level of the sector.
* unusedFlags - The integer value of UnusedFlags2 for the sector.
* walls - The number of walls the sector has.
* vertices - The number of vertices the sector has.
* adjoinSectorIndex - The index number of the sector this wall is adjoined to, or -1 if none.
* adjoinSectorName - The name of the sector this wall is adjoined to, or empty string if there is none or it doesn't habe a name.
* adjoinSector - The name of the sector this wall is adjoined to, or the index number if it doesn't have a name, or "\<NONE\>" if none.
* adjoinWall - The index number of the wall this wall is adjoined to, or -1 if none.
* adjoinFlags - A text list of the adjoin flags on the wall.
* botTextureFile - The texture of the wall's bottom part.
* botTextureOffsetX - The texture offset x of the wall's bottom part.
* botTextureOffsetZ - The texture offset z of the wall's bottom part.
* botTextureUnknown - The integer value of the wall's bottom part's TextureUnknown field.
* x1 - The x coordinate of the wall's left vertex.
* z1 - The z coordinate of the wall's left vertex.
* wallLight - The extra light level of the wall.
* midTextureFile - The texture of the wall's main part.
* midTextureOffsetX - The texture offset x of the wall's main part.
* midTextureOffsetZ - The texture offset z of the wall's main part.
* midTextureUnknown - The integer value of the wall's main part's TextureUnknown field.
* x2 - The x coordinate of the wall's right vertex.
* z2 - The z coordinate of the wall's right vertex.
* signTextureFile - The texture of the wall's sign.
* signTextureOffsetX - The texture offset x of the wall's sign.
* signTextureOffsetZ - The texture offset z of the wall's sign.
* signTextureUnknown - The integer value of the wall's sign's TextureUnknown field.
* textureMapFlags - A text list of the texture / map flags on the wall.
* topTextureFile - The texture of the wall's top part.
* topTextureOffsetX - The texture offset x of the wall's top part.
* topTextureOffsetZ - The texture offset z of the wall's top part.
* topTextureUnknown - The integer value of the wall's top part's TextureUnknown field.
* wallUnusedFlags - The integer value of UnusedFlags2 for the wall.

### HudRaycastObject

#### Type

string

#### Description

The format string used to display information about an object the pointer is aiming at.

The string can follow all conventions allowed by the .NET string.Format function: https://learn.microsoft.com/en-us/dotnet/api/system.string.format?view=net-8.0

A major difference is instead of using numbered arguments, named arguments are used, still inside {}.

The list of allowed placeholders is as follows.

* hitX - The x coordinate the pointer is aiming at in DFUs.
* hitY - The y coordinate the pointer is aiming at in DFUs.
* hitZ - The z coordinate the pointer is aiming at in DFUs.
* sectorIndex - The index number of the sector the object is in, -1 if a sector can't be found.
* sectorName - The name of the sector the object is in, or a blank string if it doesn't have a name or the sector can't be found.
* sector - The name of the sector the object is in, or the index number if it doesn't have a name, or "\<NONE\>" if there is no sector.
* object - The index number of the object.
* x - The x coorinate of the object in DFUs.
* y - The y coorinate of the object in DFUs.
* z - The z coorinate of the object in DFUs.
* pitch - The pitch of the object.
* yaw - The yaw of the object.
* roll - The roll of the object.
* difficulty - Text showing which difficulties the object is active for.
* filename - The filename the object uses for display, or a blank string if none.
* logic - The logic text associated with the object.
* type - The type of object, as a string.

## Events

The LevelPreview wrapper class will emit a few different events. The implementation differs between WebGL and Windows but the class  handles that for you.

In C# the event arguments use the standard EventArgs syntax and are named. Events use the member names given below.

In JavaScript/TypeScript arguments are directly passed to the event handler as function arguments. Event names are prefixed with "on" and are a simple function assignment. It is up to the developer if they want to modify the script for a more fully-featured event system..

For complex arguments type defintions, see the wrapper class source code.

Here are the events and arguments.

### Ready

#### Arguments

None

#### Description

This event is called on WebGL when the API is ready. On Windows you can't receive this event due to needing to register an event callback after the API is up (the wrapper script does this for you), but if you wait for `StartAsync` to complete you can issue API calls at that point.

### CursorLockStateChanged

#### Arguments

int State - The Unity CursorLockModes value. 0 is None, 1 is Locked, 2 is Confined.

#### Description

Mostly useful on Windows, used to allow the host application to work around Unity's inability to lock the cursor.

### LoadError

#### Arguments

string File - The file with the error.

int Line - The line number containing the error, 0 if non-applicable.

string Message - A human-readable description of the problem.

#### Description

If Level Preview failed to load a level, this will be fired.

### LoadWarning

#### Arguments

string File - The file with the warning.

int Line - The line number containing the warning, 0 if non-applicable.

string Message - A human-readable description of the problem.

#### Description

These warnings do not stop Level Preview from loading a level. It may not be able to read the data correctly. There are a few of these legitimately in Dark Forces, those levels will load properly.

### LevelListLoaded

#### Arguments

LevelListLevelInfo[] Levels - The list of levels.

#### Description

Fires when JEDI.LVL is read and the level list is available. You can use this to determine what levels a mod contains.

### LevelLoaded

#### Arguments

int[] Layers - List of the layers used in the level.

#### Description

Fires when the level is loaded and the user may now use the interface to explore the level.

### FloorClicked

#### Arguments

int SectorIndex - The sector number clicked.

#### Description

Fires when the user left clicks a sector floor.

### CeilingClicked

#### Arguments

int SectorIndex - The sector number clicked.

#### Description

Fires when the user left clicks a sector ceiling.

### WallClicked

#### Arguments

int SectorIndex - The sector number clicked.

int WallIndex - The wall number clicked.

#### Description

Fires when the user left clicks a sector wall.

### ObjectClicked

#### Arguments

int ObjectIndex - The object number clicked.

#### Description

Fires when the user left clicks an object.

## API

Whether in Windows or WebGL you can call APIs to adjust any of the settings at runtime as well as control the camera, geomatry, objects, and more.

In the C# wrapper class the API calls are all async functions. Await one before calling the next. All function names have Async added onto the end to conform to .NET naming conventions.

In WebGL the API calls return Promises. Same deal, wait for them to resolve before making the next call. All the function names start with a lowercase letter to conform to common JavaScript naming conventions.

### Settings APIs

Each setting with the exception of ApiPort and DataFiles can be set at runtime. The API name is the setting name prefixed with Set. See the individual settings for documentation on what they do. 

Note that related settings may be grouped together and the APIs will take each setting as a separate argument. For example, SetBackground. In C# values for colors, Vector2/Vector3/Quaternions are set with instances of those objects, while the individual fields are used in JS as separate objects as JS lacks those types natively.

Below are some notes on using the APIs to set specific settings.

#### SetDarkForcesPath

You must call ReloadDataFiles for this to take effect.

#### SetBackground / SetHudColor

In JavaScript, R, G, B values are specified in one set command as individual arguments.

In C# you can pass in individual floats or pass in a single Color object. Alpha channel is ignored for the background.

#### SetLookSensitivity / SetMoveSensitivity

In JavaScript, pass in X, Y or X, Y, Z values as individual arguments.

In C# pass in an appropriate Vector2 or Vector3 object.

#### SetYawLimits

Pass in min and max as two arguments.

### Quit

#### Arguments

None

#### Description

Shuts down Unity. No need to call this in WebGL, and it's automatically called in Windows if you properly `.Dispose()` the wrapper object.

### CaptureMouse

#### Arguments

None

#### Description

Forces mouse capture when UseMouseCapture setting is on.

This may not work properly in WebGL in all situations since the mouse needs to interact with the canvas for the browser to allow it to get captured.

### ReleaseMouse

#### Arguments

None

#### Description

Forces mouse release when UseMouseCapture setting is on.

### ReloadDataFiles

#### Arguments

None

#### Description

Completely resets the level preview state, unloading all level and object data, and reloads the Dark Forces GOBs from scratch. This gives you effectively a fresh restart (with the exception of any settings you've changed).

### AddModFile

#### Arguments

string path - The path to the file. For WebGL this should resolve to a URL of a file accessible by the client.

#### Description

Selects a mod file to load. You can select a GOB or a loose file. You should call this API after calling ReloadDataFiles and before any APIs to load level data, if you want to load mod files.

The path can be absolute or relative to the Dark Forces folder.

### LoadLevelList

#### Arguments

None

#### Description

Loads JEDI.LVL from the known files and parses out the level list, dispatching the LevelListLoaded event. Currently JEDI.LVL is expected either in a single provided mod GOB file or DARK.GOB. Other configurations may not work properly.

Note that any levels found but not specified in JEDI.LVL will be automatically appended to the end of this list so you can access them. The GOB file where JEDI.LVL was loaded from will be searched for LEV files not mentioned in JEDI.LVL for this purpose.

### LoadLevel

#### Arguments

int index - The index of the level in JEDI.LVL

#### Description

After you call LoadLevelList you can call LoadLevel to load one of the levels specified in that file, by index.

Once a level is loaded, you can call this API again to switch them out.

### ReloadLevelInPlace

#### Arguments

None

#### Description

Reloads the last level specified by LoadLevel from the file. Can be used to revert changes.

May reload from internal cache; to ensure it's reloaded from disk, start over with ReloadDataFiles.

This API is intended for use on LoadLevels loaded levels only. Levels constructed via other APIs can't use this.

### InitEmptyLevel

#### Arguments

string name - The filename without extension.

int musicIndex - The index number in the STALK filename.

string palettename - The filename without extension.

#### Description

The parameters are used to initialize the LEV metadata. Creates a new level with no geometry or objects, ready to populate using API calls.

### ReloadLevelGeometry

#### Arguments

LevelInfo level - The level data.

#### Description

Wipes out all loaded LEV data and replaces it with the data from the provided object. See the wrapper class source code for the object structure.

All sectors, walls, vertices are loaded.

Note that for each sector, any vertex coordinates that overlap will be merged into a single vertex on import.

Object data is not modified.

### SetLevelMetadata

#### Arguments

string levelFile - The filename without extension.

string musicFile - The filename for the music field of the LEV header, with extension.

string paletteFile - The filename for the palette field of the LEV header, with extension.

\[WebGL\] float parallaxX - The parallax X value in the LEV header.

\[WebGL\] float parallaxY - The parallax Y value in the LEV header.

\[Windows\] Vector2 parallax - The parallax value in the LEV header.

#### Description

Set the LEV header values for the currently loaded level. Only really affects parallax.

### ReloadSector

### Arguments

int index - The index of the sector to reload.

SectorInfo sector - The data to use to replace the existing data.

#### Description

This unloads all sector/wall/vertex information for the sector and replaces it with incoming data.

If neighboring sectors need to be adjusted to line up with the new data, it's up to the caller to adjust them with separate API calls.

If the index is one past the end of the sector list, a new sector is added.

### SetSector

### Arguments

int index - The index of the sector to set.

SectorInfo sector - The data to use to replace the existing data.

#### Description

Unlike ReloadSector, this will only replace sector metadata, and will not touch floor, ceiling, wall, or vertex data.

The fields replaced are: AltLightLevel, AltY, Flags, Layer, LightLevel, Name, Unusedflags2. All other fields are not replaced.

### MoveSector

### Arguments

int index - The index of the sector to move.

\[WebGL\] float x - DFUs to move on the X axis.

\[WebGL\] float y - DFUs to move on the Y axis.

\[WebGL\] float z - DFUs to move on the Z axis.

\[Windows\] Vector3 value - DFUs to move the sector.

#### Description

This will move a sector. Adjoined sectors are not adjusted and it is up to the developer to adjust them manually with subsquent API calls.

Sectors can be moved on the X Z plane or vertically, which will adjust Y values of floor and ceiling. Texture offsets are not adjusted.

### DeleteSector

### Arguments

int index - The index of the sector to delete.

#### Description

Deletes a sector so it won't be displayed. The indices of all sectors that come after it will shift down to fill the gap.

### SetSectorFloor

### Arguments

int index - The index of the sector to set.

HorizontalSurfaceInfo floor - The data to set on the sector's floor.

#### Description

Replaces texture and Y level data for the floor.

### SetSectorCeiling

### Arguments

int index - The index of the sector to set.

HorizontalSurfaceInfo ceiling - The data to set on the sector's floor.

#### Description

Replaces texture and Y level data for the i.

### ReloadWall

### Arguments

int sectorIndex - The index of the sector.

int wallIndex - The index of the wall to set.

WallInfo wall - The wall data.

#### Description

This unloads all wall information and replaces it with incoming data.

Any vertices this wall shares with others will also be moved to the new position of this wall's vartices.

If the index is one past the end of the wall list, a new wall is added. InsertWall can also be used for this purpose.

No other sectors are adjusted with this API call.

### InsertWall

### Arguments

int sectorIndex - The index of the sector.

int wallIndex - The index to insert the wall at in the wall list.

WallInfo wall - The wall data.

#### Description

This inserts a wall in between two existing walls. The vertices for the walls before and after the current one will be adjusted to line up with the new wall's vertices.

No other sectors are adjusted with this API call.

### DeleteWall

### Arguments

int sectorIndex - The index of the sector.

int wallIndex - The index of the wall to delete.

#### Description

Deletes a wall so it won't be displayed. The indices of all walls that come after it will shift down to fill the gap.

The left vertex of the next wall will be removed, and it will use the old left vertex of the deleted wall instead. So the deleted wall is effectively merged with the next one.

No other sectors are adjusted with this API call.

### SetVertex

### Arguments

int sectorIndex - The index of the sector.

int wallIndex - The index of the wall.

bool rightVertex - Whether or not to adjust the left or right vertex.

\[WebGL\] float x - The x coordinate to move the vertex to.

\[WebGL\] float z - The z coordinate to move the vertex to.

\[Windows\] Vector2 value - The coordinates to move the vertex to.

#### Description

Moves a vertex. Any other walls which share this vertex will also see this vertex moved.

No other sectors are adjusted with this API call.

### ReloadLevelObjects

#### Arguments

ObjectInfo[] objects - The object data.

#### Description

Wipes out all loaded O data and replaces it with the data from the provided array. See the wrapper class source code for the object structure.

All objects are cleared and reloaded.

LEV data is not modified.

### SetObject

#### Arguments

int index - The index of the object.

ObjectInfo - The object data.

#### Description

Replaces the object with new data.

If the index is one past the end of the object list, a new object is added.

### DeketeObject

#### Arguments

int index - The index of the object to delete.

#### Description

Removes an object. All objects that come after it in the object list will have their indices shifted down by one to fill the gap.

### ResetCamera

#### Arguments

None

#### Description

Resets the camera.

For the orbit camera, this moves the camera to the perimeter of the level and points the camera at the center of the level.

For the FPS camera, this moves the camera to the EYE object.

### MoveCamera

#### Arguments

\[WebGL\] float x - X coordinate in DFUs to move the camera.

\[WebGL\] float y - Y coordinate in DFUs to move the camera.

\[WebGL\] float z - Z coordinate in DFUs to move the camera.

\[Windows\] Vector3 value - The coordinates in DFUs to move the camera.

#### Description

Moves the camera to the specified coordinates.

For orbit camera, moves the focus point along with the camera.

### RotateCamera

#### Arguments

\[WebGL\] float w - W value in quaternion to rotate the camera.

\[WebGL\] float x - X value in quaternion to rotate the camera.

\[WebGL\] float y - Y value in quaternion to rotate the camera.

\[WebGL\] float z - Z value in quaternion to rotate the camera.

\[Windows\] Quaternion value - The rotation to apply to the camera.

#### Description

Rotate the camera.

For orbit camera, also moves the camera so that the camera maintains the same focus point and distance as before.

### RotateCameraEuler

In C#, the API call is RotateCameraAsync with different arguments.

#### Arguments

\[WebGL\] float pitch - The new pitch value.

\[WebGL\] float yaw - The new yaw value.

\[WebGL\] float roll - The new roll value.

\[Windows\] Vector3 value - The euler angles to apply to the camera.

#### Description

Rotate the camera.

For orbit camera, also moves the camera so that the camera maintains the same focus point and distance as before.

### MoveAndRotateCamera

#### Arguments

\[WebGL\] float posX - X coordinate in DFUs to move the camera.

\[WebGL\] float posY - Y coordinate in DFUs to move the camera.

\[WebGL\] float posZ - Z coordinate in DFUs to move the camera.

\[Windows\] Vector3 pos - The coordinates in DFUs to move the camera.

\[WebGL\] float rotW - W value in quaternion to rotate the camera.

\[WebGL\] float rotX - X value in quaternion to rotate the camera.

\[WebGL\] float rotY - Y value in quaternion to rotate the camera.

\[WebGL\] float rotZ - Z value in quaternion to rotate the camera.

\[Windows\] Quaternion rot - The rotation to apply to the camera.

#### Description

Moves and rotates the camera.

For orbit camera this will preserve the distance but adjust the focus point to match where the camera is now looking.

### MoveAndRotateCameraEuler

In C#, the API call is MoveAndRotateCameraAsync with different arguments.

#### Arguments

\[WebGL\] float posX - X coordinate in DFUs to move the camera.

\[WebGL\] float posY - Y coordinate in DFUs to move the camera.

\[WebGL\] float posZ - Z coordinate in DFUs to move the camera.

\[Windows\] Vector3 pos - The coordinates in DFUs to move the camera.

\[WebGL\] float pitch - The new pitch value.

\[WebGL\] float yaw - The new yaw value.

\[WebGL\] float roll - The new roll value.

\[Windows\] Vector3 rot - The euler angles to apply to the camera.

#### Description

Moves and rotates the camera.

For orbit camera this will preserve the distance but adjust the focus point to match where the camera is now looking.

### PointCameraAt

#### Arguments

\[WebGL\] float x - X coordinate in DFUs to point the camera.

\[WebGL\] float y - Y coordinate in DFUs to point the camera.

\[WebGL\] float z - Z coordinate in DFUs to point the camera.

\[Windows\] Vector3 value - The coordinates in DFUs to point the camera.

#### Description

Points the camera towards a point.

For orbit camera this will set the focus point to that point as well.

## Misc Details

That should be all you need to know to fully leverage this tool. However there are other things that are good to know.

### Windows build runs in-process

As discussed earlier, the Windows build runs in-process with your host app,. Since Unity leverage Windows' Raw Input APIs, it cannot receive raw inputs while it is a child of another window from another process. It seems only processes with visible top level windows can receive raw input data. So to simplify things Unity is hosted in process so it can successfully receive this data.

There is a solution to forward the data to an out of process Unity, but it's a bit convoluted as it uses the existing API framework to function. The API call is left in if you want to use it, and the code on the sample side is commented so you can make use of it. It is adapted from code found in the Unity docs. I would not recommend hosting Unity out of process though.

### API/Event dispatch

On WebGL the normal Unity mechanisms are used to dispatch events and API calls.

On Windows however there is no such standard mechanism in place. So I focused on creating a simple API that you could use from any language (WDFUSE uses some pretty old stuff). As such the API is called by POSTing an encoded query stirng form to a HTTP server. Each API endpoint is a separate URL. No data is returned from the HTTP server itself (to match WebGL behavior).

Instead, you can call a SetEventListener API and provide the URL for your own web server. This is POSTed to in the same way as events happen, where the URL is the event name, and a encoded query string form is used to pass data. The C# weapper script already sets this up for you and you can see how it works if you need to port it to another language.

HTTP was picked as it's fairly simple and easy enough to implement the bare minimum from scratch to get it working both directions, even if the framework you're using doesn't already provide implementations.

## Build

Building is not required, it's recommended you take one of the provided samples and use it as a basis for your own project.

However, you can still build the different components used for this tool yourself if you want to try on unsupported platforms or if you want to make changes to the tool for your own purposes.

### Unity Tool

First, build the File Formats DLL. Open the SLN for the file formats and do a solution build with Release, then a solution build with IL2CPP. This will drop the appropriate files in the Unity project folder.

If you want to build for WebGL, be sure to either select the LevelPreview template in Player Settings, provide your own template... or just export the built web assembly files and build something around them later...

Then you can use the build option in Unity. If you only include the LevelPreview scene you will build this tool.

Currently only builds for Windows and WebGL are supported. Linux should work too but is untested.

### WebGL Sample Template

The WebGL Sample is provided as a Unity WebGL Template.

The sample itself is mostly in vanilla JS so it is easy to work with regardless of what framework you use. The wrapper script is in TypeScript.

To build the wrapper script to JavaScript, you must have node.JS installed along with NPM, then type the following commands in a command prompt in the LevelPreviewJavaScript folder:

```shell
npm install
npx webpack
```

levelPreview.js will be generated and dropped into the sample template in Assets\WebGLTemplates\LevelPreview.

Then you can build in Unity as described above. You can also rebuild in Unity after making changes to any other WebGL template file.

Note that depending on your build settings for compression and what your server supports, you may need to disable gzip compression or enable support on your server.

Copy settings.json from the git root to the build folder and configure it as you like.

Now upload those build files to a server somewhere along with the needed GOB files and away you go!

#### WinForms Sample Preview

First, build in Unity for Windows as above.

Next, open LevelPreviewDesktopSample and build for Release.

Open the folders Build\LevelPreviewWindows and LevelPreviewDesktopSample\bin\Release\net8.0-windows7.0. Copy all files and folders from the former into the latter, except the Dark Forces Showcase.exe. This file is not needed except for running "out of process" which the sample does not support.

Rename the *_Data folder to match the WinForms Exe. In this case, LevelPreviewDesktopSample_Data.

Copy settings.json from the git root to this folder as well. Configure it as you like.

Now you should be able to successfully run the sample.
