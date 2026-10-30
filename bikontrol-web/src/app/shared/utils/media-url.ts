import { environment } from '@env/environment';

const MEDIA_PREFIX = '/api/media/';

/**
 * The API returns stored images as a path (`/api/media/attachments/{id}?exp&sig`)
 * because it cannot know the public origin it is served from. Resolve it against
 * the API origin so `<img src>` works in every setup: production and the Docker
 * proxy (same origin), and native dev (API on another port). Anything else — an
 * absolute URL or a legacy data URL — is returned untouched.
 */
export function resolveMediaUrl(url: string, apiUrl: string = environment.apiUrl): string {
  if (!url.startsWith(MEDIA_PREFIX)) return url;
  return new URL(apiUrl, window.location.origin).origin + url;
}
