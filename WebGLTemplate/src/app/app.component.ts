import { BehaviorSubject } from 'rxjs';

import { AfterViewInit, Component, ElementRef, HostListener, ViewChild } from '@angular/core';

import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarRef, TextOnlySnackBar } from '@angular/material/snack-bar';

import { DownloadDialogComponent } from './download-dialog/download-dialog.component';
import { UploadDialogComponent } from './upload-dialog/upload-dialog.component';

@Component({
	selector: 'app-root',
	templateUrl: './app.component.html',
	styleUrls: ['./app.component.scss']
})
export class AppComponent implements AfterViewInit {
	constructor(private dialog: MatDialog, private snackbar: MatSnackBar) { }

	@ViewChild("files") private files: ElementRef<HTMLInputElement> = null!;
	@ViewChild("canvas") private canvas: ElementRef<HTMLCanvasElement> = null!;

	ngAfterViewInit(): void {
		const dialog = this.dialog.open(UploadDialogComponent, {
			data: this.files.nativeElement,
			disableClose: true,
			hasBackdrop: true,
			position: {
				top: "0"
			},
			restoreFocus: true
		});
		
		dialog.afterClosed().subscribe(() => {
			this.startUnity();
		});
	}

	private uploadFileMap: Record<string, File> = {};
	private async startUnity() {
		this.uploadFileMap = {};

		const files: WebUpload[] = [];
		for (let i = 0; i < this.files.nativeElement.files!.length; i++) {
			const file = this.files.nativeElement.files![i];
			const name = (<any>file).relativePath ?? file.webkitRelativePath;
			files.push({
				name,
				size: file.size
			});

			this.uploadFileMap[name] = file;
		}

		(<any>window).GetUploadFileContents = async (path: string, buffer: Uint8Array, callback: (success: boolean) => void) =>
			await this.getUploadFileContents(path, buffer, callback);
		(<any>window).DeleteDownloadFile = (path: string) => this.deleteDownloadFile(path);
		(<any>window).CreateDownloadFolder = (path: string) => { this.createDownloadFolder(path); };
		(<any>window).SetDownloadFile = (path: string, length: number) => this.setDownloadFile(path, length);
		(<any>window).ShowDownload = (path: string) => this.showDownloadDialog(path);
		(<any>window).Download = async (path: string, buffer: Uint8Array, callback: () => void) =>
			await this.download(path, buffer, callback);
	
		if ((<any>window).createUnityInstance) {
			this.gameInstance = await (<UnityWindow><any>window).createUnityInstance(this.canvas.nativeElement, (<UnityWindow><any>window).unityData);
			this.gameInstance.SendMessage("Loader", "OnBrowserUploadedFiles", JSON.stringify(files));
		}
		
		this.downloadButtonVisible = true;
	}
	private gameInstance?: UnityInstance;
	downloadButtonVisible = false;

	private async getUploadFileContents(path: string, buffer: Uint8Array, callback: (sucess: boolean) => void) {
		const file = this.uploadFileMap[path];
		if (!file || file.size != buffer.length) {
			callback(false);
			return;
		}

		buffer.set(new Uint8Array(await file.arrayBuffer()));
		callback(true);
	}

	private downloadRoot: DownloadFolder = {
		name: "Downloads",
		path: "",
		children: new BehaviorSubject<DownloadFileSystemItem[]>([])
	};

	private isFolder(item: DownloadFileSystemItem): item is DownloadFolder {
		return "children" in item;
	}

	private downloadFileMap: Record<string, DownloadFileSystemItem> = {
		"": this.downloadRoot
	};
	private findDownloadFile(path: string): DownloadFileSystemItem | undefined {
		return this.downloadFileMap[path];
	}

	private deleteDownloadFile(path: string) {
		const file = this.findDownloadFile(path);
		if (!file || !file.parent) {
			return;
		}

		delete this.downloadFileMap[file.path];

		const parent = file.parent;
		const index = parent.children.value.indexOf(file);
		parent.children.value.splice(index, 1);

		parent.children.next(parent.children.value);
	}

	// https://stackoverflow.com/questions/1344500/efficient-way-to-insert-a-number-into-a-sorted-array-of-numbers
	private insertSortedElement(array: DownloadFileSystemItem[], value: DownloadFileSystemItem,
		sortFunc: (a: DownloadFileSystemItem, b: DownloadFileSystemItem) => number) {

    let low = 0;
		let high = array.length;

		while (low < high) {
			const mid = (low + high) >>> 1;
			if (sortFunc(array[mid], value) < 0) {
				low = mid + 1;
			} else {
				high = mid;
			}
		}
		
		array.splice(low, 0, value);
	}

