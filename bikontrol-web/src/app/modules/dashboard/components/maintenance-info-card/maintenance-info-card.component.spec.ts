import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MaintenanceInfoCardComponent } from './maintenance-info-card.component';

describe('MaintenanceInfoCardComponent', () => {
  let component: MaintenanceInfoCardComponent;
  let fixture: ComponentFixture<MaintenanceInfoCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MaintenanceInfoCardComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MaintenanceInfoCardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
