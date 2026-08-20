import { Injectable, signal } from '@angular/core';

export type NotificationKind = 'success' | 'error' | 'info';

export interface AppNotification {
  id: number;
  kind: NotificationKind;
  message: string;
}

let nextId = 1;

/**
 * Signal-based toast queue. Any interceptor, guard, or component can push
 * into it; <app-toast-container> in the shell renders whatever's queued.
 */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly _notifications = signal<AppNotification[]>([]);
  readonly notifications = this._notifications.asReadonly();

  success(message: string): void {
    this.push('success', message);
  }

  error(message: string): void {
    this.push('error', message);
  }

  info(message: string): void {
    this.push('info', message);
  }

  dismiss(id: number): void {
    this._notifications.update((list) => list.filter((n) => n.id !== id));
  }

  private push(kind: NotificationKind, message: string): void {
    const notification: AppNotification = { id: nextId++, kind, message };
    this._notifications.update((list) => [...list, notification]);
    setTimeout(() => this.dismiss(notification.id), kind === 'error' ? 8000 : 4500);
  }
}
