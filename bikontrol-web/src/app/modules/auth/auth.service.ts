import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { RegisterRequest } from './interfaces/register-request.interface';
import { environment } from '../../../environments/environment';
import { LoginRequest } from './interfaces/login-request.interface';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
    private apiUrl = environment.apiUrl + "/auth";

    constructor(private http: HttpClient) {}

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
}
