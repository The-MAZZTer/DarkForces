using MZZT.DarkForces.FileFormats;
using MZZT.DataBinding;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace MZZT {
	/// <summary>
	/// Different types of file system items.
	/// </summary>
	public enum FileSystemItemTypes {
		/// <summary>
		/// The root (My Computer).
		/// </summary>
		Root,
		/// <summary>
		/// A file.
		/// </summary>
		File,
		/// <summary>
		/// A file inside of a GOB or LFD.
		/// </summary>
		FileContainee,
		/// <summary>
		/// A GOB or LFD which can be browsed inside.
		/// </summary>
		FileContainer,
		/// <summary>
		/// A folder.
		/// </summary>
		Folder
	}

	/// <summary>
	/// The data about a file system item, sufficient to display an entry in the file browser.
	/// </summary>
	[Serializable]
	public struct FileSystemItem {
		/// <summary>
		/// Full path to the file or folder.
		/// </summary>
		public string FilePath;
		/// <summary>
		/// The display name.
		/// </summary>
		public string DisplayName;
		/// <summary>
		/// The type of the item.
		/// </summary>
		public FileSystemItemTypes Type;
		/// <summary>
		/// If the file is inside a container, at what offset does it start.
		/// </summary>
		public long ContainerOffset;
		/// <summary>
		/// The size of the file.
		/// </summary>
		public long Size;

		/// <summary>
		/// Convert the size to a user readable string.
		/// </summary>
		public string DisplaySize {
			get {
				if (this.Type == FileSystemItemTypes.Root || this.Type == FileSystemItemTypes.Folder) {
					return "";
				}

				string prefix = "BKMGTPEZY??????????????????????";
				float size = this.Size;
				int pos = 0;
				while (size >= 1000) {
					size /= 1024;
					pos++;
				}
				if (pos == 0) {
					return $"{size:0} bytes";
				}
				return $"{size:0.##} {prefix[pos]}B";
			}
		}

		/// <summary>
		/// The material icon glyph to use for the item.
		/// </summary>
		public string IconGlyph {
			get {
				// Unity doesn't support ligatures so we need to use the code point.
				switch (this.Type) {
					case FileSystemItemTypes.Root:
						return "\ue30a";
					case FileSystemItemTypes.Folder:
						if (Path.GetDirectoryName(this.FilePath) == null) {
							return "\ue1db";
						} else {
							return "\ue2c7";
						}
					case FileSystemItemTypes.FileContainer:
						return "\uf1c4";
					case FileSystemItemTypes.FileContainee:
					case FileSystemItemTypes.File:
					default:
						return "\ue24d";
				}
			}
		}
	}

	/// <summary>
	/// A container representing a folder, which will display a list of files.
	/// </summary>
	public class FileView : DataboundList<FileSystemItem> {
		[SerializeField]
		private FileSystemItem container;
		/// <summary>
		/// The item representing the container this FileView displays the items of.
		/// </summary>
		public FileSystemItem Container { get => this.container; set => this.container = value; }
		[SerializeField]
		private string[] fileSearchPatterns = Array.Empty<string>();
		/// <summary>
		/// Filters for files to display (if empty, no files are displayed).
		/// </summary>
		public string[] FileSearchPatterns { get => this.fileSearchPatterns; set => this.fileSearchPatterns = value; }
		[SerializeField]
		private bool allowNavigateGob = true;
		/// <summary>
		/// Allow clicking on GOB files to navigate into them.
		/// </summary>
		public bool AllowNavigateGob { get => this.allowNavigateGob; set => this.allowNavigateGob = value; }
		[SerializeField]
		private bool allowNavigateLfd = true;
		/// <summary>
		/// Allow clicking on LFD files to navigate into them.
		/// </summary>
		public bool AllowNavigateLfd { get => this.allowNavigateLfd; set => this.allowNavigateLfd = value; }
		[SerializeField]
		private GameObject emptyMessageContainer = null;
		[SerializeField]
		private TMP_Text emptyMessageText = null;
		[SerializeField]
		private GameObject headerSpacer = null;

		/// <summary>
		/// Refresh the list of files displayed.
		/// </summary>
		public async Task RefreshAsync() {
			if (this.emptyMessageContainer != null) {
				this.emptyMessageContainer.SetActive(false);
			}

			this.Clear();

			string[] fileSearchPatterns;
			if (this.fileSearchPatterns == null || this.fileSearchPatterns.Length == 0) {
				fileSearchPatterns = Array.Empty<string>();
			} else {
				fileSearchPatterns = this.fileSearchPatterns;
			}

			// Get the children of the current container.
			FileSystemItem[] children;
			switch (this.container.Type) {
				case FileSystemItemTypes.Root: {
					children = DriveInfo.GetDrives()
						.Select(x => {
							long size = 0;
							// The label is the friendly name of the drive in .NET, but Mono just has the drive letter!
							/*string label = x.Name;
							try {
								label = x.VolumeLabel;
							} catch (UnauthorizedAccessException) {
							} catch (IOException) {
							}*/
							try {
								size = x.TotalSize;
							} catch (UnauthorizedAccessException) {
							} catch (IOException) {
							}
							return new FileSystemItem() {
								FilePath = x.Name,
								DisplayName = x.Name, //$"{label} ({x.Name})",
								Type = FileSystemItemTypes.Folder,
								Size = size
							};
						})
						.OrderBy(x => x.FilePath.ToUpper())
						.ToArray();
					if (children.Length == 0) {
						this.ShowError("No items found.");
					}
				} break;
				case FileSystemItemTypes.Folder: {
					try {
						children = Directory.EnumerateDirectories(this.container.FilePath)
							.Select(x => new FileSystemItem() {
								FilePath = x,
								DisplayName = Path.GetFileName(x),
								Type = FileSystemItemTypes.Folder
							})
							.OrderBy(x => x.DisplayName.ToUpper())
							.Concat(
								fileSearchPatterns
								.SelectMany(x => Directory.EnumerateFiles(this.container.FilePath, x))
								.Distinct()
								.Select(x => {
									string ext = Path.GetExtension(x).ToUpper();
									return new FileSystemItem() {
										FilePath = x,
										DisplayName = Path.GetFileName(x),
										Type = ((this.allowNavigateGob && ext == ".GOB") ||
											(this.allowNavigateLfd && ext == ".LFD")) ?
											FileSystemItemTypes.FileContainer : FileSystemItemTypes.File,
										Size = new FileInfo(x).Length
									};
								})
								.OrderBy(x => Path.GetExtension(x.FilePath).ToUpper())
								.ThenBy(x => x.DisplayName.ToUpper())
							).ToArray();
						if (children.Length == 0) {
							this.ShowError("No items found.");
						}
					} catch (IOException e) {
						children = Array.Empty<FileSystemItem>();
						this.ShowError(e.Message);
					} catch (UnauthorizedAccessException e) {
						children = Array.Empty<FileSystemItem>();
						this.ShowError(e.Message);
					}
				} break;
				case FileSystemItemTypes.FileContainer: {
					string ext = Path.GetExtension(this.container.FilePath).ToUpper();
					Regex patterns = new Regex("^(" + string.Join("|", fileSearchPatterns
						.Select(x => string.Join("", x.Select(x => x switch {
							'*' => ".*",
							'?' => ".",
							_ => Regex.Escape(x.ToString())
						})))) + ")$", RegexOptions.IgnoreCase);
					switch (ext) {
						case ".GOB": {
							if (!this.allowNavigateGob) {
								children = Array.Empty<FileSystemItem>();
								this.ShowError("Invalid location.");
								break;
							}

							DfGobContainer gob = await DfGobContainer.ReadAsync(this.container.FilePath, false);
							children = gob.Files
								.Where(x => patterns.IsMatch(x.name))
								.Select(x => new FileSystemItem() {
									FilePath = $"{this.container.FilePath}{Path.DirectorySeparatorChar}{x.name}",
									DisplayName = x.name,
									Type = FileSystemItemTypes.FileContainee,
									ContainerOffset = x.offset,
									Size = x.size
								})
								.OrderBy(x => Path.GetExtension(x.FilePath).ToUpper())
								.ThenBy(x => x.DisplayName.ToUpper())
								.ToArray();
						} break;
						case ".LFD": {
							if (!this.allowNavigateLfd) {
								children = Array.Empty<FileSystemItem>();
								this.ShowError("Invalid location.");
								break;
							}

							LandruFileDirectory lfd = await LandruFileDirectory.ReadAsync(this.container.FilePath);
							children = lfd.Files
								.Where(x => patterns.IsMatch(x.name))
								.Select(x => new FileSystemItem() {
									FilePath = $"{this.container.FilePath}{Path.DirectorySeparatorChar}{x.name}",
									DisplayName = x.name,
									Type = FileSystemItemTypes.FileContainee,
									ContainerOffset = x.offset,
									Size = x.size
								})
								.OrderBy(x => Path.GetExtension(x.FilePath).ToUpper())
								.ThenBy(x => x.DisplayName.ToUpper())
								.ToArray();
						} break;
						default: {
							this.ShowError("Invalid location.");
						} return;
					}
				} break;
				default: {
					this.ShowError("Invalid location.");
				} return;
			}

			/*foreach (FileSystemItem item in children) {
				this.Add(item);
				await Task.Delay(1);
			}*/
			this.AddRange(children);

			Canvas.ForceUpdateCanvases();

			if (this.headerSpacer != null) {
				this.headerSpacer.SetActive(((RectTransform)this.transform).rect.height > ((RectTransform)this.transform.parent).rect.height);
			}
		}

		private void ShowError(string error) {
			if (this.emptyMessageText != null) {
				this.emptyMessageText.text = error;
			}
			if (this.emptyMessageContainer != null) {
				this.emptyMessageContainer.SetActive(true);
			}
		}

		public override Databound<FileSystemItem> SelectedDatabound {
			get => base.SelectedDatabound;
			set {
				if (base.SelectedDatabound == value) {
					return;
				}
				base.SelectedDatabound = value;

				_ = FileBrowser.Instance.OnSelectedValueChangedAsync(this);
			}
		}

		protected override Databound<FileSystemItem> Instantiate(FileSystemItem item) {
			FileViewItem databound = (FileViewItem)base.Instantiate(item);
			if (databound.ChildView != null) {
				// Workaround Unity bug where you can't give a prefab a reference to the prefab, only to its own root.
				databound.ChildView.DatabinderTemplate = this.DatabinderTemplate;
			}
			return databound;
		}
	}
}
