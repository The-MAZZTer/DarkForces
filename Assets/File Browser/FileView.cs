using MZZT.Data.Binding;
using MZZT.IO.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace MZZT {
	/// <summary>
	/// A container representing a folder, which will display a list of files.
	/// </summary>
	public class FileView : DataboundList<IVirtualItem> {
		[Header("File View"), SerializeField]
		private string[] fileSearchPatterns = Array.Empty<string>();
		/// <summary>
		/// Filters for files to display (if empty, no files are displayed).
		/// </summary>
		public string[] FileSearchPatterns { get => this.fileSearchPatterns; set => this.fileSearchPatterns = value; }
		[SerializeField]
		private GameObject emptyMessageContainer = null;
		[SerializeField]
		private TMP_Text emptyMessageText = null;
		[SerializeField]
		private GameObject headerSpacer = null;

		public bool ShowContainerAsSingleItem { get; set; }
		private IVirtualFolder container;
		/// <summary>
		/// The item representing the container this FileView displays the items of.
		/// </summary>
		public IVirtualFolder Container { get => this.container ?? (IVirtualFolder)this.ParentValue; set => this.container = value; }

		/// <summary>
		/// Refresh the list of files displayed.
		/// </summary>
		public async Task RefreshAsync() {
			if (this.emptyMessageContainer != null) {
				this.emptyMessageContainer.SetActive(false);
			}

			this.Clear();

			if (!this.ShowContainerAsSingleItem) {
				Regex[] fileSearchPatterns;
				if (this.fileSearchPatterns == null || this.fileSearchPatterns.Length == 0) {
					fileSearchPatterns = Array.Empty<Regex>();
				} else {
					fileSearchPatterns = this.fileSearchPatterns.Select(x => new Regex("^" + Regex.Escape(x).Replace("\\*", ".*").Replace("\\?", ".") + "$", RegexOptions.IgnoreCase)).ToArray();
				}

				// Get the children of the current container.
				List<IVirtualItem> items = new();
				await foreach (IVirtualItem item in this.container.GetChildrenAsync()) {
					if (item is IVirtualFile file) {
						if (fileSearchPatterns.Length == 0 || !fileSearchPatterns.Any(x => x.IsMatch(item.Name))) {
							continue;
						}
					}

					items.Add(item);
				}
				items.Sort((a, b) => {
					if (a is IVirtualFolder && b is not IVirtualFolder) {
						return -1;
					} else if (b is IVirtualFolder && a is not IVirtualFolder) {
						return 1;
					}
					return string.Compare(a.Name, b.Name);
				});
				this.AddRange(items);

				if (items.Count == 0) {
					this.ShowError("No items found.");
				}
			} else {
				this.Add(this.container);
			}

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

		public override IDatabind SelectedDatabound {
			get => base.SelectedDatabound;
			set {
				if (base.SelectedDatabound == value) {
					return;
				}
				base.SelectedDatabound = value;

				_ = FileBrowser.Instance.OnSelectedValueChangedAsync(this);
			}
		}

		protected override IDatabind Instantiate(int index) {
			FileViewItem databound = (FileViewItem)base.Instantiate(index);
			if (databound.ChildView != null) {
				// Workaround Unity bug where you can't give a prefab a reference to the prefab, only to its own root.
				databound.ChildView.childTemplate = this.childTemplate;
			}
			return databound;
		}
	}
}
