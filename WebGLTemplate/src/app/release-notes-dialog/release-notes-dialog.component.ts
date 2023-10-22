import { Component } from '@angular/core';

import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';

@Component({
  templateUrl: './release-notes-dialog.component.html',
  styleUrls: ['./release-notes-dialog.component.scss'],
	standalone: true,
	imports: [MatButtonModule, MatDialogModule, MatIconModule]
})
export class ReleaseNotesDialogComponent {

}
