import { Injectable, Signal, inject } from '@angular/core';
import { environment } from '@env/environment';
import { HttpClient, httpResource, HttpResourceRef } from '@angular/common/http';
import { Observable } from 'rxjs';
import { FuelUp, SaveFuelUpDTO, FuelConsumption } from '../interfaces/fuel-up.interface';

@Injectable({
  providedIn: 'root',
})
export class FuelUpsService {
  private http = inject(HttpClient);

  private apiUrl = `${environment.apiUrl}/fuel-ups`;

  getByMotorcycle(motorcycleId: string): Observable<FuelUp[]> {
    return this.http.get<FuelUp[]>(`${this.apiUrl}/motorcycle/${motorcycleId}`);
  }

  getById(id: string): Observable<FuelUp> {
    return this.http.get<FuelUp>(`${this.apiUrl}/${id}`);
  }

  create(fuelUp: SaveFuelUpDTO): Observable<FuelUp> {
    return this.http.post<FuelUp>(`${this.apiUrl}`, fuelUp);
  }

  update(id: string, fuelUp: SaveFuelUpDTO): Observable<FuelUp> {
    return this.http.put<FuelUp>(`${this.apiUrl}/${id}`, fuelUp);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  getConsumption(motorcycleId: string): Observable<FuelConsumption> {
    return this.http.get<FuelConsumption>(`${this.apiUrl}/motorcycle/${motorcycleId}/consumption`);
  }

  /**
   * Reactive read of a motorcycle's fuel-ups (newest first). Loads automatically
   * when the id signal has a value (and reloads when it changes).
   */
  getByMotorcycleResource(
    motorcycleId: Signal<string | undefined>,
  ): HttpResourceRef<FuelUp[] | undefined> {
    return httpResource<FuelUp[]>(() => {
      const id = motorcycleId();
      return id ? `${this.apiUrl}/motorcycle/${id}` : undefined;
    });
  }

  /**
   * Reactive read of the derived consumption/cost metrics. Loads automatically
   * when the id signal has a value.
   */
  getConsumptionResource(
    motorcycleId: Signal<string | undefined>,
  ): HttpResourceRef<FuelConsumption | undefined> {
    return httpResource<FuelConsumption>(() => {
      const id = motorcycleId();
      return id ? `${this.apiUrl}/motorcycle/${id}/consumption` : undefined;
    });
  }
}
