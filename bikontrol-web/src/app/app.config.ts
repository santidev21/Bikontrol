import {
  ApplicationConfig,
  ErrorHandler,
  inject,
  isDevMode,
  provideAppInitializer,
  provideZonelessChangeDetection,
} from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { provideServiceWorker } from '@angular/service-worker';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { authInterceptor } from './shared/interceptors/auth.interceptor';
import { refreshInterceptor } from './shared/interceptors/refresh.interceptor';
import { loadingInterceptor } from './shared/interceptors/loading.interceptor';
import { AppErrorHandler } from './shared/monitoring/app-error-handler';
import { ErrorReporter } from './shared/monitoring/error-reporter';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),
    { provide: ErrorHandler, useClass: AppErrorHandler },
    // Fire and forget: the SDK loads in the background and never delays startup.
    provideAppInitializer(() => {
      void inject(ErrorReporter).init();
    }),
    provideRouter(routes),
    provideHttpClient(
      withFetch(),
      // loading runs first so the spinner stays up across a token-refresh retry.
      withInterceptors([loadingInterceptor, refreshInterceptor, authInterceptor]),
    ),
    provideServiceWorker('ngsw-worker.js', {
      enabled: !isDevMode(),
      registrationStrategy: 'registerWhenStable:30000',
    }),
  ],
};
