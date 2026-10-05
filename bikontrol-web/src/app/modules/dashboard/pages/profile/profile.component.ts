import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { UserService } from '../../service/user.service';
import { SwalService } from '../../../../shared/services/swal.service';
import { HttpErrorService } from '../../../../shared/services/http-error.service';
import { AuthService } from '../../../auth/services/auth.service';
import { UpdateService } from '../../../../shared/services/update.service';
import { PushService } from '../../service/push.service';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-profile',
  imports: [CommonModule, FormsModule],
  templateUrl: './profile.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './profile.component.scss',
})
export class ProfileComponent {
  private readonly userService = inject(UserService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly swal = inject(SwalService);
  private readonly httpError = inject(HttpErrorService);
  private readonly updateService = inject(UpdateService);
  private readonly pushService = inject(PushService);

  private readonly profileRes = this.userService.getMeResource();
  readonly profile = computed(() =>
    this.profileRes.hasValue() ? this.profileRes.value() : undefined,
  );
  readonly isLoading = computed(() => this.profileRes.isLoading());

  readonly isEditingName = signal(false);
  readonly editableName = signal('');
  readonly isSavingName = signal(false);

  readonly isPasswordModalOpen = signal(false);
  readonly currentPassword = signal('');
  readonly newPassword = signal('');
  readonly confirmPassword = signal('');
  readonly isChangingPassword = signal(false);

  readonly appVersion = this.updateService.appVersion;
  readonly swVersion = this.updateService.swVersion;

  readonly pushSupported = this.pushService.isSupported;
  readonly remindersEnabled = computed(() => this.profile()?.remindersEnabled ?? false);
  readonly pushBusy = signal(false);
  readonly isExporting = signal(false);

  readonly isDeleteModalOpen = signal(false);
  readonly deleteConfirmation = signal('');
  readonly deletePassword = signal('');
  readonly isDeleting = signal(false);

  readonly isDemo = computed(() => this.authService.isDemo());
  readonly canChangePassword = computed(() => !!this.profile()?.hasPassword && !this.isDemo());
  readonly initials = computed(() => {
    const name = (this.profile()?.fullName ?? '').trim();
    if (!name) return '?';
    const parts = name.split(/\s+/);
    return (parts[0][0] + (parts.length > 1 ? parts[parts.length - 1][0] : '')).toUpperCase();
  });

  constructor() {
    effect(() => {
      const error = this.profileRes.error();
      if (error) {
        this.swal.error('Error', this.httpError.message(error, 'No se pudo cargar tu perfil.'));
      }
    });
  }

  startEditName(): void {
    this.editableName.set(this.profile()?.fullName ?? '');
    this.isEditingName.set(true);
  }

  cancelEditName(): void {
    this.isEditingName.set(false);
    this.isSavingName.set(false);
  }

  saveName(): void {
    if (this.isSavingName()) return;
    const fullName = this.editableName().trim();
    if (!fullName) {
      this.swal.error('¡Error!', 'El nombre no puede estar vacío.');
      return;
    }

    this.isSavingName.set(true);
    this.userService.updateProfile(fullName).subscribe({
      next: (data) => {
        this.profileRes.set(data);
        this.cancelEditName();
        this.swal.success('¡Éxito!', 'Se actualizó tu nombre.');
      },
      error: (err) => {
        this.isSavingName.set(false);
        this.swal.error('Error', this.httpError.message(err, 'No se pudo actualizar tu nombre.'));
      },
    });
  }

  openPasswordModal(): void {
    this.currentPassword.set('');
    this.newPassword.set('');
    this.confirmPassword.set('');
    this.isPasswordModalOpen.set(true);
  }

  closePasswordModal(): void {
    this.isPasswordModalOpen.set(false);
    this.isChangingPassword.set(false);
  }

  changePassword(): void {
    if (this.isChangingPassword()) return;

    if (!this.currentPassword() || !this.newPassword()) {
      this.swal.error('¡Error!', 'Completa todos los campos.');
      return;
    }
    if (this.newPassword().length < 6) {
      this.swal.error('¡Error!', 'La nueva contraseña debe tener al menos 6 caracteres.');
      return;
    }
    if (this.newPassword() !== this.confirmPassword()) {
      this.swal.error('¡Error!', 'La confirmación no coincide con la nueva contraseña.');
      return;
    }

    this.isChangingPassword.set(true);
    this.userService.changePassword(this.currentPassword(), this.newPassword()).subscribe({
      next: () => {
        this.closePasswordModal();
        this.swal.success('¡Éxito!', 'Se actualizó tu contraseña.');
      },
      error: (err) => {
        this.isChangingPassword.set(false);
        this.swal.error(
          'Error',
          this.httpError.message(err, 'No se pudo actualizar tu contraseña.'),
        );
      },
    });
  }

  toggleReminders(): void {
    if (this.isDemo()) return;
    const enabled = !this.remindersEnabled();
    this.userService.updateReminders(enabled).subscribe({
      next: (data) => {
        this.profileRes.set(data);
        this.swal.success(
          '¡Listo!',
          enabled ? 'Recordatorios activados.' : 'Recordatorios desactivados.',
        );
      },
      error: (err) =>
        this.swal.error(
          'Error',
          this.httpError.message(err, 'No se pudo actualizar la preferencia.'),
        ),
    });
  }

  async togglePush(): Promise<void> {
    if (this.pushBusy()) return;
    this.pushBusy.set(true);
    try {
      const current = await firstValueFrom(this.pushService.getSubscription());
      if (current) {
        await firstValueFrom(this.pushService.unsubscribe());
        this.swal.success('¡Listo!', 'Notificaciones desactivadas en este dispositivo.');
      } else {
        await firstValueFrom(this.pushService.subscribe());
        this.swal.success('¡Listo!', 'Notificaciones activadas en este dispositivo.');
      }
    } catch (err: any) {
      this.swal.error(
        'Error',
        this.httpError.message(err, 'No se pudieron cambiar las notificaciones.'),
      );
    } finally {
      this.pushBusy.set(false);
    }
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  downloadMyData(): void {
    if (this.isExporting()) return;
    this.isExporting.set(true);
    this.userService.exportMyData().subscribe({
      next: (data) => {
        this.isExporting.set(false);
        const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = `bikontrol-mis-datos-${new Date().toISOString().slice(0, 10)}.json`;
        anchor.click();
        URL.revokeObjectURL(url);
        this.swal.success('¡Listo!', 'Descargamos tus datos en un archivo JSON.');
      },
      error: (err) => {
        this.isExporting.set(false);
        this.swal.error(
          'Error',
          this.httpError.message(err, 'No se pudieron descargar tus datos.'),
        );
      },
    });
  }

  openDeleteModal(): void {
    this.deleteConfirmation.set('');
    this.deletePassword.set('');
    this.isDeleteModalOpen.set(true);
  }

  closeDeleteModal(): void {
    this.isDeleteModalOpen.set(false);
    this.isDeleting.set(false);
  }

  confirmDeleteAccount(): void {
    if (this.isDeleting()) return;

    if (this.deleteConfirmation().trim().toUpperCase() !== 'ELIMINAR') {
      this.swal.error('¡Error!', 'Escribe ELIMINAR para confirmar.');
      return;
    }
    if (this.canChangePassword() && !this.deletePassword()) {
      this.swal.error('¡Error!', 'Ingresá tu contraseña para confirmar.');
      return;
    }

    this.isDeleting.set(true);
    this.userService
      .deleteAccount(this.canChangePassword() ? this.deletePassword() : null)
      .subscribe({
        next: () => {
          this.isDeleting.set(false);
          this.isDeleteModalOpen.set(false);
          this.swal
            .success('Cuenta eliminada', 'Tu cuenta y tus datos fueron eliminados.')
            .then(() => {
              this.authService.logout();
              this.router.navigate(['/login']);
            });
        },
        error: (err) => {
          this.isDeleting.set(false);
          this.swal.error('Error', this.httpError.message(err, 'No se pudo eliminar tu cuenta.'));
        },
      });
  }
}
