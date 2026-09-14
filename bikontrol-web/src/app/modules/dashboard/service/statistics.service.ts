import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { StatisticsSummary } from '../interfaces/statistics.interface';

@Injectable({
  providedIn: 'root'
})
export class StatisticsService {
  private apiUrl = `${environment.apiUrl}/statistics`;

  constructor(private http: HttpClient) {}

  getSummary(): Observable<StatisticsSummary> {
    return this.http.get<StatisticsSummary>(`${this.apiUrl}/summary`);
  }
}
