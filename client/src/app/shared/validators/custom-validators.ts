import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/** Catalog Create allows price === 0 but rejects negatives (backend has no
 *  validator at all here — a negative throws an unhandled 500 server-side,
 *  so this client-side check is the only thing standing between the user
 *  and a raw exception). */
export function priceNonNegative(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (value === null || value === undefined || value === '') return null;
    return Number(value) < 0 ? { priceNonNegative: true } : null;
  };
}

/** Catalog Update's validator requires price strictly > 0. Also used for
 *  Orders line-item price, which has no backend validator but shouldn't
 *  accept 0 either. */
export function pricePositive(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (value === null || value === undefined || value === '') return null;
    return Number(value) <= 0 ? { pricePositive: true } : null;
  };
}

/** Every quantity field on the backend is a C# `uint` — reject negatives,
 *  zero, and decimals before they're ever sent. */
export function quantityPositive(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (value === null || value === undefined || value === '') return null;
    const n = Number(value);
    return Number.isInteger(n) && n > 0 ? null : { quantityPositive: true };
  };
}

/**
 * Cross-field rule mirroring the Customers handler's own guard clause
 * (not a FluentValidation rule — enforced only server-side in the handler,
 * see docs/frontend-prd.md §2.2 Register Customer): at least one of
 * email/phone must be present, and whichever one primaryContactType points
 * at must itself have a value. Attach to the FormGroup, not a single control.
 */
export function customerContactValidator(): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const email = (group.get('email')?.value as string | null)?.trim();
    const phone = (group.get('phone')?.value as string | null)?.trim();
    const primaryContactType = group.get('primaryContactType')?.value as 0 | 1 | null;

    if (!email && !phone) {
      return { atLeastOneContact: true };
    }

    const primaryHasValue = primaryContactType === 1 ? !!phone : !!email;
    if (!primaryHasValue) {
      return { primaryContactMissing: true };
    }

    return null;
  };
}
