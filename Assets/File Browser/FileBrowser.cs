using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MZZT {
	/// <summary>
	/// This class encompases the ability for the user to select a file or folder from a browse dialog.
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
		private FileViewItem folderRoot = null;
		[SerializeField]
		private FileView folderTree = null;
		[SerializeField]
		private FileView fileList = null;
		[SerializeField]
		private TMP_InputField fileInput = null;
		[SerializeField]
		private Button selectButton = null;
		[SerializeField, Header("Colors")]
		private Color disabledButtonTextColor = default;

		/// <summary>
		/// Options to use in the current call to the file browser.
		/// </summary>
		public struct FileBrowserOptions {
			/// <summary>
			/// The title to display at the top.
			/// </summary>
			public string Title;
			/// <summary>
			/// The Select button text.
			/// </summary>
			public string SelectButtonText;
			/// <summary>
			/// The lowest path the user can navigate to in the tree view.
			/// </summary>
			public string RootPath;
			/// <summary>
			/// The location to start the file browser.
			/// </summary>
			public string StartPath;
			/// <summary>
			/// The file to start selected. Overrides StartPath.
			/// </summary>
			public string StartSelectedFile;
			/// <summary>
			/// Whether the user should be selecting a folder instead of a file.
			/// </summary>
			public bool SelectFolder;
			/// <summary>
			/// Whether GOBs should be treated as folders and files inside them can be selected.
			/// </summary>
			public bool AllowNavigateGob;
			/// <summary>
			/// Whether LFDs should be treated as folders and files inside them can be selected.
			/// </summary>
			public bool AllowNavigateLfd;
			/// <summary>
			/// The files to display (in addition to folders).
			/// </summary>
			public string[] FileSearchPatterns;
			/// <summary>
			/// Check the input file exists before allowing it to be selected.
			/// </summary>
			public bool SelectedFileMustExist;
			/// <summary>
			/// Check the folder of the input file exists before allowing it to be selected.
			/// </summary>
			public bool SelectedPathMustExist;
			/// <summary>
			/// Ensure the input path has only valid characters before allowing it.
			/// </summary>
			public bool ValidateFileName;
		}

		private FileBrowserOptions options;

		private int initializing = 0;

		private void Start() {
			this.folderInput.onSubmit.AddListener(_ => {
				this.OnFolderInputFieldChanged();
			});
			this.fileInput.onSubmit.AddListener(async _ => {
				await this.SelectFileInputFileAsync();
			});
		}

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

			// TODO Root display name is hardcoded in the current prefab so if root is changed the display name won't.

			FileSystemItem root = new FileSystemItem() {
				DisplayName = "My Computer",
				FilePath = options.RootPath,
				Type = FileSystemItemTypes.Root
			};
			if (options.RootPath != null) {
				bool dirExists = Directory.Exists(options.RootPath);
				if (!dirExists) {
					bool fileExists = File.Exists(options.RootPath);
					if (!fileExists) {
						throw new DirectoryNotFoundException($"Can't find {options.RootPath}!");
					}

					string ext = Path.GetExtension(options.RootPath).ToUpper();
					if (ext != ".GOB" && ext != ".LFD") {
						throw new DirectoryNotFoundException($"Can't find {options.RootPath}!");
					}
					if (ext == ".GOB" && !options.AllowNavigateGob) {
						throw new DirectoryNotFoundException($"Can't find {options.RootPath}!");
					}
					if (ext == ".LFD" && !options.AllowNavigateLfd) {
						throw new DirectoryNotFoundException($"Can't find {options.RootPath}!");
					}
					root.Type = FileSystemItemTypes.FileContainer;
				} else {
					root.Type = FileSystemItemTypes.Folder;
				}
				root.DisplayName = options.RootPath;
			}
			this.folderRoot.Value = root;

			// Adjust starting folder.

			FileSystemItem start = new FileSystemItem() {
				FilePath = AppDomain.CurrentDomain.BaseDirectory,
				Type = FileSystemItemTypes.Folder
			};
			if (options.StartSelectedFile != null) {
				options.StartPath = Path.GetDirectoryName(options.StartSelectedFile);
			}
			if (options.StartPath != null) {
				bool dirExists = Directory.Exists(options.StartPath);
				if (!dirExists) {
					bool fileExists = File.Exists(options.StartPath);
					if (!fileExists) {
						throw new DirectoryNotFoundException($"Can't find {options.StartPath}!");
					}

					string ext = Path.GetExtension(options.StartPath).ToUpper();
					if (ext != ".GOB" && ext != ".LFD") {
						throw new DirectoryNotFoundException($"Can't find {options.StartPath}!");
					}
					if (ext == ".GOB" && !options.AllowNavigateGob) {
						throw new DirectoryNotFoundException($"Can't find {options.StartPath}!");
					}
					if (ext == ".LFD" && !options.AllowNavigateLfd) {
						throw new DirectoryNotFoundException($"Can't find {options.StartPath}!");
					}
					start.Type = FileSystemItemTypes.FileContainer;
				} else {
					start.Type = FileSystemItemTypes.Folder;
				}
			}

			this.titleText.text = options.Title ?? "Select file";
			this.folderTree.AllowNavigateGob = options.AllowNavigateGob;
			this.folderTree.AllowNavigateLfd = options.AllowNavigateLfd;

			List<string> containerPatterns = new List<string>();
			if (options.AllowNavigateGob) {
				containerPatterns.Add("*.GOB");
			}
			if (options.AllowNavigateLfd) {
				containerPatterns.Add("*.LFD");
			}

			this.folderTree.FileSearchPatterns = containerPatterns.ToArray();
			this.folderTree.Container = root;

			this.folderTree.Clear();

			this.fileList.AllowNavigateGob = options.AllowNavigateGob;
			this.fileList.AllowNavigateLfd = options.AllowNavigateLfd;
			this.fileList.FileSearchPatterns = options.FileSearchPatterns;
			this.fileList.Container = start;

			this.fileList.Clear();

			this.history.Clear();
			this.historyPos = -1;

			await this.folderTree.RefreshAsync();

			this.window.SetActive(true);

			await this.NavigateToFolderAsync(options.StartPath);

			if (options.StartSelectedFile != null) {
				string startFile = options.StartSelectedFile.ToUpper();
				this.fileList.SelectedDatabound = this.fileList.Databinders.FirstOrDefault(x => x.Value.FilePath.ToUpper() == startFile);

				this.fileInput.text = Path.GetFileName(options.StartSelectedFile);
			}

			this.UpdateButtons();

			this.initializing--;

			// Wait for the user to close the window.
			while (this.Visible) {
				await Task.Delay(25);
			}

			return this.ret;
		}

		private string ret;

		private readonly List<FileSystemItem> history = new List<FileSystemItem>();
		private int historyPos = 0;

		private async Task SelectTreeNodeAsync(string path) {
			// We want to select the tree node referenced to by path.
			FileSystemItem root = this.folderTree.Container;
			string current = path?.ToUpper();

			// First we'll figure out which tree nodes are between the root and our destination.
			Stack<string> pathStack = new Stack<string>();
			while (!string.IsNullOrEmpty(current) && current != root.FilePath?.ToUpper()) {
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
			FileViewItem currentNode = this.folderRoot;
			FileView currentView = this.folderTree;
			while (pathStack.Count > 0) {
				current = pathStack.Pop();
				currentNode = (FileViewItem)currentView.Databinders.FirstOrDefault(x => x.Value.FilePath.ToUpper() == current);
				// If we don't find one of the nodes...
				if (currentNode == null) {
					// Refresh its children.
					await currentView.RefreshAsync();
					// Check again.
					currentNode = (FileViewItem)currentView.Databinders.FirstOrDefault(x => x.Value.FilePath.ToUpper() == current);
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
		private void UpdateButtons() {
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

			FileSystemItem folder = this.history[this.historyPos];
			this.upButton.interactable = !string.IsNullOrEmpty(folder.FilePath) &&
				folder.FilePath.ToUpper() != this.folderTree.Container.FilePath?.ToUpper();
			this.upButton.GetComponentInChildren<TMP_Text>(true).color =
				this.upButton.interactable ? this.upButtonTextColor : this.disabledButtonTextColor;

			this.selectButton.interactable = this.SelectionValid;
			this.selectButton.GetComponentInChildren<TMP_Text>(true).color =
				this.selectButton.interactable ? this.selectButtonTextColor : this.disabledButtonTextColor;

			// If the selection is a folder to navigate into, change the button text.
			if (this.fileList.SelectedDatabound != null &&
				this.fileList.SelectedValue.Type == FileSystemItemTypes.Folder ||
				this.fileList.SelectedValue.Type == FileSystemItemTypes.FileContainer) {
				
				this.selectButton.GetComponentInChildren<TMP_Text>(true).text = "Open";
			} else {
				this.selectButton.GetComponentInChildren<TMP_Text>(true).text =
					this.options.SelectButtonText ?? "Select";
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

				FileSystemItem folder = this.history[this.historyPos];
				this.fileList.SelectedDatabound = null;
				this.fileList.Container = folder;
				this.fileList.Clear();
				await this.fileList.RefreshAsync();

				await this.SelectTreeNodeAsync(folder.FilePath);

				this.UpdateButtons();
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

				FileSystemItem folder = this.history[this.historyPos];
				this.fileList.SelectedDatabound = null;
				this.fileList.Container = folder;
				this.fileList.Clear();
				await this.fileList.RefreshAsync();

				await this.SelectTreeNodeAsync(folder.FilePath);

				this.UpdateButtons();
			} finally {
				this.initializing--;
			}

		}

		/// <summary>
		/// Move up one folder.
		/// </summary>
		public async void UpAsync() {
			FileSystemItem folder = this.history[this.historyPos];
			if (string.IsNullOrEmpty(folder.FilePath)) {
				return;
			}

			this.initializing++;
			try {
				string newFolder = Path.GetDirectoryName(folder.FilePath);
				await this.NavigateToFolderAsync(newFolder);
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
			bool exists;
			if (string.IsNullOrEmpty(path)) {
				exists = true;
			} else {
				bool dirExists = Directory.Exists(path);
				if (!dirExists) {
					bool fileExists = File.Exists(path);
					if (!fileExists) {
						exists = false;
					} else {
						string ext = Path.GetExtension(path).ToUpper();
						if (ext != ".GOB" && ext != ".LFD") {
							exists = false;
						} else if (ext == ".GOB" && !this.folderTree.AllowNavigateGob) {
							exists = false;
						} else if (ext == ".LFD" && !this.folderTree.AllowNavigateLfd) {
							exists = false;
						} else {
							exists = true;
						}
					}
				} else {
					exists = true;
				}
			}

			this.initializing++;
			try {
				this.folderTree.SelectedDatabound = null;
			} finally {
				this.initializing--;
			}

			// We want to call this one first to update the file list first.
			await this.OnSelectedTreeNodeChangedAsync(path);

			if (exists) {
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

		private bool SelectionValid {
			get {
				// File Containers and Folders can be navigated into so the select button should be enabled.
				FileSystemItem selected = this.fileList.SelectedValue;
				if (selected.FilePath != null && (selected.Type == FileSystemItemTypes.FileContainer || selected.Type == FileSystemItemTypes.Folder)) {
					return true;
				}

				if (this.options.SelectedFileMustExist) {
					if (string.IsNullOrWhiteSpace(this.fileInput.text)) {
						return false;
					}

					return File.Exists(Path.Combine(this.fileList.Container.FilePath, this.fileInput.text));
				}

				if (this.options.SelectedPathMustExist) {
					FileSystemItem container = this.fileList.Container;
					if (container.Type == FileSystemItemTypes.FileContainer) {
						return File.Exists(this.fileList.Container.FilePath);
					} else {
						return Directory.Exists(this.fileList.Container.FilePath);
					}
				}

				string path = this.fileInput.text;
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

				await this.SelectTreeNodeAsync(this.fileList.Container.FilePath);
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
			FileSystemItem info;
			// If there's a selected tree node we can use its value to populate the file list.
			// Otherwise we have to create one from scratch.
			// TODO Helper function to turn any path into a FileSystemItem. Can be used by FileView as well.
			if (folder == null) {
				FileSystemItemTypes type;
				if (path == null) {
					type = FileSystemItemTypes.Root;
				} else {
					bool dirExists = Directory.Exists(path);
					if (!dirExists) {
						bool fileExists = File.Exists(path);
						if (fileExists) {
							string ext = Path.GetExtension(options.RootPath).ToUpper();
							if ((ext == ".GOB" && options.AllowNavigateGob) ||
								(ext == ".LFD" && options.AllowNavigateLfd)) {

								type = FileSystemItemTypes.FileContainer;
							} else {
								type = FileSystemItemTypes.File;
							}
						} else {
							type = (FileSystemItemTypes)(-1);
						}
					} else {
						type = FileSystemItemTypes.Folder;
					}
				}

				info = new FileSystemItem() {
					FilePath = path,
					Type = type
				};
			} else {
				info = folder.Value;
			}

			this.folderInput.text = info.FilePath ?? "";

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
		public void OnSelectedFileChanged() {
			string path = this.fileList.SelectedValue.FilePath;
			this.fileInput.text = path != null ? Path.GetFileName(path) : "";

			this.UpdateButtons();
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
					string basePath = this.folderTree.SelectedValue.FilePath;
					if (basePath != null) {
						fullPath = Path.GetFullPath(Path.Combine(basePath, path));
					} else {
						fullPath = Path.GetFullPath(path);
					}

					string folder = Path.GetDirectoryName(fullPath);
					if (folder == null) {
						folder = fullPath;
					}

					if (folder.ToUpper() != this.folderTree.SelectedValue.FilePath?.ToUpper()) {
						await this.NavigateToFolderAsync(folder);
					}

					item = (FileViewItem)this.fileList.Databinders.FirstOrDefault(x => x.Value.FilePath.ToUpper() == fullPath.ToUpper());
				}

				this.fileList.SelectedDatabound = item;
				if (item != null) {
					item.EnsureVisible();
				}
				this.fileInput.text = (fullPath != null ? Path.GetFileName(fullPath) : null) ?? "";

				this.UpdateButtons();
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
			this.fileList.Clear();
			this.folderTree.Clear();
			this.window.SetActive(false);
			this.background.SetActive(false);
		}

		/// <summary>
		/// Open a folder or select a file.
		/// </summary>
		public async Task SelectOrOpenAsync() {
			if (!this.SelectionValid) {
				return;
			}

			FileSystemItem selected = this.fileList.SelectedValue;
			if (selected.FilePath != null && (selected.Type == FileSystemItemTypes.FileContainer || selected.Type == FileSystemItemTypes.Folder)) {
				if (!this.options.SelectFolder) {
					await this.NavigateToFolderAsync(selected.FilePath);
					return;
				}
			}

			// Set the return value and hide the window.
			this.ret = Path.Combine(this.fileList.Container.FilePath, this.fileInput.text);
			this.fileList.Clear();
			this.folderTree.Clear();
			this.window.SetActive(false);
			this.background.SetActive(false);
		}

		/// <summary>
		/// Called by the select button.
		/// </summary>
		public async void OnSelectButtonClickedAsync() {
			await this.SelectOrOpenAsync();
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
					this.UpdateButtons();
					return;
				}

				// Otherwise navigate the file list to the selected tree node.
				await this.NavigateToFolderAsync(this.folderTree.SelectedValue.FilePath);
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
				if (item.Toggle.group == this.fileList.ToggleGroup) {
					// Update the path displayed for the selected item, and the select button state.
					this.fileInput.text = Path.GetFileName(item.Value.FilePath);

					this.UpdateButtons();
					return;
				}

				// Otherwise navigate the file list to the selected tree node.
				await this.NavigateToFolderAsync(this.folderTree.SelectedValue.FilePath);
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
			if (item.Toggle.group != this.fileList.ToggleGroup) {
				return;
			}

			this.initializing++;
			try {
				await this.SelectOrOpenAsync();
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
			if (!item.ChildView.Values.Any()) {
				await item.ChildView.RefreshAsync();
			}
		}
	}
}
