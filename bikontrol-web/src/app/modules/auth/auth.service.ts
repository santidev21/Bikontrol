import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { RegisterRequest } from './interfaces/register-request.interface';
import { environment } from '../../../environments/environment';
import { LoginRequest } from './interfaces/login-request.interface';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
    private apiUrl = environment.apiUrl + "/auth";

    constructor(private http: HttpClient,
        private router: Router
    ) {}

    register(data: RegisterRequest): Observable<any> {
        return this.http.post<any>(`${this.apiUrl}/register`, data).pipe(
        catchError(this.handleError)
        );
    }

    login(data: LoginRequest): Observable<any> {
        return this.http.post<any>(`${this.apiUrl}/login`, data).pipe(
        catchError(this.handleError)
        );
    }

    private handleError(error: HttpErrorResponse) {
        if (error.status === 409) {
        return throwError(() => new Error('Email already exists. Try a different one.'));
        } else {
        return throwError(() => new Error('An error occurred. Please try again later.'));
        }
    }

    isTokenExpired(): boolean {
        const expiresAt = localStorage.getItem('expiresAt');
        if (!expiresAt) return true;

        return new Date() > new Date(expiresAt);
    }

    logout(): void {
        localStorage.removeItem('token');
        localStorage.removeItem('expiresAt');
        localStorage.removeItem('userId');
        localStorage.removeItem('fullName');

        this.router.navigate(['/auth/login']);
    }
}
