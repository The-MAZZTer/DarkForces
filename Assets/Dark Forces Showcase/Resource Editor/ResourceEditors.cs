using MZZT.DarkForces.FileFormats;
using MZZT.Data.Binding;
using MZZT.FileFormats;
using MZZT.IO.FileProviders;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MZZT.DarkForces.Showcase {
	public class ResourceEditors : Singleton<ResourceEditors> {
		[Header("Resource Editors"), SerializeField]
		private DataboundList<ResourceEditorTab> tabs;
		[SerializeField]
		private ScrollRect tabScroll;
		[SerializeField]
		private Button goLeftTab;
		[SerializeField]
		private Button goRightTab;
		[SerializeField]
		private DataboundList<ResourceListContainer> list;
		[SerializeField]
		private Button newButton;
		[SerializeField]
		private GameObject newMenu;
		[SerializeField]
		private Transform content;
		[SerializeField]
		private GameObject dirtyDialog;
		[SerializeField]
		private TMP_Text dirtyPrompt;

		public void OpenResource(ResourceEditorResource resource) {
			IDatabind tab = this.tabs.Children.FirstOrDefault(x => ((ResourceEditorTab)x.Value).Resource == resource);
			if (tab != null) {
				DataboundListChildToggle.FindToggleFor(tab).isOn = true;
				return;
			}

			ResourceEditorTab newTab = new(resource);
			this.tabs.Add(newTab);

			tab = this.tabs.Children.FirstOrDefault(x => x.Value == newTab);
			DataboundListChildToggle.FindToggleFor(tab).isOn = true;
		}

		public ResourceEditorResource FindResource(string path) => this.list.Value.SelectMany(x => {
			if (x.Resource == null) {
				return x.Resources;
			} else {
				return x.Resources.Prepend(x.Resource);
			}
		}).Concat(this.tabs.Children.Select(x => ((ResourceEditorTab)x.Value).Resource)).FirstOrDefault(x => string.Compare(x.Path, path, true) == 0);

		private string dirtyText;

		private bool dirtyDiscard;
		public void OnDirtyDialog(bool discard) {
			this.dirtyDiscard = discard;
		}

		public async Task CloseTabAsync(Databind<ResourceEditorTab> tab) {
			ResourceEditorTabList tabs = (ResourceEditorTabList)tab.Parent;
			if (tabs.SelectedDatabound == (IDatabind)tab) {
				int index = tabs.SelectedIndex;
				if (index > 0) {
					tabs.SelectedIndex = index - 1;
				} else if (index < tabs.Count - 1) {
					tabs.SelectedIndex = index + 1;
				}
			}

			if (tab.Value.Viewer != null) {
				if (tab.Value.Viewer.IsDirty) {
					this.dirtyText ??= this.dirtyPrompt.text;
					this.dirtyPrompt.text = string.Format(this.dirtyText, tab.Value.Name);

					this.dirtyDialog.SetActive(true);

					while (this.dirtyDialog.activeInHierarchy && this.isActiveAndEnabled) {
#if UNITY_EDITOR
						if (!UnityEditor.EditorApplication.isPlaying) {
							return;
						}
#endif

						await Task.Yield();
					}

					if (!this.dirtyDiscard) {
						return;
					}
				}

				tab.Value.Viewer.IsDirtyChanged -= this.Viewer_IsDirtyChanged;
				tab.Value.Viewer.TabNameChanged -= this.Viewer_TabNameChanged;
				tab.Value.Viewer.ThumbnailChanged -= this.Viewer_ThumbnailChanged;

				DestroyImmediate(((Component)tab.Value.Viewer).gameObject);
			}

			this.tabs.RemoveAt(tab.transform.GetSiblingIndex());

			this.UpdateTabStripButtonsAndScrollToCurrentTab(false);
		}

		[Serializable]
		public class ResourceViewerPrefab {
			[SerializeField]
			private ResourceTypes type;
			public ResourceTypes Type => this.type;

			[SerializeField]
			private GameObject prefab;
			public IResourceViewer Prefab => this.prefab.GetComponent<IResourceViewer>();
		}

		[SerializeField]
		private ResourceViewerPrefab[] resourceViewers;
		[SerializeField]
		private GameObject genericViewerPrefab;
		public IResourceViewer GenericViewerPrefab => this.genericViewerPrefab.GetComponent<IResourceViewer>();

		public async void OnSelectedTabChangedAsync() {
			ResourceEditorTabItem tab = (ResourceEditorTabItem)this.tabs.SelectedDatabound;
			if (tab == null) {
				return;
			}

			Component content = (Component)tab.Value.Viewer;
			foreach (Transform child in this.content) {
				if (content != null && child.gameObject == content.gameObject) {
					continue;
				}

				child.gameObject.SetActive(false);
			}

			if (tab.Value.Viewer == null) {
				ResourceTypes type = tab.Value.Type ?? ResourceDumper.GetFileType(tab.Value.Resource.Path);
				IResourceViewer viewer = this.resourceViewers.FirstOrDefault(x => x.Type == type)?.Prefab;
				if (viewer == null) {
					viewer = this.GenericViewerPrefab;
				}

				if (viewer != null) {
					GameObject obj = Instantiate(((Component)viewer).gameObject);
					obj.SetActive(false);
					obj.name = tab.Value.Resource.Name;
					obj.transform.SetParent(this.content, false);
					tab.Value.Viewer = obj.GetComponent<IResourceViewer>();

					IFile file;
					try {
						file = await tab.Value.Resource.GetFileAsync();
					} catch (Exception ex) {
						await DfMessageBox.Instance.ShowAsync($"Error reading file: {ex.Message}");
						return;
					}

					if (file is IDfFile dfFile) {
						Warning[] warnings = dfFile.Warnings.ToArray();
						if (warnings.Length > 0) {
							string warning = string.Join("\n", warnings
								.Select(x => $"{(x.Line > 0 ? $"{x.Line} - " : string.Empty)}{x.Message}"));
							await DfMessageBox.Instance.ShowAsync($"{tab.Value.Resource.Name} loaded with warnings:\n\n{warning}");
						}
					}

					if (file is ICloneable cloneable) {
						file = (IFile)cloneable.Clone();
					}

					await tab.Value.Viewer.LoadAsync(tab.Value.Resource, file);

					tab.Value.Name = tab.Value.Viewer.TabName;

					Sprite thumbnail = tab.Value.Viewer.Thumbnail;
					if (thumbnail != null) {
						tab.SetIcon(thumbnail);
					}
					tab.Value.Viewer.TabNameChanged += this.Viewer_TabNameChanged;
					tab.Value.Viewer.ThumbnailChanged += this.Viewer_ThumbnailChanged;
					tab.Value.Viewer.IsDirtyChanged += this.Viewer_IsDirtyChanged;
					tab.Invalidate();
				}
			}

			if (tab.Value.Viewer != null) {
				((Component)tab.Value.Viewer).gameObject.SetActive(true);

				await Task.Yield();

				tab.Value.Viewer.ResetDirty();
			}

			this.UpdateTabStripButtonsAndScrollToCurrentTab();
		}

		private void Viewer_TabNameChanged(object sender, EventArgs e) {
			IResourceViewer viewer = ((IResourceViewer)sender);
			ResourceEditorTabItem tab = (ResourceEditorTabItem)this.tabs.Children.First(x => ((ResourceEditorTab)x.Value).Viewer == viewer);
			tab.Value.Name = viewer.TabName;
			tab.Invalidate();
		}

		private void Viewer_ThumbnailChanged(object sender, EventArgs e) {
			IResourceViewer viewer = ((IResourceViewer)sender);
			ResourceEditorTabItem tab = (ResourceEditorTabItem)this.tabs.Children.First(x => ((ResourceEditorTab)x.Value).Viewer == viewer);
			tab.SetIcon(viewer.Thumbnail);
		}

		private void Viewer_IsDirtyChanged(object sender, EventArgs e) {
			IResourceViewer viewer = ((IResourceViewer)sender);
			ResourceEditorTabItem tab = (ResourceEditorTabItem)this.tabs.Children.First(x => ((ResourceEditorTab)x.Value).Viewer == viewer);
			tab.SetIsDirty(viewer.IsDirty);
		}

		public async Task<string> PickSaveLocationAsync(string path, string[] patterns) {
			string startFile = null;
			string startPath = null;
			string filename = "file";
			if (!string.IsNullOrEmpty(path)) {
				filename = Path.GetFileName(path);
				if (FileManager.Instance.FileExists(path)) {
					startFile = path;
				} else {
					while (!string.IsNullOrEmpty(path)) {
						path = Path.GetDirectoryName(path);
						if (FileManager.Instance.FolderExists(path)) {
							startPath = path;
							break;
						}
					}
				}
			}

			return await FileBrowser.Instance.ShowAsync(new() {
				AllowNavigateGob = false,
				AllowNavigateLfd = false,
				Filters = new[] {
					FileBrowser.FileType.Generate($"{Path.GetExtension(patterns[0]).TrimStart('.')} Files", patterns),
					FileBrowser.FileType.AllFiles
				},
				SelectButtonText = "Save",
				SelectedFileMustExist = false,
				SelectedPathMustExist = true,
				SelectFolder = false,
				StartPath = startPath,
				StartSelectedFile = startFile,
				Title = $"Save {filename}",
				ValidateFileName = true
			});;
		}

		private string lastFolder;
		public async void OpenFileAsync() {
			string path = await FileBrowser.Instance.ShowAsync(new() {
				AllowNavigateGob = true,
				AllowNavigateLfd = true,
				SelectButtonText = "Open",
				SelectedFileMustExist = true,
				StartPath = this.lastFolder ?? FileLoader.Instance.DarkForcesFolder,
				Title = $"Open File"
			});

			if (path == null) {
				return;
			}

			this.lastFolder = Path.GetDirectoryName(path);

			IDatabind tab = this.tabs.Children.FirstOrDefault(x => ((ResourceEditorTab)x.Value).Resource.Path.ToLower() == path.ToLower());
			if (tab != null) {
				DataboundListChildToggle.FindToggleFor(tab).isOn = true;
				return;
			}

			// TODO add to resource list?

			ResourceEditorTab newTab = new(new ResourceEditorResource(path, async () => {
				return await DfFileManager.Instance.ReadAsync(path);
			}, false));
			this.tabs.Add(newTab);

			tab = this.tabs.Children.FirstOrDefault(x => x.Value == newTab);
			DataboundListChildToggle.FindToggleFor(tab).isOn = true;
		}

		public void ToggleNewMenu() {
			this.newMenu.SetActive(!this.newMenu.activeSelf);
		}

		public void LeftTab() {
			this.tabs.SelectedIndex--;
		}

		public void RightTab() {
			this.tabs.SelectedIndex++;
		}

		public void UpdateTabStripButtonsAndScrollToCurrentTab(bool scroll = true) {
			ResourceEditorTabItem tab = (ResourceEditorTabItem)this.tabs.SelectedDatabound;

			Canvas.ForceUpdateCanvases();

			float viewportSize = this.tabScroll.viewport.rect.width;
			float contentSize = this.tabScroll.content.rect.width;
			if (contentSize > viewportSize && this.tabs.Count > 1) {
				if (!this.goLeftTab.gameObject.activeSelf) {
					this.goLeftTab.gameObject.SetActive(true);
					this.goRightTab.gameObject.SetActive(true);

					Canvas.ForceUpdateCanvases();

					viewportSize = this.tabScroll.viewport.rect.width;
				}

				if (scroll) {
					float viewportOffset = -this.tabScroll.content.anchoredPosition.x;
					float offset = ((RectTransform)tab.transform).anchoredPosition.x;
					float size = ((RectTransform)tab.transform).rect.width;

					if (offset < viewportOffset) {
						this.tabScroll.content.anchoredPosition = new(-offset, 0);
					} else if (offset + size > viewportOffset + viewportSize) {
						this.tabScroll.content.anchoredPosition = new(-offset - size + viewportSize, 0);
					}
				}

				this.goLeftTab.interactable = (ResourceEditorTabItem)this.tabs.Children.FirstOrDefault() != tab;
				this.goRightTab.interactable = (ResourceEditorTabItem)this.tabs.Children.LastOrDefault() != tab;
			} else {
				this.goLeftTab.gameObject.SetActive(false);
				this.goRightTab.gameObject.SetActive(false);
			}
		}

		private void Update() {
			if (Mouse.current.leftButton.wasPressedThisFrame && this.newMenu.activeSelf) {
				Vector2 pos = Mouse.current.position.value;
				RectTransform transform = (RectTransform)this.newButton.transform;
				if (RectTransformUtility.ScreenPointToLocalPointInRectangle(transform, pos, transform.GetComponentInParent<Canvas>().worldCamera, out Vector2 local)) {
					if (transform.rect.Contains(local)) {
						return;
					}
				}
				transform = (RectTransform)this.newMenu.transform;
				if (RectTransformUtility.ScreenPointToLocalPointInRectangle(transform, pos, transform.GetComponentInParent<Canvas>().worldCamera, out local)) {
					if (transform.rect.Contains(local)) {
						return;
					}
				}

				this.newMenu.SetActive(false);
			}
		}

		public void CreateNew(string type) {
			Type fileType = type.ToUpper() switch {
				".ANIM" => typeof(LandruAnimation),
				".BM" => typeof(DfBitmap),
				"BRIEFING.LST" => typeof(DfBriefingList),
				".CMP" => typeof(DfColormap),
				"CUTMUSE.TXT" => typeof(DfCutsceneMusicList),
				"CUTSCENE.LST" => typeof(DfCutsceneList),
				".DELT" => typeof(LandruDelt),
				".FILM" => typeof(LandruFilm),
				".FME" => typeof(DfFrame),
				".FNT" => typeof(DfFont),
				".FONT" => typeof(LandruFont),
				".GMD" => typeof(DfGeneralMidi),
				".GMID" => typeof(DfGeneralMidi),
				".GOL" => typeof(DfLevelGoals),
				".INF" => typeof(DfLevelInformation),
				"JEDI.LVL" => typeof(DfLevelList),
				".LEV" => typeof(DfLevel),
				".MSG" => typeof(DfMessages),
				".O" => typeof(DfLevelObjects),
				".GOB" => typeof(DfGobContainer),
				".LFD" => typeof(LandruFileDirectory),
				".PAL" => typeof(DfPalette),
				".PLTT" => typeof(LandruPalette),
				".3DP" => typeof(Df3dObject),
				".VOC" => typeof(CreativeVoice),
				".VOIC" => typeof(CreativeVoice),
				".VUE" => typeof(AutodeskVue),
				".WAX" => typeof(DfWax),
				_ => throw new ArgumentException()
			}; ;

			ResourceEditorTab newTab = new(new ResourceEditorResource(null, () => {
				return Task.FromResult((IFile)Activator.CreateInstance(fileType));
			}, false)) {
				Type = ResourceDumper.GetFileType(type)
			};
			this.tabs.Add(newTab);

			IDatabind tab = this.tabs.Children.FirstOrDefault(x => x.Value == newTab);
			DataboundListChildToggle.FindToggleFor(tab).isOn = true;

			this.newMenu.SetActive(false);
		}
	}

	public class ResourceEditorTab {
		public ResourceEditorTab(ResourceEditorResource resource) {
			this.Resource = resource;
			this.Name = resource.Name;
		}

		public string Name { get; set; }

		public ResourceEditorResource Resource { get; }

		public IResourceViewer Viewer { get; set; }

		public ResourceTypes? Type { get; set; }

		public override string ToString() => this.Resource?.ToString() ?? base.ToString();
	}
}
