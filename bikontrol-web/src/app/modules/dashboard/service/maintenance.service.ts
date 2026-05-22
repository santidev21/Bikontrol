import { Injectable } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Maintenance,
  SaveMaintenanceDTO,
  FollowMaintenancePayload,
  CreateMaintenanceRecordRequest,
  MaintenanceRecord,
  UpcomingMaintenance
} from '../interfaces/maintenance.interface';

@Injectable({
  providedIn: 'root'
})
export class MaintenanceService {
 private apiUrl = `${environment.apiUrl}/maintenances`;
  
  constructor(private http: HttpClient) {}

  getDefaultMaintenance(): Observable<Maintenance[]> {
    return this.http.get<Maintenance[]>(`${this.apiUrl}/defaults`);
  }

  getUserMaintenance(): Observable<Maintenance[]> {
    return this.http.get<Maintenance[]>(`${this.apiUrl}/mine`);
  }

  getUserMaintenanceByMotorcycle(motorcycleId: string): Observable<Maintenance[]> {
    return this.http.get<Maintenance[]>(`${this.apiUrl}/mine/motorcycle/${motorcycleId}`);
  }

  getById(id: string): Observable<Maintenance> {
    return this.http.get<Maintenance>(`${this.apiUrl}/${id}`);
  }

  createUserMaintenance(maintenance: SaveMaintenanceDTO): Observable<Maintenance> {
    return this.http.post<Maintenance>(`${this.apiUrl}/mine`, maintenance);
  }

  deleteMaintenance(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/mine/${id}`);
  }
  
  followDefaultMaintenance(payload: FollowMaintenancePayload): Observable<Maintenance> {
    return this.http.post<Maintenance>(`${this.apiUrl}/follow`, payload);
  }

  updateMaintenance(id: string, dto: SaveMaintenanceDTO): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, dto);
  }

  registerMaintenanceRecord(payload: CreateMaintenanceRecordRequest): Observable<MaintenanceRecord> {
    return this.http.post<MaintenanceRecord>(`${this.apiUrl}/records`, payload);
  }

  getMaintenanceRecordsByMotorcycle(motorcycleId: string): Observable<MaintenanceRecord[]> {
    return this.http.get<MaintenanceRecord[]>(`${this.apiUrl}/motorcycle/${motorcycleId}/records`);
  }

  getUpcomingByMotorcycle(motorcycleId: string): Observable<UpcomingMaintenance[]> {
    return this.http.get<UpcomingMaintenance[]>(`${this.apiUrl}/motorcycle/${motorcycleId}/upcoming`);
  }

}
