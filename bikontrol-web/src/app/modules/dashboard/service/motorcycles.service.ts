import { Injectable } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateMotorcycleDTO, Motorcycle, UpdateMotorcycleDTO } from '../interfaces/motorcycle.interface';

@Injectable({
  providedIn: 'root'
})
export class MotorcyclesService {
  private apiUrl = `${environment.apiUrl}/motorcycles`;
  
  constructor(private http: HttpClient) {}

  getMyMotorcycles(): Observable<Motorcycle[]> {
    return this.http.get<Motorcycle[]>(`${this.apiUrl}/mine`,);
  }

  getById(id: string): Observable<Motorcycle> {
    return this.http.get<Motorcycle>(`${this.apiUrl}/${id}`);
  }

  addMotorcycle(motorcycle: CreateMotorcycleDTO): Observable<Motorcycle> {
    return this.http.post<Motorcycle>(`${this.apiUrl}`, motorcycle);
  }

  update(id: string, dto: UpdateMotorcycleDTO): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, dto);
  }

  deleteMotorcycle(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
