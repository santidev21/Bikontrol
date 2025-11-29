import { Injectable } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { MaintenanceType } from '../interfaces/maintenance.interface';

@Injectable({
  providedIn: 'root'
})
export class MaintenanceService {
 private apiUrl = `${environment.apiUrl}/maintenance`;
  
  constructor(private http: HttpClient) {}

  getDefaultMaintenanceTypes(): Observable<MaintenanceType[]> {
    return this.http.get<MaintenanceType[]>(`${this.apiUrl}/defaults`);
  }

  getUserMaintenanceTypes(): Observable<MaintenanceType[]> {
    return this.http.get<MaintenanceType[]>(`${this.apiUrl}/mine`);
  }
}