	private createDownloadFolder(path: string): DownloadFolder {
		const segments = path.split('/').filter(x => x != "");
		let current: DownloadFolder | undefined = undefined;
		let i;
		for (i = segments.length; i >= 0; i--) {
			const file = this.findDownloadFile(segments.slice(0, i).join('/'));
			if (file != null) {
				if (!this.isFolder(file)) {
					throw new Error();
				}

				current = file;
				break;
			}
		}
		if (!current) {
			throw new Error();
		}
		for (; i < segments.length; i++) {
			const segment = segments[i];
			if (segment == "") {
				break;
			}

			const path = current.path == "" ? segment : `${current.path}/${segment}`;
			const child = <DownloadFolder>{
				name: segment,
				children: new BehaviorSubject<DownloadFileSystemItem[]>([]),
				path,
				parent: current
			};
			this.downloadFileMap[path] = child;

			this.insertSortedElement(current.children.value, child, (a, b) => {
				const aIsFolder = this.isFolder(a);
				const bIsFolder = this.isFolder(b);
				if (aIsFolder != bIsFolder) {
					return Number(bIsFolder) - Number(aIsFolder);
				}
	
				const aIndex = a.name.lastIndexOf('.');
				const bIndex = b.name.lastIndexOf('.');
				const aExt =  aIndex < 0 ? "" : a.name.substring(aIndex + 1);
				const bExt =  bIndex < 0 ? "" : b.name.substring(bIndex + 1);
				let ret = aExt.localeCompare(bExt, undefined, {
					usage: "sort",
					numeric: true,
					sensitivity: "base"
				});
				if (ret != 0) {
					return ret;
				}

				const aName =  aIndex < 0 ? a.name : a.name.substring(0, aIndex - 1);
				const bName =  bIndex < 0 ? b.name : b.name.substring(0, bIndex - 1);
	
				return aName.localeCompare(bName, undefined, {
					usage: "sort",
					numeric: true,
					sensitivity: "base"
				});
			});	

			current.children.next(current.children.value);		

			current = child;
		}
		return current;
	}

	private setDownloadFile(path: string, length: number) {
		const file = this.findDownloadFile(path);
		let folder: DownloadFolder;
		if (file) {
			if (this.isFolder(file)) {
				throw new Error();
			}
			folder = file.parent!;
			(<DownloadFile>file).size = length;
		} else {
			const segments = path.split("/").filter(x => x != "");
			folder = this.createDownloadFolder(segments.slice(0, segments.length - 1).join('/'));
			const name = segments[segments.length - 1];
	
			const childPath = folder.path == "" ? name : `${folder.path}/${name}`;
			const child = <DownloadFile>{
				name,
				size: length,
				path: childPath,
				parent: folder
			};	
			this.downloadFileMap[childPath] = child;

			this.insertSortedElement(folder.children.value, child, (a, b) => {
				const aIsFolder = this.isFolder(a);
				const bIsFolder = this.isFolder(b);
				if (aIsFolder != bIsFolder) {
					return Number(bIsFolder) - Number(aIsFolder);
				}

				const aIndex = a.name.lastIndexOf('.');
				const bIndex = b.name.lastIndexOf('.');
				const aExt =  aIndex < 0 ? "" : a.name.substring(aIndex + 1);
				const bExt =  bIndex < 0 ? "" : b.name.substring(bIndex + 1);
				let ret = aExt.localeCompare(bExt, undefined, {
					usage: "sort",
					numeric: true,
					sensitivity: "base"
				});
				if (ret != 0) {
					return ret;
				}

				const aName =  aIndex < 0 ? a.name : a.name.substring(0, aIndex - 1);
				const bName =  bIndex < 0 ? b.name : b.name.substring(0, bIndex - 1);
	
				return aName.localeCompare(bName, undefined, {
					usage: "sort",
					numeric: true,
					sensitivity: "base"
				});
			});	
		}

		folder.children.next(folder.children.value);

		if (length > 0) {
			this.showSnackbar(path);
		}
	}

