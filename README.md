# Dark Forces Library for .NET and Unity

Find out more about Star Wars Dark Forces on [Wikipedia](https://en.wikipedia.org/wiki/Star_Wars:_Dark_Forces).

Purchase a copy of the game on [Steam](https://store.steampowered.com/app/32400/STAR_WARS__Dark_Forces/)! In fact, buy that Jedi Knight Collection while you're there. They're all good games.

This toolkit is intended to allow for .NET/Unity developers to consume or generate Dark Forces data files, as well as provide additional tools to Unity developers to create Dark Forces clones or toolkits.

## Version Compatibility

The .NET DLL is compiled for .NET Standard 2.0 and thus is compatible with a wide array of .NET versions old and new.

The Unity specific components should be compatible with the latest versions of Unity 2020 and 2021.

## Showcase

Included in this repo is a Unity project holding all the Unity-specific code in the form of a showcase to help show it all off. A bunch of different scenes are included to showcase what is possible with the included library.

You need to own a copy of Dark Forces on Steam or from the original CD release. Be sure you've installed the game on Steam or done a full install from the CD.

Upon running the showcase you may be asked to select the Dark Forces install location if an installed Steam copy can't be found. Simply find and select the DARK.GOB file using the included file browser.

From the main menu you're welcome to explore. You can add mods/level files to allow the showcase to utilize community-made content in addition to the built-in original Dark Forces content.

## Current Features

* Dark Forces file format DLLs that can be dropped into any .NET or Unity project.
* Can load all game data files, including the Landru (menu/cutscene) data files and in-game data files.
* Allows reading of GOB/LFD container files.
* Helper classes to generate materials from textures, geometry from levels, sprites from objects, models from 3DOs, animations from VUEs, audio from VOCs and GMDs.
* Tolerant to issues when loading/saving data files, will log warnings but keep going if possible.
* Tool to generate automap-like view (or level editor view) of a level.

I dropped some helper classes unrelated to Dark Forces into the Unity project which I used in the showcase. These are useful in a general sense as well.

## Files

A quick breakdown of where everything is.

**Assets\\** - The Unity code and resource files.

**Assets\\Components\\** - Unity classes relating to managing component states in the editor and at rutime. In particular interest is `Singleton` which makes it easy to manage and access a single instance of a class, and configure it to persist across scenes if desired.

**Assets\\Dark Forces\\** - Classes and assets relating to Dark Forces designed for general use, not specific to the showcase.

**Assets\\Dark Forces\\Audio\\** - GMD/GMID and VOC/VOIC playback.

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

**Assets\\Data Binding\\** - General purpose code and editor support for data binding C# objects to the Unity GameObject hierarchy. Supports databinding entire objects with overrideded subclass logic to render the objects visually, or binding individual members to form controls without needing to write custom code.

**Assets\\Data Binding\\List\\** - Databinds a list of objects to a Unity GameObject, automaticallty creating and destroying child GameObjects using a prefab template as items are added and removed from the list. I've found this code endlessly useful.

**Assets\\Data Binding\\UI Controls\\** - Databinds indivudal members of an object to specific form controls to allow for display, editing, and modifing the original object as desired.

**Assets\\Extensions\\** - .NET extension classes which bridge the gap between the library DLLs (specifically the Dark Forces File Formats DLL and SkiaSharp) and Unity, making it easier to marshal data types back and forth.

**Assets\\File Browser\\** - A Unity-based file browser. I experimented with some libraries but ultimately I want to allow for navigation inside of GOBs/LFDs for a tool I want to do so I had to roll my own. Makes heavy use of databinding classes.

**Assets\\Libraries\\** - Third-party libraries and assets.

**MZZT.DarkForces\\** - The .NET Standard library.

**MZZT.DarkForces\\MZZT.DarkForces.FileFormats\\** - The classes for all of the Dark Forces file formats.

**MZZT.DarkForces\\MZZT.FileFormats.Audio\\** - Classes for MIDI and WAV, used for testing GMID/VOC parsing via conversion.

**MZZT.DarkForces\\MZZT.FileFormats.Base\\** - Base classes for file formats, defines standard load/save methods.

**MZZT.DarkForces\\MZZT.Input.ProgramArguments\\** - Used by the showcase to parse command line arguments.

**MZZT.DarkForces\\MZZT.Steam\\** - Used by the showcase to parse Steam's library folders file.

**MZZT.DarkForces\\Test\\** - Test app used mainly for dumping files from GOBs/LFDs and testing file format parsing by converting files to modern formats. Lots of unorganized commented out code in here which can be useful, and showcases using the file formats library from .NET code.

## Reporting Bugs / Feature Requests 

Feel free to use GitHub Issues. Try to make sure any issue you have is consistently reproducable if consistent steps are followed, this will make it easier for me to help you.

Log files made by the showcase are stored in `C:\Users\\\<username\>\AppData\LocalLow\MZZT\Dark Forces Showcase\Player.log`. You can use this to try and get an error message and stack trace which usually makes things far easier for me.

I'm doing this all as an opportunity to teach myself more Unity skills and as well as a passion project. No guarantees on updates, bug fixes or features.

You're welcome to fork this project and issue pull requests.

## Stuff I'm Working On

* Super secret showcase idea (!!!)
* Add more Unity helper classes and showcases such as data import/export tool and resource viewer/playback, and cutscene browser/playback.

## Stuff I See Could Use Improvement

* Code to save all data files is present but still needs testing and bug fixing.
* Creating/modifying GOB/LFD files is very basic and could use improvement.
* Find a better MIDI soundfont. DOSBox's sounds nice...
* Add collision support to the Level Explorer. The colliders are in place, just need to implement the movement properly in the camera script.
* iMUSE support for the music to transition between stalk/fight modes.
* Load INF data in Level Explorer and do things like play ambient sounds or even add some of the INF logic in.
* Do sign textures in shader.
* Look at adapting [this](https://medium.com/@jmickle_/writing-a-doom-style-shader-for-unity-63fa13678634) for Dark Forces colormaps. Current implementation generates 32-bit textures on the CPU side.
* Add support for reading/writing the PlayStation port file formats (mostly they took the text-based file formats like 3DOs and made binary formats). Would be fun to see if mods could be converted to run on the PlayStation port.

## Crazy Ideas You Could Make With This, Maybe

* Tool which adapts any VUE to adjust it for a different start or end point, and adjusts the VUE to be relative to that point.
* Tool which adds additional frames to VUEs in between existing ones to make the animation better (can Dark Forces support this?).
* Tool to automatically generate Alt Y 3DOs based on selected sector shape and desired thickness of platform, and textures to use.
* Tool which detects lines-of-sight in a level that would result in HoM effect in Dark Forces (draw distance or over 40 portals in a row at once).
* Tool which detects overlapping geometry.
* Tool which converts Doom resources to Dark Forces or vice versa.
* Unity-based Level Editor for Dark Forces levels with real-time 3D preview.
* Tool to convert Dark Forrces levels to other game engines. CTs rescuing hostages on SECBASE? Sure, why not?
* Bring mousebots 3DOs and the joy they provide to all games.
* Three words: Dark Forces Randomizer
* Tool to create and preview Landru cutscenes.

## License Stuff / Included Dependencies / Acknowledgements 

This repo is licensed under the MIT license, with the exception of any dependencies which use different licenses.

These dependencies only apply to the Unity showcase project. Thanks to the developers of these projects who have made this project a little easier.

SkiaSharp - https://github.com/mono/SkiaSharp (MIT License)

Material Design Icons - https://github.com/google/material-design-icons/tree/master/font (Apache 2.0 License)

CSharpSynthForUnity - https://github.com/kewlniss/CSharpSynthForUnity (MIT License)

Sample code for line intersections - https://github.com/Habrador/Computational-geometry (MIT License)

## Thanks

Thanks to the DF-21 Dark Forces fan community. http://www.df-21.net/
