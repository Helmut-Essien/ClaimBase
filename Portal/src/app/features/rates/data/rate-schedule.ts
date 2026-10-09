import { AbstractControl, ValidationErrors } from '@angular/forms';

import { RATE_FIELD_LIMITS } from './rates.models';

export { calendarToday } from '../../../shared/dates/calendar-date';

/** A dated amount. The end day is excluded. A null end has not been replaced. */
export interface DatedAmount {
  /** Row id. */
  id: string;
  /** Cedis amount. */
  amount: number;
  /** First day included (`yyyy-MM-dd`). */
  effectiveFrom: string;
  /** First day excluded, or null. */
  effectiveTo: string | null;
}

/**
 * True when `day` falls in the half-open range.
 * An empty end covers every later day. The end date itself does not.
 */
export function rateCovers(rate: DatedAmount, day: string): boolean {
  return rate.effectiveFrom <= day && (rate.effectiveTo === null || day < rate.effectiveTo);
}

/**
 * The amount in force on `day`.
 * A day with no row is a gap, not a zero amount. When more than one row covers the day, the later start wins.
 */
export function rateInForce<T extends DatedAmount>(rates: readonly T[], day: string): T | null {
  let match: T | null = null;
  for (const rate of rates) {
    if (!rateCovers(rate, day)) {
      continue;
    }
    if (match === null || rate.effectiveFrom > match.effectiveFrom) {
      match = rate;
    }
  }
  return match;
}

/**
 * Renders cedis with the tenant currency code and two decimal places.
 * Example: `GHS 1,250.00`.
 */
export function formatMoney(amount: number, currencyCode: string): string {
  const number = new Intl.NumberFormat('en-GH', {
    minimumFractionDigits: RATE_FIELD_LIMITS.amountScale,
    maximumFractionDigits: RATE_FIELD_LIMITS.amountScale,
  }).format(amount);
  return `${currencyCode} ${number}`;
}

/**
 * Rejects a blank amount, a negative amount, and more than two decimal places.
 * Zero is allowed. It is a stored amount, not a gap.
 */
export function cedisAmount(control: AbstractControl): ValidationErrors | null {
  const raw = String(control.value ?? '').trim();
  if (raw.length === 0) {
    return { required: true };
  }
  if (!/^\d{1,16}(\.\d{1,2})?$/.test(raw)) {
    return { amount: true };
  }
  return null;
}
