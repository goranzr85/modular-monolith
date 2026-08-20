import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrdersWorkspaceStore } from '../data/orders-workspace.store';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { ButtonComponent } from '../../../shared/ui/button/button.component';
import { PERMISSIONS } from '../../../core/auth/permissions';

/** No GET /api/orders exists, so there's no order history here — only a
 *  new-order entry point and whatever this browser session created itself
 *  (docs/frontend-prd.md §2.3, §2.3.5). */
@Component({
  selector: 'app-orders-home',
  standalone: true,
  imports: [RouterLink, HasPermissionDirective, ButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex items-center justify-between">
      <h1 class="text-lg font-semibold text-text">Orders</h1>
      <a *appHasPermission="permissions.order.create" app-button routerLink="new">New order</a>
    </div>
    <p class="mt-1 text-sm text-text-muted">
      This API has no order list or order status lookup — orders you create this session stay available
      below until you reload the page.
    </p>

    @if (store.openOrders().length > 0) {
      <div class="app-card mt-6 divide-y divide-border">
        @for (order of store.openOrders(); track order.orderId) {
          <a [routerLink]="['workspace', order.orderId]" class="flex items-center justify-between px-5 py-3 hover:bg-bg">
            <div>
              <p class="text-sm font-medium text-text">{{ order.customerLabel }}</p>
              <p class="text-xs text-text-muted">{{ order.items.length }} item(s) &middot; {{ order.status }}</p>
            </div>
            <span class="text-xs text-text-faint">{{ order.orderId }}</span>
          </a>
        }
      </div>
    } @else {
      <div class="app-card mt-6 p-5 text-sm text-text-muted">No orders created this session yet.</div>
    }
  `,
})
export class OrdersHomeComponent {
  protected readonly store = inject(OrdersWorkspaceStore);
  protected readonly permissions = PERMISSIONS;
}
