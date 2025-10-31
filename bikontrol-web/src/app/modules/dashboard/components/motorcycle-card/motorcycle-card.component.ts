import { CommonModule } from '@angular/common';
import { Component, HostListener, Input } from '@angular/core';
import { Router } from '@angular/router';
import Swal from 'sweetalert2';


@Component({
  selector: 'app-motorcycle-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './motorcycle-card.component.html',
  styleUrl: './motorcycle-card.component.scss'
})
export class MotorcycleCardComponent {
  @Input() motorcycle: any;
  menuOpen = false;

  constructor(private router: Router) {}

  goToDetails() {
    console.log('Navigating to motorcycle details:', this.motorcycle);

  }
  
  deleteMotorcycle() {
    // TODO: Pending to use login when backend is ready.
    // this.motorcycleService.disableMotorcycle(this.motorcycle.id).subscribe({
    //   next: () => {
    //     Swal.fire({
    //       title: 'Deshabilitada',
    //       text: 'Tu motocicleta ha sido deshabilitada correctamente.',
    //       icon: 'success',
    //       timer: 1500,
    //       showConfirmButton: false
    //     });
    //   },
    //   error: (err) => {
    //     console.error(err);
    //     Swal.fire({
    //       title: 'Error',
    //       text: 'No se pudo deshabilitar la motocicleta. Inténtalo de nuevo.',
    //       icon: 'error',
    //       confirmButtonText: 'Ok'
    //     });
    //   }
    // });
  }


  confirmDelete(event: Event) {
    event.stopPropagation();

    Swal.fire({
      title: '¿Estás seguro?',
      text: `Esto eliminará permanentemente "${this.motorcycle.name}".`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Sí, eliminar',
      cancelButtonText: 'Cancelar',
      background: '#fff',
      buttonsStyling: false,
      customClass: {
        confirmButton:
          'bg-red-600 text-white rounded-lg px-4 py-2 font-semibold hover:bg-red-700',
        cancelButton:
          'bg-gray-200 text-gray-800 rounded-lg px-4 py-2 font-semibold hover:bg-gray-300 mx-2',
        popup: 'rounded-xl shadow-lg'
      }
    }).then(result => {
      if (result.isConfirmed) {
        this.deleteMotorcycle();
      }
    });
  }

  toggleMenu(event: MouseEvent) {
    event.stopPropagation();
    this.menuOpen = !this.menuOpen;
  }
  @HostListener('document:click')
  closeMenu() {
    if (this.menuOpen) this.menuOpen = false;
  }
}
