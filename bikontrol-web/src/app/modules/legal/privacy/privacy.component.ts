import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/**
 * Public Privacy Policy page. The copy is a starting template (clearly marked as
 * a draft) and must be reviewed/replaced with the definitive legal text before
 * going live.
 */
@Component({
  selector: 'app-privacy',
  imports: [RouterLink],
  templateUrl: './privacy.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PrivacyComponent {
  readonly lastUpdated = '[COMPLETAR: fecha de última actualización]';
  readonly contactEmail = '[COMPLETAR: correo de contacto]';
}
