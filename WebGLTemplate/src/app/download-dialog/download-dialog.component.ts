import { Component, Inject, OnInit, QueryList, ViewChildren } from '@angular/core';
import { CommonModule } from '@angular/common';

import { NestedTreeControl } from '@angular/cdk/tree';

import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggle, MatButtonToggleModule } from '@angular/material/button-toggle';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatTreeModule, MatTreeNestedDataSource } from '@angular/material/tree';

import { DownloadDialogData, DownloadFileSystemItem, DownloadFolder, UnityInstance } from '../app.component';

@Component({
	templateUrl: './download-dialog.component.html',
	styleUrls: ['./download-dialog.component.scss'],
	standalone: true,
	imports: [CommonModule, MatButtonModule, MatButtonToggleModule, MatDialogModule, MatIconModule, MatTableModule, MatTooltipModule, MatTreeModule]
})
export class DownloadDialogComponent implements OnInit {
	constructor(@Inject(MAT_DIALOG_DATA) private data: DownloadDialogData, private dialog: MatDialog) {
		this.root = data.root;
		this.show = data.show;
		this.unity = data.unity;
	}

	private root: DownloadFolder;
	private show: DownloadFileSystemItem;
	private unity: UnityInstance;

	ngOnInit() {
		this.treeDataSource.data = [ this.root ];

		this.root.children.next(this.root.children.value);

		if (!this.isFolder(this.show)) {
			this.show = this.show.parent!;
		}
		this.navigateTo(<DownloadFolder>this.show);
	}

	isFolder(node: DownloadFileSystemItem): node is DownloadFolder {
		return "children" in node;
	}

	treeControl = new NestedTreeControl<DownloadFileSystemItem>(node => this.isFolder(node) ? node.children : null);
	treeDataSource = new MatTreeNestedDataSource<DownloadFileSystemItem>();
	treeItemHasChild(_: number, node: DownloadFileSystemItem): boolean {
		const isFolder = (node: DownloadFileSystemItem): node is DownloadFolder => "children" in node;
		if (!isFolder(node)) {
			return false;
		}

		return node.children.value.filter(x => isFolder(x)).length > 0;
	}
	treeItemHasNoChild(_: number, node: DownloadFileSystemItem): boolean {
		const isFolder = (node: DownloadFileSystemItem): node is DownloadFolder => "children" in node;
		if (!isFolder(node)) {
			return false;
		}

		return node.children.value.filter(x => isFolder(x)).length == 0;
	}

	encodeURIComponent(x: string) {
		return window.encodeURIComponent(x);
	}

	private async scrollToTreeItem(item: DownloadFolder): Promise<void> {
		while (!this.treeButtons) {
			await new Promise(x => setTimeout(x, 25));
		}
		const element = this.treeButtons.filter(x => x.value === item)[0]._buttonElement.nativeElement.parentElement!;

		let current: DownloadFolder | undefined = item;
		while (current) {
			this.treeControl.expand(current);
			current = current.parent;
		}

		await new Promise(resolve => {
			setTimeout(resolve, 1);
		});

		element.scrollIntoView({
			behavior: "smooth",
			block: "nearest"
		});
	}

	@ViewChildren(MatButtonToggle) private treeButtons?: QueryList<MatButtonToggle>;
	async navigateTo(node: DownloadFolder, source: MatButtonToggle | null = null) {
		if (!source) {
			while (!this.treeButtons) {
				await new Promise(x => setTimeout(x, 25));
			}
			source = this.treeButtons.filter(x => x.value === node)[0];
			if (!source) {
				throw new Error();
			}
		}

		this.scrollToTreeItem(node);

		source.checked = true;

		node.children.subscribe(data => {
			this.tableDataSource.data = data;
		});
	}
	
  displayedColumns: string[] = ["name", "size", "download"];
  tableDataSource = new MatTreeNestedDataSource<DownloadFileSystemItem>();

	formatSize(size?: number) {
		if (size === undefined) {
			return "";
		}

		const prefix = "BKMGTPEZY??????????????????????";
		let pos = 0;
		while (size >= 1000) {
			size /= 1024;
			pos++;
		}
		if (pos == 0) {
			return `${size} bytes`;
		}
		return `${Math.round(size * 100) / 100} ${prefix[pos]}B`;
	}

	download(node: DownloadFileSystemItem) {
		this.unity.SendMessage("Loader", "OnBrowserDownloadFile", node.path);
	}

	downloadAll() {
		this.unity.SendMessage("Loader", "OnBrowserDownloadFile", "");
	}
}
