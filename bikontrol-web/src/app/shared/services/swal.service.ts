import { Injectable } from '@angular/core';
import Swal, { SweetAlertIcon } from 'sweetalert2';

@Injectable({
  providedIn: 'root'
})
export class SwalService {
private baseConfig = {
    background: '#fff',
    buttonsStyling: false,
    customClass: {
      popup: 'rounded-xl shadow-lg',
      confirmButton:
        'bg-blue-600 text-white rounded-lg px-4 py-2 font-semibold hover:bg-blue-700',
      cancelButton:
        'bg-gray-200 text-gray-800 rounded-lg px-4 py-2 font-semibold hover:bg-gray-300 mx-2',
    },
  };

  success(title: string, text: string, confirmText = 'Aceptar') {
    return Swal.fire({
      ...this.baseConfig,
      icon: 'success',
      title,
      text,
      confirmButtonText: confirmText,
    });
  }

  error(title: string, text: string, confirmText = 'Entendido') {
    return Swal.fire({
      ...this.baseConfig,
      icon: 'error',
      title,
      text,
      confirmButtonText: confirmText,
      customClass: {
        ...this.baseConfig.customClass,
        confirmButton:
          'bg-red-600 text-white rounded-lg px-4 py-2 font-semibold hover:bg-red-700',
      },
    });
  }

  warning(title: string, text: string, confirmText = 'Entendido') {
    return Swal.fire({
      ...this.baseConfig,
      icon: 'warning',
      title,
      text,
      confirmButtonText: confirmText,
      customClass: {
        ...this.baseConfig.customClass,
        confirmButton:
          'bg-yellow-500 text-white rounded-lg px-4 py-2 font-semibold hover:bg-yellow-600',
      },
    });
  }

  confirm(
    title: string,
    text: string,
    confirmText = 'Sí, continuar',
    cancelText = 'Cancelar',
    icon: SweetAlertIcon = 'warning'
  ) {
    return Swal.fire({
      ...this.baseConfig,
      title,
      text,
      icon,
      showCancelButton: true,
      confirmButtonText: confirmText,
      cancelButtonText: cancelText,
      customClass: {
        ...this.baseConfig.customClass,
        confirmButton:
          'bg-red-600 text-white rounded-lg px-4 py-2 font-semibold hover:bg-red-700',
      },
    });
  }
}
