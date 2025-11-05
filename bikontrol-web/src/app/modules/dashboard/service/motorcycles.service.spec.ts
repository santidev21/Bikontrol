import { TestBed } from '@angular/core/testing';

import { MotorcyclesService } from './motorcycles.service';

describe('MotorcyclesService', () => {
  let service: MotorcyclesService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(MotorcyclesService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
