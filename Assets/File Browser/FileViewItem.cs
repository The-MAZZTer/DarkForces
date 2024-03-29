﻿using MZZT.Data.Binding;
using MZZT.IO.FileProviders;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MZZT {
	/// <summary>
	/// A file system item.
	/// </summary>
	public class FileViewItem : Databind<IVirtualItem>, IPointerClickHandler {
		[Header("File View Item"), SerializeField]
		private FileView childView = null;
		/// <summary>
		/// A FileView for the current file system item.
		/// </summary>
		public FileView ChildView => this.childView;
		[SerializeField] 
		private Toggle expando = null;
		[SerializeField]
		private GameObject collapsed = null;
		[SerializeField]
		private GameObject expanded = null;
		[SerializeField]
		private Toggle node = null;
		[SerializeField]
		private TMP_Text icon = null;
		[SerializeField]
		private TMP_Text sizeText = null;
		/// <summary>
		/// The Toggle for the current node.
		/// </summary>
		public Toggle Node => this.node;

		/// <summary>
		/// Convert the size to a user readable string.
		/// </summary>
		private string DisplaySize {
			get {
				if (this.Value.Size == null) {
					return string.Empty;
				}

				string prefix = "BKMGTPEZY??????????????????????";
				float size = this.Value.Size.Value;
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
		private string IconGlyph {
			get {
				// Unity doesn't support ligatures so we need to use the code point.
				// TODO file container "\ueb2c"
				if (this.Value.Parent == null) {
					return "\ue30a";
				}
				if (this.Value is IVirtualFolder) {
					if (Path.GetPathRoot(this.Value.FullPath) == this.Value.FullPath) {
						return "\ue1db";
					}
					return "\ue2c7";
				}
				return "\ue24d";
			}
		}

		protected override void OnInvalidate() {
			if (this.icon != null) {
				this.icon.text = this.IconGlyph;
			}
			if (this.sizeText != null) {
				this.sizeText.text = this.DisplaySize;
			}

			if (this.childView != null) {
				// Recurse current settings down to children.
				FileView parent = (FileView)this.Parent;
				if (parent != null) {
					this.childView.FileSearchPatterns = parent.FileSearchPatterns;
					this.childView.ToggleGroup = parent.ToggleGroup;
					this.childView.Container = (IVirtualFolder)this.Value;
				}
			}

			base.OnInvalidate();

			this.gameObject.name = this.Value.DisplayName;
		}

		/// <summary>
		/// Ensure the current node is visible in the parent ScrollView.
		/// </summary>
		public void EnsureVisible() {
			RectTransform transform = (RectTransform)this.GetComponentsInParent<ToggleGroup>(true).First().transform;

			Vector2 minOffset = transform.InverseTransformVector(this.Node.transform.position - transform.position);
			minOffset = new Vector2(minOffset.x, -minOffset.y);
			Vector2 nodeSize = ((RectTransform)this.Node.transform).rect.size;
			Vector2 maxOffset = minOffset + nodeSize;

			Vector2 minPos = transform.localPosition;
			Vector2 viewportSize = ((RectTransform)transform.parent).rect.size;
			Vector2 maxPos = minPos + viewportSize;

			if (minOffset.x < minPos.x) {
				transform.localPosition = new Vector2(-minOffset.x, transform.localPosition.y);
			} else if (maxOffset.x > maxPos.x) {
				transform.localPosition = new Vector2(-(maxOffset.x - viewportSize.x), transform.localPosition.y);
			}
			if (minOffset.y < minPos.y) {
				transform.localPosition = new Vector2(transform.localPosition.x, minOffset.y);
			} else if (maxOffset.y > maxPos.y) {
				transform.localPosition = new Vector2(transform.localPosition.x, maxOffset.y - viewportSize.y);
			}
		}

		/// <summary>
		/// Whether the node expando is open or closed.
		/// </summary>
		public bool Expanded {
			get => this.expando.isOn;
			set => this.expando.isOn = value;
		}

		/// <summary>
		/// Called by the expando when it changes state.
		/// </summary>
		/// <param name="value">State of the expando.</param>
		public async void OnExpandedChangedAsync(bool value) {
			this.childView.gameObject.SetActive(value);
			this.collapsed.SetActive(!value);
			this.expanded.SetActive(value);

			if (value) {
				await FileBrowser.Instance.OnExpandedAsync(this);
			}
		}

		/// <summary>
		/// Called by the node when it's selection state changes.
		/// </summary>
		/// <param name="value">Selection state.</param>
		public async void OnToggleValueChanged(bool value) {
			if (!value) {
				return;
			}

			await FileBrowser.Instance.OnSelectedValueChangedAsync(this);
		}

		private float lastClickTime = float.MinValue;

		/// <summary>
		/// Called when this node is clicked.
		/// </summary>
		/// <param name="eventData">Click event data.</param>
		public async void OnPointerClick(PointerEventData eventData) {
			DataboundListChildToggle.FindToggleFor(this).isOn = true;

			// Detect double click.
			if (Time.fixedTime - this.lastClickTime <= 0.5f) {
				this.lastClickTime = float.MinValue;

				await FileBrowser.Instance.OnDoubleClickAsync(this);
			} else {
				this.lastClickTime = Time.fixedTime;
			}
		}
	}
}
