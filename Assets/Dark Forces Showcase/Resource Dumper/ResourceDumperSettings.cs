using System;
using System.Collections.Generic;
using System.IO;
using Color = UnityEngine.Color;

namespace MZZT.DarkForces.Showcase {
	public class ResourceDumperSettings {
		public List<string> Inputs { get; set; } = new();

		public bool AlwaysScanInsideGobs { get; set; } = true;
		public bool AlwaysScanInsideLfds { get; set; } = true;
		public ResourceTypes ProcessTypes { get; set; } = ResourceTypes.All;

		public bool OutputCopyOfInput { get; set; } = false;
		public ResourceTypes ConvertToPng { get; set; } = ResourceTypes.Anim | ResourceTypes.Bm | ResourceTypes.Delt | ResourceTypes.Fme | ResourceTypes.Fnt |
			ResourceTypes.Font | ResourceTypes.Wax;
		public string ImageConversionPal { get; set; } = "SECBASE";
		public string ImageConversionPltt { get; set; } = null;
		public ImageConversionPaletteModes ImageConversionPaletteMode { get; set; } = ImageConversionPaletteModes.TypicalPalette;
		public byte ImageConversionLightLevelMinimum { get; set; } = 31;
		public byte ImageConversionLightLevelMaximum { get; set; } = 31;
		public Color FontColor { get; set; } = Color.white;
		public WaxOutputModes WaxOutputMode { get; set; } = WaxOutputModes.Shortcut;
		public bool ConvertVocToWav { get; set; } = true;
		public int MaximumUnrolledVocLoopIterations { get; set; } = 2;
		public bool ConvertGmdToMid { get; set; } = true;
		public bool ConvertPalPlttToJascPal { get; set; } = true;
		public bool ConvertPalPlttTo24BitPal { get; set; } = false;
		public bool ConvertPalPlttTo32BitPal { get; set; } = false;
		public bool ConvertCmpToJascPal { get; set; } = true;
		public bool ConvertCmpTo24BitPal { get; set; } = false;
		public bool ConvertCmpTo32BitPal { get; set; } = false;
		public bool ConvertFntFontToSingleImage { get; set; } = true;
		public bool ConvertFntFontToCharacterImages { get; set; } = false;

		public string BaseOutputFolder { get; set; } = null;
		public string BaseOutputFormat { get; set; } = $@"{{output}}{Path.DirectorySeparatorChar}{{inputpath}}{Path.DirectorySeparatorChar}{{file}}";
		public bool PreferThreeCharacterExtensions { get; set; } = false;
		public string ConvertedImageFilenameFormat { get; set; } = @"{inputname}.{inputext}-{palette}{lightlevel}.{outputext}";
		public string ConvertedBmFilenameFormat { get; set; } = @"{inputname}.{inputext}-{palette}{lightlevel}-{index}.{outputext}";
		public string ConvertedAnimFilenameFormat { get; set; } = $@"{{inputname}}.{{inputext}}-{{palette}}{Path.DirectorySeparatorChar}{{index}}.{{outputext}}";
		public string ConvertedWaxFilenameFormat { get; set; } = $@"{{inputname}}.{{inputext}}-{{palette}}{{lightlevel}}{Path.DirectorySeparatorChar}{{wax}}.{{sequence}}.{{frame}}.{{outputext}}";
		public string ConvertedPalPlttFilenameFormat { get; set; } = @"{inputname}.{inputext}.{format}{lightlevel}.{outputext}";
		public string ConvertedFntFontFilenameFormat { get; set; } = $@"{{inputname}}.{{inputext}}-{{palette}}{{lightlevel}}{Path.DirectorySeparatorChar}{{character}}.{{outputext}}";
		public string ConvertedVocFilenameFormat { get; set; } = @"{inputname}{index}.{outputext}";
		public string MiscFilenameFormat { get; set; } = @"{inputname}.{outputext}";
	}

	[Flags]
	public enum ResourceTypes : uint {
		ThreeDo = 0x1,
		Anim = 0x2,
		Bm = 0x4,
		BriefingLst = 0x8,
		Cmp = 0x10,
		CutmuseTxt = 0x20,
		CutsceneLst = 0x40,
		Delt = 0x80,
		Film = 0x100,
		Fme = 0x200,
		Fnt = 0x400,
		Font = 0x800,
		Gmd = 0x1000,
		Gol = 0x2000,
		Inf = 0x4000,
		JediLvl = 0x8000,
		Lev = 0x10000,
		Msg = 0x20000,
		O = 0x40000,
		Pal = 0x80000,
		Pltt = 0x100000,
		Voc = 0x200000,
		Vue = 0x400000,
		Wax = 0x800000,

		OtherInGob = 0x40000000,
		OtherInLfd = 0x80000000,

		All = 0xFFFFFFFF
	}

	[Flags]
	public enum ImageConversionPaletteModes {
		TypicalPalette,
		Pal,
		Cmp,
		Both
	}

	public enum WaxOutputModes {
		NoDuplicates,
		Duplicate,
		Shortcut,
		SymLink,
		HardLink
	}
}
