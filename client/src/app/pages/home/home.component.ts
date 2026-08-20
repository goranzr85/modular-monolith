import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { PERMISSIONS } from '../../core/auth/permissions';

interface HomeCard {
  title: string;
  description: string;
  path: string;
  permissions: readonly string[];
}

const CARDS: HomeCard[] = [
  {
    title: 'Catalog',
    description: 'Create and update product listings.',
    path: '/catalog',
    permissions: [PERMISSIONS.catalog.create, PERMISSIONS.catalog.update],
  },
  {
    title: 'Customers',
    description: 'Register customers and manage their details.',
    path: '/customers',
    permissions: [PERMISSIONS.customer.view, PERMISSIONS.customer.create, PERMISSIONS.customer.update],
  },
  {
    title: 'Orders',
    description: 'Place orders and drive them through fulfillment.',
    path: '/orders',
    permissions: [PERMISSIONS.order.create, PERMISSIONS.order.update],
  },
  {
    title: 'Warehouse',
    description: 'Receive, ship, and adjust stock on hand.',
    path: '/warehouse',
    permissions: [PERMISSIONS.warehouse.update],
  },
];

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1 class="text-xl font-semibold text-text">Welcome, {{ auth.displayName() }}</h1>
    <p class="mt-1 text-sm text-text-muted">Pick an area to work in.</p>

    @if (visibleCards().length > 0) {
      <div class="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-2">
        @for (card of visibleCards(); track card.path) {
          <a
            [routerLink]="card.path"
            class="app-card block p-5 transition-colors hover:border-accent"
          >
            <h2 class="font-medium text-text">{{ card.title }}</h2>
            <p class="mt-1 text-sm text-text-muted">{{ card.description }}</p>
          </a>
        }
      </div>
    } @else {
      <div class="app-card mt-6 p-5 text-sm text-text-muted">
        Your account doesn't have access to any area yet. Contact an administrator to have a role assigned.
      </div>
    }
  `,
})
export class HomeComponent {
  protected readonly auth = inject(AuthService);
  protected visibleCards = () => CARDS.filter((c) => this.auth.hasAnyPermission(c.permissions));
}
