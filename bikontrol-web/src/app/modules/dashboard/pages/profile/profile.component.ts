import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Profile } from '../../interfaces/profile.interface';
import { UserService } from '../../service/user.service';
import { SwalService } from '../../../../shared/services/swal.service';
import { HttpErrorService } from '../../../../shared/services/http-error.service';
import { AuthService } from '../../../auth/services/auth.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export class ProfileComponent implements OnInit {
  profile?: Profile;
  isLoading = true;

  isEditingName = false;
  editableName = '';
  isSavingName = false;

  isPasswordModalOpen = false;
  currentPassword = '';
  newPassword = '';
  confirmPassword = '';
  isChangingPassword = false;

  constructor(
    private userService: UserService,
    private authService: AuthService,
    private router: Router,
    private swal: SwalService,
    private httpError: HttpErrorService
  ) {}

  get isDemo(): boolean {
    return this.authService.isDemo();
  }

  get canChangePassword(): boolean {
    return !!this.profile?.hasPassword && !this.isDemo;
  }

  get initials(): string {
    const name = (this.profile?.fullName ?? '').trim();
    if (!name) return '?';
    const parts = name.split(/\s+/);
    return (parts[0][0] + (parts.length > 1 ? parts[parts.length - 1][0] : '')).toUpperCase();
  }

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.isLoading = true;
    this.userService.getMe().subscribe({
      next: (data) => {
        this.profile = data;
        this.isLoading = false;
      },
      error: (err) => {
        this.isLoading = false;
        this.swal.error('Error', this.httpError.message(err, 'No se pudo cargar tu perfil.'));
      }
    });
  }

  startEditName(): void {
    this.editableName = this.profile?.fullName ?? '';
    this.isEditingName = true;
  }

  cancelEditName(): void {
    this.isEditingName = false;
    this.isSavingName = false;
  }

  saveName(): void {
    if (this.isSavingName) return;
    const fullName = this.editableName.trim();
    if (!fullName) {
      this.swal.error('¡Error!', 'El nombre no puede estar vacío.');
      return;
    }

    this.isSavingName = true;
    this.userService.updateProfile(fullName).subscribe({
      next: (data) => {
        this.profile = data;
        this.cancelEditName();
        this.swal.success('¡Éxito!', 'Se actualizó tu nombre.');
      },
      error: (err) => {
        this.isSavingName = false;
        this.swal.error('Error', this.httpError.message(err, 'No se pudo actualizar tu nombre.'));
      }
    });
  }

  openPasswordModal(): void {
    this.currentPassword = '';
    this.newPassword = '';
    this.confirmPassword = '';
    this.isPasswordModalOpen = true;
  }

  closePasswordModal(): void {
    this.isPasswordModalOpen = false;
    this.isChangingPassword = false;
  }

  changePassword(): void {
    if (this.isChangingPassword) return;

    if (!this.currentPassword || !this.newPassword) {
      this.swal.error('¡Error!', 'Completa todos los campos.');
      return;
    }
    if (this.newPassword.length < 6) {
      this.swal.error('¡Error!', 'La nueva contraseña debe tener al menos 6 caracteres.');
      return;
    }
    if (this.newPassword !== this.confirmPassword) {
      this.swal.error('¡Error!', 'La confirmación no coincide con la nueva contraseña.');
      return;
    }

    this.isChangingPassword = true;
    this.userService.changePassword(this.currentPassword, this.newPassword).subscribe({
      next: () => {
        this.closePasswordModal();
        this.swal.success('¡Éxito!', 'Se actualizó tu contraseña.');
      },
      error: (err) => {
        this.isChangingPassword = false;
        this.swal.error('Error', this.httpError.message(err, 'No se pudo actualizar tu contraseña.'));
      }
    });
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
