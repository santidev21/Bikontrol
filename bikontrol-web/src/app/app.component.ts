import { Component, OnInit, ChangeDetectionStrategy, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { UpdateService } from './shared/services/update.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './app.component.scss',
})
export class AppComponent implements OnInit {
  private updateService = inject(UpdateService);

  title = 'bikontrol-web';

  ngOnInit(): void {
    this.updateService.init();
    this.updateService.checkForUpdate();
  }
}
