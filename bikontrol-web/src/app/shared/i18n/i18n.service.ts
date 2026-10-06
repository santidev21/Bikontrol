import { Injectable, signal } from '@angular/core';
import { es } from './translations/es';
import { en } from './translations/en';

export type Language = 'es' | 'en';

const STORAGE_KEY = 'bikontrol.lang';

const DICTIONARIES: Record<Language, Record<string, string>> = { es, en };

/**
 * Minimal runtime i18n: a flat key→string dictionary per language, a signal for
 * the current language and `t()` lookups with `{param}` interpolation. Spanish
 * is the default and the fallback. No external dependency (keeps the app small).
 */
@Injectable({ providedIn: 'root' })
export class I18nService {
  readonly languages: readonly Language[] = ['es', 'en'];
  private readonly current = signal<Language>(readInitialLanguage());
  readonly lang = this.current.asReadonly();

  setLanguage(lang: Language): void {
    if (!DICTIONARIES[lang]) return;
    this.current.set(lang);
    try {
      localStorage.setItem(STORAGE_KEY, lang);
    } catch {
      // Storage can be unavailable (private mode); the switch still works in-memory.
    }
    if (typeof document !== 'undefined') {
      document.documentElement.lang = lang;
    }
  }

  t(key: string, params?: Record<string, unknown>): string {
    let value = DICTIONARIES[this.current()][key] ?? es[key] ?? key;
    if (params) {
      for (const [name, replacement] of Object.entries(params)) {
        value = value.split(`{${name}}`).join(String(replacement));
      }
    }
    return value;
  }
}

function readInitialLanguage(): Language {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored === 'es' || stored === 'en') {
      return stored;
    }
  } catch {
    // Ignore unavailable storage.
  }
  return 'es';
}
