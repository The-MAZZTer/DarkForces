using Microsoft.Win32;
using MZZT.DarkForces.FileFormats;
using MZZT.Extensions;
using MZZT.Steam;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace MZZT.DarkForces {
	/// <summary>
	/// A class to assist in loading data from GOB and LFD files.
	/// </summary>
	public class FileLoader : Singleton<FileLoader> {
		/// <summary>
		/// The standard files which should be searched for GOB data files.
		/// </summary>
		public static readonly string[] DARK_FORCES_STANDARD_DATA_FILES = new[] {
			@"DARK.GOB",
			@"SOUNDS.GOB",
			@"SPRITES.GOB",
			@"TEXTURES.GOB",
			@"LOCAL.MSG"
		};

		private struct ResourceLocation {
			public string FilePath;
			public long Offset;
			public long Length;
		}

		private struct LfdInfo {
			public string LfdPath;
			public Dictionary<string, ResourceLocation> Files;
		}

		[SerializeField, Header("Folders")]
		private string darkForcesFolder;
		/// <summary>
		/// The Dark Forces game folder used as a base search path for data.
		/// </summary>
		public string DarkForcesFolder { get => this.darkForcesFolder; set => this.darkForcesFolder = value; }

		/// <summary>
		/// Try and autodetect the Dark Forces folder.
		/// </summary>
		/// <returns>The folder found, or null if not.</returns>
		public async Task<string> LocateDarkForcesAsync() {
			// TODO Add support for my Knight launcher, which stores the DF path in the regsitry.
			// I haven't run it in forever and my own path it stored is now wrong so meh.
			// We could also leverage Knight data to make picking mods easier since it knows what files
			// a mod needs and which files they override.

			// Try and find Steam and see if DF is installed there.
			string path = null;
			try {
				using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam");
				path = (string)key?.GetValue("SteamPath");
			} catch (PlatformNotSupportedException) {
			}
			if (path == null) {
				return null;
			}
			path = path.Replace('/', Path.DirectorySeparatorChar);

			// Is Dark Forces installed in the Steam folder?
			if (File.Exists(Path.Combine(path, "SteamApps", "common", "Dark Forces", "Game", "DARK.GOB"))) {
				return Path.Combine(path, "SteamApps", "common", "Dark Forces", "Game");
			}

			// Check other library folders. Start by reading the list of library folders.
			ValveDefinitionFile libraryFoldersVdf =
				await ValveDefinitionFile.ReadAsync(Path.Combine(path, "SteamApps", "libraryfolders.vdf"));

			// Normally we could deserialize into a type but the format of this file doesn't work well for that.
			// But we can read the tokens by hand.

			// Start after the root { and end before the closing }.
			int pos = 2;
			int len = libraryFoldersVdf.Tokens.Count - 1;
			List<string> libraryFolders = new List<string>();
			while (pos < len) {
				ValveDefinitionFile.Token token = libraryFoldersVdf.Tokens[pos];
				pos++;
				// This should be an int for the index of the library folder.
				string property = (token as ValveDefinitionFile.StringToken)?.Text;
				if (property == null) {
					break;
				}

				if (pos >= len) {
					break;
				}

				// Advance until we find another string token, which will be the actual library folder path.
				do {
					token = libraryFoldersVdf.Tokens[pos];
					pos++;
				} while (pos < len && (token is ValveDefinitionFile.AssignmentToken ||
					token is ValveDefinitionFile.CommentToken ||
					token is ValveDefinitionFile.ConditionToken));

				string value = (token as ValveDefinitionFile.StringToken)?.Text;
				if (value == null) {
					break;
				}

				// Check to see if the key is actually a number just to be sure.
				if (int.TryParse(property, out int _)) {
					libraryFolders.Add(value);
				}
			}

			foreach (string libraryFolder in libraryFolders) {
				if (File.Exists(Path.Combine(libraryFolder, "SteamApps", "common", "Dark Forces", "Game", "DARK.GOB"))) {
					return Path.Combine(libraryFolder, "SteamApps", "common", "Dark Forces", "Game");
				}
			}

			return null;
		}

		private readonly Dictionary<string, List<ResourceLocation>> gobMap = new Dictionary<string, List<ResourceLocation>>();
		private readonly Dictionary<string, LfdInfo> lfdOverrides = new Dictionary<string, LfdInfo>();
		private readonly Dictionary<string, string[]> gobFiles = new Dictionary<string, string[]>();

		/// <summary>
		/// Clear all cached data.
		/// </summary>
		public void Clear() {
			this.gobFiles.Clear();
			this.gobMap.Clear();
			this.lfdOverrides.Clear();
		}

		/// <summary>
		/// GOB files we know the contents of.
		/// </summary>
		public IEnumerable<string> Gobs => this.gobFiles.Keys;

		/// <summary>
		/// Reads in a GOB file and tracks the files inside of it so we can quickly find them later.
		/// </summary>
		/// <param name="path">Path to the GOB file.</param>
		public async Task AddGobFileAsync(string path) {
			if (this.DarkForcesFolder != null) {
				path = Path.Combine(this.DarkForcesFolder, path);
			}
			string key = path.ToUpper();
			// Skip if we already loaded it.
			if (this.gobFiles.ContainsKey(key)) {
				return;
			}

			switch (Path.GetExtension(key)) {
				case ".GOB": {
					DfGobContainer gob = await DfGobContainer.ReadAsync(path, false);
					List<string> files = new List<string>();
					foreach ((string name, uint offset, uint size) in gob.Files) {
						files.Add(name.ToUpper());
						// Track the GOB, offset, and size of every file.
						this.AddToGobMap(name, new ResourceLocation() {
							FilePath = path,
							Offset = offset,
							Length = size
						});
					}
					this.gobFiles[key] = files.ToArray();
				} break;
				// I used to lump GOBs and LFDs together but LFDs work differently so they're separate now.
				case ".LFD": /*{
					LandruFileDirectory lfd = await LandruFileDirectory.ReadAsync(path);
					List<string> files = new List<string>();
					foreach ((string name, string type, uint offset, uint size) in lfd.Files) {
						files.Add(name.ToUpper());
						this.AddToMap($"{name}.{type}", new ResourceLocation() {
							FilePath = path,
							Offset = offset,
							Length = size,
							Priority = priority
						});
					}
					this.files[path] = files.ToArray();
				} break;*/
					throw new NotSupportedException();
				default: {
					// An individual file, add it to the tracking list.
					string file = Path.GetFileName(path);
					// It's not in a GOB so offset is 0 and size is the full file size.
					this.AddToGobMap(file, new ResourceLocation() {
						FilePath = path,
						Offset = 0,
						Length = new FileInfo(path).Length
					});
					this.gobFiles[key] = new[] { file };
				} break;
			}
		}

		private void AddToGobMap(string name, ResourceLocation info) {
			name = name.ToUpper();
			// Mods can override files in base GOBs. So just add a record into the end of a list.
			// When we remove mod files we can remove the record and use the base file instead.
			if (!this.gobMap.TryGetValue(name, out List<ResourceLocation> results)) {
				this.gobMap[name] = results = new List<ResourceLocation>();
			}
			results.Add(info);
		}

		/// <summary>
		/// Remove a GOB/file so base files will be used instead.
		/// </summary>
		/// <param name="path">Path that was passed into the Add call.</param>
		public void RemoveGobFile(string path) {
			if (this.DarkForcesFolder != null) {
				path = Path.Combine(this.DarkForcesFolder, path);
			}
			string key = path.ToUpper();
			string[] files = this.gobFiles[key];
			this.gobFiles.Remove(key);
			foreach (string file in files) {
				List<ResourceLocation> results = this.gobMap[file];
				foreach (int i in results.Select((x, i) => (x, i)).Where(x => x.x.FilePath == path)
					.Reverse().Select(x => x.i)) {

					results.RemoveAt(i);
				}
				if (results.Count == 0) {
					this.gobMap.Remove(file);
				}
			}
		}

		/// <summary>
		/// Load in a file expected to be found in a GOB or standalone.
		/// </summary>
		/// <typeparam name="T">The data type of the file.</typeparam>
		/// <param name="name">The name and extension of the file.</param>
		/// <returns>The loaded object.</returns>
		public async Task<T> LoadGobFileAsync<T>(string name) where T : DfFile<T>, new() {
			if (!this.gobMap.TryGetValue(name, out List<ResourceLocation> results)) {
				return null;
			}
			// The most recently added GOB/file will be this one, so we are using a mod override if available.
			ResourceLocation location = results.Last();

			// Open the GOB/file and read in the data at the specified offset and size.
			using FileStream stream = new FileStream(location.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			stream.Seek(location.Offset, SeekOrigin.Begin);
			//using ScopedStream scope = new ScopedStream(stream, location.Length);
			using MemoryStream mem = new MemoryStream((int)location.Length);
			await stream.CopyToWithLimitAsync(mem, (int)location.Length);
			mem.Position = 0;
			// Load the data.
			return await DfFile<T>.ReadAsync(mem);
		}

		/// <summary>
		/// Track an LFD. A mod's LFD can replace a base one.
		/// </summary>
		/// <param name="path">Path of the LFD.</param>
		/// <param name="replace">A LFD to override, or none if null.</param>
		public void AddLfd(string path, string replace = null) {
			replace ??= Path.GetFileName(path);

			this.lfdOverrides[replace.ToUpper()] = new LfdInfo() {
				LfdPath = path
			};
		}

		/// <summary>
		/// Remove an LFD from tracking.
		/// </summary>
		/// <param name="path">The path passed into AddLfd.</param>
		public void RemoveLfd(string path) {
			string replace = this.lfdOverrides.FirstOrDefault(x => x.Value.LfdPath == path.ToUpper()).Key;
			if (replace == null) {
				return;
			}
			this.lfdOverrides.Remove(replace);
		}

		/// <summary>
		/// Read a file from an LFD.
		/// </summary>
		/// <typeparam name="T">The type of the file.</typeparam>
		/// <param name="lfdName">The name of the LFD, without path. Will use mod overrides.</param>
		/// <param name="name">The name of the file without extension.</param>
		/// <returns>The loaded object.</returns>
		public async Task<T> LoadLfdFileAsync<T>(string lfdName, string name) where T : DfFile<T>, new() {
			if (this.lfdOverrides.TryGetValue(lfdName.ToUpper(), out LfdInfo map)) {
				lfdName = map.LfdPath;
			} else {
				map.LfdPath = lfdName;
			}
			string lfdPath = Path.Combine(this.DarkForcesFolder, "LFD", lfdName);

			T file = null;
			// If we didn't read this LFD before, load in a map of its files so we don't have to read in
			// the entire LFD file directory next time.
			if (map.Files == null) {
				LandruFileDirectory lfd = await LandruFileDirectory.ReadAsync(lfdPath, async lfd => {
					// While we're here, read the file we need.
					file = await lfd.GetFileAsync<T>(name);
				});
				map.Files = lfd.Files.ToDictionary(x => $"{x.name.ToUpper()}.{x.type.ToUpper()}", x => new ResourceLocation() {
					FilePath = lfdName,
					Offset = x.offset,
					Length = x.size
				});
				this.lfdOverrides[lfdName.ToUpper()] = map;
			} else {
				if (!LandruFileDirectory.FileTypeNames.TryGetValue(typeof(T), out string type)) {
					throw new FileNotFoundException();
				}
				if (!map.Files.TryGetValue($"{name.ToUpper()}.{type.ToUpper()}", out ResourceLocation location)) {
					throw new FileNotFoundException();
				}
				// Otherwise seek right to the location in the LFD where the file is and read it.
				using FileStream stream = new FileStream(lfdPath, FileMode.Open, FileAccess.Read, FileShare.Read);
				stream.Seek(location.Offset, SeekOrigin.Begin);
				//using ScopedStream scope = new ScopedStream(stream, location.Length);
				using MemoryStream mem = new MemoryStream((int)location.Length);
				await stream.CopyToWithLimitAsync(mem, (int)location.Length);
				mem.Position = 0;
				return await DfFile<T>.ReadAsync(mem);
			}
			return file;
		}

		/// <summary>
		/// Read standard GOB file directroy information and cache it.
		/// </summary>
		public async Task LoadStandardGobFilesAsync() {
			foreach (string name in DARK_FORCES_STANDARD_DATA_FILES) {
				await this.AddGobFileAsync(Path.Combine(this.DarkForcesFolder, name));
			}
		}

		/// <summary>
		/// Search for files of a specific type.
		/// </summary>
		/// <param name="pattern">The pattern to match/</param>
		/// <param name="gob">Optionally, a specific GOB to search, or null for all of them.</param>
		/// <returns>The filenames which match.</returns>
		public IEnumerable<string> FindGobFiles(string pattern, string gob = null) {
			string[][] files;
			if (gob != null) {
				files = new string[][] { null };
				if (!this.gobFiles.TryGetValue(gob.ToUpper(), out files[0])) {
					return Enumerable.Empty<string>();
				}
			} else {
				files = this.gobFiles.Select(x => x.Value).ToArray();
			}

			Regex patterns = new Regex("^" + string.Join("", pattern.Select(x => x switch {
				'*' => ".*",
				'?' => ".",
				_ => Regex.Escape(x.ToString())
			})) + "$", RegexOptions.IgnoreCase);
			return files.SelectMany(x => x).Where(x => patterns.IsMatch(x)).Distinct();
		}
	}
}
