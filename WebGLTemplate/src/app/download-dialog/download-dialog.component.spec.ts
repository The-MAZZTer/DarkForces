import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DownloadDialogComponent } from './download-dialog.component';

describe('DownloadDialogComponent', () => {
  let component: DownloadDialogComponent;
  let fixture: ComponentFixture<DownloadDialogComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [DownloadDialogComponent]
    });
    fixture = TestBed.createComponent(DownloadDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
