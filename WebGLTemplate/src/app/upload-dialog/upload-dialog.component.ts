import { Component, Inject } from '@angular/core';

import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';

import { ReleaseNotesDialogComponent } from '../release-notes-dialog/release-notes-dialog.component';

@Component({
	templateUrl: './upload-dialog.component.html',
	styleUrls: ['./upload-dialog.component.scss'],
	standalone: true,
	imports: [MatButtonModule, MatDialogModule, MatIconModule]
})
export class UploadDialogComponent {
  constructor(@Inject(MAT_DIALOG_DATA) public files: HTMLInputElement, private dialog: MatDialog) {}

	selectFolder() {
		this.files.click();
	}

	showReleaseNotes() {
		this.dialog.open(ReleaseNotesDialogComponent, {
			hasBackdrop: true,
			restoreFocus: true
		});	
	}
}
