import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class HttpErrorService {
  message(error: any, fallback = 'Error inesperado en el servidor.'): string {
    return (
      error?.error?.error ||
      error?.error?.message ||
      error?.message ||
      fallback
    );
  }
}
