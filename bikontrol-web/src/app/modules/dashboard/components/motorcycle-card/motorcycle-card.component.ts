import { CommonModule } from '@angular/common';
import { Component, EventEmitter, HostListener, Input, OnInit, Output } from '@angular/core';
import { Router } from '@angular/router';
import { MotorcyclesService } from '../../service/motorcycles.service';
import { SwalService } from '../../../../shared/services/swal.service';


@Component({
  selector: 'app-motorcycle-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './motorcycle-card.component.html',
  styleUrl: './motorcycle-card.component.scss'
})
export class MotorcycleCardComponent implements OnInit {
  @Input() motorcycle: any;
  @Output() deleted = new EventEmitter<void>();
  menuOpen = false;
  currentKm: number | null = null;

  constructor(
    private router: Router,
    private motorcyclesService: MotorcyclesService,
    private swal : SwalService
  ) {}

  ngOnInit(): void {
    const motorcycleId = this.motorcycle?.id;
    if (!motorcycleId) return;

    this.motorcyclesService.getCurrentKm(motorcycleId).subscribe({
      next: (res) => {
        this.currentKm = res.km;
      },
      error: () => {
        this.currentKm = this.motorcycle?.km ?? 0;
      }
    });
  }

  goToDetails() {
    this.router.navigate(['/dashboard/motorcycles/summary'], {
      state: { motorcycle: this.motorcycle }
    });
  }
  
  onEdit($e: any){
    this.router.navigate(['/dashboard/motorcycles/edit', this.motorcycle.id]);
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

  get displayedKm(): number {
    return this.currentKm ?? this.motorcycle?.km ?? 0;
  }
}
