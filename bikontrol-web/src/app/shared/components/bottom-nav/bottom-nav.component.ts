import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterModule } from '@angular/router';
import { TranslatePipe } from '../../i18n/translate.pipe';

@Component({
  selector: 'app-bottom-nav',
  imports: [RouterModule, TranslatePipe],
  templateUrl: './bottom-nav.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './bottom-nav.component.scss',
})
export class BottomNavComponent {}
