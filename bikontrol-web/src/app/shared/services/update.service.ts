import { Injectable, OnDestroy, signal } from '@angular/core';
import { SwUpdate, VersionReadyEvent } from '@angular/service-worker';
import { Subscription, filter } from 'rxjs';
import { environment } from '@env/environment';
import { SwalService } from './swal.service';

@Injectable({
  providedIn: 'root'
})
export class UpdateService implements OnDestroy {
  /** Etiqueta legible de la versión (se sube solo en releases con cambios visibles). */
  readonly appVersion: string = environment.appVersion;

  /** Hash corto de la versión servida por el service worker (null si se desconoce). */
  readonly swVersion = signal<string | null>(null);

  private versionSub?: Subscription;
  private promptShown = false;

  constructor(
    private swUpdate: SwUpdate,
    private swal: SwalService
  ) {}

  init(): void {
    if (!this.swUpdate.isEnabled) return;
    this.refreshSwVersion();
    this.versionSub = this.swUpdate.versionUpdates
      .pipe(filter((e): e is VersionReadyEvent => e.type === 'VERSION_READY'))
      .subscribe((event) => this.onVersionReady(event));
    document.addEventListener('visibilitychange', this.onVisibilityChange);
  }

  checkForUpdate(): void {
    if (!this.swUpdate.isEnabled) return;
    this.swUpdate.checkForUpdate().catch(() => undefined);
  }

  private onVisibilityChange = (): void => {
    if (document.visibilityState === 'visible') {
      this.checkForUpdate();
    }
  };

  private async refreshSwVersion(): Promise<void> {
    try {
      const response = await fetch('ngsw.json', { cache: 'no-store' });
      if (!response.ok) return;
      const data = await response.json();
      if (typeof data?.hash === 'string') {
        this.swVersion.set(data.hash.slice(0, 7));
      }
    } catch {
      // Sin red o SW aún no instalado: se ignora, el hash llegará con los eventos.
    }
  }

  private async onVersionReady(event: VersionReadyEvent): Promise<void> {
    const hash = event.latestVersion?.hash;
    if (typeof hash === 'string' && hash.length > 0) {
      this.swVersion.set(hash.slice(0, 7));
    }
    if (this.promptShown) return;
    this.promptShown = true;

    const result = await this.swal.confirm(
      'Nueva versión disponible',
      'Hay una actualización de Bikontrol. Recarga para aplicarla cuando quieras.',
      'Recargar ahora',
      'Más tarde'
    );

    if (result.isConfirmed) {
      await this.swUpdate.activateUpdate();
      this.reloadApp();
    } else {
      this.promptShown = false;
    }
  }

  private reloadApp(): void {
    location.reload();
  }

  ngOnDestroy(): void {
    this.versionSub?.unsubscribe();
    document.removeEventListener('visibilitychange', this.onVisibilityChange);
  }
}
