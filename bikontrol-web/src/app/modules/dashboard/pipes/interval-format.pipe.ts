import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'intervalFormat',
  standalone: true
})
export class IntervalFormatPipe implements PipeTransform {

  /**
   * Transforms a number of weeks into a readable interval string.
   * Rules:
   * - 1 week → "1 semana"
   * - 2–4 weeks → "X semanas"
   * - >4 weeks → convert to months (1 month = 4 weeks)
   *   Example: 6 weeks → "1.5 meses"
   */
  transform(weeks: number | null | undefined): string {
    weeks = parseInt(weeks as any, 10);
    
    if (!weeks || weeks <= 0) return 'N/A';

    if (weeks === 1) return 'Semanal';

    if (weeks === 2) return '15 dias';

    if (weeks === 3) return '3 semanas';

    const months = weeks / 4;
    const rounded = this.roundNumber(months);

    return `${rounded} meses`;
  }

  /**
   * Rounds to 1 decimal max, removing ".0" when integer.
   */
  private roundNumber(value: number): string {
    const rounded = Math.round(value * 10) / 10;
    return Number.isInteger(rounded) ? rounded.toString() : rounded.toFixed(1);
  }
}
