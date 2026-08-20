import { ValidationErrors } from '@angular/forms';

/**
 * Maps Angular's built-in validator error keys (plus this app's custom ones,
 * see custom-validators.ts) to a human-readable message. Used by
 * <app-field-error> so every form gets consistent inline wording without
 * repeating strings per-field.
 */
export function getValidationMessage(errors: ValidationErrors | null, label: string): string | null {
  if (!errors) {
    return null;
  }

  if (errors['required']) return `${label} is required.`;
  if (errors['email']) return `Enter a valid email address.`;
  if (errors['maxlength']) {
    const { requiredLength } = errors['maxlength'];
    return `${label} must be ${requiredLength} characters or fewer.`;
  }
  if (errors['minlength']) {
    const { requiredLength } = errors['minlength'];
    return `${label} must be at least ${requiredLength} characters.`;
  }
  if (errors['min']) {
    const { min } = errors['min'];
    return `${label} must be ${min} or greater.`;
  }
  if (errors['priceNonNegative']) return `${label} can't be negative.`;
  if (errors['pricePositive']) return `${label} must be greater than 0.`;
  if (errors['quantityPositive']) return `${label} must be a whole number greater than 0.`;
  if (errors['atLeastOneContact']) return 'Provide an email or a phone number.';
  if (errors['primaryContactMissing']) return 'The primary contact method must have a value.';

  return `${label} is invalid.`;
}
