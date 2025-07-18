import { Component } from '@angular/core';
import { AuthService } from '../auth/auth.service';

@Component({
  selector: 'app-layout',
  templateUrl: './layout.component.html',
  styleUrls: ['./layout.component.scss']
})
export class LayoutComponent {
  username: string = '';

  constructor(private authService: AuthService) {}

  ngOnInit(): void {
    this.username = localStorage.getItem('fullName') ?? 'User';
  }
  logout(): void {
    this.authService.logout();
  }
}
