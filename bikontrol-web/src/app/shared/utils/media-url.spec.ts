import { resolveMediaUrl } from './media-url';

describe('resolveMediaUrl', () => {
  const path = '/api/media/attachments/abc?exp=1&sig=x';

  it('prefixes the API origin for an absolute API url', () => {
    expect(resolveMediaUrl(path, 'http://localhost:5202/api')).toBe(
      'http://localhost:5202/api/media/attachments/abc?exp=1&sig=x',
    );
    expect(resolveMediaUrl(path, 'https://bikontrol.santidev21.tech/api')).toBe(
      'https://bikontrol.santidev21.tech/api/media/attachments/abc?exp=1&sig=x',
    );
  });

  it('resolves against the page origin when the API url is relative (Docker proxy)', () => {
    expect(resolveMediaUrl(path, '/api')).toBe(`${window.location.origin}${path}`);
  });

  it('leaves legacy data URLs and absolute URLs untouched', () => {
    expect(resolveMediaUrl('data:image/png;base64,AAAA', 'http://x/api')).toBe(
      'data:image/png;base64,AAAA',
    );
    expect(resolveMediaUrl('https://cdn.test/a.png', 'http://x/api')).toBe(
      'https://cdn.test/a.png',
    );
  });
});
