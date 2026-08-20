import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { NotificationKind, NotificationService } from '../../../core/services/notification.service';

const KIND_CLASSES: Record<NotificationKind, string> = {
  error: 'bg-danger text-white',
  success: 'bg-success text-white',
  info: 'bg-surface-raised text-text border border-border',
};

@Component({
  selector: 'app-toast-container',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="fixed bottom-4 right-4 z-50 flex w-full max-w-sm flex-col gap-2" aria-live="polite">
      @for (n of notifications.notifications(); track n.id) {
        <div
          [class]="'flex items-start gap-3 rounded-md px-4 py-3 text-sm shadow-lg ' + kindClasses(n.kind)"
          role="status"
        >
          <span class="flex-1">{{ n.message }}</span>
          <button
            type="button"
            class="shrink-0 opacity-80 hover:opacity-100"
            (click)="notifications.dismiss(n.id)"
            aria-label="Dismiss notification"
          >
            ✕
          </button>
        </div>
      }
    </div>
  `,
})
export class ToastContainerComponent {
  protected readonly notifications = inject(NotificationService);
  protected kindClasses = (kind: NotificationKind) => KIND_CLASSES[kind];
}
