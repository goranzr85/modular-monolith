import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type ButtonVariant = 'primary' | 'secondary' | 'danger' | 'ghost';

const BASE =
  'inline-flex items-center justify-center gap-2 rounded-md text-sm font-medium px-4 py-2 ' +
  'transition-colors disabled:opacity-50 disabled:cursor-not-allowed ' +
  'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2';

const VARIANTS: Record<ButtonVariant, string> = {
  primary: 'bg-accent text-accent-contrast hover:bg-accent-hover focus-visible:outline-accent',
  secondary:
    'bg-surface border border-border-strong text-text hover:bg-bg focus-visible:outline-accent',
  danger: 'bg-danger text-white hover:opacity-90 focus-visible:outline-danger',
  ghost: 'text-text-muted hover:text-text hover:bg-bg focus-visible:outline-accent',
};

/**
 * `<button app-button variant="primary" [loading]="saving()">Save</button>`
 * A native <button> host — keeps type="submit"/click handlers working
 * exactly as plain HTML, just styled and loading-aware.
 */
@Component({
  selector: '[app-button]',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (loading()) {
      <svg class="size-4 animate-spin" viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z" />
      </svg>
    }
    <ng-content />
  `,
  host: {
    '[class]': 'hostClasses()',
    '[attr.disabled]': 'loading() || disabled() ? true : null',
    '[attr.aria-busy]': 'loading()',
  },
})
export class ButtonComponent {
  readonly variant = input<ButtonVariant>('primary');
  readonly loading = input(false);
  readonly disabled = input(false);

  protected hostClasses = () => `${BASE} ${VARIANTS[this.variant()]}`;
}
