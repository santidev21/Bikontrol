import {
  Component,
  EventEmitter,
  HostListener,
  Input,
  OnInit,
  Output,
  ChangeDetectionStrategy,
  signal,
  inject,
} from '@angular/core';
import { Router } from '@angular/router';
import { Motorcycle } from '../../interfaces/motorcycle.interface';
import { MotorcyclesService } from '../../service/motorcycles.service';
import { SwalService } from '../../../../shared/services/swal.service';
import { HttpErrorService } from '../../../../shared/services/http-error.service';
import { AuthService } from '../../../auth/services/auth.service';

import { TranslatePipe } from '../../../../shared/i18n/translate.pipe';
import { I18nService } from '../../../../shared/i18n/i18n.service';

@Component({
  selector: 'app-motorcycle-card',
  imports: [TranslatePipe],
  templateUrl: './motorcycle-card.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './motorcycle-card.component.scss',
})
export class MotorcycleCardComponent implements OnInit {
  private router = inject(Router);
  private motorcyclesService = inject(MotorcyclesService);
  private swal = inject(SwalService);
  private httpError = inject(HttpErrorService);
  private authService = inject(AuthService);
  private i18n = inject(I18nService);

  @Input() motorcycle!: Motorcycle;
  @Output() deleted = new EventEmitter<void>();
  readonly menuOpen = signal(false);
  readonly currentKm = signal<number | null>(null);

  get isDemo(): boolean {
    return this.authService.isDemo();
  }

  ngOnInit(): void {
    const motorcycleId = this.motorcycle?.id;
    if (!motorcycleId) return;

    this.motorcyclesService.getCurrentKm(motorcycleId).subscribe({
      next: (res) => {
        this.currentKm.set(res.km);
      },
      error: () => {
        this.currentKm.set(this.motorcycle?.km ?? 0);
      },
    });
  }

  goToDetails() {
    this.router.navigate(['/dashboard/motorcycles/summary'], {
      state: { motorcycle: this.motorcycle },
    });
  }

  onEdit(_e: Event) {
    this.router.navigate(['/dashboard/motorcycles/edit', this.motorcycle.id]);
  }

  deleteMotorcycle() {
    this.motorcyclesService.deleteMotorcycle(this.motorcycle.id!).subscribe({
      next: () => {
        this.swal
          .success(this.i18n.t('motoCard.deletedTitle'), this.i18n.t('motoCard.deletedText'))
          .then(() => this.deleted.emit());
      },
      error: (err) => {
        this.swal.error('Error', this.httpError.message(err, this.i18n.t('motoCard.deleteError')));
      },
    });
  }

  confirmDelete(event: Event) {
    event.stopPropagation();

    this.swal
      .confirm(
        this.i18n.t('motoCard.deleteConfirmTitle'),
        this.i18n.t('motoCard.deleteConfirmText', { name: this.motorcycle.name }),
        this.i18n.t('common.yesDelete'),
        this.i18n.t('common.cancel'),
        'warning',
      )
      .then((result) => {
        if (result.isConfirmed) {
          this.deleteMotorcycle();
        }
      });
  }

  toggleMenu(event: MouseEvent) {
    event.stopPropagation();
    this.menuOpen.update((open) => !open);
  }
  @HostListener('document:click')
  closeMenu() {
    if (this.menuOpen()) this.menuOpen.set(false);
  }

  get displayedKm(): number {
    return this.currentKm() ?? this.motorcycle?.km ?? 0;
  }
}
