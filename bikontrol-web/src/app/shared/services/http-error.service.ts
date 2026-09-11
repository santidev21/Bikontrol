import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class HttpErrorService {
  message(error: any, fallback = 'Error inesperado en el servidor.'): string {
    const serverMessage = error?.error?.error || error?.error?.message;
    if (typeof serverMessage === 'string' && serverMessage.trim().length > 0) {
      return serverMessage;
    }

    const status = typeof error?.status === 'number' ? error.status : undefined;
    switch (status) {
      case 0:
        return 'No se pudo conectar con el servidor. Verifica tu conexión e inténtalo de nuevo.';
      case 400:
        return 'Revisa los datos ingresados e inténtalo de nuevo.';
      case 401:
        return 'Tu sesión expiró. Inicia sesión de nuevo.';
      case 403:
        return 'No tienes permiso para realizar esta acción.';
      case 404:
        return 'No se encontró lo que buscabas.';
      case 409:
        return 'Ese registro ya existe.';
      case 429:
        return 'Demasiados intentos. Espera un momento e inténtalo de nuevo.';
      case 500:
      case 502:
      case 503:
        return 'El servidor no está disponible en este momento. Inténtalo más tarde.';
      case 504:
        return 'El servidor está tardando demasiado en responder. Inténtalo de nuevo.';
      default:
        return error?.message || fallback;
    }
  }
}