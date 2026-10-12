import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type SpinnerSize = 'sm' | 'md' | 'lg';

/**
 * Inline loading spinner. Inherits `currentColor`, so it adapts to the button
 * or surface it is placed on. Decorative by default; pass `label` to announce
 * it to assistive tech if there is no surrounding text conveying the state.
 */
@Component({
  selector: 'app-spinner',
  templateUrl: './spinner.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './spinner.component.scss',
})
export class SpinnerComponent {
  readonly size = input<SpinnerSize>('md');
  readonly label = input<string | undefined>(undefined);
}
