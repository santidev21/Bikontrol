import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { TranslatePipe } from '../../shared/i18n/translate.pipe';
import { LanguageSwitcherComponent } from '../../shared/i18n/language-switcher.component';

export const AUTH_IMPORTS = [
  CommonModule,
  FormsModule,
  ReactiveFormsModule,
  RouterModule,
  TranslatePipe,
  LanguageSwitcherComponent,
];
