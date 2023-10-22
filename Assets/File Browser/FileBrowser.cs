using MZZT.DarkForces.IO;
using MZZT.Data.Binding;
using MZZT.IO.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT {
	/// <summary>
	/// This class encompasses the ability for the user to select a file or folder from a browse dialog.
	/// Made for Windows but theoretically should work cross-platform.
	/// </summary>
	public class FileBrowser : Singleton<FileBrowser> {
		[SerializeField, Header("References")]
		private GameObject background = null;
		[SerializeField]
		private GameObject window = null;
		[SerializeField]
		private TMP_Text titleText = null;
		[SerializeField]
		private Button backButton = null;
		[SerializeField]
		private Button forwardButton = null;
		[SerializeField]
		private Button upButton = null;
		[SerializeField]
		private TMP_InputField folderInput = null;
		[SerializeField]
		private FileView folderTree = null;
		[SerializeField]
		private FileView fileList = null;
		[SerializeField]
		private TMP_InputField fileInput = null;
		[SerializeField]
		private GameObject fileTypesContainer = null;
		[SerializeField]
		private TMP_Dropdown fileTypes = null;
		[SerializeField]
		private Button selectButton = null;
		[SerializeField, Header("Colors")]
		private Color disabledButtonTextColor = default;

		/// <summary>
		/// Options to use in the current call to the file browser.
		/// </summary>
		public class FileBrowserOptions {
			/// <summary>
			/// The title to display at the top.
			/// </summary>
			public string Title { get; set; }
			/// <summary>
			/// The Select button text.
			/// </summary>
			public string SelectButtonText { get; set; }
			/// <summary>
			/// The lowest path the user can navigate to in the tree view.
			/// </summary>
			public string RootPath { get; set; }
			/// <summary>
			/// The location to start the file browser.
			/// </summary>
			public string StartPath { get; set; }
			/// <summary>
			/// The file to start selected. Overrides StartPath.
			/// </summary>
			public string StartSelectedFile { get; set; }
			/// <summary>
			/// Whether the user should be selecting a folder instead of a file.
			/// </summary>
			public bool SelectFolder { get; set; }
			/// <summary>
			/// Whether GOBs should be treated as folders and files inside them can be selected.
			/// </summary>
			public bool AllowNavigateGob { get; set; }
			/// <summary>
			/// Whether LFDs should be treated as folders and files inside them can be selected.
			/// </summary>
			public bool AllowNavigateLfd { get; set; }
			/// <summary>
			/// The files to display (in addition to folders).
			/// </summary>
			public FileType[] Filters { get; set; }
			/// <summary>
			/// Check the input file exists before allowing it to be selected.
			/// </summary>
			public bool SelectedFileMustExist { get; set; }
			/// <summary>
			/// Check the folder of the input file exists before allowing it to be selected.
			/// </summary>
			public bool SelectedPathMustExist { get; set; }
			/// <summary>
			/// Ensure the input path has only valid characters before allowing it.
			/// </summary>
			public bool ValidateFileName { get; set; }
		}

		public class FileType {
			public static FileType AllFiles { get; } = new FileType() {
				DisplayName = "All Files (*.*)",
				Wildcards = new[] { "*" }
			};
			public static FileType Folders { get; } = new FileType() {
				DisplayName = "Folders",
				Wildcards = Array.Empty<string>()
			};
			public static FileType Generate(string displayName, IEnumerable<string> wildcards) => new FileType() {
				DisplayName = $"{displayName} ({string.Join(';', wildcards)})",
				Wildcards = (wildcards is string[] x) ? x : wildcards.ToArray()
			};
			public static FileType Generate(string displayName, params string[] wildcards) =>
				Generate(displayName, (IEnumerable<string>)wildcards);
				
			public string DisplayName { get; set; }
			/// <summary>
			/// The files to display (in addition to folders).
			/// </summary>
			public string[] Wildcards { get; set; }
		}

		private FileBrowserOptions options;

		private int initializing = 0;

		private void Start() {
			this.folderInput.onSubmit.AddListener(_ => {
				this.OnFolderInputFieldChanged();
			});
			this.fileInput.onValueChanged.AddListener(async _ => {
				if (this.initializing > 0) {
					return;
				}

				Databind<IVirtualItem> selectedItem = (Databind<IVirtualItem>)this.fileList.SelectedDatabound;
				Databind<IVirtualItem> newSelectedItem = null;
				if (!Path.GetInvalidPathChars().Any(x => this.fileInput.text.Contains(x))) {
					string path = Path.GetFullPath(Path.Combine(this.folderTree.SelectedValue?.FullPath ?? string.Empty, this.fileInput.text));
					newSelectedItem = this.fileList.Children.Cast<Databind<IVirtualItem>>().FirstOrDefault(x => x.Value.FullPath == path);
					newSelectedItem ??= this.fileList.Children.Cast<Databind<IVirtualItem>>().FirstOrDefault(x => string.Compare(x.Value.FullPath, path, true) == 0);
				}

				if (selectedItem != newSelectedItem) {
					this.fileList.SelectedDatabound = newSelectedItem;
				} else {
					await this.UpdateButtonsAsync();
				}
			});
			this.fileInput.onSubmit.AddListener(async _ => {
				await this.SelectFileInputFileAsync();
			});
		}

		private VirtualItemTransformHandler transforms;

		/// <summary>
		/// Show the dialog and wait for the user to make a selection before continuing. 
		/// </summary>
		/// <param name="options">The options for the dialog.</param>
		/// <returns>The selected path or null for no path.</returns>
		public async Task<string> ShowAsync(FileBrowserOptions options) {
			this.initializing++;
			this.window.SetActive(false);
			this.background.SetActive(true);

			this.ret = null;
			this.options = options;

			this.transforms = new();
			if (options.AllowNavigateGob) {
				this.transforms.AddTransform(new GobTransform());
			}
			if (options.AllowNavigateLfd) {
				this.transforms.AddTransform(new LfdTransform());
			}

			IVirtualFolder root = await FileManager.Instance.GetByPathAsync(options.RootPath, this.transforms) as IVirtualFolder;
			root ??= await FileManager.Instance.GetByPathAsync(null, this.transforms) as IVirtualFolder;

			// Adjust starting folder.

			string startPath;
			if (options.StartSelectedFile != null) {
				startPath = Path.GetDirectoryName(options.StartSelectedFile);
			} else {
				startPath = options.StartPath ?? AppDomain.CurrentDomain.BaseDirectory;
			}
			IVirtualFolder startFolder = root;
			await foreach (IVirtualItem item in FileManager.Instance.GetHierarchyByPathAsync(startPath, this.transforms)) {
				IVirtualFolder folder = item as IVirtualFolder;
				if (folder == null) {
					break;
				}
				startFolder = folder;
			}

			if (options.SelectFolder) {
				options.Filters = new[] { FileType.Folders };
			} else {
				options.Filters ??= new[] { FileType.AllFiles };
			}

			this.fileTypesContainer.SetActive(!options.SelectFolder);
			this.fileTypes.ClearOptions();
			this.fileTypes.options.AddRange(options.Filters.Select(x => new TMP_Dropdown.OptionData() {
				text = x.DisplayName
			}));
			this.fileTypes.value = 1;
			this.fileTypes.value = 0;

			this.titleText.text = options.Title ?? "Select file";

			this.folderTree.ShowContainerAsSingleItem = true;
			this.folderTree.Container = root;

			this.folderTree.Clear();

			this.fileList.FileSearchPatterns = options.Filters[this.fileTypes.value].Wildcards;
			this.fileList.Container = startFolder;

			this.fileList.Clear();

			this.history.Clear();
			this.historyPos = -1;

			await this.folderTree.RefreshAsync();

			this.window.SetActive(true);

			await this.NavigateToFolderAsync(startFolder.FullPath);
			
			if (options.StartSelectedFile != null) {
				this.fileList.SelectedDatabound =
					this.fileList.Children.Cast<FileViewItem>().FirstOrDefault(x => x.Value.FullPath == options.StartSelectedFile) ??
					this.fileList.Children.Cast<FileViewItem>().FirstOrDefault(x => x.Value.FullPath.ToLower() == options.StartSelectedFile.ToLower());

				this.fileInput.text = Path.GetFileName(options.StartSelectedFile);
			}

			await this.UpdateButtonsAsync();

			this.initializing--;

			// Wait for the user to close the window.
			while (this.Visible) {
				await Task.Yield();
			}

			return this.ret;
		}

		private string ret;

		private readonly List<IVirtualFolder> history = new();
		private int historyPos = 0;

		private async Task SelectTreeNodeAsync(string path) {
			// We want to select the tree node referenced to by path.
			IVirtualFolder root = this.folderTree.Container;
			string current = path;

			// First we'll figure out which tree nodes are between the root and our destination.
			Stack<string> pathStack = new();
			while (!string.IsNullOrEmpty(current) && current != root.FullPath) {
				pathStack.Push(current);
				current = Path.GetDirectoryName(current);
			}

			this.initializing++;
			try {
				this.folderTree.SelectedDatabound = null;
			} finally {
				this.initializing--;
			}

			// Walk the stack and select each node as we go down the tree, looking for our destination.
			FileViewItem currentNode = (FileViewItem)this.folderTree.Children.First();
			currentNode.Expanded = true;

			FileView currentView = currentNode.ChildView;
			while (pathStack.Count > 0) {
				current = pathStack.Pop();
				currentNode = currentView.Children.Cast<FileViewItem>().FirstOrDefault(x => x.Value.FullPath == current);
				// If we don't find one of the nodes...
				if (currentNode == null) {
					// Refresh its children.
					await currentView.RefreshAsync();
					// Check again.
					currentNode = currentView.Children.Cast<FileViewItem>().FirstOrDefault(x => x.Value.FullPath == current);
					if (currentNode == null) {
						// Path doesn't exist (probably), give up.
						return;
					}
				}

				currentView = currentNode.ChildView;
				if (pathStack.Count > 0 && !currentNode.Expanded) {
					currentNode.Expanded = true;
				}
			}

			this.initializing++;
			try {
				this.folderTree.SelectedDatabound = currentNode;
			} finally {
				this.initializing--;
			}

			currentNode.EnsureVisible();
		}

		private Color backButtonTextColor;
		private Color forwardButtonTextColor;
		private Color upButtonTextColor;
		private Color selectButtonTextColor;
		private async Task UpdateButtonsAsync() {
			// Cache the initial color of the button text so we don't have to manually specify it in the inspector.
			if (this.backButtonTextColor == default) {
				this.backButtonTextColor = this.backButton.GetComponentInChildren<TMP_Text>(true).color;
				this.forwardButtonTextColor = this.forwardButton.GetComponentInChildren<TMP_Text>(true).color;
				this.upButtonTextColor = this.upButton.GetComponentInChildren<TMP_Text>(true).color;
				this.selectButtonTextColor = this.selectButton.GetComponentInChildren<TMP_Text>(true).color;
			}

			// Not sure why buttons don't let you change text color on button state, so we have to do it ourselves.
			this.backButton.interactable = this.historyPos > 0;
			this.backButton.GetComponentInChildren<TMP_Text>(true).color =
				this.backButton.interactable ? this.backButtonTextColor : this.disabledButtonTextColor;

			this.forwardButton.interactable = this.historyPos < this.history.Count - 1;
			this.forwardButton.GetComponentInChildren<TMP_Text>(true).color =
				this.forwardButton.interactable ? this.forwardButtonTextColor : this.disabledButtonTextColor;

			IVirtualFolder folder = this.history[this.historyPos];
			this.upButton.interactable = !string.IsNullOrEmpty(folder.FullPath) &&
				folder.FullPath != this.folderTree.Container.FullPath;
			this.upButton.GetComponentInChildren<TMP_Text>(true).color =
				this.upButton.interactable ? this.upButtonTextColor : this.disabledButtonTextColor;

			this.selectButton.interactable = await this.IsSelectionValidAsync();
			this.selectButton.GetComponentInChildren<TMP_Text>(true).color =
				this.selectButton.interactable ? this.selectButtonTextColor : this.disabledButtonTextColor;

			// If the selection is a folder to navigate into, change the button text.
			bool isFolder = this.fileList.SelectedValue is IVirtualFolder;

			if (isFolder && !this.options.SelectFolder) {
				this.selectButton.GetComponentInChildren<TMP_Text>(true).text = "Open";
			} else {
				this.selectButton.GetComponentInChildren<TMP_Text>(true).text = this.options.SelectButtonText ?? "Select";
			}
		}

		/// <summary>
		/// Move backward in the navigation history.
		/// </summary>
		public async void BackAsync() {
			if (this.historyPos <= 0) {
				return;
			}

			this.initializing++;
			try {
				this.historyPos--;

				IVirtualFolder folder = this.history[this.historyPos];
				this.fileList.SelectedDatabound = null;
				this.fileList.Container = folder;
				this.fileList.Clear();
				await this.fileList.RefreshAsync();

				await this.SelectTreeNodeAsync(folder.FullPath);

				this.folderInput.text = folder.FullPath ?? string.Empty;

				await this.UpdateButtonsAsync();
			} finally {
				this.initializing--;
			}
		}

		/// <summary>
		/// Move forward in the navigation history.
		/// </summary>
		public async void ForwardAsync() {
			if (this.historyPos >= this.history.Count - 1) {
				return;
			}

			this.initializing++;
			try {
				this.historyPos++;

				IVirtualFolder folder = this.history[this.historyPos];
				this.fileList.SelectedDatabound = null;
				this.fileList.Container = folder;
				this.fileList.Clear();
				await this.fileList.RefreshAsync();

				await this.SelectTreeNodeAsync(folder.FullPath);

				this.folderInput.text = folder.FullPath ?? "";

				await this.UpdateButtonsAsync();
			} finally {
				this.initializing--;
			}

		}

		/// <summary>
		/// Move up one folder.
		/// </summary>
		public async void UpAsync() {
			IVirtualFolder folder = this.history[this.historyPos];
			IVirtualFolder parent = folder.Parent;
			if (parent == null) {
				return;
			}

			this.initializing++;
			try {
				await this.NavigateToFolderAsync(parent.FullPath);
			} finally {
				this.initializing--;
			}
		}

		/// <summary>
		/// Navigate to a specific folder.
		/// </summary>
		/// <param name="path">The folder to navigate to.</param>
		public async Task NavigateToFolderAsync(string path) {
			this.fileList.SelectedDatabound = null;
			this.fileList.Clear();

			// These checks might not be necessary, we'll figure out later if the path isn't valid.
			IVirtualFolder folder = await FileManager.Instance.GetByPathAsync(path, this.transforms) as IVirtualFolder;

			this.initializing++;
			try {
				this.folderTree.SelectedDatabound = null;
			} finally {
				this.initializing--;
			}

			// We want to call this one first to update the file list first.
			await this.OnSelectedTreeNodeChangedAsync(path);

			if (folder != null) {
				await this.SelectTreeNodeAsync(path);
			}
		}

		/// <summary>
		/// Called by the folder input field.
		/// </summary>
		public async void OnFolderInputFieldChanged() {
			if (this.initializing > 0) {
				return;
			}

			this.initializing++;
			try {
				string path = this.folderInput.text;

				await this.NavigateToFolderAsync(path);
			} finally {
				this.initializing--;
			}
		}

		private async Task<bool> IsSelectionValidAsync() {
			// File Containers and Folders can be navigated into so the select button should be enabled.
			IVirtualItem selected = this.fileList.SelectedValue;
			if (selected?.FullPath != null && selected is IVirtualFolder) {
				return true;
			}

			string path = this.fileInput.text;
			if (this.options.SelectedFileMustExist) {
				if (string.IsNullOrWhiteSpace(path)) {
					return false;
				}

				if (path.Intersect(Path.GetInvalidPathChars()).Any()) {
					return false;
				}

				return (await FileManager.Instance.GetByPathAsync(Path.Combine(this.fileList.Container.FullPath, path), this.transforms)) != null;
			}

			if (this.options.SelectedPathMustExist) {
				if (string.IsNullOrWhiteSpace(path)) {
					IVirtualFolder container = this.fileList.Container;
					return (await FileManager.Instance.GetByPathAsync(container.FullPath, this.transforms)) != null;
				} else {
					if (path.Intersect(Path.GetInvalidPathChars()).Any()) {
						return false;
					}

					return (await FileManager.Instance.GetByPathAsync(Path.GetDirectoryName(Path.Combine(this.fileList.Container.FullPath, path)), this.transforms)) != null;
				}
			}

			if (string.IsNullOrWhiteSpace(path) && !this.options.SelectFolder) {
				return false;
			}

			if (this.options.ValidateFileName) {
				if (path.Intersect(Path.GetInvalidPathChars()).Any()) {
					return false;
				}
			}

			/*string[] fileSearchPatterns;
			if (this.fileList.FileSearchPatterns == null || this.fileList.FileSearchPatterns.Length == 0) {
				fileSearchPatterns = new[] { "*" };
			} else {
				fileSearchPatterns = this.fileList.FileSearchPatterns;
			}

			Regex patterns = new Regex("^(" + string.Join("|", fileSearchPatterns
			.Select(x => string.Join("", x.Select(x => x switch {
				'*' => ".*",
				'?' => ".",
				_ => Regex.Escape(x.ToString())
			})))) + ")$", RegexOptions.IgnoreCase);
			return patterns.IsMatch(Path.GetFileName(path));*/
			return true;
		}

		/// <summary>
		/// Refresh both the file list and folder tree.
		/// </summary>
		public async void RefreshAsync() {
			this.initializing++;
			try {
				this.fileList.Clear();
				this.folderTree.Clear();

				await this.fileList.RefreshAsync();
				await this.folderTree.RefreshAsync();

				/*FileViewItem singleChild = (FileViewItem)this.folderTree.Databinders.SingleOrDefault();
				if (singleChild != null) {
					singleChild.Expanded = true;
				}*/

				await this.SelectTreeNodeAsync(this.fileList.Container.FullPath);
			} finally {
				this.initializing--;
			}
		}

		/// <summary>
		/// Navigate the file list view to the listed path. Doesn't need to be tied to the selected tree node despite the name.
		/// </summary>
		/// <param name="path">Path to navigate to. Null to show all the drives in the root.</param>
		public async Task OnSelectedTreeNodeChangedAsync(string path = null) {
			FileViewItem folder = (FileViewItem)this.folderTree.SelectedDatabound;
			IVirtualFolder info;
			// If there's a selected tree node we can use its value to populate the file list.
			// Otherwise we have to create one from scratch.
			// TODO Helper function to turn any path into a FileSystemItem. Can be used by FileView as well.
			if (folder == null) {
				info = await FileManager.Instance.GetByPathAsync(path, this.transforms) as IVirtualFolder;
			} else {
				info = folder.Value as IVirtualFolder;
			}

			this.folderInput.text = info.FullPath ?? string.Empty;

			this.fileList.Container = info;

			await this.fileList.RefreshAsync();

			this.historyPos++;
			this.history.Insert(this.historyPos, info);
			this.history.RemoveRange(this.historyPos + 1, this.history.Count - this.historyPos - 1);

			this.OnSelectedFileChanged();
		}

		/// <summary>
		/// Called by file list when the selection changes.
		/// </summary>
		public async void OnSelectedFileChanged() {
			string path = this.fileList.SelectedValue?.FullPath;
			this.fileInput.text = path != null ? Path.GetFileName(path) : string.Empty;

			await this.UpdateButtonsAsync();
		}

		/// <summary>
		/// Called when the file input value is changed.
		/// </summary>
		public async Task SelectFileInputFileAsync() {
			if (this.initializing > 0) {
				return;
			}

			this.initializing++;
			try {
				string path = this.fileInput.text;
				FileViewItem item = null;
				string fullPath = path;
				if (!string.IsNullOrWhiteSpace(path)) {
					string basePath = this.folderTree.SelectedValue.FullPath;
					if (basePath != null) {
						fullPath = Path.GetFullPath(Path.Combine(basePath, path));
					} else {
						fullPath = Path.GetFullPath(path);
					}

					string folder = Path.GetDirectoryName(fullPath);
					if (folder == null) {
						folder = fullPath;
					}

					if (folder != this.folderTree.SelectedValue.FullPath) {
						await this.NavigateToFolderAsync(folder);
					}

					item = this.fileList.Children.Cast<FileViewItem>().FirstOrDefault(x => x.Value.FullPath == fullPath);
				}

				this.fileList.SelectedDatabound = item;
				if (item != null) {
					item.EnsureVisible();
				}
				this.fileInput.text = (fullPath != null ? Path.GetFileName(fullPath) : null) ?? string.Empty;

				await this.UpdateButtonsAsync();
			} finally {
				this.initializing--;
			}
		}

		/// <summary>
		/// Called when the cancel button is clicked.
		/// </summary>
		public void Cancel() {
			// Set the return value and hide the window.
			this.ret = null;
			this.transforms = null;
			this.fileList.Clear();
			this.folderTree.Clear();
			this.window.SetActive(false);
			this.background.SetActive(false);
		}

		/// <summary>
		/// Open a folder or select a file.
		/// </summary>
		public async Task SelectOrOpenAsync(bool doubleClick) {
			if (!await this.IsSelectionValidAsync()) {
				return;
			}

			IVirtualItem selected = this.fileList.SelectedValue;

			// Navigate if double click on non-file

			if (selected is IVirtualFolder) {
				if (doubleClick || !this.options.SelectFolder) {
					await this.NavigateToFolderAsync(selected.FullPath);
					return;
				}
			}

			// Set the return value and hide the window.
			this.ret = Path.Combine(this.fileList.Container.FullPath, this.fileInput.text);
			this.transforms = null;
			this.fileList.Clear();
			this.folderTree.Clear();
			this.window.SetActive(false);
			this.background.SetActive(false);
		}

		/// <summary>
		/// Called by the select button.
		/// </summary>
		public async void OnSelectButtonClickedAsync() {
			this.initializing++;
			try {
				await this.SelectOrOpenAsync(false);
			} finally {
				this.initializing--;
			}
		}

		/// <summary>
		/// Whether the dialog is visible.
		/// </summary>
		public bool Visible => this.background.activeSelf;

		/// <summary>
		/// Called when the value is changed in a FileView.
		/// </summary>
		/// <param name="view">Where the value was changed.</param>
		public async Task OnSelectedValueChangedAsync(FileView view) {
			if (this.initializing > 0) {
				return;
			}

			this.initializing++;
			try {
				// If it was the file list where the selection changed, update the select button.
				if (view == this.fileList) {
					await this.UpdateButtonsAsync();
					return;
				}

				// Otherwise navigate the file list to the selected tree node.
				await this.NavigateToFolderAsync(this.folderTree.SelectedValue.FullPath);
			} finally {
				this.initializing--;
			}
		}

		/// <summary>
		/// Called when a file view item is selecteed.
		/// </summary>
		/// <param name="item">The selected item.</param>
		public async Task OnSelectedValueChangedAsync(FileViewItem item) {
			if (this.initializing > 0) {
				return;
			}

			this.initializing++;
			try {
				if (((FileView)item.Parent)?.ToggleGroup == this.fileList.ToggleGroup) {
					// Update the path displayed for the selected item, and the select button state.
					this.fileInput.text = Path.GetFileName(item.Value.FullPath);

					await this.UpdateButtonsAsync();
					return;
				}

				// Otherwise navigate the file list to the selected tree node.
				await this.NavigateToFolderAsync(this.folderTree.SelectedValue?.FullPath);
			} finally {
				this.initializing--;
			}
		}

		/// <summary>
		/// Called when the user double clicks an entry.
		/// </summary>
		/// <param name="item">The item double clicked.</param>
		public async Task OnDoubleClickAsync(FileViewItem item) {
			// Only respond to double clicks on file list entries.
			if (((FileView)item.Parent)?.ToggleGroup != this.fileList.ToggleGroup) {
				return;
			}

			this.initializing++;
			try {
				await this.SelectOrOpenAsync(true);
			} finally {
				this.initializing--;
			}
		}

		private void OnApplicationQuit() {
			// This helps prevent errors due to async/await still running when editor stops play mode.
			if (!this.Visible) {
				return;
			}

			this.Cancel();
		}

		/// <summary>
		/// Called when a tree node is expanded.
		/// </summary>
		/// <param name="item">The expanded item.</param>
		public async Task OnExpandedAsync(FileViewItem item) {
			if (this.initializing > 0) {
				return;
			}

			// If there are no child folders, add them.
			if (!item.ChildView.Any()) {
				await item.ChildView.RefreshAsync();
			}
		}

		public async void OnFilterChanged() {
			if (this.initializing > 0) {
				return;
			}

			this.fileList.FileSearchPatterns = this.options.Filters[this.fileTypes.value].Wildcards;

			this.initializing++;
			try {
				this.fileList.Clear();
				await this.fileList.RefreshAsync();
			} finally {
				this.initializing--;
			}
		}

		public int FilterIndex => this.fileTypes.value;
	}
}
