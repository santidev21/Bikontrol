import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/**
 * Public Terms and Conditions page. The copy is a starting template (clearly
 * marked as a draft) and must be reviewed/replaced with the definitive legal
 * text before going live.
 */
@Component({
  selector: 'app-terms',
  imports: [RouterLink],
  templateUrl: './terms.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TermsComponent {
  readonly lastUpdated = '[COMPLETAR: fecha de última actualización]';
  readonly contactEmail = '[COMPLETAR: correo de contacto]';
}
