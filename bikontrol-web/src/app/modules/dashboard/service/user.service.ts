import { Injectable } from '@angular/core';
import { HttpClient, httpResource, HttpResourceRef } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { Profile } from '../interfaces/profile.interface';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private apiUrl = `${environment.apiUrl}/users`;

  constructor(private http: HttpClient) {}

  getMe(): Observable<Profile> {
    return this.http.get<Profile>(`${this.apiUrl}/me`);
  }

  /** Reactive read of the current user's profile. */
  getMeResource(): HttpResourceRef<Profile | undefined> {
    return httpResource<Profile>(() => `${this.apiUrl}/me`);
  }

  updateProfile(fullName: string): Observable<Profile> {
    return this.http.put<Profile>(`${this.apiUrl}/me`, { fullName });
  }

  changePassword(currentPassword: string, newPassword: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/me/password`, { currentPassword, newPassword });
  }
}
