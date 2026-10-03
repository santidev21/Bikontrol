import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { SwPush } from '@angular/service-worker';
import { environment } from '@env/environment';
import { Observable, from, switchMap } from 'rxjs';

/**
 * Web Push for maintenance reminders. Subscribing asks the browser for
 * permission, then registers the resulting subscription with the API. The
 * service worker (Angular `ngsw`) handles displaying the notification and
 * opening the app on click.
 */
@Injectable({ providedIn: 'root' })
export class PushService {
  private http = inject(HttpClient);
  private swPush = inject(SwPush);

  private apiUrl = `${environment.apiUrl}/reminders/push`;

  readonly isSupported = this.swPush.isEnabled;

  /** Current browser subscription, if any. */
  getSubscription(): Observable<PushSubscription | null> {
    return this.swPush.subscription;
  }

  /**
   * Requests permission and registers the subscription with the backend.
   * Rejects if the user denies permission or the browser does not support push.
   */
  subscribe(): Observable<unknown> {
    return from(
      this.http.get<{ publicKey: string | null }>(`${this.apiUrl}/vapid-public-key`),
    ).pipe(
      switchMap(({ publicKey }) => {
        if (!publicKey) {
          throw new Error('Las notificaciones no están disponibles en este servidor.');
        }
        return from(this.swPush.requestSubscription({ serverPublicKey: publicKey }));
      }),
      switchMap((sub) =>
        this.http.post(`${this.apiUrl}/subscribe`, {
          endpoint: sub.endpoint,
          keys: sub.toJSON().keys,
        }),
      ),
    );
  }

  /** Unsubscribes locally and removes the subscription on the backend. */
  unsubscribe(): Observable<unknown> {
    return this.getSubscription().pipe(
      switchMap((sub) => {
        const endpoint = sub?.endpoint;
        return from(this.swPush.unsubscribe()).pipe(
          switchMap(() => {
            if (!endpoint) {
              return from(Promise.resolve());
            }
            return this.http.post(`${this.apiUrl}/unsubscribe`, { endpoint });
          }),
        );
      }),
    );
  }
}
