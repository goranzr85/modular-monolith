import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

export type BannerKind = 'error' | 'success' | 'info' | 'warning';

const KIND_CLASSES: Record<BannerKind, string> = {
  error: 'bg-danger-soft text-danger border-danger/30',
  success: 'bg-success-soft text-success border-success/30',
  info: 'bg-accent-soft text-accent border-accent/30',
  warning: 'bg-warning-soft text-warning border-warning/30',
};

/**
 * Inline, page-scoped message — used for the single-message ProblemDetails
 * `detail` string the backend returns (never a per-field map), so it
 * belongs next to the action it came from, not the global toast queue.
 */
@Component({
  selector: 'app-banner',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div [class]="'flex items-start gap-2 rounded-md border px-3 py-2 text-sm ' + kindClasses()" role="alert">
      <span class="flex-1">{{ message() }}</span>
      @if (dismissible()) {
        <button
          type="button"
          class="shrink-0 opacity-70 hover:opacity-100"
          (click)="dismissed.emit()"
          aria-label="Dismiss"
        >
          ✕
        </button>
      }
    </div>
  `,
})
export class BannerComponent {
  readonly kind = input<BannerKind>('error');
  readonly message = input.required<string>();
  readonly dismissible = input(false);
  readonly dismissed = output<void>();

  protected kindClasses = () => KIND_CLASSES[this.kind()];
}
