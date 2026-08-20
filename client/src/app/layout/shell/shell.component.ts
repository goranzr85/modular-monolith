import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { PERMISSIONS } from '../../core/auth/permissions';
import { ThemeToggleComponent } from '../../shared/ui/theme-toggle/theme-toggle.component';
import { ToastContainerComponent } from '../../shared/ui/toast/toast-container.component';

interface NavItem {
  label: string;
  path: string;
  permissions: readonly string[];
}

const NAV_ITEMS: NavItem[] = [
  { label: 'Catalog', path: '/catalog', permissions: [PERMISSIONS.catalog.create, PERMISSIONS.catalog.update] },
  {
    label: 'Customers',
    path: '/customers',
    permissions: [PERMISSIONS.customer.view, PERMISSIONS.customer.create, PERMISSIONS.customer.update],
  },
  { label: 'Orders', path: '/orders', permissions: [PERMISSIONS.order.create, PERMISSIONS.order.update] },
  { label: 'Warehouse', path: '/warehouse', permissions: [PERMISSIONS.warehouse.update] },
];

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ThemeToggleComponent, ToastContainerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-full flex flex-col">
      <header class="border-b border-border bg-surface">
        <div class="mx-auto flex max-w-6xl items-center gap-6 px-4 py-3">
          <a routerLink="/" class="font-semibold tracking-tight text-text">Operator Console</a>

          <nav class="flex flex-1 items-center gap-1">
            @for (item of visibleNavItems(); track item.path) {
              <a
                [routerLink]="item.path"
                routerLinkActive="bg-accent-soft text-accent"
                class="rounded-md px-3 py-1.5 text-sm font-medium text-text-muted hover:text-text hover:bg-bg transition-colors"
              >
                {{ item.label }}
              </a>
            }
          </nav>

          <app-theme-toggle />

          <div class="flex items-center gap-3 border-l border-border pl-3">
            <span class="text-sm text-text-muted">{{ auth.displayName() }}</span>
            <button
              type="button"
              class="text-sm font-medium text-text-muted hover:text-text"
              (click)="auth.logout()"
            >
              Sign out
            </button>
          </div>
        </div>
      </header>

      <main class="mx-auto w-full max-w-6xl flex-1 px-4 py-6">
        <router-outlet />
      </main>

      <app-toast-container />
    </div>
  `,
})
export class ShellComponent {
  protected readonly auth = inject(AuthService);

  protected visibleNavItems = () => NAV_ITEMS.filter((item) => this.auth.hasAnyPermission(item.permissions));
}
