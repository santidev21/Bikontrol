import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AddMotorcycleComponent } from './add-motorcycle.component';

describe('AddMotorcycleComponent', () => {
  let component: AddMotorcycleComponent;
  let fixture: ComponentFixture<AddMotorcycleComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AddMotorcycleComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AddMotorcycleComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
