import { Injectable, inject } from '@angular/core';
import { HttpClient, httpResource, HttpResourceRef } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { Profile, UserDataExport } from '../interfaces/profile.interface';

@Injectable({
  providedIn: 'root',
})
export class UserService {
  private http = inject(HttpClient);

  private apiUrl = `${environment.apiUrl}/users`;

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
    return this.http.post<{ message: string }>(`${this.apiUrl}/me/password`, {
      currentPassword,
      newPassword,
    });
  }

  updateReminders(enabled: boolean): Observable<Profile> {
    return this.http.put<Profile>(`${this.apiUrl}/me/reminders`, { enabled });
  }

  /** Downloads everything the current user owns as one JSON document. */
  exportMyData(): Observable<UserDataExport> {
    return this.http.get<UserDataExport>(`${this.apiUrl}/me/export`);
  }
}
