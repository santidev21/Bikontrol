import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { UpdateService } from './shared/services/update.service';

@Component({
    selector: 'app-root',
    imports: [RouterOutlet],
    templateUrl: './app.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  title = 'bikontrol-web';

  constructor(private updateService: UpdateService) {}

  ngOnInit(): void {
    this.updateService.init();
    this.updateService.checkForUpdate();
  }
}
