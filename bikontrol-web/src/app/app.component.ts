import { Component, OnInit } from '@angular/core';
import { AuthService } from './modules/auth/auth.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  title = 'bikontrol-web';

  constructor(private authService: AuthService){}

  ngOnInit() {
  if (this.authService.isTokenExpired()) {
    this.authService.logout();
  }
}
}
