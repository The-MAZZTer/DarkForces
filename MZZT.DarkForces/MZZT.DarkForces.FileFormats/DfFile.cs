using MZZT.FileFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using File = System.IO.File;

namespace MZZT.DarkForces.FileFormats {
	/// <summary>
	/// A warning generated when loading or saving a file.
	/// </summary>
	public struct Warning {
		/// <summary>
		/// The line number of the warning (if applicable).
		/// </summary>
		public int Line;
		/// <summary>
		/// The warning message.
		/// </summary>
		public string Message;
	}

	/// <summary>
	/// A common interface for all DF file types.
	/// </summary>
	public interface IDfFile : IFile {
		/// <summary>
		/// Warnings generated the last time the file was loaded or saved.
		/// </summary>
		IEnumerable<Warning> Warnings { get; }
	}

	public static class DfFile {
		/// <summary>
		/// Map of type names to the internal types used.
		/// </summary>
		private readonly static IReadOnlyDictionary<string, Type> FileTypes = new Dictionary<string, Type>() {
			[".3DO"] = typeof(Df3dObject),
			[".BM"] = typeof(DfBitmap),
			["BRIEFING.LST"] = typeof(DfBriefingList),
			[".CMP"] = typeof(DfColormap),
			["CUTMUSE.TXT"] = typeof(DfCutsceneMusicList),
			["CUTSCENE.LST"] = typeof(DfCutsceneList),
			[".FME"] = typeof(DfFrame),
			[".FNT"] = typeof(DfFont),
			[".GMD"] = typeof(DfGeneralMidi),
			[".GOB"] = typeof(DfGobContainer),
			[".GOL"] = typeof(DfLevelGoals),
			[".INF"] = typeof(DfLevelInformation),
			[".LFD"] = typeof(LandruFileDirectory),
			[".MSG"] = typeof(DfMessages),
			[".O"] = typeof(DfLevelObjects),
			["JEDI.LVL"] = typeof(DfLevelList),
			[".LEV"] = typeof(DfLevel),
			[".PAL"] = typeof(DfPalette),
			[".VOC"] = typeof(CreativeVoice),
			[".VUE"] = typeof(AutodeskVue),
			[".WAX"] = typeof(DfWax),
			[".ANIM"] = typeof(LandruAnimation),
			[".ANM"] = typeof(LandruAnimation),
			[".DELT"] = typeof(LandruDelt),
			[".DLT"] = typeof(LandruDelt),
			[".FILM"] = typeof(LandruFilm),
			[".FLM"] = typeof(LandruFilm),
			[".FONT"] = typeof(LandruFont),
			[".FON"] = typeof(LandruFont),
			[".GMID"] = typeof(DfGeneralMidi),
			[".GMD"] = typeof(DfGeneralMidi),
			[".PLTT"] = typeof(LandruPalette),
			[".PLT"] = typeof(LandruPalette),
			[".VOIC"] = typeof(CreativeVoice)
		};
	
		public static Type DetectFileTypeByName(string path) {
			path = Path.GetFileName(path).ToUpper();
			if (FileTypes.TryGetValue(Path.GetExtension(path), out Type type)) {
				return type;
			}
			if (FileTypes.TryGetValue(path, out type)) {
				return type;
			}
			return typeof(Raw);
		}

		public static async Task<IFile> GetFileFromFolderOrContainerAsync(string path) {
			Type type = DetectFileTypeByName(path);
			if (type == null) {
				return null;
			}

			if (File.Exists(path)) {
				IFile file = (IFile)Activator.CreateInstance(type);
				await file.LoadAsync(path);
				return file;
			}
			string folder = Path.GetDirectoryName(path);
			if (File.Exists(folder)) {
				switch (Path.GetExtension(folder).ToLower()) {
					case ".gob":
						using (FileStream gobStream = new(folder, FileMode.Open, FileAccess.Read, FileShare.Read)) {
							DfGobContainer gob = await DfGobContainer.ReadAsync(gobStream, false);
							return await gob.GetFileAsync(Path.GetFileName(path), type, gobStream);
						}
					case ".lfd": {
						IFile data = null;
						LandruFileDirectory lfd = await LandruFileDirectory.ReadAsync(folder, async x => {
							data = await x.GetFileAsync(Path.GetFileNameWithoutExtension(path), Path.GetExtension(path).Substring(1));
						});
						return data;
					}
				}
			}

			return null;
		}

		public static async Task<T> GetFileFromFolderOrContainerAsync<T>(string path) where T : File<T>, IFile, new() {
			if (File.Exists(path)) {
				return await MZZT.FileFormats.File.ReadAsync<T>(path);
			}
			string folder = Path.GetDirectoryName(path);
			if (File.Exists(folder)) {
				switch (Path.GetExtension(folder).ToLower()) {
					case ".gob":
						using (FileStream gobStream = new(folder, FileMode.Open, FileAccess.Read, FileShare.Read)) {
							DfGobContainer gob = await DfGobContainer.ReadAsync(gobStream, false);
							return await gob.GetFileAsync<T>(Path.GetFileName(path), gobStream);
						}
					case ".lfd": {
						T data = null;
						LandruFileDirectory lfd = await LandruFileDirectory.ReadAsync(folder, async x => {
							data = await x.GetFileAsync<T>(Path.GetFileNameWithoutExtension(path), Path.GetExtension(path).Substring(1));
						});
						return data;
					}
				}
			}

			return null;
		}
	}

	/// <summary>
	/// Abstract generic class for all DF file types.
	/// </summary>
	/// <typeparam name="T">The concrete file type.</typeparam>
	public abstract class DfFile<T> : File<T>, IDfFile where T : File<T>, new() {
		private readonly List<Warning> warnings = new();
		/// <summary>
		/// Warnings generated the last time the file was loaded or saved.
		/// </summary>
		public IEnumerable<Warning> Warnings => this.warnings;
		protected void ClearWarnings() {
			this.warnings.Clear();
		}
		protected virtual void AddWarning(string warning, int line = 0) {
			this.warnings.Add(new() {
				Line = line,
				Message = warning
			});
		}
	}
}
