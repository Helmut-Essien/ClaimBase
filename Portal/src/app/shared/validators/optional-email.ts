import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Allows a blank staff email. A filled value must contain `@` away from either end.
 * Must match `StaffEmail` on the API. Submit still lowercases the address.
 */
export function optionalEmail(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (typeof control.value !== 'string') {
      return { email: true };
    }

    const trimmed = control.value.trim();
    if (trimmed.length === 0) {
      return null;
    }

    if (!trimmed.includes('@') || trimmed.startsWith('@') || trimmed.endsWith('@')) {
      return { email: true };
    }

    return null;
  };
}
