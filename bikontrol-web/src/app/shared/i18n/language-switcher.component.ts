import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { I18nService } from './i18n.service';

/** Two-button ES/EN switch that flips the language at runtime. */
@Component({
  selector: 'app-language-switcher',
  template: `
    <div class="flex items-center gap-1 text-xs">
      @for (language of i18n.languages; track language) {
        <button
          type="button"
          (click)="i18n.setLanguage(language)"
          [attr.aria-pressed]="i18n.lang() === language"
          [class]="
            i18n.lang() === language
              ? 'font-bold text-primary underline'
              : 'text-gray-500 hover:underline'
          "
        >
          {{ language === 'es' ? 'ES' : 'EN' }}
        </button>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LanguageSwitcherComponent {
  readonly i18n = inject(I18nService);
}
