# Dark Forces Library for .NET and Unity

Find out more about Star Wars Dark Forces on [Wikipedia](https://en.wikipedia.org/wiki/Star_Wars:_Dark_Forces).

Purchase a copy of the game on [Steam](https://store.steampowered.com/app/32400/STAR_WARS__Dark_Forces/)! In fact, buy that Jedi Knight Collection while you're there. They're all good games.

This toolkit is intended to allow for .NET/Unity developers to consume or generate Dark Forces data files, as well as provide additional tools to Unity developers to create Dark Forces clones or toolkits.

## Version Compatibility

The .NET DLL is compiled for .NET Standard 2.0 and thus is compatible with a wide array of .NET versions old and new.

The Unity specific components should be compatible with the latest versions of Unity 2020 and 2021.

## Showcase

Included in this repo is a showcase project holding all the Unity-specific code in the form of a showcase to help show it all off. A bunch of different scenes are included to showcase what is possible with the included library.

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

## Stuff I'm Working On

* Add full map generator sample to showcase.
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

Sample code for line intersections - https://github.com/Habrador/Computational-geometry (NIT License)

## Thanks

Thanks to the DF-21 Dark Forces fan community. http://www.df-21.net/
