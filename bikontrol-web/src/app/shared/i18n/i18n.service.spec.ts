import { I18nService } from './i18n.service';

describe('I18nService', () => {
  beforeEach(() => localStorage.clear());

  it('defaults to Spanish', () => {
    const service = new I18nService();

    expect(service.lang()).toBe('es');
    expect(service.t('landing.cta.start')).toBe('Empezar gratis');
  });

  it('switches language at runtime and persists it', () => {
    const service = new I18nService();

    service.setLanguage('en');

    expect(service.lang()).toBe('en');
    expect(service.t('landing.cta.start')).toBe('Start for free');
    expect(localStorage.getItem('bikontrol.lang')).toBe('en');
  });

  it('restores the stored language on init', () => {
    localStorage.setItem('bikontrol.lang', 'en');

    const service = new I18nService();

    expect(service.lang()).toBe('en');
  });

  it('falls back to the key when a translation is missing', () => {
    const service = new I18nService();

    expect(service.t('missing.key')).toBe('missing.key');
  });

  it('interpolates {param} placeholders', () => {
    const service = new I18nService();

    expect(service.t('Hola {name}, tenés {n} avisos', { name: 'Santi', n: 3 })).toBe(
      'Hola Santi, tenés 3 avisos',
    );
  });

  it('ignores an unsupported language', () => {
    const service = new I18nService();

    service.setLanguage('fr' as never);

    expect(service.lang()).toBe('es');
  });
});
