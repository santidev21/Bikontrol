import { CommonModule } from '@angular/common';
import { Component, EventEmitter, HostListener, Input, Output } from '@angular/core';
import { Router } from '@angular/router';
import Swal from 'sweetalert2';
import { MotorcyclesService } from '../../service/motorcycles.service';
import { SwalService } from '../../../../shared/services/swal.service';


@Component({
  selector: 'app-motorcycle-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './motorcycle-card.component.html',
  styleUrl: './motorcycle-card.component.scss'
})
export class MotorcycleCardComponent {
  @Input() motorcycle: any;
  @Output() deleted = new EventEmitter<void>();
  menuOpen = false;

  constructor(
    private router: Router,
    private motorcyclesService: MotorcyclesService,
    private swal : SwalService
  ) {}

  goToDetails() {
    console.log('Navigating to motorcycle details:', this.motorcycle);

  }
  
  deleteMotorcycle() {
    this.motorcyclesService.deleteMotorcycle(this.motorcycle.id!).subscribe({
        next: () => {
          this.swal
            .success('¡Eliminada!', 'La motocicleta fue eliminada correctamente.')
            .then(() => this.deleted.emit());
        },
        error: (err) => {
          console.error(err);
          this.swal.error(
            'Error',
            err?.error?.message || 'No se pudo eliminar la motocicleta.'
          );
        },
      });
  }


  confirmDelete(event: Event) {
    event.stopPropagation();

    this.swal
      .confirm(
        '¿Estás seguro?',
        `Esto eliminará permanentemente "${this.motorcycle.name}".`,
        'Sí, eliminar',
        'Cancelar',
        'warning'
      )
      .then((result) => {
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
