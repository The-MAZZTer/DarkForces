using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using MZZT.FileFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MZZT.DarkForces.Showcase {
	public class ResourceList : MonoBehaviour {
		[SerializeField]
		private TMP_Text nameLabel;

		private async Task UpdateModTextAsync() {
			string text;
			if (!Mod.Instance.List.Any()) {
				text = "Dark Forces";
			} else {
				string path = Mod.Instance.Gob;
				text = Path.GetFileName(path);
				if (path != null) {
					try {
						DfLevelList levels = await FileLoader.Instance.LoadGobFileAsync<DfLevelList>("JEDI.LVL");
						text = levels?.Levels.FirstOrDefault()?.DisplayName;
					} catch (Exception e) {
						Debug.LogError(e);
					}
					if (text == null) {
						text = Path.GetFileName(path);
					}
				} else {
					path = Mod.Instance.List.First().FilePath;
					text = Path.GetFileName(path);
				}
			}

			this.nameLabel.text = $"{text} Resource Editor";
		}

		[SerializeField]
		private DataboundList<ResourceListContainer> list;

		private async void Start() {
			if (!FileLoader.Instance.Gobs.Any()) {
				await FileLoader.Instance.LoadStandardFilesAsync();
			}

#if UNITY_EDITOR
			if (!UnityEditor.EditorApplication.isPlaying) {
				return;
			}
#endif

			if (!this.isActiveAndEnabled) {
				return;
			}

			await this.UpdateModTextAsync();

			string[] files;
			foreach (string gob in FileLoader.Instance.Gobs.OrderBy(x => x)) {
				files = FileLoader.Instance.GetFilesProvidedByGob(gob).OrderBy(x => x).ToArray();
				ResourceListContainer item = new(new ResourceEditorResource(gob, async () => await DfGobContainer.ReadAsync(gob, false), true));
				item.Resources.AddRange(files.Select(x => new ResourceEditorResource(Path.Combine(gob, x), async () => await FileLoader.Instance.LoadGobFileAsync(x), true)));
				this.list.Add(item);
			}
			foreach (string lfd in FileLoader.Instance.Lfds.OrderBy(x => x)) {
				files = (await FileLoader.Instance.GetFilesProvidedByLfdAsync(lfd)).OrderBy(x => x).ToArray();

#if UNITY_EDITOR
				if (!UnityEditor.EditorApplication.isPlaying) {
					return;
				}
#endif

				if (!this.isActiveAndEnabled) {
					return;
				}

				ResourceListContainer item = new(new ResourceEditorResource(lfd, async () => await LandruFileDirectory.ReadAsync(lfd), true));
				item.Resources.AddRange(files.Select(x => new ResourceEditorResource(Path.Combine(lfd, x), async () => {
					string[] file = x.Split('.');
					return await FileLoader.Instance.LoadLfdFileAsync(Path.GetFileName(lfd), file[0], file[1]);
				}, true)));
				this.list.Add(item);
			}
			files = FileLoader.Instance.GetStandaloneFiles().OrderBy(x => x).ToArray();
			if (files.Length > 0) {
				ResourceListContainer item = new("Other Files");
				item.Resources.AddRange(files.Select(x => new ResourceEditorResource(x, async () => await FileLoader.Instance.LoadGobFileAsync(Path.GetFileName(x)), true)));
				this.list.Add(item);
			}
		}

		[SerializeField]
		private DataboundList<ResourceEditorResource> searchResults;
		[SerializeField]
		private TMP_InputField searchField;
		[SerializeField]
		private GameObject searchGlyph;
		[SerializeField]
		private GameObject stopSearchGlyph;

		private const int SEARCH_TIMEOUT_MS = 500;

		private CancellationTokenSource searchTimer;
		public async void OnSearchFieldTextChangedAsync(string value) {
			this.searchTimer?.Cancel();

			if (string.IsNullOrWhiteSpace(value)) {
				this.searchResults.gameObject.SetActive(false);
				this.list.gameObject.SetActive(true);

				this.stopSearchGlyph.SetActive(false);
				this.searchGlyph.SetActive(true);

				this.searchResults.Clear();
				return;
			}

			this.searchTimer = new CancellationTokenSource();
			try {
				await Task.Delay(SEARCH_TIMEOUT_MS, this.searchTimer.Token);
			} catch (OperationCanceledException) {
				return;
			}

			this.Search();
		}

		public void OnSearchButtonClicked() {
			this.searchTimer?.Cancel();
			this.searchTimer = null;

			if (this.stopSearchGlyph.activeSelf) {
				this.searchField.text = "";
				return;
			}

			this.Search();
		}

		private void Search() {
			this.searchResults.Clear();

			this.searchGlyph.SetActive(false);
			this.stopSearchGlyph.SetActive(true);

			this.list.gameObject.SetActive(false);
			this.searchResults.gameObject.SetActive(true);

			string value = this.searchField.text;
			List<ResourceEditorResource> results = new();

			this.searchResults.AddRange(this.list
				.SelectMany(x => x.Resources.Prepend(x.Resource)/*.Select(y => (x, y))*/)
				.Where(x => x?.Name.Contains(value, StringComparison.CurrentCultureIgnoreCase) ?? false)
				/*.Select(x => new ResourceEditorResource(x.y.Path, x.y.GetFileAsync, x.y.PartOfCurrentMod) {
					Name = x.y == x.x.Resource ? x.y.Name : $"{x.x.Name}{Path.DirectorySeparatorChar}{x.y.Name}"
				})*/);
		}
	}

	public class ResourceListContainer {
		public ResourceListContainer(ResourceEditorResource resource) {
			this.Resource = resource;
			this.Name = resource.Name;
		}
		public ResourceListContainer(string name) {
			this.Name = name;
		}

		public string Name { get; }
		public ResourceEditorResource Resource { get; }
		public List<ResourceEditorResource> Resources { get; } = new();

		public override string ToString() => this.Resource?.ToString() ?? this.Name ?? base.ToString();
	}

	public class ResourceEditorResource {
		public ResourceEditorResource(string path, Func<Task<IFile>> fileGetter, bool partOfCurrentMod) {
			this.Name = System.IO.Path.GetFileName(path);
			this.Path = path;
			this.GetFileAsync = fileGetter;
			this.PartOfCurrentMod = partOfCurrentMod;
		}
		
		public bool PartOfCurrentMod { get; }
		public string Path { get; }
		public string Name { get; set; }
		public Func<Task<IFile>> GetFileAsync { get; }

		public override string ToString() => this.Name ?? base.ToString();
	}
}
