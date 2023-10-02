# Dark Forces Showcase

Find out more about Star Wars Dark Forces on [Wikipedia](https://en.wikipedia.org/wiki/Star_Wars:_Dark_Forces).

Purchase a copy of the game on [Steam](https://store.steampowered.com/app/32400/STAR_WARS__Dark_Forces/)! In fact, buy that Jedi Knight Collection while you're there. They're all good games.

This showcase contains my Dark Forces libraries for .NET and Unity.

It contains:
* A file format DLL (with full source code, in the MZZT.DarkForces folder) for generating and/or consuming all Dark Forces PC file formats.
* Some Unity scripts and assets (in Assets\Dark Forces) for using Dark Forces assets files in Unity projects.
* Some Unity scripts and assets (in Assets\Dark Forces Showcase) for demonstrating how the previous items can be used in a sample project, specifically for creating some (hopefully) useful tools.

## Version Compatibility

The .NET DLL is compiled for .NET Standard 2.0 and thus is compatible with a wide array of .NET versions old and new.

See [the official .NET documentation](https://docs.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-0) for more information on compatibility.

The Unity-specific components are perpetually updated to be compatible with the latest versions of Unity LTS. They will probably work fine on older versions back to 2020 but no promises.

## Build

### File Format DLL

This DLL should work fine on any .NET Core target platform.

To build, open the solution file in the MZZT.DarkForces folder with Visual Studio 2022 (previous versions may work as well) and build the MZZT.DarkForces.FileFormats project for Release AnyCPU (use the dropdown at the top of Visual Studio which may say Debug or Release to switch). The DLL files will be output in Assets\Dark Forces\File Formats, ready for use in Unity, which should now be able to open and run the project successfully. You can also take the DLLs from there and use them in other projects if you want.

### Unity Project

Ensure you build the file format DLL in Release mode otherwise the Unity project will be unable to run or build.

To build the Unity showcase project, first build the file format DLL as above. Then you can open the checkout folder using Unity Editor 2022.3. You can then build using the File > Build Settings... menu option in Unity Editor.

Currently for future releases development is ongoing to make things more cross-platform. For now it will only build for Windows.

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

**Assets\\Dark Forces Showcase\\Map Generator\\** - Code and assets relating to the Map Generator tool.

**Assets\\Dark Forces Showcase\\Menu\\** - Code and assets relating to the Main Menu.

**Assets\\Dark Forces Showcase\\Mod Dialog\\** - Code and assets relating to the Mod configuration dialog.

**Assets\\Dark Forces Showcase\\Randomizer\\** - Code and assets relating to the Randomizer tool.

**Assets\\Dark Forces Showcase\\Resource Dumper\\** - Code and assets relating to the Resource Dumper tool.

**Assets\\Dark Forces Showcase\\Resource Editor\\** - Code and assets relating to the Resource Editor tool.

**Assets\\Data Binding\\** - General purpose code and editor support for data binding C# objects to the Unity GameObject hierarchy. Supports databinding entire objects with overrideded subclass logic to render the objects visually, or binding individual members to form controls without needing to write custom code.

**Assets\\Data Binding\\List\\** - Databinds a list of objects to a Unity GameObject, automaticallty creating and destroying child GameObjects using a prefab template as items are added and removed from the list. I've found this code endlessly useful.

**Assets\\Data Binding\\UI Controls\\** - Databinds indivudal members of an object to specific form controls to allow for display, editing, and modifing the original object as desired.

**Assets\\Extensions\\** - .NET extension classes which bridge the gap between the library DLLs (specifically the Dark Forces File Formats DLL and SkiaSharp) and Unity, making it easier to marshal data types back and forth.

**Assets\\File Browser\\** - A Unity-based file browser. I experimented with some libraries but ultimately I want to allow for navigation inside of GOBs/LFDs for a tool I want to do so I had to roll my own. Makes heavy use of databinding classes.

**Assets\\Libraries\\** - Third-party libraries and assets.

**Assets\\UI\\** - Misc. UI classes.

**MZZT.DarkForces\\** - The .NET Standard library.

**MZZT.DarkForces\\MZZT.DarkForces.FileFormats\\** - The classes for all of the Dark Forces file formats.

**MZZT.DarkForces\\MZZT.FileFormats.Audio\\** - Classes for MIDI and WAV, used for testing GMID/VOC parsing via conversion.

**MZZT.DarkForces\\MZZT.FileFormats.Base\\** - Base classes for file formats, defines standard load/save methods.

**MZZT.DarkForces\\MZZT.Input.ProgramArguments\\** - Used by the showcase to parse command line arguments.

**MZZT.DarkForces\\MZZT.Steam\\** - Used by the showcase to parse Steam's library folders file.

**MZZT.DarkForces\\Test\\** - Test app used to quickly debug file format DLL issues. Some useful code snippets may still be in here.

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
* Dedicated embeddable 3D level preview tool (push new level data to it dynamically, embed it anywhere, etc)

I want to bring this showcase to other platforms such as WebAssembly. This will require refactoring the code base to remove direct disk access and replacing it with web browser-based file selection.

## Lower Priority Tasks

* Improve compatibility with cross-platform (remove direct filesystem access, etc)
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
