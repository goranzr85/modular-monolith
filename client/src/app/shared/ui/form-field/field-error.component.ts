import { ChangeDetectionStrategy, Component, computed, effect, input, signal } from '@angular/core';
import { AbstractControl } from '@angular/forms';
import { getValidationMessage } from '../../validators/validation-messages';

/**
 * Real-time inline validation message for a single control. Hidden until
 * the field is touched (or `forceShow` is set after a failed submit
 * attempt, to reveal every remaining error at once), then updates live —
 * `control.events` fires on every value/status/touched change, which is
 * what keeps this in sync under OnPush without an async pipe.
 */
@Component({
  selector: 'app-field-error',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (message(); as msg) {
      <p class="mt-1 text-xs text-danger">{{ msg }}</p>
    }
  `,
})
export class FieldErrorComponent {
  readonly control = input.required<AbstractControl | null>();
  readonly label = input.required<string>();
  readonly forceShow = input(false);

  private readonly tick = signal(0);

  constructor() {
    effect((onCleanup) => {
      const control = this.control();
      if (!control) return;
      const subscription = control.events.subscribe(() => this.tick.update((n) => n + 1));
      onCleanup(() => subscription.unsubscribe());
    });
  }

  protected message = computed<string | null>(() => {
    this.tick();
    const control = this.control();
    if (!control) return null;
    if (!control.touched && !this.forceShow()) return null;
    if (control.valid) return null;
    return getValidationMessage(control.errors, this.label());
  });
}
