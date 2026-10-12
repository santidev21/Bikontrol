import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { LoadingService } from '../../services/loading.service';
import { TranslatePipe } from '../../i18n/translate.pipe';
import { SpinnerComponent } from '../spinner/spinner.component';

/**
 * Global blocking overlay driven by `LoadingService`. Mounted once at the app
 * root so it covers every route (auth, marketing and dashboard).
 */
@Component({
  selector: 'app-loading-overlay',
  imports: [SpinnerComponent, TranslatePipe],
  templateUrl: './loading-overlay.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './loading-overlay.component.scss',
})
export class LoadingOverlayComponent {
  private readonly loading = inject(LoadingService);

  readonly isLoading = this.loading.isLoading;
}
