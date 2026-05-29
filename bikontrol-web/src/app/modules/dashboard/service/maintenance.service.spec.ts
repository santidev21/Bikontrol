import { of } from 'rxjs';

import { MaintenanceService } from './maintenance.service';

describe('MaintenanceService (class)', () => {
  const httpClientMock = {
    get: jest.fn().mockReturnValue(of([])),
    post: jest.fn().mockReturnValue(of({})),
    put: jest.fn().mockReturnValue(of(undefined)),
    delete: jest.fn().mockReturnValue(of(undefined))
  } as any;

  it('should be created', () => {
    const service = new MaintenanceService(httpClientMock);
    expect(service).toBeTruthy();
  });
});
