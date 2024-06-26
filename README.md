# Dark Forces Showcase

Find out more about Star Wars Dark Forces on [Wikipedia](https://en.wikipedia.org/wiki/Star_Wars:_Dark_Forces).

Purchase a copy of the game on [Steam](https://store.steampowered.com/app/2292260/STAR_WARS_Dark_Forces_Remaster/)! In fact, buy that Jedi Knight Collection while you're there. They're all good games.

This showcase contains my Dark Forces libraries for .NET and Unity.

It contains:
* A file format DLL (with full source code, in the MZZT.DarkForces folder) for generating and/or consuming all Dark Forces PC file formats.
* Some Unity scripts and assets (in Assets\Dark Forces) for using Dark Forces assets files in Unity projects.
* Some Unity scripts and assets (in Assets\Dark Forces Showcase) for demonstrating how the previous items can be used in a sample project, specifically for creating some (hopefully) useful tools.

## Usage

You can use the live web version here: https://the-mazzter.github.io/DarkForcesSite/

Or you can download and run on your PC.

For Windows, extract the release ZIP file anywhere you want on disk, and run the Dark Forces Showcase EXE.

For Linux, extract the TAR.GZ and run the .x86_64 binary file.

For WebAssembly, if you want to reproduce the website version for some reason, throw the files up on a web server. The web server must support gzip decompression of GZ files (you can create your own build with that optiond isabled if it doens't; see Unity docs for more details).

WebAssembly has extra directions included in a dialog that appears on start.

If the showcase cannot detect your Dark Forces installation, you will be presented with a file browser. Find and select your DARK.GOB file in the installation folder.

You will then see the main menu. You can select any of the options in the main list to see a description of each tool. Click Run in the lower right to use the currently selected tool.

You can use the Mod button to load mod files to use the showcase with them.

I'll leave everything else up to you to explore and discover!

## Level Preview Usage

The Level Preview tool is intended for embedding in web or desktop applications, and is a separate download. The documentation is [here](LevelPreview.md).

## Dark Forces Compatibility

This tool is compatible with the DOS version of Dark Forces and the Steam remaster.

This tool cannot yet read any of the enhanced file formats added to the remaster. However you can still read quite a bit including browsing the AVENGER level.

Simply load in the extra GOBs as a mod into this tool. EXTRAS.GOB contains AVENGER. ENHANCED.GOB contains mostly new files; you can't view most of them but can export them. OVERRIDE.GOB contains a few files altered for presumably legal purposes.

## Version Compatibility

The .NET DLL is compiled for .NET Standard 2.0 and thus is compatible with a wide array of .NET versions old and new.

See [the official .NET documentation](https://docs.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-0) for more information on compatibility.

The Unity-specific components are perpetually updated to be compatible with the latest versions of Unity LTS. They will probably work fine on older versions back to 2020 but no promises.

## Build

### File Format DLL

This DLL should work fine on any .NET Core target platform.

To build, open the solution file in the MZZT.DarkForces folder with Visual Studio 2022 (previous versions may work as well) and build the MZZT.DarkForces.FileFormats project for Release AnyCPU (use the dropdown at the top of Visual Studio which may say Debug or Release to switch). The DLL files will be output in Assets\Dark Forces\File Formats, ready for use in Unity, which should now be able to open and run the project successfully. You can also take the DLLs from there and use them in other projects if you want.

If you are making a WebGL build you should build for IL2CPP AnyCPU instead, as some workarounds for IL2CPP bugs need to be applied. These will only be applied to WebGL builds. These files will be output to the Assets\Dark Forces\File Formats\IL2CPP folder.

### Unity Project

To build the Unity showcase project, first ensure you built the file format DLL in Release mode otherwise the Unity project will be unable to run or build. If you are making a WebGL build be sure to build IL2CPP configuration as well.

Then you can open the checkout folder using Unity Editor 2022.3.

If building for WebGL, you may want to check the selected template. If building the LevelPreview you may wish to make your own template or just export the built files from the existing template and build your own around it, although a sample LevelPreview template is also provided. But for main tool builds you will want to select the proper DarkForcesShowcase template.

Build using the File > Build Settings... menu option in Unity Editor.

Include all scenes except LevelPreview to build the Dark Forces Showcase. Include only the LevelPreview scene to build that tool.

Currently supported platforms include WebGL, Linux, and Windows. Other platforms may or may not work.

### WebGL Template

There are two included templates; this deals with the Dark Forces Showcase template. The Level Preview template is vanilla JS apart from the Level Preview script itself. See the LevelPreview.md document for details.

A build of the main WebGL template is included in the repo but you can regenerate it with the Angular build tools.

You will need npm installed. First, go to the WebGLTemplate folder and run `npm install` to pull down all the required packages.

Then, use `npm install -g @angular/cli` to get the angular build tools. Then run `npx ng build`  to drop a new build into the Assets folder.

For PWA support you may want to manually update the built-in ServiceWorker.js files to add in the three JS and one CSS file generated by Angular. The names are randomized each build.

## Contents

Included in this repo is a Unity project holding all the Unity-specific code in the form of a showcase to help show it all off. A bunch of different scenes are included to showcase what is possible with the included library.

You need to own a copy of Dark Forces on Steam or from the original CD release. Be sure you've installed the game on Steam or done a full install from the CD.

Upon running the showcase you may be asked to select the Dark Forces install location if an installed Steam copy can't be found. Simply find and select the DARK.GOB file using the included file browser.

From the main menu you're welcome to explore the different tools to see what capabilites are included in the code. You can add mods/level files to explore community-made content in addition to the original Dark Forces content.

All the tools have been tested with the default Dark Forces content. Mods may or may not work.

## Features

* Dark Forces file format DLLs that can be dropped into any .NET or Unity project to provide support for reading and writing many Dark Forces file formats, including cutscene and container file formats.
* Helper classes to generate materials from textures, geometry from levels, sprites from objects, models from 3DOs, animations from VUEs, audio from VOCs and GMDs, and more.
* Tolerant to issues when loading/saving data files, will log warnings but keep going if possible.
* Generate automap-like views (or level editor view) of a level.
* Randomizer! (randomize enemy/object placement, modify basic options of levels) 
* Dump resources from GOBs/LFDs, and/or convert them to modern formats (PNG/MIDI/WAV).
* Full-blown editing tools for many DF file formats (textures, sprites, animations, audio, and more!)
* Basic level geometry and object renderer.
* Embeddable customizable version of the renderer for websites and desktop applications!
* Builds for Windows, Linux, and WebGL. Probably works for other platforms as long as you can provide a keyboard and mouse.

I dropped some helper classes unrelated to Dark Forces into the Unity project which I used in the showcase. These may be useful to unrelated projects as well.

## Files

A quick breakdown of where everything is.

**Assets\\** - The Unity code and resource files.

**Assets\\Components\\** - Unity classes relating to managing component states in the editor and at rutime. In particular interest is `Singleton` which makes it easy to manage and access a single instance of a class, and configure it to persist across scenes if desired.

**Assets\\Dark Forces\\** - Classes and assets relating to Dark Forces designed for general use, not specific to the showcase.

**Assets\\Dark Forces\\Audio\\** - GMD/GMID and VOC/VOIC playback.

**Assets\\Dark Forces\\Converters\\** - Classes to convert Dark Forces image formats into Unity texture objects, where you can then use them in Unity or export to PNG or other formats. Also has palette converter classes as well.

**Assets\\Dark Forces\\File Formats\\** - The drop folder for the .NET Standard Dark Forces library DLLs and dependencies. See below for more details on each DLL.

**Assets\\Dark Forces\\Geometry\\** - Level geometry generation and rendering

**Assets\\Dark Forces\\Loading\\** - These classes wrap the file formats DLL. Adds Unity-specific functionality for finding files, caching loaded files, and converting to Unity assets at runtime.

**Assets\\Dark Forces\\Objects\\** - Rendering Dark Forces objects including frames, WAX/sprites, 3DOs, and playing VUEs.

**Assets\\Dark Forces\\Shaders\\** - Texture shaders for Dark Forces level/object rendering.

**Assets\\Dark Forces\\Shaders\\Color.shader** - Solid color texture, used for 3DOs.

**Assets\\Dark Forces\\Shaders\\Plane.shader** - Sky/pit pixel shader.

**Assets\\Dark Forces\\Shaders\\Plane test.shader** - An attempt to write a less expensive vertex shader of the plane shader (doesn't work right).

**Assets\\Dark Forces\\Shaders\\Simple.shader** - Shader for normal geometry textures and sprites.

**Assets\\Dark Forces\\Shaders\\Transparent.shader** - Shader for walls with cutouts (if I apply this to everywhere Simple shader is used weird Z order things happen so I separated it).

**Assets\\Dark Forces Showcase\\** - All the showcase specific code and assets which will be less useful in other projects. This includes all the scenes.

**Assets\\Dark Forces Showcase\\LevelExplorer\\** - Code and assets relating to the Level Explorer tool.

**Assets\\Dark Forces Showcase\\LevelPreview\\** - Code and assets relating to the Level Preview tool.

**Assets\\Dark Forces Showcase\\Map Generator\\** - Code and assets relating to the Map Generator tool.

**Assets\\Dark Forces Showcase\\Menu\\** - Code and assets relating to the Main Menu.

**Assets\\Dark Forces Showcase\\Mod Dialog\\** - Code and assets relating to the Mod configuration dialog.

**Assets\\Dark Forces Showcase\\Randomizer\\** - Code and assets relating to the Randomizer tool.

**Assets\\Dark Forces Showcase\\Resource Dumper\\** - Code and assets relating to the Resource Dumper tool.

**Assets\\Dark Forces Showcase\\Resource Editor\\** - Code and assets relating to the Resource Editor tool.

**Assets\\Data Binding\\** - General purpose code and editor support for data binding C# objects to the Unity GameObject hierarchy. Supports databinding entire objects with overrideded subclass logic to render the objects visually, or binding individual members to form controls without needing to write custom code.

**Assets\\Data Binding\\List\\** - Databinds a list of objects to a Unity GameObject, automaticallty creating and destroying child GameObjects using a prefab template as items are added and removed from the list. I've found this code endlessly useful.

**Assets\\Data Binding\\UI Controls\\** - Databinds indivudal members of an object to specific form controls to allow for display, editing, and modifing the original object as desired.

**Assets\\Extensions\\** - .NET extension classes which bridge the gap between the library DLLs and Unity, making it easier to marshal data types back and forth.

**Assets\\File Browser\\** - A Unity-based file browser. I experimented with some libraries but ultimately I want to allow for navigation inside of GOBs/LFDs for a tool I want to do so I had to roll my own. Makes heavy use of databinding classes.

**Assets\\Libraries\\** - Third-party libraries and assets.

**Assets\\UI\\** - Misc. UI classes.

**Assets\\WebGLTemplates\\** - The templates used for WebGL builds of the main tool and the Level Preview tool.

**LevelPreviewDesktopSample\\** - The source code for the Level Preview WinForms sample.

**LevelPreviewJavaScript\\** - The source code for the Level Preview JavaScript wrapper class. The sample project itself is in the WebGL template.

**MZZT.DarkForces\\** - The .NET Standard library.

**MZZT.DarkForces\\MZZT.DarkForces.FileFormats\\** - The classes for all of the Dark Forces file formats.

**MZZT.DarkForces\\MZZT.FileFormats.Audio\\** - Classes for MIDI and WAV, used for testing GMID/VOC parsing via conversion.

**MZZT.DarkForces\\MZZT.FileFormats.Base\\** - Base classes for file formats, defines standard load/save methods.

**MZZT.DarkForces\\MZZT.Input.ProgramArguments\\** - Used by the showcase to parse command line arguments.

**MZZT.DarkForces\\MZZT.Steam\\** - Used by the showcase to parse Steam's library folders file.

**MZZT.DarkForces\\Test\\** - Test app used to quickly debug file format DLL issues. Some useful code snippets may still be in here.

**WebGLTemplate\\** - Angular source code for the main WebGL template.

## Reporting Bugs / Feature Requests 

Feel free to use GitHub Issues. Try to make sure any issue you have is reproducable if consistent steps are followed, this will make it easier for me to help you.

Log files made by the showcase are stored in `C:\Users\\\<username\>\AppData\LocalLow\MZZT\Dark Forces Showcase\Player.log`. You can use this to try and get an error message and stack trace when an issue occurs which usually makes things far easier for me.

I'm doing this all as a passion project. No guarantees on updates, bug fixes or features.

You're welcome to fork this project. You may issue pull requests for things like bug fixes, but if you're taking this code to use for some other project it may not make sense to fold the code back in.

## Future Plans

I want to add more tools such as:
* Cutscene player
* Catwalk 3DO generator (takes a sector and generates a 3DO to precisely fit it, and allows you to choose textures for it, then adds it to your level for you).
* Resource validator (verify file format library can load/save files properly, determine compression ratio when resaving files)

I want to bring this showcase to other platforms such as WebAssembly. This will require refactoring the code base to remove direct disk access and replacing it with web browser-based file selection.

## Lower Priority Tasks

* Find a better MIDI soundfont. DOSBox's sounds nice...
* iMUSE support for the music to transition between stalk/fight modes. No idea how this works in practice though. MIDI/GMID can have "markers" in the music, which IIRC is how it is done, but I've never dug into it. In addition there's the question of if the library I'm using can even support that.
* Load INF data in Level Explorer and do things like play ambient sounds or even add some of the INF logic in.
* In the Level Explorer, do sign textures in shader. Currently they're rendered on a separate polygon slighty in front. This means culling is not done properly if the sign overlaps the edge of the wall.
* Look at adapting [this](https://medium.com/@jmickle_/writing-a-doom-style-shader-for-unity-63fa13678634) for Dark Forces colormaps. Current implementation generates 32-bit textures on the CPU side.
* Add support for reading/writing the PlayStation port file formats (mostly they took the text-based file formats like 3DOs and developed binary formats that otherwise are very similar). Would be fun to see if mods could be converted to run on the PlayStation port.

## Used In Other Project

* LevelExplorer was converted into [an editor map preview](https://github.com/df21-net/DarkForcesRenderer) for [WDFUSE](https://github.com/df21-net/editor) by [Karjala22](https://github.com/Karjala22).

## License Stuff / Included Dependencies / Acknowledgements 

This repo is licensed under the MIT license, with the exception of any dependencies which use different licenses.

Thanks to the developers of these projects who have made this project a little easier.

See LICENSE file for more details on the projects used, their source locations, and licenses.

## Thanks

Thanks to the [DF-21 Dark Forces fan community](http://www.df-21.net/).
