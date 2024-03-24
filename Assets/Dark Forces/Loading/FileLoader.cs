#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && !UNITY_WEBGL
using Microsoft.Win32;
#endif
using MZZT.DarkForces.FileFormats;
using MZZT.DarkForces.Showcase;
using MZZT.Extensions;
using MZZT.FileFormats;
using MZZT.IO.FileProviders;
#if UNITY_WEBGL && !UNITY_EDITOR
using MZZT.IO.FileSystemProviders;
#endif
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && !UNITY_WEBGL
using MZZT.Steam;
#endif
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
		public static string[] DarkForcesDataFiles { get; set; } = new[] {
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
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && !UNITY_WEBGL
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
			if (FileManager.Instance.FileExists(Path.Combine(path, "SteamApps", "common", "Dark Forces", "Game", "DARK.GOB"))) {
				return Path.Combine(path, "SteamApps", "common", "Dark Forces", "Game");
			}

			// Check other library folders. Start by reading the list of library folders.
			ValveDefinitionFile libraryFoldersVdf =
				await DfFileManager.Instance.ReadAsync<ValveDefinitionFile>(Path.Combine(path, "SteamApps", "libraryfolders.vdf"));

			// Normally we could deserialize into a type but the format of this file doesn't work well for that.
			// But we can read the tokens by hand.

			int pos = 0;
			List<string> libraryFolders = new();
			while (pos < libraryFoldersVdf.Tokens.Count) {
				ValveDefinitionFile.Token token = libraryFoldersVdf.Tokens[pos];
				pos++;

				string property = (token as ValveDefinitionFile.StringToken)?.Text;
				if (property != "path" || pos >= libraryFoldersVdf.Tokens.Count) {
					continue;
				}

				token = libraryFoldersVdf.Tokens[pos];
				pos++;
				string value = (token as ValveDefinitionFile.StringToken)?.Text;
				if (value == null) {
					continue;
				}

				libraryFolders.Add(value);
			}

			foreach (string libraryFolder in libraryFolders) {
				if (FileManager.Instance.FileExists(Path.Combine(libraryFolder, "SteamApps", "common", "Dark Forces", "Game", "DARK.GOB"))) {
					return Path.Combine(libraryFolder, "SteamApps", "common", "Dark Forces", "Game");
				}
			}

			return null;
#elif !UNITY_EDITOR && UNITY_WEBGL
			IVirtualFolder uploads = await FileManager.Instance.GetByPathAsync($"{Path.DirectorySeparatorChar}Uploads") as IVirtualFolder;
			if ((await uploads.GetChildAsync("DARK.GOB")) is IVirtualFile) {
				return uploads.FullPath;
			}

			await foreach (IVirtualFolder folder in uploads.GetFoldersAsync()) {
				if ((await folder.GetChildAsync("DARK.GOB")) is IVirtualFile) {
					return folder.FullPath;
				}
			}
			return null;
#else
			await Task.CompletedTask;
			return null;
#endif
		}

		private readonly Dictionary<string, List<ResourceLocation>> gobMap = new();
		private readonly Dictionary<string, LfdInfo> lfdFiles = new();
		private readonly Dictionary<string, string[]> gobFiles = new();

		/// <summary>
		/// Clear all cached data.
		/// </summary>
		public void Clear() {
			this.gobFiles.Clear();
			this.gobMap.Clear();
			this.lfdFiles.Clear();
		}

		/// <summary>
		/// GOB files we know the contents of.
		/// </summary>
		public IEnumerable<string> Gobs => this.gobFiles.Keys.Where(x => string.Compare(Path.GetExtension(x), ".GOB", true) == 0);

		/// <summary>
		/// LFD files we know the contents of.
		/// </summary>
		public IEnumerable<string> Lfds => this.lfdFiles.Values.Select(x => x.LfdPath);

		/// <summary>
		/// Reads in a GOB file and tracks the files inside of it so we can quickly find them later.
		/// </summary>
		/// <param name="path">Path to the GOB file.</param>
		public async Task AddGobFileAsync(string path) {
			if (this.DarkForcesFolder != null) {
				if (Uri.IsWellFormedUriString(this.DarkForcesFolder, UriKind.Absolute)) {
					path = new Uri(new Uri(this.DarkForcesFolder), path).AbsoluteUri;
				} else if (!Uri.IsWellFormedUriString(path, UriKind.Absolute)) {
					path = Path.Combine(this.DarkForcesFolder, path);
				}
			}
			string key = path;
			// Skip if we already loaded it.
			if (this.gobFiles.ContainsKey(key)) {
				return;
			}

			switch (Path.GetExtension(key).ToUpper()) {
				case ".GOB": {
					DfGobContainer gob = await DfFileManager.Instance.ReadAsync<DfGobContainer>(path);
					List<string> files = new();
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
						Length = await FileManager.Instance.GetSizeAsync(path)
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
		/// Returns a list of files that the specified GOB can provide. Files that are overridden by other GOBs are not listed.
		/// </summary>
		/// <param name="gob">The path to the GOB file as previously provided to this class.</param>
		/// <returns>A list of filenames.</returns>
		public IEnumerable<string> GetFilesProvidedByGob(string gob) =>
			this.gobMap.Where(x => string.Compare(x.Value.Last().FilePath, gob, true) == 0).Select(x => x.Key);

		/// <summary>
		/// Returns a list of files that were added direct from the filesystem.
		/// </summary>
		/// <returns>A list of filenames.</returns>
		public IEnumerable<string> GetStandaloneFiles() =>
			this.gobMap.Select(x => x.Value.Last()).Where(x => x.Offset == 0).Select(x => x.FilePath);

		/// <summary>
		/// Remove a GOB/file so base files will be used instead.
		/// </summary>
		/// <param name="path">Path that was passed into the Add call.</param>
		public void RemoveGobFile(string path) {
			if (this.DarkForcesFolder != null) {
				path = Path.Combine(this.DarkForcesFolder, path);
			}
			string key = path;
			string[] files = this.gobFiles.GetValueOrDefault(key);
			if (files == null) {
				return;
			}

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
		/// Gets a Stream for a file expected to be found in a GOB or standalone.
		/// </summary>
		/// <param name="name">The name and extension of the file.</param>
		/// <returns>The Stream you can read file data from.</returns>
		public async Task<Stream> GetGobFileStreamAsync(string name) {
			if (!this.gobMap.TryGetValue(name, out List<ResourceLocation> results)) {
				return null;
			}
			// The most recently added GOB/file will be this one, so we are using a mod override if available.
			ResourceLocation location = results.Last();

			// Open the GOB/file and read in the data at the specified offset and size.
			Stream stream = await FileManager.Instance.NewFileStreamAsync(location.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			Stream scoped = null;
			try {
				stream.Seek(location.Offset, SeekOrigin.Begin);
				if (location.Offset > 0 || location.Length < stream.Length) {
					scoped = new MemoryStream((int)location.Length);
					await stream.CopyToWithLimitAsync(scoped, (int)location.Length);
					scoped.Position = 0;
				} else {
					scoped = stream;
				}
			} catch (Exception) {
				if (scoped != stream) {
					scoped?.Dispose();
				}
				throw;
			} finally {
				if (scoped != stream) {
					stream.Dispose();
				}
			}

			return scoped;
		}

		/// <summary>
		/// Load in a file expected to be found in a GOB or standalone.
		/// </summary>
		/// <param name="name">The name and extension of the file.</param>
		/// <returns>The loaded object.</returns>
		public async Task<IFile> LoadGobFileAsync(string name) {
			Stream stream = await this.GetGobFileStreamAsync(name);
			if (stream == null) {
				return null;
			}

			using (stream) {
				// Load the data
				Type type = DfFile.DetectFileTypeByName(name) ?? typeof(Raw);
				IFile file = (IFile)Activator.CreateInstance(type);
				await file.LoadAsync(stream);
				return file;
			}
		}

		/// <summary>
		/// Load in a file expected to be found in a GOB or standalone.
		/// </summary>
		/// <typeparam name="T">The data type of the file.</typeparam>
		/// <param name="name">The name and extension of the file.</param>
		/// <returns>The loaded object.</returns>
		public async Task<T> LoadGobFileAsync<T>(string name) where T : IFile, new() {
			Stream stream = await this.GetGobFileStreamAsync(name);
			if (stream == null) {
				return default;
			}

			using (stream) {
				// Load the data
				IFile file = (IFile)Activator.CreateInstance(typeof(T));
				await file.LoadAsync(stream);
				return (T)file;
			}
		}

		/// <summary>
		/// Track an LFD. A mod's LFD can replace a base one.
		/// </summary>
		/// <param name="path">Path of the LFD.</param>
		/// <param name="replace">A LFD to override, or none if null.</param>
		public void AddLfd(string path, string replace = null) {
			replace ??= Path.GetFileName(path);

			this.lfdFiles[replace] = new LfdInfo() {
				LfdPath = path
			};
		}

		/// <summary>
		/// Remove an LFD from tracking.
		/// </summary>
		/// <param name="path">The path passed into AddLfd.</param>
		public void RemoveLfd(string path) {
			string replace = this.lfdFiles.FirstOrDefault(x => x.Value.LfdPath == path).Key;
			if (replace == null) {
				return;
			}
			this.lfdFiles.Remove(replace);
		}

		/// <summary>
		/// Get a list of files in the LFD.
		/// </summary>
		/// <param name="lfdPath">The path of the LFD. Will use mod overrides.</param>
		/// <returns>The list of names and types of files in the LFD.</returns>
		public async Task<IEnumerable<string>> GetFilesProvidedByLfdAsync(string lfdPath) {
			LfdInfo map = this.lfdFiles.Values.FirstOrDefault(x => string.Compare(x.LfdPath, lfdPath, true) == 0);
			if (map.LfdPath == null) {
				throw new FileNotFoundException();
			}
			// If we didn't read this LFD before, load in a map of its files so we don't have to read in
			// the entire LFD file directory next time.
			if (map.Files == null) {
				LandruFileDirectory lfd = await DfFileManager.Instance.ReadAsync<LandruFileDirectory>(lfdPath);
				map.Files = lfd.Files.GroupBy(x => $"{x.name.ToUpper()}.{x.type.ToUpper()}").ToDictionary(x => x.Key, x => new ResourceLocation() {
					FilePath = map.LfdPath,
					Offset = x.Last().offset,
					Length = x.Last().size
				});
			}
			return map.Files.Select(x => x.Key);
		}

		/// <summary>
		/// Read a file from an LFD.
		/// </summary>
		/// <param name="lfdName">The name of the LFD, without path. Will use mod overrides.</param>
		/// <param name="name">The name of the file without extension.</param>
		/// <param name="typeName">The type of the file.</param>
		/// <returns>The Stream for the file.</returns>
		public async Task<Stream> GetLfdFileStreamAsync(string lfdName, string name, string typeName) {
			if (this.lfdFiles.TryGetValue(lfdName, out LfdInfo map)) {
				lfdName = map.LfdPath;
			} else {
				map.LfdPath = lfdName;
			}
			string lfdPath = Path.Combine(this.DarkForcesFolder, "LFD", lfdName);

			Stream stream = null;
			try {
				// If we didn't read this LFD before, load in a map of its files so we don't have to read in
				// the entire LFD file directory next time.
				if (map.Files == null) {
					LandruFileDirectory lfd = await DfFileManager.Instance.ReadLandruFileDirectoryAsync(lfdPath, async lfd => {
						// While we're here, read the file we need.
						stream = await lfd.GetFileStreamAsync(name, typeName);
					});
					// DFBRIEF.LFD has multiple SEWERS DELTs. Ignore all but the first (TODO what does DF do?).
					map.Files = lfd.Files.GroupBy(x => $"{x.name.ToUpper()}.{x.type.ToUpper()}").ToDictionary(x => x.Key, x => new ResourceLocation() {
						FilePath = lfdName,
						Offset = x.First().offset,
						Length = x.First().size
					});
					this.lfdFiles[lfdName] = map;
				} else {
					if (!map.Files.TryGetValue($"{name.ToUpper()}.{typeName.ToUpper()}", out ResourceLocation location)) {
						return null;
					}
					// Otherwise seek right to the location in the LFD where the file is and read it.
					using Stream fileStream = await FileManager.Instance.NewFileStreamAsync(lfdPath, FileMode.Open, FileAccess.Read, FileShare.Read);
					fileStream.Seek(location.Offset, SeekOrigin.Begin);
					stream = new MemoryStream((int)location.Length);
					await fileStream.CopyToWithLimitAsync(stream, (int)location.Length);
					stream.Position = 0;
				}
			} catch (Exception) {
				stream?.Dispose();
				throw;
			}
			return stream;
		}

		/// <summary>
		/// Read a file from an LFD.
		/// </summary>
		/// <param name="lfdName">The name of the LFD, without path. Will use mod overrides.</param>
		/// <param name="name">The name of the file without extension.</param>
		/// <param name="typeName">The type of the file.</param>
		/// <returns>The loaded object.</returns>
		public async Task<IFile> LoadLfdFileAsync(string lfdName, string name, string typeName) {
			Stream stream = await this.GetLfdFileStreamAsync(lfdName, name, typeName);
			if (stream == null) {
				return null;
			}

			using (stream) {
				Type type = DfFile.DetectFileTypeByName("." + typeName) ?? typeof(Raw);
				IFile file = (IFile)Activator.CreateInstance(type);
				await file.LoadAsync(stream);
				return file;
			}
		}

		/// <summary>
		/// Read a file from an LFD.
		/// </summary>
		/// <typeparam name="T">The type of the file.</typeparam>
		/// <param name="lfdName">The name of the LFD, without path. Will use mod overrides.</param>
		/// <param name="name">The name of the file without extension.</param>
		/// <returns>The loaded object.</returns>
		public async Task<T> LoadLfdFileAsync<T>(string lfdName, string name) where T : DfFile<T>, new() {
			if (!LandruFileDirectory.FileTypeNames.TryGetValue(typeof(T), out string typeName)) {
				throw new ArgumentException("Invalid type.", nameof(T));
			}

			Stream stream = await this.GetLfdFileStreamAsync(lfdName, name, typeName);
			if (stream == null) {
				return null;
			}

			using (stream) {
				return await DfFile<T>.ReadAsync(stream);
			}
		}

		/// <summary>
		/// Read standard GOB file directory information and cache it.
		/// </summary>
		public async Task LoadStandardFilesAsync() {
#if UNITY_WEBGL && !UNITY_EDITOR 
			if (FileManager.Instance.Provider is WebFileSystemProvider) {
				while (!WebFileSystemProviderBrowserCallback.Instance.BrowserFilesUploaded) {
					await Task.Delay(25);
				}
			}
#endif

			foreach (string name in DarkForcesDataFiles) {
				await this.AddGobFileAsync(name);
			}

			await foreach (string lfd in FileManager.Instance.FolderEnumerateFilesAsync(Path.Combine(this.DarkForcesFolder, "LFD"), "*.LFD", SearchOption.TopDirectoryOnly)) {
				this.AddLfd(lfd);
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
				if (!this.gobFiles.TryGetValue(gob, out files[0])) {
					return Enumerable.Empty<string>();
				}
			} else {
				files = this.gobFiles.Select(x => x.Value).ToArray();
			}

			Regex patterns = new("^" + string.Join("", pattern.Select(x => x switch {
				'*' => ".*",
				'?' => ".",
				_ => Regex.Escape(x.ToString())
			})) + "$", RegexOptions.IgnoreCase);
			return files.SelectMany(x => x).Where(x => patterns.IsMatch(x)).Distinct();
		}

		public string ModGob => this.gobFiles.Keys.Except(DarkForcesDataFiles.Select(x => Path.Combine(this.DarkForcesFolder, x))).FirstOrDefault(x => Path.GetExtension(x).ToUpper() == ".GOB");
	}
}
