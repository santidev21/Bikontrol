import { Injectable } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Maintenance, SaveMaintenanceDTO } from '../interfaces/maintenance.interface';

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

  createUserMaintenance(maintenance: SaveMaintenanceDTO): Observable<Maintenance> {
    return this.http.post<Maintenance>(`${this.apiUrl}/mine`, maintenance);
  }

  deleteMaintenance(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/mine/${id}`);
  }
  
  followDefaultMaintenance(defaultId: string): Observable<Maintenance> {
    return this.http.post<Maintenance>(`${this.apiUrl}/follow`,  { defaultId });
  }

}
