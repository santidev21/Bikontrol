import { ErrorHandler, Injectable, inject } from '@angular/core';
import { ErrorReporter } from './error-reporter';

/** Angular's default behaviour (log to the console) plus a report to the error tracker. */
@Injectable()
export class AppErrorHandler implements ErrorHandler {
  private readonly reporter = inject(ErrorReporter);

  handleError(error: unknown): void {
    console.error(error);
    this.reporter.report(error);
  }
}
