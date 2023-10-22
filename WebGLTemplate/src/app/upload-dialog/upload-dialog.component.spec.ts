import { ComponentFixture, TestBed } from '@angular/core/testing';

import { UploadDialogComponentComponent } from './upload-dialog.component';

describe('UploadDialogComponentComponent', () => {
  let component: UploadDialogComponentComponent;
  let fixture: ComponentFixture<UploadDialogComponentComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [UploadDialogComponentComponent]
    });
    fixture = TestBed.createComponent(UploadDialogComponentComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