	private snack?: MatSnackBarRef<TextOnlySnackBar>;
	private pendingDownloads: string[] = [];
	private snackTimer?: unknown;
	private snackResolve?: (value: boolean) => void;
	private async showSnackbar(path: string) {
		if (this.snack) {
			this.snack.dismiss();
			this.snack = undefined;
		}
		if (this.snackResolve) {
			clearTimeout(<number>this.snackTimer);
			this.snackResolve(false);
			this.snackResolve = undefined;
			this.snackTimer = undefined;
		}
		this.pendingDownloads.push(path);
		if (!(await new Promise<boolean>((resolve, reject) => {
			this.snackResolve = resolve;
			this.snackTimer = <unknown>setTimeout(() => resolve(true), 1000);
		}))) {
			return;
		}

		const pendingDownloads = this.pendingDownloads;
		this.pendingDownloads = [];

		if (pendingDownloads.length == 0) {
			return;
		} else if (pendingDownloads.length == 1) {
			this.snack = this.snackbar.open(`New download available: ${path}`, "Download", {
				duration: 5000,
				verticalPosition: 'bottom'
			});
			this.snack.onAction().subscribe(() => this.gameInstance!.SendMessage("Loader", "OnBrowserDownloadFile", path));
		} else {
			this.snack = this.snackbar.open(`${pendingDownloads.length} new downloads available.`, "Show", {
				duration: 5000,
				verticalPosition: 'bottom'
			});
			this.snack.onAction().subscribe(() => this.showDownloadDialog(pendingDownloads[0]));
		}
		this.snack.afterDismissed().subscribe(x => {
			this.snack = undefined;
		});
	}

	private downloadDialog?: MatDialogRef<DownloadDialogComponent, DownloadDialogData>;
	showDownloadDialog(path?: string) {
		if (!this.downloadDialog) {
			const data: DownloadDialogData = {root: this.downloadRoot, show: this.downloadRoot, unity: this.gameInstance!};
			if (path) {
				data.show = this.findDownloadFile(path) ?? this.downloadRoot;
			}
			this.downloadDialog = this.dialog.open(DownloadDialogComponent, {
				data,
				position: {
					left: "0",
					bottom: "60px"
				},
				hasBackdrop: false
			});
	
			this.downloadDialog.afterClosed().subscribe(() => {
				this.downloadDialog = undefined;
			});	
		}
	}

	toggleDownloadDialog() {
		if (this.downloadDialog) {
			this.downloadDialog.close();
			return;
		}

		this.showDownloadDialog();
	}

	private async download(path: string, buffer: Uint8Array, callback: () => void) {
		try {
			const file = this.findDownloadFile(path);
			let filename: string;
			if (!file) {
				const segments = path.split('/');
				filename = segments[segments.length - 1];
				if (filename == "") {
					filename = "Downloads.zip";
				}
			} else if (!file.parent) {
				filename = "Downloads";
			} else {
				filename = file.name;
			}
			if (file && this.isFolder(file)) {
				filename += ".zip";
			}
	
			const blob = new Blob([buffer], {
				type: "application/octec-stream"
			});
			const url = window.URL.createObjectURL(blob);
			
			const a = document.createElement('a');
			a.download = filename;
			a.href = url;
			a.click();

			setTimeout(() => window.URL.revokeObjectURL(url), 1000);
		} finally {
			callback();
		}
	}

  @HostListener("window:beforeunload", ["$event"])
  private onBeforeUnload($event: BeforeUnloadEvent): string | undefined {
		for (let path in this.downloadFileMap) {
			const file = this.downloadFileMap[path];
			if (this.isFolder(file)) {
				continue;
			}

			return $event.returnValue = "You have downloadable files. Closing or reloading this page will wipe them.";
		}
		return undefined;
  }
}

type WebUpload = {
	name: string,
	size: number
};

interface UnityWindow extends Window {
	createUnityInstance: (canvas: HTMLCanvasElement, data: UnityData) => Promise<UnityInstance>,
	unityData: UnityData
};

type UnityData = {
	dataUrl: string,
	frameworkUrl: string,
	workerUrl?: string,
	codeUrl?: string,
	memoryUrl?: string,
	symbolsUrl?: string,
	streamingAssetsUrl: string,
	companyName: string,
	productName: string,
	productVersion: string,
	matchWebGLToCanvasSize?: boolean,
	devicePixelRatio?: number
};

export type UnityInstance = {
	SendMessage: (gameObject: string, funcName: string, value?: string | number | null) => void;
};

export interface DownloadFileSystemItem {
	name: string;
	path: string;
	parent?: DownloadFolder;
}

export type DownloadFile = DownloadFileSystemItem & {
	size: number
};

export type DownloadFolder = DownloadFileSystemItem & {
	children: BehaviorSubject<DownloadFileSystemItem[]>
};

export type DownloadDialogData = {
	root: DownloadFolder,
	show: DownloadFileSystemItem,
	unity: UnityInstance
};
